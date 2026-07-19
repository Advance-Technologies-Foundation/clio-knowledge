using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ReferenceExampleBundleTests
{
    private static readonly ReferenceExampleExpectation[] RegisteredExamples =
    [
        new(
            "atf.creatio.google-pubsub-reference",
            "catalog/reference-examples/creatio-google-pubsub.yaml",
            "resources/atf.creatio.google-pubsub-reference.yaml",
            "https://github.com/Advance-Technologies-Foundation/creatio-google-pubsub-reference",
            "a2b1a3454f7f74d96cc038c69db30738ea302990"),
        new(
            "atf.creatio.kafka-reference",
            "catalog/reference-examples/creatio-kafka.yaml",
            "resources/atf.creatio.kafka-reference.yaml",
            "https://github.com/Advance-Technologies-Foundation/creatio-kafka-reference",
            "a1770613923ed48bea547a67be466e663feea1ef")
    ];

    [Test]
    [Description("Publishes every registered reference example as a signed, canonical, byte-identical YAML resource.")]
    public void KnowledgeBundle_ShouldContainCanonicalReferenceExamples_WhenCatalogEntriesAreRegistered()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "bundle-source.json")));
        JsonElement[] sourceExamples = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "reference-example")
            .ToArray();
        using ZipArchive bundle = ZipFile.OpenRead(Path.Combine(repositoryRoot, "knowledge-bundle.zip"));
        byte[] manifestBytes = ReadEntry(bundle, "manifest.json");
        using JsonDocument manifest = JsonDocument.Parse(manifestBytes);
        JsonElement[] manifestExamples = manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "reference-example")
            .ToArray();
        using ECDsa publicKey = ECDsa.Create();
        publicKey.ImportFromPem(File.ReadAllText(Path.Combine(
            repositoryRoot,
            "fixtures/keys/p1-test-public.pem")));

        // Act
        bool signatureIsValid = publicKey.VerifyData(
            manifestBytes,
            ReadEntry(bundle, "manifest.sig"),
            HashAlgorithmName.SHA256);

        // Assert
        signatureIsValid.Should().BeTrue(
            because: "reference-example metadata must be covered by the same detached signature as guidance");
        sourceExamples.Should().HaveCount(RegisteredExamples.Length,
            because: "each registered catalog entry must have exactly one publication descriptor");
        manifestExamples.Should().HaveCount(RegisteredExamples.Length,
            because: "the signed bundle must expose every registered reference example without cloning its repository");

        foreach (ReferenceExampleExpectation expected in RegisteredExamples)
        {
            JsonElement sourceExample = sourceExamples.Should()
                .ContainSingle(
                    resource => resource.GetProperty("itemId").GetString() == expected.ItemId,
                    because: $"catalog item '{expected.ItemId}' must retain its stable identity")
                .Which;
            JsonElement manifestExample = manifestExamples.Should()
                .ContainSingle(
                    resource => resource.GetProperty("itemId").GetString() == expected.ItemId,
                    because: $"signed manifest must expose catalog item '{expected.ItemId}'")
                .Which;
            string expectedUri = BundleBuilder.CreateCanonicalUri("com.creatio.clio", expected.ItemId);
            byte[] canonicalSourceBytes = CanonicalBytes(repositoryRoot, expected.SourcePath);
            byte[] bundledBytes = ReadEntry(bundle, expected.BundlePath);
            string catalogText = Encoding.UTF8.GetString(canonicalSourceBytes);

            sourceExample.GetProperty("topicId").GetString().Should().Be(expected.ItemId,
                because: "the catalog stable ID is also the logical reference-example topic");
            sourceExample.GetProperty("sourcePath").GetString().Should().Be(expected.SourcePath,
                because: "publication must read the human-authored catalog entry");
            sourceExample.GetProperty("bundlePath").GetString().Should().Be(expected.BundlePath,
                because: "the transport-neutral bundle path must be deterministic");
            sourceExample.GetProperty("mediaType").GetString().Should().Be("text/yaml",
                because: "reference metadata remains human-readable YAML inside every transport");
            manifestExample.GetProperty("uri").GetString().Should().Be(expectedUri,
                because: "agents need an exact namespaced URI independent of transport or local checkout state");
            bundledBytes.Should().Equal(canonicalSourceBytes,
                because: "the signed resource must preserve the canonical catalog content byte-for-byte");
            manifestExample.GetProperty("length").GetInt64().Should().Be(canonicalSourceBytes.LongLength,
                because: "the signed manifest length must describe the exact catalog payload");
            manifestExample.GetProperty("digest").GetString().Should().Be(
                Convert.ToHexStringLower(SHA256.HashData(canonicalSourceBytes)),
                because: "the signed manifest digest must bind the exact catalog payload");
            catalogText.Should().Contain($"repository: {expected.Repository}",
                because: "discovery must return the exact repository an agent can choose to clone");
            catalogText.Should().Contain($"revision: {expected.Revision}",
                because: "published examples must pin immutable repository evidence");
        }
    }

    private static byte[] CanonicalBytes(string repositoryRoot, string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(repositoryRoot, relativePath), new UTF8Encoding(false, true));
        return Encoding.UTF8.GetBytes(BundleBuilder.CanonicalizeText(text.TrimStart('\uFEFF')));
    }

    private static byte[] ReadEntry(ZipArchive archive, string path)
    {
        using Stream stream = archive.GetEntry(path)!.Open();
        using MemoryStream result = new();
        stream.CopyTo(result);
        return result.ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }

    private sealed record ReferenceExampleExpectation(
        string ItemId,
        string SourcePath,
        string BundlePath,
        string Repository,
        string Revision);
}

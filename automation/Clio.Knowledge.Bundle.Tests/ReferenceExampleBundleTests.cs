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
            "https://github.com/Advance-Technologies-Foundation/creatio-google-pubsub-reference",
            "a2b1a3454f7f74d96cc038c69db30738ea302990"),
        new(
            "atf.creatio.kafka-reference",
            "catalog/reference-examples/creatio-kafka.yaml",
            "https://github.com/Advance-Technologies-Foundation/creatio-kafka-reference",
            "a1770613923ed48bea547a67be466e663feea1ef")
    ];

    [Test]
    [Description("Publishes every registered reference example directly from its canonical YAML source file.")]
    public void RepositoryManifest_ShouldPublishCanonicalReferenceExamples_WhenCatalogEntriesAreRegistered()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "bundle-source.json")));

        // Act
        JsonElement[] sourceExamples = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "reference-example")
            .ToArray();

        // Assert
        sourceExamples.Should().HaveCount(RegisteredExamples.Length,
            because: "each registered catalog entry must have exactly one publication descriptor");
        foreach (ReferenceExampleExpectation expected in RegisteredExamples)
        {
            JsonElement sourceExample = sourceExamples.Should()
                .ContainSingle(
                    resource => resource.GetProperty("itemId").GetString() == expected.ItemId,
                    because: $"catalog item '{expected.ItemId}' must retain its stable identity")
                .Which;
            string sourcePath = sourceExample.GetProperty("sourcePath").GetString()!;
            string catalogText = File.ReadAllText(Path.Combine(repositoryRoot, sourcePath));

            sourcePath.Should().Be(expected.SourcePath,
                because: "Git consumers must read the human-authored catalog entry directly");
            sourceExample.GetProperty("uri").GetString().Should().Be(
                BundleBuilder.CreateCanonicalUri("com.creatio.clio", expected.ItemId),
                because: "agents need one exact namespaced URI independent of the local checkout path");
            sourceExample.GetProperty("mediaType").GetString().Should().Be("text/yaml",
                because: "reference metadata remains human-readable source content");
            catalogText.Should().Contain($"repository: {expected.Repository}",
                because: "discovery must return the repository an agent can choose to clone");
            catalogText.Should().Contain($"revision: {expected.Revision}",
                because: "published examples must pin immutable repository evidence");
        }
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
        string Repository,
        string Revision);
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the invariant a consumer relies on to accept a candidate: one generation label, one set
/// of bytes.
/// </summary>
/// <remarks>
/// Clio refuses a candidate whose sequence equals the active one while its content digest differs,
/// and it rolls the whole checkout back rather than skipping the changed article. Publishing edited
/// content under an unchanged <c>sequence</c> therefore does not degrade one topic — it makes the
/// entire library unactivatable for anyone who already synced the earlier bytes.
/// </remarks>
[TestFixture]
public sealed class PublishedGenerationTests
{
    // ASCII unit separator: cannot occur inside an identifier, URI, or path, so no field value can
    // impersonate a field boundary and mask a change.
    private const char Separator = '\u001F';

    // Bump both of these together with libraryVersion/sequence in bundle-source.json whenever
    // published content or resource metadata changes. A failure here is not a broken test: it means
    // the working tree publishes different bytes than the recorded generation claims.
    private const ulong PublishedSequence = 9;
    private const string PublishedContentDigest =
        "447FCED98A7EA33D1F42561B161F648661F5AF2AD0F9CC9BB5C18308746FDE61";

    [Test]
    [Description("Verifies that the published content digest still matches the generation the repository declares, so edited content can never ship under a reused sequence.")]
    public void PublishedContent_ShouldMatchDeclaredGeneration()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement root = source.RootElement;

        // Act
        ulong declaredSequence = root.GetProperty("sequence").GetUInt64();
        string digest = ComputeContentDigest(repositoryRoot, root);

        // Assert
        declaredSequence.Should().Be(PublishedSequence,
            because: "the recorded generation and the manifest must describe the same publication");
        digest.Should().Be(PublishedContentDigest,
            because: "published bytes changed without a new sequence, which Clio rejects as InvalidContent "
                + "and recovers from by rolling the checkout back — bump libraryVersion and sequence, "
                + "then record the new digest here");
    }

    // Hashes exactly what a consumer sees: every resource's identity, routing, gating, and body
    // bytes, in a stable order. Deliberately independent of the signed artifact, whose ECDSA
    // signature is not reproducible across runs.
    private static string ComputeContentDigest(string repositoryRoot, JsonElement root)
    {
        StringBuilder canonical = new();
        IOrderedEnumerable<JsonElement> resources = root.GetProperty("resources")
            .EnumerateArray()
            .OrderBy(resource => resource.GetProperty("itemId").GetString(), StringComparer.Ordinal);
        foreach (JsonElement resource in resources)
        {
            canonical.Append(resource.GetProperty("itemId").GetString()).Append(Separator);
            canonical.Append(resource.GetProperty("topicId").GetString()).Append(Separator);
            canonical.Append(resource.GetProperty("role").GetString()).Append(Separator);
            canonical.Append(resource.GetProperty("uri").GetString()).Append(Separator);
            canonical.Append(Join(resource, "legacyUris")).Append(Separator);
            canonical.Append(Join(resource, "requiredFeatures")).Append(Separator);
            canonical.Append(resource.GetProperty("mediaType").GetString()).Append(Separator);
            string sourcePath = resource.GetProperty("sourcePath").GetString()!;
            canonical.Append(sourcePath).Append(Separator);
            byte[] body = File.ReadAllBytes(Path.Combine(
                repositoryRoot,
                sourcePath.Replace('/', Path.DirectorySeparatorChar)));
            canonical.Append(Convert.ToHexString(SHA256.HashData(body))).Append(Separator);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static string Join(JsonElement resource, string property) =>
        resource.TryGetProperty(property, out JsonElement value)
            ? string.Join(',', value.EnumerateArray().Select(item => item.GetString()))
            : string.Empty;

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
}

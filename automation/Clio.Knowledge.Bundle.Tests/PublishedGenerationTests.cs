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
    // ASCII unit separator: cannot occur inside an identifier, URI, or path, and BundleBuilder
    // refuses to publish a title or description carrying any control character, so no hashed field
    // value can impersonate a field boundary and mask a change.
    private const char Separator = '\u001F';

    // Bump both of these together with libraryVersion/sequence in bundle-source.json whenever
    // published content or resource metadata changes. A failure here is not a broken test: it means
    // the working tree publishes different bytes than the recorded generation claims.
    private const ulong PublishedSequence = 10;
    private const string PublishedContentDigest =
        "F4928784AD66711F432539FE322E205727FFB2189CEF50968AD4ADB796D16D5B";

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

    // Hashes exactly what a consumer sees: every resource's identity, selection metadata, routing,
    // gating, and body bytes, in a stable order.
    //
    // Covered: itemId, title, description, topicId, role, uri, legacyUris, requiredFeatures,
    // mediaType, sourcePath, and the SHA-256 of the body at that path. Title and description are in
    // scope because they are the resources/list selection signal — an agent chooses an article from
    // them, so rewriting one changes what a consumer gets even when the body is byte-identical.
    //
    // Not covered: bundlePath, a packaging detail no consumer reads; the manifest-level
    // contractVersion, bundleSchemaVersion, libraryId, compatibility, and requirements fields; and
    // the signed artifact, whose ECDSA signature is not reproducible across runs. sequence and
    // libraryVersion are excluded by design — sequence is the generation label this digest is
    // compared against, so hashing it would let a version bump mask a content change.
    private static string ComputeContentDigest(string repositoryRoot, JsonElement root)
    {
        StringBuilder canonical = new();
        IOrderedEnumerable<JsonElement> resources = root.GetProperty("resources")
            .EnumerateArray()
            .OrderBy(resource => resource.GetProperty("itemId").GetString(), StringComparer.Ordinal);
        foreach (JsonElement resource in resources)
        {
            canonical.Append(resource.GetProperty("itemId").GetString()).Append(Separator);
            canonical.Append(resource.GetProperty("title").GetString()).Append(Separator);
            canonical.Append(resource.GetProperty("description").GetString()).Append(Separator);
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

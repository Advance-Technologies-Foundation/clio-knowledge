using System.Buffers.Binary;
using System.Security.Cryptography;
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
    // Bump both of these together with libraryVersion/sequence in bundle-source.json whenever any
    // manifest byte or any published body changes. A failure here is not a broken test: it means the
    // working tree publishes different bytes than the recorded generation claims.
    private const ulong PublishedSequence = 31;
    private const string PublishedContentDigest =
        "ED18E5FA4BB3D646347F90C7667E7918D9BE94AFA31CBB7839D9986A8739EF0E";

    [Test]
    [Description("Verifies that the published content digest still matches the generation the repository declares, so edited content can never ship under a reused sequence.")]
    public void PublishedContent_ShouldMatchDeclaredGeneration()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        byte[] manifestBytes = File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json"));
        using JsonDocument source = JsonDocument.Parse(manifestBytes);

        // Act
        ulong declaredSequence = source.RootElement.GetProperty("sequence").GetUInt64();
        string digest = ComputeContentDigest(repositoryRoot, manifestBytes, source.RootElement);

        // Assert
        declaredSequence.Should().Be(PublishedSequence,
            because: "the recorded generation and the manifest must describe the same publication");
        digest.Should().Be(PublishedContentDigest,
            because: "published bytes changed without a new sequence, which Clio rejects as InvalidContent "
                + "and recovers from by rolling the checkout back — bump libraryVersion and sequence, "
                + "then record the new digest here");
    }

    // Recomputes the value Clio itself derives in KnowledgeGitRepositoryReader.TryRead and keeps as
    // KnowledgeGitRepositorySnapshot.ContentDigest: one SHA-256 over the framed raw bytes of
    // bundle-source.json, then the framed bytes of every resource body in manifest DECLARATION
    // order. Reproducing the consumer's formula instead of projecting selected fields is the whole
    // point of the guard — a projection stays green for any manifest edit it does not happen to read
    // (key order, formatting, compatibility ranges, requirements, libraryId, or a reordered resource
    // array), while the consumer computes a different digest and refuses the entire library.
    //
    // Covered: every byte of the manifest, so contractVersion, bundleSchemaVersion, libraryId,
    // libraryVersion, sequence, compatibility, requirements, and each resource descriptor — itemId,
    // title, description, topicId, role, uri, legacyUris, requiredFeatures, sourcePath, bundlePath,
    // mediaType — along with their declaration order and the surrounding JSON formatting; plus the
    // body bytes behind every declared sourcePath.
    //
    // Not covered: repository files no manifest resource declares, and the signed distribution
    // artifact, whose ECDSA signature is not reproducible across runs.
    private static string ComputeContentDigest(string repositoryRoot, byte[] manifestBytes, JsonElement root)
    {
        using IncrementalHash digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendFramed(digest, manifestBytes);
        foreach (JsonElement resource in root.GetProperty("resources").EnumerateArray())
        {
            string sourcePath = resource.GetProperty("sourcePath").GetString()!;
            AppendFramed(digest, File.ReadAllBytes(Path.Combine(
                repositoryRoot,
                sourcePath.Replace('/', Path.DirectorySeparatorChar))));
        }
        return Convert.ToHexString(digest.GetHashAndReset());
    }

    // The framing is part of the hashed value, so it mirrors KnowledgeGitRepositoryReader.AppendFramed
    // byte for byte: an eight-byte little-endian length ahead of the content, which stops the boundary
    // between two adjoining bodies from being forged by moving bytes across it.
    private static void AppendFramed(IncrementalHash digest, ReadOnlySpan<byte> content)
    {
        Span<byte> length = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(length, content.Length);
        digest.AppendData(length);
        digest.AppendData(content);
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
}

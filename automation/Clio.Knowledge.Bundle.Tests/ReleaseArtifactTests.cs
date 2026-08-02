using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Covers the artifact contract the GitHub Release pipeline publishes and Clio consumes.
/// </summary>
/// <remarks>
/// These build the real <c>bundle-source.json</c> rather than a fixture: the guarantees under test —
/// that a release carries runtime content and nothing else, and that rebuilding the same commit
/// produces the same content — are only meaningful for the artifact that actually ships.
/// </remarks>
[TestFixture]
public sealed class ReleaseArtifactTests
{
    private const string KeyId = "release-artifact-test-key";

    private string _directory = null!;
    private string _repositoryRoot = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "clio-knowledge-release-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _repositoryRoot = FindRepositoryRoot();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Test]
    [Description("Verifies the release artifact contains exactly the declared runtime content and no repository material.")]
    public void ReleaseBundle_ShouldContainOnlyDeclaredRuntimeContent()
    {
        // Arrange
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string outputPath = Path.Combine(_directory, "clio-knowledge-bundle.zip");

        // Act
        BundleBuildResult result = Build(key, outputPath);
        using ZipArchive archive = ZipFile.OpenRead(outputPath);
        string[] entries = archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray();
        string[] expected = new[] { "manifest.json", "manifest.sig" }
            .Concat(result.Manifest.Resources.Select(resource => resource.Path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        entries.Should().Equal(expected,
            because: "a public release artifact must carry the signed manifest and the declared resources only");
        entries.Should().OnlyContain(
            entry => entry == "manifest.json"
                || entry == "manifest.sig"
                || entry.StartsWith("resources/", StringComparison.Ordinal),
            because: "automation sources, test projects, fixtures, migration evidence, and key material "
                + "must never be swept into a published artifact");
        entries.Should().NotContain(entry => entry.Contains(".git", StringComparison.Ordinal),
            because: "repository metadata is not part of the runtime delivery contract");
    }

    [Test]
    [Description("Verifies that rebuilding the same source produces byte-identical manifest, resources, and archive layout.")]
    public void ReleaseBundle_ShouldRebuildDeterministically_ExceptForItsDetachedSignature()
    {
        // Arrange
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string firstPath = Path.Combine(_directory, "first.zip");
        string secondPath = Path.Combine(_directory, "second.zip");

        // Act
        BundleBuildResult first = Build(key, firstPath);
        BundleBuildResult second = Build(key, secondPath);
        IReadOnlyList<ArchiveEntrySnapshot> firstEntries = Snapshot(firstPath);
        IReadOnlyList<ArchiveEntrySnapshot> secondEntries = Snapshot(secondPath);

        // Assert
        second.ManifestBytes.Should().Equal(first.ManifestBytes,
            because: "the manifest is the signed identity, so the same source must always produce the same bytes");
        firstEntries.Where(entry => entry.Path != "manifest.sig").Should().Equal(
            secondEntries.Where(entry => entry.Path != "manifest.sig"),
            because: "entry order, timestamps, attributes, and content must not depend on when or where the build ran");
        firstEntries.Select(entry => entry.Path).Should().Equal(
            secondEntries.Select(entry => entry.Path),
            because: "the archive layout is part of the reproducible contract");
        // ECDSA signatures embed a random nonce, so `manifest.sig` — and therefore the archive hash —
        // differs per build. That is not a reproducibility gap for the consumer: identity is
        // (libraryId, sequence, bundleDigest) over the manifest, and transport integrity is the digest
        // GitHub publishes for the one uploaded artifact, not a digest anyone recomputes from source.
        second.ArtifactSha256.Should().NotBeNullOrWhiteSpace(
            because: "each build still reports the digest of the exact artifact it produced");
    }

    [Test]
    [Description("Verifies the built release artifact passes the same verification the release pipeline runs before publishing.")]
    public void ReleaseBundle_ShouldVerifyAgainstItsPublisherKey()
    {
        // Arrange
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string outputPath = Path.Combine(_directory, "clio-knowledge-bundle.zip");
        BundleBuildResult built = Build(key, outputPath);

        // Act
        BundleVerificationResult verified = new BundleVerifier().Verify(
            outputPath,
            key.ExportSubjectPublicKeyInfoPem(),
            KeyId,
            built.Manifest.LibraryVersion);

        // Assert
        verified.LibraryId.Should().Be(built.Manifest.LibraryId,
            because: "verification must confirm the library identity the release claims to ship");
        verified.Sequence.Should().Be(built.Manifest.Sequence,
            because: "the monotonic sequence is the generation ordering a consumer enforces");
        verified.ArtifactSha256.Should().Be(built.ArtifactSha256,
            because: "the digest the pipeline publishes must be the digest of the artifact it verified");
        verified.ResourceCount.Should().Be(built.Manifest.Resources.Count,
            because: "every declared resource must be present in the archive exactly once");
    }

    [Test]
    [Description("Verifies that a release tag is checked against the library version the artifact declares.")]
    public void ReleaseBundle_ShouldRefuseVerification_WhenTheTagDoesNotMatchTheLibraryVersion()
    {
        // Arrange
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string outputPath = Path.Combine(_directory, "clio-knowledge-bundle.zip");
        Build(key, outputPath);

        // Act
        Action verify = () => new BundleVerifier().Verify(
            outputPath,
            key.ExportSubjectPublicKeyInfoPem(),
            KeyId,
            "99.99.99");

        // Assert
        verify.Should().Throw<InvalidDataException>(
                because: "the release tag is what a consumer records as the revision, so it must name "
                    + "the same generation the signed manifest declares")
            .WithMessage("*99.99.99*");
    }

    [Test]
    [Description("Verifies that a bundle signed by an untrusted key is refused before it can be published.")]
    public void ReleaseBundle_ShouldRefuseVerification_WhenSignedByAnotherKey()
    {
        // Arrange
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string outputPath = Path.Combine(_directory, "clio-knowledge-bundle.zip");
        Build(signingKey, outputPath);

        // Act
        Action verify = () => new BundleVerifier().Verify(
            outputPath,
            otherKey.ExportSubjectPublicKeyInfoPem(),
            KeyId);

        // Assert
        verify.Should().Throw<InvalidDataException>(
                because: "publishing an artifact the trusted key does not vouch for would break every consumer")
            .WithMessage("*signature does not verify*");
    }

    [Test]
    [Description("Verifies that an artifact with an entry the manifest does not declare is refused before publication.")]
    public void ReleaseBundle_ShouldRefuseVerification_WhenAnUndeclaredEntryIsPresent()
    {
        // Arrange
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string outputPath = Path.Combine(_directory, "clio-knowledge-bundle.zip");
        Build(key, outputPath);
        using (ZipArchive archive = ZipFile.Open(outputPath, ZipArchiveMode.Update))
        {
            using Stream stream = archive.CreateEntry("automation/leaked.txt").Open();
            stream.Write("this should never ship"u8);
        }

        // Act
        Action verify = () => new BundleVerifier().Verify(outputPath, key.ExportSubjectPublicKeyInfoPem(), KeyId);

        // Assert
        verify.Should().Throw<InvalidDataException>(
                because: "an undeclared entry means something outside the publication contract reached the artifact")
            .WithMessage("*automation/leaked.txt*");
    }

    private BundleBuildResult Build(ECDsa key, string outputPath) => new BundleBuilder().Build(
        Path.Combine(_repositoryRoot, "bundle-source.json"),
        outputPath,
        key,
        new BundlePublicationMetadata(
            new SourceProvenance("Advance-Technologies-Foundation/clio-knowledge", new string('a', 40)),
            new SignatureDescriptor("ECDSA-P256-SHA256", KeyId)));

    private static IReadOnlyList<ArchiveEntrySnapshot> Snapshot(string archivePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        return archive.Entries
            .Select(entry =>
            {
                byte[] bytes = new byte[entry.Length];
                using Stream stream = entry.Open();
                stream.ReadExactly(bytes);
                return new ArchiveEntrySnapshot(
                    entry.FullName,
                    entry.LastWriteTime,
                    entry.ExternalAttributes,
                    Convert.ToHexStringLower(SHA256.HashData(bytes)));
            })
            .ToArray();
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

    private sealed record ArchiveEntrySnapshot(
        string Path,
        DateTimeOffset LastWriteTime,
        int ExternalAttributes,
        string Digest);
}

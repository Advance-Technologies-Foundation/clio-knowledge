using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class GuidanceMigrationTests
{
    private static readonly (string Canonical, string Oracle)[] MigratedArticles =
    [
        ("guidance/mcp/guides/esq.md", "fixtures/oracles/esq/resources/esq.md"),
        ("guidance/mcp/guides/esq-filter-parsing.md", "fixtures/oracles/esq/resources/esq-filter-parsing.md"),
        ("guidance/mcp/guides/esq-filters/index.md", "fixtures/oracles/esq/resources/esq-filters.md"),
        ("guidance/mcp/guides/esq-filters/backend.md", "fixtures/oracles/esq/resources/esq-filters-backend.md"),
        ("guidance/mcp/guides/esq-filters/frontend.md", "fixtures/oracles/esq/resources/esq-filters-frontend.md")
    ];

    [Test]
    public void CanonicalGuidance_ShouldMatchFrozenClioOracle_AfterInitialMigration()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        var differences = MigratedArticles
            .Where(pair => !CanonicalBytes(repositoryRoot, pair.Canonical)
                .SequenceEqual(CanonicalBytes(repositoryRoot, pair.Oracle)))
            .Select(pair => pair.Canonical)
            .ToArray();

        // Assert
        differences.Should().BeEmpty(
            because: "the initial content migration must preserve the exact guidance served by Clio");
    }

    [Test]
    [Description("Verifies that publication reads only canonical human-authored guidance rather than frozen oracle fixtures.")]
    public void BundleSource_ShouldPublishOnlyCanonicalGuidanceFiles()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        string[] sourcePaths = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("sourcePath").GetString()!)
            .ToArray();

        // Assert
        sourcePaths.Should().NotBeEmpty(because: "a published bundle must contain guidance resources");
        sourcePaths.Should().OnlyContain(path => path.StartsWith("guidance/", StringComparison.Ordinal),
            because: "developers must publish canonical guidance rather than immutable oracle fixtures");
    }

    [Test]
    [Description("Keeps the v1 schema aligned with the builder's complete Git object provenance requirement.")]
    public void BundleSchema_ShouldRequireCompleteCommitObjectId()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "schemas/v1/knowledge-bundle.schema.json")));
        string pattern = schema.RootElement.GetProperty("$defs")
            .GetProperty("source")
            .GetProperty("properties")
            .GetProperty("commit")
            .GetProperty("pattern")
            .GetString()!;
        Regex commitPattern = new(pattern, RegexOptions.CultureInvariant);

        // Act
        bool completeSha1Accepted = commitPattern.IsMatch("0123456789abcdef0123456789abcdef01234567");
        bool completeSha256Accepted = commitPattern.IsMatch(
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        bool abbreviatedCommitAccepted = commitPattern.IsMatch("0123456789abcdef");

        // Assert
        completeSha1Accepted.Should().BeTrue(
            because: "the v1 provenance contract supports complete SHA-1 Git object IDs");
        completeSha256Accepted.Should().BeTrue(
            because: "the v1 provenance contract supports complete SHA-256 Git object IDs");
        abbreviatedCommitAccepted.Should().BeFalse(
            because: "abbreviated Git revisions are not immutable publication evidence");
    }

    [Test]
    [Description("Builds the deterministic v1 library and verifies root, fixture, and package-source artifact parity.")]
    public void BundleSource_ShouldDeclareCanonicalMultiSourceIdentity()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string sourcePath = Path.Combine(repositoryRoot, "bundle-source.json");
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(sourcePath));
        JsonElement root = source.RootElement;
        string libraryId = root.GetProperty("libraryId").GetString()!;
        JsonElement[] resources = root.GetProperty("resources").EnumerateArray().ToArray();
        string outputPath = Path.Combine(
            Path.GetTempPath(),
            "clio-knowledge-tests",
            $"canonical-{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using ECDsa key = ECDsa.Create();
        key.ImportFromPem(File.ReadAllText(Path.Combine(repositoryRoot, "fixtures/keys/p1-test-private.pem")));

        try
        {
            // Act
            BundleBuildResult result = new BundleBuilder().Build(sourcePath, outputPath, key);

            // Assert
            root.GetProperty("contractVersion").GetString().Should().Be("1.0.0",
                because: "multi-source identity is the canonical v1 publication contract");
            libraryId.Should().Be("com.creatio.clio",
                because: "the migrated Clio guidance library needs one stable reverse-DNS publisher identity");
            root.GetProperty("sequence").GetUInt64().Should().Be(2,
                because: "v1 must advance beyond the implicit com.creatio.clio v0 sequence-one generation");
            resources.Select(resource => resource.GetProperty("itemId").GetString()).Should().OnlyHaveUniqueItems(
                because: "item identities are immutable within a library");
            resources.Select(resource => $"{resource.GetProperty("topicId").GetString()}|{resource.GetProperty("role").GetString()}")
                .Should().OnlyHaveUniqueItems(
                    because: "one library must not offer ambiguous candidates for the same topic and role");
            resources.Should().OnlyContain(resource =>
                    resource.GetProperty("uri").GetString() == BundleBuilder.CreateCanonicalUri(
                        libraryId,
                        resource.GetProperty("itemId").GetString()!),
                because: "namespaced lookup must be exact and derivable without transport state");
            resources.Should().OnlyContain(resource => resource.GetProperty("legacyUris").GetArrayLength() == 1,
                because: "every currently migrated v0 route remains available as signed transition metadata");
            result.Manifest.Resources.Select(resource => resource.ItemId).Should().Equal(
                resources.Select(resource => resource.GetProperty("itemId").GetString())
                    .OrderBy(itemId => itemId, StringComparer.Ordinal),
                because: "the builder emits the real repository inventory in deterministic item order");
            string rootArtifactPath = Path.Combine(repositoryRoot, "knowledge-bundle.zip");
            string fixturePath = Path.Combine(repositoryRoot, "fixtures/bundles/clio-knowledge-v1/valid.zip");
            File.ReadAllBytes(fixturePath).Should().Equal(File.ReadAllBytes(rootArtifactPath),
                because: "the v1 fixture must preserve the exact signed artifact consumed by Git transport");
            using ZipArchive rootArtifact = ZipFile.OpenRead(rootArtifactPath);
            byte[] artifactManifest = ReadEntry(rootArtifact, "manifest.json");
            artifactManifest.Should().Equal(result.ManifestBytes,
                because: "the ready root artifact must contain the current deterministic manifest");
            key.VerifyData(artifactManifest, ReadEntry(rootArtifact, "manifest.sig"), HashAlgorithmName.SHA256)
                .Should().BeTrue(
                    because: "the ready root artifact must retain a valid detached signature over its exact manifest bytes");
            File.ReadAllText(Path.Combine(
                    repositoryRoot,
                    "distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj"))
                .Should().Contain("..\\..\\knowledge-bundle.zip",
                    because: "NuGet distribution must package the same root artifact rather than an independently signed copy");
        }
        finally
        {
            File.Delete(outputPath);
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
}

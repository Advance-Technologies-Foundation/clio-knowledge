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
    [Description("Verifies that publication reads only canonical human-authored knowledge rather than frozen oracle fixtures.")]
    public void BundleSource_ShouldPublishOnlyCanonicalKnowledgeFiles()
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
        sourcePaths.Should().NotBeEmpty(because: "a published bundle must contain knowledge resources");
        sourcePaths.Should().OnlyContain(path =>
                path.StartsWith("guidance/", StringComparison.Ordinal)
                || path.StartsWith("catalog/", StringComparison.Ordinal),
            because: "developers must publish canonical guidance or catalog content rather than immutable oracle fixtures");
    }

    [Test]
    [Description("Keeps the checked-in Git repository manifest associated with its strict source schema and free of publication metadata.")]
    public void BundleSource_ShouldReferenceRepositorySchema_WithoutPublicationMetadata()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string sourcePath = Path.Combine(repositoryRoot, "bundle-source.json");
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(sourcePath));
        JsonElement root = source.RootElement;
        string schemaReference = root.GetProperty("$schema").GetString()!;
        string schemaPath = Path.GetFullPath(Path.Combine(repositoryRoot, schemaReference));

        // Act
        bool schemaExists = File.Exists(schemaPath);
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        JsonElement properties = schema.RootElement.GetProperty("properties");

        // Assert
        schemaExists.Should().BeTrue(because: "editors and agents need a resolvable schema beside the repository manifest");
        schema.RootElement.GetProperty("additionalProperties").GetBoolean().Should().BeFalse(
            because: "the repository contract must reject accidental transport or publication fields");
        root.TryGetProperty("source", out _).Should().BeFalse(
            because: "Git already provides repository and commit identity");
        root.TryGetProperty("signature", out _).Should().BeFalse(
            because: "signing is a NuGet publication concern rather than repository content");
        root.TryGetProperty("issuedAt", out _).Should().BeFalse(
            because: "Git already records the commit timestamp");
        properties.TryGetProperty("source", out _).Should().BeFalse(
            because: "the source schema must not redeclare Git provenance");
        properties.TryGetProperty("signature", out _).Should().BeFalse(
            because: "the source schema must stay transport-neutral");
        properties.TryGetProperty("issuedAt", out _).Should().BeFalse(
            because: "the source schema must not duplicate Git timestamps");
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
            BundleBuildResult result = new BundleBuilder().Build(
                sourcePath,
                outputPath,
                key,
                new BundlePublicationMetadata(
                    new SourceProvenance(
                        "Advance-Technologies-Foundation/clio-knowledge",
                        "0123456789abcdef0123456789abcdef01234567"),
                    new SignatureDescriptor("ECDSA-P256-SHA256", "p1-test")));

            // Assert
            root.GetProperty("contractVersion").GetString().Should().Be("1.0.0",
                because: "multi-source identity is the canonical v1 publication contract");
            libraryId.Should().Be("com.creatio.clio",
                because: "the migrated Clio guidance library needs one stable reverse-DNS publisher identity");
            root.GetProperty("sequence").GetUInt64().Should().Be(6,
                because: "per-resource feature gating follows the mandatory guidance migration generation");
            resources.Select(resource => resource.GetProperty("itemId").GetString()).Should().OnlyHaveUniqueItems(
                because: "item identities are immutable within a library");
            resources.Should().OnlyContain(resource =>
                    !string.IsNullOrWhiteSpace(resource.GetProperty("title").GetString())
                    && !string.IsNullOrWhiteSpace(resource.GetProperty("description").GetString()),
                because: "every published item must be discoverable without loading its body");
            resources.Select(resource => $"{resource.GetProperty("topicId").GetString()}|{resource.GetProperty("role").GetString()}")
                .Should().OnlyHaveUniqueItems(
                    because: "one library must not offer ambiguous candidates for the same topic and role");
            resources.Should().OnlyContain(resource =>
                    resource.GetProperty("uri").GetString() == BundleBuilder.CreateCanonicalUri(
                        libraryId,
                        resource.GetProperty("itemId").GetString()!),
                because: "namespaced lookup must be exact and derivable without transport state");
            resources.Where(resource => resource.GetProperty("role").GetString() == "guidance")
                .Should().OnlyContain(resource => resource.GetProperty("legacyUris").GetArrayLength() == 1,
                    because: "every currently migrated v0 guidance route remains available as signed transition metadata");
            resources.Count(resource => resource.GetProperty("role").GetString() == "guidance").Should().Be(63,
                because: "every guidance article merged into the repository must be published by the manifest");
            result.Manifest.Resources.Select(resource => resource.ItemId).Should().Equal(
                resources.Select(resource => resource.GetProperty("itemId").GetString())
                    .OrderBy(itemId => itemId, StringComparer.Ordinal),
                because: "the builder emits the real repository inventory in deterministic item order");
            result.Manifest.Resources.Should().OnlyContain(resource =>
                    !string.IsNullOrWhiteSpace(resource.Title)
                    && !string.IsNullOrWhiteSpace(resource.Description),
                because: "generated delivery manifests must preserve producer-owned discovery metadata");
            File.ReadAllText(Path.Combine(
                    repositoryRoot,
                    "distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj"))
                .Should().Contain("GeneratedBundlePath",
                    because: "NuGet may generate its delivery archive without committing ZIP artifacts to the Git repository");
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    [Description("Declares process-modeling as feature-gated and keeps requiredFeatures optional in both v1 contracts.")]
    public void FeatureGating_ShouldBeDeclaredByTheResourceAndBothSchemas()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "bundle-source.json")));
        using JsonDocument repositorySchema = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "schemas/v1/knowledge-repository.schema.json")));
        using JsonDocument bundleSchema = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "schemas/v1/knowledge-bundle.schema.json")));

        // Act
        JsonElement processModeling = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(resource => resource.GetProperty("itemId").GetString() == "process-modeling");
        string[] requiredFeatures = processModeling.GetProperty("requiredFeatures")
            .EnumerateArray()
            .Select(feature => feature.GetString()!)
            .ToArray();
        JsonElement repositoryResource = repositorySchema.RootElement.GetProperty("$defs")
            .GetProperty("resource");
        JsonElement bundleResource = bundleSchema.RootElement.GetProperty("$defs")
            .GetProperty("resource");

        // Assert
        requiredFeatures.Should().Equal(["process-designer"],
            because: "process-modeling must not be advertised while its experimental Clio feature is disabled");
        repositoryResource.GetProperty("properties").TryGetProperty("requiredFeatures", out _).Should().BeTrue(
            because: "Git repositories must be able to declare per-resource feature requirements");
        bundleResource.GetProperty("properties").TryGetProperty("requiredFeatures", out _).Should().BeTrue(
            because: "packaged manifests must preserve the same feature-gating contract");
        repositoryResource.GetProperty("required").EnumerateArray()
            .Select(property => property.GetString()).Should().NotContain("requiredFeatures",
                because: "resources without feature requirements remain backward compatible");
        bundleResource.GetProperty("required").EnumerateArray()
            .Select(property => property.GetString()).Should().NotContain("requiredFeatures",
                because: "the delivery contract keeps feature requirements optional");
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

using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class Mig2PageSchemaMigrationTests
{
    private static readonly MigratedArticle[] Articles =
    [
        new(
            "creatio-freedom-iframe-section",
            "docs://mcp/guides/creatio-freedom-iframe-section",
            "guidance/mcp/guides/platform/freedom-ui/iframe-section.md"),
        new(
            "desktop-page",
            "docs://mcp/guides/desktop-page",
            "guidance/mcp/guides/platform/desktop/page.md"),
        new(
            "mobile-page-modification",
            "docs://mcp/guides/mobile-page-modification",
            "guidance/mcp/guides/platform/mobile/page-modification.md"),
        new(
            "page-schema-converters",
            "docs://mcp/guides/page-schema-converters",
            "guidance/mcp/guides/page-schema/converters.md"),
        new(
            "page-schema-creatio-devkit-common",
            "docs://mcp/guides/page-schema-creatio-devkit-common",
            "guidance/mcp/guides/page-schema/creatio-devkit-common.md"),
        new(
            "page-schema-handlers",
            "docs://mcp/guides/page-schema-handlers",
            "guidance/mcp/guides/page-schema/handlers.md"),
        new(
            "page-schema-resources",
            "docs://mcp/guides/page-schema-resources",
            "guidance/mcp/guides/page-schema/resources.md"),
        new(
            "page-schema-validators",
            "docs://mcp/guides/page-schema-validators",
            "guidance/mcp/guides/page-schema/validators.md"),
        new(
            "related-list",
            "docs://mcp/guides/related-list",
            "guidance/mcp/guides/related-data/list.md"),
        new(
            "related-page-binding",
            "docs://mcp/guides/related-page-binding",
            "guidance/mcp/guides/related-data/page-binding.md")
    ];

    [Test]
    [Description("Verifies that every MIG2 canonical article differs from its frozen Clio oracle only by canonicalized reference links.")]
    public void CanonicalPageSchemaGuidance_ShouldMatchFrozenClioOracleByteForByte()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string[] differences = Articles
            .Where(article => !string.Equals(
                ReferenceLinkMigration.NormalizeToFrozenLinkText(ReadText(repositoryRoot, article.CanonicalPath)),
                ReadText(repositoryRoot, $"fixtures/oracles/clio-guidance-v0/resources/{article.Id}.md"),
                StringComparison.Ordinal))
            .Select(article => article.Id)
            .ToArray();

        // Assert
        differences.Should().BeEmpty(
            because: "the reference migration changes only links that now target independently published articles");
    }

    [Test]
    [Description("Verifies MIG2 partition ownership, canonical path scope, and stable resource URIs.")]
    public void Mig2Mappings_ShouldPreserveOwnershipPathsAndStableUris()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument partitions = ReadJson(repositoryRoot, "migration/guidance-partitions.json");
        using JsonDocument provenance = ReadJson(repositoryRoot,
            "fixtures/oracles/clio-guidance-v0/provenance.json");
        string[] partitionIds = partitions.RootElement.GetProperty("partitions")
            .EnumerateArray()
            .Single(partition => partition.GetProperty("id").GetString() == "MIG2")
            .GetProperty("guideIds")
            .EnumerateArray()
            .Select(guide => guide.GetString()!)
            .ToArray();
        Dictionary<string, string> frozenUris = provenance.RootElement.GetProperty("resources")
            .EnumerateArray()
            .ToDictionary(
                resource => resource.GetProperty("id").GetString()!,
                resource => resource.GetProperty("uri").GetString()!,
                StringComparer.Ordinal);
        string[] allowedPathPrefixes =
        [
            "guidance/mcp/guides/page-schema/",
            "guidance/mcp/guides/related-data/",
            "guidance/mcp/guides/platform/"
        ];

        // Act
        string[] uriDifferences = Articles
            .Where(article => frozenUris[article.Id] != article.Uri)
            .Select(article => article.Id)
            .ToArray();
        string[] pathsOutsideOwnedDirectories = Articles
            .Where(article => !allowedPathPrefixes.Any(prefix =>
                article.CanonicalPath.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(article => article.CanonicalPath)
            .ToArray();

        // Assert
        Articles.Select(article => article.Id).Should().Equal(partitionIds,
            because: "MIG2 must migrate every owned guide exactly once and preserve partition order");
        uriDifferences.Should().BeEmpty(
            because: "the clearer canonical directory layout must not change stable docs resource URIs");
        pathsOutsideOwnedDirectories.Should().BeEmpty(
            because: "MIG2 must remain isolated to its assigned page-schema, related-data, and platform directories");
    }

    private static byte[] ReadBytes(string repositoryRoot, string relativePath) =>
        File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string ReadText(string repositoryRoot, string relativePath) =>
        File.ReadAllText(Path.Combine(
            repositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static JsonDocument ReadJson(string repositoryRoot, string relativePath) =>
        JsonDocument.Parse(ReadBytes(repositoryRoot, relativePath));

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

    private sealed record MigratedArticle(string Id, string Uri, string CanonicalPath);
}

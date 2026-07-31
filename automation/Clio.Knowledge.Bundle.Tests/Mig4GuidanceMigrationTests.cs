using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class Mig4GuidanceMigrationTests
{
    private static readonly MigratedArticle[] MigratedArticles =
    [
        new("atf-repository-dev", "docs://mcp/guides/atf-repository-dev", "guidance/mcp/guides/composable-app/atf-repository-dev.md"),
        new("atf-repository-model-management", "docs://mcp/guides/atf-repository-model-management", "guidance/mcp/guides/composable-app/atf-repository-model-management.md"),
        new("atf-repository-tests", "docs://mcp/guides/atf-repository-tests", "guidance/mcp/guides/composable-app/atf-repository-tests.md"),
        new("composable-app-e2e-test-implementation", "docs://mcp/guides/composable-app-e2e-test-implementation", "guidance/mcp/guides/composable-app/composable-app-e2e-test-implementation.md"),
        new("composable-app-repo-bootstrap", "docs://mcp/guides/composable-app-repo-bootstrap", "guidance/mcp/guides/composable-app/composable-app-repo-bootstrap.md"),
        new("configuration-entity-event-listener", "docs://mcp/guides/configuration-entity-event-listener", "guidance/mcp/guides/composable-app/configuration-entity-event-listener.md"),
        new("configuration-entity-event-listener-tests", "docs://mcp/guides/configuration-entity-event-listener-tests", "guidance/mcp/guides/composable-app/configuration-entity-event-listener-tests.md"),
        new("configuration-webservice", "docs://mcp/guides/configuration-webservice", "guidance/mcp/guides/backend/configuration-webservice.md"),
        new("configuration-webservice-tests", "docs://mcp/guides/configuration-webservice-tests", "guidance/mcp/guides/backend/configuration-webservice-tests.md"),
        new("creatio-composable-app-development", "docs://mcp/guides/creatio-composable-app-development", "guidance/mcp/guides/composable-app/creatio-composable-app-development.md"),
        new("dataforge-orchestration", "docs://mcp/guides/dataforge-orchestration", "guidance/mcp/guides/integration/dataforge-orchestration.md"),
        new("feature-toggle", "docs://mcp/guides/feature-toggle", "guidance/mcp/guides/composable-app/feature-toggle.md"),
        new("feature-toggle-tests", "docs://mcp/guides/feature-toggle-tests", "guidance/mcp/guides/composable-app/feature-toggle-tests.md"),
        new("integration-testing", "docs://mcp/guides/integration-testing", "guidance/mcp/guides/integration/integration-testing.md"),
        new("package-dependencies", "docs://mcp/guides/package-dependencies", "guidance/mcp/guides/backend/package-dependencies.md"),
        new("server-to-server-oauth", "docs://mcp/guides/server-to-server-oauth", "guidance/mcp/guides/integration/server-to-server-oauth.md"),
        new("sys-setting", "docs://mcp/guides/sys-setting", "guidance/mcp/guides/composable-app/sys-setting.md"),
        new("sys-setting-tests", "docs://mcp/guides/sys-setting-tests", "guidance/mcp/guides/composable-app/sys-setting-tests.md"),
        new("sys-settings", "docs://mcp/guides/sys-settings", "guidance/mcp/guides/backend/sys-settings.md"),
        new("virtual-entities", "docs://mcp/guides/virtual-entities", "guidance/mcp/guides/backend/virtual-entities.md")
    ];

    [Test]
    [Description("Verifies that the MIG4 mapping covers its complete partition and preserves every stable ID and URI.")]
    public void MigrationMap_ShouldCoverMig4PartitionAndPreserveStableUris()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument partitions = ReadJson(repositoryRoot, "migration/guidance-partitions.json");
        using JsonDocument provenance = ReadJson(repositoryRoot, "fixtures/oracles/clio-guidance-v0/provenance.json");
        string[] partitionIds = partitions.RootElement.GetProperty("partitions")
            .EnumerateArray()
            .Single(partition => partition.GetProperty("id").GetString() == "MIG4")
            .GetProperty("guideIds")
            .EnumerateArray()
            .Select(guide => guide.GetString()!)
            .ToArray();
        Dictionary<string, string> oracleUris = provenance.RootElement.GetProperty("resources")
            .EnumerateArray()
            .ToDictionary(
                resource => resource.GetProperty("id").GetString()!,
                resource => resource.GetProperty("uri").GetString()!,
                StringComparer.Ordinal);

        // Act
        string[] mappedIds = MigratedArticles.Select(article => article.Id).ToArray();
        string[] uriMismatches = MigratedArticles
            .Where(article => !oracleUris.TryGetValue(article.Id, out string? uri)
                || !string.Equals(uri, article.Uri, StringComparison.Ordinal))
            .Select(article => article.Id)
            .ToArray();

        // Assert
        mappedIds.Should().Equal(partitionIds,
            because: "the focused migration must cover every MIG4 article exactly once and no other partition");
        MigratedArticles.Select(article => article.CanonicalPath).Should().OnlyHaveUniqueItems(
            because: "every stable guidance ID must own a distinct canonical source file");
        uriMismatches.Should().BeEmpty(
            because: "moving guidance into canonical directories must not change its published docs URI");
    }

    [Test]
    [Description("Verifies that every MIG4 article differs from its frozen Clio oracle only by canonicalized reference links.")]
    public void CanonicalGuidance_ShouldMatchFrozenClioOracle_ByteForByte()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string[] differences = MigratedArticles
            .Where(article => !string.Equals(
                ReferenceLinkMigration.NormalizeToFrozenLinkText(File.ReadAllText(ToFullPath(
                    repositoryRoot,
                    article.CanonicalPath))),
                File.ReadAllText(ToFullPath(
                    repositoryRoot,
                    $"fixtures/oracles/clio-guidance-v0/resources/{article.Id}.md")),
                StringComparison.Ordinal))
            .Select(article => article.Id)
            .ToArray();

        // Assert
        differences.Should().BeEmpty(
            because: "the reference migration changes only links that now target independently published articles");
    }

    [Test]
    [Description("Verifies that virtual entity write guidance retains its Creatio 10.0 compatibility boundary.")]
    public void VirtualEntitiesGuidance_ShouldRequireCreatio100ForWrites()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string path = ToFullPath(
            repositoryRoot,
            "guidance/mcp/guides/backend/virtual-entities.md");

        // Act
        string guidance = File.ReadAllText(path);

        // Assert
        guidance.Should().Contain("Write through EntityEventListener (Creatio 10.0+ only)",
            because: "virtual entity writes are unsupported before Creatio 10.0 and the boundary must remain explicit");
    }

    private static JsonDocument ReadJson(string repositoryRoot, string relativePath) =>
        JsonDocument.Parse(File.ReadAllBytes(ToFullPath(repositoryRoot, relativePath)));

    private static string ToFullPath(string repositoryRoot, string relativePath) =>
        Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

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

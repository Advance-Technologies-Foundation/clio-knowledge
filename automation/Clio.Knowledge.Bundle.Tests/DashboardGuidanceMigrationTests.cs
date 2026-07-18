using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class DashboardGuidanceMigrationTests
{
    private static readonly GuidanceMapping[] MigratedArticles =
    [
        new("chart-widget", "docs://mcp/guides/chart-widget",
            "guidance/mcp/guides/dashboards/chart-widget.md",
            "fixtures/oracles/clio-guidance-v0/resources/chart-widget.md"),
        new("dashboard-creation", "docs://mcp/guides/dashboard-creation",
            "guidance/mcp/guides/dashboards/creation.md",
            "fixtures/oracles/clio-guidance-v0/resources/dashboard-creation.md"),
        new("dashboard-design", "docs://mcp/guides/dashboard-design",
            "guidance/mcp/guides/dashboards/design.md",
            "fixtures/oracles/clio-guidance-v0/resources/dashboard-design.md"),
        new("dashboards", "docs://mcp/guides/dashboards",
            "guidance/mcp/guides/dashboards/index.md",
            "fixtures/oracles/clio-guidance-v0/resources/dashboards.md"),
        new("indicator-widget", "docs://mcp/guides/indicator-widget",
            "guidance/mcp/guides/dashboards/indicator-widget.md",
            "fixtures/oracles/clio-guidance-v0/resources/indicator-widget.md"),
        new("theming", "docs://mcp/guides/theming",
            "guidance/mcp/guides/theming/index.md",
            "fixtures/oracles/clio-guidance-v0/resources/theming.md")
    ];

    [Test]
    [Description("Verifies that every MIG3 canonical article is byte-identical to its frozen Clio oracle.")]
    public void CanonicalArticles_ShouldMatchFrozenOracleBytes_AfterInitialMigration()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string[] differences = MigratedArticles
            .Where(mapping => !ReadBytes(repositoryRoot, mapping.CanonicalPath)
                .SequenceEqual(ReadBytes(repositoryRoot, mapping.OraclePath)))
            .Select(mapping => mapping.Id)
            .ToArray();

        // Assert
        differences.Should().BeEmpty(
            because: "the initial MIG3 migration must preserve the exact UTF-8/LF bytes frozen from Clio");
    }

    [Test]
    [Description("Verifies that MIG3 canonical paths retain the stable guidance IDs and docs URIs exposed by Clio.")]
    public void CanonicalArticleMappings_ShouldPreserveStableIdsAndUris()
    {
        // Arrange
        string[] expectedIds =
        [
            "chart-widget",
            "dashboard-creation",
            "dashboard-design",
            "dashboards",
            "indicator-widget",
            "theming"
        ];

        // Act
        string[] actualIds = MigratedArticles.Select(mapping => mapping.Id).ToArray();
        string[] mismatchedUris = MigratedArticles
            .Where(mapping => mapping.Uri != $"docs://mcp/guides/{mapping.Id}")
            .Select(mapping => mapping.Id)
            .ToArray();

        // Assert
        actualIds.Should().Equal(expectedIds,
            because: "the MIG3 partition owns these six stable guidance IDs in canonical order");
        actualIds.Should().OnlyHaveUniqueItems(
            because: "each canonical MIG3 article must retain one unambiguous public identity");
        mismatchedUris.Should().BeEmpty(
            because: "moving source files into family directories must not change their public docs URIs");
    }

    private static byte[] ReadBytes(string repositoryRoot, string relativePath) =>
        File.ReadAllBytes(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

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

    private sealed record GuidanceMapping(
        string Id,
        string Uri,
        string CanonicalPath,
        string OraclePath);
}

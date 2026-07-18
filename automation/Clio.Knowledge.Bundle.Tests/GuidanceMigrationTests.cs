using System.Text;
using System.Text.Json;
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

    private static byte[] CanonicalBytes(string repositoryRoot, string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(repositoryRoot, relativePath), new UTF8Encoding(false, true));
        return Encoding.UTF8.GetBytes(BundleBuilder.CanonicalizeText(text.TrimStart('\uFEFF')));
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

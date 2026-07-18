using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class Mig1PagesMigrationTests
{
    private static readonly MigratedArticle[] Articles =
    [
        new("data-bindings", "docs://mcp/guides/data-bindings", "guidance/mcp/guides/pages/data-bindings.md"),
        new("page-creation", "docs://mcp/guides/page-creation", "guidance/mcp/guides/pages/creation.md"),
        new("page-modification", "docs://mcp/guides/page-modification", "guidance/mcp/guides/pages/modification/index.md"),
        new("page-modification-components", "docs://mcp/guides/page-modification-components", "guidance/mcp/guides/pages/modification/components.md"),
        new("page-modification-containers", "docs://mcp/guides/page-modification-containers", "guidance/mcp/guides/pages/modification/containers.md"),
        new("page-modification-field-contract", "docs://mcp/guides/page-modification-field-contract", "guidance/mcp/guides/pages/modification/field-contract.md"),
        new("page-modification-overview", "docs://mcp/guides/page-modification-overview", "guidance/mcp/guides/pages/modification/overview.md")
    ];

    [Test]
    [Description("Verifies that every MIG1 canonical page article exactly matches its frozen Clio oracle.")]
    public void CanonicalPageGuidance_ShouldMatchFrozenClioOracle()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string[] differences = Articles
            .Where(article => !CanonicalBytes(repositoryRoot, article.CanonicalPath)
                .SequenceEqual(CanonicalBytes(repositoryRoot,
                    $"fixtures/oracles/clio-guidance-v0/resources/{article.Id}.md")))
            .Select(article => article.Id)
            .ToArray();

        // Assert
        differences.Should().BeEmpty(
            because: "the initial MIG1 migration must preserve the exact guidance bytes served by Clio");
    }

    [Test]
    [Description("Verifies that MIG1 ownership and stable resource URIs match the frozen inventory.")]
    public void Mig1Ownership_ShouldMatchPartitionAndStableUris()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument partitions = ReadJson(repositoryRoot, "migration/guidance-partitions.json");
        using JsonDocument provenance = ReadJson(repositoryRoot,
            "fixtures/oracles/clio-guidance-v0/provenance.json");
        string[] partitionIds = partitions.RootElement.GetProperty("partitions")
            .EnumerateArray()
            .Single(partition => partition.GetProperty("id").GetString() == "MIG1")
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

        // Act
        string[] uriDifferences = Articles
            .Where(article => frozenUris[article.Id] != article.Uri)
            .Select(article => article.Id)
            .ToArray();

        // Assert
        Articles.Select(article => article.Id).Should().BeEquivalentTo(partitionIds,
            because: "MIG1 must migrate every owned guide exactly once and no guide owned by another agent");
        uriDifferences.Should().BeEmpty(
            because: "canonical file layout changes must not change stable docs resource URIs");
    }

    private static byte[] CanonicalBytes(string repositoryRoot, string relativePath)
    {
        string text = File.ReadAllText(
            Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)),
            new UTF8Encoding(false, true));
        return Encoding.UTF8.GetBytes(BundleBuilder.CanonicalizeText(text.TrimStart('\uFEFF')));
    }

    private static JsonDocument ReadJson(string repositoryRoot, string relativePath) =>
        JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar))));

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

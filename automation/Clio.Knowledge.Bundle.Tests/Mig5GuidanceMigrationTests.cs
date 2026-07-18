using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class Mig5GuidanceMigrationTests
{
    private static readonly (string Id, string Uri, string CanonicalPath)[] Articles =
    [
        ("agent-execution", "docs://mcp/guides/agent-execution", "guidance/mcp/guides/operations/agent-execution.md"),
        ("app-modeling", "docs://mcp/guides/app-modeling", "guidance/mcp/guides/applications/app-modeling.md"),
        ("business-rule-filters", "docs://mcp/guides/business-rule-filters", "guidance/mcp/guides/business-rules/business-rule-filters.md"),
        ("business-rules", "docs://mcp/guides/business-rules", "guidance/mcp/guides/business-rules/business-rules.md"),
        ("deploy-lifecycle", "docs://mcp/guides/deploy-lifecycle", "guidance/mcp/guides/operations/deploy-lifecycle.md"),
        ("describe-environment", "docs://mcp/guides/describe-environment", "guidance/mcp/guides/operations/describe-environment.md"),
        ("existing-app-maintenance", "docs://mcp/guides/existing-app-maintenance", "guidance/mcp/guides/applications/existing-app-maintenance.md"),
        ("identity-assertion", "docs://mcp/guides/identity-assertion", "guidance/mcp/guides/operations/identity-assertion.md"),
        ("process-modeling", "docs://mcp/guides/process-modeling", "guidance/mcp/guides/processes/process-modeling.md"),
        ("run-process-button", "docs://mcp/guides/run-process-button", "guidance/mcp/guides/processes/run-process-button.md"),
        ("support-mode", "docs://mcp/guides/support-mode", "guidance/mcp/guides/operations/support-mode.md"),
        ("ui-project", "docs://mcp/guides/ui-project", "guidance/mcp/guides/applications/ui-project.md")
    ];

    [Test]
    [Description("Verifies that every canonical MIG5 article is byte-identical to its frozen Clio oracle.")]
    public void CanonicalArticles_ShouldMatchFrozenOracleByteForByte()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string[] differences = Articles
            .Where(article => !ReadBytes(repositoryRoot, article.CanonicalPath)
                .SequenceEqual(ReadBytes(repositoryRoot,
                    $"fixtures/oracles/clio-guidance-v0/resources/{article.Id}.md")))
            .Select(article => article.Id)
            .ToArray();

        // Assert
        Articles.Should().HaveCount(12,
            because: "MIG5 owns exactly the twelve application, business-rule, process, and operational articles");
        differences.Should().BeEmpty(
            because: "the initial migration must preserve the frozen Clio guidance bytes without normalization");
    }

    [Test]
    [Description("Verifies that MIG5 exclusively owns the mapped articles and excludes deferred safety guidance.")]
    public void MigrationPartition_ShouldOwnExactlyTheCanonicalMig5Articles()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument partitionsDocument = ReadJson(repositoryRoot, "migration/guidance-partitions.json");
        JsonElement[] partitions = partitionsDocument.RootElement.GetProperty("partitions")
            .EnumerateArray()
            .ToArray();
        JsonElement mig5 = partitions.Single(partition =>
            partition.GetProperty("id").GetString() == "MIG5");
        string[] mappedIds = Articles
            .Select(article => article.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        // Act
        string[] partitionIds = mig5.GetProperty("guideIds")
            .EnumerateArray()
            .Select(guide => guide.GetString()!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] unexpectedOwners = partitions
            .Where(partition => partition.GetProperty("id").GetString() != "MIG5")
            .SelectMany(partition => partition.GetProperty("guideIds").EnumerateArray()
                .Select(guide => new
                {
                    Partition = partition.GetProperty("id").GetString()!,
                    Guide = guide.GetString()!
                }))
            .Where(owner => mappedIds.Contains(owner.Guide, StringComparer.Ordinal))
            .Select(owner => $"{owner.Partition}:{owner.Guide}")
            .ToArray();

        // Assert
        mappedIds.Should().OnlyHaveUniqueItems(
            because: "each canonical MIG5 article must have one stable guidance identity");
        partitionIds.Should().Equal(mappedIds,
            because: "the explicit canonical mapping must cover every article assigned to MIG5 and no other partition");
        unexpectedOwners.Should().BeEmpty(
            because: "parallel migration partitions must not overlap MIG5 ownership");
        partitionIds.Intersect(["core-rules", "routing"], StringComparer.Ordinal).Should().BeEmpty(
            because: "hard invariants and routing remain deferred to the separate safety migration");
        Articles.Select(article => article.CanonicalPath).Should().OnlyContain(path =>
                path.StartsWith("guidance/mcp/guides/applications/", StringComparison.Ordinal)
                || path.StartsWith("guidance/mcp/guides/business-rules/", StringComparison.Ordinal)
                || path.StartsWith("guidance/mcp/guides/operations/", StringComparison.Ordinal)
                || path.StartsWith("guidance/mcp/guides/processes/", StringComparison.Ordinal),
            because: "MIG5 canonical files must remain inside its four owned guidance directories");
        mig5.GetProperty("issue").GetInt32().Should().Be(19,
            because: "partition ownership must remain traceable to the MIG5 migration issue");
    }

    [Test]
    [Description("Verifies that every explicit MIG5 stable ID retains its frozen docs URI.")]
    public void StableIds_ShouldPreserveEveryFrozenDocsUri()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument provenance = ReadJson(repositoryRoot,
            "fixtures/oracles/clio-guidance-v0/provenance.json");
        string[] mappedIds = Articles.Select(article => article.Id).ToArray();
        string[] expectedMappings = Articles
            .Select(article => $"{article.Id}|{article.Uri}")
            .OrderBy(mapping => mapping, StringComparer.Ordinal)
            .ToArray();

        // Act
        string[] oracleMappings = provenance.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => mappedIds.Contains(resource.GetProperty("id").GetString()!,
                StringComparer.Ordinal))
            .Select(resource =>
                $"{resource.GetProperty("id").GetString()}|{resource.GetProperty("uri").GetString()}")
            .OrderBy(mapping => mapping, StringComparer.Ordinal)
            .ToArray();

        // Assert
        Articles.Select(article => article.Uri).Should().OnlyHaveUniqueItems(
            because: "each MIG5 stable guidance ID must retain one distinct resource URI");
        Articles.Should().OnlyContain(article =>
                article.Uri == $"docs://mcp/guides/{article.Id}",
            because: "the explicit stable ID mappings must preserve the established MCP URI contract");
        oracleMappings.Should().Equal(expectedMappings,
            because: "all twelve canonical MIG5 articles must preserve their frozen stable IDs and docs URIs");
    }

    private static byte[] ReadBytes(string repositoryRoot, string relativePath) =>
        File.ReadAllBytes(Path.Combine(repositoryRoot,
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
}

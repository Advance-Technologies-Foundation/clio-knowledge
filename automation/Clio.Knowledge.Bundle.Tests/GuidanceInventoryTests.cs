using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class GuidanceInventoryTests
{
    private const string BaselineCommit = "baa34546589413aa898429051d1702442bbd2dd2";

    [Test]
    [Description("Verifies that the full Clio guidance oracle is complete, unique, and byte-integral.")]
    public void CompleteOracle_ShouldPreserveEveryCatalogEntryAndItsCanonicalBytes()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument provenance = ReadJson(repositoryRoot,
            "fixtures/oracles/clio-guidance-v0/provenance.json");
        JsonElement root = provenance.RootElement;
        JsonElement[] resources = root.GetProperty("resources").EnumerateArray().ToArray();
        string[] ids = resources.Select(resource => resource.GetProperty("id").GetString()!).ToArray();
        string[] uris = resources.Select(resource => resource.GetProperty("uri").GetString()!).ToArray();
        string[] routingReferences = root.GetProperty("routingReferences")
            .EnumerateArray()
            .Select(reference => reference.GetString()!)
            .ToArray();

        // Act
        string[] invalidResources = resources
            .Where(resource => !ResourceMatchesProvenance(repositoryRoot, resource))
            .Select(resource => resource.GetProperty("id").GetString()!)
            .ToArray();
        string[] frozenResourcePaths = Directory.GetFiles(Path.Combine(
                repositoryRoot,
                "fixtures",
                "oracles",
                "clio-guidance-v0",
                "resources"), "*.md", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] routedIds = resources
            .Where(resource => resource.GetProperty("routed").GetBoolean())
            .Select(resource => resource.GetProperty("id").GetString()!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        // Assert
        root.GetProperty("clioRepository").GetString().Should().Be("Advance-Technologies-Foundation/clio",
            because: "the oracle must identify the authoritative source repository as well as its commit");
        root.GetProperty("clioCommit").GetString().Should().Be(BaselineCommit,
            because: "the oracle must remain tied to the immutable pre-extraction Clio baseline");
        resources.Should().HaveCount(62,
            because: "compiled GuidanceCatalog at the baseline commit exposes exactly 62 entries");
        ids.Should().OnlyHaveUniqueItems(because: "stable guidance IDs must be globally unique");
        uris.Should().OnlyHaveUniqueItems(because: "each stable guide must own exactly one resource URI");
        uris.Should().OnlyContain(uri => uri.StartsWith("docs://mcp/guides/", StringComparison.Ordinal),
            because: "the get-guidance catalog must only expose canonical guide resources");
        invalidResources.Should().BeEmpty(
            because: "every frozen resource must match its recorded byte length and SHA-256 digest");
        frozenResourcePaths.Should().Equal(ids.Select(id => $"{id}.md").OrderBy(path => path, StringComparer.Ordinal),
            because: "the oracle directory must not retain unregistered or stale guidance files");
        routingReferences.Should().OnlyContain(reference => ids.Contains(reference, StringComparer.Ordinal),
            because: "the routing article must not reference an unknown guidance ID");
        routedIds.Should().Equal(routingReferences.OrderBy(id => id, StringComparer.Ordinal),
            because: "per-resource routing metadata must match the extracted routing references");
        resources.Should().OnlyContain(resource =>
                resource.GetProperty("sourcePath").GetString()!.EndsWith(".cs", StringComparison.Ordinal),
            because: "every frozen guide must retain its originating Clio source file");
    }

    [Test]
    [Description("Verifies that migration partitions account for every frozen guide exactly once.")]
    public void MigrationPartitions_ShouldAssignEveryFrozenGuideExactlyOnce()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument provenance = ReadJson(repositoryRoot,
            "fixtures/oracles/clio-guidance-v0/provenance.json");
        using JsonDocument partitionsDocument = ReadJson(repositoryRoot,
            "migration/guidance-partitions.json");
        string[] oracleIds = provenance.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("id").GetString()!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        JsonElement[] partitions = partitionsDocument.RootElement.GetProperty("partitions")
            .EnumerateArray()
            .ToArray();

        // Act
        string[] assignedIds = partitions
            .SelectMany(partition => partition.GetProperty("guideIds").EnumerateArray())
            .Select(guide => guide.GetString()!)
            .ToArray();
        string[] parallelMigrationIds = partitions
            .Where(partition => Enumerable.Range(1, 5)
                .Select(number => $"MIG{number}")
                .Contains(partition.GetProperty("id").GetString(), StringComparer.Ordinal))
            .Select(partition => partition.GetProperty("id").GetString()!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        // Assert
        partitionsDocument.RootElement.GetProperty("baselineCommit").GetString().Should().Be(BaselineCommit,
            because: "partition ownership must describe the same immutable oracle baseline");
        assignedIds.Should().OnlyHaveUniqueItems(
            because: "parallel migration agents must never own the same guidance article");
        assignedIds.OrderBy(id => id, StringComparer.Ordinal).Should().Equal(oracleIds,
            because: "every frozen guide must have exactly one migration owner or explicit deferred owner");
        parallelMigrationIds.Should().Equal(["MIG1", "MIG2", "MIG3", "MIG4", "MIG5"],
            because: "the five independent post-MIG0 work slices must remain explicit");
        ReadPartitionIds(partitions, "MIG-SAFETY").Should().BeEquivalentTo(["core-rules", "routing"],
            because: "hard invariants and routing require their separately approved safety review");
        ReadPartitionIds(partitions, "MIG-ESQ").Should().HaveCount(5,
            because: "the already migrated ESQ slice contains the five frozen ESQ family articles");
    }

    [Test]
    [Description("Verifies that the earlier ESQ oracle is an exact subset of the complete MIG0 oracle.")]
    public void CompleteOracle_ShouldRetainThePreviouslyFrozenEsqBytes()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument esqProvenance = ReadJson(repositoryRoot, "fixtures/oracles/esq/provenance.json");
        string[] esqIds = esqProvenance.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("id").GetString()!)
            .ToArray();

        // Act
        string[] differences = esqIds
            .Where(id => !File.ReadAllBytes(Path.Combine(
                    repositoryRoot,
                    "fixtures",
                    "oracles",
                    "esq",
                    "resources",
                    $"{id}.md"))
                .SequenceEqual(File.ReadAllBytes(Path.Combine(
                    repositoryRoot,
                    "fixtures",
                    "oracles",
                    "clio-guidance-v0",
                    "resources",
                    $"{id}.md"))))
            .ToArray();

        // Assert
        differences.Should().BeEmpty(
            because: "expanding the capture from ESQ to the full catalog must not change frozen bytes");
    }

    private static bool ResourceMatchesProvenance(string repositoryRoot, JsonElement resource)
    {
        string path = resource.GetProperty("path").GetString()!;
        string fullPath = Path.Combine(
            repositoryRoot,
            "fixtures",
            "oracles",
            "clio-guidance-v0",
            path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return false;
        }
        byte[] bytes = File.ReadAllBytes(fullPath);
        return bytes.LongLength == resource.GetProperty("length").GetInt64()
            && Convert.ToHexStringLower(SHA256.HashData(bytes)) == resource.GetProperty("sha256").GetString();
    }

    private static string[] ReadPartitionIds(IEnumerable<JsonElement> partitions, string partitionId) =>
        partitions.Single(partition => partition.GetProperty("id").GetString() == partitionId)
            .GetProperty("guideIds")
            .EnumerateArray()
            .Select(guide => guide.GetString()!)
            .ToArray();

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
}

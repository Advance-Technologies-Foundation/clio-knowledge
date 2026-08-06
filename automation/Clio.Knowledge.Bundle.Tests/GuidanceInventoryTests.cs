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
    [Description("Verifies that every guidance article in the working tree is published by the manifest exactly once, so adding a file without declaring it cannot pass unnoticed.")]
    public void PublishedGuidance_ShouldCorrespondExactlyToTheGuidanceSourceTree()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = ReadJson(repositoryRoot, "bundle-source.json");

        // Act
        string[] declared = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => string.Equals(
                resource.GetProperty("role").GetString(),
                "guidance",
                StringComparison.Ordinal))
            .Select(resource => resource.GetProperty("sourcePath").GetString()!)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] files = Directory.GetFiles(
                Path.Combine(repositoryRoot, "guidance"),
                "*.md",
                SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !string.Equals(path, "guidance/README.md", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // Assert
        files.Should().Equal(declared,
            because: "an authored article the manifest never declares is invisible to every consumer, and a "
                + "declared path with no file breaks the build of the bundle — this correspondence is what a "
                + "hand-maintained article count used to approximate");
        files.Should().OnlyContain(path => new FileInfo(Path.Combine(repositoryRoot, path)).Length > 0,
            because: "an empty published article resolves to a blank answer rather than a missing one");
    }

    [Test]
    [Description("Verifies that mandatory MCP bootstrap and request-selection guidance remains published with stable identities.")]
    public void RequiredGuidance_ShouldRemainInThePublishedInventory()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = ReadJson(repositoryRoot, "bundle-source.json");
        JsonElement root = source.RootElement;
        Dictionary<string, JsonElement> resources = root.GetProperty("resources")
            .EnumerateArray()
            .ToDictionary(resource => resource.GetProperty("itemId").GetString()!, StringComparer.Ordinal);
        HashSet<string> requiredItemIds = root.GetProperty("requirements")
            .GetProperty("itemIds")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        (string ItemId, string SourcePath)[] mandatoryGuidance =
        [
            ("core-rules", "guidance/mcp/guides/core-rules.md"),
            ("routing", "guidance/mcp/guides/routing.md"),
            ("when-to-use-requests", "guidance/mcp/guides/pages/when-to-use-requests.md")
        ];

        // Act
        string[] missingResources = mandatoryGuidance
            .Where(item => !resources.ContainsKey(item.ItemId))
            .Select(item => item.ItemId)
            .ToArray();
        string[] missingRequirements = mandatoryGuidance
            .Where(item => !requiredItemIds.Contains(item.ItemId))
            .Select(item => item.ItemId)
            .ToArray();
        string[] invalidContracts = mandatoryGuidance
            .Where(item => resources.TryGetValue(item.ItemId, out JsonElement resource)
                && (!string.Equals(resource.GetProperty("topicId").GetString(), $"creatio.{item.ItemId}", StringComparison.Ordinal)
                    || !string.Equals(resource.GetProperty("role").GetString(), "guidance", StringComparison.Ordinal)
                    || !string.Equals(resource.GetProperty("uri").GetString(),
                        $"docs://knowledge/com.creatio.clio/{item.ItemId}", StringComparison.Ordinal)
                    || resource.GetProperty("legacyUris").GetArrayLength() != 1
                    || !string.Equals(resource.GetProperty("legacyUris")[0].GetString(),
                        $"docs://mcp/guides/{item.ItemId}", StringComparison.Ordinal)
                    || !string.Equals(resource.GetProperty("sourcePath").GetString(), item.SourcePath,
                        StringComparison.Ordinal)
                    || !File.Exists(Path.Combine(repositoryRoot,
                        item.SourcePath.Replace('/', Path.DirectorySeparatorChar)))))
            .Select(item => item.ItemId)
            .ToArray();

        // Assert
        missingResources.Should().BeEmpty(
            because: "Clio's mandatory startup and request-selection instructions must always resolve to published articles");
        missingRequirements.Should().BeEmpty(
            because: "activation must reject a bundle that silently omits mandatory guidance");
        invalidContracts.Should().BeEmpty(
            because: "mandatory guide IDs, routes, legacy aliases, and canonical source paths are stable contracts");
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
}

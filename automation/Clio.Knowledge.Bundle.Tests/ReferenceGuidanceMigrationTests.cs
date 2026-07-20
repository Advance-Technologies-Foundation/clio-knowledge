using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ReferenceGuidanceMigrationTests
{
    private const string CanonicalPrefix = "docs://knowledge/com.creatio.clio/reference.";
    private const string LegacyPrefix = "docs://mcp/references/";

    private static readonly string[] ExpectedLegacyUris =
    [
        "docs://mcp/references/atf-repository-dev/models-and-relations",
        "docs://mcp/references/atf-repository-dev/package-and-version",
        "docs://mcp/references/atf-repository-dev/provider-and-context",
        "docs://mcp/references/atf-repository-dev/query-patterns",
        "docs://mcp/references/atf-repository-dev/write-operations",
        "docs://mcp/references/atf-repository-model-management/collision-and-cleanup",
        "docs://mcp/references/atf-repository-model-management/generation-workflow",
        "docs://mcp/references/atf-repository-model-management/model-graph-selection",
        "docs://mcp/references/atf-repository-tests/assertion-patterns",
        "docs://mcp/references/atf-repository-tests/build-and-verify",
        "docs://mcp/references/atf-repository-tests/data-seeding-patterns",
        "docs://mcp/references/atf-repository-tests/memory-provider-setup",
        "docs://mcp/references/atf-repository-tests/minimal-test-setup",
        "docs://mcp/references/composable-app-e2e-test-implementation/environment-readiness",
        "docs://mcp/references/composable-app-e2e-test-implementation/flow",
        "docs://mcp/references/composable-app-e2e-test-implementation/sources",
        "docs://mcp/references/configuration-entity-event-listener/listener-patterns",
        "docs://mcp/references/configuration-entity-event-listener/review-checklist",
        "docs://mcp/references/configuration-entity-event-listener/validation-patterns",
        "docs://mcp/references/configuration-entity-event-listener-tests/review-checklist",
        "docs://mcp/references/configuration-entity-event-listener-tests/test-patterns",
        "docs://mcp/references/configuration-entity-event-listener-tests/validation-test-patterns",
        "docs://mcp/references/configuration-webservice/composition-root-pattern",
        "docs://mcp/references/configuration-webservice/dto-patterns",
        "docs://mcp/references/configuration-webservice/manual-runtime-checklist",
        "docs://mcp/references/configuration-webservice/status-code-patterns",
        "docs://mcp/references/configuration-webservice-tests/assertion-style",
        "docs://mcp/references/configuration-webservice-tests/endpoint-test-patterns",
        "docs://mcp/references/configuration-webservice-tests/test-fixture-pattern",
        "docs://mcp/references/creatio-composable-app-development/official-docs",
        "docs://mcp/references/creatio-freedom-iframe-section/creatio-iframe-section-template",
        "docs://mcp/references/feature-toggle/constants-pattern",
        "docs://mcp/references/feature-toggle/implementation-patterns",
        "docs://mcp/references/feature-toggle/runtime-behavior",
        "docs://mcp/references/feature-toggle-tests/constants-and-fixture-pattern",
        "docs://mcp/references/feature-toggle-tests/feature-stub-pattern",
        "docs://mcp/references/feature-toggle-tests/test-coverage-checklist",
        "docs://mcp/references/sys-setting/access-patterns",
        "docs://mcp/references/sys-setting/constants-pattern",
        "docs://mcp/references/sys-setting/review-checklist",
        "docs://mcp/references/sys-setting-tests/coverage-checklist",
        "docs://mcp/references/sys-setting-tests/mock-settings-attribute",
        "docs://mcp/references/sys-setting-tests/setup-sys-settings-pattern"
    ];

    [Test]
    [Description("Publishes every supporting reference formerly embedded in Clio with stable canonical and legacy identities.")]
    public void BundleSource_ShouldPublishCompleteLegacyReferenceInventory()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = ReadManifest(repositoryRoot);

        // Act
        JsonElement[] references = ReferenceResources(source).ToArray();
        string[] legacyUris = references
            .SelectMany(resource => resource.GetProperty("legacyUris").EnumerateArray())
            .Select(uri => uri.GetString()!)
            .OrderBy(uri => uri, StringComparer.Ordinal)
            .ToArray();

        // Assert
        references.Should().HaveCount(43,
            because: "the migration must preserve all 36 composable-app and seven web-service supporting references");
        legacyUris.Should().Equal(ExpectedLegacyUris.OrderBy(uri => uri, StringComparer.Ordinal),
            because: "every former docs://mcp/references route needs an exact compatibility identity");
        references.Should().OnlyContain(resource =>
                resource.GetProperty("role").GetString() == "reference"
                && resource.GetProperty("mediaType").GetString() == "text/markdown"
                && resource.GetProperty("topicId").GetString()
                    == $"creatio.{resource.GetProperty("itemId").GetString()}"
                && resource.GetProperty("uri").GetString()
                    == $"docs://knowledge/com.creatio.clio/{resource.GetProperty("itemId").GetString()}",
            because: "supporting references remain readable without appearing as bare primary guidance names");
    }

    [Test]
    [Description("Keeps the reference source directory and manifest inventory in exact correspondence.")]
    public void ReferenceFiles_ShouldMatchPublishedManifestInventory()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = ReadManifest(repositoryRoot);

        // Act
        string[] declared = ReferenceResources(source)
            .Select(resource => resource.GetProperty("sourcePath").GetString()!)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] files = Directory.GetFiles(
                Path.Combine(repositoryRoot, "references"),
                "*.md",
                SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(path => !string.Equals(path, "references/README.md", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // Assert
        files.Should().Equal(declared,
            because: "an undeclared or missing supporting article would make the published inventory incomplete");
        files.Should().OnlyContain(path => new FileInfo(Path.Combine(repositoryRoot, path)).Length > 0,
            because: "every published supporting article must retain its migrated body");
    }

    [Test]
    [Description("Requires primary guidance to link supporting articles through canonical routes that the generic Clio resource handler can read.")]
    public void PrimaryGuidance_ShouldUseOnlyPublishedCanonicalReferenceLinks()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = ReadManifest(repositoryRoot);
        string[] publishedUris = ReferenceResources(source)
            .Select(resource => resource.GetProperty("uri").GetString()!)
            .OrderBy(uri => uri, StringComparer.Ordinal)
            .ToArray();
        string[] guidanceFiles = Directory.GetFiles(
            Path.Combine(repositoryRoot, "guidance", "mcp", "guides"),
            "*.md",
            SearchOption.AllDirectories);

        // Act
        string completeGuidance = string.Join("\n", guidanceFiles.Select(File.ReadAllText));
        string[] canonicalLinks = Regex.Matches(
                completeGuidance,
                "docs://knowledge/com\\.creatio\\.clio/reference\\.[a-z0-9.-]+",
                RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(uri => uri, StringComparer.Ordinal)
            .ToArray();

        // Assert
        completeGuidance.Should().NotContain(LegacyPrefix,
            because: "new guidance must not depend on a legacy reference URI template");
        completeGuidance.Should().NotMatchRegex(@"references/[a-z0-9-]+\.md",
            because: "flat guide folders cannot resolve family-specific relative reference paths safely");
        canonicalLinks.Should().Equal(publishedUris,
            because: "every migrated supporting article must be reachable from its primary guide and every link must resolve");
    }

    private static IEnumerable<JsonElement> ReferenceResources(JsonDocument source) =>
        source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("uri").GetString()!.StartsWith(
                CanonicalPrefix,
                StringComparison.Ordinal));

    private static JsonDocument ReadManifest(string repositoryRoot) =>
        JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

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

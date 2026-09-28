using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class LocalizationGuidanceTests
{
    [Test]
    [Description("Keeps backend localization ownership, lookup, testing, and Freedom UI routing in one published guidance contract.")]
    public void LocalizableValuesGuide_ShouldPublishVerifiedOwnershipAndLookupRules()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "localizable-values.md"));
        string routing = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "routing.md"));
        string pageResources = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "page-schema", "resources.md"));
        string catalog = File.ReadAllText(Path.Combine(repositoryRoot,
            "catalog", "reference-examples", "creatio-localization.yaml"));
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "localizable-values");
        MatchCollection revisionMatches = Regex.Matches(catalog,
            "(?m)^[ \\t]*revision:[ \\t]*(\\S+)[ \\t]*\\r?$");

        // Assert
        guide.Should().Contain("A dedicated source-code schema MAY own package-level backend values",
            because: "the generated app primitive must remain a narrow owner rather than a global registry");
        guide.Should().Contain("Starting with Clio 8.1.0.111",
            because: "tool-dependent guidance must declare the first compatible Clio release");
        guide.Should().Contain("ILocalizableStringResolver",
            because: "the article must teach the injectable boundary over the Creatio platform primitive");
        guide.Should().Contain("LocalizableStringResolver",
            because: "the article must identify the one adapter that constructs the concrete platform type");
        guide.Should().Contain("MUST NOT construct `LocalizableString`",
            because: "transport entry points must remain validation and delegation boundaries");
        guide.Should().Contain("GetCultureValueWithFallback",
            because: "strict and fallback lookup are different observable contracts");
        guide.Should().Contain("throwIfNoManager: false",
            because: "the generated adapter must make the platform boolean's meaning explicit");
        guide.Should().Contain("string greeting = _strings.GetCultureValueWithFallback",
            because: "the C# 7.3 teaching example must keep returned values inspectable at a breakpoint");
        guide.Should().Contain("The resolver can still return `null`",
            because: "C# 7.3 syntax must not hide the runtime null contract");
        guide.Should().Contain("substituted `IResourceStorage`",
            because: "developers need an executable seam for unit-testing the concrete generated adapter");
        guide.Should().Contain("add `resource.<culture>.xml`",
            because: "the guide must explain how to create a secondary-culture resource file");
        guide.Should().Contain("Add or activate that culture in Creatio's Languages section",
            because: "a culture file alone is not testable until the platform language is active");
        guide.Should().Contain("`ResourceContent`",
            because: "resource-file assertions must be distinct from executable implementation tests");
        guide.Should().Contain("`Implementation`",
            because: "the concrete resolver and its consumers need an explicit unit-test category");
        guide.Should().Contain("100% line, branch, and method coverage",
            because: "the reference lab must fail when production behavior loses unit coverage");
        guide.Should().Contain("bundle.resources.strings.<Key>",
            because: "a rendered Freedom UI page needs a platform-oracle assertion beyond resource files");
        guide.Should().Contain("For an explicitly registered custom page resource",
            because: "the runtime bundle oracle must not be generalized to every Freedom UI resource type");
        guide.Should().Contain("absence from this node is not by itself proof of a defect",
            because: "the primary Freedom UI boundary must reject absence-only registration diagnoses");
        guide.Should().Contain("Do not add or change page resource metadata based only on absence",
            because: "recovery guidance must defer resource registration decisions to their canonical owner");
        guide.Should().Contain("also read `page-schema-resources`",
            because: "Freedom UI authoring rules already have one canonical owner");
        guide.Should().Contain("https://github.com/Advance-Technologies-Foundation/creatio-localization-lab",
            because: "agents need the independent executable reference after it is published");
        catalog.Should().Contain("revision: 2e65b3537c37bfb8b4264e3dc871828fb95c94c8",
            because: "the catalog must pin the exact reviewed reference revision");
        revisionMatches.Should().ContainSingle(
            because: "the reference catalog must expose exactly one revision pointer");
        revisionMatches[0].Groups[1].Value.Should().MatchRegex("^[0-9a-f]{40}$",
            because: "the sole reference revision must be an immutable lowercase commit SHA");
        guide.Should().Contain($"/tree/{revisionMatches[0].Groups[1].Value}",
            because: "the guide and catalog must point agents at the same reviewed lab revision");
        catalog.Should().Contain("status: published",
            because: "the merged reference is now publicly consumable");
        guide.Should().NotContain("ILocalizableStringHelper",
            because: "guidance must not prescribe a mechanism-named helper abstraction");
        routing.Should().Contain(
            "backend localizable values, schema ownership, culture fallback, or localization tests -> name=localizable-values; for Freedom UI page resources also read name=page-schema-resources",
            because: "agents must discover both localization and page-resource owners from the exact routing contract");
        pageResources.Should().Contain("MUST also read `localizable-values`",
            because: "Freedom UI work must link back to the localization ownership and testing contract");
        source.RootElement.GetProperty("requirements").GetProperty("itemIds").EnumerateArray()
            .Select(item => item.GetString()).Should().Contain("localizable-values",
                because: "activation requirements must include the localization guide item");
        source.RootElement.GetProperty("requirements").GetProperty("resourceUris").EnumerateArray()
            .Select(item => item.GetString()).Should().Contain(
                "docs://knowledge/com.creatio.clio/localizable-values",
                because: "activation requirements must include the localization guide URI");
        resource.GetProperty("uri").GetString().Should().Be(
            "docs://knowledge/com.creatio.clio/localizable-values",
            because: "the article needs one stable canonical route");
        resource.GetProperty("sourcePath").GetString().Should().Be(
            "guidance/mcp/guides/localizable-values.md",
            because: "Git consumers must read the canonical human-authored article");
    }

    [Test]
    [Description("Keeps the inline-literal rule for the push-workspace source path in its owner article, with the gate row only pointing to it.")]
    public void PageSchemaResourcesGuide_ShouldOwnTheSourcePathLiteralRule()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string pageResources = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "page-schema", "resources.md"));
        string pageModification = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "pages", "modification", "index.md"));

        // Act
        string gateRow = pageModification.Split('\n')
            .Single(line => line.Contains("| `page-schema-resources` |", StringComparison.Ordinal));

        // Assert
        pageResources.Should().Contain("SAME RULE ON THE SOURCE PATH (`push-workspace`)",
            because: "the owner article must state the inline-literal rule for workspace sources");
        pageResources.Should().Contain("`push-workspace` does NOT reject an inline literal",
            because: "push-workspace keeps installing literal-bearing pages and only warns");
        pageResources.Should().Contain("clio 8.1.0.134 and earlier do not have it",
            because: "tool-dependent guidance must state a checkable clio version boundary");
        pageResources.Should().Contain("resource binding on the literal-only `crt.ImageInput.tooltip`",
            because: "push-workspace warns about both text cases update-page rejects, not only inline literals");
        pageResources.Should().Contain("A schema file it cannot read gets its own warning with the file path",
            because: "an unreadable schema is skipped with a per-file warning while the other schemas are still checked");
        pageResources.Should().NotContain("clio/issues/1639",
            because: "guidance must name a clio version, not an internal issue, as the compatibility boundary");
        gateRow.Should().Contain("see `page-schema-resources` for how `push-workspace` handles inline literals",
            because: "the gate row must point to the owner article for the source-path rule");
        gateRow.Should().NotContain("warns per page schema",
            because: "the gate row must not restate the owner's push-workspace behavior");
    }

    [Test]
    [Description("Pins the page-translation safety rules, their single owner, and the route that reaches them.")]
    public void PageTranslationGuide_ShouldPublishSafetyRulesOwnerAndRoute()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guidesRoot = Path.Combine(repositoryRoot, "guidance", "mcp", "guides");
        string translation = File.ReadAllText(Path.Combine(guidesRoot, "page-schema", "translation.md"));
        string maintenance = File.ReadAllText(Path.Combine(guidesRoot, "applications",
            "existing-app-maintenance.md"));
        string localizableValues = File.ReadAllText(Path.Combine(guidesRoot, "localizable-values.md"));
        string routing = File.ReadAllText(Path.Combine(guidesRoot, "routing.md"));
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "page-schema-translation");
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Assert
        translation.Should().Contain("never send an empty string",
            because: "localize-page stores an empty string as the value, so the guide must forbid sending one");
        translation.Should().Contain("`culture: \"en-US\"` is REJECTED",
            because: "the default culture is changed through update-page, never through localize-page");
        translation.Should().Contain("writes ONE culture of ONE page per call",
            because: "each call must leave every other culture unchanged");
        maintenance.Should().Contain(
            "MUST call `get-tool-contract` for `update-app-section` before ANY update of an existing section",
            because: "an earlier clio ignores caption-culture and every section update deletes other-language titles");
        localizableValues.Should().Contain("owned by `page-schema-translation`",
            because: "the localize-page workflow has exactly one owning article");
        localizableValues.Should().NotContain("resource registration, and the `localize-page` workflow",
            because: "page-schema-resources no longer owns the localize-page workflow");
        routing.Should().Contain("translate a page or an app / add a language to it (page captions, page title) -> name=page-schema-translation",
            because: "agents must be routed to the owner of the page-translation workflow");
        resource.GetProperty("uri").GetString().Should().Be(
            "docs://knowledge/com.creatio.clio/page-schema-translation",
            because: "the article needs one stable canonical route");
        resource.GetProperty("sourcePath").GetString().Should().Be(
            "guidance/mcp/guides/page-schema/translation.md",
            because: "Git consumers must read the canonical human-authored article");
        requirements.GetProperty("itemIds").EnumerateArray()
            .Select(item => item.GetString()).Should().Contain("page-schema-translation",
                because: "activation requirements must include the page-translation guide item");
        requirements.GetProperty("resourceUris").EnumerateArray()
            .Select(item => item.GetString()).Should().Contain(
                "docs://knowledge/com.creatio.clio/page-schema-translation",
                because: "activation requirements must include the page-translation guide URI");
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

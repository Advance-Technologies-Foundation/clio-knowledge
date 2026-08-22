using System.Text.Json;
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
        guide.Should().Contain("also read `page-schema-resources`",
            because: "Freedom UI authoring rules already have one canonical owner");
        guide.Should().Contain("https://github.com/Advance-Technologies-Foundation/creatio-localization-lab",
            because: "agents need the independent executable reference after it is published");
        catalog.Should().Contain("revision: 273eb7531a8284b6072730b097769b95df56a02e",
            because: "the catalog must pin the exact reviewed reference revision");
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

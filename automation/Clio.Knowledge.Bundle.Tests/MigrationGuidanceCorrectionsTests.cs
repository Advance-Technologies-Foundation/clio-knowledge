using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class MigrationGuidanceCorrectionsTests
{
    private static readonly Regex UsrPrefixMandate = new(
        @"(MUST\s+)?(use|with)\s+the\s+`usr\.`\s+prefix",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Test]
    [Description("No guidance article presents usr. as the required namespace for custom names.")]
    public void Guidance_ShouldNotMandateUsrPrefix_InAnyArticle()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string[] offenders = [.. DeclaredGuidancePaths(repositoryRoot)
            .Where(path => UsrPrefixMandate.IsMatch(ProcessGuideSet.Read(repositoryRoot, path)))];

        // Assert
        offenders.Should().BeEmpty(
            because: "a team package with its own namespace must not be pushed to usr.; page-schema-handlers owns the rule");
    }

    [Test]
    [Description("Custom requests, converters and validators use the package's namespace; usr. is only the default.")]
    public void PageSchemaGuidance_ShouldRouteNamespaceRuleToHandlers_WhenNamingCustomEntries()
    {
        // Arrange
        string handlers = ReadGuide("page-schema/handlers.md");
        string converters = ReadGuide("page-schema/converters.md");
        string validators = ReadGuide("page-schema/validators.md");

        // Act
        string[] citingGuides = [converters, validators];

        // Assert
        handlers.Should().Contain("Custom namespace (this rule owns it for requests, converters and validators)",
                because: "one article owns the namespace rule and the others cite it")
            .And.Contain("Use `usr.` only when the package has no custom namespace yet.",
                because: "usr. stays the default for a package without a namespace")
            .And.Contain("Never use `crt.`",
                because: "crt. is the built-in namespace");
        citingGuides.Should().AllSatisfy(guide =>
            guide.Should().Contain("`page-schema-handlers`").And.Contain("\"Custom namespace\"",
                because: "the converter and validator guides cite the owner instead of restating the rule"));
        converters.Should().NotContain("Rename to `usr.<Name>`.",
            because: "the crt. lint remedy must send agents to the package's namespace, not to usr.");
    }

    [Test]
    [Description("The operator table comes before the first filter example, and handler code is routed to the SDK builder.")]
    public void FrontendFilterGuidance_ShouldShowOperatorsFirst_AndRouteHandlerCodeToSdk()
    {
        // Arrange
        string guidance = ReadGuide("esq-filters/frontend.md");

        // Act
        int operatorsIndex = guidance.IndexOf("## Operators (`comparisonType`)", StringComparison.Ordinal);
        int firstExampleIndex = guidance.IndexOf("\"comparisonType\":", StringComparison.Ordinal);

        // Assert
        operatorsIndex.Should().BePositive(because: "the operator table must stay in the guide");
        operatorsIndex.Should().BeLessThan(firstExampleIndex,
            because: "an agent reading the guide in slices skipped a table that sat after the examples");
        guidance.Should().Contain("Handler code is not a stored tree.",
                because: "the numeric JSON shape is for stored filter trees, not runtime handler code")
            .And.Contain("build it with `sdk.FilterGroup`",
                because: "handler code uses the SDK builder the devkit guide documents")
            .And.Contain("`page-schema-creatio-devkit-common`",
                because: "the SDK calls are owned by the devkit guide");
    }

    [Test]
    [Description("Row selection is described as the platform runs it, with source evidence.")]
    public void RelatedListGuidance_ShouldDescribeRowSelection_AsThePlatformRunsIt()
    {
        // Arrange
        string guidance = ReadGuide("related-data/list.md");

        // Act
        string section = Section(guidance, "Acting on the rows the user selected (row selection)");

        // Assert
        section.Should().Contain("\"_selectionOptions\": { \"attribute\": \"<GridName>_SelectionState\" }",
                because: "the Designer's shape is the one to write")
            .And.Contain("is NOT broken: at page load the platform derives",
                because: "the runtime fills a missing _selectionOptions, so it must not be named as a defect")
            .And.Contain("creatio-ui commit `cc78bd8be9b`",
                because: "the runtime claim needs its source evidence")
            .And.Contain("handles BOTH types",
                because: "a select-all state has no selected list");
    }

    [Test]
    [Description("The Designer-save section warns that the Read-only checkbox flattens a bound readonly.")]
    public void OverviewGuidance_ShouldWarnAboutReadOnlyToggle_WhenReadonlyIsBound()
    {
        // Arrange
        string guidance = ReadGuide("pages/modification/overview.md");

        // Act
        string section = Section(guidance, "What survives an Interface Designer save (web pages)");

        // Assert
        section.Should().Contain("A bound `readonly` survives an ordinary save, but NOT a touch of the Read-only checkbox.",
                because: "the warning belongs to the Designer-save section agents are routed to")
            .And.Contain("writes the literal `\"readonly\": true` to EVERY element bound to the same attribute",
                because: "the literal spreads to every element of the attribute")
            .And.Contain("Evidence: Creatio 10.2.959.0",
                because: "the claim must keep its stand evidence")
            .And.Contain("(\"Conditions a page rule cannot express\")",
                because: "the warning points to the alternative");
    }

    [Test]
    [Description("Page-rule operands match clio, entity-rule scope is stated, and lookup-filter limits are named.")]
    public void BusinessRulesGuidance_ShouldStateOperandsScopeAndLimits()
    {
        // Arrange
        string guidance = ReadGuide("business-rules/business-rules.md");

        // Act
        string conditionSection = Section(guidance, "Conditions a page rule cannot express (EXPERIMENTAL)");

        // Assert
        guidance.Should().NotContain("Condition attribute names come from bundle.viewModelConfig.attributes",
                because: "clio rejects virtual, collection and multi-segment attributes that viewModelConfig lists")
            .And.Contain("clio commit `a35201cdb`",
                because: "the operand list must name the clio source it mirrors")
            .And.Contain("Condition attributes clio rejects (\"Unknown or unsupported datasource-bound page attribute\")",
                because: "agents need the rejected kinds, with the message they will see")
            .And.Contain("There is no operand for an operation right (`CanEditX`) or a feature toggle.",
                because: "rights and features were the conditions the migration could not express")
            .And.Contain("Scope warning: an entity rule changes EVERY page and list of the object",
                because: "page-scoped requirements were silently widened to the whole object")
            .And.Contain("narrows the DROPDOWN only",
                because: "the _List_DS LoadDataRequest pattern does not reach the selection window");
        conditionSection.Should().Contain("`page-schema-handlers` (\"Context read/write patterns\")",
            because: "the handler guide owns the handler pattern and business-rules only routes to it");
    }

    [Test]
    [Description("The handler guide owns the EXPERIMENTAL read-only pattern with its evidence and limits.")]
    public void HandlersGuidance_ShouldOwnExperimentalReadOnlyPattern_WithItsLimits()
    {
        // Arrange
        string guidance = ReadGuide("page-schema/handlers.md");

        // Act
        string section = Section(guidance, "Context read/write patterns");

        // Assert
        section.Should().Contain("EXPERIMENTAL — read-only decided by page code",
                because: "the pattern is unconfirmed platform API and must say so")
            .And.Contain("request.$context.setAttributePropertyValue(\"<ControlAttribute>\", \"readonly\", <boolean>)",
                because: "the handler guide holds the call itself")
            .And.Contain("This read-only is presentation only, not access control",
                because: "an operation right must still be enforced on the server")
            .And.Contain("It is not confirmed as public devkit API for configuration pages",
                because: "the limit of the evidence must travel with the pattern");
    }

    private static string ReadGuide(string pathUnderGuides) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), "guidance/mcp/guides/" + pathUnderGuides);

    // The text from a heading line to the next heading line: a non-empty line after a blank line that is
    // not a list item, an indented continuation or a code fence.
    private static string Section(string guidance, string heading)
    {
        int start = guidance.IndexOf(heading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: $"the heading \"{heading}\" is cited by other articles");
        Match next = Regex.Match(guidance[(start + heading.Length)..], @"\n\s*\n(?=[^\s\-`|])");
        return next.Success
            ? guidance.Substring(start, heading.Length + next.Index)
            : guidance[start..];
    }

    private static IEnumerable<string> DeclaredGuidancePaths(string repositoryRoot)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return [.. manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "guidance")
            .Select(resource => resource.GetProperty("sourcePath").GetString()!)];
    }
}

using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class MigrationGuidanceCorrectionsTests
{
    [Test]
    [Description("Custom requests, converters and validators use the package's namespace; usr. is only the default.")]
    public void PageSchemaGuidance_ShouldNotRequireUsrNamespace_WhenNamingCustomEntries()
    {
        // Arrange
        string handlers = ReadGuide("page-schema/handlers.md");
        string converters = ReadGuide("page-schema/converters.md");
        string validators = ReadGuide("page-schema/validators.md");

        // Act
        string[] pageSchemaGuides = [handlers, converters, validators];

        // Assert
        pageSchemaGuides.Should().NotContain(
            guide => guide.Contains("use the `usr.` prefix", StringComparison.OrdinalIgnoreCase),
            because: "a team package with its own namespace must not be pushed to usr.");
        handlers.Should().Contain("Custom namespace (this rule owns it for requests, converters and validators)",
                because: "one article owns the namespace rule and the others cite it")
            .And.Contain("Use `usr.` only when the package has no custom namespace yet.",
                because: "usr. stays the default for a package without a namespace")
            .And.Contain("Never use `crt.`",
                because: "crt. is the built-in namespace");
        converters.Should().Contain("the namespace rule is in NON-NEGOTIABLES below",
            because: "the converter guide states the rule once and points to it");
        new[] { converters, validators }.Should().AllSatisfy(guide =>
            guide.Should().Contain("`page-schema-handlers`").And.Contain("\"Custom namespace\")",
                because: "the converter and validator guides cite the owner instead of restating the rule"));
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
        string section = guidance[guidance.IndexOf("Acting on the rows the user selected (row selection)", StringComparison.Ordinal)..];

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
        int sectionIndex = guidance.IndexOf("What survives an Interface Designer save (web pages)", StringComparison.Ordinal);
        int warningIndex = guidance.IndexOf("A bound `readonly` survives an ordinary save, but NOT a touch of the Read-only checkbox.", StringComparison.Ordinal);

        // Assert
        warningIndex.Should().BeGreaterThan(sectionIndex,
            because: "the warning belongs to the Designer-save section agents are routed to");
        guidance.Should().Contain("writes the literal `\"readonly\": true` to EVERY element bound to the same attribute",
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
        string conditionSection = guidance[guidance.IndexOf("Conditions a page rule cannot express (EXPERIMENTAL)", StringComparison.Ordinal)..];

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
        string section = guidance[guidance.IndexOf("Context read/write patterns", StringComparison.Ordinal)..];

        // Assert
        section.Should().Contain("EXPERIMENTAL — read-only decided by page code",
                because: "the pattern is unconfirmed platform API and must say so")
            .And.Contain("request.$context.setAttributePropertyValue(\"<ControlAttribute>\", \"readonly\", <boolean>)",
                because: "the handler guide holds the call itself")
            .And.Contain("It is not confirmed as public devkit API for configuration pages",
                because: "the limit of the evidence must travel with the pattern");
    }

    private static string ReadGuide(string pathUnderGuides) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), "guidance/mcp/guides/" + pathUnderGuides);
}

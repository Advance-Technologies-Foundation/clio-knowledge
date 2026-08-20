using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-95429 payload in the mobile page-modification guide: the
/// <c>OPERATION SHAPE</c> rule and the <c>BUTTON PLACEMENT</c> rule, plus the clauses that keep each
/// one correct and honestly bounded. The content digest cannot guard a rule's wording — it is
/// re-recorded on every edit — so a dropped scope note, a lost evidence pointer, or a regression to
/// the earlier (wrong) description of what the differ does with an unresolved parent would otherwise
/// ship green. Mirrors <see cref="ElementPlacementRuleTests"/>.
/// </summary>
[TestFixture]
public sealed class MobileOperationShapeRuleTests
{
    private const string Guide = "guidance/mcp/guides/platform/mobile/page-modification.md";

    private const string OperationShapeHeading = "OPERATION SHAPE — the component \"type\" MUST be inside \"values\"";
    // Ends at the section that follows it in the guide, NOT at VALIDATORS: AUTHORING CHILDREN was
    // inserted between the two, and using the later marker silently widened this section to include it,
    // which let four of the pins below match text they do not own (raised in review of clio-knowledge#74).
    private const string OperationShapeEnd = "AUTHORING CHILDREN";

    private const string ButtonPlacementHeading = "BUTTON PLACEMENT — not the Scaffold \"actions\"/\"leading\" slots";
    private const string ButtonPlacementEnd = "COMPONENT REGISTRY";

    private const string AuthoringChildrenHeading = "AUTHORING CHILDREN — a \"merge\" is not a reliable way to create them";
    private const string AuthoringChildrenEnd = "VALIDATORS, CONVERTERS, HANDLERS";

    private static readonly (string Fragment, string Because)[] AuthoringChildrenClauses =
    [
        ("STRIPS the whole property", "the mechanism — the property is removed before anything is copied — is what makes the rule true"),
        ("Target slot absent or empty", "the other outcome is NOT a failure, and hiding it would make the rule read as universal when it is not"),
        ("verified on a live stand for ENG-95429", "AGENTS.md requires an evidence pointer, and the bare ticket id also appears in the prose above, so only the full sentence pins it"),
        ("target platform version", "AGENTS.md requires a version boundary for a prescriptive runtime claim"),
        ("READ FROM THE APPLIER", "the claims that were reasoned rather than observed must stay labelled as such"),
        ("merge group runs before inserts", "the two-step idiom only works because of the ordering; dropping it leaves the remedy unexplained"),
        ("an insert into a property the element does not carry throws", "this is why one insert is not enough, and why the single-element slot has no alternative"),
        ("NOT a carve-out from the strip rule", "the single-element case must not read as exempt from the mechanism the section documents"),
        ("it REPLACES the element", "an insert into a POPULATED single-element slot is the working route; saying only that a merge is the only route leaves that case with no answer"),
        ("EMPTY base", "the reason clio warns rather than refuses outside the Scaffold slots must stay visible"),
        ("two operations", "an insert into a slot the element lacks throws, so the two-step idiom is the only route and must not be dropped"),
        ("EMPTY array is exactly that first step", "the escape hatch must stay stated, or the rule reads as blocking the idiom it recommends"),
        ("Keep it an object", "following the collection idiom on a single-element slot such as floatAction would corrupt the shape"),
        ("they stay advisory", "outside the Scaffold the rule warns rather than blocking, and 'not affected' would read as silent"),
        ("viewConfigDiff rule", "the path diffs carry named config nodes inline by design, and over-applying the rule there would refuse clio's own converter output"),
        ("ANY OTHER element are not", "the blocking set is consulted only for a merge on the Scaffold; readers must not generalise it to every items slot"),
        ("BlankMobilePageTemplate", "the false positive the split accepts must be named, not discovered by whoever hits it"),
        ("BECOMES the element", "insert/set are out of scope and children declared in their values ARE created")
    ];

    private static readonly (string Fragment, string Because)[] OperationShapeClauses =
    [
        ("from \"values\" ALONE", "the mechanism — the element is built from values and nothing else — is what makes the rule true"),
        ("ENG-95429", "AGENTS.md requires an evidence pointer for a prescriptive runtime claim"),
        ("target platform version", "AGENTS.md requires a version boundary for a prescriptive runtime claim"),
        ("MOBILE bodies only", "the differ is shared with web, so the enforcement boundary must stay explicit"),
        ("still be refused by update-page", "a body can pass validate-page and fail the write; dropping this teaches a false green"),
        ("came back from get-page", "an already-defective page reads back defective, and the agent must fix it rather than retry"),
        ("REJECTED", "the outcome table is what keeps the guide and the validator from drifting apart"),
        ("SILENT", "the shapes clio deliberately does not diagnose must stay listed as such")
    ];

    private static readonly (string Fragment, string Because)[] ButtonPlacementClauses =
    [
        ("ENG-95429", "AGENTS.md requires an evidence pointer for a prescriptive claim"),
        ("falls back to the root", "an unresolved parentName PERSISTS the element at the root; describing it as dropped is wrong and was corrected once already"),
        ("extrapolation", "only the actions slot was tested — the leading slot must stay labelled as an extension of that observation"),
        ("NOT claimed", "the guide must keep disclaiming any statement about runtime rendering, which was never tested"),
        ("patches PROPERTIES", "the rule is about an insert of your own button; a merge that patches an existing element PROPERTIES is a different case and must not be read as blocked"),
        ("elementMap", "a converted page follows its elementMap placement, so the two guides cannot be read as contradicting")
    ];

    [Test]
    [Description("The OPERATION SHAPE rule still exists as a section of the mobile guide with every load-bearing clause intact.")]
    public void OperationShapeRule_ShouldExistWithLoadBearingClauses()
    {
        // Arrange
        string guide = ReadGuide();

        // Act
        string section = Section(guide, OperationShapeHeading, OperationShapeEnd);

        // Assert
        DroppedClauses(section, OperationShapeClauses).Should().BeEmpty(
            because: "each clause was added to make the rule correct or to bound it honestly; "
                + "dropping one re-records the content digest silently");
    }

    [Test]
    [Description("The BUTTON PLACEMENT rule still exists as a section of the mobile guide with every load-bearing clause intact.")]
    public void ButtonPlacementRule_ShouldExistWithLoadBearingClauses()
    {
        // Arrange
        string guide = ReadGuide();

        // Act
        string section = Section(guide, ButtonPlacementHeading, ButtonPlacementEnd);

        // Assert
        DroppedClauses(section, ButtonPlacementClauses).Should().BeEmpty(
            because: "each clause was added to make the rule correct or to bound it honestly; "
                + "dropping one re-records the content digest silently");
    }

    [Test]
    [Description("The AUTHORING CHILDREN rule still exists as a section of the mobile guide with every load-bearing clause intact.")]
    public void AuthoringChildrenRule_ShouldExistWithLoadBearingClauses()
    {
        // Arrange
        string guide = ReadGuide();

        // Act
        string section = Section(guide, AuthoringChildrenHeading, AuthoringChildrenEnd);

        // Assert
        DroppedClauses(section, AuthoringChildrenClauses).Should().BeEmpty(
            because: "each clause was added to make the rule correct or to bound it honestly; "
                + "dropping one re-records the content digest silently");
    }

    [Test]
    [Description("The button-placement rule must not send a reader towards a merge: the carve-out it used to carry was the hole this guide now closes.")]
    public void ButtonPlacementRule_ShouldNotCarveOutMerge()
    {
        // Arrange
        string guide = ReadGuide();

        // Act
        string section = Normalize(Section(guide, ButtonPlacementHeading, ButtonPlacementEnd));

        // Assert
        section.Should().Contain(Normalize("a merge carrying the button inside values.actions is a WORSE failure"),
            because: "an insert-scoped rule must say what a merge does, or a reader treats merge as the way around it");
        section.Should().Contain("AUTHORING CHILDREN",
            because: "the pointer to the owning rule is what keeps the two sections from contradicting each other");
        section.Should().NotContain(Normalize("A \"merge\"/\"set\" that patches an element the template already owns in that slot is a different case; nothing here applies to it."),
            because: "that exact carve-out is the hole this change closed, and re-adding it alongside the pointer would restore it");
    }

    [Test]
    [Description("The OPERATION SHAPE rule sits with the body-format material, not inside the Scaffold section, so a reader looking for the body's shape finds it.")]
    public void OperationShapeRule_ShouldPrecedeTheScaffoldMaterial()
    {
        // Arrange
        string guide = ReadGuide();

        // Act
        string normalized = Normalize(guide);
        int shapeIndex = normalized.IndexOf(Normalize(OperationShapeHeading), StringComparison.Ordinal);
        int scaffoldIndex = normalized.IndexOf("crt.Scaffold — do NOT re-insert", StringComparison.Ordinal);

        // Assert
        shapeIndex.Should().BeGreaterThanOrEqualTo(0, because: "the rule must exist under its own heading");
        scaffoldIndex.Should().BeGreaterThan(shapeIndex,
            because: "the operation shape is a property of every body, not of the Scaffold; it belongs next to "
                + "BODY FORMAT so it is read before any insert is authored");
    }

    private static string[] DroppedClauses(string section, (string Fragment, string Because)[] clauses) =>
        clauses
            .Where(clause => !Normalize(section).Contains(Normalize(clause.Fragment), StringComparison.Ordinal))
            .Select(clause => $"{clause.Fragment} ({clause.Because})")
            .ToArray();

    // Clauses are matched against whitespace-collapsed text so a hand-wrapped line break inside a
    // pinned phrase does not read as a dropped clause.
    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");

    private static string Section(string guide, string heading, string endHeading)
    {
        // Both sides are normalized so a hand-rewrapped heading reports as a reflowed line through the
        // clause assertions, not as a missing section.
        string normalized = Normalize(guide);
        int start = normalized.IndexOf(Normalize(heading), StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: $"the guide must still carry the '{heading}' section");
        int end = normalized.IndexOf(Normalize(endHeading), start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start,
            because: $"the section must be bounded by '{endHeading}' so clauses are asserted INSIDE it");
        return normalized[start..end];
    }

    private static string ReadGuide() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), Guide.Replace('/', Path.DirectorySeparatorChar)));

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

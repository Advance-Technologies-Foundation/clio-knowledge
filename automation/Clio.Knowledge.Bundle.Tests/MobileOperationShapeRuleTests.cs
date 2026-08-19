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
    private const string OperationShapeEnd = "VALIDATORS, CONVERTERS, HANDLERS";

    private const string ButtonPlacementHeading = "BUTTON PLACEMENT — not the Scaffold \"actions\" / \"leading\" slots";
    private const string ButtonPlacementEnd = "COMPONENT REGISTRY";

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
        ("merge\"/\"set\"", "the rule is about an insert of your own button and must not be read as blocking a legitimate patch"),
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
    [Description("The OPERATION SHAPE rule sits with the body-format material, not inside the Scaffold section, so a reader looking for the body's shape finds it.")]
    public void OperationShapeRule_ShouldPrecedeTheScaffoldMaterial()
    {
        // Arrange
        string guide = ReadGuide();

        // Act
        int shapeIndex = guide.IndexOf(OperationShapeHeading, StringComparison.Ordinal);
        int scaffoldIndex = guide.IndexOf("crt.Scaffold — do NOT re-insert", StringComparison.Ordinal);

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
        int start = guide.IndexOf(heading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: $"the guide must still carry the '{heading}' section");
        int end = guide.IndexOf(endHeading, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start,
            because: $"the section must be bounded by '{endHeading}' so clauses are asserted INSIDE it");
        return guide[start..end];
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

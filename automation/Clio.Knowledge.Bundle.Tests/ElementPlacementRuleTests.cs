using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-94937 payload: the <c>ELEMENT PLACEMENT IS AUTHORITATIVE</c> rule, the
/// load-bearing clauses that make it correct, and the intra-guide citations that point at it. The
/// content digest cannot guard a rule's wording — it is re-recorded on every edit — so a rename, a
/// dropped clause, a moved bullet, or a dangling "see … in HARD MOBILE RULES" citation would
/// otherwise ship green.
/// </summary>
[TestFixture]
public sealed class ElementPlacementRuleTests
{
    private const string RuleName = "ELEMENT PLACEMENT IS AUTHORITATIVE";
    private const string OwnerGuide = "guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md";
    private const string SectionHeading = "HARD MOBILE RULES";
    private const string NextSectionHeading = "LIMITATIONS (be transparent)";

    // The ELEMENT PLACEMENT rule is deliberately SELF-CONTAINED in the conversion guide (the only place
    // an elementMap is consumed). General mobile-editing guides — page-modification.md, containers.md —
    // stay converter-free, so there are no reciprocal pointers to guard here; the citation scan below runs
    // over the owner guide alone.

    // The clauses added by the ENG-94937 review rounds that each carry weight: dropping any one leaves
    // the rule green but wrong, so each is pinned with the reason it protects.
    private static readonly (string Fragment, string Because)[] LoadBearingClauses =
    [
        ("for component SHAPE", "the scope boundary keeps get-component-info authoritative for component shape"),
        ("QuickFilterGroup_Value", "the runtime mechanism must name the model attribute the chips are built from"),
        ("crt.QuickFilterGroupAttributeConverter", "the runtime mechanism must name the converter that materializes the chips"),
        ("ENG-94937", "AGENTS.md requires an evidence pointer for a prescriptive runtime claim"),
        ("target platform version", "AGENTS.md requires a version boundary for a prescriptive runtime claim"),
        ("necessary but NOT sufficient", "placement alone must not be presented as enough — the model side is also required"),
        ("incomplete guide output", "a missing model side must route to STOP-and-report, not hand-authoring (no improvising)")
    ];

    [Test]
    [Description("The ELEMENT PLACEMENT IS AUTHORITATIVE rule still exists as a bullet INSIDE the HARD MOBILE RULES section, with every load-bearing clause intact.")]
    public void Rule_ShouldExistWithLoadBearingClausesInsideSection()
    {
        string section = HardMobileRulesSection(ReadGuide(OwnerGuide));

        section.Should().MatchRegex($@"(?m)^- {Regex.Escape(RuleName)}\b",
            because: "the rule is the whole payload of ENG-94937 and must live in the section its citations point at; "
                + "renaming, deleting, or moving it out of HARD MOBILE RULES must fail a test");

        string sectionNormalized = Normalize(section);
        string[] droppedClauses = LoadBearingClauses
            .Where(clause => !sectionNormalized.Contains(clause.Fragment))
            .Select(clause => $"{clause.Fragment} ({clause.Because})")
            .ToArray();

        droppedClauses.Should().BeEmpty(
            because: "each clause was added to make the rule correct; dropping one re-records the digest silently");
    }

    [Test]
    [Description("Both 'see <RULE NAME> in HARD MOBILE RULES' anchor citations in the owner guide exist and resolve to a bullet with that exact name.")]
    public void RuleCitations_ShouldExistAndResolveToABullet()
    {
        string ownerNormalized = Normalize(ReadGuide(OwnerGuide));

        // Widened capture (up to the " in " delimiter) so a cited name carrying a hyphen, backtick, slash or
        // parenthesis is validated instead of silently skipped.
        string[] citedRules = Regex.Matches(ownerNormalized, @"see ([A-Za-z0-9 '`/().-]+?) in HARD MOBILE RULES")
            .Select(match => match.Groups[1].Value.Trim())
            .ToArray();

        citedRules.Length.Should().BeGreaterThanOrEqualTo(2,
            because: "the two anchor citations — the elementMap field description and the FLOW insert branch, the exact "
                + "points where the model improvised the parent — must both exist, not just be resolvable when absent");

        string[] unresolved = citedRules
            .Where(citedRule => !ownerNormalized.Contains("- " + citedRule))
            .Distinct()
            .ToArray();

        unresolved.Should().BeEmpty(
            because: "a citation naming a rule that no bullet defines is a dangling pointer");
    }

    private static string HardMobileRulesSection(string guide)
    {
        int start = guide.IndexOf(SectionHeading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: "the owner guide must have a HARD MOBILE RULES section");
        int end = guide.IndexOf(NextSectionHeading, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start,
            because: "the HARD MOBILE RULES section must be bounded by the next section so the bullet is asserted INSIDE it");
        return guide[start..end];
    }

    private static string ReadGuide(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");

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

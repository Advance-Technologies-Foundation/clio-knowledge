using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-93152 payload: the <c>RETARGET INTO A TEMPLATE-PROVIDED PARENT</c> HARD RULE and the
/// load-bearing clauses that make it correct. The content digest cannot guard a rule's wording — it is
/// re-recorded on every edit — so a rename, a dropped clause, or a moved bullet would otherwise ship
/// green. This rule exists specifically because an agent got it wrong in practice (it recreated a
/// template-provided parent instead of inserting only the children), so its clauses are load-bearing.
/// </summary>
[TestFixture]
public sealed class RetargetParentRuleTests
{
    private const string RuleName = "RETARGET INTO A TEMPLATE-PROVIDED PARENT";
    private const string OwnerGuide = "guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md";
    private const string SectionHeading = "HARD MOBILE RULES";
    private const string NextSectionHeading = "LIMITATIONS (be transparent)";

    // Each clause carries weight: dropping any one leaves the rule green but wrong, so each is pinned with
    // the reason it protects.
    private static readonly (string Fragment, string Because)[] LoadBearingClauses =
    [
        ("parentExistsOnTemplate", "the rule is driven by this feature-detected signal; without naming it the caller cannot act on it"),
        ("guide.constraints", "the instruction is also repeated at runtime, so the rule must say where"),
        ("mobile-page-modification", "the single-element-slot rule is OWNED there and only cross-referenced here — not duplicated"),
        ("single-element-slot", "the rule must name the owned mechanism it is the conversion-time reminder of"),
        ("inherited from the web template", "the drop is decided by web-template-baseline membership, not by a mobile-template name match"),
        ("above the web-template baseline", "a page-AUTHORED element is not chrome and must still convert — the rule must say so"),
        ("predates the flag", "the rule must degrade gracefully on an older guide that omits the signal")
    ];

    [Test]
    [Description("The RETARGET INTO A TEMPLATE-PROVIDED PARENT rule still exists as a bullet INSIDE the HARD MOBILE RULES section, with every load-bearing clause intact.")]
    public void Rule_ShouldExistWithLoadBearingClausesInsideSection()
    {
        string section = HardMobileRulesSection(ReadGuide(OwnerGuide));

        section.Should().MatchRegex($@"(?m)^- {Regex.Escape(RuleName)}\b",
            because: "the rule is the payload of ENG-93152 and must live in the section HARD MOBILE RULES; "
                + "renaming, deleting, or moving it out must fail a test");

        string sectionNormalized = Normalize(section);
        string[] droppedClauses = LoadBearingClauses
            .Where(clause => !sectionNormalized.Contains(clause.Fragment))
            .Select(clause => $"{clause.Fragment} ({clause.Because})")
            .ToArray();

        droppedClauses.Should().BeEmpty(
            because: "each clause was added to make the rule correct; dropping one re-records the digest silently");
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

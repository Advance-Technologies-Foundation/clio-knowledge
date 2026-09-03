using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-93152 payload: the <c>NEVER AUTHOR A PARENT THE DIFF DOES NOT CREATE</c> HARD RULE and
/// the load-bearing clauses that make it correct. The content digest cannot guard a rule's wording — it is
/// re-recorded on every edit — so a rename, a dropped clause, or a moved bullet would otherwise ship
/// green. This rule exists specifically because an agent got it wrong in practice (it recreated a
/// template-provided parent instead of inserting only the children), so its clauses are load-bearing.
/// </summary>
/// <remarks>
/// ENG-95827 renamed the rule and re-pinned two clauses, deliberately — the guard caught both, which is
/// what it is for. It was <c>RETARGET INTO A TEMPLATE-PROVIDED PARENT</c> and keyed off
/// <c>parentExistsOnTemplate</c>, a boolean that clio set on its three RETARGET code paths only, so an
/// ORDINARY insert into a template-provided parent carried nothing and the rule did not reach it. clio now
/// stamps <c>parentSource</c> on every insert, so the rule covers every parent rather than retargets alone
/// and its name says so. The <c>guide.constraints</c> clause is gone because it stopped being true: the
/// instruction is no longer repeated per response, so pinning it would force the guide to claim a runtime
/// duplicate that does not exist. The older-guide fallback is still pinned — a guide that predates
/// <c>parentSource</c> must not have the boolean's ABSENCE read as "safe to author".
/// </remarks>
[TestFixture]
public sealed class RetargetParentRuleTests
{
    private const string RuleName = "NEVER AUTHOR A PARENT THE DIFF DOES NOT CREATE";
    private const string OwnerGuide = "guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md";
    private const string SectionHeading = "HARD MOBILE RULES";
    private const string NextSectionHeading = "LIMITATIONS (be transparent)";

    // Each clause carries weight: dropping any one leaves the rule green but wrong, so each is pinned with
    // the reason it protects.
    private static readonly (string Fragment, string Because)[] LoadBearingClauses =
    [
        ("unresolvedParents", "the only case the response still reports as a field, and the only one the caller cannot derive from the diff itself"),
        ("insert ONLY the child", "the directive itself; without it the rule states a fact and asks for nothing"),
        ("mobile-page-modification", "the single-element-slot rule is OWNED there and only cross-referenced here — not duplicated"),
        ("single-element-slot", "the rule must name the owned mechanism it is the conversion-time reminder of"),
        ("drop-inherited-chrome", "the drop is decided by web-template-baseline membership, not by a mobile-template name match; pinned as the reason CODE since ENG-95827 replaced the sentence with one"),
        ("above the web-template baseline", "a page-AUTHORED element is not chrome and must still convert — the rule must say so"),
        ("parentExistsOnTemplate", "an older guide carries this retarget-only boolean instead, and the rule must name it to be usable there"),
        ("older guide", "the rule must degrade gracefully, and specifically must not let the old boolean's ABSENCE be read as \"safe to author\"")
    ];

    [Test]
    [Description("The NEVER AUTHOR A PARENT THE DIFF DOES NOT CREATE rule still exists as a bullet INSIDE the HARD MOBILE RULES section, with every load-bearing clause intact.")]
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

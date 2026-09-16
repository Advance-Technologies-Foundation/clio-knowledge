using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the clauses of the conversion article that describe the response SHAPE — the closed value sets a
/// caller branches on, and the directives that tell it to stop rather than guess.
/// </summary>
/// <remarks>
/// <para>
/// The content digest is re-recorded on every edit, so it cannot guard wording. Guards already exist for
/// the 23 reason codes, the parent-authoring rule, the excluded-components codes and the button-placement
/// carve-out; the fields added or corrected by ENG-95827 had none. They are exactly the kind that desync
/// silently: <c>dataSectionConflicts.kind</c> and <c>componentSuggestions.category</c> are closed
/// vocabularies a caller switches on, and <c>unresolvedParents</c> / <c>sectionRegistration</c> /
/// <c>layoutResolution</c> each carry a STOP directive whose whole value is that it is not softened.
/// </para>
/// <para>
/// This repository cannot reach clio, so these are literals on one side of a two-sided contract, same as
/// <c>MobileDropReasonCodeCoverageTests</c>: a value renamed in clio fails THERE, a clause deleted here
/// fails HERE.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ResponseShapeFieldGuidanceTests
{
    private const string OwnerGuide = "guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md";

    // Closed value sets. Spelling is the contract: a caller branches on these strings verbatim, so a
    // case change in the article is as wrong as a missing entry.
    private static readonly (string Fragment, string Because)[] ClosedVocabularies =
    [
        ("DirectMapping", "a componentSuggestions category, PascalCase exactly as it ships"),
        ("AlternativeAvailable", "a componentSuggestions category, PascalCase exactly as it ships"),
        ("WithAdaptation", "a componentSuggestions category, and the one NOT derived from viewConfigDiff"),
        ("Unsupported", "a componentSuggestions category, PascalCase exactly as it ships"),
        ("RequiresManualDecision", "a componentSuggestions category, PascalCase exactly as it ships")
    ];

    // Directives whose value is that they are absolute. Softening one is invisible to a digest.
    private static readonly (string Fragment, string Because)[] StopDirectives =
    [
        ("unresolvedParents",
            "an insert whose parent NOTHING provides; the caller must report it and stop rather than invent a container"),
        ("sectionRegistration",
            "registering the converted page as a mobile section is its own gated step, not part of the body build"),
        ("layoutResolution",
            "how the mobile layout was decided is reported, never re-derived by the caller"),
        ("dataSectionConflicts",
            "a data-section change the differ cannot express as a targeted merge; the caller is told, not asked to resolve it")
    ];

    // The WithAdaptation carve-out. The field entry once claimed the WHOLE section was derived from
    // viewConfigDiff while the classification section defined this one category as rules-only, which sent
    // an agent looking for operation evidence the response cannot carry.
    private static readonly (string Fragment, string Because)[] DerivationCarveOut =
    [
        ("Four of the five are DERIVED from the finished viewConfigDiff",
            "the blanket claim was the contradiction; the count is what makes the exception below readable"),
        ("WithAdaptation is the exception",
            "naming the exception is the fix — an agent must not look for an operation backing this category"),
        ("only from a conversion rule that declares it",
            "where the judgement actually comes from, so the caller knows it is unverifiable against the diff")
    ];

    // resourceStrings. The PR's own pre-review found this section self-contradicting; nothing pinned the
    // corrected wording, so the same contradiction could return unnoticed.
    private static readonly (string Fragment, string Because)[] ResourceStringClauses =
    [
        ("the SOURCE PAGE DECLARES",
            "declared, not referenced — the distinction the corrected wording turns on, and the half that was self-contradicting"),
        ("declared text is EMPTY is included on purpose",
            "an empty value is the page's own 'no visible label', so dropping it as noise makes the mobile page differ from the web one"),
        ("NOT EVERY #ResourceString TOKEN IN THE BODY HAS AN ENTRY HERE",
            "without this an agent reads a missing entry as a converter bug and starts filling gaps"),
        ("do NOT invent",
            "the directive itself: an invented key REPLACES a localized column title with one hardcoded culture")
    ];

    [Test]
    [Description("Every componentSuggestions category ships in the article with the exact PascalCase spelling a caller branches on. A case change or a dropped entry re-records the digest silently.")]
    public void Guide_ShouldCarryEveryComponentSuggestionCategory_VerbatimAsItShips()
    {
        AssertAllPresent(ClosedVocabularies, caseSensitive: true);
    }

    [Test]
    [Description("The fields that tell a caller to report and stop are still named. Each is the only channel for a decision the caller cannot make from the diff, so losing one turns a reportable state into a silent guess.")]
    public void Guide_ShouldCarryEveryStopDirectiveField()
    {
        AssertAllPresent(StopDirectives, caseSensitive: true);
    }

    [Test]
    [Description("The componentSuggestions field entry states WHICH categories are derived from viewConfigDiff and which is not. Restoring the blanket 'DERIVED' claim would contradict the classification section in the same article, and an agent would treat a rules-only judgement as diff-confirmed.")]
    public void Guide_ShouldCarveOutWithAdaptation_FromTheDerivedFromDiffClaim()
    {
        AssertAllPresent(DerivationCarveOut, caseSensitive: true);
    }

    [Test]
    [Description("The corrected resourceStrings semantics survive. The article previously contradicted itself here and the fix had no guard, so the contradiction could return without failing anything.")]
    public void Guide_ShouldKeepTheCorrectedResourceStringsClauses()
    {
        AssertAllPresent(ResourceStringClauses, caseSensitive: true);
    }

    // The list row. clio builds it on BOTH paths; the article said it was prebuilt for an INSERT only
    // and handed the merge path back to the caller, which is how a converted section list shipped with
    // no title and no body while nothing failed.
    private static readonly (string Fragment, string Because)[] ListRowClauses =
    [
        ("THE CONVERTER BUILDS IT FOR YOU ON BOTH PATHS",
            "the correction itself — an agent that believes the row is its job on the merge path writes one by hand or, more often, writes none"),
        ("arrives as its OWN operation",
            "where the row IS on the merge path: a merge on ListItem beside the advisory merge on List, not a value of the parent"),
        ("NEVER put itemLayout inside a merge of the parent List",
            "the neighbouring mistake: crt.List is not a container and itemLayout is an input, so addressing it as a child slot fails the WHOLE schema build"),
        ("provides List but NO ListItem under it",
            "the one case that IS still the caller's — naming it is what keeps the blanket claim above safe to act on")
    ];

    [Test]
    [Description("The article states that the grid row is built by the converter on the merge path as well as the insert path, names the operation it arrives as, and still forbids folding itemLayout into the parent List's merge. It previously said the row was prebuilt for an INSERT only, which was true of clio and is no longer: a section list converted onto a template that already provides List/ListItem shipped one empty merge and no row at all.")]
    public void Guide_ShouldSayTheConverterBuildsTheListRow_OnTheMergePathToo()
    {
        AssertAllPresent(ListRowClauses, caseSensitive: true);
    }

    [Test]
    [Description("No step tells the caller to APPLY an element map. ElementMapEntry is converter bookkeeping that is never serialized, so an instruction to iterate it in order cannot be followed — the response carries viewConfigDiff and no elementMap. Naming the old field is fine and necessary in the back-compat clause; instructing the reader to work from it is not, so this forbids the instructions rather than the word.")]
    public void Guide_ShouldNeverInstructApplyingAnElementMap()
    {
        string guide = Normalize(ReadGuide(OwnerGuide));

        guide.Should().NotContain("element-map order",
            because: "the response carries no element map, so ordering by one is an instruction with no subject");
        guide.Should().NotContain("inserts in the element map",
            because: "same instruction in prose form - the synthesized layers arrive in viewConfigDiff");
        guide.Should().NotContain("baked into the element map",
            because: "the third phrasing of the same instruction");
    }

    [Test]
    [Description("The rename that carries the payload has a back-compat clause. constraints, diagnostics, nextSteps and parentExistsOnTemplate are all hedged for a reader on an older clio; elementMap was not, even though it is the field every instruction here depends on. Publishing this article before clio reaches users would otherwise tell an opted-in reader to paste a viewConfigDiff their response does not contain — and an empty one is ACCEPTED by validate-page, so the failure is a blank page rather than an error.")]
    public void Guide_ShouldHedgeTheViewConfigDiffRename_ForAReaderOnAnOlderClio()
    {
        string guide = Normalize(ReadGuide(OwnerGuide));

        guide.Should().Contain("OLDER clio returns them as `elementMap`",
            because: "the reader has to be told which field to look for instead, by name");
        guide.Should().Contain("mobileValues",
            because: "the entry-level renames are part of the mapping and useless without it");
        guide.Should().Contain("empty body because the field you expected is missing",
            because: "the failure mode is silent - an empty viewConfigDiff validates and ships a blank page");
    }

    private static void AssertAllPresent((string Fragment, string Because)[] clauses, bool caseSensitive)
    {
        string guide = Normalize(ReadGuide(OwnerGuide));
        string[] missing = clauses
            .Where(clause => !guide.Contains(clause.Fragment,
                caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            .Select(clause => $"{clause.Fragment} ({clause.Because})")
            .ToArray();

        missing.Should().BeEmpty(
            because: "each clause describes a closed value set or a stop directive the caller acts on; the "
                + "content digest is re-recorded on every edit and cannot notice one going missing");
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

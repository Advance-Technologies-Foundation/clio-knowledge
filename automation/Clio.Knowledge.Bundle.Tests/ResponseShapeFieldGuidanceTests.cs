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
/// <para>
/// PR #174 added <c>resolvedCandidateSchemaName</c>, <c>resolvedSourceType</c>, <c>recommendedAction</c>
/// and the corrected FIVE-collection count for clio #1562, and separately reinstated
/// <c>bindingRemoved</c> / <c>drop-request-target-missing</c> after a brief retirement (commit 6cab799)
/// that a review caught before merge. Neither had a pin, so the same review would be needed again on the
/// next silent drift; <see cref="UnresolvedTargetRequestFields"/> and
/// <see cref="BindingRemovedContract"/> exist so it is not.
/// </para>
/// <para>
/// clio #1562 (ENG-94839), companion to toolkit #183, later moved missing-target dedup and the
/// existing-mobile-page check server-side: <c>existingMobilePages</c> is a brand-new field, and
/// <c>missingTargetPages</c> silently widened from web-page-only to also aggregating
/// entity-default-mobile-page targets. <see cref="ExistingMobilePagesFields"/> pins both.
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

    // unresolvedTargetRequests / requestConversions field set clio #1562 introduced. PR #174's review found
    // the article silent on resolvedCandidateSchemaName and stale on the collection count (it still said
    // FOUR after missingTargetPages became a fifth); nothing pinned the fix, so the same drift could return
    // unnoticed the way the FOUR count itself once did. resolvedSourceType/recommendedAction were briefly
    // documented here as always-null RESERVED fields — clio #1562 deleted both outright as dead wire surface
    // (a settable-but-never-written property is not a contract). The pin below asserts the NEGATION
    // sentence verbatim rather than the bare identifiers: PR #174's own review (Major) caught that
    // AssertAllPresent is guide.Contains(fragment) — a presence check — so pinning "resolvedSourceType"
    // alone is satisfied equally by the sentence this requires and by the sentence it forbids ("...reads
    // null"). Pinning the full negation closes that hole; NoResolvedSourceTypeOrRecommendedAction below
    // additionally forbids the positive phrasings the negation must never be replaced by.
    private static readonly (string Fragment, string Because)[] UnresolvedTargetRequestFields =
    [
        ("resolvedCandidateSchemaName",
            "the entity-default-mobile-page read's candidate web edit page; null when none was found, and never a schema to assume is on mobile"),
        ("No `resolvedSourceType`/`recommendedAction`.",
            "clio deleted both fields outright; pinning the negation sentence itself (not just the identifiers) is what actually fails if the article reverts to claiming they exist and read null"),
        ("FIVE collections",
            "the count itself: missingTargetPages is the fifth, and a caller iterating requestConversions by this number would skip it if it silently reverted to FOUR"),
        ("missingTargetPages",
            "must be named among the requestConversions collections, not only in its own field entry, or an agent counting collections never finds it")
    ];

    // Complements UnresolvedTargetRequestFields: forbids the positive phrasings the negation sentence must
    // never be replaced by. A presence-only pin on the bare identifiers could not distinguish these from
    // the required negation; these NotContain checks can.
    private static readonly string[] ResolvedSourceTypeForbiddenPhrasings =
    [
        "resolvedSourceType` — reserved",
        "resolvedSourceType` - reserved",
        "resolvedSourceType` reads null",
        "recommendedAction` reads null",
        "resolvedSourceType` is always null",
        "recommendedAction` is always null"
    ];

    // guide.existingMobilePages and the widened missingTargetPages aggregation clio #1562 added
    // (companion to toolkit #183, ENG-94839). Neither had a pin: existingMobilePages is a brand-new
    // top-level field a caller must check before Gate M, and missingTargetPages silently went from
    // web-page-only to covering entity-default-mobile-page targets too — a caller that assumed the old
    // web-page-only scope would miss half the queue with no error to notice it by.
    private static readonly (string Fragment, string Because)[] ExistingMobilePagesFields =
    [
        ("existingMobilePages",
            "the reuse-vs-convert-again field; losing its entry would leave a caller searching for an existing mobile page by hand again"),
        ("entity-default-mobile-page",
            "one of the two existingMobilePages.source values and one of the two kinds missingTargetPages now aggregates; losing it collapses the vocabulary back to one kind"),
        ("collapses into",
            "the rule that a web-page target and an entity target sharing a schema name merge into ONE missingTargetPages row instead of two"),
    ];

    // The two-repository vocabulary contract clio #1562 briefly broke: commit 6cab799 retired
    // drop-request-target-missing and stripped bindingRemoved from this article on the premise the
    // converter never removes a binding for a missing navigation target, then commit 268c2d5 reinstated
    // both once ENG-94839's review showed the "always keep" behavior violated AC-2. Nothing pinned the
    // OWNER article's half of that reversal, so the same silent strip could recur without failing here —
    // only MobileDropReasonCodeCoverageTests would notice, and only for the reason-codes article.
    private static readonly (string Fragment, string Because)[] BindingRemovedContract =
    [
        ("bindingRemoved",
            "the field this article branches state on; stripping it again would silently revert to the 'always keep' behavior that violated AC-2"),
        ("drop-request-target-missing",
            "the cross-reference from the OWNER article to the code that fires when bindingRemoved is true; losing it here decouples the two halves of the contract again")
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

    // One source element, several operations. The article used to hardcode the case — "when the template
    // provides List/ListItem you get TWO operations" — which was true, and useless for the next type whose
    // rule declares a merge onto a template-provided sub-element. The general rule is what an agent needs;
    // the list row is the worked example under LIST ROW.
    private static readonly (string Fragment, string Because)[] ListRowClauses =
    [
        ("SEVERAL operations for ONE source element",
            "the general rule — a conversion rule may declare merges onto template-provided sub-elements beside the element's own operation, for any type"),
        ("Paste ALL of them, in order",
            "the directive: an agent that pastes only the operation it recognises drops the sub-element's payload silently"),
        ("never invent one",
            "the converter emits nothing on purpose when the template provides no such sub-element (or more than one) — an invented merge onto a guessed name is the failure this forbids"),
        ("NEVER put itemLayout inside a merge of the parent List",
            "the worked example's mechanics: crt.List is not a container and itemLayout is an input, so addressing it as a child slot fails the WHOLE schema build")
    ];

    [Test]
    [Description("The article states the general rule — a conversion rule may produce several operations for one source element, and every one of them is pasted, none invented — and keeps the list row as the worked example with its build-breaking prohibition intact. The previous wording hardcoded List/ListItem into the rule itself, which said nothing about the next type whose rule declares a merge onto a template-provided sub-element.")]
    public void Guide_ShouldStateTheSeveralOperationsRule_TypeAgnostically_WithTheListRowAsExample()
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
    [Description("resolvedCandidateSchemaName is documented, resolvedSourceType/recommendedAction are documented as NOT existing on the wire (the negation sentence itself, not just the bare identifiers), and requestConversions is counted as FIVE collections including missingTargetPages. PR #174's review found the article silent on clio #1562's new field and still saying FOUR; this pin is what that review itself was missing.")]
    public void Guide_ShouldDocumentTheClio1562UnresolvedTargetRequestFields()
    {
        AssertAllPresent(UnresolvedTargetRequestFields, caseSensitive: true);
    }

    [Test]
    [Description("resolvedSourceType/recommendedAction must never be documented as existing fields that read null. PR #174's second review found the sibling presence-only pin structurally unable to catch this — it passes on the bare identifier regardless of which sentence surrounds it — so this asserts the forbidden phrasings directly.")]
    public void Guide_ShouldNeverDocumentResolvedSourceTypeOrRecommendedActionAsExistingFields()
    {
        string guide = Normalize(ReadGuide(OwnerGuide));

        foreach (string forbidden in ResolvedSourceTypeForbiddenPhrasings)
        {
            guide.Should().NotContain(forbidden,
                because: "clio #1562 deleted both fields outright; documenting either as present and null "
                    + "reintroduces the always-null RESERVED-field claim PR #174's review rejected");
        }
    }

    [Test]
    [Description("bindingRemoved and its drop-request-target-missing cross-reference survive in the OWNER article. Commit 6cab799 stripped both on the premise the converter never removes this binding; commit 268c2d5 reinstated them once review showed that broke AC-2. A future edit reverting to the 'always keep' premise must fail here, not only in MobileDropReasonCodeCoverageTests.")]
    public void Guide_ShouldKeepTheBindingRemovedContract()
    {
        AssertAllPresent(BindingRemovedContract, caseSensitive: true);
    }

    [Test]
    [Description("existingMobilePages is documented as its own field, and missingTargetPages is documented as covering entity-default-mobile-page targets (with the same-schema collapse rule), not only web-page ones. clio #1562 added both server-side as the companion to toolkit #183; nothing pinned them, so the same silent drift that hit resolvedCandidateSchemaName before PR #174 could recur unnoticed.")]
    public void Guide_ShouldDocumentExistingMobilePagesAndTheWidenedMissingTargetPages()
    {
        AssertAllPresent(ExistingMobilePagesFields, caseSensitive: true);
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

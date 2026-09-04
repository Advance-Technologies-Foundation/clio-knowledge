using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the load-bearing claims of the Change access rights guidance (ENG-92717), the way
/// <see cref="ProcessScriptTaskGuidanceTests"/> pins the ScriptTask rules.
///
/// Every claim here was verified against the server implementation and a live stand run, and each
/// NotContain guards wording a review caught before it shipped.
///
/// Two things this set exists to keep straight, because both shipped backwards once. FIRST, the two
/// filter states are OPPOSITES: a record filter that is ABSENT is the WIDENING state -- the runtime
/// never enters its filter branch, so the query runs unfiltered with record permissions disabled and
/// the change lands on every row -- while a filter that is PRESENT but carries no conditions is the
/// INERT one, and a current package refuses that at build. Reading absence as inert is what makes an
/// agent omit the filter deliberately. SECOND, the build-time refusals are not the only feedback, but
/// neither are they absent: of the three no-op causes only both-collections-empty builds green
/// unrefused. On an access-control element with no output parameters, getting either wrong licenses
/// "the build succeeded, so the rights changed" -- the one conclusion this guidance exists to
/// prevent. The negative assertions sweep every DECLARED process article rather than a named file,
/// because ownership of this narrative has already moved once.
/// </summary>
[TestFixture]
public sealed class ChangeAccessRightsGuidanceTests
{
    private const string OwningArticle = "guidance/mcp/guides/processes/access-rights.md";
    private const string EntryArticle = "guidance/mcp/guides/processes/process-modeling.md";
    private const string FilterArticle = "guidance/mcp/guides/processes/data-elements.md";

    [Test]
    [Description("The owning article publishes the payload shape an agent cannot build the element without.")]
    public void OwningArticle_ShouldPublishTheBlockShapeAndItsGranteeDiscriminator()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, OwningArticle);

        // Assert
        guide.Should().Contain("\"accessRights\": {",
            because: "prose naming the block is not a shape; an agent needs the literal JSON to write one");
        guide.Should().Contain("`grantee` is an OBJECT, not a string.",
            because: "the grantee carries a required 'type' discriminator, and guidance that reads as a "
                + "scalar enum produces a build failure on the first attempt");
        guide.Should().Contain("\"type\": \"selectedEmployees\"",
            because: "each grantee kind must show which sibling key carries its payload");
        guide.Should().Contain("A `level` on a REMOVE entry is REFUSED, not ignored",
            because: "a silently dropped level would change what the caller believes was granted");
        guide.Should().Contain("evaluates this filter with record permissions",
            because: "the selected-employees filter matches every contact it describes regardless of who "
                + "runs the process -- a security-relevant surprise that must be stated where it is used");
        guide.Should().Contain("writable on a **`remove` entry only**",
            because: "decode-only left every shipped approval process carrying this grantee uneditable "
                + "through the API - describe returns the entry, a supplied collection REPLACES the stored "
                + "one, so the read-back could not be sent back");
        guide.Should().Contain("NOT interchangeable with a `role` entry",
            because: "the two run different platform operations - a role entry on a remove drops one "
                + "role's row and leaves individual grants standing, so it reports success and does NOT "
                + "lock the record, which is the whole point of the approval shape");
        guide.Should().Contain("UPDATES that row down to Deny",
            because: "restrict's real worst case is DESTRUCTIVE, not accidental widening: it downgrades a "
                + "grant the record already had and denies the two operations the caller never named. An "
                + "article warning only about accidental granting gates the wrong thing");
        guide.Should().Contain("never build a replacement from a described element unless EVERY entry decoded",
            because: "a supplied add/remove REPLACES the collection while describe reads back lossily, so this "
                + "sentence is the only thing standing between a routine read-modify-write and silently deleted "
                + "permission entries");
        guide.Should().Contain("fails the WHOLE batch at build",
            because: "writing back a selected-employees entry whose filter did not decode no longer widens the "
                + "grant silently -- the applier refuses a conditionless grantee filter, so the round trip is a "
                + "hard failure instead. The agent must be prepared for the batch to fail rather than for a "
                + "scoped grant to quietly become unscoped");
        guide.Should().Contain("at least in these cases",
            because: "the silent-no-op list is an OPEN set -- a fourth cause (a package too old to "
                + "understand the block) produces the same symptom, and a closed 'in each of these cases' "
                + "told agents the enumeration was exhaustive");
        guide.Should().Contain("A FOURTH cause produces the same symptom",
            because: "the package-age discard is invisible at build time and is the one cause clio can "
                + "detect, so it has to be named where the other three are");
        guide.Should().Contain("VERSION BOUNDARIES - there are TWO",
            because: "an agent has to answer two DIFFERENT questions - whether the ENVIRONMENT's deployed package "
                + "lands the block at all, and whether the CLIO it is running would tell it if not. A single "
                + "boundary conflated them, so an agent could satisfy itself on the wrong one and read the absence "
                + "of a warning as proof the revoke landed");
        guide.Should().Contain("<TBD-CLIO-VERSION>",
            because: "both boundaries are placeheld until the release carrying this feature exists, and the marker "
                + "is deliberately greppable so it cannot ship unnoticed. If this ever fails, check that a REAL "
                + "version replaced it rather than the sentence being quietly deleted");
        guide.Should().Contain("MUST, before you apply any of this to a live environment",
            because: "CONTRIBUTING requires a removal-class instruction to carry its preconditions inline, "
                + "and setElement can revoke permissions people currently rely on");
        guide.Should().Contain("Use a `remove` entry to take access away",
            because: "remove is the verified mechanism and restrict is the destructive one, so the article "
                + "must name the safe path explicitly rather than leaving restrict as the obvious way to "
                + "block");
        guide.Should().Contain("also denies edit and delete for that grantee",
            because: "a fresh insert writes one row per operation -- the named one at the caller's level and "
                + "the other two at Deny -- so restrict reaches operations the caller never mentioned. This "
                + "replaced a pin asserting the opposite direction (that restrict accidentally GRANTS), which "
                + "was the inverted model");
        guide.Should().Contain("a conditionless filter here is REFUSED at build",
            because: "the applier rejects a selectedEmployees filter with no conditions rather than storing "
                + "it, so an agent must not be told the state is merely hazardous -- it is unreachable, and "
                + "an article that describes it as storable sends the agent into a refusal documented nowhere");
        guide.Should().Contain("applies the change to EVERY record of the target object",
            because: "verified against ChangeAdminRightsUserTask.InternalExecute: an ABSENT record filter "
                + "never enters the filter block, so the ESQ runs unfiltered. This is the element's widest "
                + "state and it is neither refused nor warned, so the article naming it is the only thing "
                + "standing between an agent omitting the filter to stage an element and a mass grant");
        guide.Should().Contain("the runtime changes nothing (silent no-op)",
            because: "the mirror fact: a record filter PRESENT with no conditions is the silent no-op, not "
                + "the fail-open case. The two states were documented backwards across three repos, and "
                + "pinning both directions is what stops the inversion coming back");
        guide.Should().Contain("DELETES the matching record-right rows; it does not deny",
            because: "record permissions are additive, so a successful revoke can still leave the operation "
                + "in place through another role -- an agent that reads remove as 'deny' ships a false negative");
    }


    /// <summary>
    /// Whitespace-normalised, regex-family matching - the technique
    /// <see cref="RecordFilterDirectionSweepTests"/> proves out, applied to the named guards.
    /// <para>These were byte-exact NotContain assertions. On prose rewritten four times in three days that
    /// combination fails the wrong way twice over: a harmless reflow or copy-edit turns the build red, while
    /// the same false claim reworded slips through green. Each pattern below names the CLAIM rather than one
    /// spelling of it, and the match is reported so a failure says what it actually found.</para>
    /// </summary>
    private static void ShouldNotClaim(string text, string pattern, string subject, string because)
    {
        Match match = Regex.Match(Regex.Replace(text, @"\s+", " "), pattern,
            RegexOptions.IgnoreCase);
        match.Success.Should().BeFalse(
            because: $"{subject}: {because}"
                + (match.Success ? $" - found: '{match.Value}'" : string.Empty));
    }

    [Test]
    [Description("The manifest DESCRIPTIONS are swept for the filter-state inversion too, not just the article bodies. A description is what an MCP client shows an agent choosing which guide to read, so it is the first thing read about this element and the last thing anyone edits carefully. Every other pin in this fixture reads a .md; the description is the one surface none of them covered, and it is the one that shipped the inversion.")]
    public void ManifestDescriptions_ShouldNotStateTheFilterStatesBackwards()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string[] descriptions = [.. manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.TryGetProperty("description", out JsonElement value)
                ? value.GetString() ?? string.Empty
                : string.Empty)];

        // Assert
        descriptions.Should().NotBeEmpty(because: "the manifest declares described resources");
        foreach (string description in descriptions)
        {
            ShouldNotClaim(description,
                @"no-?ops?[^.]{0,80}?not\s+refused[^.]{0,60}?no\s+record\s+filter", "manifest description",
                because: "this exact phrasing shipped and called an ABSENT record filter a silent no-op; it is the widening state, and an agent reading the description decides on that basis whether to open the "
                    + "article at all");
            ShouldNotClaim(description,
                @"opposite\s+hazard[^.]{0,60}?(no\s+conditions|conditionless)", "manifest description",
                because: "a conditionless filter is the INERT state and is refused at build, so calling it the opposite hazard inverts both halves at once");
        }
    }

    [Test]
    [Description("The three no-op causes are named with the RIGHT ones marked refused: cases 1 and 3 are refused at build time and only case 2 builds green. The article once claimed one refusal while its own list named two, and the entry article has to state the absent-filter hazard in the widening direction.")]
    public void Guidance_ShouldNameTheNoOpCauses_AndMarkTheRefusedOnesCorrectly()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string owning = ProcessGuideSet.Read(repositoryRoot, OwningArticle);
        string entry = ProcessGuideSet.Read(repositoryRoot, EntryArticle);

        // Assert
        owning.Should().Contain("Cases 1 and 3 are refused at build time",
            because: "TWO of the three no-ops are enforced, and the article previously claimed one while its "
                + "own refused-at-build list named both - a contradiction this pin actively held in place, "
                + "since correcting the prose turned the test red and pushed the next contributor back to the "
                + "wrong claim");
        ShouldNotClaim(owning, @"only\s+case\s+3\s+is\s+refused", "the owning article",
            because: "the superseded scoping must not come back; it is the half that made the article "
                + "disagree with itself");
        owning.Should().Contain("Clearing one is safe only while the OTHER still holds an entry",
            because: "`[]` is the documented clearing idiom, so the both-empty hazard has to be attached "
                + "at the point of use rather than left to a distant paragraph");
        entry.Should().Contain("a record filter that is ABSENT is the opposite hazard and acts on every record",
            because: "the entry article is read first and must name the states the way the runtime behaves, "
                + "not the way the phrase \"empty filter\" suggests -- reading absence as inert is what makes "
                + "an agent omit the filter deliberately");
        entry.Should().Contain("so a clean build does NOT mean the element will do anything",
            because: "this is the inverted, correct form of the 'build-time refusals are the only feedback' "
                + "overclaim that a review caught");

        foreach (ProcessGuideSet.Article article in ProcessGuideSet.Declared(repositoryRoot))
        {
            string text = ProcessGuideSet.Read(repositoryRoot, article.SourcePath);
            ShouldNotClaim(text, @"fails\s+silently\s*\(\s*empty\s+filter", article.ItemId,
                because: $"{article.ItemId} would pair both hazards with a build-time refusal that exists "
                    + "only for the non-administrated object -- round-1 review Blocker. (Scoped to this "
                    + "phrasing on purpose: \"empty filter\" is legitimate in process-data-elements, where "
                    + "readData/changeData do not have this element's inverted states.)");
            ShouldNotClaim(text, @"only\s+feedback\s+that\s+will\s+ever\s+exist", article.ItemId,
                because: $"{article.ItemId} would tell an agent a green build proves an effective element, "
                    + "while two unrefused silent no-ops exist -- round-2 review Major");
        }
    }

    [Test]
    [Description("The element is routed, indexed and reachable, and the filter owner points at its article.")]
    public void OwningArticle_ShouldBeRoutedAndIndexed_AndTheFilterOwnerShouldPointAtIt()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string routing = ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/routing.md");
        string owning = ProcessGuideSet.Read(repositoryRoot, OwningArticle);
        string entry = ProcessGuideSet.Read(repositoryRoot, EntryArticle);
        string filters = ProcessGuideSet.Read(repositoryRoot, FilterArticle);

        // Assert
        routing.Should().Contain("name=process-access-rights",
            because: "routing is the map an agent reads before choosing a guide, so an unrouted article "
                + "is unreachable however correct it is");
        owning.Should().Contain("Part of the process guide set.",
            because: "set membership is declared by this literal token, and the entry-index guard binds "
                + "only to declared articles carrying it");
        entry.Should().Contain("`process-access-rights`",
            because: "the entry article keeps only a pointer, so the pointer is the whole route to the shape");
        filters.Should().Contain("Defaults to the signal entity on a signalStart ONLY",
            because: "the unscoped default was the round-9 defect: it is wrong for readData, changeData "
                + "and changeAccessRights alike, since DataNodeFilterTarget refuses a filter with no object");
        filters.Should().Contain("it is REQUIRED",
            because: "reverting the correction would resume telling agents to omit the object and walk "
                + "them into a build refusal on every data element");
        filters.Should().Contain("and on a `changeAccessRights` element (which records get or lose",
            because: "the record filter is owned there; an enumeration that omits the element tells agents "
                + "its filter is unsupported, producing the silent no-op the guidance warns about");
    }
}

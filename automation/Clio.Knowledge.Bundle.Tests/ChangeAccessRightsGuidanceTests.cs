using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the load-bearing claims of the Change access rights guidance (ENG-92717), the way
/// <see cref="ProcessScriptTaskGuidanceTests"/> pins the ScriptTask rules.
///
/// Every claim here was verified against the server implementation and a live stand run, and each
/// NotContain guards wording a review caught before it shipped. Two overclaims were removed and must
/// not come back anywhere in the set: that the server refuses an EMPTY RECORD FILTER at build time (it
/// never does -- only a non-administrated object is refused), and that the build-time refusals are
/// therefore the only feedback that will ever exist (they are not: NO record filter and
/// both-collections-empty each build green and then do nothing). On an access-control element with no
/// output parameters, either wording licenses "the build succeeded, so the rights changed" -- the one
/// conclusion this guidance exists to prevent. The negative assertions sweep every DECLARED process
/// article rather than a named file, because ownership of this narrative has already moved once.
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
        guide.Should().Contain("The legacy `allRolesAndUsers` grantee is DESCRIBE-ONLY",
            because: "it decodes but is refused on write, so agents must model it as explicit role entries");
        guide.Should().Contain("Confirm on your stand that it actually blocks the",
            because: "`restrict` is prescribed as the way to BLOCK access but is enum-derived and unobserved; "
                + "separating the recommendation from that hedge is what a review caught, and a size trim is "
                + "exactly what would reunite them");
        guide.Should().Contain("never build a replacement from a described element unless EVERY entry decoded",
            because: "a supplied add/remove REPLACES the collection while describe reads back lossily, so this "
                + "sentence is the only thing standing between a routine read-modify-write and silently deleted "
                + "permission entries");
        guide.Should().Contain("empty grantee filter matches every contact",
            because: "it names the widening consequence of writing back a selected-employees entry whose filter "
                + "did not decode -- a scoped grant becomes an unscoped one");
        guide.Should().Contain("at least in these cases",
            because: "the silent-no-op list is an OPEN set -- a fourth cause (a package too old to "
                + "understand the block) produces the same symptom, and a closed 'in each of these cases' "
                + "told agents the enumeration was exhaustive");
        guide.Should().Contain("A FOURTH cause produces the same symptom",
            because: "the package-age discard is invisible at build time and is the one cause clio can "
                + "detect, so it has to be named where the other three are");
        guide.Should().Contain("VERSION BOUNDARY",
            because: "the clio read-back warning ships WITH this release; stating it unconditionally let an "
                + "agent on an older clio read the ABSENCE of a warning as proof the revoke landed");
        guide.Should().Contain("MUST, before you apply any of this to a live environment",
            because: "CONTRIBUTING requires a removal-class instruction to carry its preconditions inline, "
                + "and setElement can revoke permissions people currently rely on");
        guide.Should().Contain("is NOT a verified substitute for removal",
            because: "positioning an enum-derived, unobserved level as a replacement for the one verified "
                + "mechanism is how an unenforced deny gets shipped as an access control");
        guide.Should().Contain("a `restrict` entry lives in `add`, the GRANT collection",
            because: "an unverified level in the grant collection fails in the OPPOSITE direction -- if "
                + "the runtime does not treat it as a deny, the entry ADDS access for the grantee the "
                + "caller meant to block");
        guide.Should().Contain("a conditionless filter here is REFUSED at build",
            because: "the applier rejects a selectedEmployees filter with no conditions rather than storing "
                + "it, so an agent must not be told the state is merely hazardous -- it is unreachable, and "
                + "an article that describes it as storable sends the agent into a refusal documented nowhere");
        guide.Should().Contain("builds green and DOES fail open",
            because: "the element's own record filter is NOT symmetrical with the grantee filter: one "
                + "carrying an object but no conditions is unrefused and still changes permissions on every "
                + "record of the target object. Refusing it was raised in review and rejected as outside the "
                + "story's acceptance criteria, so the article must keep stating the hazard plainly");
        guide.Should().Contain("DELETES the matching record-right rows; it does not deny",
            because: "record permissions are additive, so a successful revoke can still leave the operation "
                + "in place through another role -- an agent that reads remove as 'deny' ships a false negative");
    }

    [Test]
    [Description("Both silent runtime no-ops are named, and no declared article claims they are refused.")]
    public void Guidance_ShouldNameBothSilentNoOps_AndNoArticleShouldClaimTheyAreRefused()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string owning = ProcessGuideSet.Read(repositoryRoot, OwningArticle);
        string entry = ProcessGuideSet.Read(repositoryRoot, EntryArticle);

        // Assert
        owning.Should().Contain("Only case 3 is refused at build time.",
            because: "exactly one of the three no-ops is enforced; scoping it wrong is what shipped as a "
                + "review Blocker the first time");
        owning.Should().Contain("Clearing one is safe only while the OTHER still holds an entry",
            because: "`[]` is the documented clearing idiom, so the both-empty hazard has to be attached "
                + "at the point of use rather than left to a distant paragraph");
        entry.Should().Contain("silently does nothing with NO record filter at all OR with both collections empty",
            because: "the entry article is read first and must name the states as the owner does -- \"empty filter\" now reads as the FAIL-OPEN conditionless case, whose blast radius is the opposite");
        entry.Should().Contain("so a clean build does NOT mean the element will do anything",
            because: "this is the inverted, correct form of the 'build-time refusals are the only feedback' "
                + "overclaim that a review caught");

        foreach (ProcessGuideSet.Article article in ProcessGuideSet.Declared(repositoryRoot))
        {
            string text = ProcessGuideSet.Read(repositoryRoot, article.SourcePath);
            text.Should().NotContain("fails silently (empty filter, non-administrated object)",
                because: $"{article.ItemId} would pair both hazards with a build-time refusal that exists "
                    + "only for the non-administrated object -- round-1 review Blocker");
            text.Should().NotContain("the only feedback that will ever exist",
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

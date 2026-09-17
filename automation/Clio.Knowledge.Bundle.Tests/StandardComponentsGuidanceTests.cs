using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the reason ENG-94756 exists. Two things decide whether a record feed or an attachments list
/// comes out matching the platform, and <c>get-component-info</c> carries neither: what the page's PARENT
/// TEMPLATE already ships, which decides <c>merge</c> against <c>insert</c>; and, for the attachments
/// list, WHICH of two real shapes the record-page case wants — the catalog's own worked example builds a
/// list over a per-entity file entity (<c>ContactFile</c>) as a <c>list</c>, while the platform's creation
/// flow produces <c>SysFile</c> / <c>RecordId</c> / <c>gallery</c> with a companion <c>AttachmentListDS</c>
/// and an empty handlers array. Neither mistake is rejected at save time, because <c>update-page</c>
/// validates the diff it is sent rather than the merged result.
///
/// For <c>crt.Feed</c> the catalog's documentation DOES already give the record-feed combination — the
/// measurement confirms it rather than filling a gap, and the fixture says so, because claiming a gap
/// that is not there is the duplication CONTRIBUTING asks contributors to rule out.
///
/// So the VALUES are the deliverable, and they are measured rather than derived — lab scenario of
/// 2026-09-11 on an internal Creatio Studio stand, read through <c>get-page</c>: the creation-flow page
/// <c>UsrSourceCodes_FormPage</c>, its parent <c>PageWithTabsFreedomTemplate</c> (the MERGE path), and
/// <c>PageWithTopAreaAndTabsFreedomTemplate</c>, which ships neither component (the INSERT path). The
/// stand ALIAS is deliberately not asserted here: this is a public repository, the alias means nothing to
/// that audience, and pinning it in a test is what makes generalising the article break the suite.
///
/// CORRECTION, 2026-09-16, and the reason
/// <see cref="StandardComponentsGuidance_ShouldNotPrescribeASingleUnconditionalAttachmentsShape"/> now
/// exists: those three schemas shared a blind spot. All of them belong to objects with no per-object file
/// entity, so the article read one shape as THE shape and its table prescribed
/// <c>recordColumnName: "RecordId"</c> over <c>SysFile</c> unconditionally. A live migration of a Classic
/// section produced <c>recordColumnName: "UsrToMigrate"</c> over <c>UsrToMigrateFile</c>, and was RIGHT
/// to: on that stand the record's attachment is a row in the per-object table and <c>SysFile</c> holds
/// none for that object. An agent obeying the table would have pointed the gallery at a table with no
/// rows for the record — and <c>update-page</c> would have reported success. The article already
/// mentioned both shapes in prose; the prescriptive table did not branch, and the table is what gets
/// copied. That is the failure this fixture now guards on both sides: the branch must stay decidable, and
/// NEITHER absolute may come back.
///
/// This fixture keeps that measured set, the merge-vs-insert rule and the routing that reaches them from
/// drifting back out; a canonical value silently edited to a plausible-looking one would otherwise ship
/// green and reintroduce the defect verbatim.
///
/// SCOPE, after the split: the INSERT-path deliverables (the tab toolbar above all) and everything about
/// tags moved to <see cref="TemplateSuppliedPartsGuidanceTests"/> when the article outgrew one
/// get-guidance response — a routing problem before a size one, since an agent needing only tags had to
/// load the whole Feed/Attachments article to reach them. No assertion was dropped in the move, and
/// <c>ShouldHandTheInsertPathToTheOwningGuide</c> guards the seam in both directions: this article must
/// point at the other one, and must not grow its own copy of what moved.
/// </summary>
[TestFixture]
public sealed class StandardComponentsGuidanceTests
{
    private const string GuideRelativePath =
        "guidance/mcp/guides/pages/modification/standard-components.md";

    [Test]
    [Description("Keeps the measured crt.Feed value set intact — the measurement that confirms the catalog's record-feed combination.")]
    public void StandardComponentsGuidance_ShouldCarryTheMeasuredFeedValueSet()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("\"feedType\": \"Record\"",
                because: "the record-scoped feed is the case this guide governs")
            .And.Contain("\"primaryColumnValue\": \"$Id\"",
                because: "the feed binds to the open record through the page Id attribute")
            .And.Contain("\"cardState\": \"$CardState\"",
                because: "the record-page template supplies the card state the feed reads")
            .And.Contain("\"dataSourceName\": \"PDS\"",
                because: "the feed reads the page primary data source");
        guidance.Should().Contain("MUST equal the `entitySchemaName` of the page's primary data source",
            because: "entitySchemaName is the one object-specific value here, and copying a literal from "
                + "another page is the mistake worth naming");
    }

    [Test]
    [Description("Keeps the measured crt.FileList value set and its companion data source intact.")]
    public void StandardComponentsGuidance_ShouldCarryTheMeasuredAttachmentListValueSet()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("\"masterRecordColumnValue\": \"$Id\"")
            .And.Contain("\"items\": \"$AttachmentList\"")
            .And.Contain("\"primaryColumnName\": \"AttachmentListDS_Id\"")
            .And.Contain("\"code\": \"AttachmentListDS_Name\"")
            .And.Contain("\"dataValueType\": 28")
            .And.Contain("\"viewType\": \"gallery\"")
            .And.Contain("\"tileSize\": \"small\"");
    }

    [Test]
    [Description("Keeps BOTH attachments shapes present and keeps the guide from prescribing either one unconditionally — the defect a live migration run exposed.")]
    public void StandardComponentsGuidance_ShouldNotPrescribeASingleUnconditionalAttachmentsShape()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert — both value sets survive, complete, with the pair that belongs to each.
        guidance.Should().Contain("\"recordColumnName\": \"RecordId\"",
                because: "the shared-table shape is real and is what the Freedom creation flow produces")
            .And.Contain("\"recordColumnName\": \"UsrToMigrate\"",
                because: "the per-object shape is equally real and is what a migrated Classic section "
                    + "produced; replacing one absolute with the other was the failure to avoid here")
            .And.Contain("\"entitySchemaName\": \"UsrToMigrateFile\"",
                because: "recordColumnName and the data source entity are ONE decision, so the "
                    + "per-object branch has to carry both halves or it teaches the mismatch it warns "
                    + "about");

        // Assert — the branch is decidable, not left to the reader's judgement. A table that names a
        // shape with no test to apply is what the old prose-near-the-top version already was, and an
        // agent read straight past it to the prescriptive table.
        guidance.Should().Contain("`parent-schema-name` is `File`",
                because: "the existence of an <Entity>File inheriting File is the stated signal, and a "
                    + "rule an agent cannot evaluate against a live environment is not a rule")
            .And.Contain("`find-entity-schema`",
                because: "the decision is made against the target environment rather than from memory")
            .And.Contain("READ `recordColumnName` rather than spelling it",
                because: "the per-object column is conventionally the object's own name, and a "
                    + "convention asserted as a contract is the same class of defect one level down");

        // Assert — the absolute is gone from the places an agent actually copies from.
        guidance.Should().Contain("| `recordColumnName` | OBJECT-SPECIFIC",
                because: "the property table is the thing an agent copies from, and it said `RecordId` "
                    + "flatly. Naming the cell OBJECT-SPECIFIC is the correction; a bare NotContain on "
                    + "the old row would pass again the moment the comparison table above it is the "
                    + "only place the literal survives")
            .And.NotContain("The `SysFile` column pointing back at the master record.",
                because: "that note framed SysFile as THE file entity rather than one of two, and it is "
                    + "the exact sentence the table's authority rested on");
    }

    [Test]
    [Description("Keeps the companion data source stated as mandatory — omitting it is the silent half of the defect.")]
    public void StandardComponentsGuidance_ShouldRequireTheCompanionDataSource()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("You MUST declare `AttachmentListDS` under",
                because: "the list has no entity of its own; without the data source it renders empty")
            .And.Contain("\"entitySchemaName\": \"SysFile\"",
                because: "the shared-table entity stays quoted in full on the insert path — the "
                    + "correction branches the entity, it does not delete the shape that was right")
            .And.Contain("\"scope\": \"viewElement\"",
                because: "the companion data source is view-element scoped, not the page data source");
        guidance.Should().Contain("MERGE path: an overlay on the declaration the template already carries",
                because: "the one-attribute block is what the measured page carries ON TOP of the "
                    + "template's declaration; presented as the whole data source it under-specifies "
                    + "every insert that copies it")
            .And.Contain("INSERT path: the full declaration",
                because: "the insert path has no template declaration underneath, so the full "
                    + "four-attribute form is the deliverable there")
            .And.Contain("\"CreatedOn\": { \"path\": \"CreatedOn\" }")
            .And.Contain("\"CreatedBy\": { \"path\": \"CreatedBy\" }")
            .And.Contain("\"path\": \"Size\"");
    }

    [Test]
    [Description("Keeps the unobserved consequences labelled as reasoning rather than asserted as behaviour.")]
    public void StandardComponentsGuidance_ShouldNotAssertWhatItDidNotObserve()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("NOT OBSERVED",
                because: "the duplicate-insert and missing-data-source consequences are reasoned from "
                    + "structure; AGENTS.md forbids presenting them as verified behaviour")
            .And.NotContain("renders the component twice",
                because: "nobody watched a duplicated insert render, so the guide may name the hazard "
                    + "but not the outcome");
    }

    [Test]
    [Description("Keeps the merge-vs-insert rule, which is what makes a duplicate component impossible to ship by accident.")]
    public void StandardComponentsGuidance_ShouldOwnTheMergeVersusInsertRule()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("You MUST use `\"operation\": \"merge\"` when the parent template already ships",
                because: "the creation flow merges onto template-shipped containers rather than inserting")
            .And.Contain("You MUST NOT emit an `insert` for a component the template already ships",
                because: "the platform does not correct it and update-page still reports success")
            .And.Contain("FeedTabContainer")
            .And.Contain("AttachmentsTabContainer");
    }

    [Test]
    [Description("Keeps the INSERT path handed off rather than half-answered here, and keeps the seam clean: the template-supplied parts live in exactly one guide.")]
    public void StandardComponentsGuidance_ShouldHandTheInsertPathToTheOwningGuide()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("`page-modification-template-supplied-parts`",
            because: "the value sets below are not the whole deliverable on the insert path, and an "
                + "agent told to insert without being told where the rest is will ship a gallery with "
                + "no upload button");
        guidance.Should().NotContain("AttachmentAddButton",
                because: "the toolbar block belongs to exactly one guide; a copy here is the "
                    + "duplication the split was supposed to end, and copies drift apart")
            .And.NotContain("crt.TagSelectPropertiesPanel",
                because: "tags are owned end to end by the other guide — this one may point at it, "
                    + "never restate it");
    }

    [Test]
    [Description("Keeps the catalog divergence stated: get-component-info's own crt.FileList example is a DIFFERENT case, and an agent that follows it on a record page drifts from the platform shape.")]
    public void StandardComponentsGuidance_ShouldNameTheDivergenceFromTheComponentCatalogExample()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("ContactFile",
                because: "naming the catalog's own entity is what makes the divergence checkable rather "
                    + "than a claim the reader has to take on trust")
            .And.Contain("Both shapes are real",
                because: "the catalog example is a different case, not a defect, and saying otherwise "
                    + "would set this guide against an authoritative source it does not own")
            .And.Contain("the catalog example is NOT a different case",
                because: "the divergence is presentation and wiring ONLY. On the file entity the "
                    + "catalog's ContactFile / \"Contact\" pair is correct for the objects that have "
                    + "one, and this guide was the side over-generalising — a correction that runs "
                    + "against the article's own earlier framing has to be pinned, or it reverts to the "
                    + "tidier wrong story")
            .And.Contain("the catalog's `documentation` already gives the record-feed combination",
                because: "crt.Feed is NOT a gap — claiming one where the catalog already answers is the "
                    + "duplication CONTRIBUTING asks contributors to rule out first");
    }

    [Test]
    [Description("Keeps the evidence basis attached, as CONTRIBUTING requires for behavioural guidance.")]
    public void StandardComponentsGuidance_ShouldCarryItsEvidenceBasis()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("UsrSourceCodes_FormPage",
                because: "a prescriptive value set must name the schema it was measured from — the "
                    + "SCHEMA, not the stand alias it happened to be read on, which is meaningless to "
                    + "the readers of a public repository and pins an internal name into the suite")
            .And.Contain("PageWithTabsFreedomTemplate",
                because: "the merge-vs-insert rule is a property of the parent template, so the template "
                    + "the MERGE path was measured on has to be named")
            .And.Contain("PageWithTopAreaAndTabsFreedomTemplate",
                because: "the INSERT-path claims are measured on a template shipping neither component; "
                    + "without naming it, STEP 5 reads as inference again")
            .And.Contain("UsrToMigrateFreedom_FormPage",
                because: "the per-object branch is measured on a real migration run, and a branch whose "
                    + "evidence is not named is the kind of claim this suite exists to stop")
            .And.Contain("`SysFile` holds 2,681 rows",
                because: "the row counts are what make the two shapes DISJOINT rather than "
                    + "interchangeable, and a figure dropped from the evidence turns the branch back "
                    + "into a preference");
        guidance.Should().NotContain("TBD",
            because: "a published body must not ship an unresolved version boundary");
    }

    [Test]
    [Description("Keeps the guide reachable: the page-modification router must send a feed/attachments edit to it.")]
    public void PageModificationRouter_ShouldRouteFeedAndAttachmentsEditsToTheOwningGuide()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string router = File.ReadAllText(Path.Combine(repositoryRoot, "guidance", "mcp", "guides",
            "pages", "modification", "index.md"));

        // Assert
        router.Should().Contain("`page-modification-standard-components`",
            because: "routing sends an agent to ONE sub-guide; a guide no row points at is unreachable "
                + "even though get-guidance would serve it");
    }

    [Test]
    [Description("Keeps the owning guide from restating rules other guides own, per the one-owner rule in AGENTS.md.")]
    public void StandardComponentsGuidance_ShouldCiteSiblingOwnersRatherThanRestateThem()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("`page-modification-containers`",
                because: "container selection is owned elsewhere")
            .And.Contain("`page-schema-resources`",
                because: "the caption/resource rule is owned elsewhere")
            .And.Contain("`related-list`",
                because: "the collection-attribute and entity-data-source wiring pattern is owned elsewhere")
            .And.Contain("`get-component-info`",
                because: "the property vocabulary stays with the component catalog; this guide supplies values");
    }

    private static string ReadGuide()
    {
        string repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(repositoryRoot,
            GuideRelativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

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

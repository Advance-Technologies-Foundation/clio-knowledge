using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the half of ENG-94756 that is about what the record-page TEMPLATE supplies, split out of
/// <see cref="StandardComponentsGuidanceTests"/> when the article outgrew one response.
///
/// The split is a routing decision before it is a size one. Two arrivals were being forced through one
/// article: "I am adding a feed or an attachments list, what are the values" — which is
/// <c>page-modification-standard-components</c> — and "what does the template already give me, and what
/// must I build" — which is this guide, and which is also where TAGS belong, because
/// <c>crt.TagSelect</c> is a template-supplied part whose wiring is never the page's job. An agent that
/// only needs tags had to load the whole Feed/Attachments article to reach three paragraphs.
///
/// What this fixture keeps from drifting back out: the attachments tab's <c>tools</c> toolbar, which is
/// the ONLY thing that uploads a file and the single most omittable deliverable on the insert path; the
/// five-child-attribute collection with its sorting; the <c>Id</c> / <c>CardState</c> attributes that are
/// NEVER the page's to declare (asserting otherwise once set this guidance against the component
/// catalog); and the tag rules.
///
/// THE TAG QUESTION IS NOW ANSWERED. It was left UNVERIFIED because the first stand could not settle it:
/// <c>TagInRecord</c> and <c>ContactInTag</c> both held zero rows, so no tagging behaviour was visible
/// either way. A migration run on a second stand supplied the missing case — a record whose three tags
/// live in a per-object <c>UsrToMigrateInTag</c> junction while <c>TagInRecord</c> is empty across the
/// whole table, under a template-supplied control that defaults to <c>TagInRecord</c>. So the guidance
/// turned prescriptive, and the fixture splits accordingly:
/// <see cref="TemplateSuppliedPartsGuidance_ShouldNotPrescribeCreatingATagJunction"/> keeps the ORIGINAL
/// retraction (the default path needs no per-object junction — never tell anyone to create one), and
/// <see cref="TemplateSuppliedPartsGuidance_ShouldOwnTheJunctionOverrideRule"/> keeps the NEW rule
/// (when one already exists, <c>tagInRecordSourceSchemaName</c> must name it). The two are easy to
/// collapse into each other by accident and they say opposite-looking things, which is exactly why they
/// are asserted separately — and why the new rule is still pinned as NOT OBSERVED at the browser.
///
/// Measured read-only on internal Creatio Studio stands (2026-09-11, tags 2026-09-14, migration run
/// 2026-09-16) across four merged bundles: a template that ships both components, a template that ships
/// neither, the creation-flow page built on the first, and a migrated Classic section. The stand ALIAS is
/// deliberately not asserted — this is a public repository.
/// </summary>
[TestFixture]
public sealed class TemplateSuppliedPartsGuidanceTests
{
    private const string GuideRelativePath =
        "guidance/mcp/guides/pages/modification/template-supplied-parts.md";

    [Test]
    [Description("Keeps the tab toolbar in the INSERT-path deliverables — without it an inserted gallery has no control that uploads a file.")]
    public void TemplateSuppliedPartsGuidance_ShouldNameTheTemplateSuppliedAttachmentsToolbar()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("AttachmentAddButton",
                because: "the upload control lives in the tab container's tools slot, not on the file "
                    + "list, so an insert that stops at the component ships a gallery nobody can add to")
            .And.Contain("\"request\": \"crt.UploadFileRequest\"")
            .And.Contain("\"viewElementName\": \"AttachmentList\"",
                because: "the upload request has to name the list element it uploads into")
            .And.Contain("AttachmentRefreshButton")
            .And.Contain("\"request\": \"crt.LoadDataRequest\"")
            .And.Contain("\"dataSourceName\": \"AttachmentListDS\"",
                because: "the refresh request reloads the companion data source by name");
    }

    [Test]
    [Description("Keeps the collection attribute at its measured shape — five child attributes and the sorting, not the single column the view config declares.")]
    public void TemplateSuppliedPartsGuidance_ShouldCarryTheFullCollectionAttribute()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("\"AttachmentListDS_Name\"")
            .And.Contain("\"AttachmentListDS_CreatedOn\"")
            .And.Contain("\"AttachmentListDS_CreatedBy\"")
            .And.Contain("\"AttachmentListDS_Size\"")
            .And.Contain("\"AttachmentListDS_Id\"",
                because: "primaryColumnName resolves through this child attribute, so a Name-only "
                    + "collection breaks row identity as well as the tile content")
            .And.Contain("\"columnName\": \"CreatedOn\", \"direction\": \"desc\"",
                because: "newest-first ordering comes from sortingConfig, not from crt.FileList");
    }

    [Test]
    [Description("Keeps Id and CardState on the not-yours list, which is where the component catalog puts them too.")]
    public void TemplateSuppliedPartsGuidance_ShouldNotClaimTheAttributesTheTemplateAlwaysSupplies()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("#PrimaryDataSourceName()#.Id",
                because: "both measured templates declare the Id attribute — including the one shipping "
                    + "neither component — so an insert must NOT file it as its own deliverable")
            .And.NotContain("\"Id\": { \"modelConfig\": { \"path\": \"PDS.Id\" } }",
                because: "listing that as an insert deliverable is what set this guidance against "
                    + "get-component-info, whose crt.Feed text says the platform provides $Id and "
                    + "$CardState on edit pages");
    }

    [Test]
    [Description("Keeps the crt.TagSelect case stated as the inverse of the attachments case: template-provided, preprocessor-wired, never hand-wired on the page.")]
    public void TemplateSuppliedPartsGuidance_ShouldOwnTheTagSelectShape()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("{ \"type\": \"crt.TagSelect\", \"recordId\": \"$Id\", \"name\": \"TagSelect\" }",
                because: "the measured form is three properties and nothing else; a fuller-looking "
                    + "recipe is what sends an agent into hand-wiring the preprocessor's job")
            .And.Contain("There is no merge-vs-insert decision",
                because: "both measured templates ship the component — including the one that ships "
                    + "neither the feed nor the attachments list — so the merge-vs-insert rule in "
                    + "page-modification-standard-components does not apply to tags at all")
            .And.Contain("CardToolsContainer",
                because: "the measured placement is the card tools area, not a tab container")
            .And.Contain("without `recordId` the preprocessor cannot",
                because: "the one property that must survive any edit, and it is the catalog's own "
                    + "stated pitfall rather than an observation made here");
        guidance.Should().Contain("You MUST NOT hand-wire `items`, `listItems` or the CRUD outputs",
                because: "this is the instruction that inverts the attachments rules, and an agent "
                    + "arriving from the value-set guide will otherwise generalise the wrong way")
            .And.Contain("crt.TagSelectPropertiesPanel",
                because: "naming the preprocessor is what makes the rule checkable rather than a "
                    + "prohibition the reader has to take on trust")
            .And.Contain("`tagInRecordSourceSchemaName` defaults to `\"TagInRecord\"`");
    }

    [Test]
    [Description("Keeps the RETRACTION: tagging does not require a per-object junction, and the guide must never tell anyone to create one.")]
    public void TemplateSuppliedPartsGuidance_ShouldNotPrescribeCreatingATagJunction()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("needs NO per-object junction schema",
                because: "TagInRecord keys an association by RecordId plus RecordSchemaName, so a "
                    + "missing <Entity>InTag is not by itself evidence of a broken tag control")
            .And.Contain("BaseEntityInTag",
                because: "the older per-object model has to be named for the distinction to be usable")
            .And.Contain("do NOT create an `<Entity>InTag` schema to make tagging work",
                because: "the measurement that arrived is about pointing the control at a junction that "
                    + "ALREADY exists; it is not licence to reintroduce the claim that tagging needs "
                    + "one, which is the claim this guidance was corrected for once already");
        guidance.Should().NotContain("requires a per-object junction",
            because: "the default path genuinely needs none, and the retraction outlives the new "
                + "instruction rather than being softened by it");
    }

    [Test]
    [Description("Keeps the MEASURED junction rule: when an <Entity>InTag exists, the control has to be pointed at it or the chips render empty over real data.")]
    public void TemplateSuppliedPartsGuidance_ShouldOwnTheJunctionOverrideRule()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert — the rule itself, and the shape an agent copies.
        guidance.Should().Contain("\"tagInRecordSourceSchemaName\": \"UsrToMigrateInTag\"",
                because: "the remedy is one property naming the junction, and a rule with no copyable "
                    + "form is what the previous revision already was")
            .And.Contain("leave the control exactly as the template shipped it",
                because: "the no-junction branch must stay a positive instruction, or an agent adds the "
                    + "override everywhere and points the control at a schema that does not exist");

        // Assert — it is stated as measured, with the counts that make it so, and NOT as a hypothesis.
        guidance.Should().Contain("`TagInRecord` holds 0 rows in the entire table",
                because: "the earlier revision could settle nothing because both candidate tables were "
                    + "empty; what changed is that one of them now holds the record's data and the "
                    + "other is still empty, and that asymmetry IS the finding")
            .And.Contain("`UsrToMigrateInTag` 3, all three with",
                because: "three rows for the record under test is what turns 'the control might not see "
                    + "these' into 'the control is reading an empty table while the data sits here'")
            .And.NotContain("UNVERIFIED",
                because: "the question this word guarded is now measured; leaving it in would tell an "
                    + "agent to stop investigating at exactly the point where the answer begins");

        // Assert — and the remedy is still honestly short of observed.
        guidance.Should().Contain("NOT OBSERVED",
            because: "nobody watched the override repopulate the chips. The cause is measured and the "
                + "remedy is the catalog's, and shipping the second as if it were the first is the "
                + "habit this repository keeps having to unlearn");
    }

    [Test]
    [Description("Keeps the evidence basis attached, as CONTRIBUTING requires for behavioural guidance.")]
    public void TemplateSuppliedPartsGuidance_ShouldCarryItsEvidenceBasis()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("PageWithTabsFreedomTemplate",
                because: "the template that SHIPS the parts is where the inventory is read from")
            .And.Contain("PageWithTopAreaAndTabsFreedomTemplate",
                because: "the template that ships NEITHER component is what makes the insert column "
                    + "measured rather than inferred")
            .And.Contain("UsrSourceCodes_FormPage",
                because: "the creation-flow page is what shows the page itself declaring none of it")
            .And.Contain("UsrToMigrateFreedom_FormPage",
                because: "the tag rules turned prescriptive on a live migration run, and the schema it "
                    + "was measured on has to be named for the rule to be checkable")
            .And.Contain("`duper taf`",
                because: "the tag names are observed, not reasoned, and naming them is what "
                    + "distinguishes this from the hypothesis it replaced");
        guidance.Should().NotContain("TBD",
            because: "a published body must not ship an unresolved version boundary");
    }

    [Test]
    [Description("Keeps the two guidance items independently reachable: this one has its own router row, phrased for how an agent actually arrives at it.")]
    public void PageModificationRouter_ShouldRouteTagsAndInsertPathEditsToThisGuide()
    {
        // Arrange
        string router = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "guidance", "mcp", "guides",
            "pages", "modification", "index.md"));

        // Assert
        router.Should().Contain("`page-modification-template-supplied-parts`",
                because: "a guide no row points at is unreachable even though get-guidance would serve "
                    + "it, and the whole point of the split was that tags stop requiring the "
                    + "Feed/Attachments article to be loaded first")
            .And.Contain("crt.TagSelect",
                because: "the row has to be findable from how the requirement is actually phrased — a "
                    + "tag edit, or a tag control that renders empty");
    }

    [Test]
    [Description("Keeps the pair cross-linked, so an agent that lands on the wrong half is told where the other half is.")]
    public void TemplateSuppliedPartsGuidance_ShouldPointBackAtTheValueSetGuide()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("`page-modification-standard-components`",
            because: "the value sets and the merge-vs-insert decision live there; an insert needs both "
                + "halves, and neither half should leave the reader guessing where the other is");
    }

    [Test]
    [Description("Keeps this guide from restating rules other guides own, per the one-owner rule in AGENTS.md.")]
    public void TemplateSuppliedPartsGuidance_ShouldCiteSiblingOwnersRatherThanRestateThem()
    {
        // Arrange
        string guidance = ReadGuide();

        // Assert
        guidance.Should().Contain("`page-modification-containers`",
                because: "the rule that an inserted container must initialize its content slot is "
                    + "owned there")
            .And.Contain("`page-schema-resources`",
                because: "registering the toolbar caption keys is owned there")
            .And.Contain("`related-list`",
                because: "the collection-attribute and entity-data-source wiring pattern is owned there")
            .And.Contain("`get-component-info`",
                because: "the property vocabulary stays with the component catalog");
    }

    private static string ReadGuide() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            GuideRelativePath.Replace('/', Path.DirectorySeparatorChar)));

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

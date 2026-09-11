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
/// and no handlers. Neither failure is visible at save time: an <c>insert</c> over a component the template
/// already ships returns <c>success: true</c> and renders it twice, and a missing companion data source
/// renders the list empty and never issues a query.
///
/// For <c>crt.Feed</c> the catalog's documentation DOES already give the record-feed combination — the
/// measurement confirms it rather than filling a gap, and the fixture says so, because claiming a gap
/// that is not there is the duplication CONTRIBUTING asks contributors to rule out.
///
/// So the VALUES are the deliverable, and they are measured rather than derived — lab scenario of
/// 2026-09-11 on stand <c>eng96655</c>, page <c>UsrSourceCodes_FormPage</c>, read through <c>get-page</c>.
/// This fixture keeps that measured set, the merge-vs-insert rule, and the routing that reaches them from
/// drifting back out; a canonical value silently edited to a plausible-looking one would otherwise ship
/// green and reintroduce the defect verbatim.
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
            .And.Contain("\"recordColumnName\": \"RecordId\"")
            .And.Contain("\"items\": \"$AttachmentList\"")
            .And.Contain("\"primaryColumnName\": \"AttachmentListDS_Id\"")
            .And.Contain("\"code\": \"AttachmentListDS_Name\"")
            .And.Contain("\"dataValueType\": 28")
            .And.Contain("\"viewType\": \"gallery\"")
            .And.Contain("\"tileSize\": \"small\"");
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
                because: "the attachments list reads files, and SysFile is the measured entity")
            .And.Contain("\"scope\": \"viewElement\"",
                because: "the companion data source is view-element scoped, not the page data source");
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
            .And.Contain("renders the component twice",
                because: "the consequence is what makes the rule worth obeying, and it is invisible at save time")
            .And.Contain("FeedTabContainer")
            .And.Contain("AttachmentsTabContainer");
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
        guidance.Should().Contain("eng96655",
                because: "a prescriptive value set must name the observation it was measured from")
            .And.Contain("UsrSourceCodes_FormPage")
            .And.Contain("PageWithTabsFreedomTemplate");
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

using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-files-parameter</c> guide: the Process parameter variant (the <c>files</c> group) of the
/// Process file element, split out of <c>process-files</c> when that guide reached its design target. The rest of the
/// element and the File types stay in <c>process-files</c>, pinned by <see cref="ProcessFileGuidanceTests"/> and
/// <see cref="FileParameterGuidanceTests"/>; the report group is pinned by <see cref="ProcessFileReportGuidanceTests"/>.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each clause is a rule the package enforces. The single-File arm rests on the P3 measurement, which
/// stays with its rule in <c>process-files</c> and is pinned there.
/// </remarks>
[TestFixture]
public sealed class ProcessFileParameterGuidanceTests
{
    private const string ItemId = "process-files-parameter";

    private const string ArticlePath = "guidance/mcp/guides/processes/files-parameter.md";

    private const string ParentPath = "guidance/mcp/guides/processes/files.md";

    private const string SectionHeading = "== The Process parameter variant: the `files` group ==";

    /// <summary>
    /// This guide's own design target, in the response characters of <see cref="ProcessGuideResponseSizeTests"/>. The
    /// guide is about 3,000 characters, so 5,000 leaves room for a few more clauses and fails a larger addition, which
    /// should then be reviewed rather than absorbed. The 19,000-byte inline rule is asserted separately.
    /// </summary>
    private const int DesignTargetResponseCharacters = 5_000;

    [Test]
    [Description("process-files-parameter is registered banner-less and ungated with no legacy route, as process-files-report is: published at its itemId path, required by the library, routed after process-files, and pointed to by process-files from its scope, its element section and its hand-on recipe.")]
    public void Resource_ShouldBeRegisteredBannerLessAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string[] requiredItems = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString()!)];
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string routing = Collapse(ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/routing.md"));
        string guide = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        string parent = Collapse(ProcessGuideSet.Read(repositoryRoot, ParentPath));

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
            because: "the manifest must point at the article this fixture pins");
        resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
            because: "the flat resources folder names each article by its itemId");
        resource.GetProperty("topicId").GetString().Should().Be("creatio.process-files-parameter",
            because: "the topic is the stable cross-library identity of the guide");
        resource.GetProperty("role").GetString().Should().Be("guidance",
            because: "get-guidance serves only guidance resources");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "the files group ships enabled; a gated guide would be hidden while its tool accepts the group");
        resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
            because: "the guide was born after the v0 migration and has no v0 route to preserve");
        requiredItems.Should().Contain(ItemId,
            because: "the library requires every item it publishes for the process tools");
        requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
            because: "the requirement lists the canonical route next to the item id");
        routing.Should().Contain("-> name=process-files-parameter (after name=process-files)",
            because: "an article nothing routes to is never read, and it extends process-files rather than replacing it");
        parent.Should().Contain("its `files` group: `process-files-parameter`",
            because: "an agent that opened process-files for the element must learn where the files group lives");
        parent.Should().Contain("`files` (Process parameter: `process-files-parameter` owns it)",
            because: "the element section names the owner of each group it lists");
        parent.Should().Contain("its `files` group (`process-files-parameter`)",
            because: "the hand-on recipe cites the group by the article that owns it, not by a section that left the guide");
        parent.Should().NotContain(SectionHeading,
            because: "the section moved here; a second copy in process-files would be two owners of one rule");
        guide.Should().NotContain(ProcessGuideSet.SetBanner,
            because: "a banner obliges process-modeling to index the guide, and that article has no room left under the inline rule");
    }

    [Test]
    [Description("The Process parameter section: saving only, exactly one source and the refused value, both levels for a collection, one file for a single File, an unset single File stops the process, an empty collection copies nothing, an expression goes through as given, the outputs, and the block-level describe-only bindingLevels with its issues: none means the platform refuses to start the process, a stored useInProcess or an invalid action fails at run time, and a mapped action is decided at run time.")]
    public void Section_ShouldStateTheProcessParameterGroup()
    {
        // Arrange
        string section = ReadSection();

        // Act
        string[] clauses =
        [
            "It copies files the process holds to a record's attachments, and only that: saveTo is required on create and useInProcess is refused.",
            "`files` takes exactly ONE of processParameter, sourceElement + sourceElementParameter, or expression. A value is refused (a file has no constant form)",
            "a source with no file item or with several, a parameter of the element itself, and any mapping FROM its Files.",
            "A collection (a FileCollection, or an element's ObjectFiles or ReportFiles) binds BOTH levels, Files and Files.File: one copy per row.",
            "A single File binds Files.File only (P3, `process-files`) and copies that one file.",
            "A single File must be set before the element runs: an unset one stops the process with an error.",
            "An empty collection copies nothing, silently.",
            "An expression goes onto Files.File as given, with a notice: nothing checks that it yields one file.",
            "Outputs: ObjectFiles (item ObjectFile) holds the COPIES, and CreatedObjectFileIds their ids.",
            "Describe adds the describe-only fileProcessing.bindingLevels (both, itemOnly, outerOnly or none), with an issue for a stored shape that would not resubmit as stored (none: nothing bound, the platform refuses to start the process) and for an action other than save: a stored useInProcess or an invalid one fails at run time, a mapped one is decided at run time.",
            "Remove it before resubmitting."
        ];

        // Assert
        section.Should().NotBeEmpty(because: "anti-vacuity: process-files-parameter carries the Process parameter section");
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "the variant only saves, an unset single File fails the run with an error, and an empty collection succeeds while copying nothing");
        }
        section.Should().NotContain("files.bindingLevels",
            because: "describe writes bindingLevels on the fileProcessing block, not inside the files group");
        section.Should().NotContain("a stored action other than save (it fails at run time)",
            because: "a mapped action is decided at run time and does not necessarily fail; only a stored useInProcess or an invalid action does");
    }

    [Test]
    [Description("The guide states what it owns and sends the reader to process-files first for the rest of the element, the File types and the two-level rules it applies.")]
    public void Scope_ShouldNameItsOwnershipAndTheEntryArticle()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        bool ownsTheGroup = guide.Contains("This guide owns the `files` group of the Process file element (`fileProcessing`)",
            StringComparison.Ordinal);
        bool readsTheEntryFirst = guide.Contains(
            "`process-files` owns the rest of the element (the contract gate, the `attachments` group, `action`, `saveTo`, the setElement merge, the describe-only members other than `bindingLevels`, the consumers), the File and FileCollection parameters and the two-level rules (P1-P3, R-M1-R-M3) this group applies: read it first.",
            StringComparison.Ordinal);

        // Assert
        ownsTheGroup.Should().BeTrue(
            because: "a split article states which rules it is the authoritative owner of");
        readsTheEntryFirst.Should().BeTrue(
            because: "routing sends the reader here after process-files, and the gate and the binding rules live only there");
    }

    [Test]
    [Description("The guide names no internal task and no clio release, and cites a CrtProcessBuilder version nowhere: the version boundary is owned by process-files and the catalog.")]
    public void Guide_ShouldNotCarryInternalReferencesOrReleaseNumbers()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        bool namesATask = Regex.IsMatch(guide, @"\b[A-Z][A-Z0-9]+-\d+\b|/pull/\d+|#\d+\b");
        bool namesARelease = Regex.IsMatch(guide, @"(?:clio|CrtProcessBuilder) \d+\.\d+\.\d+");

        // Assert
        namesATask.Should().BeFalse(because: "published guidance names no Jira key, pull request or issue number");
        namesARelease.Should().BeFalse(
            because: "the gate and the package floor are stated once, in process-files and the element catalog");
        guide.Should().NotContain("TBD", because: "a placeholder boundary cannot merge: the release gate refuses it");
    }

    [Test]
    [Description("The guide stays inside its own design target and inside the 19,000-byte inline reply rule.")]
    public void Guide_ShouldStayInsideItsDesignTarget()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        int responseCharacters = ProcessGuideResponseSizeTests.ResponseSize(repositoryRoot, ArticlePath);
        int replyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(ProcessGuideSet.Read(repositoryRoot, ArticlePath));
        TestContext.WriteLine($"{ItemId}: {responseCharacters:N0} response characters of {DesignTargetResponseCharacters:N0}, "
            + $"{replyBytes:N0} reply bytes of {GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes:N0}");

        // Assert
        responseCharacters.Should().BeLessThanOrEqualTo(DesignTargetResponseCharacters,
            because: "the guide was split out to give the files group room; it must not grow past a reviewed size");
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline rule is spilled to a file and read back in pieces");
    }

    private static string ReadSection()
    {
        string guide = ReadCollapsed(ArticlePath);
        int start = guide.IndexOf(SectionHeading, StringComparison.Ordinal);
        return start < 0 ? string.Empty : guide[start..];
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

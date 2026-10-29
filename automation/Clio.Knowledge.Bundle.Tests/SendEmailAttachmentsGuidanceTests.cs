using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-send-email-attachments</c> guide: the files a Send email element sends
/// (<c>email.attachments</c>). It is registered as <c>process-files-parameter</c> is - banner-less, ungated, no
/// legacy route - and reached from <c>process-send-email</c>, <c>process-files</c>, the element catalog,
/// <c>process-modeling</c> and routing. The rest of the Send email element stays pinned by
/// <see cref="SendEmailGuidanceTests"/>; the Process file element whose output an email sends, by
/// <see cref="ProcessFileGuidanceTests"/>.
/// </summary>
/// <remarks>
/// TRIM ALARMS: every clause below is a rule CrtProcessBuilder 1.6.6.127 enforces or a run-time fact an agent
/// cannot see from a green build. A reword that keeps the rule updates the clause here; a trim that drops it is
/// the failure these tests exist for.
/// </remarks>
[TestFixture]
public sealed class SendEmailAttachmentsGuidanceTests
{
    private const string ItemId = "process-send-email-attachments";

    private const string ArticlePath = "guidance/mcp/guides/processes/send-email-attachments.md";

    private const string EntryPath = "guidance/mcp/guides/processes/send-email.md";

    private const string FilesPath = "guidance/mcp/guides/processes/files.md";

    private const string CatalogPath = "guidance/mcp/guides/processes/element-catalog.md";

    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";

    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    /// <summary>The first CrtProcessBuilder that writes, describes and judges <c>email.attachments</c>.</summary>
    private static readonly Version Floor = new("1.6.6.127");

    /// <summary>
    /// This guide's design target, in the response characters of <see cref="ProcessGuideResponseSizeTests"/>. The
    /// guide is about 11,000 characters, so 12,500 leaves room for a clause or two and fails a larger addition,
    /// which should then be reviewed rather than absorbed. The 19,000-byte inline rule is asserted separately.
    /// </summary>
    private const int DesignTargetResponseCharacters = 12_500;

    [Test]
    [Description("The guide is registered banner-less and ungated with no legacy route, published at its itemId path, required by the library, routed after process-send-email, and pointed to by the Send email article, process-files, the element catalog and process-modeling.")]
    public void Resource_ShouldBeRegisteredAndReachedFromEveryArticleThatMentionsTheBlock()
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
        string guide = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        string routing = ReadCollapsed(RoutingPath);
        string entry = ReadCollapsed(EntryPath);
        string files = ReadCollapsed(FilesPath);
        string catalog = ReadCollapsed(CatalogPath);
        string modeling = ReadCollapsed(ModelingPath);

        // Assert
        using (new AssertionScope())
        {
            resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("topicId").GetString().Should().Be($"creatio.{ItemId}",
                because: "the topic is the stable cross-library identity of the guide");
            resource.GetProperty("uri").GetString().Should().Be($"docs://knowledge/com.creatio.clio/{ItemId}",
                because: "the builder derives the canonical route from the library and item ids");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the block ships enabled; a gated guide would be hidden while the tool accepts the block");
            resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
                because: "the guide was born after the v0 migration and has no v0 route to preserve");
            requiredItems.Should().Contain(ItemId,
                because: "the library requires every item it publishes for the process tools");
            requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
                because: "the requirement lists the canonical route next to the item id");
            guide.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "a banner obliges process-modeling to index the guide, and that article has no room left under the inline rule");
            routing.Should().Contain("-> name=process-send-email-attachments (after name=process-send-email)",
                because: "an article nothing routes to is never read, and it extends process-send-email rather than replacing it");
            entry.Should().Contain("The files the email SENDS, `email.attachments`, are owned by `process-send-email-attachments`.",
                because: "the Send email article owns the email block, so it must name the owner of the one member it does not own");
            entry.Should().Contain("\"attachments\"?: [ one source per entry:",
                because: "the entry article keeps the whole email-block contract, the new member included");
            files.Should().Contain("Send email attachments: `process-send-email-attachments`.",
                because: "an agent that opened process-files to hand files on must learn where emailing them is owned");
            catalog.Should().Contain("from CrtProcessBuilder **1.6.6.127**, the files it sends.",
                because: "the catalog owns what builds today and from which package");
            catalog.Should().Contain("`process-send-email-attachments` owns `email.attachments`.",
                because: "the catalog names the owner of every block it lists");
            modeling.Should().Contain("a sent `email.attachments` list REPLACES (`process-send-email-attachments`).",
                because: "the modify recipe lists what setElement does to a Send email, and attachments break the recipients' append rule");
        }
    }

    [Test]
    [Description("The guide owns the block, states the real CrtProcessBuilder floor and the contract gate, and tells the agent to confirm the list landed.")]
    public void Guide_ShouldOwnTheBlockAndCarryTheRealFloor()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        Match floor = Regex.Match(guide, @"Needs CrtProcessBuilder ([0-9]+[.][0-9]+[.][0-9]+[.][0-9]+) or later on the environment\.");

        // Assert
        guide.Should().Contain("This guide owns `email.attachments` of the Send email element (`sendEmail`)",
            because: "one owner per rule: the article declares what it owns");
        floor.Success.Should().BeTrue(
            because: "the block is gated on a four-part CrtProcessBuilder version (AGENTS.md compatibility rule)");
        new Version(floor.Groups[1].Value).Should().Be(Floor,
            because: "1.6.6.127 is the package clio requires for email.attachments; an older number would promise the block on a package that drops it");
        guide.Should().Contain("Use it only when the FULL create-business-process contract (`get-tool-contract` with `tool-names: [\"create-business-process\"]`, `detail: \"full\"`) names `attachments` in the `email` block.",
            because: "the default contract is shortened, so only the full one tells the agent its clio carries the member");
        guide.Should().Contain("an `email` block without the `attachments` you sent means the list did not land, so never report those files as attached.",
            because: "an older package drops the list while answering success, and the agent must not claim files it did not attach");
    }

    [Test]
    [Description("email.attachments (files the email sends) is kept apart from fileProcessing.attachments (files a Process file element reads), in the guide and in the Send email article.")]
    public void Guide_ShouldKeepTheTwoAttachmentsWordsApart()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string entry = ReadCollapsed(EntryPath);

        // Act
        const string clash = "Two words clash: `email.attachments` are the files an email SENDS; `fileProcessing.attachments` is the Object attachments group of a Process file element, which READS a record's files.";

        // Assert
        guide.Should().Contain(clash,
            because: "both members are named 'attachments' and point opposite ways; an agent that confuses them configures the wrong element");
        entry.Should().Contain("The files the email SENDS, `email.attachments`",
            because: "the Send email article says which direction its attachments member points");
    }

    [Test]
    [Description("The entry contract: exactly one of three sources, value refused, the accepted sources with the single File, and the refusals the package raises before any write.")]
    public void Guide_ShouldStateTheEntryAndItsRefusals()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "names exactly ONE source: `processParameter`, `sourceElement` + `sourceElementParameter` (both halves), or `expression`",
            "`value` is refused: a file has no constant form.",
            "a Process file element's output - `ObjectFiles` (Object attachments, Process parameter) or `ReportFiles` (Generated report); another element's FileCollection or File output; a process FileCollection",
            "a single File.",
            "REFUSED, naming the entry by its zero-based position (`email.attachments[1]`)",
            "a source that holds no files (neither a file nor a collection of files, or a collection with no File item)",
            "a collection with several File items (name the one to attach, `<collection>.<item>`)",
            "a Process file element's `Files` input (the files it is GIVEN",
            "more than 1000 entries.",
            "A refused entry writes none of the list.",
            "A Send email made multi-instance in the designer refuses `email.attachments` whole, `[]` included",
            "a mapping (`mappings[]`, `addMapping`) or a Formula element result onto one is refused, and so is any mapping, formula or attachment that READS one (it only receives files)."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "each is a refusal or a source rule the package enforces; the agent predicts the refusal instead of meeting it");
        }
    }

    [Test]
    [Description("Both order rules: declaration order on create and existence on modify, and the flow rule with its warning, its limit and the silent run-time consequence.")]
    public void Guide_ShouldStateBothOrderRules()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "declare the source element EARLIER in `elements[]` than the Send email, or the entry is refused with the forward-reference sentence.",
            "the source must exist when the operation runs - add it in an earlier operation of the batch or an earlier request.",
            "On the FLOW the source must run before the email.",
            "the answer warns that it \"does not lie on a path before\" the email: fix the flows.",
            "The warning is a hint, not a proof - a source on a gateway branch the run does not take still reaches the email and is not warned about.",
            "An email whose source has not run goes out WITHOUT those files and WITHOUT an error."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "the declaration order is refused but the flow order is only warned, and the run sends the email anyway");
        }
    }

    [Test]
    [Description("A sent list replaces the rows, an omitted list keeps them, [] removes all, the describe-before-send recipe and the missing concurrency check, the removal notice and the reader guard; recipients keep their append rule.")]
    public void Guide_ShouldStateTheReplaceRuleAndTheRecipe()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string entry = ReadCollapsed(EntryPath);

        // Act
        string[] clauses =
        [
            "`setElement` with `elementUpdate.email.attachments` replaces the element's attachments.",
            "every row the list does not name is REMOVED.",
            "`attachments` omitted: the rows are untouched. `[]`: every row is removed.",
            "Recipients differ: they MATCH-OR-APPEND (`process-send-email`).",
            "`describe-business-process element=<email>` IMMEDIATELY before, take its `email.attachments`, add or drop the entry, send the whole list.",
            "Modify has no concurrency check, so a list from an older read deletes the rows added since",
            "the entry, in the write shape, that restores it (\"To restore it, add { ... } to the list.\")",
            "A row something still reads is not removed: the request is refused, naming the readers."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "an agent used to the recipients' append rule sends only the new entry and deletes every other row");
        }
        entry.Should().Contain("Not so `email.attachments`: a sent list REPLACES the element's attachments (`process-send-email-attachments`).",
            because: "the recipients' no-removal sentence would otherwise read as covering the whole email block");
    }

    [Test]
    [Description("The troubleshooting line names the UseProcessEmailAttachments feature for the user who runs the process, and no process article claims a build-time check of it.")]
    public void Guide_ShouldCarryTheFeatureLine_AndNoArticleShouldClaimABuildTimeCheck()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string guide = ReadCollapsed(ArticlePath);
        (string ItemId, string Text)[] articles = [.. ProcessGuideSet.Declared(repositoryRoot)
            .Select(article => (article.ItemId, Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath))))];

        // Act
        const string claim = @"(build|save|create-business-process|modify-business-process)[^.]{0,60}(checks|refuses|warns about|reads)[^.]{0,40}UseProcessEmailAttachments"
            + @"|UseProcessEmailAttachments[^.]{0,60}(is checked|is refused|refuses the build|warns at build)";
        string[] claiming = [.. articles
            .Where(article => Regex.IsMatch(article.Text, claim, RegexOptions.IgnoreCase))
            .Select(article => article.ItemId)];

        // Assert
        guide.Should().Contain("An email that arrives WITHOUT its files: check that the source ran before the email, that its record scope or filter selected files, and feature `UseProcessEmailAttachments` for the user who runs the process",
            because: "the run time honours the feature for the executing user and sends the email without files when it is off");
        guide.Should().Contain("No build or save checks that feature.",
            because: "no build-time check reads the feature, so a green build proves nothing about it");
        claiming.Should().BeEmpty(
            because: "a claimed build-time check would make the agent trust a green build; found in: " + string.Join(", ", claiming));
    }

    [Test]
    [Description("Run-time facts: every mode and message source, manual mode's email activity, an empty file failing the element, system rights and the outside-Creatio destination behind the unscoped-source refusal judged on every save.")]
    public void Guide_ShouldStateTheRunTimeFactsAndTheScopeRefusal()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "The rows work alike in auto and manual mode and with a custom or a template message",
            "manual mode copies them onto the email activity it creates.",
            "A row with no file fails the element: an empty single File, or an `expression` that yields none.",
            "The email reads its files with rights disabled and sends them outside Creatio.",
            "an Object attachments element by `attachments.recordId` or a filter condition, a Generated report by `report.recordId` or a filter condition.",
            "Otherwise the request is refused at its END",
            "EVERY save through this tool is judged, so a designer-built process whose email already sends an unscoped source is refused on its next save of any kind",
            "A filter that selects something passes, even one by file name only.",
            "the files a Sub-process or a custom user task returns: this tool cannot see which records that code reads them from"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "these are what only the run shows, or a refusal that fires on a save that touched no attachment");
        }
        guide.Should().Contain("has not been measured through this tool",
            because: "the run-time statements are source traces, and the guide must say so (AGENTS.md: no unverified claim presented as verified)");
    }

    [Test]
    [Description("The read-back: the write shape plus slot, bindingLevels and issues, an omitted member when there are no rows, the describe-only members dropped before sending back, and the foreign-schema row left out.")]
    public void Guide_ShouldStateTheReadBack()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "returns `email.attachments` by row number, each entry in the write shape plus the describe-only `slot` (`Attachments<N>`), `bindingLevels` (`both` | `outerOnly` | `itemOnly` | `none`) and `issues`",
            "An element with no rows OMITS `attachments`.",
            "clio ignores `slot`, `bindingLevels` and `issues` with a warning, so drop them.",
            "An entry with an issue may be refused when sent back: the issue says what to send instead.",
            "A row created in another process schema is listed with an issue"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "describe is the modify recipe's input, so its shape has to be stated as the agent will see it");
        }
        guide.Should().NotContain("\"attachments\": []",
            because: "describe omits the member when there are no rows; an empty array is the write that removes every row");
    }

    [Test]
    [Description("The retired 'Send email attachments are not buildable' claim is gone from every declared process article and every manifest description, matched whitespace-insensitively.")]
    public void NoArticleOrDescription_ShouldClaimSendEmailAttachmentsAreNotBuildable()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        const string pattern = @"(send\s+email|email)\s+attachments[^.]{0,80}not\s+(yet\s+)?(buildable|supported)"
            + @"|attachments\s+(are|is)\s+not\s+(yet\s+)?(buildable|supported)[^.]{0,40}email";
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        string[] articles = [.. ProcessGuideSet.Declared(repositoryRoot)
            .Where(article => Regex.IsMatch(Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)), pattern, RegexOptions.IgnoreCase))
            .Select(article => article.ItemId)];
        string[] descriptions = [.. manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Where(resource => resource.TryGetProperty("description", out JsonElement value)
                && Regex.IsMatch(value.GetString() ?? string.Empty, pattern, RegexOptions.IgnoreCase))
            .Select(resource => resource.GetProperty("itemId").GetString()!)];

        // Assert
        articles.Should().BeEmpty(
            because: "the block builds now; the old claim would make an agent refuse a request the tool serves. Found in: " + string.Join(", ", articles));
        descriptions.Should().BeEmpty(
            because: "a manifest description is what an MCP client shows an agent choosing a guide. Found in: " + string.Join(", ", descriptions));
    }

    [Test]
    [Description("The guide carries no internal reference, no placeholder floor, no host or address, and does not describe the per-recipient multi-instance Sub-process pattern, which no measurement has confirmed yet.")]
    public void Guide_ShouldCarryNoPlaceholderOrInternalReference_AndNoUnmeasuredPattern()
    {
        // Arrange
        string guide = ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath);
        string collapsed = Collapse(guide);

        // Act
        bool jiraKey = Regex.IsMatch(guide, @"\b[A-Z][A-Z0-9]+-\d+\b");
        bool url = Regex.IsMatch(guide, @"https?://", RegexOptions.IgnoreCase);
        bool mailbox = Regex.IsMatch(guide, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");
        bool perRecipient = Regex.IsMatch(collapsed, @"per[- ]recipient|one file per recipient|each recipient gets", RegexOptions.IgnoreCase);

        // Assert
        guide.Should().NotContain("TBD",
            because: "a placeholder version boundary blocks the merge and tells the agent nothing about the environment");
        jiraKey.Should().BeFalse(because: "published guidance references no internal task");
        url.Should().BeFalse(because: "the guide names no stand, host or issue link");
        mailbox.Should().BeFalse(because: "the guide names no mailbox address");
        perRecipient.Should().BeFalse(
            because: "the per-recipient pattern is documented only after a run confirms a single File attaches one file");
    }

    [Test]
    [Description("The guide stays inside its design target and the 19,000-byte inline reply rule.")]
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
            because: "a larger addition should be reviewed, or split, rather than absorbed");
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline rule is spilled to a file and read back in pieces");
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

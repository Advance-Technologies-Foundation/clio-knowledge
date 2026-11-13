using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-send-chat-message</c> guide: the <c>chatMessage</c> block of the Send chat message element. It is
/// registered as <c>process-web-service</c> is - banner-less, ungated, no legacy route, loaded on demand rather than
/// indexed by the process digest - and reached from the element catalog, its details, process-modeling and routing.
/// </summary>
/// <remarks>
/// TRIM ALARMS: every clause below is a rule CrtProcessBuilder enforces, a refusal or notice an agent has to recognise,
/// or a run-time fact an agent cannot see from a green build (an omitted <c>ignoreErrors</c> stops the process; a
/// switch to custom needs the new text; a <c>connections[]</c> replay aborts the edit). A reword that keeps the rule
/// updates the clause here; a trim that drops it is the failure these tests exist for.
/// </remarks>
[TestFixture]
public sealed class SendChatMessageGuidanceTests
{
    /// <summary>
    /// The first CrtProcessBuilder that builds and describes the element: a released four-part version, or the
    /// placeholder token while the package cut is not fixed. The release gate refuses the placeholder, so it cannot
    /// merge; the same token is stated in the article, the two catalog articles and the manifest description.
    /// </summary>
    private const string FloorPattern = @"(?:<[A-Z][A-Z0-9-]*TBD>|\d+(?:\.\d+){3})";

    private const string ClioGate =
        "Use it only when the FULL contract (`get-tool-contract` with `tool-names: [\"create-business-process\"]`, `detail: \"full\"`) names `sendChatMessage`; a clio whose contract lacks it refuses the block before sending.";

    private const string ItemId = "process-send-chat-message";

    private const string ArticlePath = "guidance/mcp/guides/processes/send-chat-message.md";

    private const string CatalogPath = "guidance/mcp/guides/processes/element-catalog.md";

    private const string CatalogDetailsPath = "guidance/mcp/guides/processes/element-catalog-details.md";

    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";

    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    /// <summary>
    /// This guide's design target, in the response characters of <see cref="ProcessGuideResponseSizeTests"/>. The guide
    /// is about 20,500 response characters, so a new clause pays for itself with a trim, and a larger addition is reviewed, or
    /// split, rather than absorbed. The 19,000-byte inline rule is asserted separately.
    /// </summary>
    private const int DesignTargetResponseCharacters = 21_500;

    [Test]
    [Description("The guide owns the block and states the FULL-contract gate and the floor; it is registered banner-less, ungated and without a legacy route, required by the library, and the element catalog, its details, process-modeling and routing send the reader to it, with one floor everywhere.")]
    public void Guide_ShouldOwnTheBlock_AndTheEntryArticlesShouldRouteToIt()
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
        string raw = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        string guide = Collapse(raw);
        string catalog = ReadCollapsed(CatalogPath);
        string details = ReadCollapsed(CatalogDetailsPath);
        string modeling = ReadCollapsed(ModelingPath);
        string routing = ReadCollapsed(RoutingPath);
        string description = resource.GetProperty("description").GetString()!;
        Match guideFloor = Regex.Match(guide, $@"Needs CrtProcessBuilder (?<floor>{FloorPattern}) or later on the environment\.");
        Match catalogFloor = Regex.Match(catalog, $@"- `sendChatMessage` \(Send chat message\), from CrtProcessBuilder \*\*(?<floor>{FloorPattern})\*\* and only when the FULL contract names it:");
        Match detailsFloor = Regex.Match(details, $@"BUILDABLE from CrtProcessBuilder \*\*(?<floor>{FloorPattern})\*\* via `type:""sendChatMessage""` with a `chatMessage` block, only when the FULL contract names it; `process-send-chat-message` owns it");
        Match descriptionFloor = Regex.Match(description, $@"\(CrtProcessBuilder (?<floor>{FloorPattern}) or newer; only when the FULL create-business-process contract names sendChatMessage\)");

        // Assert
        using (new AssertionScope())
        {
            resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("topicId").GetString().Should().Be($"creatio.{ItemId}",
                because: "the topic is the stable cross-library identity of the guide");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the clio side is gated by the FULL contract, not by a feature; a gated guide would be hidden while the tool accepts the block");
            resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
                because: "the guide was born after the v0 migration and has no v0 route to preserve");
            requiredItems.Should().Contain(ItemId,
                because: "the library requires every item it publishes for the process tools");
            requiredUris.Should().Contain($"docs://knowledge/com.creatio.clio/{ItemId}",
                because: "the requirement lists the canonical route next to the item id");
            raw.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "the guide is loaded on demand; a banner obliges process-modeling to index it, and that article has no room left under the inline rule");

            guide.Should().Contain("This guide owns the `chatMessage` block of the Send chat message element",
                because: "the article is the one owner of the block");
            guide.Should().Contain("`chatMessage {from, to, messageSource?, template?, message?, ignoreErrors?, columnValues?}`, on `create-business-process`, `addElement` and `setElement` (`elementUpdate.chatMessage`)",
                because: "the contract anchor names every member and every route that takes the block");
            guide.Should().Contain(ClioGate,
                because: "an older clio refuses the block in its pre-send key check, so the package floor alone does not make the block usable");
            guideFloor.Success.Should().BeTrue(because: "the package floor gates every instruction in the article");
            catalogFloor.Success.Should().BeTrue(
                because: "the catalog owns what builds today and from which package, with the guide's contract gate");
            detailsFloor.Success.Should().BeTrue(
                because: "the element entry carries the floor and names the owner of its block");
            descriptionFloor.Success.Should().BeTrue(
                because: "a client picking guides by description would otherwise build the block on a clio that refuses it");
            new[] { catalogFloor, detailsFloor, descriptionFloor }
                .Select(match => match.Groups["floor"].Value)
                .Should().AllBe(guideFloor.Groups["floor"].Value,
                    because: "the floor moves in the article, both catalog articles and the manifest description together");

            catalog.Should().Contain("`process-send-chat-message` owns its `chatMessage` block.",
                because: "the catalog names the owner of every block it lists");
            Regex.IsMatch(catalog, $@"`SendChatMessageUserTask` \(Send chat message\): from CrtProcessBuilder \*\*{FloorPattern}\*\* REFUSED on create; on an older package do not build it on the generic route either")
                .Should().BeTrue(because: "the data-id paragraph maps a task name to a build type; the refusal arrives with the package floor, and below it the generic route builds an element that cannot carry its template or column values");
            modeling.Should().Contain("* a `chatMessage` block - partial; a switch to custom needs `message` (`process-send-chat-message`; FULL contract only);",
                because: "process-modeling lists the blocks whose partial-update rule an edit gets wrong, and the switch to custom is that rule here");
            routing.Should().Contain("(`sendChatMessage`; build it only when the FULL contract names it):",
                because: "routing carries the guide's contract gate, as it does for the other blocks a released clio may not know");
            routing.Should().Contain("-> name=process-send-chat-message",
                because: "an article nothing routes to is never read");
        }
    }

    [Test]
    [Description("The guide states when not to use the element, the channel ranking with the system-setting check, the one-contact recipient, and the template choose-or-ask rule with provider templates left to the designer.")]
    public void Guide_ShouldStateTheChoices()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "a bulk send - a marketing broadcast to an audience: that is a campaign, not this element. A per-record notification is this element",
            "each send still needs the identity and the 24-hour window",
            "\"send, then wait for the answer\": the chat is created Completed, and a reply opens a NEW chat;",
            "Prefer a Lookup SYSTEM SETTING that references the ROOT Channel object: an `expression` `[#SysSettings.<Code>#]`.",
            "It is stored in the typed form the designer writes, `[#SysSettings.<Code><Guid>#]`, where `<Guid>` is literal text (the value type), as in `[#SysSettings.UsrSupportChatChannel<Guid>#]`; describe echoes that.",
            "no setting with that code, not a Lookup, or a Lookup of another object: REFUSED, naming the code. A Lookup of a replacing Channel schema: built with a notice",
            "Create a setting only after the user confirms (an environment change), then SET ITS VALUE.",
            "A constant channel LAST:",
            "An INACTIVE channel is built with a notice: the designer offers active channels only, and the run fails until the channel is activated.",
            "ONE contact; an array is refused. `value` is a contact id or its `[#Lookup…#]` macro, never a name",
            "A fixed contact LAST: advise against it and take it only when the user insists.",
            "REFUSED, naming it: an unknown name or id, another type (an email template), no type, and an ambiguous name (pass the id).",
            "When you cannot tell which fits, ASK the user to name the template, or offer a custom message instead; never guess",
            "No system setting selects the template. The template's language is chosen at run time: the contact's language, then the channel's, then the `DefaultMessageLanguage` setting, then the default language.",
            "Template macros resolve against the CREATED CHAT, not the process's record:",
            "PROVIDER templates (WhatsApp approved templates) are not supported by this tool yet",
            "build the process here and let the user pick the provider template in the Process Designer."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "a channel, contact or template picked by guess builds green and is wrong only at run time. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide states the custom-text rules, ignoreErrors and the two outputs, the column values on the chat, the run-time prerequisites and the failure classes with the measured chat outcome.")]
    public void Guide_ShouldStateTheMessageAndRunTimeRules()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "a `[#...#]` macro or a `[[keyword:...]]` token in `message` is REFUSED; other bracket text is sent literally. Personalise with a FORMULA on the element's `Message` parameter through a mapping",
            "the parameter reference form is owned by `process-formulas`",
            "Line breaks in a constant are kept, with a notice: the designer's message field is one line",
            "`ignoreErrors` omitted on `create-business-process` and `addElement` is FALSE: a failed send STOPS the process, and the element log carries the error.",
            "`SendStatus` is `\"Sent\"` or `\"Error\"`; `SendStatusDetails` is empty on success, else the error text.",
            "It also swallows INPUT errors",
            "so a completed run is not a delivered message: read `SendStatus`.",
            "An unknown column and `Id` are refused, and so is `selectionColumn`.",
            "is accepted with a notice: your value overwrites it.",
            "A `Channel` row alone is not a working channel: it needs its messenger settings",
            "the recipient is taken from the contact's PRIMARY \"Mobile phone\" communication (not the `MobilePhone` column)",
            "WhatsApp and Facebook Messenger: the provider's 24-hour window.",
            "Recognise the class; never key on the exact text, which can change between builds:",
            "`Channel with Id=<id> is not active.`",
            "On current builds a failed send leaves no chat (measured on 10.2.1122); older OmnichannelMessaging builds may leave an empty Completed chat."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "none of these is visible from a green build. Missing: " + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide states the partial setElement, the switch to custom that needs message, the raw-mapping refusals, the connections[] read-back that must never be replayed, and the read-back with its describe-only members.")]
    public void Guide_ShouldStateModifyAndReadBack()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "`setElement` with `elementName` and `elementUpdate.chatMessage` is a PARTIAL update: omitted members keep their stored values.",
            "Template to custom: send `message` in the SAME call, or the switch is refused - even when the element shows text:",
            "A raw `addMapping` onto `Template`, `MessageType`, `RecordColumnValues` or `IgnoreErrors`, or of a CONSTANT onto `Channel`, `Contact` or `Message` (use `chatMessage.message`), is refused, naming the block member to use.",
            "A switch that clears `Template` or `Message` while another element reads it is refused: re-point or remove those readers first, in a SEPARATE, earlier call.",
            "a constant reads back as `{value: <record id>, display: <name>}`, and the describe-only `display` names the channel and the contact",
            "ignore them and NEVER replay them - `setConnections` refuses this task and the edit aborts. Edit through `chatMessage`.",
            "On `setElement` an omitted list is kept, a sent list REPLACES the stored set, and `[]` clears it.",
            "`ignoreErrors` (always stated, `false` included: an untouched designer checkbox reads false)",
            "To resubmit, drop `templateName`, `outputs` and `issues` and send the rest unchanged (`display` is tolerated); when `template` is a raw provider-template id, drop `template` too: on `setElement` the same id sent back is a no-op and any other is refused, and on create it is refused.",
            "The generic `userTask` route REFUSES `SendChatMessageUserTask` on create: build `sendChatMessage`."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "a replayed describe or a half switch is how an edit breaks a working element. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide and its manifest description carry no internal task key, URL, host, internal path or test-stand name.")]
    public void Guide_ShouldCarryNoInternalReference()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string article = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string description = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId)
            .GetProperty("description").GetString()!;
        string guide = article + "\n" + description;

        // Act
        bool jiraKey = Regex.IsMatch(guide, @"\b[A-Z][A-Z0-9]+-\d+\b");
        bool url = Regex.IsMatch(guide,
            @"https?://|\blocalhost\b|\b\d{1,3}(?:\.\d{1,3}){3}(?::\d+|/)|\b(?:[a-z0-9-]+\.)+(?:com|net|org|io|dev|local|cloud|ru|ua)\b",
            RegexOptions.IgnoreCase);
        bool specPath = Regex.IsMatch(guide, @"spec/|\.cs\b|docs/knowledge|packages/|\b[A-Z]:\\", RegexOptions.IgnoreCase);
        bool standName = Regex.IsMatch(guide, @"UsrChatCap|\bstudio-\d+|\beng-?\d{4,}", RegexOptions.IgnoreCase);

        // Assert
        using (new AssertionScope())
        {
            jiraKey.Should().BeFalse(because: "published guidance references no internal task");
            url.Should().BeFalse(because: "the guide names no stand, host or issue link");
            specPath.Should().BeFalse(because: "the guide names no internal file path of the clio repository or the package");
            standName.Should().BeFalse(because: "the capture processes and the stand are not product names");
        }
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

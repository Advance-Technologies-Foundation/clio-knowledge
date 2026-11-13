using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-dynamic-case</c> guide: <c>create-dynamic-case</c>, a minimal dynamic case (DCM) from a lookup
/// column plus the record-page stage progress bar. Registered banner-less and ungated with no legacy route, routed from
/// <c>routing</c> on its own row and from its Pages block.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each clause below is a rule the tool enforces or a run-time fact an agent cannot see from a successful
/// call - the order rule, the absent compile, the undo order and what is not supported. A reword that keeps the rule
/// updates the clause here; a trim that drops it is the failure these tests exist for.
/// </remarks>
[TestFixture]
public sealed class ProcessDynamicCaseGuidanceTests
{
    private const string ItemId = "process-dynamic-case";
    private const string ArticlePath = "guidance/mcp/guides/processes/dynamic-case.md";
    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";
    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath), @"\s+", " ");

    [Test]
    [Description("The guide is registered at its itemId path, required by the library, and routed from routing on its own row and from the Pages block.")]
    public void Resource_ShouldBeRegisteredAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string routing = ReadCollapsed(RoutingPath);
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string guide = ProcessGuideSet.Read(repositoryRoot, ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString())
                .Should().Contain(ItemId, because: "the library requires every guide it ships");
            requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
                because: "the requirement lists the canonical route next to the item id");
            resource.GetProperty("topicId").GetString().Should().Be("creatio.process-dynamic-case",
                because: "the topic is the stable cross-library identity of the guide");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the tool ships ungated; a gated guide would be hidden while the tool answers");
            guide.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "a banner obliges process-modeling to index the guide, and that article has no room left");
            ProcessGuideSet.GoLiveFloor.Should().NotContain(ItemId,
                because: "the floor holds only banner-carrying articles split out of process-modeling");
            Regex.Matches(routing, $"name={ItemId}\\b").Count.Should().BeGreaterThanOrEqualTo(2,
                because: "a stage lifecycle and a progress bar on a page both lead an agent here");
        }
    }

    [Test]
    [Description("The guide carries the rules an agent cannot see from a successful call: the clio version boundary, pass stages, the order source is reported, no compile or restart, undo only what the call created and in order, and what is not supported yet.")]
    public void Article_ShouldKeepTheRulesASuccessfulCallHides()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("create-dynamic-case", because: "the guide owns this tool");
            guide.Should().MatchRegex(
                @"Version boundary: `create-dynamic-case` needs clio (<[A-Z][A-Z0-9-]*TBD>|\d+(\.\d+){3}) or later; on an older clio, stop here",
                because: "a published guide reaches every clio, and an older one has no such tool to call");
            guide.Should().Contain("pass `stages`", because: "only the caller knows the business order");
            guide.Should().Contain("orderSource", because: "the order used is always answered and must be relayed");
            guide.Should().Contain("NO compile and NO restart", because: "a case is interpreted; a compile only costs time");
            guide.Should().Contain("ONLY when the call answered `settings.status: created`",
                because: "deleting a reused settings row cascades away other cases' link rows");
            guide.Should().Contain("Steps 1-3 ONLY for a call whose `case.status` is `created`",
                because: "after already-present the case and its link row were never written by this call");
            guide.Should().Contain("Steps 4 and 5 follow `page.status` and `dependencies.status` alone",
                because: "a call that found the case can still have inserted the bar or added dependencies");
            guide.Should().Contain("ONLY when `dependencies.status` is `added` or `left-after-failure`; for `kept-for-leftovers` only after the leftovers are gone",
                because: "removed-after-failure dependencies are already gone, and kept ones still hold the leftovers");
            guide.Should().Contain("stop at the first failure",
                because: "a later step assumes the earlier ones took");
            guide.Should().Contain("Re-run the call with `dry-run: true` before anything else",
                because: "after a lost reply the case may exist; the tool's own message says the same, and a real re-run would create it unasked");
            guide.Should().Contain("KEEP this result's `dependencies.added`: no later result reports them",
                because: "a re-run finds the dependencies present and reports none added, so step 5 would never remove them");
            guide.Should().Contain("With `outcome: refused` nothing was written",
                because: "request-failed and page-append-rejected also follow a write, under unknown and created-page-failed");
            guide.Should().Contain("or one without its settings row or link row",
                because: "already-present needs the settings row and the link row too, not only the same stages");
            guide.Should().Contain("`saveOnChange` from `save-on-change` (default false)",
                because: "the bar keeps its own default, a click that only sets the field, unless the user asks for a save on click");
            guide.Should().Contain("on clio OLDER than 8.1.0.152 (8.1.0.151 and earlier have no `save-on-change`): do not ask and do not pass it",
                because: "an older clio always saves on a click, so offering the choice would promise a bar the user does not get");
            guide.Should().Contain("the bar's \"Save on change\" toggle in the page designer is the only way to change that",
                because: "on an older clio the toggle is the only lever");
            guide.Should().Contain("ask once before the call, naming what each choice means",
                because: "the save mode is the user's choice, and the default is the manual one");
            guide.Should().Contain("nothing that reacts to a saved record (processes started by the stage or the record change, rules on the saved data) runs until Save",
                because: "the user must know that without a save no automation reacts to the new stage");
            guide.Should().Contain("a click saves the whole record with any other unsaved edit, starts what reacts to the save, and a failed save reverts the stage",
                because: "the user must know what a save on click also saves and starts");
            guide.Should().Contain("Pass `save-on-change: true` only for the second",
                because: "the default needs no argument");
            guide.Should().Contain("When the dry run plans a bar (`page.status: planned`) and the user has not already said, ask once",
                because: "a choice asked for a page that already has a bar would not be applied");
            guide.Should().Contain("With `page.status: already-present` do not ask and do not pass it",
                because: "the call never rewrites a bar it found, so the question would promise a mode the user does not get");
            guide.Should().Contain("a stage click on a bar this call inserts saves the record at once",
                because: "on an older clio only the bar the call inserts saves on a click; an existing bar keeps its toggle");
            guide.Should().Contain("(8.1.0.151 and earlier: `saveOnChange: true`, `askUserToChangeSchema: false`, no `page.saveOnChange`)",
                because: "the record-page facts differ by clio version, and an older result has no page.saveOnChange");
            guide.Should().Contain("keeps its own mode: `save-on-change` does not change it, the toggle does",
                because: "the call never rewrites a bar it found, so the user's choice does not reach it");
            guide.Should().Contain("A page stored in the full form (every record page `create-app` makes) takes it too",
                because: "an agent that read the old refusal would otherwise skip the bar on every create-app page");
            guide.Should().Contain("Remedy: `add-progress-bar: false`, fix the page as the message says, then re-run the same call",
                because: "a page refusal is fixed on the page, and the re-run answers already-present for the case");
            guide.Should().Contain("MUST have the user's explicit agreement",
                because: "the undo tools delete live data and ask for no confirmation");
            guide.Should().Contain("ONLY when `page.status` is `inserted`",
                because: "after already-present the bar on the page is not this call's to remove");
            guide.Should().MatchRegex(
                @"1\. `delete-schema \{ schema-name: case\.schemaName, environment-name, remote: true \}` .* 2\. `remove-data-binding-row-db` on the `SysDcmSchemaInSettings` entry of `settings\.bindings`, package-name `case\.packageName`, key .* 3\. `remove-data-binding-row-db` on the `SysDcmSettings` entry of `settings\.bindings`, package-name `case\.packageName`, key .* 4\. ONLY when `page\.status` is `inserted`: remove the two inserted items .* `update-page` with `mode: replace` .* 5\. `remove-package-dependency` for each of `dependencies\.added`",
                because: "the case goes first as the server's own rollback does (an enabled case without its settings rows keeps starting and cannot be opened), delete-schema reaches an environment only with remote: true, remove-data-binding-row-db requires the package the server bound the rows to, append cannot remove the bar, and the dependencies go last");
            guide.Should().Contain("== Not supported yet ==", because: "an agent must not promise steps or transitions");
            guide.Should().Contain("Steps inside stages", because: "the case is minimal: its stages are empty");
            guide.Should().Contain("several cases per object", because: "the platform starts every enabled case");
            guide.Should().Contain("a new version of a case, deleting a case or changing its stage column",
                because: "a case is changed in place only; versions, deletion and the stage column are the case designer's");
            guide.Should().NotContain("Part of the process guide set",
                because: "a banner would make process-modeling index the guide, which has no room");
        }
    }

    [Test]
    [Description("The guide makes an agent answer a stage question from a dry run, not from schema reads, and answer the cases the tool cannot do in user words: a field that is not a lookup, what is not supported, and the Save on change toggle.")]
    public void Article_ShouldMakeTheAgentAnswerFromTheTool()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("answer only after `describe-dynamic-case { entity-schema-name }` (read-only)",
                because: "agents answered from schema reads, which cannot see the object's case or case settings");
            guide.Should().Contain("only after the call with `dry-run: true` (it writes nothing)",
                because: "an older clio has no describe tool, and its dry run still sees the case");
            guide.Should().Contain("gets it CHANGED (\"Change an existing case\"), never a second one",
                because: "on prompt 11 an agent changed nothing and did not say how a case is changed");
            guide.Should().Contain("WITHOUT `stages` (a stage list is checked before the object's case)",
                because: "a stage the lookup does not have is refused before case-exists and settings-conflict, hiding them");
            guide.Should().Contain("refusal IS the answer",
                because: "agents offered to rewrite the lookup values instead of relaying case-exists or settings-conflict");
            guide.Should().Contain("Never offer to rename, replace or add lookup values, or another column (an order column), to get around it",
                because: "a new column or new values still meet the existing case, which the tool does not change");
            guide.Should().Contain("Stages need a field with a fixed list of values (a lookup)",
                because: "an agent asked a clarifying question instead of saying an amount cannot be stages");
            guide.Should().Contain("is not supported yet; the case designer does that",
                because: "an agent planned a task inside a stage instead of saying steps are not supported");
            guide.Should().Contain("Name the toggle", because: "an agent answered the save question without the Save on change toggle");
            guide.IndexOf("== Answer the user from the tool ==", StringComparison.Ordinal).Should().BeLessThan(
                guide.IndexOf("== Before the call ==", StringComparison.Ordinal),
                because: "these answers come before any call is planned, so they lead the guide");
        }
    }

    [Test]
    [Description("The guide sends an agent to describe and dry-run before changing an existing case, never lets it confirm running cases on its own, and keeps enabled a call of its own.")]
    public void Article_ShouldGuideAChangeToAnExistingCase()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("== Change an existing case (clio <CLIO-TBD>+) ==",
                because: "the tools ship with a clio release, and the guide publishes to every clio at once");
            guide.Should().Contain("`describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only)",
                because: "the agent reads the case before it plans a change");
            guide.Should().Contain("a stage left out is REMOVED",
                because: "stages is the whole new list, and a dropped item removes a stage");
            guide.Should().Contain("First `dry-run: true`: show the user `diff` and the notes, write only after their yes",
                because: "agents answered about existing cases without a dry run and missed what would change");
            guide.Should().Contain("pass `confirm-running-instances: true` only after they say yes, NEVER on your own",
                because: "every save changes what the running cases follow");
            guide.Should().Contain("goes ALONE (`invalid-request` otherwise)",
                because: "switching a case on or off is a save of its own");
            guide.Should().Contain("change that case instead (`modify-dynamic-case`)",
                because: "case-exists now has a remedy");
            guide.Should().Contain("`successful: false` (final group only",
                because: "only the final group can end a case as unsuccessful, as in the case designer");
        }
    }

    [Test]
    [Description("A request about the order or outcomes of an object's status values reaches the case without the word \"stage\": a stand run had one agent draw a diagram and another add an order column to the lookup instead of reading the case.")]
    public void StatusFlowWording_ShouldRouteToTheCase()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);
        string routing = ReadCollapsed(RoutingPath);

        // Assert
        using (new AssertionScope())
        {
            routing.Should().Contain("or the order or outcomes of its status values (no \"stage\" needed)",
                because: "routing is what an agent reads first, and the failing prompts never said stage");
            routing.Should().Contain("the user names the object by its caption: `list-apps` / `get-app-info` when a schema-name search misses",
                because: "a rerun reached the routing and still stopped: find-entity-schema matches schema names, not \"purchase requests\"");
            guide.Should().Contain("an order or outcomes of status values",
                because: "the scope names the wording that belongs to a case");
            guide.Should().Contain("stages or status flow (\"ends Approved or Rejected\", \"move X before Y\")",
                because: "the read-first rule covers the two prompts that failed");
        }
    }

    [Test]
    [Description("The whole guide fits one inline get-guidance reply: it is not one of the split articles, so nothing else measures it.")]
    public void Article_ShouldFitOneInlineReply()
    {
        // Arrange
        string article = ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath);

        // Act
        int replyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(article);

        // Assert
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply past the inline limit is spilled to a file the release gate's agents do not read; split the article at a section boundary instead");
    }
    [Test]
    [Description("process-modeling no longer lists DCM among the schemas that carry their own compile obligations: a case is interpreted.")]
    public void ProcessModeling_ShouldNotClaimACaseNeedsACompile()
    {
        // Arrange & Act
        string modeling = ReadCollapsed(ModelingPath);

        // Assert
        modeling.Should().NotContain("DCM",
            because: "the compile-obligation list sent agents to compile after creating a case, wherever DCM stands in it");
    }
}

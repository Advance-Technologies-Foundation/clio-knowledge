using System;
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
    private const string ChangeItemId = "process-dynamic-case-change";
    private const string ChangeArticlePath = "guidance/mcp/guides/processes/dynamic-case-change.md";
    private const string FiltersItemId = "process-dynamic-case-filters";
    private const string FiltersArticlePath = "guidance/mcp/guides/processes/dynamic-case-filters.md";
    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";
    private const string RoutingPath = "guidance/mcp/guides/routing.md";
    private const string VersionsPath = "guidance/mcp/guides/processes/versions.md";

    /// <summary>A clio version boundary: the release placeholder until the release, then the version it shipped in.</summary>
    private const string ClioVersion = @"(<[A-Z][A-Z0-9-]*TBD>|\d+(\.\d+){3})";

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
                @"Version boundary, before anything else: `create-dynamic-case` needs clio (<[A-Z][A-Z0-9-]*TBD>|\d+(\.\d+){3}) or later\. If `clio-run` answers `unknown tool` for it, the clio serving you is older: stop here",
                because: "a published guide reaches every clio, and an older one has no such tool to call");
            guide.IndexOf("Version boundary, before anything else", StringComparison.Ordinal).Should().BeLessThan(
                guide.IndexOf("== Answer the user from the tool ==", StringComparison.Ordinal),
                because: "an agent on an older clio must meet the boundary before it reads how to call the tool");
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
            guide.Should().Contain("a start condition other than one filter column",
                because: "several cases are split by one filter column only; any other start condition is the case designer's");
            guide.Should().Contain("A value left out of `stages` is not a stage",
                because: "a case may take some of the lookup's values, and a record holding another shows no current stage");
            guide.Should().Contain("a start condition other than one filter column, deleting a case or changing its stage or filter column",
                because: "deletion and the stage column are still the case designer's");
            guide.Should().NotContain("a new version of a case",
                because: "a version is made and switched by the change guide's tools now, so the guide must not send the user to the designer for it");
            guide.Should().Contain("(`describe-dynamic-case`, `modify-dynamic-case`) and its versions: `process-dynamic-case-change`",
                because: "an agent reaching the create guide with a version request must find the guide that owns versions");
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
            guide.Should().Contain("gets it CHANGED (\"Change an existing case\" in `process-dynamic-case-change`), never a second one unless the user splits its records by a filter",
                because: "on prompt 11 an agent changed nothing and did not say how a case is changed");
            guide.Should().Contain("When `clio-run` answers `unknown tool` for `describe-dynamic-case` (clio older than",
                because: "the agent needs the signal that tells an older clio apart, not only its version");
            guide.Should().Contain("A newer clio refuses both for an older CrtProcessBuilder, naming the version it needs: `install-process-builder` restarts the site, so only once the user approves it",
                because: "a newer clio floors describe and create at the package version its refusal names, so no dry run answers on an older package, and an install restarts the site");
            guide.Should().NotContain("get-tool-contract",
                because: "a version check through the contract index cost a large call, and clio-run's unknown-tool answer is the signal");
            guide.Should().Contain("in any editable package and WITHOUT `stages`",
                because: "a dry run in the object's read-only package is refused package-read-only before it reaches the case");
            guide.Should().Contain("WITHOUT `stages` (a stage list is checked before the object's case)",
                because: "a stage the lookup does not have is refused before case-exists and settings-conflict, hiding them");
            guide.Should().Contain("refusal IS the answer",
                because: "agents offered to rewrite the lookup values instead of relaying case-exists or settings-conflict");
            guide.Should().Contain("Never offer to rename, replace or add lookup values, or another column (an order column), to get around an existing case",
                because: "a new column or new values still meet the existing case");
            guide.Should().Contain("adding a value the user asked to make a stage is part of changing the case",
                because: "the change flow adds a missing lookup value, and the read-first rule must not forbid it");
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
    [Description("The change guide sends an agent to describe and dry-run before changing an existing case, never lets it confirm running cases or move records on its own, keeps enabled a call of its own, and names the remedy of every refusal and outcome.")]
    public void Article_ShouldGuideAChangeToAnExistingCase()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ChangeArticlePath);
        string createGuide = ReadCollapsed(ArticlePath);
        string filters = ReadCollapsed(FiltersArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().MatchRegex(
                $@"Version boundary, before anything else: `describe-dynamic-case` and `modify-dynamic-case` need clio {ClioVersion} or later\. If `clio-run` answers `unknown tool` for `describe-dynamic-case`, the clio serving you is older: stop here",
                because: "the tools ship with a clio release, and the guide publishes to every clio at once");
            guide.IndexOf("Version boundary, before anything else", StringComparison.Ordinal).Should().BeLessThan(
                guide.IndexOf("== Change an existing case ==", StringComparison.Ordinal),
                because: "an agent on an older clio must meet the boundary before it reads how to call the tools");
            guide.Should().Contain("They also need CrtProcessBuilder 1.6.6.209 or later on the environment: clio refuses an older one, naming the version it needs",
                because: "the tools refuse an older package although the clio has them, and the floor is the one the versions need");
            guide.Should().Contain("`process-builder-without-case-change`",
                because: "clio names this code, and the guide that owns describe and modify must give its remedy");
            createGuide.Should().Contain("Its twin for describe and modify, `process-builder-without-case-change`: `process-dynamic-case-change`",
                because: "the refusal of describe and modify is answered by the guide that owns them");
            guide.Should().Contain("its describe has no `records` (absent is not 0), and removing a stage that records hold is not refused: count them with the data tools and ask the user first",
                because: "a clio of 8.1.0.153 has describe and reads this guide, so the precondition of a stage removal stays where that clio can read it");
            filters.Should().NotContain("absent is not 0",
                because: "the older-clio rules have one owner, the change guide");
            filters.Should().NotContain("1.6.6.179",
                because: "the stale package floor moved back to the change guide and was raised there");
            guide.Should().Contain("`packageReadOnly: true` (the package is not editable): a change to its stages, captions or filter goes straight to a new version",
                because: "a run spent a refused in-place dry run on a case describe had already shown as read-only");
            guide.Should().Contain("MUST read `process-dynamic-case-filters` first: a FIRST filter sets the object's filter column for good (no tool removes it), and every case of the object then needs a filter; say so before the yes",
                because: "a split must not separate the irreversible first filter from its precondition");
            createGuide.Should().Contain("`process-builder-without-case-change`",
                because: "an agent that meets the code beside process-builder-without-build-case must find where it is answered");
            guide.Should().Contain("`describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only)",
                because: "the agent reads the case before it plans a change");
            guide.Should().Contain("the top-level `caption` renames the CASE",
                because: "the top-level caption renames the case; only an item's caption renames a stage");
            guide.Should().Contain("`{\"value\":\"Rejected\",\"group\":\"Approved\",\"successful\":false}`",
                because: "an item is keyed by value, and one example shows the shape the tool takes");
            guide.Should().Contain("an item's `caption` renames the stage AND its lookup value, as the case designer does (the bar shows the value's name): the lookup's value is renamed WHEREVER it is used",
                because: "the bar and the designer show the value's name, so a stage renamed alone is shown nowhere (dev testing A6)");
            guide.Should().Contain("`value-rename-confirmation-required` (a caption renames the lookup's value wherever it is used) -> `confirm-value-rename: true`",
                because: "a value rename shows on every record of the object, so the user agrees first");
            guide.Should().Contain("`stage-records-confirmation-required` (records hold a removed stage's value: all keep their data; those without a running case show no current stage while the case is enabled, and no bar of it while it is disabled (another enabled case they fit shows its own); one whose case runs keeps its version while it runs",
                because: "measured: a record with no running case shows the bar with no current stage while the case is enabled (dev testing A13) and no bar of it while it is disabled (round 4, M2), and a running case keeps showing its own version; that another enabled case the record fits draws its own bar follows from the bar's source rule (the record's running case draws it) and was not measured");
            guide.Should().Contain("`records` (the records holding its value, finished cases included)",
                because: "describe counts the records per stage, so the agent can tell the user before a removal");
            guide.Should().Contain("add it with the data tools first, only after the user agreed to that new value, never unasked",
                because: "a new lookup value shows in every record's choice list");
            guide.Should().Contain("`color` (`#RRGGBB`) recolours the stage",
                because: "the colour is changed through the tool, not left to the designer");
            guide.Should().Contain("An older clio (no `confirm-value-rename`) works the older way",
                because: "a knowledge release reaches every clio at once, and an older one renames only the stage and refuses no removal");
            guide.Should().Contain("a stage left out is REMOVED",
                because: "stages is the whole new list, and a dropped item removes a stage");
            guide.Should().Contain("so this changes the case, it does not get around it",
                because: "adding a missing value the user asked for is part of the change, still after their yes");
            guide.Should().Contain("First `dry-run: true`: show the user `diff` and the notes, write only after their yes",
                because: "agents answered about existing cases without a dry run and missed what would change");
            guide.Should().Contain("confirm-running-instances?, confirm-stage-records?, confirm-value-rename?, dry-run }",
                because: "an agent builds its call from the signature, so every flag the tool takes is listed there");
            guide.Should().Contain("`invalid-request` for `enabled` on a case that is not the active version (name the active one the message gives)",
                because: "a member that is not active is changed in place now, and only enabled, which belongs to the whole family, is refused naming the active one");
            guide.Should().NotContain("a new version is not supported yet",
                because: "package-read-only has a remedy now: the change as a new version in an editable package");
            guide.Should().Contain("`package-read-only` (the case's package is not editable: offer the change as a new version in an editable package",
                because: "the package's refusal text points at modify-dynamic-case-as-new-version, and the guide gives the same remedy");
            guide.Should().Contain("or, after their yes, make the change as a new version (\"Versions of a case\"): running cases keep the version they started on",
                because: "a version has no stage-in-use refusal, since running cases stay on the version they started on");
            guide.Should().Contain("pass each flag only after they say yes to it, NEVER on your own",
                because: "every confirmation flag stands for the user's own yes");
            guide.Should().Contain("new records start no case of it (one that fits another enabled case of the object starts that one)",
                because: "a disabled case starts nothing, but a record that fits another enabled case of the object starts that case");
            guide.Should().Contain("its running cases continue, unless another case of the object stays enabled: then each is CANCELLED at its record's next change of the stage or filter column, and the record starts the enabled case it fits, or none",
                because: "measured: the platform cancels a disabled case's running instances when another case of the object is enabled, at a change of the stage column (dev testing X7, round 4 M3) and of the filter column (round 5 M4, 2026-11-26), and the record then starts the enabled case it fits (M4) or none (M3)");
            guide.Should().Contain("goes ALONE (`invalid-request` otherwise)",
                because: "switching a case on or off is a save of its own");
            guide.Should().Contain("`filter-overlap`; ask the user whether to disable that case first or keep this one disabled",
                because: "the platform would start both enabled cases, and which one runs is the user's choice");
            guide.Should().Contain("`settings-conflict` on re-enabling",
                because: "the package checks the settings row before it enables a case");
            guide.Should().Contain("Re-enabling next to another enabled case that would start for the same records: `case-exists` or `filter-overlap`",
                because: "a re-enabled case meets the same coexistence rules as a new one, filters included");
            guide.Should().Contain("A case without a filter while the cases are split by a column: `filter-required` (give it a filter first)",
                because: "re-enabling a case without a filter is refused while the settings row names a filter column");
            guide.Should().Contain("they move them to another stage or wait until the cases end - never change records' stage on your own",
                because: "moving records writes business data and starts what reacts to the save; a stage that holds records is never removed");
            guide.Should().Contain("`failed-leftover`: relay its `leftovers`",
                because: "only the result names what the server could not put back");
            guide.Should().Contain("describe the case before retrying",
                because: "after a lost reply the change may already be written");
            createGuide.Should().Contain("change that case instead (`modify-dynamic-case`)",
                because: "case-exists now has a remedy");
            createGuide.Should().Contain("`successful: false` (final group only",
                because: "only the final group can end a case as unsuccessful, as in the case designer");
            createGuide.Should().MatchRegex($@"GROUPS \(clio {ClioVersion}\+ with CrtProcessBuilder 1\.6\.6\.179\+",
                because: "an older clio or package refuses group and successful on create");
            createGuide.Should().Contain("has no stage until the case is changed (`process-dynamic-case-change`)",
                because: "modify-dynamic-case can add a value the lookup gained later");
        }
    }

    [Test]
    [Description("The change guide is registered, required and routed on its own row, and its catalog entry leads with its version boundary; GuidanceInlineReplyBudgetTests measures both case guides as split articles.")]
    public void ChangeGuide_ShouldBeRegisteredAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement[] resources = [.. source.RootElement.GetProperty("resources").EnumerateArray()];
        JsonElement resource = resources.Single(item => item.GetProperty("itemId").GetString() == ChangeItemId);
        JsonElement createResource = resources.Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string routing = ReadCollapsed(RoutingPath);
        string createGuide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            resource.GetProperty("sourcePath").GetString().Should().Be(ChangeArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ChangeItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("topicId").GetString().Should().Be($"creatio.{ChangeItemId}",
                because: "the topic is the stable cross-library identity of the guide");
            requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString())
                .Should().Contain(ChangeItemId, because: "the library requires every guide it ships");
            resource.GetProperty("description").GetString().Should().MatchRegex($@"^Needs clio {ClioVersion} or later",
                because: "the get-guidance catalog shows the boundary before the article is opened");
            createResource.GetProperty("description").GetString().Should().Contain(ChangeItemId,
                because: "the catalog entry of the create guide must not advertise only create");
            routing.Should().Contain("an object that already has a case: read, change (stages, order, groups), disable or re-enable it",
                because: "a change request read both case guides after the words naming what a change touches were dropped");
            routing.Should().Contain($"disable or re-enable it, make a new version or switch the active one -> name={ChangeItemId}",
                because: "a case version request must reach the case guide, not the process version row");
            resource.GetProperty("description").GetString().Should().NotContain("names the conflicting case",
                because: "settings-conflict and other refusals of the switch name no other case");
            resource.GetProperty("description").GetString().Should().Contain("set-active-dynamic-case-version",
                because: "the catalog entry must show that the guide owns the case version tools");
            routing.Should().Contain($"split its records into cases by type (B2B vs B2C orders) -> name={FiltersItemId}",
                because: "a request for different stages per type must reach the guide that owns filters");
            routing.Should().Contain("set up on an object with no case yet, or only asked about",
                because: "change and disable requests matched both rows and took a hop through the create guide");
            Regex.Matches(routing, $"name={ChangeItemId}\\b").Count.Should().BeGreaterThanOrEqualTo(1,
                because: "routing is what an agent reads first");
            createGuide.Should().Contain($"`{ChangeItemId}`", because: "the create guide hands changes and filters over");
        }
    }

    [Test]
    [Description("The filters guide is registered, required and routed, its catalog entry leads with its version boundary, and the change guide sends filter work to it instead of carrying the rules itself.")]
    public void FiltersGuide_ShouldBeRegisteredAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == FiltersItemId);
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string routing = ReadCollapsed(RoutingPath);
        string changeGuide = ReadCollapsed(ChangeArticlePath);
        string filters = ReadCollapsed(FiltersArticlePath);
        string guide = ProcessGuideSet.Read(repositoryRoot, FiltersArticlePath);

        // Assert
        using (new AssertionScope())
        {
            resource.GetProperty("sourcePath").GetString().Should().Be(FiltersArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{FiltersItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("topicId").GetString().Should().Be($"creatio.{FiltersItemId}",
                because: "the topic is the stable cross-library identity of the guide");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the filter tools ship ungated; a gated guide would be hidden while the tools answer");
            requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString())
                .Should().Contain(FiltersItemId, because: "the library requires every guide it ships");
            requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
                because: "the requirement lists the canonical route next to the item id");
            resource.GetProperty("description").GetString().Should().MatchRegex($@"^Needs clio {ClioVersion} or later\.",
                because: "the get-guidance catalog shows the boundary of the filter arguments before the article is opened");
            guide.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "a banner obliges process-modeling to index the guide, and that article has no room left");
            Regex.Matches(routing, $"name={FiltersItemId}\\b").Count.Should().BeGreaterThanOrEqualTo(1,
                because: "every process article is named in routing, and a split request goes straight to its owner");
            changeGuide.Should().Contain($"read `{FiltersItemId}` first",
                because: "the change guide every case run reads points at the filter rules it no longer carries");
            changeGuide.Should().Contain($"A refusal of these tools whose fix changes a filter: relay it, the user picks the fix, then read `{FiltersItemId}` before that fix's dry run",
                because: "a run met 'read the filters guide first' in the scope and 'relay the refusal' in the step, so the scope names the one order: relay, the user's pick, the read, the dry run");
            changeGuide.Should().Contain($"relay it without describing again, and the user picks the fix; for one that changes a filter, read `{FiltersItemId}` before its dry run",
                because: "the switch step names the same order as the scope, so neither line can be read as reading the filters guide before the refusal is relayed");
            changeGuide.Should().NotContain("a filter refusal whose remedy is a filter change: read",
                because: "that scope line told the agent to read the filters guide before relaying the refusal");
            resource.GetProperty("description").GetString().Should().Contain("or, for another filter refusal, after relaying it and once the user picks a filter change, before its dry run",
                because: "the catalog entry names the same order as the change guide, and keeps the read before the first answer that create's unfiltered-case-exists needs");
            resource.GetProperty("description").GetString().Should().Contain("before the first answer to create-dynamic-case's unfiltered-case-exists",
                because: "the filters guide's first answer to that refusal offers the case designer's two ways, each with its dry run, so it is read before that answer");
            changeGuide.Should().NotContain("== Several cases on one object ==",
                because: "the filter rules have one owner, so they cannot drift apart");
            filters.Should().Contain("Split out of `process-dynamic-case-change`",
                because: "the split article names the entry point that owns describing, changing and every confirmation");
        }
    }

    [Test]
    [Description("A request to switch an object's status flow to a new version reaches the case guide, not the process version guides: a run read process-versions and the library view before it found the case.")]
    public void CaseVersionWording_ShouldNotRouteToProcessVersions()
    {
        // Arrange & Act
        string routing = ReadCollapsed(RoutingPath);
        string versions = ReadCollapsed(VersionsPath);

        // Assert
        using (new AssertionScope())
        {
            routing.Should().Contain($"which one runs (not an object's stages: name={ChangeItemId}), or editing or launching ANY existing process",
                because: "the process version row matched \"switch orders to the new flow\" and sent the agent to the process version model");
            versions.Should().Contain($"Versions of a dynamic case (an object's stages or status flow) are not here: `{ChangeItemId}`.",
                because: "an agent that reached the process version guide anyway is sent back at its first lines");
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
            routing.Should().Contain("or the order records move through its status values / how that flow ends (no \"stage\" needed; not the sort order of a lookup in lists or dropdowns)",
                because: "routing is what an agent reads first, the failing prompts never said stage, and sorting a lookup in a list is not a case");
            routing.Should().Contain("pass the user's word for the object, `find-entity-schema` if not found",
                because: "the package resolves a unique caption and lists candidates, so a schema-name search first only costs calls; it stays the fallback for an older package");
            guide.Should().Contain("the order records move through status values or how that flow ends (not a lookup's sort order in lists or dropdowns)",
                because: "the scope names the wording that belongs to a case, and the one that does not");
            guide.Should().Contain("stages or status flow (\"ends Approved or Rejected\", \"move X before Y\")",
                because: "the read-first rule covers the two prompts that failed");
        }
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

    [Test]
    [Description("The filters guide carries the rules of several cases on one object: one filter column, no shared value, the designer's two ways from one unfiltered case and deactivate-unfiltered only after the user's choice, the refusals with remove-filter, the edit right, values no case is for, and what a filter or a disabled case does to running cases and the bar.")]
    public void FiltersGuide_ShouldCarryTheRulesOfSeveralCases()
    {
        // Arrange & Act
        string guide = ReadCollapsed(FiltersArticlePath);
        int boundary = guide.IndexOf("== Several cases on one object == - Needs clio 8.1.0.154 or later", StringComparison.Ordinal);

        // Assert
        using (new AssertionScope())
        {
            boundary.Should().BeGreaterThanOrEqualTo(0,
                because: "the filter arguments ship with clio 8.1.0.154, and the boundary leads the section it gates");
            boundary.Should().BeLessThan(guide.IndexOf("The platform starts EVERY enabled case", StringComparison.Ordinal),
                because: "an agent on an older clio must meet the boundary before it reads how to split the cases");
            guide.Should().Contain("If the `modify-dynamic-case` contract (`get-tool-contract`) does not list `filter`, the clio serving you is older and has no `filter`, `remove-filter` or `deactivate-unfiltered`: do not send them; tell the user to update clio",
                because: "the boundary says how to tell an older clio and what to do, and an argument is told apart by the contract");
            guide.Should().NotContain("== An older clio or CrtProcessBuilder ==",
                because: "an older clio's rules stay in the change guide, which that clio can read");
            guide.Should().Contain("The platform starts EVERY enabled case whose filter matches a new record",
                because: "that is why two cases must never share a value");
            guide.Should().Contain("split by ONE lookup column, the filter column (never the stage column)",
                because: "the bar binds one filter column per object, as the designer allows");
            guide.Should().Contain("`filter: {column, empty: true}`", because: "records with an empty column can have a case too");
            guide.Should().Contain("`stages` takes any 2 to 20 values of the stage lookup",
                because: "each type has stages of its own");
            guide.Should().Contain("a filtered `create-dynamic-case` is refused `unfiltered-case-exists`",
                because: "the package refuses a filtered case next to an enabled case without one, before any write");
            guide.Should().Contain("In the FIRST answer, before any write, offer the case designer's two ways, each with its dry run, and let the user choose",
                because: "turning the existing case off changes which case new records start; the user decides");
            guide.Should().Contain("(1) keep the existing stages for some values: `modify-dynamic-case { filter }` on that case, then `create-dynamic-case { filter, stages }` for the others; nothing is disabled",
                because: "the designer's first way keeps the existing case for its values and disables nothing");
            guide.Should().Contain("disables that case: new records start only the filtered cases, and each running case of it is CANCELLED at its record's next change of the stage or filter column",
                because: "measured on a stand: after deactivate-unfiltered both running cases of the disabled case were Canceled at their record's next status change, and a change of the filter column alone cancels such a case too (round 5, M4, 2026-11-26)");
            guide.Should().NotContain("next status change",
                because: "a change of the filter column cancels the disabled case's running instance before any status change (round 5, M4, 2026-11-26)");
            guide.Should().Contain("its records start no case, and one without a running case shows no stage bar",
                because: "measured: a record still running a case of a disabled family keeps that case's bar (round 4, M2 control)");
            guide.Should().Contain("A record whose filter value changes keeps its case while that case is enabled",
                because: "measured: a filter-value change cancels a disabled case's running instance when another case of the object is enabled (round 5, M4, 2026-11-26), while an enabled case keeps running");
            guide.Should().Contain("starts the filtered case its value fits, in its current status, or none",
                because: "measured: the B2B record started the B2B case at its new status, the B2C record was left without a case");
            guide.Should().Contain("Say so, with the running count from describe, before the user chooses",
                because: "the user loses those running cases, so the choice of way (2) must be informed");
            guide.Should().Contain("Pass `deactivate-unfiltered` ONLY after the user chose (2), never on your own",
                because: "it disables the case every new record of the object starts today");
            guide.Should().Contain("Undoing (2) (the undo in `process-dynamic-case`) also re-enables that case (cancelled cases stay cancelled): `modify-dynamic-case { enabled: true }`, alone, after the user's yes",
                because: "removing the new case alone would leave the case it disabled switched off");
            guide.Should().Contain("so a case without a filter is then refused `filter-required`: offer it a filter first",
                because: "the filter column the create set stays on the settings row, and no tool removes it yet");
            guide.Should().Contain("Refusals, all before anything is written",
                because: "a refused filter call writes nothing, so it is safe to correct and repeat");
            guide.Should().Contain("`filter-overlap` (another enabled case has one of these values",
                because: "one record never starts two cases");
            guide.Should().Contain("`filter-required` (the cases are split by a column",
                because: "a case without a filter would start for every record next to them");
            guide.Should().Contain("`remove-filter: true` removes the case's filter, but only where the settings row names no filter column",
                because: "removing the last filter column is not supported yet, so remove-filter cannot merge split cases back");
            guide.Should().Contain("it is refused `filter-required`, so do not offer it",
                because: "every filter these tools set also sets the filter column, so the agent must not offer a removal that is always refused");
            guide.Should().Contain("with `filter`: `invalid-request`",
                because: "clio refuses a call that sends a filter and removes it at once");
            guide.Should().Contain("`filter-column-invalid` (not a lookup column of the object, or the stage column)",
                because: "the filter column must be a lookup of the object other than the stage column");
            guide.Should().Contain("right to edit `SysDcmSettings` records",
                because: "the first filter sets the filter column on the case settings with the caller's rights");
            guide.Should().Contain("There is no catch-all case",
                because: "records of a value no case is for start no case, and one without a running case shows no bar");
            guide.Should().Contain("A filter decides only which NEW records start a case: running cases continue",
                because: "a filter added to a case does not move the records already going through it");
            guide.Should().Contain("`askUserToChangeSchema`", because: "the bar's switch offer is a page change the user may want");
            guide.Should().Contain("Answer \"which stages do B2B orders have\" from it",
                because: "a read-only question is answered from describe, changing nothing");
        }
    }

    [Test]
    [Description("The change guide carries the version flow: its own clio boundary, describe of any member, a draft changed in place, a new version made with a dry run and the user's yes and never activated or removed, a separate ask before the switch, what the switch does to running cases and records, a disabled family, package position, and the outcome remedies.")]
    public void ChangeGuide_ShouldCarryTheVersionFlow()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ChangeArticlePath);
        int section = guide.IndexOf("== Versions of a case ==", StringComparison.Ordinal);

        // Assert
        using (new AssertionScope())
        {
            section.Should().BeGreaterThan(guide.IndexOf("Version boundary, before anything else", StringComparison.Ordinal),
                because: "the article-wide boundary still gates the version section, which adds its own");
            guide.Should().MatchRegex(
                $@"== Versions of a case == - Needs clio {ClioVersion} or later: if `clio-run` answers `unknown tool` for `modify-dynamic-case-as-new-version`, the clio is older",
                because: "the version tools ship in a later clio than the change tools, and the boundary leads the section it gates");
            guide.Should().Contain("a missing list there does not mean the case has no other version",
                because: "an older clio drops versions from describe, and an agent must not read that as a family of one");
            guide.Should().Contain("`describe-dynamic-case` takes any member by `case-name` / `case-uid` (one that is not active gets a note naming the active one",
                because: "the package describes any member of the family by name or UId");
            guide.Should().Contain("the list is absent for a case with no other version",
                because: "versions is left out for a family of one");
            guide.Should().Contain("`modify-dynamic-case` by its `case-name` / `case-uid` changes it in place (`case.activeVersion: false`, a note)",
                because: "a draft is edited in place, and new records do not see the edit until the draft is active");
            guide.Should().Contain("on a member that is not active it is refused `invalid-request`; send it for the active one",
                because: "enabled belongs to the whole family, so the package refuses it on a member that is not active");
            guide.Should().Contain("`modify-dynamic-case-as-new-version { environment-name, case-name | case-uid | entity-schema-name, package-name?, stages?, caption?, filter?, remove-filter?, confirm-stage-records?, confirm-value-rename?, dry-run? }`",
                because: "an agent builds its call from the signature, and the tool takes neither enabled nor confirm-running-instances");
            guide.Should().Contain("copies the family's ACTIVE member (another member is refused, naming the active one) into a new version that is NOT active",
                because: "only the active member is copied, and creating never activates");
            guide.Should().Contain("otherwise refused `package-name-required`: ask the user which editable package",
                because: "the version goes to the source's package only when it is editable, and the target package is the user's choice");
            guide.Should().Contain("First `dry-run: true` (`planned`): show the user `diff`, the notes and the warnings, write only after their yes",
                because: "every write makes one more version that no tool removes");
            guide.Should().Contain("NOT idempotent: every write makes one more version, and the toolkit cannot delete a version",
                because: "a saved version is kept even when the call fails after the save, and no clio tool removes one");
            guide.Should().Contain("it is saved inactive, so new records keep starting the active case until a switch (a confirmed value rename has acted already, below), and a later change to it is `modify-dynamic-case` by its `case-name`, in place (no further version)",
                because: "a run presented a new version as irreversible, while new records keep starting the active case until a switch and a member that is not active is changed in place; a confirmed value rename is written before the save, so the version is not free of effect");
            guide.Should().Contain("say so before the yes; activating then changes no stage name on the bar or in the designer",
                because: "a run gave the rename as a reason to activate, while the bar and the designer show the value's name, renamed at the write (dev testing A6); other surfaces were not measured");
            guide.Should().Contain("A stage value rename (`confirm-value-rename`) acts AT ONCE, not at activation: it renames the value for the active case and every record too - say so before the yes",
                because: "a lookup value rename is written at once, not at activation");
            guide.Should().Contain("so it is refused `not-in-version` (set the filter on the active case with `modify-dynamic-case` first, or leave the version without one)",
                because: "a first filter sets the object-wide filter column, which would split the live cases at once");
            guide.Should().Contain("the version was saved and is KEPT, named in `leftovers` with its link row - do not activate it",
                because: "a failure after the save keeps the version, and it may read back wrong");
            guide.Should().Contain("do not activate it; relay `leftovers` in full (a value left renamed stays renamed: offer to rename it back) and describe the family before telling the user what changed; no tool removes the version;",
                because: "a confirmed value rename stays on failed-leftover and is listed in leftovers, and this outcome is also reached when the saved version reads as active or the save flipped the family's enabled flag, so the agent reads the family before saying what changed; the toolkit has no case deletion");
            guide.Should().NotContain("in the case library",
                because: "the case library hides Delete on its rows and on its Versions tab (VwDcmLibSection, ProcessVersionsDetail.getDeleteRecordMenuItem), so the guide names no deletion route there");
            guide.Should().Contain("After `failed-leftover` or `unknown`, describe the family before any retry: a retry makes another version",
                because: "a retry after a saved or possibly saved version adds one more version");
            guide.Should().Contain("`entity-schema-name`: the schema name or the user's word (a unique caption is resolved). `entity-not-found` or several matches list `Name (caption)` candidates: pick one or ask. None listed (older package): `find-entity-schema`",
                because: "the package resolves a unique caption, so a find-entity-schema call first only adds a call; candidates replace the search, which stays for an older package");
            guide.Should().Contain("Asked for the switch (\"switch orders to it\")? That is the yes: notes only - switch, relay every note in full with the result; warnings - show them, wait for the yes. Not asked: step 3",
                because: "the owner decided that a request to switch is the consent after a dry run without warnings, while a version only created still needs its own question; a run that condensed a note dropped its sentences about the records they applied to");
            guide.Should().Contain("Tell the user what the switch does, for the records that fit the version's filter (if any): new ones start it once the family is enabled (unless Package position, below, applies)",
                because: "a disabled family starts nothing whichever member is active, and a filtered version starts only for the records its filter is for (a run told the user every new record starts the new version)");
            guide.Should().Contain("At most one member is ACTIVE: while the family is enabled, new records start that one (if it has a filter, only those it is for; Package position, below, is the exception;",
                because: "the version model names the active member as the one new records start, which holds only while the family is enabled, only for the values of its filter, and only where no member outranks it by package position");
            guide.Should().NotContain("new records start the active version",
                because: "that sentence holds only for an enabled family and for the records the version's filter is for");
            guide.Should().Contain("`version-not-found`: describe the case by its object (`entity-schema-name`) and name the family's `versions` to the user; never guess a name",
                because: "a run that left for find-app after version-not-found never named the existing versions");
            guide.Should().Contain("give the user its `designerUrl`",
                because: "the user checks the version in the case designer before deciding to activate it");
            guide.Should().Contain("ASK the user whether to make it active: a decision of its own, never part of the yes to create it",
                because: "the case designer asks separately whether a new version becomes actual, and the tool never activates");
            guide.Should().Contain("`set-active-dynamic-case-version { environment-name, version-name | version-uid, confirm-package-position?, dry-run? }`, destructive",
                because: "the switch changes what new records start");
            guide.Should().Contain("`unchanged`: it is active already, nothing written",
                because: "the switch is idempotent, so a repeat is answered unchanged");
            guide.Should().Contain("Refused before any write: `settings-conflict`, `case-exists`, `filter-overlap`, `unfiltered-case-exists`, `filter-required`",
                because: "activation runs the case library's coexistence checks before it writes");
            guide.Should().Contain("First `dry-run: true`; do not describe that member to prepare the switch (step 2's check aside)",
                because: "a run described the target before a dry run that answered the question, and step 2's check still describes a new version");
            guide.Should().Contain("the message says what conflicts and, except for `settings-conflict` (the case designer settles it), how to fix it; relay it without describing again",
                because: "a run re-described the target after a filter-overlap refusal that named the case and the value, and settings-conflict names no case");
            guide.Should().NotContain("names the conflicting case",
                because: "settings-conflict and the first filter-required branch name no other case");
            guide.Should().Contain("a running case keeps its version while it runs, and Change case on the record's page cancels it and starts the enabled case the record fits, if any (with this family enabled and the record fitting it: the new version, from its first stage, written into the record); open pages need F5",
                because: "a switch never migrates running cases, and Change case starts only an enabled case the record fits (measured)");
            guide.Should().Contain("while the family is enabled, one with no running case shows the version at once (no current stage if it holds a stage value the version lacks)",
                because: "measured: a record without a running case follows the active version at the switch itself while the family is enabled, and shows no bar of it while it is disabled (round 4, M2)");
            guide.Should().Contain("never \"the records show no current stage\"",
                because: "only records without a running case lose their current stage");
            guide.Should().Contain("Change case cancels the record's running case and starts only an enabled case the record fits, never a version of this family",
                because: "the package's note: a disabled family is never started by Change case");
            guide.Should().Contain("the family's running cases are cancelled at their record's next change of the stage or filter column, and the record then starts the enabled case it fits, or none",
                because: "measured: a change of the stage column (round 4, M3) or of the filter column (round 5, M4, 2026-11-26) cancels a disabled family's running case while another case of the object is enabled, and the record then starts the enabled case it fits (M4) or none (M3)");
            guide.Should().Contain("Until then a record without a running case shows no bar of it (an enabled case it fits shows its own);",
                because: "a disabled family draws no bar for a caseless record, and Change case starts none of its versions (measured, round 4 M2); that another enabled case the record fits draws its own bar follows from the bar's source rule (the record's running case draws it) and was not measured");
            guide.Should().Contain("A disabled family is switched too, and starts nothing until `modify-dynamic-case { enabled: true }` on the new active member, alone, after the user's yes",
                because: "activation does not change enabled, so a disabled family stays silent after the switch");
            guide.Should().Contain("may not become the one new records start",
                because: "a member in a non-editable package is never written False, and the platform's pick then falls to the package rank");
            guide.Should().Contain("The switch is then refused `package-position-confirmation-required`",
                because: "a switch a foreign member may outrank is refused until the user's yes, like the other confirmations");
            guide.Should().Contain("the remedy is a dependency of the version's package on that member's package, or `confirm-package-position: true` to switch anyway, each only after the user's yes to it",
                because: "a dependency ranks the version's package higher, switching anyway may leave new records on a member nobody chose, and both are the user's to approve");
            guide.Should().Contain("it does not travel with any package (a note); tell the user",
                because: "a switch to a member in a non-editable package is kept as a setting of this environment only");
            guide.Should().Contain("it is never undone, and the same call can be repeated once the cause is fixed",
                because: "a switch that did not read back is reported, not restored, and the call is idempotent");
            guide.Should().NotContain("DcmCasesVersioning",
                because: "only the shipped default of the feature is supported, so the guide says nothing about it");
        }
    }

    [Test]
    [Description("The create guide keeps the precondition of deactivate-unfiltered next to the flag, states what makes a filtered re-run already-present, undoes what a deactivating create disabled, and leaves the filter rules to the filters guide.")]
    public void CreateGuide_ShouldKeepTheFilterPreconditionsAndOneOwner()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("`deactivate-unfiltered` disables the object's unfiltered case: pass it only after the user chose that, never on your own",
                because: "a split must not separate a destructive flag from its precondition, and agents reach this guide first");
            guide.Should().Contain("`case-exists`: the object already has an enabled case without a filter that is not this call's equivalent",
                because: "a case next to filtered ones is answered by the filter refusals, not case-exists");
            guide.Should().Contain("is `already-present` only with the SAME object, column, stages in order, groups and filter (a call without a filter matches only a case without one)",
                because: "the package's equivalence check compares the stage column, the stages in order, the groups, the filter and the settings link");
            guide.Should().Contain("or the cases are split by another filter column",
                because: "a filtered create can meet settings split by another column, and without the cause an agent just retries");
            guide.Should().Contain("After `deactivatedCases`, also re-enable those cases (`modify-dynamic-case { enabled: true }`, alone, after the user's yes); the filter column the call set stays: `process-dynamic-case-filters`",
                because: "undoing only the new case would leave the case it disabled switched off, and the filter column cannot be removed");
            guide.Should().Contain("(a `filter` needs one more: `process-dynamic-case-filters`)",
                because: "the filter rules have one owner; this guide only names it");
            guide.Should().Contain("several cases on one object split by a filter: `process-dynamic-case-filters`",
                because: "the scope sends a split straight to its owner instead of through the change guide");
            guide.Should().NotContain("There is no catch-all case",
                because: "the filter rules have one owner, process-dynamic-case-filters, so they cannot drift apart");
            guide.Should().NotContain("right to edit `SysDcmSettings`",
                because: "the edit right a filter needs is owned by process-dynamic-case-filters");
        }
    }

    [Test]
    [Description("The change guide states what a stage's steps do to a change: a removed stage takes its steps and the dry run names them, a removal is refused only for a step condition, and a new version copies the source's steps and refuses steps sent with it.")]
    public void ChangeGuide_ShouldCarryTheStepRulesOfAStageChangeAndAVersion()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ChangeArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("a stage left out is REMOVED with its steps (the dry run's `diff.steps` names them: relay them before the yes)",
                because: "the package removes a stage together with its steps and names them in the dry run, so the user must hear which work goes before the yes");
            guide.Should().Contain("`stage-is-condition-target` (a step condition moves records to that stage) and `step-has-conditions` (a step of that stage has conditions)",
                because: "these are the only two refusals of a stage removal that the steps cause");
            guide.Should().NotContain("stage-has-steps",
                because: "the package no longer refuses a stage only because it has steps");
            guide.Should().Contain("a version copies the source's steps as they are, so steps or `transitionType` sent with it are refused: change them on the version afterwards (`modify-dynamic-case` by its `case-name`, or the case designer)",
                because: "steps-not-supported on a version is answered by changing the version in place once it exists");
            guide.Should().Contain("a case with steps gets a version only in its own package",
                because: "a version in another package would miss the steps' own schemas, so the package refuses it");
        }
    }
}

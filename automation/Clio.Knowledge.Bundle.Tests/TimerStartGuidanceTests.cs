using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-timer-start</c> guide: the <c>timer</c> block of the Timer start element, the <c>setTimer</c>
/// operation and the confirmation owed before a save that schedules runs. It is registered as
/// <c>process-web-service</c> is - banner-less, ungated, no legacy route, loaded on demand - and reached from the element
/// catalog, its details, process-modeling and routing. It stays OFF the process digest card, as <c>webService</c> does.
/// </summary>
/// <remarks>
/// TRIM ALARMS: every clause below is a gate an agent must pass, a refusal it has to recognise, or a run-time fact it
/// cannot see from a green build (a saved timer runs unattended as the System operations user, with no parameters; a
/// day step restarts every month; a kept past start can fire at once). A reword that keeps the rule updates the clause
/// here; a trim that drops it is the failure these tests exist for.
/// </remarks>
[TestFixture]
public sealed class TimerStartGuidanceTests
{
    /// <summary>
    /// The floor sentence, in the form <c>process-multi-start</c> uses. It names no version: the clio side of the gate is
    /// the FULL create contract naming <c>timerStart</c>, and that clio refuses an older package up front. A package or
    /// clio version is added here and in the article together once it is released; a placeholder never is.
    /// </summary>
    private const string FloorSentence =
        "Gate: use `timerStart` and `setTimer` only when the `create-business-process` contract (`get-tool-contract` with `tool-names: [\"create-business-process\"]`) names `timerStart` - that clio refuses an older package up front. The short reply names it in its SHAPE line, so read `detail: \"full\"` only when the short one does not. Otherwise this clio does not carry it: build no timer start, and tell the user that a clio update adds it.";

    /// <summary>
    /// The confirm-before-save gate, worded on the trigger rather than on a list of tools, so an action nobody listed
    /// is still covered.
    /// </summary>
    private const string GateTrigger =
        "Before any action after which a timer that did not fire before, or now fires on a different schedule or zone, sits in the enabled, active version of a process, you MUST state the following and get an explicit yes BEFORE you call the tool";

    /// <summary>
    /// The wording the gate shares with the confirm-first rule of <c>process-delete-data</c>. It is part of
    /// <see cref="GateTrigger"/>, so it is asserted against delete-data only: the gate's copy is pinned by the trigger.
    /// </summary>
    private const string ConfirmFirstWording = "and get an explicit yes BEFORE you call";

    private const string ItemId = "process-timer-start";

    private const string ArticlePath = "guidance/mcp/guides/processes/timer-start.md";

    private const string CatalogPath = "guidance/mcp/guides/processes/element-catalog.md";

    private const string CatalogDetailsPath = "guidance/mcp/guides/processes/element-catalog-details.md";

    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";

    private const string ConnectionsDetailsPath = "guidance/mcp/guides/processes/activity-connections-details.md";

    private const string DeleteDataPath = "guidance/mcp/guides/processes/delete-data.md";

    private const string DigestPath = "guidance/mcp/guides/processes/digest.md";

    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    private const string VersionWritesPath = "guidance/mcp/guides/processes/version-writes.md";

    /// <summary>
    /// The one exemption from the gate. Pinned whole, and asserted to be the only one: a second "needs no"
    /// anywhere in the article is a widened gate whatever it is worded as.
    /// </summary>
    private const string Exemption =
        "An edit that leaves a timer whose `state` is `scheduled` unchanged (a caption, a label) needs no schedule confirmation; other confirm gates still apply: a change to what a scheduled run writes is an in-place edit of the active version (`process-modeling`), and `process-delete-data` keeps its own.";

    /// <summary>
    /// This guide's design target, in the response characters of <see cref="ProcessGuideResponseSizeTests"/>. The guide
    /// is about 21,500 response characters: the fact-check round added the run's system access, the misfire behaviour,
    /// the recovery paths and the year and cron limits, and was reviewed rather than absorbed; the second review round
    /// (the run's missing parameters in the confirmation, the activation's notice and refusal, the every-N-weeks
    /// alternatives) was paid for by trims. A new clause pays for
    /// itself with a trim; a larger addition is split into a <c>-details</c> article. The 19,000-byte inline rule is
    /// asserted separately.
    /// </summary>
    private const int DesignTargetResponseCharacters = 21_500;

    [Test]
    [Description("The guide is registered banner-less, ungated and without a legacy route, required by the library, its description opens with the contract gate, and the element catalog, its details, process-modeling and routing send the reader to it while the digest card does not list it.")]
    public void Guide_ShouldBeRegistered_AndTheEntryArticlesShouldRouteToIt()
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
        string catalog = ReadCollapsed(CatalogPath);
        string details = ReadCollapsed(CatalogDetailsPath);
        string modeling = ReadCollapsed(ModelingPath);
        string routing = ReadCollapsed(RoutingPath);
        string card = ReadCollapsed(DigestPath);
        string versionWrites = ReadCollapsed(VersionWritesPath);

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
            resource.GetProperty("description").GetString().Should().StartWith(
                "Only when the create-business-process contract names timerStart (the short reply names it).",
                because: "a boundary that gates the whole article comes first, so the get-guidance catalog shows it before the article is opened");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the clio side is gated by the create contract, not by a feature; a gated guide would be hidden while the tool accepts the block");
            resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
                because: "the guide was born after the v0 migration and has no v0 route to preserve");
            requiredItems.Should().Contain(ItemId,
                because: "the library requires every item it publishes for the process tools");
            requiredUris.Should().Contain($"docs://knowledge/com.creatio.clio/{ItemId}",
                because: "the requirement lists the canonical route next to the item id");
            raw.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "the guide is loaded on demand; a banner obliges process-modeling to index it, and that article has no room left under the inline rule");

            catalog.Should().Contain("`timerStart` (schedule, only when the create contract names it; `process-timer-start` owns its `timer` block and the confirmation it needs)",
                because: "the catalog owns what builds today, and the element it lists must carry its contract gate, its owner and the confirmation it needs");
            catalog.Should().Contain("`startEventTimer`->`timerStart`",
                because: "the data-id map turns the catalog's Start timer into the build type");
            catalog.Should().Contain("filter, signal, timer or connections with setFilter / setSignal / setTimer / setConnections, not in `setElement`",
                because: "a timer is written with setTimer; setElement refuses a timer key");
            details.Should().Contain("`startEventTimer` Start timer (schedule/CRON; BUILDABLE via `type:\"timerStart\"` with a `timer` block, only when the create contract names it; owned by `process-timer-start`)",
                because: "the element entry names the build type, its contract gate and the owner of its block");
            details.Should().Contain("a timer start inside an event sub-process is not handled at all",
                because: "only top-level timer starts are built and described; a nested one is out of reach of every tool");
            modeling.Should().Contain("setTimer (`process-timer-start`)",
                because: "process-modeling lists every edit op and names the owner of each that carries its own rules");
            routing.Should().Contain("- Creatio work ON A SCHEDULE or at a set time -> a timer-start process: name=process-digest, then name=process-timer-start. NEVER your client's own cron or scheduler tool",
                because: "a measured agent with the routing guide already read answered 'every 5 minutes add a note' with its own client's cron tool, which runs nothing in Creatio");
            routing.IndexOf("Creatio work ON A SCHEDULE", StringComparison.Ordinal).Should().BeLessThan(
                routing.IndexOf("- Business processes (BPMN)", StringComparison.Ordinal),
                because: "the scheduling row is a top-level domain row met before the process section, since the request does not say 'process'");
            versionWrites.Should().Contain("Activating a version that carries a timer, a rollback included, or enabling a disabled process that has one, owes the confirmation of `process-timer-start` first.",
                because: "an agent activating a version follows routing to process-version-writes, so the schedule confirmation the timer article postpones to activation must be pointed at from there");
            card.Should().NotContain("timerStart",
                because: "the timer start stays off the digest card; agents reach it through the create contract, the catalog and routing");
            card.Should().NotContain(ItemId,
                because: "the card's budget is unchanged by this element; ProcessDigestTests holds the budget itself");
        }
    }

    [Test]
    [Description("The guide states the floor sentence in the process-multi-start form, with no version placeholder in the article or its description.")]
    public void Guide_ShouldStateTheFloor_WithoutAPlaceholder()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        Regex placeholder = new(@"<[A-Z][A-Z0-9-]*TBD>", RegexOptions.Compiled);

        // Act
        string raw = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        string guide = Collapse(raw);
        string description = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId)
            .GetProperty("description").GetString()!;
        int scope = guide.IndexOf("Scope", StringComparison.Ordinal);
        int gate = guide.IndexOf(FloorSentence, StringComparison.Ordinal);
        int firstSection = guide.IndexOf("== ", StringComparison.Ordinal);

        // Assert
        using (new AssertionScope())
        {
            gate.Should().BeGreaterThan(scope,
                because: "the floor sentence follows the Scope paragraph, in the form process-multi-start uses");
            gate.Should().BeLessThan(firstSection,
                because: "a boundary that gates the whole article is met before anything else is read");
            placeholder.IsMatch(raw).Should().BeFalse(
                because: "a published body never carries an unresolved version boundary; merging publishes");
            placeholder.IsMatch(description).Should().BeFalse(
                because: "the manifest description is published too");
            guide.Should().Contain("Read the element back from the write's own summary: a create or a modify answers with `startTimers` (schedule, stored cron, `start`, zone, scheduler state, next fire) read from the saved process, so do not run a describe after it for that.",
                because: "the write's summary is a describe of the saved process, so a describe right after it only repeats the read");
            guide.Should().Contain("When it is absent (a process that calls a sub-process, a failed read-back), for `end` and `repeatOnMisfire`, and after an enable, run `describe-business-process element=<name>` (`mode=full` before a replace).",
                because: "clio returns no summary for a process that calls a sub-process or whose read-back failed, the summary omits the end and misfire, and an enable returns no timer summary of its own");
        }
    }

    [Test]
    [Description("The confirm-before-save gate is worded on the trigger, shares the confirm-first wording of process-delete-data, lists every action it covers and what the user is told, says what to do when the notice differs or never comes, exempts exactly one edit, and the existing-process section points back to it.")]
    public void Guide_ShouldGateEverySaveThatSchedulesRuns()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string deleteData = ReadCollapsed(DeleteDataPath);
        string[] coveredActions =
        [
            "a create with a `timerStart`",
            "adding a `timerStart` to an existing process (next to the Simple start, or to a timer-only process)",
            "the Simple -> Timer replace",
            "a `setTimer`, the re-save below included",
            "`set-active-business-process-version` on a version that carries a timer (one added through `modify-business-process-as-new-version` fires only from then)",
            "enabling a disabled process that carries a timer"
        ];
        string[] statedToTheUser =
        [
            "the schedule in words (a step's real cadence)",
            "the zone you will send as `timeZone`",
            "the first run as your estimate (it may be immediate, above)",
            "that runs use the System operations user with system access (no permission checks) and get no process parameters or record Id",
            "which data the process writes or deletes"
        ];

        // Act
        string gateSection = Regex.Match(guide, @"== Confirm before a save that schedules runs ==(?<body>.+?)== ").Groups["body"].Value;
        string[] missingActions = [.. coveredActions.Where(action => !gateSection.Contains(action, StringComparison.Ordinal))];
        string[] missingStatements = [.. statedToTheUser.Where(item => !gateSection.Contains(item, StringComparison.Ordinal))];
        int exemptions = Regex.Matches(guide, @"needs no|no confirmation|without (?:a |the )?confirm", RegexOptions.IgnoreCase).Count;
        string existingProcess = Regex.Match(guide, @"== A timer on an existing process ==(?<body>.+?)== ").Groups["body"].Value;

        // Assert
        using (new AssertionScope())
        {
            gateSection.Should().NotBeEmpty(because: "anti-vacuity: the gate has its own section");
            gateSection.Should().Contain("STOP AND CONFIRM FIRST.",
                because: "the gate opens the way the other confirm-first rules of the process set do");
            gateSection.Should().Contain(GateTrigger,
                because: "the trigger covers an action nobody listed: what counts is a new or changed schedule landing in the enabled, active version");
            deleteData.Should().Contain(ConfirmFirstWording,
                because: "the wording this gate shares must still be the delete-data wording, or the two rules have drifted apart");
            missingActions.Should().BeEmpty(
                because: "each of these puts a new or changed schedule into the active version. Missing: " + string.Join(" | ", missingActions));
            missingStatements.Should().BeEmpty(
                because: "the user approves a schedule they can picture: when, in which zone, first when, with whose rights, on which data. Missing: "
                    + string.Join(" | ", missingStatements));
            gateSection.Should().Contain("Then read the post-save notice and pass it on.",
                because: "the notice is the only place the saved schedule's next run, or the reason there is none, is reported");
            gateSection.Should().Contain("tell the user at once and offer to revert",
                because: "a notice that differs from what the user approved means the user approved something else");
            gateSection.Should().Contain("An activation answers with the same notice and `startTimers`; an enable answers with none: read `scheduled` from `describe-business-process element=<name>` instead.",
                because: "an agent waiting for a notice that never comes reports nothing, and one that ignores the activation's notice misses a differing schedule");
            gateSection.Should().Contain(Exemption,
                because: "the exemption is narrow and does not lift any other confirmation the edit owes");
            exemptions.Should().Be(1,
                because: "the exemption above is the only one; a second 'needs no' or 'no confirmation' anywhere in the article widens the gate, however it is worded");
            existingProcess.Should().Contain("(the confirmation above applies)",
                because: "adding a timer to an existing process is the commonest way to schedule runs, and the pointer back to the gate is what stops it being treated as an ordinary edit");
        }
    }

    [Test]
    [Description("Replacing the Simple start is gated on the user asking for it, a describe, a SysSubProcessInProcess caller query and asking the user about case, code and manual starts; adding a timer next to it, after checking what the steps read, is the default.")]
    public void Guide_ShouldGateTheReplace_OnTheCallerCheck()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string section = Regex.Match(guide, @"== A timer on an existing process ==(?<body>.+?)== setTimer ==").Groups["body"].Value;

        // Assert
        using (new AssertionScope())
        {
            section.Should().NotBeEmpty(because: "anti-vacuity: the existing-process section is where the replace rule lives");
            section.Should().Contain("By default ADD it: `addElement` a `timerStart` and `addFlow` from it to the first element, next to the existing start, in one request (the confirmation above applies).",
                because: "adding keeps every manual start and every caller working, so it is the default");
            section.Should().Contain("First check what the steps read: a parameter or a start's record Id is empty on a timer run, so set it on the timer branch (`process-parameters`) or ask the user.",
                because: "a timer run carries no parameter and no record Id, and process-parameters owns how to set one on a start's branch");
            section.Should().Contain("REPLACE the Simple start only when the user asks for a timer-only process, in one request",
                because: "replacing is never the agent's own idea");
            section.Should().Contain("and only after a `describe-business-process mode=full`, the caller check below and the user's confirmation",
                because: "a timer-only process loses every manual start and every caller, and the save does not say so");
            section.Should().Contain("The table holds process callers only: a case (DCM) step that runs the process, or code that starts it, is not in it - ask about those too",
                because: "zero rows does not mean no case or code runs the process");
            section.Should().Contain("one `execute-esq` on `SysSubProcessInProcess` by the family root (`versionRootSchemaUId` from describe); any row is a caller",
                because: "SysSubProcessInProcess is the table behind the process library's used-as-a-sub-process detail");
            section.Should().Contain("rootSchemaName: \"SysSubProcessInProcess\"",
                because: "the literal query is what the agent runs; a paraphrase is where the filter column goes wrong");
            section.Should().Contain("columnPath: \"ParentSubProcessUId\"",
                because: "the callee is matched by its family root in ParentSubProcessUId");
            section.Should().Contain("is reported to the user, never taken as \"no callers\"",
                because: "the read needs an admin operation, and a refused read is not an empty one");
            section.Should().Contain("`VwProcessLib.AddToRunButton` of the process (`execute-esq`) shows a Run process menu entry",
                because: "the Run process menu is the one manual start a query can find");
            section.Should().Contain("both are partial checks, never proof of absence, so ASK the user whether anything starts the process by hand",
                because: "page buttons, classic pages, dashboards, integrations and run-process calls leave no index to query");
        }
    }

    [Test]
    [Description("The guide states the run-time facts, the step semantics, the every-N-weeks refusal, the zone default chain and the read-back rules an agent cannot see from a green build.")]
    public void Guide_ShouldStateWhatAGreenBuildDoesNotShow()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string[] clauses =
        [
            "It runs as the System operations user (the `SystemUser` system setting, Supervisor by default)",
            "with SYSTEM ACCESS: object, column, record and operation permissions are not checked, whatever roles that user has.",
            "It receives NO process parameters and no record Id: each parameter starts at its default value.",
            "A Simple start next to it keeps both (on a new process add one only if the user wants that)",
            "so it does not reproduce a scheduled run",
            "Prefer ONE timer that reads the due records and handles each",
            "The block builds nothing finer than every minute, and the platform sets no minimum interval and no cap on runs",
            "`schedule` is required on create and `addElement` (`setTimer`: below)",
            "a `start`, the first fire and a `cron` year must not fall after 2099 (an `end` may)",
            "(0..23, BOTH hours inclusive, default 0..23; it may wrap, 22..6, computed but not run on a stand)",
            "(1..31, no `\"last\"`)",
            "A 5-field Unix cron is refused, and so is a day-of-month `nW` above `28W`.",
            "(`0 0 7 1 2/3 ? *`: every 3 months from February)",
            "\"Every N\" is a cron step, not an interval",
            "hours from `fromHour` each day (9..18 every 5: 09:00 and 14:00; 22..6 every 3: 22:00, 01:00, 04:00)",
            "Every step above 1 gets a notice naming the real cadence, but only after the save: work the cadence out yourself for the confirmation",
            "Every N WEEKS is not supported by the platform scheduler: `every` on `week` is refused",
            "a `day` step of 7, 14, 21 or 28 and a `cron` whose day-of-month field is a step that is a multiple of 7 (`0 0 8 */14 * ? *`)",
            "Do not build a substitute",
            "A designer-built timer with such a day step is described as it is; a rebuild from that describe, and a `setTimer` that resends that step, are refused; a `setTimer` with another schedule group replaces it, and one without the schedule group keeps it.",
            "with no offset, `Z` or milliseconds",
            "the schedule must fire at least once at or before it",
            "A `start`, `end` or `once` time in the zone's skipped spring-forward hour is refused",
            "A timer that keeps a past `start` may run ONCE immediately when it is registered afresh - when the process is re-enabled, or when it had no job.",
            "your profile's zone, else the `DefaultTimeZone` system setting, else refused",
            "When the user did not name a zone, ASK and send `timeZone`: the clio login may be a technical user",
            "A `setTimer` without `timeZone` keeps the stored zone",
            "changes nothing at run time: with either value, a fire missed while the scheduler was down runs once as soon as it is back, then the schedule continues. Tell the user that.",
            "The schedule group is optional (required while no complete schedule is stored), and REPLACED when sent",
            "with `schedule` required and an omitted `at` back at `00:00`; nothing is merged with the stored one. To change only the zone or the dates, send no schedule field.",
            "An empty `timer {}` re-saves the timer unchanged: that is the re-save this article asks for.",
            "From describe: leave out an unchanged stored past `start`.",
            "A date you send (no `Z`, no milliseconds) equal to the stored one to the second counts as unchanged",
            "a `timer` key in `setElement` (use `setTimer`)",
            "`useBackgroundMode: true` (`false`, as describe reports it, passes)",
            "a `once` always reads as `once`, a stray cron only in `storedCron`",
            "a kind named in `designerKind` comes back as Other frequency, and a never-configured timer is refused until a schedule is sent",
            "`staleTrigger` - the stored trigger differs from the timer, belongs to another version, or is paused or errored: re-save the active version",
            "`noJob` - normal for a `once` whose time has passed or a recurring timer past its `end` (it has finished), otherwise re-save",
            "A re-save moves a stored cron trigger to its next fire; only a timer with no trigger is registered afresh, and a kept past `start` may then run once at once.",
            "the edit IS saved; do NOT resend the original operations.",
            "If the message says the scheduler refused the timer, fix the timer (confirming again) and re-save it with `setTimer`",
            "if it says the scheduler itself failed, re-save later with `setTimer` sending the same values",
            ".NET 8 is not measured",
            "neither built, described nor found by `setTimer`"
        ];

        // Act
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "none of these is visible from a green build. Missing: " + string.Join(" | ", missing));
    }

    [Test]
    [Description("No process article and no manifest description still claims, in any letter case, that the timer start is read-only or cannot be built.")]
    public void NoProcessArticleOrManifestDescription_ShouldClaimTimerStartIsUnbuildable()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string catalog = ReadCollapsed(CatalogPath);
        string details = ReadCollapsed(CatalogDetailsPath);
        string modeling = ReadCollapsed(ModelingPath);
        string connections = ReadCollapsed(ConnectionsDetailsPath);

        // Act
        string catalogDescription = Collapse(source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-element-catalog")
            .GetProperty("description").GetString()!);
        string readOnlyInDescription = Regex.Match(catalogDescription, @"while (?<list>[^.]+?) remain read-only").Groups["list"].Value;
        string notBuildable = Regex.Match(catalog, @"NOT yet buildable(?<list>.+?)== Building from a data-id").Groups["list"].Value;
        string readOnlyInDetails = Regex.Match(details, @"`endEvent` builds Terminate today \(see N6 in `process-naming`\)\. (?<sentence>[^.]+?) are READ-ONLY here").Groups["sentence"].Value;
        string cannotCreate = Regex.Match(modeling, @"Constructs the builder cannot create \((?<list>[^)]+)\)").Groups["list"].Value;
        string notInSlice = Regex.Match(connections, @"are all in that slice now; (?<list>.+?) are not\.").Groups["list"].Value;
        string[] stale = [.. ProcessGuideSet.Declared(repositoryRoot)
            .Where(article => ReadCollapsed(article.SourcePath).Contains("timer/message", StringComparison.OrdinalIgnoreCase))
            .Select(article => article.ItemId)];

        // Assert
        using (new AssertionScope())
        {
            notBuildable.Should().NotBeEmpty(because: "anti-vacuity: the catalog lists what does not build");
            notBuildable.Should().NotContainEquivalentOf("timer", because: "the timer start is buildable now");
            notBuildable.Should().Contain("message start", because: "the message start is still not buildable");
            readOnlyInDetails.Should().NotBeEmpty(because: "anti-vacuity: the details catalog names what is read-only");
            readOnlyInDetails.Should().NotContainEquivalentOf("timer start", because: "the read-only marker would now be a lie");
            cannotCreate.Should().NotBeEmpty(because: "anti-vacuity: process-modeling lists what the builder cannot create");
            cannotCreate.Should().NotContainEquivalentOf("timer", because: "the builder creates a timer start now");
            notInSlice.Should().NotBeEmpty(because: "anti-vacuity: the R1-R20 heading lists what is outside the buildable slice");
            notInSlice.Should().NotContainEquivalentOf("timer", because: "a validated timer start is buildable now");
            readOnlyInDescription.Should().NotBeEmpty(because: "anti-vacuity: the description lists what remains read-only");
            readOnlyInDescription.Should().NotContainEquivalentOf("timer",
                because: "a manifest description is what an MCP client shows an agent choosing a guide");
            catalogDescription.Should().Contain("Timer start: timerStart, contract-gated; owner process-timer-start",
                because: "the description lists what builds today, with the guide's contract gate, and names the owner");
            stale.Should().BeEmpty(
                because: "\"timer/message\" is the shape every not-buildable list used before the timer start was buildable. Found in: "
                    + string.Join(", ", stale));
        }
    }

    [Test]
    [Description("The guide stays inside its design target and the inline reply rule.")]
    public void Guide_ShouldStayInsideItsSizeTargets()
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

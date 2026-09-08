using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Content pins for the <c>process-versions</c> article (ENG-94374).
///
/// The article exists because an agent that assumes the ordinary "one schema, many revisions" shape
/// explains and edits a graph nobody runs. Each fact pinned below is one an agent has to be told and
/// cannot derive, so losing it in an edit is the failure this fixture exists to catch.
///
/// Every pin here reads WHITESPACE-COLLAPSED text and collapses its own literal with the same helper.
/// The articles are hard-wrapped, so a phrase a reflow moves across a line break is a phrase a raw
/// <c>Contain</c> stops finding — and a raw <c>NotContain</c> then passes while scanning nothing, which
/// is the incident <c>ProcessGuideCrossReferenceTests</c> records against its own markers. Collapsing
/// removes that class outright, so a pin can be as long as the fact needs rather than as short as the
/// line width allows.
/// </summary>
[TestFixture]
public sealed class ProcessVersionsGuidanceTests
{
    private const string GuidePath = "guidance/mcp/guides/processes/versions.md";
    private const string EntryArticlePath = "guidance/mcp/guides/processes/process-modeling.md";
    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    /// <summary>
    /// The shape a version boundary takes while the number is still unknowable here, and the pattern
    /// <c>.github/workflows/validate-pull-request.yml</c> refuses a pull request over. A non-failing
    /// warning was the previous guard and could not hold: NUnit records one inside a PASSING run, so the
    /// required check reported success and merging to master publishes.
    /// </summary>
    private static readonly Regex PlaceholderToken = new("<[A-Z][A-Z0-9-]*TBD>", RegexOptions.Compiled);

    [Test]
    [Description("Pins the five platform facts about the version model that an agent cannot derive: flat family, one active version, instances pinned to their version, rollback affecting later runs only, and no delete.")]
    public void Guide_ShouldStateTheVersionModel()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("The family is FLAT.",
            because: "an agent that expects a chain looks for 'the previous version', which the platform does not store");
        guide.Should().Contain("At most ONE member of a family is the ACTIVE version",
            because: "the read side has two branches for a family with no active member, so an 'exactly one' law makes its own mandatory instruction read as impossible");
        guide.Should().Contain("a well-formed family has exactly one",
            because: "at-most-one alone would leave the agent no expectation to measure a family against");
        guide.Should().Contain("A running INSTANCE stays on the version it started on.",
            because: "an agent that believes an edit or a rollback reaches work in flight promises a repair that never happens");
        guide.Should().Contain("affects only runs that start afterwards",
            because: "that is the whole meaning of a rollback here, and the half a builder most often assumes wrongly");
        guide.Should().Contain("DELETING a version does not exist.",
            because: "the platform's own removal path would cancel every logged run of that schema, so the absence is permanent rather than a gap to work around");
    }

    [Test]
    [Description("Pins the rule that versionhood is never inferable from a schema Name, and that absent version fields mean unknown rather than unversioned.")]
    public void Guide_ShouldRefuseNameBasedInferenceAndZeroDefaulting()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("A schema Name tells you NOTHING about whether a process is a version",
            because: "a version's name tail is a package name plus a number, so a regex over names is a wrong answer that looks right");
        guide.Should().Contain("ABSENT is not zero, and zero is not \"unversioned\" either.",
            because: "reporting an unestablished standing as 'unversioned' reproduces the defect the version fields were added to stop");

        guide.Should().Contain("only THIS IS THE FAMILY ROOT",
            because: "the root of a versioned family reports 0 with no warning, so 0 alone can never settle whether a process has versions - the review found this sentence claiming the opposite");
        guide.Should().Contain("LENGTH of `versions[]`",
            because: "a rule that only forbids an inference leaves the agent stuck; the article has to name the field that actually discriminates");
        guide.Should().Contain("a capped list is not a count",
            because: "versions[] can be truncated, so the discriminator is unusable without that caveat attached to it");
    }

    [Test]
    [Description("Pins the tool names the article instructs an agent to call, since clio owns those constants and a rename there must not leave this article pointing at a tool that does not exist.")]
    public void Guide_ShouldNameOnlyShippedToolNames()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        // modify-business-process is deliberately absent: it is a strict PREFIX of
        // modify-business-process-as-new-version, so requiring it here asserts nothing. Its one
        // load-bearing occurrence - the delegation of the operation vocabulary - is pinned below.
        string[] instructedTools =
        [
            "describe-business-process",
            "get-process-signature",
            "generate-process-model",
            "modify-business-process-as-new-version",
            "set-active-business-process-version",
            "install-process-builder",
            "run-process"
        ];
        // Near-misses of the names above rather than arbitrary wrong strings, because a drifting edit
        // reaches for the plausible shape, not an implausible one.
        string[] wrongSpellings =
        [
            "describe-process",
            "describe_business_process",
            "run_process",
            "get-process-signatures",
            "generate-process-models",
            "create-business-process-version",
            "set-active-process-version",
            "activate-business-process-version"
        ];

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        foreach (string tool in instructedTools)
        {
            guide.Should().Contain(tool,
                because: $"the article instructs the agent to call {tool}, so the exact shipped name has to appear");
        }
        foreach (string wrong in wrongSpellings)
        {
            guide.Should().NotContain(wrong,
                because: $"'{wrong}' is not a tool clio ships, and an agent that copies it from guidance gets an unknown-tool error");
        }
        guide.Should().Contain("the one `modify-business-process` takes",
            because: "this is the article's only delegation of the operation vocabulary to its owning article, so deleting it both orphans the operations argument and duplicates a rule that has another owner");
    }

    [Test]
    [Description("Pins the two-step write sequence and the fact that there is no separate create step. An agent looking for a 'create version' tool it cannot find either invents one or reports the capability as missing; both are wrong, and one call carrying the edits IS the create.")]
    public void Guide_ShouldStateTheTwoStepWriteSequence()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("There is NO separate \"create a version\" step",
            because: "the shape an agent expects - create, then edit - does not exist, and looking for it is the first wrong turn");
        guide.Should().Contain("carries the edits AND produces the version",
            because: "one call does both, and an agent that splits them makes a second version by accident");
        guide.Should().Contain("EMPTY operations array is how you take a plain snapshot",
            because: "the restore-point gesture has no tool of its own, so it has to be named where the tool is");
        guide.Should().Contain("created INACTIVE",
            because: "an agent that assumes the edit went live reports the work as done while the old graph still runs");
        guide.Should().Contain("you MAY choose the package",
            because: "the next sentence offers the package argument, so 'you cannot choose or predict it' was wrong in the one component the agent controls");
        guide.Should().Contain("the version goes to the SOURCE's package",
            because: "the default IS established and is the source's package, always - the article previously said it was unestablished, mirroring a server docblock that promised a design-package fallback ResolveTargetPackage never implemented. Package placement has deployment consequences, so a guessed default is worse than a stated one");
        guide.Should().Contain("no design-package fallback",
            because: "the wrong default is the one an agent would infer from the platform designer's own behaviour, so it is refused by name rather than merely left unmentioned");
        guide.Should().Contain("ONE AT A TIME",
            because: "these two tools are the shape an agent fans out - 'take a restore point of these six processes' - and a parallel burst of schema writes trips IIS rapid-fail and takes the app pool down, which is an environment outage rather than a refused call. Nothing else in the chain states the rule");
    }

    [Test]
    [Description("Pins how far a session answer reaches. Q1 routes and Q2 is a preference, so neither pre-authorizes the one call that changes what a live environment executes; without the deciding sentence the article held a standing yes and an absolute refusal at once.")]
    public void Guide_ShouldMakeTheSessionPolicyAgentBehaviourRatherThanARequestField()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("Two questions, asked ONCE",
            because: "asking on every edit is as bad as never asking, and the article has to say which");
        guide.Should().Contain("say what you did in EVERY reply",
            because: "a builder who cannot tell whether the running version changed has lost what versioning was for");
        guide.Should().Contain("YOUR behaviour, not a field on any request",
            because: "no tool takes a session mode, so an agent that treats the policy as an argument invents one");
        guide.Should().Contain("Never activate on your own initiative",
            because: "the product asks in its own prompt between the two steps, so chaining them takes a decision away from the user");

        guide.Should().Contain("this paragraph decides any sentence that seems to say otherwise",
            because: "the article carried a held session answer and an absolute refusal with nothing deciding between them, so which one an agent followed depended on how it weighted two sentences");
        guide.Should().Contain("Q1 is a ROUTING answer",
            because: "picking a tool changes nothing on an environment, which is why that answer can be held for the session at all");
        guide.Should().Contain("Q2 is a PREFERENCE, not a consent",
            because: "a held yes would otherwise authorize the only call in this article that changes which graph a live environment executes");
        guide.Should().Contain("needs its own request, naming the version to be made actual",
            because: "consent has to name the target for the agent to be able to tell whether it has it");
        guide.Should().Contain("a session answer is never that request, per `core-rules`",
            because: "core-rules owns the rule that an earlier answer is not standing consent, so this article cites it rather than restating or contradicting it");
        guide.Should().Contain("`core-rules` * naming a process",
            because: "the index of where the other rules live is what a reader follows, and an approval gate with two owners is the contradiction AGENTS.md forbids");
    }

    [Test]
    [Description("Pins that a rejected edit leaves nothing behind, its opposite past a successful save, and the bound on retrying: an unanswered call is not the reported-failure case, because the version it may have created can never be removed.")]
    public void Guide_ShouldStateThatARejectedEditSavesNothing()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("nothing is written at all",
            because: "a failure before the save leaves no draft, so there is nothing to clean up and no reason to hunt for one");
        guide.Should().Contain("no half-created version",
            because: "that is the specific worry an agent has about an aborted multi-operation call");
        guide.Should().Contain("still names it, because it cannot be taken back",
            because: "a failure AFTER the save names a version that really exists, and reading that as debris loses it");

        guide.Should().Contain("A call that never answered reported nothing",
            because: "the retry instruction was disambiguated only by the failure message, and a timed-out call carries none");
        guide.Should().Contain("re-describe the family and compare `versions[]` before re-sending",
            because: "the platform allocates the version number, so a blind retry can leave a permanent extra version that V6 says nothing can remove");
        guide.Should().Contain("`core-rules` owns the write-timeout rule",
            because: "the general write-timeout rule has an owner already, so restating it here would put one rule in two places");
        guide.Should().Contain("settle it by re-reading the family with `describe-business-process`",
            because: "activation re-saves every family member in one transaction, so re-issuing it is the most expensive wrong move on a slow answer");
    }

    [Test]
    [Description("Pins what a rollback does and does not do. Each of the three is a promise an agent otherwise makes wrongly: that in-flight work is repaired, that the bad version is gone, and that the requested version is the one now running.")]
    public void Guide_ShouldBoundWhatARollbackDoes()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("It reaches NEW instances only.",
            because: "promising that a rollback repairs work in flight is the most expensive wrong answer here");
        guide.Should().Contain("It DELETES nothing",
            because: "a builder asking to remove the bad version has to hear that no operation does it, not that a permission is missing");
        guide.Should().Contain("READ-BACK, not from the request",
            because: "the platform swallows a failed sibling deactivation, so the requested version is not automatically the running one");
    }

    [Test]
    [Description("Keeps the article a declared, routed get-guidance topic, keeps its routing trigger unconditional, and keeps the entry article's own index and counts in step with it.")]
    public void Resource_ShouldBeDeclaredRoutedAndCounted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-versions");
        string routing = Collapsed(repositoryRoot, RoutingPath);
        string entryArticle = Collapsed(repositoryRoot, EntryArticlePath);
        string entryDescription = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-modeling")
            .GetProperty("description").GetString()!;

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(GuidePath,
            because: "the manifest is the agent-facing contract, and a body it does not declare is unreachable");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "the version model applies to every existing process, gated tools or not");

        int pointer = routing.IndexOf("name=process-versions", StringComparison.Ordinal);
        pointer.Should().BeGreaterThan(0,
            because: "routing is the only guidance pointer clio's MCP instructions carry, so an unrouted article is reachable only by already knowing it exists");
        // The trigger, not the whole map: routing is read before any article, so this row's wording is
        // the one an agent applies first.
        string trigger = routing[Math.Max(0, pointer - 240)..pointer];
        trigger.Should().Contain("ANY existing process",
            because: "a pointer conditional on 'has versions' is unusable: that fact is only knowable from the article the pointer gates");
        trigger.Should().NotContain("has versions",
            because: "the entry article states the trigger unconditionally, and two indexes stating one trigger two ways is the contradictory source of truth AGENTS.md forbids - routing is the one seen first");

        entryArticle.Should().Contain("`process-versions`",
            because: "the entry article indexes the set, and an article missing from the index is one a reader never learns to fetch");
        entryDescription.Should().Contain("and process-versions.",
            because: "the entry item's description enumerates what it routes to, so a new article has to join that list");
        entryDescription.Should().Contain("Each article in the set is sized",
            because: "the wording has to stay count-FREE. This pin used to demand a number, and the number went stale the first time master split an article out - which is the failure the count itself was supposed to prevent, arriving from the other direction. A phrase that names no quantity cannot go stale, and the enumeration above is what keeps the membership honest");
    }

    [Test]
    [Description("Pins both version boundaries and the modify precondition: guidance publishes ahead of the releases that carry these operations, and every modify is an irreversible overwrite whichever member it names.")]
    public void Guide_ShouldDeclareItsClioBoundaryAndTheModifyPrecondition()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);
        string entryArticle = Collapsed(repositoryRoot, EntryArticlePath);

        // Assert
        // AGENTS.md requires an explicit compatible version. Neither number is knowable in this
        // repository yet - the clio release that first carries the version read-back is untagged, and
        // 8.1.0.118 is released WITHOUT it, so naming it would tell a user on that build they have the
        // feature - so each boundary declares either a four-part version or a token the publish gate
        // refuses.
        Match clioBoundary = Regex.Match(guide, "from clio (<[A-Z][A-Z0-9-]*TBD>|[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+) onward");
        clioBoundary.Success.Should().BeTrue(
            because: "the read-back boundary must name a four-part clio version or carry the gated placeholder, or the article declares no compatibility at all");
        if (!PlaceholderToken.IsMatch(clioBoundary.Groups[1].Value))
        {
            clioBoundary.Groups[1].Value.Should().NotBe("8.1.0.118",
                because: "that release is tagged and does NOT contain the read half, so it would tell every user on it that the fields are available");
        }
        Match writeBoundary = Regex.Match(guide, "`CrtProcessBuilder` package on the target environment from (<[A-Z][A-Z0-9-]*TBD>|[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+) onward");
        writeBoundary.Success.Should().BeTrue(
            because: "'the version that first carries these operations' is unfalsifiable, and every sibling in this folder names the CrtProcessBuilder number for the operations it gates");

        int writeSection = guide.IndexOf("== Writing a version, and making it actual ==", StringComparison.Ordinal);
        int nextSection = guide.IndexOf("== Ask once, then behave predictably ==", StringComparison.Ordinal);
        writeSection.Should().BeGreaterThan(0, because: "the write section is where the two tools are named");
        nextSection.Should().BeGreaterThan(writeSection, because: "the bound has to close the section it opens");
        writeBoundary.Index.Should().BeInRange(writeSection, nextSection,
            because: "a precondition filed under what the build cannot do reads as a limitation to note rather than a check to perform, which is the split CONTRIBUTING.md forbids for an operation against a live environment");

        guide.Should().Contain("Check by BEHAVIOUR rather than by number",
            because: "the number is advisory and can be wrong across forks and pre-releases; the absent-fields-and-no-warning test is performable and always true");
        guide.Should().Contain("does not report version standing at all",
            because: "on an older clio every version field is absent WITHOUT a warning, a state the read-failure rule does not cover and which the agent would otherwise read as unversioned");
        entryArticle.Should().Contain("You MUST read `isActiveVersion` from the describe output before ANY modify",
            because: "CONTRIBUTING.md forbids separating an irreversible operation from its preconditions, and modify-business-process is owned by this article, not by process-versions");
        entryArticle.Should().Contain("the overwrite is irreversible either way",
            because: "a modify overwrites the one schema it names and no version has revision history, so the FALSE branch destroys a graph exactly as permanently - stating it only of TRUE reads as reassurance");
        entryArticle.Should().Contain("FALSE: the graph you hold is not the one that runs, so do NOT modify it",
            because: "FALSE is the common case, and a MUST that prescribes an action only for TRUE leaves the agent free to edit a graph nobody runs and report success on production");
        entryArticle.Should().Contain("EMPTY operations array first as a snapshot",
            because: "that is the only stated way to make an in-place edit of the running version recoverable, and it is offered where the confirmation is asked for");
        entryArticle.Should().Contain("launching ANY existing process",
            because: "a pointer conditional on 'has versions' is unusable: that fact is only knowable from the article the pointer gates");
    }

    [Test]
    [Description("Binds every unresolved version boundary to the pull-request gate that refuses one, so an undeclared boundary cannot be published by a suite that is green.")]
    public void PublishGate_ShouldRefuseEveryPlaceholderTheArticlesStillCarry()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string workflow = File.ReadAllText(Path.Combine(
            repositoryRoot, ".github", "workflows", "validate-pull-request.yml"));
        string[] declaredTokens = ProcessGuideSet.Declared(repositoryRoot)
            .SelectMany(article => Regex
                .Matches(ProcessGuideSet.Read(repositoryRoot, article.SourcePath), "<[^<> ]*TBD[^<> ]*>")
                .Select(match => match.Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Assert
        workflow.Should().Contain(PlaceholderToken.ToString(),
            because: "the gate greps published bodies for this exact pattern, and a pattern that drifts from the one this fixture accepts is a gate reporting success on a token it no longer matches");
        workflow.Should().Contain("unresolved version boundary",
            because: "the failure a contributor sees has to say what to do, since the remedy is to find a released version rather than to edit the article");
        declaredTokens.Where(token => !PlaceholderToken.IsMatch(token)).Should().BeEmpty(
            because: "a placeholder written in any other shape is one the gate misses while the publish path stays armed - merging to master releases whatever the manifest declares, and a version an agent creates from that guidance can never be deleted. Found: "
                + string.Join(", ", declaredTokens.Where(token => !PlaceholderToken.IsMatch(token))));
    }

    [Test]
    [Description("Pins the clio-owned field names the article is now the library's only description of, plus the two numbered rules it cites by ID, the provenance of the write half, and the boundary of what this build cannot do.")]
    public void Guide_ShouldPinTheFieldContractTheRulesItCitesAndItsBoundary()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string[] fields =
        [
            "`version`", "`isActiveVersion`", "`activeVersionName`", "`activeVersionSchemaUId`",
            "`versionRootSchemaUId`", "`versions[]`", "`activeVersionSource`", "`versionsTruncatedAt`",
            "`versionReadWarning`"
        ];

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        foreach (string field in fields)
        {
            guide.Should().Contain(field,
                because: $"clio owns {field} and this article is the library's only description of it, so a rename there has to break here rather than ship a contract nothing serves");
        }
        // The article cites V1 and V4 by number three times, so trimming or renumbering them silently
        // breaks its own cross-references.
        guide.Should().Contain(Collapse("V1  A version is a SEPARATE SCHEMA"),
            because: "process-name and run-process both justify themselves by citing V1");
        guide.Should().Contain(Collapse("V4  A version's Name is"),
            because: "V7's first bullet cites V4 for why a name tail proves nothing");
        guide.Should().Contain("the implication runs one way",
            because: "a stock stand carries parentless schemas numbered 1 and 2, so 'the root is version 0' as a two-way rule is measurably false (ENG-94374 story 8)");
        guide.Should().Contain("Treat this as the default,",
            because: "stock versions named ...V2 / ...Extended / ...WithTracking exist, so V4 must not read as a test a caller can apply");
        // The boundary MOVED in ENG-94374 stories 16-17: creating a version and setting the active one
        // are now clio operations, so the two claims that used to be pinned here are false and their
        // assertions are gone rather than softened. What is still a boundary is pinned instead - a
        // running instance cannot be migrated and a version cannot be deleted, both platform facts.
        guide.Should().Contain("Nothing MIGRATES a running instance between versions",
            because: "an agent that believes a rollback moves work in flight promises a repair the platform cannot perform");
        guide.Should().Contain("Nothing DELETES a version",
            because: "the one gesture a builder asks for that no tool anywhere provides, so it has to be refused by name");
        guide.Should().Contain("Save new version (Ctrl+Alt+N)",
            because: "refusing without naming where the product does it is half an answer, and this is the affordance a builder needs");
        guide.Should().Contain("ASKS, in its own prompt",
            because: "the platform lets the person choose whether the new version becomes actual, so guidance must not present create and activate as one step");

        guide.Should().Contain("SOURCE-READ from clio's implementation of them",
            because: "the read half grades every fact V1-V7, so ungraded write-half claims read as measured - and these are the claims an agent uses to tell a user what a partially failed write left behind");
        guide.Should().Contain("were NOT exercised on a stand",
            because: "AGENTS.md forbids claiming behaviour is verified without identifying its evidence, and no version was created or activated on a stand");
    }

    [Test]
    [Description("Pins the three-outcome isActiveVersion rule, the three states inside its last branch, and that the redirect instruction appears once and only inside the branch that can perform it.")]
    public void Guide_ShouldDefineAllThreeOutcomesAndNotRestateTheUnconditionalRedirect()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapsed(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("Three outcomes",
            because: "this is the only instruction the article makes mandatory, so an undefined branch propagates into a wrong edit");
        int redirectable = guide.IndexOf("FALSE WITH an `activeVersionSchemaUId`", StringComparison.Ordinal);
        int notRedirectable = guide.IndexOf("FALSE with NO `activeVersionSchemaUId`", StringComparison.Ordinal);
        redirectable.Should().BeGreaterThan(0,
            because: "the redirect is only performable when the response carries the UId to redirect by");
        notRedirectable.Should().BeGreaterThan(redirectable,
            because: "the branch where there is nothing to redirect to has to be stated, not left to the reader");

        // An invariant rather than a banned sentence: the contradiction that shipped green was one
        // paraphrase of the redirect, and a NotContain on that paraphrase is silent about the next one.
        MatchCollection redirects = Regex.Matches(guide, "[Dd]escribe again");
        redirects.Should().HaveCount(1,
            because: "a second redirect instruction is how the unconditional one survived round one: it read as complete while the branch above it said the UId may not be there");
        redirects[0].Index.Should().BeInRange(redirectable, notRedirectable,
            because: "outside the branch that carries the UId, 'describe again' prescribes a step the response cannot support");

        guide.Should().Contain("THREE states reach this branch",
            because: "fields absent with a warning is a failed READ, and collapsing it into 'no active version was established' reports an affirmative claim about a customer's data that was never read");
        guide.Should().Contain("`versionsTruncatedAt` is absent, name its members",
            because: "versions[] is ascending, so a capped list hides the newest members - exactly the ones a rollback conversation is about");
        guide.Should().Contain("only when the response carries that field",
            because: "activeVersionName has no stated presence rule, so an unconditional launch instruction leaves the agent starting the root on a live environment or starting nothing with no branch to follow");
        guide.Should().Contain("launch NOTHING: report the standing",
            because: "the terminal action here starts a business process in a customer environment, so the unknown-standing branch needs a stated stop");
    }

    private static string Collapsed(string repositoryRoot, string sourcePath) =>
        Collapse(ProcessGuideSet.Read(repositoryRoot, sourcePath));

    private static string Collapse(string text) => Regex.Replace(text, "[ \t\r\n]+", " ");
}

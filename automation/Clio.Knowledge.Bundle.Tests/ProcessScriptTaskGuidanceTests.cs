using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ProcessScriptTaskGuidanceTests
{
    [Test]
    [Description("Keeps ScriptTask guidance independently discoverable without inheriting the experimental process-designer feature gate.")]
    public void Resource_ShouldBeUngatedAndRouted()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-script-task");
        string routing = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/routing.md"));
        string runProcessButton = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/run-process-button.md"));

        // Assert
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "ScriptTask C# guidance applies to existing processes even when experimental process-designer tools are disabled");
        routing.Should().Contain("name=process-script-task",
            because: "an ungated article is useful only when the mandatory routing guide can select it");
        runProcessButton.Should().Contain("copy exactly what `get-process-signature` echoes back",
            because: "the button guide must remain self-contained when process-modeling is feature-gated");
        runProcessButton.Should().NotContain("process-modeling",
            because: "an ungated button workflow must not require an unavailable experimental article");
    }

    [Test]
    [Description("Pins the compatibility details that prevent common ScriptTask compile failures across Creatio runtimes.")]
    public void Guide_ShouldKeepPortableParameterLoggingAndJsonRules()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/process-script-task.md"));

        // Assert
        guide.Should().Contain("Get<Guid>(\"UsrAccountId\")",
            because: "ScriptTask inputs must be read through the generated process parameter API by exact code");
        guide.Should().Contain("get-process-signature process-name=<code-or-caption> environment-name=<env>",
            because: "the ungated guide must name the executable tool path that discovers exact parameter codes");
        guide.Should().Contain("Set<string>(\"UsrResult\"",
            because: "ScriptTask outputs must be written through the same generated parameter API");
        guide.Should().Contain("global::Common.Logging.LogManager",
            because: "global qualification avoids the Terrasoft.Common namespace collision in generated process code");
        guide.Should().Contain("JsonConvert.SerializeObject(value)",
            because: "the one-argument overload remains portable to older Newtonsoft.Json assemblies");
        guide.Should().Contain("`AggregationTypeStrict` and `LogicalOperationStrict` belong to `Terrasoft.Common`",
            because: "AggregationTypeStrict belongs to Terrasoft.Common rather than Terrasoft.Core.DB");
        guide.Should().NotContain("AggregationTypeStrict` and `LogicalOperationStrict` belong to `Terrasoft.Core.DB`",
            because: "the concrete compiler failure came from assigning the enums to the wrong namespace");
        guide.Should().Contain("Creatio 10.0.0.858 assemblies",
            because: "a compatibility rule must carry its verified runtime boundary rather than read as universal folklore");
        guide.Should().Contain("Treat every process parameter as untrusted input",
            because: "matching the signature establishes type compatibility, not record or business authorization");
    }

    [Test]
    [Description("Pins the rules a ScriptTask author cannot recover from anywhere else: that the element costs a compile and comes last in the decision order, that the compile is compile-creatio process-name, the namespaces that are NOT imported, why an alias exists and when one is refused, and that Get/Set names are case-sensitive. Each was measured on a stand (ENG-92711); losing one sends an author into a compile failure or a silently stale process.")]
    public void Guide_ShouldKeepTheAuthoringDecisionCompileAndNamespaceRules()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/process-script-task.md"));

        // Assert
        guide.Should().Contain("Decide first: a ScriptTask costs a compile",
            because: "the element is the one that makes a clio-built process need a compile, so choosing it is a decision");
        guide.Should().Contain("call it from a custom user task",
            because: "reusable C# belongs in a compiled user task, whose callers need no compile of their own");
        guide.Should().Contain("run `compile-creatio` with `process-name` set to the process",
            because: "on Creatio 10.x a package-name compile was measured leaving an edited body uncompiled, and a full one takes 20 minutes");
        guide.Should().Contain("Ask before building it - before the save, never after it.",
            because: "TC-05 (ENG-92711): an agent told only to INFORM built first and explained afterwards, in C# terms; the user decides on custom server code before it lands in a shared package");
        guide.Should().Contain("Save the ScriptTask only after a yes",
            because: "a saved ScriptTask whose code does not compile breaks every later compile of its package");
        guide.Should().Contain("That yes approves the code, not the compile",
            because: "core-rules and compile-creatio require consent right before EVERY compile; an earlier yes is not standing consent, so the question before the save must not replace it");
        guide.Should().NotContain("do not ask for it again",
            because: "the retired single-question wording contradicted the per-compile consent rule on six other surfaces");
        guide.Should().Contain("with the user's confirmation that tool requires",
            because: "a compile reloads the runtime for every user, so the process compile is asked for like any other");
        guide.Should().Contain("keeps running its PREVIOUS body",
            because: "an edited, uncompiled script runs the old code silently, which no save reports");
        guide.Should().Contain("There is NO `System.Linq`, and no `Terrasoft.Configuration`",
            because: "the two namespaces a script most often needs are exactly the two it does not get");
        guide.Should().Contain("An entry describe marks `ignored`",
            because: "describe marks the usings the code generator skips, and a rebuild from describe must leave them out");
        guide.Should().Contain("An ALIAS on a default namespace is refused",
            because: "the generator drops such an entry with its alias, so the alias would not exist at compile time");
        guide.Should().Contain("makes `SysSettings` ambiguous (CS0104)",
            because: "this is the measured collision an alias exists to break");
        guide.Should().Contain("the run-time lookup is case-sensitive",
            because: "Get/Set resolve names ordinally, unlike every name lookup in the builder");
        guide.Should().Contain("\"until the configuration is compiled\"",
            because: "the guide quotes the phrase the compile-required warning carries - a wording pin only: the constant clio keys on (CommandExecutionResult.CompileRequiredWarningMarker) lives in clio, so rewording it there must update this article by hand");
        guide.Should().Contain("The text is C# CLASS MEMBERS, not statements",
            because: "the methods text is pasted into the generated class, so statements there do not compile");
    }

    [Test]
    [Description("Pins the rules the second QA round (ENG-92711) added: no run of a new version before activation, consent for activation, no second compile after a compile that succeeded, the retry before a restart while the runtime reloads, the Latin and keyword name rules, and the multi-line verbatim string. Each came from a stand measurement or a QA finding; losing one sends an agent into a run that proves nothing, a needless compile or restart, or a string that changes its value.")]
    public void Guide_ShouldKeepTheQaRoundTwoVersionCompileAndNameRules()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/process-script-task.md"));
        string coreRules = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/core-rules.md"));

        // Assert
        guide.Should().Contain("(`process-versions` has the measurement)",
            because: "the article the fold is measured in is called process-versions; a bare `versions` names no guide");
        guide.Should().Contain("Activation is the user's decision (`process-version-writes`)",
            because: "the consent rule lives in process-version-writes, and a bare `version-writes` names no guide");
        guide.Should().Contain("compile SUCCEEDED (no compiler errors)",
            because: "a compile that failed, or one followed by another edit, covers nothing");
        guide.Should().Contain("a new version only once it is active",
            because: "the general verification paragraph must not send a new version to run-process before activation");
        guide.Should().Contain("that warning does not ask for a SECOND compile",
            because: "a version compiled before its activation ran its new C# without another compile, and the activation answer used to read as demanding one");
        guide.Should().Contain("activate it with `set-active-business-process-version` only when the user decides to; then verify it on a run",
            because: "activating an uncompiled version breaks every new instance, and a version can only be run once it is active");
        guide.Should().Contain("a Latin letter (a-z, A-Z)",
            because: "the designer and the platform refuse a non-Latin element name, although C# allows one");
        guide.Should().Contain("not a C# keyword",
            because: "a keyword name breaks the configuration compile once the process is compiled instead of interpreted");
        guide.Should().Contain("run it again about two minutes later, and restart only if it still does",
            because: "the reload trailed the compile's answer by about two minutes, and a restart reloads the runtime for every user");
        guide.Should().Contain("Ask the user before that second run when the process has side effects",
            because: "the second run repeats what the elements before the script did");
        guide.Should().NotContain("if the run still shows the old code, restart;",
            because: "that sent an agent to a restart a short wait would have made unnecessary");
        guide.Should().Contain("CrtProcessBuilder 1.6.6.61 and later name such a string on the save",
            because: "the save's notice is what lets the agent fix the string before the compile, not after the run");
        guide.Should().Contain("A verbatim string (`@\"...\"`) that spans lines gains tabs",
            because: "the generator indents every line of the body, so such a string changes its value");
        guide.Should().Contain("the code of a version that is not active runs the ACTIVE version",
            because: "a run of the new version's code before activation executes the previous version, so it proves nothing");
        guide.Should().NotContain("verify it on a run, and only then activate it",
            because: "that order cannot be followed: run-process folds a non-active version's code onto the active one");
        coreRules.Should().Contain("run it again about two minutes later (asking first when the process has side effects) and ask before a restart",
            because: "core-rules carries the same exception for a process-name compile, and every operation reads it");
        coreRules.Should().NotContain("run it again a minute later",
            because: "one minute was shorter than the measured reload");
    }

    [Test]
    [Description("Pins what to do when the MCP client stops waiting for compile-creatio before it answers (ENG-102333): the compile may still be running, so poll compile-status rather than compiling again, and, when compile-status has no record, read the timed compilation history its not-found answer lists - last-compilation-log, whose verdict carries no time, is the fallback when that field is absent (unreadable history or an older clio). Measured on a stand: a process-name compile ran about five minutes while the client gave up after 60 s. Round 3 (QA): no row since the call can mean the request was lost with a restarted server, so it ends with the user, not with last-compilation-log. The rule lives in core-rules; process-script-task only points to it.")]
    public void Guides_ShouldSayWhatToDoAfterAClientSideCompileTimeout()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/process-script-task.md"));
        string coreRules = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/core-rules.md"));

        // Assert
        coreRules.Should().Contain("If YOUR MCP client stops waiting first",
            because: "core-rules owns the long-running rule every operation reads, and a client-side timeout is the case it did not cover");
        coreRules.Should().Contain("do NOT call it again to check; poll compile-status / restart-status",
            because: "a second compile-creatio is a second runtime reload for every user, or a refusal, never a status");
        coreRules.Should().Contain("`not-found` means this MCP server session holds no record, not that nothing ran",
            because: "a not-found that reads as 'nothing ran' is what sent agents to recompile or guess");
        coreRules.Should().Contain("lists the environment's newest compilation-history rows",
            because: "compile-status's not-found answer carries the environment's own history, which survives a client restarting the MCP server");
        coreRules.Should().Contain("only rows that finished after you called compile-creatio can be your compile's: compare their `finished-utc` with the `started-utc` of its in-progress answer, allowing a minute either way",
            because: "the finish time is what ties a history row to the agent's own compile, the in-progress answer carries the call's time, and other compiles write rows too");
        coreRules.Should().Contain("A row that finished clearly before your call is not yours, even one more than seven minutes old",
            because: "right after a restart the newest row can be over seven minutes old and still predate the call (ENG-102333 QA)");
        coreRules.Should().Contain("finished only when its newest row is more than seven minutes old",
            because: "a compile writes a row per project as each ends, so its first row is not its end");
        coreRules.Should().Contain("ask the user before compiling again",
            because: "an agent whose compile wrote no row needs a way out, and every compile needs the user's confirmation");
        coreRules.Should().NotContain("you may start it again",
            because: "the guidance must not license a compile the user did not confirm");
        coreRules.Should().Contain("ask the user before a restart that rests on them alone",
            because: "any schema publisher can write a row, so a restart for every user cannot rest on rows alone");
        coreRules.Should().Contain("If no row was written since your call, the compile has either not finished its first project yet or never started",
            because: "a request still on its way when the MCP server restarted is lost with it, so no row is not proof the compile runs (ENG-102333 QA)");
        coreRules.Should().Contain("treat it as not run and ask the user before compiling again. Do not read last-compilation-log for it",
            because: "with no row since the call that undated verdict can only be an earlier compile's, which a QA agent read as its own success");
        coreRules.Should().NotContain("it is still running: poll again",
            because: "calling a rowless compile running is what let an agent report a compile that never happened");
        coreRules.Should().Contain("When the answer has no `compilation-history` field (it carries `compilation-history-error`, or it comes from a clio that predates the field), fall back to last-compilation-log (through clio-run) - unless",
            because: "last-compilation-log's undated verdict is the fallback for an unreadable history or an older clio, not the first answer");
        coreRules.Should().Contain("Note that last-compilation-log reads the latest FINISHED compile and carries no time",
            because: "last-compilation-log has no timestamp, so read while the compile still runs it reports an earlier compile's verdict");
        coreRules.Should().Contain("do not rely on it, or restart on it, before then",
            because: "a stale success read mid-compile would otherwise lead to the restart core-rules forbids during a compile; after the compile has finished, a restart the workflow needs is still owed, so the rule is about timing, not a ban");
        coreRules.Should().Contain("When restart-status answers `requestfailed`, the restart request itself failed and no restart happened: ask the user, then retry once.",
            because: "the restart request runs inside the deadline race, so a failed request is reported after the answer, and checking that the environment answers would point the wrong way - it answers because it never restarted");
        coreRules.Should().Contain("for a restart, possibly not yet confirmed",
            because: "a restart's in-progress answer can come before its request is confirmed");
        coreRules.Should().NotContain("operation accepted",
            because: "at the deadline the request may not have reached the environment, so the answer is 'started', never 'accepted' (ENG-102333 QA)");
        coreRules.Should().Contain("so report its verdict to the user as unconfirmed",
            because: "a green last-compilation-log cannot show that the agent's own compile ran");
        coreRules.Should().Contain("A `status: not-started` whose error says the launch request was never sent means nothing ran",
            because: "run-process now answers at the shared deadline, and its not-started answer is exact - the request is never sent after it");
        coreRules.Should().Contain("Any other `not-started` is the environment refusing the launch: act on the cause its error names instead of calling again",
            because: "a platform refusal shares the status, and retrying it would loop, or repeat elements that already ran");
        coreRules.Should().Contain("tell the user the environment is not answering",
            because: "retrying a launch that cannot even be prepared needs an end");
        coreRules.Should().Contain("`status: still-running` means the request was sent and clio has no verdict - do NOT re-run it",
            because: "a sent launch request may have started the run, and a second launch duplicates its work");
        coreRules.Should().NotContain("never restart on that answer alone",
            because: "that wording read as a permanent ban and contradicted the package-compile activation cycle");
        guide.Should().Contain("If your client reports the call timed out (for example `Request timed out`)",
            because: "a process-name compile is the long call a ScriptTask author meets, so the article names the case where it happens");
        guide.Should().Contain("follow the Long-running tools rule in `core-rules`",
            because: "the rule has one owner; the article points to it instead of restating it");
        guide.Should().NotContain("last-compilation-log",
            because: "the fallback and its no-timestamp caveat live in core-rules only, so the two cannot drift apart");
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

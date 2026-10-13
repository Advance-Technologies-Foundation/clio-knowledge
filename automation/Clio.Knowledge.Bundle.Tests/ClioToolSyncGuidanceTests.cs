using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the guidance that follows four clio tool changes: <c>get-guidance name=list</c> returns a
/// catalog, <c>get-page</c> omits operations by default, <c>describe-business-process</c> answers a large
/// process with a summary, and <c>place-section-in-workplace</c> replaces the manual workplace recipe.
/// TRIM ALARMS: they fail when a clause is reworded or dropped.
/// </summary>
[TestFixture]
public sealed class ClioToolSyncGuidanceTests
{
    [Test]
    [Description("Routing tells the agent how to list the guides, not the retired availableGuides dump.")]
    public void Routing_ShouldPointGuideDiscoveryAtTheCatalog()
    {
        string routing = Collapse(Read("guidance/mcp/guides/routing.md"));

        routing.Should().Contain("`get-guidance name=list` returns the catalog",
            because: "clio no longer fills availableGuides; name=list is the one call that lists the guides");
        routing.Should().NotContain("an unknown name returns availableGuides",
            because: "on the clio this targets an unknown name returns suggestions, and the catalog is name=list");
    }

    [Test]
    [Description("Page guidance says to ask get-page for the operations when it needs them.")]
    public void PageModification_ShouldAskForOperationsExplicitly()
    {
        string overview = Collapse(Read("guidance/mcp/guides/pages/modification/overview.md"));

        overview.Should().Contain("`get-page` omits the operations by default",
            because: "the edit-the-introducing-operation rule needs component names the default reply no longer carries");
        overview.Should().Contain("pass `include-operations=true`",
            because: "that is the argument that restores the list");
    }

    /// <summary>
    /// A CALL of <c>describe-business-process</c> in guidance: the tool named after "with", "from", "through",
    /// "read", "re-read", "run is", a numbered step or an opening parenthesis, or as the subject of "reads ...
    /// back". Such a mention sends the agent to read something back, so on a large process it must ask for
    /// <c>mode=full</c> or <c>element=</c>: the default SUMMARY carries no uids, bodies, element parameters or
    /// blocks. A mention that only describes the tool ("describe-business-process reports ...") is not a call.
    /// </summary>
    private static readonly Regex PlainDescribeCall = new(
        @"(?:(?:\b(?:with|from|through|read|re-read|re-reading|run is)\s+|\d\)\s+|\(\s*)`?describe-business-process`?"
            + @"(?!\s*`?\s*(?:mode=|element=))|`?describe-business-process`?\s+reads\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The plain calls that are right as written, each with why: evidence of a past probe, a "do not" read, or a
    /// read of what the summary DOES carry (the version standing, an element's <c>type</c>). Matched against the
    /// text just before the call; an entry no call matches any more fails <see cref="DescribeCallExemptions_ShouldStillMatch"/>.
    /// </summary>
    private static readonly (string File, string Fragment, string Why)[] PlainDescribeCallExemptions =
    [
        ("processes/naming.md", "each read the caption back verbatim through", "evidence of a 2026-08-20 probe"),
        ("processes/naming.md", "Read the runtime type back with", "the summary carries each element's type"),
        ("processes/task-category.md", "Evidence: observed on a Creatio 8.x stand through", "evidence"),
        ("processes/tracing.md", "do not re-read the process with", "an instruction NOT to read"),
        ("processes/version-writes.md", "settle it by re-reading the family with", "the summary carries the version standing"),
        ("processes/versions-details.md", "queried through the process-library view and then through", "evidence"),
        ("processes/versions.md", "read `isActiveVersion` from", "the summary carries isActiveVersion"),
        ("processes/versions.md", "treat it as a lead to confirm with", "the summary carries the active version")
    ];

    [Test]
    [Description("No guidance reads a process back with a plain describe-business-process: every call asks for mode=full or element=, unless it reads only what the summary carries.")]
    public void Guidance_ShouldAskForTheFullGraphWhereverItReadsAProcessBack()
    {
        string[] plain = [.. PlainDescribeCalls()
            .Where(call => !PlainDescribeCallExemptions.Any(exemption => IsExempted(call, exemption)))
            .Select(call => $"{call.File}: ...{call.Context}")];

        plain.Should().BeEmpty(
            because: "from the clio that added the summary, describe-business-process answers a large process with a "
                + "SUMMARY (element names, types and captions, flows, start signals, process parameter names): an "
                + "instruction that reads uids, bodies, element parameters or blocks without mode=full or element=<name> "
                + "reads nothing, and the agent guesses tokens or reports that nothing is bound. Add mode=full (or "
                + "element=<name>), or, when the call reads only what the summary carries, exempt it with the reason");
    }

    [Test]
    [Description("Every exemption from the plain-describe scan still matches a call, so a reworded one cannot hide a new call.")]
    public void DescribeCallExemptions_ShouldStillMatch()
    {
        (string File, string Context)[] calls = [.. PlainDescribeCalls()];

        string[] stale = [.. PlainDescribeCallExemptions
            .Where(exemption => !calls.Any(call => IsExempted(call, exemption)))
            .Select(exemption => $"{exemption.File}: {exemption.Fragment}")];

        stale.Should().BeEmpty(because: "an exemption that matches nothing is dead weight that could later hide a real call");
    }

    [Test]
    [Description("The owner names both ways to read detail, and gates the mode argument by clio version.")]
    public void ProcessModeling_ShouldOwnTheFullGraphRuleAndItsVersionGate()
    {
        string modeling = Collapse(Read("guidance/mcp/guides/processes/process-modeling.md"));

        modeling.Should().Contain("pass `mode=full` (the whole graph) or `element=<name>` (one element)",
            because: "the owner says how to get past the summary");
        modeling.Should().Contain("Every `mode=full` in these guides: on clio older than 8.1.0.141, omit `mode`.",
            because: "an older clio refuses an argument it does not know, which would stall every modify on its first describe");
    }

    [Test]
    [Description("Workplace placement goes through the tool; the manual recipe lives in the details article.")]
    public void Workplaces_ShouldRouteToThePlacementTool()
    {
        string workplaces = Collapse(Read("guidance/mcp/guides/applications/workplaces.md"));
        string details = Collapse(Read("guidance/mcp/guides/applications/workplaces-details.md"));

        workplaces.Should().Contain("call `place-section-in-workplace` through `clio-run` (after `get-tool-contract`",
            because: "the tool replaces a 16-37 call manual recipe");
        workplaces.Should().Contain("A move with no roles and `remove-from-my-applications=true` is refused",
            because: "the refusal is the tool's answer to a section that would become invisible");
        workplaces.Should().Contain("ask the questions below FIRST",
            because: "the tool takes no confirm and does not choose the workplace or the audience");
        workplaces.Should().Contain("A workplace another package ships (a product one: `Sales`, `Service`, ...) is used but never bound into yours",
            because: "the tool uses a foreign-owned workplace without binding it, and the guidance must say what it really does");
        workplaces.Should().Contain("It still binds into your package the grant of every role you pass that no other package ships",
            because: "a grant no package ships yet is the one row on a foreign-owned workplace the tool still ships from the user's package");
        workplaces.Should().Contain("pass a role only after the user explicitly agrees to ship its grant everywhere",
            because: "a grant bound into the user's package reaches every environment installing it, so it needs explicit consent");
        workplaces.Should().Contain("keep it live-only (unbound) unless the user explicitly agrees to ship it everywhere, on the tool path too",
            because: "the consent rule applies whichever path writes the grant");
        workplaces.Should().NotContain("- Move a section between workplaces",
            because: "the manual move recipe is the fallback, kept in workplaces-details");
        details.Should().Contain("## Manual placement — only when `place-section-in-workplace` is unavailable or fails",
            because: "a tool failure or an older clio still needs the manual recipe somewhere");
        details.Should().Contain("- Move a section between workplaces",
            because: "the fallback keeps the move recipe");
    }

    [Test]
    [Description("A runtime check stays optional; core-rules points to it in one line, and core-rules-details names the tools with the arguments clio really has.")]
    public void RuntimeVerification_ShouldBeOptionalAndRoutedToTheTools()
    {
        string coreRules = Collapse(Read("guidance/mcp/guides/core-rules.md"));
        string details = Collapse(Read("guidance/mcp/guides/core-rules-details.md"));
        string modeling = Collapse(Read("guidance/mcp/guides/processes/process-modeling.md"));

        coreRules.Should().Contain("A runtime check is optional and the user's choice; before you make one, read `core-rules-details` (Runtime check)",
            because: "core-rules is read on every operation, so it carries only the pointer");
        coreRules.Should().NotContain("page-smoke",
            because: "core-rules is read on every operation; the UI check is routed from core-rules-details and the page guide");
        coreRules.Should().NotContain("trigger-smoke",
            because: "the tool detail lives in core-rules-details, out of the article every operation reads");
        details.Should().Contain("A runtime check is optional and the user's choice.",
            because: "the routing must not turn verification into a duty");
        foreach (string clause in new[]
        {
            "`run-process` with `wait`", "`watch` (1-5 entities; the records that appeared come back as `created`)",
            "`cleanup` (requires `watch`; deletes only the `created` records you created that are tied to this run)",
            "`trigger-smoke` for a record-triggered (signal) process",
            "`entity`, `values` (the trigger record's columns), `watch` (1-5 entities, required), optionally `process`",
            "A created record with no such lookup stays (it is listed in `cleanup.skipped`)",
            "`odata-delete` with `dry-run`, `ids` or `cascade`",
            "`page-smoke` only when the task needs a UI check of a Freedom UI form page",
            "INSTEAD of a hand-written Playwright script or browser steps, not as a routine step after every page change",
            "`page` (the form page schema name), optionally `record-id`",
            "It is read-only unless you pass `click` (a button caption or element name)",
            "treat it as a write and confirm the target environment with the user first"
        })
        {
            details.Should().Contain(clause,
                because: "without the tools and their real arguments, agents hand-roll create/sleep/read/delete loops of 8-20 calls");
        }
        Collapse(Read("guidance/mcp/guides/pages/modification/components.md")).Should().Contain(
            "use `page-smoke` (clio 8.1.0.141 and later; `core-rules-details`, Runtime check) INSTEAD of a hand-written Playwright script or browser steps; it is not a routine step after every save",
            because: "the page guide routes a UI check of a button to the one-call tool, and only when the task needs it");
        modeling.Should().Contain("A runtime check is optional (the user's choice); use `run-process` or `trigger-smoke` (`core-rules-details`).",
            because: "the build recipe's verify step is where an agent decides how to check its process");
    }

    private static IEnumerable<(string File, string Context)> PlainDescribeCalls()
    {
        string guides = Path.Combine(ProcessGuideSet.FindRepositoryRoot(), "guidance", "mcp", "guides");
        foreach (string path in Directory.EnumerateFiles(guides, "*.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string text = Collapse(File.ReadAllText(path));
            string file = Path.GetRelativePath(guides, path).Replace('\\', '/');
            foreach (Match match in PlainDescribeCall.Matches(text))
            {
                int from = Math.Max(0, match.Index - 80);
                yield return (file, text[from..(match.Index + match.Length)]);
            }
        }
    }

    private static bool IsExempted((string File, string Context) call, (string File, string Fragment, string Why) exemption) =>
        call.File == exemption.File && call.Context.Contains(exemption.Fragment, StringComparison.Ordinal);

    private static string Read(string relativePath) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath);

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

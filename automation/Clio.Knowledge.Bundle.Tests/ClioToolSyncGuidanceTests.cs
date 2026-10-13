using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the guidance that follows four clio tool changes (ENG-103031): <c>get-guidance name=list</c> returns a
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

    [Test]
    [Description("Every process instruction that reads uids, labels or layout asks for the full graph.")]
    public void ProcessGuidance_ShouldAskForTheFullGraphWhereItReadsDetail()
    {
        (string Path, string Clause)[] required =
        [
            ("guidance/mcp/guides/processes/process-modeling.md", "pass `mode=full` (the whole graph) or `element=<name>` (one element)"),
            ("guidance/mcp/guides/processes/process-modeling.md", "ALWAYS `describe-business-process mode=full` first"),
            ("guidance/mcp/guides/processes/formulas.md", "call `describe-business-process mode=full` and take the parameter's `uid`"),
            ("guidance/mcp/guides/processes/diagram-layout.md", "`describe-business-process mode=full` returns the DIAGRAM"),
            ("guidance/mcp/guides/processes/naming.md", "`describe-business-process mode=full` reports each flow's `label`"),
            ("guidance/mcp/guides/processes/branch-conditions.md", "`describe-business-process mode=full` first"),
            ("guidance/mcp/guides/processes/branch-conditions-details.md", "`describe-business-process mode=full` first"),
            ("guidance/mcp/guides/processes/data-elements.md", "`describe-business-process mode=full` and send it complete"),
            ("guidance/mcp/guides/processes/read-data.md", "`describe-business-process mode=full` and send it complete")
        ];

        string[] missing = [.. required
            .Where(row => !Collapse(Read(row.Path)).Contains(Collapse(row.Clause), StringComparison.Ordinal))
            .Select(row => $"{row.Path}: {row.Clause}")];

        missing.Should().BeEmpty(
            because: "a large process now answers with a summary that has no uids, labels, blocks or layout, so an "
                + "instruction that reads one of those without mode=full reads nothing. Missing: "
                + string.Join("; ", missing));
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
        workplaces.Should().NotContain("- Move a section between workplaces",
            because: "the manual move recipe is the fallback, kept in workplaces-details");
        details.Should().Contain("## Manual placement — only when `place-section-in-workplace` is unavailable or fails",
            because: "a tool failure or an older clio still needs the manual recipe somewhere");
        details.Should().Contain("- Move a section between workplaces",
            because: "the fallback keeps the move recipe");
    }

    [Test]
    [Description("A runtime check stays optional, and when an agent makes one it is routed to the verification tools.")]
    public void RuntimeVerification_ShouldBeOptionalAndRoutedToTheTools()
    {
        string coreRules = Collapse(Read("guidance/mcp/guides/core-rules.md"));
        string modeling = Collapse(Read("guidance/mcp/guides/processes/process-modeling.md"));

        coreRules.Should().Contain("A runtime check is optional and the user's choice",
            because: "the routing must not turn verification into a duty");
        foreach (string clause in new[]
        {
            "`run-process` with `wait`", "`watch` (reports the records the run created as `created`)",
            "`cleanup` (deletes only records you created)", "`trigger-smoke` for a record-triggered (signal) process",
            "`odata-delete` with `dry-run`, `ids` or `cascade`"
        })
        {
            coreRules.Should().Contain(clause,
                because: "without the tools named, agents hand-roll create/sleep/read/delete loops of 8-20 calls");
        }
        modeling.Should().Contain("A runtime check is optional (the user's choice); use `run-process` or `trigger-smoke` (`core-rules`).",
            because: "the build recipe's verify step is where an agent decides how to check its process");
    }

    private static string Read(string relativePath) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath);

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

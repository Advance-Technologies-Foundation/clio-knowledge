using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the navigation placement gate: one owner (<c>core-rules</c>), a branch for an agent that cannot ask,
/// and one-line citations in the guides that used to restate it.
/// </summary>
/// <remarks>
/// The gate told the agent to STOP and ask which workplace an app belongs to. An agent that cannot ask - a
/// headless, scheduled or subagent run - resolved the question itself and created a workplace the request never
/// asked for: in one measured run 22 calls and about 38 KB of workplace and data-binding guidance went into a
/// workplace for a request that said nothing about navigation. The gate was stated in three articles, so each
/// copy had to be kept in step; it now lives in core-rules only. An agent that cannot ask follows the request as written:
/// it keeps the create-app placement and says so.
/// </remarks>
[TestFixture]
public sealed class NavigationPlacementGateTests
{
    private const string CoreRules = "guidance/mcp/guides/core-rules.md";
    private const string AppModeling = "guidance/mcp/guides/applications/app-modeling.md";
    private const string Workplaces = "guidance/mcp/guides/applications/workplaces.md";
    private const string HomePage = "guidance/mcp/guides/dashboards/home-page.md";

    [Test]
    [Description("core-rules owns the gate and follows the request: do what it names, ask once when it leaves placement open, and when nobody can be asked keep the create-app placement and report it.")]
    public void CoreRules_ShouldOwnTheGateIncludingTheNonInteractiveBranch()
    {
        // Arrange
        string root = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string coreRules = Normalize(File.ReadAllText(Path.Combine(root, CoreRules)));

        // Assert
        coreRules.Should().Contain("Navigation placement (this rule owns the gate; other guides cite it)",
            because: "one owner is what keeps the three former copies from drifting apart");
        coreRules.Should().Contain("When the request names the workplace or audience, do that",
            because: "a named placement or audience is the user's decision already made");
        coreRules.Should().Contain("When it leaves placement open and you can ask, ask once",
            because: "placement and audience are the user's decisions, so an agent that can ask does");
        coreRules.Should().Contain("a mention in a summary is not asking",
            because: "an interactive agent must not replace the question with a note at the end");
        coreRules.Should().Contain("BEFORE `create-app` or any other navigation write",
            because: "asking after the pages are built makes the user re-decide work already done");
        coreRules.Should().Contain("When you cannot ask (a headless, scheduled or subagent run, or asking disabled), keep the `create-app` placement and say so in the final summary",
            because: "an agent with nobody to ask otherwise answers the question itself and builds a workplace nobody asked for");
    }

    [Test]
    [Description("app-modeling, workplaces and home-page cite the core-rules gate instead of restating STOP-and-ask.")]
    public void OtherGuides_ShouldCiteTheGateInsteadOfRestatingIt()
    {
        // Arrange
        string root = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string appModeling = Normalize(File.ReadAllText(Path.Combine(root, AppModeling)));
        string workplaces = Normalize(File.ReadAllText(Path.Combine(root, Workplaces)));
        string homePage = Normalize(File.ReadAllText(Path.Combine(root, HomePage)));

        // Assert
        appModeling.Should().Contain("follow the Navigation placement rule in `core-rules` before `create-app`",
            because: "app-modeling is read before create-app, so it must still lead the agent to the gate");
        appModeling.Should().NotContain("STOP after `create-app`",
            because: "the gate has one owner; a restated STOP has no non-interactive branch and sends a headless agent into workplaces");
        workplaces.Should().Contain("`core-rules` (Navigation placement) owns WHEN to ask, and what to do when you cannot",
            because: "workplaces keeps the option set and recipes, and cites the gate for when they apply");
        workplaces.Should().NotContain("When the request names no target workplace, STOP",
            because: "the gate has one owner");
        homePage.Should().Contain("When you cannot ask, `core-rules` (Navigation placement) applies: leave the page unbound",
            because: "a home-page request with no named workplace hits the same gate");
    }

    private static string Normalize(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
}

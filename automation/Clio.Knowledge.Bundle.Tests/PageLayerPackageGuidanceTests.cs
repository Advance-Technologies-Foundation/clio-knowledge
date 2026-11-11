using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the check that runs before the first write to a page customized in more than one package:
/// the agent asks which package to change instead of letting the automatic save target decide silently.
/// TRIM ALARMS: they fail when a load-bearing clause is reworded or dropped.
/// </summary>
[TestFixture]
public sealed class PageLayerPackageGuidanceTests
{
    private const string Overview = "guidance/mcp/guides/pages/modification/overview.md";
    private const string Index = "guidance/mcp/guides/pages/modification/index.md";

    [Test]
    [Description("The blanket ban on the package selector stays retired; it is passed only after the user's choice.")]
    public void Overview_ShouldKeepTheOldSelectorBanRetired()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().NotContain("Do NOT pass `target-package-uid` in normal flows",
            because: "the selector is how a save reaches the package the user chose");
        overview.Should().Contain("never pass it on your own initiative",
            because: "the selector bypasses the backend's package resolution, so only the user's choice or another guide licenses it");
    }

    [Test]
    [Description("A page with an unlocked layer outside the default target stops for the user's choice.")]
    public void Overview_ShouldStopAndAskBeforeTheFirstWrite()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("STOP and ask the user which package to change before writing anything",
            because: "the backend saves to one layer and the response never mentions the others");
        overview.Should().Contain("do not pick one yourself",
            because: "the package choice is the user's");
        overview.Should().Contain("Skip the check for a page `create-page` made in this task",
            because: "the create-page -> update-page flow owns that first write, and keeping the default would leave a created mobile schema empty");
    }

    [Test]
    [Description("A save into a chosen non-default layer appends, never replaces with the get-page body.")]
    public void Overview_ShouldAppendIntoTheChosenLayer()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("Send only your change as a `mode: \"append\"` fragment with that layer's `packageUId` as `target-package-uid`",
            because: "append keeps the chosen layer's own body on the server");
        overview.Should().Contain("never fall back to the `get-page` body",
            because: "get-page returns the default target's body and would overwrite the chosen layer's diff");
        overview.Should().Contain("save through `update-page` only",
            because: "sync-pages has no package selector and always writes to the default target");
    }

    [Test]
    [Description("An unreadable hierarchy never leads to a silent write.")]
    public void Overview_ShouldNotWriteWhenTheLayersCannotBeRead()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("`get-page-hierarchy` failed: no layers are known. Do not write",
            because: "with no layers known the agent cannot tell whether another package already customizes the page");
        overview.Should().Contain("do not write silently",
            because: "an unknown lock state must reach the user as a question");
    }

    [Test]
    [Description("The modification index routes the first write of a page to the check.")]
    public void Index_ShouldGateTheFirstWriteOnTheCheck()
    {
        string index = Collapse(Read(Index));

        index.Should().Contain("| the FIRST write (`update-page` / `sync-pages`) to a page in this task | `page-modification-overview` |",
            because: "the GATE table is how an agent learns that the first write needs the check");
    }

    private static string Read(string relativePath) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath);

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

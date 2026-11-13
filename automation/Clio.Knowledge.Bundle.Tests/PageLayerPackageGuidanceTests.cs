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
    [Description("packageChoice.required or a package-choice-required refusal stops for the user's choice, even when one layer looks obvious.")]
    public void Overview_ShouldStopAndAskBeforeTheFirstWrite()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("`packageChoice.required: true`, or a `package-choice-required` refusal: STOP and ask the user which package to change before writing anything",
            because: "clio reports the case in get-page and refuses the write, and the agent must turn both into a question");
        overview.Should().Contain("never pick a package yourself — not even when one layer looks like the obvious choice",
            because: "in QA the agent read the rule and still saved to the top layer because it looked obvious");
        overview.Should().Contain("Skip the question only for a page `create-page` made in this task",
            because: "the create-page -> update-page flow owns that first write, and keeping the default would leave a created mobile schema empty");
    }

    [Test]
    [Description("Keeping the default target is an explicit target-package-uid, because clio refuses a write without one.")]
    public void Overview_ShouldNameTheDefaultTargetWhenTheUserKeepsIt()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("save with `target-package-uid` set to `packageChoice.defaultTargetPackageUId`",
            because: "clio 8.1.0.152+ refuses a save without a target while another editable layer exists");
    }

    [Test]
    [Description("The base page is located by rootSchemaUId, never as the chain root at level 0.")]
    public void Overview_ShouldNotOfferTheBasePageAsALayer()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("NOT the chain root at `hierarchyLevel` 0, which is a platform template such as `BaseTemplate`",
            because: "reading level 0 as the base made the agent offer, and write into, the base page");
        overview.Should().NotContain("except the root (base) schema at `hierarchyLevel` 0",
            because: "that definition is wrong for every page built from a template");
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

        overview.Should().Contain("If `get-page-hierarchy` fails, do not write",
            because: "with no layers known the agent cannot tell whether another package already customizes the page");
        overview.Should().Contain("or `packageLocked: null`, or no `packageLocked` key at all, ask as above",
            because: "an unknown lock state must reach the user as a question");
    }

    [Test]
    [Description("The modification index routes the first write of a page to the check.")]
    public void Index_ShouldGateTheFirstWriteOnTheCheck()
    {
        string index = Collapse(Read(Index));

        index.Should().Contain("| the FIRST write (`update-page` / `sync-pages`) to a page in this task, `get-page` `page.packageChoice.required` is true, or a save was refused with `package-choice-required` | `page-modification-overview` |",
            because: "the GATE table is how an agent learns that the first write needs the check");
    }

    private static string Read(string relativePath) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath);

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

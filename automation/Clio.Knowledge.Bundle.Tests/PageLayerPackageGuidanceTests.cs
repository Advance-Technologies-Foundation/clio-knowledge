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
        overview.Should().Contain("never pick or recommend a package yourself — not even when one layer looks like the obvious choice",
            because: "in QA the agent read the rule and still saved to the top layer because it looked obvious");
        overview.Should().Contain("No recommendation anywhere",
            because: "in the stand check the agent asked but marked the default as recommended, which steers the user's choice");
        overview.Should().Contain("Never retry the write without a target",
            because: "a refusal must become a question, not a retry");
        overview.Should().Contain("Skip the question also for a page `create-page` made in this task",
            because: "the create-page -> update-page flow owns that first write, and keeping the default would leave a created mobile schema empty");
    }

    [Test]
    [Description("The user's answer holds for the page until the end of the task, even while get-page still reports required: true.")]
    public void Overview_ShouldAskOncePerPagePerTask()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("later writes to that page in the same task reuse the chosen `target-package-uid` without asking again, even though `get-page` still reports `required: true`; a new task asks again",
            because: "required stays true after the choice, and asking before every write breaks the rule that the choice holds for the task");
        overview.Should().Contain("A missing `packageChoice` key is not `false`: on that clio run the manual check below",
            because: "an older clio has no packageChoice, and reading its absence as required: false skips the check");
    }

    [Test]
    [Description("The question lists one option per layer, the default target first, and never offers a locked layer.")]
    public void Overview_ShouldListEveryEditableLayerAsAnOption()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("one option per entry of `packageChoice.layers` whose `packageLocked` is not `true`, the default target first and marked `(default)` — never `(Recommended)`",
            because: "with required: true clio lists locked layers too, and only the editable ones are a choice; the default must not read as advice");
        overview.Should().Contain("the locked layers are in `packageChoice.layers` with `packageLocked: true`",
            because: "clio reports every replacing layer, locked or not, once required is true, so the override check reads them from there");
        overview.Should().Contain("plus the default target as its own option when no layer has `isDefaultTarget: true`",
            because: "keeping the default target is a valid choice even before it holds a layer");
        overview.Should().Contain("for the default target added as its own option, `packageChoice.defaultTargetPackageName`, or `packageChoice.defaultTargetPackageUId` when the name is absent",
            because: "a default target that holds no layer has no layers entry to take its name from, and clio sets the name only when it could read it");
        overview.Should().Contain("`packageLocked: true` layers cannot receive the change: name them only when they override the user's element (below), never as an option",
            because: "a save into a locked package fails, so offering it would lead the user into a dead end");
        overview.Should().Contain("warn the user, before writing, that the higher layer overrides the change at runtime",
            because: "a change into a lower layer is invisible at runtime when a higher layer changes the same element");
    }

    [Test]
    [Description("Keeping the default target is an explicit target-package-uid, because clio refuses a write without one.")]
    public void Overview_ShouldNameTheDefaultTargetWhenTheUserKeepsIt()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("save through `update-page` (`sync-pages` refuses the page while `required` is `true` and cannot name a target package) with `target-package-uid` set to `packageChoice.defaultTargetPackageUId`",
            because: "clio <CLIO-TBD>+ refuses a save without a target while another editable layer exists");
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
        overview.Should().Contain("When `update-page` cannot read the page's layers it refuses the save (`Failed to load hierarchy ... (package-choice check)`): do not write",
            because: "on a clio with packageChoice the failed layer read is a refusal, and it must reach the user, not a retry");
    }

    [Test]
    [Description("On a clio without packageChoice the manual check names its own fields for the question, the save and the override warning.")]
    public void Overview_ShouldSpellOutTheFallbackWithoutPackageChoice()
    {
        string overview = Collapse(Read(Overview));

        overview.Should().Contain("The page's base schema is the entry whose `schemaUId` equals `page.rootSchemaUId` from `get-page`",
            because: "the base is the lowest entry with the page name, never a layer to offer");
        overview.Should().Contain("with these fields in place of `packageChoice`: one option per replacing layer whose `packageLocked` is not `true`",
            because: "the packageChoice-based rules have no fields to read on an older clio");
        overview.Should().Contain("The default target is `page.designPackageUId`: list it first and mark it `(default)`",
            because: "without packageChoice the default target comes from get-page");
        overview.Should().Contain("with `target-package-uid` only when the user picks a layer other than the default target (this clio does not refuse a save without it)",
            because: "an older clio saves to the default target without a selector");
        overview.Should().Contain("read the bodies of the chosen layer and of every layer above it (`get-page-hierarchy` without `metadata-only`) and compare the `viewConfigDiff` `name`s",
            because: "metadata-only carries no changes, so the override warning needs the bodies on an older clio");
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

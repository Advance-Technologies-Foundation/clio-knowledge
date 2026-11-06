using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ThemeBackgroundGuidanceTests
{
    private const string Branding = "guidance/mcp/guides/theming/branding.md";
    private const string Theming = "guidance/mcp/guides/theming/index.md";
    private const string Routing = "guidance/mcp/guides/routing.md";

    [TestCase(Theming, "(`get-guidance name=branding` → \"Background\" owns the site background and which one an account sees)")]
    [TestCase(Theming, "Only `create-theme` and `update-theme` write it, and only the no-code flow keeps it: the `theme.json` that `build-theme` writes has no background, so the next `push-workspace` drops one set on a workspace theme — tell the user.")]
    [TestCase(Theming, "on a call that sets a background it can mean the background floor instead — see \"Theme background\".")]
    [TestCase(Theming, "pass at most one of `background-image-id` (upload the file with `upload-image` first) or `background-color` (#RGB, #RRGGBB or #RRGGBBAA; convert another form with `advise-theme-palette operation=triage`) to `create-theme` (brand mode too) or `update-theme`; `remove-background` on `update-theme` clears it.")]
    [TestCase(Theming, "With a background argument `update-theme` needs only the `id` — the omitted `caption`, `css-class-name` and `css-content` keep their values, so never send the CSS for a background change.")]
    [TestCase(Theming, "Pass a theme background in this same call — see \"Theme background\".")]
    [TestCase(Theming, "a full overwrite of caption + css-class-name + css-content, unless the call only changes the background")]
    [TestCase(Theming, "Version: Creatio 10.2.96 or later; on an older one the call fails with `version-too-old` and writes nothing, so tell the user and repeat `create-theme` without the background argument. Set the site background instead only if the user agrees. A clio whose `create-theme` contract has no `background-color` cannot set one — ask the user to update clio.")]
    [TestCase(Theming, "setting a background leaves `UsePanelIconBackground` off for All employees (turned off if it is on), because it can cover the background — an environment-wide change, so tell the user before the write that it will be off for everyone. Pass `keep-icon-background` = true only if the user asks to keep it.")]
    [TestCase(Theming, "The result says which: `panel-icon-background` (`off` or `kept`) from `create-theme`, a log line from `update-theme`.")]
    [TestCase(Theming, "A failed turn-off is a warning.")]
    [TestCase(Theming, "a failed write changes nothing else; an image uploaded for it stays, unbound.")]
    [TestCase(Theming, "After a successful one an image is bound into the theme's package — for an existing theme the `Pkg/<package>` segment of its `cssFilePath` (check it with `get-target-package`), not a branding run's target package — which `create-theme` returns in `background-image-package` and `update-theme` names in its log; a color travels inside the theme. The theme write does not bind the panel icon state, only a site background does, so unless the theme's package also carries a site background, on another environment the icon background can still cover the theme background — tell the user.")]
    [TestCase(Theming, "A binding problem is a warning — the theme exists, do not create it again; relay the warnings (`create-theme` returns them in `warnings`, `update-theme` in its log).")]
    [TestCase(Theming, "`remove-background`, a `background-color` that replaces an image, and `delete-theme` keep the image binding, so the package still ships the image — tell the user.")]
    [TestCase(Theming, "`cssClassName`, `cssFilePath`, and `background`")]
    [TestCase(Theming, "cssContentLength, background?, error? }")]
    [TestCase(Branding, "The logos, the favicon and the site background are environment-wide (All-Users) settings")]
    [TestCase(Branding, "The one exception is a background for an existing theme: it goes to that theme's own package (`get-guidance name=theming` → \"Theme background\").")]
    [TestCase(Branding, "A background has two levels: a theme's own background (an image or a color) is seen by that theme's accounts, the site background by every account whose theme has none.")]
    [TestCase(Branding, "In a branding run, give the new theme its background; without a new theme, ask which theme gets it. Set the site background only when the user asks for the whole site.")]
    [TestCase(Branding, "A theme background is written with the theme — see `get-guidance name=theming` → \"Theme background\".")]
    [TestCase(Branding, "clio sets the site background only as an image and cannot remove it, only replace it.")]
    [TestCase(Branding, "An account sees the first that applies: its `UseAccessibleDesktopColor` system setting; its theme's color, or its image if it loads, unless the `DisableThemeBackground` feature is on; the site background (`CrtBackgroundConfig`); the bundled default image.")]
    [TestCase(Branding, "To tell which applies, read the account's `UseAccessibleDesktopColor` value (`SysSettingsValue`) and find its theme: its own (`SysProfileData`, key `Theme`) if that theme is active, else the `DefaultTheme` system setting's if that one is active, else the stock theme, which the account also gets when the `DisableChangeTheme` feature is on or it lacks `CanCustomizeBranding`. A missing feature row means off.")]
    [TestCase(Branding, "When the expected background does not show, tell the user which one wins, that the panel icon background covers it, or that the theme's image was deleted")]
    [TestCase(Branding, "Never change `UseAccessibleDesktopColor` or `DisableThemeBackground` to make a background show.")]
    [TestCase(Routing, "site background image, or choosing it over a theme's own background -> name=branding")]
    [TestCase(Routing, "set the default) / a theme's own background -> name=theming")]
    [Description("Each background rule is stated in the guide that owns it.")]
    public void Guide_ShouldStateTheBackgroundRule(string path, string clause) =>
        Read(path).Should().Contain(clause);

    [Test]
    [Description("The theming guide states the version floor once, and no guide restates another guide's background tokens.")]
    public void Guides_ShouldNotRestateEachOthersBackgroundRules()
    {
        string theming = Read(Theming);
        string branding = Read(Branding);
        string routing = Read(Routing);

        using (new AssertionScope())
        {
            Regex.Matches(theming, @"10\.2\.96").Count.Should().Be(1);
            branding.Should().NotContain("10.2.96");
            routing.Should().NotContain("10.2.96");
            foreach (string token in new[] { "background-image-id", "background-color", "remove-background" })
            {
                branding.Should().NotContain(token);
                routing.Should().NotContain(token);
            }
            foreach (string token in new[] { "UseAccessibleDesktopColor", "DisableThemeBackground", "CrtBackgroundConfig", "set-background-image" })
            {
                theming.Should().NotContain(token);
                routing.Should().NotContain(token);
            }
        }
    }

    private static string Read(string relativePath) =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar))),
            @"\s+",
            " ");
}

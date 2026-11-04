using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ThemeBackgroundGuidanceTests
{
    private const string BrandingPath = "guidance/mcp/guides/theming/branding.md";

    private const string ThemingPath = "guidance/mcp/guides/theming/index.md";

    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    [Test]
    [Description("The theming guide writes a theme background through the theme methods and states their arguments once.")]
    public void Theming_ShouldWriteTheBackgroundThroughTheThemeMethods()
    {
        ShouldContainAll(ThemingPath,
            "the theme methods are the only writers of a theme background",
            "Only `create-theme` and `update-theme` write it; `build-theme` does not.",
            "pass at most one of `background-image-file` (a local image, uploaded in the same call), `background-image-id` (an image from `upload-image`) or `background-color` (`#RGB`, `#RRGGBB` or `#RRGGBBAA`) to `create-theme`, in brand mode as well, or to `update-theme`.",
            "`update-theme` also takes `remove-background`, which clears it so the theme's accounts see the site background again.",
            "With a background argument, `update-theme` needs only the `id`: the `caption`, `css-class-name` and `css-content` you leave out keep their stored values, so a background change never needs the theme CSS.",
            "To give the theme its own background, pass it in this same call — see \"Theme background\".",
            "a full overwrite of caption + css-class-name + css-content, unless the call only changes the background");
    }

    [Test]
    [Description("The version floor has one owner sentence, and the site background replaces a theme background only with the user's consent.")]
    public void Theming_ShouldOwnTheVersionFloorAndTheFallback()
    {
        string theming = ReadCollapsed(ThemingPath);
        string branding = ReadCollapsed(BrandingPath);

        using (new AssertionScope())
        {
            theming.Should().Contain(
                "a theme background needs Creatio 10.2.96 or later — `themeBackgroundSupported` in `check-theming-access` tells you up front, and the write tools check it again before any write. A clio whose `create-theme` contract has no `background-color` cannot write one either (it can be updated). In both cases tell the user; set the site background instead only if the user agrees, even when the error suggests it.",
                because: "an older ThemeService drops the background, and the clio hint names the site background");
            Regex.Matches(theming, @"10\.2\.96").Count.Should().Be(1, because: "the floor has one owner sentence");
            branding.Should().NotContain("10.2.96", because: "the theming guide owns the floor");
        }
    }

    [Test]
    [Description("The panel icon turn-off is announced before the write, its result is read, and the theme package does not carry it.")]
    public void Theming_ShouldStateThePanelIconAndThePackage()
    {
        ShouldContainAll(ThemingPath,
            "the panel icon change is environment-wide and a theme package does not carry it",
            "That is an environment-wide change: when `check-theming-access` reports `panelIconBackground` other than `off`, tell the user before the write.",
            "The result reports `panelIconBackground` as `turned-off`, `already-off` or `kept`; a failed turn-off is a warning, not a failure.",
            "The panel icon state is not bound, so where the package is installed the panel icon background can still cover the theme background.",
            "A binding problem after a successful write is a warning — the theme exists, so do not create it again; relay the warnings.",
            "`remove-background` and `delete-theme` leave the image bindings, so the package still ships the image; tell the user.",
            "when the theme write fails, nothing follows it — no panel icon change and no bindings. A file uploaded before it stays uploaded, and the error names its image id — retry with that `background-image-id` instead of uploading the file again.",
            "`get-theme` and `list-themes` return it as `background` (`mode` `Image` with `imageId`, or `Color` with `color`), absent when the theme has none.");
    }

    [Test]
    [Description("The branding guide owns the site background, the choice between the levels and which background an account sees.")]
    public void Branding_ShouldOwnTheSiteBackgroundAndThePrecedence()
    {
        ShouldContainAll(BrandingPath,
            "the branding guide is where a run picks the level and diagnoses a hidden background",
            "A theme's own background, an image or a color, is seen by the accounts on that theme; the site background, an image, by every account whose theme has none.",
            "In a branding run, give the new theme its background; set the site background only when the user asks for the whole site.",
            "A theme's background is written with the theme itself — see `get-guidance name=theming` → \"Theme background\".",
            "The site background cannot be a color or be removed: to change it, set another image.",
            "An account sees the first that applies: its personal `UseAccessibleDesktopColor` system setting; its theme's color, or its image if it loads, unless the `DisableThemeBackground` feature is on; the site background (the `CrtBackgroundConfig` system setting); the bundled default image.",
            "Never change `UseAccessibleDesktopColor` or `DisableThemeBackground` to make a background show.");
    }

    [Test]
    [Description("Each guide states only its own background rules, and routing names both owners.")]
    public void Guides_ShouldNotRestateEachOthersBackgroundRules()
    {
        string theming = ReadCollapsed(ThemingPath);
        string branding = ReadCollapsed(BrandingPath);
        string routing = ReadCollapsed(RoutingPath);

        string[] themingOnly =
        [
            "background-image-file",
            "background-image-id",
            "background-color",
            "remove-background",
            "themeBackgroundSupported"
        ];
        string[] brandingOnly =
        [
            "UseAccessibleDesktopColor",
            "DisableThemeBackground",
            "CrtBackgroundConfig",
            "set-background-image"
        ];

        using (new AssertionScope())
        {
            foreach (string token in themingOnly)
            {
                branding.Should().NotContain(token, because: "the theming guide owns the theme background arguments");
                routing.Should().NotContain(token, because: "routing names the owner, not its arguments");
            }
            foreach (string token in brandingOnly)
            {
                theming.Should().NotContain(token, because: "the branding guide owns the site background and the precedence");
                routing.Should().NotContain(token, because: "routing names the owner, not its tools or settings");
            }
            routing.Should().Contain("site background image, or choosing it over a theme's own background -> name=branding",
                because: "a request that does not name the level must reach the guide that picks it");
            routing.Should().Contain("set the default) / a theme's own background -> name=theming",
                because: "a theme background request must reach the guide that owns its arguments");
        }
    }

    private static void ShouldContainAll(string relativePath, string because, params string[] clauses)
    {
        string guide = ReadCollapsed(relativePath);

        using (new AssertionScope())
        {
            foreach (string clause in clauses)
            {
                guide.Should().Contain(clause, because: because);
            }
        }
    }

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar))),
            @"\s+",
            " ");
}

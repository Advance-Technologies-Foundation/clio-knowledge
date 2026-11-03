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
    [Description("Branding keeps every background rule in one Background block, like Logos & favicon.")]
    public void Branding_ShouldKeepTheBackgroundInOneBlock()
    {
        string raw = File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), BrandingPath.Replace('/', Path.DirectorySeparatorChar)));

        Regex.Matches(raw, @"^Background\b", RegexOptions.Multiline).Count.Should().Be(1,
            because: "the background is one section of the branding guide, not a set of sub-sections");
    }

    [Test]
    [Description("The levels, the branding-run default and the precedence are stated once.")]
    public void Branding_ShouldStateTheLevelsAndThePrecedence()
    {
        ShouldContainAll(
            "the creatio-ui AppBackgroundService resolves the background in these steps, and a generated background belongs to the new theme",
            "A theme background is an image or a color; the site background is an image for every account whose theme has none.",
            "In a branding run, give the new theme its background; set the site background only when the user asks for the whole site.",
            "An account sees the first that applies: its personal `UseAccessibleDesktopColor` system setting; its theme's color, or its image if it loads, unless the `DisableThemeBackground` feature is on; the site background (the `CrtBackgroundConfig` system setting); the bundled default image.");
    }

    [Test]
    [Description("The theme level is gated once on the Creatio floor and the tool contract, and the site fallback needs the user's consent.")]
    public void Branding_ShouldGateTheThemeLevelOnTheFloorAndTheContract()
    {
        string guide = ReadCollapsed(BrandingPath);

        using (new AssertionScope())
        {
            guide.Should().Contain(
                "Theme backgrounds need Creatio 10.2.96 or later and a clio whose `get-tool-contract` returns `set-background-color` and `remove-background`. When this clio lacks them (it can be updated) or a tool answers `version-too-old`, tell the user; set the site background instead only if the user agrees, even when the error suggests it.",
                because: "a ThemeService before 10.2.96 refuses the write, and the clio hint names the site background");
            Regex.Matches(guide, @"10\.2\.96").Count.Should().Be(1, because: "the floor has one owner sentence");
        }
    }

    [Test]
    [Description("The three tools, their levels and the read shape are stated with the limits a green call does not show.")]
    public void Branding_ShouldStateTheToolsAndTheirLimits()
    {
        ShouldContainAll(
            "these are the clio contract's background operations and their limits",
            "`set-background-image` — `file`, or the `image-id` of an `upload-image` result; with `theme` it sets that theme's background, without it the site background. With `file`, a wrong `theme` still leaves the image uploaded.",
            "`set-background-color` — `color` and `theme`.",
            "`remove-background` — `theme`; safe to repeat.",
            "Only these change a theme background. `get-theme` and `list-themes` return it as `background` (`mode` `Image` with `imageId`, or `Color` with `color`), absent when the theme has none.");
    }

    [Test]
    [Description("The panel icon turn-off is environment-wide, its off-state is bound only when verified, and removal keeps the image bindings.")]
    public void Branding_ShouldStateThePanelIconAndThePackage()
    {
        ShouldContainAll(
            "the panel icon is environment-wide, and the install target needs the image row that removal does not drop",
            "The set tools also turn off the panel icon background (the `UsePanelIconBackground` feature), which can cover the background, for the whole environment;",
            "Package: a theme's image is bound under the theme's own folders, apart from the site background's; a color travels inside the theme and binds nothing.",
            "The panel icon off-state is bound only when the All-Users state row reads off",
            "`remove-background` and `delete-theme` leave the image bindings, so the package still ships the image; tell the user.");
    }

    [Test]
    [Description("A hidden background is explained without changing the accessibility setting or the kill switch.")]
    public void Branding_ShouldExplainAHiddenBackground()
    {
        ShouldContainAll(
            "a hidden background has causes no background write fixes",
            "tell them which one wins, or that the panel icon background covers it, or that the theme's image was deleted (`get-theme` still reports its `imageId`; set a new one).",
            "Never change `UseAccessibleDesktopColor` or `DisableThemeBackground` to make a background show.");
    }

    [Test]
    [Description("Theming and routing point to branding and state none of the background rules.")]
    public void ThemingAndRouting_ShouldPointToBrandingWithoutTheBackgroundRules()
    {
        string theming = ReadCollapsed(ThemingPath);
        string routing = ReadCollapsed(RoutingPath);

        string[] pointers =
        [
            "`get-guidance name=branding` covers the product logos, the favicon, and backgrounds",
            "To add a background to the theme, see `get-guidance name=branding` → \"Background\".",
            "for a theme with an image background see `get-guidance name=branding` → \"Background\""
        ];
        string[] brandingOnly =
        [
            "set-background-image",
            "set-background-color",
            "remove-background",
            "UsePanelIconBackground",
            "DisableThemeBackground",
            "UseAccessibleDesktopColor",
            "CrtBackgroundConfig",
            "10.2.96"
        ];

        using (new AssertionScope())
        {
            foreach (string pointer in pointers)
            {
                theming.Should().Contain(pointer, because: "the theming guide must name where the background is owned");
            }
            foreach (string token in brandingOnly)
            {
                theming.Should().NotContain(token, because: "branding owns every background rule");
                routing.Should().NotContain(token, because: "routing names the owner, not its tools or settings");
            }
            routing.Should().Contain("browser-tab favicon / theme or site background -> name=branding",
                because: "a theme background request must route to branding");
        }
    }

    private static void ShouldContainAll(string because, params string[] clauses)
    {
        string guide = ReadCollapsed(BrandingPath);

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

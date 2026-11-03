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
    [Description("Branding owns one precedence list: accessibility color, theme background, site background, bundled image.")]
    public void Branding_ShouldOwnThePrecedenceList()
    {
        string guide = ReadCollapsed(BrandingPath);

        string[] rows =
        [
            "1. Accessibility color — its personal `UseAccessibleDesktopColor` system setting is on.",
            "2. Theme image or color — its theme declares one (`set-background-image` or `set-background-color` with `theme`), the image loads, and the `DisableThemeBackground` feature is off.",
            "3. Site background — `set-background-image` without `theme` (the `CrtBackgroundConfig` system setting).",
            "4. Bundled default image."
        ];
        int[] positions = [.. rows.Select(row => guide.IndexOf(row, StringComparison.Ordinal))];

        using (new AssertionScope())
        {
            positions.Should().NotContain(-1, because: "the creatio-ui AppBackgroundService resolves the background in these four steps");
            positions.Should().BeInAscendingOrder(because: "the first item that applies is what the account sees");
        }
    }

    [Test]
    [Description("The theme level is gated once on the Creatio floor and the tool contract, and the site fallback needs the user's consent.")]
    public void Branding_ShouldGateTheThemeLevelOnTheFloorAndTheContract()
    {
        string guide = ReadCollapsed(BrandingPath);

        using (new AssertionScope())
        {
            guide.Should().Contain(
                "The theme level needs Creatio 10.2.96 or later and a clio whose `get-tool-contract` for `set-background-color` and `remove-background` returns both. When the tools are missing (clio can be updated) or a tool answers `version-too-old`, tell the user; set the site background instead only if the user agrees, even when the error suggests it.",
                because: "a ThemeService before 10.2.96 refuses the write, and the clio hint names the site background");
            Regex.Matches(guide, @"10\.2\.96").Count.Should().Be(1, because: "the floor has one owner sentence");
        }
    }

    [Test]
    [Description("The tools section states each operation and the limits a green call does not show.")]
    public void Branding_ShouldStateTheToolsAndTheirLimits()
    {
        ShouldContainAll(
            BrandingPath,
            "these are the clio contract's theme-level operations and their limits",
            "`set-background-image`: exactly one of `file` (uploaded) or `image-id` (from `upload-image`); with `theme` it sets that theme's background, without it the site background. With `file` the upload runs before `theme` is checked, so a wrong id leaves the image uploaded.",
            "`set-background-color`: `color` (`#RGB`, `#RRGGBB` or `#RRGGBBAA`) and `theme`; there is no site color.",
            "`remove-background`: `theme`; safe to repeat.",
            "`theme` is the `id` from `create-theme` or `list-themes`; only these three tools change a theme's background.",
            "`get-theme` and `list-themes` return `background`: `{ \"mode\": \"Image\", \"imageId\": \"<id>\" }` or `{ \"mode\": \"Color\", \"color\": \"<hex>\" }`, absent if the theme has none.");
    }

    [Test]
    [Description("A branding run puts the background into its new theme; the site background is set only on request.")]
    public void Branding_ShouldPutTheRunBackgroundIntoTheNewTheme()
    {
        ShouldContainAll(
            BrandingPath,
            "a generated background belongs to the theme being built, not to every account on the site",
            "In a branding run, give the new theme the background (`theme` = the `id` that `create-theme` returned).",
            "Set the site background only when the user asks for the whole site.");
    }

    [Test]
    [Description("The panel icon turn-off is environment-wide, and only set-background-image binds its off-state.")]
    public void Branding_ShouldKeepThePanelIconRuleForBothLevels()
    {
        ShouldContainAll(
            BrandingPath,
            "both tools turn the feature off, but only the image tool ships the off-state",
            "`set-background-image` and `set-background-color` turn off the panel icon background (the `UsePanelIconBackground` feature), which can cover the background.",
            "The feature is environment-wide, so a theme background turns it off on every theme.",
            "Only `set-background-image` binds the off-state into the package, and only when the All-Users state row reads off; otherwise relay its `warnings` entry");
    }

    [Test]
    [Description("A theme image is bound under the theme's folders in the target package; a color binds nothing; removal keeps the image bindings.")]
    public void Branding_ShouldStateThePackageOfAThemeBackground()
    {
        ShouldContainAll(
            BrandingPath,
            "the install target needs the image row, and clio does not drop it on removal",
            "A theme image is bound into `package` under the theme's own folders, apart from the site background's bindings. Pass the package from \"Target package\" — in a branding run, the theme's.",
            "`set-background-color` and `remove-background` bind nothing; a color travels inside the theme.",
            "`remove-background` and `delete-theme` leave the image bindings, so the package still ships the image; tell the user.");
    }

    [Test]
    [Description("A hidden background is explained from the list and two extra causes, without changing the accessibility setting or the kill switch.")]
    public void Branding_ShouldExplainAHiddenBackground()
    {
        ShouldContainAll(
            BrandingPath,
            "a hidden background has causes no background write fixes",
            "Tell the user which item above wins for their account, or that the panel icon covers the background, or that the theme's image was deleted (`get-theme` still reports its `imageId`; set a new one).",
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
            "for a theme with an image background see `get-guidance name=branding` → \"Background: package\""
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

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

        string[] items =
        [
            "1. Accessibility color — its personal `UseAccessibleDesktopColor` system setting is on.",
            "2. Theme background — its theme has a color or an image that loads, and the `DisableThemeBackground` feature is off.",
            "3. Site background (the `CrtBackgroundConfig` system setting).",
            "4. Bundled default image."
        ];
        int[] positions = [.. items.Select(item => guide.IndexOf(item, StringComparison.Ordinal))];

        using (new AssertionScope())
        {
            guide.Should().Contain("There are two levels: a theme background (an image or a color) and the site background (an image).",
                because: "the site level takes no color");
            positions.Should().NotContain(-1, because: "the creatio-ui AppBackgroundService resolves the background in these four steps");
            positions.Should().BeInAscendingOrder(because: "the first item that applies is what the account sees");
        }
    }

    [Test]
    [Description("A branding run sets the new theme's background, and the theme level is gated once on the Creatio floor and the tool contract.")]
    public void Branding_ShouldGateTheThemeLevelOnTheFloorAndTheContract()
    {
        string guide = ReadCollapsed(BrandingPath);

        using (new AssertionScope())
        {
            guide.Should().Contain("In a branding run, give the new theme its background; set the site background only when the user asks for the whole site.",
                because: "a generated background belongs to the theme being built, not to every account on the site");
            guide.Should().Contain(
                "Theme backgrounds need Creatio 10.2.96 or later and a clio whose `get-tool-contract` for `set-background-color` and `remove-background` returns both. When the tools are missing (clio can be updated) or a tool answers `version-too-old`, tell the user; set the site background instead only if the user agrees, even when the error suggests it.",
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
            "Only the three tools below change a theme background; `theme` is the `id` from `create-theme` or `list-themes`.",
            "`set-background-image` — exactly one of `file` (uploaded) or `image-id` (from `upload-image`); with `theme` it sets the theme background, without it the site background. With `file` the upload runs before `theme` is checked, so a wrong id leaves the image uploaded.",
            "`set-background-color` — `color` (`#RGB`, `#RRGGBB` or `#RRGGBBAA`) and `theme`.",
            "`remove-background` — `theme`; safe to repeat.",
            "`get-theme` and `list-themes` return the theme background as `{ \"mode\": \"Image\", \"imageId\": \"<id>\" }` or `{ \"mode\": \"Color\", \"color\": \"<hex>\" }` in `background`, absent if the theme has none.");
    }

    [Test]
    [Description("The panel icon turn-off is environment-wide, and only set-background-image binds its off-state.")]
    public void Branding_ShouldKeepThePanelIconRuleForBothLevels()
    {
        ShouldContainAll(
            BrandingPath,
            "both tools turn the feature off, but only the image tool ships the off-state",
            "The panel icon background (the `UsePanelIconBackground` feature) can cover the background. `set-background-image` and `set-background-color` turn it off environment-wide, on every theme;",
            "It also binds the panel icon off-state, but only when the All-Users state row reads off;");
    }

    [Test]
    [Description("A theme image is bound under the theme's folders; a color binds nothing; removal keeps the image bindings.")]
    public void Branding_ShouldStateThePackageOfAThemeBackground()
    {
        ShouldContainAll(
            BrandingPath,
            "the install target needs the image row, and clio does not drop it on removal",
            "`set-background-image` binds a theme background's image under the theme's own folders, apart from the site background's bindings.",
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
            "Tell the user which item above wins for their account, or that the panel icon background covers it, or that the theme's image was deleted (`get-theme` still reports its `imageId`; set a new one).",
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

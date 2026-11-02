using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ThemeBackgroundGuidanceTests
{
    private const string BrandingPath = "guidance/mcp/guides/theming/branding.md";

    [Test]
    [Description("The branding guide owns one precedence table: the accessibility color, then the theme background behind the DisableThemeBackground switch, then the site background, then the bundled image.")]
    public void Branding_ShouldOwnThePrecedenceTable()
    {
        string guide = ReadCollapsed(BrandingPath);

        string[] rows =
        [
            "| 1 | Accessibility color | the personal `UseAccessibleDesktopColor` system setting; no branding tool writes it | the setting is on |",
            "| 2 | Theme background: an image or a color | `set-background-image` or `set-background-color` with `theme` | the account's current theme declares one, its image loads, and the `DisableThemeBackground` feature is off for the account |",
            "| 3 | Site background | `set-background-image` without `theme` (the `CrtBackgroundConfig` system setting) | rows 1 and 2 do not apply |",
            "| 4 | Bundled default image | — | no site background is set |"
        ];
        int[] positions = [.. rows.Select(row => guide.IndexOf(row, StringComparison.Ordinal))];

        using (new AssertionScope())
        {
            positions.Should().NotContain(-1,
                because: "the creatio-ui AppBackgroundService resolves the background in exactly these four steps");
            positions.Should().BeInAscendingOrder(
                because: "the table is read top-down: the first row that applies is what the account sees");
        }
    }

    [Test]
    [Description("The theme level is gated inline on the Creatio floor and on a clio whose contract has the theme tools, and the floor is stated once.")]
    public void Branding_ShouldGateTheThemeLevelOnTheFloorAndTheContract()
    {
        string guide = ReadCollapsed(BrandingPath);

        const string gate = "The theme level needs Creatio 10.2.96 or later and a clio whose `get-tool-contract` with `tool-names: [\"set-background-color\", \"remove-background\"]` returns both tools; that clio also takes `theme` in `set-background-image` and returns `background` from `get-theme` / `list-themes`.";
        int floorMentions = Regex.Matches(guide, @"10\.2\.96").Count;

        using (new AssertionScope())
        {
            guide.Should().Contain(gate,
                because: "a ThemeService before 10.2.96 refuses the background write, and a released clio without the tools cannot send it");
            floorMentions.Should().Be(1,
                because: "the version floor has one owner sentence; the fallback refers to it instead of restating it");
        }
    }

    [Test]
    [Description("In a branding run the background goes into the theme the run created, and the site background is set only on the user's request.")]
    public void Branding_ShouldPutTheRunBackgroundIntoTheNewTheme()
    {
        string guide = ReadCollapsed(BrandingPath);

        const string runDefault = "Put the background into the theme the run creates: after `create-theme`, call `set-background-image` with `theme` set to the `id` it returned.";
        const string siteOnRequest = "Call `set-background-image` without `theme` only when the user asks for a background for the whole site: every account whose theme declares no background sees it.";
        int onRequestMentions = Regex.Matches(guide, "only when the user asks for").Count;

        using (new AssertionScope())
        {
            guide.Should().Contain(runDefault,
                because: "a generated background belongs to the theme being built, not to every account on the site");
            guide.Should().Contain(siteOnRequest,
                because: "the site background reaches accounts on every theme that declares none");
            onRequestMentions.Should().Be(1,
                because: "the site-on-request rule lives in its own section and the run section points to it");
        }
    }

    [Test]
    [Description("An existing theme is read with get-theme and changed with set-background-image, set-background-color or remove-background; update-theme keeps the stored background and a site color is unsupported.")]
    public void Branding_ShouldRouteAnExistingThemeToTheThreeTools()
    {
        string guide = ReadCollapsed(BrandingPath);

        string[] clauses =
        [
            "Read its current background with `get-theme`: `background` is `{ \"mode\": \"Image\", \"imageId\": \"<id>\" }` or `{ \"mode\": \"Color\", \"color\": \"<hex>\" }`, and absent when the theme declares none",
            "`set-background-color` takes `color` (`#RGB`, `#RRGGBB` or `#RRGGBBAA`) and `theme`, and replaces that theme's background with the color. A site color is not supported.",
            "`remove-background` takes `theme`; the theme's accounts see the site background again. A repeat run succeeds.",
            "`create-theme` and `update-theme` take no background, and `update-theme` keeps the stored one; only the three tools above change it.",
            "With `file` the upload runs before the theme is checked, so a wrong id leaves the uploaded image behind."
        ];

        using (new AssertionScope())
        {
            foreach (string clause in clauses)
            {
                guide.Should().Contain(clause,
                    because: "these are the clio contract's theme-level operations and the limits an agent cannot see from a green call");
            }
        }
    }

    [Test]
    [Description("The panel icon turn-off is environment-wide even for a theme background, and only set-background-image ships its off-state.")]
    public void Branding_ShouldKeepThePanelIconRuleForBothLevels()
    {
        string guide = ReadCollapsed(BrandingPath);

        using (new AssertionScope())
        {
            guide.Should().Contain("`set-background-image` and `set-background-color` also turn off the panel's own icon background for all users (the `UsePanelIconBackground` feature)",
                because: "both tools turn the feature off unless keep-icon-background is passed");
            guide.Should().Contain("The feature is environment-wide, so a theme background turns it off on every theme.",
                because: "a theme background still changes the panel for accounts on other themes");
            guide.Should().Contain("Only `set-background-image` binds the off-state with the background, so the install target inherits it.",
                because: "set-background-color binds nothing, so a color ships without the off-state");
        }
    }

    [Test]
    [Description("The theme and its background share the target package, a color binds nothing, and removal leaves the image bindings, which the agent tells the user.")]
    public void Branding_ShouldStateThePackageOfAThemeBackground()
    {
        string guide = ReadCollapsed(BrandingPath);

        using (new AssertionScope())
        {
            guide.Should().Contain("Pass the package resolved under \"Target package\" — in a branding run, the one the theme was created in.",
                because: "the theme carries the reference and the package carries the image; apart, the install target shows the site background");
            guide.Should().Contain("`set-background-color` and `remove-background` bind nothing; a color travels inside the theme.",
                because: "only an image needs a SysImage row on the install target");
            guide.Should().Contain("`remove-background` and `delete-theme` leave the image bindings in the package, which keeps shipping the image. Tell the user so",
                because: "clio does not drop the bindings, so the package ships an image no theme uses");
        }
    }

    [Test]
    [Description("Without the theme level the agent asks the user before falling back to the site background, and it names why a background may not show without changing the accessibility setting or the kill switch.")]
    public void Branding_ShouldAskBeforeTheSiteFallbackAndExplainAHiddenBackground()
    {
        string guide = ReadCollapsed(BrandingPath);

        string[] clauses =
        [
            "No theme can carry a background when this clio lacks the theme-level tools (the user can update clio) or the environment is below the Creatio floor (the tool answers `version-too-old`).",
            "Tell the user, and set the site background (see \"Background: site\") instead only after the user agrees, even when the tool's error suggests it.",
            "`UseAccessibleDesktopColor` is on: the accessibility color replaces every background.",
            "`DisableThemeBackground` is on for them: the site background shows. A change to the feature shows on the next application load.",
            "The theme's image no longer loads (its `SysImage` was deleted): the site background shows, and `get-theme` still reports the `imageId`.",
            "Never change `UseAccessibleDesktopColor` or `DisableThemeBackground` to make a background show."
        ];

        using (new AssertionScope())
        {
            foreach (string clause in clauses)
            {
                guide.Should().Contain(clause,
                    because: "the clio version-floor hint names the site background, which every account without a theme background sees, and a hidden background has causes no write can fix");
            }
        }
    }

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar))),
            @"\s+",
            " ");
}

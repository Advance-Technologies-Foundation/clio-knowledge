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

    [TestCase(Theming, "Only `create-theme` and `update-theme` write it.")]
    [TestCase(Theming, "pass at most one of `background-image-id` (upload the file with `upload-image` first) or `background-color` to `create-theme` (brand mode too) or `update-theme`; `remove-background` on `update-theme` clears it.")]
    [TestCase(Theming, "With a background argument `update-theme` needs only the `id` — the omitted `caption`, `css-class-name` and `css-content` keep their values, so never send the CSS for a background change.")]
    [TestCase(Theming, "Pass a theme background in this same call — see \"Theme background\".")]
    [TestCase(Theming, "a full overwrite of caption + css-class-name + css-content, unless the call only changes the background")]
    [TestCase(Theming, "Version: Creatio 10.2.96 or later; the write tools refuse an older environment before writing. A clio whose `create-theme` contract has no `background-color` cannot set one. Either way tell the user, and set the site background instead only if the user agrees.")]
    [TestCase(Theming, "an environment-wide change, so tell the user before the write.")]
    [TestCase(Theming, "A failed turn-off is a warning.")]
    [TestCase(Theming, "a failed write changes nothing else.")]
    [TestCase(Theming, "The panel icon state is not bound, so on another environment it can still cover the background.")]
    [TestCase(Theming, "A binding problem is a warning — the theme exists, do not create it again; relay the warnings (`create-theme` returns them in `warnings`, `update-theme` in its log).")]
    [TestCase(Theming, "`remove-background` and `delete-theme` keep the image binding, so the package still ships the image — tell the user.")]
    [TestCase(Theming, "`cssClassName`, `cssFilePath`, and `background`")]
    [TestCase(Theming, "cssContentLength, background?, error? }")]
    [TestCase(Branding, "A background has two levels: a theme's own background (an image or a color) is seen by that theme's accounts, the site background (an image) by every account whose theme has none.")]
    [TestCase(Branding, "In a branding run, give the new theme its background; set the site background only when the user asks for the whole site.")]
    [TestCase(Branding, "A theme background is written with the theme — see `get-guidance name=theming` → \"Theme background\".")]
    [TestCase(Branding, "The site background cannot be a color or be removed, only replaced with another image.")]
    [TestCase(Branding, "An account sees the first that applies: its `UseAccessibleDesktopColor` system setting; its theme's color, or its image if it loads, unless the `DisableThemeBackground` feature is on; the site background (`CrtBackgroundConfig`); the bundled default image.")]
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

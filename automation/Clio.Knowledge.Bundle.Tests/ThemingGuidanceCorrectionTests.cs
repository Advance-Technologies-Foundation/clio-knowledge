using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ThemingGuidanceCorrectionTests
{
    [Test]
    [Description("Keeps an unresolved version probe distinct from a confirmed unsupported Creatio version.")]
    public void ThemingGuide_ShouldNotTreatUndeterminableVersionAsTooOld()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/theming/index.md"));

        // Assert
        guide.Should().Contain("`version-too-old` means the resolved version is below 10.0.0",
            because: "only a successfully resolved old version proves the theming requirement is unmet");
        guide.Should().Contain("`version-undeterminable` means the environment was reachable but did not expose a parseable version",
            because: "a failed version extraction needs registration/connectivity recovery rather than an unsupported-version verdict");
        guide.Should().Contain("verify the registered URI, authentication, and runtime routing with `get-info`",
            because: "the caller needs one bounded recovery step before stopping on an unresolved probe");
        guide.Should().Contain("identifies a specific incorrect or missing registration setting",
            because: "re-registering identical live probe inputs cannot change the resolved version");
        guide.Should().Contain("preserving every untouched authentication, workspace, runtime, and safety setting",
            because: "reg-web-app replacement defaults must not silently weaken or erase the existing registration");
        guide.Should().Contain("stop rather than creating a duplicate registration",
            because: "a fresh name is not a recovery when no concrete registration mismatch was found");
    }

    [Test]
    [Description("Keeps the supported no-code embedded-font path and its size/licensing safeguards discoverable.")]
    public void ThemingGuide_ShouldDocumentEmbeddedFontDataUriBoundaries()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/theming/index.md"));
        string nonGoogleDecision = guide.Split('\n')
            .Single(line => line.TrimEnd('\r')
                .StartsWith("- When a family is not on Google Fonts", StringComparison.Ordinal));

        // Assert
        guide.Should().Contain("`@font-face` `data:` URI",
            because: "ready-made CSS can carry a local webfont without a separate binary upload endpoint");
        nonGoogleDecision.Should().Contain("Offer three choices: find the published spelling or pick an alternative",
            because: "the non-Google-font decision branch must route a supplied font file to embedded CSS rather than generic fallback");
        nonGoogleDecision.Should().Contain("user-approved trusted source",
            because: "an environment-wide theme must not embed untrusted font bytes in every user's browser");
        nonGoogleDecision.Should().Contain("`@font-face` `data:` URI",
            because: "the supplied-font option must be actionable inside the decision branch itself");
        guide.Should().Contain("1 MiB UTF-8 CSS limit",
            because: "base64 expansion can otherwise make an accepted design exceed the tool contract");
        nonGoogleDecision.Should().Contain("licence permits web embedding",
            because: "technical support for a data URI does not grant redistribution rights");
    }

    [Test]
    [Description("Keeps branding licence checks separate from the image API transport used by background uploads.")]
    public void BrandingGuide_ShouldSeparateAccessFromImageTransport()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/theming/branding.md"));

        // Assert
        guide.Should().Contain("successful access check proves the branding licence/rights only",
            because: "check-theming-access does not establish image API connectivity");
        guide.Should().Contain("`set-logo` writes Binary system settings",
            because: "a successful logo path must not be used as proof that the background image path works");
        guide.Should().Contain("authentication, proxy, or CSRF configuration",
            because: "the caller needs the concrete transport boundary to diagnose the failing tool");
    }

    [Test]
    [Description("Keeps a recolour from carrying the old primary's derived secondary into the new theme.")]
    public void ThemingGuide_ShouldReDeriveDerivedColoursWhenThePrimaryChanges()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/theming/index.md"));
        string restyleStep = guide.Split('\n')
            .Single(line => line.StartsWith("2. Restyle with `update-theme`", StringComparison.Ordinal));

        // Assert
        guide.Should().Contain("When the primary changes, re-derive the secondary and accent from the new primary",
            because: "a recolour that keeps the old derived secondary renders the new brand with the old one's palette");
        guide.Should().Contain("A colour the tool derived is never a user choice",
            because: "a secondary the agent passed to create-theme is still derived, not a settled user choice");
        restyleStep.Should().Contain("To change the primary, rebuild the CSS with `build-theme`",
            because: "swapping only the primary values in the get-theme CSS leaves every dependent token on the old brand");
    }

    [Test]
    [Description("Removes a theme background with a null background argument on update-theme.")]
    public void ThemingGuide_ShouldRemoveAThemeBackgroundWithNull() =>
        ReadThemingGuide().Should().Contain("Pass either one as null to `update-theme` to remove it",
            because: "update-theme has no separate removal argument, so a null image id or color is the only way to remove a background");

    [Test]
    [Description("Keeps the confirmed theme when the Creatio version refuses its background.")]
    public void ThemingGuide_ShouldKeepTheThemeWhenTheVersionRefusesItsBackground()
    {
        string guide = ReadThemingGuide();

        guide.Should().Contain("After `create-theme`, tell the user and repeat it without the background",
            because: "below 10.2.96 the confirmed theme can still be created without its background");
        guide.Should().Contain("never fall back to `create-theme`",
            because: "a refused update leaves the existing theme as it is, and a create would add a second theme");
        guide.Should().Contain("for a call with a background argument see \"Theme background\" instead",
            because: "the Constraints rule for a version below 10.0.0 must not stop a call the background floor refused");
    }

    [Test]
    [Description("Announces the environment-wide panel icon turn-off before a theme background is written.")]
    public void ThemingGuide_ShouldAnnounceThePanelIconTurnOffBeforeTheWrite() =>
        ReadThemingGuide().Should().Contain("That is environment-wide: tell the user before the write",
            because: "a theme background changes the panel for every account, not only for the theme's accounts");

    [Test]
    [Description("Binds an existing theme's background image into the theme's own package.")]
    public void ThemingGuide_ShouldBindAnExistingThemesImageIntoItsOwnPackage() =>
        ReadThemingGuide().Should().Contain("for an existing theme the Pkg/<package> segment of its `cssFilePath`",
            because: "an existing theme lives in its own package, not in a branding run's target package");

    [Test]
    [Description("Warns that the panel icon state does not travel with a theme background's package.")]
    public void ThemingGuide_ShouldWarnThatThePanelIconStateIsNotBound() =>
        ReadThemingGuide().Should().Contain("The panel icon state is not bound, so elsewhere the icons can cover the background: tell the user.",
            because: "no clio result says whether that package carries the panel icon state, so the warning names no condition the agent cannot check");

    [Test]
    [Description("Names the image size cap before a background image is uploaded.")]
    public void Guides_ShouldNameTheBackgroundImageSizeCap()
    {
        ReadThemingGuide().Should().Contain("(upload the file, 10 MB at most, with `upload-image` first)",
            because: "clio refuses a larger image, and the agent must know that before the user confirms the write");
        ReadBrandingGuide().Should().Contain("(`file`, 10 MB at most)",
            because: "set-background-image uploads through the same capped image path");
    }

    [Test]
    [Description("Says what a translucent theme color lies over.")]
    public void ThemingGuide_ShouldSayWhatATranslucentColorShowsThrough() =>
        ReadThemingGuide().Should().Contain("say only the plain page shows through it",
            because: "a theme color lies over the plain page, not over the shell background or an earlier image");

    [Test]
    [Description("Reads a feature with no definition as off.")]
    public void BrandingGuide_ShouldReadAFeatureWithNoDefinitionAsOff() =>
        ReadBrandingGuide().Should().Contain("a feature with no `Feature` row is off",
            because: "with no definition there is no DefaultState to fall back on, and the platform reads the feature as off");

    [Test]
    [Description("Names the reads that place an account's roles in the role tree.")]
    public void BrandingGuide_ShouldNameTheReadsForARolesLevel() =>
        ReadBrandingGuide().Should().Contain("An account's roles are its `SysAdminUnitInRole` rows, and a role's level is the number of `SysAdminUnit.ParentRole` steps from it to a role with none.",
            because: "the deepest-level rule needs each role's depth, also for roles on different branches");

    [Test]
    [Description("Keeps the inactive-theme setting out of the changes made to show a background.")]
    public void BrandingGuide_ShouldNotChangeInactiveThemesToShowABackground() =>
        ReadBrandingGuide().Should().Contain("`DisableChangeTheme` or `InactiveThemes` to make a background show",
            because: "InactiveThemes applies to every account on that theme, so it is the administrator's call");

    [Test]
    [Description("Keeps the theming article within one get-guidance response.")]
    public void ThemingGuide_ShouldFitOneGetGuidanceResponse() =>
        ProcessGuideResponseSizeTests.ResponseSize(FindRepositoryRoot(), ThemingGuidePath)
            .Should().BeLessThanOrEqualTo(ProcessGuideResponseSizeTests.MaxResponseCharacters,
                because: "an article that spills out of one response reaches the agent only in part");

    private const string ThemingGuidePath = "guidance/mcp/guides/theming/index.md";

    private static string ReadThemingGuide() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), ThemingGuidePath));

    private static string ReadBrandingGuide() =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), "guidance/mcp/guides/theming/branding.md"));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

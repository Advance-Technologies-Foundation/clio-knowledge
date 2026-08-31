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

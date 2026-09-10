using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the mobile guide's "web-only" component list and the ENG-91859 cutover note attached to it.
/// </summary>
/// <remarks>
/// The list is a claim about the CATALOG, not about the Flutter runtime, and two of its entries are only
/// true while the published mobile catalog is still the 46 web-derived types: `crt.DataGrid` and
/// `crt.IFrame` exist in the runtime and stop being web-only the day ENG-91859 publishes the
/// runtime-derived catalog. No test in this repository can observe that publication, so these tests do not
/// claim to detect it. What they do is remove the silent-drift mode: the ten names and the cutover note
/// cannot be edited, reordered or deleted without this file failing, so any change to the claim is a
/// deliberate, reviewed one.
/// </remarks>
[TestFixture]
public sealed class MobileWebOnlyComponentListTests
{
    private const string GuideRelativePath = "guidance/mcp/guides/platform/mobile/page-modification.md";

    [Test]
    [Description("Pins the exact web-only component list in the mobile page-modification guide so the claim cannot change without review.")]
    public void MobilePageModificationGuidance_ShouldPinTheWebOnlyComponentList()
    {
        // Arrange
        string guidePath = Path.Combine(FindRepositoryRoot(), GuideRelativePath);

        // Act
        // Newlines normalised: .gitattributes pins these files to LF, but a multi-line pin that also
        // asserted the line ending would fail on a checkout that ignored it, for no useful reason.
        string guidance = File.ReadAllText(guidePath).Replace("\r\n", "\n");

        // Assert
        guidance.Should().Contain(
                "NOT available in mobile (web-only):\n"
                + "  crt.DataGrid, crt.HtmlEditor, crt.PasswordInput, crt.EncryptedInput,\n"
                + "  crt.ColorPicker, crt.TagSelect, crt.MultiSelect, crt.IFrame,\n"
                + "  crt.Chat, crt.Dashboards",
                because: "the ten names are a claim about the published mobile catalog and each one steers an "
                    + "agent away from a component; adding, removing or reordering one must be a reviewed edit, "
                    + "not a silent drift")
            .And.NotContain("crt.ChartWidget, ",
                because: "crt.ChartWidget and crt.IndicatorWidget ARE mobile components — the section below the "
                    + "list says so explicitly, and listing them here would contradict it");
    }

    [Test]
    [Description("Keeps the ENG-91859 cutover dependency recorded next to the list a reader acts on, naming the two entries that stop being true when the runtime-derived catalog publishes.")]
    public void MobilePageModificationGuidance_ShouldRecordTheEng91859CutoverDependency()
    {
        // Arrange
        string guidePath = Path.Combine(FindRepositoryRoot(), GuideRelativePath);

        // Act
        // Newlines normalised: .gitattributes pins these files to LF, but a multi-line pin that also
        // asserted the line ending would fail on a checkout that ignored it, for no useful reason.
        string guidance = File.ReadAllText(guidePath).Replace("\r\n", "\n");

        // Assert
        guidance.Should().Contain("CUTOVER DEPENDENCY — ENG-91859",
                because: "a reader of the web-only list has to be told that two of its entries are catalog "
                    + "facts with a known expiry, and the note has to sit where that reader already is")
            .And.Contain("`crt.DataGrid` and `crt.IFrame` DO exist in the Flutter runtime",
                because: "the note is only actionable if it names WHICH entries expire; without the names a "
                    + "future editor cannot tell which part of the list the warning is about")
            .And.Contain("46 mobile types extracted from the web monorepo",
                because: "the claim is scoped to a specific published catalog, and stating which one is what "
                    + "makes the expiry checkable rather than a standing hedge");
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

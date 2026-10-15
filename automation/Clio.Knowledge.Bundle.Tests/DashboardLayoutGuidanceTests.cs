using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the metric-widget rules of <c>dashboard-and-home-page-layout</c> and the lines that route the sibling
/// widget guides to it.
/// </summary>
/// <remarks>
/// The rules come from the stock product pages, read with <c>get-page</c> on a Sales/Service Enterprise
/// environment on 2026-10-15: the <c>Main</c> grid has 28px rows on a dashboard and 64px rows on a home page,
/// so one metric <c>rowSpan</c> for both surfaces renders a 100px tile on one and a 208px tile on the other;
/// no stock metric is wider than 4 columns; every stock dashboard metric has an icon; and four widget color
/// presets fall below a 3:1 WCAG contrast ratio on the white card. The guide owns those rules,
/// <c>indicator-widget</c> and <c>chart-widget</c> only cite it, so each guide is pinned for its own part.
/// </remarks>
[TestFixture]
public sealed class DashboardLayoutGuidanceTests
{
    private const string LayoutPath = "guidance/mcp/guides/dashboards/layout.md";
    private const string ChartWidgetPath = "guidance/mcp/guides/dashboards/chart-widget.md";
    private const string IndicatorWidgetPath = "guidance/mcp/guides/dashboards/indicator-widget.md";
    private const string LayoutOwner = "`dashboard-and-home-page-layout`";

    /// <summary>The chart size floor as published before the color rule was added; it must not move.</summary>
    private const string ChartSizeFloor =
        "- SIZE FLOOR — never ship a tiny chart. In a `crt.GridContainer` set `layoutConfig.rowSpan` >= 6\n"
        + "  (platform default 9; funnels 15). In a `crt.FlexContainer` the parent uses `FlexLayoutConfig` —\n"
        + "  set `layoutConfig.height` >= 350 (px) so the flex child doesn't collapse.";

    private static readonly string[] BannedColors = ["orange-red", "orange", "light-green", "turquoise"];

    [Test]
    [Description("The layout guide sizes metric tiles per surface from the Main grid rows read with get-page, not with one rowSpan for both.")]
    public void LayoutGuide_ShouldSizeMetricTilesPerSurface()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string[] lines = Lines(guide);

        // Assert
        guide.Should().Contain("read the `rows` of the page's `Main` `crt.GridContainer` with `get-page`",
            because: "the row height decides how tall a rowSpan renders, and it differs per surface");
        lines.Should().Contain("| Dashboard (`BaseDashboardTemplate`) | `28px` | 3, or 2 in a dense band | 3 |",
            because: "a dashboard metric is 3 rows of 28px, about 100px tall, as on the stock dashboards");
        lines.Should().Contain("| Home page (`BaseHomePage`) | `64px` | 3, or 2 in a dense band | 2 |",
            because: "a home-page metric is 2 rows of 64px; rowSpan 3 there renders the bloated 208px tile");
        guide.Should().NotContainAny(
            ["metric tile 3, gauge 3", "~3 for a metric tile", "Metric tiles are 1 row tall", "metric tiles stay short"],
            because: "a single metric rowSpan for both surfaces is the defect this guide replaced");
    }

    [Test]
    [Description("The layout guide never stretches a metric tile to fill a row and keeps a widget beside a stack bottom-aligned.")]
    public void LayoutGuide_ShouldNotStretchMetricTiles()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "NEVER derive a tile width from 12 divided by the tile count",
            "a tile is at most `colSpan` 4",
            "The 12-column rule applies to the FILLED AREA, not to each row",
            "BOTTOM ALIGNMENT: a widget beside a stack has a `rowSpan` equal to the stack's total"
        ], because: "no stock metric is wider than 4 columns; two tiles stretched to 6 + 6 look nothing like the product");
        flattened.Should().ContainAll(
        [
            "| Number of accounts | metric, `steel-blue`, `work-icon` | `{ \"column\": 1, \"row\": 1, \"colSpan\": 3, \"rowSpan\": 3 }` |",
            "| Accounts by industry | doughnut | `{ \"column\": 7, \"row\": 1, \"colSpan\": 6, \"rowSpan\": 12 }` |"
        ], because: "the Accounts analytics reference shows the real layoutConfig of a metric block with a widget beside it");
        guide.Should().NotContainAny(
            ["metric tile width = 12 /", "12 / (tiles in the band)", "Every row's widget widths must sum to a full 12"],
            because: "deriving a tile width from the tile count is what stretched the tiles");
    }

    [Test]
    [Description("The layout guide owns the widget color rule: four presets banned, the 4.5:1 group the default, color by meaning.")]
    public void LayoutGuide_ShouldOwnTheWidgetColorRule()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "MUST NOT use in any widget (below 3:1, unreadable on white): `orange-red` 2.39, `turquoise` 2.33, `orange` 1.88, `light-green` 1.61.",
            "DEFAULT choice (4.5:1 or more): `dark-blue` 13.83, `navy-blue` 7.21, `forest-green` 6.73, `burnt-coral` 6.01, "
                + "`vivid-purple` 5.79, `steel-blue` 5.78, `blue` 5.77, `violet` 5.30, `cadmium-red` 5.01, `dark-turquoise` 4.50."
        ], because: "the banned and default presets are decided by their WCAG contrast on the white card");
        flattened.Should().ContainAll(
        [
            "| Neutral count, amount or time | `steel-blue` |",
            "| Negative: overdue, missing data, expiring, inactive | `cadmium-red` |",
            "| New, this month, inflow | `dark-turquoise` |",
            "| Attention: waiting, reopened | `burnt-coral` |",
            "| Good result | `forest-green` |"
        ], because: "color follows meaning, as on the stock dashboards, never variety");
    }

    [Test]
    [Description("The layout guide makes an icon mandatory on every metric tile and gives meaning-to-icon examples.")]
    public void LayoutGuide_ShouldRequireAnIconOnEveryMetricTile()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().Contain("Every metric tile MUST carry an icon (`config.layout.icon.iconName`) chosen by meaning",
            because: "every metric on the stock dashboards has an icon");
        guide.Should().ContainAll(
            ["`hourglass-icon`", "`clock-icon`", "`coins-icon`", "`bell-icon`", "`checkmark-filled-icon`"],
            because: "the agent needs concrete icon names to choose from");
    }

    [Test]
    [Description("The layout guide makes every home-page widget without-fill and names the component-doc default it overrides.")]
    public void LayoutGuide_ShouldOverrideTheComponentDocThemeOnHomePages()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "Every widget on a dashboard AND on a home page uses the plain-white card, `theme` \"without-fill\"",
            "On a home page this OVERRIDES the `crt.IndicatorWidget` component documentation, which says `theme` \"full-fill\" for home pages"
        ], because: "the component documentation still says full-fill for home pages, and an agent reads both");
    }

    [Test]
    [Description("The layout guide gives the steps for adding a metric to an existing page, never appending it, with the Case Intelligence worked example.")]
    public void LayoutGuide_ShouldPlaceANewMetricInsideTheExistingBlock()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "## Adding to an existing page",
            "Copy the SIZE and TEXT settings of the sibling metric tiles",
            "but never their `theme` or a banned color",
            "Put the tile in a free slot of the metric block",
            "add a new tile row directly UNDER the block",
            "a side widget whose rows exactly match the block",
            "NEVER append the tile at the bottom of the page",
            "never restyle, resize, or move an existing widget beyond step 4"
        ], because: "a tile appended at the bottom or restyled like an off-palette sibling is what the stock pages never show");
        flattened.Should().ContainAll(
        [
            "Case Intelligence dashboard",
            "the new tile is `{ \"column\": 1, \"row\": 13, \"colSpan\": 2, \"rowSpan\": 3 }`",
            "it becomes `{ \"column\": 5, \"row\": 1, \"colSpan\": 8, \"rowSpan\": 15 }`",
            "the widgets on row 13 move to row 16 and those on row 23 to row 26"
        ], because: "the worked example shows the insertion rule on a full stock metric block with concrete layoutConfig values");
    }

    [Test]
    [Description("The layout guide keeps routing list authors to list-widget and fits a get-guidance reply inline.")]
    public void LayoutGuide_ShouldKeepItsRoutesAndFitTheInlineReplyBudget()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        int replyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(guide);

        // Assert
        guide.Should().Contain("MUST read `list-widget`",
            because: "list authors reach the binding contract from the layout guide");
        guide.Should().Contain("`desktop-page`",
            because: "desktops keep their own guide");
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline budget is spilled to a file the agent reads back in pieces; "
                + "rewrite a section rather than append one");
    }

    [Test]
    [Description("The chart guide bans the four low-contrast presets for its colors, cites the layout guide, and keeps its size floor.")]
    public void ChartWidgetGuide_ShouldBanTheLowContrastColorsAndKeepItsSizeFloor()
    {
        // Arrange
        string guide = Read(ChartWidgetPath);

        // Act
        string[] colorLines = Lines(guide).Where(line => line.Contains("`orange-red`", StringComparison.Ordinal)).ToArray();

        // Assert
        colorLines.Should().ContainSingle(because: "the chart guide states the color ban in one line and cites its owner");
        colorLines[0].Should().ContainAll(
            [.. BannedColors.Select(color => $"`{color}`"), "`config.color`", "`series[*].color`", LayoutOwner],
            because: "the ban covers the title accent and every series color, and the layout guide owns the palette");
        guide.Should().Contain(ChartSizeFloor,
            because: "the chart size floor is out of scope of the metric rules and must not move");
    }

    [Test]
    [Description("The indicator guide routes tile size, placement, color and icon on dashboards and home pages to the layout guide.")]
    public void IndicatorWidgetGuide_ShouldRouteTileLayoutToTheLayoutGuide()
    {
        // Arrange
        string guide = Read(IndicatorWidgetPath);

        // Act
        string[] routeLines = Lines(guide)
            .Where(line => line.Contains("size, placement, color and icon", StringComparison.Ordinal))
            .ToArray();

        // Assert
        routeLines.Should().ContainSingle(because: "an agent that reads only the widget guide must still reach the layout rules");
        routeLines[0].Should().ContainAll(["dashboard", "home page", LayoutOwner],
            because: "the layout guide owns metric size, placement, color and icon on both surfaces");
    }

    private static string Read(string relativePath) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath).Replace("\r\n", "\n");

    private static string[] Lines(string text) => text.Split('\n');

    /// <summary>Joins wrapped Markdown lines so a pinned sentence does not depend on where the text wraps.</summary>
    private static string Flatten(string text) =>
        string.Join(' ', Lines(text).Select(line => line.Trim()).Where(line => line.Length > 0));
}

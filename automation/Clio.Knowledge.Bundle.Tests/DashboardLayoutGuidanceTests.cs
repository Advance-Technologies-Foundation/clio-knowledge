using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the metric-widget rules of <c>dashboard-and-home-page-layout</c> and <c>widget-color-and-icon</c>, and the
/// lines that route the sibling dashboard guides to them.
/// </summary>
/// <remarks>
/// The rules come from the stock product pages, read with <c>get-page</c> on a Sales/Service Enterprise
/// environment on 2026-10-15: the <c>Main</c> grid has 28px rows on a dashboard and 64px rows on a home page,
/// so one metric <c>rowSpan</c> for both surfaces renders a 100px tile on one and a 208px tile on the other;
/// no stock dashboard metric is wider than 4 columns, while on a home page the engineer chose metric and chart
/// rows that fill all 12 columns (two tiles 6 + 6, not 3 + 3 with an empty right half, from runtime screenshots
/// on 2026-10-22) for new rows only, never resizing the tiles already on a page; every stock dashboard metric
/// has an icon; four widget color presets fall below a 3:1 WCAG contrast ratio on the white card; and all 76
/// stock metric tiles show the value alone (<c>text.template</c> <c>{0}</c>, the name only in
/// <c>config.title</c>), with <c>text.fontSizeMode</c> "medium" on every dashboard (the engineer chose "large"
/// for home pages); their titles name the measure in 2-4 words, about 22 characters at the median, and none
/// appends a filter after a comma (the engineer set the 25- and 30-character bounds). The layout guide owns
/// size, placement, card theme and the metric text rules; the color palette and the icon rule were split out
/// into <c>widget-color-and-icon</c> to keep the layout guide inside the inline reply budget. Every other
/// dashboard guide only cites them, so each guide is pinned for its own part and the family is scanned for
/// phrases that would contradict the owners.
/// </remarks>
[TestFixture]
public sealed class DashboardLayoutGuidanceTests
{
    private const string DashboardsFolder = "guidance/mcp/guides/dashboards";
    private const string LayoutPath = DashboardsFolder + "/layout.md";
    private const string ColorAndIconPath = DashboardsFolder + "/widget-color-and-icon.md";
    private const string ChartWidgetPath = DashboardsFolder + "/chart-widget.md";
    private const string IndicatorWidgetPath = DashboardsFolder + "/indicator-widget.md";
    private const string LayoutOwner = "`dashboard-and-home-page-layout`";
    private const string ColorAndIconItemId = "widget-color-and-icon";
    private const string ColorAndIconOwner = "`widget-color-and-icon`";
    private const string ColorAndIconCitation = "get-guidance name=widget-color-and-icon";

    /// <summary>
    /// The chart size floor; only its metric-tile parenthetical changed, from a dashboard row count to a
    /// citation of the layout guide that owns the metric size per surface.
    /// </summary>
    private const string ChartSizeFloor =
        "SIZE FLOOR — never ship a tiny chart. In a `crt.GridContainer` set `layoutConfig.rowSpan` >= 6 "
        + "(platform default 9; funnels 15). In a `crt.FlexContainer` the parent uses `FlexLayoutConfig` — "
        + "set `layoutConfig.height` >= 350 (px) so the flex child doesn't collapse. The same floor applies "
        + "to list/pivot widgets. Metric TILES are exempt (their size is owned by `dashboard-and-home-page-layout`).";

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
        lines.Should().Contain(
            "| Home page (`BaseHomePage`) | `64px` | a row fills 12 per \"Home page widths\"; a single metric 3-4; beside a chart 3, or 2 dense | 2 |",
            because: "a home-page metric is 2 rows of 64px, and the table must show the width exceptions next to the fill rule");
        guide.Should().NotContainAny(
            ["metric tile 3, gauge 3", "~3 for a metric tile", "Metric tiles are 1 row tall", "metric tiles stay short"],
            because: "a single metric rowSpan for both surfaces is the defect this guide replaced");
    }

    [Test]
    [Description("The layout guide sets the metric text size per surface and keeps the siblings' text settings on an existing page.")]
    public void LayoutGuide_ShouldSetTheMetricTextSizePerSurface()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().Contain(
            "TEXT SIZE: `text.fontSizeMode` \"medium\" (the designer's size M) on a dashboard, \"large\" (size L) on a home page.",
            because: "every stock dashboard metric uses medium, and a home-page metric is large");
        flattened.Should().ContainAll(
        [
            "On an existing page copy the sibling tiles' text settings instead",
            "Copy the SIZE and TEXT settings of the sibling metric tiles (`colSpan`, `rowSpan`, `text.fontSizeMode`, `text.labelPosition`)",
            "`text.fontSizeMode` \"medium\" like its siblings"
        ], because: "a new tile on an existing page matches the tiles already there rather than the surface default");
    }

    [Test]
    [Description("The layout guide keeps a metric's text template to the value alone, with the name only in the title, on new and existing pages.")]
    public void LayoutGuide_ShouldShowTheMetricValueAloneInTheTile()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "VALUE ONLY: `text.template` is exactly `{0}`, the value alone",
            "The metric's name is `config.title`: never repeat it as a caption inside the tile",
            "Text beside `{0}` belongs only to a comparison metric"
        ], because: "all 76 stock metric tiles show the value alone; a caption repeating the title is what the designer flagged");
        flattened.Should().ContainAll(
        [
            "Its `text.template` is `{0}` even when a sibling repeats its name as a caption",
            "`text.template` `{0}`, its name only in `config.title`"
        ], because: "the existing-page steps and the finish checklist must not let a copied or invented caption through");
    }

    [Test]
    [Description("The layout guide keeps a metric title a short one-line name of the measure, with a filter folded into the words, on new and existing pages.")]
    public void LayoutGuide_ShouldKeepTheMetricTitleShortOnOneLine()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "TITLE: `config.title` is a short noun phrase naming the measure, 2-4 words on ONE line: aim at 25 characters or fewer, never more than 30.",
            "After the name only a unit after a comma (\"Average resolution time, hours\") or a qualifier in parentheses (\"Overdue cases (response)\").",
            "Never append a filter as a comma list, as in \"Closed requests, Satisfaction 1\": fold it into the words (\"Requests rated 1\", \"Low-rated requests\")."
        ], because: "the stock titles name the measure in 2-4 words and none appends a filter; a filter list is what made the agent's title unclear");
        flattened.Should().ContainAll(
        [
            "Its title follows TITLE (\"Metric tile size\") even when a sibling's title is longer",
            "Every metric title is a 2-4 word noun phrase on one line (25 characters or fewer, never more than 30) with no filter appended after a comma"
        ], because: "the existing-page steps and the finish checklist must not let a long or filter-listing title through");
    }

    [Test]
    [Description("The layout guide never stretches a metric tile to fill a row on a dashboard, keeps a widget beside a stack bottom-aligned, and states every dashboard width limit only for dashboards.")]
    public void LayoutGuide_ShouldNotStretchMetricTilesOnADashboard()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "DASHBOARD WIDTH: NEVER derive a tile width from 12 divided by the tile count",
            "a dashboard tile is at most `colSpan` 4. On a dashboard two tiles are 3 + 3, not 6 + 6.",
            "Dense band, `colSpan` 2: on a dashboard 5 or 6 tiles across (more go on a second tile row) or a block "
                + "two tiles wide in 4 columns; on a home page only that 2-wide block beside a chart.",
            "On a dashboard the 12-column rule applies to the FILLED AREA, not to each row",
            "BOTTOM ALIGNMENT: a widget beside a stack has a `rowSpan` equal to the stack's total"
        ], because: "no stock dashboard metric is wider than 4 columns; two tiles stretched to 6 + 6 look nothing like the product");
        UnscopedOccurrences(flattened, "tile is at most `colSpan` 4", "a dashboard ").Should().Be(0,
            because: "the colSpan 4 limit is a dashboard rule: a home page fills its rows");
        UnscopedOccurrences(flattened, "two tiles are 3 + 3, not 6 + 6", "on a dashboard ").Should().Be(0,
            because: "3 + 3 is a dashboard rule: a home page row with two tiles is 6 + 6");
        UnscopedOccurrences(flattened, "the 12-column rule applies to the filled area", "on a dashboard ").Should().Be(0,
            because: "the filled-area rule is a dashboard rule: a home page fills each row");
        UnscopedOccurrences(flattened, "5 or 6 tiles across", "on a dashboard ").Should().Be(0,
            because: "a home-page metric block beside a chart is only ever the 2-wide block in 4 columns");
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
    [Description("The layout guide fills new metric and chart rows of a home page across 12 columns with the exceptions visible, keeps heights, and never resizes what is already on a page.")]
    public void LayoutGuide_ShouldFillTheRowsOfAHomePage()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "## Home page widths",
            "METRIC ROW: with nothing beside it, a home-page metric row fills all 12 columns with equal tiles: "
                + "2 -> 6 + 6, 3 -> 4 + 4 + 4, 4 -> 3 + 3 + 3 + 3, 5 -> 4 + 4 + 4 then 6 + 6, 6 -> 2 each.",
            "7 or more: rows of at most 6 tiles, as even as possible, each row 2, 3, 4 or 6 tiles so it fills 12 (7 -> 4 + 3, 8 -> 4 + 4).",
            "A single metric stays `colSpan` 3-4, never stretched to 12, or sits beside a chart.",
            "The metric `rowSpan` stays 2."
        ], because: "the engineer chose 6 + 6 over 3 + 3 with an empty right half for a home page's metric row");
        flattened.Should().ContainAll(
        [
            "CHARTS: one chart takes 12 columns.",
            "Ordinary charts (bar, line/spline, area) go at most 2 per row (6 + 6); an odd last one stretches to 12, "
                + "preferably the one with the most data points (a trend or timeline).",
            "Compact charts (doughnut/pie, a bar with few short bars) may go 3 per row (4 + 4 + 4), the remainder split evenly "
                + "(6 + 6 or 12); 4 compact charts are 6 + 6, 6 + 6.",
            "Never 4 or more charts in a row.",
            "Charts in one row share the same `rowSpan`.",
            "A mixed row such as doughnut 4 + wide chart 8 is fine.",
            "Chart heights and floors do not change"
        ], because: "a home page's chart rows fill the width the same way, without changing chart heights");
        flattened.Should().ContainAll(
        [
            "Dashboards keep the widths above.",
            "These widths lay out new rows; on an existing page never resize the widgets already there",
            "On a home page a metric row with nothing beside it and every chart row fill 12 columns per \"Home page widths\", "
                + "except a single metric (`colSpan` 3-4) and a metric block beside a chart (`colSpan` 3, or 2 dense).",
            "On a home page a new metric row with nothing beside it and every new chart row fill 12 columns per \"Home page widths\", "
                + "except a single metric (3-4 columns) and a block beside a chart (3, or 2 dense); existing tiles keep their size."
        ], because: "the core rule and the finish checklist must show where the fill rule stops, and never let it resize an existing tile");
    }

    [Test]
    [Description("The color guide owns the widget palette: four presets banned, the 4.5:1 group the default, the 3:1 to 4.5:1 group allowed, color by meaning.")]
    public void ColorAndIconGuide_ShouldOwnTheWidgetColorRule()
    {
        // Arrange
        string guide = Read(ColorAndIconPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().ContainAll(
        [
            "MUST NOT use in any widget (below 3:1, unreadable on white): `orange-red` 2.39, `turquoise` 2.33, `orange` 1.88, `light-green` 1.61.",
            "DEFAULT choice (4.5:1 or more): `dark-blue` 13.83, `navy-blue` 7.21, `forest-green` 6.73, `burnt-coral` 6.01, "
                + "`vivid-purple` 5.79, `steel-blue` 5.78, `blue` 5.77, `violet` 5.30, `cadmium-red` 5.01, `dark-turquoise` 4.50.",
            "Allowed (3:1 to 4.5:1): `celestial-blue` 4.05, `coral` 3.65, `red` 3.50, `purple` 3.08, `green` 3.05, "
                + "`rusty-orange` 3.03, `light-blue` 3.03, `bright-red` 3.02, `dark-green` 3.01."
        ], because: "the banned, default and allowed presets are decided by their WCAG contrast on the white card");
        flattened.Should().ContainAll(
        [
            "| Neutral count, amount or time | `steel-blue` |",
            "| Negative: overdue, missing data, expiring, inactive | `cadmium-red` |",
            "| New, this month, inflow | `dark-turquoise` |",
            "| Attention: waiting, reopened | `burnt-coral` |",
            "| Good result | `forest-green` |"
        ], because: "color follows meaning, as on the stock dashboards, never variety");
        flattened.Should().Contain("This guide is the single owner of the widget color palette and the metric icon rule",
            because: "the split article states which rules it owns");
    }

    [Test]
    [Description("The color guide makes an icon mandatory on every metric tile and gives meaning-to-icon examples.")]
    public void ColorAndIconGuide_ShouldRequireAnIconOnEveryMetricTile()
    {
        // Arrange
        string guide = Read(ColorAndIconPath);

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
    [Description("The layout guide cites the color guide by get-guidance name instead of restating the palette or the icon table.")]
    public void LayoutGuide_ShouldCiteTheColorAndIconGuideWithoutRestatingIt()
    {
        // Arrange
        string guide = Read(LayoutPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().Contain(
            $"MUST read {ColorAndIconCitation} before you choose a widget color or a metric icon",
            because: "an agent routed to the layout guide must reach the palette and the icon rule");
        guide.Should().NotContainAny(
            ["## Color", "## Icon", "2.39", "13.83", "4.05", "| Neutral count", "| Amounts |"],
            because: "the palette and the icon table have one owner; a copy in the layout guide would drift");
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
    [Description("The layout guide gives the same steps for adding a metric to an existing dashboard or home page, never resizing a sibling and never appending, with the Case Intelligence worked example.")]
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
            "The same steps hold on a dashboard and on a home page.",
            "Never resize an existing sibling tile to make room, even on a home page whose metric row fills 12 columns.",
            "Copy the SIZE and TEXT settings of the sibling metric tiles",
            "but never their `theme` or a banned color",
            $"color by meaning per {ColorAndIconOwner}",
            "Put the tile in a free slot of the metric block",
            "add a new tile row directly UNDER the block",
            "move every widget that starts on or below that row down by the tile's `rowSpan`, in every column",
            "a side widget whose rows exactly match the block",
            "NEVER append the tile at the bottom of the page",
            "never restyle, resize, or move an existing widget beyond step 4."
        ], because: "a tile appended at the bottom, or siblings resized or restyled to make room, is what the stock pages never show");
        flattened.Should().NotContainEquivalentOf("re-split",
            because: "re-splitting an existing home-page row resized stock tiles; the engineer dropped it");
        flattened.Should().ContainAll(
        [
            "Case Intelligence dashboard",
            "the new tile is `{ \"column\": 1, \"row\": 13, \"colSpan\": 2, \"rowSpan\": 3 }`",
            "it becomes `{ \"column\": 5, \"row\": 1, \"colSpan\": 8, \"rowSpan\": 15 }`",
            "the widgets on row 13 move to row 16 and those on row 23 to row 26"
        ], because: "the worked example shows the insertion rule on a full stock metric block with concrete layoutConfig values");
    }

    [Test]
    [Description("The layout guide keeps routing list authors to list-widget, and it and the color guide each fit a get-guidance reply inline.")]
    public void LayoutGuides_ShouldKeepTheirRoutesAndFitTheInlineReplyBudget()
    {
        // Arrange
        string layout = Read(LayoutPath);
        string colorAndIcon = Read(ColorAndIconPath);

        // Act
        int layoutReplyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(layout);
        int colorAndIconReplyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(colorAndIcon);
        foreach ((string itemId, int size) in new[]
        {
            ("dashboard-and-home-page-layout", layoutReplyBytes),
            (ColorAndIconItemId, colorAndIconReplyBytes)
        })
        {
            TestContext.WriteLine(
                $"{itemId,-36} {size,7:N0}  {(double)size / GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,6:P1} of budget");
        }

        // Assert
        layout.Should().Contain("MUST read `list-widget`",
            because: "list authors reach the binding contract from the layout guide");
        layout.Should().Contain("`desktop-page`",
            because: "desktops keep their own guide");
        layoutReplyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline budget is spilled to a file the agent reads back in pieces; "
                + "rewrite a section or split the article rather than append one");
        colorAndIconReplyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "the split-out article must fit the same inline budget");
    }

    [Test]
    [Description("The manifest declares the color guide as its own guidance item with a stable identity.")]
    public void Manifest_ShouldDeclareTheColorAndIconGuide()
    {
        // Arrange
        string root = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "bundle-source.json")));

        // Act
        JsonElement resource = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ColorAndIconItemId);
        JsonElement requirements = manifest.RootElement.GetProperty("requirements");

        // Assert
        resource.GetProperty("topicId").GetString().Should().Be("creatio.widget-color-and-icon");
        resource.GetProperty("role").GetString().Should().Be("guidance",
            because: "get-guidance resolves a bare name only for the guidance role");
        resource.GetProperty("uri").GetString().Should().Be("docs://knowledge/com.creatio.clio/widget-color-and-icon");
        resource.GetProperty("sourcePath").GetString().Should().Be(ColorAndIconPath);
        requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString())
            .Should().Contain(ColorAndIconItemId);
        requirements.GetProperty("resourceUris").EnumerateArray().Select(item => item.GetString())
            .Should().Contain("docs://knowledge/com.creatio.clio/widget-color-and-icon");
    }

    [Test]
    [Description("The chart guide bans the four low-contrast presets for its colors, cites the color guide, and keeps its size floor.")]
    public void ChartWidgetGuide_ShouldBanTheLowContrastColorsAndKeepItsSizeFloor()
    {
        // Arrange
        string guide = Read(ChartWidgetPath);

        // Act
        string[] colorLines = Lines(guide).Where(line => line.Contains("`orange-red`", StringComparison.Ordinal)).ToArray();

        // Assert
        colorLines.Should().ContainSingle(because: "the chart guide states the color ban in one line and cites its owner");
        colorLines[0].Should().ContainAll(
            [.. BannedColors.Select(color => $"`{color}`"), "`config.color`", "`series[*].color`", ColorAndIconOwner, ColorAndIconCitation],
            because: "the ban covers the title accent and every series color, and the color guide owns the palette");
        Flatten(guide).Should().Contain(ChartSizeFloor,
            because: "the chart size floor must not move, and the metric size it exempts is owned by the layout guide");
        guide.Should().NotContain("they stay ~3 rows",
            because: "3 rows is a dashboard metric height; a home-page metric is 2 rows");
    }

    [Test]
    [Description("The indicator guide routes tile size and placement to the layout guide and color and icon to the color guide, on dashboards and home pages.")]
    public void IndicatorWidgetGuide_ShouldRouteTileLayoutToItsOwners()
    {
        // Arrange
        string guide = Read(IndicatorWidgetPath);

        // Act
        string flattened = Flatten(guide);

        // Assert
        flattened.Should().Contain(
            $"On a dashboard or home page, tile size and placement are owned by {LayoutOwner}, and the tile's color and icon by "
                + $"{ColorAndIconOwner} ({ColorAndIconCitation}): read both before you add or move a tile.",
            because: "an agent that reads only the widget guide must still reach both owners, scoped to the surfaces they cover");
    }

    [Test]
    [Description("No dashboard guide but the layout guide's owner text carries a phrase that contradicts the per-surface sizes or names a removed section.")]
    public void DashboardGuides_ShouldNotCarryStaleLayoutPhrases()
    {
        // Arrange
        string[] stalePhrases =
        [
            "metric-band-then-chart-grid", "worked patterns", "per-type sizes as a dashboard", "same layout and the same",
            "they stay ~3 rows", "re-split"
        ];

        // Act
        string[] offenders = [.. DashboardGuides()
            .Where(path => !path.EndsWith("/layout.md", StringComparison.Ordinal))
            .SelectMany(path => stalePhrases
                .Where(phrase => Flatten(Read(path)).Contains(phrase, StringComparison.OrdinalIgnoreCase))
                .Select(phrase => $"{path}: {phrase}"))];

        // Assert
        offenders.Should().BeEmpty(
            because: "a home page has its own metric sizes and row widths, and the layout guide no longer has those sections");
    }

    [Test]
    [Description("Every dashboard guide that names a low-contrast preset names it only to ban it.")]
    public void DashboardGuides_ShouldNameTheBannedColorsOnlyToBanThem()
    {
        // Arrange
        string[] tokens = [.. BannedColors.SelectMany(color => new[] { $"`{color}`", $"\"{color}\"" })];
        string[] banMarkers = ["must not", "no `", "never", "banned"];

        // Act
        string[] offenders = [.. DashboardGuides()
            .SelectMany(path => Blocks(Read(path))
                .Where(block => tokens.Any(token => block.Contains(token, StringComparison.Ordinal)))
                .Where(block => !banMarkers.Any(marker => block.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                .Select(block => $"{path}: {block}"))];

        // Assert
        offenders.Should().BeEmpty(
            because: "orange-red, orange, light-green and turquoise fall below 3:1 on the white card in every widget");
    }

    private static IEnumerable<string> DashboardGuides()
    {
        string root = ProcessGuideSet.FindRepositoryRoot();
        return Directory.GetFiles(Path.Combine(root, DashboardsFolder.Replace('/', Path.DirectorySeparatorChar)), "*.md")
            .Select(path => $"{DashboardsFolder}/{Path.GetFileName(path)}")
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static string Read(string relativePath) =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath).Replace("\r\n", "\n");

    private static string[] Lines(string text) => text.Split('\n');

    /// <summary>Joins wrapped Markdown lines so a pinned sentence does not depend on where the text wraps.</summary>
    private static string Flatten(string text) =>
        string.Join(' ', Lines(text).Select(line => line.Trim()).Where(line => line.Length > 0));

    /// <summary>
    /// Splits Markdown into its paragraphs, list items, table rows and headings, each with its wrapped lines
    /// joined, so a rule is judged with the words around it.
    /// </summary>
    private static IEnumerable<string> Blocks(string text)
    {
        List<string> current = [];
        foreach (string line in Lines(text).Select(line => line.Trim()))
        {
            bool startsBlock = line.Length == 0 || line.StartsWith("- ", StringComparison.Ordinal)
                || line.StartsWith('|') || line.StartsWith('#') || line.StartsWith("```", StringComparison.Ordinal)
                || (line.Length > 1 && char.IsDigit(line[0]) && line.Contains(". ", StringComparison.Ordinal));
            if (startsBlock && current.Count > 0)
            {
                yield return string.Join(' ', current);
                current.Clear();
            }
            if (line.Length > 0)
            {
                current.Add(line);
            }
        }
        if (current.Count > 0)
        {
            yield return string.Join(' ', current);
        }
    }

    /// <summary>
    /// Counts the places a phrase occurs, ignoring case, without the scope words directly before it, so a
    /// dashboard-only limit cannot come back as a universal one.
    /// </summary>
    private static int UnscopedOccurrences(string text, string phrase, string scope)
    {
        int count = 0;
        for (int index = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            index >= 0;
            index = text.IndexOf(phrase, index + phrase.Length, StringComparison.OrdinalIgnoreCase))
        {
            bool scoped = index >= scope.Length
                && string.Compare(text, index - scope.Length, scope, 0, scope.Length, StringComparison.OrdinalIgnoreCase) == 0;
            if (!scoped)
            {
                count++;
            }
        }
        return count;
    }
}

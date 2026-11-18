using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the metric-widget rules of <c>dashboard-and-home-page-layout</c>, <c>widget-color-and-icon</c> and the
/// "Metric title and text" section of <c>indicator-widget</c>, and the lines that route the sibling dashboard
/// guides to them.
/// </summary>
/// <remarks>
/// The rules come from the stock product pages, read with <c>get-page</c> from 9 stock analytics dashboards and
/// 4 stock home pages (13 pages) of a Sales/Service Enterprise environment on 2026-10-15 and re-read unchanged
/// on Creatio 10.3.1163.0 on 2026-11-18: the <c>Main</c> grid has 28px rows on a dashboard and 64px rows on a
/// home page, so one metric <c>rowSpan</c> for both surfaces renders a 100px tile on one and a 208px tile on the
/// other; no stock dashboard metric is wider than 4 columns, while on a home page the engineer chose new metric
/// and chart rows that fill all 12 columns (from runtime screenshots on 2026-10-22), never resizing the tiles
/// already on a page; every stock dashboard metric has an icon; four widget color presets fall below a 3:1 WCAG
/// contrast ratio on the white card; and all 76 stock metric tiles show the value alone (<c>text.template</c>
/// <c>{0}</c>) under a 2-4 word title. The tests assert key tokens and the section a rule sits in, not whole
/// sentences, so a guide can be reworded without breaking the build while a rule cannot move, vanish or be
/// stated twice.
/// </remarks>
[TestFixture]
public sealed class DashboardLayoutGuidanceTests
{
    private const string GuidesFolder = "guidance/mcp/guides";
    private const string DashboardsFolder = GuidesFolder + "/dashboards";
    private const string LayoutPath = DashboardsFolder + "/layout.md";
    private const string ColorAndIconPath = DashboardsFolder + "/widget-color-and-icon.md";
    private const string ChartWidgetPath = DashboardsFolder + "/chart-widget.md";
    private const string IndicatorWidgetPath = DashboardsFolder + "/indicator-widget.md";
    private const string LayoutItemId = "dashboard-and-home-page-layout";
    private const string LayoutOwner = "`dashboard-and-home-page-layout`";
    private const string ColorAndIconItemId = "widget-color-and-icon";
    private const string ColorAndIconOwner = "`widget-color-and-icon`";
    private const string ColorAndIconCitation = "get-guidance name=widget-color-and-icon";
    private const string TitleAndTextSection = "## Metric title and text";

    private static readonly string[] BannedColors = ["orange-red", "orange", "light-green", "turquoise"];

    private static readonly string[] DefaultColors =
    [
        "dark-blue", "navy-blue", "forest-green", "burnt-coral", "vivid-purple", "steel-blue", "blue", "violet",
        "cadmium-red", "dark-turquoise"
    ];

    private static readonly string[] AllowedColors =
    [
        "celestial-blue", "coral", "red", "purple", "green", "rusty-orange", "light-blue", "bright-red", "dark-green"
    ];

    [Test]
    [Description("The layout guide runs from sizes to widths, new page, existing page and card theme.")]
    public void LayoutGuide_ShouldOrderItsSectionsFromSizesToCardTheme()
    {
        // Arrange
        string[] expectedOrder =
        [
            "## Core rules", "## Metric tile size", "## Row widths", "### Dashboard", "### Home page",
            "## New page layout", "## Adding to an existing page", "## Card theme", "## Widget catalog",
            "## Finish checklist"
        ];

        // Act
        string[] headings = [.. Headings(Read(LayoutPath))];
        int[] positions = [.. expectedOrder.Select(heading => Array.FindIndex(headings, line => line.StartsWith(heading, StringComparison.Ordinal)))];

        // Assert
        positions.Should().NotContain(-1, because: "every section of the layout guide must exist");
        positions.Should().BeInAscendingOrder(because: "the guide reads sizes, widths, new page, existing page, card theme");
    }

    [Test]
    [Description("The layout guide sizes metric tiles and their text per surface from the Main grid rows read with get-page.")]
    public void LayoutGuide_ShouldSizeMetricTilesPerSurface()
    {
        // Arrange
        string section = Section(Read(LayoutPath), "## Metric tile size");

        // Act
        string dashboardRow = Lines(section).Single(line => line.StartsWith("| Dashboard", StringComparison.Ordinal));
        string homePageRow = Lines(section).Single(line => line.StartsWith("| Home page", StringComparison.Ordinal));

        // Assert
        section.Should().ContainAll(["`rows`", "`Main`", "`get-page`"],
            because: "the row height decides how tall a rowSpan renders, and it differs per surface");
        dashboardRow.Should().ContainAll(["`BaseDashboardTemplate`", "`28px`"]).And.EndWith("| 3 |",
            because: "a dashboard metric is 3 rows of 28px, about 100px tall");
        homePageRow.Should().ContainAll(["`BaseHomePage`", "`64px`", "\"Row widths\""]).And.EndWith("| 2 |",
            because: "a home-page metric is 2 rows of 64px, and its widths are owned by the row widths section");
        Flatten(section).Should().ContainAll(["`text.fontSizeMode` \"medium\"", "\"large\"", "sibling tiles' text settings"],
            because: "dashboard metrics are medium, home-page metrics large, and an existing page keeps its siblings' text");
    }

    [Test]
    [Description("The indicator guide owns the metric title and text template for a metric on any page.")]
    public void IndicatorWidgetGuide_ShouldOwnTheMetricTitleAndText()
    {
        // Arrange
        string section = Flatten(Section(Read(IndicatorWidgetPath), TitleAndTextSection));

        // Act
        string[] tokens =
        [
            "any page", "`config.title`", "2-4 words", "ONE line", "25 characters", "never more than 30",
            "`#ResourceString(...)#`", "unit after a comma", "qualifier in parentheses", "Never append a filter",
            "fold it into the words", "`text.template` is exactly `{0}`", "never repeat it as a caption",
            "comparison metric"
        ];

        // Assert
        section.Should().ContainAll(tokens,
            because: "the stock titles name the measure in 2-4 words with the value alone in the tile, on any page");
        section.Should().Contain(LayoutOwner,
            because: "the text size depends on the surface and stays with the layout guide");
    }

    [Test]
    [Description("The layout guide cites the indicator guide for the metric title and text instead of restating them.")]
    public void LayoutGuide_ShouldLeaveTheMetricTitleAndTextToTheIndicatorGuide()
    {
        // Arrange
        string guide = Flatten(Read(LayoutPath));

        // Act
        int citations = Occurrences(guide, "\"Metric title and text\"");

        // Assert
        guide.Should().Contain("get-guidance name=indicator-widget", because: "the owner is cited by get-guidance name");
        citations.Should().BeGreaterThanOrEqualTo(3,
            because: "the owner paragraph, the existing-page steps and the finish checklist all point to the section");
        guide.Should().NotContainAny(
            ["2-4 word", "25 characters", "more than 30", "Satisfaction 1", "VALUE ONLY", "is exactly `{0}`"],
            because: "the title and template rules have one owner; a copy in the layout guide would drift");
    }

    [Test]
    [Description("The dashboard width limits sit only in the dashboard part of the row widths, each stated once.")]
    public void LayoutGuide_ShouldScopeTheDashboardWidthLimitsToDashboards()
    {
        // Arrange
        string guide = Flatten(Read(LayoutPath));
        string dashboard = Flatten(Section(Read(LayoutPath), "### Dashboard"));
        string[] dashboardOnly =
        [
            "12 divided by the tile count", "at most `colSpan` 4", "3 + 3, not 6 + 6", "5 or 6 tiles across",
            "FILLED AREA"
        ];

        // Act
        string[] misplaced = [.. dashboardOnly.Where(token => Occurrences(guide, token) != 1 || Occurrences(dashboard, token) != 1)];

        // Assert
        misplaced.Should().BeEmpty(
            because: "no stock dashboard metric is wider than 4 columns, but a home page fills its rows");
    }

    [Test]
    [Description("The home-page row widths fill new metric and chart rows across 12 columns, with the exceptions stated once.")]
    public void LayoutGuide_ShouldFillTheRowsOfAHomePage()
    {
        // Arrange
        string guide = Flatten(Read(LayoutPath));
        string homePage = Flatten(Section(Read(LayoutPath), "### Home page"));

        // Act
        string[] tokens =
        [
            "METRIC ROW", "2 -> 6 + 6", "3 -> 4 + 4 + 4", "4 -> 3 + 3 + 3 + 3", "6 -> 2 each",
            "7 -> a row of 4 tiles (3 columns each) then a row of 3 (4 each)", "8 -> two rows of 4",
            "`rowSpan` stays 2", "EXCEPTIONS", "`colSpan` 3-4", "never stretched to 12", "CHARTS",
            "one chart takes 12 columns", "Never 4 or more charts in a row", "same `rowSpan`",
            "Chart heights and floors do not change"
        ];

        // Assert
        homePage.Should().ContainAll(tokens,
            because: "the engineer chose rows that fill 12 columns on a home page, without changing chart heights");
        Occurrences(guide, "`colSpan` 3-4").Should().Be(1,
            because: "the home-page exceptions are stated in the row widths and referred to elsewhere");
        guide.Should().NotContainAny(["7 -> 4 + 3", "8 -> 4 + 4"],
            because: "tile counts written like column sums read as widths");
    }

    [Test]
    [Description("The layout guide states the no-resize rule once, in the existing-page steps, and calls a horizontal group of tiles a metric row.")]
    public void LayoutGuide_ShouldStateTheNoResizeRuleOnceAndUseOneTermForARowOfTiles()
    {
        // Arrange
        string guide = Read(LayoutPath);
        string existingPage = Section(guide, "## Adding to an existing page");

        // Act
        string[] otherTerms = [.. new[] { @"\bband\b", @"\bstrip\b", @"\btile row\b" }
            .Where(pattern => Regex.IsMatch(guide, pattern, RegexOptions.IgnoreCase))];

        // Assert
        Occurrences(guide, "resize").Should().Be(1, because: "the never-resize rule has one owner section");
        Occurrences(existingPage, "resize").Should().Be(1, because: "the existing-page steps own the never-resize rule");
        otherTerms.Should().BeEmpty(because: "a horizontal group of metric tiles is always a metric row");
        guide.Should().ContainAll(["METRIC BLOCK", "METRIC ROWS"], because: "the core rule defines both terms");
    }

    [Test]
    [Description("The layout guide gives the same steps for adding a metric to an existing dashboard or home page, never appending, with the Case Intelligence worked example.")]
    public void LayoutGuide_ShouldPlaceANewMetricInsideTheExistingBlock()
    {
        // Arrange
        string section = Flatten(Section(Read(LayoutPath), "## Adding to an existing page"));

        // Act
        string[] steps =
        [
            "on a dashboard and on a home page", "NEVER resize", "SIZE and TEXT settings", "never their `theme`",
            ColorAndIconOwner, "free slot", "directly UNDER the block", "down by the tile's `rowSpan`",
            "exactly match the block", "NEVER append"
        ];
        string[] workedExample =
        [
            "Case Intelligence", "`{ \"column\": 1, \"row\": 13, \"colSpan\": 2, \"rowSpan\": 3 }`",
            "`{ \"column\": 5, \"row\": 1, \"colSpan\": 8, \"rowSpan\": 15 }`", "row 16", "row 26",
            "`{ \"column\": 3, \"row\": 13, \"colSpan\": 2, \"rowSpan\": 3 }`"
        ];

        // Assert
        section.Should().ContainAll(steps,
            because: "a tile appended at the bottom, or siblings resized or restyled to make room, is what the stock pages never show");
        section.Should().ContainAll(workedExample,
            because: "the worked example shows the insertion rule on a full stock metric block");
        section.Should().NotContainEquivalentOf("re-split",
            because: "re-splitting an existing home-page row resized stock tiles; the engineer dropped it");
    }

    [Test]
    [Description("The card theme is plain white on dashboards and home pages, the home-page difference from stock is deliberate, and it overrides the component doc.")]
    public void LayoutGuide_ShouldKeepEveryCardPlainWhite()
    {
        // Arrange
        string guide = Read(LayoutPath);
        string section = Flatten(Section(guide, "## Card theme"));

        // Act
        string[] tokens = ["\"without-fill\"", "home page", "on purpose", "OVERRIDES", "`crt.IndicatorWidget`", "\"full-fill\""];

        // Assert
        section.Should().ContainAll(tokens,
            because: "stock home pages use filled cards, new widgets deliberately do not, and the component doc says full-fill");
        guide.Should().NotContainAny(["base product", "like stock", "as stock"],
            because: "home pages differ from the stock ones on purpose");
    }

    [Test]
    [Description("The finish checklist refers to the owner sections and keeps the gauge floor.")]
    public void LayoutGuide_ShouldReferToTheOwnersInTheFinishChecklist()
    {
        // Arrange
        string section = Flatten(Section(Read(LayoutPath), "## Finish checklist"));

        // Act
        string[] tokens =
        [
            "\"Row widths\"", "\"Adding to an existing page\"", "\"Metric title and text\"", ColorAndIconOwner,
            "each gauge is 6 (floor 5)", ">= 6"
        ];

        // Assert
        section.Should().ContainAll(tokens, because: "the checklist checks every rule by its owner and keeps the size floors");
    }

    [Test]
    [Description("The layout and color guides state the 13-page evidence and the Creatio version it was re-read on.")]
    public void LayoutGuides_ShouldStateTheirEvidenceAndVersion()
    {
        // Arrange
        string[] guides = [Read(LayoutPath), Read(ColorAndIconPath)];

        // Act
        string[] evidence = [.. guides.Select(guide => Blocks(guide).Single(block => block.StartsWith("Evidence:", StringComparison.Ordinal)))];

        // Assert
        evidence.Should().AllSatisfy(text => text.Should().ContainAll(["9 stock analytics dashboards", "4 stock home pages", "10.3.1163.0"],
            because: "evidence names what was measured and on which version"));
        guides.Should().AllSatisfy(guide => guide.Should().NotContainAny(["8 stock", "platform version not read"],
            because: "the survey covered 9 dashboards and the version is now known"));
    }

    [Test]
    [Description("The color guide owns the widget palette: four presets banned, the 4.5:1 group the default, the 3:1 to 4.5:1 group allowed, color by meaning.")]
    public void ColorAndIconGuide_ShouldOwnTheWidgetColorRule()
    {
        // Arrange
        string guide = Read(ColorAndIconPath);
        string[] lines = Lines(Section(guide, "## Color"));

        // Act
        string banned = Flatten(Block(lines, "- MUST NOT"));
        string defaults = Flatten(Block(lines, "- DEFAULT"));
        string allowed = Flatten(Block(lines, "- Allowed"));

        // Assert
        banned.Should().ContainAll([.. BannedColors.Select(color => $"`{color}`"), "3:1"]);
        defaults.Should().ContainAll([.. DefaultColors.Select(color => $"`{color}`"), "4.5:1"]);
        allowed.Should().ContainAll([.. AllowedColors.Select(color => $"`{color}`"), "3:1 to 4.5:1"]);
        Occurrences(guide, "WCAG 2").Should().Be(1, because: "the contrast method is explained once");
        lines.Should().Contain(line => line.StartsWith("| Neutral", StringComparison.Ordinal) && line.Contains("`steel-blue`"))
            .And.Contain(line => line.StartsWith("| Negative", StringComparison.Ordinal) && line.Contains("`cadmium-red`"),
                because: "color follows meaning, as on the stock dashboards, never variety");
        Flatten(guide).Should().Contain("single owner of the widget color palette and the metric icon rule");
    }

    [Test]
    [Description("The color guide makes an icon mandatory on every metric tile and keys the overdue icons by measure.")]
    public void ColorAndIconGuide_ShouldRequireAnIconOnEveryMetricTile()
    {
        // Arrange
        string section = Section(Read(ColorAndIconPath), "## Icon");

        // Act
        string hourglass = Lines(section).Single(line => line.Contains("`hourglass-icon`", StringComparison.Ordinal));
        string slaBreach = Lines(section).Single(line => line.Contains("`message-warning-icon`", StringComparison.Ordinal));

        // Assert
        Flatten(section).Should().ContainAll(["MUST carry an icon", "`config.layout.icon.iconName`", "by meaning"]);
        section.Should().ContainAll(["`clock-icon`", "`coins-icon`", "`bell-icon`", "`checkmark-filled-icon`"],
            because: "the agent needs concrete icon names to choose from");
        hourglass.Should().Contain("Elapsed or waiting time").And.NotContain("SLA",
            because: "the hourglass marks elapsed or waiting time");
        slaBreach.Should().ContainAll(["SLA breached", "`problem-icon`"],
            because: "message-warning and problem mark a breached SLA, so one measure maps to one icon");
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
        flattened.Should().Contain($"MUST read {ColorAndIconCitation}",
            because: "an agent routed to the layout guide must reach the palette and the icon rule");
        guide.Should().NotContainAny(
            ["## Color", "## Icon", "2.39", "13.83", "4.05", "| Neutral count", "| Amounts |"],
            because: "the palette and the icon table have one owner; a copy in the layout guide would drift");
    }

    [Test]
    [Description("The layout guide keeps routing list authors to list-widget, and the dashboard guides it touches each fit a get-guidance reply inline.")]
    public void LayoutGuides_ShouldKeepTheirRoutesAndFitTheInlineReplyBudget()
    {
        // Arrange
        string layout = Read(LayoutPath);
        (string itemId, int size)[] replies =
        [
            (LayoutItemId, GuidanceInlineReplyBudgetTests.ReplyBytes(layout)),
            (ColorAndIconItemId, GuidanceInlineReplyBudgetTests.ReplyBytes(Read(ColorAndIconPath))),
            ("indicator-widget", GuidanceInlineReplyBudgetTests.ReplyBytes(Read(IndicatorWidgetPath)))
        ];

        // Act
        foreach ((string itemId, int size) in replies)
        {
            TestContext.WriteLine(
                $"{itemId,-36} {size,7:N0}  {(double)size / GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,6:P1} of budget");
        }

        // Assert
        layout.Should().Contain("MUST read `list-widget`", because: "list authors reach the binding contract from the layout guide");
        layout.Should().Contain("`desktop-page`", because: "desktops keep their own guide");
        replies.Should().AllSatisfy(reply => reply.size.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: $"{reply.itemId} must not spill to a file the agent reads back in pieces"));
    }

    [Test]
    [Description("The manifest declares the color guide with a stable identity, sorted after when-to-use-requests, and names only topics in the dashboard descriptions.")]
    public void Manifest_ShouldDeclareTheColorAndIconGuide()
    {
        // Arrange
        string root = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "bundle-source.json")));
        JsonElement[] resources = [.. manifest.RootElement.GetProperty("resources").EnumerateArray()];
        JsonElement requirements = manifest.RootElement.GetProperty("requirements");

        // Act
        string?[] resourceIds = [.. resources.Select(item => item.GetProperty("itemId").GetString())];
        string?[] itemIds = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString())];
        string?[] uris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(item => item.GetString())];
        JsonElement resource = resources.Single(item => item.GetProperty("itemId").GetString() == ColorAndIconItemId);
        string[] descriptions = [.. resources
            .Where(item => item.GetProperty("itemId").GetString() is LayoutItemId or ColorAndIconItemId)
            .Select(item => item.GetProperty("description").GetString() ?? string.Empty)];

        // Assert
        resource.GetProperty("topicId").GetString().Should().Be("creatio.widget-color-and-icon");
        resource.GetProperty("role").GetString().Should().Be("guidance", because: "get-guidance resolves a bare name only for the guidance role");
        resource.GetProperty("uri").GetString().Should().Be("docs://knowledge/com.creatio.clio/widget-color-and-icon");
        resource.GetProperty("sourcePath").GetString().Should().Be(ColorAndIconPath);
        Array.IndexOf(resourceIds, ColorAndIconItemId).Should().Be(Array.IndexOf(resourceIds, "when-to-use-requests") + 1);
        Array.IndexOf(itemIds, ColorAndIconItemId).Should().Be(Array.IndexOf(itemIds, "when-to-use-requests") + 1);
        Array.IndexOf(uris, "docs://knowledge/com.creatio.clio/widget-color-and-icon")
            .Should().Be(Array.IndexOf(uris, "docs://knowledge/com.creatio.clio/when-to-use-requests") + 1,
                because: "the new item takes its alphabetical place in every list");
        descriptions.Should().HaveCount(2).And.AllSatisfy(description =>
        {
            description.Length.Should().BeLessThanOrEqualTo(300, because: "a description names topics, not rules");
            description.Should().NotContainAny(["Split out", "never", "stretched", "filling 12"],
                because: "history and rules belong in the guide, not in the item an agent picks it by");
        });
    }

    [Test]
    [Description("The chart guide bans the four low-contrast presets for its colors, cites the color guide, and keeps its size floor.")]
    public void ChartWidgetGuide_ShouldBanTheLowContrastColorsAndKeepItsSizeFloor()
    {
        // Arrange
        string guide = Read(ChartWidgetPath);

        // Act
        string[] colorLines = [.. Lines(guide).Where(line => line.Contains("`orange-red`", StringComparison.Ordinal))];
        string sizeFloor = Blocks(guide).Single(block => block.Contains("SIZE FLOOR", StringComparison.Ordinal));

        // Assert
        colorLines.Should().ContainSingle(because: "the chart guide states the color ban in one line and cites its owner");
        colorLines[0].Should().ContainAll(
            [.. BannedColors.Select(color => $"`{color}`"), "`config.color`", "`series[*].color`", ColorAndIconOwner, ColorAndIconCitation],
            because: "the ban covers the title accent and every series color, and the color guide owns the palette");
        sizeFloor.Should().ContainAll(["`layoutConfig.rowSpan` >= 6", "funnels 15", ">= 350", "Metric TILES are exempt", LayoutOwner],
            because: "the chart size floor must not move, and the metric size it exempts is owned by the layout guide");
    }

    [Test]
    [Description("The indicator guide routes tile size and placement to the layout guide and color and icon to the color guide, on dashboards and home pages.")]
    public void IndicatorWidgetGuide_ShouldRouteTileLayoutToItsOwners()
    {
        // Arrange
        string rule = Blocks(Read(IndicatorWidgetPath)).Single(block => block.Contains("tile size and placement", StringComparison.Ordinal));

        // Act
        string[] tokens = ["dashboard or home page", LayoutOwner, ColorAndIconOwner, ColorAndIconCitation, "read both"];

        // Assert
        rule.Should().ContainAll(tokens,
            because: "an agent that reads only the widget guide must still reach both owners, scoped to the surfaces they cover");
    }

    [Test]
    [Description("home-page, dashboard-design and the dashboards router name the owner guides without restating what they cover; the routing map names the color guide.")]
    public void SiblingGuides_ShouldOnlyNameTheOwnerGuides()
    {
        // Arrange
        string[] siblings = [DashboardsFolder + "/home-page.md", DashboardsFolder + "/design.md", DashboardsFolder + "/index.md"];
        string[] restated = ["12-column grid", "metric block", "per-surface metric sizes", "plain-white card", "row widths", "by contrast"];

        // Act
        string[] offenders = [.. siblings.SelectMany(path => restated
            .Where(phrase => Read(path).Contains(phrase, StringComparison.OrdinalIgnoreCase))
            .Select(phrase => $"{path}: {phrase}"))];
        string homePage = Read(DashboardsFolder + "/home-page.md");
        string routingLine = Lines(Read(GuidesFolder + "/routing.md")).Single(line => line.Contains("-> name=dashboards ", StringComparison.Ordinal));

        // Assert
        offenders.Should().BeEmpty(because: "the sibling guides name the owner and its topic, they do not re-list its rules");
        siblings.Should().AllSatisfy(path => Read(path).Should().ContainAll([LayoutItemId, ColorAndIconItemId]));
        Occurrences(homePage, LayoutOwner).Should().Be(1, because: "home-page names the layout guide once");
        routingLine.Should().ContainAll([LayoutItemId, ColorAndIconItemId], because: "the router lists every dashboards target it routes onward to");
    }

    [Test]
    [Description("No dashboard guide but the layout guide carries a phrase that contradicts the per-surface sizes or names a removed section.")]
    public void DashboardGuides_ShouldNotCarryStaleLayoutPhrases()
    {
        // Arrange
        string[] stalePhrases =
        [
            "metric-band-then-chart-grid", "worked patterns", "per-type sizes as a dashboard", "same layout and the same",
            "they stay ~3 rows", "re-split", "like the base product"
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

    /// <summary>Joins wrapped Markdown lines so a pinned token does not depend on where the text wraps.</summary>
    private static string Flatten(string text) =>
        string.Join(' ', Lines(text).Select(line => line.Trim()).Where(line => line.Length > 0));

    /// <summary>The Markdown headings of a guide, outside code fences, in document order.</summary>
    private static IEnumerable<string> Headings(string text)
    {
        bool inFence = false;
        foreach (string line in Lines(text))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }
            else if (!inFence && line.StartsWith('#'))
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// The body of the section whose heading starts with <paramref name="heading"/>, up to the next heading of
    /// the same or a higher level, so a test can tell which section a rule sits in.
    /// </summary>
    private static string Section(string text, string heading)
    {
        int level = heading.TakeWhile(character => character == '#').Count();
        string[] lines = Lines(text);
        int start = Array.FindIndex(lines, line => line.StartsWith(heading, StringComparison.Ordinal));
        start.Should().BeGreaterThanOrEqualTo(0, because: $"the guide must have a section \"{heading}\"");
        List<string> body = [];
        bool inFence = false;
        foreach (string line in lines.Skip(start + 1))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }
            else if (!inFence && line.StartsWith('#') && line.TakeWhile(character => character == '#').Count() <= level)
            {
                break;
            }
            body.Add(line);
        }
        return string.Join('\n', body);
    }

    /// <summary>The list item that starts with <paramref name="prefix"/>, with its wrapped lines.</summary>
    private static string Block(string[] lines, string prefix)
    {
        int start = Array.FindIndex(lines, line => line.StartsWith(prefix, StringComparison.Ordinal));
        start.Should().BeGreaterThanOrEqualTo(0, because: $"the section must have an item \"{prefix}\"");
        return string.Join('\n', lines.Skip(start).TakeWhile((line, index) => index == 0 || line.StartsWith("  ", StringComparison.Ordinal)));
    }

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

    /// <summary>Counts the places a token occurs, ignoring case.</summary>
    private static int Occurrences(string text, string token)
    {
        int count = 0;
        for (int index = text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            index >= 0;
            index = text.IndexOf(token, index + token.Length, StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }
        return count;
    }
}

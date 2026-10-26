clio MCP dashboard and home page layout guide

Use this guide whenever you add, arrange, size, group, color, or style Freedom UI ANALYTICAL widgets
(metrics/indicators and charts) on a DASHBOARD or a HOME PAGE, new or existing, so they look like
Creatio's own product dashboards. Desktops are a separate surface: read `desktop-page` for those.

This guide is the single owner of WHERE a widget goes, HOW BIG it is (a metric's text size too), its card
theme, and the metric title and value-only text rules on dashboards and home pages; `indicator-widget`
and `chart-widget` cite it. The widget color palette and the metric icon rule are owned by
`widget-color-and-icon`: MUST read get-guidance name=widget-color-and-icon before you choose a widget
color or a metric icon. This guide does NOT own the rest of a widget's runtime payload: for
`crt.IndicatorWidget` read `indicator-widget`, for `crt.ChartWidget` read `chart-widget`, and call
`get-component-info`. For page structure and containers read `page-modification`; filtering a
dashboard's widgets by its page data (`DashboardDS`) is in `dashboard-design`.

Evidence: the sizes below were read with `get-page` from the 8 stock analytics dashboards and 4 stock
home pages of a Sales/Service Enterprise environment on 2026-10-15, their 76 metric tiles' text settings
and titles on 2026-10-22 (platform version not read). The home-page widths were chosen from runtime
screenshots on 2026-10-22. The reference is the Accounts analytics dashboard
(`AccountsAnalyticsDashboard`).

## Core rules (in priority order)

Hard rules unless the user explicitly overrides one.

1. PLACE: metric tiles form a METRIC BLOCK at the top, either a band across the page or a block of
   columns with a tall widget beside it. Charts go below or beside the block, never inside it; a metric
   never goes below the charts. On an existing page follow "Adding to an existing page". A surface that
   covers several subjects is split into LABELED sections (a header and spacing, never card color), each
   its own metric block and chart row.
2. SIZE: read the page grid first; a metric tile takes the size and text size of its SURFACE ("Metric
   tile size"). On a dashboard a tile is never stretched to fill a row. On a home page a metric row
   with nothing beside it and every chart row fill 12 columns per "Home page widths", except a single
   metric (`colSpan` 3-4) and a metric block beside a chart (`colSpan` 3, or 2 dense). Charts are sized
   by type ("Widget catalog").
3. STYLE: every widget is a plain-white card ("Card theme") and takes a color by MEANING; every metric
   tile carries an icon chosen by meaning (both per `widget-color-and-icon`) and shows its value alone
   under a short one-line title.

## Metric tile size

Before sizing anything, read the `rows` of the page's `Main` `crt.GridContainer` with `get-page` (the
merged `bundle.json`). Both surfaces have a 12-column grid and an 8px gap, but different row heights:

| Surface | `Main` rows | Metric `colSpan` | Metric `rowSpan` |
|---|---|---|---|
| Dashboard (`BaseDashboardTemplate`) | `28px` | 3, or 2 in a dense band | 3 |
| Home page (`BaseHomePage`) | `64px` | a row fills 12 per "Home page widths"; a single metric 3-4; beside a chart 3, or 2 dense | 2 |

Height in px = `rowSpan` x row height + (`rowSpan` - 1) x gap. The same `rowSpan` 3 is 100px on a
dashboard and 208px on a home page, so a `rowSpan` copied from one surface to the other renders a
bloated or squashed tile. For any other `rows` value copy the sibling tiles' size; with none, pick the
`rowSpan` whose height is closest to the stock 100-136px.

- DASHBOARD WIDTH: NEVER derive a tile width from 12 divided by the tile count, and never widen a tile
  to fill a row: a dashboard tile is at most `colSpan` 4. On a dashboard two tiles are 3 + 3, not 6 + 6.
- Dense band, `colSpan` 2: on a dashboard 5 or 6 tiles across (more go on a second tile row) or a block
  two tiles wide in 4 columns; on a home page only that 2-wide block beside a chart.
- On a dashboard the 12-column rule applies to the FILLED AREA, not to each row. When the tiles of a row
  do not reach 12 columns, put a tall widget beside the block, or leave the rest of the row empty.
- BOTTOM ALIGNMENT: a widget beside a stack has a `rowSpan` equal to the stack's total, so both end on
  the same row.

Metric title and text (`config.title` and `config.text` of the tile):

- TITLE: `config.title` is a short noun phrase naming the measure, 2-4 words on ONE line: aim at 25
  characters or fewer, never more than 30. Count the text the title registers, not its
  `#ResourceString(...)#` key. After the name only a unit after a comma ("Average resolution time,
  hours") or a qualifier in parentheses ("Overdue cases (response)"). Never append a filter as a comma
  list, as in "Closed requests, Satisfaction 1": fold it into the words ("Requests rated 1",
  "Low-rated requests"). Stock titles read like "Overdue orders" or "Cases in progress".
- TEXT SIZE: `text.fontSizeMode` "medium" (the designer's size M) on a dashboard, "large" (size L) on a
  home page. On an existing page copy the sibling tiles' text settings instead ("Adding to an existing
  page").
- VALUE ONLY: `text.template` is exactly `{0}`, the value alone; a `#ResourceString(...)#` template
  registers the text `{0}`. The metric's name is `config.title`: never repeat it as a caption inside
  the tile, as in "Open critical requests: {0}". Text beside `{0}` belongs only to a comparison metric
  (e.g. "10 %"). All 76 stock metric tiles use `{0}`.

The reference, a metric block with a doughnut beside it (dashboard grid):

| Widget | Type, color, icon | `layoutConfig` |
|---|---|---|
| Number of accounts | metric, `steel-blue`, `work-icon` | `{ "column": 1, "row": 1, "colSpan": 3, "rowSpan": 3 }` |
| Accounts without primary contact | metric, `cadmium-red`, `document-edit-icon` | `{ "column": 4, "row": 1, "colSpan": 3, "rowSpan": 3 }` |
| Accounts by industry | doughnut | `{ "column": 7, "row": 1, "colSpan": 6, "rowSpan": 12 }` |
| Accounts by category | bar | `{ "column": 1, "row": 4, "colSpan": 6, "rowSpan": 9 }` |
| Accounts by type | horizontal bar | `{ "column": 1, "row": 13, "colSpan": 6, "rowSpan": 8 }` |
| Accounts by employee count | bar | `{ "column": 7, "row": 13, "colSpan": 6, "rowSpan": 8 }` |

The doughnut's `rowSpan` 12 = 3 (tiles) + 9 (bar under them), so it ends on the same row as the bar.

## Home page widths

A home page fills its width. Dashboards keep the widths above. These widths lay out new rows; on an
existing page never resize the widgets already there: follow "Adding to an existing page".

- METRIC ROW: with nothing beside it, a home-page metric row fills all 12 columns with equal tiles:
  2 -> 6 + 6, 3 -> 4 + 4 + 4, 4 -> 3 + 3 + 3 + 3, 5 -> 4 + 4 + 4 then 6 + 6, 6 -> 2 each. 7 or more:
  rows of at most 6 tiles, as even as possible, each row 2, 3, 4 or 6 tiles so it fills 12
  (7 -> 4 + 3, 8 -> 4 + 4). A single metric stays `colSpan` 3-4, never stretched to 12, or sits beside
  a chart. A block with a chart beside it uses `colSpan` 3, or 2 in a dense band. The metric `rowSpan`
  stays 2.
- CHARTS: one chart takes 12 columns. Ordinary charts (bar, line/spline, area) go at most 2 per row
  (6 + 6); an odd last one stretches to 12, preferably the one with the most data points (a trend or
  timeline). Compact charts (doughnut/pie, a bar with few short bars) may go 3 per row (4 + 4 + 4), the
  remainder split evenly (6 + 6 or 12); 4 compact charts are 6 + 6, 6 + 6. Never 4 or more charts in a
  row. Charts in one row share the same `rowSpan`. A mixed row such as doughnut 4 + wide chart 8 is fine.
  Chart heights and floors do not change ("Widget catalog").

## Card theme

Every widget on a dashboard AND on a home page uses the plain-white card, `theme` "without-fill", like
the base product: a customization should not stand out. On a home page this OVERRIDES the
`crt.IndicatorWidget` component documentation, which says `theme` "full-fill" for home pages: follow
this guide. Use a colored card only when the user explicitly asks for one, and never in a banned color.

## Adding to an existing page

The same steps hold on a dashboard and on a home page. Never resize an existing sibling tile to make
room, even on a home page whose metric row fills 12 columns.

1. Read the page with `get-page`: the `Main` rows and every widget's `layoutConfig`, `theme`, and color.
2. Copy the SIZE and TEXT settings of the sibling metric tiles (`colSpan`, `rowSpan`,
   `text.fontSizeMode`, `text.labelPosition`), or take them from "Metric tile size" when the page has
   no metric yet, but never their `theme` or a banned color: the new tile is "without-fill" with a
   color by meaning per `widget-color-and-icon`, even when its siblings are full-fill or use a banned
   color. Its `text.template` is `{0}` even when a sibling repeats its name as a caption. Its title
   follows TITLE ("Metric tile size") even when a sibling's title is longer.
3. Put the tile in a free slot of the metric block: the next free columns of the block's rows.
4. When the block is full, add a new tile row directly UNDER the block, in the block's columns:
   - move every widget that starts on or below that row down by the tile's `rowSpan`, in every column,
     so nothing below overlaps;
   - extend by the same `rowSpan` a side widget whose rows exactly match the block, or the block and
     the widgets under it (it starts on the block's first row and ends on the same row as that stack),
     so the bottoms stay aligned. A side widget that does not match keeps its size.
5. NEVER append the tile at the bottom of the page or among the charts, and never restyle, resize, or
   move an existing widget beyond step 4.

Worked example: "Add a metric with the number of closed cases to the Case Intelligence dashboard".
Before: 8 metric tiles 2x3 fill columns 1-4, rows 1-12 (at rows 1, 4, 7, 10 in columns 1 and 3); a
doughnut `{ "column": 5, "row": 1, "colSpan": 8, "rowSpan": 12 }` sits beside them; three
horizontal bars 4x10 start on row 13 (columns 1, 5, 9); a doughnut 4x10 and a bar 8x10 start on row 23.
The block is full, so:

- the new tile is `{ "column": 1, "row": 13, "colSpan": 2, "rowSpan": 3 }`, titled "Closed cases",
  "without-fill", `checkmark-filled-icon`, a color by meaning (e.g. `forest-green` for a good result),
  `text.fontSizeMode` "medium" like its siblings and `text.template` `{0}`;
- the doughnut matched rows 1-12, so it becomes `{ "column": 5, "row": 1, "colSpan": 8, "rowSpan": 15 }`;
- the widgets on row 13 move to row 16 and those on row 23 to row 26;
- a second new tile takes the free slot `{ "column": 3, "row": 13, "colSpan": 2, "rowSpan": 3 }`.

## New page layout

Every native analytics surface has the same top-down shape: a metric block, then chart rows.

```
METRIC BAND  [tile 3][tile 3][tile 3][tile 3]          (4 tiles; or 6 tiles at 2 columns)
CHART ROW    [ chart 4 ][ chart 4 ][ chart 4 ]
CHART ROW    [   chart 6   ][   chart 6   ]
WIDE TREND   [          line / timeline 12          ]
```

- A chart row's widths sum to 12; mix widths only so they still do. Doughnuts come in threes at 4 columns.
  On a home page the widths follow "Home page widths".
- Wide trends go last, full width when they have many time points.
- Patterns from the product: a 6-tile strip then [chart 4][chart 8] rows (a small breakdown beside a wide
  trend); a metric block in columns 1-4 or 1-6 beside a focal doughnut ("Metric tile size"); a 6-tile
  strip then [bar 6][bar 6] then three doughnuts (by service, by category, by channel).

## Concept -> widget map

| The user wants to show... | Widget type |
|---|---|
| A single number / KPI (count, sum, average, min/max) | Metric (indicator) |
| How a total splits into parts / shares of 100% | Doughnut / Pie |
| A value compared across categories (few categories, or ranking) | Bar / Column chart |
| How a value changes over time (trend, dynamics) | Line / Spline chart |
| A "X vs Y" comparison (e.g. Accepted vs Missed) | Bar/Column (grouped) or Line if over time |
| A short live list of records (top N, my open items) | List widget |
| A multi-dimension table of aggregates | Pivot table |
| External content (report, page, embed) | Web page widget |

## Widget catalog: type, when to use, default size

Height is `layoutConfig.rowSpan`; the widths below are for dashboards. Platform defaults: gauge 3,
chart 9, funnel 15, list/pivot 9. HARD FLOOR: a chart, list, or pivot must have `rowSpan` >= 6, below
that it renders unreadably short. A METRIC TILE is exempt and takes its surface size ("Metric tile
size"). A GAUGE is NOT: it ships the tile's default of 3, but at 3 rows its semicircular arc is too
small to read. Give a gauge `rowSpan` 6, floor 5.

- Metric (indicator): a headline number (count, sum, average, min/max, a "without X" gap count). Size per
  this guide, color and icon per `widget-color-and-icon`, payload per `indicator-widget`.
- Bar / Column: compares a measure across categories, or a grouped "X vs Y". Vertical for few short
  labels, horizontal for many or long labels (people, accounts, services). 4 columns when 3 per row,
  6 when labels are long or it is primary.
- Doughnut / Pie: shares of a whole with <= ~6 slices; more categories -> bar chart. 4 columns, 3 per
  row; a single focal doughnut may take 8 columns beside a metric column (6 beside the reference's
  6-column block). Never for time series.
- Line / Spline: a measure over time (trend, dynamics). 4 columns beside sibling charts; 6-12 for a
  primary timeline with many points.
- List widget: individual records the user acts on (e.g. "My overdue cases"). 6-12 columns, ~5-10
  visible rows, minimal columns.
- Pivot table: a two-dimension aggregate a chart cannot convey. 6-12 columns, height by row count.
- Web page widget: external content; avoid when a native widget shows the same data. 6-12 columns.

## Finish checklist

- `Main` rows read; every metric tile has its surface size. On a dashboard it is at most 4 columns wide
  and not stretched to fill a row. On a home page a new metric row with nothing beside it and every new
  chart row fill 12 columns per "Home page widths", except a single metric (3-4 columns) and a block
  beside a chart (3, or 2 dense); existing tiles keep their size.
- Metric tiles form one block at the top (or under it on an existing page), never among the charts or
  appended at the bottom.
- A widget beside a stack ends on the same row as the stack; every chart row sums to 12 columns.
- Every widget is "without-fill" (home pages too); its color and every metric icon pass the checklist
  of `widget-color-and-icon`.
- Every metric tile has its surface's `text.fontSizeMode` (on an existing page its siblings') and
  `text.template` `{0}`, its name only in `config.title`.
- Every metric title is a 2-4 word noun phrase on one line (25 characters or fewer, never more than 30)
  with no filter appended after a comma.
- Each chart/list/pivot meets the `rowSpan` floor (>= 6; default 9, funnel 15); each gauge is 6.
- Multi-topic surfaces are split into labeled sections.

For a List widget's binding and clone/repair contract, MUST read `list-widget` and fetch
`get-component-info` for its runtime assembly documentation.

clio MCP dashboard and home page layout guide

Use this guide whenever you add, arrange, size, group, color, or style Freedom UI ANALYTICAL widgets
(metrics/indicators and charts) on a DASHBOARD or a HOME PAGE, new or existing, so they look like
Creatio's own product dashboards. Desktops are a separate surface: read `desktop-page` for those.

This guide is the single owner of WHERE a widget goes, HOW BIG it is (a metric's text size too), its card
theme, the widget color palette, and the metric icon and value-only text rules on dashboards and home
pages; `indicator-widget` and `chart-widget` cite it. It does NOT own a widget's runtime payload: for `crt.IndicatorWidget` read `indicator-widget`, for
`crt.ChartWidget` read `chart-widget`, and call `get-component-info`. For page structure and containers
read `page-modification`; filtering a dashboard's widgets by its page data (`DashboardDS`) is in
`dashboard-design`.

Evidence: the sizes, colors, and icons below were read with `get-page` from the 8 stock analytics
dashboards and 4 stock home pages of a Sales/Service Enterprise environment on 2026-10-15, their 76
metric tiles' text settings on 2026-10-22 (platform version not read). The reference is the Accounts analytics dashboard (`AccountsAnalyticsDashboard`).

## Core rules (in priority order)

Hard rules unless the user explicitly overrides one.

1. PLACE: metric tiles form a METRIC BLOCK at the top, either a band across the page or a block of
   columns with a tall widget beside it. Charts go below or beside the block, never inside it; a metric
   never goes below the charts. On an existing page follow "Adding to an existing page". A surface that
   covers several subjects is split into LABELED sections (a header and spacing, never card color), each
   its own metric block and chart row.
2. SIZE: read the page grid first; a metric tile takes the size and text size of its SURFACE ("Metric
   tile size") and is never stretched to fill a row. Charts are sized by type ("Widget catalog").
3. STYLE: every widget is a plain-white card, takes a color by MEANING from the allowed palette ("Color"),
   and every metric tile carries an icon ("Icon") and shows its value alone, its name being the title.

## Metric tile size

Before sizing anything, read the `rows` of the page's `Main` `crt.GridContainer` with `get-page` (the
merged `bundle.json`). Both surfaces have a 12-column grid and an 8px gap, but different row heights:

| Surface | `Main` rows | Metric `colSpan` | Metric `rowSpan` |
|---|---|---|---|
| Dashboard (`BaseDashboardTemplate`) | `28px` | 3, or 2 in a dense band | 3 |
| Home page (`BaseHomePage`) | `64px` | 3, or 2 in a dense band | 2 |

Height in px = `rowSpan` x row height + (`rowSpan` - 1) x gap. The same `rowSpan` 3 is 100px on a
dashboard and 208px on a home page, so a `rowSpan` copied from one surface to the other renders a
bloated or squashed tile. For any other `rows` value copy the sibling tiles' size; with none, pick the
`rowSpan` whose height is closest to the stock 100-136px.

- NEVER derive a tile width from 12 divided by the tile count, and never widen a tile to fill a row: a
  tile is at most `colSpan` 4. Two tiles are 3 + 3, not 6 + 6.
- Dense band: 5 or 6 tiles across (more go on a second tile row), or a block two tiles wide in 4
  columns; use `colSpan` 2 there.
- The 12-column rule applies to the FILLED AREA, not to each row. When the tiles of a row do not reach 12
  columns, put a tall widget beside the block, or leave the rest of the row empty.
- BOTTOM ALIGNMENT: a widget beside a stack has a `rowSpan` equal to the stack's total, so both end on
  the same row.

Metric text (`config.text` of the tile):

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

## Color

Applies to every widget: a metric's `config.layout.color`, a chart's `config.color` and its
`series[*].color`. The number is the WCAG 2 contrast ratio of the preset's swatch color against the
white card.

- MUST NOT use in any widget (below 3:1, unreadable on white): `orange-red` 2.39, `turquoise` 2.33,
  `orange` 1.88, `light-green` 1.61.
- DEFAULT choice (4.5:1 or more): `dark-blue` 13.83, `navy-blue` 7.21, `forest-green` 6.73,
  `burnt-coral` 6.01, `vivid-purple` 5.79, `steel-blue` 5.78, `blue` 5.77, `violet` 5.30,
  `cadmium-red` 5.01, `dark-turquoise` 4.50.
- Allowed (3:1 to 4.5:1): `celestial-blue` 4.05, `coral` 3.65, `red` 3.50, `purple` 3.08, `green` 3.05,
  `rusty-orange` 3.03, `light-blue` 3.03, `bright-red` 3.02, `dark-green` 3.01.

Color follows MEANING, never variety; the same meaning keeps the same color on one page. As on the
stock dashboards:

| Meaning | Color | Stock example |
|---|---|---|
| Neutral count, amount or time | `steel-blue` | Number of accounts |
| Negative: overdue, missing data, expiring, inactive | `cadmium-red` | Accounts without primary contact |
| New, this month, inflow | `dark-turquoise` | New contacts this month |
| Attention: waiting, reopened | `burnt-coral` | Reopened cases |
| Good result | `forest-green` | First contact resolution |
| A separate category | `vivid-purple` or `navy-blue` | Emails, calls |

On a chart, don't recolor bars per value; the reference's series use `blue` and `burnt-coral`.

## Icon

Every metric tile MUST carry an icon (`config.layout.icon.iconName`) chosen by meaning; every metric on
the stock dashboards has one. Examples:

| Meaning | `iconName` |
|---|---|
| Accounts, contacts, orders | `work-icon`, `contact-group-icon`, `trolley-icon` |
| Amounts | `coins-icon`, `money-icon` |
| Overdue, inactive, response time | `hourglass-icon` |
| Duration, resolution time | `clock-icon` |
| Overdue response, overdue resolution | `message-warning-icon`, `problem-icon` |
| Waiting, reopened | `bell-icon` |
| Expiring | `fire-icon` |
| Missing data, contracts | `document-edit-icon`, `document-icon` |
| Goal, open opportunities, first contact resolution | `goal-icon` |
| Calls, emails | `phone-icon`, `mail-icon` |
| Satisfaction | `like-icon` |
| Resolved, closed | `checkmark-filled-icon` |
| New this month, in progress | `diagram-icon`, `level-icon` |

## Card theme

Every widget on a dashboard AND on a home page uses the plain-white card, `theme` "without-fill", like
the base product: a customization should not stand out. On a home page this OVERRIDES the
`crt.IndicatorWidget` component documentation, which says `theme` "full-fill" for home pages: follow
this guide. Use a colored card only when the user explicitly asks for one, and never in a banned color.

## Adding to an existing page

1. Read the page with `get-page`: the `Main` rows and every widget's `layoutConfig`, `theme`, and color.
2. Copy the SIZE and TEXT settings of the sibling metric tiles (`colSpan`, `rowSpan`,
   `text.fontSizeMode`, `text.labelPosition`), or take them from "Metric tile size" when the page has
   no metric yet, but never their `theme` or a banned color: the new tile is "without-fill" with a
   color by meaning from "Color", even when its siblings are full-fill or use a banned color. Its
   `text.template` is `{0}` even when a sibling repeats its name as a caption.
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

- the new tile is `{ "column": 1, "row": 13, "colSpan": 2, "rowSpan": 3 }`, "without-fill",
  `checkmark-filled-icon`, a color by meaning (e.g. `forest-green` for a good result),
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

Height is `layoutConfig.rowSpan`. Platform defaults: gauge 3, chart 9, funnel 15, list/pivot 9. HARD
FLOOR: a chart, list, or pivot must have `rowSpan` >= 6, below that it renders unreadably short. A
METRIC TILE is exempt and takes its surface size ("Metric tile size"). A GAUGE is NOT: it ships the
tile's default of 3, but at 3 rows its semicircular arc is too small to read. Give a gauge `rowSpan` 6,
floor 5.

- Metric (indicator): a headline number (count, sum, average, min/max, a "without X" gap count). Size,
  color, and icon per this guide; payload per `indicator-widget`.
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

- `Main` rows read; every metric tile has its surface size, is at most 4 columns wide, and is not
  stretched to fill a row.
- Metric tiles form one block at the top (or under it on an existing page), never among the charts or
  appended at the bottom.
- A widget beside a stack ends on the same row as the stack; every chart row sums to 12 columns.
- Every widget is "without-fill" (home pages too); no `orange-red`, `orange`, `light-green` or
  `turquoise` anywhere; colors follow meaning.
- Every metric tile has an icon chosen by meaning, its surface's `text.fontSizeMode` (on an existing
  page its siblings') and `text.template` `{0}`, its name only in `config.title`.
- Each chart/list/pivot meets the `rowSpan` floor (>= 6; default 9, funnel 15); each gauge is 6.
- Multi-topic surfaces are split into labeled sections.

For a List widget's binding and clone/repair contract, MUST read `list-widget` and fetch
`get-component-info` for its runtime assembly documentation.

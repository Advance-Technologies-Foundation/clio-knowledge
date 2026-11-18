clio MCP indicator widget guide

Before you create, edit, filter, or troubleshoot a `crt.IndicatorWidget` on a Freedom UI page,
you MUST call `get-component-info` for `crt.IndicatorWidget` and read its documentation in full,
including every reference and link it points to.

That component documentation is the single source of truth for indicator widgets. It owns the
generation contract (diff sections, aggregation expression, filter-leaf shapes), the intent ->
runtime config translation, the authoring workflow, and the related `esq-filters`,
`page-modification`, and `page-schema-resources` guidance.

Do NOT author or edit an indicator widget payload from memory or from this pointer alone — read
the `get-component-info` documentation and its references first.

----

## General

### Classify the filter intent BEFORE authoring a filter
An analytics widget can carry three DIFFERENT kinds of filter. Decide which the user means from the wording
and never conflate them; when more than one is present they compose with AND:

- **Record-context (page data)** — the widget follows the record or the host list/dashboard. Signals: "for the
  current \<X\>", "related to this \<X\>", "on the \<X\> form page".
- **Pre-configured (static)** — a FIXED condition the creator sets now; the end user cannot change it at
  runtime. Signals: "only \<X\>", "restricted to", an explicit fixed value or set.
- **Quick filter (interactive)** — the END USER chooses the value at runtime. Signals: "so users can choose",
  "let the user pick", "selectable", "a \<X\> selector". A widget supports it when its own `get-component-info`
  shows a `filterAttributes` slot — metric, chart, gauge, list, pivot, waterfall and pipeline-movement all do.
  NEVER silently emit a fixed filter for a quick-filter request.

Wording that carries BOTH a fixed set and a user choice ("add a Stage filter so users can choose ... such as
Qualification, Proposal") asks for both halves: the static filter over the listed set AND the quick-filter entry.

Which config slot each kind lands in is owned by the widget's own documentation: read `get-component-info` for
the widget, and for the chip side read it for `crt.QuickFilter` ("Target is an analytics widget").

### Title localization
The widget `config.title` is emitted as `#ResourceString(IndicatorWidget_<slug>_title)#`. Clio registers it ONLY when you pass it in
the `resources` parameter.
ALWAYS pair the title with `resources: '{"IndicatorWidget_<slug>_title": "<the title text>"}'`.
Saving (`update-page` / `sync-pages`) now HARD REJECTS an inserted widget title whose key would not
be registered this way; `validate-page` flags it as a warning  — see `page-schema-resources`.

### Placement Rules
- Never set `parentName` as code of a dashboard component.
- You may use `parentName`: "Main" only when working with Home pages.
- On any other page, if the user asks to add a widget but does not clarify where on the page, and
  you know there are other widgets, place it near the existing ones (use the same `parentName` as
  another widget).
- On a dashboard or home page, tile size and placement are owned by `dashboard-and-home-page-layout`, and the
  tile's color and icon by `widget-color-and-icon` (get-guidance name=widget-color-and-icon): read both before
  you add or move a tile.

## Metric title and text
These hold for a metric on any page, also on an existing page whose sibling metrics break them.

- TITLE: `config.title` is a short noun phrase naming the measure, 2-4 words on ONE line: aim at 25
  characters or fewer, never more than 30. Count the text the title registers, not its
  `#ResourceString(...)#` key. After the name only a unit after a comma ("Average resolution time,
  hours") or a qualifier in parentheses ("Overdue cases (response)"). Never append a filter as a comma
  list, as in "Closed requests, Satisfaction 1": fold it into the words ("Requests rated 1",
  "Low-rated requests"). Stock titles read like "Overdue orders" or "Cases in progress".
- VALUE ONLY: `text.template` is exactly `{0}`, the value alone; a `#ResourceString(...)#` template
  registers the text `{0}`. The metric's name is `config.title`: never repeat it as a caption inside
  the tile, as in "Open critical requests: {0}". Text beside `{0}` belongs only to a comparison metric
  (e.g. "10 %"). All 76 stock metric tiles use `{0}`.

The text size (`text.fontSizeMode`) depends on the surface: on a dashboard or home page it is owned by
`dashboard-and-home-page-layout`.

## Card theme
The card theme is set by the SURFACE's guide, not here: `dashboard-and-home-page-layout` for dashboards and home
pages (plain-white / `theme` "without-fill"), `desktop-page` for desktops (glassmorphism). For the
rest of the runtime config read the `crt.IndicatorWidget` documentation via `get-component-info`.

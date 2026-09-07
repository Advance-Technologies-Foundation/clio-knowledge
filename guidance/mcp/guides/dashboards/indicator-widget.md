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

## Card theme
The card theme is set by the SURFACE's guide, not here: `dashboard-and-home-page-layout` for dashboards
and home pages (a metric tile is plain-white / `theme` "without-fill" on both; a filled tile is that
guide's rare critical-KPI exception), `desktop-page` for desktops (glassmorphism). For the rest of the
runtime config read the `crt.IndicatorWidget` documentation via `get-component-info`.

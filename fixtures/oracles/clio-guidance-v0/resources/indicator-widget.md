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

### Placement Rules
- Never set `parentName` as code of a dashboard component.
- You may use `parentName`: "Main" only when working with Home pages.
- On any other page, if the user asks to add a widget but does not clarify where on the page, and
  you know there are other widgets, place it near the existing ones (use the same `parentName` as
  another widget).

## Page specific rules
Pick `theme` / `layout.color` by the page surface (desktop, list/form, home). Those per-surface
defaults are owned by the `crt.IndicatorWidget` documentation — read it via `get-component-info`.
For dashboards, see the `dashboards` guidance (band/grid layout and the plain-white card policy).

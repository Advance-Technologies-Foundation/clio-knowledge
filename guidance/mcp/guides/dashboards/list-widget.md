clio MCP list widget binding guide

This guide owns the pre-edit binding checks and clone/repair workflow for ordinary WEB
`crt.ListWidget` and `crt.DataGrid` entity-backed collections. Read `page-modification` first.
It does not own component payloads: call `get-component-info` for the exact type and environment;
for ListWidget also fetch `crt.DataGrid` and follow its embedded assembly documentation.
Read `dashboard-and-home-page-layout` for placement and styling, `related-list` for master-detail
dependencies, `esq-filters-frontend` for filter syntax, and `page-schema-resources` for localization.
Mobile and custom/programmatically populated collections are outside this recipe.

## Distinguish the two meanings of items

For these data-bound lists, `values.items` is a binding expression such as `$ListWidget_abc123`.
It resolves to a collection of rows; it is NOT a child-component slot. Preserve it as a string.
Do not replace it with `[]` to satisfy the new-container rule, or insert child components into it.
The insert's `propertyName: "items"` instead names the PARENT container's child slot.
The component registry may describe the resolved runtime input as `DataItem[] | BaseViewModelCollection`;
that does not make the page's binding expression a container array.

## Resolve the complete binding chain before editing

An entity-backed list requires three coordinated pieces, which may live in different ancestors:

| Piece | Required connection |
| --- | --- |
| View node in `viewConfigDiff` | `values.items` points to the collection attribute. `columns[].code` and `primaryColumnName` name row attributes. |
| Collection in `viewModelConfigDiff` | `attributes[collectionName]` has `isCollection: true`, `modelConfig.path` identifying the source, and `viewModelConfig.attributes` mapping row attributes to source columns. |
| Source in `modelConfigDiff` | `dataSources[sourceName]` defines the `crt.EntityDataSource`, its scope, entity, and projected attributes. |

For a simple `$Name` binding, remove ONLY the leading `$` to obtain `collectionName`.
Keep the ENTIRE `ListWidget_` or `DataGrid_` prefix. Read `sourceName` from the collection's
`modelConfig.path`; do not infer it from a hash or append `DS` blindly. Generated names are a
convention, not a lookup algorithm. Expressions more complex than a simple attribute reference
need their own analysis; do not strip or rewrite them heuristically.

Trace every displayed `columns[].code` and `primaryColumnName` through the collection's row
attributes and their `modelConfig.path` to the actual source. Preserve the identity mapping even
if the identifier is not displayed. Use the registry recipe for column types and lookup values.

Preserve each referenced `modelConfig.filterAttributes[].name` and its corresponding page
attribute value. A generated `<collectionName>_PredefinedFilter` is a sibling attribute, not a
child row attribute. It is required WHEN referenced; a list with no filter reference does not
need an invented empty filter. Preserve sorting, paging and intended filtering together. Do not
drop a filter or dependency to make a spinner disappear: that can broaden the displayed records.
For page-record scoping, follow `related-list`; do not copy a donor's master dependency without
verifying that its master source exists and means the same thing on the target.

## Clone or repair safely

1. Fetch donor and target with `get-page`, using separate output directories when necessary.
   Each call replaces its schema scratch directory; preserve edits outside that directory.
2. Find the donor node in the merged `bundle.json.viewConfig`, then follow the exact chain above
   through `viewModelConfig.attributes` and `modelConfig.dataSources`. Inspect the target's
   inherited configuration too. An empty local diff does not establish that a binding is absent.
3. Check that the bundle actually contains the donor's working wiring. If it does not, STOP the
   extraction and inspect `body.js` and the relevant ancestor bodies; do not invent missing entries
   or declare the runtime invalid. Some readbacks can omit a nested merge that the browser applies.
   Continue only after you can account for the effective binding chain and referenced filters.
4. Copy the view node plus any companion metadata not already supplied correctly by the target.
   Do not resend the donor's entire body or overwrite unrelated target data sources. Resolve name
   collisions deliberately; if renaming, update EVERY connected reference consistently.
5. Use section-specific diff operations. A root merge for the source has `path: []` and
   `values: { "dataSources": { "<sourceName>": <copied source> } }`. A root merge for the collection
   and referenced filter attributes has `path: []` and
   `values: { "attributes": { "<collectionName>": <copied collection>, ... } }`.
   These are structural illustrations, not literal JSON to submit. Preserve the source's complete
   registry-supported configuration. Resolve captions/titles in the target's resource context.
6. Follow `page-modification-overview` for the supported save mode and conflict/readback flow.
   Run `validate-page`, save with `update-page` or `sync-pages`, and re-read the target. Neither a
   successful save nor `valid: true` proves the list can load its data source at runtime.
7. Open the target in the browser, wait for initial loading, and verify known expected rows and
   displayed values, including intended exclusions from filters. Navigate away and reopen it to
   verify persisted wiring. A title, card frame, or column header alone is NOT acceptance.

## Failure signals and limits

A persistent loading placeholder/spinner with no expected rows can indicate a dangling collection
binding or missing source. Compare the chain before changing visual properties. Loading briefly is
normal; a settled empty result can also be correct for the current filter or permissions. If the
chain is complete, inspect the actual request failure and access/filter outcome; do not diagnose
every empty list as this defect or bypass permissions.

`validate-page` is an offline body check, not a full runtime/inheritance/data-access oracle.
Do not reject a list solely because its source is absent from the leaf body. This guidance adds
an authoring gate, not a claim that clio now hard-rejects every dangling list binding.

Evidence: the disposable lab for clio#1412 / clio-knowledge#161 used Creatio 10.1.585, .NET 8,
PostgreSQL and clio 8.1.0.127. A view-only clone passed validation and save but showed the loading
placeholder; copying the complete entity-backed binding repaired the target and loaded the same
five known contacts. A second clone copied the referenced predefined filter and, after navigation
away and reopening, showed only the expected contact and excluded the other four. Detailed
observations are recorded with those issues. The live component catalog returned an
`environment-superset`, so browser execution, not that catalog's version label, establishes this
case. Freedom UI's `ListWidgetToSchemaConverter` at revision
`ebc0208fa6181d18bfd098cf060e259fe7d51fee` independently emits the three matching sections and
referenced predefined-filter attribute. These checks do not establish all custom collection,
filter, or inherited-schema variants as tested.

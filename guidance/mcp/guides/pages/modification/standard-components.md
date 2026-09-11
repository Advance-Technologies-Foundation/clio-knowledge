clio MCP page modification standard components guide

This is a focused sub-guide of `page-modification`. It owns ONE thing: the canonical configuration of
the two standard record-page components a Freedom UI FORM page is expected to carry — the record feed
(`crt.Feed`) and the attachments list (`crt.FileList`) — and the MERGE-vs-INSERT decision that precedes
writing either of them.

It does NOT own, and does not restate:
- the property vocabulary of either component — `get-component-info` for `crt.Feed` / `crt.FileList` is
  authoritative, and the COMPONENT-TYPE VERIFICATION step in `page-modification` is mandatory here too;
- how to pick a `parentName` — `page-modification-containers`;
- how a `viewConfigDiff` entry is composed — `page-modification-components`;
- the caption/resource rule for the grid column — `page-schema-resources` (it already names the
  `#ResourceString(AttachmentListDS_Name)#` macro form as the established convention for embedded grids);
- the collection-attribute + `crt.EntityDataSource` wiring pattern in general — `related-list`.

What this guide supplies is the part none of those carry: the VALUES.

Why this guide exists, and how it relates to `get-component-info`
The component catalog is authoritative for the property vocabulary and stays so. What it cannot carry
is what the PARENT TEMPLATE of the page already ships, and for the attachments list its worked example
describes a different case from the one a record page needs.

- `crt.Feed` — the catalog's `documentation` already gives the record-feed combination, and the measured
  creation-flow page agrees with it property for property. This guide CONFIRMS that set by measurement
  and adds the part the catalog cannot know: whether to `merge` or `insert` it.
- `crt.FileList` — the catalog's worked example builds a file list over a PER-ENTITY file entity
  (`ContactFile`), scoped by a master column named after that entity (`recordColumnName: "Contact"`),
  rendered as `viewType: "list"`, with its own data-source name and explicit upload handlers. The
  attachments tab the platform's own section/application creation flow produces is a DIFFERENT shape:
  `SysFile`, `recordColumnName: "RecordId"`, `viewType: "gallery"` with `tileSize`, the data source named
  `AttachmentListDS`, and NO handlers at all. Both shapes are real; only the second is the record-page
  attachments tab. An agent that follows the catalog example for a record page produces a list that
  works and does not match the platform, which is the drift this guide exists to stop.

Two failures follow from getting this wrong, and neither is visible at save time: an `insert` over a
component the parent template ALREADY ships saves with `success: true` and renders the component twice,
and an `AttachmentList` without its companion `AttachmentListDS` data source renders empty and never
issues a query.

STEP 1 — MERGE or INSERT. Decide before writing anything.
The parent template, not the page, decides this.

- You MUST use `"operation": "merge"` when the parent template already ships the component and its tab
  container. The template supplies the structure; the page supplies only the object-specific values.
- You MUST use `"operation": "insert"` — with the SAME value set — when the template ships neither, and
  you must then also supply the pieces the template was supplying (see STEP 4).
- You MUST NOT emit an `insert` for a component the template already ships. It is not corrected by the
  platform and it is not reported by `update-page`: the save returns `success: true` and the record page
  renders the component twice.

How to tell, without guessing: call `get-page` on a page built from that template and read
`bundle.containers`. A template that ships the feed exposes a container named `FeedTabContainer`; one
that ships attachments exposes `AttachmentsTabContainer`. Absent container means absent component, and
that is the INSERT path.

STEP 2 — `crt.Feed`, canonical value set
Merged onto (or inserted into) `FeedTabContainer`. Every property below is required; none of them has a
useful default.

| Property | Value | Note |
| --- | --- | --- |
| `type` | `crt.Feed` | |
| `feedType` | `"Record"` | The record-scoped feed. Not a free-form label — see `get-component-info` for the accepted set. |
| `primaryColumnValue` | `"$Id"` | Binds the feed to the open record; requires the `Id` attribute of STEP 5. |
| `cardState` | `"$CardState"` | Supplied by the record-page template. |
| `dataSourceName` | `"PDS"` | The page `primaryDataSourceName`. Use the page's actual value if it is not `PDS`. |
| `entitySchemaName` | the page's own object | OBJECT-SPECIFIC. It MUST equal the `entitySchemaName` of the page's primary data source, never a copied literal. |

```jsonc
{
  "operation": "merge",
  "name": "Feed",
  "values": {
    "type": "crt.Feed",
    "feedType": "Record",
    "primaryColumnValue": "$Id",
    "cardState": "$CardState",
    "dataSourceName": "PDS",
    "entitySchemaName": "UsrSourceCodes"
  },
  "parentName": "FeedTabContainer",
  "propertyName": "items",
  "index": 0
}
```

The feed carries no `layoutConfig` — it fills its tab container.

STEP 3 — `crt.FileList`, canonical value set
Merged onto (or inserted into) `AttachmentsTabContainer`.

| Property | Value | Note |
| --- | --- | --- |
| `type` | `crt.FileList` | |
| `masterRecordColumnValue` | `"$Id"` | The open record the files hang off; requires the `Id` attribute of STEP 5. |
| `recordColumnName` | `"RecordId"` | The `SysFile` column pointing back at the master record. |
| `items` | `"$AttachmentList"` | The collection attribute the list binds to. |
| `primaryColumnName` | `"AttachmentListDS_Id"` | Derived from the data-source name of STEP 4. |
| `columns` | one column over the file name | See below. |
| `viewType` | `"gallery"` | |
| `tileSize` | `"small"` | |

```jsonc
{
  "operation": "merge",
  "name": "AttachmentList",
  "values": {
    "type": "crt.FileList",
    "masterRecordColumnValue": "$Id",
    "recordColumnName": "RecordId",
    "layoutConfig": { "column": 1, "row": 1, "colSpan": 2, "rowSpan": 6 },
    "items": "$AttachmentList",
    "primaryColumnName": "AttachmentListDS_Id",
    "columns": [
      {
        "id": "<a fresh GUID>",
        "code": "AttachmentListDS_Name",
        "caption": "#ResourceString(AttachmentListDS_Name)#",
        "dataValueType": 28,
        "width": 200
      }
    ],
    "viewType": "gallery",
    "tileSize": "small"
  },
  "parentName": "AttachmentsTabContainer",
  "propertyName": "items",
  "index": 0
}
```

- The column `id` is a per-page generated GUID. You MUST generate a fresh one. Copying the GUID out of
  another page's body (or out of this guide) is how two pages end up sharing a column identity.
- `dataValueType` 28 is the file-name column's type. Take the value from the column you are binding
  rather than assuming 28 for any other column you add.
- The `caption` uses the `#ResourceString(...)#` macro form deliberately; `page-schema-resources` owns
  that rule and states which keys need registering. Do NOT write an inline literal — `update-page`
  hard-rejects one.
- `layoutConfig` is the observed placement inside a template-shipped attachments tab. On the INSERT path
  it is container-dependent: set it for the container you are actually inserting into.

STEP 4 — the companion data source is part of the deliverable, not an option
`AttachmentList` does not carry its own entity. You MUST declare `AttachmentListDS` under
`modelConfig.dataSources` in the same change. Without it, `primaryColumnName` and the `code` of every
column resolve to nothing: the list renders empty and never issues a query.

```jsonc
"AttachmentListDS": {
  "type": "crt.EntityDataSource",
  "scope": "viewElement",
  "config": {
    "entitySchemaName": "SysFile",
    "attributes": {
      "Name": { "path": "Name" }
    }
  }
}
```

The data-source NAME is load-bearing: `AttachmentListDS` is what makes `AttachmentListDS_Id` and
`AttachmentListDS_Name` resolve. Renaming the data source means renaming both.

STEP 5 — what the template was supplying, and you are not
On the MERGE path the template already provides everything below, which is why a page created from a
feed-shipping template can carry the two components above and nothing else. On the INSERT path it does
not, and each item becomes yours:

- the tab containers themselves (`FeedTabContainer`, `AttachmentsTabContainer`) — a container you insert
  MUST initialize its content slot (`"items": []`), or the page fails at runtime with
  `Item "<name>" is not a container for other items`. See `page-modification-containers`.
- the `Id` attribute the `"$Id"` bindings resolve against:
  `"Id": { "modelConfig": { "path": "PDS.Id" } }` under `viewModelConfig.attributes`.
- the `AttachmentList` collection attribute that `items: "$AttachmentList"` binds to, over
  `AttachmentListDS`. The collection-attribute shape (`isCollection`, `modelConfig.path`, per-column
  child attributes) is owned by `related-list` — read it rather than improvising one.
- `$CardState`, which the record-page template provides.

UNSUPPORTED: reproducing the feed or the attachments list out of primitive components (a `crt.DataGrid`
over `SysFile`, a hand-built comment list). Neither is a substitute; both lose the platform behaviour
the record page is expected to have.

Verifying the result
`update-page` returning `success: true` is not evidence that either component works — an unknown type,
a duplicated insert and a missing data source all save cleanly. Reload the record page and confirm: the
feed tab shows the record feed exactly once, and the attachments tab shows the gallery, once, with its
Name column. A list that shows a `crt-data-grid-placeholder` has not loaded YET — `related-list` owns
the rule for telling that apart from a failure, and you MUST apply it before reporting either as broken.

Evidence
Lab scenario, 2026-09-11, Creatio Studio stand `eng96655`, read-only via `get-page`.

Reference page `UsrSourceCodes_FormPage` (package `UsrSourceCodes`), produced by the section/application
creation flow, parent template `PageWithTabsFreedomTemplate`. Its `viewConfigDiff` carries three
operations: one `insert` for the profile field, and `merge` for BOTH `AttachmentList` (onto
`AttachmentsTabContainer`) and `Feed` (onto `FeedTabContainer`). Every value in STEP 2 and STEP 3, and the
`AttachmentListDS` declaration in STEP 4, are read from that body verbatim; the page's own
`viewModelConfig` declares only `UsrName` and `Id`, which is what establishes that the collection
attribute and `$CardState` in STEP 5 come from the template rather than the page.

The `crt.Feed` / `crt.FileList` catalog responses quoted above were read from the same environment on
the same date; `get-component-info` reported `resolvedFrom: "environment-superset"` with
`resolvedTargetVersion: "latest"`, so the catalog was NOT version-pinned to that stand. That is why the
divergence is stated as "two shapes exist, this is the record-page one" rather than as a catalog defect.

Applicability: Freedom UI web FORM pages (`schema-type: "web"`). Mobile pages draw from a separate
catalog and map these components under different names — read `mobile-page-modification` first. The
merge-vs-insert rule is a property of the parent template, so it is re-checked per template rather than
assumed from this one.

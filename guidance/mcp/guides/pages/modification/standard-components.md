clio MCP page modification standard components guide

This is a focused sub-guide of `page-modification`. It owns the canonical configuration of the two
standard record-page components a Freedom UI FORM page is expected to carry — the record feed
(`crt.Feed`) and the attachments list (`crt.FileList`) — and the MERGE-vs-INSERT decision that precedes
writing either of them.

On the INSERT path you ALSO need `page-modification-template-supplied-parts`, which owns what the parent
template was supplying and now is not: the tab containers, the attachments toolbar that carries the
upload button, the collection attribute, and what you must NOT declare. The value sets below are not the
whole deliverable there. That guide also owns TAG chips (`crt.TagSelect`) end to end — tags are a
different shape and none of the rules below apply to them.

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
  rendered as `viewType: "list"`, with its own data-source name, `uploadClicked` / `fileDropped` bound on
  the component itself and handlers written for `crt.UploadFileRequest`. The attachments tab the
  platform's own section/application creation flow produces is a DIFFERENT shape: `SysFile`,
  `recordColumnName: "RecordId"`, `viewType: "gallery"` with `tileSize`, the data source named
  `AttachmentListDS`, and no upload binding on the component at all — the merged page declares an EMPTY
  `handlers` array. That is not the same as "no upload wiring": upload and refresh are buttons in the tab
  container's `tools` toolbar, and on the INSERT path they are yours to supply — the block is in
  `page-modification-template-supplied-parts`.
  Both shapes are real; only the second is the record-page attachments tab. An agent that follows the
  catalog example for a record page produces a list that works and does not match the platform, which is
  the drift this guide exists to stop.

Two failure modes follow from getting this wrong, and neither is rejected at save time. Both are REASONED
FROM THE MEASURED STRUCTURE, NOT OBSERVED at runtime — treat them as hazards to design away from, not as
reported behaviour:

- an `insert` over a component the parent template ALREADY ships puts a second element of the same name
  into the same container. `update-page` validates the diff you send, not the merged result, so nothing
  rejects it. What the record page then renders was not observed.
- an `AttachmentList` without its companion `AttachmentListDS` data source has nothing for
  `primaryColumnName` or any column `code` to resolve against: every child attribute of the
  `AttachmentList` collection binds to a `AttachmentListDS.<column>` path, so with the data
  source absent the list has no query to issue.

STEP 1 — MERGE or INSERT. Decide before writing anything.
The parent template, not the page, decides this.

- You MUST use `"operation": "merge"` when the parent template already ships the component and its tab
  container. The template supplies the structure; the page supplies only the object-specific values.
- You MUST use `"operation": "insert"` — with the SAME value set — when the template ships neither, and
  you must then also supply every piece the template was supplying — read
  `page-modification-template-supplied-parts` before you write the body. The value sets in STEP 2 and
  STEP 3 are NOT the whole deliverable on that path.
- You MUST NOT emit an `insert` for a component the template already ships. `update-page` checks the diff
  you send, not the merged result, so nothing corrects it and nothing reports it.

How to tell, without guessing: call `get-page` on a page built from that template and read the MERGED
`bundle.json`, not the page's own `body.js` — the body shows only that page's overlay. A template that
ships the feed exposes a container named `FeedTabContainer`; one that ships attachments exposes
`AttachmentsTabContainer`. Absent container means absent component, and that is the INSERT path. While
you are there, read `viewModelConfig.attributes` and `modelConfig.dataSources` in the same bundle: they
are what the INSERT-path inventory in `page-modification-template-supplied-parts` is checked against.

STEP 2 — `crt.Feed`, canonical value set
Merged onto (or inserted into) `FeedTabContainer`. Every property below is required; none of them has a
useful default.

| Property | Value | Note |
| --- | --- | --- |
| `type` | `crt.Feed` | |
| `feedType` | `"Record"` | The record-scoped feed. Not a free-form label — see `get-component-info` for the accepted set. |
| `primaryColumnValue` | `"$Id"` | Binds the feed to the open record. `$Id` is supplied by the record-page template on BOTH paths — never declare it yourself. |
| `cardState` | `"$CardState"` | Also template-supplied on both paths. It hides the post composer while the record is still in `Add` mode. |
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
| `masterRecordColumnValue` | `"$Id"` | The open record the files hang off. `$Id` is template-supplied on both paths. |
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
column have nothing to resolve against.

On the MERGE path the template has already declared this data source, and the measured page re-states
only the one attribute its column reads. So the block below is an OVERLAY, not the whole data source:

```jsonc
// MERGE path: an overlay on the declaration the template already carries
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

On the INSERT path nothing declares it for you, so you MUST declare it whole. The merged bundle of a
template that ships attachments carries FOUR attributes, and the collection attribute in
`page-modification-template-supplied-parts` binds a child attribute to each of them:

```jsonc
// INSERT path: the full declaration, as measured in the template's merged bundle
"AttachmentListDS": {
  "type": "crt.EntityDataSource",
  "scope": "viewElement",
  "config": {
    "entitySchemaName": "SysFile",
    "attributes": {
      "Name":      { "path": "Name" },
      "CreatedOn": { "path": "CreatedOn" },
      "CreatedBy": { "path": "CreatedBy" },
      "Size":      { "path": "Size" }
    }
  }
}
```

The data-source NAME is load-bearing: `AttachmentListDS` is what makes `AttachmentListDS_Id` and
`AttachmentListDS_Name` resolve. Renaming the data source means renaming both. `Id` is NOT among the
declared attributes and does not need to be — the measured child attribute `AttachmentListDS_Id` binds to
the path `AttachmentListDS.Id` regardless.

UNSUPPORTED: reproducing the feed or the attachments list out of primitive components (a `crt.DataGrid`
over `SysFile`, a hand-built comment list). Neither is a substitute; both lose the platform behaviour
the record page is expected to have.

Verifying the result
`update-page` returning `success: true` is not evidence that either component works — an unknown type, a
duplicated insert and a missing data source are none of them rejected. Reload the record page and confirm
in the browser: the feed tab shows the record feed exactly once; the attachments tab shows the gallery,
once, with its Name column AND its upload and refresh buttons; and an upload actually lands. A list that
shows a `crt-data-grid-placeholder` has not loaded YET — `related-list` owns the rule for telling that
apart from a failure, and you MUST apply it before reporting either as broken.

Evidence
Lab scenario, 2026-09-11, on an internal Creatio Studio stand, read-only via `get-page` and
`get-component-info`. Three schemas were read, each through the MERGED bundle rather than the page body.

1. `UsrSourceCodes_FormPage` (package `UsrSourceCodes`), produced by the section/application creation
   flow, parent template `PageWithTabsFreedomTemplate`. Its `viewConfigDiff` carries three operations: one
   `insert` for the profile field, and `merge` for BOTH `AttachmentList` (onto `AttachmentsTabContainer`)
   and `Feed` (onto `FeedTabContainer`). Every value in STEP 2 and STEP 3 is read from that body verbatim,
   as is the one-attribute `AttachmentListDS` overlay in STEP 4; the page's own `viewModelConfig` declares
   only `UsrName` and `Id`, and its `handlers` array is empty.
2. `PageWithTabsFreedomTemplate` — the MERGE-path template. It ships both tab containers, both components,
   the attachments tab toolbar, the `AttachmentList` collection attribute with its five child attributes
   and its `sortingConfig`, the four-attribute `AttachmentListDS`, and `Id` / `CardState` — the inventory
   `page-modification-template-supplied-parts` is built from. Its own `handlers` array is empty too.
3. `PageWithTopAreaAndTabsFreedomTemplate` — a template that ships NEITHER component, read to pin the
   INSERT path down instead of inferring it: no `FeedTabContainer`, no `AttachmentsTabContainer`, no
   `AttachmentList` attribute, no data sources at all — which is what establishes the INSERT path as a
   real case rather than a hypothetical, and what
   `page-modification-template-supplied-parts` measures its inventory against.

NOT observed, and marked as such where they appear: what a page renders after a duplicated insert, and
what a list missing its data source renders. Both are reasoned from the measured structure. No page was
written and no migration was run for this guide — every call was read-only.

The `crt.Feed` / `crt.FileList` catalog responses quoted above were read from the same environment on
the same date; `get-component-info` reported `resolvedFrom: "environment-superset"` with
`resolvedTargetVersion: "latest"`, so the catalog was NOT version-pinned to that stand. That is why the
divergence is stated as "two shapes exist, this is the record-page one" rather than as a catalog defect.

Applicability: Freedom UI web FORM pages (`schema-type: "web"`). Mobile pages draw from a separate
catalog and map these components under different names — read `mobile-page-modification` first. The
merge-vs-insert rule is a property of the parent template, so it is re-checked per template rather than
assumed from this one.

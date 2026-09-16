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
- `crt.FileList` — the catalog's worked example differs from a record page's attachments tab along TWO
  independent axes, and conflating them is what this guide first got wrong. Read them apart:
  - PRESENTATION AND WIRING, where the catalog example genuinely is a different case: `viewType: "list"`,
    its own data-source name, `uploadClicked` / `fileDropped` bound on the component and handlers written
    for `crt.UploadFileRequest`. A record page's tab is `viewType: "gallery"` with `tileSize`, the data
    source is `AttachmentListDS`, and the component carries no upload binding at all — the merged page
    declares an EMPTY `handlers` array. That is not "no upload wiring": upload and refresh are buttons in
    the tab container's `tools` toolbar, yours to supply on the INSERT path, and the block is in
    `page-modification-template-supplied-parts`. Follow the catalog here and you get a list that works
    and does not match the platform — the drift this guide exists to stop.
  - THE FILE ENTITY, where the catalog example is NOT a different case and this guide used to say it was.
    Its `ContactFile` / `recordColumnName: "Contact"` pair is the per-object shape, and it is CORRECT for
    any object that has an `<Entity>File` — which the creation-flow page STEP 3's shared shape was first
    measured on simply does not have. Both shapes are real, both are record-page attachments tabs, and
    STEP 3 decides between them per object. A live migration of a Classic-era object took the per-object
    pair and was right to; a table prescribing `SysFile` / `"RecordId"` unconditionally would have broken
    that page.

Three failure modes follow from getting this wrong, and none of them is rejected at save time. The first
two are REASONED FROM THE MEASURED STRUCTURE, NOT OBSERVED at runtime — treat them as hazards to design
away from, not as reported behaviour. The third has a measured half, marked inline:

- an `insert` over a component the parent template ALREADY ships puts a second element of the same name
  into the same container. `update-page` validates the diff you send, not the merged result, so nothing
  rejects it. What the record page then renders was not observed.
- an `AttachmentList` without its companion `AttachmentListDS` data source has nothing for
  `primaryColumnName` or any column `code` to resolve against: every child attribute of the
  `AttachmentList` collection binds to a `AttachmentListDS.<column>` path, so with the data
  source absent the list has no query to issue.
- an `AttachmentListDS` pointed at the wrong file entity, or a `recordColumnName` taken from the other
  shape. MEASURED: on the migrated object of STEP 3 the two shapes address disjoint sets of rows.
  REASONED, not observed: that the gallery therefore comes up empty. Nothing about it looks like a
  failure — the component renders, the tab opens, the save succeeded.

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

FIRST, resolve WHERE this object's attachments live. That one fact decides TWO values —
`recordColumnName` here and `entitySchemaName` on the companion data source of STEP 4 — and there is no
single canonical answer. The data source names the entity the file rows are in; `recordColumnName` names
the column IN that entity pointing back at the open record. Take the two from different shapes, or either
from another page, and the gallery queries a table the record's files are not in. It renders empty, and
`update-page` still reports success.

Two shapes exist. BOTH are real, both measured, and neither is the default:

| | SHARED file table | PER-OBJECT file table |
| --- | --- | --- |
| `AttachmentListDS` → `entitySchemaName` | `SysFile` | `<Entity>File` — e.g. `UsrToMigrateFile` |
| `recordColumnName` | `"RecordId"` | the master lookup on that entity — e.g. `"UsrToMigrate"` |
| how a file row keys its record | `RecordId` (Guid) + `RecordSchemaName` (text) — entity-agnostic | one REQUIRED Lookup to the object |
| which objects have it | objects with no `<Entity>File` — what the Freedom creation flow produces | objects whose Classic section wizard created satellite schemas — typical of a migrated section |

How to decide, per object. Do NOT assume, and do NOT copy the shape off another page:

1. Call `find-entity-schema` with `search-pattern: "<Entity>"` against the target environment. A schema
   named `<Entity>File` whose `parent-schema-name` is `File` is this object's attachment store — take the
   PER-OBJECT column.
2. No such schema — take `SysFile` / `"RecordId"`. (`SysFile` inherits `File` too, so the parent alone
   decides nothing. What distinguishes it is its OWN columns, `RecordId` and `RecordSchemaName`, which is
   what makes it entity-agnostic.)
3. On the per-object branch, READ `recordColumnName` rather than spelling it: call
   `get-entity-schema-properties` on `<Entity>File` and take the column whose `source` is `own` and whose
   `reference-schema-name` is the object. It is conventionally the object's own name, and on the measured
   schema it is exactly that — but that is a wizard naming convention, not a contract, so read it.

`get-component-info` says the same from its side, and always did: `recordColumnName` is "the column on
the file entity that references the parent record", and its second named pitfall is a wrong one — "the
file entity must have a lookup column back to the master entity (e.g. `Contact` on `ContactFile`)". The
general rule was in the catalog; this guide is what pinned it to one entity.

The existence check is the SIGNAL, not the proof. When a gallery comes up empty and you want the answer
rather than an inference, count rows in both candidates — on the migrated object measured here the
record's one attachment is a row in `UsrToMigrateFile` and `SysFile` carries none, so the `SysFile` shape
would have rendered empty over a record that demonstrably has an attachment. A `crt.RichTextEditor` on
the same page corroborates for free: its `filesStorage` block spells the decision out in
`entitySchemaName`, `recordEntitySchemaName` and `recordColumnName`.

| Property | Value | Note |
| --- | --- | --- |
| `type` | `crt.FileList` | |
| `masterRecordColumnValue` | `"$Id"` | The open record the files hang off. `$Id` is template-supplied on both paths. |
| `recordColumnName` | OBJECT-SPECIFIC — `"RecordId"` OR the per-object master lookup | Decided above. It names the column in the STEP 4 data-source entity that points back at the master record; the two MUST come from the same shape. |
| `items` | `"$AttachmentList"` | The collection attribute the list binds to. |
| `primaryColumnName` | `"AttachmentListDS_Id"` | Derived from the data-source name of STEP 4. |
| `columns` | one column over the file name | See below. |
| `viewType` | `"gallery"` | |
| `tileSize` | `"small"` | |

A — SHARED file table (`SysFile`), as measured on the creation-flow page:

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

B — PER-OBJECT file table, as measured on the migrated page. `recordColumnName` is the ONLY property
that differs — everything else, including the `merge` operation and its placement, is shape-independent —
and it is paired with `entitySchemaName: "UsrToMigrateFile"` in STEP 4:

```jsonc
{
  "operation": "merge",
  "name": "AttachmentList",
  "values": {
    "type": "crt.FileList",
    "masterRecordColumnValue": "$Id",
    "recordColumnName": "UsrToMigrate",
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

Its `entitySchemaName` is the OTHER half of the STEP 3 decision. The two blocks below differ in
ATTRIBUTE COUNT — merge overlay against full insert declaration — and NOT in entity, which STEP 3 decides
per object independently of the merge-vs-insert path. They are quoted from the two measured pages, which
happen to sit on opposite sides of both choices.

On the MERGE path the template has already declared this data source, and the measured page re-states
only the one attribute its column reads. So the block below is an OVERLAY, not the whole data source:

```jsonc
// MERGE path: an overlay on the declaration the template already carries.
// Quoted from the MIGRATED page, so the entity here is the PER-OBJECT one.
"AttachmentListDS": {
  "type": "crt.EntityDataSource",
  "scope": "viewElement",
  "config": {
    "entitySchemaName": "UsrToMigrateFile",
    "attributes": {
      "Name": { "path": "Name" }
    }
  }
}
```

An overlay that re-states `entitySchemaName` REPLACES the template's. That is what makes the migrated
page work, and it is also why an overlay copied wholesale from another object silently re-points the
gallery at that object's file table.

On the INSERT path nothing declares it for you, so you MUST declare it whole. The merged bundle of a
template that ships attachments carries FOUR attributes, and the collection attribute in
`page-modification-template-supplied-parts` binds a child attribute to each of them:

```jsonc
// INSERT path: the full declaration, as measured in the template's merged bundle.
// The entity shown is the SHARED one; substitute <Entity>File per STEP 3 when the object has one.
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
once, with its Name column AND its upload and refresh buttons; the files ALREADY on the record appear
(an empty gallery on a record that has attachments is the STEP 3 shape mismatch, not a load delay); and an
upload actually lands. A list that
shows a `crt-data-grid-placeholder` has not loaded YET — `related-list` owns the rule for telling that
apart from a failure, and you MUST apply it before reporting either as broken.

Evidence
Two lab scenarios on internal Creatio Studio stands, read-only via `get-page`, `get-component-info`,
`find-entity-schema`, `get-entity-schema-properties` and read-only SQL. The first (2026-09-11) measured
three schemas through the MERGED bundle rather than the page body. The second (2026-09-16) measured a
REAL MIGRATION RUN, and it is what branched STEP 3.

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

4. `UsrToMigrateFreedom_FormPage` (package `UsrToMigrateApp`, parent template
   `PageWithTabsFreedomTemplate`) — a Classic section migrated to Freedom UI on a second stand, read
   2026-09-16 on record `2c1ab91e-0895-4ec1-b394-d4edab9d87fa`. It is the PER-OBJECT reference in STEP 3
   and STEP 4: its `merge` for `AttachmentList` carries `recordColumnName: "UsrToMigrate"`, its
   `AttachmentListDS` overlay carries `entitySchemaName: "UsrToMigrateFile"`, and every other value
   matches the creation-flow page property for property. Schemas read the same day: `UsrToMigrateFile` (`parent-schema-name: "File"`; ONE
   own column, `UsrToMigrate`, a required Lookup to the object) against `SysFile` (`File` as parent too —
   own columns `RecordId`, `RecordSchemaName`, `LastError`, `ToDelete`). Row counts: the record's single
   attachment, `Screenshot 2026-08-25 143520.png`, is a row in `UsrToMigrateFile` keyed to that record,
   while `SysFile` holds 2,681 rows, ALL for `RecordSchemaName = 'ConfActivityLog'` and none for this
   object. 49 `<Entity>File`-shaped tables exist there, so the per-object shape is the norm for
   Classic-era objects, not an exotic case. `UsrSourceCodes` has no `UsrSourceCodesFile` at all — which is
   why the first scenario could only measure the shared shape, and why this guide prescribed it as if it
   were the only one.

NOT observed, and marked as such where they appear: what a page renders after a duplicated insert, what a
list missing its data source renders, and what a gallery pointed at the wrong file entity renders. All
three are reasoned from the measured structure; for the third, the disjointness of the two row sets IS
measured and only the rendering is not. No page was written by either scenario — every call was
read-only. The migration of scenario 4 was performed by someone else and is read here as evidence, not
produced here.

The `crt.Feed` / `crt.FileList` catalog responses quoted above were read from the same environment on
the same date; `get-component-info` reported `resolvedFrom: "environment-superset"` with
`resolvedTargetVersion: "latest"`, so the catalog was NOT version-pinned to that stand. That is why the
divergence is stated as two axes, one of which the catalog answers correctly, rather than as a catalog
defect. The correction in STEP 3 runs in the catalog's favour: its `ContactFile` / `"Contact"` pair was
right for the case it documents, and this guide was the one over-generalising.

Applicability: Freedom UI web FORM pages (`schema-type: "web"`). Mobile pages draw from a separate
catalog and map these components under different names — read `mobile-page-modification` first. The
merge-vs-insert rule is a property of the parent template, so it is re-checked per template rather than
assumed from this one.

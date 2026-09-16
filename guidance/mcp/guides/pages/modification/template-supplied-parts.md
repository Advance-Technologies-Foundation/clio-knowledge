clio MCP page modification template-supplied parts guide

A Freedom UI record-page template does not merely lay out containers. It SUPPLIES working parts: the page
attributes the standard components bind to, the tab containers they sit in, the toolbar whose button
uploads a file, the data source the attachments list queries, and the tag control itself. This sub-guide
of `page-modification` owns that inventory, and the two questions that follow from it:

- which parts STOP being supplied when you `insert` a component onto a template that does not ship it,
  and therefore become yours to write;
- which parts are NEVER yours, because the template or the platform always provides them — tag chips
  (`crt.TagSelect`) above all, where hand-wiring is the defect rather than the fix.

Read this when you are inserting a record feed or an attachments list onto a template that does not
already carry one, when you are touching TAGS on a form page, when a tag control renders empty, or when
you simply need to know what the template gives you and what you must build.

The canonical VALUE SETS for `crt.Feed` and `crt.FileList`, and the MERGE-vs-INSERT decision that comes
before either, live in `page-modification-standard-components`. Read that guide first when you are adding
one of those two components: this one is what its INSERT path additionally requires. For tags you do not
need it — the TAGS section below stands on its own.

It does NOT own, and does not restate:
- the property vocabulary of any component — `get-component-info` is authoritative, and the
  COMPONENT-TYPE VERIFICATION step in `page-modification` is mandatory here too;
- how to pick a `parentName`, or the rule that an inserted container must initialize its content slot —
  `page-modification-containers`;
- how a `viewConfigDiff` entry is composed — `page-modification-components`;
- which resource keys need registering for the captions quoted below — `page-schema-resources`;
- the collection-attribute + `crt.EntityDataSource` wiring pattern in general — `related-list`.

The inventory, as measured
Two templates were measured for this — one that ships both the feed and the attachments list, and one
that ships neither — and the difference between them IS this guide.

| Part | Where it lives | Yours on an INSERT? |
| --- | --- | --- |
| `FeedTabContainer` / `AttachmentsTabContainer` | `viewConfig` | YES |
| the attachments tab's `tools` toolbar (`AttachmentAddButton`, `AttachmentRefreshButton`) | the tab container's `tools` slot | YES |
| the `AttachmentList` collection attribute (five child attributes + `sortingConfig`) | `viewModelConfig.attributes` | YES |
| `AttachmentListDS` | `modelConfig.dataSources` | YES — and in full |
| `Id`, `CardState` | `viewModelConfig.attributes` | NO — always supplied |
| `crt.TagSelect` | `CardToolsContainer` | NO — always supplied, and never hand-wired |

On the MERGE path the template provides every row of it, which is why a page created from such a template
carries the two component value sets and nothing else. On the INSERT path the YES rows become yours, and
the value sets alone are NOT a working deliverable.

YOURS on the INSERT path

1. The tab containers themselves (`FeedTabContainer`, `AttachmentsTabContainer`). A container you insert
   MUST initialize its content slot (`"items": []`), or the page fails at runtime with
   `Item "<name>" is not a container for other items`. See `page-modification-containers`.

2. The attachments tab's TOOLBAR. This is the one most easily missed, and an inserted gallery without it
   has no control that uploads a file. The measured `AttachmentsTabContainer` carries a `tools` slot with
   a header label and TWO buttons:

```jsonc
"tools": [{
  "type": "crt.FlexContainer", "direction": "row", "alignItems": "center",
  "name": "AttachmentsTabContainerHeaderContainer",
  "items": [
    { "type": "crt.Label", "caption": "#ResourceString(AttachmentsTabContainerCaption)#",
      "labelType": "headline-3", "name": "AttachmentsTabContainerHeaderLabel" },
    { "type": "crt.Button", "name": "AttachmentAddButton",
      "caption": "#ResourceString(AttachmentAddButtonCaption)#",
      "icon": "upload-button-icon", "iconPosition": "only-icon", "color": "default", "size": "medium",
      "clicked": { "request": "crt.UploadFileRequest",
                   "params": { "viewElementName": "AttachmentList" } } },
    { "type": "crt.Button", "name": "AttachmentRefreshButton",
      "caption": "#ResourceString(AttachmentRefreshButtonCaption)#",
      "icon": "reload-button-icon", "iconPosition": "only-icon", "color": "default", "size": "medium",
      "clicked": { "request": "crt.LoadDataRequest",
                   "params": { "config": { "loadType": "reload" },
                               "dataSourceName": "AttachmentListDS" } } }
  ]
}]
```

   `viewElementName` must name YOUR list element and `dataSourceName` YOUR data source, so renaming
   either means editing here too. Note what this resolves: the upload is a declarative request binding on
   a BUTTON, not a handler — which is why the measured page's `handlers` array is empty while the upload
   control still exists. "No handlers" never meant "no upload wiring". The captions are resource keys;
   `page-schema-resources` owns registering them.

   `FeedTabContainer`'s `tools` carries only a header label. The feed needs no toolbar buttons.

3. The `AttachmentList` collection attribute that `items: "$AttachmentList"` binds to — with FIVE child
   attributes and a sorting config, not just the single Name column the component declares:

```jsonc
"AttachmentList": {
  "isCollection": true,
  "modelConfig": {
    "path": "AttachmentListDS",
    "sortingConfig": { "default": [{ "columnName": "CreatedOn", "direction": "desc" }] }
  },
  "viewModelConfig": { "attributes": {
    "AttachmentListDS_Name":      { "modelConfig": { "path": "AttachmentListDS.Name" } },
    "AttachmentListDS_CreatedOn": { "modelConfig": { "path": "AttachmentListDS.CreatedOn" } },
    "AttachmentListDS_CreatedBy": { "modelConfig": { "path": "AttachmentListDS.CreatedBy" } },
    "AttachmentListDS_Size":      { "modelConfig": { "path": "AttachmentListDS.Size" } },
    "AttachmentListDS_Id":        { "modelConfig": { "path": "AttachmentListDS.Id" } }
  } }
}
```

   Cutting this down to `AttachmentListDS_Name` because the component declares a single Name column is
   the mistake this guide exists to prevent: `primaryColumnName` resolves through `AttachmentListDS_Id`,
   the newest-first order comes from `sortingConfig` rather than from the component, and size/author/date
   are available to the gallery tile only because they are declared here. The collection-attribute shape
   in general is owned by `related-list` — read it rather than improvising one.

4. `AttachmentListDS` itself, declared in FULL — four attributes (`Name`, `CreatedOn`, `CreatedBy`,
   `Size`) at `scope: "viewElement"`, over the file entity THIS OBJECT uses. Which entity that is
   (`SysFile` or a per-object `<Entity>File`, paired with the matching `recordColumnName`) is decided in
   `page-modification-standard-components` STEP 3 — it is not a constant, and an insert that hard-codes
   one of the two builds a gallery over the wrong table. The block itself is in that guide's STEP 4, which
   also explains why the short one-attribute form you may have seen on a real page is a MERGE overlay
   rather than the whole data source.

NOT yours on either path. Do NOT file these as insert deliverables:

- the `Id` attribute. Both measured templates declare it — including the one that ships neither component
  — as `"Id": { "modelConfig": { "path": "#PrimaryDataSourceName()#.Id" } }`. `get-component-info` for
  `crt.Feed` states the same from the other side: on edit pages the platform provides `$Id` and
  `$CardState`, and no extra declaration is needed. The measured creation-flow page does re-declare `Id`
  as `PDS.Id`, which is harmless and is not a thing to copy.
- `$CardState`. Declared by both measured templates, and platform-provided per the same catalog text.

TAGS — `crt.TagSelect`, the part you may POINT but never WIRE
Tags produce the same user-visible symptom as a broken attachments list — a control that is there and
empty — and the instinct is to fix them the same way. That instinct is wrong in every particular. Three
measured differences:

- **There is no merge-vs-insert decision.** Both measured templates ship `crt.TagSelect` — including the
  one that ships NEITHER the feed nor the attachments list — always inside `CardToolsContainer`, always
  in the same three-property form. It arrives with the record-page template:

```jsonc
{ "type": "crt.TagSelect", "recordId": "$Id", "name": "TagSelect" }
```

- **`recordId: "$Id"` is the whole contract, and you MUST keep it.** `get-component-info` for
  `crt.TagSelect` names omitting it as its first pitfall: without `recordId` the preprocessor cannot
  resolve the tag-to-record association and the component renders empty. That is the catalog's
  statement, cited rather than re-observed here.
- **You MUST NOT hand-wire `items`, `listItems` or the CRUD outputs** (`createTag`, `editTag`,
  `deleteTag`, `addTagsInRecord`, `deleteTagInRecord`). The `crt.TagSelectPropertiesPanel` DESIGNER
  PREPROCESSOR generates them, and the catalog warns that bypassing it makes every one of those outputs
  yours to handle. This is the OPPOSITE of the attachments list, where the page really does carry the
  collection attribute, the data source and the toolbar wiring — everything above this section. Do not
  carry that habit across.

So there is NO tag data source to propagate. All four measured schemas declare only `AttachmentListDS`
and `PDS`; none of them contains `listItems` or any tag data source at all.
`tagInRecordSourceSchemaName` defaults to `"TagInRecord"`, and it is the ONE tag property a page may
legitimately have to add — see the next section. It is a schema NAME, not a data source and not a
binding, so setting it does not reopen the hand-wiring prohibition above.

Two tagging models exist, and the control reads only one of them
This was recorded as an open question in an earlier revision. A migration run settled it, and the answer
changed an instruction — read this section rather than the previous one you may remember.

- THE DEFAULT MODEL. `TagInRecord` (package `CrtBase`) is entity-agnostic: it keys an association by
  `RecordId` (Guid) plus `RecordSchemaName` (text) against the `Tag` dictionary, which is itself scoped by
  an `EntitySchemaName` text column. On this path a record page needs NO per-object junction schema, and
  the absence of one is NOT evidence that the control is broken. That retraction stands unchanged:
  do NOT create an `<Entity>InTag` schema to make tagging work.
- THE OLDER PER-OBJECT MODEL. Junctions named `<Entity>InTag`, inheriting `BaseEntityInTag`, with an
  `Entity` lookup to the object and a `Tag` lookup to a per-object dictionary `<Entity>Tag` inheriting
  `BaseTag` (`ContactInTag.Tag` → `ContactTag`). The Classic section wizard creates these alongside the
  object, so a Classic-era section typically has one — 40 such junction tables on the measured stand,
  against zero for the object the Freedom creation flow produced. A migrated page inherits the object, and
  the object brings its junction with it. Check, do not assume: step 1 below is the check.

The control reads `tagInRecordSourceSchemaName`, which defaults to `TagInRecord`. So when an object
carries an `<Entity>InTag`, its tags are in a table the default path never queries.

THE RULE, and it is decidable before you look at a browser:

1. Call `find-entity-schema` with `search-pattern: "<Entity>"` on the target environment.
2. NO `<Entity>InTag` — leave the control exactly as the template shipped it. The default path is correct
   and there is nothing to add.
3. AN `<Entity>InTag` EXISTS — point the control at it. This is the one case the catalog means by
   "Override only when your module uses a custom junction schema", and its own checklist says
   `tagInRecordSourceSchemaName` must match the actual junction entity:

```jsonc
{ "type": "crt.TagSelect", "recordId": "$Id", "name": "TagSelect",
  "tagInRecordSourceSchemaName": "UsrToMigrateInTag" }
```

Without it the chips come up empty on a record that HAS tags, which is indistinguishable at a glance from
a record that has none — and `update-page` reports success either way. Migration builds nothing for the
tag control, so nobody is prompted to notice.

What was MEASURED, on a Classic section migrated to Freedom UI: the object carries both
`UsrToMigrateInTag` (inheriting `BaseEntityInTag`) and `UsrToMigrateTag` (inheriting `BaseTag`); the
junction holds 3 rows, all three for the record under test, naming the tags `duper taf`, `dsgsg` and
`sgsg`; `TagInRecord` holds 0 rows in the entire table, as does the `Tag` dictionary; and the migrated
page's control is the bare template-supplied form with no `tagInRecordSourceSchemaName`, so it resolves to
`TagInRecord`. The data exists, the control is on the page, and the table it reads is empty.

NOT OBSERVED, and do not report it as fixed until you have looked: that adding the override repopulates
the chips. The cause above is measured; the remedy is the catalog's prescription applied to it, and the
last step is still a reload of the record page in a browser.

Verifying an INSERT
`update-page` returning `success: true` proves nothing here — every omission in this guide saves cleanly,
because `update-page` validates the diff you send rather than the merged result. Reload the record page
and confirm in the browser: the attachments tab shows its gallery WITH the upload and refresh buttons, an
upload actually lands, the newest file sorts first, and the tag control shows the tags the record
ALREADY has rather than an empty strip. A list showing a `crt-data-grid-placeholder` has not loaded YET — `related-list` owns the rule for telling
that apart from a failure, and you MUST apply it before reporting anything as broken.

Evidence
Two lab scenarios on internal Creatio Studio stands, read-only via `get-page`, `get-component-info`,
`find-entity-schema`, `get-entity-schema-properties` and read-only SQL. The first (2026-09-11, tags
2026-09-14) read three schemas through the MERGED bundle rather than the page body. The second
(2026-09-16) read a REAL MIGRATION RUN, and it is what turned the tag question from an open one into an
instruction.

1. `PageWithTabsFreedomTemplate` — the template that SHIPS both components. Everything in the inventory
   above marked template-supplied is read from here: both tab containers, the `AttachmentsTabContainer`
   toolbar with `AttachmentAddButton` and `AttachmentRefreshButton`, the `AttachmentList` collection
   attribute with its five child attributes and `sortingConfig`, the four-attribute `AttachmentListDS`,
   `Id`, `CardState` and `crt.TagSelect`. Its own `handlers` array is empty.
2. `PageWithTopAreaAndTabsFreedomTemplate` — a template that ships NEITHER component, read so the INSERT
   column is measured rather than inferred: no `FeedTabContainer`, no `AttachmentsTabContainer`, no
   `AttachmentList` attribute, no data sources at all — but `Id`, `CardState` and `crt.TagSelect` ARE
   declared, which is what puts those three on the not-yours rows.
3. `UsrSourceCodes_FormPage` (package `UsrSourceCodes`), produced by the section/application creation
   flow on top of `PageWithTabsFreedomTemplate` — the MERGE-path reference. Its `viewModelConfig` declares
   only `UsrName` and `Id`, and its `handlers` array is empty, which is what establishes that the toolbar,
   the collection attribute and `CardState` come from the template rather than from the page.

4. `UsrToMigrateFreedom_FormPage` (package `UsrToMigrateApp`, parent template
   `PageWithTabsFreedomTemplate`) — a Classic section migrated to Freedom UI on a second stand, read
   2026-09-16 on record `2c1ab91e-0895-4ec1-b394-d4edab9d87fa`. Its own body carries five `insert`
   operations and `merge` for `AttachmentList` and `Feed`; its `handlers` array is empty; it declares no
   tag anything. It is the fourth schema the tag rules are measured on, and the first on which the older
   tagging model held data.

For tags specifically: `crt.TagSelect` is present in ALL FOUR, always inside `CardToolsContainer` and
always in the three-property form quoted above; none of the four declares a tag data source, `listItems`
or `tagInRecordSourceSchemaName`. On the first stand: `TagInRecord` (package `CrtBase` — `RecordId`,
`RecordSchemaName`, `Tag`, `TagRecordId`), `Tag` (carrying `EntitySchemaName`), about thirty
`<Entity>InTag` schemas inheriting `BaseEntityInTag`, `UsrSourceCodes` with no `UsrSourceCodesInTag`, and
`TagInRecord` and `ContactInTag` both empty — which is why that scenario could settle nothing.

On the migration stand, measured 2026-09-16, it settles. `UsrToMigrate` has `UsrToMigrateInTag`
(`parent-schema-name: "BaseEntityInTag"`, ZERO own columns — `Entity` → `UsrToMigrate` and `Tag` →
`UsrToMigrateTag` are both inherited and re-pointed) and `UsrToMigrateTag`
(`parent-schema-name: "BaseTag"`). Row counts, by SQL: `UsrToMigrateInTag` 3, all three with
`EntityId = 2c1ab91e-0895-4ec1-b394-d4edab9d87fa`, joining to tags named `duper taf`, `dsgsg`, `sgsg`;
`UsrToMigrateTag` 3; `TagInRecord` 0 across the whole table; `Tag` 0. 40 `<Entity>InTag` tables exist
there. The catalog was re-read against that environment the same day and still says
`tagInRecordSourceSchemaName` defaults to `TagInRecord` and is to be overridden for a custom junction
schema, with a checklist item requiring it to match the actual junction entity.

NOT observed, and marked as such where it appears: that setting `tagInRecordSourceSchemaName` to the
per-object junction makes the chips render. The CAUSE is measured — data in one table, control reading
another — and the remedy is the catalog's own instruction applied to that cause, but no page was written
and no browser was opened, so the last step remains yours. The catalog statements about `recordId` and
about the preprocessor are likewise cited from `get-component-info`, not re-derived here.
`get-component-info` reported `resolvedFrom: "environment-superset"` with
`resolvedTargetVersion: "latest"` on both stands, so the catalog was NOT version-pinned to either. No page
was written by either scenario — every call was read-only. The migration of scenario 4 was performed by
someone else and is read here as evidence, not produced here.

Applicability: Freedom UI web FORM pages (`schema-type: "web"`). Mobile pages draw from a separate catalog
and map these components under different names — read `mobile-page-modification` first. What a template
supplies is a property OF THAT TEMPLATE: the inventory above is measured on two of them and is re-checked
per template with `get-page`, never assumed.

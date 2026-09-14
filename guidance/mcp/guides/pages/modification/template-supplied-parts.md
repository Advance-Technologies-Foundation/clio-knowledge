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
   `Size`) over `SysFile`, at `scope: "viewElement"`. The block is in
   `page-modification-standard-components` STEP 4, which also explains why the short one-attribute form
   you may have seen on a real page is a MERGE overlay rather than the whole data source.

NOT yours on either path. Do NOT file these as insert deliverables:

- the `Id` attribute. Both measured templates declare it — including the one that ships neither component
  — as `"Id": { "modelConfig": { "path": "#PrimaryDataSourceName()#.Id" } }`. `get-component-info` for
  `crt.Feed` states the same from the other side: on edit pages the platform provides `$Id` and
  `$CardState`, and no extra declaration is needed. The measured creation-flow page does re-declare `Id`
  as `PDS.Id`, which is harmless and is not a thing to copy.
- `$CardState`. Declared by both measured templates, and platform-provided per the same catalog text.

TAGS — `crt.TagSelect`, the part that is never yours to wire
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

So there is NO tag data source to propagate. All three measured schemas declare only `AttachmentListDS`
and `PDS`; none of them contains `listItems`, `tagInRecordSourceSchemaName`, or any tag data source at
all. Only `recordId` reaches the schema. `tagInRecordSourceSchemaName` defaults to `"TagInRecord"` —
override it only for a custom junction schema.

Where a dead tag control actually comes from, and what this guide does NOT claim
`TagInRecord`, the default, is entity-agnostic: it keys an association by `RecordId` (Guid) plus
`RecordSchemaName` (text) against the `Tag` dictionary, which is itself scoped by an `EntitySchemaName`
text column. On that path a record page needs NO per-object junction schema, and the absence of one is
NOT evidence that the control is broken.

A second, older model exists beside it: per-object junctions named `<Entity>InTag`, inheriting
`BaseEntityInTag` and pointing at a per-object tag dictionary (`ContactInTag.Tag` → `ContactTag`). Around
thirty of them exist on the measured stand.

UNVERIFIED, and deliberately not made into an instruction: whether tags recorded under the older
per-object model are reachable through the component's default `TagInRecord` path. If a migrated page
shows an empty tag control, that question — not the page body — is where to look, and it needs its own
investigation. This guide does NOT tell you to create an `<Entity>InTag` schema: the default path does
not read one, and whether creating one is a migration step or a platform concern was not established.

Verifying an INSERT
`update-page` returning `success: true` proves nothing here — every omission in this guide saves cleanly,
because `update-page` validates the diff you send rather than the merged result. Reload the record page
and confirm in the browser: the attachments tab shows its gallery WITH the upload and refresh buttons, an
upload actually lands, the newest file sorts first, and the tag control is where the template put it. A
list showing a `crt-data-grid-placeholder` has not loaded YET — `related-list` owns the rule for telling
that apart from a failure, and you MUST apply it before reporting anything as broken.

Evidence
Lab scenario, 2026-09-11 and 2026-09-14, on an internal Creatio Studio stand, read-only via `get-page`,
`get-component-info`, `find-entity-schema` and `get-entity-schema-properties`. Three schemas were read,
each through the MERGED bundle rather than the page body.

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

For tags specifically: `crt.TagSelect` is present in ALL three, always inside `CardToolsContainer` and
always in the three-property form quoted above; none of the three declares a tag data source, `listItems`
or `tagInRecordSourceSchemaName`. The junction schemas were read on the same stand: `TagInRecord`
(package `CrtBase` — `RecordId`, `RecordSchemaName`, `Tag`, `TagRecordId`), `Tag` (carrying
`EntitySchemaName`), and about thirty `<Entity>InTag` schemas inheriting `BaseEntityInTag`.
`UsrSourceCodes` has no `UsrSourceCodesInTag`. Runtime settles nothing in either direction:
`TagInRecord` and `ContactInTag` both hold zero rows on that stand, so no tagging behaviour was observed.

NOT observed, and marked as such where it appears: whether tags held under the older per-object model
surface through the default `TagInRecord` path. The catalog statements about `recordId` and about the
preprocessor are cited from `get-component-info`, not re-derived here. `get-component-info` reported
`resolvedFrom: "environment-superset"` with `resolvedTargetVersion: "latest"`, so the catalog was NOT
version-pinned to that stand. No page was written and no migration was run for this guide — every call
was read-only.

Applicability: Freedom UI web FORM pages (`schema-type: "web"`). Mobile pages draw from a separate catalog
and map these components under different names — read `mobile-page-modification` first. What a template
supplies is a property OF THAT TEMPLATE: the inventory above is measured on two of them and is re-checked
per template with `get-page`, never assumed.

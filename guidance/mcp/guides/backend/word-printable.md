clio MCP word-printable guide — build, change, show and ship an MS Word printable

Scope
This guide owns building and changing an MS Word printable: its `SysModuleReport` row, its `SysModuleReportTable`
rows (table parts), its .docx template, where it is offered, and how it reaches another environment in a package.
- The template itself (merge fields, names, detail tables, images, PDF, out-of-the-box tokens, a custom
  `IExpressionConverter`): `get-guidance name=word-printable-template`.
- A printable that fails, prints blank or wrong, or ignores PDF: `get-guidance name=word-printable-troubleshooting`.
- Generating a printable from a business process (the Process file element): `get-guidance name=process-files-report`.
- A page button that prints: the `crt.PrintablesRequest` parameters come from `get-request-info`, and `templateId`
  only from `list-printables` (`get-guidance name=when-to-use-requests`). This guide does not restate them.
- FastReport (`SysModuleReport.Type` `8def0f6d-…`) and DevExpress (`32f72c9c-…`) are different engines. Stop and say
  so; nothing here applies to them.

Applicability: read in the configuration source of Creatio 10.2.444 and 10.3.1152 (the generator, design service
and report service are identical in both) and measured on a 10.3.1152 .NET Framework / MSSQL environment. Creatio 8.x
behaviour is not verified; .NET 8 is expected to match (same configuration code) but was not measured.

== What a printable is ==
One `SysModuleReport` row (Type = MS Word, `8bc259ef-4276-4906-b7a6-23dc59be7fe2`), zero or more
`SysModuleReportTable` rows, and the .docx in `SysModuleReport.File`. The generator ships with the product; a project
owns the rows, the template and any custom converter.

| `SysModuleReport` column | Holds |
|---|---|
| `Caption` | the name users see and the generated file name (localizable) |
| `Type` | the engine; Word is the Id above |
| `SysEntitySchema` | the object the printable reads (the entity schema UId, portable across environments) |
| `SysModule` | a section; generation, Freedom UI and process generation use it only when `SysEntitySchema` is empty. The designer overwrites it from the object on every save (measured) |
| `File` | the .docx template, the only one the generator reads |
| `MacrosSettings` | the configured columns: an array of `{caption, metaPath, metaPathCaption, …}`, where `metaPath` is the column UId, or `<lookupColumnUId>.<targetColumnUId>` for a lookup path. The designer writes it with unquoted keys; strict JSON is read the same way (measured) |
| `MacrosList` | legacy column list, read only when feature `Use7xFiltersForWordReports` (shipped on) is off or `MacrosSettings` is empty |
| `ShowInCard` / `ShowInSection` | offered on a record page / on a list |
| `ConvertInPDF` | the default PDF flag (PDF in `get-guidance name=word-printable-template`) |
| `TypeColumnValue` | Classic UI only: offered only for records of this type |

`SysModuleReportTable`, one row per table part: `Caption` (what the template writes before the dot, no `.` in it),
`ObjectId` (the detail object's UId), `ReferenceColumn` / `ObjectColumnPath` (the link back to the record),
`MacrosSettings`, `FilterSettings`, `IsEmptyTableHide`.

The template binds to the column **captions** in `MacrosSettings`, exactly and case-sensitively. A caption is a
contract between the row and the .docx: renaming one breaks every field that spells it.

== 1. Read what exists ==
Read before you change anything, and keep what you read as the baseline and the rollback.
1. `list-printables` with `entity-name` (through `clio-run`): `templateId`, `printableCaption`, `convertInPDF`,
   `showInCard`, `showInSection`.
2. The rows: `odata-read` on `SysModuleReport` (by `Id`) and on `SysModuleReportTable` (filter `SysModuleReport`).
   Record the object, every column caption, every table part caption and its object.
3. The field list the template may use: `clio call-service -e <env> --service-path
   rest/WordReportingDesignService/GetWordReportSettings -b '{"reportId":"<templateId>"}'` (CLI) returns the object
   and table parts with their `MacrosCollection` (captions and paths). An unknown id answers HTTP 404 with an empty
   body, which `clio call-service` shows as no output at all with exit code 0 (clio 8.1.0.130, measured): empty output
   means a wrong id, so take it from `list-printables`.
4. The template: `GET /0/rest/WordReportingDesignService/DownloadReportTemplate/<templateId>` returns `File` as
   `<Caption>.docx`. MUST NOT download it with `clio call-service`: it decodes the body as text and the saved file is
   not a valid .docx (measured). Use an HTTP client from a shell (log in through `ServiceModel/AuthService.svc/Login`,
   replay the `BPMCSRF` cookie as a header), or ask the user to click Download template in the designer. Read the
   login from the registered clio environment settings, never from the conversation, and never print or store the
   password, the session cookies or `BPMCSRF`: run the login from a script that reads the settings at run time,
   never as a command line that carries the password.
5. Whether a package already ships the row: `execute-esq` on `SysPackageSchemaData` filtered by `Name` starting with
   `SysModuleReport` (product printables ship as `SysModuleReport_<Name>` / `SysModuleReportTable_<Name>`).

== 2. Configure the rows in the designer ==
- The designer is System Designer -> Report setup, which opens the Printables page (`/0/ClientApp/#/Printables`; a
  printable opens at `#/Printables/<typeId>/<templateId>`). It sets the name, object and type, the
  columns (Set up report data), the table parts (Set up report tables), the two Show flags, and uploads or downloads
  the template. Once columns or table parts exist, the object is locked: clear them to change it.
- MUST configure columns and table parts in the designer whenever a user can open it. MUST NOT hand-edit
  `MacrosSettings` for a printable that already works: every entry carries column UIds, and one stale UId fails the
  report as soon as the template uses that column (`ItemNotFoundException` naming the UId; measured).
- A column's caption is what the template spells. When the user picks columns, tell them which captions the
  template expects, or read the captions back afterwards (step 1.3) before you write fields.
- Rights: the operation `CanManageReports` only shows the Report setup entry. Saving the row and uploading the
  template are entity writes on `SysModuleReport` / `SysModuleReportTable`, and the product grants only read on them
  to All employees (read in source; authoring as a non-administrator was not measured). If a user with
  `CanManageReports` cannot save, the missing piece is edit rights on those objects: tell the user, and grant them
  only after the owner's explicit yes. Both objects are administered by operations (`administratedByOperations`,
  measured on 10.3.1152), so those rights come from system operations: `get-guidance name=entity-operation-access`.

Without a designer (shell only, measured on 10.3.1152): when no user can open the designer and you have an HTTP
client, a printable can be created and filled by API. Use it only then, and say that you did.
1. Column UIds: `clio call-service -e <env> --service-path DataService/json/SyncReply/RuntimeEntitySchemaRequest -b
   '{"Name":"<Object>"}'` (CLI) -> `schema.columns.Items[*].uId` (clio 8.1.0.130, measured). clio has no MCP tool that
   returns column UIds. The upload, the download and the generation below still need your own HTTP client (binary
   bodies).
2. The row: `odata-create` on `SysModuleReport` with `Caption`, `TypeId` (Word), `SysEntitySchemaId` (the object's
   UId), `ShowInCard` / `ShowInSection`, and `MacrosSettings` with one entry per column:
   `{"caption":"<Caption>","metaPath":"<colUId>[.<colUId>]","metaPathCaption":"<Caption>","aggregationType":"None",
   "dataValueType":"Text"}`. `caption` and `metaPathCaption` are both required.
3. The template: `POST /0/rest/WordReportingDesignService/UploadReportTemplate` with the raw .docx as the body,
   headers `BPMCSRF`, `Content-Type: application/octet-stream` and `Content-Range: bytes 0-<length-1>/<length>`, and
   the query `reportId=<templateId>`, `fileId=<new GUID>`, `entitySchemaName=SysReportTemplate`, `columnName=File`,
   `parentColumnName=Id`, `parentColumnValue=<the same fileId>`, `fileName`, `totalFileLength`,
   `additionalParams={"ReportId":"<templateId>"}`. Success is HTTP 201 with `{"success":true}`. Without
   `Content-Range` it answers HTTP 500 with `errorInfo.errorCode` 405. Errors come back as HTTP 500 with the reason in
   `errorInfo.errorCode` (404 = row missing or not found as MS Word), so read the body, not only the status. The upload
   also deletes the temporary upload rows of other users from the last day: do not run it while someone else uploads.
   The size limit is system setting `FileImportMaxFileSize` (MB).
4. Read it back: download the template and compare the bytes, then generate once (Prove it, below).
The designer opens such a row unchanged; saving it there rewrites `MacrosSettings` in its own format and sets
`SysModule` from the object, and the report still generates (measured).

== 3. Author the template ==
Read `get-guidance name=word-printable-template` before you write or edit a single field. The rules that fail
silently: a field without a switch prints blank; binding is exact and case-sensitive; no space before `[#`;
`[#…#]` template tokens are not process-formula macros.

== 4. Offer it to users ==
- Freedom UI: a button wired to `crt.PrintablesRequest`. In menu mode it lists every Word printable of the data
  source's object, `ShowInCard` ones for a record data source and `ShowInSection` ones for a list. The list is cached
  for the browser session: reload the page after adding or changing a printable. `TypeColumnValue` is ignored.
- Classic UI: the card and section print menus list Word printables found by the type **name** `MS Word`, narrowed
  by `TypeColumnValue`, and cached per object in the browser.
- Product printables (Invoice, Contract, Quotation) ship with both Show flags off, so "it is not offered" can be the
  shipped state rather than a defect.
- Every generation reads with the generating user's rights, so a user without read on `SysModuleReport` sees no
  printables, and fields come out blank for columns that user cannot read.

== 5. Ship it in a package ==
The designer and the upload write the environment only; nothing is added to a package. Ship a printable as data
bindings, the way the product ships its own (`SysModuleReport_Invoice` and others).
- clio cannot generate this binding today. `create-data-binding-db` with a full `SysModuleReport` row binds only
  part of it: on clio 8.1.0.130 `Type` and `SysEntitySchema` were left out of the binding and no column was marked
  force-update (measured). Such a package installs a row
  without its type and object, so it is not offered as a Word printable. The local `create-data-binding` has no
  binary type and writes every column with `IsForceUpdate: false`.
- So the binding is made in the Configuration section: open the package, Add -> Data, object `SysModuleReport`, bind
  the printable's row with these columns: `Id` (key), `Caption`, `Type`, `SysEntitySchema` (or `SysModule` when that
  is the source), `ShowInCard`, `ShowInSection`, `ConvertInPDF`, `MacrosSettings`, `File`, plus `TypeColumnValue` if
  set. Mark `File` and `MacrosSettings` as force-update when later template edits must reach environments that
  already have the row. Bind the table parts the same way in a second binding on `SysModuleReportTable` (all columns
  of its rows, with the columns you will change marked force-update). If you cannot drive that UI, give the user this
  column list and ask them to create it.
- What the install does (measured with `push-pkg` on 10.3.1152):
  - a row that does not exist is inserted with every bound column, the template included (it travels as base64 in
    `data.json`; `Caption` travels in the binding's `Localization` files);
  - a row that exists takes ONLY the force-update columns: without force, a changed template and changed columns
    never reach it; with `File` and `MacrosSettings` forced, those two are replaced and `Caption` / Show flags edited
    on the target are kept;
  - an archive whose binding has the same `ModifiedOnUtc` as the installed one applies nothing, forced or not;
  - on an environment where that binding was edited locally, the install skips it with the warning `Unable to install
    SchemaData "<name>" … the element has been modified locally` and reports success.
  The general rules (force update, `ModifiedOnUtc`, apply routes, `pkg-to-db`) are owned by
  `get-guidance name=data-bindings`; read it before changing the binding.
- `InstallType` of the binding: 0 (Install) is what you want. `FirstInstall` (1) does nothing once the package exists
  on the target, and `InstallIfNotExists` never updates an existing row (read in source; one product printable
  binding uses `FirstInstall`).
- The template lives twice: on the environment and in the binding. The package export carries the binding's copy as
  of its last save, not the current `File` (measured). After every template upload, open the binding in the
  Configuration section and save it again (this refreshes the row values and advances `ModifiedOnUtc`), or the next
  forced install puts the old template back.
- `MacrosSettings` holds column UIds. A column recreated with a new UId on the target breaks the printable there.

== 6. Prove it ==
- Generate it for a real record and open the file: every field filled, one table row per detail record, images in
  place, PDF when PDF was asked for. By API: `POST /0/rest/ReportService/CreateReport` with `templateId` and
  `recordId` (other string members empty) returns a key; `GET /0/rest/ReportService/GetReportFile/<key>` returns the
  file once (the key lives in the user session and is removed on first read). Without an HTTP client, ask the user to
  print it.
- Generate it as a user who will print, not only as an administrator: the generator reads with that user's rights.
  How to do that without that user's password is owned by `word-printable-troubleshooting` (Reproduce first).
- On the target after delivery, download the template (or read `SysModuleReport.ModifiedOn` and the downloaded size)
  before calling the new template delivered.

== Hazards ==
- The type name `MS Word`: Classic print menus, the template design service and process generation find Word
  printables by the lookup's **name**, not its Id. Do not rename it. The product ships translations that do not match
  in some cultures (ar-SA, he-IL, hu-HU, zh-TW); for users in those cultures those paths may find nothing (read in
  source and data; not measured).
- A printable has no record-level history: before overwriting a template or `MacrosSettings`, keep the downloaded
  template and the row you read in step 1.

clio MCP process-dynamic-case guide - a minimal dynamic case (DCM) from a lookup column, plus the record-page stage progress bar

Scope
This guide owns `create-dynamic-case`: it gives an object a stage lifecycle. One call builds a dynamic case (DCM)
whose stages are the values of one lookup column of the object (for example `Status`), in a stated order, and adds
the stage progress bar (`crt.EntityStageProgressBar`) to the object's Freedom UI record page. Use it when the user
asks for "stages", "a status lifecycle", "a progress bar on the record page" or "a case". "When X happens, do Y" is a
business process: `process-digest`. A case is not the `Case` object.

== Before the call ==
- The object and the lookup column are PUBLISHED (a column added in this session is published first, or the call is
  refused `column-not-found`), the lookup has 2 to 20 values the caller can see, and the target package is editable.
- The case's package must reach the packages of the object, the column and the lookup, and CrtCoreBase / CrtBase
  (the case settings tables). The tool adds the missing ones itself (`dependencies.added`); with
  `add-package-dependencies: false` it refuses `dependencies-missing` and names them.
- The call runs as a General user with `CanManageDcm`, `CanManageSolution` and the right to add `SysDcmSettings` and
  `SysDcmSchemaInSettings` records (by default only System administrators); it is refused `rights-missing` naming
  the missing one, before anything is read. `CanManageProcessDesign` is not needed.

== The call ==
`create-dynamic-case { environment-name, package-name, entity-schema-name, stage-column-name, stages?,
order-column?, name?, caption?, add-progress-bar = true, page-schema-name?, add-package-dependencies = true,
dry-run = false }`; it writes, so it is destructive. Version boundary: `create-dynamic-case` needs clio <CLIO-TBD>
or later; on an older clio, stop here and tell the user to update clio - do not build a case by other means.
- ORDER. Nothing on the platform orders a lookup's values, and the bar shows the stages in the stored order, so
  pass `stages` (every value exactly once, by Id or exact display value) whenever the business order matters.
  Without it: `order-column` (an integer column of the lookup) > `Position` > `StageOrder` (then
  `StageInnerOrder`) > `Number` > `StageNumber` > display value. Display order turns "New, In progress, Done" into
  "Done, In progress, New". The result ALWAYS states `case.orderSource` and the ordered captions: read them back to
  the user. `dry-run: true` runs every check and returns the whole plan with nothing written: use it to confirm the
  order first.
- A subset, a value named twice and an unknown or ambiguous value are refused (`stage-list-incomplete`,
  `stage-list-duplicate`, `stage-value-unknown`, `stage-value-ambiguous`): a record holding a value that is not a
  stage shows no current stage.

== What one call writes ==
- The case schema in the package: one EMPTY stage per value (`Stage1..N`, caption = the display value, colour
  `#0C8701`), every stage connected to every other (so every stage is clickable on the bar), enabled, the active
  version. Its link row (`SysDcmSchemaInSettings`), bound to the package, and its settings row (`SysDcmSettings`):
  created and bound, or reused when the object already has one with the same column and no filter column. A reused
  row no package carries yet is bound to this package; one another package carries stays there, and the note names
  that package to deploy with this one: relay it. The case designer needs both rows to open the case;
  `settings.bindings` names the bindings this call wrote.
- The record page: a `ProgressBarContainer` at index 0 of `MainContainer` holding `crt.EntityStageProgressBar` with
  the object's `entityName`, in the default design package (as `update-page` writes). Never add `recordId`,
  `value`, `stages` or handlers to it: the runtime wires them on every open. `page.status = already-present` when
  the page has a bar already.
- NO compile and NO restart, on .NET Framework and on .NET 8 (measured 2026-11-11 on Creatio 10.2.1102
  .NET Framework MSSQL and 10.2.1081 .NET 8 PostgreSQL): a case is interpreted. `obligations.compileRequired` and `restartRequired` are false; do not run `compile-creatio` or
  `restart` after it. Tell the user to reload (F5) any open page of the object (`reloadOpenPages`).

== Tell the user (relay the notes) ==
- Existing records get a case on their next change of the stage column; new records start one on insert. A record
  whose stage column is empty, or holds a value that is not a stage, shows no current stage. Offer (do not do it
  unasked) to set the column's default to the first stage with the entity tools.
- The lookup values are referenced by Id and NOT bound to the package: another environment needs the same value
  Ids. A value added to the lookup LATER has no stage until the case is changed in the case designer.
- A business process that also sets the stage column fights the case; the Academy recommends disabling it.
- The case opens in the designer at `case.designerUrl`.

== Refusals that need the user ==
- `case-exists`: the object already has an enabled case with other stages or another column; the platform starts
  every enabled case, so a second one is never created, and changing a case is not supported. A re-run with the
  SAME object, column and order answers `already-present` and writes nothing to the case (it still adds the bar
  when the page has none), so a failed call is safe to repeat.
- `settings-conflict`: the object's case settings use another stage column or a filter column, or there are
  several rows.
- `record-page-ambiguous` (a typed page set such as Activity: pass `page-schema-name`), `record-page-not-freedom`
  (a Classic page: create a Freedom UI page and its related-page add-on first), `placement-unsupported` (a mini
  page), `page-entity-mismatch` (the page's primary data source `PDS` is not the object: a page made by
  `create-page` from a template has none until a field of the object is added). All page refusals come BEFORE
  anything is written.
- `created-page-failed`: the case WAS created and kept; `page.retry` carries the exact `update-page` call.
- `failed-rolled-back` / `failed-leftover`: the build failed after writing; the second lists what to remove by hand.
- `process-builder-without-build-case`: the environment's CrtProcessBuilder predates this tool;
  `install-process-builder`, once the user approves it (`core-rules`), is the remedy.

== Undo (sequential, in this order) ==
Undo ONLY a call whose `case.status` is `created`; after `already-present` this call wrote no case, so never remove
the case or its link row. Each step deletes live data and none asks for confirmation: it MUST have the user's explicit
agreement for the named environment and rows first (`core-rules`). Take every name from the result, never compose
one from an Id.
1. `remove-data-binding-row-db` on the `SysDcmSchemaInSettings` entry of `settings.bindings`, key
   `settings.sysDcmSchemaInSettingsId`.
2. `remove-data-binding-row-db` on the `SysDcmSettings` entry of `settings.bindings`, key `settings.sysDcmSettingsId`,
   ONLY when the call answered `settings.status: created`: a reused row carries other cases' link rows, which
   deleting it would cascade away.
3. `delete-schema` on `case.schemaName`.
4. ONLY when `page.status` is `inserted`: remove the two inserted items (the container and the
   `crt.EntityStageProgressBar` in it) with `update-page`, by the exact names in `page.fragment`; never delete the
   page schema. After `already-present` the bar was there before the call.

== Not supported yet ==
Steps inside stages (tasks, approvals, pages, sub-processes), stage conditions and permissions, alternative or
unsuccessful stages, transition rules, start conditions, several cases per object, and changing, disabling or
deleting an existing case: the case designer does those. No mobile page, mini page or Classic page bar.

Troubleshooting: no bar after F5 - the object has no enabled active-version case, or the feature
`DisableEntityStageProgressBarPreprocessing` is ON on the environment (the bar is then not wired). Academy: Case
Designer (8.x), Set up case stages, Execute a case.

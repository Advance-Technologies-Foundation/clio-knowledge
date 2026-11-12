clio MCP process-dynamic-case guide - a minimal dynamic case (DCM) from a lookup column, plus the record-page stage progress bar

Scope
This guide owns `create-dynamic-case`: it gives an object a stage lifecycle. One call builds a dynamic case (DCM)
whose stages are the values of one lookup column of the object (for example `Status`), in a stated order, and adds
the stage progress bar (`crt.EntityStageProgressBar`) to the object's Freedom UI record page. Use it when the user
asks for "stages", "a status lifecycle", "a progress bar on the record page" or "a case". "When X happens, do Y" is a
business process: `process-digest`. A case is not the `Case` object.

== Before the call ==
- The object and the lookup column are PUBLISHED: a column saved and not published is refused `column-not-published`
  (publish the object, then retry), one that does not exist `column-not-found`. The lookup has 2 to 20 values the
  caller can see, and the target package is editable.
- The case's package must reach the packages of the object, the column and the lookup, and CrtCoreBase / CrtBase
  (the case settings tables). The tool adds the missing ones itself (`dependencies.added`); with
  `add-package-dependencies: false` it refuses `dependencies-missing` and reports them in `dependencies.missing`.
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
  the page has a bar already. The bar is inserted in AUTO-SAVE mode (`saveOnChange: true`,
  `askUserToChangeSchema: false`): a stage click saves the whole record, with any other unsaved edit on the form,
  and a failed save reverts the stage. Tell the user; the manual mode (the click only sets the field, the user
  saves) is the "Save on change" toggle in the bar's properties in the page designer.
- NO compile and NO restart, on .NET Framework and on .NET 8 (measured 2026-11-11 on Creatio 10.2.1102
  .NET Framework MSSQL and 10.2.1081 .NET 8 PostgreSQL): a case is interpreted. `obligations.compileRequired` and
  `restartRequired` are false; do not run `compile-creatio` or `restart` after it. One exception: with
  `dependencies.compilationRequired: true` the dependency save asked for a compilation, `obligations` lists
  `package-dependencies`, and a configuration compile (`compile-creatio` without `package-name`) is due. Tell the
  user to reload (F5) any open page of the object (`reloadOpenPages`). Before your next `update-page` of a page
  this call wrote, `get-page` it again.

== Tell the user (relay the notes) ==
- Existing records get a case on their next change of the stage column; new records start one on insert. A record
  whose stage column is empty, or holds a value that is not a stage, shows no current stage. Offer (do not do it
  unasked) to set the column's default to the first stage with the entity tools.
- The lookup values are referenced by Id and NOT bound to the package: another environment needs the same value
  Ids. A value added to the lookup LATER has no stage until the case is changed in the case designer.
- A business process that also sets the stage column fights the case; the Academy recommends disabling it.
- The case opens in the designer at `case.designerUrl`.

== Refusals that need the user ==
With `outcome: refused` nothing was written, unless the item says otherwise. The same `errorCode` can come with
another outcome (`request-failed` with `unknown`, `page-append-rejected` with `created-page-failed`): then read
"Partial and unknown outcomes".
- `case-exists`: the object already has an enabled case that is not this call's equivalent: other stages, another
  column, or one without its settings row or link row. The platform starts every enabled case, so a second one is
  never created, and changing a case is not supported. A re-run with the SAME object, column and order answers
  `already-present` and writes nothing to the case (it still adds the bar when the page has none), so a failed
  call is safe to repeat.
- `settings-conflict`: the object's case settings use another stage column or a filter column, or there are
  several rows. When another call created a row at the same moment this call removes its own: retry.
- Page refusals, all BEFORE anything is written: `record-page-not-found` (no record page could be found: pass
  `page-schema-name`), `record-page-ambiguous` (a typed page set such as Activity: pass `page-schema-name`),
  `record-page-not-freedom` (a Classic page: create a Freedom UI page and its related-page add-on first),
  `placement-unsupported` (a mini page), `page-entity-mismatch` (the page's primary data source `PDS` is not the
  object: a page made by `create-page` from a template has none until a field of the object is added),
  `page-append-rejected` (the page refused the bar's append dry run; the message says why).
- `dependency-cycle` (the package storage refused the added dependencies) and `dependency-add-failed`: settle
  the package's dependencies with the user, or pass a package that already reaches the object.
- `request-failed`: BuildCase could not be called, or answered something that is not its result (an error or a
  login page); read the message and retry once the environment answers.
- `process-builder-without-build-case`: BuildCase answered 404, so the environment's CrtProcessBuilder has no
  dynamic-case operation (it needs 1.6.6.165 or later); `install-process-builder`, once the user approves it
  (`core-rules`), is the remedy.

== Partial and unknown outcomes ==
- `unknown`: the reply to the writing pass was lost, so the case may or may not exist. Re-run the call with
  `dry-run: true` before anything else (it writes nothing), and KEEP this result's `dependencies.added`: no later
  result reports them. `case.status: already-present` means the case was written, but that answer names no link
  row or binding, so undo steps 1-3 cannot be driven from it: tell the user it can be removed in the case
  designer. `planned` means it was not: with the user's agreement run the real call, or remove those dependencies
  (step 5). Never undo from the `unknown` result itself.
- `created-page-failed`: the case is in place (`case.status` `created` or `already-present`) and kept; the bar is
  not. `page.retry` carries the exact `update-page` call.
- `failed-rolled-back` / `failed-leftover`: the build failed after writing; the second lists what to remove by
  hand, in order (the case schema with the undo step 1 call, `remote: true` included), and keeps the dependencies
  this call added (`dependencies.status: kept-for-leftovers`): remove them only after the leftovers. Either way the platform's own save may have left a start-event row
  (`SysEntityPrcStartEvent`); the message says so.

== Undo (sequential, in this order; stop at the first failure) ==
Steps 1-3 ONLY for a call whose `case.status` is `created`; after `already-present` this call wrote no case, so
never remove the case or its link row. Steps 4 and 5 follow `page.status` and `dependencies.status` alone. Each
step deletes live data and none asks for confirmation: it MUST have the user's explicit agreement for the named
environment and rows first (`core-rules`). Take every name from the result, never compose one from an Id.
1. `delete-schema { schema-name: case.schemaName, environment-name, remote: true }` (without `remote` it acts
   on a local workspace). The case goes first, as the server's own rollback does: an enabled case without its
   settings rows keeps starting and cannot be opened in the designer.
2. `remove-data-binding-row-db` on the `SysDcmSchemaInSettings` entry of `settings.bindings`, package-name
   `case.packageName`, key `settings.sysDcmSchemaInSettingsId`.
3. `remove-data-binding-row-db` on the `SysDcmSettings` entry of `settings.bindings`, package-name
   `case.packageName`, key `settings.sysDcmSettingsId`, ONLY when the call answered `settings.status: created`: a reused row carries other cases' link rows, which
   deleting it would cascade away. A reused row this call bound stays bound (no tool removes only the binding):
   tell the user the package now ships it.
4. ONLY when `page.status` is `inserted`: remove the two inserted items (the container and the
   `crt.EntityStageProgressBar` in it, by the exact names in `page.fragment`): `get-page`, then `update-page` with
   `mode: replace` and the body without them (append cannot remove). Never delete the page schema. After
   `already-present` the bar was there before the call.
5. `remove-package-dependency` for each of `dependencies.added`, unless something else in the package needs it,
   ONLY when `dependencies.status` is `added` or `left-after-failure`; for `kept-for-leftovers` only after the
   leftovers are gone; `removed-after-failure` needs nothing.

== Not supported yet ==
Steps inside stages (tasks, approvals, pages, sub-processes), stage conditions and permissions, alternative or
unsuccessful stages, transition rules, start conditions, several cases per object, and changing, disabling or
deleting an existing case: the case designer does those. No mobile page, mini page or Classic page bar.

Troubleshooting: no bar after F5 - the object has no enabled active-version case, or the feature
`DisableEntityStageProgressBarPreprocessing` is ON on the environment (the bar is then not wired). Academy: Case
Designer (8.x), Set up case stages, Execute a case.

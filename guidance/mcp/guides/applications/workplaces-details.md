clio MCP workplaces-details guide — deleting a workplace, removing rows and bindings, product-workplace moves, and the evidence behind the workplace rules

A details article of the workplaces guide, reached through `workplaces`, which points here and is read first.
This article is the authoritative owner of the MANUAL placement recipe (create a workplace, grant a
role, add or move a section by hand) for when `place-section-in-workplace` is unavailable or fails; of
the ordered procedure for deleting a workplace and the recovery when the parent was deleted first; of removing one section placement, a role grant or the
`My applications` bindings; of moving a section into a product-owned workplace; and of the evidence and
reasoning behind the client-type, binding-column, `Position`, home-page and cache rules. Split out of
`workplaces`, which keeps the three-table model, every confirmation gate, the placement-and-audience
questions, the placement tool, the binding column sets and names, and the rules that bite; read that
first — every rule it states applies here too.

## Ask where things belong before you write — why
- A NEW workplace named for the app is the recommendation when scaffolding because it keeps the
  app's navigation self-contained. When the app already has its own workplace, a second one named
  after the app competes with the first for every binding because `SysWorkplace`.`Name` is not
  unique (see Operations — create a workplace below).
- The section and the home page are ONE question because asking only about the home page binds it to
  one workplace while the section stays in `My applications`, so no single workplace shows a working
  app.
- The audience question is not reduced to `System administrators` vs everyone because that framing
  makes a blanket grant look like the only way to let a normal user in.

## Client type — evidence
- LIVE row: the platform's live write path fills in the WEB client when `SysApplicationClientTypeId`
  is omitted. That is an observation of current behaviour, not a contract, which is why `workplaces`
  tells you to pass the value anyway.
- BINDING row: a binding ships only the columns you passed and install supplies no defaults, so a
  binding without the client type installs a workplace with EMPTY `SysApplicationClientType`, `Type`,
  and `LoaderId` — verified on a cross-environment transfer. An empty client type matches no client,
  so do not expect the workplace to render; the verified fact is the empty columns, not the rendering
  failure.

## Portal workplace type — evidence
NOT isolated: whether portal visibility REQUIRES `Type = Portal`, or whether a `General` workplace
granted to `All external users` suffices, was not A/B-tested on a live stand (it needs a
portal-licensed external user). Record which workplace type you used so the result can be checked.

## Ship every change as a data binding — why the column sets are what they are
- `remove-data-binding-row-db` deletes the live record as well as unbinding it, so there is no way to
  un-ship a workplace row without deleting the workplace. That is why New apps start in a default
  workplace tells you to leave the `My applications` bindings alone.
- `SysModuleInWorkplace` and `Position` on a MOVE: you are upserting over the binding `create-app`
  already shipped, and an upsert rewrites the columns you pass without dropping the ones already
  there — so `Position` stays in that binding and you cannot remove it with these tools. That is
  harmless: the target gets a possibly-meaningless order value, not an empty column. Do not delete the
  binding to "clean" the column set — `remove-data-binding-row-db` would take the live placement row
  with it.
- Each binding appears as a `Data/<Schema>_<Suffix>/data.json` folder in the package.

## Manual placement — only when `place-section-in-workplace` is unavailable or fails
Use this on a clio older than the tool, or when the tool fails for a reason its arguments cannot fix;
the tool does all of it in one idempotent call otherwise. Every write here is the two-step live write
plus binding of `workplaces` → Ship every change as a data binding, and every rule of `workplaces`
(confirmation gates, the questions, the rules that bite) applies.
- Client type: resolve it explicitly (`odata-read` `SysApplicationClientType`, match `Name` `Web` or
  `Mobile`) and pass it in BOTH writes — the `odata-create` payload AND the binding row. Omitting it
  on the LIVE row is survivable (WEB is filled in), on the BINDING row it fails silently (see Client
  type — evidence). `Type` and `LoaderId` are likewise platform-set live and absent from a hand-built
  binding: ship all three.
- Get `Type`, `LoaderId` and the client-type GUID by READ-BACK: right after `odata-create`,
  `odata-read` `SysWorkplace` for the new `Id` selecting `SysApplicationClientTypeId`, `TypeId`,
  `LoaderId`, and pass them into the binding row as `SysApplicationClientType`, `Type`, `LoaderId`
  (only FK columns drop the `Id` suffix; `LoaderId` is the real name and keeps it).
- Create a workplace: `odata-read` `SysWorkplace` for the name FIRST — `Name` is NOT unique and
  `create-data-binding-db` matches an existing row by `Name` before `Id`, keeping the FIRST match
  (see Operations — create a workplace below). On a collision pick a distinct name or verify with
  `read-data-binding-db` that the bound `Id` is yours. Then `odata-create` it (`Name`, `Position`,
  client type), read it back, and `create-data-binding-db` with the FULL column set. To place it in
  the switcher, create it then update `Position`. It has NO `SysAdminUnitInWorkplace` rows, so nobody
  sees it: grant the agreed audience, or the workplace is dead.
- Grant a role's access: resolve the role `Id` from `SysAdminUnit` by name — names are not unique;
  if several match, disambiguate before writing — then `odata-create` a `SysAdminUnitInWorkplace` row
  and bind it. Confirm the role list with the USER and start from the NARROWEST role; never default to
  a blanket role such as `All employees`. Before you BIND it, check whether another package binds the
  workplace (`SysPackageSchemaData` filtered by `SysSchema.Name = 'SysWorkplace'`): on a workplace you
  do not own, keep the grant live-only (unbound) unless the user explicitly agrees to ship it
  everywhere.
- Add a section: resolve its `Id` from `SysModule` by code first — codes are NOT unique
  (`Code = 'Contact'` returns two rows on a stock environment); disambiguate by the `SysModuleId` the
  existing `SysModuleInWorkplace` rows use (the zero-GUID check does not catch a wrong-but-real
  `SysModuleId`). Then `odata-create` the `SysModuleInWorkplace` row and bind it.
- Move a section between workplaces (how an app leaves `My applications`): the TARGET `SysWorkplace`
  row must already have its OWN `SysWorkplace` binding in the same package — bind it FIRST, or the
  junction binding fails with "Data is not bound for connected object". Then `odata-update` the row's
  `SysWorkplaceId` to the target with `confirm=true` and `upsert-data-binding-row-db` the junction
  binding row (it keeps the `create-app` binding's `Position` — harmless, see above). A move is ONE row
  changing parents — do not create a row in the target and leave the source row behind. Then grant the
  audience on the target if it has none, and verify `My applications` no longer lists the section.

## Operations — create a workplace: what a name collision looks like
`SysWorkplace`.`Name` is NOT unique — a stand can hold a web and a mobile workplace with the same
name. `create-data-binding-db` matches an existing row by `Name` before `Id` and keeps the FIRST
match, so a name collision silently binds the OTHER workplace and reports it only as
"Skipped existing row: <that other id>". Binding only `Name`/`Position` yields a package that installs
a workplace with no client type (see Client type — evidence).

## Operations — removing or changing a role's access
- Remove a role's access: `odata-delete` the `SysAdminUnitInWorkplace` row, then reflect it in the
  binding. Resolve the role `Id` from `SysAdminUnit` by name first — names are not guaranteed unique,
  so if more than one row matches, disambiguate before writing. Removing visibility changes who can
  see data: confirm it with the USER first (`workplaces` → Confirmation gates, which also covers
  `remove-data-binding-row-db` deleting the live row with no `confirm` gate).
- Change a role: a remove of the old row plus a grant of the new one (the tool, or Manual placement
  below, including the owning-package check before you bind it).

## Operations — removing one section row
Resolve the section `Id` from `SysModule` by code first — codes are NOT unique, and the zero-GUID rule
does not catch the wrong one, because a wrong-but-real `SysModuleId` is non-zero. Disambiguate by
reading which `SysModuleId` the existing `SysModuleInWorkplace` rows already use.
To remove ONE row you still need its `binding-name` (read `SysPackageSchemaData` as in Deleting a
workplace step 1); only the ORDER is free, because `remove-data-binding-row-db` deletes the live row
itself — so it alone is enough, and a preceding `odata-delete` is redundant. The children-first rule
applies only to the parent cascade. Get the user's agreement before the call (it has no `confirm`
gate).

## Operations — moving a section: the error, and product workplaces
Without the target's own `SysWorkplace` binding in the same package, the junction binding fails with
`SaveSchema failed: Data is not bound for connected object "SysWorkplace" by column "SysWorkplace"`,
a message that names neither the cause nor the fix. Binding the target first is verified for a
workplace you created in your own package.
When the target is a pre-existing product workplace (`Sales`, `Service`, ...) it is bound in a PRODUCT
package, so adopting it into yours makes your package ship a product row with whatever column set you
pass — that path is NOT verified, so surface it to the user and prefer a workplace your own package
owns. A move is still ONE row changing parents — do not create a row in the target and leave the
source row behind.

## Deleting a workplace — the ordered procedure
This destroys the workplace, every section placement in it, and every role grant on it, and no
re-install brings them back. Step 0 is NOT a tool call: confirm with the USER that this workplace on
THIS environment is the intended target, and say what will be lost. Note also that step 4 deletes the
live child rows, not just their bindings (see Ship every change as a data binding).
Deleting the parent CASCADES its child rows in the database but NOT in the package, and once the
children are gone from the database you can no longer read their `Id` values with `odata-read` —
recovering them then means digging them out of an exported package. Orphaned child rows otherwise
stay in the package and get reinstalled later. So read first and delete children first:
1. `execute-esq` over `SysPackageSchemaData` (filter `SysPackage.Name`; columns `Name`,
   `SysSchema.Name`) — cache the `binding-name` of every workplace-related binding in the package.
   `remove-data-binding-row-db` REQUIRES `binding-name` and the binding tools have no list mode, so
   steps 4-6 cannot run without this read. Do not skip it and do not guess the names.
2. `odata-read` `SysModuleInWorkplace` filtered by `SysWorkplace/Id` — cache the `Id` values.
3. `odata-read` `SysAdminUnitInWorkplace` filtered by `SysWorkplace/Id` — cache the `Id` values.
4. `read-data-binding-db` each child binding and INTERSECT its rows with the `Id` values from steps
   2-3. `remove-data-binding-row-db` handles only rows the binding actually carries — it throws
   "Row with key … was not found in binding" on anything else and aborts the cascade half-done. So:
   `remove-data-binding-row-db` for each child row that IS bound, and plain `odata-delete` for any
   live child row that was never bound.
5. `odata-delete` the `SysWorkplace` row with `confirm=true`. Optional: step 6 deletes this row
   itself, so you may skip straight to it.
6. `remove-data-binding-row-db` for the `SysWorkplace` binding row. This works even when step 5
   already removed the live row — verified on a live stand, because the bound-row set is read from the
   binding's own data and not from the table, so the key is still found. Do not "fix" the order.
Recovery if the parent was already deleted: get the binding NAMES from `SysPackageSchemaData` as in
step 1, then get the orphaned row `Id` values with `read-data-binding-db`, which lists every bound row
of that binding. `execute-esq` cannot do this (`SysPackageSchemaData` holds one record per BINDING,
not per bound row), and neither can the write-side binding tools.

## New apps start in a default workplace — evidence, and removing the `My applications` bindings
- Both facts about `My applications` are verified: on an environment with no custom app there is no
  `My applications` row at all (`create-app` creates it, along with a `SysWorkplace_MyApps` binding),
  and the single `SysAdminUnitInWorkplace` row `create-app` ships grants `System administrators` only.
- Home page: the `AppFreedomUI` template creates no home page and leaves `My applications`.
  `HomePageUId` empty, but `AppWithHomePage` creates one AND points `My applications` at it, and its
  `SysWorkplace_MyApps` binding carries that `HomePageUId` — verified on a live stand. So the package
  EXPORTS a mutation of a workplace it does not own: installing it repoints `My applications` on every
  target environment. Clearing it to the zero GUID empties one field; leaving the app's own UId in
  place is strictly worse, because that hijacks the shared workplace instead.
- Removing the `SysWorkplace_MyApps` and `SysAdminUnitInWorkplace_MyApps` bindings: the default is
  to leave both and tell the user the package creates an empty `My applications`. Remove them only
  when ALL of these hold: the user agreed after being told the row is deleted live; `odata-read`
  `SysModuleInWorkplace` filtered by `SysWorkplace/Id` shows no other app's sections; and you remove
  the CHILD binding (`SysAdminUnitInWorkplace_MyApps`) BEFORE the parent (`SysWorkplace_MyApps`), per
  Deleting a workplace. `My applications` is SHARED — deleting it cascades the section placements and
  role grants of every OTHER custom app on the environment, unrecoverably.

## Rules that bite — `Position` and zero-GUID evidence
- `SysModuleInWorkplace.Position` ignores the value you send outright (verified: sent 99, stored
  123) — which is why the binding column set omits it.
- `SysWorkplace.Position` on `odata-create` is NORMALISED (verified: sent 91, stored 24 — the next
  free slot), while on `odata-update` it is stored exactly as sent (verified: 24 changed to 7). Several
  workplaces routinely share one number.
- A junction row whose `SysModuleId` or `SysWorkplaceId` is the zero GUID
  (`00000000-0000-0000-0000-000000000000`) is dead weight: it inserts and reads back but binds nothing.

## When changes appear — why only a re-login covers every case
A signed-in user keeps seeing the old navigation because the lists are cached per session. Creatio
invalidates those caches from an entity event listener on `SysUserInRole` / `SysAdminUnitInWorkplace`
INSERT and DELETE only, so a role grant made through `odata-create` may publish itself, while creating
a workplace, moving a section, or pointing `HomePageUId` at a home page invalidates nothing — and a row
written by the binding tools goes straight through the database engine, which raises no entity events
at all. A re-login is the one instruction that covers every case.

## Verify — reading a binding
`read-data-binding-db` lists a localizable column such as `Name` inline; the package export splits
those into a `Localization` folder, so `read-data-binding-db` shows one more column than `data.json`
for the same binding — that is the same binding, not a discrepancy. Fall back to
`download-application` plus extracting `Data/<Schema>_<Suffix>/data.json` only when you need the raw
archive; it is several steps for the same answer.

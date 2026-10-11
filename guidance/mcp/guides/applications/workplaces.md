clio MCP workplaces guide

A workplace (`SysWorkplace`) is a named container in the Creatio left navigation: it groups
sections and is shown only to the roles granted access to it. Managing one spans three tables —
this guide owns that model. Read `core-rules` first, and `data-bindings` for the binding-tool
contract this guide relies on.
Moved to `workplaces-details`: the step-by-step delete and recovery, removing a section, grant or
the `My applications` bindings, moving into a product workplace, and the evidence behind these rules.
Read it when you delete or remove anything, target a product workplace (`Sales`, `Service`, ...), or
need a rule's evidence.

## The three tables
- `SysWorkplace` — the workplace: `Name`; `Position` (order in the switcher);
  `SysApplicationClientTypeId` (web or mobile, see Client type); `HomePageUId` (the home page, owned
  by `home-page`); `Type` (a `SysWorkplaceType` lookup: `General`, `Portal`, ...) and
  `LoaderId` (the loader schema) — the platform defaults these two on a live write, but a data binding
  MUST carry them (see Client type).
- `SysModuleInWorkplace` — one row per section: required `SysModuleId` (a `SysModule` row) and
  `SysWorkplaceId`; `Position` orders the sections.
- `SysAdminUnitInWorkplace` — one row per role that sees the workplace: required `SysAdminUnitId` (a
  role or user, a `SysAdminUnit` row) and `SysWorkplaceId`.
All three key on `Id`. Lookups take a GUID, in a column form that depends on the call:
- `odata-create` / `odata-update` payloads and `odata-delete` keys: the `<Field>Id` scalar —
  `SysWorkplaceId`, `SysModuleId`, `SysAdminUnitId`.
- `odata-read` FILTERS over a junction table: the navigation path `SysWorkplace/Id`, NOT the scalar
  `SysWorkplaceId`.
- data-binding (`*-db`) calls: the logical name — `SysWorkplace`, `SysModule`, `SysAdminUnit`.

## Confirmation gates
The gate is per tool, not per table, and one destructive tool has NO gate:
- `odata-create` is NOT destructive and accepts no `confirm` argument — do not send one.
- `odata-update` and `odata-delete` ARE destructive and require `confirm=true`.
- `remove-data-binding-row-db` is flagged destructive but accepts NO `confirm` argument either, and
  it DELETES THE LIVE RECORD as well as unbinding it. Nothing holds the call, so get the user's
  agreement yourself before every invocation.
Separately, granting or removing a role's visibility changes who can see data: confirm that intent
with the USER first, even though the create itself is ungated.

## Not to be confused with
Navigation `SysWorkplace` is NOT `SysWorkspace` (the dev configuration workspace) and NOT clio
`create-workspace` (a local project folder). This guide covers navigation `SysWorkplace` only.

## Ask where things belong before you write
Placement and audience are the user's decisions. When the request names no target workplace, STOP
and ask before any NAVIGATION write (any of the three tables or their bindings). `create-app` makes
such a write itself (it creates `My applications` and the section placement), so for a NEW app ask
BEFORE `create-app`, in the same turn you confirm the environment — at the latest right after
`create-app`, before anything further.
Offer:
- a NEW workplace named for the app — recommend it when SCAFFOLDING a new app. Do NOT lead with it
  when adding a section to an app that already has its own workplace: `Name` is not unique, so a
  second same-named workplace is created silently and competes for every binding. Read
  `SysModuleInWorkplace` for the app's existing sections first;
- the workplace the app's sections ALREADY live in — recommend it when adding a section to an
  existing app, and name it;
- `My applications` — it may not exist yet, and leaving the app there restricts it to administrators;
- an existing workplace the user names (list the available `Name` values).
When SCAFFOLDING, the SECTION and the HOME PAGE go in the SAME workplace: ONE question, asked once
for both, then apply both. `home-page` owns the home-page half and this guide the section half —
neither is finished alone.
Then ask WHO SHOULD SEE IT: a new workplace has no `SysAdminUnitInWorkplace` row and is invisible to
everyone, so choosing one obliges you to grant an audience. Do NOT offer this as free text and do NOT
reduce it to the two extremes (`System administrators` vs everyone). Build the options from the
environment:
1. `odata-read` `SysAdminUnit` for roles, not users — `$filter=SysAdminUnitTypeId ne <user type>`
   is fragile, so read `Name` plus `SysAdminUnitTypeId` and keep the organisational and functional
   roles. Sort by `Name` and cap the list at a handful.
2. Offer, in this order: the role(s) the request implies ("for sales managers" puts the matching role
   first, as the RECOMMENDED option); then the other concrete roles you read; then `All employees` —
   labelled as granting every user in the system, and never pre-marked as the recommendation.
3. Offer `System administrators` only as the deliberate admin-tool answer, and say what it costs:
   ordinary users will not see the app at all. It is the `create-app` default, so it needs no write.
Pick the narrowest role that satisfies the request. If the request scopes the app or page to a role,
an existing target workplace's `SysAdminUnitInWorkplace` rows should match it; if they do not,
surface the mismatch and confirm rather than silently granting access.

## Client type
A workplace belongs to one client type via `SysApplicationClientTypeId` (a web workplace never
appears in Creatio Mobile, and vice versa). Resolve it explicitly (`odata-read`
`SysApplicationClientType`, match `Name` `Web` or `Mobile`) and pass it in BOTH writes — the
`odata-create` payload AND the binding row; for the mobile flow read `mobile-page-modification`. Do
not generalise from one write to the other:
- LIVE row: omitting it is survivable (the live write path fills in WEB — an observation, not a
  contract). Pass it anyway; WEB is wrong for mobile.
- BINDING row: omitting it fails SILENTLY — install supplies no defaults, so the workplace arrives
  with EMPTY `SysApplicationClientType`, `Type` and `LoaderId` (the latter two are likewise
  platform-set on a live insert and absent from a hand-built binding). Ship all three.

## Ship every change as a data binding
Each operation is TWO steps: the live write (`odata-create` / `odata-update`), then a binding in
the target package so it transfers — `create-data-binding-db` for the FIRST bind of a row (it
adopts an existing row by `Id`), `upsert-data-binding-row-db` for every later change. `data-bindings`
owns those contracts and how to inspect existing bindings first.
`remove-data-binding-row-db` is the exception (see Confirmation gates): a workplace row cannot be
un-shipped without deleting it live — hence leave the `My applications` bindings alone.
A binding ships ONLY the columns you passed (`data-bindings` owns that rule and the
`IsForceUpdate: false` first-install-only consequence). Column sets:
- `SysWorkplace` — `Id`, `Name`, `Position`, `SysApplicationClientType`, `Type`, `LoaderId`, plus
  `HomePageUId` when the workplace has a home page.
- `SysModuleInWorkplace` — `Id`, `SysWorkplace`, `SysModule`; deliberately no `Position` in a binding
  you CREATE (the server assigns it). A MOVE upserts over the `create-app` binding, which keeps its
  `Position` — harmless; do not delete that binding to "clean" it (that deletes the live placement).
- `SysAdminUnitInWorkplace` — `Id`, `SysWorkplace`, `SysAdminUnit`.
Get `Type`, `LoaderId` and the client-type GUID by READ-BACK (the live write sets them): right after
`odata-create`, `odata-read` `SysWorkplace` for the new `Id` selecting `SysApplicationClientTypeId`,
`TypeId`, `LoaderId`, and pass them into the binding row as `SysApplicationClientType`, `Type`,
`LoaderId` (only FK columns drop the `Id` suffix; `LoaderId` is the real name and keeps it).
Get it right on the FIRST install: a corrected package cannot overwrite a workplace row already on
the target (per `data-bindings`), so a wrong first install is repaired by hand there.
Binding NAMES carry a per-table suffix. Never leave `binding-name` unset — the default is the bare
schema name, which creates a PARALLEL binding instead of touching the app's:
- the package already ships the binding: do NOT derive the name, read it with `execute-esq` over
  `SysPackageSchemaData` (filter `SysPackage.Name`; columns `Name`, `SysSchema.Name`). For section
  code `UsrTodo`, `create-app` ships `SysWorkplace_MyApps` and `SysAdminUnitInWorkplace_MyApps`
  (suffix = WORKPLACE) and `SysModuleInWorkplace_UsrTodo` (suffix = SECTION code).
- you create the binding: pass the name explicitly — `<Schema>_<Workplace>` for `SysWorkplace` /
  `SysAdminUnitInWorkplace` (e.g. `SysWorkplace_Todo`), `<Schema>_<SectionCode>` for
  `SysModuleInWorkplace`.

## Operations
- Create a workplace: `odata-read` `SysWorkplace` for the name FIRST — `Name` is NOT unique and
  `create-data-binding-db` matches an existing row by `Name` before `Id`, keeping the FIRST match, so
  a collision silently binds the OTHER workplace. On a collision pick a distinct name or verify with
  `read-data-binding-db` that the bound `Id` is yours. Then `odata-create` it (`Name`, `Position`,
  client type), read it back, and `create-data-binding-db` with the FULL column set. It has NO
  `SysAdminUnitInWorkplace` rows, so nobody sees it: agree the audience with the user and grant it,
  or the workplace is dead.
- Update a workplace: `odata-update` by `Id` with `confirm=true`, then `upsert-data-binding-row-db`
  on its binding with the full column set.
- Grant a role's access: resolve the role `Id` from `SysAdminUnit` by name — names are not unique;
  if several match, disambiguate before writing — then `odata-create` a `SysAdminUnitInWorkplace` row
  and bind it. It decides who reaches the data behind every section in the workplace: confirm
  the role list with the USER, start from the NARROWEST role that satisfies the request, and never
  default to a blanket role such as `All employees` because it is convenient. Before you BIND it,
  check whether another package binds the workplace (`SysPackageSchemaData` filtered by
  `SysSchema.Name = 'SysWorkplace'`): a bound grant is EXPORTED to every environment installing your
  package, so on a workplace you do not own, say so and keep the grant live-only (unbound) unless the
  user explicitly agrees to ship it everywhere. Removing a role, or changing one (remove the old
  row, grant the new one): `workplaces-details`.
- Add a section: resolve its `Id` from `SysModule` by code first — codes are NOT unique
  (`Code = 'Contact'` returns two rows on a stock environment); disambiguate by the `SysModuleId` the
  existing `SysModuleInWorkplace` rows use. Then `odata-create` the `SysModuleInWorkplace` row and
  bind it. Removing one section row: `workplaces-details`.
- Move a section between workplaces (how an app leaves `My applications`): the TARGET `SysWorkplace`
  row must already have its OWN `SysWorkplace` binding in the same package — bind it FIRST, or the
  junction binding fails with "Data is not bound for connected object". Then `odata-update` the row's
  `SysWorkplaceId` to the target with `confirm=true` and `upsert-data-binding-row-db` the junction
  binding row. A move is ONE row changing parents — do not create a row in the target and leave the
  source row behind. Prefer a target your own package owns; moving into a product workplace (`Sales`,
  `Service`, ...) is NOT verified — surface it to the user and read `workplaces-details`.
- Home page: `home-page` owns `HomePageUId`, including unsetting it safely — go there, do not
  improvise it.
- Delete a workplace: children FIRST — see Deleting a workplace.

## Deleting a workplace
Unrecoverable: the workplace, its section placements and role grants are gone, and no re-install
brings them back. Step 0 is NOT a tool call: confirm with the USER that this workplace on THIS
environment is the intended target, and say what will be lost. The parent's delete cascades its
children in the database but NOT in the package, and their `Id` values are then unreadable — so read
first (binding names from `SysPackageSchemaData`, never guessed; child `Id` values) and delete
children first. Read `workplaces-details` for the ordered steps before the first delete.

## New apps start in a default workplace
`create-app` registers its section in `My applications` and ships that placement as a
`SysModuleInWorkplace` binding — the app does NOT appear in a business workplace on its own.
`My applications` may not pre-exist (`create-app` CREATES it, so it can be missing from the `Name`
list you show), and it is NOT neutral: the one
`SysAdminUnitInWorkplace` row `create-app` ships grants `System administrators` only. Once the target
is agreed, MOVE the app out — a second placement leaves it in two places at once, which is not what
the user asked for:
- move the section row to the target per Operations (target workplace bound FIRST);
- if you created a home page, point the TARGET workplace at it per `home-page` and update that
  workplace's binding;
- CHECK `My applications`.`HomePageUId`: when it points at THIS app's home page (`AppWithHomePage`
  does that and ships it in `SysWorkplace_MyApps`, repointing `My applications` on every target),
  `odata-update` it to `00000000-0000-0000-0000-000000000000` and `upsert-data-binding-row-db` the
  `SysWorkplace_MyApps` row with that zero GUID. Never remove that binding instead: removal deletes
  the live shared workplace, and an upsert cannot drop a bound column. Report plainly that the package
  still ships that column, and that its effect on a target with its own `My applications` home page is
  NOT measured — a residual risk, not a known outcome. Leaving the app's UId there is strictly worse
  (it hijacks the shared workplace); if it points at something you did not create, leave it alone and
  surface the conflict;
- grant the agreed audience on the target if it has none yet — without a `SysAdminUnitInWorkplace`
  row it is invisible to everyone;
- verify `My applications` no longer lists the section.
The package still ships `SysWorkplace_MyApps` and `SysAdminUnitInWorkplace_MyApps`, recreating an
empty `My applications` on every target. Do NOT reflexively "clean that up": no tool unbinds a row
without deleting it, and `My applications` is SHARED — deleting it cascades every OTHER custom app's
placements and grants, unrecoverably. Default to leaving both bindings and telling the user. Remove
them only when ALL hold: the user agreed after being told the row is deleted live; no other app's
sections are in it; the CHILD binding goes before the parent (steps: `workplaces-details`).

## Rules that bite
- There is NO unique constraint on (`SysModule`, `SysWorkplace`): adding or moving a section already
  present in the target creates a duplicate. Read the target's rows first and skip if it is there.
- `Position` is unstable on BOTH tables: `SysModuleInWorkplace` ignores the value you send;
  `SysWorkplace` NORMALISES it on `odata-create`, stores it as sent on `odata-update`, keeps it NOT
  unique, and renumbers every workplace when one is added or removed. To place a workplace, create it
  then update it; always read back the actual order; never treat `Position` as an identifier.
- A junction row whose `SysModuleId` or `SysWorkplaceId` is the zero GUID inserts and reads back but
  binds nothing. Assert both links are non-zero after every write; repair such a row by creating a
  replacement with a new `Id`, not by patching it.

## When changes appear
Workplace, section, and edit-page lists are cached PER SESSION: a browser refresh alone does NOT
show the change. So the LAST step of every navigation change is to tell the user, in the result, to
LOG OUT AND BACK IN. Do not report the change as done without that; do not tell them to press F5;
do not claim a restart is required — it is not. Do not expect the platform to publish the change for
you (why: `workplaces-details`); a re-login covers every case.

## Verify
Read back after every mutation with `odata-read` (filter junctions by `SysWorkplace/Id`); an install
log is not proof. After a MOVE, read BOTH workplaces — the row must be in the target and absent from
the source.
Bindings need their OWN check, BEFORE they reach any environment: a perfect live read-back says
nothing about what the package will install. `read-data-binding-db` with `package-name` and
`binding-name` reports the shipped column set, which must carry every column listed in Ship every
change as a data binding. Reading the workplace back after install (assert
`SysApplicationClientTypeId`, `TypeId`, `LoaderId` non-empty) CONFIRMS the result but does not
protect you: a wrong first install cannot be repaired by re-installing.

clio MCP workplaces guide

A workplace (`SysWorkplace`) is a named container in the Creatio left navigation: it groups
sections and is shown only to the roles granted access to it. Managing one spans three tables —
this guide owns that model. Read `core-rules` first, and `data-bindings` for the binding-tool
contract this guide relies on.
Moved to `workplaces-details`: the MANUAL placement recipe (create a workplace, grant a role, add or
move a section by hand) for when the tool below is unavailable or fails, the step-by-step delete and
recovery, removing a section placement, a role grant or the `My applications` bindings, moving into a
product workplace, and the evidence behind these rules. Read it when you delete or remove anything,
place by hand, target a product workplace (`Sales`, `Service`, ...), or need a rule's evidence.

## Place a section, grant roles, leave My applications: the tool
To place a section in a workplace, grant roles, or move an app out of `My applications`, call
`place-section-in-workplace` through `clio-run` (after `get-tool-contract`; clio 8.1.0.141 and later).
Arguments: `environment-name`, `section-code`, `workplace-name`, `package-name`, `roles`; optional
`mode` `existing`|`new`|`none`, `client-type` (`web` default), `remove-from-my-applications` (default
true), `position`. It resolves client type, section and roles, binds each row it owns (a
workplace with `Type` and `LoaderId`), never duplicates or deletes, and is safe to re-run. A move with no roles and
`remove-from-my-applications=true` is refused (the section would become invisible). Relay `warnings`
(a section code, workplace or role name shared by several rows is resolved by a rule or refused: say
which row it took), report `section-workplaces` and give the user `next-step` (log out and in).
A workplace another package ships (a product one: `Sales`, `Service`, ...) is used but never bound
into yours: the tool warns that installing your package needs that one, refuses a `position` change,
and leaves that package's existing grants and placements to it. It still binds into your package the
grant of every role you pass that no other package ships (a new one, or an existing live-only one),
and then ships it to every environment installing your package: for such a workplace pass a role only after the user explicitly agrees to ship its grant everywhere; otherwise
omit it and grant live-only (unbound) by hand (`workplaces-details`, manual placement). Such a target
is NOT verified: surface that to the user and prefer a workplace your own package owns.
It takes no `confirm`, and it does not choose for you: ask the questions below FIRST, then pass the
user's workplace and roles. Do by hand only what it does not do — deletes and removals (Deleting a
workplace, `workplaces-details`), home pages (`home-page`, and the `My applications` check below), a
rename or other workplace update (Operations), and a tool failure or an older clio
(`workplaces-details`, manual placement).

## The three tables
- `SysWorkplace` — the workplace: `Name`; `Position` (order in the switcher);
  `SysApplicationClientTypeId` (web or mobile); `HomePageUId` (the home page, owned by `home-page`);
  `Type` (a `SysWorkplaceType` lookup) and `LoaderId` (the loader schema).
- `SysModuleInWorkplace` — one row per section: `SysModuleId` (a `SysModule` row), `SysWorkplaceId`;
  `Position` orders the sections.
- `SysAdminUnitInWorkplace` — one row per role that sees the workplace: `SysAdminUnitId` (a role or
  user), `SysWorkplaceId`.
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
with the USER first, whichever tool makes the write.

## Not to be confused with
Navigation `SysWorkplace` is NOT `SysWorkspace` (the dev configuration workspace) and NOT clio
`create-workspace` (a local project folder). This guide covers navigation `SysWorkplace` only.

## Ask where things belong before you write
`core-rules` (Navigation placement) owns WHEN to ask, and what to do when you cannot; it covers every
NAVIGATION write (any of the three tables or their bindings). When it has you ask, offer:
- a NEW workplace named for the app — recommend it when SCAFFOLDING a new app. Do NOT lead with it
  when adding a section to an app that already has its own workplace: `Name` is not unique, so a
  second same-named workplace competes with the first. Read `SysModuleInWorkplace` for the app's
  existing sections first;
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
surface the mismatch and confirm rather than silently granting access. Before granting on a workplace
another package binds (`SysPackageSchemaData` filtered by `SysSchema.Name = 'SysWorkplace'`), say so:
a bound grant is EXPORTED to every environment installing your package, so keep it live-only (unbound)
unless the user explicitly agrees to ship it everywhere, on the tool path too.

## Client type
A workplace belongs to one client type (a web workplace never appears in Creatio Mobile, and vice
versa). The tool takes `client-type`; for the mobile flow read `mobile-page-modification`. A MANUAL
binding row must carry `SysApplicationClientType`, `Type` and `LoaderId` — install supplies no
defaults, so without them the workplace arrives empty (`workplaces-details`).

## Portal / external-user workplaces
A section that PORTAL (self-service / external) users must reach lives in a workplace granted to the
EXTERNAL audience. The end-to-end portal-section flow (page binding, this placement, object-access
grant) is owned by `portal-sections`; here are the two workplace specifics it relies on, on top of the
internal flow above.
- Audience — grant the workplace to the role **All external users**
  (`720b771c-e7a7-4f31-9cfb-52cd21c3739f`) through `roles` of `place-section-in-workplace`, as for an
  internal role. A workplace no external role can see hides the section from portal navigation. Confirm
  the audience with the user, and do NOT add `All external users` to a workplace that also carries
  internal-only sections unless exposing those is intended.
- Type — `SysWorkplace.Type` (the `SysWorkplaceType` lookup) has `Portal`
  (`111c7ff1-8224-45b0-a24e-fcc983fc0c70`) and `Self-service` (`a77fefc0-227d-4e21-9992-a9a7ee889e21`)
  in addition to `General` and Limited internal. `place-section-in-workplace` creates a workplace with
  the platform's default `Type` and has no argument for it, so a PORTAL-type workplace is created BY
  HAND (`workplaces-details`, manual placement) with `Type` set EXPLICITLY on `odata-create` AND carried
  in the binding — a hand-built binding does not inherit it (same silent-empty trap as
  `SysApplicationClientType`). Then place the section and grant the audience with the tool,
  `mode=existing`.
  Prefer a dedicated `Portal`-type workplace for a portal section; a `General` one granted to
  `All external users` is unverified (`workplaces-details`, Portal workplace type — evidence).

## Ship every change as a data binding
A write you make BY HAND is TWO steps: the live write (`odata-create` / `odata-update`), then a binding
in the target package so it transfers — `create-data-binding-db` for the FIRST bind of a row (it
adopts an existing row by `Id`), `upsert-data-binding-row-db` for every later change. `data-bindings`
owns those contracts and how to inspect existing bindings first. The tool does both for its rows.
`remove-data-binding-row-db` is the exception (see Confirmation gates): a workplace row cannot be
un-shipped without deleting it live — hence leave the `My applications` bindings alone.
A binding ships ONLY the columns you passed (`data-bindings` owns that rule and the
`IsForceUpdate: false` first-install-only consequence). Column sets:
- `SysWorkplace` — `Id`, `Name`, `Position`, `SysApplicationClientType`, `Type`, `LoaderId`, plus
  `HomePageUId` when the workplace has a home page.
- `SysModuleInWorkplace` — `Id`, `SysWorkplace`, `SysModule`; no `Position` in a binding you CREATE.
- `SysAdminUnitInWorkplace` — `Id`, `SysWorkplace`, `SysAdminUnit`.
Get it right on the FIRST install: a corrected package cannot overwrite a workplace row already on
the target (per `data-bindings`), so a wrong first install is repaired by hand there.
Binding NAMES carry a per-table suffix. Never leave `binding-name` unset — the default is the bare
schema name, which creates a PARALLEL binding instead of touching the app's:
- the package already ships the binding: do NOT derive the name, read it with `execute-esq` over
  `SysPackageSchemaData` (filter `SysPackage.Name`; columns `Name`, `SysSchema.Name`). For section
  code `UsrTodo`, `create-app` ships `SysWorkplace_MyApps` and `SysAdminUnitInWorkplace_MyApps`
  (suffix = WORKPLACE) and `SysModuleInWorkplace_UsrTodo` (suffix = SECTION code); the tool reuses them.
- you create the binding: pass the name explicitly — `<Schema>_<Workplace>` for `SysWorkplace` /
  `SysAdminUnitInWorkplace` (e.g. `SysWorkplace_Todo`), `<Schema>_<SectionCode>` for
  `SysModuleInWorkplace`.

## Operations
- Create a workplace, grant a role, add a section, move a section: the tool. By hand only when it is
  unavailable or fails: `workplaces-details`, manual placement.
- Update a workplace (rename, reorder): `odata-update` by `Id` with `confirm=true`, then
  `upsert-data-binding-row-db` on its binding with the full column set.
- Remove a role, change one, or remove one section row: `workplaces-details`.
- Home page: `home-page` owns `HomePageUId`, including unsetting it safely — go there, do not
  improvise it.
- Delete a workplace: children FIRST — see Deleting a workplace.

## Deleting a workplace
Unrecoverable: the workplace, its section placements and role grants are gone, and no re-install
brings them back. Step 0 is NOT a tool call: confirm with the USER that this workplace on THIS
environment is the intended target, and say what will be lost. The parent's delete cascades its
children in the database but NOT in the package, and their `Id` values are then unreadable with
`odata-read` (recovery: `workplaces-details`) — so read first (binding names from
`SysPackageSchemaData`, never guessed; child `Id` values) and delete children first. You MUST read
`workplaces-details` for the ordered steps before the first delete: a removal the binding does not
carry aborts the cascade half-done.

## New apps start in a default workplace
`create-app` registers its section in `My applications` and ships that placement as a
`SysModuleInWorkplace` binding — the app does NOT appear in a business workplace on its own.
`My applications` may not pre-exist (`create-app` CREATES it, so it can be missing from the `Name`
list you show), and it is NOT neutral: the one `SysAdminUnitInWorkplace` row `create-app` ships grants
`System administrators` only. Once the target and audience are agreed, MOVE the app out — call the
tool with the agreed `roles` (`remove-from-my-applications` defaults to true; a second placement would
leave the app in two places). Then, by hand:
- if you created a home page, point the TARGET workplace at it per `home-page`;
- CHECK `My applications`.`HomePageUId`: when it points at THIS app's home page (`AppWithHomePage`
  does that and ships it in `SysWorkplace_MyApps`, repointing `My applications` on every target),
  `odata-update` it to `00000000-0000-0000-0000-000000000000` and `upsert-data-binding-row-db` the
  `SysWorkplace_MyApps` row with that zero GUID. Never remove that binding instead: removal deletes
  the live shared workplace, and an upsert cannot drop a bound column. Report plainly that the package
  still ships that column, and that its effect on a target with its own `My applications` home page is
  NOT measured — a residual risk, not a known outcome. Leaving the app's UId there is strictly worse
  (it hijacks the shared workplace); if it points at something you did not create, leave it alone and
  surface the conflict.
The package still ships `SysWorkplace_MyApps` and `SysAdminUnitInWorkplace_MyApps`, recreating an
empty `My applications` on every target. Do NOT reflexively "clean that up": no tool unbinds a row
without deleting it, and `My applications` is SHARED — deleting it cascades every OTHER custom app's
placements and grants, unrecoverably. Default to leaving both bindings and telling the user. Remove
them only when ALL hold: the user agreed after being told the row is deleted live; no other app's
sections are in it; the CHILD binding goes before the parent (steps: `workplaces-details`).

## Rules that bite (manual writes)
- There is NO unique constraint on (`SysModule`, `SysWorkplace`): adding or moving a section already
  present in the target creates a duplicate. Read the target's rows first and skip if it is there.
- `Position` is unstable on BOTH tables: `SysModuleInWorkplace` ignores the value you send;
  `SysWorkplace` NORMALISES it on `odata-create`, stores it as sent on `odata-update`, keeps it NOT
  unique, and renumbers every workplace when one is added or removed. Always read back the actual
  order; never treat `Position` as an identifier.
- A junction row whose `SysModuleId` or `SysWorkplaceId` is the zero GUID inserts and reads back but
  binds nothing. Assert both links are non-zero after every write; repair such a row by creating a
  replacement with a new `Id`, not by patching it.

## When changes appear
Workplace, section, and edit-page lists are cached PER SESSION: a browser refresh alone does NOT
show the change. So the LAST step of every navigation change is to tell the user, in the result, to
LOG OUT AND BACK IN (the tool's `next-step` says so). Do not report the change as done without that;
do not tell them to press F5; do not claim a restart is required — it is not. Do not expect the
platform to publish the change for you (why: `workplaces-details`); a re-login covers every case.

## Verify
The tool reads its result back (`section-workplaces`). After a write BY HAND, read back with
`odata-read` (filter junctions by `SysWorkplace/Id`); an install log is not proof. After a manual MOVE,
read BOTH workplaces — the row must be in the target and absent from the source.
Bindings need their OWN check, BEFORE they reach any environment: a perfect live read-back says
nothing about what the package will install. `read-data-binding-db` with `package-name` and
`binding-name` reports the shipped column set, which must carry every column listed in Ship every
change as a data binding. Reading the workplace back after install (assert
`SysApplicationClientTypeId`, `TypeId`, `LoaderId` non-empty) CONFIRMS the result but does not
protect you: a wrong first install cannot be repaired by re-installing.

clio MCP portal sections guide

Register a Freedom UI SECTION so PORTAL (self-service / external) users can open it. This is an
ORCHESTRATION guide: it does not add a new tool, it SEQUENCES four capabilities that already have
owners, and it owns only what is specific to the portal case — the ordering, the single audience that
must be carried through every step, and the deny-by-default rule that makes a portal section different
from an ordinary one. Read `core-rules` first. Each step names the sibling guide that owns its
mechanics; do not re-derive them here.

## The one rule that changes everything: external users are deny-by-default
An INTERNAL user reaches an object unless something restricts them (the "…any data" system operations
let internal roles bypass object permissions — see `entity-operation-access`). An EXTERNAL / portal
user is the opposite: they reach ONLY what an explicit grant gives them. So a section that "just works"
for an employee is invisible to a portal user until BOTH its navigation AND its data access are granted
to the portal audience. Every step below therefore carries the SAME audience — the platform role
**All external users** (`720b771c-e7a7-4f31-9cfb-52cd21c3739f`) — and a portal section is finished only
when all of navigation, page binding, and object access name that role. Miss one and the user sees an
empty section, a blank list, or nothing in the menu, with no error.

## Freedom, not Classic — do not write the SSP tables
This flow is the FREEDOM UI portal mechanism. The Classic self-service-portal (SSP) artifacts —
`Portal_SysModule`, `SysModuleEntityInPortal`, `SysPortal` (package SSP), and `PortalSchemaAccessList` /
`PortalColumnAccessList` — are NOT part of it and must NOT be written for a Freedom portal section; a
Freedom section grants access through `SysSchemaOperationRight` (the object-rights step below), and
`PortalSchemaAccessList` stays empty. Evidence: the visibility mechanism was read from the creatio-ui
source (`ObjectPermissionsValidator` in the related-pages designer) and confirmed against
`RightManagementService` on a live stand (Creatio 10.2.129.0). Writing the Classic tables for a Freedom
section adds dead configuration that grants nothing.

## Out of scope — the users and roles themselves
This guide makes an EXISTING section reachable by the portal audience. It does NOT create external
users, add them to the `All external users` role, or assign portal licenses — that is user and role
administration (`administration`). It assumes `All external users` already has the intended members;
if it does not, the section is correctly configured and still seen by no one. Confirm the audience with
the USER before granting — a portal grant widens who can read business data.

## The four steps (in order)
Each is owned by another guide; here is what the portal case adds. Do them in this order — a later step
assumes the earlier one exists.

1. The SECTION and its page must exist. A portal section is an ordinary Freedom section
   (`SysModule`, Type=0) with a Freedom form page — there is no separate "portal section" object.
   Create them the normal way (`app-modeling` for the section/app, `pages/creation` for a page) if they
   do not exist yet. Nothing here is portal-specific.

2. Bind the page for the portal audience — owner: `related-page-binding`. Give the record page an
   `All external users` entry so opening a record as a portal user resolves to the Freedom page:
   `create-related-page-addon` with `--portal-default-page` (and `--portal-add-page` if adding differs).
   The audience is the entry's ROLE — `related-page-binding` owns the rule that the internal base
   default (`All employees`) is NOT a verified portal fallback, so an explicit portal entry is required.
   The `is-ssp-default` flag is NOT how the portal audience is set; do not touch it.

3. Place the section in a workplace the portal audience can see — owner: `workplaces`. The section needs
   a `SysModuleInWorkplace` row in a workplace whose `SysAdminUnitInWorkplace` grants **All external
   users**; a workplace no external role can see hides the section from the menu. `workplaces` owns
   `SysWorkplace.Type` (the `SysWorkplaceType` lookup has a `Portal` value) and the three-table model —
   go there for the writes and their data bindings. Confirm placement and audience with the user, as
   that guide requires.

4. Grant OBJECT access to the portal audience — owner: `object-rights`. Give `All external users`
   operation permissions on the section's object AND its lookup objects, or the list and fields are
   empty even though the section and page exist. `--include-connected` grants read to the WHOLE external
   audience on every connected lookup too, so REVIEW WHAT THE FAN-OUT EXPOSES BEFORE GRANTING — a
   connected lookup can carry PII or internal reference data (`Account`, `Contact`, price lists,
   catalogs) that must not become world-readable to every external user:
   1. Dry-read the connected set first: `get-object-rights --entity-schema-name <Object>
      --grantee 720b771c-e7a7-4f31-9cfb-52cd21c3739f --include-connected` to enumerate the object and
      the lookups the grant would touch. On MCP the grant applies without a preview, so this read is the
      only place the target list is seen. Security and system lookups (for example `SysAdminUnit`) are
      never in the fan-out — the read names them in a warning; granting one is a separate decision.
   2. Confirm none of them is unsafe for the whole external audience. Where a shared lookup holds
      sensitive data, do NOT fan it out — grant per-object (drop `--include-connected` and run
      `set-object-rights` only on the safe objects) and handle the sensitive lookup another way.
   3. Then grant the reviewed set:
      `set-object-rights --entity-schema-name <Object> --grantee 720b771c-e7a7-4f31-9cfb-52cd21c3739f
      --operations read --include-connected --confirm`.
   Pin `--operations read` explicitly. The root default (read/create/edit) would hand the WHOLE external
   audience create and edit on the section's object; the connected lookups get read only unless
   `--connected-operations` widens them. Grant only what the portal scenario needs, and add
   `create`/`edit` only where external users genuinely author records.
   This read-exposure review mirrors the write caution — a fan-out grant is a disclosure decision, not a
   mechanical step. Finally VERIFY with the same `get-object-rights … --include-connected` read: it lists
   the objects the portal audience cannot READ, so after the read-only grant above the list is empty. An
   object it reports as unverified (not read, or the connected set could not be enumerated) is NOT
   covered — re-run the read; do not treat it as done. `object-rights` owns the coverage rule and the
   failure semantics.

## Fixed ids
- `All external users` role (the portal audience, used in steps 2–4):
  `720b771c-e7a7-4f31-9cfb-52cd21c3739f`.
Other ids (the `Portal` workplace type, `All employees`) are owned by the guides that use them —
`workplaces` and `related-page-binding` / `object-rights` respectively.

## Verify — the real proof is an external user
Reading each step back (the related-page entry, the `SysModuleInWorkplace` row, the object grant)
confirms stored configuration, not that a portal user sees the section. The authoritative check is a
portal-LICENSED external user, a member of `All external users`, logging into the portal and opening the
section with data visible — and, per `workplaces`, navigation caches per session, so that user must log
in AFTER step 3 (a re-login, not F5). Evidence boundary: the grant mechanism (a `SysSchemaOperationRight`
grant to `All external users`) is grounded in the creatio-ui source and confirmed at the
`RightManagementService` layer; whether portal visibility additionally depends on the workplace's
`SysWorkplace.Type` being `Portal` versus the audience grant alone was NOT isolated on a live stand
(it needs a portal-licensed external user to A/B). Treat the external-user round-trip as the acceptance
test, not the per-step read-backs.

<!-- Version boundary: the object-rights step uses set-object-rights / get-object-rights, which ship in
clio <SET-OBJECT-RIGHTS-CLIO-VERSION-TBD>. Replace this placeholder with the released clio version
before merging (merging publishes; an unresolved boundary is refused by the producer contract suite).
Until then this article stays on a draft PR. -->

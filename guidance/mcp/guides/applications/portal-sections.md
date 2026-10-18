clio MCP portal sections guide

Register a Freedom UI SECTION so PORTAL (self-service / external) users can open it. This is an
ORCHESTRATION guide: it does not add a new tool, it SEQUENCES five capabilities that already have
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
**All external users** (`720b771c-e7a7-4f31-9cfb-52cd21c3739f`). A portal section is finished when its
navigation, its page binding and its object access each name that role, and — on every object whose
"Use record permissions" switch is ON — a default record rule names it as the AUTHOR, so that employees
reach what portal users create (step 5). Each miss fails with no error, and each has its own symptom:
- no workplace granted to the role — the section is not in the menu;
- no portal page binding — a record does not open on the portal page;
- no object grant — the list and the fields are empty;
- record permissions ON with no rule authored by the role — employees do not see the records created on
  the portal (only holders of "view any data" do), so nobody processes them.

## Freedom, not Classic — do not write the SSP tables
This flow is the FREEDOM UI portal mechanism. The Classic self-service-portal (SSP) artifacts —
`Portal_SysModule`, `SysModuleEntityInPortal`, `SysPortal` (package SSP), and `PortalSchemaAccessList` /
`PortalColumnAccessList` — are NOT part of it and must NOT be written for a Freedom portal section; a
Freedom section grants access through `SysEntitySchemaOperationRight` (the object-rights step below), and
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

## The five steps (in order)
Each is owned by another guide; here is what the portal case adds. Do them in this order — a later step
assumes the earlier one exists.

1. The SECTION and a SEPARATE PORTAL PAGE must exist. A portal section is an ordinary Freedom section
   (`SysModule`, Type=0) — there is no separate "portal section" object; create it the normal way
   (`app-modeling`) if it does not exist yet. The page is portal-specific: ALWAYS design a dedicated
   portal record page (`page-creation`), never reuse the employees' page. External users usually must
   see a LIMITED part of the record — a few fields, no internal details, notes, history or system
   fields — and the employees' page shows everything the object holds. When the request does not say
   what the portal page shows, ASK THE USER before designing it: which fields and details external
   users see, which of them they may edit, and whether they add records. That scope drives the rest:
   step 2 binds this page, and step 4 grants only the objects it shows.

2. Bind the portal page for the portal audience — owner: `related-page-binding`. Give the section's
   object an `All external users` entry so opening a record as a portal user resolves to the portal page
   of step 1:
   `create-related-page-addon` with `--portal-default-page` (and `--portal-add-page` if adding differs).
   The audience is the entry's ROLE — `related-page-binding` owns the rule that the internal base
   default (`All employees`) is NOT a verified portal fallback, so an explicit portal entry is required.
   The `is-ssp-default` flag is NOT how the portal audience is set; do not touch it.

3. Place the section in a workplace the portal audience can see — owner: `workplaces`. The section needs
   a `SysModuleInWorkplace` row in a workplace whose `SysAdminUnitInWorkplace` grants **All external
   users**; a workplace no external role can see hides the section from the menu. `workplaces` owns
   `SysWorkplace.Type` and the three-table model — go there for the placement
   (`place-section-in-workplace` with `All external users` in `roles`). Confirm placement and audience
   with the user, as that guide requires.

4. Grant OBJECT access to the portal audience — owner: `object-rights`; read it before this step (it
   owns the row priority, the enable flag and the ask-before-write rule). Give `All external users`
   operation permissions on the section's object AND on every object the portal pages show, or the list
   and fields are empty even though the section and page exist. Each object is its own grant, and each
   grant is a disclosure decision, not a mechanical step:
   1. LIST: build the list from what the portal pages show — the section's object, the object of each
      detail on the page, and the lookup object of each column shown. `get-object-rights
      --entity-schema-name <Object> --grantee 720b771c-e7a7-4f31-9cfb-52cd21c3739f --include-connected`
      reads the object and its OWN lookups with their rows and writes nothing; it does not list lookups
      the object inherits (for example `Created by` → `Contact`), detail objects or their lookups, so
      read each of those by name. Security and system lookups (for example `SysAdminUnit`) are never
      listed; granting one is a separate decision.
   2. ASK THE USER before any write, per object, with the facts of a preview of the planned call
      (`preview=true`): the operations, and whether operation permissions turn on (with the rows that
      then start to decide). Say that EVERY external user gets that access, on EVERY record of the
      object: a shared lookup (`Account`, `Contact`, price lists, catalogs) can carry PII or internal
      reference data that must not become readable to every external user — leave such a lookup out and
      handle it another way. Which RECORDS they reach is step 5, not this step.
   3. GRANT each approved object in its own call: `set-object-rights --entity-schema-name <Object>
      --grantee 720b771c-e7a7-4f31-9cfb-52cd21c3739f --operations read`, plus
      `--enable-operation-permissions` when the object is not administered yet — such an object is closed
      to external users, so their first grant on it always needs the flag. `--operations` is required:
      `read`, plus `create`/`edit` on the section's object only where external users genuinely author
      records. The All employees row does not decide for external users, so it does not shadow this grant.
   4. VERIFY with the same `get-object-rights … --include-connected` read. The tool reports facts, not a
      verdict: every approved object must show an `All external users` row with at least `read`. An
      object reported as `not administered by operation permissions` is still CLOSED to external users;
      an object that could not be read, or a connected set that could not be enumerated, is not verified
      — re-run the read; do not treat it as done.

5. Set RECORD access on the objects granted in step 4 — owner: `default-record-rights`; read it before
   any write here (it owns the switch, the rules, the ask-before-write rule and the apply decision).
   With the "Use record permissions" switch ON, a record is reached only by its author, through its
   record rights, or with "view any data". So the rules decide two directions, and they are separate:
   - portal → employees: a record a portal user creates is reached by employees only through a rule with
     AUTHOR `All external users` (or the portal role) and an internal GRANTEE — the OOTB `Account` rule
     `All external users` → `All employees` is the model. Without it the portal works for the portal
     user, and the business never sees what came in. This rule is REQUIRED on every object where portal
     users create records.
   - employees → portal: whether portal users see records they did not create is a business decision.
     Seeing only their own records is often the intended behaviour; when they must see more, that is a
     rule with GRANTEE `All external users`, or record rights per record / at runtime (below).
   1. READ the record part of the same `get-object-rights` read: `Record permissions: ON|OFF` and every
      default rule. OFF: every user with read reaches EVERY record — every external user sees every
      record, the disclosure of step 4; keep the object OFF only if that is intended. ON: name to the
      user the rules whose AUTHOR or GRANTEE is `All external users` or a portal role.
   2. DECIDE with the user, in this order:
      - the INTERNAL role that must work with portal-created records (`All employees`, or a narrower
        role such as a support line) → a rule with AUTHOR `All external users` and that GRANTEE, with the
        operations they need (`read`, usually `edit`);
      - whether portal users see only their own records; if not, a rule with GRANTEE
        `All external users` and the AUTHOR whose records they must see. One particular record →
        `record-rights`; records filtered by the external user's account or contact need a runtime rule
        (`record-permissions` is the entry point for choosing).
      Write each rule with `set-default-record-rights`, preview first, as that guide requires. Turning the
      switch ON to stop every external user reaching every record is the same tool with
      `enable-record-permissions` together with the first rule; it changes access for INTERNAL users too —
      say so.
   3. A rule reaches NEW records only. Existing records keep their rights until
      `apply-default-record-rights` runs — the user's decision, never yours: tell them the record count
      from the result and ask, per `default-record-rights`.

## Fixed ids
- `All external users` role (the portal audience, used in steps 2–5):
  `720b771c-e7a7-4f31-9cfb-52cd21c3739f`.
Other ids (the `Portal` workplace type, `All employees`) are owned by the guides that use them —
`workplaces` and `related-page-binding` / `object-rights` respectively.

## Verify — the real proof is an external user
Reading each step back (the related-page entry, the `SysModuleInWorkplace` row, the object grant)
confirms stored configuration, not that a portal user sees the section. The authoritative check is a
portal-LICENSED external user, a member of `All external users`, logging into the portal and opening the
section with data visible — and, per `workplaces`, navigation caches per session, so that user must log
in AFTER step 3 (a re-login, not F5). Evidence boundary: the grant mechanism (a
`SysEntitySchemaOperationRight` grant to `All external users`) is grounded in the creatio-ui source and
confirmed at the `RightManagementService` layer; whether portal visibility additionally depends on the workplace's
`SysWorkplace.Type` being `Portal` versus the audience grant alone was NOT isolated on a live stand
(it needs a portal-licensed external user to A/B). Treat the external-user round-trip as the acceptance
test, not the per-step read-backs. On an object with record permissions ON, also check the other
direction: an employee in the rule's grantee role, without "view any data", opens a record the portal
user created — a system administrator sees it whatever the rules say, so they prove nothing.

Version boundary: the object-rights step uses `set-object-rights` / `get-object-rights`, which ship in
clio 8.1.0.138 and later; on an older clio, stop before step 4 and tell the user to update clio. The
record step needs a clio newer than 8.1.0.140 (`set-default-record-rights`, and the record part of
`get-object-rights`); on an older clio, use the "Use record permissions" page of the Object permissions
designer for step 5.

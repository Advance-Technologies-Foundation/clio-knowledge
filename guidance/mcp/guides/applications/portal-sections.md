clio MCP portal sections guide

Owns: the order of the portal-section steps and the single audience carried through them; the separate
portal page and its scope question; which objects and which record rules the portal case needs; the
external-user acceptance test.

Register a Freedom UI SECTION so PORTAL (self-service / external) users can open it. This is an
ORCHESTRATION guide: it does not add a new tool, it SEQUENCES five capabilities that already have
owners. Read `core-rules` first. Each step names the sibling guide that owns its mechanics — read it
before the step; its rules are not restated here.

## The one rule that changes everything: external users are deny-by-default
An INTERNAL user reaches an object unless something restricts them (the "…any data" system operations
let internal roles bypass object permissions — see `object-rights`). An EXTERNAL / portal
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
Each is owned by another guide; read the owner before its step — here is only what the portal case adds.
Do them in this order — a later step assumes the earlier one exists.

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
   object an explicit `All external users` entry that opens the portal page of step 1
   (`create-related-page-addon` with `--portal-default-page`, and `--portal-add-page` if adding differs);
   the internal base default does not cover portal users.

3. Place the section in a workplace the portal audience can see — owner: `workplaces`
   (`place-section-in-workplace` with `All external users` in `roles` and `workplace-type=portal`, clio
   <CLIO-WORKPLACE-TYPE-TBD> and later). A workplace no external role can see hides the section from the
   menu. Confirm placement and audience with the user, as that guide requires.

4. Grant OBJECT access to the portal audience — owner: `object-rights`. Follow its "Give a role access to
   an object and its lookups" flow with grantee `All external users`; it owns the listing, the preview,
   the grant and the verify. `set-object-rights` writes at once: you MUST preview each call and ASK THE
   USER before any write, and GRANT each approved object in its own call — first
   `set-object-rights entity-schema-name=<Object> grantee=720b771c-e7a7-4f31-9cfb-52cd21c3739f operations=read preview=true`,
   then, on the user's yes, the same call without `preview`. What the portal case adds:
   - the list is what the portal pages show — the section's object, the object of each detail on the
     page, and the lookup object of each column shown — not only what `include-connected` reports;
   - each grant is a disclosure decision: say that EVERY external user gets that access, on EVERY record
     of the object. A shared lookup (`Account`, `Contact`, price lists, catalogs) can carry PII or internal
     reference data — leave such a lookup out and handle it another way;
   - `read`, plus `create`/`edit` on the section's object only where external users genuinely author
     records. An object not administered by operation permissions is CLOSED to external users, so their
     first grant on it needs `enable-operation-permissions`, which turns operation permissions ON for
     EVERY role, not only the portal audience. Before that write you MUST name to the user, from the
     preview, each stored row of an external role that starts to decide (it can WIDEN external access)
     and say that internal users then keep only what the rows give them (`object-rights` owns the rule);
   - done when every approved object shows an `All external users` row with at least `read` in the
     read-back. Which RECORDS they reach is step 5.

5. Set RECORD access on the objects of step 4 — owner: `default-record-rights`; it owns the switch, the
   rules, the preview and the apply. With "Use record permissions" OFF every external user with read
   reaches EVERY record — keep it OFF only if that is intended. With it ON the rules decide two
   directions, and they are separate decisions for the user:
   - portal → employees: a record a portal user creates is reached by employees only through a rule with
     AUTHOR `All external users` (or the portal role) and an internal GRANTEE (`All employees`, or a
     narrower role such as a support line), usually with `read` and `edit`. This rule is REQUIRED on
     every object where portal users create records — without it the business never sees what came in.
   - employees → portal: whether portal users see records they did not create is a business decision.
     Seeing only their own records is often the intended behaviour; when they must see more, that is a
     rule with GRANTEE `All external users`, `record-rights` for one record, or a runtime rule
     (`record-permissions` is the entry point for choosing).
   A rule reaches NEW records only. Existing records keep their rights until
   `apply-default-record-rights` runs — the user's decision, never yours.

## Fixed ids
- `All external users` role (the portal audience, used in steps 2–5):
  `720b771c-e7a7-4f31-9cfb-52cd21c3739f`.
Other ids (`All employees`) are owned by the guides that use them — `related-page-binding` /
`object-rights`.

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

Version boundary: steps 3, 4 and 5 need the clio releases named in `workplaces` (clio 8.1.0.141 and later
for `place-section-in-workplace`, a later release for its `workplace-type`), `object-rights` and
`default-record-rights`; on an older clio, stop
before that step and use the fallback that guide names.

clio MCP object-rights guide

Manage OBJECT operation permissions — who may read/create/edit/delete ANY record of an entity (the
SysSchemaOperationRight / "Object permissions" layer) — for ANY role, with two tools, NOT with
hand-written ESQ into the rights tables:
- get-object-rights — read the per-role operation permissions of an object.
- set-object-rights — grant or revoke a role's operations. DESTRUCTIVE (applies immediately).

This is the object-level analog of `record-rights`. Do NOT confuse the layers:
- Object operation rights — SysSchemaOperationRight. Per-ROLE "who may read/create/edit/delete ANY
  record of this entity". This is the System Designer "Object permissions" section, and what these
  tools own.
- Record-level rights — Sys<Entity>Right / SysSchemaAdminUnitRight. Per-RECORD "who can access THIS
  record/dashboard" -> read `get-guidance name=record-rights`.
- System-operation permissions — SysAdminOperation (e.g. "Manage users") -> read
  `get-guidance name=administration`.
Querying the wrong layer to answer "who can access this object" gives the WRONG answer. System
operations ("View/Add/Edit/Delete any data") granted to a role ADD access beyond what these tools
report, so an internal role may still reach records not listed here — that layer is owned by
`get-guidance name=entity-operation-access`.

grantee is a SysAdminUnit id (a role or user id). Names are NOT unique — resolve a name to its id
yourself (e.g. execute-esq on SysAdminUnit by Name), the tools take the id. The portal audience has a
fixed platform id — see the portal use case below.

get-object-rights args: entity-schema-name (required), grantee (optional), include-connected (optional).
- With no grantee it lists EVERY role's rights on the object; with a grantee it reports just that role.
- An object not administered by operation permissions is available to all INTERNAL users only:
  external/portal users are DENY-BY-DEFAULT and reach an object only through an explicit grant. So with a
  grantee, a not-administered object is LISTED as still lacking access (never counted as covered). The
  tools report the rights layer and cannot infer portal reachability by themselves.
- include-connected also reads the object's OWN lookup objects (inherited BaseEntity audit lookups such
  as CreatedBy/ModifiedBy are skipped). Security and system objects — SysAdmin*, SysUser*, SysSchema*,
  SysPackage*, SysSettings* and *Right/*Rights — are never part of the connected set; both tools name them
  in a warning. With a grantee it lists the objects that role cannot READ. READ is the bar on every object:
  it is what makes a record and its lookup values visible, and what set-object-rights grants on connected
  lookups by default. The operations each object holds are printed per object — do not add create/edit
  to an object just because it shows read only.
- Read-only; use it to see current access and to VERIFY a set-object-rights change. It FAILS
  (success=false) when the root object is not found or cannot be read. A connected object that cannot be
  read, or a connected set that cannot be enumerated, is reported as UNVERIFIED and the all-clear is
  withheld — an unread object is unknown, not covered, and for the portal case it can still leave the
  section empty for external users.

set-object-rights args: entity-schema-name + grantee (required); operations=read,create,edit,delete for
the ROOT object (default read,create,edit — delete is NOT granted unless you pass it); revoke=true to
remove; include-connected=true to fan out to the object's own lookup objects (security/system objects
excluded, as above), which get connected-operations (default READ only on a grant — create/edit are never
fanned out to shared lookups implicitly); disable-operation-permissions (see below); --confirm on the CLI.
On MCP the Destructive flag is the only gate and the write applies WITHOUT a preview, so read the target
set first with get-object-rights include-connected. An unknown or misspelled argument name is refused
before any write.
- Failures never report success: a root object that is not found fails (nothing was written); if the
  connected objects cannot be enumerated nothing is written and the call fails; when the root write fails
  the connected objects are not attempted; a connected object that fails is named and the rest are still
  attempted (the call fails); a connected lookup that is not found only warns.
- revoke with include-connected leaves the connected lookups UNTOUCHED unless connected-operations names
  what to revoke there — the read-only grant default must not strip READ from shared lookups that other
  sections of the same audience still need.
- Read-modify-write: the per-role grid is read, the grantee's row is added/updated (or removed when a
  revoke empties it — refused when it is the object's last row, see below), and the object is saved.
- Granting to an object that does not yet use operation permissions TURNS THEM ON — an access NARROWING for
  every other internal role. The tool reports it per object, connected lookups included, so a fan-out that
  turns operation permissions on for a SHARED lookup narrows that lookup system-wide and says so. Creatio
  may also add an "All employees" row at that point (observed on the stands used to build these tools; not
  a documented contract), which would keep internal users' access. Do not assume either way: read the
  object back with get-object-rights. For EXCLUSIVE access — only the grantee — inspect the "All employees"
  row after the first grant and revoke or narrow it.
- A revoke only narrows. A revoke that would remove the object's LAST rights row is REFUSED (writes
  nothing, fails) because the only end states are "reachable by nobody" or — with operation permissions
  turned off — "available to ALL internal users", an access WIDENING. Pass disable-operation-permissions
  only when widening to every internal user is the intent; it applies to the ROOT object only, never to a
  connected lookup. To empty the row, revoke every operation the role holds (operations=read,create,edit,
  delete). Whether a revoke hits the last row depends on the other rows present (for example an
  "All employees" row), so read the object back with get-object-rights before and after.
- Read-modify-write is last-writer-wins: a change another client saves between the read and the save is
  overwritten. Read the result back when concurrent edits are possible.
- Idempotent: re-applying the same grant/revoke is safe, and the connected set is re-resolved on every
  call, so a lookup added to the object later is picked up by a re-run. It does NOT change column
  permissions.

Use cases (all one general capability):
- Grant or revoke any role's object access — the general audit-and-fix use.
- Make a Freedom PORTAL section's object available to external users: grant
  grantee=720b771c-e7a7-4f31-9cfb-52cd21c3739f (All external users) operations=read with
  include-connected, so the object and its lookups are readable by portal users (the lookups get read by
  default). PRECONDITION — the fan-out gives the WHOLE external audience READ on ANY record of every
  lookup, limited only by record-level rights, and turns operation permissions ON for a lookup that is not
  administered yet. So first run get-object-rights entity-schema-name=<root> include-connected=true to list
  the lookups it will touch, and confirm with the user every lookup that holds internal or personal data
  (Contact, Account, Employee, custom objects) or is not administered yet. Where a lookup must not be
  exposed, grant the root WITHOUT include-connected and grant only the approved lookups individually. Pin operations=read on the root explicitly: its default also grants create and edit to the whole
  external audience. An object external users cannot read is invisible to them even when the
  section and page exist. A lookup to a security/system object (for example SysAdminUnit) is NOT granted
  by the fan-out; decide with the user whether the whole external audience may read it before granting it
  as a root. The surrounding portal-section steps are `related-page-binding` (bind the page
  as portal) and `workplaces` (add the section to an external workplace).
- Enable operation permissions on an object from scratch by granting the first role.

Where the rights live: SysSchemaOperationRight (per role, per operation), served by the native
RightManagementService. get-object-rights reads it for you — do not query the table directly. For the
"is this object administered, and does this role hold a grant" decision, get-object-rights /
RightManagementService is authoritative; get-entity-schema-properties reports DESIGN-TIME schema metadata
that can disagree with it, so use that tool to inspect the schema and to drive the connected-object
fan-out, not to settle that decision.

Scope boundary: object operation permissions for a role on an object (and, with include-connected, its
own lookup objects). NOT column permissions, NOT record-level rights, NOT role/user provisioning.

<!-- Version boundary: set-object-rights / get-object-rights ship in clio
<SET-OBJECT-RIGHTS-CLIO-VERSION-TBD>. Replace this placeholder with the released clio version before
merging (merging publishes; an unresolved boundary is refused by the producer contract suite). Until
then this article stays on a draft PR. -->


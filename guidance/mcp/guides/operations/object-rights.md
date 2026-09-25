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
  as CreatedBy/ModifiedBy are skipped). With a grantee this lists the objects where that role does not
  hold the full read/create/edit triple — the tool's "has access" bar. A deliberate read-only grant (the
  portal case below) is therefore EXPECTED to appear in that list; do not add create/edit to clear it.
- Read-only; use it to see current access and to VERIFY a set-object-rights change. Verification has
  limits: the connected fan-out is best-effort, so a schema that cannot be read makes BOTH tools warn and
  fall back to the ROOT object alone, and get-object-rights skips objects it cannot read (its all-clear
  then reads "on every object that could be read"). An unread object is UNKNOWN, not verified — for the
  portal case it can still leave the section empty for external users.

set-object-rights args: entity-schema-name + grantee (required); operations=read,create,edit,delete for
the ROOT object (default read,create,edit — delete is NOT granted unless you pass it); revoke=true to
remove; include-connected=true to fan out to the object's own lookup objects, which get
connected-operations (default READ only — create/edit are never fanned out to shared lookups implicitly);
disable-operation-permissions (see below); --confirm on the CLI (on MCP the Destructive flag is the gate).
An unknown or misspelled argument name is refused before any write. A root object that is not found fails
(nothing was written); a connected lookup that is not found only warns.
- Read-modify-write: the per-role grid is read, the grantee's row is added/updated (or removed when a
  revoke empties it), and the object is saved.
- Granting to an object that does not yet use operation permissions TURNS THEM ON — an access NARROWING for
  every other internal role, which the tool reports. Creatio then also grants "All employees" by default so
  internal users keep access.
- A revoke only narrows. A revoke that would remove the object's LAST rights row is REFUSED (writes
  nothing, fails) because the only end states are "reachable by nobody" or — with operation permissions
  turned off — "available to ALL internal users", an access WIDENING. Pass disable-operation-permissions
  only when widening to every internal user is the intent. The auto-granted "All employees" row usually
  keeps the object administered; read the result back with get-object-rights.
- Idempotent: re-applying the same grant/revoke is safe. It does NOT change column permissions.

Use cases (all one general capability):
- Grant or revoke any role's object access — the general audit-and-fix use.
- Make a Freedom PORTAL section's object available to external users: grant
  grantee=720b771c-e7a7-4f31-9cfb-52cd21c3739f (All external users) operations=read with
  include-connected, so the object and its lookups are readable by portal users (the lookups get read by
  default). Pin operations=read on the root explicitly: its default also grants create and edit to the whole
  external audience. An object external users cannot read is invisible to them even when the
  section and page exist. The surrounding portal-section steps are `related-page-binding` (bind the page
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


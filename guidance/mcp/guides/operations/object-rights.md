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
- An object not administered by operation permissions is reported as available to all. That holds for
  INTERNAL users only: external/portal users are DENY-BY-DEFAULT and reach an object only through an
  explicit grant, so "not administered" must be read as "NOT reachable by external users". These tools
  report the rights layer and cannot infer portal reachability by themselves.
- include-connected also reads the object's OWN lookup objects (inherited BaseEntity audit lookups such
  as CreatedBy/ModifiedBy are skipped). With a grantee this lists the objects that role still lacks
  access to.
- Read-only; use it to see current access and to VERIFY a set-object-rights change.

set-object-rights args: entity-schema-name + grantee (required); operations=read,create,edit,delete
(default read,create,edit — delete is NOT granted unless you pass it); revoke=true to remove;
include-connected=true to fan out to the object's own lookup objects; --confirm on the CLI (on MCP the
Destructive flag is the gate).
- Read-modify-write: the per-role grid is read, the grantee's row is added/updated (or removed when a
  revoke empties it), and the object is saved.
- Granting to an object that does not yet use operation permissions TURNS THEM ON. Creatio then also
  grants "All employees" by default so internal users keep access. The tool does not turn operation
  permissions back OFF on a revoke (the "Use operation permissions" toggle is a separate manual step).
- Idempotent: re-applying the same grant/revoke is safe. It does NOT change column permissions.

Use cases (all one general capability):
- Grant or revoke any role's object access — the general audit-and-fix use.
- Make a Freedom PORTAL section's object available to external users: grant
  grantee=720b771c-e7a7-4f31-9cfb-52cd21c3739f (All external users) operations=read with
  include-connected, so the object and its lookups are readable by portal users. Pin operations
  explicitly: the default also grants create and edit, which on a shared lookup would hand the whole
  external audience write access. An object external users cannot read is invisible to them even when the
  section and page exist. The surrounding portal-section steps are `related-page-binding` (bind the page
  as portal) and `workplaces` (add the section to an external workplace).
- Enable operation permissions on an object from scratch by granting the first role.

Where the rights live: SysSchemaOperationRight (per role, per operation), served by the native
RightManagementService. get-object-rights reads it for you — do not query the table directly, and do
not self-serve the administration flag from get-entity-schema-properties: those flags are design-time
schema metadata, reliable only for the connected-object fan-out. get-object-rights /
RightManagementService is authoritative for the granted/administrated decision.

Scope boundary: object operation permissions for a role on an object (and, with include-connected, its
own lookup objects). NOT column permissions, NOT record-level rights, NOT role/user provisioning.

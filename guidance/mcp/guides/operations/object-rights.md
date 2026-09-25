clio MCP object-rights guide

Manage OBJECT operation permissions — who may read/create/edit/delete ANY record of an entity (the
SysSchemaOperationRight / "Object permissions" layer) — for ANY role, with two tools, NOT with
hand-written ESQ into the rights tables:
- get-object-rights — read the per-role operation permissions of an object.
- set-object-rights — grant or revoke a role's operations. DESTRUCTIVE — writes immediately, no publish
  step. Whether users who are already logged in see the change without a new session is NOT verified,
  so check the effect as a user who logged in AFTER the change.

Access to ONE specific record (or dashboard) is a different layer — `get-guidance name=record-rights`.

grantee is a SysAdminUnit id (a role or user id). Names are NOT unique — resolve a name to its id
yourself (e.g. execute-esq on SysAdminUnit by Name), the tools take the id.

get-object-rights args: entity-schema-name (required), grantee (optional), include-connected (optional).
- With no grantee it lists EVERY role's rights on the object; with a grantee it reports just that role.
  Internal roles holding the "…any data" system operations can reach records beyond what this reports —
  see `get-guidance name=entity-operation-access`.
- It reports facts per object — the operations each role (or the grantee) holds, "NO object operations
  granted", or "not administered by operation permissions" — and draws no verdict.
- An object not administered by operation permissions is available to all INTERNAL users only:
  external/portal users are DENY-BY-DEFAULT and reach an object only through an explicit grant.
- include-connected also reads the object's OWN lookup objects (inherited BaseEntity audit lookups such
  as CreatedBy/ModifiedBy are skipped). Security and system objects — SysAdmin*, SysUser*, SysSchema*,
  SysPackage*, SysSettings* and *Right/*Rights — are never part of the connected set; both tools name them
  in a warning.
- Read-only; use it to see current access and to verify a set-object-rights change. It FAILS
  (success=false) when the root object is not found or cannot be read; a connected object that cannot be
  read, or a connected set that cannot be enumerated, is reported with a warning.

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
- Make a Freedom PORTAL section's object available to external users — `get-guidance name=portal-sections`
  owns that flow.
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


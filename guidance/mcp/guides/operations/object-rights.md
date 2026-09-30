clio MCP object-rights guide

Manage OBJECT operation permissions — who may read/create/edit/delete ANY record of an entity (the
SysEntitySchemaOperationRight / "Object permissions" layer) — for ANY role, with two tools, NOT with
hand-written ESQ into the rights tables:
- get-object-rights — read the rows of an object's operation permissions. Read-only.
- set-object-rights — grant or revoke operations for ONE role on ONE object. DESTRUCTIVE: it writes in
  one call, with no publish step, and the change reaches users who are already logged in immediately.

Access to ONE specific record (or dashboard) is a different layer — `get-guidance name=record-rights`.

grantee is a SysAdminUnit id (a role or user id). Names are NOT unique — resolve a name to its id
yourself (e.g. execute-esq on SysAdminUnit by Name); the tools take the id.

## How the platform decides — read this before any write
- The rows are a PRIORITY LIST, not a union of flags. Position 0 is the highest. A user who is in several
  roles gets the operations of the HIGHEST MATCHING ROW, and that row decides per row, not per operation:
  a row with read only DENIES create/edit/delete to its members even when a lower row grants them. So:
  - a row with no operations is an explicit DENY for its members, not "nothing";
  - a row below a broader role can be shadowed: with `[0] All employees: read` and
    `[1] Sales managers: read/create/edit`, a sales manager can still only read;
  - removing a row lets the next matching row decide, which can WIDEN access.
- The "Use operation permissions" switch. While an object is NOT administered by operation permissions,
  company employees have full access (the rows are ignored), EXTERNAL users have NO access, and technical
  users follow the rows. External / portal users are therefore deny-by-default: they reach an object only
  through an explicit grant.
- Turning the switch ON is a narrowing: from then on only the rows decide, for every role.
- Internal roles holding the "…any data" system operations reach records whatever the object rows say —
  see `get-guidance name=entity-operation-access`.

Evidence: the priority rule is Creatio Academy's (a user in several roles gets the permissions of the
highest role in the list). It was reproduced per row as a non-admin internal user on a Creatio 8.3.4 stand
(2026-09-29), with the switch rules as the designer states them, the change reaching a logged-in session
at once, and a denied read returning zero rows.

## get-object-rights
Args: entity-schema-name (required), grantee (optional), include-connected (optional).
- Lists the rows in priority order, each with its [position], and states the priority rule. Per object
  it reports one of: administered, with the rows; administered with NO rows (only holders of the "…any
  data" system operations reach it); or NOT administered — then it also lists the rows that would start
  to decide if operation permissions were turned on.
- With grantee it shows that role's row (or "has NO row") and the rows ABOVE it: for a user who is also
  in one of those roles, those rows decide first.
- include-connected also reads the object's OWN lookup objects (inherited BaseEntity audit lookups such
  as CreatedBy/ModifiedBy are skipped). Security and system objects — SysAdmin*, SysUser*, SysSchema*,
  SysPackage*, SysSettings*, SysLic*, SysProcess*, Vw* and *Right/*Rights — are never listed as connected
  objects; they are named in a warning. This is the DISCOVERY step before granting.
- It reports facts and draws no verdict about who can actually reach the object. It FAILS
  (success=false) when the object is not found or cannot be read; a connected object that cannot be read,
  or a connected set that cannot be enumerated, is reported with a warning — that object is NOT verified.

## set-object-rights
Args: entity-schema-name + grantee + operations (all REQUIRED); revoke; enable-operation-permissions;
disable-operation-permissions; preview. On the CLI, --confirm applies without a prompt.
- ONE object per call. It never touches the object's lookups: each lookup is its own call.
- operations=read,create,edit,delete names exactly what is granted or revoked. It is REQUIRED on every
  call, grant and revoke — nothing is granted by default. Pass only what the scenario needs.
- On MCP the call applies the change. The host's approval of the call is the confirmation, and the
  arguments name the whole effect: the object, the role, the operations, and whether the switch turns on
  or off. preview=true writes nothing and shows the planned change, or why it would be refused.
- An unknown or misspelled argument name is refused before any read or write. The grantee must exist in
  SysAdminUnit.

Grant:
- A role with no row gets a new row at the LOWEST priority, as in the designer. The result names the rows
  ABOVE it: for a user who is also in one of those roles, that higher row decides, so the grant does
  nothing for that user.
- Every INTERNAL user is in All employees. So when an "All employees" row sits above the grantee's row,
  the grant changes nothing for internal users — they get what the All employees row says. The tool never
  reorders rows: a grant that must take effect for internal users needs the grantee's row moved ABOVE the
  All employees row in the Object permissions designer. Tell the developer BEFORE the write.
- External users are not in All employees, so a grant to All external users (or another external role)
  is not shadowed by the All employees row.
- A role that has a row gets the named operations added to it; the row keeps its position.
- A grant on an object that is NOT administered is REFUSED (nothing is written) unless
  enable-operation-permissions is passed. The refusal names the rows that would start to decide. With the
  flag the switch turns ON, and so that internal users keep their access the same save keeps the
  object's "All employees" row, or adds one with read/create/edit/delete below the stored rows when the
  object has stored rows but none for All employees. Stored rows ABOVE that row then restrict their
  members — the result names each of them.
- EXCLUSIVE access (only the grantee's members): narrowing the All employees row while it sits above the
  grantee's row denies the grantee's internal members too. It needs the grantee's row above All employees
  first (the designer), then a revoke on the All employees row.
- enable-operation-permissions is valid only on a grant; disable-operation-permissions only on a revoke.

Revoke:
- A revoke clears the named operations on the grantee's row and KEEPS the row. For a user whose highest
  matching row it is, the cleared operations are then DENIED — even if a lower row grants them. No row is
  ever removed; removing a role's row is done in the designer.
- A revoke on an object that is NOT administered is REFUSED: company employees reach it whatever its rows
  say. To limit access, first turn operation permissions on (a grant with enable-operation-permissions),
  then revoke from the "All employees" row what employees must not have. A role that must keep MORE than
  employees needs its row above the All employees row — the designer sets the order.
- A revoke that would leave the object with no row that grants any operation is REFUSED. Passing
  disable-operation-permissions instead turns operation permissions OFF: the object becomes available to
  ALL internal users (and closed to external ones) — an access WIDENING. Pass it only when that is the
  intent. The rows are kept and apply again if the switch is turned back on.
- A revoke from a role that has no row, or of operations the row does not hold, changes nothing.

Refused as well: a grantee with more than one row on the object (which one decides depends on the other
rows) — remove the duplicates in the designer, then re-run.

Results:
- The object is read back and compared with the plan, row by row. A row this call writes that is missing
  from the read-back, or a switch that is not as planned, FAILS the call. A read-back that itself fails
  after the save reports "saved, but NOT verified" (the call fails) — read the object with
  get-object-rights before retrying.
- A save that reports an error may still have been committed: the object is read back, and the call
  succeeds with a warning only when the read-back shows the planned change.
- Re-running the same call is safe: it reports no change.
- Read-modify-write is last-writer-wins: a change another client saves between the read and the save is
  overwritten. It does NOT change column or record permissions.

## Give a role access to an object and its lookups
1. Read: get-object-rights entity-schema-name=<Object> grantee=<role id> include-connected=true. Note,
   per object, whether it is administered, the grantee's row, and the rows above it.
2. Ask the developer BEFORE any write. Show every object you propose to change, the operations for each,
   and, per object, whether operation permissions turn ON (and which rows then start to decide) and which
   rows above the grantee's would shadow the grant. Two facts to say out loud:
   - read on a SHARED lookup (Contact, Account, price lists and the like) is read on the WHOLE table for
     that role, not only on the rows this app uses;
   - turning operation permissions on for a shared lookup makes its rows decide for every role,
     system-wide, wherever that lookup is used.
   Grant only the objects the developer approves; a security or system object is granted only when the
   developer asks for that object by name.
3. Make one set-object-rights call per approved object, each with its own operations — typically the
   scenario's operations on the object itself and read on its lookups — and enable-operation-permissions
   where the object is not administered.
4. Verify with the same get-object-rights read.

## Verify the effect
- Re-read with get-object-rights. It shows the stored rows, not what a given user can do.
- To prove what a user can do, test AS that user (a non-admin, not a holder of the "…any data" system
  operations). A DataService read that is denied returns success with ZERO rows, not an error — "the query
  succeeded" does not prove read access. A denied create fails with a security error.

Use cases (all one general capability):
- Grant or revoke any role's object access — the general audit-and-fix use.
- Make a Freedom PORTAL section's object available to external users (the `All external users` role):
  they are deny-by-default, so the object and each lookup they must see need an explicit grant.
- Enable operation permissions on an object from scratch: the first grant with
  enable-operation-permissions.

Where the rights live: SysEntitySchemaOperationRight (one row per role, per object), served by the native
RightManagementService. get-object-rights reads it for you — do not query the table directly. For the
"is this object administered, and which rows decide" question, get-object-rights /
RightManagementService is authoritative; get-entity-schema-properties reports DESIGN-TIME schema metadata
that can disagree with it, so use that tool to inspect the schema, not to settle that question.

Scope boundary: object operation permissions for a role on one object. NOT column permissions, NOT
record-level rights, NOT role/user provisioning.

<!-- Version boundary: set-object-rights / get-object-rights ship in clio
<SET-OBJECT-RIGHTS-CLIO-VERSION-TBD>. Replace this placeholder with the released clio version before
merging (merging publishes; an unresolved boundary is refused by the producer contract suite). Until
then this article stays on a draft PR. -->

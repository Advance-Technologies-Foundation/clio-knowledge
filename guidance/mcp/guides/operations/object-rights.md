clio MCP object-rights guide

Owns: the row priority rule of object operation permissions; the set-object-rights rules for a grant, a
revoke, and turning operation permissions on or off; and the list → ask → grant flow for an object and
its lookups.

Manage OBJECT operation permissions — who may read/create/edit/delete ANY record of an entity (the
SysEntitySchemaOperationRight / "Object permissions" layer) — for ANY role, with two tools, NOT with
hand-written ESQ into the rights tables:
- get-object-rights — read the rows of an object's operation permissions. Read-only.
- set-object-rights — grant or revoke operations for ONE role on ONE object, or turn the object's
  operation permissions on or off. DESTRUCTIVE: it writes in one call, with no publish step, and the
  change reaches users who are already logged in immediately.

Version boundary: get-object-rights and set-object-rights require clio <SET-OBJECT-RIGHTS-CLIO-VERSION-TBD>
or later. On an older clio neither tool exists: change object permissions in the Object permissions
designer instead.

Access to ONE specific record (or dashboard) is a different layer — `get-guidance name=record-rights`.

grantee is a SysAdminUnit id (a role or user id). Names are NOT unique — resolve a name to its id
yourself (e.g. execute-esq on SysAdminUnit by Name); the tools take the id. When several rows match,
you MUST NOT pick one: show each candidate's id, name and type, ask the developer which one is meant, and
name the chosen id and type in the preview you show. Fixed platform roles:
`All employees` = `a29a3ba5-4b0d-de11-9a51-005056c00008` (every internal user is in it) and
`All external users` = `720b771c-e7a7-4f31-9cfb-52cd21c3739f` (the external / portal audience).

entity-schema-name is the object's SCHEMA name. The word a developer uses for an object can be its title
or its schema name, and the two can differ: the object titled "Feature" can be schema Specification,
while schema Feature is titled "Creatio functionality". find-entity-schema matches schema names only;
get-entity-schema-properties shows an object's title. You MUST name the object by schema name AND title
in what you show the developer — Feature ("Creatio functionality") — and when the developer's word is not
exactly that object's title, you MUST NOT decide which object is meant: ask before reading or writing,
because the same word can be another object's title.

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
- Turning the switch ON makes the rows decide for every role, and rows stored earlier (kept while the
  switch was off) come back and decide again. For INTERNAL users that narrows access: they keep only what
  the rows give them. For EXTERNAL users it can WIDEN access: every stored row that grants an external
  role (All external users, another portal role, a portal user) starts to apply. Before an enable you
  MUST name each such row to the developer.
- Internal roles holding the "…any data" system operations reach records whatever the object rows say.

Evidence: the priority rule is Creatio Academy's (a user in several roles gets the permissions of the
highest role in the list). It was reproduced per row as a non-admin internal user on a Creatio 8.3.4 stand
(2026-09-29), with the switch rules as the designer states them, the change reaching a logged-in session
at once, and a denied read returning zero rows.

## get-object-rights
Args: entity-schema-name (required), grantee (optional), include-connected (optional).
- Lists the rows in priority order, each with its [position], and states the priority rule. Per object
  it reports one of: administered, with the rows; administered with NO rows (only holders of the "…any
  data" system operations reach it); or NOT administered — then it lists EVERY row that would start to
  decide if operation permissions were turned on, and says when an enable would add an All employees row.
- For an object with no stored rows the read itself shows `[0] All employees: read/create/edit/delete`.
  That row is synthesized by the service, not stored; an enable stores it.
- With grantee it shows that role's row and the rows ABOVE it: for a user who is also in one of those
  roles, those rows decide first. When the role has NO row it lists every row: a new row goes below all
  of them.
- include-connected also reads the object's OWN lookup objects. It does NOT list lookups the object
  inherits (CreatedBy/ModifiedBy → Contact and the like), detail (child) objects, or their lookups — read
  those by name. Security and system objects — SysAdmin*, SysUser*, SysSchema*, SysPackage*, SysSettings*,
  SysLic*, SysProcess*, Vw* and *Right/*Rights — are never listed as connected objects; they are named in
  a warning. A read that times out stops the listing and names the objects not read yet; so does, on MCP,
  a listing past 90 s (each read there is one attempt of at most 30 s) — read those objects one by one.
- It reports facts and draws no verdict about who can actually reach the object. It FAILS
  (success=false) when the object is not found or cannot be read; a connected object that cannot be read,
  or a connected set that cannot be enumerated, is reported with a warning — that object is NOT verified.

## set-object-rights
Args: entity-schema-name (always); grantee + operations for a grant or a revoke; revoke;
enable-operation-permissions; disable-operation-permissions; preview.
- ONE object per call. It never touches the object's lookups: each lookup is its own call.
- operations=read,create,edit,delete names exactly what is granted or revoked. It is REQUIRED on every
  grant and revoke — nothing is granted by default, and a grant or revoke without it (or with a value
  that names no operation) is refused before any read or write. Pass only what the scenario needs.
- The switch and the rows change separately: a revoke never turns operation permissions off, and turning
  them on or off alone changes no row (see "The switch alone").
- You MUST ask the developer in chat before every write, even a single grant or revoke. On MCP the host
  approves the clio-run call that carries set-object-rights, and an auto-approve mode skips that
  approval, so it is not the developer's yes. The arguments name the object, the role, the operations
  and the switch; what comes with the change — the rows that start to decide, an added All employees
  row, the rows above the grantee's — is in the preview. So you MUST first call it with preview=true: it
  writes nothing and shows the planned change, or why the call would be refused (a refused preview
  answers success=false). Show those facts, get the yes, then make the same call without preview.
- On the CLI the same rules hold: run it with --preview first, ask in chat, and only then re-run it with
  --confirm, which applies the change without a prompt.
- An unknown or misspelled argument name is refused before any read or write. The grantee must exist in
  SysAdminUnit.

Grant:
- A role with no row gets a new row at the LOWEST priority, as in the designer. The result names the rows
  ABOVE it: for a user who is also in one of those roles, that higher row decides, so the grant does
  nothing for that user.
- Every INTERNAL user is in All employees. So when an "All employees" row sits above the grantee's row,
  the grant changes nothing for internal users — the highest matching row at or above All employees
  decides for them. The tool never reorders rows: a grant that must take effect for internal users needs
  the grantee's row moved ABOVE the All employees row in the Object permissions designer. You MUST tell
  the developer so before the write.
- External users are not in All employees, so a grant to All external users (or another external role)
  is not shadowed by the All employees row.
- A role that has a row gets the named operations added to it; the row keeps its position.
- A grant on an object that is NOT administered is REFUSED (nothing is written) unless
  enable-operation-permissions is passed. The refusal names the rows that would start to decide. With the
  flag the switch turns ON, and the same save sets the All employees row by the state it meets:

  | The object before the enable | The All employees row after it |
  |---|---|
  | no stored rows (the read shows a synthesized `[0] All employees: read/create/edit/delete`) | stored as the read shows it: read/create/edit/delete at position 0 |
  | stored rows that include an All employees row | kept AS STORED: if it grants less than read/create/edit/delete, internal users get only that |
  | stored rows but none for All employees | added below them with read/create/edit/delete |

  When the grantee is All employees itself, the grant changes that row like any grantee's: the named
  operations are added to it, and where the object has stored rows but no All employees row, its new row
  gets exactly the named operations — no second row is added.
  Stored rows ABOVE the All employees row restrict their members — the preview and the result name each
  of them. After an enable, a new grantee row always sits BELOW the All employees row.
- For an INTERNAL grantee, do not turn operation permissions on just to grant: while the object is not
  administered its members already reach it, and after the enable the new row sits below All employees.
  Enable for an external grantee, or when the developer wants that object restricted.
- EXCLUSIVE access (only the grantee's members): narrowing the All employees row while it sits above the
  grantee's row denies the grantee's internal members too. It needs the grantee's row above All employees
  first (the designer), then a revoke on the All employees row.
- enable-operation-permissions goes with a grant or alone; disable-operation-permissions always goes alone;
  neither goes with a revoke.

Revoke:
- A revoke clears the named operations on the grantee's row and KEEPS the row. For a user whose highest
  matching row it is, the cleared operations are then DENIED — even if a lower row grants them. No row is
  ever removed; removing a role's row is done in the designer.
- A revoke on an object that is NOT administered is REFUSED: company employees reach it whatever its rows
  say. To limit access, first turn operation permissions on with a grant to All employees
  (grantee=a29a3ba5-4b0d-de11-9a51-005056c00008, operations=read,create,edit,delete,
  enable-operation-permissions — name all four: where the object has stored rows but no All employees
  row, the new row gets only the operations named, see Grant), then revoke from that row what employees must not have. A role that must
  keep MORE than employees needs its row above the All employees row — the designer sets the order.
- A revoke that would leave the object with no row that grants any operation is REFUSED. A revoke never
  turns operation permissions off; to turn them off instead, make the disable call (The switch alone).
- A revoke from a role that has no row, or of operations the row does not hold, changes nothing.

The switch alone (no grantee, no operations, no revoke):
- disable-operation-permissions alone turns operation permissions OFF and keeps every row exactly as it
  is, operations included, as the designer's switch does; the rows apply again when it is turned back on.
  The object becomes available to ALL internal users — an access WIDENING — and EXTERNAL users lose the
  access its rows gave them (they have none while it is off); technical users keep following the rows.
  Before a disable you MUST name to the developer each row of an external role that stops applying, and
  you MUST NOT disable unless that widening is the developer's intent.
- enable-operation-permissions alone turns operation permissions ON with the stored rows as they are,
  under the same All employees rule as an enabling grant (the table under Grant). Every stored row starts
  to decide again — name the rows of external roles, as for any enable. It is refused when no row would
  grant any operation: grant operations to a role in the same call instead.
- A switch call on a switch already in place reports no change.

Refused as well: a grantee with more than one row on the object (which one decides depends on the other
rows) — remove the duplicates in the designer, then re-run.

Results:
- The object is read back and compared with the plan, row by row. A row this call writes that is missing
  from the read-back, or a switch that is not as planned, FAILS the call. A read-back that itself fails
  after the save reports "saved, but NOT verified" (the call fails) — read the object with
  get-object-rights before retrying.
- A save that reports an error may still have been committed: the object is read back, and the call
  succeeds with a warning only when the read-back shows the planned change.
- Re-running a call that already landed is safe: it reports no change.
- The save is sent once, with no automatic retry. A save that did not answer in time may still land after
  the read-back: its failure says so — re-read the object with get-object-rights before retrying or
  reporting a failure.
- Read-modify-write is last-writer-wins: a change another client saves between the read and the save is
  overwritten. It does NOT change column or record permissions.

## Give a role access to an object and its lookups
1. Read: get-object-rights entity-schema-name=<Object> include-connected=true (and grantee=<role id> to
   see that role's row and the rows above it). Add any object the listing does not include — a lookup the
   object inherits, a detail object — by reading it by name.
2. Decide per object: the operations, and whether it needs enable-operation-permissions (see Grant — for
   an internal grantee an object that is not administered usually needs no grant at all).
3. Preview each planned call (preview=true) and show the developer, per object: the operations, whether
   operation permissions turn ON and which rows then start to decide — naming each row of an external
   role among them, since it opens the object to that audience — and which rows above the grantee's
   would shadow the grant. Two facts to say out loud:
   - read on a SHARED lookup (Contact, Account, price lists and the like) is read on the WHOLE table for
     that role, not only on the rows this app uses;
   - turning operation permissions on for a shared lookup makes its rows decide for every role,
     system-wide, wherever that lookup is used.
   Grant only the objects the developer approves. You MUST NOT propose a security or system object (the
   names listed under get-object-rights). Grant one only when the developer named that object first, and
   before asking say what it opens — for example, read on SysAdminUnit lets the grantee read every user
   and role.
4. Make one set-object-rights call per approved object, each with its own operations — typically the
   scenario's operations on the object itself and read on its lookups.
5. Verify with the same get-object-rights read.

## Verify the effect
- Re-read with get-object-rights. It shows the stored rows, not what a given user can do.
- To prove what a user can do, test AS that user (a non-admin, not a holder of the "…any data" system
  operations). A DataService read that is denied returns success with ZERO rows, not an error — "the query
  succeeded" does not prove read access. A denied create fails with a security error.

Use cases (all one general capability):
- Grant or revoke any role's object access — the general audit-and-fix use.
- Make a Freedom PORTAL section's object available to external users (the `All external users` role):
  they are deny-by-default, so the object and each lookup they must see need an explicit grant. Read on
  a shared lookup (Contact, Account and the like) opens its WHOLE table to every external user; limiting
  them to their own records is record-level access (`get-guidance name=record-permissions`), not this
  tool.
- Enable operation permissions on an object from scratch: the first grant with
  enable-operation-permissions.
- Turn an object's operation permissions off and back on, keeping its rows: the switch alone.

Where the rights live: SysEntitySchemaOperationRight (one row per role, per object), served by the native
RightManagementService. get-object-rights reads it for you; you MUST NOT query the table directly. For the
"is this object administered, and which rows decide" question, get-object-rights /
RightManagementService is authoritative; get-entity-schema-properties reports DESIGN-TIME schema metadata
that can disagree with it, so use that tool to inspect the schema, not to settle that question.

Scope boundary: object operation permissions for a role on one object. NOT column permissions, NOT
record-level rights, NOT role/user provisioning.

clio MCP process-access-rights guide — the Change access rights element

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.

This article is the authoritative owner of the Creatio **Change access rights** element
(`changeAccessRights` / `ChangeAdminRightsUserTask`): its `accessRights` block, the permission entries,
the three grantee kinds, and the read-back contract. `process-modeling` owns the build lifecycle and
points here; `process-data-elements` owns the record `filter` shape this element consumes.

== Read this first: the element cannot tell you it did nothing ==
This element has **NO output parameters**. Nothing downstream can branch on whether permissions were
changed, and a run that changes nothing looks exactly like a run that worked. Its runtime silently
does NOTHING — no error, no log an agent can read back — at least in these cases:
  1. the element's record `filter` is empty (WHICH records to act on was never narrowed);
  2. `add` and `remove` are BOTH empty (there is no permission to apply);
  3. the target object does not use record permissions (`AdministratedByRecords` off).
Only case 3 is refused at build time. Cases 1 and 2 build green, save, publish and run — so a clean
build is NOT evidence that the element will do anything.
A FOURTH cause produces the same symptom with a different fix: an environment whose deployed
CrtProcessBuilder predates this element DISCARDS the whole `accessRights` block while deserializing and
still answers success, leaving the element unconfigured. A clio that carries the accessRights read-back
check detects this one for you: it reads the process back after the operation and warns when the block
did not land, or when the read-back could not be obtained at all. Treat either warning as "the
permissions were NOT changed". VERSION BOUNDARY: that check ships WITH the clio release that introduced
this element, so on an OLDER clio there is no such warning and its absence proves nothing. Unless you
know the clio you are running emits it, verify the outcome yourself with `describe-business-process`
before reporting a grant or revoke as applied. This matters most on a REVOKE: a
revocation that silently does nothing leaves privileges in place while reporting success.
Always supply a record `filter`, and always leave at least one entry in a collection.

== The accessRights block ==
Set it on a `changeAccessRights` element at create, or later with `modify-business-process`
`setElement`. `object` is required at create; on `setElement` an omitted `object` keeps the current one.

  "accessRights": {
    "object": "Order",                  // entity whose RECORD permissions change (required at create)
    "considerTimeInFilter": false,      // optional; the record filter's date/time flag
    "add":    [ /* entries to GRANT  */ ],
    "remove": [ /* entries to REVOKE */ ]
  }

One entry = one grantee and the operations it gains or loses:

  {
    "operations": ["read", "edit", "delete"],   // at least one; any subset
    "level": "permit",                          // ADD entries only (see below)
    "grantee": { "type": "role", "role": "..." }
  }

`grantee` is an OBJECT, not a string. `type` is the discriminator and decides which sibling key
carries the payload; the other two are ignored:

  { "type": "role",              "role":    "<role name | record id | [#Lookup…#] macro>" }
  { "type": "employee",          "contact": "<contact record id | [#Lookup…#] macro | any formula>" }
  { "type": "selectedEmployees", "filter":  { /* a Contact-rooted filter */ } }

A successful `remove` DELETES the matching record-right rows; it does not deny. Creatio record
permissions are grant-based and additive, so the grantee can still hold the operation through another
role's row or through the object's default rights. If the intent is to BLOCK rather than to un-grant, `level: "restrict"` is the only candidate mechanism —
but it is NOT a verified substitute for removal: it is enum-derived and UNOBSERVED, and the PROVENANCE
note under Levels records evidence pointing the other way (the platform captions that same value
"NotSet"). Do not swap a removal for a `restrict` entry on the assumption that it denies. Confirm on
your stand that it actually blocks the operation, and never report an access block as achieved on the
strength of a green build.

== Levels (add entries only) ==
  `permit`   — the default when `level` is omitted: the grantee gets the operation.
  `delegate` — "Permit with rights to delegate": the grantee may pass the right onward.
  `restrict` — an explicit deny (UNVERIFIED — see PROVENANCE below).
A `level` on a REMOVE entry is REFUSED, not ignored: the runtime never reads one there, so accepting
it would silently discard your intent.

PROVENANCE of `restrict`: it is enum-derived (`EntitySchemaRecordOperationRightLevel.Deny = 0`), not
observed — none of the seven captured designer specimens uses it, and the record-rights detail captions
that same value "NotSet". Verify it on your stand before relying on it as an access control.

== The three grantee kinds ==
- `role` — a user role. Accepts a role NAME (resolved through the platform role view; an ambiguous
  name is refused, so pass the id instead), a record id, or the `[#Lookup…#]` macro that describe
  echoes back. A user's own `SysAdminUnit` id is not a role and is refused. Same semantics as the
  Perform task performer's `role`.
- `employee` — one employee, identified by CONTACT. Accepts a contact record id or an echoed
  `[#Lookup…#]` macro (both are checked to exist, then stored as the macro with the record's name as
  the caption), or ANY other formula stored verbatim — a process-parameter meta-path, or
  `[#SysVariable.CurrentUserContact#]`. Contact NAMES are deliberately NOT resolved: duplicate names
  make that unsafe. Pass an id.
- `selectedEmployees` — every user whose CONTACT matches a filter. The filter is the ordinary filter
  shape (see `process-data-elements`) and its root is always `Contact`: omit `object` or set it to
  `Contact`; anything else is refused rather than silently ignored. SECURITY NOTE: the runtime
  evaluates this filter with record permissions DISABLED — it matches every contact the filter
  describes, regardless of what the user running the process can see. Scope it deliberately.

A `role` or `employee` grantee is backed by a generated element parameter (`Role<N>` / `Employee<N>`);
you never create those yourself. `selectedEmployees` needs no parameter.

The legacy `allRolesAndUsers` grantee is DESCRIBE-ONLY: shipped processes carry it and the runtime
honours it, but the current designer cannot create one and writing it is refused. Model it as
explicit role entries.

== Which records: the element's own filter ==
`accessRights` says WHO and WHAT; the element's separate `filter` block says WHICH records — it is a
sibling of `accessRights` on the element, not a key inside it, and it uses the shape
`process-data-elements` owns. It is the element's BLAST RADIUS: a filter that matches more records
than you intended changes permissions on all of them, and one that matches none changes nothing while
reporting success. To act on a single record, filter `Id` against a process parameter or a trigger
output.

  { "name": "GrantRights", "type": "changeAccessRights", "caption": "Grant rights",
    "accessRights": { "object": "Order", "add": [ { "operations": ["read"],
        "grantee": { "type": "role", "role": "Sales Department" } } ] },
    "filter": { "object": "Order", "conditions": [
        { "column": "Id", "comparison": "equal", "processParameter": "OrderIdParameter" } ] } }

Grant to the NARROWEST role that satisfies the request. `All employees` resolves and builds green, so an
example copied with only the object and filter substituted would ship a grant to the entire user base.

== Changing it later (setElement) ==
MUST, before you apply any of this to a live environment: a `remove` entry, a `[]` clear and an `object`
retarget all CHANGE OR DESTROY record permissions that people currently rely on, and the element reports
nothing at run time about what it did. Show the user the target object, the record `filter` that decides
WHICH records are affected, and every grantee with its operations and level, and get an explicit yes
before sending the operation — the same destructive-confirmation rule `process-modeling` states for
`removeElement` / `removeFlow` / `removeParameter`, which owns it in full.
`{ "op": "setElement", "elementName": "GrantRights", "elementUpdate": { "accessRights": { … } } }`
- A partial update: omitted fields keep their values.
- A supplied `add`/`remove` REPLACES that whole collection — its previous grantee parameters are
  removed. Replacement is the only way to remove an entry; there is no append or delete-one op.
  MUST: never build a replacement from a described element unless EVERY entry decoded. `describe` reports a
  stored-but-undecodable collection as `[]` and reports a legacy `FilterEdit` selected-employees entry
  WITHOUT its filter (see Read-back), so a naive describe -> modify -> setElement round trip deletes entries
  that were never read back, and can turn a scoped selected-employees grant into an unscoped one — an
  empty grantee filter matches every contact. Omit the collection to keep what is stored.
- `[]` clears a collection. Clearing one is safe only while the OTHER still holds an entry —
  clearing both leaves a permanently dead element that still reports success.
- On ANY object change, the FIRST configuration included, the stored record filter clears unless it
  already targets the incoming object. Re-issue `setFilter` in the same operations array.
- A present-but-blank `object` is refused; omit it to keep the current target.

== Read-back (describe-business-process) ==
The element returns its `accessRights` block: `object` + `objectSchemaUId` (`object` is null when the
stored UId resolves to no entity), `considerTimeInFilter`, and both entry collections with their
operations, level and grantee in the same wire shape you write. A role/employee grantee reports its
stored formula plus the stored caption in `display`, and an echoed `[#Lookup…#]` macro re-applies as
written. A `selectedEmployees` filter decodes when stored in the modern format; a legacy `FilterEdit`
value reports the entry without its filter. A stored-but-undecodable collection reports as an EMPTY
array — indistinguishable from a genuinely empty one, so do not treat `[]` as proof there is nothing
there. The legacy `allRolesAndUsers` kind is reported truthfully and refused if written back.

== What is refused at build ==
  - an object that does not use record permissions;
  - an unknown object, or a present-but-blank `object`;
  - `level` on a remove entry;
  - an empty `operations` list;
  - an unknown grantee `type`, or `allRolesAndUsers` on write;
  - an unknown or ambiguous role name; a contact id that matches no record;
  - a `selectedEmployees` filter rooted anywhere but `Contact`;
  - an `accessRights` block on an element that is not a Change access rights element.
Others exist (an entry with no `grantee`, an unknown `operations` or `level` token, a grantee type
missing its payload key), so treat this as the shape of what is checked rather than an exhaustive list.
The one thing it is safe to conclude: the two silent runtime no-ops at the top of this article are NOT
among the refusals — they build green.

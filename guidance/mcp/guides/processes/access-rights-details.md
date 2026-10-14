clio MCP process-access-rights-details guide — the Change access rights read-back check, its version boundaries, describe's full field shape, the provenance of restrict, and the evidence and reasoning behind the element's rules

A details article of the process guide set, reached through `process-access-rights`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the two VERSION BOUNDARIES of the Change access rights element
(which CrtProcessBuilder lands the `accessRights` block, and which clio tells you when it does not), of
when clio's read-back check runs on a `clearFilter` batch, of the field-by-field shape
`describe-business-process` returns for the block, of the PROVENANCE of the `restrict` level, and of
the evidence, platform mechanics and reasoning behind the element's rules, grouped by the core section
they explain: what a read-back cannot see, how `restrict` writes rows, why the record and grantee filters
are as wide as stated, how `allRolesAndUsers` differs from a `role` remove, why a supplied collection's
replacement cuts both ways, and why a `setFilter` before a retarget goes unwarned. Split out
of `process-access-rights`, which keeps the block and entry shapes, the grantee kinds, the levels, the
record filter, every refusal, what a supplied collection REPLACES, what a read-back proves, and every
confirmation duty; read that first. Nothing here relaxes a rule stated there.

== Version boundaries: whether the block lands, and whether you are told ==
VERSION BOUNDARIES - there are TWO, and they are different questions:
  - the CrtProcessBuilder deployed on the ENVIRONMENT decides whether the block lands at all. Anything
    older than `1.6.0.2` discards it - that is the first archive containing the element at all, and
    every earlier one, 1.6.0.1 included, drops the block while still answering success. Check with
    `list-packages` and read the CrtProcessBuilder row.
  - the CLIO you are running decides whether you are TOLD. The read-back check ships in the same
    release that starts bundling CrtProcessBuilder `1.6.0.2`, so a clio bundling an older archive
    predates the check and emits no warning - its silence proves nothing. `install-process-builder`
    installs whatever archive YOUR clio bundles, so a clio old enough to lack the check also installs
    a package old enough to discard the block.
  On a clio or an environment below those boundaries, do not treat the absence of a warning as
  evidence: read the process back with `describe-business-process mode=full` yourself.
MUST, owned by `process-access-rights`, which states it in full: that read-back proves only that the
`accessRights` block LANDED, never that a permission changed, so before you report a REVOKE as applied,
read the permissions on a record the filter matched with `get-record-rights`.

== PROVENANCE of restrict ==
MUST, owned by `process-access-rights`, which states it in full: `restrict` is DESTRUCTIVE - it
downgrades an existing Allow row for the grantee to Deny and, on a fresh insert, denies the two operations
you did not name - so weigh it like a `remove` entry and get the user's explicit yes before you apply it
to a live environment.
PROVENANCE of `restrict`: it is enum-derived (`EntitySchemaRecordOperationRightLevel.Deny = 0`), not
observed — none of the seven captured designer specimens uses it, and the record-rights detail captions
that same value "NotSet". Verify it on your stand before relying on it as an access control.

== When the read-back check runs on a clearFilter batch ==
MUST, owned by `process-access-rights`, which states it in full: a `clearFilter` on this element is gated
like any other change to it - before you apply the batch to a live environment, show the user the element
and the object whose filter is being cleared (plus every grantee the batch names, with its operations and
level) and get an explicit yes.
- A clio carrying the read-back check also reads the process back after an operations array that CONTAINS
  a `clearFilter` — a mixed batch counts, the check is keyed on the operation and not on the array being
  a single one — and warns on the resulting filter state of any Change access rights element among the
  cleared elements — clearing a record filter makes that element act on EVERY record
  of its object, so that batch is checked rather than waved through silently.

== Read-back: describe's full field shape ==
MUST, owned by `process-access-rights`, which states it in full: never build a replacement `add` or
`remove` from this read-back unless EVERY entry decoded - `[]` with a non-zero `addUnreadable` or
`removeUnreadable` count means UNKNOWN, not empty, and a supplied collection REPLACES the stored one.
The element returns its `accessRights` block: `object` + `objectSchemaUId` (`object` is null when the
stored UId resolves to no entity), `considerTimeInFilter`, and both entry collections with their
operations, level and grantee in the same wire shape you write. A role/employee grantee reports its
stored formula plus the stored caption in `display`, and an echoed `[#Lookup…#]` macro re-applies as
written.

== Read this first — evidence ==
For cases 1 and 3 of the silent no-ops, the build refusal and the runtime no-op are the same fact seen from
two sides: the build rejects the configuration precisely BECAUSE the runtime would exit on it.
The absent record filter, the wide state, is refused by nothing on the platform and reported by no output
parameter; a clio carrying the read-back check warns about it, and a clio predating that check says nothing
at all.
A `describe-business-process` read-back cannot see a record `filter` that matched zero records, a `remove`
whose grantee still holds the operation through another role or a default right, or an `employee` formula
that resolved to nobody.

== The accessRights block and Levels — platform mechanics ==
A `remove` entry is not a block because Creatio record permissions are grant-based and additive: the
grantee can still hold the operation through another role's row or through the object's default rights.
The `UseDenyRecordRights` system setting gates only record positioning, never whether a `restrict` rights
row is written, and a fresh insert writes one row per operation - the one you named at your
level, and the OTHER TWO at Deny.
The `permit` default for an omitted `level` is applied by the SERVER, which writes the permit value
explicitly onto the stored entry, so the platform enum's zero value named in the PROVENANCE note above is
never reached by omitting `level`.
The record-rights detail captions the `restrict` value "NotSet"; that is a UI rendering, not the storage.

== The three grantee kinds — reasoning ==
A `role` grantee has the same semantics as the Perform task performer's `role`.
The `selectedEmployees` grantee filter is evaluated with record permissions disabled, and so is the
element's OWN record filter, so neither filter is limited to records the running user could see. Beyond that
the two filters behave in OPPOSITE ways and must not be reasoned about by analogy: a record filter carrying an
`object` but no conditions is a silent no-op, while the total ABSENCE of a record filter is the wide state
that acts on every record (see "Which records" in `process-access-rights`).
A `selectedEmployees` grantee needs no generated element parameter, unlike `role` and `employee`.
The lock-a-record shape - `allRolesAndUsers` removing edit and delete, then an `add` granting them back to
the approver - is how the shipped approval processes are built, and so is the Academy reference example for
this element.
`allRolesAndUsers` is not interchangeable with a `role` entry on a remove because the two run different
platform operations: `allRolesAndUsers` drops every rights row for that operation, while a role drops only
that one role's row and leaves every individually granted user's access standing - so a role entry there
produces a process that reports success and does not lock the record.
`allRolesAndUsers` is refused on `add` because it carries no access level, and "grant to everyone" is not
a shape the platform models this way. A second `allRolesAndUsers` entry in the same collection is refused
too, matching the designer, which hides the option once that collection already has one.

== Which records — reasoning ==
The record filter is the element's BLAST RADIUS: a filter that matches more records than you intended
changes permissions on all of them, and one that matches none changes nothing while reporting success.
A record filter carrying an `"object"` but no conditions is also REFUSED at build by the current package, so a
caller writing through clio cannot reach that state, though a designer-built element can. That refusal is a
no-op guard - the same class as the non-administrated-object refusal - NOT a fail-open guard.
A `setFilter` sent before a retarget goes unwarned for this reason. `setFilter` is not re-read, because it
always carries a full `object` plus that filter's conditions as a replacement, so on its own it can only
leave the element narrowing. That holds only while nothing LATER in the same array retargets the element's
`object`: a retarget clears a filter that does not already target the incoming object, so
`[ {setFilter …}, {setElement {accessRights:{object:"Other"}}} ]` ends in the wide state with nothing
warned, because the read-back is keyed on `clearFilter`.
The rule to grant to the narrowest role exists because `All employees` resolves and builds green, so an
example copied with only the object and filter substituted would ship a grant to the entire user base.

== Changing it later (setElement) — reasoning ==
Both `add` and `remove` are on the confirmation list. A supplied `add` REPLACES the whole collection, so it
destroys every grant it does not restate, and it widens access to whoever it names. A supplied `remove`
replaces its collection the same way and drops any entry you do not restate, including one that was itself
revoking access - and dropping a `remove` entry BROADENS access exactly as dropping an `add` entry narrows
it, so the replacement hazard cuts both ways. A `restrict` entry and a `remove` entry both read to the user
as a revoke: `restrict` because it denies, `remove` because a deleted row reads as one even though it is not.

== Read-back and refusals — detail ==
A `selectedEmployees` filter decodes when stored in the modern format; a legacy `FilterEdit` value reports
the entry without its filter. The `allRolesAndUsers` kind is reported truthfully and can be written back on
a `remove` entry, so a read-back carrying one re-applies unchanged.
The one thing it is safe to conclude from the refusal list: of the three no-op causes at the top of
`process-access-rights`, only case 2 (`add` and `remove` both empty) is absent from the refusals and builds
green. Cases 1 and 3 ARE refused, as stated there.

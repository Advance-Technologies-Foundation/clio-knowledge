clio MCP process-access-rights-details guide — the Change access rights read-back check, its version boundaries, describe's full field shape and the provenance of restrict

A details article of the process guide set, reached through `process-access-rights`, which points here; `process-modeling` is the set's entry point.
This article is the authoritative owner of the two VERSION BOUNDARIES of the Change access rights element
(which CrtProcessBuilder lands the `accessRights` block, and which clio tells you when it does not), of
when clio's read-back check runs on a `clearFilter` batch, of the field-by-field shape
`describe-business-process` returns for the block, and of the PROVENANCE of the `restrict` level. Split out
of `process-access-rights`, which keeps the block and entry shapes, the grantee kinds, the levels, the
record filter, every refusal, what a supplied collection REPLACES, what a read-back cannot see, and every
confirmation duty; read that first.

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
  evidence: read the process back with `describe-business-process` yourself.
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

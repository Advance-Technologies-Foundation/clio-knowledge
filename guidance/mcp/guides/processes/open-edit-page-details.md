clio MCP process-open-edit-page-details guide — the Open edit page element's evidence and read-back

A details article of the process guide set, reached through `process-open-edit-page`, which points here; `process-modeling` is the set's entry point.
This article is the authoritative owner of `useBackgroundMode` on the Open edit page element, the measured
evidence behind its Log activity default, why its record type follows the page, the designer's value-source
menu for its fields, the `showPage` value written at create, and reading an existing element back with
`describe-business-process`. Split out of `process-open-edit-page`, which keeps the `openEditPage` block, every
field rule and refusal, the destructive-change guards and the ROUTING rule for choosing this element; read that
first.

Measured on a 10.1.628 core: a freshly built element comes back from `describe` with `logActivity.enabled: true`,
a 5-minute duration and `Medium` priority, none of it requested. The shipped 7.8.0 copy of the same schema has
the gate at `false`, so the default is VERSION-DEPENDENT and cannot be assumed either way.

`useBackgroundMode` is covered by the SCOPE rule of `process-open-edit-page` too, ON THIS ELEMENT: leave it off
unless the request asks for background execution. Two things say so, and the second is the one that matters. The
platform's own corpus: of the 120 Open edit page elements shipped across `PackageStore`, 118 leave the flag off,
and the only two that carry it are `ProcessTests` fixtures rather than business processes (across all 469 shipped
user tasks, 7). And the failure it produces is SILENT: measured on a 10.1.628 core, an Open edit page step with
the flag ON did NOT complete after its completion condition was satisfied and the record saved — no error, no
log entry, the instance simply sits in `Running` and the performer's task never clears; clearing the flag
completed it. So do not set it here unless asked — and when a step will not complete although its condition is
met, suspect this flag before anything else.

The designer's page list carries ONE entry per page (`_fillPageSchemaList` merges a repeat row instead of adding a
second), so a page registered for several record types is offered once and the type FOLLOWS the page.

Per-entry value sources map onto the designer's own menu, whose contents depend on the COLUMN's type: on a text
column it offers "Process parameter", "System setting" and "Formula"; on a lookup column it adds "Lookup value".
`processParameter` = "Process parameter"; `expression` with `[#Lookup.{objectSchemaUId}.{recordId}#]` = "Lookup
value"; `value` is a plain TEXT constant (a typed constant is refused — the runtime reads those columns typed).
The `recordId` field's menu is the richest — it also offers "Current user account" when the page's object is
Account — and any such option is reachable through `expression`, which is passed through verbatim.

`showPage` is written explicitly at create (an inherited default would be unreportable, since `describe` reports
only what an element STORES) and its VALUE follows the performer: `true` for a `user` performer or none at all,
`false` for `manager`/`role`. That is not a policy of ours — the platform opens the page automatically only for
the user the step is assigned to, and the designer disables the checkbox for the other two kinds.

(The designer permits that state, so a process read back with `onConditions` and no conditions completes on every
save regardless of what its card suggests; switching such an element to `onConditions` is refused until it has
real conditions.)

Two states to recognize when READING an existing element: the designer lets a human switch the checkbox on and
leave the required Column empty, and `describe` reports that faithfully as `enabled: true` with `column: null` —
a switched-on list that produces no results. Such a block cannot be fed straight back (the write path requires
the column), so supply a column or `enabled: false` when re-applying.

`describe-business-process` reads the configuration back as the element's `openEditPage` block, round-trippable
into a build/modify block with ONE asymmetry — the read reports pre-filled values AND a record when the schema
carries both (the write path refuses that pair), so drop the one that does not belong to the reported `editMode`
before re-applying, and feed `pageTypeUId` back as `recordType`. A `performer` of `null` in the read-back means
UNASSIGNED, never unsupported.

clio MCP process-add-data guide — the Add data element

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.

== What this article owns ==
This article owns the Add data element: its `addData` block, both adding modes, every value source the
column assignments accept, the transitions `setElement.addData` refuses, and the notices a build raises
rather than refusing. Split out of `process-data-elements` when that article outgrew a single
get-guidance response; the sibling data elements (record triggers, Read data, Modify data) stay there.
The record `filter` this element carries is owned by `process-data-source-filters` — this article says
WHETHER it takes one and what it means here, that one says what a filter may contain. The build
lifecycle and descriptor shape live in `process-modeling`; what is buildable today lives in
`process-element-catalog`.

== Add data element (addData) ==
- An `addData` element CREATES records. Two modes, chosen by `mode`: `one` (the default) adds ONE record;
  `selection` adds one record PER RECORD of a filtered selection from another object.
- Add ONE record:
    { "name": "AddContact", "type": "addData", "caption": "Add a contact",
      "addData": {
        "source": "Contact",                                   // REQUIRED at create: the object to add to
        "mode": "one",                                         // optional — "one" is the default
        "values": [
          { "column": "Name", "value": "New contact" },
          { "column": "Account", "processParameter": "AccountParameter" }
        ] } }
- Add a record PER SELECTION RECORD (the bulk mode). `selection` names the object being iterated, the
  element's `filter` says WHICH of its records qualify, and `selectionColumn` maps a column of each new
  record to the corresponding column of the selection record that produced it:
    { "name": "AddParticipants", "type": "addData", "caption": "Add a participant per contact",
      "addData": {
        "source": "ActivityParticipant",
        "mode": "selection",
        "selection": "Contact",                                // REQUIRED in selection mode
        "values": [
          { "column": "Participant", "selectionColumn": "Id" },       // from THIS selection record
          { "column": "Activity", "processParameter": "ActivityParameter" }
        ] },
      "filter": { "object": "Contact",                         // the filter is over the SELECTION object
        "conditions": [ { "column": "Type", "comparison": "equal", "value": "Employee" } ] } }
- `values` entries take the SAME shape and the same rules as `changeData` (see below): `column` plus exactly
  ONE source, `value` for TEXT columns only and non-empty, unknown columns rejected at build. It is the same
  code path, so the two elements cannot diverge on any of it.
- `selectionColumn` is the ONE value source that exists on no other element, and it is valid ONLY in
  `selection` mode — in `one` mode there is no selection record to take a column from, so it is REFUSED with
  the mode named. It is type-checked against the target column by the same rule the designer filters the
  source columns it offers.
- `selection` is REQUIRED in `selection` mode and REFUSED in `one` mode. That refusal is not pedantry: the
  designer's card reads the stored selection object as the MODE when no mode is stored, so a one-record
  element carrying one is a contradiction a later human save would resolve against you.
- The `filter` is over the SELECTION object, and is effectively mandatory in `selection` mode: without one the
  element iterates EVERY record of that object. In `one` mode the element applies no filter at all. BOTH
  mismatches WARN, never refuse: `selection` with no filter warns it adds one record per record of the ENTIRE
  object (issue `setFilter`); `one` WITH a filter warns it is inert but KEPT for a later switch to `selection`.
- `values` may be OMITTED or empty — unlike `changeData`, whose values-less element the runtime skips. An
  Add data element with no values still inserts a row of the target object's defaults. A REQUIRED column of
  the target object left unset is reported as a WARNING on the response, not refused, because a business rule
  or the platform may fill it; read the warnings.
- OUTPUT: the element's ONLY result is the id of the new record, on its `RecordId` parameter. Map it by NAME
  to use the new record downstream:
    { "targetProcessParameter": "NewContactId", "sourceElement": "AddContact",
      "sourceElementParameter": "RecordId" }
  `describe-business-process` does NOT list `RecordId` in the element's `parameters[]` — the platform declares
  it neither `IsResult` nor `Out` — so do not go looking for it there; the name above is the contract. Any
  OTHER column of the new record requires a following `readData` element: the element returns the id and
  nothing else.
- Change an EXISTING element in place with the `setElement` op's `addData` field: omit `source` to keep the
  target, omit `mode` to keep the adding mode, omit `values` to keep the assignments (a supplied array
  REPLACES the whole set). Five transitions are REFUSED rather than half-applied: a `source` retarget needs
  `values` in the same update (the stored ones name the old entity's columns) and is refused while another
  parameter still maps from the element — which the `RecordId` mapping above makes common, so expect it;
  switching to `selection` needs `selection`; and switching to `one` (which clears the selection object)
  or retargeting `selection` is refused while a stored `selectionColumn` would strand — re-supply
  `values`. On ANY change of
  the target or the selection the stored record filter clears unless it already targets the incoming
  selection object — issue a `setFilter` in the same operations array when it cleared.
- `describe-business-process` reads the block back with the EFFECTIVE mode, i.e. what the designer's card
  shows: an element with no stored mode reads as `selection` when it stores a selection object and `one`
  otherwise. A stored "Column from this selection" decodes back to `selectionColumn` when its column resolves
  on that selection object, and stays column-only when it does not.

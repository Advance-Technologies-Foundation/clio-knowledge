clio MCP process-delete-data guide — the Delete data element

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.

This article is the authoritative owner of the Creatio **Delete data** element (`deleteData` /
`DeleteDataUserTask`): its one-field `deleteData` block, the record filter that decides what is destroyed,
and — the part that makes it a separate article rather than a paragraph — the confirmation you owe the user
before you build one. `process-element-catalog` says the element is buildable; `process-data-elements` owns
its Read data and Modify data siblings; `process-data-source-filters` owns what a filter may contain.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

== Delete data element (deleteData) ==
- STOP AND CONFIRM FIRST. A `deleteData` element permanently deletes every record its `filter` matches,
  every time the process runs. Deletion is irreversible and cascades to dependent records. You MUST state
  what will be deleted and get an explicit yes BEFORE you call create/modify-business-process with the
  element. Never add a `deleteData` element to fill a gap the user did not ask for.
- COUNT THE RECORDS FIRST. Do not ask the user to approve a filter they cannot picture. Translate the
  filter into a count-only read against the same object and report the NUMBER:
    read { entity: "Lead", filters: { all: [ … same conditions … ] }, count: true, top: 0 }
    -> { total: 1847, value: [] }
  A count is one cheap request and it is what turns an approval into an informed one. Three cases where you
  cannot produce a number — say WHICH one applies rather than skipping the count silently:
  (a) the filter references a process parameter or a trigger output (e.g. a `signalStart` element's
      `RecordId`) — the value does not exist until the process runs, so no count is possible. Say so, and
      describe the shape instead ("exactly the one record that fired the trigger");
  (b) the environment has no data connection registered for reading — say you could not count and let the
      user decide whether to check in the UI first;
  (c) a condition uses a macro or a comparison the read tool cannot express — count what you CAN and say
      the real number will be smaller or larger, and which way.
- THE CONFIRMATION MESSAGE. Write it in the user's own language, in their words, not in contract terms
  (say "Leads", not "the Lead entity"; say the filter in prose, not as JSON). Include all five parts:
    1. WHAT — the object, by its business name.
    2. HOW MANY — the counted number today, or the named reason there is none.
    3. WHICH — the filter in one plain sentence.
    4. THE CONSEQUENCES — irreversible; dependent records go with them; it runs on EVERY execution of the
       process, not once.
    5. THE ASK — a direct yes/no question, plus a concrete way to narrow it.
  Template:
    > Before I add this step — it deletes data permanently, so I want to confirm what it will remove.
    >
    > **Object:** Leads
    > **Right now this matches:** 1,847 records
    > **Which ones:** created before 3 September 2025 and marked Disqualified
    >
    > Deleting them cannot be undone, and Creatio removes their related records too (activities, links to
    > accounts). This step runs every time the process executes, not once — so any Lead that matches later
    > will also be deleted.
    >
    > Do you want me to add it? I can narrow it first — for example only Leads with no activity in the last
    > year, or a dry run that reads and reports them instead of deleting.
- THE SERVER WARNS, on the two states it can verify. A build that configures a `deleteData` element with
  no `filter`, and a `setElement` retarget that CLEARED one, both come back with a warning in the response
  naming the element. Neither is a refusal — `addElement` then `setFilter` is a legitimate two-step — but
  both describe a step that, as saved, deletes nothing and fails on its first run. Do not report such a
  build as a working delete: finish it with a `setFilter`, or say plainly that the element is incomplete.
  The warnings say nothing about whether the user agreed to anything; that part is still yours.
- RULES FOR THE EXCHANGE:
  * A vague reply is not a yes. "ok go on", "whatever you think" -> ask once more, plainly.
  * If the filter CHANGES after the user agreed, confirm again — consent was for the old set, not this one.
  * If the user asks for deletion but the safer step is a status change or an archive flag, say so ONCE,
    then do what they asked. Do not refuse and do not keep pushing.
  * Never soften the number or leave it out because it is large. A big number is exactly the case the
    confirmation exists for.
  * The confirmation belongs in the conversation BEFORE the tool call. Adding the element and then
    mentioning the risk is not a confirmation.
- A `deleteData` element deletes every record of one object matching its `filter`:
    { "name": "DeleteStaleLeads", "type": "deleteData", "caption": "Delete the stale leads",
      "deleteData": { "source": "Lead" },                     // REQUIRED at create: the object to delete from
      "filter": { "object": "Lead",
        "conditions": [ { "column": "Id", "comparison": "equal",
          "elementParameter": { "elementName": "RecordAddedSignal", "parameter": "RecordId" } } ] } }
- The block has exactly ONE field, `source`. There are no column values (nothing is being written) and no
  mode flag: unlike `changeData`, the Delete data runtime has no "match conditions" switch to turn off — it
  always applies the filter.
- The `filter` is MANDATORY in effect. With no filter the element throws the platform's empty-filter error,
  deletes NOTHING, and the failure shows in the process log. There is no delete-every-record mode; if that is
  genuinely the intent, it has to be expressed as a filter that matches everything, and it should be
  confirmed twice.
- To delete ONE record, filter on `Id` against a process parameter or a trigger output such as a
  `signalStart` element's `RecordId` — the same shape as the `changeData` single-record filter above.
  LIMITATION: the record read by a preceding `readData` element is NOT referenceable here (see the readData
  LIMITATION; ENG-91844).
- Change an EXISTING element in place with the `setElement` op's `deleteData` field: omit `source` to keep
  the current target. A retarget is refused while another parameter still maps from the element, and on ANY
  target change (FIRST configuration included) the stored record filter clears unless its root already
  targets the incoming object — issue a `setFilter` in the same operations array when it cleared, or the
  element is left in the deletes-nothing-and-fails state. Same reset rule as `readData` / `changeData`.
- `describe-business-process` reads the block back as `source` + `sourceSchemaUId`. `source` is null when the
  element's target object is set by a formula/mapping instead of a constant — the block is still reported,
  and retargeting such an element needs an explicit `source`.

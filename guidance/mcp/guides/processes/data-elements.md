clio MCP process-data-elements guide — record triggers and Modify data

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of starting a process from a record event and of the Modify data
element; Read data has its own article, `process-read-data`; Add data has `process-add-data`; Delete data
has `process-delete-data`. The `filter` these elements
carry is owned by `process-data-source-filters`: every element here says WHETHER it takes one and what
that means for it, and that article says what a filter may contain. The Change access rights element consumes the same
filter; see `process-access-rights`.

== Trigger a process on a record event ("run on save" of a page/record) — READ THIS ==
- When the goal is "run a process when a record is saved / added / changed / deleted" (e.g. on a page
  like UsrXxx_FormPage), that is a PROCESS trigger, NOT page logic. Make the process START with a
  Signal start element bound to the object. Do NOT add a client-side save handler
  (`crt.SaveRecordRequest` / any page handler) to launch a process on save — that is the wrong tool and
  a fragile workaround. The signal start is the platform-native, declarative trigger.
- Build it with `create-business-process`. The start element is:
    { "name": "RecordModifiedSignal", "type": "signalStart", "caption": "Record is modified",
      "signal": { "entity": "<EntityName>", "on": "modified" } }
  then the activity (e.g. a Perform task / `performTask` that shows a Task), then an `endEvent`,
  wired RecordModifiedSignal -> activity -> end. (`entity` is the page's object, e.g. UsrTestRunButton.)
- `on` is a SINGLE event: "added" | "modified" | "deleted" (the designer has no combined
  "added or modified"). "On save" of a record edited on a page = "modified"; a brand-new record = "added".
- A "modified" trigger fires on ANY field change by default. To restrict it to fire ONLY when specific
  columns change, add `changedColumns` (an array of column NAMES on the trigger entity) to the signal:
    { "name": "OrderAmountOrStatusChangedSignal", "type": "signalStart", "caption": "Order amount or status changed",
      "signal": { "entity": "Order", "on": "modified", "changedColumns": ["Amount", "StatusId"] } }
  `changedColumns` is valid ONLY for `on: "modified"` (the designer's "expect changes" case) — the server
  rejects it for "added"/"deleted", and rejects a name that is not a column on the entity. Use entity COLUMN
  names (e.g. `Amount`), not field captions; omit `changedColumns` (or pass []) to fire on any change — but an
  array that contains ONLY blank entries is rejected, since that reads as a mistake rather than as a request
  to widen the trigger (blanks mixed with real names are simply ignored). This is
  INDEPENDENT of `filter`: `changedColumns` narrows WHICH columns count as a change, `filter` narrows WHICH
  records qualify — combine them freely.
- To fire the trigger ONLY for records matching a condition (e.g. only when Name = "Start"), add a
  `filter` to the signalStart element (full shape in the "Data source filters" section of
  `process-data-source-filters`):
    { "name": "RunButtonPressedSignal", "type": "signalStart", "caption": "Run button is pressed",
      "signal": { "entity": "UsrTestRunButton", "on": "modified" },
      "filter": { "object": "UsrTestRunButton",
        "conditions": [ { "column": "UsrName", "comparison": "equal", "value": "Start" } ] } }
  Use the entity COLUMN name (here `UsrName`), not the field caption ("Name").
- To convert an EXISTING process to start on a record event, use `modify-business-process`:
  removeElement the current start, addElement a `signalStart`, addFlow signalStart -> (first activity).
  `removeElement` is DESTRUCTIVE and no pre-save validation restores what it breaks: it cascades to every flow
  touching the element without re-joining the gap. The rules that make a removal safe — describe first,
  validate the graph AS IT WILL BE, confirm with the user — are in `process-modeling`. Read them before
  removing anything, not after.
- To change an EXISTING signal's trigger or tracked columns IN PLACE (without re-adding it), use the
  `setSignal` op — it preserves the element and its flows:
    { "op": "setSignal", "elementName": "OrderAmountOrStatusChangedSignal",
      "signal": { "on": "modified", "changedColumns": ["Amount"] } }
  Partial update: omit `changedColumns` to clear column tracking (fire on any change), omit `on` to keep the
  current change type, and include `entity` only to retarget the trigger object (retargeting clears any
  filter bound to the old entity).
  MUST, on a live process: two of those WIDEN the trigger silently, and neither is reported as an error.
  Omitting `changedColumns` clears column tracking, so the process fires on ANY change to the record
  rather than on the columns it tracked. Including `entity` clears the record filter, so the process
  fires on every record of the new object rather than on the filtered subset (the change type itself is
  unaffected -- `on` is one event and omitting it keeps the current one). Re-state `changedColumns` when
  you meant to keep it, and re-send the filter with `setFilter` in the same operations array when you
  retarget -- `process-data-source-filters` owns `setFilter`, and that op REPLACES the whole filter, so
  read the current one back first.

== Read data element (readData) — MOVED ==
- Read `process-read-data`: the block, all four modes and their outputs, the column selection and sort,
  the collection shape and its top-N, and what a mode conversion clears.

== Add data element (addData) — MOVED ==
- Read `process-add-data`: the block, both modes, the value sources and the refused transitions.

== A column of a Read data record as a source ==
- ONE column of the record a first-record `readData` element read (its `ResultEntity`) is a value source,
  named by the column's CODE. The server resolves it on the read object, type-checks it against the target
  and writes the platform's three-segment meta path, so there are no UIds to find. This article owns the
  forms and the refusals:
  * a mapping (`mappings[]` at create, `addMapping` on modify) - the element source plus `sourceColumn`:
        { "elementName": "Call", "elementParameter": "OwnerId",
          "sourceElement": "ReadContact", "sourceElementParameter": "ResultEntity", "sourceColumn": "Owner" }
  * a `changeData` / `addData` / `openEditPage` value - the same trio; the entry's `column` is its TARGET:
        { "column": "Owner", "sourceElement": "ReadContact", "sourceElementParameter": "ResultEntity",
          "sourceColumn": "Owner" }
  * an `openEditPage` `recordId` - the trio, with a LOOKUP column that points at the page's object (or the
    `Id` column);
  * a filter's right-hand side - `"elementParameter": { "elementName": "ReadContact", "parameter":
    "ResultEntity", "column": "Owner" }`;
  * a branch condition at CREATE, or a Formula body - `[#ReadContact.ResultEntity.DoNotUseCall#] == true`.
- Refused at build, naming the field: a PATH (`Owner.Name` - read the related record with its own
  `readData`, filter `Id` = the column); a collection output, an item of one, or a lookup/`RecordId`
  parameter (nothing loads a lookup's record, so its columns would stay empty); a read in any mode but
  `first` (`count` / `aggregation` never fill `ResultEntity`, and in `collection` mode it is not the output -
  use the list outputs); a column outside a non-empty `readData.columns` list
  (the platform fetches only the listed columns, so it would arrive EMPTY - list it or omit `columns`; the
  primary column `Id` is always fetched); a type that does not fit the target, by the rule
  parameter-to-parameter mappings use (`OwnerId` <- `Owner` builds, <- `Account` is refused). A later
  `setElement readData.columns` that drops a column something still reads is refused too. A column name in
  `sourceElementParameter` (`"Email"`) is still "element has no parameter": the column goes in
  `sourceColumn`. Nothing checks ORDER on a mapping, filter or condition, so place the read before its
  consumers in the flow yourself.
- `describe-business-process` reports such a value's `sourceElement` / `sourceElementParameter` /
  `sourceColumn` beside the raw `value`, only when those names would re-apply to the identical value
  (same spelling, a fitting type, a loaded column).
- A condition on the MODIFY path has no name expansion, so write the UId form: the read element's `uid`
  and its `ResultEntity` parameter's `uid` from describe, the column's `u-id` from
  `get-entity-schema-properties` (merged view, no `package-name`) -
  `[#[Element:{<elementUid>}].[Parameter:{<ResultEntityUid>}].[EntityColumn:{<columnUid>}]#] == true`.
  This form is stored as written and gets NO load check: include the column in `readData.columns`, or omit
  the list, yourself. The same holds for the UId form inside any raw `expression`.
  Describe again and require `kind: "conditional"` plus the exact text, then run with a matching and a
  non-matching record: a read-back proves authoring, NOT routing.
  Verified for `Contact.DoNotUseCall` true/false on Creatio 10.1.585 (.NET 8, PostgreSQL), clio 8.1.0.131, CrtProcessBuilder 1.6.2.24;
  [validation evidence](https://github.com/Advance-Technologies-Foundation/clio/issues/1645#issuecomment-5760323479).
- A Send email BODY macro reaches a column with its own grammar, and a recipient reaches one through a
  process parameter; `process-send-email` owns both.

== Modify data element (changeData) ==
- A `changeData` element updates every record matching its `filter` with the declared column values:
    { "name": "UpdateContact", "type": "changeData", "caption": "Update the contact",
      "changeData": {
        "source": "Contact",                                   // REQUIRED at create: the entity to update
        "values": [                                            // REQUIRED at create: one entry per column
          { "column": "JobTitle", "value": "Manager" },        // plain constant — TEXT columns ONLY (see below)
          { "column": "Notes", "processParameter": "NoteTextParameter" }, // a process parameter's value
          { "column": "Account", "sourceElement": "RecordModifiedSignal", "sourceElementParameter": "RecordId" }
        ] },
      "filter": { "object": "Contact",
        "conditions": [ { "column": "Name", "comparison": "contains", "value": "Creatio" } ] } }
- Each `values` entry sets `column` (entity COLUMN name) + exactly ONE source: `value` | `processParameter` |
  `sourceElement` + `sourceElementParameter` (+ `sourceColumn`, one column of that element's record) |
  `expression` — the mapping source vocabulary. One entry per
  column (duplicates rejected); unknown columns/parameters rejected at build.
- `value` is a plain constant for TEXT columns ONLY, and non-empty. The platform stores it as the raw string
  and the runtime reads every non-text column TYPED — a date/lookup/numeric constant would save green and
  fail at run time, so the server REFUSES it at build. Assign non-text columns via `processParameter`,
  `sourceElement`+`sourceElementParameter`, or an `expression` macro (`[#DateValue.…#]` / `[#Lookup.…#]` —
  same macro grammar as parameter defaults — see the DEFAULT-value macro rules in `process-parameters`). An empty `value` is refused
  for every type: the runtime silently discards an empty assignment.
- The `filter` is EFFECTIVELY MANDATORY: the runtime refuses to update with an empty filter (it would mean
  "update every record of the object"). To target ONE record, filter on `Id` against a process parameter
  or a trigger output such as a `signalStart` element's `RecordId`:
    "filter": { "object": "Contact",
      "conditions": [ { "column": "Id", "comparison": "equal",
        "elementParameter": { "elementName": "RecordModifiedSignal", "parameter": "RecordId" } } ] }
  A read record's `Id` is NOT an element parameter: the `elementParameter` shorthand above cannot
  name it on a `readData` element (see the readData LIMITATION above).
- Change an EXISTING element in place with the `setElement` op's `changeData` field: omit `source` to keep
  the current target; a supplied `values` array REPLACES the whole assignment set. Retargeting `source` to a
  different object REQUIRES `values` for the new entity in the same update — the server REFUSES a values-less
  retarget, because the cleared element would be silently skipped by the runtime (the same fact that makes
  `values` mandatory at create); the same refusal covers a values-less update on an element with no stored
  values yet, and a retarget is refused while another parameter still maps from the element. On ANY target change
  (FIRST configuration included) the stored record filter clears UNLESS its root already targets the incoming
  object — `setFilter` never validates its `object` against the element, so a same-object filter set before the
  target survives; issue a `setFilter` in the same operations array when it cleared. Same rule on `readData`.
  MUST: that `setFilter` REPLACES the element's whole filter — read the current one back with
  `describe-business-process` and send it complete, or the records this element updates silently widen.
  `process-data-source-filters` owns the op and its read-back. `describe-business-process` reads the block back (`source` is null when the element's target object is set by a formula/mapping instead of a constant — the block is still reported, and retargeting such an element needs an explicit `source`; constants in `value`; a
  `processParameter` / `sourceElement` binding decodes back to its NAME, so the block re-applies in another
  process — a decoded `sourceElement` still obeys the create-time rule that its element appear EARLIER in
  `elements[]`, and describe emits stored order, so a described block may need reordering before it re-creates.
  A stored value the write path would refuse — a non-text or empty constant, or a binding that fails the type
  check — reads back as its COLUMN ALONE rather than as something you cannot write back, and any other formula
  comes back as its raw `[#…#]` in `expression`).

== Delete data element (deleteData) ==
- Owned by its own article: `process-delete-data`. A `deleteData` element permanently deletes every record
  its `filter` matches, every time the process runs, and the block that configures it has ONE field. Fetch
  that article before you plan one in — it carries the count-first rule and the confirmation message you owe
  the user, and neither is optional.

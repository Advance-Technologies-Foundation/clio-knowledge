clio MCP process-data-elements guide — record triggers, Read data, Modify data and their filters

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of starting a process from a record event, the Read data and Modify data elements, and the record filter all three share.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

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
  `filter` to the signalStart element (full shape in "Data source filters" below):
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

== Read data element (readData) — first-record mode ==
- A `readData` element reads the FIRST record of a sorted selection into its `ResultEntity` output
  parameter (the whole record). Configure it with the element's `readData` block:
    { "name": "ReadNewestContact", "type": "readData", "caption": "Read newest contact",
      "readData": {
        "source": "Contact",                                  // REQUIRED at create: the entity to read
        "mode": "first",                                      // optional; "first" is the only buildable mode
        "columns": ["Name", "Email"],                         // optional; omit or [] = read ALL columns
        "sort": { "column": "CreatedOn", "direction": "desc" } // optional; direction defaults to "asc"
      },
      "filter": { "object": "Contact",
        "conditions": [ { "column": "Name", "comparison": "contains", "value": "Creatio" } ] } }
- `mode`: only `first` (first record of the sorted selection). The designer's other read modes —
  collection, count, aggregation — are NOT buildable yet and are REJECTED with a clear error. An element a
  human configured in one of those modes CANNOT be converted to first-record through this API at all — an
  explicit `"mode": "first"` is refused too, because the conversion would leave the element's collection
  item parameters behind. Remove the element (`removeElement`) and add a new `readData` one instead —
  under the destructive-removal rules in `process-modeling`, since the removal cascades to this
  element's flows and mappings and the modify path will not warn you.
- `columns` are TOP-LEVEL entity COLUMN names (not captions); an unknown name is rejected at build. Omit the
  list (or pass `[]`) to read all columns. A dot-separated path into a linked object (`Owner.Name`) is NOT
  supported and is rejected — such paths exist only in hand-authored metadata (the Read data card's own
  picker lists top-level columns only); read the whole record (omit `columns`) if you need them. `sort`
  makes "the first record" deterministic — without it the platform reads an arbitrary first record; single
  column only (multi-column ordering is designer-only), and the sort column must be top-level too.
- WHICH records qualify is the element's separate `filter` block (full shape in "Data source filters"
  below). Unlike a signalStart filter, a readData filter MAY reference `processParameter` /
  `elementParameter` — the element runs inside a live process instance.
- LIMITATION — a read record's individual COLUMN values are out of reach in practice, so "the record I
  just read has status X", the likeliest branch after a read, cannot be authored. NOT because the
  platform refuses a third segment — it parses one — but because describe reports no column UIds, so
  there is nowhere to GET the one you would have to write. Author TWO segments
  (`[#[Element:{uid}].[Parameter:{uid}]#]`) in a mapping, a `changeData` value or a filter condition.
  One exception, whose form `process-send-email` owns: a Send email BODY macro reaches a column by NAME,
  `[[element:Read.ResultEntity.Column]]`. The
  element's only output parameter is `ResultEntity` (the whole record, `isResult:true` in describe);
  the record's columns are NOT element parameters, so a mapping, `changeData` value or filter condition
  that references them (e.g. `sourceElementParameter: "Email"` on the read element) FAILS the build with
  "element has no parameter". Entity-column access needs meta-path support (planned; ENG-91844). To key
  work off a specific record today, use a `signalStart` trigger output (`RecordId`) or a process parameter.
- Change an EXISTING element in place with the `setElement` op's `readData` field (preserves the element
  and its flows):
    { "op": "setElement", "elementName": "ReadNewestContact",
      "elementUpdate": { "readData": { "sort": { "column": "ModifiedOn", "direction": "desc" } } } }
  Partial update: omit `source` to keep the current source object, omit `columns`/`sort` to keep the
  current selection/order, pass `columns: []` to reset to ALL columns. RETARGETING `source` to a different
  object is REFUSED while any other parameter still maps from the element (the refusal names each
  dependent — re-map or remove them first, the same block the designer applies); a retarget that proceeds
  clears the columns, sort AND record filter bound to the old entity — re-supply them (and issue a
  `setFilter`) in the same operations array. `describe-business-process` reads the whole block back
  (`source`, `mode`, `columns` as names, `sort`), so anything the builder made round-trips into
  create/modify. Read-back limits on a HUMAN-made element: a linked-object column is omitted from
  `columns` (it cannot be expressed here), and `sort` is the EFFECTIVE PRIMARY entry — the one the
  runtime's ORDER BY actually ranks first — while any further ACTIVE secondary sort entries are not
  reported, and a `sort` write replaces the whole stored order. So for such an element the described
  block is narrower than what it really does — do not feed it back as a full replacement.
- A HUMAN SAVE quietly changes a builder-made element's plumbing: opening the element card and clicking OK
  always writes `ReadSomeTopRecords = true` + `NumberOfRecords`, which a builder-made element leaves unset
  (row count stays 1 — what "first record" means). Under the `FeatureReadDataUserTaskEntityReadOldMode`
  feature that flag changes how many rows the element reads, and the drift is INVISIBLE to
  `describe-business-process` (unset parameters are omitted) — a builder-made and a human-touched element
  look identical there. Nothing to do about it at build time; know it when diagnosing a stand.

== Modify data element (changeData) ==
- A `changeData` element updates every record matching its `filter` with the declared column values:
    { "name": "UpdateContact", "type": "changeData", "caption": "Update the contact",
      "changeData": {
        "source": "Contact",                                   // REQUIRED at create: the entity to update
        "values": [                                            // REQUIRED at create: one entry per column
          { "column": "JobTitle", "value": "Manager" },        // plain constant — TEXT columns ONLY (see below)
          { "column": "Notes", "processParameter": "NoteTextParameter" }, // a process parameter's value
          { "column": "AccountId", "sourceElement": "RecordModifiedSignal", "sourceElementParameter": "RecordId" }
        ] },
      "filter": { "object": "Contact",
        "conditions": [ { "column": "Name", "comparison": "contains", "value": "Creatio" } ] } }
- Each `values` entry sets `column` (entity COLUMN name) + exactly ONE source: `value` | `processParameter` |
  `sourceElement` + `sourceElementParameter` | `expression` — the mapping source vocabulary. One entry per
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
  LIMITATION: the record read by a preceding `readData` element is NOT referenceable here — its column
  values (including `Id`) live inside the `ResultEntity` output, not as element parameters (see the
  readData LIMITATION above; ENG-91844).
- Change an EXISTING element in place with the `setElement` op's `changeData` field: omit `source` to keep
  the current target; a supplied `values` array REPLACES the whole assignment set. Retargeting `source` to a
  different object REQUIRES `values` for the new entity in the same update — the server REFUSES a values-less
  retarget, because the cleared element would be silently skipped by the runtime (the same fact that makes
  `values` mandatory at create); the same refusal covers a values-less update on an element with no stored
  values yet, and a retarget is refused while another parameter still maps from the element. On ANY target change
  (FIRST configuration included) the stored record filter clears UNLESS its root already targets the incoming
  object — `setFilter` never validates its `object` against the element, so a same-object filter set before the
  target survives; issue a `setFilter` in the same operations array when it cleared. Same rule on `readData`. `describe-business-process` reads the block back (`source` is null when the element's target object is set by a formula/mapping instead of a constant — the block is still reported, and retargeting such an element needs an explicit `source`; constants in `value`; a
  `processParameter` / `sourceElement` binding decodes back to its NAME, so the block re-applies in another
  process — a decoded `sourceElement` still obeys the create-time rule that its element appear EARLIER in
  `elements[]`, and describe emits stored order, so a described block may need reordering before it re-creates.
  A stored value the write path would refuse — a non-text or empty constant, or a binding that fails the type
  check — reads back as its COLUMN ALONE rather than as something you cannot write back, and any other formula
  comes back as its raw `[#…#]` in `expression`).

== Data source filters (signalStart trigger condition / readData + changeData record filter) ==
- A `filter` declares, high-level, WHICH records a filtered element acts on. The server serializes it to
  the platform Terrasoft.FilterGroup — you NEVER hand-write the escaped filter JSON.
- Usable today on a `signalStart` (restrict the record trigger), on a `readData` element (restrict which
  records the read selects from) and on a `changeData` element (restrict which records are updated —
  effectively mandatory there). Shape:
    "filter": {
      "object": "<EntityName>",        // root object; defaults to the signal entity if omitted
      "logicalOperation": "and",       // "and" (default) | "or"
      "conditions": [
        { "column": "UsrName",      "comparison": "equal", "value": "Start" },
        { "column": "Account.Code", "comparison": "equal", "value": "1" }   // dot-path traverses a lookup
      ],
      "groups": [                       // optional nested groups, each with its own logicalOperation
        { "logicalOperation": "or", "conditions": [ /* ... */ ] }
      ]
    }
- `column` is the entity COLUMN name (e.g. `UsrName`, not the caption "Name") and may be a dot-path
  through lookups (`Account.Code`, `Account.Owner.Name`); the server resolves the column type from the
  object's schema (so you don't supply types).
- `comparison`: equal (default) | notEqual | greater | greaterOrEqual | less | lessOrEqual | contains |
  notContains | startWith | notStartWith | endWith | notEndWith | isNull | isNotNull.
- The right-hand value of a condition is exactly ONE of: `value` (a constant as a string — the server
  types it by the column; for a Date/DateTime/Time column pass ISO-8601, e.g. `2026-05-01` or
  `2026-05-01T12:00:00Z`), `processParameter` (a process parameter by name), `elementParameter`
  ({ elementName, parameter } — another element's output; the parameter must EXIST on that element — a
  `readData` element exposes only `ResultEntity`, so `{ "elementName": "ReadNewestContact", "parameter": "Id" }` is
  refused, see the readData LIMITATION), `expression` (a raw token), or `macro` (a
  relative-date / system macro — the complete set is in the next bullet). isNull/isNotNull take none.
- `macro` vocabulary (COMPLETE set — an unknown name is rejected at BUILD, validated against the platform
  macro catalog, never silently accepted): **relative periods** `Yesterday` | `Today` | `Tomorrow`, plus
  `Previous`/`Current`/`Next` for each of `Week` | `Month` | `Quarter` | `HalfYear` | `Year` | `Hour`
  (so `CurrentHalfYear`, `NextWeek`, `PreviousQuarter`, `CurrentHour`, … are ALL valid); **argument macros**
  (require an integer `macroArgument`) `NextNDays` | `PreviousNDays` | `NextNHours` | `PreviousNHours` |
  `NextNDaysOfYear` | `PreviousNDaysOfYear` | `DayOfYearTodayPlusDaysOffset`; **recurring "every year"**
  `DayOfYearToday` (the ONLY DayOfYear macro that takes NO argument); **system / lookup** `CurrentUser` |
  `CurrentUserContact`.
- SIGNAL-START RESTRICTION (important): on a `signalStart` filter the right-hand side may ONLY be a constant
  `value`, a `macro`, or isNull/isNotNull (`datePart` is a LEFT-hand modifier, never a source) — NOT `processParameter` / `elementParameter` /
  `expression`. The signal is evaluated to decide WHICH records start the process, BEFORE any process
  instance exists, so a parameter / element output / meta-path reference has no value yet. The server
  REJECTS a parameter reference on a signal filter (the visual designer likewise hides the "select
  parameter" option for signal starts). Parameter references ARE valid on a data-operation element filter —
  the element runs inside a live process instance — and are end-to-end buildable on a `readData` element
  (e.g. filter the read by a process parameter's value) and on a `changeData` element, where the filter is
  effectively MANDATORY (the runtime refuses to update with an empty one); on Add/Delete data they serialize
  but the task itself is not buildable yet (see below).
- `datePart` (optional, LEFT-hand modifier — NOT a right-hand source): extract a calendar/clock part from a
  Date/DateTime `column` and compare that part instead of the whole date. `Year` | `Month` | `Day` |
  `Week` | `Weekday` | `Hour` extract an INTEGER — pair with an integer `value` (a signalStart filter
  allows only a constant `value` or `macro`, never a `processParameter` — see the restriction above):
  `{ "column": "CreatedOn", "datePart": "Year", "comparison": "equal", "value": "2026" }` reads
  `Year(CreatedOn) = 2026`. `HourMinute` is the exception — it extracts the TIME-OF-DAY and compares it to a
  `value` in `HH:mm[:ss]` form: `{ "column": "CreatedOn", "datePart": "HourMinute", "comparison": "equal",
  "value": "14:30" }` reads `HourMinute(CreatedOn) = 14:30`. Combines with any comparison (`greaterOrEqual`,
  …); it modifies the left side, so it is independent of the right-hand source choice (but do not use it with
  a `macro`).
- Groups nest to any depth: A AND (B OR C) = conditions:[A] + groups:[{ "logicalOperation":"or",
  conditions:[B, C] }].
- A `filter` on a `readData` element is end-to-end usable (pair it with the element's `readData` block —
  see the "Read data element" section), and on a `changeData` element it is effectively MANDATORY — the
  runtime refuses to update with an empty filter (see the "Modify data element" section). A `filter` on an
  Add/Delete-data task is serialized too, but those tasks' target object / values are not buildable yet, so
  THEIR filters are not end-to-end usable in this increment.
- On an EXISTING process, set/clear a filter via `modify-business-process` ops `setFilter`
  ({ op:"setFilter", elementName, filter }) and `clearFilter` ({ op:"clearFilter", elementName }).
  `setFilter` REPLACES the element's whole filter (there is no add-one-condition op); to add a condition,
  read the current filter first (below) and send the complete new filter.
- `describe-business-process` reads a filter back: an element carries a decoded `filter` (the same
  object / logicalOperation / conditions / groups shape) when it has one, so you can inspect it or
  round-trip it into a `setFilter`. A parameter reference comes back as its raw meta-path `expression`.
  A lookup value reads back as the raw id in `value` plus its resolved caption in `displayValue` (so
  `UsrStage` shows `Approved`, not a bare GUID); `displayValue` is read-only — omit it on `setFilter`.

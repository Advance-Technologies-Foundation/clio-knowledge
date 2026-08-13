clio MCP process-modeling guide — design Creatio business processes (BPMN)

== How clio builds processes (read first) ==
- clio makes no LLM call. You own the intent->BPMN translation: decide which elements the process
  needs, their parameters, and how they connect. The server-side ProcessDesignService package owns
  metadata serialization — you NEVER hand-author process metadata, filters, or column mappings.
- The build is DECLARATIVE: you describe the process (elements + flows + parameters + mappings) and
  clio builds + saves it in one call. Diagram layout is automatic (start leftmost, end rightmost, no
  overlap) — do not set positions.
- Tools:
  * list-user-tasks         — the user-task palette (name + uid); pass a name as `userTaskName`.
    CAVEAT: it lists RETIRED schemas as equal peers with no marker — `CallUserTask`, `EmailUserTask` and
    `SendEmailUserTask` are all still returned. And TWO shipped schemas share the caption "Send email":
    `EmailTemplateUserTask` is the live one (17 connections, creates an Activity), `SendEmailUserTask` has
    none and creates nothing. Always key on the schema NAME the tool returns, never on a caption.
  * create-business-process — build a NEW process from a JSON descriptor, and save it.
  * modify-business-process — edit an EXISTING process by an ordered list of operations.
  * describe-business-process        — read a process back as a structured graph (verify / explain).
    Also returns, per element: `connections[]` (bound "Connected to" links, raw + decoded), `deprecated`
    (the user-task schema is retired), and `writesConnectionsAtRuntime` — where FALSE is the answer that
    matters: it marks a process whose connections persist and compile while writing nothing. FALSE has two
    causes, fixed differently: the user task's runtime never writes them (change the element kind), or this
    element's activity-creation gate is shut (set `CreateActivity` true). `null` means not established — not
    false, and NOT permission: a non-user-task element, an unresolvable user-task schema and a user task
    outside the supported six all report `null`. `setConnections` is refused on `false` AND on `null`; only
    `true` means it is accepted.
  * validate-process-graph  — pre-check a planned graph against the connection rules R1-R17.

== What you can build today (create-business-process) ==
- NOT in a build descriptor: the "Connected to" links of an Activity a task creates. Add the element
  first, then bind them with `modify-business-process` → `setConnections` (see "Activity connections").
- Events: `startEvent` (Simple start), `signalStart` (record signal: add/modify/delete), `endEvent`.
- Activities: `userTask` referencing any task from list-user-tasks via `userTaskName`
  (aliases `readData`->ReadDataUserTask, `performTask`->ActivityUserTask). CAVEAT: `readData` (and the
  other data-operation tasks) PLACES an UNCONFIGURED element — its source object, filters, and columns
  cannot be set yet, so the step does nothing useful until a human configures it in the designer. Say so
  when you use it; do not present the result as a working data operation.
- Sequence flows; process-level parameters (with an optional constant default value); element-parameter mappings.
- `useBackgroundMode` on ANY element (it is a platform property of every process element, not signal-specific);
  change it later on an EXISTING element with the `setElement` op
  (`{ "op": "setElement", "elementName": "task1", "elementUpdate": { "useBackgroundMode": false } }`):
  `true` runs it asynchronously via the background scheduler, `false` inline. OMIT it to keep the element
  kind's own default, which mirrors the visual designer's palette — a `signalStart` defaults to background
  mode, so a signal-started process runs asynchronously and its effects appear a moment after the record is
  saved. The platform ANDs the flag with the global `UseBackgroundProcessMode` setting (on by default), so
  with that setting off background mode is inactive regardless — and since the platform then does not
  persist the flag at all, `useBackgroundMode: true` is REJECTED with a clear error on such an environment
  instead of being silently dropped. `false` is always accepted (inline execution is what that environment
  already does). `describe-business-process` reports the effective value per element, so it round-trips.
- A data source `filter` on a `signalStart` to restrict WHICH records fire the trigger (see the
  "Data source filters" section below).
- NOT yet buildable: gateways, conditional/default flows, timer/message start, intermediate events,
  sub-process, the Read/Add/Modify/Delete-data target object + read config (so a `filter` on a data
  task is serialized but not end-to-end usable yet — only the `signalStart` filter is). Use the catalog
  below to reason about a solution and to READ existing processes (`describe-business-process`); don't
  expect to build those types in this increment.

== Descriptor (create-business-process) ==
{
  "name": "UsrSchemaCode", "caption": "Title", "packageName": "Custom",
  "elements": [
    { "name": "Start1", "type": "startEvent" },
    { "name": "task1",  "type": "performTask", "caption": "..." },
    { "name": "End1",   "type": "endEvent" }
  ],
  "flows":      [ { "source": "Start1", "target": "task1" }, { "source": "task1", "target": "End1" } ],
  "parameters": [ { "name": "MyText", "type": "Text", "direction": "In", "caption": "..." } ],
  "mappings":   [ { "elementName": "task1", "elementParameter": "<ParamName>", "processParameter": "MyText" } ]
}
- `name` is the local element handle (the schema element Name, a string code) used by flows
  (`source`/`target`) and mappings (`elementName`). Creatio identifies an element by this Name plus a
  UId GUID; the platform reserves "Id" for the GUID, so the handle is `name`, not `id`. A `userTask`
  element auto-carries the task's parameters; map values into them with `mappings`. For a record trigger
  use `signalStart` (next section).

== Trigger a process on a record event ("run on save" of a page/record) — READ THIS ==
- When the goal is "run a process when a record is saved / added / changed / deleted" (e.g. on a page
  like UsrXxx_FormPage), that is a PROCESS trigger, NOT page logic. Make the process START with a
  Signal start element bound to the object. Do NOT add a client-side save handler
  (`crt.SaveRecordRequest` / any page handler) to launch a process on save — that is the wrong tool and
  a fragile workaround. The signal start is the platform-native, declarative trigger.
- Build it with `create-business-process`. The start element is:
    { "name": "Start1", "type": "signalStart", "signal": { "entity": "<EntityName>", "on": "modified" } }
  then the activity (e.g. a Perform task / `performTask` that shows a Task), then an `endEvent`,
  wired Start1 -> activity -> end. (`entity` is the page's object, e.g. UsrTestRunButton.)
- `on` is a SINGLE event: "added" | "modified" | "deleted" (the designer has no combined
  "added or modified"). "On save" of a record edited on a page = "modified"; a brand-new record = "added".
- A "modified" trigger fires on ANY field change by default. To restrict it to fire ONLY when specific
  columns change, add `changedColumns` (an array of column NAMES on the trigger entity) to the signal:
    { "name": "Start1", "type": "signalStart",
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
    { "name": "Start1", "type": "signalStart",
      "signal": { "entity": "UsrTestRunButton", "on": "modified" },
      "filter": { "object": "UsrTestRunButton",
        "conditions": [ { "column": "UsrName", "comparison": "equal", "value": "Start" } ] } }
  Use the entity COLUMN name (here `UsrName`), not the field caption ("Name").
- To convert an EXISTING process to start on a record event, use `modify-business-process`:
  removeElement the current start, addElement a `signalStart`, addFlow signalStart -> (first activity).
- To change an EXISTING signal's trigger or tracked columns IN PLACE (without re-adding it), use the
  `setSignal` op — it preserves the element and its flows:
    { "op": "setSignal", "elementName": "Start1",
      "signal": { "on": "modified", "changedColumns": ["Amount"] } }
  Partial update: omit `changedColumns` to clear column tracking (fire on any change), omit `on` to keep the
  current change type, and include `entity` only to retarget the trigger object (retargeting clears any
  filter bound to the old entity).

== Data source filters (signalStart trigger condition) ==
- A `filter` declares, high-level, WHICH records a filtered element acts on. The server serializes it to
  the platform Terrasoft.FilterGroup — you NEVER hand-write the escaped filter JSON.
- Usable today on a `signalStart` (restrict the record trigger). Shape:
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
  ({ elementName, parameter } — another element's output), `expression` (a raw token), or `macro` (a
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
  `value`, a `macro`, a `datePart`, or isNull/isNotNull — NOT `processParameter` / `elementParameter` /
  `expression`. The signal is evaluated to decide WHICH records start the process, BEFORE any process
  instance exists, so a parameter / element output / meta-path reference has no value yet. The server
  REJECTS a parameter reference on a signal filter (the visual designer likewise hides the "select
  parameter" option for signal starts). Parameter references are valid only on a data-operation element
  filter (Read/Add/Modify/Delete data) — which is not end-to-end buildable yet (see below), so in practice a
  buildable filter today uses value / macro / datePart only.
- `datePart` (optional, LEFT-hand modifier — NOT a right-hand source): extract a calendar/clock part from a
  Date/DateTime `column` and compare that part instead of the whole date. `Year` | `Month` | `Day` |
  `Week` | `Weekday` | `Hour` extract an INTEGER — pair with an integer `value` (a signalStart filter
  allows only a constant `value`/`macro`/`datePart`, never a `processParameter` — see the restriction above):
  `{ "column": "CreatedOn", "datePart": "Year", "comparison": "equal", "value": "2026" }` reads
  `Year(CreatedOn) = 2026`. `HourMinute` is the exception — it extracts the TIME-OF-DAY and compares it to a
  `value` in `HH:mm[:ss]` form: `{ "column": "CreatedOn", "datePart": "HourMinute", "comparison": "equal",
  "value": "14:30" }` reads `HourMinute(CreatedOn) = 14:30`. Combines with any comparison (`greaterOrEqual`,
  …); it modifies the left side, so it is independent of the right-hand source choice (but do not use it with
  a `macro`).
- Groups nest to any depth: A AND (B OR C) = conditions:[A] + groups:[{ "logicalOperation":"or",
  conditions:[B, C] }].
- A `filter` on a data task (Read/Add/Modify/Delete data) is serialized too, but those tasks' target
  object / read config is not buildable yet, so data-task filters are NOT end-to-end usable in this
  increment — use the signalStart filter.
- On an EXISTING process, set/clear a filter via `modify-business-process` ops `setFilter`
  ({ op:"setFilter", elementName, filter }) and `clearFilter` ({ op:"clearFilter", elementName }).
  `setFilter` REPLACES the element's whole filter (there is no add-one-condition op); to add a condition,
  read the current filter first (below) and send the complete new filter.
- `describe-business-process` reads a filter back: an element carries a decoded `filter` (the same
  object / logicalOperation / conditions / groups shape) when it has one, so you can inspect it or
  round-trip it into a `setFilter`. A parameter reference comes back as its raw meta-path `expression`.
  A lookup value reads back as the raw id in `value` plus its resolved caption in `displayValue` (so
  `UsrStage` shows `Approved`, not a bare GUID); `displayValue` is read-only — omit it on `setFilter`.

== Build recipe (intent -> running process) ==
1. Translate the request into a graph: one start event, the activities, the sequence flows, one or
   more end events; plus process parameters and the value mappings between them.
2. (recommended) `validate-process-graph(graph)` -> fix every error-severity finding.
3. `list-user-tasks` -> pick the exact `userTaskName`(s) for your activities.
4. `create-business-process(descriptor)` -> builds + saves in one call (layout is automatic).
5. Verify: `describe-business-process` (element types, user-task names, parameter sources + direction + isResult
   — an output you can map FROM has `isResult:true` or `direction:"Out"`; the signal trigger) /
   `execute-esq` (VwProcessLib by caption).
6. Change it later with `modify-business-process` (ops: addElement / removeElement / addFlow / removeFlow /
   addParameter / addMapping / setParameter / removeParameter / setFilter / clearFilter / setSignal /
   setElement / setConnections / clearConnections — same parameter/mapping/filter/signal shapes as a
   build; setSignal reconfigures an existing signalStart's record trigger + tracked columns in place,
   setElement changes element-level fields (useBackgroundMode) in place on any element kind,
   setConnections/clearConnections bind and unbind an Activity's "Connected to" links (see below)).
- File-design-mode caveat: on an FSD stand a built process is saved to the file system (the designer
  sees it) but is NOT runtime-active until it is loaded FS->DB and published — so a signal won't
  physically fire yet.

== Modifying an existing process — safety rules (modify-business-process) ==
- ALWAYS `describe-business-process` first, and re-describe after the edit to verify the result.
- The modify path runs NO structural validation (only the create path validates the graph):
  `removeElement` / `removeFlow` can leave the process unreachable or with dangling paths and the save
  still succeeds. `removeElement` also CASCADES — it deletes every flow touching the element and the
  mappings TARGETING it, but does NOT re-join the flow across the gap, and mappings/values READING the
  removed element's outputs may survive as dangling references. Add the bridging `addFlow` in the same
  operations array, then re-describe and clean up any leftover references to the removed element.
- Before removals, run `validate-process-graph` on the graph AS IT WILL BE after your operations
  (describe output + your planned ops applied), and confirm destructive removals with the user.
- If describe shows constructs the builder cannot create (gateways, conditional/default flows,
  sub-process, timer/message/intermediate events), they survive a save untouched as data — but you CAN
  still remove or rewire them by name and nothing will warn you. Treat such a process as high-risk:
  prefer additive edits, do not remove or rewire those elements, and tell the user what you left alone.
- Every modify re-applies the automatic layout to the WHOLE diagram: a hand-arranged multi-lane or
  branched diagram is flattened into generated left-to-right rows (process data intact, manual layout
  lost). Warn the user before editing a process with a curated diagram.

== Element catalog (data-id -> label -> purpose) ==
(The `data-id` strings below are the vocabulary for `validate-process-graph` and for reasoning about /
reading processes. To BUILD, map them to the create-business-process `type` + `userTaskName`: events
`startEvent`/`startEventSignal`->`signalStart`/`endEvent`; a user/system task -> `type:"userTask"` with
`userTaskName` from list-user-tasks, e.g. Perform task = `performTask`/ActivityUserTask, Read data =
`readData`/ReadDataUserTask.)
System actions (palette group "System actions"):
- `readDataUserTask`  Read data    — read first record / aggregate / count / collection of an object.
    Setup fields: DataReadMode, EntitySchemaSelect (object), filters, SortByColumn_N, ColumnSelectMode
    (designer-only for now — the builder cannot set them; a built Read data lands unconfigured).
- `addDataUserTask`   Add data     — create record(s) in background; one-record mode returns only the Id.
- `changeDataUserTask` Modify data — bulk-update matched records (same values to all).
- `deleteDataUserTask` Delete data — delete matched records.
- `formulaTask`       Formula      — compute a value (math/string/date/bool) into an output param.
- `scriptTask`        Script task  — custom C# (ends with `return true;`; needs publication).
- `webService`        Call web service — call a registered service; outputs Success + Http status code.
- `callActivity`      Sub-process  — run another process (must start with a Simple start); multi-instance over a collection.
- `userTask`/`*UserTask` — user/system tasks (Perform task, Open edit page, Send email, Approval, etc.).
User actions: `activityUserTask` Perform task, `userQuestionUserTask` User dialog,
  `openEditPageUserTask` Open edit page, `autoGeneratedPageUserTask` Auto-generated page,
  `preconfiguredPageUserTask` Pre-configured page, `emailTemplateUserTask` Send email, `approvalUserTask` Approval.
Events: `startEvent` Simple start, `startEventSignal` Signal start (record add/modify/delete or custom
  signal), `startEventTimer` Start timer (schedule/CRON), `startEventMessage` Start message, intermediate
  catch/throw (`intermediateCatchEvent*`/`intermediateThrowEvent*`), `endEvent` End/Terminate.
Gateways: `exclusiveGateway` (OR), `parallelGateway` (AND), `inclusiveGateway` (OR), `eventBasedGateway`.
Flows: sequence (default `connect`), conditional (setup -> conditionalConnection), default (setup -> defaultConnection).

== Parameters / mapping / formulas ==
- Process parameters (`parameters[]`): { name, type (Text/Long text/Integer/Float/Money/Boolean/Date/Date-time/Time/Guid/Lookup),
  direction (In/Out/Variable/Internal), caption, description, or referenceSchema = an object name (e.g. City) to make
  it a Lookup to that object }, and an optional value (a constant default; NOT valid for Date / Date-time /
  Time / Lookup — those defaults are formula macros, see the date/lookup macro rule below). A user-task
  element's own parameters come from the task. The same shape is
  used by modify-business-process `addParameter`. Supported types: Text, Long text, Integer, Float, Money,
  Boolean, Date, Date-time, Time, Guid, and Lookup — other types (composite / entity / file / ...) are not
  supported yet.
- To create a process parameter that mirrors an element parameter's EXACT type (e.g. expose a user-task
  OUTPUT for mapping with NO conversion), set `typeFromElement` + `typeFromElementParameter` instead of
  `type`/`referenceSchema` — the data value type (and lookup reference object) is copied verbatim.
- Edit a parameter with `setParameter` (parameterName + parameterUpdate: any of caption/description/code/
  direction/referenceSchema/value, applied in place — the UId and its references are preserved). A
  data-type change is rejected, and referenceSchema can only RE-TARGET a parameter that is already a
  Lookup (it cannot convert a scalar to a Lookup). Do NOT set a Date / Date-time / Time / Lookup default
  through setParameter `value` — such defaults are formula macros, not plain constants; use the
  mapping + `expression` path below (addMapping overwrites, so it edits a default exactly as it
  creates one).
- Remove a parameter with `removeParameter` (parameterName; blocked when another parameter's value or an
  element mapping still references it).
- Mappings (`mappings[]`): bind a TARGET parameter to a SOURCE.
  TARGET — `elementName` + `elementParameter` (an element input) OR `targetProcessParameter`
  (a process parameter, e.g. expose an element's OUTPUT as a process output).
  SOURCE — exactly ONE of: `sourceElement` + `sourceElementParameter` (another element's OUTPUT parameter) |
  processParameter (a process parameter by name) | value (a constant) | expression (a raw formula).
  Identifying an OUTPUT for `sourceElementParameter`: in `describe-business-process` output an element parameter
  is usable as a mapping source when `isResult: true` OR `direction: "Out"`. Most user-task outputs come back as
  `isResult: true` with `direction: "Variable"` (the platform reports element params as Variable), so detect
  outputs by `isResult`, NOT by `direction` alone.
  Parameter-to-parameter mappings require COMPATIBLE TYPES (target-driven, mirroring the visual designer);
  incompatible pairs are rejected:
  * text -> text: any text source into a base-text target; Phone/Email/Web/Rich targets accept only the
    SAME extra type or a base-text source (never a different extra type);
  * Money <-> Float map to each other; Integer maps ONLY to Integer (NOT to Float or Money);
  * date/time is asymmetric via Date-time: a Date-time target accepts Date/Date-time/Time; a Date target
    accepts Date/Date-time; a Time target accepts Time/Date-time; Date <-> Time is NOT allowed;
  * Lookup: the same reference object on both sides; a Guid source INTO a lookup target IS allowed;
  * Boolean only from Boolean; any other type: exact match only. When the target must match a source
    exactly, mirror it with `typeFromElement` instead of guessing.
  `processParameter` flows a process input into the
  field (the server builds the correct reference); `expression` is a raw C#-like formula passed through UNVALIDATED — the backend (unlike the visual designer) does NOT check it, so a wrong token / function / type fails only at RUNTIME. Do NOT invent or guess formulas: formula-authoring guidance (token format + the allowed function set) is not available yet. Prefer `value` / `processParameter` / `sourceElement`; use `expression` ONLY with a formula you already know is correct (user-supplied, or copied verbatim from an existing process via describe-business-process), e.g.
  `[#SysVariable.CurrentUserContact#]`, `[#SysVariable.CurrentDateTime#].AddDays(3)`.
- UNBOUND element INPUT parameters are NOT listed by `describe-business-process` (it returns only
  value-bearing parameters and outputs) — absence from describe does NOT mean the parameter does not
  exist. Input parameter names come from the user task's schema (for a custom task, the parameters it
  was created with); a wrong `elementParameter` name fails the build with a clear error and nothing is
  saved — never invent names silently.
- To CHANGE a bound value, send `addMapping` again for the same target — it overwrites the binding in
  place (like the designer). There is NO clear/unbind operation (no removeMapping): if asked to
  "remove" a value, say clearing is not supported yet and offer to overwrite it instead.
- Date / Date-time / Time DEFAULT VALUES are the ONE formula you may author (an EXCEPTION to the
  "don't invent formulas" rule): the designer stores a date/time constant as a formula macro (a Script
  source), NOT a plain `value` (a `ConstValue`). Set it via `expression` — for a process-parameter
  default, a mapping with `targetProcessParameter` + `expression`. The inner format is FIXED (NOT ISO,
  NOT locale): `dd.MM.yyyy` and 24-hour `HH:mm`.
  Date → `[#DateValue.dd.MM.yyyy#]` (e.g. `[#DateValue.03.07.2026#]`);
  Date-time → `[#DateTimeValue.dd.MM.yyyy HH:mm#]` (e.g. `[#DateTimeValue.03.07.2026 02:15#]`);
  Time → `[#TimeValue.HH:mm#]` (e.g. `[#TimeValue.12:20#]`). A LOOKUP default is the same kind of macro — set via `expression` as `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` (both are GUIDs: the referenced OBJECT's schema UId, NOT its name, and the chosen RECORD's Id, e.g. `[#Lookup.5ca90b6a-…(City object).1548d3d2-…(a City record)#]`). You cannot guess these ids — copy the token from an existing process (`describe-business-process`) or resolve the object/record ids first; a bare record id as `value` will NOT work.
  EXCEPTION — an Activity CONNECTION: there you send a bare `recordId` to `setConnections` and the server
  composes the token from the target column, so hand-writing it is both unnecessary and easy to get wrong.
- To read another element's output, PREFER the structured `sourceElement` + `sourceElementParameter` mapping (above) — the server builds the correct reference. Do NOT hand-write an element-output reference —
  in the saved metadata it is a server-generated UId meta-path (`[#...[Element:{uid}].[Parameter:{uid}].[EntityColumn:{uid}]#]`), NOT a friendly `Element.Property` path, so you cannot author it — ALWAYS use `sourceElement`. Formulas are strictly typed (convert with `.ToString()` etc.).

== Activity connections ("Connected to") ==
- WHAT: which records the Activity a task creates is attached to — a contact, an account, and whatever else
  the environment registers as a connection; the set is per-environment, never a fixed list.
  It is functional, not decorative: set, the task appears on the connected record's Activities detail and
  its Timeline and the page fields are pre-filled; unset, none of that happens. An email counts as
  "processed" only with Account or Contact PLUS one further connection.
- HOW: `modify-business-process` → `setConnections` with `elementName` and
  `connections:[{ column, <exactly ONE source> }]`. Sources: `recordId` (a fixed record) |
  `processParameter` | `sourceElement` + `sourceElementParameter` | `expression` (a raw macro — for the
  CURRENT USER see the dedicated rule below, it is the one macro you may author here).
  `referenceSchema` is optional, belongs to `recordId` ALONE, and is a
  CHECK rather than a source — sending it with any other source is refused, because the entity of those is
  whatever the source resolves to.
- `recordId` NEEDS NO SCHEMA UId. The server composes `[#Lookup.{schemaUId}.{recordId}#]` from the target
  column's own reference entity, so send the bare record id. This is the one place the "you cannot guess
  these ids" warning ABOVE does not apply — for a connection, do NOT hand-write the Lookup token.
- CURRENT USER — "link it to me / to my contact / to my account". This is the ONE macro you may author on a
  connection, because the set is CLOSED and named here. Send it as `expression`, chosen by the target
  column's own entity: a Contact column -> `[#SysVariable.CurrentUserContact#]`; an Account column ->
  `[#SysVariable.CurrentUserAccount#]`; a SysAdminUnit (user) column -> `[#SysVariable.CurrentUser#]`.
  Those three are the WHOLE set usable as a connection. Do not invent a fourth (`CurrentUserAccountId`,
  `CurrentAccount`, …), and do not go looking one up: system variables are neither an entity nor an entity
  schema, so `odata-read` answers 404 and `find-entity-schema` answers empty for them — that is those tools
  being right, not the variable being absent. Spell them EXACTLY as above: a name outside the set is not
  refused on write, it is stored, and the process then fails to COMPILE later with nothing pointing back at
  the connection.
  One caveat that is data rather than syntax: `CurrentUserAccount` writes EMPTY when the running user's
  contact has no account — where `CurrentUserContact` raises an error in the same situation, the Account
  side stays silent. If the Account link comes back unset, check the user's contact before suspecting the
  macro.
- UPSERT, keyed on `column`. The columns you list are set or re-set; every column you do NOT list is left
  alone. There is no collection-replace and no implicit clearing — so changing one connection can never
  disturb another, and clearing is only ever explicit via `clearConnections`.
- Changing a connection, INCLUDING across dialects (a process parameter → a fixed record), is the same
  `setConnections` call with a new source. Re-sending an unchanged request is idempotent.
- `clearConnections` takes `connections:[{ column }]` and UNBINDS — the element parameter stays. A source
  on a clear entry is rejected. Clearing an already-unbound column is a no-op, not an error. It REPORTS
  what it cleared, and you need that: a cleared connection disappears from describe, so afterwards
  "cleared" and "never bound" are indistinguishable from the read-back alone.
- READ IT BACK with `describe-business-process`: each element carries `connections[]`, every entry giving
  both the raw macro (`value`) and a decoded source in exactly the shape `setConnections` accepts, so you
  can feed it straight back — with THREE exceptions that refuse on re-apply, all of them values a designer or
  an older build stored:
  (1) a fixed-record connection whose stored macro names a different entity than its column. TWO remedies,
      and they are not interchangeable: re-send the raw `value` as `expression` to keep the stored macro
      exactly as it is, or omit `referenceSchema` to re-point the connection at the column's OWN entity —
      which rewrites the macro and is a repair, not a re-apply. Choose deliberately;
  (2) a stored value with no macro shape at all (check `source`; it comes back as `expression`) — refused as
      "not a platform macro", because a bare value cannot be a source. Use `recordId`;
  (3) a stored value that IS macro-shaped but from a family that cannot hold a record id — `DateValue`,
      `DateTimeValue`, `TimeValue`, `BooleanValue`. `[#SysSettings...#]` is the one family accepted instead
      of refused, with a warning (below), precisely so designer-authored processes stay re-appliable.
  Each entry also carries `registered` — `false` means the value IS written at run time but the connection
  is invisible to every registry-reading feature, the same caveat as the write warning below — and `source`,
  the platform value source. Only BOUND connections appear, so absence does NOT mean the column cannot be
  connected; and the WHOLE array is absent when the host entity cannot be resolved or the registry cannot be
  read, so "no connections" is never verified-empty. A macro this build does not recognise degrades to
  `expression` rather than breaking the read.
- WHEN IT IS REFUSED, and why each refusal is worth reading rather than retrying:
  * the user task is not one connections are supported on. The supported set is exactly SIX —
    `ActivityUserTask` (`performTask`), `EmailTemplateUserTask`, `UserQuestionUserTask`,
    `OpenEditPageUserTask`, `AutoGeneratedPageUserTask`, `PreconfiguredPageUserTask` — and anything else,
    including a CUSTOM user task and `approvalUserTask`, is refused with the supported list quoted. A
    non-user-task element is refused too (it creates no record), as is one whose user-task schema does not
    resolve on the environment. This is the most likely refusal in practice, so check it first;
  * the user task's runtime never writes connections (`CallUserTask` builds its Activity directly;
    `EmailUserTask` and `SendEmailUserTask` have none; `readData` creates no activity at all) — model a
    call as `performTask` with the Call activity category instead, and set that category with `addMapping`
    (see the next bullet — `ActivityCategory` is not a connection);
  * the column is not a CONNECTION at all — `ActivityCategory` and `ShowInScheduler` are written through
    their own path and with their own encoding, so use `addMapping` for them. Binding one as a connection
    would set the column and silently degrade the element, which is why it is refused rather than accepted;
  * they would not TAKE EFFECT on this element — almost always `CreateActivity` left at its `false`
    default, which produces a process that saves, compiles, runs green and writes nothing. The refusal
    quotes the exact operation to PREPEND to your own array, so the fix costs one array element, not
    another round trip. `performTask` never hits this: it has no such parameter. A manual-send
    `EmailTemplateUserTask` does not either — the manual sender has no gate;
  * the column is not one this element can carry, or the host entity has no such column at all. Those are
    DIFFERENT diagnoses: the second needs a data-model change (add the lookup column to Activity and
    register it), which `setConnections` deliberately does not make;
  * an `expression` that is not a platform macro at all (it must look like `[#...#]`; a bare value is
    refused — use `recordId`), or one whose macro family cannot hold a record reference (a date, time or
    boolean constant);
  * `referenceSchema` sent without `recordId` — it is a check on the fixed-record source only, so accepting
    it elsewhere would ignore it;
  * a malformed `recordId`, a column that references no entity, or a `processParameter` / `sourceElement`
    of an incompatible type (same type group, and for a lookup the same reference entity — a `Guid` or a
    same-entity Lookup parameter is what works).
- WHEN THE CONNECTION DOES NOT EXIST YET — linking an activity to a record of YOUR OWN entity. This is the
  common ask ("add a button that creates a task linked to this record"), and it is the ONE case that needs a
  DATA-MODEL change, which `setConnections` deliberately does not make for you.
  Do NOT decide whether you are in that case by inspecting the object first — let the OPERATION tell you,
  because the surfaces disagree with each other. Measured for ONE lookup column on one environment: the
  physical `Activity` table carried it, `get-entity-schema-properties` listed it, the object designer did not
  show it, and a process wrote its value successfully. In the other direction, several connection columns
  existed physically while being ABSENT from the schema. WHICH columns those are is a property of the product
  and the installed package chain, not of Creatio — so no list belongs here, including a list of "the
  connections Creatio ships": whatever it named would be wrong on some environment. The refusals ARE the
  check, and they distinguish three states:
  * `<host> has no '<column>' column` — the data-model change below is required;
  * `the column exists on <host> but no connection-registry row registers it and this element's user task
    declares no parameter for it` — only step 2 is required;
  * anything else, including success — there was nothing to add.
  1. add a Lookup column to `Activity` IN THE PACKAGE THAT OWNS THE REFERENCED ENTITY — not in `Custom`, and
     not as a matter of taste. `Custom` is the LAST package: it depends on the others and nothing depends on it
     (measured — `Custom` depends on the platform core, the app package and a product package; no edge points
     back). So a schema in the
     entity's own package cannot reference a column placed in `Custom` without adding the REVERSE edge, and that
     inverts an existing one: the save is refused with "Cyclic dependencies detected", naming
     `EntityColumnValues.Column.<yours>`. Placed in the referenced entity's own package the column needs NO new
     dependency at all, and the environment's existing custom sections show the same shape — each carries its
     own replacing `Activity` layer.
     The call is `update-entity-schema`, which is NON-RESIDENT, so send it through `clio-run`. Args:
     `environment-name`, `package-name` (the REFERENCED entity's), `schema-name: "Activity"`, and
     `operations` — an ARRAY of operation objects, one here:
     `{"action":"add","column-name":"Usr<YourEntity>","type":"Lookup","reference-schema-name":"<your entity>","indexed":true}`.
     The first four keys are all required — omitting `column-name` is the easy mistake, since the column being
     added is named nowhere else; `indexed` is optional and worth setting on a column you will filter by.
     Measured: the column lands and the schema republishes in ~13 s, and reads back as `source: own`.
     A `Reference schema '<your entity>' was not found` refusal means the TARGET package cannot see the
     REFERENCED entity — that, not the `Activity` side, is the dependency that blocks, and the placement above
     is what makes it a non-issue.
     CAVEAT — measured only where that package ALREADY had a replacing `Activity` layer; with no layer yet
     this step takes a path nothing has exercised.
  2. register the column as a connection — ONE bound row in `EntityConnection`, through the
     `create-data-binding-db` tool (also non-resident, also via `clio-run`). Args: `package-name` (yours),
     `schema-name: "EntityConnection"`, `binding-name` (e.g. `"EntityConnectionUsr<YourEntity>"`), and
     `rows`: `[{"values":{"SysEntitySchemaUId":"c449d832-a4cc-4b01-b9d5-8a12c42a9f89","ColumnUId":"<u-id>"}}]`.
     `SysEntitySchemaUId` is Activity's ROOT schema UId — that literal. The column's `u-id` comes from
     `get-entity-schema-properties` (resident, call it natively), NOT from
     `get-entity-schema-column-properties`, whose response carries no `u-id` at all. `rows` is load-bearing:
     without it the tool creates an EMPTY binding and nothing is registered. The package must be non-foreign.
  3. `setConnections` on the element. The element may predate the column by any amount — the operation
     creates the element parameter when the user task declares none.
  Skipping step 2 is not fatal, and the mechanism is worth knowing rather than guessing: the binder resolves a
  column through the registry OR through a parameter the user task already DECLARES, so a declared connection
  binds and writes — with a caveat in the log — even with no registry row. Measured: an `Opportunity`
  connection written by a process on an environment whose registry carried 17 rows, with a Next Steps
  component then displaying the activity. What registration buys is availability to EVERY element rather than
  only to a task that happens to declare that parameter, plus visibility to the surfaces that read the
  registry. After step 2 the designer may keep showing the old set until its caches refresh; the run-time
  write is unaffected.
  Do NOT offer, as a lighter alternative, writing the record's NAME into the activity's title or description.
  That produces no link — no Activities detail, no Timeline, no pre-filled fields — and the ask was a link.
- SUCCEEDS WITH A WARNING, two cases. A column that exists but has no connection-registry row IS written at
  run time, yet the connection is ignored by the record page's connections detail, Next Steps, email
  auto-relation rules and quick-add, and is normally absent from the designer's "Connected to" as well —
  except `Project`, which the designer injects client-side and DOES display. And an `expression` in the
  `[#SysSettings...#]` family is accepted unchecked: its value type cannot be read at design time, so a
  setting that does not hold a record id leaves the column empty at run time. Read the caveats — they arrive
  as `message-type: "Warning"` entries in `execution-log-messages`, NOT as a `warnings` field on the
  response, so finding no such field is not evidence there were none. Some are neutral acknowledgements (a
  column that was already unbound), not failures.
- `addMapping` is NOT deprecated — it remains the general primitive — but it only reaches a connection the
  element ALREADY declares as a parameter and fails with "has no parameter" otherwise; the two page tasks
  (`AutoGeneratedPageUserTask`, `PreconfiguredPageUserTask`) declare none, and since unbound element inputs
  are omitted from describe you cannot tell in advance. Prefer `setConnections`: it creates the parameter
  when one is needed, and adds the validation, the `recordId` ergonomics and the read-back.
- Connections are NOT graph edges. `validate-process-graph` neither checks nor is affected by them; R1-R17
  below are about sequence flows only.

== Connection rules R1–R17 (validate-process-graph enforces the structural subset: R1–R3, R7,
   R9–R15, R17; R4–R6, R8 and R16 are semantic or not yet enforced — verify those yourself.
   Validation pass ≠ buildable: the rules cover the FULL catalog incl. gateways and conditional
   flows, but only the "What you can build today" slice above can actually be built) ==
R1  Start event: no incoming flow; exactly one outgoing.
R2  End event: no outgoing flow; one or more incoming.
R3  Exactly one top-level start event; every path reaches an end event.
R4  Terminate end kills the whole instance; Simple end ends only its path.
R5  Start triggers: Simple=user/run; Signal(object)=record add/modify/delete; custom signal=broadcast; message=directed; timer=schedule/CRON.
R6  Diverging gateway: 1 in, >=2 out. Converging gateway: >=2 in, 1 out.
R7  Exclusive(OR) diverge: conditional flows + exactly one default; one path taken. Converge: first arrival, no sync.
R8  Parallel(AND) diverge: all out fire, plain sequence flows only. Converge: waits for all incoming.
R9  Inclusive(OR) diverge: conditional flows + required default; >=1 path. Converge: syncs active branches.
R10 Event-based gateway: each outgoing sequence flow leads directly to an intermediate catch event; first event wins.
R11 Parallel and event-based gateways must not carry conditional/default flows.
R12 Sequence flow: target runs after source. Multiple outgoing sequence flows = implicit parallel split.
R13 Conditional flow originates only from a gateway or an activity.
R14 Default flow is legal only if >=1 conditional flow leaves the same element; diverging Exclusive/Inclusive require a default.
R15 No orphan/unreachable nodes; every flow needs a valid source and target.
R16 Sub-process (callActivity) target must begin with a Simple start; collection mapping => multi-instance.
R17 (advisory) Add data one-record mode outputs only Id; chain a Read data for other fields.

Quick can/can't (source -> target via sequence flow): start->{activity,gateway,intermediate,end} ok,
never ->start (R1); end is a sink, never a source (R2); event-based gateway out must hit a catch event (R10).
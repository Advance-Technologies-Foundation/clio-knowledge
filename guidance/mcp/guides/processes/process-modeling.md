clio MCP process-modeling guide — design Creatio business processes (BPMN)

== Which process article to read ==
This article is the ENTRY POINT. It owns the build lifecycle: what the tools are, what the
descriptor looks like, what is buildable today, the recipe, the safety rules for editing an
existing process, and the element catalog. Everything else has its own article and its own
authoritative owner -- read the one your task needs instead of guessing:
  * `process-naming`               - N1-N10: the process caption and code, element captions and
                                     codes, parameter codes. Read it BEFORE you name anything.
  * `process-data-elements`        - start a process from a record event (signalStart), the Read
                                     data and Modify data elements, and the record filters all
                                     three share.
  * `process-parameters`           - process parameters, element-parameter mappings, type
                                     compatibility, and the date/time/lookup default macros.
  * `process-perform-task`         - the Perform task element: its parameter table, the performer
                                     layers, and what the runtime sets.
  * `process-send-email`           - the Send email element: mode, sender, recipients, subject,
                                     HTML body and its process macros.
  * `process-approval`             - the Approval element: the object and record under approval, who
                                     approves, delegation and the two email notifications.
  * `process-activity-connections` - the "Connected to" links of the Activity a task creates,
                                     and the R1-R17 connection rules.
Each is sized to be read WHOLE through get-guidance. Do not infer a rule that lives in another
article from what this one says; read that article.

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
  * validate-process-graph  — pre-check a planned graph against the connection rules R1-R17
    (the rules themselves are in `process-activity-connections`).

== What you can build today (create-business-process) ==
- NOT in a build descriptor: the "Connected to" links of an Activity a task creates. Add the element
  first, then bind them with `modify-business-process` → `setConnections` (see `process-activity-connections`).
- Events: `startEvent` (Simple start), `signalStart` (record signal: add/modify/delete), `endEvent`.
- Activities: `userTask` referencing any task from list-user-tasks via `userTaskName`
  (aliases `readData`->ReadDataUserTask, `changeData`->ChangeDataUserTask, `performTask`->ActivityUserTask).
  A `readData` element is CONFIGURABLE via its `readData` block — source object, first-record mode, result
  columns, sort, plus a record `filter` (see `process-data-elements`). A `changeData` element
  is CONFIGURABLE via its `changeData` block — target object + column values, plus a record `filter` (see
  `process-data-elements`). CAVEAT: Add data and Delete data still place an UNCONFIGURED
  element — their target object and values cannot be set yet, so those steps do nothing useful until a
  human configures them in the designer. Say so when you use one; do not present such a result as a working
  data operation.
- Send email: `sendEmail` (the Send email element / EmailTemplateUserTask) is BUILDABLE and fully
  configurable through its `email` block — mode, sender, recipients, subject, HTML body, options and the
  manual-mode performer. `process-send-email` owns the contract and its limits.
- Approval: `approval` (the Approval element / ApprovalUserTask) is BUILDABLE and configurable through its
  `approval` block — the object and record under approval, WHO approves, delegation and the two email
  notifications. What it does NOT give you is branching on the verdict: routing approved/rejected/canceled
  needs a gateway, which is not buildable, so say "a configured approval STEP, not an approval FLOW".
  `process-approval` owns the contract and its limits.
- Sequence flows; process-level parameters (with an optional constant default value); element-parameter mappings.
- `useBackgroundMode` on any element that OFFERS it (it is not signal-specific, but neither is it universal —
  four element kinds REMOVE the control outright, so a rule of the form "tick it on every element" states an
  impossible requirement). Verified against the designer's own property pages (`CrtProcessDesigner`,
  2026-08-21): `ProcessTerminateEventPropertiesPage`, `ProcessTimerStartEventPropertiesPage`,
  `IntermediateThrowMessagePropertiesPage` and `SendEmailUserTaskPropertiesPage` each apply a schema-diff
  `remove` operation against the background-mode control; a Terminate element therefore CANNOT be put in
  background mode and its `false` is correct, not an oversight. `EmailTemplateUserTask` — the `sendEmail`
  element kind — INSERTS the control and so does take the flag; do not confuse it with `SendEmailUserTask`,
  which does not. For a SIGNAL-STARTED process set the flag on every element that offers it — the trigger fires
  with no one waiting at a screen, so there is nothing for inline execution to return to. The designer gates the control on
  `canUseBackgroundProcessMode()` = the `UseBackgroundProcessMode` feature enabled AND the schema not embedded,
  so on an environment with that feature off the control is absent everywhere and there is nothing to set;
  change it later on an EXISTING element with the `setElement` op
  (`{ "op": "setElement", "elementName": "NotifyAccountOwner", "elementUpdate": { "useBackgroundMode": false } }`):
  `true` runs it asynchronously via the background scheduler, `false` inline. OMIT it to keep the element
  kind's own default, which mirrors the visual designer's palette — a `signalStart` defaults to background
  mode, so a signal-started process runs asynchronously and its effects appear a moment after the record is
  saved. The platform ANDs the flag with the global `UseBackgroundProcessMode` setting (on by default), so
  with that setting off background mode is inactive regardless — and since the platform then does not
  persist the flag at all, `useBackgroundMode: true` is REJECTED with a clear error on such an environment
  instead of being silently dropped. `false` is always accepted (inline execution is what that environment
  already does). `describe-business-process` reports the effective value per element, so it round-trips.
- A data source `filter` on a `signalStart` to restrict WHICH records fire the trigger (see the
  "Data source filters" section of `process-data-elements`).
- NOT yet buildable: gateways, conditional/default flows, timer/message start, intermediate events,
  sub-process, the Add/Delete-data target object + values (a `filter` on THOSE tasks is serialized
  but not end-to-end usable — the buildable filters are `signalStart`, `readData` and `changeData`), and the Read data
  collection / count / aggregation modes (only the first-record mode builds; the others are designer-only).
  Use the catalog below to reason about a solution and to READ existing processes
  (`describe-business-process`); don't expect to build those types in this increment.

== Descriptor (create-business-process) ==
{
  "name": "UsrAccount_Onboard", "caption": "Account onboarding", "packageName": "Custom",
  "elements": [
    { "name": "OnboardingRequestedStart", "type": "startEvent",  "caption": "Onboarding requested" },
    { "name": "NotifyAccountOwner",       "type": "performTask", "caption": "Notify the account owner" },
    { "name": "EndOnboardingHandedOff",   "type": "endEvent",    "caption": "Onboarding handed off" }
  ],
  "flows":      [ { "source": "OnboardingRequestedStart", "target": "NotifyAccountOwner" },
                  { "source": "NotifyAccountOwner", "target": "EndOnboardingHandedOff" } ],
  "parameters": [ { "name": "AccountNameParameter", "type": "Text", "direction": "In",
                    "caption": "Account name" } ],
  "mappings":   [ { "elementName": "NotifyAccountOwner", "elementParameter": "<ParamName>",
                    "processParameter": "AccountNameParameter" } ]
}
- `name` is the local element handle (the schema element Name, a string code) used by flows
  (`source`/`target`) and mappings (`elementName`). Creatio identifies an element by this Name plus a
  UId GUID; the platform reserves "Id" for the GUID, so the handle is `name`, not `id`. A `userTask`
  element auto-carries the task's parameters; map values into them with `mappings`. For a record trigger
  use `signalStart` (see `process-data-elements`).
- EVERY code and caption in the examples across these process guides is N1-N10 compliant on purpose (see `process-naming`): copy their SHAPE, not just their fields. A generated `Start1` / `task1` / `End1` is the
  failure those rules exist to prevent, and an example is what a model copies first.

== Build recipe (intent -> running process) ==
1. Translate the request into a graph: one start event, the activities, the sequence flows, one or
   more end events; plus process parameters and the value mappings between them — and name them per
   N1-N10 in `process-naming`, which is what makes the result reviewable in the Process Designer.
2. (recommended) `validate-process-graph(graph)` -> fix every error-severity finding.
3. `list-user-tasks` -> pick the exact `userTaskName`(s) for your activities.
4. `create-business-process(descriptor)` -> builds + saves in one call (layout is automatic).
5. Verify: `describe-business-process` (element types, user-task names, parameter sources + direction + isResult
   — an output you can map FROM has `isResult:true` or `direction:"Out"`; the signal trigger). Verify through
   `describe-business-process`, not a raw `execute-esq`/`odata-read` of the process record (see the readiness
   bullet below).
6. Change it later with `modify-business-process` (ops: addElement / removeElement / addFlow / removeFlow /
   addParameter / addMapping / setParameter / removeParameter / setFilter / clearFilter / setSignal /
   setElement / setConnections / clearConnections — same parameter/mapping/filter/signal/readData/
   changeData/email shapes as a build; setSignal reconfigures an existing signalStart's record trigger +
   tracked columns in place, setElement changes element-level fields in place: `useBackgroundMode` on any
   element kind, `readData` / `changeData` on the matching data element only (see `process-data-elements` for their
   partial-update and source-retarget rules), and a sendEmail
   element's `email` block (a partial update; to/cc/bcc recipients MATCH-OR-APPEND — a new address is added,
   an identical one is a no-op, and none can be removed); setConnections/clearConnections bind and unbind an
   Activity's "Connected to" links (see `process-activity-connections`)).
- File-design-mode caveat: on an FSD stand a built process is saved to the file system (the designer
  sees it) but is NOT runtime-active until it is loaded FS->DB and published — so a signal won't
  physically fire yet.
- Do NOT run `compile-creatio` to "make a process runnable", and do NOT read a raw system record
  (`odata-read`/`execute-esq`) to decide readiness — read status back with `describe-business-process`.
  Inferring "needs a compile" from a raw column NAME is the trap here: a raw read of `VwSysProcess` (what
  `odata-read`/`execute-esq` returns for a process — verified: run_20260820_133837) surfaces per-process
  DIRTY flags — `NeedInstall`, `NeedUpdateSourceCode`, `NeedUpdateStructure` — that are ALL `true` on a
  freshly-saved process. None of them is a `compile-creatio` instruction (`NeedInstall` in particular is a
  DB-install marker meaning "finish installing this into the DB", never "compile"), and the same caution
  applies to any `NeedXxx` / `IsXxx` column reached through a raw read.
  WITHIN A PROCESS exactly two things pull a compile in, and both are C# YOU authored: a `scriptTask`,
  and a `userTask` carrying an after-activity-save script. Everything else — add/read/modify data,
  formulas, connections, signals, and USING an already-compiled user task — is applied and runs with no
  compile. This bullet scopes compilation to the PROCESS; other configuration schemas (source code,
  business objects, DCM, value lists, and a CUSTOM user-task schema — see the user-task note below) carry
  their own compile obligations and are NOT covered here.

== Set what was asked for, and nothing else ==
- An OPTIONAL field the request did not mention stays OUT of the descriptor. Filling it in changes
  behaviour the requester never chose (a flag left out keeps the platform's own value, which is not always
  the falsy one), and it destroys the "nobody decided this" signal — `describe-business-process` reports
  what is WRITTEN, so absence means "not set", never "off".
- Set an unrequested field only when the request implies it unambiguously. For fields that genuinely
  cannot be omitted the server REFUSES rather than defaulting, and names what is missing — so you will be
  told; never pre-empt that by inventing a value.

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
`readData`/ReadDataUserTask. TWO user tasks have their own dedicated build type and must NOT be built as a
generic `userTask`: `emailTemplateUserTask` -> `type:"sendEmail"` — full custom-message configuration
(mode/sender/recipients/subject/body/options/performer; no email templates), see `process-send-email`; and
`approvalUserTask` -> `type:"approval"` — the object and record under approval, the approver, delegation and
the two email notifications, see `process-approval`.)
System actions (palette group "System actions"):
- `readDataUserTask`  Read data    — read first record / aggregate / count / collection of an object.
    FIRST-RECORD mode is buildable via the element's `readData` block (source object, columns, sort) plus
    a `filter` — see `process-data-elements`. The other read modes (collection / count /
    aggregation) remain designer-only; describe reports them as `mode: "collection"` / `"function"`.
- `addDataUserTask`   Add data     — create record(s) in background; one-record mode returns only the Id.
- `changeDataUserTask` Modify data — bulk-update matched records (same values to all). BUILDABLE via the
    element's `changeData` block (target object + column values) plus a `filter` — see
    `process-data-elements`.
- `deleteDataUserTask` Delete data — delete matched records.
- `formulaTask`       Formula      — compute a value (math/string/date/bool) into an output param.
- `scriptTask`        Script task  — custom C# (ends with `return true;`; needs publication).
  - Compile note: a `scriptTask`, and a `userTask` carrying an after-activity-save script, are the two
    IN-PROCESS elements whose authored C# makes the process itself need a compile before it runs.
- `webService`        Call web service — call a registered service; outputs Success + Http status code.
- `callActivity`      Sub-process  — run another process (must start with a Simple start); multi-instance over a collection.
- `userTask`/`*UserTask` — user/system tasks (Perform task, Open edit page, Send email, Approval, etc.).
User actions: `activityUserTask` Perform task, `userQuestionUserTask` User dialog,
  `openEditPageUserTask` Open edit page, `autoGeneratedPageUserTask` Auto-generated page,
  `preconfiguredPageUserTask` Pre-configured page, `emailTemplateUserTask` Send email, `approvalUserTask` Approval.
Events: `startEvent` Simple start, `startEventSignal` Signal start (record add/modify/delete or custom
  signal), `startEventTimer` Start timer (schedule/CRON), `startEventMessage` Start message, intermediate
  catch/throw (`intermediateCatchEvent*`/`intermediateThrowEvent*`), `endEvent` End/Terminate — the
  BPMN catalog has both, but a `create-business-process` `endEvent` builds Terminate today (see N6).
Gateways: `exclusiveGateway` (OR), `parallelGateway` (AND), `inclusiveGateway` (OR), `eventBasedGateway`.
Flows: sequence (default `connect`), conditional (setup -> conditionalConnection), default (setup -> defaultConnection).
- Custom user-task compile rule: a CUSTOM user task is a `ProcessUserTask` SCHEMA, not a process element —
  its own C# methods are generated into the package assembly (it has no `IsInterpretable`; that property
  exists only on `ProcessSchema`), so CREATING or CHANGING one needs a compile before any process can use
  it. Merely REFERENCING an already-compiled user task by `userTaskName` needs nothing. (This is a
  user-task-schema obligation, separate from the in-process compile note under `scriptTask` above.)

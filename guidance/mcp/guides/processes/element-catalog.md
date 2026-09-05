clio MCP process-element-catalog guide — which process elements exist, and which clio builds today

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of what `create-business-process` can build TODAY and of the
element catalog -- the `data-id` vocabulary `validate-process-graph` speaks and
`describe-business-process` reports back. Split out of `process-modeling` because that article had no
budget headroom left, and because both of these sections grow with every element the platform gains
while the lifecycle around them does not.
`process-modeling` keeps the lifecycle: the tools, the descriptor, the build recipe and the safety
rules for editing an existing process.
Naming anything here? Every element, parameter and process code and caption is governed by N1-N10,
owned by `process-naming` — read it BEFORE you name anything, including when you entered at this
leaf rather than through `process-modeling`.

== What you can build today (create-business-process) ==
- NOT in a build descriptor: the "Connected to" links of an Activity a task creates. Add the element
  first, then bind them with `modify-business-process` → `setConnections` (see `process-activity-connections`).
- Events: `startEvent` (Simple start), `signalStart` (record signal: add/modify/delete), `endEvent`.
- Activities: `userTask` referencing any task from list-user-tasks via `userTaskName`
  (aliases `readData`->ReadDataUserTask, `changeData`->ChangeDataUserTask, `performTask`->ActivityUserTask).
  A `readData` element is CONFIGURABLE via its `readData` block — source object, first-record mode, result
  columns, sort, plus a record `filter` (the block is in `process-data-elements`, the filter contract in
  `process-data-source-filters`). A `changeData` element
  is CONFIGURABLE via its `changeData` block — target object + column values, plus a record `filter`
  (same two owners). CAVEAT: Add data and Delete data still place an UNCONFIGURED
  element — their target object and values cannot be set yet, so those steps do nothing useful until a
  human configures them in the designer. Say so when you use one; do not present such a result as a working
  data operation.
- Send email: `sendEmail` (the Send email element / EmailTemplateUserTask) is BUILDABLE and fully
  configurable through its `email` block — mode, sender, recipients, subject, HTML body, options and the
  manual-mode performer. `process-send-email` owns the contract and its limits.
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
  "Data source filters" section of `process-data-source-filters`).
- A CONDITIONAL BRANCH: build a plain flow, then turn it into a conditional one with
  `modify-business-process` + `setFlowCondition`. No gateway element is involved — see
  `process-branch-conditions`.
- NOT yet buildable — each of these is UNSUPPORTED through `create-business-process` and MUST NOT be put
  in a build descriptor: gateway ELEMENTS, default flows, timer/message start, intermediate events,
    `formulaTask`, `scriptTask`, `webService` (each also marked READ-ONLY in the
    catalog below, where silence used to read as "buildable"),
  sub-process, the Add/Delete-data target object + values (a `filter` on THOSE tasks is serialized
  but not end-to-end usable — the buildable filters are `signalStart`, `readData` and `changeData`), and the Read data
  collection / count / aggregation modes (only the first-record mode builds; the others are designer-only).
  Use the catalog below to reason about a solution and to READ existing processes
  (`describe-business-process`); don't expect to build those types in this increment.

== Element catalog (data-id -> label -> purpose) ==
(The `data-id` strings below are the vocabulary for `validate-process-graph` and for reasoning about /
reading processes. To BUILD, map them to the create-business-process `type` + `userTaskName`: events
`startEvent`/`startEventSignal`->`signalStart`/`endEvent`; a user/system task -> `type:"userTask"` with
`userTaskName` from list-user-tasks, e.g. Perform task = `performTask`/ActivityUserTask, Read data =
`readData`/ReadDataUserTask. Send email is the ONE user task with its own dedicated build type:
`emailTemplateUserTask` -> `type:"sendEmail"` (NOT a generic `userTask`) — full custom-message configuration
(mode/sender/recipients/subject/body/options/performer; no email templates), see `process-send-email`.)
System actions (palette group "System actions"):
- `readDataUserTask`  Read data    — read first record / aggregate / count / collection of an object.
    FIRST-RECORD mode is buildable via the element's `readData` block (source object, columns, sort) plus
    a `filter` — see `process-data-elements` for the block and `process-data-source-filters` for the
    filter. The other read modes (collection / count /
    aggregation) remain designer-only; describe reports them as `mode: "collection"` / `"function"`.
- `addDataUserTask`   Add data     — create record(s) in background; one-record mode returns only the Id.
    The element builds, but its target object and column values do NOT yet — see the caveat near
    the top of this guide.
- `changeDataUserTask` Modify data — bulk-update matched records (same values to all). BUILDABLE via the
    element's `changeData` block (target object + column values) plus a `filter` — see
    `process-data-elements` for the block and `process-data-source-filters` for the filter.
- `deleteDataUserTask` Delete data — delete matched records. Like its Add-data twin the element
    BUILDS, but its target object and values do NOT yet — see the caveat near the top of this guide. Its
    `filter` is SERIALIZED, so the build is clean, but a scoped delete is UNSUPPORTED while the target
    object is unset: do not report the element as a working delete.
- `formulaTask`       Formula      — compute a value (math/string/date/bool) into an output param.
    READ-ONLY here: the element is NOT buildable, and it is the one entry in this catalog most likely to
    be reached for by mistake, because formulas themselves ARE buildable — as a flow CONDITION and as a
    mapping `expression` (see `process-formulas`). Compute a value with a mapping onto a process
    parameter instead of asking for this element.
- `scriptTask`        Script task  — custom C# (ends with `return true;`; needs publication). READ-ONLY here.
  - Compile note: a `scriptTask`, and a `userTask` carrying an after-activity-save script, are the two
    IN-PROCESS elements whose authored C# makes the process itself need a compile before it runs.
- `webService`        Call web service — call a registered service; outputs Success + Http status code.
    READ-ONLY here.
- `callActivity`      Sub-process  — run another process (must start with a Simple start); multi-instance
    over a collection. READ-ONLY here — and its children live in its OWN element collection: the delete guards see them
    (they walk recursively), `describe-business-process` and `setElement` do not, so a refusal can name a
    flow no read call shows you.
- `userTask`/`*UserTask` — user/system tasks (Perform task, Open edit page, Send email, Approval, etc.).
User actions: `activityUserTask` Perform task, `userQuestionUserTask` User dialog,
  `openEditPageUserTask` Open edit page, `autoGeneratedPageUserTask` Auto-generated page,
  `preconfiguredPageUserTask` Pre-configured page, `emailTemplateUserTask` Send email, `approvalUserTask` Approval.
Events: `startEvent` Simple start, `startEventSignal` Signal start (record add/modify/delete or custom
  signal), `startEventTimer` Start timer (schedule/CRON), `startEventMessage` Start message, intermediate
  catch/throw (`intermediateCatchEvent*`/`intermediateThrowEvent*`), `endEvent` End/Terminate — the
  BPMN catalog has both, but a `create-business-process` `endEvent` builds Terminate today (see N6 in `process-naming`).
Gateways: `exclusiveGateway` (OR), `parallelGateway` (AND), `inclusiveGateway` (OR), `eventBasedGateway`.
Flows: sequence (default `connect`), conditional (setup -> conditionalConnection), default (setup -> defaultConnection).
- Custom user-task compile rule: a CUSTOM user task is a `ProcessUserTask` SCHEMA, not a process element —
  its own C# methods are generated into the package assembly (it has no `IsInterpretable`; that property
  exists only on `ProcessSchema`), so CREATING or CHANGING one needs a compile before any process can use
  it. Merely REFERENCING an already-compiled user task by `userTaskName` needs nothing. (This is a
  user-task-schema obligation, separate from the in-process compile note under `scriptTask` above.)

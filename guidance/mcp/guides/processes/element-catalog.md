clio MCP process-element-catalog guide — which process elements exist, and which clio builds today

Part of the process guide set. A build starts at `process-digest`; open `process-modeling` only when
the card or this article sends you there.
This article is the authoritative owner of what `create-business-process` can build TODAY, of how a
`data-id` maps to a build `type`, and of the custom user-task compile rule. The element catalog itself --
the `data-id` vocabulary `validate-process-graph` speaks and `describe-business-process` reports back --
is `process-element-catalog-details`; open it to read a process or reason about an element not listed
here. Split out of `process-modeling` because that article had no budget headroom left, and because
both of these sections grow with every element the platform gains while the lifecycle around them does not.
`process-modeling` keeps the lifecycle: the tools, the descriptor, the build recipe and the safety
rules for editing an existing process.
Naming anything here? Every element, parameter and process code and caption is governed by N1-N10,
owned by `process-naming` and restated in `process-digest` — read the card's N1-N10 block BEFORE you
name anything, and `process-naming` itself only for a case the card does not cover.

== What you can build today (create-business-process) ==
- NOT in a build descriptor: the "Connected to" links of an Activity a task creates. Add the element
  first, then bind them with `modify-business-process` → `setConnections` (see `process-activity-connections`).
- Events: `startEvent` (Simple start), `signalStart` (record signal: add/modify/delete), `endEvent`.
- Activities: `userTask` referencing any task from list-user-tasks via `userTaskName`
  (aliases `readData`->ReadDataUserTask, `changeData`->ChangeDataUserTask,
  `addData`->AddDataUserTask, `deleteData`->DeleteDataUserTask, `performTask`->ActivityUserTask).
  A `readData` element is CONFIGURABLE via its `readData` block — source object, mode (`first` | `collection` |
  `count` | `aggregation`), result columns and
  sort (`first` / `collection` — refused for `count`/`aggregation`; REQUIRED for `collection`), a
  `numberOfRecords` top-N (`collection` ONLY), an `aggregation` function + column (`aggregation`
  ONLY), plus a record `filter` (the block is in `process-data-elements`, the
  filter contract in `process-data-source-filters`). A `changeData` element
  is CONFIGURABLE via its `changeData` block — target object + column values, plus a record `filter`
  (same two owners). A `deleteData` element is CONFIGURABLE via its `deleteData` block — the target object
  and nothing else, because the record `filter` is what decides which records are destroyed. It is the one
  data element whose filter is not merely recommended: with none the runtime deletes nothing and fails.
  DESTRUCTIVE — count the matching records, name the object and what the filter selects, and get an
  explicit yes BEFORE you build it; `process-delete-data` carries the message template. An `addData`
  element is CONFIGURABLE via its `addData` block in BOTH modes — Add one record and Add selection
  (target object, adding mode, selection object, column values including `Column from this
  selection`), plus a record `filter` over the SELECTION object.
- Send email: `sendEmail` (the Send email element / EmailTemplateUserTask) is BUILDABLE and fully
  configurable through its `email` block — mode, sender, recipients, subject, the message as EITHER an HTML
  body OR an existing email template (with the record its macros resolve against), options and the
  manual-mode performer. `process-send-email` owns the element and the CUSTOM message;
  `process-send-email-template` owns the TEMPLATE message and its limits.
- Open edit page: `openEditPage` (the Open edit page element / OpenEditPageUserTask) is BUILDABLE and fully
  configurable through its `openEditPage` block — page, editing mode, pre-filled values, the record to open,
  performer, Log activity, result column and completion condition. `process-open-edit-page` owns the
  contract, the rule for choosing this element, and its limits.
- Approval: `approval` (the Approval element / ApprovalUserTask) is BUILDABLE and configurable through its
  `approval` block — the object and the record under approval, who approves, and the two notifications with
  their email templates. It is a configured approval STEP, not a FLOW: branching on the verdict is a
  conditional flow off the element and needs no gateway, but the designer edits that branch through a
  result-selection editor — see `process-activity-result-branches`. `process-approval` owns the contract and its
  limits.
- `preconfiguredPage` — Pre-configured page: shows a Freedom UI page to a user and resumes when the user
  presses a completing button, and is the only page element that can hand a user a purpose-built page.
  CRITICAL before you build one: the page's buttons and data sources are FACTS to read, not values to
  invent — a page inherits its buttons from its template chain, so the server cannot see them. Call
  `get-process-page-facts --schema-name <page>` first. `process-preconfigured-page` owns the contract.
- Routing between the three page elements (Open edit page, Pre-configured page, Auto-generated page) is
  owned by `process-open-edit-page` — read its ROUTING section before choosing; the Pre-configured page's own
  contract lives in `process-preconfigured-page`. NOTE `autoGeneratedPage`'s `Buttons` parameter uses a
  DIFFERENT storage shape from `preconfiguredPage`; the two share only the parameter name.

- Sequence flows; process-level parameters (with an optional constant default value); element-parameter mappings.
- `useBackgroundMode` on any element that OFFERS it (it is not signal-specific, but neither is it universal —
  four element kinds REMOVE the control outright, so a rule of the form "tick it on every element" states an
  impossible requirement). Verified against the designer's own property pages (`CrtProcessDesigner`,
  2026-08-21): `ProcessTerminateEventPropertiesPage`, `ProcessTimerStartEventPropertiesPage`,
  `IntermediateThrowMessagePropertiesPage` and `SendEmailUserTaskPropertiesPage` each apply a schema-diff
  `remove` operation against the background-mode control; a Terminate element therefore CANNOT be put in
  background mode and its `false` is correct, not an oversight. `EmailTemplateUserTask` — the `sendEmail`
  element kind — INSERTS the control and so does take the flag; do not confuse it with `SendEmailUserTask`,
  which does not. Do NOT set it on the elements of a signal-started process just because the process is
  signal-started: the start's own default (below) already runs the instance in a background worker up to its first
  element that waits, and on an ordinary activity the flag changes nothing — the platform inserts a background
  token only for a start event and a Sub-process (a multi-instance one inside its iteration flow, `process-sub-process`). It matters on an element that WAITS (a task, a page, a catch event, a
  Sub-process), where it decides whether the work after that element runs inside the request that completes it
  or is queued; set it there only when the request asks for that. A background run never pops a page open on the
  user's screen (the platform's `ForbidUserInteractionInBackground`, on by default, skips it; the step still waits
  in the performer's task list), and an `openEditPage` step with the flag ON was
  measured not to resume — `process-open-edit-page` owns that rule and `process-open-edit-page-details` the
  measurement. Shipped signal-started processes carry the flag on
  28 of the 166 elements after their start, all 28 behind a background start (108 processes of the shipped
  packages, packages named Test or Demo excluded; scanned 2026-09-24). The designer gates the control on
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
- BRANCHING, declared where the flow is declared. `flows[]` takes `kind` (`sequence` | `conditional` |
  `default`) and, on a conditional flow, its `condition`. The older two-step route — build the flow
  plain, then `setFlowCondition` — still works and is what you use on a flow that ALREADY exists, but
  do not reach for it when creating: it saves the process once with a flow that does not yet branch.
  See `process-branch-conditions`.
- A flow LABEL, the text the designer draws on the connector: `flows[].label` on the build path, and
  a `label` argument on the flow-EDIT operations. Label every conditional and default arm and leave a
  plain continuation bare — that is what the shipped product does. `process-naming` N10 owns the
  wording rule, the measured figures, the compatibility note and the EDIT contract, and go there
  before editing one: an empty `label` clears a designer's caption, so the edit route is not
  described here rather than described without its preconditions.
- `exclusiveGateway` (XOR) and `parallelGateway` (AND) ELEMENTS. A gateway is OPTIONAL for branching —
  the platform synthesizes one for a conditional flow whose source is an ordinary activity, which is
  what 485 of the 1 406 conditional flows in the shipped product do — so the element is about the
  diagram being readable, not about making the branch work. Three rules apply to the flows leaving a
  gateway element, none of them visible in the descriptor schema; `process-branch-conditions` owns
  them.
- `formulaTask` (Formula), from CrtProcessBuilder **1.6.3.16**. Below that version the type is refused
  outright, naming the ones it does build. It computes ONE expression and writes the result into ONE
  parameter — see its catalog entry in `process-element-catalog-details` for the block.
- `subProcess` (Sub-process), from CrtProcessBuilder **1.6.3.26**. Below that version the type is
  refused outright, naming the ones it does build. It calls ANOTHER process (the BPMN call activity) —
  naming the callee is what copies that process's parameters onto the element — see `process-sub-process`
  for the block.
- `scriptTask` (Script task), from CrtProcessBuilder **1.6.6.30**: C# the process must be COMPILED for -
  `process-script-task` owns when to use one, the question to ask BEFORE adding one, the block, the usings
  and the methods.
- NOT yet buildable — each of these is UNSUPPORTED through `create-business-process` and MUST NOT be put
  in a build descriptor: the INCLUSIVE and EVENT-BASED gateway elements, timer/message start,
  intermediate events,
    `webService` (also marked READ-ONLY in the
    catalog in `process-element-catalog-details`, where silence used to read as "buildable"),
  and reading one COLUMN out of a read COLLECTION's items (a first-record read's column IS a source,
  `sourceColumn` - see `process-data-elements`) — all
  four Read data modes DO build, see the catalog entry in `process-element-catalog-details`. A collection IS consumed now: a multi-instance
  Sub-process element iterates one, once per item (see the `callActivity` entry in `process-element-catalog-details`).
  Use the catalog in `process-element-catalog-details` to reason about a solution and to READ existing processes
  (`describe-business-process`); don't expect to build those types in this increment.

== Building from a data-id ==
(The `data-id` strings in `process-element-catalog-details` are the vocabulary for `validate-process-graph` and for reasoning about /
reading processes. To BUILD, map them to the create-business-process `type` + `userTaskName`: events
`startEvent`/`startEventSignal`->`signalStart`/`endEvent`; a user/system task -> `type:"userTask"` with
`userTaskName` from list-user-tasks, e.g. Perform task = `performTask`/ActivityUserTask, Read data =
`readData`/ReadDataUserTask. FOUR user tasks have their own dedicated build type and must NOT be built as
a generic `userTask`: `preconfiguredPageUserTask` -> `type:"preconfiguredPage"`, see
`process-preconfigured-page` — the generic route cannot carry its page, buttons or data sources, so it could
only build a page-less element that fails at run time, and it is REFUSED; `emailTemplateUserTask` -> `type:"sendEmail"` — full configuration in both message
modes (mode/sender/recipients/subject/body OR template + templateEntity/options/performer), see
`process-send-email`, and `process-send-email-template` for the template mode;
`openEditPageUserTask` -> `type:"openEditPage"`, see `process-open-edit-page`; and `approvalUserTask` ->
`type:"approval"`, see `process-approval`.)
- Custom user-task compile rule: a CUSTOM user task is a `ProcessUserTask` SCHEMA, not a process element —
  its own C# methods are generated into the package assembly (it has no `IsInterpretable`; that property
  exists only on `ProcessSchema`), so CREATING or CHANGING one needs a compile before any process can use
  it. Merely REFERENCING an already-compiled user task by `userTaskName` needs nothing. (This is a
  user-task-schema obligation, separate from the in-process compile note under `scriptTask` above.)

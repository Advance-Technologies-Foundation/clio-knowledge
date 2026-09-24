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
  measured not to resume — `process-open-edit-page` owns that. Shipped signal-started processes carry the flag on
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
  parameter — see its catalog entry below for the block.
- `subProcess` (Sub-process), from CrtProcessBuilder **1.6.3.26**. Below that version the type is
  refused outright, naming the ones it does build. It calls ANOTHER process (the BPMN call activity) —
  naming the callee is what copies that process's parameters onto the element — see `process-sub-process`
  for the block.
- NOT yet buildable — each of these is UNSUPPORTED through `create-business-process` and MUST NOT be put
  in a build descriptor: the INCLUSIVE and EVENT-BASED gateway elements, timer/message start,
  intermediate events,
    `scriptTask`, `webService` (each also marked READ-ONLY in the
    catalog below, where silence used to read as "buildable"),
  and reading one COLUMN out of a read collection — all
  four Read data modes DO build, see the catalog entry below. A collection IS consumed now: a multi-instance
  Sub-process element iterates one, once per item (see the `callActivity` entry below).
  Use the catalog below to reason about a solution and to READ existing processes
  (`describe-business-process`); don't expect to build those types in this increment.

== Element catalog (data-id -> label -> purpose) ==
(The `data-id` strings below are the vocabulary for `validate-process-graph` and for reasoning about /
reading processes. To BUILD, map them to the create-business-process `type` + `userTaskName`: events
`startEvent`/`startEventSignal`->`signalStart`/`endEvent`; a user/system task -> `type:"userTask"` with
`userTaskName` from list-user-tasks, e.g. Perform task = `performTask`/ActivityUserTask, Read data =
`readData`/ReadDataUserTask. THREE user tasks have their own dedicated build type and must NOT be built as
a generic `userTask`: `emailTemplateUserTask` -> `type:"sendEmail"` — full configuration in both message
modes (mode/sender/recipients/subject/body OR template + templateEntity/options/performer), see
`process-send-email`, and `process-send-email-template` for the template mode;
`openEditPageUserTask` -> `type:"openEditPage"`, see `process-open-edit-page`; and `approvalUserTask` ->
`type:"approval"`, see `process-approval`.)
System actions (palette group "System actions"):
- `readDataUserTask`  Read data    — read first record / aggregate / count / collection of an object.
    FIRST-RECORD, COLLECTION, COUNT and AGGREGATION modes are buildable via the element's `readData` block (source
    object, mode, columns/sort — `first` / `collection`, refused for `count`/`aggregation` — a
    `numberOfRecords` top-N (`collection` ONLY) and an aggregation function + column —
    `aggregation` ONLY) plus a `filter` — see `process-read-data` for the block
    and `process-data-source-filters` for the filter; describe reads them back as `mode: "first" |
    "collection" | "count" | "aggregation"`. `collection` reads every match into `ResultEntityCollection`
    and the shaped `ResultCompositeObjectList`, which a `Collection` process parameter mirrors; it requires
    an explicit `columns` selection.
- `addDataUserTask`   Add data     – create record(s) in background; BUILDABLE via the `addData` block in
                                     both modes. Returns ONLY the new record's Id, on `RecordId`.
- `changeDataUserTask` Modify data — bulk-update matched records (same values to all). BUILDABLE via the
    element's `changeData` block (target object + column values) plus a `filter` — see
    `process-data-elements` for the block and `process-data-source-filters` for the filter.
- `changeAdminRightsUserTask` Change access rights - grant/revoke record permissions on matched
    records. BUILDABLE via `accessRights` (alias `changeAccessRights`) plus a `filter`; no outputs.
    `process-access-rights` owns the shape and the hazards. Both no-op states are worth naming here
    because they build green: `add` and `remove` both empty changes nothing, and a filter that is
    PRESENT but carries no conditions is the inert state (the package refuses that one at build);
    a record filter that is ABSENT is the opposite hazard and acts on every record of the object -
    nothing refuses or warns it. The element has no output parameters,
    so a clean build does NOT mean the element will do anything - check the filter and the entries.
- `deleteDataUserTask` Delete data — delete matched records. BUILDABLE via the element's `deleteData`
    block (target object — the only field it has) plus a `filter` — see `process-delete-data` for the
    block and the confirmation duty, `process-data-source-filters` for the filter. Unlike Modify data there
    is no mode flag: the runtime always applies the filter and throws its empty-filter error without one,
    so a filterless element deletes nothing and fails rather than deleting everything. DESTRUCTIVE and
    irreversible, and it repeats on EVERY run — confirm the object and the selected records with the user
    before you plan one in.
- `formulaTask`       Formula      — compute a value (math/string/date/bool) into ONE parameter.
    BUILDABLE from CrtProcessBuilder **1.6.3.16** with a `formula` block:
    `{body, and exactly ONE of resultProcessParameter | elementName + elementParameter}`.
    Four things about it are not guessable from the schema:
      * `body` is the SAME dialect as a flow condition (`process-formulas` owns the vocabulary). A process
        parameter may be referenced by NAME — `[#Amount#]` — on EVERY route that writes a body: create,
        `addElement` and `setElement` alike, because the expansion lives in the applier they all go
        through. A body already in the meta-path form `describe-business-process` reports passes through
        untouched, so echoing a read-back is safe.
      * BOTH the body and a target are required when the element is created, and naming two targets is
        refused rather than resolved by precedence. On modify the block is a PARTIAL update: a body-only
        edit keeps the target, a target-only edit keeps the expression.
      * the target is NOT a mapping and needs none of the mapping sources. The platform stores it as a
        map path on the element itself, which is why `describe-business-process` reports it under
        `formula.target` — resolved back to names — rather than among the process mappings. A three-part
        target (a COLUMN of an element parameter's record) reports that column as `entityColumnUId`,
        because the process cannot name it. `formula.target.unresolved` means the stored path could not
        be decoded into names — NOT that the element writes nowhere: the platform's reader accepts
        shapes a read-back may not, so treat it as "not named here" and look at the raw path.
      * an element the DESIGNER built reads back the same way, so a described formula feeds straight
        into a build.
    Still true, and still the cheaper answer for a one-off value: a mapping with an `expression` source
    computes a value without an element at all. Reach for this element when the computation deserves to
    be visible on the diagram, or when the result must be written between two steps.
- `scriptTask`        Script task  — custom C# (ends with `return true;`; needs publication). READ-ONLY here.
  - Compile note: a `scriptTask`, and a `userTask` carrying an after-activity-save script, are the two
    IN-PROCESS elements whose authored C# makes the process itself need a compile before it runs.
- `webService`        Call web service — call a registered service; outputs Success + Http status code.
    READ-ONLY here.
- `callActivity`      Sub-process  — call ANOTHER process (the BPMN call activity) and run it once, passing
    values through THAT process's own parameters. BUILDABLE from CrtProcessBuilder **1.6.3.26** via
    `type:"subProcess"` with a `subProcess` block
    (`{processName | processUId, resync, multiInstanceOptions}`); the block, the parameter
    mirroring/mapping rule, `resync`, MULTI-INSTANCE (running the callee once per item of a collection,
    from **1.6.6.14**), the refusals and what is NOT supported (the event/expanded sub-processes) are owned
    by `process-sub-process` — go there before writing any of it; WHETHER a request needs one at all is
    owned by `process-sub-process-when`. Its EVENT and EXPANDED variants keep
    their children in their OWN collection, which `describe-business-process` does not walk, but
    the delete guards see them, walking it recursively so a reference from inside one still blocks a delete.
- `userTask`/`*UserTask` — user/system tasks (Perform task, Open edit page, Send email, Approval, etc.).
User actions: `activityUserTask` Perform task, `userQuestionUserTask` User dialog,
  `openEditPageUserTask` Open edit page (BUILDABLE via `type:"openEditPage"` — see "What you can build today"), `autoGeneratedPageUserTask` Auto-generated page,
  `preconfiguredPageUserTask` Pre-configured page, `emailTemplateUserTask` Send email, `approvalUserTask` Approval.
  Six of these ENUMERATE RESULTS, which changes how a branch leaving them is authored: Perform task, User
  dialog, Open edit page, Auto-generated page, Pre-configured page and Approval. A conditional flow off one
  is edited in the designer as a result SELECTION rather than a formula — `process-activity-result-branches` owns
  that rule, and no guide here owns User dialog or Auto-generated page, so treat a branch off either the
  same way.
Events: `startEvent` Simple start, `startEventSignal` Signal start (record add/modify/delete or custom
  signal), `startEventTimer` Start timer (schedule/CRON), `startEventMessage` Start message, intermediate
  catch/throw (`intermediateCatchEvent*`/`intermediateThrowEvent*`), `endEvent` End/Terminate — the
  BPMN catalog has both, but a `create-business-process` `endEvent` builds Terminate today (see N6 in `process-naming`).
  Timer start, message start and the intermediate catch/throw events are READ-ONLY here.
Gateways: `exclusiveGateway` (XOR, BUILDABLE), `parallelGateway` (AND, BUILDABLE),
  `inclusiveGateway` (OR, read-only), `eventBasedGateway` (read-only) -- see
  `process-branch-conditions`.
Flows: sequence (default `connect`), conditional (setup -> conditionalConnection), default (setup -> defaultConnection).
  All three are BUILDABLE: declare the kind with the flow (`flows[].kind`, plus `flows[].condition`
  on a conditional one) rather than drawing it and setting it afterwards. Each also takes
  `flows[].label`, the connector text — the member first shipped in `CrtProcessBuilder` 1.6.0.8 and is
  reported by `describe-business-process`. Read that as provenance, not as a check to run: see
  `process-naming` N10 for why a version number cannot tell you whether an archive carries it.
- Custom user-task compile rule: a CUSTOM user task is a `ProcessUserTask` SCHEMA, not a process element —
  its own C# methods are generated into the package assembly (it has no `IsInterpretable`; that property
  exists only on `ProcessSchema`), so CREATING or CHANGING one needs a compile before any process can use
  it. Merely REFERENCING an already-compiled user task by `userTaskName` needs nothing. (This is a
  user-task-schema obligation, separate from the in-process compile note under `scriptTask` above.)

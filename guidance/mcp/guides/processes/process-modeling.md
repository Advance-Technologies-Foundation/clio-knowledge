clio MCP process-modeling guide — design Creatio business processes (BPMN)

Moved to `process-modeling-details`: the evidence, version history and reasoning behind these rules,
and the stranded-mapping-row case. Read it when you rewire an existing Sub-process element or for a
rule's evidence.

== Which process article to read ==
A build starts at `process-digest`. This article owns the build lifecycle (tools, descriptor, recipe,
modify safety); every other topic has its own owner -- read the one your task needs, never guess:
  * `process-digest` - the BUILD CARD: recipe, names and buildable types in short, and the ONE
    article per element.
  * `process-custom-elements` - custom user-task elements: Classic panels and registration.
  * `process-custom-element-families` - one toolbox entry selecting separate tasks and pages.
  * `process-element-catalog` - what `create-business-process` can build TODAY and what it cannot; the
    element catalog is `process-element-catalog-details`.
  * `process-diagram-layout` - how the diagram is drawn, and the re-draw refusal.
  * `process-naming` - N1-N10 for every caption and code. A build names from `process-digest`; open
    this only to relabel or settle a name collision.
  * `process-data-elements` - a record-event start (signalStart), and the Modify data element.
  * `process-read-data` - the Read data element: modes, outputs, columns, sort, top-N.
  * `process-add-data` - the Add data element: modes, selection, value sources, refusals.
  * `process-delete-data` - the Delete data element and the confirmation you owe the user before
    building one. Read it BEFORE planning a delete.
  * `process-data-source-filters` - the `filter` those three carry, its value sources, date macros
    and the signal-start restriction.
  * `process-parameters` - process parameters, mappings, type compatibility, date/time/lookup defaults.
  * `process-formulas` - the `expression` mapping source and the formula vocabulary.
  * `process-branch-conditions` - a conditional flow's condition, branch precedence, and the
    parallel-split hazard.
  * `process-activity-result-branches` - a connector off an activity's results takes a result
    SELECTION, never a formula.
  * `process-perform-task` - the Perform task element and its parameter table.
  * `process-task-performer` - who performs a task: the element-level performer block (the only route to
    a TEAM) or OwnerId for one person.
  * `process-task-category` - how a category or priority must be mapped, and what degrades silently
    when it is not (not needed to BUILD one: `process-perform-task` carries the ids and refusals).
    Read it when a field shows a raw Guid or a results dropdown offers the wrong set.
  * `process-open-edit-page` - the Open edit page element, and when to choose it.
  * `process-access-rights` - the Change access rights element and its silent no-ops.
  * `process-send-email` - the Send email element: mode, sender, recipients, subject, custom HTML
    body, auto-mode checklist.
  * `process-send-email-template` - the Send email element's TEMPLATE message.
  * `process-approval` - the Approval element and its two notifications.
  * `process-preconfigured-page` - the Pre-configured page element and the page facts to read first.
  * `process-sub-process` - the Sub-process element: callee, mirrored parameters, resync.
  * `process-sub-process-when` - WHEN a request needs more than one process: D1, D2, D4.
  * `process-activity-connections` - the "Connected to" links of the Activity a task creates.
  * `process-versions` - the version model and which member runs. Read it BEFORE editing or launching
    ANY existing process.
  * `process-version-writes` - saving a new version, a restore point, rollback.
Each is sized to be read WHOLE through `get-guidance`. Never infer another article's rule from this
one; read that article.

== How clio builds processes (read first) ==
- clio makes no LLM call: you own the intent->BPMN translation. You NEVER hand-author process
  metadata, filters, or column mappings.
- The build is DECLARATIVE: describe elements + flows + parameters + mappings and clio builds + saves
  the process in one call. The diagram is drawn for you - do not set positions.
- Tools:
  * list-user-tasks - the user-task palette (name + uid); pass a name as `userTaskName`. It lists
    RETIRED schemas (`CallUserTask`, `EmailUserTask`, `SendEmailUserTask`) as equal peers with no
    marker, and TWO schemas share the caption "Send email" (`EmailTemplateUserTask` is the live one,
    `SendEmailUserTask` creates nothing): always key on the schema NAME, never on a caption.
  * create-business-process - build a NEW process from a JSON descriptor, and save it.
  * modify-business-process - edit an EXISTING process by an ordered list of operations.
  * describe-business-process - read a process back as a graph; from clio <CLIO-TBD> a large
    one is a SUMMARY (no uids, labels, blocks, layout): pass `mode=full` (the whole graph) or `element=<name>` (one element). Per element also
    `connections[]`, `deprecated` (retired user-task schema) and `writesConnectionsAtRuntime`. FALSE:
    the connections persist and compile but are never written - change the element kind (its runtime
    never writes them) or set `CreateActivity` true (its activity-creation gate is shut). `null`: not
    established - not false, NOT permission. `setConnections` is refused on `false` AND `null`.
  * validate-process-graph - pre-check a planned graph against the connection rules R1-R20
    (`process-activity-connections-details`).
- If a tool refuses for a missing or older CrtProcessBuilder, STOP and ask the user before
  `install-process-builder`: it builds the configuration and restarts the site for every user
  (`core-rules`).

== Descriptor (create-business-process) ==
{
  "name": "UsrAccount_Onboard", "caption": "Account onboarding", "packageName": "<package-name from get-target-package>",
  "elements": [
    { "name": "OnboardingRequestedStart", "type": "startEvent", "caption": "Onboarding requested" },
    { "name": "NotifyAccountOwner", "type": "performTask", "caption": "Notify the account owner" },
    { "name": "EndOnboardingHandedOff", "type": "endEvent", "caption": "Onboarding handed off" }
  ],
  "flows": [ { "source": "OnboardingRequestedStart", "target": "NotifyAccountOwner" },
             { "source": "NotifyAccountOwner", "target": "EndOnboardingHandedOff" } ],
  "parameters": [ { "name": "AccountNameParameter", "type": "Text", "direction": "In",
                    "caption": "Account name" } ],
  "mappings": [ { "elementName": "NotifyAccountOwner", "elementParameter": "<ParamName>",
                  "processParameter": "AccountNameParameter" } ]
}
- `packageName` must accept edits: the package the user named, else the application's (`package-name`
  from `get-app-info` / `create-app`), else the environment's current one, resolved with ONE
  `get-target-package` call (`clio-run`; add `package` for a named or application package; it answers
  `package-name`). Never guess one from `list-packages`: it does not say which packages accept changes.
  On `success: false` relay its error and ask; never substitute a package.
- `name` is the local element handle used by flows (`source`/`target`) and mappings (`elementName`);
  it is `name`, not `id`. A `userTask` element auto-carries the task's parameters; map values into them
  with `mappings`. For a record trigger use `signalStart` (see `process-data-elements`).
- EVERY code and caption in these guides' examples is N1-N10 compliant on purpose (see `process-naming`):
  copy their SHAPE, not just their fields - never a generated `Start1` / `task1` / `End1`.

== Build recipe (intent -> running process) ==
Before step 1 you MUST read `process-element-catalog` (or `process-digest`, while every element you
plan is on its buildable list): a plan around something `create-business-process` cannot build fails
only at build time. For a NEW process, or a change for EACH item of a set,
you MUST also check the D1/D2/D4 triggers of `process-sub-process-when` first (in short on
`process-digest`): they decide how many processes the request is.
1. Translate the request into a graph (starts, activities, flows, ends, parameters, mappings) and
   name them per N1-N10 in `process-naming` (a build: the card's Names block).
   ONE START PER TRIGGER the process must react to: a process run both when a record is ADDED and
   when it is CHANGED carries TWO signal starts, not two processes and not one trigger. Signal, timer and message starts may be several; the SIMPLE (manual) start only
   once (`validate-process-graph` R3 and `create-business-process` enforce it). Two starts need
   CrtProcessBuilder 1.6.2.24 or later on the environment AND clio 8.1.0.131 or later; older ones refuse
   the second start (`process-modeling-details`).
2. (recommended) `validate-process-graph(graph)` -> fix every error-severity finding.
3. `list-user-tasks` -> pick the exact `userTaskName`(s) for your activities.
4. `create-business-process(descriptor)` -> builds + saves in one call. Pass the descriptor as the JSON
   object itself; a clio that answers "must be a string" predates that - pass a string holding the
   same JSON.
5. Verify with `describe-business-process mode=full` (element types, user-task names, parameter sources +
   direction - an output you can map FROM has `isOutput:true`, not `isResult` - and the signal
   trigger), never with a raw `execute-esq`/`odata-read` of the process record. A runtime check is
   optional (the user's choice); use `run-process` or `trigger-smoke` (`core-rules`).
6. Change it later with `modify-business-process`, same shapes as a build. Ops: addElement /
   removeElement / addFlow / removeFlow / addParameter / addMapping / setParameter / removeParameter /
   setFilter / clearFilter / setSignal (a signalStart's trigger + tracked columns, in place) / setFlow /
   setFlowCondition / setFlowResults (`process-activity-result-branches`) / setConnections /
   clearConnections ("Connected to" links, `process-activity-connections`) / addUsing / removeUsing /
   setMethods / setElement, which changes element-level fields in place:
   * `useBackgroundMode` where the element OFFERS it (four kinds do not:
     `process-element-catalog`);
   * `readData` / `changeData` / `addData` on the matching data element only (partial-update,
     mode-switch and retarget rules: `process-read-data`, `process-data-elements`, `process-add-data`);
   * `deleteData` on a Delete data element only - MUST: a target retarget clears the record filter, and
     without one the element deletes nothing and fails at run time, so re-issue `setFilter` in the same
     batch; state the object and the records and get an explicit yes first, as for a build
     (`process-delete-data`);
   * `accessRights` on a Change access rights element only - MUST: a supplied `add`/`remove` REPLACES
     that whole collection on live records, destroying every grant it does not restate while widening
     access to whoever it names, with nothing reported at run time; show the user the target object, the
     record `filter` and every grantee with its operations and level, and get an explicit yes first
     (`process-access-rights`);
   * a sendEmail `email` block - a partial update; to/cc/bcc recipients MATCH-OR-APPEND (a new address
     is added, an identical one is a no-op, none can be removed).
- On a file-design-mode (FSD) stand a built process is NOT runtime-active until it is loaded FS->DB
  and published, so a signal won't fire yet.
- Do NOT run `compile-creatio` to "make a process runnable" unless its save WARNED it cannot run until
  compiled (a `scriptTask`; then pass `process-name`), and do NOT read a raw system record
  (`odata-read`/`execute-esq`) to decide readiness - read status back with `describe-business-process`.
  No `NeedXxx` / `IsXxx` column of a raw read is a compile instruction. WITHIN A PROCESS exactly two
  things pull a compile in, both C# YOU authored: a `scriptTask` (with the `methods` it calls) and a
  `userTask` with an after-activity-save script; all else, USING a compiled user task included, runs
  without one. Other schemas (source code, business objects, DCM, value lists, a CUSTOM user-task
  schema - `process-element-catalog`) carry their own compile obligations.

== Set what was asked for, and nothing else ==
- An OPTIONAL field the request did not mention stays OUT of the descriptor: filling it in changes
  behaviour nobody chose, and describe reports what is WRITTEN, so absence means "not set", never "off".
- Required fields need no guessing: the server REFUSES and names what is missing. When that is a
  BUSINESS decision the request did not make - who approves, who performs, whom to notify - ASK; a
  value carried over from a process built earlier in the session is a guess, not context.

== Modifying an existing process — safety rules (modify-business-process) ==
- ALWAYS `describe-business-process mode=full` first, and re-describe after the edit to verify the result.
- The modify path runs NO structural validation (only the create path validates the graph):
  `removeElement` / `removeFlow` can leave the process unreachable or with dangling paths and the save
  still succeeds. `removeElement` also CASCADES - it deletes every flow touching the element and the
  mappings TARGETING it, but does NOT re-join the gap, and mappings/values READING its outputs may
  survive as dangling references. Add the bridging `addFlow` in the same operations array, then
  re-describe and clean up leftover references to the removed element.
- **`removeFlow` takes `source` and `target` ONLY — MUST strip `kind` and `condition` first.** It
  resolves the flow by the PAIR, so a field carried over from `describe` is REFUSED, and the refusal
  ABORTS THE WHOLE atomic BATCH - every other operation rolls back with it. Do not rely on the refusal:
  it is version-dependent (`process-modeling-details`).
  `process-branch-conditions` owns the same rule for `setFlow` and `setFlowCondition`.
- Before removals, run `validate-process-graph` on the graph AS IT WILL BE after your operations
  (describe output + your planned ops applied), and confirm destructive removals with the user.
- Constructs the builder cannot create (inclusive and event-based gateways, timer/message starts,
  intermediate events, `webService`; full list in `process-element-catalog`) survive a save untouched
  as data - but you CAN still remove or rewire them by name and nothing will warn you. Gateway
  ELEMENTS, DEFAULT flows and the Formula element are buildable and NOT on that list
  (`process-branch-conditions`, `process-element-catalog`).
- The SUB-PROCESS element is NOT on that list - it is buildable (`type:"subProcess"`, from
  CrtProcessBuilder 1.6.3.26) and the build path does warn you. CREATING one is safe; REWIRING an
  existing one - retargeting its called process, deleting it, editing its parameters - is high-risk.
  The build path REFUSES a retarget while anything reads from the element, or did when the request
  started; the remedy is in `process-sub-process` - never clear the reader in the same batch. A
  MULTI-INSTANCE element (a common state) is de-converted, edited with every guard and re-converted
  (`process-sub-process`). `validate-process-graph` has no parameter or mapping rule, so planning
  catches none of this. A caller reporting a mapping that resolves to nothing: see the
  stranded-mapping-row case in `process-modeling-details`. CONDITIONAL flows belong on this list even
  though you CAN build one (`process-branch-conditions` owns the detail): removing the last conditional
  flow off an element stops the platform synthesizing the gateway, so EVERY outgoing flow is taken - a
  parallel split where an approval or threshold gate used to be, which describe reports as
  `kind: "sequence"` on both, reading exactly like "condition cleared, as asked". Treat such a process
  as high-risk: prefer additive edits, do not remove or rewire those elements, and tell the user what
  you left alone.
- Every modify re-draws the WHOLE diagram; where that would REPLACE an arrangement the server REFUSES
  the edit and asks rather than flattening it. Read `process-diagram-layout` (that refusal and its two
  answers) before editing a process whose diagram matters.
- You MUST read `isActiveVersion` from the describe output before ANY modify of a process that is not
  a build in progress (`process-version-writes`): a modify overwrites the ONE schema you named (a process
  can be a family of them), and the overwrite is irreversible either way. TRUE: you would overwrite the
  graph the runtime executes - get explicit confirmation (the edit request is not one;
  `process-version-writes` says what is) and offer `modify-business-process-as-new-version`: the SAME
  operations against a new version, or an EMPTY operations array first as a snapshot, then the in-place
  edit. FALSE: the graph you hold is not the one that runs, so do NOT modify it -- re-describe by
  `activeVersionSchemaUId` and edit that member, or report the standing and ask. `process-versions` owns every other outcome and the identity rules;
  `process-version-writes` owns what to ask before activating.

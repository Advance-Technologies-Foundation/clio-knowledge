clio MCP process-modeling guide — design Creatio business processes (BPMN)

== Which process article to read ==
This article is the ENTRY POINT. It owns the build lifecycle: what the tools are, what the
descriptor looks like, the recipe, and the safety rules for editing an existing process. What is
buildable today and the element catalog moved to `process-element-catalog`: this article had no
budget headroom left, and both of those sections grow with every element the platform gains while
the lifecycle around them does not. Everything else has its own article and its own authoritative
owner -- read the one your task needs instead of guessing:
  * `process-element-catalog`      - what `create-business-process` can build TODAY, what it cannot,
                                     and the element catalog (data-id -> label -> purpose).
  * `process-naming`               - N1-N10: the process caption and code, element captions and
                                     codes, parameter codes. Read it BEFORE you name anything.
  * `process-data-elements`        - start a process from a record event (signalStart), and the Read
                                     data and Modify data elements.
  * `process-data-source-filters`  - the `filter` those three carry: its shape, the comparisons, the
                                     right-hand value sources, the relative-date macro vocabulary and
                                     the signal-start restriction.
  * `process-parameters`           - process parameters, element-parameter mappings, type
                                     compatibility, and the date/time/lookup default macros.
  * `process-formulas`             - the `expression` mapping source and the formula
                                    vocabulary both it and a condition use
  * `process-branch-conditions`    - the condition on a conditional flow: setting one,
                                    branch precedence, and the parallel-split hazard
  * `process-perform-task`         - the Perform task element: what it produces, its parameter table and
                                     what the runtime sets.
  * `process-task-performer`       - who performs a task: the element-level performer block (the only
                                     route to a TEAM) and OwnerId for one person.
  * `process-task-category`        - WHY a category or priority MUST be a bare-Guid constant and MUST
                                     NOT be a formula, and what degrades silently when it is one. Not
                                     needed to BUILD one -- `process-perform-task` carries the ids and
                                     the refusals. Read this when a field shows a raw Guid or a results
                                     dropdown offers the wrong set.
  * `process-send-email`           - the Send email element: mode, sender, recipients, subject,
                                     HTML body and its process macros.
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
Before step 1 you MUST read `process-element-catalog`. It is the authoritative owner of what
`create-business-process` builds today and what it does not, and a plan built around something it
cannot build fails only at build time -- there is no earlier signal. A short form was tried here and
reverted: it stated the conditional branch wrongly, dropped the "Connected to" links (which are NOT
in a build descriptor at all) and dropped the Add/Delete-data filter caveat, and the test written to
keep the two copies in step could not see any of the three. One fetch is cheaper than a wrong plan.
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
   setFlowCondition / setElement / setConnections / clearConnections — same parameter/mapping/filter/
   signal/readData/
   changeData/email shapes as a build; setSignal reconfigures an existing signalStart's record trigger +
   tracked columns in place, setElement changes element-level fields in place: `useBackgroundMode` on any
   element that OFFERS it (four kinds remove the control — see the element catalog in
   `process-element-catalog`), `readData` /
   `changeData` on the matching data element only (see `process-data-elements` for their
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
  business objects, DCM, value lists, and a CUSTOM user-task schema — the custom user-task compile
  rule is in `process-element-catalog`) carry their own compile obligations and are NOT covered here.

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
- If describe shows constructs the builder cannot create (gateway ELEMENTS, default flows,
  sub-process, timer/message/intermediate events), they survive a save untouched as data — but you CAN
  still remove or rewire them by name and nothing will warn you. CONDITIONAL flows belong on this list
  even though you CAN build one, and `process-branch-conditions` owns the detail: removing the last
  conditional flow off an element leaves it with plain flows only, the platform stops synthesizing the
  gateway, and EVERY outgoing flow is then taken — a parallel split where an approval or threshold gate
  used to be, which describe reports as `kind: "sequence"` on both, reading exactly like "condition
  cleared, as asked". Treat such a process as high-risk:
  prefer additive edits, do not remove or rewire those elements, and tell the user what you left alone.
- Every modify re-applies the automatic layout to the WHOLE diagram: a hand-arranged multi-lane or
  branched diagram is flattened into generated left-to-right rows (process data intact, manual layout
  lost). Warn the user before editing a process with a curated diagram.

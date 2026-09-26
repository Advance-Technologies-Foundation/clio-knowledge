clio MCP process-digest guide - the build card: what every process build needs, and where an edit starts, in one read

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This card OWNS NO RULE. It restates, in short form, the rules every build of a NEW process needs from
four articles - `process-modeling` (tools, descriptor, recipe, modify safety), `process-element-catalog`
(what builds today), `process-naming` (N1-N10) and `process-sub-process-when` (how many processes) -
and each block names its owner. Read this card INSTEAD of those four for an ordinary build; open an owner
when a line below sends you there, or when your case is not the ordinary one a line describes. The owner
always wins over this card. Editing an existing process starts at `process-versions` (last block).
Each ELEMENT you put in the process has its own article; the routing map's Business processes rows and
the index in `process-modeling` name it - a record signal and Modify data: `process-data-elements`,
gateways and flow conditions: `process-branch-conditions`. No article owns User dialog or Auto-generated
page. Read the articles of the elements you actually use, and only those.

== How many processes (owner: `process-sub-process-when`) ==
- A request produces ONE process. Split only when one of these fires - nothing else is a reason.
  Check all three for EVERY new process, and for an edit that is itself per-item: they are hard to
  recognise without their rules, so do not skip this block because none seems to apply.
  * D1 — the same work for EACH item of a set: multi-instance, do not ask. The same steps for every
    record of a set, and one of them acts ONCE per run: a task, an approval, a page, an email (an email
    to each item is D1 even when every item gets the same text). Work a set-based data element does in
    one run (Modify / Delete data, Add data in selection mode, Change access rights over a filter) is
    NOT D1 - one element with a filter; but Modify data writes the SAME values to every record, so a
    value computed from each record's own data is D1 again, and a task or an email per item stays D1
    although each is stored as an Activity - an Add data of Activity records is no substitute.
  * D2 — the same fragment twice in the plan: one helper, do not ask. At least 3 elements of the same
    kinds in the same order, differing only in values, at at least 2 places - after the owner's join
    and hoist rules have had their chance to leave one copy.
  * D4 — two or more long phases: propose a split, and ask. Two or more stages that each hold
    several steps around human work (a lone task is a step, not a phase); a plan past about 30
    elements is checked by the owner too, which says when size alone is a reason.
- A split the user ASKS for is built as asked. If D1, D2 or D4 fires, read `process-sub-process-when`
  BEFORE you plan the graph (it owns the join and hoist rules, the build order, the execution mode,
  what to ask and the names), and `process-sub-process` for the element.

== What builds today (owner: `process-element-catalog`) ==
- Buildable `type`s: `startEvent`, `signalStart`, `endEvent` (builds a Terminate end), `userTask`
  with a `userTaskName` from `list-user-tasks` (aliases `readData`, `changeData`, `addData`,
  `deleteData`, `performTask`, `changeAccessRights`), `sendEmail`, `approval`, `openEditPage`,
  `preconfiguredPage`, `exclusiveGateway`, `parallelGateway`, `formulaTask`, `subProcess`.
  THREE user tasks have their own dedicated build type and must NOT be built as a generic
  `userTask`: Send email -> `sendEmail`, Open edit page -> `openEditPage`, Approval -> `approval`.
- Flows are declared on the flow: `flows[].kind` (`sequence` | `conditional` | `default`),
  `flows[].condition` on a conditional one, and `flows[].label`. A conditional flow leaving an
  activity that enumerates results - Perform task, User dialog, Open edit page, Auto-generated page,
  Pre-configured page, Approval - takes a result SELECTION, never a formula (`process-activity-result-branches`).
- Choosing between the three page elements (Open edit page, Pre-configured page, Auto-generated page)
  is owned by `process-open-edit-page`: read its ROUTING section before choosing.
- `formulaTask` and `subProcess` need a recent CrtProcessBuilder on the environment; an older one
  refuses the type outright and names the types it does build.
- Two buildable elements are DESTRUCTIVE and owe the user a confirmation BEFORE you build them:
  `deleteData` (count the records its filter matches, name the object as the designer names it, and
  get an explicit yes - `process-delete-data` carries the message, read it before planning the step)
  and Change access rights (show the object, the record filter and every grantee with its
  operations and level, and get an explicit yes - `process-access-rights`).
- UNSUPPORTED through `create-business-process`: the inclusive and event-based gateways, timer and
  message starts, intermediate events, `scriptTask`, `webService`, and reading one COLUMN out of a
  read collection. They are read-only; do not plan around them.
- The "Connected to" links of an Activity a task creates are NOT in a build descriptor - add the
  element, then `setConnections` (`process-activity-connections`).
- Read `process-element-catalog` itself for any element not listed here, for `useBackgroundMode`, or
  to read a process that contains read-only constructs.

== Names and codes, N1-N10 (owner: `process-naming`) ==
- N1 process `caption`: sentence case. N2 process `name`: `<prefix><Object>_<Action>` in PascalCase
  segments (`UsrAccount_Onboard`); the prefix is the environment's own, from `get-schema-name-prefix`
  (never hard-code `Usr`; an empty prefix means none); no autonumber, random suffix or GUID fragment,
  and the package name only to break a real collision. N3 a process meant to be called ends with
  `SubProcess`.
- N4 EVERY element gets an explicit `caption`, sentence case, <= 60 characters (a gateway's ~44):
  an ACTIVITY is verb first ("Read primary contact"), an EVENT is the trigger or outcome as a noun
  phrase ("Record is modified").
- N5 `elements[].name` is DERIVED from its own caption: take its words in order; drop ONLY these
  words: `a`, `an`, `the`, `is`, `are`, `was`, `were`, `be`, `been`, `has`, `have`, `had` (keep
  every other word); treat punctuation as a word boundary; PascalCase the rest; add only the prefix or
  suffix the shape requires, and never pad a code with the element's type name. Shapes: a
  `signalStart` is `<Trigger>Signal`, a `startEvent` `<Reason>Start`, an end event `End<Reason>`.
  "Account is added" -> `AccountAddedSignal`, "Create the follow-up task" -> `CreateFollowUpTask`.
- N6 a code never contradicts the runtime type (`endEvent` is a Terminate end, so no `EndNormal`).
  N7 no `UserTask` postfix on an element code. N8 `parameters[].name`: PascalCase plus a `Parameter`
  suffix (`TargetAccountParameter`); the caption carries no suffix, and the parameters the platform
  creates on an element (`Duration`, `ResultEntity`, ...) are never renamed. N9 codes are stable: never
  from the clock, a GUID or a counter.
- N10 LABEL EVERY CONDITIONAL AND DEFAULT ARM with the outcome in business words (about 20
  characters, never the expression) and leave a plain continuation bare. Before EDITING a flow's
  label read `process-naming` N10 and `process-branch-conditions`: an empty `label` clears a
  designer's caption.

== Build recipe (owner: `process-modeling`) ==
1. Decide how many processes (above) and translate the request into a graph: start event(s),
   activities, flows, end event(s), process parameters and mappings, named per N1-N10 above
   (`process-naming`).
   ONE START PER TRIGGER the process must react to: a process that runs when a record is ADDED and
   when it is CHANGED carries TWO signal starts - not two processes. The simple (manual) start may
   appear only once. Two starts need CrtProcessBuilder 1.6.2.24 or later on the environment AND clio
   8.1.0.131 or later: an older environment refuses the second start at build time, and an older clio
   reports it as an R3 error from `validate-process-graph`.
2. (recommended) `validate-process-graph` -> fix every error-severity finding.
3. `list-user-tasks` -> the exact `userTaskName` for each user task. It lists RETIRED schemas as equal
   peers with no marker (`CallUserTask`, `EmailUserTask`, `SendEmailUserTask`), and two shipped schemas
   share the caption "Send email": key on the schema NAME, never a caption.
4. `create-business-process` with the descriptor. Pass the descriptor as the JSON object itself; a
   clio that answers "must be a string" predates that - pass a string holding the same JSON.
5. Verify with `describe-business-process`: element types, user-task names, parameter sources and
   direction; an output you can map FROM has `isOutput: true`.
- Set what was asked for, and nothing else: an OPTIONAL field the request did not mention stays out of
  the descriptor. When the server names a missing value that is a BUSINESS decision (who approves,
  who performs, whom to notify), ask.
- Do NOT run `compile-creatio` to make a process runnable, and do NOT read a raw system record
  (`odata-read` / `execute-esq`) to decide readiness - read status back with `describe-business-process`.
  WITHIN A PROCESS only a `scriptTask` and a `userTask` carrying an after-activity-save script - C#
  you authored - pull a compile in; a CUSTOM user-task schema carries its own compile obligation
  (`process-element-catalog`). On a file-design-mode stand a built process is not runtime-active until
  it is loaded FS->DB and published, so a signal will not fire yet.

== Descriptor skeleton (owner: `process-modeling`) ==
{ "name": "UsrAccount_Onboard", "caption": "Account onboarding", "packageName": "Custom",
  "elements": [ { "name": "OnboardingRequestedStart", "type": "startEvent", "caption": "Onboarding requested" },
                { "name": "NotifyAccountOwner", "type": "performTask", "caption": "Notify the account owner" },
                { "name": "EndOnboardingHandedOff", "type": "endEvent", "caption": "Onboarding handed off" } ],
  "flows": [ { "source": "OnboardingRequestedStart", "target": "NotifyAccountOwner" },
             { "source": "NotifyAccountOwner", "target": "EndOnboardingHandedOff" } ],
  "parameters": [ { "name": "AccountNameParameter", "type": "Text", "direction": "In", "caption": "Account name" } ],
  "mappings": [ { "elementName": "NotifyAccountOwner", "elementParameter": "<ParamName>",
                  "processParameter": "AccountNameParameter" } ] }
- `name` is the element handle used by `flows` (`source` / `target`) and `mappings` (`elementName`).
  Each element's own block (`readData`, `email`, `approval`, `subProcess`, ...), a `filter`, and a
  mapping source other than a process parameter are owned by that element's article,
  `process-data-source-filters` and `process-parameters`; a formula by `process-formulas`, a flow
  condition by `process-branch-conditions`.

== Changing an existing process (owners: `process-modeling`, `process-versions`, `process-sub-process-when`) ==
- Editing or launching ANY existing process starts at `process-versions`; the code you were handed
  is often not the version that runs. Saving a revision, making one actual or rolling back is
  `process-version-writes`.
- ALWAYS `describe-business-process` first, and re-describe after the edit.
- You MUST read `isActiveVersion` from the describe output before ANY modify: a modify overwrites the
  one schema you named, and the previous graph is gone. TRUE: get explicit confirmation (the edit
  request is not one) and offer `modify-business-process-as-new-version`. FALSE: do not modify it -
  re-describe by `activeVersionSchemaUId`, or report and ask.
- Never restructure an existing process without being asked: "add a step" adds it in place and
  extracts nothing. D2 and D4 do not apply to an existing process; D1 only when the requested change is
  itself per-item.
- The modify path runs NO structural validation. `removeElement` cascades - it deletes the flows
  touching the element and the mappings targeting it, and does NOT re-join the gap: add the bridging
  `addFlow` in the same operations array, then re-describe and clean up references that still read the
  removed element. `removeFlow` takes `source` and `target` ONLY — MUST strip `kind` and `condition`
  first; a refused operation rolls back the whole batch.
- Every modify re-draws the WHOLE diagram, and where that would replace a hand arrangement the server
  refuses the edit and asks: read `process-diagram-layout` before editing a process whose diagram
  matters.
- An element whose conditional flows decide a branch: removing its last conditional flow turns the
  branch into a parallel split (every outgoing flow then runs), so prefer additive edits, do not remove
  or rewire it, and tell the user what you left alone. A Delete data retarget and a Change access
  rights collection or filter carry their element article's confirmation. Read the modify safety rules
  in `process-modeling` before any removal or retarget, and the element's own article before editing
  its block.

clio MCP process-digest guide - process build card

Part of the process guide set. Owners win: `process-modeling`, `process-element-catalog`,
`process-naming`, `process-sub-process-when`; open one only when a line sends you. Read ONLY the
articles named here for what you build.

== Recipe (`process-modeling`) ==
0. A request produces ONE process, unless D1 (same work per item of a set, e.g. a task or email per
   record), D2 (3+ element fragment at 2+ places) or D4 (2+ long human phases) fires: read
   `process-sub-process-when` first.
1. ONE START PER TRIGGER (add + change = two `signalStart`s in one process; CrtProcessBuilder 1.6.2.24+, clio 8.1.0.131+).
2. `validate-process-graph`; fix every error.
3. `list-user-tasks` -> `userTaskName`; key on the schema NAME (it lists retired `CallUserTask`, `EmailUserTask`, `SendEmailUserTask` too).
4. ONE `get-target-package` (`package` = named/app package) -> `packageName`; on `success: false` ask, never guess.
   `create-business-process`: pass the descriptor as the JSON object itself, {name, caption, packageName,
   elements[{name, type, caption, <type block>}], flows[{source, target, kind, condition, label}], parameters, mappings}.
5. Verify: `describe-business-process`. No `compile-creatio` unless the save warns.
Set only what was asked; ask who approves, performs, is notified.
Types: startEvent signalStart endEvent exclusiveGateway parallelGateway formulaTask subProcess
scriptTask; sendEmail approval openEditPage preconfiguredPage (never as a userTask); userTask +
userTaskName (readData changeData addData deleteData performTask changeAccessRights).

== Names: N1-N10 of `process-naming` ==
Process `<prefix><Object>_<Action>` (`get-schema-name-prefix`); a called one ends in `SubProcess`.
A caption on EVERY element: sentence case, <= 60 chars, activity verb first, event a noun phrase.
Element `name` = its caption in PascalCase minus only a/an/the/is/are/was/were/be/been/has/have/had;
`<X>Start`, `<X>Signal`, `End<X>`; no `UserTask` postfix, counter or GUID. Parameter: PascalCase
plus a `Parameter` suffix. LABEL EVERY CONDITIONAL AND DEFAULT ARM.

== Element or need -> article ==
- record trigger, Modify data `process-data-elements`; Read data `process-read-data`
- Add data `process-add-data`; Delete data (confirm first) `process-delete-data`
- filter `process-data-source-filters`; parameters, mappings `process-parameters`
- formula `process-formulas`; flow condition `process-branch-conditions`
- branch on a result `process-activity-result-branches`
- Perform task `process-perform-task`; performer `process-task-performer`
- Send email `process-send-email`; Approval `process-approval`
- a page `process-open-edit-page`; Sub-process `process-sub-process`
- access rights (confirm first) `process-access-rights`
- Connected to: `process-activity-connections`; Script task `process-script-task`
- edit or launch an existing process: `process-versions`, then `process-modeling`.

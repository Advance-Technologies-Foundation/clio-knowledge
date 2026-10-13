Part of the process guide set. Build card: owners win; follow pointers.
0. A request produces ONE process (a split the user asks for is built as asked); split only on D1 (per item of a set, unless one filtered data/rights element does it all), D2 (3+ element fragment at 2+ places), D4 (2+ long human phases): `process-sub-process-when`. Never restructure an existing process without being asked (D1 only, per item).
1. ONE START PER TRIGGER (add + change: two `signalStart`s; CrtProcessBuilder 1.6.2.24+, clio 8.1.0.131+).
2. `validate-process-graph`; fix all errors.
3. `list-user-tasks` -> `userTaskName`; key on the schema NAME (retired: `CallUserTask`, `EmailUserTask`, `SendEmailUserTask`).
4. ONE `get-target-package` (named/app `package`); on `success: false` ask, never guess. `create-business-process`: the descriptor as the JSON object itself, {name, caption, packageName, elements[{name, type, caption, block}], flows[{source, target, kind, condition, label}], parameters, mappings}.
5. `describe-business-process`; `compile-creatio` only if the save warns.
Set only what was asked; ask only an unsaid approver, performer or recipient.
Types (else `process-element-catalog`): startEvent signalStart endEvent exclusiveGateway parallelGateway formulaTask(1.6.3.16+) subProcess(1.6.3.26+) scriptTask(1.6.6.30+); sendEmail approval openEditPage preconfiguredPage (never as a userTask); userTask + userTaskName or alias type readData changeData addData deleteData performTask changeAccessRights.
== Names N1-N10 (`process-naming`: relabel, clash) ==
Process `<prefix><Object>_<Action>` (`get-schema-name-prefix`); a called one ends in `SubProcess`. Captions (process, EVERY element): sentence case; element <= 60 chars, activity verb first, event a noun phrase.
Element `name` = caption in PascalCase minus only a/an/the/is/are/was/were/be/been/has/have/had; punctuation splits words; `<X>Start`, `<X>Signal`, `End<X>`; never against the type (no `EndNormal`), no `UserTask` postfix, counter or GUID.
Parameter: PascalCase plus a `Parameter` suffix, caption without; never rename an element's own. LABEL EVERY CONDITIONAL AND DEFAULT ARM; leave an ordinary continuation bare.
== Articles ==
`process-read-data` `process-add-data` `process-delete-data` (confirm first) `process-perform-task` `process-task-performer` `process-send-email` `process-approval` `process-script-task` `process-open-edit-page` `process-preconfigured-page` `process-sub-process` `process-formulas` `process-parameters` `process-access-rights` (confirm first) `process-data-source-filters` `process-branch-conditions`; record trigger, Modify data `process-data-elements`; result branches `process-activity-result-branches`; Connected to `process-activity-connections`
- edit or launch: a family YOU created in THIS task is NOT existing: fix, re-save, run here, no `isActiveVersion` confirmation, unless active and any run (any start) can touch data you did not create (`process-version-writes`). Else or unsure: existing, `process-versions` then `process-modeling`.

clio MCP process-digest guide - the build card: which element, which block, which article

Part of the process guide set; a build starts here. This card owns no rule - it routes. Read ONLY
the articles of the elements you use, and open an owner when its line below applies.

== Build path ==
1. One process per request. Read `process-sub-process-when` first if the request repeats work for
   each item of a set, repeats a fragment, or has two long phases around human work.
2. Names and captions follow `process-naming` (N1-N10): read it before naming anything.
3. Package: the one the user named, else the application's, else ONE `get-target-package` call.
   Never guess one from `list-packages`.
4. `create-business-process` with the descriptor as a JSON object (`process-modeling` owns the
   descriptor and the recipe), then verify with `describe-business-process`. Do not compile unless
   the save warned that the process must be compiled.
5. Editing an existing process starts at `process-versions`.

== Element -> type -> block -> article ==
- runs on a record event: `signalStart` + `signal`/`filter` -> `process-data-elements`,
  `process-data-source-filters`
- read records: `readData` -> `process-read-data`
- change records: `changeData` -> `process-data-elements`
- add records: `addData` -> `process-add-data`
- delete records: `deleteData` -> `process-delete-data` (confirm first)
- a task or activity: `performTask` + `performer`/`activity` -> `process-perform-task`
- grant record access: `changeAccessRights` + `accessRights` -> `process-access-rights` (confirm first)
- email: `sendEmail` + `email` -> `process-send-email`
- approval: `approval` -> `process-approval`
- a page for a user: `openEditPage` / `preconfiguredPage` -> `process-open-edit-page`
- compute a value: `formulaTask` + `formula` -> `process-formulas`
- a branch: `exclusiveGateway` + flow `condition`/`results` -> `process-branch-conditions`,
  `process-activity-result-branches`
- call a process or loop over a set: `subProcess` -> `process-sub-process`
- C#: `scriptTask` -> `process-script-task`
- parameters and mappings: `process-parameters`; "Connected to" links: `process-activity-connections`
- anything else, or what does not build: `process-element-catalog`

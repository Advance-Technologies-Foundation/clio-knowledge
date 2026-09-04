clio MCP process-perform-task guide — the Perform task element (ActivityUserTask)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Perform task element -- what it produces, its parameters,
and what the runtime writes back. Two of its settings are their own subject and have their own articles:
WHO performs the task is owned by `process-task-performer`, and HOW a category or priority value must be
written -- and what silently degrades when it is written as a formula -- is owned by
`process-task-category`. The ids, the two refusal texts and their remedy are here.
Naming anything here? Every element, parameter and process code and caption is governed by N1-N10,
owned by `process-naming` — read it BEFORE you name anything, including when you entered at this
leaf rather than through `process-modeling`.

== Element: Perform task (userTask / performTask -> ActivityUserTask) ==
- WHAT IT IS: the "Perform task" element. Type alias `performTask` (equivalently `userTask` with
  `userTaskName: "ActivityUserTask"`). It creates an Activity of type Task, assigns it to a person, and then
  PAUSES the process until that person completes the activity with a result. It is the way to put a human step
  inside an automated flow.
- USE IT FOR: call a client, review a document, follow up, prepare paperwork, a manual check — any step where the
  process must wait for a person to act outside the process.
- DO NOT USE IT FOR approvals. Creatio has a dedicated Approval element that creates an Approval record (not an
  Activity), emails approver and author, supports delegation, and branches on the verdict. Perform task has no
  approved/rejected semantics. Approval is not buildable from clio yet — say so rather than emulating it with a
  task.
- A "CALL TASK" IS THIS ELEMENT WITH A CALL CATEGORY, NOT THE CALL ELEMENT. `CallUserTask` (the "Call" entry
  in the list-user-tasks palette) is RETIRED: the product removed it from the designer palette and keeps the
  schema only for backward compatibility with old processes. NEVER build a new element with
  `userTaskName: "CallUserTask"` — describe reports it as `deprecated: true`, and it is a dead end in two ways
  a caller cannot see: it builds its Activity through its own private code path instead of the shared user-task
  one, so the element-level performer-assignment object is IGNORED there (a role or manager performer written
  on it is a silent no-op, and its properties page hard-disables the option), and no future clio capability for
  user tasks will reach it. Same for `SendEmailUserTask` and `EmailUserTask` — use the `sendEmail` element.
- ACTIVITY TYPE IS NOT SETTABLE, AND THAT IS NORMAL. A Perform task ALWAYS produces an Activity of type
  **Task** (the platform writes `TypeId = Task` unconditionally; no parameter carries the type). "Call",
  "meeting", "paperwork" intent is expressed through ActivityCategory — the platform's own convention for
  every API that creates activities. When a user asks for "a call task", build a Perform task with
  ActivityCategory = Call and SAY that the activity's type is Task with category Call. Do not reach for the
  retired Call element to satisfy the word "type", and do not claim the type was set.
- WHAT IT PRODUCES: one Activity row — Title, Owner, Category, Priority, Start date (= now + StartIn),
  Due date (= start + Duration), reminder, and any "Connected to" links. It appears in the performer's
  "Business process tasks" tab. The next element runs only after the activity is completed WITH A RESULT.
- READ-BACK CAVEAT: describe-business-process shows an element parameter only when it is BOUND (or is a result).
  A fresh Perform task therefore shows only 11 parameters out of the full set it actually carries (37 declared
  on a stock environment). Absence from describe does NOT mean the parameter does not exist — every parameter
  in the table below is settable by name with `addMapping`. The element's full set is also not a fixed number:
  the platform can derive one extra connection parameter per Activity lookup column that exists on YOUR
  environment, so a custom column adds a parameter.
- IDENTIFY IT IN describe OUTPUT by `buildType: "usertask"` PLUS `userTaskName: "ActivityUserTask"`. It never
  reads back as `performTask`.

--- Parameters you set (addMapping, target = elementName + elementParameter) ---
  Recommendation      LocalizableString. The task subject ("What should be done?"). Becomes the Activity Title
                      (truncated to 500 chars). Works as a plain `value`: the server materializes it into the
                      process schema resource, exactly where the designer stores it (live-verified end to end:
                      the constant reached Activity.Title on a running process). ALWAYS also give the element a
                      meaningful `caption` — the Title falls back to the caption when Recommendation is empty,
                      which makes a good caption a free safety net.
  OwnerId             Lookup -> Contact. THE PERFORMER ("Who performs the task?"), for ONE named person.
                      A TEAM is never routed through OwnerId -- that needs the element-level `performer`
                      block. Both layers, their accepted sources and their refusals are owned by
                      `process-task-performer`; read it before you answer "who".
  ActivityCategory    Lookup -> ActivityCategory. Task category. Required by the designer UI.
                      MUST be a bare record Guid in `value`, never a `[#Lookup...#]` formula -- the
                      formula form degrades the allowed-results list silently, and
                      `process-task-category` owns that rule and its evidence.
                      "To do" = F51C4643-58E6-DF11-971B-001D60E938C6 (also the runtime default).
                      "Call" is TWO rows and the element needs the TASK-typed one — the platform names both:
                      03DF85BF-6B19-4DEA-8463-D5D49B80BB28 is ActivityType Task
                      (ConfigurationConstants.Activity.ActivityCategory.CallAsTask) and is the one to use;
                      E52BD583-7825-E011-8165-00155D043204 is ActivityType Call (the constant plainly named
                      Call) and is the wrong row here. Why: a Perform task ALWAYS creates a Task-typed
                      Activity, and the designer's own category dropdown filters ActivityCategory by
                      ActivityType = Task, so the Call-typed row is one it never offers on this element —
                      resolving "Call" by NAME is a coin flip whose wrong side no human designer can produce.
                      Verify against the environment before trusting either id.
                      Set it as a bare record Guid in `value`. The route ships from CrtProcessBuilder
                      1.3.1.1, and a CURRENT clio additionally refuses any environment older than the
                      version it bundles. From 1.4.0.40 the server also resolves the record's NAME into the
                      parameter's display value, so the designer's "Task category" field shows `Call`
                      rather than the raw Guid, and describe reports it as `valueDisplay` (see
                      `process-task-category`).
                      A stale environment surfaces as ONE OF TWO refusals, and both mean YOUR ENVIRONMENT IS BEHIND,
                      not that the parameter is unsettable: a current clio refuses the call UP FRONT with its
                      package-convergence message naming both versions and the install hint; an older clio
                      lets the call through and the old package rejects it with "Value '...' is not valid for
                      parameter 'ActivityCategory' of type Lookup: a Lookup constant is a formula token, not a
                      plain value. Set it via a mapping 'expression' instead...". Either way: update the
                      package (install-process-builder); do NOT fall back to the expression form (see
                      `process-task-category`).
  ActivityPriority    Lookup -> ActivityPriority. Default = ab96fa02-7fe6-df11-971b-001d60e938c6 (Medium).
                      Same bare-Guid `value` route and same version story as ActivityCategory.
  Duration            Integer, default 20.  Planned duration.        DueDate = StartDate + Duration
  DurationPeriod      Integer, default 0.   Unit for Duration.
  StartIn             Integer, default 0.   Delay before the task starts. StartDate = now + StartIn
  StartInPeriod       Integer, default 0.   Unit for StartIn.
  RemindBefore        Integer, default 0.   Remind the owner this long before the start. 0 = no reminder.
                      Non-zero sets RemindToOwner and RemindToOwnerDate = StartDate - offset.
  RemindBeforePeriod  Integer, default 0.   Unit for RemindBefore.
  ShowExecutionPage   Boolean, default true.  Open the task page automatically for the current user.
  ShowInScheduler     Boolean, default false. Show the task in the Activities calendar. The designer exposes
                      it as the "Show in calendar" checkbox (inherited from the base user-task properties
                      page); addMapping sets the same parameter.
  InformationOnStep   LocalizableString. Designer label "Hint for user" — shown behind the info button on the
                      task page. Works as a plain `value` (same schema-resource materialization as
                      Recommendation).

  ALL THREE *Period PARAMETERS USE THE SAME ENUM:  0=minutes  1=hours  2=days  3=weeks  4=months

--- Parameters the RUNTIME sets — read them, never write them as targets ---
  ActivityResult      Guid. The element's RESULT (the completed activity's result record). Visible in describe
                      from the start (isResult: true). Usable as a mapping SOURCE for a downstream element via
                      `sourceElement` + `sourceElementParameter` (verified: saves, reads back as a
                      server-built `[Element:{uid}]` metapath, and resolves at run time). You can branch
                      on it with `setFlowCondition`, but ONLY while nothing is selected in the results
                      editor for that connector — the designer opens that editor rather than a formula
                      field here, and a selected result makes the platform stop reading the formula.
                      clio refuses it; `process-branch-conditions` owns the rule. Affects 337 of the 1 522
                      conditional flows shipped in 7.8.0. Say two things out loud, or the owner finds
                      them alone: the designer's save raises "Required fields of some elements are not
                      filled in" naming that connector, and a human cannot see or edit the formula
                      there.
  CurrentActivityId   Guid. The created Activity's Id.
                      It is INVISIBLE in describe until bound — the name above is the only way to find it.
                      It resolves as a mapping SOURCE for a downstream element (verified end to end).
                      TRAP: mapping it INTO a later Perform task's own CurrentActivityId makes that task ADOPT
                      the referenced activity instead of creating its own — the platform pattern for updating
                      ONE activity across steps. If the adopted activity is already completed when the later
                      task starts, that task waits FOREVER (completion events route through the activity's
                      ProcessElementId, which is cleared when it first completes). Map the id into a plain Guid
                      parameter or a process parameter unless adopting is exactly what you want.
  IsActivityCompleted Boolean. The runtime sets false at creation and true at completion.
                      It looks writable (it ships a default) — setting it does NOTHING. Do not.
  ExecutionContext    Technical (not serializable). Ignore.

--- Out of scope for parameter mapping ---
  The "Connected to" lookups are CONNECTIONS. Bind them with the `setConnections` op — see
  `process-activity-connections` — NOT with addMapping. THE SHIPPED SET IS THESE 19 (Lead, Account, Contact, Opportunity,
  Invoice, Document, Incident, Case, Order, Requests, Listing, Property, Contract, Project, Problem, Change,
  Release, Application, FinApplication) — AN ENVIRONMENT MAY HAVE MORE: the platform derives one connection
  parameter per Activity lookup column, so a custom column appears as an extra one.
  Careful: ActivityCategory, OwnerId and ShowInScheduler look like connections (same internal tag) but are
  ORDINARY parameters and must be set with addMapping.
  QueueItem: do not use it — no consumer of this parameter is known in the platform runtime or the designer
  package (searched, not proven absent), so a written value has no known effect.

--- Worked example: "Call the client, due in 2 days, assigned to the process starter" ---
1) create-business-process
   { "name": "UsrClient_Call", "caption": "Call client about renewal",
     "elements": [
       { "name": "RenewalCallRequestedStart", "type": "startEvent", "caption": "Renewal call requested" },
       { "name": "CallClientAboutRenewal", "type": "performTask", "caption": "Call the client about the renewal" },
       { "name": "EndClientCalled", "type": "endEvent", "caption": "Client called" } ],
     "flows": [ { "source": "RenewalCallRequestedStart", "target": "CallClientAboutRenewal" },
                { "source": "CallClientAboutRenewal", "target": "EndClientCalled" } ] }

2) modify-business-process  (operations, in this order)
   [ { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "Recommendation",
       "value": "Call the client about the renewal" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "OwnerId",
       "expression": "[#SysVariable.CurrentUserContact#]" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "Duration",       "value": "2" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "DurationPeriod", "value": "2" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "RemindBefore",       "value": "30" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "RemindBeforePeriod", "value": "0" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "ActivityCategory",
       "value": "03DF85BF-6B19-4DEA-8463-D5D49B80BB28" } } ]
   <- ActivityCategory CallAsTask. The two "Call" rows, which one to use and why resolving by NAME is a coin
      flip are stated once near the top of this article; do not restate them here.

3) describe-business-process -> every parameter you bound now appears with its source and value.
   The ones you did NOT bind stay hidden. That is expected; it is not a failure.

Variant — the same task ASSIGNED TO A TEAM ("the sales department calls the client"): drop the OwnerId
mapping from step 2 and set the element-level performer instead (works inline in step 1's element too;
the block's full contract is in `process-task-performer`):
   [ { "op": "setElement", "elementName": "CallClientAboutRenewal",
       "elementUpdate": { "performer": { "type": "role", "role": "Sales Department" } } } ]
Look the role name up on the environment first (SysAdminUnit; a role with no users means a task nobody
sees). And when the request says "a CALL task", set ActivityCategory to the environment's Call category and
SAY the Activity's TYPE is still Task — see the Type-is-not-settable rule at the top of this section.

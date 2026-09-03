clio MCP process-perform-task guide — the Perform task element (ActivityUserTask)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Perform task element -- what it produces, its parameters, and who performs it.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.
Creating a process here? Its code and caption, and every element and parameter code, are governed by
N1-N10, owned by `process-naming` — read it BEFORE you name anything, even if you entered from this leaf
guide rather than `process-modeling`.

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
  OwnerId             Lookup -> Contact. THE PERFORMER ("Who performs the task?").                   [see NOTE-1]
  ActivityCategory    Lookup -> ActivityCategory. Task category. Required by the designer UI.        [see NOTE-2]
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
                      rather than the raw Guid, and describe reports it as `valueDisplay` (see NOTE-2).
                      A stale environment surfaces as ONE OF TWO refusals, and both mean YOUR ENVIRONMENT IS BEHIND,
                      not that the parameter is unsettable: a current clio refuses the call UP FRONT with its
                      package-convergence message naming both versions and the install hint; an older clio
                      lets the call through and the old package rejects it with "Value '...' is not valid for
                      parameter 'ActivityCategory' of type Lookup: a Lookup constant is a formula token, not a
                      plain value. Set it via a mapping 'expression' instead...". Either way: update the
                      package (install-process-builder); do NOT fall back to the expression form (see NOTE-2).
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
                      clio refuses it; `process-formulas` owns the rule. Affects 337 of the 1 522
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

NOTE-1 (the performer): "Who performs the task?" has TWO layers, and picking the right one is the whole game.
  LAYER 1 — the element-level `performer` block (ships from CrtProcessBuilder 1.3.1.1, same version story as
  ActivityCategory). Set it on the performTask element in create/addElement, or in place via setElement's
  `elementUpdate.performer`: { "type": "user"|"manager"|"role", "contact"?, "role"?, "showPage"? }.
  * type "role" is THE way to assign to a TEAM: pass a role name or record id in `role`. The created
    Activity carries the role in its own OwnerRole column and its Owner stays EMPTY — the claim model:
    every user of the role sees the task, whoever takes and completes it is recorded. Do not read the
    empty Owner back as "unassigned". The role is CHECKED TO EXIST on either route, against the same role
    set the designer's picker offers — so a typo'd name, an invented Guid and a USER's own SysAdminUnit id
    are refused instead of stored (a user is not a role; for one person use type "user"), and so is a name
    that matches MORE THAN ONE role - a name cannot say which group performs the task, so pass the id. Look
    the role up on the environment rather than guessing an id.
  * type "manager" resolves the contact's MANAGER at RUN time (default contact = the process starter); when
    the contact's employee record has no manager the process raises an error at run time — say so when the
    org data may be incomplete.
  * type "user" with `contact` is the single-person form: pass a bare Contact record Guid (checked to exist,
    and stored as the encoding the designer produces) or a formula like [#SysVariable.CurrentUserContact#];
    an omitted contact defaults to the process starter.
  * `showPage` omitted defaults to false for manager/role (designer parity — a role activity has no single
    performer to open the page for) and stays untouched for user.
  * describe reads the block back top-level on the element (`performer`: type + the stored formula +
    roleDisplay) and it is re-appliable verbatim. This ELEMENT-LEVEL block is REFUSED on any element other
    than performTask — the retired CallUserTask by name (its runtime IGNORES the assignment). A sendEmail
    element has its own `email.performer`, which is a different field and is not refused.
  LAYER 2 — the OwnerId parameter (Lookup -> Contact), for a SPECIFIC PERSON only. Four working ways:
  * a bare Contact record Guid in `value` — the Guid must be an EXISTING Contact record: an id of another
    entity (a ROLE id is the classic mistake) is REFUSED naming the reference object, because before this
    guard it persisted as a well-formed ConstValue referencing nothing at run time;
  * a process parameter: create it with `typeFromElement` + `typeFromElementParameter: "OwnerId"` so the types
    are guaranteed compatible, then map it in;
  * another element's Contact/Guid output parameter;
  * `expression: "[#SysVariable.CurrentUserContact#]"` for "whoever started the process".
  A Lookup -> SysAdminUnit PARAMETER source is likewise REJECTED (incompatible reference object). NEVER route
  a team through OwnerId — that is what the `performer` block's type "role" is for.
  Leaving both layers unset is NOT an unassigned task — at run time the task silently falls to the current
  user's contact (whoever started the process). There is no "nobody" state; omitting the performer is a choice.

NOTE-2 (ActivityCategory): it MUST be a constant (`value`, stored as ConstValue), not a formula. The element's
  allowed-results list is computed from the category ONLY when the category's source is ConstValue (the
  platform's `GetResultParameterAllValues` reads `SourceValue.Value` only for a ConstValue source — client-side
  and server-side alike); writing it as a `[#Lookup...#]` expression sets the Activity's category column but
  SILENTLY DEGRADES the allowed-results list the task page / designer result dropdown offers, falling back to
  the default set. Do NOT try to verify the degradation through the `Activity.AllowedResult` column — that
  column derives from outgoing CONDITIONAL flows, not from the category, and is empty either way on a process
  without them. So the bare-Guid `value` is the only correct route; on a pre-1.3.1.1 package the parameter
  cannot be set correctly — update the package rather than using the expression form.

  This matches what the DESIGNER stores for the real-lookup families this rule is about (a task element's
  ActivityCategory / ActivityPriority): a lookup constant a human picks is
  `{Source: ConstValue, Value: <bare record Guid>}` on the element parameter, with the record's NAME - or
  nothing at all - in the parameter's DisplayValue. Do not read that as a universal designer rule: the
  designer's own corpus is mixed (absent, the raw Guid and a readable name all occur, and the platform ships
  a first-party schema with the raw Guid in DisplayValue), so an agent checking a real schema will find
  counter-examples. Name-or-nothing is the CORRECT convention, not the most common one. The `[#Lookup...#]`
  macro form the designer does produce belongs to a different place: a change-data COLUMN mapping, where the
  value is a formula in its own right. Do not carry the macro across to an element parameter because you saw it
  in a designer-authored schema.

  DisplayValue is where a design-time defect used to live and is worth understanding, because it is invisible
  in `metadata.json`: it is a LOCALIZABLE string, so it is serialized into the schema's RESOURCES
  (`BaseElements.<Element>.Parameters.<Param>.DisplayValue`), not into the metadata beside `Value`. The designer
  shows a NON-EMPTY DisplayValue verbatim and resolves the record name itself only when it is EMPTY — so a
  DisplayValue holding the raw id made the "Task category" field render `03df85bf-…` instead of `Call`, while
  the runtime behaved correctly the whole time. From CrtProcessBuilder 1.4.0.40 the server resolves the
  referenced record's name and stores THAT, and leaves DisplayValue unset when it cannot (which is the correct
  degrade — the designer then resolves the name). Nothing about the input contract changed: you still pass a
  bare record Guid.
  Why the server resolves the name rather than simply leaving DisplayValue empty: only the Perform task's
  category field re-resolves an empty display value (`ActivityUserTaskPropertiesPage.initActivityCategory`).
  Every other designer surface reads the parameter through `getMappingValue()`, which returns
  `displayValue || value` (`process-schema-parameter.js`) — an empty display value renders the raw Guid again
  there. "Just write nothing" is therefore the cheaper WRONG fix, not a safe alternative.
  Evidence: observed on a Creatio 8.x stand through `describe-business-process` and the pulled schema
  resources (`Resources/<Process>.Process/resource.en-US.xml`) against CrtProcessBuilder 1.4.0.40; the client
  behaviour is read from the designer's own source, not inferred.

  Two conveniences shipped with it, both from 1.4.0.40:
  * an already-composed `[#Lookup.{objectUId}.{recordId}#]` passed as a MAPPING `value` on a Lookup target is
    DECODED to the bare record id and stored as a ConstValue — so a value echoed back from describe re-submits
    unchanged. This does not make the expression form correct here; it makes the round trip safe. Which
    routes accept the macro and which refuse it is owned by `process-parameters` (the "A LOOKUP value is
    DIFFERENT" bullet) — read it there rather than here, so the two never drift;
  * `describe-business-process` reports the resolved name as `valueDisplay` beside the unchanged bare-Guid
    `value`. `valueDisplay` is read-only and re-derived on every write — never feed it back as `value`. Its
    absence means the environment could not name the record, NOT that the value is wrong.

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
   <- ActivityCategory CallAsTask. The three rows, which one to use and why resolving by NAME is a coin
      flip are stated once near the top of this article; do not restate them here.

3) describe-business-process -> every parameter you bound now appears with its source and value.
   The ones you did NOT bind stay hidden. That is expected; it is not a failure.

Variant — the same task ASSIGNED TO A TEAM ("the sales department calls the client"): drop the OwnerId
mapping from step 2 and set the element-level performer instead (works inline in step 1's element too):
   [ { "op": "setElement", "elementName": "CallClientAboutRenewal",
       "elementUpdate": { "performer": { "type": "role", "role": "Sales Department" } } } ]
Look the role name up on the environment first (SysAdminUnit; a role with no users means a task nobody
sees). And when the request says "a CALL task", set ActivityCategory to the environment's Call category and
SAY the Activity's TYPE is still Task — see the Type-is-not-settable rule at the top of this section.

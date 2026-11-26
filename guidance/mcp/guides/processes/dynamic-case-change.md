clio MCP process-dynamic-case-change guide - read and change an existing dynamic case

Scope
Reads and changes an existing dynamic case (DCM) with `describe-dynamic-case` and `modify-dynamic-case`: its stages,
their order, groups, captions and colours, the steps inside a stage and when it moves on, the case caption, and
whether it is enabled. Make the change as a new version or switch the active one: read
`process-dynamic-case-versions` first. Building a case, the bar, the other refusals of
`create-dynamic-case` and the undo: `process-dynamic-case`. Sending `filter`, `remove-filter` or
`deactivate-unfiltered`, or splitting an object's records into cases by a lookup column (B2B and B2C orders with
stages of their own): read `process-dynamic-case-filters` first; a stage change on one of several filtered cases
does not need it. A refusal of these tools whose fix changes a filter: relay it, the user picks
the fix, then read `process-dynamic-case-filters` before that fix's dry run.

Version boundary, before anything else: `describe-dynamic-case` and `modify-dynamic-case` need clio 8.1.0.153 or
later. If `clio-run` answers `unknown tool` for `describe-dynamic-case`, the clio serving you is older: stop here and
tell the user to update clio (until then the case designer changes a case); answer a question about the case as
`process-dynamic-case` says, from its dry run. They also need a recent CrtProcessBuilder on the
environment: clio refuses an older one, naming the version it needs (`process-builder-without-case-change` when it
has no such operation), and `install-process-builder`, once the user approves it (`core-rules`), is the remedy. An
older clio (no `confirm-value-rename`) works the older way: its describe has no `records` (absent is not 0), and
removing a stage that records hold is not refused: count them with the data tools and ask the user first; an item's
`caption` renames only the case stage (do not offer it; to rename one, update clio).

== Change an existing case ==
1. `describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only): the stages in
   order with `group`, `successful`, `isFinal`, `hasSteps`, `runningInstances`, `records` (the records holding its
   value, finished cases included), `steps` and `transitionType` ("Steps inside a stage"), and `enabled`.
   `packageReadOnly: true` (the package is not editable): a change to
   its stages, captions or filter goes straight to a new version (`process-dynamic-case-versions`), never through step 2
   (`package-read-only`); with `hasSteps` too, no version can be made (a case with steps gets one only in its own
   package): the case designer makes that change, say so before offering a version. Several cases on the object: name one with `case-name` (modify by object answers
   `case-ambiguous`). A warning that a stage "shows as" another name: the bar and the designer show the value's name,
   not the stage caption; a rename (step 2) fixes it. `entity-schema-name`: the schema name or the user's word (a
   unique caption is resolved). `entity-not-found` or several matches list `Name (caption)` candidates: pick one or
   ask. None listed (older package): `find-entity-schema`.
2. `modify-dynamic-case { environment-name, case-name | case-uid | entity-schema-name, stages?, caption?, filter?,
   remove-filter?, enabled?, confirm-running-instances?, confirm-stage-records?, confirm-value-rename?, dry-run }`
   writes; the top-level `caption` renames the CASE. `stages` is the WHOLE new list of items `{"value": Id or exact
   display value, "caption"?, "color"?, "group"?, "successful"?}`, e.g.
   `{"value":"Rejected","group":"Approved","successful":false}`: a stage left out is REMOVED with its steps ("Steps
   inside a stage"), a value not yet a stage
   is ADDED (it must be in the lookup: add it with the data tools first, only after the user agreed to that new
   value, never unasked; the user asked for that stage, so this changes the case, it does not get around it). Fields
   an item leaves out keep their values; `group: null` ungroups; `color` (`#RRGGBB`) recolours the stage; an item's
   `caption` renames the stage AND its lookup value, as the case designer does (the bar shows the value's name): the
   lookup's value is renamed WHEREVER it is used (every record, list and filter of every object that shows it), in
   the user's language; ask for the yes in those words. A name another value already has, or two items given the same
   name: `stage-value-ambiguous`; no right to edit the lookup: `rights-missing`. `filter` and `remove-filter`:
   MUST read `process-dynamic-case-filters` first: a FIRST filter sets the object's filter column for good (no tool
   removes it), and every case of the object then needs a filter; say so before the yes. First `dry-run: true`: show
   the user `diff` and the notes, write only after their yes. `unchanged` means nothing to do.
3. Some writes need the user's own yes, and are refused until then; one refusal, and the dry run's notes, name
   every one still missing: `running-instances-confirmation-required` (cases run: how many, in which stages; they
   continue, possibly on the stages as they were) -> `confirm-running-instances: true`;
   `stage-records-confirmation-required` (records hold a removed stage's value: all keep their data; those without
   a running case show no current stage while the case is enabled, and no bar of it while it is disabled (another
   enabled case they fit shows its own); one whose case runs keeps its version while it runs; for a version, once
   it is active; relay the note's split) -> `confirm-stage-records: true`; `value-rename-confirmation-required`
   (a caption renames the lookup's value wherever it is used) -> `confirm-value-rename: true`. Tell the user each
   reason, and pass each flag only after they say yes to it, NEVER on your own.
4. `enabled: false` / `true` disables or re-enables the case and goes ALONE (`invalid-request` otherwise). Disabled:
   nothing is deleted; new records start no case of it (one that fits another enabled case of the object starts
   that one); its running cases continue, unless another case of the object stays enabled: then each is CANCELLED at
   its record's next change of the stage or filter column, and the record starts the enabled case it fits, or none
   (the note says so; tell the user before their yes).
   Re-enabling next to another enabled case that would start for the same records: `case-exists` or
   `filter-overlap`; ask the user whether to disable that case first or keep this one disabled. A case without a filter while the cases are split by a column:
   `filter-required` (give it a filter first). `settings-conflict` on re-enabling: the object's case settings name
   another stage column or another filter column, or there are several rows; the case designer settles that.
Refusals: `stage-in-use`: tell the user how many records sit there; they move them to another stage or wait until
the cases end - never change records' stage on your own - or, after their yes, make the change as a new version
(read `process-dynamic-case-versions` first): running cases keep the version they started on. `stage-is-condition-target` (a kept step's condition moves records to it): a Perform task: send that step's
`conditions` without that stage in the same change; another step: the case designer, or remove the step; or keep the
stage; `package-read-only` (the case's package is not editable: offer the change
as a new version in an editable package, `process-dynamic-case-versions`), `invalid-request` for `enabled` on a case that is
not the active version (name the active one the message gives) or for a case not of `entity-schema-name`,
`not-in-version` (a first filter on a member that is not active: `process-dynamic-case-versions`), `stage-list-duplicate` for
a case with two stages on one value (the case designer fixes it).
Outcomes: `failed-restored`: the save did not take, the case is as before; `failed-leftover`: relay its `leftovers`
and have the user check the case in the designer; `unknown`: the reply was lost, so the change may or may not have
been written - describe the case before retrying. No compile or restart; F5.

== Steps inside a stage ==
- Needs clio 8.1.0.159 or later (its `modify-dynamic-case` stage item takes `steps` in `get-tool-contract`)
  and CrtProcessBuilder 1.6.6.204 (`install-process-builder` once the user approves it). Older: the case designer.
- Describe first: each stage's `steps` (`uId`, `caption`, `type`, `required`, `after`, `hasConditions`,
  `runningInstances`; a Perform task also `performer`, `activity`, `category`, `hint`, `useBackgroundMode`) and
  `transitionType`.
- A stage item's `steps` is the WHOLE ordered step list of that stage; leave it out to keep them. A stored step is
  matched by `uId`, else by caption among the steps of its type, and one left out is REMOVED (the step that waited
  for it then starts with the stage). A kept step may be sent back as describe returns it (its `name`,
  `hasConditions` and `runningInstances` are ignored): only what differs from the stored step changes. A new step:
  `{"type":"performTask","caption":"Check the order","required":true,"after":"Draft check","performer":{"type":"user","contact":"[#Record.UsrOwner#]"},"activity":{"duration":{"value":2,"unit":"hours"},"priority":"High"},"category":"To do","hint":"..."}`;
  optional by default, a task of To do and Medium for the current user, linked to the record (a note says when tasks have no connection to the object). Only
  `performTask` is written: another type (the refusal lists the offered ones) keeps its settings, and the case
  designer changes them; its caption, `required` and `after` still change here.
- `after` is the caption or `uId` of an EARLIER step of the stage; `""` makes it start with the stage. A value is a
  constant, `[#SysVariable.CurrentUserContact#]`, or a column of the case's record `[#Record.<Column>#]`: a case has
  no process parameters (`step-value-source-unsupported`).
- `transitionType`: `none` (moved by hand), `afterRequired` (after its required steps), `afterAll`. A stage also
  waits for its steps with conditions. A note warns when there is no step to wait for (an automatic move passes over
  the stage; a record put into it stays) or it cannot move on (last stage, no later connection).
- Steps create work on real records: an added step starts for records that ENTER the stage from now on. Show the
  dry run's `diff.steps` and notes and write only after the user's yes. `step-in-use`: running cases wait in the
  removed step (the count; they never move on), so keep it or wait until they end. Other refusals name the stage
  and step: `step-invalid`, `step-after-invalid`, `step-type-not-supported`.

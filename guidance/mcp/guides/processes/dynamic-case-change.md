clio MCP process-dynamic-case-change guide - change an existing dynamic case, and several cases on one object split by a filter

Scope
Reads and changes an existing dynamic case (DCM) with `describe-dynamic-case` and `modify-dynamic-case`: its stages,
their order, groups, captions and colours, the case caption, its filter, and whether it is enabled. Splits an
object's records between several cases by one lookup column (for example B2B and B2C orders with stages of their
own), and owns the filter rules of `create-dynamic-case` too. Building a case, the bar, the other refusals of
`create-dynamic-case` and the undo: `process-dynamic-case`.

Version boundary, before anything else: `describe-dynamic-case` and `modify-dynamic-case` need clio 8.1.0.153 or
later. If `get-tool-contract` does not list `describe-dynamic-case`, the clio serving you is older: stop here and
tell the user to update clio (until then the case designer changes a case); answer a question about the case as
`process-dynamic-case` says, from its dry run. They also need CrtProcessBuilder 1.6.6.179 or later on the
environment: an older one refuses them (`process-builder-without-case-change` when it has no such operation), and
`install-process-builder`, once the user approves it (`core-rules`), is the remedy. A clio whose
`modify-dynamic-case` takes `confirm-value-rename` (`get-tool-contract`) refuses both tools below 1.6.6.184 the same
way. An older clio (no `confirm-value-rename`) works the older way, so read steps 1-3 with that: describe has no
`records` (absent is not 0: count the records with the data tools); an item's `caption` renames only the stage in the
case, which the bar and the designer do not show, so do not offer it, and to rename a stage tell the user to update
clio; removing a stage that records hold is not refused, so check them with the data tools and ask the user first.

== Change an existing case ==
1. `describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only): the stages
   in order with `group`, `successful`, `isFinal`, `hasSteps`, `runningInstances`, `records` (the records holding
   its value, finished cases included), and `enabled`. Several cases on the object: name one with `case-name`
   (modify by object answers `case-ambiguous`). A warning that a stage "shows as" another name: the bar and the
   designer show the value's name, not the stage caption; a rename (step 2) fixes it.
2. `modify-dynamic-case { environment-name, case-name | case-uid | entity-schema-name, stages?, caption?, filter?,
   remove-filter?, enabled?, confirm-running-instances?, confirm-stage-records?, confirm-value-rename?, dry-run }`
   writes; the top-level `caption` renames the CASE. `stages` is the WHOLE new list of items `{"value": Id or exact
   display value, "caption"?, "color"?, "group"?, "successful"?}`, e.g.
   `{"value":"Rejected","group":"Approved","successful":false}`: a stage left out is REMOVED, a value not yet a stage
   is ADDED (it must be in the lookup: add it with the data tools first, only after the user agreed to that new
   value, never unasked; the user asked for that stage, so this changes the case, it does not get around it). Fields
   an item leaves out keep their values; `group: null` ungroups; `color` (`#RRGGBB`) recolours the stage; an item's
   `caption` renames the stage AND its lookup value, as the case designer does (the bar shows the value's name): the
   lookup's value is renamed WHEREVER it is used (every record, list and filter of every object that shows it), in
   the user's language; ask for the yes in those words. A name another value already has, or two items given the same
   name: `stage-value-ambiguous`; no right to edit the lookup: `rights-missing`. `filter` and `remove-filter`:
   "Several cases on one object". First `dry-run: true`: show the user `diff` and the notes, write only after their
   yes. `unchanged` means nothing to do.
3. Some writes need the user's own yes, and are refused until then; one refusal, and the dry run's notes, name
   every one still missing: `running-instances-confirmation-required` (cases run: how many, in which stages; they
   continue, possibly on the stages as they were) -> `confirm-running-instances: true`;
   `stage-records-confirmation-required` (records hold a removed stage's value, finished cases included: they keep
   their data and show no current stage) -> `confirm-stage-records: true`; `value-rename-confirmation-required`
   (a caption renames the lookup's value wherever it is used) -> `confirm-value-rename: true`. Tell the user each reason, and
   pass each flag only after they say yes to it, NEVER on your own.
4. `enabled: false` / `true` disables or re-enables the case and goes ALONE (`invalid-request` otherwise). Disabled:
   nothing is deleted, new records start no case, and its running cases continue, unless another case of the
   object stays enabled: then each is CANCELLED at its record's next stage change (the note says so; tell the user
   before their yes). Re-enabling next to another enabled case
   that would start for the same records: `case-exists` or `filter-overlap`; ask the user whether to disable that
   case first or keep this one disabled. A case without a filter while the cases are split by a column:
   `filter-required` (give it a filter first). `settings-conflict` on re-enabling: the object's case settings name
   another stage column or another filter column, or there are several rows; the case designer settles that.
Refusals: `stage-in-use`: tell the user how many records sit there; they move them to another stage or wait until
the cases end - never change records' stage on your own. `stage-has-steps` (it has steps or a step condition
targets it: the case designer), `package-read-only` (a new version is not supported yet).
Outcomes: `failed-restored`: the save did not take, the case is as before; `failed-leftover`: relay its `leftovers`
and have the user check the case in the designer; `unknown`: the reply was lost, so the change may or may not have
been written - describe the case before retrying. No compile or restart; F5.

== Several cases on one object ==
- Needs clio 8.1.0.154 or later: an older clio has no `filter`, `remove-filter` or `deactivate-unfiltered`.
- The platform starts EVERY enabled case whose filter matches a new record. One object's cases are split by ONE
  lookup column, the filter column (never the stage column): each case takes some of its values
  (`filter: {column, values: [Id or exact display value]}`) or its empty value (`filter: {column, empty: true}`),
  and no two cases share a value. Each case has its own stages: `stages` takes any 2 to 20 values of the stage
  lookup, in order.
- Describe first: `describe-dynamic-case { entity-schema-name }` shows each case's `filter` and the settings row's
  `filterColumnName`; its notes name the values no case is for. Answer "which stages do B2B orders have" from it.
- The object has ONE case without a filter (the usual start), so a filtered `create-dynamic-case` is refused
  `unfiltered-case-exists`. In the FIRST answer, before any write, offer the case designer's two ways, each with
  its dry run, and let the user choose:
  (1) keep the existing stages for some values: `modify-dynamic-case { filter }` on that case, then
  `create-dynamic-case { filter, stages }` for the others; nothing is disabled. (2) turn the existing stages off:
  `create-dynamic-case { filter, stages, deactivate-unfiltered: true }` disables that case: new records start only
  the filtered cases, and each running case of it is CANCELLED at its record's next status change; the record then
  starts the filtered case its value fits, in its current status, or none. Say so, with the running count from
  describe, before the user chooses. Pass `deactivate-unfiltered` ONLY after the user chose (2), never on your own.
  Undoing (2) (the undo in `process-dynamic-case`) also re-enables that case (cancelled cases stay cancelled):
  `modify-dynamic-case { enabled: true }`, alone, after the user's yes. The filter column the create set stays (no
  tool removes it yet), so a case without a filter is then refused `filter-required`: offer it a filter first.
- Refusals, all before anything is written: `filter-overlap` (another enabled case has one of these values, or the
  empty value: change that case instead, one record never starts two cases); `filter-required` (the cases are split
  by a column, so the case needs a filter). `remove-filter: true` removes the case's filter, but only where the
  settings row names no filter column (a filter made in the designer): once a filter column is set, which every
  filter set by these tools does, it is refused `filter-required`, so do not offer it; with `filter`:
  `invalid-request`. `settings-conflict` (they are split by another column: not supported yet);
  `filter-column-invalid` (not a lookup column of the object, or the stage column); `filter-value-unknown` /
  `filter-value-ambiguous` (as for stages).
- The first filter sets the filter column on the object's case settings (`settings.filterColumnSet`): from then on
  every case of the object needs one. A call with a filter needs the right to edit `SysDcmSettings` records too
  (`rights-missing` otherwise).
- A value no enabled case is for (a note names it): its records start no case and show no stage bar. There is no
  catch-all case; a type that should have no stages gets no case: create nothing and say so.
- A filter decides only which NEW records start a case: running cases continue, and an existing record without a
  case gets the matching one at its next change of the stage or filter column. A record whose filter value changes
  keeps its case. The record page is not changed: the bar switches to the matching case only when its
  `askUserToChangeSchema` is true; offer that page change, do not make it unasked.

clio MCP process-dynamic-case-change guide - change an existing dynamic case, make a new version of it and switch the active one

Scope
Reads and changes an existing dynamic case (DCM) with `describe-dynamic-case` and `modify-dynamic-case`: its stages,
their order, groups, captions and colours, the case caption, and whether it is enabled. Owns a case's versions: a
change saved as a new version (`modify-dynamic-case-as-new-version`) and the switch of the active one
(`set-active-dynamic-case-version`). Building a case, the bar, the other refusals of `create-dynamic-case` and the
undo: `process-dynamic-case`. Several cases on one object split by a lookup column (B2B and B2C orders with stages
of their own), a `filter`, `remove-filter` or `deactivate-unfiltered`: read `process-dynamic-case-filters` first. A
refusal of these tools whose fix changes a filter: relay it, the user picks the fix, then read
`process-dynamic-case-filters` before that fix's dry run.

Version boundary, before anything else: `describe-dynamic-case` and `modify-dynamic-case` need clio 8.1.0.153 or
later. If `clio-run` answers `unknown tool` for `describe-dynamic-case`, the clio serving you is older: stop here and
tell the user to update clio (until then the case designer changes a case); answer a question about the case as
`process-dynamic-case` says, from its dry run. They also need CrtProcessBuilder 1.6.6.209 or later on the
environment: clio refuses an older one, naming the version it needs (`process-builder-without-case-change` when it
has no such operation), and `install-process-builder`, once the user approves it (`core-rules`), is the remedy. An
older clio (no `confirm-value-rename`) works the older way: its describe has no `records` (absent is not 0), and
removing a stage that records hold is not refused: count them with the data tools and ask the user first; an item's
`caption` renames only the case stage (do not offer it; to rename one, update clio).

== Change an existing case ==
1. `describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only): the stages in
   order with `group`, `successful`, `isFinal`, `hasSteps`, `runningInstances`, `records` (the records holding its
   value, finished cases included), and `enabled`. `packageReadOnly: true` (the package is not editable): a change to
   its stages, captions or filter goes straight to a new version ("Versions of a case", step 1), never through step 2
   (`package-read-only`). Several cases on the object: name one with `case-name` (modify by object answers
   `case-ambiguous`). A warning that a stage "shows as" another name: the bar and the designer show the value's name,
   not the stage caption; a rename (step 2) fixes it. `entity-schema-name`: the schema name or the user's word (a
   unique caption is resolved). `entity-not-found` or several matches list `Name (caption)` candidates: pick one or
   ask. None listed (older package): `find-entity-schema`.
2. `modify-dynamic-case { environment-name, case-name | case-uid | entity-schema-name, stages?, caption?, filter?,
   remove-filter?, enabled?, confirm-running-instances?, confirm-stage-records?, confirm-value-rename?, dry-run }`
   writes; the top-level `caption` renames the CASE. `stages` is the WHOLE new list of items `{"value": Id or exact
   display value, "caption"?, "color"?, "group"?, "successful"?}`, e.g.
   `{"value":"Rejected","group":"Approved","successful":false}`: a stage left out is REMOVED with its steps (the dry
   run's `diff.steps` names them: relay them before the yes), a value not yet a stage
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
("Versions of a case"): running cases keep the version they started on. `stage-is-condition-target` (a step
condition moves records to that stage) and `step-has-conditions` (a step of that stage has conditions): change
the step in the case designer first, or keep the stage; `package-read-only` (the case's package is not editable: offer the change
as a new version in an editable package, "Versions of a case"), `invalid-request` for `enabled` on a case that is
not the active version (name the active one the message gives) or for a case not of `entity-schema-name`,
`not-in-version` (a first filter on a member that is not active: "Versions of a case"), `stage-list-duplicate` for
a case with two stages on one value (the case designer fixes it).
Outcomes: `failed-restored`: the save did not take, the case is as before; `failed-leftover`: relay its `leftovers`
and have the user check the case in the designer; `unknown`: the reply was lost, so the change may or may not have
been written - describe the case before retrying. No compile or restart; F5.

== Versions of a case ==
- Needs clio <CLIO-TBD> or later: if `clio-run` answers `unknown tool` for `modify-dynamic-case-as-new-version`, the
  clio is older: tell the user to update clio, or to save the new version and make it actual in the case designer.
  An older clio's describe has no `versions`: a missing list there does not mean the case has no other version.
- A version is a separate case schema of the case's family. At most one member is ACTIVE: while the family is
  enabled, new records start that one (if it has a filter, only those it is for; Package position, below, is the
  exception; `process-versions` owns the version model). `describe-dynamic-case` takes any member by `case-name` / `case-uid`
  (one that is not active gets a note naming the active one; `entity-schema-name` reads the active one) and lists the
  family in `versions` (each member's package, `active`, running cases, link row); the list is absent for a case with
  no other version.
- A member that is not active is a draft: `modify-dynamic-case` by its `case-name` / `case-uid` changes it in place
  (`case.activeVersion: false`, a note); new records do not see it until it is made active. Not fully apart: a stage
  caption on it still renames the shared value at once, and a member that was active before may still have running
  cases. `enabled` belongs to the whole family: on a member that is not active it is refused `invalid-request`; send
  it for the active one.
- Offer a new version when the user wants the change prepared without new records seeing it yet, when the case's
  package is not editable (`packageReadOnly: true`, or `package-read-only`), or when running cases sit in a stage to
  remove (`stage-in-use`).
1. Make it: `modify-dynamic-case-as-new-version { environment-name, case-name | case-uid | entity-schema-name,
   package-name?, stages?, caption?, filter?, remove-filter?, confirm-stage-records?, confirm-value-rename?, dry-run?
   }` copies the family's ACTIVE member (another member is refused, naming the active one) into a new version that is
   NOT active, and applies `stages`, `caption` and `filter` to it with the shapes and the confirmations of
   `modify-dynamic-case` (no `enabled`; running cases are not asked about: they keep their version). `package-name`
   left out: the case's own package when it is editable, otherwise refused `package-name-required`: ask the user which
   editable package. `dependencies-missing`: the package must depend on the packages the message names; add them only
   after the user's yes. `steps-not-supported`: a version copies the source's steps as they are, so steps or
   `transitionType` sent with it are refused: change them on the version afterwards (`modify-dynamic-case` by its
   `case-name`, or the case designer); and a case with steps gets a version only in its own package (or in the case
   designer). First `dry-run: true` (`planned`): show the user `diff`, the notes and the warnings, write only
   after their yes. NOT idempotent: every write makes one more version, and the toolkit cannot delete a version; it is
   saved inactive, so new records keep starting the active case until a switch (a confirmed value rename has acted
   already, below), and a later change to it is `modify-dynamic-case` by its `case-name`, in place (no further
   version). A stage value rename
   (`confirm-value-rename`) acts AT ONCE, not at activation: it renames the value for the active case and every
   record too - say so before the yes; activating then changes no stage name on the bar or in the designer. A FIRST
   filter would set the object's filter column for every
   case at once, so it is refused `not-in-version` (set the filter on the active case with `modify-dynamic-case`
   first, or leave the version without one). The checks against the object's other cases run at activation; here
   they are notes.
   Outcomes: `version-created` with the inactive `version`; `failed-rolled-back`: nothing of the version stays;
   `failed-leftover` WITH a `version` (`version-number-taken` when another writer took its number): the version was
   saved and is KEPT, named in `leftovers` with its link row - do not activate it; relay `leftovers` in full (a value
   left renamed stays renamed: offer to rename it back) and describe the family before telling the user what changed;
   no tool removes the version; `failed-leftover` WITHOUT a `version`: nothing was saved, but a row or
   a value rename could not be undone - relay `leftovers` (a value left renamed: offer to rename it back); `unknown`:
   the reply was lost. After `failed-leftover` or `unknown`, describe the family before any retry: a retry makes
   another version.
2. Check it: `describe-dynamic-case { case-name: version.name }` and give the user its `designerUrl` to open the
   version in the case designer.
3. ASK the user whether to make it active: a decision of its own, never part of the yes to create it.
4. Make it active: `set-active-dynamic-case-version { environment-name, version-name | version-uid,
   confirm-package-position?, dry-run? }`, destructive, any member of the family (the first one too: that is the way
   back). `version-not-found`: describe the case by its object (`entity-schema-name`) and name the family's `versions`
   to the user; never guess a name. First `dry-run: true`; do not describe that member to prepare the switch (step
   2's check aside): the dry run checks it, and its notes count the records sitting in stages of the active version
   that it lacks; describe it (`case-name`) only when the user asks what it holds or which member they meant is
   unclear. Asked for the switch ("switch orders to it")? That is the yes: notes only - switch, relay every note in
   full with the result; warnings - show them, wait for the yes. Not asked: step 3. `unchanged`: it is active
   already, nothing written. Refused before any write: `settings-conflict`, `case-exists`, `filter-overlap`,
   `unfiltered-case-exists`, `filter-required` (the family's own members do not count): the message says what
   conflicts and, except for `settings-conflict` (the case designer settles it), how to fix it; relay it without
   describing again, and the user picks the fix; for one that changes a filter, read `process-dynamic-case-filters`
   before its dry run; run a fix (a filter change, or disabling another case) only after the user's yes.
   Tell the user what the switch does, for the records that fit the version's filter (if any): new ones start it once
   the family is enabled (unless Package position, below, applies); while the family is enabled, one with no running
   case shows the version at once (no current stage if it holds a stage value the version lacks); a running case
   keeps its version while it runs, and Change case on the record's page cancels it and starts the enabled case the
   record fits, if any (with this family enabled and the record fitting it: the new version, from its first stage,
   written into the record); open pages need F5. Relay the notes' split of the records in stages the new version
   lacks, never "the records show no current stage".
   - A disabled family is switched too, and starts nothing until `modify-dynamic-case { enabled: true }` on the
     new active member, alone, after the user's yes. Until then a record without a running case shows no bar of it
     (an enabled case it fits shows its own); Change case cancels the record's running case and starts only an
     enabled case the record fits, never a version of this family; and if another case of the object is enabled, the
     family's running cases are cancelled at their record's next change of the stage or filter column, and the record
     then starts the enabled case it fits, or none (the notes say so).
   - Package position: a version whose package ranks at or below a member in a non-editable package that still reads
     active may not become the one new records start (a warning at creation and in the dry run;
     `decidedByPackagePosition` names such members). The switch is then refused
     `package-position-confirmation-required`; relay it: the remedy is a dependency of the version's package on that
     member's package, or `confirm-package-position: true` to switch anyway, each only after the user's yes to it. A
     switch to a member in a non-editable package is kept on this environment only: it does not travel with any
     package (a note); tell the user.
   Outcomes: `activated`; `failed-not-restored` (`write-failed`): the switch did not read back (relay
   `activeVersion` and `siblingsStillFlagged`); it is never undone, and the same call can be repeated once the cause
   is fixed; `failed-leftover`: no switch, a link row it added could not be removed - relay `leftovers`; `unknown`:
   describe the family (`active` names the member new records start) before repeating the call. No compile or
   restart.

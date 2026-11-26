clio MCP process-dynamic-case-versions guide - make a new version of a dynamic case and switch the active one

Scope
Split out of `process-dynamic-case-change`, which owns describing and changing a case, the shapes of `stages`
and `caption`, and every confirmation of `modify-dynamic-case`. A switch of the active version needs nothing from it;
a change made as a new version needs both articles: read them together, in one message. A `filter` or
`remove-filter`: `process-dynamic-case-filters`. This article owns a case's
versions: the `versions` list of `describe-dynamic-case`, a member that is not active changed in place, a change
saved as a new version (`modify-dynamic-case-as-new-version`) and the switch of the active one
(`set-active-dynamic-case-version`).

Version boundary, before anything else: `modify-dynamic-case-as-new-version` and `set-active-dynamic-case-version`
need clio <CLIO-TBD> or later. If `clio-run` answers `unknown tool` for `modify-dynamic-case-as-new-version`, the
clio is older: stop here and tell the user to update clio, or to save the new version and make it actual in the case
designer. An older clio's describe has no `versions`: a missing list there does not mean the case has no other
version. They also need CrtProcessBuilder 1.6.6.217 or later on the environment: clio refuses an older one, naming
the version it needs, and `install-process-builder`, once the user approves it (`core-rules`), is the remedy.

== Versions of a case ==
- A version is a separate case schema of the case's family. At most one member is ACTIVE: while the family is
  enabled, new records start that one (if it has a filter, only those it is for; Package position, below, is the
  exception). `describe-dynamic-case` takes any member by `case-name` / `case-uid`
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
  remove (`stage-in-use`). Never for a case with steps (`hasSteps`) in a package that is not editable: such a case
  gets a version only in its own package, so the case designer makes that change; say so instead.
1. Make it: `modify-dynamic-case-as-new-version { environment-name, case-name | case-uid | entity-schema-name,
   package-name?, stages?, caption?, filter?, remove-filter?, confirm-stage-records?, confirm-value-rename?, dry-run?
   }` copies the family's ACTIVE member (another member is refused, naming the active one) into a new version that is
   NOT active, and applies `stages`, `caption` and `filter` to it with the shapes and the confirmations of
   `modify-dynamic-case` (`process-dynamic-case-change`; `filter` or `remove-filter`: MUST read `process-dynamic-case-filters` first; no `enabled`; running cases are not asked about: they keep their version). `stages` is the WHOLE
   new list, as there: send every stage, a renamed one too; a stage left out is removed. `package-name`
   left out: the case's own package when it is editable, otherwise refused `package-name-required`: ask the user which
   editable package. `dependencies-missing`: the package must depend on the packages the message names; add them only
   after the user's yes. `steps-not-supported`: a version copies the source's steps as they are, so steps or
   `transitionType` sent with it are refused: change them on the version afterwards (`modify-dynamic-case` by its
   `case-name`, or the case designer); and a case with steps gets a version only in its own package (or in the case
   designer). `stage-is-condition-target` (a step condition moves records to a stage the version removes): keep the
   stage in the version, then change the version in place with `modify-dynamic-case` by its `case-name`: for a Perform
   task, send that step's `conditions` without that stage together with the stages without it; for another step,
   remove the step or change its conditions in the case designer on the version, then remove the stage. First `dry-run: true` (`planned`): show the user `diff`, the notes and the warnings, write only
   after their yes. NOT idempotent: every write makes one more version; no case tool or Case-library action deletes one (an inactive version goes as a case does, `process-dynamic-case` undo steps 1-2, after the user's yes); it is
   saved inactive, so until a switch the new records the active case's filter fits (every new record when it has
   none) keep starting the active case while the family is enabled (a confirmed value rename has acted already,
   below), and a later change to it is `modify-dynamic-case` by its `case-name`, in place (no further
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
   to remove it: undo steps 1-2 of `process-dynamic-case`, after the user's yes; `failed-leftover` WITHOUT a `version`: nothing was saved, but a row or
   a value rename could not be undone - relay `leftovers` (a value left renamed: offer to rename it back); `unknown`:
   the reply was lost. After `failed-leftover`, `unknown` or no reply at all (a timeout), describe the family before any retry: a retry makes
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
   unclear. Asked for the switch ("switch orders to it")? That is the yes: notes only - switch; with the
   result, relay every note sentence about the user's records as written, none dropped or merged (a tool call in a
   note is for you: offer it as the user's next step in plain words); warnings - show them, wait for the yes. Not asked: step 3. `unchanged`: it is active
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

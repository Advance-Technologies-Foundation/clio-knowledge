clio MCP process-dynamic-case-change guide - change an existing dynamic case, and several cases on one object split by a filter

Scope
Reads and changes an existing dynamic case (DCM) with `describe-dynamic-case` and `modify-dynamic-case`, and splits
an object's records between several cases by one lookup column (for example B2B and B2C orders with stages of their
own). Building a case, the bar, the refusals of `create-dynamic-case` and the undo: `process-dynamic-case`.

== Change an existing case (clio <CLIO-TBD>+) ==
1. `describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only): the stages
   in order with `group`, `successful`, `isFinal`, `hasSteps`, `runningInstances`, and `enabled`. Several cases
   on the object (`case-ambiguous`): name one with `case-name`.
2. `modify-dynamic-case { environment-name, case-name | case-uid | entity-schema-name, stages?, caption?, filter?,
   remove-filter?, enabled?, confirm-running-instances?, dry-run }` writes. `stages` is the WHOLE new list: a stage
   left out is REMOVED, a value not yet a stage is ADDED (it must be in the lookup: add it with the data tools
   first, after asking). Fields an item leaves out keep their values; `group: null` ungroups; `color` (`#RRGGBB`)
   recolours it; `caption` renames the stage in the case only, never the value every record shares. First
   `dry-run: true`: show the user `diff` and the notes, write only after their yes. `unchanged` means nothing to do.
3. While cases run (`runningInstances.total` > 0) every write is refused `running-instances-confirmation-required`:
   tell the user how many run and in which stages; pass `confirm-running-instances: true` only after they say yes,
   NEVER on your own. Running cases continue, possibly on the stages as they were.
4. `enabled: false` / `true` disables or re-enables the case and goes ALONE (`invalid-request` otherwise). Disabled:
   nothing is deleted, running cases continue, new records start no case. Re-enabling next to another enabled case
   that would start for the same records: `case-exists` or `filter-overlap`.
Refusals: `stage-in-use` (cases sit in a stage being removed: move them first), `stage-has-steps` (it has steps or
a step condition targets it: the case designer), `package-read-only` (a new version is not supported yet).
`failed-restored`: the save did not take, the case is as before; `failed-leftover`: check it in the designer.
No compile or restart; F5.

== Several cases on one object (clio <CLIO-TBD>+) ==
- The platform starts EVERY enabled case whose filter matches a new record. One object's cases are split by ONE
  lookup column, the filter column (never the stage column): each case takes some of its values
  (`filter: {column, values: [Id or exact display value]}`) or its empty value (`filter: {column, empty: true}`),
  and no two cases share a value. Each case has its own stages: `stages` takes any 2 to 20 values of the stage
  lookup, in order.
- Describe first: `describe-dynamic-case { entity-schema-name }` shows each case's `filter` and the settings row's
  `filterColumnName`; its notes name the values no case is for. Answer "which stages do B2B orders have" from it.
- The object has ONE case without a filter (the usual start), so a filtered `create-dynamic-case` is refused
  `unfiltered-case-exists`. Offer the case designer's two ways, each with its dry run, and let the user choose:
  (1) keep the existing stages for some values: `modify-dynamic-case { filter }` on that case, then
  `create-dynamic-case { filter, stages }` for the others; nothing is disabled. (2) turn the existing stages off:
  `create-dynamic-case { filter, stages, deactivate-unfiltered: true }` disables that case; records going through it
  lose it at their next status change. Pass `deactivate-unfiltered` ONLY after the user chose (2), never on your own.
- Refusals: `filter-overlap` (another enabled case has one of these values, or the empty value: change that case
  instead, one record never starts two cases); `filter-required` (the cases are split by a column, so the case
  needs a filter; removing the last filter column is not supported yet); `settings-conflict` (they are split by
  another column: not supported yet); `filter-column-invalid` (not a lookup column of the object, or the stage
  column); `filter-value-unknown` / `filter-value-ambiguous` (as for stages).
- The first filter sets the filter column on the object's case settings (`settings.filterColumnSet`): from then on
  every case of the object needs one. A call with a filter needs the right to edit `SysDcmSettings` records too
  (`rights-missing` otherwise).
- A value no enabled case is for (a note names it): its records start no case and show no stage bar. There is no
  catch-all case; a type that should have no stages gets no case: create nothing and say so.
- A filter decides only which NEW records start a case: running cases continue, and an existing record without a
  case gets the matching one at its next change of the stage or filter column. A record whose filter value changes
  keeps its case. The record page is not changed: the bar switches to the matching case only when its
  `askUserToChangeSchema` is true; offer that page change, do not make it unasked.

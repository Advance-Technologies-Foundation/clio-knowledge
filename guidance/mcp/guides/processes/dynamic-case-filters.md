clio MCP process-dynamic-case-filters guide - several dynamic cases on one object split by a filter

Scope
Split out of `process-dynamic-case-change`, which owns describing, changing, disabling and versioning a case and
every confirmation; read it first. This article owns the filter rules of `modify-dynamic-case` and
`create-dynamic-case` (several cases on one object split by one lookup column, `filter`, `remove-filter`,
`deactivate-unfiltered`, the filter refusals and their remedies).

== Several cases on one object ==
- Needs clio 8.1.0.154 or later. If the `modify-dynamic-case` contract (`get-tool-contract`) does not list `filter`,
  the clio serving you is older and has no `filter`, `remove-filter` or `deactivate-unfiltered`: do not send them;
  tell the user to update clio, or to split the cases in the case designer.
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

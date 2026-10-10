clio MCP process-data-source-filters guide — the `filter` that decides WHICH records an element acts on

Part of the process guide set. A build starts at `process-digest`; open `process-modeling` only when
the card or this article sends you there.
This article is the authoritative owner of the data source `filter`: its shape, the column dot-path, the
comparison set, every right-hand value source, the COMPLETE relative-date macro vocabulary, the `datePart`
left-hand modifier, the signal-start restriction, and how a filter is set, cleared and read back.
Split out of `process-data-elements` because that article had no response-budget headroom left. The
three ELEMENTS that carry a filter stay there — the record trigger, Read data and Modify data — and this
is the one contract all three share, which is why it reads as its own subject.

== Data source filters (signalStart trigger condition / readData + changeData record filter) ==
- A `filter` declares, high-level, WHICH records a filtered element acts on. The server serializes it to
  the platform Terrasoft.FilterGroup — you NEVER hand-write the escaped filter JSON.
- Usable today on a `signalStart` (restrict the record trigger), on a `readData` element (restrict which
  records the read selects from), on a `changeData` element (restrict which records are updated —
  effectively mandatory there) and on a `changeAccessRights` element (which records get or lose
  permissions — MANDATORY in effect too: with NO filter the runtime acts on EVERY record of the
  object, and `process-access-rights` owns that hazard and the three filter states). Shape:
    "filter": {
      "object": "<EntityName>",        // root object. Defaults to the signal entity on a signalStart ONLY;
                                 // on readData / changeData / changeAccessRights it is REQUIRED
                                 // and a filter without it is refused at build
      "logicalOperation": "and",       // "and" (default) | "or"
      "conditions": [
        { "column": "UsrName",      "comparison": "equal", "value": "Start" },
        { "column": "Account.Code", "comparison": "equal", "value": "1" }   // dot-path traverses a lookup
      ],
      "groups": [                       // optional nested groups, each with its own logicalOperation
        { "logicalOperation": "or", "conditions": [ /* ... */ ] }
      ]
    }
- `column` is the entity COLUMN name (e.g. `UsrName`, not the caption "Name") and may be a dot-path
  through lookups (`Account.Code`, `Account.Owner.Name`); the server resolves the column type from the
  object's schema (so you don't supply types).
- `comparison`: equal (default) | notEqual | greater | greaterOrEqual | less | lessOrEqual | contains |
  notContains | startWith | notStartWith | endWith | notEndWith | isNull | isNotNull.
- The right-hand value of a condition is exactly ONE of: `value` (a constant as a string — the server
  types it by the column; for a Date/DateTime/Time column pass ISO-8601, e.g. `2026-05-01` or
  `2026-05-01T12:00:00Z`), `processParameter` (a process parameter by name), `elementParameter`
  ({ elementName, parameter, column? } — another element's output; the parameter must EXIST on that element —
  a `readData` element exposes only `ResultEntity`, so `{ "elementName": "ReadNewestContact", "parameter": "Id" }`
  is refused; compare against ONE column of the read record with `column`:
  `{ "elementName": "ReadNewestContact", "parameter": "ResultEntity", "column": "Id" }`, see
  `process-data-elements`), `expression` (the BARE meta path of such a reference - see the `expression`
  bullet after the macro vocabulary), or
  `macro` (a
  relative-date / system macro — the complete set is in the next bullet). isNull/isNotNull take none.
- `macro` vocabulary (COMPLETE set — an unknown name is rejected at BUILD, validated against the platform
  macro catalog, never silently accepted): **relative periods** `Yesterday` | `Today` | `Tomorrow`, plus
  `Previous`/`Current`/`Next` for each of `Week` | `Month` | `Quarter` | `HalfYear` | `Year` | `Hour`
  (so `CurrentHalfYear`, `NextWeek`, `PreviousQuarter`, `CurrentHour`, … are ALL valid); **argument macros**
  (require an integer `macroArgument`) `NextNDays` | `PreviousNDays` | `NextNHours` | `PreviousNHours` |
  `NextNDaysOfYear` | `PreviousNDaysOfYear` | `DayOfYearTodayPlusDaysOffset`; **recurring "every year"**
  `DayOfYearToday` (the ONLY DayOfYear macro that takes NO argument); **system / lookup** `CurrentUser` |
  `CurrentUserContact`.
- `expression` is another SPELLING of a `processParameter` / `elementParameter` (+ `column`) reference, never
  a value of its own: the BARE meta path - `[IsOwnerSchema:false].[IsSchema:false].[Parameter:{uid}]`, or
  `...[Element:{uid}].[Parameter:{uid}]`, optionally `.[EntityColumn:{uid}]` - exactly as
  `describe-business-process` reports it. Unlike a formula, a flow condition or a mapping value, a filter
  NEVER evaluates a `[#…#]` wrapper: a wrapped parameter reference fails the element at run time, and a
  wrapped column reference matches NO record with no error (both measured on a stand, 2026-10-06).
  CrtProcessBuilder 1.6.6.74+ refuses at build a wrapped value, any other spelling (no `.` before
  `[EntityColumn:…]`, the prefix-less short form, a repeated segment, extra text), an element / parameter /
  column the process does not have, and a column of a process parameter. When it can tell which item the
  value means, the refusal names that item's canonical token; otherwise it names the problem, or points you to
  `describe-business-process` for the right token.
  That token is only the right SPELLING: the checks of `elementParameter` + `column` (a record read in
  first-record mode, a column it loads, a compatible type) still apply when you resend it. Below 1.6.6.74
  those values save green - only a column the record's object does not have is refused, by the platform's
  own validation - and a wrapped, dangling or dot-less one then fails, or acts on no record, at run time,
  while the prefix-less short form does resolve. So prefer the structured sources, which build the token.
  When the user asks for a `[#…#]`-wrapped or otherwise misspelled filter value, say that a filter does not
  accept that form and which bare token you send instead (or already have) - never report it as the same value.
- SIGNAL-START RESTRICTION (important): on a `signalStart` filter the right-hand side may ONLY be a constant
  `value`, a `macro`, or isNull/isNotNull (`datePart` is a LEFT-hand modifier, never a source) — NOT `processParameter` / `elementParameter` /
  `expression`. The signal is evaluated to decide WHICH records start the process, BEFORE any process
  instance exists, so a parameter / element output / meta-path reference has no value yet. The server
  REJECTS a parameter reference on a signal filter (the visual designer likewise hides the "select
  parameter" option for signal starts). Parameter references ARE valid on a data-operation element filter —
  the element runs inside a live process instance — and are end-to-end buildable on a `readData` element
  (e.g. filter the read by a process parameter's value), on a `changeData` element, where a filter is
  effectively MANDATORY (`process-data-elements` owns that rule and states why), and on a `deleteData`
  element, where it is mandatory for the same reason and decides what gets destroyed; on an `addData`
  element the filter is over the SELECTION object and applies in `selection` mode only (see below).
- `datePart` (optional, LEFT-hand modifier — NOT a right-hand source): extract a calendar/clock part from a
  Date/DateTime `column` and compare that part instead of the whole date. `Year` | `Month` | `Day` |
  `Week` | `Weekday` | `Hour` extract an INTEGER — pair with an integer `value`; a `datePart` WITH a
  `macro` is refused outright (a signalStart narrows the right side further — see above):
  `{ "column": "CreatedOn", "datePart": "Year", "comparison": "equal", "value": "2026" }` reads
  `Year(CreatedOn) = 2026`. `HourMinute` is the exception — it extracts the TIME-OF-DAY and compares it to a
  `value` in `HH:mm[:ss]` form: `{ "column": "CreatedOn", "datePart": "HourMinute", "comparison": "equal",
  "value": "14:30" }` reads `HourMinute(CreatedOn) = 14:30`. Combines with any comparison (`greaterOrEqual`,
  …); it modifies the left side, so it is independent of the right-hand source choice (but do not use it with
  a `macro`).
- Groups nest to any depth: A AND (B OR C) = conditions:[A] + groups:[{ "logicalOperation":"or",
  conditions:[B, C] }].
- A `filter` on a `readData` element is end-to-end usable (pair it with the element's `readData` block —
  see `process-read-data`), and on a `changeData` element it is
  effectively MANDATORY — the runtime refuses to update with an empty filter (see the "Modify data
  element" section of `process-data-elements`). On a `deleteData` element it is mandatory for the same
  reason and carries the added weight that whatever it selects is DELETED, irreversibly, on every run —
  see `process-delete-data` for the confirmation duty that goes
  with writing one. On an `addData` element the `filter` is over the SELECTION object and matters in
  `selection` mode only, where it decides which records a new one is added for; in `one` mode it is
  inert. `process-add-data` owns that rule and the warnings a build raises for either mismatch.
- On an EXISTING process, set/clear a filter via `modify-business-process` ops `setFilter`
  ({ op:"setFilter", elementName, filter }) and `clearFilter` ({ op:"clearFilter", elementName }).
  MUST: `setFilter` REPLACES the element's whole filter (there is no add-one-condition op), so to add a
  condition, read the current filter back first (below) and send the complete new filter. Sending only
  the condition you care about silently drops every other one, which WIDENS the records the element acts
  on -- on a `changeData` element that is a bulk update on live records.
- `describe-business-process` reads a filter back: an element carries a decoded `filter` (the same
  object / logicalOperation / conditions / groups shape) when it has one, so you can inspect it or
  round-trip it into a `setFilter`. A parameter reference comes back as its BARE meta-path `expression`,
  which re-applies as read while the element it names still passes the checks above - send it exactly as
  read, never wrapped in `[#…#]`. An `expression` that reads back in another form was stored before the
  build checked it: a WRAPPED one never resolved at run time, a differently spelled one (the prefix-less
  short form, say) may have. Neither is accepted any more, and since `setFilter` re-sends the whole filter,
  that one condition refuses the edit - replace it with the bare form (or a structured source) in the same
  `setFilter`. MUST: repairing a condition that never resolved changes what the element does - it failed, or
  acted on no record, and from then on it selects records. Tell the user, and on a `changeData` /
  `deleteData` / `changeAccessRights` element, or an `addData` element in `selection` mode, get their
  confirmation before you send it (for `deleteData`, with the record count `process-delete-data` requires). When you cannot tell which item a legacy value meant
  (a repeated segment, say), ask the user rather than pick one.
  A lookup value reads back as the raw id in `value` plus its resolved caption in `displayValue` (so
  `UsrStage` shows `Approved`, not a bare GUID); `displayValue` is read-only — omit it on `setFilter`.
- LEGACY `isNotNull` (CrtProcessBuilder earlier than 1.6.6.81): those builds stored an is-null condition
  without the platform's own `isNull` flag, which the runtime reads instead of the comparison and which
  defaults to null. Every `isNotNull` they wrote therefore runs as IS NULL and selects the OPPOSITE records
  (measured on a .NET Framework / MSSQL stand, October 2026); `isNull` was unaffected. 1.6.6.81 and later write the flag. On 1.6.6.81 or
  later such an element still reads back as `isNotNull` but with `filterDecodedCompletely: false`;
  re-sending the filter with `setFilter` rewrites the condition (follows from `setFilter` replacing the
  whole filter; not tried on a legacy process). The repair flips which records the element acts on: tell the
  user and get confirmation exactly as the repair MUST above requires.

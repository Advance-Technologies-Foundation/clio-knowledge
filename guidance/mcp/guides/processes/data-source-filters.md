clio MCP process-data-source-filters guide — the `filter` that decides WHICH records an element acts on

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
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
  records the read selects from) and on a `changeData` element (restrict which records are updated —
  effectively mandatory there). Shape:
    "filter": {
      "object": "<EntityName>",        // root object; defaults to the signal entity if omitted
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
  ({ elementName, parameter } — another element's output; the parameter must EXIST on that element — a
  `readData` element exposes only `ResultEntity`, so `{ "elementName": "ReadNewestContact", "parameter": "Id" }` is
  refused, see the readData LIMITATION in `process-data-elements`), `expression` (a raw token), or
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
- SIGNAL-START RESTRICTION (important): on a `signalStart` filter the right-hand side may ONLY be a constant
  `value`, a `macro`, or isNull/isNotNull (`datePart` is a LEFT-hand modifier, never a source) — NOT `processParameter` / `elementParameter` /
  `expression`. The signal is evaluated to decide WHICH records start the process, BEFORE any process
  instance exists, so a parameter / element output / meta-path reference has no value yet. The server
  REJECTS a parameter reference on a signal filter (the visual designer likewise hides the "select
  parameter" option for signal starts). Parameter references ARE valid on a data-operation element filter —
  the element runs inside a live process instance — and are end-to-end buildable on a `readData` element
  (e.g. filter the read by a process parameter's value) and on a `changeData` element, where the filter is
  effectively MANDATORY (the runtime refuses to update with an empty one); on Add/Delete data they serialize
  but the task itself is not buildable yet (see below; `process-element-catalog` owns which elements are
  buildable and is the article to re-read when that changes).
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
  see the "Read data element" section of `process-data-elements`), and on a `changeData` element it is
  effectively MANDATORY — the runtime refuses to update with an empty filter (see the "Modify data
  element" section of `process-data-elements`). A `filter` on an
  Add/Delete-data task is serialized too, but those tasks' target object / values are not buildable yet
  (`process-element-catalog` owns that, and this sentence is only true while it says so), so THEIR
  filters are not end-to-end usable in this increment.
- On an EXISTING process, set/clear a filter via `modify-business-process` ops `setFilter`
  ({ op:"setFilter", elementName, filter }) and `clearFilter` ({ op:"clearFilter", elementName }).
  `setFilter` REPLACES the element's whole filter (there is no add-one-condition op); to add a condition,
  read the current filter first (below) and send the complete new filter.
- `describe-business-process` reads a filter back: an element carries a decoded `filter` (the same
  object / logicalOperation / conditions / groups shape) when it has one, so you can inspect it or
  round-trip it into a `setFilter`. A parameter reference comes back as its raw meta-path `expression`.
  A lookup value reads back as the raw id in `value` plus its resolved caption in `displayValue` (so
  `UsrStage` shows `Approved`, not a bare GUID); `displayValue` is read-only — omit it on `setFilter`.

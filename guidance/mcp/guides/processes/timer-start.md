clio MCP process-timer-start guide - the Timer start element (timerStart)

Scope
This guide owns the `timer` block of the Timer start element (`timerStart`, the designer's Start timer), the
`setTimer` operation, and the confirmation you owe the user before a save that schedules runs. `process-modeling`
owns the build recipe and the other edit ops; `process-version-writes` owns making a version active;
`process-sub-process` owns calling a process from another; `process-parameters` owns setting a parameter on a
start's branch. A timer start inside an event sub-process is not handled by these tools: it is neither built,
described nor found by `setTimer`.
Gate: use `timerStart` and `setTimer` only when the FULL `create-business-process` contract (`get-tool-contract`
with `tool-names: ["create-business-process"]`, `detail: "full"`) names `timerStart` - that clio refuses an older
package up front. Otherwise this clio does not carry it: build no timer start, and tell the user that a clio update
adds it. Always read the element back.

== When to use it ==
- A timer start runs the WHOLE process on a schedule, once or repeatedly: a nightly clean-up, a sync, a reminder
  sweep. It is not a per-record delay or a wait inside a run (an intermediate timer: read-only, see
  `process-element-catalog`).
- Prefer ONE timer that reads the due records and handles each (`process-sub-process-when`, D1) to many processes
  or schedules, one per record or small job: every timer is a job on the environment's shared scheduler threads.
- Schedule no more often than the work needs. The block builds nothing finer than every minute, and the platform sets
  no minimum interval and no cap on runs: each fire is a full process run, competing with every other timer and job.

== What a run is ==
- It runs as the System operations user (the `SystemUser` system setting, Supervisor by default), the author of every
  record it writes, with SYSTEM ACCESS: object, column, record and operation permissions are not checked, whatever
  roles that user has. Not as the user who built it.
- It receives NO process parameters and no record Id: each parameter starts at its default value. Read what the run
  needs inside it.
- It runs only while the process is enabled and the version carrying the timer is the active one; a disabled process
  skips its fires, with only a log entry.
- A process whose only start is a timer cannot be started by hand (the platform refuses a manual start, so
  `run-process` and a page Run process button fail) or called as a sub-process. A Simple start next to it keeps
  both (on a new process add one only if the user wants that); a run through it is the current user's and may carry
  parameters, so it does not reproduce a scheduled run.

== The block ==
`timer {schedule, every?, at?, fromHour?, toHour?, daysOfWeek?, dayOfMonth?, weekday?, workday?, month?, cron?,
start?, end?, timeZone?, repeatOnMisfire?}`, on `create-business-process`, `addElement` and `setTimer`:
"elements": [{ "name": "NightlyCleanupTimeStart", "caption": "Nightly cleanup time", "type": "timerStart",
  "timer": { "schedule": "day", "at": "02:30", "timeZone": "FLE Standard Time" } }]
`schedule` is required on create and `addElement` (`setTimer`: below); `at` is `HH:mm` (default `00:00`); `every`
defaults to 1; a field the kind does not use is refused; nothing may fall after 2099 (a `start`, a first fire or a
`cron` year). The kinds:
- `once`: `"start": "2026-12-31T10:15"`, required and in the future; no `end`.
- `minute` / `hour`: `"every": 30, "fromHour": 9, "toHour": 18` - minutes 1..59 or hours 1..23, inside an hour window
  (0..23, BOTH hours inclusive, default 0..23; it may wrap, 22..6, computed but not run on a stand). The window keeps
  whole hours: 9..18 every 30 minutes also fires at 18:00 and 18:30.
- `day`: `"every": 2, "at": "03:30"` (1..31).
- `week`: `"daysOfWeek": ["mon", "wed", "fri"], "at": "07:00"` - non-empty, unique; no `every`.
- `month`: `"every": 2` (1..12) and exactly ONE of `"dayOfMonth": 15` (1..31 or `"last"`),
  `"weekday": { "position": "third", "day": "wed" }` (first..fourth or last), `"workday": "first"` (or `"last"`).
- `year`: `"month": 3` and ONE of `"dayOfMonth": 25` (1..31, no `"last"`) or a `weekday` (first..fifth, no last).
- `cron`: `"cron": "0 15 10 ? * MON-FRI"` - Quartz: 6 or 7 fields starting with seconds, which must be 0; day of
  week 1 = Sunday; exactly one of day-of-month and day-of-week is `?`. A 5-field Unix cron is refused, and so is a
  day-of-month `nW` above `28W`. Use it only for what the kinds cannot say, such as an offset step
  (`0 0 7 1 2/3 ? *`: every 3 months from February). The server builds each kind as the designer would, so the
  designer opens it on its own page; a `cron` opens as Other frequency.

== "Every N" is a cron step, not an interval ==
- `every` is a step counted from the start of its field and restarted there: minutes from :00 of each hour, hours
  from `fromHour` each day (9..18 every 5: 09:00 and 14:00; 22..6 every 3: 22:00, 01:00, 04:00), days from the 1st
  of each month, months from January. `minute` every 45 fires at :00 and :45 of every hour; `day` every 2 fires on
  the 1st, 3rd, 5th ... and again on the 1st (the 31st and the 1st are one day apart); `month` every 5 fires in
  January, June and November, then January. Only a step that divides its cycle (60 minutes, 24 hours, 12 months) is
  regular. Every step above 1 gets a notice naming the real cadence, but only after the save: work the cadence out
  yourself for the confirmation, and tell the user that cadence, not "every N".
- Every N WEEKS is not supported by the platform scheduler: `every` on `week` is refused, and so are its look-alikes,
  a `day` step of 7, 14, 21 or 28 and a `cron` whose day-of-month field is a step that is a multiple of 7
  (`0 0 8 */14 * ? *`): a day step restarts on the 1st, so 14 fires on the 1st, 15th and 29th. Do not build a
  substitute; tell the user and let them choose another schedule.
- A designer-built timer with such a day step is described as it is; a rebuild from that describe, and a `setTimer`
  that resends that step, are refused; a `setTimer` with another schedule group replaces it, and one without the
  schedule group keeps it.

== Dates, zone, misfire ==
- `start` and `end` are wall-clock times in the timer's zone, with no offset, `Z` or milliseconds
  (`2026-12-31T10:15`). On a recurring kind both are optional and bound the window; `end` must be later than `start`
  and than now, and the schedule must fire at least once at or before it (a February 30 or a past year is refused);
  a first fire more than a year away gets a notice. A `start`, `end` or `once` time in the zone's skipped
  spring-forward hour is refused; a time in the repeated autumn hour is stated in UTC in the notice.
- A past `start` on a recurring kind means "start now": a sent one that differs from the stored one is dropped AND
  clears the stored start, with a notice naming what was cleared; a stored one you do not send is kept. A timer that
  keeps a past `start` may run ONCE immediately when it is registered afresh - when the process is re-enabled, or
  when it had no job.
- `timeZone`: a Creatio time zone Code (Windows id: `FLE Standard Time`, `UTC`), case-insensitive; an IANA id
  (`Europe/Kyiv`) is refused. Omitted when a timer is created: your profile's zone, else the `DefaultTimeZone` system
  setting, else refused ("timeZone is required: neither your profile nor the DefaultTimeZone setting names one").
  When the user did not name a zone, ASK and send `timeZone`: the clio login may be a technical user whose zone is
  not theirs. A `setTimer` without `timeZone` keeps the stored zone; an old timer with none stored runs, and is
  described, as `UTC`.
- `repeatOnMisfire` (the designer's "Repeat on misfire") is stored for parity and changes nothing at run time: with
  either value, a fire missed while the scheduler was down runs once as soon as it is back, then the schedule
  continues. Tell the user that.

== Confirm before a save that schedules runs ==
- STOP AND CONFIRM FIRST. A timer runs on its own, unattended, with system access, on every fire. Before any action
  after which a timer that did not fire before, or now fires on a different schedule or zone, sits in the enabled,
  active version of a process, you MUST state the following and get an explicit yes BEFORE you call the tool: the
  schedule in words (a step's real cadence), the zone you will send as `timeZone`, the first run as your estimate
  (it may be immediate, above), that runs use the System operations user with system access (no permission checks),
  and which data the process writes or deletes. Then read the post-save notice and pass it on. If it differs from
  what the user approved (another zone or first run, an immediate run, NOT scheduled), tell the user at once and
  offer to revert (`setTimer` back, or remove the timer). An activation or an enable returns no timer notice: read
  `scheduled` from `describe-business-process mode=full` instead.
- That covers: a create with a `timerStart`; adding a `timerStart` to an existing process (next to the Simple start,
  or to a timer-only process); the Simple -> Timer replace; a `setTimer`, the re-save below included;
  `set-active-business-process-version` on a version that carries a timer (one added through
  `modify-business-process-as-new-version` fires only from then); enabling a disabled process that carries a timer.
- An edit that leaves an already scheduled timer unchanged (a caption, a label) needs no schedule confirmation; other
  confirm gates still apply: a change to what a scheduled run writes is an in-place edit of the active version
  (`process-modeling`), and `process-delete-data` keeps its own.

== A timer on an existing process ==
- By default ADD it: `addElement` a `timerStart` and `addFlow` from it to the first element, next to the existing
  start, in one request (the confirmation above applies). A Simple start then keeps the process runnable by hand and
  callable as a sub-process; a signal-started process has none. First check what the steps read: a parameter or a
  start's record Id is empty on a timer run, so set it on the timer branch (`process-parameters`) or ask the user.
- REPLACE the Simple start only when the user asks for a timer-only process, in one request (`removeElement` it,
  which removes its flows too, `addElement`, `addFlow`), and only after a `describe-business-process mode=full`, the
  caller check below and the user's confirmation: a timer-only process loses every manual start and every caller,
  and nothing in the save says so.
- Caller check, read-only:
  * Process callers: one `execute-esq` on `SysSubProcessInProcess` by the family root (`versionRootSchemaUId` from
    describe); any row is a caller (`ProcessUId` is the calling version, `ParentProcessUId` its family). The platform
    writes one set of rows per caller version, active or not. A refused read (it needs the "Access to process data
    tables" operation) is reported to the user, never taken as "no callers". The table holds process callers only:
    a case (DCM) step that runs the process, or code that starts it, is not in it - ask about those too:
clio-run { command: "execute-esq", args: { environment-name: "<env>", query: { rootSchemaName: "SysSubProcessInProcess", columns: { items: { ProcessUId: { expression: { expressionType: 0, columnPath: "ProcessUId" } }, ParentProcessUId: { expression: { expressionType: 0, columnPath: "ParentProcessUId" } } } }, filters: { filterType: 6, items: { r: { filterType: 1, comparisonType: 3, leftExpression: { expressionType: 0, columnPath: "ParentSubProcessUId" }, rightExpression: { expressionType: 2, parameter: { dataValueType: 0, value: "<versionRootSchemaUId>" } } } } } } } }
  * Manual starts - a page button, a classic page, a dashboard, an integration, a `run-process` call: no tool proves
    there are none. `VwProcessLib.AddToRunButton` of the process (`execute-esq`) shows a Run process menu entry, and
    the process's own app pages read with `get-page` for a `crt.RunBusinessProcessRequest` naming it show a button;
    both are partial checks, never proof of absence, so ASK the user whether anything starts the process by hand.

== setTimer ==
`{ "op": "setTimer", "elementName": "NightlyCleanupTimeStart", "timer": { "schedule": "day", "at": "04:00" } }` changes
an existing top-level timer start in place (UId, name and flows kept, so its scheduler job is updated):
- The schedule group is optional, and REPLACED when sent: any of `schedule`, `every`, `at`, `fromHour`, `toHour`,
  `daysOfWeek`, `dayOfMonth`, `weekday`, `workday`, `month`, `cron` rebuilds the whole schedule from what is sent,
  with `schedule` required and an omitted `at` back at `00:00`; nothing is merged with the stored one. To change only
  the zone or the dates, send no schedule field.
- `start`, `end`, `timeZone` and `repeatOnMisfire` change one by one; `start: null` / `end: null` clear. An empty
  `timer {}` re-saves the timer unchanged: that is the re-save this article asks for.
- A kind change clears `start` and `end` unless you send them; `minute` <-> `hour` is one kind and keeps them.
- The checks run on the timer as it will be: a `timeZone` change that makes a stored `once` instant past is refused.
- From describe: leave out an unchanged stored past `start`. A date you send (no `Z`, no milliseconds) equal to the
  stored one to the second counts as unchanged, and the stored text is kept as it is.
- Refused: a `timer` key in `setElement` (use `setTimer`); `setTimer` on any other element; `useBackgroundMode: true`
  (`false`, as describe reports it, passes) or a `filter` on a timer start.

== Read back and notices ==
- `describe-business-process` reports `buildType: "timerstart"` and the `timer` block, decoded, with read-back-only
  `storedCron`, `designerKind` and `scheduled`, ignored when sent back. A stored cron that does not fit its recurring
  kind comes back as `schedule: "cron"`, its kind in `designerKind` (`"empty"`: never configured); a `once` always
  reads as `once`, a stray cron only in `storedCron`. The block sent back to `addElement` rebuilds the same timer,
  except: a past recurring `start` is dropped, milliseconds and a `Z` are trimmed, an expired `end` or a past `once`
  is refused, a missing zone becomes `UTC`, a day step that is a multiple of 7 is refused, a kind named in
  `designerKind` comes back as Other frequency, and a never-configured timer is refused until a schedule is sent.
- `scheduled` gives the next and previous fire time in UTC, or why there is none: `processDisabled`;
  `notActiveVersion` (with the active version's next run); `staleTrigger` - the stored trigger differs from the
  timer, belongs to another version, or is paused or errored: re-save the active version; `noJob` - normal for a
  `once` whose time has passed or a recurring timer past its `end` (it has finished), otherwise re-save;
  `schedulerUnavailable`. A re-save registers the timer afresh, so a kept past `start` may run once at once.
- Every save that touches a timer adds a notice: "Timer `X` is scheduled: next run ... It runs as the System
  operations user and receives no process parameters", or "saved but is NOT scheduled" with the reason (the process
  is disabled; on a new version, "the active version keeps running its current schedule"). A failed scheduler read
  adds "could not read the scheduler" and does not change success. Tell the user.
- "the edit was written but the save did not finish (scheduler: ...)": the edit IS saved; do NOT resend the original
  operations. If the message says the scheduler refused the timer, fix the timer (confirming again) and re-save it
  with `setTimer`; if it says the scheduler itself failed, re-save later with `setTimer` sending the same values.

Evidence: designer saves of every kind, a builder edit and a timer run captured on Creatio 10.2 (.NET Framework) in
November 2026; .NET 8 is not measured. The run-as user and its system access, the parameters, when a job exists and
the misfire handling read from the Creatio core source of November 2026; fire times and misfire handling also
computed with the platform's Quartz 3.2.2 outside a stand. Not run on a stand: a missed fire, a wrapping hour window
and an `at` in a skipped or repeated hour.

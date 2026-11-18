clio MCP process-timer-start guide - the Timer start element (timerStart)

Scope
This guide owns the `timer` block of the Timer start element (`timerStart`, the designer's Start timer), the
`setTimer` operation, and the confirmation you owe the user before a save that schedules runs. `process-modeling`
owns the build recipe and the edit ops; `process-version-writes` owns making a version active; `process-sub-process`
owns calling a process from another. A timer start inside an event sub-process is not handled by these tools: it is
neither built, described nor found by `setTimer`.
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
- It runs as the System operations user (the `SystemUser` system setting, Supervisor by default): with that user's
  rights, and as the author of every record it writes. Not as the user who built it.
- It receives NO process parameters: each one starts at its default value. Read what the run needs inside it.
- It runs only while the process is enabled and the version carrying the timer is the active one; a disabled process
  skips its fires, with only a log entry.
- A process whose only start is a timer cannot be started by hand (the platform refuses a manual start, so
  `run-process` and a page Run process button fail) or called as a sub-process. To test one, keep or add a Simple start next to it, as most shipped timer
  processes do.

== The block ==
`timer {schedule, every?, at?, fromHour?, toHour?, daysOfWeek?, dayOfMonth?, weekday?, workday?, month?, cron?,
start?, end?, timeZone?, repeatOnMisfire?}`, on `create-business-process`, `addElement` and `setTimer`:
"elements": [{ "name": "NightlyCleanupTimeStart", "caption": "Nightly cleanup time", "type": "timerStart",
  "timer": { "schedule": "day", "at": "02:30", "timeZone": "FLE Standard Time" } }]
`schedule` is required; `at` is `HH:mm` (default `00:00`); `every` defaults to 1; a field the kind does not use is
refused. The kinds:
- `once`: `"start": "2026-12-31T10:15"`, required and in the future; no `end`.
- `minute` / `hour`: `"every": 30, "fromHour": 9, "toHour": 18` - minutes 1..59 or hours 1..23, inside an hour window
  (0..23, BOTH hours inclusive, default 0..23; it may wrap, 22..6). The window keeps whole hours: 9..18 every 30
  minutes also fires at 18:00 and 18:30.
- `day`: `"every": 2, "at": "03:30"` (1..31).
- `week`: `"daysOfWeek": ["mon", "wed", "fri"], "at": "07:00"` - non-empty, unique; no `every`.
- `month`: `"every": 2` (1..12) and exactly ONE of `"dayOfMonth": 15` (1..31 or `"last"`),
  `"weekday": { "position": "third", "day": "wed" }` (first..fourth or last), `"workday": "first"` (or `"last"`).
- `year`: `"month": 3` and ONE of `"dayOfMonth": 25` or a `weekday` (first..fifth, no last).
- `cron`: `"cron": "0 15 10 ? * MON-FRI"` - Quartz: 6 or 7 fields starting with seconds, which must be 0; day of
  week 1 = Sunday; exactly one of day-of-month and day-of-week is `?`. A 5-field Unix cron is refused. Use it only for
  what the kinds cannot say, such as an offset step (`0 0 7 1 2/3 ? *`: every 3 months from February). The server
  builds each kind as the designer would, so the designer opens it on its own page; a `cron` opens as Other frequency.

== "Every N" is a cron step, not an interval ==
- `every` on minute, hour, day and month restarts at each hour, day, month or year boundary. `minute` every 45 fires
  at :00 and :45 of every hour; `day` every 2 fires on the 1st, 3rd, 5th ... of each month and again on the 1st (the
  31st and the 1st are one day apart); `month` every 5 fires in January, June and November, then January. Only a
  step that divides its cycle (60 minutes, 24 hours, 12 months) is regular. Every step above 1 gets a notice naming
  the real cadence: tell the user that cadence, not "every N".
- Every N WEEKS is not supported by the platform scheduler: `every` on `week` is refused, and so are its look-alikes,
  a `day` step of 7, 14, 21 or 28 and a `cron` whose day-of-month field is a step that is a multiple of 7
  (`0 0 8 */14 * ? *`): a day step restarts on the 1st, so 14 fires on the 1st, 15th and 29th. Do not build a
  substitute; tell the user and let them choose another schedule.
- A designer-built timer with such a day step is described as it is; a rebuild from that describe, and a `setTimer`
  that sends the schedule group, are refused; a `setTimer` without the schedule group keeps it.

== Dates, zone, misfire ==
- `start` and `end` are wall-clock times in the timer's zone, with no offset (`2026-12-31T10:15`). On a recurring
  kind both are optional and bound the window; `end` must be later than `start` and than now, and the schedule must
  fire at least once before it (a February 30 or a past year is refused); a first fire more than a year away gets a
  notice. A `start`, `end` or `once` time in the zone's skipped spring-forward hour is refused; a time in the repeated
  autumn hour is stated in UTC in the notice.
- A past `start` on a recurring kind means "start now": a sent one that differs from the stored one is dropped AND
  clears the stored start, with a notice naming what was cleared; a stored one you do not send is kept. A timer that
  keeps a past `start` may run ONCE immediately when it is registered afresh - when the process is enabled, or when it
  had no job.
- `timeZone`: a Creatio time zone Code (Windows id: `FLE Standard Time`, `UTC`), case-insensitive; an IANA id
  (`Europe/Kyiv`) is refused. Omitted when a timer is created: your profile's zone, else the `DefaultTimeZone` system
  setting, else refused ("timeZone is required: neither your profile nor the DefaultTimeZone setting names one").
  The notice names the zone and where it came from. When the user did not name a zone, CONFIRM it: the clio login may
  be a technical user whose zone is not theirs. A `setTimer` without `timeZone` keeps the stored zone; an old timer
  with none stored runs, and is described, as `UTC`.
- `repeatOnMisfire` is the designer's "Repeat on misfire", stored for parity. Its run-time effect is not verified:
  promise the user nothing from it, either way.

== Confirm before a save that schedules runs ==
- STOP AND CONFIRM FIRST. A timer runs on its own, unattended, with the System operations user's rights, on every
  fire. Before any action after which a timer that did not fire before, or now fires on a different schedule or
  zone, sits in the enabled, active version of a process, you MUST state the following and get an explicit yes
  BEFORE you call the tool: the schedule in words (a step's real cadence), the zone and its source, the first run
  (computed as the notice would; it may be immediate, above), that runs use the System operations user's rights, and
  which data the process writes or deletes. Then read the post-save notice and pass it on.
- That covers: a create with a `timerStart`; adding a `timerStart` to an existing process (next to the Simple start,
  or to a timer-only process); the Simple -> Timer replace; a `setTimer`; `set-active-business-process-version` on a
  version that carries a timer (one added through `modify-business-process-as-new-version` fires only from then);
  enabling a disabled process that carries a timer.
- An edit that leaves an already scheduled timer unchanged (a caption, an added script task) needs no schedule
  confirmation; other confirm gates, such as the one of `process-delete-data`, still apply.

== A timer on an existing process ==
- By default ADD it: `addElement` a `timerStart` and `addFlow` from it to the first element, next to the existing
  Simple start, in one request (the confirmation above applies). The Simple start keeps the process runnable by hand
  and callable as a sub-process.
- REPLACE the Simple start (`removeElement` it, `addElement`, `addFlow`) only after a `describe-business-process
  mode=full`, the caller check below and the user's confirmation: a timer-only process loses every manual start and
  every caller, and nothing in the save says so.
- Caller check, read-only:
  * Sub-process callers: one `execute-esq` on `SysSubProcessInProcess` by the family root (`versionRootSchemaUId`
    from describe); any row is a caller (`ProcessUId` is the calling version, `ParentProcessUId` its family). The
    platform writes one set of rows per caller version, active or not. A refused read (it needs the "Access to
    process data tables" operation) is reported to the user, never taken as "no callers":
clio-run { command: "execute-esq", args: { environment-name: "<env>", query: { rootSchemaName: "SysSubProcessInProcess", columns: { items: { ProcessUId: { expression: { expressionType: 0, columnPath: "ProcessUId" } }, ParentProcessUId: { expression: { expressionType: 0, columnPath: "ParentProcessUId" } } } }, filters: { filterType: 6, items: { r: { filterType: 1, comparisonType: 3, leftExpression: { expressionType: 0, columnPath: "ParentSubProcessUId" }, rightExpression: { expressionType: 2, parameter: { dataValueType: 0, value: "<versionRootSchemaUId>" } } } } } } } }
  * Manual starts - a page button, a classic page, a dashboard, an integration, a `run-process` call: no tool can
    search for them, so ASK the user whether anything starts the process by hand. Reading the pages of the process's
    own app with `get-page` for a `crt.RunBusinessProcessRequest` naming it is a partial check, never proof of
    absence.

== setTimer ==
`{ "op": "setTimer", "elementName": "NightlyCleanupTimeStart", "timer": { "schedule": "day", "at": "04:00" } }` changes
an existing top-level timer start in place (UId, name and flows kept, so its scheduler job is updated):
- The schedule group is REPLACED: any of `schedule`, `every`, `at`, `fromHour`, `toHour`, `daysOfWeek`, `dayOfMonth`,
  `weekday`, `workday`, `month`, `cron` rebuilds the whole schedule from what is sent, with `schedule` required;
  nothing is merged with the stored one.
- `start`, `end`, `timeZone` and `repeatOnMisfire` change one by one; `start: null` / `end: null` clear.
- A kind change clears `start` and `end` unless you send them; `minute` <-> `hour` is one kind and keeps them.
- The checks run on the timer as it will be: a `timeZone` change that makes a stored `once` instant past is refused.
- From describe: leave out an unchanged stored past `start`. A date sent equal to the stored one to the second (a `Z`
  and milliseconds aside) counts as unchanged and is kept as stored.
- Refused: `setElement` with a `timer` key ("use setTimer"); `setTimer` on any other element; `useBackgroundMode` or
  a `filter` on a timer start.

== Read back and notices ==
- `describe-business-process` reports `buildType: "timerstart"` and the `timer` block, decoded, with read-back-only
  `storedCron`, `designerKind` and `scheduled`, ignored when sent back. A stored cron that does not fit its kind comes
  back as `schedule: "cron"`, its kind in `designerKind`. The block sent back to `addElement` rebuilds the same timer,
  except: a past recurring `start` is dropped, milliseconds and a `Z` are trimmed, an expired `end` or a past `once`
  is refused, a missing zone becomes `UTC`, and a day step that is a multiple of 7 is refused.
- `scheduled` gives the next and previous fire time in UTC, or why there is none: `processDisabled`;
  `notActiveVersion` (with the active version's next run); `staleTrigger` - the stored trigger differs from the timer
  or belongs to another version: re-save the active version; `noJob`; `schedulerUnavailable`.
- Every save that touches a timer adds a notice: "Timer `X` is scheduled: next run ... It runs as the System
  operations user and receives no process parameters", or "saved but is NOT scheduled" with the reason (the process
  is disabled; on a new version, "the active version keeps running its current schedule"). A failed scheduler read
  adds "could not read the scheduler" and does not change success. Tell the user.
- "the edit was written but the save did not finish (scheduler: ...)": the edit IS saved. Fix the timer and re-save
  it with `setTimer`; do NOT retry the same edit.

Evidence: designer saves of every kind, a builder edit and a timer run captured on Creatio 10.2 (.NET Framework) in
November 2026; the run-as user, the parameters and when a job exists read from the Creatio core source. Not
verified: the misfire effect, a wrapping hour window and an `at` in a skipped or repeated hour.

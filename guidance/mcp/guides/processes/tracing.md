clio MCP process-tracing guide — switch process tracing on, read the trace, switch it off

Scope
Use this guide when a business process run failed or produced a wrong result and you need to see the VALUES its
elements received and returned, and whenever a request asks to switch process tracing on or off. It owns the
`isTracing` field of `create-business-process`, the `setTracing` operation of `modify-business-process` and
`modify-business-process-as-new-version`, and the `tracing` block of `describe-business-process`. The process graph
itself belongs to `process-modeling`; which version of a process runs belongs to `process-versions`.
Needs CrtProcessBuilder 1.6.6.92 or later on the environment, and a clio whose `create-business-process` and
`modify-business-process` contracts name `isTracing` and `setTracing` (check `get-tool-contract`) - that clio refuses
an older package up front. An older pair accepts `isTracing` and drops it in silence, so after ANY switch confirm it
with `describe-business-process`: the `tracing` block is the proof, never the success of the write.
Switching it needs the `CanManageSolution` right on top of process design (`CanManageProcessDesign`) - the same
right the product's own checkbox asks for. Without it the call is refused before anything is saved; say so to the
user rather than looking for another route.

What tracing is, and what you get without it
Every run of a clio-built process already writes the ELEMENT LOG: a `SysProcessElementLog` row per executed
element (Caption, Status, StartDate, CompleteDate, SysProcess; the start event wrote none in the measured run). That answers WHICH elements ran and where a run
stopped, and it needs no tracing. Read it first.
Tracing is the platform's "Enable tracing" checkbox on the Process Library record page. While it is on, every run
also writes two `SysPrcElementTraceLog` rows per executed ACTIVITY - a task such as Read data; events write none -
`TraceEvent` 0 when it starts and 1 when it completes - and each row carries two JSON arrays: `ElementData`, every parameter value of that element, and
`ProcessData`, every process parameter value at that moment. Each array item is
{Parameter:{UId, Name, Caption, Direction}, Value}. That answers WHAT the element saw and what it produced.
Measured on a .NET Framework stand on 2026-10-09 - CrtProcessBuilder 1.6.6.91 with tracing switched through the
platform's own request, then 1.6.6.92 switching it through clio: tracing off - two element-log rows and
zero trace rows for a Start -> Read data -> End process; tracing on - the same two element-log rows plus two trace
rows of about 5 KB each for the Read data element, its ResultEntity holding the record it read.

Decide first: ask, and keep it short
Tracing costs two things a success response does not show, so tell the user before you switch it on:
- DATA: the trace stores every parameter value, including data read from records. That can be personal data, and
  it is kept in the process log beside the run. Switching tracing off stops NEW rows only: the rows already written
  stay in the log.
- VOLUME: two rows per executed activity per run. A process started by a frequent record signal, or one that loops
  or calls a multi-instance sub-process, multiplies that.
Switch it on to diagnose a specific run, never as a default, and switch it off once that run has been traced.

How to switch it
- New process: top-level `isTracing: true` in the `create-business-process` descriptor. Omitted or false leaves it
  off, which is where every new process starts.
- Existing process: `modify-business-process` with {"op":"setTracing","enabled":true} or {"enabled":false}.
  `enabled` is REQUIRED - a missing one is refused, because off is a real request. A batch made ONLY of
  `setTracing` does not save the process: no re-layout, no new modified stamp, nothing for the pre-save gate to
  refuse. Combined with other operations it is written AFTER the edit saves, so a refused edit leaves tracing
  unchanged. One `setTracing` per request; `enabled` on any other operation is refused.
- Switching tracing is NOT a graph edit: send `setTracing` ALONE to `modify-business-process` - naming any member
  of the family reaches the root - even when the user chose to take edits as new versions
  (`process-version-writes`). `modify-business-process-as-new-version` REFUSES a batch made only of `setTracing`,
  because it would save a version that can never be deleted only to flip a switch; nothing is created. Beside real
  edits it is accepted there, and then the switch applies to the whole family at once - the version that runs now
  included - while the new version's edits wait until it is made actual.
- If the switch fails after an edit saved, the error says the edit WAS saved: send `setTracing` again ON ITS OWN,
  never the whole batch.

ONE switch per version family
The switch lives on the version family's ROOT, where the Process Library page writes it, and the runtime reads the
root for every version - so switching it for any version switches it for all of them (measured: a version run with
only the root switched on was traced). Switching OFF also clears the own switch of any version that carries one, which
another route can set. Per the platform source, a sub-process whose caller is traced is traced too.
The warning says the switch went to the root when the process you named is a version. You cannot trace one version
alone through clio.

It switches itself off
The platform switches tracing off by itself a number of days after it was switched on: system setting
`ProcessParameterTracingDisableTimeoutDays` (0 = never; it was 5 on the measured stand - read the date, do not
assume the number). The warning on a write that switches it on names the date, and `describe-business-process`
reports `tracing: {enabled: true, turnOffDate: "yyyy-MM-dd"}` - the last traced day - ONLY while it is on; an absent
`tracing` key means runs are not traced (or, rarely, that the server could not read the switch; it logs that). Switching on a process that is already traced writes nothing and does NOT
move the date (the warning says so); to restart the countdown, switch it off and on again.

Reading a trace
1. Run the process (`run-process`, or the trigger it normally runs on) and keep the `processId` it returns.
2. Element log: `execute-esq` on `SysProcessElementLog`, filter `SysProcess` = processId, columns Caption,
   Status.Name, StartDate, CompleteDate. A row without CompleteDate is an element still waiting (a human step, an
   approval), not a hang.
3. Values: `execute-esq` on `SysPrcElementTraceLog`, filter `SysProcessElementLog.SysProcess` = processId, columns
   TraceEvent, SysProcessElementLog.Caption, ElementData, ProcessData. Read only the element in question (filter on
   `SysProcessElementLog.Caption`), with those columns selected explicitly: the JSON is long, `execute-esq` fails a
   response over its size budget, and every value it returns is record data you then hold. Report to the user the
   values that explain the run, not the whole payload.
4. Compare ElementData at TraceEvent 0 and 1 for the element in question: the inputs it was given, and the
   outputs it produced.
Then switch tracing off with {"op":"setTracing","enabled":false}. Off stays stored as a `False` value on the root,
which is the same state the platform's own switch-off leaves; the trace rows of the runs you made stay in the log.

clio MCP process-perform-task-details guide — why Perform task is not an approval, which Call category row it needs, and the describe and parameter-count evidence

A details article of the process guide set, reached through `process-perform-task`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the REASONING and EVIDENCE behind several Perform task
(ActivityUserTask) rules: what the Approval element does that Perform task does not, why the Task-typed
"Call" category row is the one the element needs, how many parameters describe shows against how many the
element carries, why the full parameter set varies by environment, and how many shipped conditional flows
the activity-result rule affects. Split out of `process-perform-task`, which keeps the element, its
parameters, the category ids, the two refusal texts and their remedy, and every rule, refusal and
confirmation duty; read that first.

== Element: Perform task — not for approvals ==

`process-perform-task` says do not use this element for approvals. Why: Creatio has a dedicated Approval element that creates an Approval record (not an
Activity), emails approver and author, supports delegation, and branches on the verdict. Perform task has no
approved/rejected semantics. The Approval element is owned by `process-approval`.

== Element: Perform task — the describe read-back caveat ==

`describe-business-process` shows an element parameter only when it is BOUND or is a result
(`process-perform-task`). A fresh Perform task therefore shows only 11 parameters out of the full set it actually carries (37 declared
on a stock environment). The element's full set is also not a fixed number:
the platform can derive one extra connection parameter per Activity lookup column that exists on YOUR
environment, so a custom column adds a parameter.

== Parameters you set — ActivityCategory: why the Task-typed "Call" row ==

`process-perform-task` names 03DF85BF-6B19-4DEA-8463-D5D49B80BB28 (CallAsTask) as the row to use and
E52BD583-7825-E011-8165-00155D043204 as the wrong one. Why: a Perform task ALWAYS creates a Task-typed
Activity, and the designer's own category dropdown filters ActivityCategory by
ActivityType = Task, so the Call-typed row is one it never offers on this element —
resolving "Call" by NAME is a coin flip whose wrong side no human designer can produce.

== Parameters the runtime sets — ActivityResult: scale of the result-branch rule ==

A formula on a connector leaving a Perform task whose category has result entries is refused, and
`process-activity-result-branches` owns that rule. Affects 337 of
the 1 406 conditional flows shipped in 7.8.0.

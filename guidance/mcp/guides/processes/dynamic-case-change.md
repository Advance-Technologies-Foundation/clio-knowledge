clio MCP process-dynamic-case-change guide - change an existing dynamic case

Scope
Reads and changes an existing dynamic case (DCM) with `describe-dynamic-case` and `modify-dynamic-case`: its stages,
their order, groups, captions and colours, the case caption, and whether it is enabled. Building a case, the bar,
the refusals of `create-dynamic-case` and the undo: `process-dynamic-case`.

Version boundary, before anything else: `describe-dynamic-case` and `modify-dynamic-case` need clio 8.1.0.153 or
later. If `get-tool-contract` does not list `describe-dynamic-case`, the clio serving you is older: stop here and
tell the user to update clio (until then the case designer changes a case); answer a question about the case as
`process-dynamic-case` says, from its dry run. They also need CrtProcessBuilder 1.6.6.173 or later on the
environment: an older one refuses them (`process-builder-without-case-change` when it has no such operation), and
`install-process-builder`, once the user approves it (`core-rules`), is the remedy.

== Change an existing case ==
1. `describe-dynamic-case { environment-name, entity-schema-name | case-name | case-uid }` (read-only): the stages
   in order with `group`, `successful`, `isFinal`, `hasSteps`, `runningInstances`, and `enabled`. Several cases
   on the object: name one with `case-name` (modify by object answers `case-ambiguous`).
2. `modify-dynamic-case { environment-name, case-name | case-uid | entity-schema-name, stages?, caption?, enabled?,
   confirm-running-instances?, dry-run }` writes; the top-level `caption` renames the CASE. `stages` is the WHOLE
   new list of items `{"value": Id or exact display value, "caption"?, "color"?, "group"?, "successful"?}`, e.g.
   `{"value":"Rejected","group":"Approved","successful":false}`: a stage left out is REMOVED, a value not yet a
   stage is ADDED (it must be in the lookup: add it with the data tools first, after asking; the user asked for that
   stage, so this changes the case, it does not get around it). Fields an item leaves out keep their values;
   `group: null` ungroups; `color` (`#RRGGBB`) recolours it; an item's `caption` renames the stage in the case only,
   never the value every record shares. First `dry-run: true`: show the user `diff` and the notes, write only after
   their yes. `unchanged` means nothing to do.
3. While cases run (`runningInstances.total` > 0) every write is refused `running-instances-confirmation-required`:
   tell the user how many run and in which stages; pass `confirm-running-instances: true` only after they say yes,
   NEVER on your own. Running cases continue, possibly on the stages as they were.
4. `enabled: false` / `true` disables or re-enables the case and goes ALONE (`invalid-request` otherwise). Disabled:
   nothing is deleted, running cases continue, new records start no case. Re-enabling while another case of the
   object is enabled: `case-exists`; ask the user whether to disable that case first or keep this one disabled.
   `settings-conflict` on re-enabling: the object's case settings name another stage column, or there are several
   rows; the case designer settles that.
Refusals: `stage-in-use`: tell the user how many records sit there; they move them to another stage or wait until
the cases end - never change records' stage on your own. `stage-has-steps` (it has steps or a step condition
targets it: the case designer), `package-read-only` (a new version is not supported yet).
Outcomes: `failed-restored`: the save did not take, the case is as before; `failed-leftover`: relay its `leftovers`
and have the user check the case in the designer; `unknown`: the reply was lost, so the change may or may not have
been written - describe the case before retrying. No compile or restart; F5.

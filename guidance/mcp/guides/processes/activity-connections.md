clio MCP process-activity-connections guide — bind an Activity's "Connected to" links

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the "Connected to" links of the Activity a task creates, and the R1-R17 connection rules.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

== Activity connections ("Connected to") ==
- WHAT: which records the Activity a task creates is attached to — a contact, an account, and whatever else
  the environment registers as a connection; the set is per-environment, never a fixed list.
  It is functional, not decorative: set, the task appears on the connected record's Activities detail and
  its Timeline and the page fields are pre-filled; unset, none of that happens. An email counts as
  "processed" only with Account or Contact PLUS one further connection.
- HOW: `modify-business-process` → `setConnections` with `elementName` and
  `connections:[{ column, <exactly ONE source> }]`. Sources: `recordId` (a fixed record) |
  `processParameter` | `sourceElement` + `sourceElementParameter` | `expression` (a raw macro — for the
  CURRENT USER see the dedicated rule below, it is the one macro you may author here).
  `referenceSchema` is optional, belongs to `recordId` ALONE, and is a
  CHECK rather than a source — sending it with any other source is refused, because the entity of those is
  whatever the source resolves to.
- `recordId` NEEDS NO SCHEMA UId. The server composes `[#Lookup.{schemaUId}.{recordId}#]` from the target
  column's own reference entity, so send the bare record id. This is the one place the "you cannot guess
  these ids" warning ABOVE does not apply — for a connection, do NOT hand-write the Lookup token.
- CURRENT USER — "link it to me / to my contact / to my account". This is the ONE macro you may author on a
  connection, because the set is CLOSED and named here. Send it as `expression`, chosen by the target
  column's own entity: a Contact column -> `[#SysVariable.CurrentUserContact#]`; an Account column ->
  `[#SysVariable.CurrentUserAccount#]`; a SysAdminUnit (user) column -> `[#SysVariable.CurrentUser#]`.
  Those three are the WHOLE set usable as a connection. Do not invent a fourth (`CurrentUserAccountId`,
  `CurrentAccount`, …), and do not go looking one up: system variables are neither an entity nor an entity
  schema, so `odata-read` answers 404 and `find-entity-schema` answers empty for them — that is those tools
  being right, not the variable being absent. Spell them EXACTLY as above, because what a wrong name costs
  depends on the environment's CrtProcessBuilder and BOTH outcomes are bad: a current build refuses it at the
  write, naming the valid alternatives; an older one stores it unchecked, and the process then fails to
  COMPILE later, far from the edit and with nothing pointing back at the connection.
  One caveat that is data rather than syntax: `CurrentUserAccount` writes EMPTY when the running user's
  contact has no account — where `CurrentUserContact` raises an error in the same situation, the Account
  side stays silent. If the Account link comes back unset, check the user's contact before suspecting the
  macro.
- UPSERT, keyed on `column`. The columns you list are set or re-set; every column you do NOT list is left
  alone. There is no collection-replace and no implicit clearing — so changing one connection can never
  disturb another, and clearing is only ever explicit via `clearConnections`.
- Changing a connection, INCLUDING across dialects (a process parameter → a fixed record), is the same
  `setConnections` call with a new source. Re-sending an unchanged request is idempotent.
- `clearConnections` takes `connections:[{ column }]` and UNBINDS — the element parameter stays. A source
  on a clear entry is rejected. Clearing an already-unbound column is a no-op, not an error. It REPORTS
  what it cleared, and you need that: a cleared connection disappears from describe, so afterwards
  "cleared" and "never bound" are indistinguishable from the read-back alone.
- READ IT BACK with `describe-business-process`: each element carries `connections[]`, every entry giving
  both the raw macro (`value`) and a decoded source in exactly the shape `setConnections` accepts, so you
  can feed it straight back — with FOUR exceptions that refuse on re-apply, none of them values you wrote:
  they are what a designer, an older build, a hand edit or another environment left behind.
  (1) a fixed-record connection whose stored macro names a different entity than its column. TWO remedies,
      and they are not interchangeable: re-send the raw `value` as `expression` to keep the stored macro
      exactly as it is, or omit `referenceSchema` to re-point the connection at the column's OWN entity —
      which rewrites the macro and is a repair, not a re-apply. Choose deliberately;
  (2) a stored value with no macro shape at all (check `source`; it comes back as `expression`) — refused as
      "not a platform macro", because a bare value cannot be a source. Use `recordId`;
  (3) a stored value that IS macro-shaped but from a family that cannot hold a record id — `DateValue`,
      `DateTimeValue`, `TimeValue`, `BooleanValue`. `[#SysSettings...#]` is the one family accepted instead
      of refused, with a warning (below), precisely so designer-authored processes stay re-appliable;
  (4) a `[#SysVariable...#]` whose name does not resolve on THIS environment, or resolves to a variable that
      cannot hold a record id (`CurrentDate`, `CurrentUserRoles`, …). Unlike (1)-(3) this one depends on where
      you are: a current `CrtProcessBuilder` checks the name against the platform's own vocabulary, an older
      one does not, so the same read-back re-applies on one environment and is refused on another. It appears
      when process metadata travelled from a different platform version, or when a connection was hand-edited
      — a designer cannot produce it. Re-point the connection rather than forcing the stored value through.
  Each entry also carries `registered` — `false` means the value IS written at run time but the connection
  is invisible to every registry-reading feature, the same caveat as the write warning below — and `source`,
  the platform value source. Only BOUND connections appear, so absence does NOT mean the column cannot be
  connected; and the WHOLE array is absent when the host entity cannot be resolved or the registry cannot be
  read, so "no connections" is never verified-empty. A macro this build does not recognise degrades to
  `expression` rather than breaking the read.
- WHEN IT IS REFUSED, and why each refusal is worth reading rather than retrying:
  * the user task is not one connections are supported on. The supported set is exactly SIX —
    `ActivityUserTask` (`performTask`), `EmailTemplateUserTask`, `UserQuestionUserTask`,
    `OpenEditPageUserTask`, `AutoGeneratedPageUserTask`, `PreconfiguredPageUserTask` — and anything else,
    including a CUSTOM user task and `approvalUserTask`, is refused with the supported list quoted. A
    non-user-task element is refused too (it creates no record), as is one whose user-task schema does not
    resolve on the environment. This is the most likely refusal in practice, so check it first;
  * the user task's runtime never writes connections (`CallUserTask` builds its Activity directly;
    `EmailUserTask` and `SendEmailUserTask` have none; `readData` creates no activity at all) — model a
    call as `performTask` with the Call activity category instead, and set that category with `addMapping`
    (see the next bullet — `ActivityCategory` is not a connection);
  * the column is not a CONNECTION at all — `ActivityCategory` and `ShowInScheduler` are written through
    their own path and with their own encoding, so use `addMapping` for them. Binding one as a connection
    would set the column and silently degrade the element, which is why it is refused rather than accepted;
  * they would not TAKE EFFECT on this element — almost always `CreateActivity` left at its `false`
    default, which produces a process that saves, compiles, runs green and writes nothing. The refusal
    quotes the exact operation to PREPEND to your own array, so the fix costs one array element, not
    another round trip. `performTask` never hits this: it has no such parameter. A manual-send
    `EmailTemplateUserTask` does not either — the manual sender has no gate;
  * the column is not one this element can carry, or the host entity has no such column at all. Those are
    DIFFERENT diagnoses: the second needs a data-model change (add the lookup column to Activity and
    register it), which `setConnections` deliberately does not make;
  * an `expression` that is not a platform macro at all (it must look like `[#...#]`; a bare value is
    refused — use `recordId`), or one whose macro family cannot hold a record reference (a date, time or
    boolean constant);
  * `referenceSchema` sent without `recordId` — it is a check on the fixed-record source only, so accepting
    it elsewhere would ignore it;
  * a malformed `recordId`, a column that references no entity, or a `processParameter` / `sourceElement`
    of an incompatible type (same type group, and for a lookup the same reference entity — a `Guid` or a
    same-entity Lookup parameter is what works).
- WHEN THE CONNECTION DOES NOT EXIST YET — linking an activity to a record of YOUR OWN entity. This is the
  common ask ("add a button that creates a task linked to this record"), and it is the ONE case that needs a
  DATA-MODEL change, which `setConnections` deliberately does not make for you.
  Do NOT decide whether you are in that case by inspecting the object first — let the OPERATION tell you,
  because the surfaces disagree with each other. Measured for ONE lookup column on one environment: the
  physical `Activity` table carried it, `get-entity-schema-properties` listed it, the object designer did not
  show it, and a process wrote its value successfully. In the other direction, several connection columns
  existed physically while being ABSENT from the schema. WHICH columns those are is a property of the product
  and the installed package chain, not of Creatio — so no list belongs here, including a list of "the
  connections Creatio ships": whatever it named would be wrong on some environment. The refusals ARE the
  check, and they distinguish three states:
  * `<host> has no '<column>' column` — the data-model change below is required;
  * `the column exists on <host> but no connection-registry row registers it and this element's user task
    declares no parameter for it` — only step 2 is required;
  * anything else, including success — there was nothing to add.
  1. add a Lookup column to `Activity` IN THE PACKAGE THAT OWNS THE REFERENCED ENTITY — not in `Custom`, and
     not as a matter of taste. `Custom` is the LAST package: it depends on the others and nothing depends on it
     (measured — `Custom` depends on the platform core, the app package and a product package; no edge points
     back). So a schema in the
     entity's own package cannot reference a column placed in `Custom` without adding the REVERSE edge, and that
     inverts an existing one: the save is refused with "Cyclic dependencies detected", naming
     `EntityColumnValues.Column.<yours>`. Placed in the referenced entity's own package the column needs NO new
     dependency at all, and the environment's existing custom sections show the same shape — each carries its
     own replacing `Activity` layer.
     The call is `update-entity-schema`, which is NON-RESIDENT, so send it through `clio-run`. Args:
     `environment-name`, `package-name` (the REFERENCED entity's), `schema-name: "Activity"`, and
     `operations` — an ARRAY of operation objects, one here:
     `{"action":"add","column-name":"Usr<YourEntity>","type":"Lookup","reference-schema-name":"<your entity>","indexed":true}`.
     The first four keys are all required — omitting `column-name` is the easy mistake, since the column being
     added is named nowhere else; `indexed` is optional and worth setting on a column you will filter by.
     Measured: the column lands and the schema republishes in ~13 s, and reads back as `source: own`.
     A `Reference schema '<your entity>' was not found` refusal means the TARGET package cannot see the
     REFERENCED entity — that, not the `Activity` side, is the dependency that blocks, and the placement above
     is what makes it a non-issue.
     CAVEAT — measured only where that package ALREADY had a replacing `Activity` layer; with no layer yet
     this step takes a path nothing has exercised.
  2. register the column as a connection — ONE bound row in `EntityConnection`, through the
     `create-data-binding-db` tool (also non-resident, also via `clio-run`). Args: `package-name` (yours),
     `schema-name: "EntityConnection"`, `binding-name` (e.g. `"EntityConnectionUsr<YourEntity>"`), and
     `rows`: `[{"values":{"SysEntitySchemaUId":"c449d832-a4cc-4b01-b9d5-8a12c42a9f89","ColumnUId":"<u-id>"}}]`.
     `SysEntitySchemaUId` is Activity's ROOT schema UId — that literal. The column's `u-id` comes from
     `get-entity-schema-properties` (resident, call it natively), NOT from
     `get-entity-schema-column-properties`, whose response carries no `u-id` at all. `rows` is load-bearing:
     without it the tool creates an EMPTY binding and nothing is registered. The package must be non-foreign.
  3. `setConnections` on the element. The element may predate the column by any amount — the operation
     creates the element parameter when the user task declares none.
  Skipping step 2 is not fatal, and the mechanism is worth knowing rather than guessing: the binder resolves a
  column through the registry OR through a parameter the user task already DECLARES, so a declared connection
  binds and writes — with a caveat in the log — even with no registry row. Measured: an `Opportunity`
  connection written by a process on an environment whose registry carried 17 rows, with a Next Steps
  component then displaying the activity. What registration buys is availability to EVERY element rather than
  only to a task that happens to declare that parameter, plus visibility to the surfaces that read the
  registry. After step 2 the designer may keep showing the old set until its caches refresh; the run-time
  write is unaffected.
  Do NOT offer, as a lighter alternative, writing the record's NAME into the activity's title or description.
  That produces no link — no Activities detail, no Timeline, no pre-filled fields — and the ask was a link.
- SUCCEEDS WITH A WARNING, two cases. A column that exists but has no connection-registry row IS written at
  run time, yet the connection is ignored by the record page's connections detail, Next Steps, email
  auto-relation rules and quick-add, and is normally absent from the designer's "Connected to" as well —
  except `Project`, which the designer injects client-side and DOES display. And an `expression` in the
  `[#SysSettings...#]` family is accepted unchecked: its value type cannot be read at design time, so a
  setting holding the wrong KIND of value leaves the column empty at run time. A setting holding NOTHING is
  the other case and worse: the interpreted engine THROWS on it rather than resolving to null, so an empty
  setting fails the step instead of blanking the column. Read the caveats — they arrive
  as `message-type: "Warning"` entries in `execution-log-messages`, NOT as a `warnings` field on the
  response, so finding no such field is not evidence there were none. Some are neutral acknowledgements (a
  column that was already unbound), not failures.
- `addMapping` is NOT deprecated — it remains the general primitive — but it only reaches a connection the
  element ALREADY declares as a parameter and fails with "has no parameter" otherwise; the two page tasks
  (`AutoGeneratedPageUserTask`, `PreconfiguredPageUserTask`) declare none, and since unbound element inputs
  are omitted from describe you cannot tell in advance. Prefer `setConnections`: it creates the parameter
  when one is needed, and adds the validation, the `recordId` ergonomics and the read-back.
- Connections are NOT graph edges. `validate-process-graph` neither checks nor is affected by them; R1-R17
  below are about sequence flows only.

== Connection rules R1–R17 (validate-process-graph enforces the structural subset: R1–R3, R7,
   R9–R15, R17; R4–R6, R8 and R16 are semantic or not yet enforced — verify those yourself.
   Validation pass ≠ buildable: the rules cover the FULL catalog, but only the "What you can build
   today" slice in `process-modeling` can be built — conditional flows ARE in that slice, gateway
   ELEMENTS and default flows are not. The exclusive gateway the platform synthesizes for a conditional
   branch is a GENERATION-TIME construct and never appears as a graph node, so R7 and R14 do not apply
   to it: do not model one when you validate a planned branch, and do not report a process as violating
   them because it has one) ==
R1  Start event: no incoming flow; exactly one outgoing.
R2  End event: no outgoing flow; one or more incoming.
R3  Exactly one top-level start event; every path reaches an end event.
R4  Terminate end kills the whole instance; Simple end ends only its path.
R5  Start triggers: Simple=user/run; Signal(object)=record add/modify/delete; custom signal=broadcast; message=directed; timer=schedule/CRON.
R6  Diverging gateway: 1 in, >=2 out. Converging gateway: >=2 in, 1 out.
R7  Exclusive(OR) diverge: conditional flows + exactly one default; one path taken. Converge: first arrival, no sync.
R8  Parallel(AND) diverge: all out fire, plain sequence flows only. Converge: waits for all incoming.
R9  Inclusive(OR) diverge: conditional flows + required default; >=1 path. Converge: syncs active branches.
R10 Event-based gateway: each outgoing sequence flow leads directly to an intermediate catch event; first event wins.
R11 Parallel and event-based gateways must not carry conditional/default flows.
R12 Sequence flow: target runs after source. Multiple outgoing sequence flows = implicit parallel split.
R13 Conditional flow originates only from a gateway or an activity.
R14 Default flow is legal only if >=1 conditional flow leaves the same element; diverging Exclusive/Inclusive require a default.
R15 No orphan/unreachable nodes; every flow needs a valid source and target.
R16 Sub-process (callActivity) target must begin with a Simple start; collection mapping => multi-instance.
R17 (advisory) Add data one-record mode outputs only Id; chain a Read data for other fields.

Quick can/can't (source -> target via sequence flow): start->{activity,gateway,intermediate,end} ok,
never ->start (R1); end is a sink, never a source (R2); event-based gateway out must hit a catch event (R10).

clio MCP process-parameters guide — process parameters, mappings and formula defaults

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of process parameters, the mappings that bind them, and the date/time/lookup default macros.

== Parameters / mapping / formulas ==
- Process parameters (`parameters[]`): { name, type (Text/Long text/Integer/Float/Money/Boolean/Date/Date-time/Time/Guid/Lookup),
  direction (In/Out/Variable/Internal), caption, description, or referenceSchema = an object name (e.g. City) to make
  it a Lookup to that object }, and an optional value (a constant default; NOT valid for Date / Date-time /
  Time — those defaults are formula macros, see the date macro rule below. A LOOKUP default takes a bare
  record Guid in `value` — stored as the ConstValue the runtime reads. The route ships from
  CrtProcessBuilder 1.3.1.1, and a current clio additionally refuses any environment older than the version
  it bundles (up front, via its package-convergence message), while an older clio surfaces the old package's
  [#Lookup…#]-macro rejection — either refusal means the package is behind, not that the default is
  unsettable). A user-task
  element's own parameters come from the task. The same shape is
  used by modify-business-process `addParameter`. Supported types: Text, Long text, Integer, Float, Money,
  Boolean, Date, Date-time, Time, Guid, and Lookup — other types (composite / entity / file / ...) are not
  supported yet. Name a process parameter per N8 in `process-naming`.
- To create a process parameter that mirrors an element parameter's EXACT type (e.g. expose a user-task
  OUTPUT for mapping with NO conversion), set `typeFromElement` + `typeFromElementParameter` instead of
  `type`/`referenceSchema` — the data value type (and lookup reference object) is copied verbatim.
- Edit a parameter with `setParameter` (parameterName + parameterUpdate: any of caption/description/code/
  direction/referenceSchema/value, applied in place — the UId and its references are preserved). A
  data-type change is rejected, and referenceSchema can only RE-TARGET a parameter that is already a
  Lookup (it cannot convert a scalar to a Lookup). Do NOT set a Date / Date-time / Time default
  through setParameter `value` — those defaults are formula macros, not plain constants; use the
  mapping + `expression` path below (addMapping overwrites, so it edits a default exactly as it
  creates one). A Lookup default IS settable through `value` as a bare record Guid
  (same version story as the parameters note above).
- Remove a parameter with `removeParameter` (parameterName; blocked when another parameter's value, an
  element mapping, an execution-context parameter or a CONDITIONAL FLOW'S CONDITION still references it —
  sub-processes included. The refusal names each usage site. The scan is a SUPERSET of the designer's: it
  matches a parameter UId case-insensitively where the designer matches case-sensitively, so it can refuse
  a delete the designer would allow. Broader is the safe direction — the failure it prevents is a dangling
  reference that surfaces at run time.). That refusal is CrtProcessBuilder's own scan, which is why it can
  NAME each usage site. And the modify path is not unvalidated FOR A FORMULA: the whole schema goes through the platform's
  own process validation before the save (which fails CLOSED — no verdict is treated as invalid, never as
  valid), and that gate is what judges a formula — an `expression` mapping and a flow condition alike. From
  CrtProcessBuilder 1.4.0.41 it is the ONLY thing that judges one: the package no longer checks a formula
  before storing it, so a bad formula fails the whole call at the save rather than the operation that
  carried it. See `process-formulas` for what that refusal says. (The gate's own dangling-reference message
  used to be an unnamed serialised error carrying only a parameter UId; from 1.4.0.41 the package rewrites
  that one message into a sentence naming the reference and the remedy.)
  What none of that judges is whether the removal is the one you MEANT, so on an EXISTING customer process the
  describe-first and confirm-the-removal rules in `process-modeling` still apply.
- A SUB-PROCESS element's parameters are the CALLED process's contract, copied onto the element and
  re-derived whenever the platform builds a schema instance. `process-element-catalog` owns the BUILD
  contract - how the callee is named, which directions keep a value, what `resync` refuses. This guide
  owns what the call does at RUN TIME and how little of it you can observe:
  * THE RULE: after any change to a called process's parameters, re-synchronize every caller with
    `subProcess: {resync: true}`. A MULTI-INSTANCE caller is the exception - it is refused, and has to be
    edited in the designer. Nothing here lists a process's callers: `execute-esq` over `VwProcessLib`
    gives you the candidates, then `describe` each and look for a `subProcess.process` naming it.
  * A `setElement` carrying NO `subProcess` block does not write THE ELEMENT - the rest of the edit is
    applied as asked, and the element is only reported on, as a re-synchronization OWED. Three shapes DO
    re-synchronize, and two are easy to send by accident: `{resync: true}`, any block naming the process
    ALREADY called (`resync: false` does not decline it - that only declines when no process is named),
    and an EMPTY `subProcess: {}`. A block naming a DIFFERENT process is a retarget: it replaces the whole
    contract, and `process-element-catalog` owns its refusals.
  * WHAT A RE-SYNCHRONIZATION COSTS, so you can weigh sending one: where the platform cannot deliver the
    callee it removes the element's parameters AND DELETES THEIR MAPPING ROWS, flags every dependent
    element invalid, and the replacements it creates carry NEW UIds - so re-selecting the process does not
    restore the bindings. A re-sync that was not needed is otherwise safe, with one cost: it clears values
    that came from the callee's own defaults, keeps the ones the caller wrote, and says so in its notices.
  * READ ALL THE NOTICES, not one. A re-sync answers with what the platform's diff moved - parameters
    ADDED, REMOVED, RENAMED, RETYPED, values CLEARED - separately with references left DANGLING (the sites
    still bound to a parameter the element no longer carries), and separately again if it SKIPPED the
    element, for a self-reference or a callee it could not read. Two answers look like success and are
    not. An EMPTY diff is what you get when the load already converged the element: "nothing left to see",
    not "nothing changed". And a notice saying the dangling scan could not complete does not mean nothing
    broke. THE ONE POSITIVE SIGNAL: a `resync: true` that RETURNS AT ALL guarantees every parameter the
    callee currently declares is now present on the element - the write refuses rather than saving a copy
    that did not land.
  * WHAT THE DANGLING NOTICE CATCHES, from CrtProcessBuilder 1.6.3.26, is the sites the platform's own
    pre-save validation does not catch first - a stored blob such as a Modify-data element's column
    bindings. A reference this package wrote (a formula, or a mapping - it stores both as `Script`) is
    usually refused by that validation before the re-sync can report it; usually, because that refusal is
    gated on an application-CONFIGURATION key, `Feature-UseVerificationOfProcessParameterDirection`,
    default ON and not switchable from the product UI. Where it is off the notice reaches those sites too,
    and it can name several in one message.
  * WHY IT MATTERS: values cross by parameter NAME, matched CASE-SENSITIVELY, and direction is ignored on
    the receiving side - the copy-back writes into whatever element parameter carries the callee's name,
    `In` and `Variable` alike. A name present on only one side is SKIPPED, on the interpreted path with no
    exception and no log line. Requiredness is never validated on either side. A COLLECTION-typed
    parameter crosses like any other; what does not cross is a member of a collection ITEM's structure. So
    the cases that bite are narrow: a parameter the callee DROPPED, one it renamed while the element
    stayed BEHIND, and - for that parameter alone - one it ADDED that nobody mapped. A RETYPE still
    crosses. To USE an output, map FROM it: name the element and the parameter as the SOURCE of a mapping
    onto a process parameter or another element's input.
  * AND WHICH PROCESS ACTUALLY RUNS is not the one you synchronized against, if the callee is VERSIONED.
    Design time uses the stored UId; run time resolves that UId's family and executes its ACTIVE VERSION.
    A caller re-synchronized against the version you edited can therefore still call a different one, and
    nothing in a read tells you so - check which version of the callee is active before concluding that a
    correct re-sync fixed the delivery.
  * `inSync` IS NOT A DRIFT REPORT. It compares NAMES and nothing else, CASE-INSENSITIVELY, in ONE
    direction - whether every parameter the callee currently declares is PRESENT on the element. So it
    catches an ADD, and misses a REMOVE, a caption change, and a CASE-ONLY code rename, which the runtime
    does bind on. On a MULTI-INSTANCE element `false` is permanent and meaningless (the element carries
    two collections and three iteration counters instead of the callee's names), so check `multiInstance`
    first. And `true` can simply mean the read repaired the element on its way to you: materialising a
    schema instance from metadata re-synchronizes every sub-process element on it, and so does every fetch
    of the DESIGN instance - the path every write takes. Never read `true` as "intact".
  * A parameter on the element that the callee does NOT declare is not necessarily damage. One the CALLER
    created, with no mapping row, is a legitimate and permanent state: no re-synchronization removes it.
  * IF YOU NEED `inSync` AS EVIDENCE, use the recipe - save the caller, `describe` it once, change the
    callee, `describe` again - and know its limits. Step 1 is a WRITE that converges and persists the
    element, so it erases any drift that had already happened. The caller must not be saved again in
    between, because a second save re-converges and re-persists it. And it can only ever reveal an ADD or
    a non-case rename. If those limits do not fit, skip the recipe and just re-synchronize - its notices
    are computed against the callee as the manager currently holds it, which a save of the CALLEE
    refreshes.
  * A RENAME is followed automatically. The element parameter is paired to the callee's through the
    mapping row's source UId rather than by name, so the element's copy keeps its own UId while its name,
    caption, data type, direction and five other properties are overwritten from the callee. Whether its
    VALUE survives needs BOTH a provenance stamp saying the caller wrote it and a direction of `In` or
    `Variable`; the notice says whether it survived, not which condition failed. What is NOT followed is
    anything YOU wrote naming the old parameter; that has to be updated by hand.
  * THE DESIGNER CARD cannot check any of this. Its parameter row has no code column: a type icon, a
    direction icon, the caption - or the CODE, when a parameter has no caption - and the mapping value.
    Opening the card re-derives the element's parameters and re-attaches the stored values BY NAME, so a
    code rename can leave the row EMPTY while the caller's stored mapping is intact. An empty row means
    "the carry-over did not match", not that your data is gone. Do not re-map on the strength of one, and
    DO NOT SAVE the caller from that card - re-deriving drops the unmatched parameter's mapping row in
    memory, and saving persists that. Close it and read `describe-business-process`, which reports every
    parameter of a sub-process element with its `source` and `value`. (Observed once, 2026-09-17, and only
    when the caption was renamed alongside the code. Nothing in the client reads the caption on this path,
    so the condition is unexplained - assume EITHER rename can do it until it is re-measured.)
  `describe-business-process` reports the element's parameters as the instance it was handed carries them.
  `direction` and `isRequired` are re-copied from the callee on every synchronization, so they track it;
  `isResult` is copied only when the element parameter is CREATED and is never refreshed, so a callee that
  later flips it leaves the element reporting the old value, and no re-sync reports or fixes that.
- Mappings (`mappings[]`): bind a TARGET parameter to a SOURCE.
  TARGET — `elementName` + `elementParameter` (an element input) OR `targetProcessParameter`
  (a process parameter, e.g. expose an element's OUTPUT as a process output).
  SOURCE — exactly ONE of: `sourceElement` + `sourceElementParameter` (another element's OUTPUT parameter) |
  processParameter (a process parameter by name) | value (a constant) | expression (a raw formula).
  Identifying an OUTPUT for `sourceElementParameter`: in `describe-business-process` output an element parameter
  is usable as a mapping source when `isResult: true` OR `direction: "Out"`. Most user-task outputs come back as
  `isResult: true` with `direction: "Variable"` (the platform reports element params as Variable), so detect
  outputs by `isResult`, NOT by `direction` alone.
  Parameter-to-parameter mappings require COMPATIBLE TYPES (target-driven, mirroring the visual designer);
  incompatible pairs are rejected:
  * text -> text: any text source into a base-text target; Phone/Email/Web/Rich targets accept only the
    SAME extra type or a base-text source (never a different extra type);
  * Money <-> Float map to each other; Integer maps ONLY to Integer (NOT to Float or Money);
  * date/time is asymmetric via Date-time: a Date-time target accepts Date/Date-time/Time; a Date target
    accepts Date/Date-time; a Time target accepts Time/Date-time; Date <-> Time is NOT allowed;
  * Lookup: the same reference object on both sides; a Guid source INTO a lookup target IS allowed;
  * Boolean only from Boolean; any other type: exact match only. When the target must match a source
    exactly, mirror it with `typeFromElement` instead of guessing.
  `processParameter` flows a process input into the
  field (the server builds the correct reference); `expression` is a FORMULA — see `process-formulas` for the
  vocabulary and what is checked. Still PREFER `value` / `processParameter` / `sourceElement` when one of
  them expresses the intent: they are structural, so the server builds the reference and a rename cannot
  break it. Reach for `expression` when the value has to be COMPUTED, or for the constant families that have
  no literal form — date/time, system variable, system setting. A LOOKUP is not one of them on a PARAMETER:
  its value is a bare record Guid in `value`. The macro form is still the route for a CONSTANT lookup
  column on a `changeData` element, whose `value` is text-only; a column fed from the process uses
  `processParameter` or `sourceElement` — see `process-data-elements`.
- UNBOUND element INPUT parameters are NOT listed by `describe-business-process` — except on a SUB-PROCESS
  element, whose whole parameter set is reported, because those parameters are the called process's own
  contract rather than inherited task defaults — (it returns only
  value-bearing parameters and outputs) — absence from describe does NOT mean the parameter does not
  exist. Input parameter names come from the user task's schema (for a custom task, the parameters it
  was created with); a wrong `elementParameter` name fails the build with a clear error and nothing is
  saved — never invent names silently.
- To CHANGE a bound value, send `addMapping` again for the same target — it overwrites the binding in
  place (like the designer). There is NO clear/unbind operation (no removeMapping): if asked to
  "remove" a value, say clearing is not supported yet and offer to overwrite it instead.
- Date / Date-time / Time DEFAULT VALUES must be a formula, not a constant: the designer stores a
  date/time constant as a formula macro (a Script source), NOT a plain `value` (a `ConstValue`). Set it via `expression` — for a process-parameter
  default, a mapping with `targetProcessParameter` + `expression`. The inner format is FIXED (NOT ISO,
  NOT locale): `dd.MM.yyyy` and 24-hour `HH:mm`.
  Date → `[#DateValue.dd.MM.yyyy#]` (e.g. `[#DateValue.03.07.2026#]`);
  Date-time → `[#DateTimeValue.dd.MM.yyyy HH:mm#]` (e.g. `[#DateTimeValue.03.07.2026 02:15#]`);
  Time → `[#TimeValue.HH:mm#]` (e.g. `[#TimeValue.12:20#]`). A LOOKUP value is DIFFERENT: prefer a bare record
  Guid in `value` (route ships from CrtProcessBuilder 1.3.1.1 — stored as the ConstValue the runtime reads; on an
  ActivityUserTask category the ConstValue encoding is REQUIRED, owned by `process-task-category`). The
  `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` expression form (both GUIDs: the referenced OBJECT's
  schema UId, NOT its name, then the RECORD's Id) still exists, but reach for it only on a pre-1.3.1.1
  package that rejects the bare Guid — and never for a parameter whose consumer reads ConstValue only, an
  ActivityUserTask's category being that case. From 1.4.0.40 that same macro is ACCEPTED in a MAPPING's
  `value` (`addMapping`, and `mappings[]` at create) on a Lookup target and decoded back to the bare record
  id, so a value echoed from describe re-submits unchanged; that is a round-trip convenience, not a reason
  to author the macro form. It is the MAPPING route only — `addParameter` / `setParameter` still take the
  bare Guid and refuse the macro. 1.4.0.40 also resolves the referenced record's NAME into the parameter's
  display value — the designer renders that, so a lookup constant shows a word instead of a Guid, and
  describe reports it as the read-only `valueDisplay` beside the unchanged bare-Guid `value`.
  It remains the route for a CONSTANT lookup on a `changeData` element's column, whose `value` is text-only
  — but a column fed from elsewhere in the process is not a constant and does not use it:
  `processParameter` or `sourceElement` carry a record id that exists only at run time, which a
  `[#Lookup…#]` macro cannot — see `process-data-elements`.
  EXCEPTION — an Activity CONNECTION: there you send a bare `recordId` to `setConnections` and the server
  composes the token from the target column, so hand-writing it is both unnecessary and easy to get wrong.
- To read another element's output, PREFER the structured `sourceElement` + `sourceElementParameter` mapping (above) — the server builds the correct reference. Do NOT hand-write an element-output reference —
  in the saved metadata it is a server-generated UId meta-path
  (`[#...[Element:{uid}].[Parameter:{uid}].[EntityColumn:{uid}]#]`), NOT a friendly `Element.Property`
  path — ALWAYS use `sourceElement` for a MAPPING. Formulas are strictly typed (convert with `.ToString()`
  etc.).
  This applies to the `sourceElement` mapping ONLY. It does NOT mean a formula cannot reference a
  parameter: inside an `expression` there is no structured alternative, and the UId meta-path is exactly
  what you write. `process-formulas` owns that form — you build it from the `uid` that
  `describe-business-process` reports, and it is the only accepted one.
  The third segment above, `[EntityColumn:{uid}]`, is what the PLATFORM writes when it stores such a
  reference. You cannot author one: `describe-business-process` reports no column UIds, so there is nowhere
  to get it. A read record's individual columns are not referenceable from a MAPPING, a `changeData` value
  or a filter condition either (ENG-91844) — but an email BODY macro does reach them, with
  `[[element:<Element>.<OutputParameter>.<Column>]]`, a different grammar that needs no UId (see
  `process-send-email`). Inside a formula: author two segments, not three.

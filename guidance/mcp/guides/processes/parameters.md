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
  contract - how the callee is named, which directions keep a value, what `resync` does and what it
  refuses. What that contract cannot tell you is how the call behaves at RUN TIME, and how little of its
  drift is observable:
  * values cross by parameter NAME, and DIRECTION IS IGNORED when they do - the copy-back writes into
    whatever element parameter carries the callee's name, including an `In` one. A name present on only
    one side is SKIPPED with no exception and no log line, and so is a name that DOES match but whose
    value fails conversion. Requiredness is never validated on either side. So a callee that gained,
    lost or renamed a parameter leaves the caller RUNNING and quietly delivering nothing. A
    COLLECTION-typed parameter crosses like any other - what does not cross is a member of a collection
    ITEM's structure. To USE an output, map FROM it: name the element and the parameter as the SOURCE of
    a mapping onto a process parameter or another element's input
  * `inSync` compares NAMES and nothing else, CASE-INSENSITIVELY. It cannot see a caption change, and
    not because the element keeps a caption of its own - the platform overwrites the element parameter's
    caption, and eight further properties, from the callee on every synchronization - but because the
    predicate reads only names. A CASE-ONLY code rename (`Amount` to `amount`) is a real change the
    runtime binds on, and `inSync` stays `true`
  * `inSync` is ONE-DIRECTIONAL: it asks whether every parameter the callee CURRENTLY declares is
    PRESENT on the element. A callee that ADDS one flips it to `false`; a callee that REMOVES one leaves
    it `true`, so a dropped parameter is invisible to it. On a MULTI-INSTANCE element `false` is
    permanent and means nothing - the element carries two collections and three iteration counters
    instead of the callee's parameter names, so the test cannot be satisfied unless the callee declares
    no parameters at all, and the re-sync a `false` would call for is refused there anyway. Check
    `multiInstance` before acting on `false`
  * WHAT `inSync` CAN SHOW YOU DEPENDS ON THE CALLER'S CACHED SCHEMA INSTANCE, and it fails silently in
    both directions. Drift is visible only while that cached instance PREDATES the callee's change. On a
    COLD cache the first read BUILDS the instance and building CONVERGES it, so `true` on a first read
    says nothing about the saved metadata. What erases the drift afterwards is eviction or a converging
    path: saving or publishing the CALLER, a configuration compile, an application restart, and any
    operation routed through the DESIGN instance, which re-synchronizes on every fetch. Two things do
    NOT erase it, against expectation: re-reading, which hands back the same cached object, and RUNNING
    the caller, which executes that same stale element - that IS the silent no-delivery failure, not a
    way to clear it. Read `describe-business-process` BEFORE changing the callee where you can;
    afterwards treat `inSync: true` as NO EVIDENCE rather than as "intact"
  * what the re-sync REPORTS, from CrtProcessBuilder 1.6.3.7, is the CONSEQUENCE of a DROPPED parameter:
    it names the reference sites still bound to a parameter the element no longer carries. In practice
    those are the sites the platform's own pre-save validation does not catch first - a stored blob such
    as a Modify-data element's column bindings. A reference whose source is a formula or a mapping is
    usually refused by that validation before the re-sync can report it; usually, because the refusal
    sits behind a platform feature flag and safe-generation mode, so where either is off the notice
    reaches those sites too, and it can name several in one message
  * a RENAME is followed automatically AND reported. The element parameter is paired to the callee's
    through the mapping row's source UId rather than by name, so the element's copy keeps its own UId
    while its name, caption, data type, direction and five other properties are overwritten from the
    callee; whether its VALUE survives is decided separately by a provenance stamp, and the notice says
    which happened. The rename notice fires for a rename the request itself observes. ACROSS requests it
    usually stays silent - the design-time convergence already applied the new name before the diff was
    taken - so silence here is not evidence either. Anything you WROTE naming the old parameter still
    has to be updated by hand
  * THE DESIGNER CARD cannot be used to check any of this. Its parameter ROW has no code column: a type
    icon, a direction icon, the caption - or the CODE, when a parameter has no caption - and the mapping
    value. Opening the card re-derives the element's parameters from the callee and carries the stored
    values over BY NAME, because the re-derived copies get fresh UIds and the UId branch of that lookup
    cannot match. So a CODE rename can leave the row EMPTY while the caller's stored mapping is intact,
    and a caption-only rename never does. An empty row is therefore "the carry-over did not match", not
    a statement about your data - do not re-map on the strength of one. But DO NOT SAVE the caller from
    a card showing one either: re-deriving clears the element's mapping rows in memory, and saving
    persists that. Close the card and read `describe-business-process`, which reports the binding under
    the callee's CURRENT name. (One stand run, 2026-09-17, recorded the empty row only when the caption
    was renamed alongside the code. Nothing in the client reads the caption when deciding this, so treat
    that as an artefact of instance freshness rather than a caption rule.)
  * so the rule is procedural: after ANY change to a called process's parameters, re-synchronize every
    caller with `subProcess: {resync: true}`. A `setElement` carrying NO `subProcess` block does not
    write - it reports whether a re-synchronization is OWED and leaves the element alone. Three shapes
    DO write, and two of them are easy to send by accident: `{resync: true}`, a block naming the process
    the element ALREADY calls, and an EMPTY `subProcess: {}` block, which is not read as a decline.
    Send the explicit one, because you meant it - not because something looked wrong
  `describe-business-process` reports each parameter's `direction`, `isResult` and `isRequired`, so which
  way a value travels is readable before you map it.
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

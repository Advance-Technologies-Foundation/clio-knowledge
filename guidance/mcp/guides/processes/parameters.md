clio MCP process-parameters guide — process parameters, mappings and formula defaults

One of seven process articles. `process-modeling` is the entry point and indexes the other six.
This article is the authoritative owner of process parameters, the mappings that bind them, and the date/time/lookup default macros.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

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
- Remove a parameter with `removeParameter` (parameterName; blocked when another parameter's value or an
  element mapping still references it).
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
  field (the server builds the correct reference); `expression` is a raw C#-like formula passed through UNVALIDATED — the backend (unlike the visual designer) does NOT check it, so a wrong token / function / type fails only at RUNTIME. Do NOT invent or guess formulas: formula-authoring guidance (token format + the allowed function set) is not available yet. Prefer `value` / `processParameter` / `sourceElement`; use `expression` ONLY with a formula you already know is correct (user-supplied, or copied verbatim from an existing process via describe-business-process), e.g.
  `[#SysVariable.CurrentUserContact#]`, `[#SysVariable.CurrentDateTime#].AddDays(3)`.
- UNBOUND element INPUT parameters are NOT listed by `describe-business-process` (it returns only
  value-bearing parameters and outputs) — absence from describe does NOT mean the parameter does not
  exist. Input parameter names come from the user task's schema (for a custom task, the parameters it
  was created with); a wrong `elementParameter` name fails the build with a clear error and nothing is
  saved — never invent names silently.
- To CHANGE a bound value, send `addMapping` again for the same target — it overwrites the binding in
  place (like the designer). There is NO clear/unbind operation (no removeMapping): if asked to
  "remove" a value, say clearing is not supported yet and offer to overwrite it instead.
- Date / Date-time / Time DEFAULT VALUES are the ONE formula you may author (an EXCEPTION to the
  "don't invent formulas" rule): the designer stores a date/time constant as a formula macro (a Script
  source), NOT a plain `value` (a `ConstValue`). Set it via `expression` — for a process-parameter
  default, a mapping with `targetProcessParameter` + `expression`. The inner format is FIXED (NOT ISO,
  NOT locale): `dd.MM.yyyy` and 24-hour `HH:mm`.
  Date → `[#DateValue.dd.MM.yyyy#]` (e.g. `[#DateValue.03.07.2026#]`);
  Date-time → `[#DateTimeValue.dd.MM.yyyy HH:mm#]` (e.g. `[#DateTimeValue.03.07.2026 02:15#]`);
  Time → `[#TimeValue.HH:mm#]` (e.g. `[#TimeValue.12:20#]`). A LOOKUP value is DIFFERENT: prefer a bare record
  Guid in `value` (route ships from CrtProcessBuilder 1.3.1.1 — stored as the ConstValue the runtime reads; on an
  ActivityUserTask category the ConstValue encoding is REQUIRED, see NOTE-2 in `process-perform-task`). The
  `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` expression form (both GUIDs: the referenced OBJECT's
  schema UId, NOT its name, then the RECORD's Id) still exists, but reach for it only on a pre-1.3.1.1 package
  that rejects the bare Guid — and never for a parameter whose consumer reads ConstValue only.
  EXCEPTION — an Activity CONNECTION: there you send a bare `recordId` to `setConnections` and the server
  composes the token from the target column, so hand-writing it is both unnecessary and easy to get wrong.
- To read another element's output, PREFER the structured `sourceElement` + `sourceElementParameter` mapping (above) — the server builds the correct reference. Do NOT hand-write an element-output reference —
  in the saved metadata it is a server-generated UId meta-path (`[#...[Element:{uid}].[Parameter:{uid}].[EntityColumn:{uid}]#]`), NOT a friendly `Element.Property` path, so you cannot author it — ALWAYS use `sourceElement`. Formulas are strictly typed (convert with `.ToString()` etc.).

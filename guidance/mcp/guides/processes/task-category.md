clio MCP process-task-category guide — a task's category and priority are lookup CONSTANTS

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of WHY a task element's ActivityCategory and ActivityPriority
MUST be a bare record Guid in `value`, stored as a ConstValue, and MUST NOT be a `[#Lookup...#]`
formula. `process-perform-task` owns the parameter itself: the category ids, which of the two "Call"
rows to use, and the two refusal texts a stale package returns with their remedy. Read this article for
why the formula form is wrong and what it silently degrades -- why the formula form degrades the allowed-results list silently, how DisplayValue is
serialized into the schema resources rather than the metadata, and which CrtProcessBuilder version
each behaviour ships from. `process-perform-task` owns the element and its parameter table, and states
WHICH category id to use; this article states what goes wrong when the value is written any other way.
Split out of `process-perform-task` because that article had no response-budget headroom left, and
because this is the one block in it that a reader needs only when something already looks wrong.

== A category or priority value MUST be a constant ==
ActivityCategory MUST be a constant (`value`, stored as ConstValue), not a formula. The element's
allowed-results list is computed from the category ONLY when the category's source is ConstValue (the
platform's `GetResultParameterAllValues` reads `SourceValue.Value` only for a ConstValue source — client-side
and server-side alike); writing it as a `[#Lookup...#]` expression sets the Activity's category column but
SILENTLY DEGRADES the allowed-results list the task page / designer result dropdown offers, falling back to
the default set. Do NOT try to verify the degradation through the `Activity.AllowedResult` column — that
column derives from outgoing CONDITIONAL flows, not from the category, and is empty either way on a process
without them. So the bare-Guid `value` is the only correct route; on a pre-1.3.1.1 package the parameter
cannot be set correctly — update the package rather than using the expression form.

This matches what the DESIGNER stores for the real-lookup families this rule is about (a task element's
ActivityCategory / ActivityPriority): a lookup constant a human picks is
`{Source: ConstValue, Value: <bare record Guid>}` on the element parameter, with the record's NAME - or
nothing at all - in the parameter's DisplayValue. Do not read that as a universal designer rule: the
designer's own corpus is mixed (absent, the raw Guid and a readable name all occur, and the platform ships
a first-party schema with the raw Guid in DisplayValue), so an agent checking a real schema will find
counter-examples. Name-or-nothing is the CORRECT convention, not the most common one. The `[#Lookup...#]`
macro form the designer does produce belongs to a different place: a change-data COLUMN mapping, where the
value is a formula in its own right. Do not carry the macro across to an element parameter because you saw it
in a designer-authored schema.

DisplayValue is where a design-time defect used to live and is worth understanding, because it is invisible
in `metadata.json`: it is a LOCALIZABLE string, so it is serialized into the schema's RESOURCES
(`BaseElements.<Element>.Parameters.<Param>.DisplayValue`), not into the metadata beside `Value`. The designer
shows a NON-EMPTY DisplayValue verbatim and resolves the record name itself only when it is EMPTY — so a
DisplayValue holding the raw id made the "Task category" field render `03df85bf-…` instead of `Call`, while
the runtime behaved correctly the whole time. From CrtProcessBuilder 1.4.0.40 the server resolves the
referenced record's name and stores THAT, and leaves DisplayValue unset when it cannot (which is the correct
degrade — the designer then resolves the name). Nothing about the input contract changed: you still pass a
bare record Guid.
Why the server resolves the name rather than simply leaving DisplayValue empty: only the Perform task's
category field re-resolves an empty display value (`ActivityUserTaskPropertiesPage.initActivityCategory`).
Every other designer surface reads the parameter through `getMappingValue()`, which returns
`displayValue || value` (`process-schema-parameter.js`) — an empty display value renders the raw Guid again
there. "Just write nothing" is therefore the cheaper WRONG fix, not a safe alternative.
Evidence: observed on a Creatio 8.x stand through `describe-business-process` and the pulled schema
resources (`Resources/<Process>.Process/resource.en-US.xml`) against CrtProcessBuilder 1.4.0.40; the client
behaviour is read from the designer's own source, not inferred.

Two conveniences shipped with it, both from 1.4.0.40:
* an already-composed `[#Lookup.{objectUId}.{recordId}#]` passed as a MAPPING `value` on a Lookup target is
  DECODED to the bare record id and stored as a ConstValue — so a value echoed back from describe re-submits
  unchanged. This does not make the expression form correct here; it makes the round trip safe. Which
  routes accept the macro and which refuse it is owned by `process-parameters` (the "A LOOKUP value is
  DIFFERENT" bullet) — read it there rather than here, so the two never drift;
* `describe-business-process` reports the resolved name as `valueDisplay` beside the unchanged bare-Guid
  `value`. `valueDisplay` is read-only and re-derived on every write — never feed it back as `value`. Its
  absence means the environment could not name the record, NOT that the value is wrong.

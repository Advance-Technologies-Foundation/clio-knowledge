clio MCP process-web-service details - the Call web service element's rare run-time shapes and evidence

Scope
Split out of `process-web-service`, which owns the `webService` block and every other rule of the element; read it
first. This article owns the two list shapes below, the rarer refusals and edge cases listed after them, and the
evidence of the element's rules.

== Run time: list shapes ==
- An inner list with no source of its own while its items are bound (`items_In.parts_In`): Creatio builds without the
  platform fix for arrays inside arrays (10.1.37 among them), or with feature `DisableParametersWithNestedArrays` on,
  send NO value for that inner list. Bind the inner list itself to a collection, or build the body in `RequestBody`.
  The notice comes on every environment: the fix cannot be read.
- While application setting `Feature-ProcessParameterCollections` is on, a name or tag repeated in one top-level
  list's tree: in a request list a repeated name stops the element once two of those items carry a value, a repeated
  tag sends one value; in a response list a repeated tag stops it when the result is stored, a repeated name keeps one
  value.

== Rarer refusals and edge cases ==
- Refusals of a `setElement` `webService` block (`process-web-service`): `service` naming ANOTHER service without
  `method`: refused with that service's methods; the SAME service and nothing else: refused, nothing to change. A
  block that names nothing is refused.
- A multi-instance Call web service element refuses the `webService` block; mappings onto it still apply, and
  describe shows it with an issue. The block itself: `process-web-service`.
- `RequestBody` replaces the body the element would build. An empty string from a formula or parameter replaces it too
  (an empty body is sent); a constant "" counts as unset. Nothing checks it as JSON, and nothing warns when it and body parameters are both
  bound.
- Besides an unresolved service or method, describe's `issues` report nested items that differ from the service, a
  missing `Success` / `StatusCode` / `Response` (the run stops with an error), an element or parameter marked invalid
  (e.g. a required input not set), and the two list shapes above.

== Evidence ==
Build-time rules pinned by CrtProcessBuilder 1.6.6.150 unit tests. Measured on Creatio 10.1.37 (.NET
Framework) on 2026-11-03 (designer-built elements): a 404 and a refused connection, `ignoreErrors` on and off; the
designer deleting all parameters when the method is gone; an unrelated modify pruning a case-drifted element. Every other
run-time rule is read from the Creatio core source and the designer client code,
not measured.

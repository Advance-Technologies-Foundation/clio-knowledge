clio MCP process-web-service guide - the Call web service element (webService)

Scope
This guide owns the `webService` block of the Call web service element.
`process-parameters` owns mappings and declared Collections; `process-files` owns rule P1 (an item mapping binds its
collection). A C# web service that Creatio EXPOSES is not this element: `configuration-webservice`.
Needs CrtProcessBuilder 1.6.6.150 or later on the environment. Use it only when the FULL
contract (`get-tool-contract` with `tool-names: ["create-business-process"]`, `detail: "full"`) names `webService`;
a clio whose contract lacks it refuses the block before sending. The default reply is shortened: its silence
proves nothing. Always read the element back: its parameter names come from the service.

== Only a registered service ==
The element calls only a web service REGISTERED in Studio > Web services (REST, or SOAP: built with REST's parameter
rules, with a notice); the process tools do not register, change or test one. Nothing the SERVICE stores - its base
URI, authentication, header or parameter defaults - comes back in any answer.

== The block ==
`webService {service, method, timeoutSeconds?, ignoreErrors?}`, on `create-business-process`, `addElement` and
`setElement` (`elementUpdate.webService`):
"elements": [{ "name": "CallOrderService", "type": "webService",
  "webService": { "service": "UsrOrderService", "method": "GetOrders", "timeoutSeconds": 30, "ignoreErrors": true } }]
- `service`: its name (code), caption or UId. `method`: the method's name. Both are REQUIRED on
  `create-business-process` and `addElement`; a sole method is not picked for you.
- `timeoutSeconds`: whole seconds, an overall limit; `0`, or absent on create and `addElement`: none (absent on
  `setElement`: kept). Negative or over 2147483 is refused.
- `ignoreErrors`: `true` lets the process go on after a call with no HTTP answer (== Run time ==).
- Nothing else: request values are mappings; no member for a base URI, credential, default or multi-instance.
  `type: "webService"` without the block, or the block on another type, is refused.
- Caption: omitted, it is `<service caption, or its name> <N>`; a sent `caption` is kept.
- The generic `userTask` route REFUSES `WebServiceUserTask`: a legacy SOAP task, not this element.

== Naming the service and method ==
- The service resolves by UId, then by name (exact, else ignoring case), then by caption (ignoring case); the first
  step that matches decides. The method: by name within that service, exact, else ignoring case.
- The CANONICAL spelling is stored (a case-only change, with a notice): Creatio looks a method up exactly at run time.
- REFUSED, nothing saved: an unknown service (the refusal lists 30 services with caption and up to 8 methods, 70 more
  with caption, then a count - one not listed still resolves by exact name or UId; one shown as "methods not readable"
  IS registered, never register it again - and says to register a missing one in Studio > Web services); no service
  registered; a registered service that cannot be read (open and save it in Studio > Web services); a name or caption
  matching several services (listed with methods; send the exact name or the UId); an unknown method (the refusal
  lists the service's methods with captions); a method name matching several only when case is ignored; a method with an object
  parameter that is not an array.
- A service in a package the process's package does not depend on is built, with a notice: the Process Designer adds
  that dependency when it saves (while `AutoAddPackageDependenciesInProcesses` is on, the default), this tool does
  not. Add it, or the package exported alone fails at run time.

== A task that names no service or method ==
1. Find the service and method through ONE refusal: a build naming an unknown service (and any `method`) saves
   nothing and lists the registered services (bounds above). Listed without methods? Probe it with an unknown
   `method`: that refusal lists them. Use an unused process name: a taken one is refused first. Pick the pair that
   fits; when several fit, ASK the user which; never guess between them.
2. When none fits, STOP: no process, no placeholder element, no Script task in its place. Tell the user to register
   it in Studio > Web services, and what that needs: the base URL, the method and its parameters, and the
   authentication. The user enters any credential there, not you.
3. Build in two steps: the element first, then the mappings, from the names the answer and describe report.
   Declare their process parameters with `typeFromElement`: a `Text` one cannot be retyped later.
4. A key or token parameter is filled from a system setting - an `expression` `[#SysSettings.<Code>#]`
   (`process-formulas`) - or from a process parameter, NEVER from a constant: describe returns a stored one to any
   reader. When the service itself defaults the key from a system setting (the shipped
   `GeminiApiSearch` does), leave that input unbound: an unbound input is skipped.

== The parameters the block generates ==
The method's parameters become the element's own, under these names:
- request parameter `title` (body, header, query, path or cookie alike) -> input `title_In`; response parameter
  `status` -> output `status_Out`;
- an object array -> a list (`CompositeObjectList`) whose items are named the same way and addressed dotted:
  `items_In.name_In`, at any depth;
- a scalar array `tags` -> a list (`ObjectList`) with one item, `tags_In.tagsItemValue_In`;
- then four the element adds itself: `Success` (Boolean, out), `Response` (the response body, Text, out),
  `StatusCode` (the HTTP status code, Integer, out), `RequestBody` (raw request text, Text, in).
The create or retarget answer lists the generated names (`*` required); describe's `webService.parameters` is the
authoritative tree. No aliases: the service's own name `title` is not an element parameter. A mapping onto a name the
element lacks is refused, listing its top-level parameters.
- A process parameter that feeds an input or takes an output: declare it with `typeFromElement` +
  `typeFromElementParameter` (`process-parameters`), NEVER a guessed `type`: a mapping across type groups is refused.
  A list is refused as a mirror: declare a Collection (== Reading the response ==).

== Request values: mappings ==
Set an input through the ordinary mapping surface (`process-parameters`): `mappings[]` on create, `addMapping` on
modify, target `elementName` + `elementParameter`. Mappings in the same request can address the generated names.
"mappings": [
  { "elementName": "CallOrderService", "elementParameter": "customerId_In", "processParameter": "CustomerIdParameter" },
  { "elementName": "CallOrderService", "elementParameter": "mode_In", "value": "full" },
  { "elementName": "CallOrderService", "elementParameter": "apiKey_In", "expression": "[#SysSettings.UsrOrderApiKey#]" } ]
- A composite body: map a list item from an item of another collection (`items_In.name_In` from
  `ReadContacts.ResultCompositeObjectList.Name`) and the list is bound to that collection too, one request item per
  source row (rule P1, `process-files`). Leaves bound only from parameters, constants or formulas send ONE item.
  A scalar list (`tags_In`) cannot take rows from a list of objects, nor the reverse: the types differ, refused.
- An item fed by a FORMULA that reads row items does NOT bind its list: map the list itself to the source collection
  as well, or the formula is evaluated outside any row. Nothing warns about this for a top-level list; an inner list
  draws the unsourced-inner-list notice.
- `RequestBody`: when non-null it REPLACES the whole body the element would build from its body parameters; headers,
  query, path and cookies are still built from theirs. An empty string from a formula or parameter replaces it too (an
  empty body is sent); a constant "" counts as unset. Use it for a method with no body parameter (as the shipped
  `ActionGeminiSearch` does with a formula), a body mappings cannot express, and the inner-list workaround (== Run
  time ==). Nothing checks it as JSON, and nothing warns when it and body parameters are both bound.

== Reading the response ==
- `<name>_Out`, `Success`, `StatusCode` and `Response` are ordinary outputs (`sourceElement` +
  `sourceElementParameter`).
- An object-array response: declare a process `Collection` with `itemProperties` (at most TWO collection levels,
  `process-parameters`), then map its items from the output's items: `targetProcessParameter`
  `OrdersParameter.Number` from `sourceElementParameter` `orders_Out.number_Out`. Mapping an item binds the
  collections above it too (rule P1).
- Not buildable yet: a scalar-array output into a process parameter, a third collection level, and the designer's
  "Create from element" shape. Read `Response` in a Script task (`process-script-task`) instead.

== Changing the service or method (modify) ==
`setElement` with `elementName` and `elementUpdate.webService`, top-level elements only (a nested one: the designer):
- Omitted members keep their stored values (a service change resets the timeout, below). `method` alone: another
  method of the same service, refused while the stored service is not registered or readable. `service` naming ANOTHER
  service without `method`: refused with that service's methods; the SAME service and nothing else: refused, nothing
  to change. `timeoutSeconds: 0` clears the timeout; `ignoreErrors: false` clears the flag. A block that names nothing
  is refused.
- The same service and method (case ignored): a SETTINGS change of the timeout and the flag only - UNLESS the
  stored method differs in case from the service's: then it is a retarget, guarded as below.
- Another method or service: a RETARGET:
  * EVERY parameter of the element is replaced by the new method's and every input binding on them is dropped,
    `RequestBody` included; `Success`, `Response` and `StatusCode` keep their identity here, so their readers stay
    valid (the designer re-creates those three under new ids, leaving their readers dangling);
  * the answer lists every dropped input binding WITH its value, as `addMapping` `mapping` objects, so one call
    re-applies those whose target name the new method still has; a binding no mapping can write (a system value or
    setting, a stored mapping) is listed apart, with its stored source: re-bind it in the Process Designer;
  * a service change ALWAYS renames the caption to `<service caption, or its name> <N>` and resets the timeout unless
    the block sends one; a method change keeps both;
  * REFUSED, nothing written, while anything - another element, a process parameter, a flow condition, a formula -
    reads ANY other parameter of the element: an output, a nested item, an input, `RequestBody`. A same-named output
    of the new method is a new parameter and does not keep its readers. The refusal names each reader. Remove or
    re-point those uses in a SEPARATE, earlier call: the check reads the process as the request found it, so the
    same batch does not help.
- A multi-instance Call web service element refuses the `webService` block; mappings onto it still apply, and
  describe shows it with an issue.

== When the service changes under a process ==
Every load of a process re-reads each Call web service element from its service. When the service is gone, or no longer has the stored method spelled exactly, that load REMOVES the element's service
parameters, references to them stop working, and a save keeps them removed.
- `describe-business-process` reports `serviceResolved: false` or `methodResolved: false` with a sentence in
  `issues`: the call fails at run time, `ignoreErrors` or not. An issue saying the service could not be read or
  checked means UNKNOWN, not failing.
- Every `modify-business-process` request warns for EVERY such element of the process, touched or not; a settings-only
  change does not resolve it.
- In the Process Designer, when the service exists but lacks the stored method, OPENING the element deletes EVERY
  parameter of that element, `Success`, `Response` and `StatusCode` included, with their mapping rows; closing its
  panel marks it invalid, and a designer save keeps that. When the service itself is gone, the load has already
  removed the service parameters, and the designer leaves those three.
- So FIX FIRST, before anyone opens the element in the designer or makes an unrelated modify. `methodResolved: false`
  is the signal (an unreadable service: unknown, above). A request's load has already dropped the service inputs and
  their bindings, so a retarget lists at most a `RequestBody` binding. To keep them (before any save), restore the
  method in Studio > Web services, or install the package that carries the service (one registered again gets another
  UId). Else retarget (`setElement`) and bind the inputs again. A retarget is refused while anything reads a parameter
  of the element, a removed one too; a read of a removed one also makes Creatio's pre-save check refuse EVERY modify,
  an unrelated one too, nothing saved. Adding the parameter does not repair it, nor does re-pointing the element; the
  notice names the elements and the order: restore as above and repeat (unless the process was saved in the designer
  since); else remove or replace every read in an earlier request, then retarget and re-bind the reads in one request.
- `issues` also report nested items that differ from the service, a missing `Success` / `StatusCode` / `Response` (the
  run stops with an error), an element or parameter marked invalid (e.g. a required input not set), and the inner-list
  and duplicate-item shapes below.

== Run time ==
- An HTTP error answer (404, 500) never fails the element: it completes with `Success` false and that `StatusCode`,
  whatever `ignoreErrors` is: branch on them.
- A call with NO HTTP answer (a refused or unreachable connection, an expired timeout): with `ignoreErrors` off
  the element and the process FAIL; with it on the element completes with `Success` false and `Response` holding the
  error text, and `StatusCode` is NOT written.
- While feature `DisableIgnoreErrorsInWebServiceUserTask` is on, `ignoreErrors: true` is saved, with a notice, but a
  failing call still stops the process. It does NOT cover computing the inputs (an empty system setting behind
  `[#SysSettings.<Code>#]` fails the element), an unresolved service or method, or reading the response.
- Timeout: absent or `0`, only the method's own timeout applies, to each attempt, and retries repeat it.
  `timeoutSeconds` adds an overall limit over the whole call, retries included; it cannot lengthen an attempt beyond
  the method's timeout. When it expires, the call fails like a call with no answer, unless an earlier attempt had an
  HTTP error answer: that answer completes the element. A compiled (not interpretable) process applies neither
  `timeoutSeconds` nor `ignoreErrors`.
- An inner list with no source of its own while its items are bound (`items_In.parts_In`): Creatio builds without the
  platform fix for arrays inside arrays (10.1.37 among them), or with feature `DisableParametersWithNestedArrays` on,
  send NO value for that inner list. Bind the inner list itself to a collection, or build the body in `RequestBody`.
  The notice comes on every environment: the fix cannot be read.
- While application setting `Feature-ProcessParameterCollections` is on, a name or tag repeated in one top-level
  list's tree: in a request list a repeated name stops the element once two of those items carry a value, a repeated
  tag sends one value; in a response list a repeated tag stops it when the result is stored, a repeated name keeps one
  value.

== Notices (warnings: the process saves) ==
Tell the user each. Besides those above: a required input with no value (the designer marks the element invalid).

== Read back ==
`describe-business-process` (`element=<name>` or `mode=full`) returns the element's `webService` block: `service` (the
name; null when the stored service is not registered), `method`, `timeoutSeconds` (may be fractional), `ignoreErrors`
(absent when false), and the describe-only `serviceUId`, `serviceResolved`, `methodResolved`, `parameters` (every
stored parameter, unbound inputs included, with `tag`, `direction`, `type`, nested `items` and the `required`, `bound`
and `result` flags; no values) and `issues`. A bound input's value comes back in the element's generic `parameters`.
To resubmit, send the block back unchanged - the describe-only members are ignored with a warning - except: a
fractional `timeoutSeconds` is refused on write (leave it out on `setElement` to keep the stored timeout);
`service: null` leaves a method-only block, which is refused; a stored method whose case differs from the service's makes the
block a retarget, not a no-op.

Evidence: build-time rules pinned by CrtProcessBuilder 1.6.6.150 unit tests. Measured on Creatio 10.1.37 (.NET
Framework) on 2026-11-03 (designer-built elements): a 404 and a refused connection, `ignoreErrors` on and off; the
designer deleting an unresolved method's parameters; an unrelated modify pruning a case-drifted element. Every other
run-time rule is read from the Creatio core source and the designer client code,
not measured.

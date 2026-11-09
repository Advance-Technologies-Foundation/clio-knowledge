clio MCP process-web-service guide - the Call web service element (webService)

Scope
This guide owns the `webService` block of the Call web service element (`type: "webService"`): the service and
method, the generated parameters, request and response, retargeting, refusals, notices and the read-back.
`process-parameters` owns mappings and declared Collections; `process-files` owns rule P1 (an item mapping binds its
collection); `process-formulas` owns the formula vocabulary. A C# web service that Creatio EXPOSES is not this
element: `configuration-webservice`.
Needs CrtProcessBuilder <CRTPROCESSBUILDER-WEBSERVICE-TBD> or later on the environment. Use it only when the FULL
contract (`get-tool-contract` with `tool-names: ["create-business-process"]`, `detail: "full"`) names `webService`
and clio is at least <CLIO-WEBSERVICE-TBD>; an older clio refuses the block before sending. The default reply is
shortened, so its silence proves nothing. Always read the element back: its parameter names come from the service.

== Only a registered service ==
The element calls a web service already REGISTERED in Studio > Web services (REST, or SOAP 1.1 / 1.2). The process
tools do not register, change or test a service; an unregistered one cannot be called. Nothing the SERVICE stores -
its base URI, authentication, header or parameter defaults - comes back in any answer (describe, refusals, notices).
The element's OWN bindings do: describe returns a bound input's value, and a retarget lists the values it dropped.

== The block ==
`webService {service, method, timeoutSeconds?, ignoreErrors?}`, on `create-business-process`, `addElement` and
`setElement` (`elementUpdate.webService`):
"elements": [{ "name": "CallOrderService", "type": "webService", "caption": "Call order service",
  "webService": { "service": "UsrOrderService", "method": "GetOrders", "timeoutSeconds": 30, "ignoreErrors": true } }]
- `service`: the service's name (its code), caption or UId. `method`: the method's name. Both are REQUIRED on
  `create-business-process` and `addElement`; a sole method is not picked for you.
- `timeoutSeconds`: whole seconds, an overall limit; absent or `0`: none. Negative, or more than 2147483, is
  refused.
- `ignoreErrors`: `true` lets the process go on after a call that gets no HTTP answer (== Run time ==). Default false.
- Nothing else: request values are mappings; no member for a base URI, credential, default or multi-instance.
  `type: "webService"` without the block, or the block on another type, is refused.
- Caption: omit it and the element is captioned `<service caption, or its name> <N>`, as the Process Designer does
  (`N` the smallest number no other element uses); a `caption` you send is kept. Name the element from the Names
  block of `process-digest`.
- The generic `userTask` route REFUSES `WebServiceUserTask`: a legacy SOAP task, not this element.

== Naming the service and method ==
- The service resolves by UId, then by name (exact, else ignoring case), then by caption (ignoring case); the first
  step that matches decides. The method resolves within that service by name, exact, else ignoring case.
- The CANONICAL spelling is stored; a case-only difference is written in the service's spelling, with a notice.
  Creatio looks a method up exactly at run time.
- REFUSED, nothing saved: an unknown service (the refusal lists every registered service as `Name (caption)` and
  says to register a missing one in Studio > Web services); no service registered at all; a registered service that
  cannot be read (open and save it in Studio > Web services); a name or caption that matches several services (send
  the exact name or the UId); an unknown method (the refusal lists the service's methods); a method name that matches
  several methods only when case is ignored; a method with an object parameter that is not an array.
- A service in a package the process's package does not depend on is built, with a notice: the Process Designer adds
  that dependency when it saves (while `AutoAddPackageDependenciesInProcesses` is on, the default), this tool does
  not. Add it, or an export of the process's package alone fails at run time.
- A SOAP service is built with the same parameter rules as REST, with a notice saying so.

== A task that names no service or method ==
1. Find the service through the refusal: a build naming a service that does not exist saves nothing and lists every
   registered service with its caption. Pick the one that fits; when several
   fit, ASK the user which; never guess between them.
2. When none fits, STOP: no process, no placeholder element, no Script task in its place. Tell the user the service
   has to be registered in Studio > Web services, and what that needs: the base URL, the method and its parameters,
   and the authentication. The user enters any credential there, not you.
3. Build in two steps: the element first, then the mappings, from the names the answer and describe report.
4. A key or token parameter is filled from a system setting - an `expression`
   `[#SysSettings.<Code>#]` (`process-formulas`) - or from a process parameter, NEVER from a constant: a constant
   is stored in the process, and describe returns it to whoever reads the process. When the service itself defaults
   the key from a system setting (the shipped `GeminiApiSearch` sends `x-goog-api-key` from `GeminiApiKey`), leave
   that input unbound: an unbound input is skipped.

== The parameters the block generates ==
The method's parameters become the element's own, named as describe returns them and as mappings address them:
- request parameter `title` (body, header, query, path or cookie alike) -> input `title_In`; response parameter
  `status` -> output `status_Out`;
- an object array -> a list (`CompositeObjectList`) whose items are named the same way and addressed dotted:
  `items_In.name_In`, at any depth;
- a scalar array `tags` -> a list (`ObjectList`) with one item, `tags_In.tagsItemValue_In`;
- then four the element adds itself: `Success` (Boolean, out), `Response` (the response body, Text, out),
  `StatusCode` (the HTTP status code, Integer, out), `RequestBody` (raw request text, Text, in).
The create or retarget answer lists the generated names (`*` required); describe's `webService.parameters` is the
authoritative tree. No aliases: the service's own name `title` is not an element parameter. A mapping onto a name the
element lacks is refused, listing the element's top-level parameters.

== Request values: mappings ==
Set an input through the ordinary mapping surface (`process-parameters`): `mappings[]` on create, `addMapping` on
modify, target `elementName` + `elementParameter`. Mappings of the same create request, or a later operation of the
same modify batch, can address the generated names.
"mappings": [
  { "elementName": "CallOrderService", "elementParameter": "customerId_In", "processParameter": "CustomerIdParameter" },
  { "elementName": "CallOrderService", "elementParameter": "mode_In", "value": "full" },
  { "elementName": "CallOrderService", "elementParameter": "apiKey_In", "expression": "[#SysSettings.UsrOrderApiKey#]" } ]
- A composite body: map a list item from an item of another collection (`items_In.name_In` from
  `ReadContacts.ResultCompositeObjectList.Name`) and the list is bound to that collection too, one request item per
  source row (rule P1, `process-files`). Leaves bound only from parameters, constants or formulas send ONE item.
- An item fed by a FORMULA that reads row items does NOT bind its list: map the list itself to the source collection
  as well, or the formula is evaluated outside any row. Nothing warns about this for a top-level list; an inner list
  draws the unsourced-inner-list notice.
- `RequestBody`: when non-null it REPLACES the whole body the element would build from its body parameters; headers,
  query, path and cookies are still built from theirs. An empty string from a formula or parameter replaces it too
  (an empty body is sent); a constant "" counts as unset. Use it for a method with no body parameter (the shipped
  process `ActionGeminiSearch` builds the JSON body of `GeminiApiSearch.GenerateContent` there with a formula), a
  body mappings cannot express, and the inner-list workaround (== Run time ==). Nothing checks it as JSON, and
  nothing warns when it and body parameters are both bound.

== Reading the response ==
- `<name>_Out`, `Success`, `StatusCode` and `Response` are ordinary outputs: map them (`sourceElement` +
  `sourceElementParameter`) into a process parameter, another element's input, a flow condition or a formula.
- An object-array response: declare a process `Collection` with `itemProperties` (at most TWO collection levels,
  `process-parameters`), then map its items from the output's items: `targetProcessParameter`
  `OrdersParameter.Number` from `sourceElementParameter` `orders_Out.number_Out`. Mapping an item binds the
  collections above it too (rule P1). In the Process Designer: map an item first; the designer then binds the
  collection to the source collection itself.
- Not buildable yet: mirroring a list output with `typeFromElement` (refused, pointing at the declared Collection), a
  scalar-array output into a process parameter, a third collection level, and the designer's "Create from element"
  shape. Read `Response` in a Script task (`process-script-task`) instead.

== Changing the service or method (modify) ==
`setElement` with `elementUpdate.webService`, the element named in `elementName`:
- Omitted members keep their stored values. `method` alone: another method of the same service. `service` naming
  ANOTHER service without `method`: refused with that service's methods, nothing written. `timeoutSeconds: 0` clears
  the timeout; `ignoreErrors: false` clears the flag. A block that names nothing is refused.
- The same service and method (case ignored): a SETTINGS change; only the timeout and the flag change - UNLESS the
  stored method differs in case from the service's: then it is a retarget, guarded as below.
- Another method or service: a RETARGET:
  * EVERY parameter of the element is replaced by the new method's and every input binding on them is dropped,
    `RequestBody` included; `Success`, `Response` and `StatusCode` keep their identity here, so their readers stay
    valid (the Process Designer re-creates those three under new ids and leaves their readers dangling);
  * the answer lists every dropped input binding WITH its value, in the `addMapping` write shape, so one call
    re-applies those whose target name the new method still has;
  * a service change ALWAYS renames the caption to `<service caption, or its name> <N>` (`setElement` has no
    caption member) and resets the timeout unless the block sends one; a method change keeps both;
  * REFUSED, nothing written, while anything - another element, a process parameter, a flow condition, a formula -
    reads ANY other parameter of the element: an output, a nested item, an input, `RequestBody`. A same-named output
    of the new method is a new parameter and does not keep its readers. The refusal names each reader. Remove or
    re-point those uses in a SEPARATE, earlier call: the check also reads the process as the request found it, so
    clearing them in the same batch does not help.
- A multi-instance Call web service element refuses the `webService` block; mappings onto it still apply, and
  describe shows it with an issue.

== When the service changes under a process ==
Each time Creatio loads a process from the database it re-reads every Call web service element from its service.
When the service is gone, or no longer has the stored method spelled exactly, that load REMOVES the element's service
parameters, references to them stop working, and a save of the process keeps them removed.
- `describe-business-process` reports `serviceResolved: false` or `methodResolved: false` with a sentence in
  `issues`: the call fails at run time, `ignoreErrors` or not (the lookup runs before the error handling). An issue
  saying the service could not be read or checked means UNKNOWN, not failing.
- Every `modify-business-process` request warns for EVERY such element of the process, touched or not; a settings-only
  change does not resolve it.
- In the Process Designer, when the service exists but lacks the stored method, OPENING the element deletes EVERY
  parameter of that element, `Success`, `Response` and `StatusCode` included, with their mapping rows; closing its
  panel marks it invalid, and a designer save keeps that. When the service itself is gone, the load has already
  removed the service parameters, and the designer leaves `Success`, `Response` and `StatusCode`.
- So FIX FIRST, before anyone opens the element in the designer or makes an unrelated modify: a missing method,
  retarget with `setElement` (`webService.method`); a missing service, install the package that carries it (a
  service registered again gets another UId) or retarget to another one (`webService.service` + `webService.method`).
  A retarget is refused while anything still reads a service parameter of the element: remove those readers in a
  separate, earlier call. `methodResolved: false` is the signal.
- `issues` also report nested items that differ from the service (the designer rebuilds them from the service when
  the element's properties panel is opened), a missing `Success` / `StatusCode` / `Response` (the run stops with an
  error), an element the designer marked invalid (a required input not set), and the
  inner-list and duplicate-item shapes below.

== Run time ==
- An HTTP error answer (404, 500) never fails the element: it completes with `Success` false and that `StatusCode`,
  whatever `ignoreErrors` is: branch on them.
- A call that gets NO HTTP answer (a refused or unreachable connection, an expired timeout): with `ignoreErrors` off
  the element and the process FAIL; with it on the element completes with `Success` false and `Response` holding the
  error text, and `StatusCode` is NOT written.
- `ignoreErrors` needs Creatio 8.3.2 or later (earlier builds ignore the flag). While feature
  `DisableIgnoreErrorsInWebServiceUserTask` is on, `ignoreErrors: true` is saved, with a notice, but a failing call
  still stops the process. It does NOT cover computing the inputs (an empty system setting behind
  `[#SysSettings.<Code>#]` fails the element), an unresolved service or method, or reading the response.
- Timeout: absent or `0`, only the method's own timeout applies, to each attempt, and retries repeat it.
  `timeoutSeconds` adds an overall limit over the whole call, retries included; it cannot lengthen an attempt beyond
  the method's timeout. When it expires, the call fails like a call with no answer.
- An inner list with no source of its own while its items are bound (`items_In.parts_In`): Creatio builds before the
  platform fix for arrays inside arrays (10.1.37 among them; fixed in 10.1.540 and 10.2.1), or later builds with
  feature `DisableParametersWithNestedArrays` on, send NO value for that inner list. Bind the inner list itself to a
  collection, or build the body in `RequestBody`. The notice comes on every environment: the fix cannot be read.
- While feature `ProcessParameterCollections` is on: a duplicate name among the request items sent, or a duplicate
  tag among the response items, stops the element, across nesting levels too; a duplicate request tag under
  different names silently sends one value.

== Notices (warnings: the process saves) ==
Tell the user each. Besides those named above: a required input with no value (the Process Designer marks such an
element invalid), and a duplicate item name or tag at any depth.

== Read back ==
`describe-business-process` (`element=<name>` or `mode=full`) returns the element's `webService` block: `service`
(the name; null when the stored service is not registered), `method`, `timeoutSeconds` (may be fractional),
`ignoreErrors` (absent when false), and the describe-only `serviceUId`, `serviceResolved`, `methodResolved`,
`parameters` (every stored parameter, unbound inputs included, with `tag`, `direction`, `type`, nested `items` and the
`required`, `bound` and `result` flags; no values) and `issues`. A bound input's value comes back in the element's
generic `parameters`. No service default is ever shown. To resubmit, send the block back unchanged - the describe-only
members are ignored with a warning - except: a fractional `timeoutSeconds` is refused on write (send whole seconds);
`service: null` leaves a method-only block, which is refused; a stored method whose case differs from the service's
makes the block a retarget, not a no-op.

Evidence: build-time rules pinned by CrtProcessBuilder <CRTPROCESSBUILDER-WEBSERVICE-TBD> unit tests. Measured on a
stand on 2026-11-03 (designer-built elements): a 404 completed the element, `ignoreErrors` on or off; a refused
connection completed it with `ignoreErrors` on and failed the process with it off; the designer deleted every
parameter of an element whose method no longer resolved; an unrelated modify deleted the service parameters of an
element whose stored method differed in case. The run-time rules for `RequestBody`, the timeout, `ignoreErrors`,
duplicates and the inner list are read from the Creatio core source and the designer client code, not measured.

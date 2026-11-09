clio MCP process-web-service guide - the Call web service element (webService)

Scope
This guide owns the `webService` block of the Call web service element (`type: "webService"`, the Process Designer's
"Call web service"): naming the service and method, the parameters the block generates, request values and the
response through them, changing the service or method, the refusals, the notices and describe's read-back.
`process-parameters` owns mappings and declared Collections; `process-files` owns rule P1 (an item mapping binds its
collection); `process-formulas` owns the formula vocabulary. A C# web service that Creatio EXPOSES is not this
element: `configuration-webservice`.
Needs CrtProcessBuilder <CRTPROCESSBUILDER-WEBSERVICE-TBD> or later on the environment. Use it only when the FULL
contract (`get-tool-contract` with `tool-names: ["create-business-process"]`, `detail: "full"`) names `webService`;
the default reply is shortened, so its silence proves nothing. Always read the element back: its parameter names come
from the service, not from you.

== Only a registered service ==
The element calls a web service already REGISTERED in Studio > Web services (REST, or SOAP 1.1 / 1.2). The process
tools do not register, change or test a service. A service that is not registered cannot be called from here:
tell the user it has to be registered in Studio > Web services first. Nothing the SERVICE stores - its base URI,
authentication, header or parameter defaults - comes back in any answer (describe, refusals, notices). The element's
OWN bindings do: describe returns a bound input's value, and a retarget lists the values it dropped.

== The block ==
`webService {service, method, timeoutSeconds?, ignoreErrors?}`, on `create-business-process`, `addElement` and
`setElement` (`elementUpdate.webService`):
"elements": [{ "name": "CallOrderService", "type": "webService", "caption": "Call order service",
  "webService": { "service": "UsrOrderService", "method": "GetOrders", "timeoutSeconds": 30, "ignoreErrors": true } }]
- `service`: the service's name (its code), caption or UId. `method`: the method's name. Both are REQUIRED on
  `create-business-process` and `addElement`; a sole method is not picked for you.
- `timeoutSeconds`: whole seconds. Absent or `0`: the method's own timeout. Negative is refused.
- `ignoreErrors`: `true` lets the process go on after a call that gets no HTTP answer (== Run time ==). Default false.
- The block takes nothing else: request values are mappings, and there is no member for a base URI, a credential, a
  parameter default, or multi-instance. `type: "webService"` without the block is refused; the block on another
  type is refused.
- Caption: omit it and the element is captioned `<service caption> <N>`, as the Process Designer does (`N` the
  smallest number no other element uses); a `caption` you send is kept. Name the element from the Names block of
  `process-digest`.
- The generic `userTask` route REFUSES `WebServiceUserTask`: a legacy SOAP task, not this element. Build
  `type: "webService"` with its block.

== Naming the service and method ==
- The service resolves by UId, then by name (exact, else ignoring case), then by caption (ignoring case); the first
  step that matches decides. The method resolves within that service by name, exact, else ignoring case.
- The CANONICAL spelling is stored; a case-only difference is written in the service's spelling, with a notice.
  Creatio looks a method up exactly at run time, so send what the refusal or describe spells.
- REFUSED, nothing saved: an unknown service (the refusal lists every registered service as `Name (caption)` and
  says to register a missing one in Studio > Web services); no service registered at all; a name or caption that
  matches several services (send the exact name or the UId); an unknown method (the refusal lists the service's
  methods); a method name that matches several methods only when case is ignored; a method with an object parameter
  that is not an array.
- A service in a package the process's package does not depend on is built, with a notice: the Process Designer adds
  that dependency when it saves, this tool does not. Add it, or an export of the process's package alone fails at run
  time.
- A SOAP service is built with the same parameter rules as REST, with a notice saying so.

== A task that names no service or method ==
1. Find the service through the refusal: a build naming a service that does not exist saves nothing and lists every
   registered service with its caption (an unknown method lists the methods). Pick the one that fits; when several
   fit, ASK the user which; never guess between them.
2. When none fits, STOP: no process, no placeholder element, no Script task in its place. Tell the user the service
   has to be registered in Studio > Web services, and what that needs: the base URL, the method and its parameters,
   and the authentication. The user enters any credential there, not you.
3. Build in two steps: the element first, then the mappings, from the names the answer and describe report.
4. A key or token parameter (an API key header, a token) is filled from a system setting - an `expression`
   `[#SysSettings.<Code>#]` (`process-formulas`) - or from a process parameter, NEVER from a constant: a constant
   is stored in the process, and describe returns it to whoever reads the process.

== The parameters the block generates ==
The method's parameters become the element's own, named as describe returns them and as mappings address them:
- request parameter `title` (body, header, query, path or cookie alike) -> input `title_In`; response parameter
  `status` -> output `status_Out`;
- an object array -> a list (`CompositeObjectList`) whose items are named the same way and addressed dotted:
  `items_In.name_In`, at any depth;
- a scalar array `tags` -> a list (`ObjectList`) with one item, `tags_In.tagsItemValue_In`;
- then four the element adds itself: `Success` (Boolean, out), `Response` (the response body, Text, out),
  `StatusCode` (the HTTP status code, Integer, out), `RequestBody` (raw request text, Text, in).
The create or retarget answer lists the generated names (inputs, then outputs, items in brackets, `*` required);
describe's `webService.parameters` is the authoritative tree. No aliases: the service's own name `title` is not an
element parameter. A mapping onto a name the element lacks is refused, listing the element's top-level parameters.

== Request values: mappings ==
Set an input through the ordinary mapping surface (`process-parameters`): `mappings[]` on create, `addMapping` on
modify, target `elementName` + `elementParameter`. On create, mappings of the same request can address the generated
names; on modify, an `addElement` generates them before a later `addMapping` of the same batch runs.
"mappings": [
  { "elementName": "CallOrderService", "elementParameter": "customerId_In", "processParameter": "CustomerIdParameter" },
  { "elementName": "CallOrderService", "elementParameter": "mode_In", "value": "full" },
  { "elementName": "CallOrderService", "elementParameter": "apiKey_In", "expression": "[#SysSettings.UsrOrderApiKey#]" } ]
- A composite body: map a list item from an item of another collection (`items_In.name_In` from
  `ReadContacts.ResultCompositeObjectList.Name`) and the list is bound to that collection too, one request item per
  source row (rule P1, `process-files`). Leaves bound only from parameters, constants or formulas send ONE item.
- An item fed by a FORMULA that reads row items does NOT bind its list: map the list itself to the source collection
  as well, or the formula is evaluated outside any row. Nothing warns about this.
- `RequestBody`: when it holds a value (a constant, a formula, a parameter), it REPLACES the whole body the element
  would build from its body parameters. Use it for a method with no body parameter (the shipped `GeminiApiSearch`
  service builds its JSON body there with a formula), for a body shape mappings cannot express, and for the inner-list
  workaround (== Run time ==). It is stored as given: nothing checks it as JSON, and nothing warns when it and body
  parameters are both bound - only `RequestBody` is sent.

== Reading the response ==
- `<name>_Out`, `Success`, `StatusCode` and `Response` are ordinary outputs: map them (`sourceElement` +
  `sourceElementParameter`) into a process parameter, another element's input, a flow condition or a formula.
- An object-array response: declare a process `Collection` with `itemProperties` (at most TWO collection levels,
  `process-parameters`), then map its items from the output's items: `targetProcessParameter`
  `OrdersParameter.Number` from `sourceElementParameter` `orders_Out.number_Out`. Mapping an item binds the
  collections above it too (rule P1). In the Process Designer the same: map an item first, because the collection
  picker offers no web-service object array.
- Not buildable yet: mirroring a list output with `typeFromElement` (refused, pointing at the declared Collection), a
  scalar-array output into a process parameter, a third collection level, and the designer's "Create from element"
  shape. Read `Response` in a Script task (`process-script-task`) instead.

== Changing the service or method (modify) ==
`setElement` with `elementUpdate.webService`, the element named in `elementName`:
- Omitted members keep their stored values. `method` alone: another method of the same service. `service` naming
  ANOTHER service without `method`: refused with that service's methods, nothing written. `timeoutSeconds: 0` clears
  the timeout; `ignoreErrors: false` clears the flag. A block that names nothing is refused.
- The same service and method (case ignored): a SETTINGS change; only the timeout and the flag change.
- Another method or service: a RETARGET, the Process Designer's replace:
  * EVERY parameter of the element is replaced by the new method's and every input binding on them is dropped,
    `RequestBody` included; `Success`, `Response` and `StatusCode` keep their identity, so their readers stay valid;
  * the answer lists every dropped input binding WITH its value, in the `addMapping` write shape, so one call
    re-applies those whose target name the new method still has;
  * a service change ALWAYS renames the caption to `<service caption> <N>` (`setElement` has no caption member) and
    resets the timeout unless the block sends one; a method change keeps both;
  * REFUSED, nothing written, while anything - another element, a process parameter, a flow condition, a formula -
    reads ANY other parameter of the element: an output, a nested item, an input, `RequestBody`. A same-named output
    of the new method is a new parameter and does not keep its readers. The refusal names each reader and what it
    reads. Remove or re-point those uses in a SEPARATE, earlier call: the check also reads the process as the
    request found it, so clearing them in the same batch does not help.
- A multi-instance Call web service element (made in the Process Designer) is refused: describe shows it, nothing here
  configures it.

== When the service changes under a process ==
Each time Creatio loads a process from the database it re-reads every Call web service element from its service.
When the service is gone, or no longer has the stored method spelled exactly, that load REMOVES the element's service
parameters, references to them stop working, and a save of the process keeps them removed.
- `describe-business-process` reports `serviceResolved: false` or `methodResolved: false` with a sentence in
  `issues`; either means the call fails at run time.
- Every `modify-business-process` request warns for EVERY such element of the process, touched or not; a settings-only
  change does not resolve it.
- A save in the Process Designer of a process whose element no longer resolves DELETES EVERY parameter of that
  element, `Success`, `Response` and `StatusCode` included, with their mapping rows, and marks the element invalid.
  So FIX FIRST, before any designer save or unrelated modify: a missing method, retarget with `setElement`
  (`webService.method`); a missing service, install the package that carries it (a service registered again gets
  another UId) or retarget to another one (`webService.service` + `webService.method`). `methodResolved: false` is
  the signal.
- `issues` also report nested items that differ from the service (the designer rebuilds them when the method is
  re-selected), a missing `Success` / `StatusCode` / `Response` (the run stops with an error), an element the
  designer marked invalid (a required input not set), a multi-instance element, and the inner-list and duplicate-item
  shapes below.

== Run time ==
- An HTTP error answer (404, 500) never fails the element: it completes with `Success` false and that `StatusCode`,
  whatever `ignoreErrors` is. Branch on `Success` or `StatusCode` when the answer matters.
- A call that gets NO HTTP answer (a refused or unreachable connection): with `ignoreErrors` off the element and the
  process FAIL; with it on the element completes with `Success` false and `StatusCode` 0.
- While feature `DisableIgnoreErrorsInWebServiceUserTask` is on, `ignoreErrors: true` is saved, with a notice, but a
  failing call still stops the process.
- `timeoutSeconds` covers the whole call, retries included.
- An inner list with no source of its own while its items are bound (`items_In.parts_In`): Creatio builds before the
  platform fix for arrays inside arrays (10.1.37 among them) send NO value for that inner list. Bind the inner list
  itself to a collection, or build the body in `RequestBody`. The notice comes on every environment: whether yours
  has the fix cannot be read.
- Two items of one list parameter with the same name or tag: Creatio stops with an error when it runs the element.

== Notices (warnings: the process saves) ==
Tell the user each: the generated parameter names (create, retarget); a service or method written in its canonical
spelling; `ignoreErrors` saved while the feature above is on; the input bindings a retarget dropped, with their values;
a service in a package the process's package does not depend on; a SOAP service built with the REST rules; a required
input with no value (the Process Designer marks such an element invalid); an unsourced inner list; a duplicate item
name or tag; and, on every modify, each element whose service or method no longer resolves.

== Read back ==
`describe-business-process` (`element=<name>` or `mode=full`) returns the element's `webService` block: `service`
(the name; null when the stored service is not registered), `method`, `timeoutSeconds` (a fraction when the stored
milliseconds are not whole seconds), `ignoreErrors` (absent when false), and the describe-only `serviceUId`,
`serviceResolved`, `methodResolved`, `parameters` (every stored parameter, unbound inputs included, with `tag`,
`direction`, `type`, nested `items` and the `required`, `bound` and `result` flags; no values) and `issues`. A bound
input's value comes back in the element's generic `parameters`. No service default is ever shown. To resubmit,
send the block back unchanged: the describe-only members are ignored with a warning.

Evidence: the build-time rules are pinned by CrtProcessBuilder <CRTPROCESSBUILDER-WEBSERVICE-TBD> unit tests.
Measured on a stand on 2026-11-03 against designer-built elements: an HTTP 404 completed the element with `Success`
false and the process went on, `ignoreErrors` on or off; a refused connection completed it with `StatusCode` 0 with
`ignoreErrors` on and failed the element and the process with it off; a designer save of a process whose element's
method no longer resolved deleted every parameter of the element and its mapping rows; an unrelated modify deleted
the service parameters of an element whose stored method differed in case. What `RequestBody`, the timeout and the
inner-list defect do at run time is read from the Creatio core source and its unit tests, not measured.

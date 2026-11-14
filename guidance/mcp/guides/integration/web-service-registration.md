clio MCP web service registration guide — REST and SOAP services in Studio > Web services

Scope and ownership
- Use this guide when an external REST or SOAP API must be registered in Creatio (Studio > Web services) so a
  business process can call it, or when a registered service must be read, changed or test-called. For a SOAP
  service also read `get-guidance name=web-service-soap`: it owns what differs (type, namespaces, message element,
  SOAP action, element paths, SOAP header, `setNamespaces`, `setType`); everything here applies to SOAP too.
- This guide owns `register-web-service`, `list-web-services`, `describe-web-service`,
  `modify-web-service` and `test-web-service-method`: their arguments, the credential rule, the usage guard
  and the follow-up steps the user owns.
- It does NOT own the Call web service process element: `get-guidance name=process-web-service` owns the
  element, and the process element catalog (`get-guidance name=process-element-catalog`) owns whether it can be
  built. This guide only produces the service such an element calls.
- It does NOT cover configuration web services (C# endpoints Creatio exposes); for those read
  `configuration-webservice`.
- Availability: the five tools exist only in a clio that lists `register-web-service`. They are NOT in
  `tools/list`: read their contract with `get-tool-contract` and call them through `clio-run`
  (`{"command": "register-web-service", "args": {...}}`). If `get-tool-contract` does not know
  `register-web-service`, tell the user the registration has to be done in Studio > Web services and stop; do not
  fall back to raw DataService writes.

Before registering
1. Call `list-web-services` and reuse a service whose `base-uri` already points at the same API. A second service for
   the same API is a maintenance burden the user did not ask for.
2. Ask the user for what you cannot infer: the base URI, the methods the process needs, the authentication
   type, and the package that should own the service. Never guess a credential.
3. The service code MUST carry the environment's schema-name prefix, the `SchemaNamePrefix` system setting
   (`get-schema-name-prefix`), for example `UsrErpOrders`, not `ErpOrders`.
   The tool refuses an unprefixed code and a code another service already uses, because the platform save
   would silently overwrite that service. To change an existing service use `modify-web-service`.
4. Every method code and every parameter code MUST carry the same prefix: `UsrGetOrders`, `UsrOrderId`,
   `UsrStatus`, nested items included. Studio validates these codes against `SchemaNamePrefix` whenever it is
   set, so a method without it opens in Studio > Web services but cannot be saved there, and the only way out
   is a rename that breaks every process calling the method. The tool refuses unprefixed codes, all of them
   in one answer. With an empty `SchemaNamePrefix` there is no prefix rule, as in Studio.

Credential rule (MUST)
- No credential value ever passes through the agent. Basic and Digest authentication take
  `username-sys-setting` and `password-sys-setting` codes only; OAuth 2.0 takes `oauth-app`, the name or the
  id of an existing OAuth application. The tool resolves it to the application's id, stores that id (the reply
  shows it as `oauth-app-id`, which `setAuth` accepts back) and refuses one no application carries. The application itself, with its
  client id and secret, is created by the user; nothing else is needed on the service side.
- The platform sends Digest as a preemptive `Authorization: Basic` header, with no challenge and response
  (measured): an API that accepts only Digest refuses every call, and no service setting fixes that; and Digest
  protects nothing more than Basic, so on an `http` base URI the password travels readable. Tell the user.
- A header, a cookie, or any parameter whose code or `path` contains `key`, `token`, `secret`, `passw`, `pwd`,
  `auth`, `credential`, `signature`, `session`, `cookie`, `licence`, `license`, `nonce`, `passcode`, `passphrase`, `bearer` or
  `jwt` anywhere, or `pass` not followed by a lowercase letter and not after `com`, `by` or `sur` (`AdminPass`,
  `Pass_1`, `UsrPassId`, `PASSPORT_NO`; not `compass`, `bypass`, `surpass`, `passport` or `passenger`), in any
  letter case (so `author` and `keyword` count too), MUST NOT get
  `default-value`. Give it `default-sys-setting` with a system-setting code instead. The
  tool refuses a constant there and never echoes the refused value. A query parameter whose code or `path` is
  `sig`, `sas`, `code`, `appid`, `sign` or `hmac` follows the same rule when its constant is 16 characters or
  longer (an OpenWeatherMap `appid`, an Azure Functions `code`); a short value such as `code=USD` stays. So does
  a path parameter whose `{placeholder}` is the whole value of such a key in the `uri` (`search?api_key={k}`):
  describe hides its constant, which is kept when no default is sent while the `uri` keeps that key;
  `default-value ""` clears it, and a `uri` that would show it is refused.
- Every system setting a call references anew MUST exist before the call; such a reference to a missing
  setting is refused, because at run time the platform drops it silently and the request goes out without the
  header. A reference the same method (or the authentication) already had, whose setting was deleted since,
  comes back as a notice. Create the
  setting first with `create-sys-setting` and no value, `value-type-name` SecureText for a key, token or
  password, and let the user fill it (`get-guidance name=sys-settings`).
- An API key the built-in authentication types do not cover (a key header, a key query parameter, a Bearer
  token) is an ordinary request parameter with `default-sys-setting`. The value cannot be composed: for
  `Authorization: Bearer <token>` the setting holds the whole `Bearer <token>` text. A request that must be
  signed per call (HMAC and similar) cannot be described this way; tell the user.
- `describe-web-service` never returns credential values or secret-like constant defaults
  (`default-masked: true`, `constant-credentials-masked: true`). Do not try to read them another way. A key
  kept in the query of a base URI, a method `uri` or a `soap-action` (a service made in Studio can hold one) shows
  as `[masked]`, and the tools never write such a key back: a `setMethod` or `setBaseUri` that sends `[masked]` is
  refused. Leave that method or base URI out of the call, make the key a parameter with `default-sys-setting`
  the user fills (REST), or ask the user to change it in Studio. A `soap-action` with such a key is kept when
  `setMethod` leaves `soap-action` out.

Registering
- `register-web-service` arguments: `environment-name`, `package-name`, `code`, `base-uri`, `methods`
  (required); `caption`, `retry-count`, `auth` (optional).
- `base-uri` is an absolute http(s) URI without credentials, a query or a fragment; the tool adds the trailing
  slash the designer needs, because Studio joins base URI and method URI verbatim. A `{placeholder}` in it is
  filled from a path parameter like one in a method `uri`. A method `uri` may carry a fixed query
  (`search?format=json`), but a key there is refused: any value under a credential name, and a value of 16 or
  more characters under `sig`, `sas`, `code`, `appid`, `sign` or `hmac` (`code=USD` stays), with `&` or `;`
  between the pairs. Make it a query parameter with `default-sys-setting`.
- Each method: `code` (prefixed; what a Call web service element stores), `http-method` (GET, POST, PUT,
  PATCH, DELETE), `uri` relative to the base, a path parameter written as `{<its path>}`, its wire name;
  optional `caption`, `timeout-ms` (default 5000), `content-type` (json or xml, default json), `use-auth`,
  `request-parameters`, `response-parameters`.
- `use-auth` says whether the method sends the service authentication. Left out, it is true when the service
  has authentication once the whole call is applied (a `setAuth` in the same `modify-web-service` call counts,
  whatever its position), false otherwise.
- Each parameter: `code` (prefixed), `in` (path, query, header, body or cookie for a request; body, header
  or cookie for a response; default body), `type` (Text, Integer, Float, Boolean, DateTime, Date, Time, Guid,
  Object; default Text), `path`, `array`, `required`, `default-value` or `default-sys-setting`, and `items` for an
  Object array. Give `in` on every top-level request parameter of a GET method: one without it is a body
  parameter and is refused. Fields inside an Object array (`items`) take no `in`.
- Object is ONLY an array (`"type": "Object", "array": true`, its element fields in `items`), for a list of
  records such as order lines. A single nested object is NOT an Object parameter: describe each of its fields
  as its own parameter with a dotted `path`, the way Studio's quick setup does - request body
  `{"code": "UsrCustomerName", "path": "customer.name"}` and `{"code": "UsrCustomerEmail", "path":
  "customer.email"}` go out as one `{"customer": {"name": ..., "email": ...}}`; a response value reads
  `$.customer.name`. The tool refuses a new single Object: Studio cannot save one and a Call web service element
  gets a parameter with no fields to bind. One the service already has is kept as it is.
- `path` is the name the API sees: the query, header, cookie or body key (dotted for a field of a nested
  object), the `{placeholder}` of a path parameter, or the JSONPath of a response value (`$.status`; a field
  inside an Object array uses its key relative to the element, `number`). The code carries the prefix and is NOT the wire name, so on an environment with a
  prefix every new parameter MUST give `path`: without it `UsrPage` would go out as `?UsrPage=` and a
  response `UsrStatus` would read a key the API never sends, with no error. The tool refuses a new prefixed
  parameter without `path`. Example: `{"code": "UsrApiKey", "in": "header", "path": "X-Api-Key",
  "default-sys-setting": "UsrErpApiKey"}`.
- A response value carried in a header or a cookie is a response parameter with `in: header` or `in: cookie`
  and the header or cookie name in `path`. The platform matches a response header name EXACTLY as the API
  sends it, letter case included: `X-Request-Id` reads nothing when the API sends `x-request-id` (measured;
  many APIs send lowercase). Ask the user or check the API documentation for the exact spelling.
- Every `{placeholder}` in `uri` needs a path parameter whose `path` is that name, letter case included (the
  platform substitutes it case-sensitively), and the reverse; path parameters are required by default.
- Body rules Studio enforces, and the tool refuses the same: a GET method has no body request parameters,
  and an `xml` method has no body parameters in its request or its response (the platform builds a REST
  body as JSON only, so such a call fails). An XML API is registered with its path, query, header and cookie
  parameters only; a process then sends the XML text through the Call web service element's `RequestBody`
  and reads the raw `Response`.
- An unknown argument is refused with its name (at the top level also with the list of valid fields); fix the
  name rather than dropping it.
- The reply returns the saved service as Studio stores it. Its method and parameter codes are the ones a
  process element maps, so keep the reply for the process step.

After registering — tell the user (the tool also returns these as notices)
- They fill the values of the referenced system settings in System settings. Until then, calls fail or go
  out without credentials.
- To ship the setting values with the package, the settings have to be bound to the package data
  (`get-guidance name=data-bindings`). The tool does not do it.
- A process that calls the service from another package needs a dependency on the service package; when the
  process tools build it, `process-web-service` owns that rule.

Testing
- `test-web-service-method` sends ONE real request, as Studio's Send test request does, and returns the
  HTTP status, the response body (cut at 20,000 characters) and `response-values`: the response parameters as
  the platform parsed them, by code, so you can check that a response `path` reads what the process needs. It is
  absent when the platform returns none, and too long ones give `response-values-omitted: true` instead. Request and response headers are never
  returned whole. Its arguments are `code` (the service) and `method` (the method code), not `service-code` or
  `method-code`. `notices` flags a definition problem: a response parameter with a JSONPath on an XML response,
  which reads nothing. Values the request carried from system settings (path parameters included) or in `Authorization`,
  the secret constant defaults describe hides and the keys kept in the stored URIs' queries, are replaced with
  `[masked]` in the body and the error
  (`sent-secrets-masked: true`), because an API may echo them. In a JSON body every value and every property name
  carrying a secret is masked (a number or boolean whole), and the other keys and values stay readable; a masked
  JSON body comes back re-indented. Values shorter than four characters are not masked. When a value
  filled from a system setting cannot be found in what the platform reports as sent (a path value not found in
  the URL, a body value or an array item field missing from the body, nothing reported for a failed call, or a
  request body that is neither JSON nor XML), the response is withheld and only the status code comes back. Response values read from a cookie, a credential-named header, or with a
  setting or secret default are masked too.
- A REST method with `content-type` xml cannot be test-called: the platform refuses it and nothing is sent; a process sends its XML body through the Call web
  service element's `RequestBody`.
- `parameters` maps top-level request parameter codes to values: text, a number or a boolean for a scalar; a
  JSON array of them for a body array (`["a", "b"]`); and for an Object array a JSON array of objects keyed by the
  item's field codes (`[{"UsrSku": "A1", "UsrQty": 2}]`), where a field that is itself an array takes a JSON
  array the same way. A path, query, header or cookie value is one text even when the parameter is marked as an
  array: give a list as `"1,2"`. A scalar field left out of an item, or null, gets its default, and a
  system-setting default is filled in every item; an array left out, at any level, is not sent at all. A code the
  method does not have (at any level), a field given twice and a value of the wrong shape are refused with their
  location (`parameters.UsrLines[0].UsrSku`; for an unknown code also the valid codes), and nothing is sent. A single Object parameter takes no value. A clio that refuses every array with "one text,
  number or boolean value per top-level parameter" predates array values.
  A scalar body field given no value goes out as `null`, not left out (measured), so a strict API may reject it.
- The request reaches the external API. Before test-calling any method but a REST GET (POST, PUT, PATCH, DELETE,
  a method with no known verb, and every SOAP method), ALWAYS tell the user it sends a real request that can change
  data in that API and wait for their explicit yes, even when they asked you to "test" it: a request to test is not
  a confirmation. Only after the yes, call with `user-confirmed: true`; never set it on your own. A yes covers that
  one call: ask again before calling with other values or another method. Without it the tool refuses such a call
  with `confirmation-required: true` and sends nothing. A REST GET needs no question. A clio
  that answers `user-confirmed` as an unknown argument predates the check: still ask first, then call without it.
- A call returning 401 or 403: first check in `describe-web-service` that the method has `use-auth: true` when
  the service has authentication. If it does, the system-setting values are usually still empty: an empty setting
  sends no header or parameter at all, without any error. Say so instead of changing the service.

Changing a registered service
- Read it first with `describe-web-service`. Add `usages: true` to list the processes that call it; the scan
  reads every process schema and takes seconds to minutes on a large environment.
- `modify-web-service` takes `code`, `operations` and optional `acknowledge-usages`. Operations apply in
  order, all or nothing: `addMethod` (`method`), `setMethod` (`method`; replaces the method of the same code,
  keeping the UIds of parameters it keeps), `removeMethod` (`method-code`), `setBaseUri` (`base-uri`),
  `setRetryCount` (`retry-count`), `setAuth` (`auth`; `type` none, basic, digest or oauth20, and none removes the
  authentication); a SOAP service also takes `setNamespaces` and `setType` (`web-service-soap`). Missing settings
  follow the credential rule above. Each operation names its kind in `op`:
  `"operations": [{"op": "addMethod", "method": {...}}, {"op": "setAuth", "auth": {"type": "oauth20", "oauth-app": "Erp"}}]`.
- What `describe-web-service` returns can be sent back as it is (except a method whose `{placeholder}` differs
  in letter case from its path parameter; the platform never fills it, so fix the case; and a `uri`, `soap-action`
  or base URI that shows a `[masked]` key, which is refused, see above): a method (with its parameters) as the
  `method` of `setMethod`, and the `auth` block as the `auth` of `setAuth`. Read-only fields
  (`default-masked`, `constant-credentials-masked`) are accepted and ignored, and `oauth-app-id` is taken
  like `oauth-app`. A kept parameter whose secret constant default describe hid (`default-masked: true`)
  keeps that default when no default is sent; the agent never needs, and never gets, the value. To remove
  such a constant send `"default-value": ""`, or replace it with `default-sys-setting`; moving it to a place
  where it would show (a query parameter, a path that does not look like a credential) is refused.
- `setMethod` REPLACES the method. Every parameter it keeps must be sent complete, as `describe-web-service`
  returned it: an omitted field is not kept but reset (`in` to body, `type` to Text, `array` and `required` to
  false, the caption to the code, and an omitted `default-sys-setting` is dropped). Only `path` and a hidden
  secret constant carry over. To add one parameter, send the whole method from describe plus the new one.
- `setAuth` changes the service authentication only. Methods the call does not touch keep their `use-auth`, and
  a method with `use-auth: false` sends no authentication at all (measured on SOAP 1.2; REST takes the same code
  path). Which methods send the authentication is the user's decision: before or after `setAuth`, ASK the user about
  each method with `use-auth: false` (the tool names them in a notice after a change of the authentication); never
  set `use-auth` on your own, not even in the same call as `setAuth`. Only on their yes, send that method with
  `setMethod` and `use-auth: true` (the usage guard applies).
- One exception: an `auth` block with `constant-credentials-masked: true` (Basic or Digest credentials typed
  in Studio as constants, both or only one of them) cannot be sent back; `setAuth` needs BOTH
  `username-sys-setting` and `password-sys-setting` codes, and the user fills those settings.
- USAGE GUARD: `setMethod` and `removeMethod` on a method that a process calls are REFUSED, with the
  processes and elements listed. A removed method, or a removed or renamed parameter, leaves those elements
  with mappings that point at nothing, and they fail at run time with nothing to warn the user. Show the list to
  the user; only on their explicit yes, repeat the call with `acknowledge-usages` listing the `process` codes
  from the refusal (not the captions), exactly those. Never fill `acknowledge-usages` on your own. A process
  that names the service but whose metadata cannot be read (or that names it outside a Call web service
  element) is listed with no element and counts as calling every method.
- `setBaseUri` that really changes the base URI changes every call those processes make, and `setAuth` that
  really changes the authentication changes every call of the methods with `use-auth: true`, so both are refused
  the same way while a process calls such a method; the refusal says what changes ("the new base URI changes every
  call ..."), and the elements keep their parameters. The tool compares the service before and after the whole call:
  values sent back unchanged (as `describe-web-service` returned them, a `[masked]` key aside) are not a change. `setRetryCount` is never
  guarded. A clio whose `modify-web-service` contract does not say that `setBaseUri` and `setAuth` are refused
  while a process calls the service predates this check and saves them without a refusal. With such a clio, before
  the call list the service's callers with `describe-web-service` `usages: true` and ask the user first.
- `addMethod` never breaks a caller and is not guarded. Adding a parameter to a method a process already calls
  is a `setMethod` and is guarded; after the user agrees, the new parameter appears on that element at its next
  load, unbound, and until it is bound a body field goes out as `null` (measured), so bind it or tell the user.
- Codes the service already has stay as they are, with or without the prefix: a service registered before
  the prefix rule, or shipped by Creatio, keeps them. NEVER rename a method or parameter only to add the
  prefix - the rename is exactly the change that breaks its callers. In `setMethod`, send the existing
  method and kept parameters under their exact current codes (letter case included); only methods and
  parameters the call adds follow the prefix and `path` rules. A kept parameter sent without `path` keeps its
  stored wire name.

Rights and limits
- Every tool except `list-web-services` needs the CanManageSolution operation right; without it the call is
  refused before anything is read.
- There is no delete tool. Removing a service is done in Studio > Web services.
- Evidence: the tools reuse the platform contracts the Studio web service designer calls
  (`ServiceSchemaRequest`, `ServiceDataContract`, `CheckUniqueSchemaName`, `CallServiceSchemaService`).
  Measured on Creatio 10.2 (.NET Framework, MSSQL) in November 2026: register, describe, modify, the
  credential refusal, a SecureText key sent as a header, its masking in an echoed response, and the usage
  guard against a process that calls a stock service; the usage scan read
  about 1,300 process schemas in about 15 seconds. On a .NET Framework stand with prefix `Usr`: a method
  whose codes lacked the prefix could not be saved in Studio, a prefixed one saved unchanged, and a process
  built on a registered service called it and received the response (HTTP 200). Request body fields with dotted
  paths went out as one nested object, a dotted response JSONPath filled the element output, and a
  setting-bound body field was masked in the test-call reply. A test call with a text array and an Object
  array whose items held a text array and an inner Object array went out with every value in place, in a REST
  JSON body and in a SOAP envelope, and a setting-bound item field was filled in every item; the platform sent a
  JSON array given to a query, header or path parameter as `System.Object[]` (so clio refuses one) and a text as
  written. Other versions are unknown.

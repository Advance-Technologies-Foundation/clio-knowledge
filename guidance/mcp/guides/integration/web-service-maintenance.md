clio MCP web service maintenance guide — test-call and change a registered REST or SOAP service

Scope and ownership
- Use this guide to test-call a method of a web service registered in Studio > Web services, or to change such a
  service. It owns `test-web-service-method` (one real request, asked for first) and `modify-web-service` (its
  operations, `setMethod` as a full replacement, `setAuth` and `use-auth`, and the usage guard).
- `web-service-registration` owns registering, `list-web-services`, `describe-web-service` and the credential
  rule, which every change here follows; read it first. For a SOAP service also read `web-service-soap`.
- Both tools need clio 8.1.0.150 or later and are NOT in `tools/list`: read their contract with
  `get-tool-contract` and call them through `clio-run`. If `get-tool-contract` does not list
  the tool, tell the user the test call or change is done in Studio > Web services and stop.
- Every tool here needs the CanManageSolution operation right; without it the call is refused before anything
  is read.

Testing
- `test-web-service-method` sends ONE real request, as Studio's Send test request does, and returns the
  HTTP status, the response body (cut to fit the reply, `response-body-truncated: true`) and `response-values`: the
  response parameters as
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
  setting or secret default are masked too. For the same reason, in a process that calls a web service or reads a
  SecureText setting (the platform decrypts it into a parameter), `run-process` returns every text, Guid or collection
  result as `[withheld]`, named in `withheldResultParameters`; flags, numbers and dates (`Success`, `StatusCode`) come
  back. Map a SecureText setting only into element inputs, never copy a response or a key elsewhere to read it: the
  user sees the values in the process log.
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
- A test call on an OAuth 2.0 method that comes back withheld with status 0, no body and an error naming the
  OAuth application usually means the access token could not be retrieved (or the API host is unreachable).
  That is the OAuth application, not the service: ask the user to check its
  client id and secret, scope and token URL, and, when the token endpoint refuses client credentials in a Basic
  header, to set "Send client credentials" to "In request body" (measured: the default Basic header failed every
  call where the request body worked). Studio's test request on the same method shows the platform's error.

Changing a registered service
- Read it first with `describe-web-service`. Add `usages: true` to list the processes that call it; the scan
  reads every process schema and takes seconds to minutes on a large environment. For a service with many methods
  pass `methods` with the codes you change: a method listed only in `methods-not-shown` was not shown, so read it
  before a `setMethod`. The reply of `modify-web-service` keeps the methods the call built in full first.
- `modify-web-service` takes `code`, `operations` and optional `acknowledge-usages`. Operations apply in
  order, all or nothing: `addMethod` (`method`), `setMethod` (`method`; replaces the method of the same code,
  keeping the UIds of parameters it keeps), `removeMethod` (`method-code`), `setBaseUri` (`base-uri`),
  `setRetryCount` (`retry-count`), `setAuth` (`auth`; `type` none, basic, digest or oauth20, and none removes the
  authentication and clears `use-auth` on every method), `setDescription` (`description`; "" clears it); a SOAP
  service also takes `setNamespaces` and `setType` (`web-service-soap`). Credentials and missing settings
  follow the credential rule of `web-service-registration` (MUST: a credential is a system-setting reference,
  never a value, and every setting a call references anew must exist first). Each operation names its kind in `op`:
  `"operations": [{"op": "addMethod", "method": {...}}, {"op": "setAuth", "auth": {"type": "oauth20", "oauth-app": "Erp"}}]`.
- What `describe-web-service` returns can be sent back as it is (except a method whose `{placeholder}` differs
  in letter case from its path parameter; the platform never fills it, so fix the case; and a `uri`, `soap-action`
  or base URI that shows a `[masked]` key, which is refused by the credential rule): a method (with its parameters)
  as the
  `method` of `setMethod`, and the `auth` block as the `auth` of `setAuth`. Read-only fields
  (`default-masked`, `constant-credentials-masked`) are accepted and ignored, and `oauth-app-id` is taken
  like `oauth-app`. A kept parameter whose secret constant default describe hid (`default-masked: true`)
  keeps that default when no default is sent; the agent never needs, and never gets, the value. To remove
  such a constant send `"default-value": ""`, or replace it with `default-sys-setting`; moving it to a place
  where describe would show it (a query or body parameter, or a code or path where neither the name nor the value
  hides it) is refused.
- `setMethod` REPLACES the method. Every parameter it keeps must be sent complete, as `describe-web-service`
  returned it: an omitted field is not kept but reset (`in` to body, `type` to Text, `array` and `required` to
  false, the caption to the name the API sees, and an omitted `default-sys-setting` is dropped). Only `path` and a hidden
  secret constant carry over. To add one parameter, send the whole method from describe plus the new one.
- `setAuth` changes the service authentication only. Methods the call does not touch keep their `use-auth` (except
  that removing the authentication, type none, clears it on every method and a notice names them: if
  authentication comes back later, each of those methods needs `use-auth: true` again, by the same rule below), and
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

Evidence
- The tools reuse the platform contracts the Studio web service designer calls
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
  written. On the same stand in November 2026: a plain `Accept-Language: en-US` header constant was registered,
  shown by describe and echoed readable by an echo API, while a key-like constant stored the Studio way under the
  neutral header `X-Partner` was hidden by describe, masked in the echo, and refused when sent back as a constant.
  Other versions are unknown.

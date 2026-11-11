clio MCP web service registration guide — REST services in Studio > Web services

Scope and ownership
- Use this guide when an external REST API must be registered in Creatio (Studio > Web services) so a
  business process can call it, or when a registered REST service must be read, changed or test-called.
- This guide owns `register-web-service`, `list-web-services`, `describe-web-service`,
  `modify-web-service` and `test-web-service-method`: their arguments, the credential rule, the usage guard
  and the follow-up steps the user owns.
- It does NOT own the Call web service process element. The process element catalog
  (`get-guidance name=process-element-catalog`) owns whether a `webService` element can be built; this guide
  only produces the service such an element calls.
- It does NOT cover configuration web services (C# endpoints Creatio exposes); for those read
  `configuration-webservice`.
- Availability: the five tools exist only in a clio that lists `register-web-service`. If
  `get-tool-contract` does not know it, tell the user the registration has to be done in Studio > Web
  services and stop; do not fall back to raw DataService writes.

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
- A header, a cookie, or any parameter whose code or `path` looks like a key, token, secret, password,
  signature or session MUST NOT get `default-value`. Give it `default-sys-setting` with a system-setting code instead. The
  tool refuses a constant there and never echoes the refused value.
- Every referenced system setting MUST exist before the call; a reference to a missing setting is refused,
  because at run time the platform drops it silently and the request goes out without the header. Create the
  setting first with `create-sys-setting` and no value, `value-type-name` SecureText for a key, token or
  password, and let the user fill it (`get-guidance name=sys-settings`).
- An API key the built-in authentication types do not cover (a key header, a key query parameter, a Bearer
  token) is an ordinary request parameter with `default-sys-setting`. The value cannot be composed: for
  `Authorization: Bearer <token>` the setting holds the whole `Bearer <token>` text. A request that must be
  signed per call (HMAC and similar) cannot be described this way; tell the user.
- `describe-web-service` never returns credential values or secret-like constant defaults
  (`default-masked: true`, `constant-credentials-masked: true`). Do not try to read them another way.

Registering
- `register-web-service` arguments: `environment-name`, `package-name`, `code`, `base-uri`, `methods`
  (required); `caption`, `retry-count`, `auth` (optional).
- `base-uri` is an absolute http(s) URI without credentials; the tool adds the trailing slash the designer
  needs, because Studio joins base URI and method URI verbatim.
- Each method: `code` (prefixed; what a Call web service element stores), `http-method` (GET, POST, PUT,
  PATCH, DELETE), `uri` relative to the base, a path parameter written as `{<its path>}`, its wire name;
  optional `caption`, `timeout-ms` (default 5000), `content-type` (json or xml), `use-auth`,
  `request-parameters`, `response-parameters`.
- Each parameter: `code` (prefixed), `in` (path, query, header, body or cookie for a request; body, header
  or cookie for a response), `type` (Text, Integer, Float, Boolean, DateTime, Date, Time, Guid, Object), `path`, `array`,
  `required`, `default-value` or `default-sys-setting`, and `items` for an Object array.
- Object is ONLY an array (`"type": "Object", "array": true`, its element fields in `items`), for a list of
  records such as order lines. A single nested object is NOT an Object parameter: describe each of its fields
  as its own parameter with a dotted `path`, the way Studio's quick setup does - request body
  `{"code": "UsrCustomerName", "path": "customer.name"}` and `{"code": "UsrCustomerEmail", "path":
  "customer.email"}` go out as one `{"customer": {"name": ..., "email": ...}}`; a response value reads
  `$.customer.name`. The tool refuses a single Object: Studio cannot save one and a Call web service element
  gets a parameter with no fields to bind.
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
- Every `{placeholder}` in `uri` needs a path parameter whose `path` is that name, and the reverse; path
  parameters are required by default.
- Body rules Studio enforces, and the tool refuses the same: a GET method has no body request parameters,
  and an `xml` method has no body parameters in its request or its response (the platform builds a REST
  body as JSON only, so such a call fails). An XML API is registered with its path, query, header and cookie
  parameters only; a process then sends the XML text through the Call web service element's `RequestBody`
  and reads the raw `Response`.
- An unknown argument is refused with the list of valid fields; fix the name rather than dropping it.
- The reply returns the saved service as Studio stores it. Its method and parameter codes are the ones a
  process element maps, so keep the reply for the process step.

After registering — tell the user (the tool also returns these as notices)
- They fill the values of the referenced system settings in System settings. Until then, calls fail or go
  out without credentials.
- To ship the setting values with the package, the settings have to be bound to the package data
  (`get-guidance name=data-bindings`). The tool does not do it.
- A process saved in a package other than the service package needs a dependency on the service package
  (`add-package-dependency`) before that process package is exported.

Testing
- `test-web-service-method` sends ONE real request, as Studio's Send test request does, and returns the
  HTTP status and the response body (cut at 20,000 characters). Request and response headers are never
  returned. Values the request carried from system settings or in `Authorization` are replaced with
  `[masked]` in the body and the error (`sent-secrets-masked: true`), because an API may echo them. In a JSON
  body every value and every property name carrying a secret is masked (a number or boolean whole), and the
  other keys and values stay readable; a masked JSON body comes back re-indented.
  Values shorter than four characters and path parameters are not masked.
- The request reaches the external API. For POST, PUT, PATCH or DELETE, confirm with the user first: it
  changes data on their side.
- A GET returning 401 or 403 usually means the system-setting values are still empty: an empty setting
  sends no header or parameter at all, without any error. Say so instead of changing the service.

Changing a registered service
- Read it first with `describe-web-service`. Add `usages: true` to list the processes that call it; the scan
  reads every process schema and takes seconds to minutes on a large environment.
- `modify-web-service` takes `code`, `operations` and optional `acknowledge-usages`. Operations apply in
  order, all or nothing: `addMethod`, `setMethod` (replaces the method of the same code, keeping the UIds of
  parameters it keeps), `removeMethod` (`method-code`), `setBaseUri`, `setRetryCount`, `setAuth`. Only REST
  services can be changed; a SOAP service stays in Studio.
- USAGE GUARD: `setMethod` and `removeMethod` on a method that a process calls are REFUSED, with the
  processes and elements listed. Those elements would lose their parameters on the next designer load and
  fail at run time, with nothing to warn the user. Show the list to the user; only on their explicit yes,
  repeat the call with `acknowledge-usages` naming exactly those processes. Never fill `acknowledge-usages`
  on your own.
- `addMethod` never breaks a caller and is not guarded. An existing element sees new parameters only after
  it is reopened in the designer.
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
  setting-bound body field was masked in the test-call reply. Other versions are unknown.

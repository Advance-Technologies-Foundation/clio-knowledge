clio MCP web service registration guide — REST and SOAP services in Studio > Web services

Scope and ownership
- Use this guide when an external REST or SOAP API must be registered in Creatio (Studio > Web services) so a
  business process can call it, or when a registered service must be read. For a SOAP
  service also read `get-guidance name=web-service-soap`: it owns what differs (type, namespaces, message element,
  SOAP action, element paths, SOAP header, `setNamespaces`, `setType`); everything here applies to SOAP too.
- This guide owns `register-web-service`, `list-web-services` and `describe-web-service`: their arguments, the
  credential rule and the follow-up steps the user owns. Test-calling a method (`test-web-service-method`, a real
  request the user agrees to first) and changing a registered service (`modify-web-service` and its usage guard)
  are owned by `get-guidance name=web-service-maintenance`: read it before either.
- Version boundary, before anything else: the web service tools (these three and the two of
  `web-service-maintenance`) need clio 8.1.0.150 or later. If `get-tool-contract`
  does not list `register-web-service`, the clio serving you is older: tell the user to update clio, or that the
  service is registered in Studio > Web services, and stop.
- It does NOT own the Call web service process element: `get-guidance name=process-web-service` owns the
  element, and the process element catalog (`get-guidance name=process-element-catalog`) owns whether it can be
  built. This guide only produces the service such an element calls.
- It does NOT cover configuration web services (C# endpoints Creatio exposes); for those read
  `configuration-webservice`.
- Availability: the web service tools exist only in a clio that lists `register-web-service`. They are NOT in
  `tools/list`: read their contract with `get-tool-contract` and call them through `clio-run`
  (`{"command": "register-web-service", "args": {...}}`). If `get-tool-contract` does not know
  `register-web-service`, tell the user the registration has to be done in Studio > Web services and stop; do not
  fall back to raw DataService writes.

Before registering
1. Call `list-web-services` and reuse a service whose `base-uri` already points at the same API. A second service for
   the same API is a maintenance burden the user did not ask for. `filter` keeps the services whose code, caption or
   package contains a text (`Erp`); a list too long to return inline is cut, with a notice to narrow it. A clio that
   answers `filter` as unknown predates it: list without it.
2. Ask the user for what you cannot infer: the base URI, the methods the process needs, the authentication
   type, and the package that should own the service. Never guess a credential.
3. The service code MUST carry the environment's schema-name prefix, the `SchemaNamePrefix` system setting
   (`get-schema-name-prefix`), for example `UsrErpOrders`, not `ErpOrders`.
   The tool refuses an unprefixed code and a code another service already uses, because the platform save
   would silently overwrite that service. To change an existing service use `modify-web-service`
   (`web-service-maintenance`).
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
- A cookie, or any parameter whose code or `path` contains `key`, `token`, `secret`, `passw`, `pwd`,
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
- A header parameter, and a SOAP `soap-header` element, follows the name rule above (so `Authorization`, an
  `auth` name, always takes `default-sys-setting`) and one more: a constant whose value looks like a key is
  refused too, and describe hides one Studio stored. The value is split at spaces, commas, semicolons and `&`. It
  is a key when it starts with an authentication scheme (`Basic`, `Bearer`, `Token`...) followed by anything, or
  when a part, without quotes, base64 padding and a leading `name=`, is a credential pair (`password=...`, or
  `sig=`/`code=`... with 16 or more characters), 16 or more characters mixing letters and digits (a generated key;
  also a GUID, an ISO timestamp, a build tag such as `4.18.2-build20240101` or a multipart boundary), 24 or more
  letters, 12 or more digits, or a password-shaped word (8 or more characters with upper and lower case, a digit
  and one of `!@#$%^&*?~`); an address is one when it has user information, or when a path segment (a webhook
  token), a query or a fragment value is one. Words, short numbers and short versions joined by `/ . + - _` stay
  plain. Any other value is an ordinary constant, as Studio allows it: `Accept-Language: en-US`,
  `X-Api-Version: 2`, `Content-Type: application/xml`, `Cache-Control: max-age=3600`,
  `Accept: application/vnd.github.v3+json`, a `User-Agent`, a host name or `production-eu-west-1` are written,
  shown by describe and left readable in a test-call echo. The rule is a deny-list: a short chosen secret under a
  neutral name (`X-Pin: 4821`, `Winter-2024`) is not recognised, so the tool accepting a value does not make it
  public: a key, password or PIN the user gives, short or not, still goes into a system setting. A refusal that
  says "refused on a header, cookie or
  credential-like parameter" comes from a clio released before this rule, which refuses every header constant:
  tell the user to update clio instead of moving a plain value into a system setting. A refusal that says the
  value "looks like a key" comes from this rule; then use `default-sys-setting`.
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
- `describe-web-service` never returns credential values or secret-like constant defaults by the rules above
  (`default-masked: true`, `constant-credentials-masked: true`); a plain header constant is returned as is. Do not try to read them another way. A key
  kept in the query of a base URI, a method `uri` or a `soap-action` (a service made in Studio can hold one) shows
  as `[masked]`, and the tools never write such a key back: a `setMethod` or `setBaseUri` that sends `[masked]` is
  refused. Leave that method or base URI out of the call, make the key a parameter with `default-sys-setting`
  the user fills (REST), or ask the user to change it in Studio. A `soap-action` with such a key is kept when
  `setMethod` leaves `soap-action` out.

Registering
- `register-web-service` arguments: `environment-name`, `package-name`, `code`, `base-uri`, `methods`
  (required); `caption` (default: the code), `description`, `retry-count`, `auth` (optional). A clio that answers
  `description` or a `setDescription` op as unknown predates them, and with them the default parameter caption
  (there: the code) and `setAuth` none clearing `use-auth`: leave `description` out, and do not tell the user
  about those three behaviours.
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
  whatever its position), false otherwise. A service registered without authentication keeps no method with
  `use-auth: true` (a notice names any it cleared).
- Each parameter: `code` (prefixed), `in` (path, query, header, body or cookie for a request; body, header
  or cookie for a response; default body), `type` (Text, Integer, Float, Boolean, DateTime, Date, Time, Guid,
  Object; default Text), `path`, `array`, `required`, `default-value` or `default-sys-setting`, and `items` for an
  Object array. Give `in` on every top-level request parameter of a GET method: one without it is a body
  parameter and is refused. Fields inside an Object array (`items`) take no `in`. A parameter sent without
  `caption` is captioned with the name the API sees (a body path's last field, a SOAP path's last element, a query,
  header, cookie or path name as given), or its code when it has no `path`.
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

Reading a registered service
- `describe-web-service` (`code`; optional `usages`, `methods`) returns the base URI, the authentication and every
  method with its parameter trees, masked by the credential rule above. `usages: true` also lists the processes and
  Call web service elements that call it (it reads every process schema: seconds to minutes). `methods` (method
  codes) returns only those methods; a code the service does not have is refused with the codes it has. A reply too
  large to return inline (also from register and modify) gives as many methods in full as fit and lists the rest by
  code, with their parameter counts, in `methods-not-shown`: read those with `methods` before you use or send one
  back. A clio that answers `methods` as unknown predates it and returns every method.

After registering — tell the user (the tool also returns these as notices)
- They fill the values of the referenced system settings in System settings. Until then, calls fail or go
  out without credentials.
- To ship the setting values with the package, the settings have to be bound to the package data
  (`get-guidance name=data-bindings`). The tool does not do it.
- A process that calls the service from another package needs a dependency on the service package; when the
  process tools build it, `process-web-service` owns that rule.

Rights and limits
- Every tool except `list-web-services` needs the CanManageSolution operation right; without it the call is
  refused before anything is read.
- There is no delete tool. Removing a service is done in Studio > Web services.
- Evidence for every web service tool is in `web-service-maintenance` (kept there so this article fits one reply).

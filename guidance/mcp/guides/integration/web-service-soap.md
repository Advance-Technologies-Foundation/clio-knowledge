clio MCP SOAP web service guide — register and change a SOAP 1.1 or SOAP 1.2 service in Studio > Web services

Scope and ownership
- Read `web-service-registration` first: it owns the five web service tools, the credential rule, the prefix and
  `path` rules, the usage guard and the follow-up steps, and all of it applies to a SOAP service too. This guide
  owns only what differs for SOAP: reading a WSDL, the service type, the namespaces, the method's message element
  and SOAP action, element paths, the SOAP header, array items, and the two operations only a SOAP service has.
- Availability: SOAP registration exists only in a clio whose `register-web-service` contract
  (`get-tool-contract`) has the `type` and `namespaces` arguments. Without them, tell the user that a SOAP service is
  registered in Studio > Web services and stop. Reading and test-calling a SOAP service works as for REST. A
  test call whose reply has a status but a withheld body means a system-setting value sent in the envelope could
  not be located for masking; that is the masking rule, not a broken definition.
- From a WSDL (a URL or files the user gives): call `read-wsdl` first and register what it returns. Describe a
  service by hand only when there is no WSDL, or for what read-wsdl says it left out; never hand-build what it
  returned (the one exception, copying a method's own parameters back from `describe-web-service`, is below). Never
  guess an operation, an element name or a namespace. A clio whose `get-tool-contract` does not list
  `read-wsdl` has no WSDL reading: describe the service from the WSDL text or its documentation then.

Reading a WSDL (read-wsdl)
- Arguments: `environment-name` and either `wsdl-url` or `wsdl-files`; optional `port`, `operations`, and `code` or
  `service-code`. When the user named the code of a NEW service, pass it as `code`: the arguments then carry it, so
  they are registered as they are. `service-code` is ONLY for an existing SOAP service the operations go into (a
  service-code that names no service is refused); never both. It saves nothing. A `wsdl-url` is downloaded, with the
  schemas it imports by an absolute `schemaLocation`, by the stand through Studio's own endpoint; clio never fetches
  it. `wsdl-files` are absolute paths, on the machine clio runs on, of the WSDL and of every schema it imports; they
  are refused when clio is reached as an HTTP MCP server, where only `wsdl-url` works.
- Refused before anything is sent: a URL with `user:password@` or a key or token in its query, and a loopback,
  link-local or cloud metadata address (an advisory check: a name that only resolves to the stand itself gets past
  it). A network path in `wsdl-files` is not read. Ask the user to download the WSDL and its schemas themselves and
  pass them as `wsdl-files`. A failed download comes back with the platform's reason and the same advice.
- The result's `services` has one entry per SOAP 1.1 and SOAP 1.2 port (HTTP ports are dropped): `port`,
  `studio-pick` (the port Studio's quick setup would create, the SOAP 1.2 one when both exist; Studio's choice, not
  a recommendation), `arguments`, and `existing-methods` with `service-code`. `warnings` and `notices` are listed
  once for the whole result.
- Before registering, show the user the operations, and the ports when there are several. Register the port a
  warning names to register instead ("Register port ... instead"), else the `studio-pick` port, unless the user asks
  for another. When they want only some operations, read again with `operations` (and `port`) instead of editing
  the arguments.
- BEFORE `register-web-service`, show the user every warning and notice about the port you are about to register
  (its SOAP version, a base URI on another host or on localhost, a missing soap-action) and wait for their answer
  where one asks for it; never register first and warn afterwards.
- Register a port's `arguments` with `register-web-service` as they are, adding only `package-name` and what a
  warning asks the user for. Do not rename codes or rewrite paths: a response path comes with its prefix
  (`tem:AddResult`); keep it. Method and parameter codes are the prefix plus the full WSDL names in PascalCase, the
  same on every read: `UsrIntA`, `UsrAddResult`; a nested field joins its whole path (`customer/name` ->
  `UsrCustomerName`); an array adds the element that repeats (the array `lines` repeating inside `order` ->
  `UsrOrderLines`).
- Pass every warning on. Some ask for the user: a `base-uri` left out (the address failed the base-URI check, had a
  fragment, had spaces, control or non-ASCII characters, or was too long) needs the real endpoint from the user
  before registering; while a namespace is left out, `register-web-service` refuses the call if a request element
  uses its prefix, until the user gives the URI; a `soap-action` left out needs the right value from the API owner.
- A port warning says the platform may not be able to call that port as the WSDL describes it. When it says
  "Register port ... instead", tell the user and register that port. When it names the SOAP 1.1 port with a
  difference (another host, or http where the warned port is https), show the user its `base-uri` and register it
  only on their yes. When it names the SOAP 1.1 port only for a failed test call (a SOAP 1.2 port of a WSDL WCF did
  not write; an ASMX service answers SOAP 1.2), keep the SOAP 1.2 port and switch only if a test call fails
  because the action or operation is not found (WCF: `ActionNotSupported`, Action ''). Switch the registered
  service with `modify-web-service` `setType` soap11, plus `setBaseUri` when the SOAP 1.1 port's base-uri differs;
  `register-web-service` refuses a code already used. The platform sends a SOAP 1.2
  action only as a `SOAPAction` header, never as the `action` parameter of `Content-Type`, so a service that
  dispatches SOAP 1.2 by that parameter (WCF) answers every call with `ActionNotSupported` and Action ''; and it
  writes no WS-Addressing header, so a port that requires WS-Addressing (WCF `wsHttpBinding` with security mode
  None) answers `DestinationUnreachable` with To ''. A port that requires WS-Security message security (a default
  `wsHttpBinding`) cannot be called at all; ask the API owner for a plain endpoint. With no SOAP 1.1 port the
  warning gives the workaround, measured to work: for SOAP 1.2, a request header parameter (a prefixed code) whose
  `path` is `Content-Type` and whose value is `application/soap+xml;charset=utf-8;action="<the soap-action>"`, one
  per method; for WS-Addressing, the namespace the warning names (usually `http://www.w3.org/2005/08/addressing`)
  and two `soap-header` body parameters `wsa:Action` (the soap-action) and `wsa:To` (the base URI). A constant is
  refused on both, so their values come from a system setting (`default-sys-setting`) or from the process. Do this
  only with the user's agreement, and test a call.
- Notices: when the code is already used by a web service, ask the user which way to go and do not pick a code
  yourself: register under a code the user names (read again with that `code`), or change that service (read again
  with `service-code`). When the
  WSDL sends calls to another host than the one it came from, show the user the `base-uri` before registering.
- A result too large to return whole lists only the operation names, with a notice: ask the user which operations
  they need and read again with `operations` (and `port`). When even one operation of one port is too large, the
  notice says so: ask the user which of its fields they need and describe that method by hand.
- Operations into an existing SOAP service: read with `service-code`. `existing-methods` lists the codes of its
  methods that an operation matched (same root element and SOAP action); those operations keep the codes. Add the
  others with
  `modify-web-service` `addMethod`, and send the `namespaces` with `setNamespaces` when they changed. Replace an
  existing one with `setMethod` only when the user wants it updated: the arguments then carry what Studio set on it
  (timeout, authentication, each parameter's default and required flag), and a warning names its parameters the
  WSDL lacks, which `setMethod` would remove; copy those from `describe-web-service` or ask the user. The usage
  guard applies.

Registering a SOAP service
- `type`: `soap11` or `soap12` (default `rest`). REST and SOAP cannot be switched later.
- `namespaces` (at least one): `[{"prefix": "tem", "uri": "http://tempuri.org/"}]`. Exactly one is the default:
  the one marked `default: true`, else the first. Prefixes starting with `xml` and the prefix `soapenv` are
  reserved and refused; a namespace `uri` with `user:password@` is refused.
- `base-uri` is the endpoint URL as the WSDL's `soap:address` gives it (`http://www.dneonline.com/calculator.asmx`).
  It is stored as given: no trailing slash is added, and a `{placeholder}` is refused, because a SOAP method has no
  address of its own. A fixed query stays in it (SAP: `?sap-client=100`), since SOAP has no query parameters; a
  credential-like key with a constant value there is refused, and so is `?wsdl`, `?singleWsdl` or `?xsd` (a WCF
  `.svc?xsd=xsd0`): that is the WSDL or schema address, not the endpoint.
- Each method: `code` (prefixed) and `message-element`, the request body's root element with its prefix
  (`tem:Add`), one segment only; without a prefix it goes out in the default namespace. No `uri`: a SOAP method
  is always POST with XML, so `http-method` may only be POST and `content-type` only xml, and both can be left out.
- `soap-action` is optional: take it from the WSDL (the binding operation's `soapAction`) or ask the user; NEVER
  make one up. Left out, no SOAPAction header is sent and many SOAP 1.1 servers refuse the call; for a SOAP 1.1
  service (also after `setType` to soap11) the tool then returns a notice starting "Tell the user:". Pass the whole notice on, risk sentence included, and ask the user
  for the action. The platform sends the action only as that header, for SOAP 1.1 and 1.2 alike: a SOAP 1.2
  method without one carries no action at all, and a WCF SOAP 1.2 endpoint ignores the header (see the port
  warnings under read-wsdl).
- Request parameters: `in` is `body` (default), `header` or `cookie`; there are no path or query parameters.
  A body parameter's `path` is its element path with declared prefixes: `tem:intA`, or nested
  `tem:Order/tem:Id`. Every prefix used in `message-element`, in every body request parameter `path` (items included) and in a
  request array's `sequence-element` must be declared in `namespaces`;
  an undeclared one is refused with its name. A prefixed segment must be `prefix:name` with letters, digits,
  `_` and `-` only, each part starting with a letter or `_`: the platform cuts a name at any other character (`tem:Api.Key` would go out as `<tem:Api>`), so the
  tool refuses it. An unprefixed name may use any letters (`ДатаНачала`) and goes to the default namespace.
- Response parameters: `path` is an element path read from the response body: local element names joined by
  `/` (`AddResult` or `AddResponse/AddResult`), or with their prefixes as read-wsdl writes them
  (`tem:AddResult`). NEVER a JSONPath; `$.AddResult` is refused.
- `soap-header: true` on a top-level body request parameter sends it in the envelope's Header instead of the
  Body (WS-Security and similar); its `items` go with it. Example of a WS-Security password:
  `{"code": "UsrWsPassword", "path": "wsse:Security/wsse:UsernameToken/wsse:Password", "soap-header": true,
  "default-sys-setting": "UsrErpWsPassword"}`, with `wsse` declared. A soap-header parameter takes no
  `default-value` (refused like a header constant): give it `default-sys-setting` naming an EXISTING setting;
  per the credential rule, create it first as SecureText with no value for the user to fill. The credential-name
  check also covers `licence`/`license`, `nonce`, `passcode`, `passphrase`, `bearer`, `jwt` and `pass` not
  followed by a letter (not `compass`, `bypass`, `passport`).
- `sequence-element` on an array parameter names the element of each item: a plain name (`string`; default
  `item`) or `prefix:name` with a declared prefix (`tem:Region`), as Studio's wizard writes it. An empty one on a
  response array reads every child element: send describe's `""` back, or `setMethod` makes it `item`. On a
  request array a blank one is stored as `item`.
- The tool reports most problems of the definition in one refusal (at most 50); fix them all before
  repeating, and expect a retry to surface a few more.

Changing a SOAP service
- `modify-web-service` takes the same operations as for REST, with SOAP methods, plus two:
  `setNamespaces` (`namespaces`, the WHOLE list, replacing the old one) and `setType` (`type`: `soap11` or
  `soap12`). A prefix that a kept method still uses must stay; removing it is refused, naming the method and
  the prefix.
- What `describe-web-service` returns is valid input: a method as the `method` of `setMethod`, and the
  `namespaces` list for `setNamespaces`. `setMethod` replaces the method: an omitted `soap-action` is cleared, so
  send describe's value back to keep it, and send `soap-header` and `sequence-element` back on the parameters that
  have them.
- USAGE GUARD: `setNamespaces` and `setType` change every call the service makes, so when they really change the
  namespaces or the version while any process calls the service, they are refused like `setMethod` and
  `removeMethod`, with the processes listed (describe's list or the current type sent back is not a change). The refusal says which
  case applies: a changed or removed method makes its callers' elements lose their parameters; a new version or
  new namespaces keep the elements as they are but change what they send. Show the list to the user and repeat
  with `acknowledge-usages` only on their explicit yes; after a version or namespace change, suggest re-running
  those processes to check them. `setBaseUri` and `setAuth` are guarded the same way (`web-service-registration`).

Test calls
- `test-web-service-method` (`code`, `method`, `parameters`) works for a SOAP method as for REST. Every SOAP call
  is a POST that may change data on the other side, so the confirmation rule of `web-service-registration` applies
  to every SOAP method, a read-wsdl one included: ask the user first. Values sent from
  system settings are masked inside the envelope and its Header too; when such a value cannot be found in what was
  sent, the body is withheld and only the status comes back. `response-values` gives the response
  parameters as the platform read them by their element paths.
- A SOAP 1.2 call answered with a fault `ActionNotSupported` and Action '', or `DestinationUnreachable` and To '',
  hit the limits above: switch to the WSDL's SOAP 1.1 port (`setType` soap11), or use the workaround the
  read-wsdl warning gives. Authentication goes out on a SOAP call exactly as on REST, for a method with `use-auth: true`.

Evidence
- Measured on Creatio 10.2 (.NET Framework, MSSQL, prefix `Usr`) in November 2026, through an isolated agent:
  a SOAP 1.1 service registered from a description, opened and saved in Studio with no difference in its
  stored definition, test-called (HTTP 200, the expected result) and called from a process; a SOAP 1.2 service
  with a WS-Security password from a SecureText setting, sent in the Header and masked in the echoed reply; a
  switch from SOAP 1.1 to 1.2 after the user's yes, the calling process unchanged in the designer and returning
  the result; undeclared prefixes, a JSONPath response and a constant licence header refused; a used namespace
  kept; a missing SOAP action reported and relayed, with no SOAPAction header sent. Other versions are unknown.
- read-wsdl, measured on the same platform in November 2026: the public calculator WSDL through Studio's SOAP quick
  setup and through read-wsdl and register-web-service gave equal services (type, address, namespaces, every
  method's SOAP action and every parameter's path and type), except Studio's random method-code suffix, its
  shortened parameter codes and its bare message element; both answered 2 + 3 = 5. Isolated agents given only the
  WSDL URL called read-wsdl on their own in 2 of 2 sessions, registered the SOAP 1.2 pick, test-called it and built
  a process that called it and returned the result. One of them rewrote the response paths without their prefixes
  (`AddResult`); both forms read the result.
- WCF, measured on the same platform in November 2026 against a .NET Framework 4.8 WCF host with one contract on
  four endpoints: `BasicHttpBinding` (SOAP 1.1) answered; SOAP 1.2 without WS-Addressing answered
  `ActionNotSupported`, and with the `Content-Type` workaround the result; SOAP 1.2 with WS-Addressing and
  `wsHttpBinding` with security mode None answered `DestinationUnreachable`, and with `wsa:Action` and `wsa:To` the
  result. SOAP 1.1 and
  1.2 sent `Authorization: Basic` from system settings like REST, and Digest as Basic. An ASMX service answered on
  SOAP 1.2 as well. Other servers are unknown.

clio MCP SOAP web service guide — register and change a SOAP 1.1 or SOAP 1.2 service in Studio > Web services

Scope and ownership
- Read `web-service-registration` first: it owns the five web service tools, the credential rule, the prefix and
  `path` rules, the usage guard and the follow-up steps, and all of it applies to a SOAP service too. This guide
  owns only what differs for SOAP: the service type, the namespaces, the method's message element and SOAP
  action, element paths, the SOAP header, array items, and the two operations only a SOAP service has.
- Availability: SOAP registration exists only in a clio whose `register-web-service` contract
  (`get-tool-contract`) has the `type` and `namespaces` arguments. Without them, tell the user that a SOAP service is
  registered in Studio > Web services and stop. Reading and test-calling a SOAP service works as for REST.
- There is no WSDL import. The agent describes the service from what the user gives: the WSDL text, its
  documentation, or the operations they name. Never guess an operation, an element name or a namespace.

Registering a SOAP service
- `type`: `soap11` or `soap12` (default `rest`). REST and SOAP cannot be switched later.
- `namespaces` (at least one): `[{"prefix": "tem", "uri": "http://tempuri.org/"}]`. Exactly one is the default:
  the one marked `default: true`, else the first. Prefixes starting with `xml` and the prefix `soapenv` are
  reserved and refused; a namespace `uri` with `user:password@` is refused.
- `base-uri` is the endpoint URL as the WSDL's `soap:address` gives it (`http://www.dneonline.com/calculator.asmx`).
  It is stored as given: no trailing slash is added, and a `{placeholder}` is refused, because a SOAP method has no
  address of its own.
- Each method: `code` (prefixed) and `message-element`, the request body's root element with its prefix
  (`tem:Add`), one segment only; without a prefix it goes out in the default namespace. No `uri`: a SOAP method
  is always POST with XML, so `http-method` may only be POST and `content-type` only xml, and both can be left out.
- `soap-action` is optional: take it from the WSDL (the binding operation's `soapAction`) or ask the user; NEVER
  make one up. Left out, no SOAPAction header is sent and many SOAP 1.1 servers refuse the call; the tool then
  returns a notice starting "Tell the user:". Pass the whole notice on, risk sentence included, and ask the user
  for the action. The platform sends the action only as that header, for SOAP 1.1 and 1.2 alike: a SOAP 1.2
  method without one carries no action at all.
- Request parameters: `in` is `body` (default), `header` or `cookie`; there are no path or query parameters.
  A body parameter's `path` is its element path with declared prefixes: `tem:intA`, or nested
  `tem:Order/tem:Id`. Every prefix used in `message-element` and in every body request parameter `path` (items included) must be
  declared in `namespaces`;
  an undeclared one is refused with its name. A prefixed segment must be `prefix:name` with letters, digits,
  `_` and `-` only, each part starting with a letter or `_`: the platform cuts a name at any other character (`tem:Api.Key` would go out as `<tem:Api>`), so the
  tool refuses it.
- Response parameters: `path` is an element path read from the response body, local element names joined by
  `/` (`AddResult` or `AddResponse/AddResult`), NEVER a JSONPath; `$.AddResult` is refused.
- `soap-header: true` on a top-level body request parameter sends it in the envelope's Header instead of the
  Body (WS-Security and similar); its `items` go with it. Example of a WS-Security password:
  `{"code": "UsrWsPassword", "path": "wsse:Security/wsse:UsernameToken/wsse:Password", "soap-header": true,
  "default-sys-setting": "UsrErpWsPassword"}`, with `wsse` declared. A soap-header parameter takes no
  `default-value` (refused like a header constant): give it `default-sys-setting` naming an EXISTING setting;
  per the credential rule, create it first as SecureText with no value for the user to fill. The credential-name
  check also covers `licence`/`license`, `nonce` and `pass` at the end of a word (not `compass`, `bypass`).
- `sequence-element` on an array parameter names the element of each item (`string`; default `item`).
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
  those processes to check them.

Test calls
- `test-web-service-method` (`code`, `method`, `parameters`) works for a SOAP method as for REST. Every SOAP call
  is a POST that may change data on the other side: confirm with the user before calling. Values sent from
  system settings are masked inside the envelope and its Header too; when such a value cannot be found in what was
  sent, the body is withheld and only the status comes back. `response-values` gives the response
  parameters as the platform read them by their element paths.

Evidence
- Measured on Creatio 10.2 (.NET Framework, MSSQL, prefix `Usr`) in November 2026, through an isolated agent:
  a SOAP 1.1 service registered from a description, opened and saved in Studio with no difference in its
  stored definition, test-called (HTTP 200, the expected result) and called from a process; a SOAP 1.2 service
  with a WS-Security password from a SecureText setting, sent in the Header and masked in the echoed reply; a
  switch from SOAP 1.1 to 1.2 after the user's yes, the calling process unchanged in the designer and returning
  the result; undeclared prefixes, a JSONPath response and a constant licence header refused; a used namespace
  kept; a missing SOAP action reported and relayed, with no SOAPAction header sent. Other versions are unknown.

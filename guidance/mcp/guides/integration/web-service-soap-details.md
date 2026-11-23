clio MCP SOAP web service details — why some ports cannot be called, and what the SOAP rules were measured on

Scope
- Why some SOAP ports cannot be called, and the evidence behind `web-service-soap`. The rules themselves are
  owned by `web-service-soap`, `web-service-registration` and `web-service-maintenance`; read those to act.

Why a port warning names another port
- The platform sends a SOAP 1.2 action only as a `SOAPAction` header, never as the `action` parameter of
  `Content-Type`, so a service that dispatches SOAP 1.2 by that parameter (WCF) answers every call with
  `ActionNotSupported` and Action ''; and it writes no WS-Addressing header, so a port that requires WS-Addressing
  (WCF `wsHttpBinding` with security mode None) answers `DestinationUnreachable` with To ''. A port that requires
  WS-Security message security (a default `wsHttpBinding`) cannot be called at all; ask the API owner for a plain
  endpoint.

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

Manual Runtime Checklist

Use this after implementation when you need a concrete runtime verification plan.

Endpoint Checklist
1. Authenticate in Creatio and keep the required session or auth headers.
2. Call the endpoint with the correct route for the target runtime:
   - NET472: /0/rest/<ServiceName>/<MethodName>
   - NETSTANDARD2_0: /rest/<ServiceName>/<MethodName>
3. Send a representative success request.
4. Send at least one representative failure request.
5. Verify:
   - HTTP status code
   - response body shape
   - DTO field names
   - error message or code for failure path
6. Record one success response and one failure response in task notes.

What To Watch For
- route mismatches between NET472 and NETSTANDARD2_0
- DTO fields not serialized as expected
- methods returning interface or object types
- custom status codes not being applied on one of the target frameworks

Endpoint Test Patterns

What To Assert
Test what the endpoint actually controls:
- DTO-returning method: assert returned object and any custom Response.StatusCode
- void response writer: assert Response.StatusCode, ContentType, and written body from OutputStream
- Stream response: assert Response.StatusCode, ContentType, and stream content
- For DTO endpoints backed by an application service, assert the dependency was called with the expected mapped arguments.

Minimum coverage per endpoint:
- Success path
- One negative path
- Status code behavior
- Response payload or stream content

Workflow
1. Identify whether the endpoint returns DTO, void, or Stream.
2. Build a fixture that provides the service with a mocked HttpContextAccessor.
3. Inject substitutes for application-service dependencies through the package composition root.
4. Add focused success and negative-path tests.
5. Assert the HTTP status code whenever the endpoint can set one explicitly.
6. Assert dependency invocation arguments for entry-point mapping.
7. Assert body or stream content for manual-writer or Stream endpoints.
8. Keep service-layer behavior in separate unit tests when that improves clarity.

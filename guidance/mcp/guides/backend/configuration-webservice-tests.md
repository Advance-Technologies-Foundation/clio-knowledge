configuration-webservice-tests

Write, update, or review tests for Creatio Configuration Web Services under test folders such as tests/PkgOne/EntryPoints/WebServices. Use when adding endpoint tests, building workspace-specific web-service fixtures, enforcing AAA structure, requiring [Description] on every test, adding because reasons to assertions, checking status-code mapping, or validating DTO, Stream, or manual-response endpoint behavior. Pair with configuration-webservice when production endpoint code changes.

Non-Negotiable Rules
- Add or update tests for production web-service changes unless the user explicitly says not to.
- Structure every test method with explicit // Arrange, // Act, and // Assert sections.
- Decorate every test with [Description("...")].
- Add because: "..." to every assertion.
- Prefer the local workspace test base and fixture pattern over repository-agnostic examples.
- Assume the endpoint return type must be concrete. If production code returns an interface or object, flag it as incorrect rather than writing tests around it.
- Prefer mocking application-service dependencies in web-service tests. Inject the substitute through the package composition root and assert the dependency was called with the expected parameters.

Workflow
1. Identify whether the endpoint returns DTO, void, or Stream.
2. Build or update the local HTTP-context fixture for the package.
3. Inject substitutes for application-service dependencies through the package composition root when testing the web-service entry point.
4. Add focused success and negative-path tests.
5. Assert explicit HTTP status mapping whenever the endpoint sets it.
6. Assert the dependency was called with the mapped request values.
7. Build and run tests sequentially in this workspace.

References
Read only what you need:
- docs://knowledge/com.creatio.clio/reference.configuration-webservice-tests.test-fixture-pattern: local fixture shape, composition-root reset, and dependency-injection mocking pattern
- docs://knowledge/com.creatio.clio/reference.configuration-webservice-tests.assertion-style: AAA layout, [Description], and because assertion rules
- docs://knowledge/com.creatio.clio/reference.configuration-webservice-tests.endpoint-test-patterns: what to assert for DTO, void, and Stream endpoints

Build And Verify
Typical commands:

```powershell
dotnet build .\MainSolution.slnx -c dev-n8 -v d
dotnet test .\tests\<PACKAGE_NAME>\<PACKAGE_NAME>.Tests.csproj -c dev-n8 --no-build
```

Use the matching dev-nf configuration for net472 targets.

Run build and test sequentially in this workspace. Parallel dotnet build and dotnet test can lock package outputs under obj.

What To Report Back
- Test files changed, with one-line reason per file
- Coverage intent for each added or updated test
- Build/test commands run, or the exact blocker if not run
- Any workspace-specific fixture or dependency issue discovered while wiring the tests
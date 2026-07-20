# Feature Toggle Tests

## Non-Negotiable Rules

- Add or update tests for production feature-toggle changes unless the user explicitly says not to.
- Reuse the same feature-name constant from `Constants.cs`. Do not duplicate raw feature-name literals in tests.
- Prefer setting the default feature state in shared fixture or setup code when most tests use the same value.
- Add at least one enabled-path test and one disabled-path test when the feature changes observable behavior.
- Pair this skill with `feature-toggle` when production code also changes.

## Required Stub Pattern

Disabled:

```csharp
FeatureRequest request =
	new Creatio.FeatureToggling.Providers.FeatureRequest(Constants.FeatureName);
Creatio.FeatureToggling.TestKit.FeatureStub.Setup(request, false);
```

Enabled:

```csharp
FeatureRequest request =
	new Creatio.FeatureToggling.Providers.FeatureRequest(Constants.FeatureName);
Creatio.FeatureToggling.TestKit.FeatureStub.Setup(request, true);
```

Use `true` for enabled coverage and `false` for disabled coverage.

## Workflow

1. Reuse or add the feature-name constant in `Constants.cs`.
2. Set the default feature state in shared fixture or setup code when that keeps the test suite simpler.
3. Cover the observable enabled behavior.
4. Cover the observable disabled or fallback behavior.
5. Override the stub in the arrange section only for tests that need a different feature state.
6. Build and run the relevant tests sequentially.

## Coverage Checklist

1. Feature enabled path returns or triggers the expected result.
2. Feature disabled path preserves the fallback behavior.
3. Tests reference `Constants.cs` instead of repeating the raw feature name.
4. Feature stub setup is obvious from the fixture setup or an explicit per-test override.
5. Assertions focus on the gated behavior, not unrelated implementation details.

## References

Read only what you need:
- `docs://knowledge/com.creatio.clio/reference.feature-toggle-tests.feature-stub-pattern`: exact `FeatureRequest` and `FeatureStub.Setup(...)` usage
- `docs://knowledge/com.creatio.clio/reference.feature-toggle-tests.constants-and-fixture-pattern`: how to keep constants shared across production code and tests
- `docs://knowledge/com.creatio.clio/reference.feature-toggle-tests.test-coverage-checklist`: minimum coverage expectations for feature-gated logic

## Build And Verify

Typical commands:

```powershell
dotnet build .\MainSolution.slnx -c dev-n8 -v d
dotnet test .\tests\<PACKAGE_NAME>\<PACKAGE_NAME>.Tests.csproj -c dev-n8 --no-build
```

Use the matching `dev-nf` configuration for `net472` targets.

Run build and test sequentially in this workspace. Parallel `dotnet build` and `dotnet test` can lock package outputs under `obj`.

## What To Report Back

- Test files changed, with one-line reason per file
- Which feature constant was added or reused
- Which enabled and disabled behaviors are covered
- Build/test commands run, or the exact blocker if not run
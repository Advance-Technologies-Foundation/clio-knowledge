# Feature Toggle

## Non-Negotiable Rules

- Prefer `const string` feature names in `Constants.cs`. Do not introduce new inline string literals in production code when a package constants file exists or should exist.
- Use `Creatio.FeatureToggling.Features.GetIsEnabled(...)` for the decision point.
- Evaluate the feature once per logical branch and store the result in a local variable when the value is reused.
- Keep both enabled and disabled paths explicit. Do not hide the fallback behavior.
- If production code changes under a feature flag, also apply `feature-toggle-tests`.

## Required Pattern

Use this shape unless the local package already has a stronger convention:

```csharp
var isFeatureEnabled =
	Creatio.FeatureToggling.Features.GetIsEnabled(Constants.FeatureName);
```

Behavior reminder:
- `true` means the feature exists and is enabled globally, for the current user, or for a user group the current user belongs to.
- `false` means the feature is missing or not enabled for the current execution context.

## Constants Pattern

Prefer a package-level constants holder such as:

```csharp
public static class Constants {
	public const string FeatureName = "FeatureName";
}
```

Rules:
- Reuse an existing `Constants.cs` file when present.
- Add a narrowly named constant for each feature flag instead of reusing unrelated constants.
- Reference the constant from production code and tests.
- Do not duplicate the raw feature name in multiple files.

## Implementation Workflow

1. Find the package `Constants.cs` file. Add a feature-name constant there if one does not already exist.
2. Add the feature check close to the behavior it gates.
3. Keep the non-feature path readable and unchanged unless the feature requires a deliberate fallback.
4. If the method has multiple gated branches, compute the feature state once and reuse the variable.
5. If tests are required, apply `feature-toggle-tests`.
6. Build and run the relevant tests after production changes.

## Review Checklist

1. Feature name comes from `Constants.cs`.
2. The code uses `Creatio.FeatureToggling.Features.GetIsEnabled(...)`.
3. The feature state is not recomputed unnecessarily.
4. Enabled and disabled behavior are both understandable from the code.
5. The gated logic does not silently change unrelated behavior.
6. Tests cover both enabled and disabled outcomes when the behavior affects observable results.

## References

Read only what you need:
- `docs://knowledge/com.creatio.clio/reference.feature-toggle.constants-pattern`: where to keep feature-name constants and how to reference them
- `docs://knowledge/com.creatio.clio/reference.feature-toggle.implementation-patterns`: example usage in methods and review guidance
- `docs://knowledge/com.creatio.clio/reference.feature-toggle.runtime-behavior`: what `GetIsEnabled` means for global, user, and group assignments

## Build And Verify

Typical commands:

```powershell
dotnet build .\MainSolution.slnx -c dev-n8 -v d
dotnet test .\tests\<PACKAGE_NAME>\<PACKAGE_NAME>.Tests.csproj -c dev-n8 --no-build
```

Use the matching `dev-nf` configuration for `net472` targets.

Run build and test sequentially in this workspace. Parallel `dotnet build` and `dotnet test` can lock package outputs under `obj`.

## What To Report Back

- Files changed, with one-line reason per file
- Which feature flag constant was added or reused
- Where the feature check was added and what behavior it gates
- Tests added or updated, or the reason tests were not changed
- Build/test commands run, or the exact blocker if not run
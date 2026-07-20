# Runtime Behavior

`Creatio.FeatureToggling.Features.GetIsEnabled(...)` returns:

- `true` when the feature exists and is enabled globally
- `true` when the feature is enabled for the current user
- `true` when the feature is enabled for a user group the current user belongs to
- `false` otherwise

Treat `false` as the default and keep the fallback behavior explicit in code and tests.

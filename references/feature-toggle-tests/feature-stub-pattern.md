# Feature Stub Pattern

Use `FeatureRequest` plus `FeatureStub.Setup(...)` to control the feature state in tests.

## Disabled

```csharp
FeatureRequest request =
	new Creatio.FeatureToggling.Providers.FeatureRequest(Constants.FeatureName);
Creatio.FeatureToggling.TestKit.FeatureStub.Setup(request, false);
```

## Enabled

```csharp
FeatureRequest request =
	new Creatio.FeatureToggling.Providers.FeatureRequest(Constants.FeatureName);
Creatio.FeatureToggling.TestKit.FeatureStub.Setup(request, true);
```

## Guidance

- Build the request from the shared constant.
- Prefer shared fixture or setup code for the default feature state.
- Override the stub in the arrange phase only when a test needs a different state.
- Add separate tests when enabled and disabled behavior differ.

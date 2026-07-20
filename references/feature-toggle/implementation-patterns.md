# Implementation Patterns

Reuse the local package style when it already contains feature-toggle usage. Otherwise add a small, explicit gate around the affected behavior.

## Basic Usage

```csharp
public CalculationResult Calculate(CalculationRequest request) {
	var isFeatureEnabled =
		Creatio.FeatureToggling.Features.GetIsEnabled(Constants.FeatureName);

	if (!isFeatureEnabled) {
		return CalculateLegacy(request);
	}

	return CalculateWithFeature(request);
}
```

## Review Guidance

- Compute the feature state once when multiple branches depend on it.
- Keep the feature check near the behavior it controls.
- Leave the fallback path easy to read.
- Prefer feature-gating orchestration code, not low-level helper lines that make the behavior harder to trace.

# Constants And Fixture Pattern

Reuse the same feature constant from production code and configure the stub in the local fixture style.

## Example

```csharp
protected override void SetUp() {
	base.SetUp();

	FeatureRequest request =
		new Creatio.FeatureToggling.Providers.FeatureRequest(Constants.FeatureName);
	Creatio.FeatureToggling.TestKit.FeatureStub.Setup(request, false);
}
```

## Rules

- Prefer `Constants.cs` over raw feature-name literals.
- Reuse the local fixture style from the package test project when one already exists.
- If a test changes the feature state from the fixture default, make that override explicit in the arrange section.

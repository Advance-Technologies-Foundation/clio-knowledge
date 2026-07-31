# Constants Pattern

Keep feature names in a package-level `Constants.cs` file and reuse them from both production code and tests.

## Example

```csharp
namespace PkgOne {
	public static class Constants {
		public const string FeatureName = "FeatureName";
	}
}
```

## Rules

- Prefer extending an existing `Constants.cs` file over introducing a second constants holder.
- Use a specific constant name that communicates the gated behavior.
- Reference the constant everywhere the feature name is needed.
- Do not mix raw string literals and constants for the same feature.

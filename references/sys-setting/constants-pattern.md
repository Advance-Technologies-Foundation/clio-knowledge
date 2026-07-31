# Constants Pattern

Keep sys-setting code strings in a package-level `Constants.cs` file and prefer a nested `SysSettingCodes` holder when the package does not already have an established constants layout.

## Example

```csharp
namespace PkgOneApp {
	public static class Constants {
		public static class SysSettingCodes {
			public const string MySetting = "MySetting";
		}
	}
}
```

## Rules

- Reuse an existing `Constants.cs` file when present.
- Prefer `Constants.SysSettingCodes`, but do not introduce a second constants pattern if the package already uses another established layout.
- Add one narrowly named constant per system setting.
- Do not mix raw string literals and constants for the same sys setting.

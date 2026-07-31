# Sys Setting

## Non-Negotiable Rules

- Keep every sys-setting code string in `Constants.cs`. Prefer a nested holder such as `Constants.SysSettingCodes`, but extend the existing package constants layout when one is already established instead of introducing a competing holder.
- Reuse the same constant from production code and tests. Do not repeat the raw sys-setting code in multiple files.
- Use `Terrasoft.Core.Configuration.SysSettings` directly for reads.
- Choose the accessor that matches the behavior: `TryGetValue(...)` for optional reads, `GetValue(...)` for current value reads, `GetDefValue(...)` for default-value reads.
- If production sys-setting code changes, also apply `sys-setting-tests`.

## Required Access Patterns

Optional read:

```csharp
bool isSettingValue =
	Terrasoft.Core.Configuration.SysSettings.TryGetValue(
		UserConnection,
		Constants.SysSettingCodes.MySetting,
		out object settingValue);
```

Current value:

```csharp
var value = Terrasoft.Core.Configuration.SysSettings.GetValue(
	UserConnection,
	Constants.SysSettingCodes.MySetting);
```

Default value:

```csharp
var defValue = Terrasoft.Core.Configuration.SysSettings.GetDefValue(
	UserConnection,
	Constants.SysSettingCodes.MySetting);
```

## Constants Pattern

Prefer this shape unless the package already has a stronger convention:

```csharp
public static class Constants {
	public static class SysSettingCodes {
		public const string MySetting = "MySetting";
	}
}
```

Rules:
- Extend an existing `Constants.cs` file when present.
- Prefer `Constants.SysSettingCodes`, but keep the existing package constants layout if one already exists.
- Add a narrowly named constant for each sys setting.
- Reference the constant everywhere the code is needed.

## Workflow

1. Find the package `Constants.cs` file and add or reuse `Constants.SysSettingCodes.<SettingName>`.
2. Identify whether the code needs `TryGetValue(...)`, `GetValue(...)`, or `GetDefValue(...)`.
3. Place the sys-setting read close to the behavior that depends on it.
4. Reuse the returned value instead of re-reading the same setting inside one logical branch.
5. Keep fallback behavior explicit when `TryGetValue(...)` can return `false`.
6. If tests are required, apply `sys-setting-tests`.
7. Build and run the relevant tests after production changes.

## Review Checklist

1. Sys-setting code string comes from `Constants.SysSettingCodes`.
2. The accessor matches the intent: optional, current value, or default value.
3. The same setting is not re-read unnecessarily inside the same flow.
4. `TryGetValue(...)` failure handling is explicit and readable.
5. Tests cover the observable behavior that depends on the setting when behavior changes.

## References

Read only what you need:
- `docs://knowledge/com.creatio.clio/reference.sys-setting.constants-pattern`: where to keep sys-setting code constants
- `docs://knowledge/com.creatio.clio/reference.sys-setting.access-patterns`: exact `TryGetValue(...)`, `GetValue(...)`, and `GetDefValue(...)` usage
- `docs://knowledge/com.creatio.clio/reference.sys-setting.review-checklist`: implementation and review reminders for sys-setting reads

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
- Which sys-setting constant was added or reused
- Which accessor was chosen and why
- Tests added or updated, or the reason tests were not changed
- Build/test commands run, or the exact blocker if not run
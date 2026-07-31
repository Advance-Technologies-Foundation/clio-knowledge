# Access Patterns

Use the `Terrasoft.Core.Configuration.SysSettings` accessor that matches the intended behavior.

## `TryGetValue(...)`

Use when the code can proceed differently if the setting is missing or unavailable.

```csharp
bool isSettingValue = Terrasoft.Core.Configuration.SysSettings.TryGetValue(
	UserConnection,
	Constants.SysSettingCodes.MySetting,
	out object settingValue);
```

## `GetValue(...)`

Use when the current effective setting value is required.

```csharp
var value = Terrasoft.Core.Configuration.SysSettings.GetValue(
	UserConnection,
	Constants.SysSettingCodes.MySetting);
```

## `GetDefValue(...)`

Use when the default setting value is required instead of the current resolved value.

```csharp
var defValue = Terrasoft.Core.Configuration.SysSettings.GetDefValue(
	UserConnection,
	Constants.SysSettingCodes.MySetting);
```

## Rules

- Pass `UserConnection` to `TryGetValue(...)` and `GetValue(...)`.
- Keep the sys-setting code in `Constants.SysSettingCodes`.
- Reuse a local variable when later code needs the same result more than once.

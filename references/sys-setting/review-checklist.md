# Review Checklist

Use this list when reviewing sys-setting access changes.

1. The sys-setting code comes from `Constants.SysSettingCodes`.
2. The chosen accessor matches the behavior under test or implementation.
3. `TryGetValue(...)` callers handle the `false` path explicitly.
4. The same setting is not re-read unnecessarily in the same logical branch.
5. Tests were updated when the setting changes observable behavior.

# Coverage Checklist

Use this checklist when reviewing sys-setting unit tests.

1. The class is decorated with `[MockSettings(RequireMock.All)]`.
2. `SetupSysSettings()` calls `base.SetupSysSettings()`.
3. `FakeSysSettings` uses `Constants.SysSettingCodes` for the setting code.
4. `FakeSysSettingsEngine` mocks the same accessor the production code calls.
5. Tests assert the behavior caused by the mocked setting value.
6. Raw sys-setting code strings do not appear in test code.

# Sys Setting Tests

## Non-Negotiable Rules

- Decorate the test class with `[MockSettings(RequireMock.All)]`.
- Reuse the same sys-setting code constant from `Constants.SysSettingCodes`. Do not repeat raw sys-setting strings in tests.
- Override `SetupSysSettings()` when the test fixture reads sys settings.
- Register every required setting with `FakeSysSettings.Setup(...)`.
- Mock `FakeSysSettingsEngine` for each accessor the production code uses: `GetDefaultSettingsValue(...)`, `GetSettingsValue(...)`, or `TryGetSettingsValue(...)`.
- Pair this skill with `sys-setting` when production code also changes.

## Required Fixture Pattern

Mock only the accessor or accessors used by the production code under test. The example below shows all three for reference.

```csharp
[MockSettings(RequireMock.All)]
public class MyTests : MyTestsBase {
	protected override void SetupSysSettings() {
		base.SetupSysSettings();

		FakeSysSettings mySetting = new FakeSysSettings {
			Code = PkgOneApp.Constants.SysSettingCodes.MySetting,
		};

		FakeSysSettings.Setup(new[] { mySetting });
		FakeSysSettingsEngine engine = Substitute.For<FakeSysSettingsEngine>();
		FakeSysSettingsEngine.Setup(engine);
		const string mockValue = "mock value";

		engine.GetDefaultSettingsValue(Arg.Is(PkgOneApp.Constants.SysSettingCodes.MySetting))
			.Returns(mockValue + "GetDefValue");

		engine.GetSettingsValue(PkgOneApp.Constants.SysSettingCodes.MySetting, Arg.Any<Guid>())
			.Returns(_ => mockValue + "GetSettingsValue");

		engine.TryGetSettingsValue(
				PkgOneApp.Constants.SysSettingCodes.MySetting,
				Arg.Any<Guid>(),
				out Arg.Any<object>())
			.Returns(callInfo => {
				callInfo[2] = mockValue + "TryGetSettingsValue";
				return true;
			});
	}
}
```

## Workflow

1. Add `[MockSettings(RequireMock.All)]` to the test class.
2. Reuse or add the setting code under `Constants.SysSettingCodes`.
3. Override `SetupSysSettings()` and call `base.SetupSysSettings()`.
4. Register the needed `FakeSysSettings` entries with the same constants used by production code.
5. Set up `FakeSysSettingsEngine` responses for the sys-setting accessor used in the production path under test.
6. Add focused assertions for the observable behavior driven by the mocked setting values.
7. Build and run tests sequentially.

## Coverage Checklist

1. The test class has `[MockSettings(RequireMock.All)]`.
2. `SetupSysSettings()` registers every setting the test subject reads.
3. `GetDefaultSettingsValue(...)` is mocked when production code calls `GetDefValue(...)`.
4. `GetSettingsValue(...)` is mocked when production code calls `GetValue(...)`.
5. `TryGetSettingsValue(...)` is mocked when production code calls `TryGetValue(...)`.
6. Tests use `Constants.SysSettingCodes` instead of raw strings.
7. Assertions cover the behavior affected by the sys-setting value.

## References

Read only what you need:
- `docs://knowledge/com.creatio.clio/reference.sys-setting-tests.mock-settings-attribute`: when and why to require `[MockSettings(RequireMock.All)]`
- `docs://knowledge/com.creatio.clio/reference.sys-setting-tests.setup-sys-settings-pattern`: exact `SetupSysSettings()` and `FakeSysSettingsEngine` pattern
- `docs://knowledge/com.creatio.clio/reference.sys-setting-tests.coverage-checklist`: minimum expectations for sys-setting test coverage

## Build And Verify

Typical commands:

```powershell
dotnet build .\MainSolution.slnx -c dev-n8 -v d
dotnet test .\tests\<PACKAGE_NAME>\<PACKAGE_NAME>.Tests.csproj -c dev-n8 --no-build
```

Use the matching `dev-nf` configuration for `net472` targets.

Run build and test sequentially in this workspace. Parallel `dotnet build` and `dotnet test` can lock package outputs under `obj`.

## What To Report Back

- Test files changed, with one-line reason per file
- Which sys-setting constant was added or reused
- Which sys-setting accessor mocks were configured
- Build/test commands run, or the exact blocker if not run
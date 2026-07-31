# Setup Sys Settings Pattern

Override `SetupSysSettings()` to register fake settings and configure the fake engine for the accessors used by the production code.

## Example

Mock only the accessor or accessors used by the production code under test. The example below shows all three for reference.

```csharp
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
```

## Rules

- Always call `base.SetupSysSettings()` first.
- Register the required `FakeSysSettings` entries before setting up the engine.
- Configure only the accessors the production code actually uses, unless the shared fixture benefits from setting all three.
- Keep the mocked values obvious so tests can assert the behavior they drive.

# Mock Settings Attribute

Decorate sys-setting test classes with `[MockSettings(RequireMock.All)]` when the fixture uses fake sys-setting infrastructure.

## Example

```csharp
[MockSettings(RequireMock.All)]
public class MyTests : MyTestsBase {
}
```

## Rules

- Apply the attribute at the test-class level.
- Keep it in place for suites that depend on `FakeSysSettings` or `FakeSysSettingsEngine`.
- Do not rely on partially mocked sys settings when `RequireMock.All` is the workspace rule.

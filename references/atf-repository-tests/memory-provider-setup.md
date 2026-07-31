# Memory Provider Setup

## Default Test Double

Use `MemoryDataProviderMock` as the default in-memory data provider for ATF.Repository tests.

Pattern:

```csharp
MemoryDataProviderMock dataProvider = new MemoryDataProviderMock();
IAppDataContext appDataContext = AppDataContextFactory.GetAppDataContext(dataProvider);
```

## Register Models Before Use

Register every model that participates in the test:

```csharp
dataProvider.DataStore.RegisterModelSchema<Contact>();
```

Or register a graph:

```csharp
dataProvider.DataStore.RegisterModelSchema(typeof(Account), typeof(Contact), typeof(ContactDecisionRole));
```

Register:

- the root model under test
- related lookup models
- related detail models
- nested related models used by filters or assertions

## Default Values

When production logic depends on default values, configure them explicitly:

```csharp
dataProvider.DataStore.SetDefaultValues(defaults => {
	defaults.Add("StatusId", someStatusId);
});
```

## Additional Mock Features

`MemoryDataProviderMock` also supports:

- `MockSysSettingValue(...)`
- `MockFeatureEnable(...)`
- `MockExecuteProcess(...)`

Use these when repository code depends on sys settings, feature flags, or process execution.

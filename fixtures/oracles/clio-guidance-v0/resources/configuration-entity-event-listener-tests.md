# Configuration Entity Event Listener Tests

## Non-Negotiable Rules

- Add or update tests when entity-listener production behavior changes unless the user explicitly says not to.
- Create a fresh listener instance per test when the listener stores private operation state such as `UserConnection`.
- Instantiate the real entity schema through `UserConnection.EntitySchemaManager.GetInstanceByName(...)`.
- Fill only the minimal entity data needed for the scenario, typically with `CreateEntity(UserConnection)` and `SetDefColumnValues()`.
- Invoke only the minimal set of listener events required for the behavior under test, but keep the real event order whenever logic spans multiple events.
- Mock helper or service dependencies instead of testing heavy business logic through the listener itself.
- Pair this skill with `configuration-entity-event-listener` when production listener code also changes.

## Required Test Shape

Typical test location:
- `tests/<PACKAGE_NAME>/EntityEventListeners/<EntitySchemaName>EntityEventListenerTests.cs`

Typical fixture shape:

```csharp
[TestFixture]
[MockSettings(RequireMock.DBEngine)]
public class AccountAnniversaryEntityEventListenerTests : BaseConfigurationTestFixture {
	private AccountAnniversaryEntityEventListener _listener;
	private IAccountAnniversaryHelper _accountAnniversaryHelper;

	protected override void SetUp() {
		base.SetUp();
		_accountAnniversaryHelper = Substitute.For<IAccountAnniversaryHelper>();
		ClassFactory.RebindWithFactoryMethod(() => _accountAnniversaryHelper);
		_listener = new AccountAnniversaryEntityEventListener();
	}
}
```

## Workflow

1. Inspect which listener events actually contain or enable the behavior under test.
2. Create or reuse the entity schema instance with only the needed default and explicit field values.
3. Rebind helper dependencies through `ClassFactory` or the local composition mechanism used by the listener.
4. Create a fresh listener instance in `SetUp()` or the arrange section.
5. Invoke the minimal event chain in the real lifecycle order.
6. Assert the observable behavior: helper calls, validation messages, entity changes, or side effects.
7. Build and run the relevant tests sequentially.

## Event Invocation Rules

- If the listener logic lives only in `OnDeleting`, call only `OnDeleting`.
- If `OnSaved` depends on data captured in `OnSaving`, call `OnSaving` and then `OnSaved`.
- If the listener logic spans create or update specific hooks, preserve the real order from the Creatio pipeline.
- For validation handlers added in `OnSaving`, call `OnSaving` before raising or inspecting validation behavior.

## Coverage Checklist

1. Tests cover the exact listener method or event chain that enables the behavior.
2. Each test uses a fresh listener instance when private state matters.
3. Entity setup is minimal and readable.
4. Helper or service dependencies are mocked and verified directly.
5. Validation scenarios assert `ValidationMessages` rather than exception flow for expected failures.
6. Multi-event scenarios call methods in the correct order.

## References

Read only what you need:
- `references/test-patterns.md`: fixture shape, entity creation, helper rebinding, and event invocation examples
- `references/validation-test-patterns.md`: how to test `entity.Validating` handlers and validation messages
- `references/review-checklist.md`: review points and common listener-test defects

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
- Which listener event or event chain each test covers
- Which helper or service dependency was mocked and verified
- Build/test commands run, or the exact blocker if they were not run
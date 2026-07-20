# Test Patterns

## Minimal Helper-Call Test

Use the smallest event chain that enables the listener logic:

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

	[Test]
	public void OnSaved_Should_Call_CalculateRegistrationsAndBookedValue() {
		EntitySchema schema =
			UserConnection.EntitySchemaManager.GetInstanceByName("AccountAnniversary");
		Entity entity = schema.CreateEntity(UserConnection);
		entity.SetDefColumnValues();

		_listener.OnSaving(entity, null);
		_listener.OnSaved(entity, null);

		_accountAnniversaryHelper.Received(1).CalculateRegistrationsAndBookedValue();
	}
}
```

## Core Guidance

- Create the real schema instance through `EntitySchemaManager`.
- Prefer `SetDefColumnValues()` before adding scenario-specific values.
- Rebind helper dependencies before creating or invoking the listener.
- Use one listener instance per test when it caches state across events.

## Event Chain Selection

- `OnSaving` only: test shared before-save logic.
- `OnSaving` + `OnSaved`: test logic that captures state before save and uses it after save.
- `OnInserting` or `OnInserted`: test create-specific logic only.
- `OnUpdating` or `OnUpdated`: test update-specific logic only.
- `OnDeleting` or `OnDeleted`: test delete-specific logic only.

## What To Avoid

- Replaying the full lifecycle when one method is enough.
- Creating unnecessary records or unrelated mocks.
- Reusing a listener instance across tests.
- Verifying helper internals instead of the listener's observable collaboration.

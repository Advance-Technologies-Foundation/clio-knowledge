# Validation Test Patterns

## Testing Validation Subscription

When the listener subscribes to `entity.Validating` from `OnSaving`, first call `OnSaving`, then trigger validation through the entity flow available in the workspace test base.

Example pattern:

```csharp
[Test]
public void OnSaving_Should_Add_ValidationMessage_When_Entity_Is_Invalid() {
	EntitySchema schema =
		UserConnection.EntitySchemaManager.GetInstanceByName("AccountAnniversary");
	Entity entity = schema.CreateEntity(UserConnection);
	entity.SetDefColumnValues();

	_listener.OnSaving(entity, null);

	entity.Validate();

	entity.ValidationMessages.Count.Should().BeGreaterThan(0);
	entity.ValidationMessages[0].Text.Should().Contain("Validation failed");
}
```

## Validation Assertions

- Assert that validation messages were added.
- Assert the failing column when the listener sets it.
- Assert the visible message text or a stable part of it.
- Prefer testing the user-visible validation outcome over private helper implementation.

## If Direct Validation Trigger Differs

Some workspace fixtures expose validation differently. If `entity.Validate()` is not the right trigger in the local package tests:

1. Inspect existing entity-validation tests in the package.
2. Reuse the local trigger pattern.
3. Keep the skill output explicit about which trigger path was chosen and why.

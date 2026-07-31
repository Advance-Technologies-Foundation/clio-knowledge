# Review Checklist

## Fixture And Setup

1. Test fixture uses the workspace base class expected for configuration tests.
2. Listener instance is fresh per test when private state is involved.
3. Helper dependencies are substituted and rebound before the listener is exercised.
4. Entity schema is instantiated through `UserConnection.EntitySchemaManager`.

## Behavioral Coverage

1. Tests call the minimal event set required for the scenario.
2. Multi-event scenarios preserve the real lifecycle order.
3. Assertions focus on observable listener behavior: helper call, validation message, or entity mutation.
4. Validation tests verify `ValidationMessages` for expected failures.

## Common Defects

- Calling `OnSaved` without first calling `OnSaving` when the listener caches `UserConnection`
- Reusing one listener instance across multiple tests
- Over-mocking the entity instead of using the real schema instance
- Testing helper internals rather than the listener-to-helper collaboration
- Using the wrong event order for create or update scenarios

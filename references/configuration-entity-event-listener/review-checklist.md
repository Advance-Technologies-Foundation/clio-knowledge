# Review Checklist

## Listener Contract

1. Class inherits `BaseEntityEventListener`.
2. `[EntityEventListener(SchemaName = "...")]` matches the real entity schema code.
3. File path and namespace match package conventions.
4. Class name follows `<EntitySchemaName>EntityEventListener` unless a package convention overrides it.

## Event Choice

1. Selected overrides match the business requirement.
2. Create, update, and delete flows respect the real event order.
3. Shared logic is not duplicated across insert and update without need.

## Implementation Quality

1. Listener stays thin and delegates business logic.
2. `Entity` and `UserConnection` extraction is safe and clear.
3. Any private state stored between events is operation-scoped and justified.
4. Validation uses `ValidationMessages` rather than exceptions for expected failures.
5. `base.On...` calls match the override being implemented.

## Tests And Validation

1. Unit tests or other automated coverage were added or updated when behavior changed.
2. Build and relevant tests were run, or a blocker is documented.
3. If the listener affects user-visible validation, tests or evidence cover the failing and passing cases.

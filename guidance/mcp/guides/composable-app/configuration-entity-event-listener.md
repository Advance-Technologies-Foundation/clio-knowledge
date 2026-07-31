# Configuration Entity Event Listener

## Non-Negotiable Rules

- Inherit `Terrasoft.Core.Entities.Events.BaseEntityEventListener`.
- Add `[EntityEventListener(SchemaName = "<EntitySchemaName>")]` with the exact schema code.
- Name the class `<EntitySchemaName>EntityEventListener` unless the package already uses another convention.
- Keep the listener thin. Move business rules, side effects, and heavy queries into helper or service classes.
- Use `sender as Entity` or `(Entity)sender` and work through `entity.UserConnection`.
- Do not throw for expected business or validation flow when the workspace already uses value-based error handling.
- For validation, prefer `entity.Validating += ...` and add `EntityValidationMessage` entries instead of throwing.

## Required Location And Shape

Source path:
- `packages/<PACKAGE_NAME>/Files/src/cs/EntryPoints/EntityEventListeners/<EntitySchemaName>EntityEventListener.cs`

Namespace:
- `<PackageNamespace>.EntryPoints.EntityEventListeners`

Minimal skeleton:

```csharp
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Entities.Events;

namespace <PackageNamespace>.EntryPoints.EntityEventListeners {
	[EntityEventListener(SchemaName = "<EntitySchemaName>")]
	public class <EntitySchemaName>EntityEventListener : BaseEntityEventListener {
		public override void OnSaving(object sender, EntityBeforeEventArgs e) {
			base.OnSaving(sender, e);
			Entity entity = (Entity)sender;
			UserConnection userConnection = entity.UserConnection;
		}
	}
}
```

## Default Workflow

1. Confirm the exact entity schema code and package namespace.
2. Create or update the listener under `Files/src/cs/EntityEventListeners/`.
3. Choose the smallest event surface that solves the task.
4. Capture any state needed across events in private fields only when the same listener instance will reuse it during one operation.
5. Delegate business logic to a helper or application service.
6. Add or update unit tests when production behavior changes.
7. Build and run relevant tests.

## Event Order You Must Respect

Create flow:
1. `OnSaving`
2. `OnInserting`
3. `OnInserted`
4. `OnSaved`

Update flow:
1. `OnSaving`
2. `OnUpdating`
3. `OnUpdated`
4. `OnSaved`

Delete flow:
1. `OnDeleting`
2. `OnDeleted`

## State Across Events

- Treat one listener instance as reusable during the same entity operation.
- Store only operation-scoped state in private fields, for example `UserConnection`, flags, or values captured before save and used after save.
- Initialize lazily when helper construction depends on `UserConnection`.
- Do not let the listener become a service locator for unrelated logic.

## Validation Pattern

- Subscribe to `entity.Validating` from `OnSaving` when entity validation must happen in the save pipeline.
- Add one or more `EntityValidationMessage` items to `entity.ValidationMessages`.
- Point the message to the failing column when possible.
- Keep validation text user-facing and specific.

## Read These References

- `docs://knowledge/com.creatio.clio/reference.configuration-entity-event-listener.listener-patterns`: class structure, naming, event selection, and thin-listener patterns
- `docs://knowledge/com.creatio.clio/reference.configuration-entity-event-listener.validation-patterns`: validation subscription and `EntityValidationMessage` examples
- `docs://knowledge/com.creatio.clio/reference.configuration-entity-event-listener.review-checklist`: review points and common defects

## What To Report Back

- Which entity schema and event hooks were used
- Whether logic stayed inside the listener or moved into a helper, and why
- Tests added or updated, or the reason tests were not changed
- Build and test commands run, or the exact blocker if they were not run
# Listener Patterns

## Core Shape

Use this base structure:

```csharp
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Entities.Events;

namespace <PackageNamespace>.EntryPoints.EntityEventListeners {
	[EntityEventListener(SchemaName = "AccountAnniversary")]
	public class AccountAnniversaryEntityEventListener : BaseEntityEventListener {
		private UserConnection _userConnection;
		private IAccountAnniversaryHelper _helper;

		private IAccountAnniversaryHelper Helper => _helper ??=
			ClassFactory.Get<IAccountAnniversaryHelper>(
				new ConstructorArgument("userConnection", _userConnection));

		public override void OnSaving(object sender, EntityBeforeEventArgs e) {
			base.OnSaving(sender, e);
			Entity entity = (Entity)sender;
			_userConnection = entity.UserConnection;
		}

		public override void OnSaved(object sender, EntityAfterEventArgs e) {
			base.OnSaved(sender, e);
			Helper.CalculateRegistrationsAndBookedValue();
		}
	}
}
```

## Choose The Right Event

- Use `OnSaving` for logic shared by both insert and update.
- Use `OnInserting` and `OnInserted` only for create-specific behavior.
- Use `OnUpdating` and `OnUpdated` only for update-specific behavior.
- Use `OnDeleting` and `OnDeleted` only for delete-specific behavior.
- Use `OnSaved` when work must happen after both insert and update are completed in the entity pipeline.

## Private State Between Events

Valid uses:

- cache `UserConnection`
- cache old or derived values captured before save and reused after save
- lazy-create a helper that depends on `UserConnection`

Avoid:

- long-lived mutable caches unrelated to one operation
- direct heavy business logic in every override
- subscribing to unrelated events without a clear reason

## Thin Listener Pattern

Prefer:

1. Extract entity and `UserConnection`.
2. Gather only the small amount of context required for later steps.
3. Call a helper, domain service, or application service.

Avoid:

1. Duplicating business rules across multiple overrides.
2. Hiding complex SQL or repository logic directly in the listener.
3. Letting one listener orchestrate many unrelated responsibilities.

## Common Review Defects

- Wrong `base.*` call, for example `base.OnSaving` inside `OnSaved`
- Missing `SchemaName` or wrong schema code in `[EntityEventListener]`
- Listener class name does not match workspace convention
- Heavy logic embedded directly in overrides instead of a helper
- Using a more specific event when `OnSaving` or `OnSaved` would be clearer
- Forgetting that create and update have different event sequences

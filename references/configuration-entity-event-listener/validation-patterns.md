# Validation Patterns

## Subscribe From `OnSaving`

Validation runs immediately after `OnSaving`, so subscribe there:

```csharp
using Terrasoft.Common;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Entities.Events;

[EntityEventListener(SchemaName = "AccountAnniversary")]
public class AccountAnniversaryEntityEventListener : BaseEntityEventListener {
	public override void OnSaving(object sender, EntityBeforeEventArgs e) {
		base.OnSaving(sender, e);
		Entity entity = (Entity)sender;
		entity.Validating += OnValidating;
	}

	private void OnValidating(object sender, EntityValidationEventArgs e) {
		Entity entity = (Entity)sender;
		if (CheckIsEntityValid(entity, out string invalidColumn, out string invalidMessage)) {
			return;
		}

		entity.ValidationMessages.Add(new EntityValidationMessage {
			MessageType = MessageType.Error,
			Column = entity.Schema.Columns.FindByName(invalidColumn),
			Text = $"Validation failed for column: {invalidColumn}, due to {invalidMessage}"
		});
	}
}
```

## Validation Guidance

- Keep the validation function deterministic and side-effect free.
- Return the failing column code when possible so Creatio can point the user to the exact field.
- Prefer one clear message over many vague messages.
- If multiple checks are required, add multiple validation messages only when the UI benefits from seeing all of them at once.
- Keep validation in a helper when the same rule is reused elsewhere.

## Review Notes

- Use `MessageType`, not misspelled property names.
- Resolve the column through `entity.Schema.Columns.FindByName(...)`.
- Keep the handler private unless tests or shared infrastructure require wider scope.
- If repeated saves may attach duplicate handlers in the same flow, inspect the surrounding code and prevent duplicate subscription when needed.

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
			MassageType = MessageType.Error,
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

- Match the property name exposed by the target Creatio reference assembly. The verified Creatio Core contract, including the supported Creatio 10.0 .NET 8 target, exposes the legacy `EntityValidationMessage.MassageType` spelling; `EntityValidationMessage.MessageType` does not compile there.
- Keep the enum name distinct from the property name: use `MassageType = MessageType.Error`.
- Do not normalize `MassageType` to the corrected English spelling unless the selected target assembly actually exposes `MessageType`.
- Resolve the column through `entity.Schema.Columns.FindByName(...)`.
- Keep the handler private unless tests or shared infrastructure require wider scope.
- If repeated saves may attach duplicate handlers in the same flow, inspect the surrounding code and prevent duplicate subscription when needed.

Verified boundary: Creatio Core `Terrasoft.Core.Entities.EntityValidationMessage` source at commit `70ce6dcfa30085aebb96443ad1ee6e0baebdace5` declares `MassageType`, and `EntityValidationMessageCollection.HasErrors()` reads that property. The same contract was reported from a supported Creatio 10.0 .NET 8 reference assembly. For another runtime version, compile against that runtime's references and preserve its exact public member name.

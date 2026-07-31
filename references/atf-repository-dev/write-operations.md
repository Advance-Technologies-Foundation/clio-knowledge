# Write Operations

## Insert

Preferred `IAppDataContext` pattern in this workspace:

```csharp
ContactModel model = appDataContext.CreateModel<ContactModel>();
model.Name = "New contact";
model.AccountId = accountId;

var saveResult = appDataContext.Save();
if (!saveResult.Success) {
	// Map the failure to the workspace error-as-value pattern.
}
```

## Update

Load the existing model, change mapped properties, then save:

```csharp
ContactModel model = appDataContext.GetModel<ContactModel>(contactId);
model.Name = "Updated name";
model.AccountId = newAccountId;

var saveResult = appDataContext.Save();
```

## Delete

Mark the tracked model for deletion, then save:

```csharp
ContactModel model = appDataContext.GetModel<ContactModel>(contactId);
appDataContext.DeleteModel(model);

var saveResult = appDataContext.Save();
```

Equivalent repository style:

## Save Result Handling

In this workspace DLL, `IAppDataContext.Save()` returns `ISaveResult` with:

- `Success`
- `RowsAffected`
- `ErrorMessage`

Use that result to make write behavior explicit:

```csharp
var saveResult = appDataContext.Save();
if (!saveResult.Success) {
	// In production code, map this to the workspace error-handling pattern.
}
```

## Write Guidance

- Create new models only through repository APIs so defaults are applied correctly.
- For updates, load first, then mutate properties, then save once.
- For deletes, call repository delete API instead of trying to hack `Entity` state manually.
- Prefer one `Save()` per logical unit of work.
- If application logic expects business-validation failures, map them to the project error-as-value pattern rather than throwing for expected flow.

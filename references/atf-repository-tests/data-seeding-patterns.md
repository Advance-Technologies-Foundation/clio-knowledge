# Data Seeding Patterns

## Typed Seed

Use typed seeding when the model class already exists and typed setup is clearer.

```csharp
dataProvider.AddModel<Contact>(model => {
	model.Id = contactId;
	model.Name = "Alex";
	model.AccountId = accountId;
});
```

Fixed id overload:

```csharp
dataProvider.AddModel<Contact>(contactId, model => {
	model.Name = "Alex";
	model.AccountId = accountId;
});
```

## Raw Seed

Use raw seed dictionaries when you need a quick table-style setup or when constructing typed models is noisy.

Prefer raw seed by default when:

- the test is about a reverse relation such as `Contact -> Account by Owner`
- the test needs to make foreign-key links visually obvious
- the available `ATF.Repository.Mock` version may not expose the same `AddModel(...)` helper overloads
- table-style setup is easier to review than a sequence of typed lambdas

```csharp
dataProvider.DataStore.RegisterModelSchema<Contact>();
dataProvider.DataStore.AddModelRawData("Contact", new List<Dictionary<string, object>> {
	new Dictionary<string, object> {
		["Id"] = contactId,
		["Name"] = "Alex",
		["Account"] = accountId
	}
});
```

Reverse-relation example:

```csharp
dataProvider.DataStore.RegisterModelSchema(typeof(ContactModel), typeof(AccountModel));
dataProvider.DataStore.AddModelRawData("Contact", new List<Dictionary<string, object>> {
	new Dictionary<string, object> {
		["Id"] = contactId,
		["Name"] = "Alex Carter",
		["Surname"] = "Carter",
		["GivenName"] = "Alex"
	}
});
dataProvider.DataStore.AddModelRawData("Account", new List<Dictionary<string, object>> {
	new Dictionary<string, object> {
		["Id"] = accountId,
		["Name"] = "Alpha",
		["Owner"] = contactId
	}
});
```

Prefer typed seeding when it is clearly supported in the current mock package and the test data shape stays simple.

## Relation Graph Seeding

When testing nested lookups, seed the full chain:

- root record
- related lookup record
- nested lookup record if assertions or filters depend on it

Example:

- `Account.Owner`
- `Owner.DecisionRole`
- `Owner.DecisionRole.Name`

Requires seeding:

- `Account`
- `Contact`
- `ContactDecisionRole`

For reverse relations, seed:

- the master rows
- the detail rows
- the foreign-key column that links the detail rows back to the master

## Test Data Rule

Seed only the rows needed to prove the behavior.

Use:

- one matching record
- one non-matching record when filtering matters
- the minimum related rows needed for navigation assertions

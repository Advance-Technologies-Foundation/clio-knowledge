# Query Patterns

## Querying Through Models

Primary pattern:

```csharp
var contacts = appDataContext.Models<ContactModel>()
	.Where(x => x.Name.Contains("Alex"))
	.OrderBy(x => x.Name)
	.ToList();
```

Repository queries are model-based, so filter, sort, and project against model properties rather than raw schema column strings whenever possible.

## Allowed LINQ Members

When querying from `Models<T>()`, use only:

- `Skip`
- `Take`
- `FirstOrDefault`
- `First`
- `Where`
- `OrderBy`
- `OrderByDescending`
- `ThenBy`
- `ThenByDescending`
- `Max`
- `Min`
- `Average`
- `Sum`
- `Count`
- `Any`
- `Select`
- `GroupBy`

Calling other LINQ methods can lead to runtime errors.

## Common Filters

Boolean:

```csharp
var activeContacts = appDataContext.Models<ContactModel>()
	.Where(x => x.Active)
	.ToList();
```

Numeric or date comparisons:

```csharp
var matureContacts = appDataContext.Models<ContactModel>()
	.Where(x => x.Age >= 50)
	.ToList();
```

Combined conditions:

```csharp
var filtered = appDataContext.Models<ContactModel>()
	.Where(x => x.Age > 10)
	.Where(x => x.TypeId == contactTypeId)
	.ToList();
```

Paging and sorting:

```csharp
var page = appDataContext.Models<ContactModel>()
	.Where(x => x.Name.Contains("Abc"))
	.OrderBy(x => x.CreatedOn)
	.Skip(20)
	.Take(10)
	.ToList();
```

## Navigating Relations

Direct relation:

```csharp
var accountName = contact.Account.Name;
```

Reverse relation:

```csharp
var primaryAddresses = contact.Addresses
	.Where(x => x.IsPrimary)
	.ToList();
```

## Loading A Single Existing Model

`IAppDataContext` pattern in this workspace:

```csharp
ContactModel model = appDataContext.GetModel<ContactModel>(contactId);
```

## Practical Query Guidance

- Start with `Models<T>()` when you need filtering.
- Use `GetModel<T>(id)` when the identifier is already known and the operation is about one record.
- Keep query expressions simple and readable; split very complex filtering into named intermediate steps when needed.
- Model only the relations you actually need to navigate in the current feature.

## Allowed Aggregations

For aggregation queries, use only:

- `Average`
- `Count`
- `Max`
- `Min`
- `Sum`

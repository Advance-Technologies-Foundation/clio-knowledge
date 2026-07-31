# Provider And Context

## Roles

`IDataProvider` is the low-level transport for repository operations.

Its job is to talk to the actual data source:

- local Creatio runtime through `UserConnection`
- remote Creatio instance through HTTP-based provider implementations

`IAppDataContext` is the higher-level repository work surface.

Its job is to:

- create tracked models
- expose queryable model sets through `Models<T>()`
- load single models
- mark models for deletion
- persist accumulated changes through `Save()`

Short version:

- `IDataProvider` = how repository talks to data
- `IAppDataContext` = how application code works with models

## Creating A Local Provider

Use this when the code runs inside Creatio and already has `UserConnection`.

```csharp
using ATF.Repository;
using ATF.Repository.Providers;

IDataProvider dataProvider = new LocalDataProvider(userConnection);
IAppDataContext appDataContext = AppDataContextFactory.GetAppDataContext(dataProvider);
```

## Creating A Remote Provider

Use this when code must talk to another Creatio instance instead of the current runtime.

`RemoteDataProvider` is not `IDisposable`. Do not wrap it in `using` or `await using`.

Cookie-based auth:

```csharp
IDataProvider dataProvider =
	new RemoteDataProvider(applicationUrl, username, password, isNetCore);
```

Credentials-based auth:

```csharp
IDataProvider dataProvider =
	new RemoteDataProvider(applicationUrl, credentials, isNetCore);
```

OAuth-based auth:

```csharp
IDataProvider dataProvider =
	new RemoteDataProvider(applicationUrl, authApp, clientId, clientSecret, isNetCore);
```

Then:

```csharp
IAppDataContext appDataContext = AppDataContextFactory.GetAppDataContext(dataProvider);
```

## Standalone Console App Pattern

Use this for small external utilities that read or write against a remote Creatio instance.

```csharp
using ATF.Repository;
using ATF.Repository.Attributes;
using ATF.Repository.Providers;

[Schema("Contact")]
public class ContactModel : BaseModel {
	[SchemaProperty("Name")]
	public string Name { get; set; }

	[SchemaProperty("Surname")]
	public string Surname { get; set; }

	[SchemaProperty("GivenName")]
	public string GivenName { get; set; }
}

const string url = "http://localhost:5000";
const string userName = "Supervisor";
const string password = "Supervisor";
const bool isNetCore = true;

RemoteDataProvider dataProvider = new(url, userName, password, isNetCore);
IAppDataContext appDataContext = AppDataContextFactory.GetAppDataContext(dataProvider);

foreach (ContactModel contact in appDataContext.Models<ContactModel>()
		.OrderBy(item => item.Surname)
		.ThenBy(item => item.GivenName)
		.ThenBy(item => item.Name)) {
	Console.WriteLine($"{contact.Surname}, {contact.GivenName} | {contact.Name}");
}
```

Before writing the app, install the current latest stable `ATF.Repository` package from NuGet for the target project.

## Minimal Remote Reporting Pattern

Use this when the task is a small report or console app and only needs a few scalar fields plus one reverse relation.

- Hand-author only the scalar properties the report prints.
- Add a `DetailProperty` collection when the report needs child rows from the already loaded master rows.
- Avoid a second top-level `Models<T>()` query just to regroup child data client-side when the relationship can be modeled directly.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ATF.Repository;
using ATF.Repository.Attributes;
using ATF.Repository.Providers;

[Schema("Contact")]
public class ContactModel : BaseModel {
	[SchemaProperty("Name")]
	public string Name { get; set; }

	[DetailProperty(nameof(AccountModel.OwnerId))]
	public virtual List<AccountModel> OwnedAccounts { get; set; }
}

[Schema("Account")]
public class AccountModel : BaseModel {
	[SchemaProperty("Name")]
	public string Name { get; set; }

	[SchemaProperty("Owner")]
	public Guid OwnerId { get; set; }
}

RemoteDataProvider dataProvider = new(url, userName, password, isNetCore);
IAppDataContext appDataContext = AppDataContextFactory.GetAppDataContext(dataProvider);

foreach (ContactModel contact in appDataContext.Models<ContactModel>().OrderBy(x => x.Name)) {
	int ownedAccountsCount = contact.OwnedAccounts.Count;
	string singleAccountName = ownedAccountsCount == 1
		? contact.OwnedAccounts[0].Name
		: string.Empty;

	Console.WriteLine($"{contact.Name} | Owned accounts: {ownedAccountsCount} {singleAccountName}");
}
```

## DI Registration

Preferred local pattern in Creatio package code:

```csharp
// UserConnection is owned by the Creatio platform. Inject it through a Func accessor so the
// DI container never tracks or disposes the per-request connection.
serviceCollection.AddTransient<Func<UserConnection>>(sp => () => UserConnection);
serviceCollection.AddScoped<IDataProvider>(sp =>
	new LocalDataProvider(sp.GetRequiredService<Func<UserConnection>>()()));
```

Guidance:

- Never register `UserConnection` as scoped or transient: the container would dispose the
  platform-owned connection when the scope closes. Inject `Func<UserConnection>` and call it.
- Register `IDataProvider` as scoped.
- Do not register `IAppDataContext` in DI.
- Create `IAppDataContext` from the current `IDataProvider` close to the operation that uses it.
- Resolve `IDataProvider` from application services rather than newing it up deep inside business logic methods.

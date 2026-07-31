# Models And Relations

## What A Model Is

A repository model is a C# class that maps one Creatio entity schema to a tracked object graph.

Rules:

- Inherit `BaseModel`
- Mark the class with `[Schema("<EntitySchemaName>")]`
- Map scalar columns with `[SchemaProperty("<ColumnName>")]`
- Keep CLR property types aligned with Creatio column types
- Do not use `Nullable<T>` or `T?` in model properties

Example:

```csharp
using System;
using System.Collections.Generic;
using ATF.Repository;
using ATF.Repository.Attributes;

[Schema("Contact")]
public class ContactModel : BaseModel {
	[SchemaProperty("Name")]
	public string Name { get; set; }

	[SchemaProperty("Account")]
	public Guid AccountId { get; set; }

	[LookupProperty("Account")]
	public virtual AccountModel Account { get; set; }

	[DetailProperty(nameof(ContactAddressModel.ContactId))]
	public virtual List<ContactAddressModel> Addresses { get; set; }
}

[Schema("Account")]
public class AccountModel : BaseModel {
	[SchemaProperty("Name")]
	public string Name { get; set; }
}

[Schema("ContactAddress")]
public class ContactAddressModel : BaseModel {
	[SchemaProperty("Contact")]
	public Guid ContactId { get; set; }

	[SchemaProperty("Address")]
	public string Address { get; set; }
}
```

## Direct Relation

Use `[LookupProperty("<LookupColumnName>")]` on a `virtual` property of another model type.

This is the standard choice for:

- many-to-one navigation
- one-to-one style navigation where the link is stored as a lookup column

Pattern:

```csharp
[SchemaProperty("Owner")]
public Guid OwnerId { get; set; }

[LookupProperty("Owner")]
public virtual ContactModel Owner { get; set; }
```

## Reverse Relation

Use `[DetailProperty("<DetailModelPropertyName>")]` on a `virtual List<TDetail>` property in the master model.

This is the standard choice for:

- one-to-many collections
- traversing detail rows from the master model
- point the attribute to the detail model property name marked by `[SchemaProperty(...)]`

Pattern:

```csharp
[DetailProperty(nameof(OrderLineModel.OrderId))]
public virtual List<OrderLineModel> Lines { get; set; }
```

Where the detail model contains the actual mapped foreign key:

```csharp
[SchemaProperty("Order")]
public Guid OrderId { get; set; }
```

Reporting example with `Contact -> Account by Owner`:

```csharp
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
```

## Many-To-Many

ATF.Repository works best when the link entity is modeled explicitly.

Recommended approach:

1. Create a model for the link schema.
2. Expose a detail collection from the master model to the link model.
3. Add lookup navigation from the link model to each side.

Pattern:

```csharp
[Schema("UsrContactRole")]
public class ContactRoleLinkModel : BaseModel {
	[SchemaProperty("Contact")]
	public Guid ContactId { get; set; }

	[LookupProperty("Contact")]
	public virtual ContactModel Contact { get; set; }

	[SchemaProperty("Role")]
	public Guid RoleId { get; set; }

	[LookupProperty("Role")]
	public virtual RoleModel Role { get; set; }
}
```

This keeps repository mapping explicit and avoids hiding the link schema.

## DetailProperty Rule

For `DetailProperty`, do not pass the schema column name directly.

Use the name of the detail-model property that is marked with `[SchemaProperty(...)]`, preferably through `nameof(...)`.

Pattern:

```csharp
[Schema("Account")]
public class Account: BaseModel {

	[SchemaProperty("Name")]
	public string Name { get; set; }

	[DetailProperty(nameof(AccountAddress.AccountId))]
	public virtual List<AccountAddress> CollectionOfAccountAddressByAccount { get; set; }
}

[Schema("AccountAddress")]
public class AccountAddress: BaseModel {

	[SchemaProperty("Name")]
	public string Name { get; set; }

	[SchemaProperty("Account")]
	public Guid AccountId { get; set; }
}
```

## Practical Modeling Guidance

- Keep model names business-readable even if they differ from schema names.
- Do not add navigation properties unless the feature actually needs them.
- For write-heavy scenarios, always keep the scalar foreign key property alongside the navigation property.
- For detail collections, point `DetailProperty` to the detail model property name, preferably via `nameof(...)`.
- For read-only reporting tasks, prefer traversing a modeled detail collection from the master row over running a separate top-level query and regrouping in memory.

# Minimal Test Setup

Use this as the smallest complete example when the skill needs to remember the wiring pattern for repository tests.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ATF.Repository;
using ATF.Repository.Mock;
using FluentAssertions;
using NUnit.Framework;

[Test]
[Description("Returns matching contacts for an account")]
public void Execute_ReturnsMatchingContacts() {
	// Arrange
	Guid accountId = Guid.NewGuid();
	MemoryDataProviderMock dataProvider = new MemoryDataProviderMock();
	dataProvider.DataStore.RegisterModelSchema<Contact>();
	dataProvider.AddModel<Contact>(model => {
		model.Id = Guid.NewGuid();
		model.Name = "Alex";
		model.AccountId = accountId;
	});
	IAppDataContext appDataContext = AppDataContextFactory.GetAppDataContext(dataProvider);

	// Act
	List<Contact> result = appDataContext.Models<Contact>()
		.Where(x => x.AccountId == accountId)
		.ToList();

	// Assert
	result.Should().HaveCount(1, because: "one seeded contact belongs to the requested account");
result[0].Name.Should().Be("Alex", because: "the seeded contact should be returned");
}
```

When the test depends on reverse relations or the `ATF.Repository.Mock` helper surface is uncertain, switch this example to `AddModelRawData(...)` instead of assuming `AddModel(...)` is available in the current package version.

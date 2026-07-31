# Assertion Patterns

## Test Layout

Use explicit AAA sections:

```csharp
// Arrange
// Act
// Assert
```

Decorate every test with:

```csharp
[Description("...")]
```

## Query Assertions

Assert through the query result:

```csharp
result.Should().HaveCount(1, because: "only one seeded record matches the filter");
result.Single().Name.Should().Be("Alex", because: "the matching seeded record should be returned");
```

## Save Assertions

For insert/update/delete, assert both save result and final state:

```csharp
var saveResult = appDataContext.Save();
saveResult.Success.Should().BeTrue(because: "the in-memory save should succeed");

List<Contact> contacts = appDataContext.Models<Contact>().ToList();
contacts.Should().ContainSingle(x => x.Name == "Updated", because: "the updated record should be persisted in memory");
```

Delete example:

```csharp
var saveResult = appDataContext.Save();
saveResult.Success.Should().BeTrue(because: "delete should complete successfully");

appDataContext.Models<Contact>()
	.Any(x => x.Id == contactId)
	.Should()
	.BeFalse(because: "deleted records should no longer be returned");
```

## Recommended Assertions

Prefer asserting:

- returned collections
- selected scalar values
- relation navigation results
- `Save()` success and final in-memory state

Avoid asserting:

- private implementation details
- internal intermediate collections that are not part of the observable behavior

## Workspace Style

Use:

- NUnit `[Test]` and `[Description]`
- explicit Arrange/Act/Assert comments
- FluentAssertions with `because`

Match the existing local test tone unless a target test file already follows a stronger convention.

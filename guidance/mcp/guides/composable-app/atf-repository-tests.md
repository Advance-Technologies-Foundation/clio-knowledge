# ATF.Repository Tests

## Use This Skill When

- Production code uses `ATF.Repository` and needs unit or component-style tests.
- Tests should run against `MemoryDataProviderMock` instead of a real `UserConnection`.
- You need to seed in-memory records for repository queries or write operations.
- You need to register repository models and relation graphs before exercising code under test.
- You need to verify repository-driven read, insert, update, or delete behavior.

## Package Acquisition

- For test projects that do not already reference the repository test tooling, install `ATF.Repository.Mock` from NuGet as the default package for repository tests.
- Verify the latest stable `ATF.Repository.Mock` version on NuGet at task time, then install that exact version instead of searching for local DLLs or copying binaries from other projects.
- Prefer `dotnet add <test-project> package ATF.Repository.Mock --version <latest-stable>` or an explicit `<PackageReference />` with the verified latest stable version.
- `ATF.Repository.Mock` brings a compatible `ATF.Repository` dependency, but if the solution already pins `ATF.Repository`, align the versions deliberately and report the choice.

## Non-Negotiable Rules

- Add or update tests when repository-based production code changes unless the user explicitly says not to.
- Use `MemoryDataProviderMock` as the default repository test double when repository behavior itself is under test.
- Build `IAppDataContext` from the mock provider with `AppDataContextFactory.GetAppDataContext(...)`.
- Register every model involved in the tested query or relation graph in `DataStore` before seeding or querying.
- Prefer `AddModelRawData(...)` when seeding relation-heavy scenarios or reverse relations because it makes the foreign-key links explicit and avoids relying on mock helper overloads that may vary by package version.
- If a task requires creating or changing models for the test scenario, apply `atf-repository-model-management` before writing the tests.
- Structure every test method with explicit `// Arrange`, `// Act`, and `// Assert`.
- Decorate every test with `[Description("...")]`.
- Use assertion messages or FluentAssertions `because` reasons so failures explain intent.
- Prefer asserting observable repository outcomes by re-querying the in-memory context after `Save()` rather than inspecting implementation details.
- If tests require models that do not yet exist in project code, apply `atf-repository-model-management` first.

## Core Workflow

1. Identify the repository models and relation graph required by the production behavior.
2. Create `MemoryDataProviderMock`.
3. Register all required models in `mock.DataStore`.
4. Seed data with `AddModel(...)`, `AddModel(recordId, ...)`, or `AddModelRawData(...)`.
5. For reverse relations, lookup chains, or version-sensitive mock APIs, prefer `AddModelRawData(...)`.
6. Create `IAppDataContext` with `AppDataContextFactory.GetAppDataContext(mock)`.
7. Execute the production logic.
8. Assert returned values, save result, and final repository state by querying the in-memory context.

## References

Read only what you need:

- `references/minimal-test-setup.md`: a compact end-to-end test example with `MemoryDataProviderMock`
- `references/memory-provider-setup.md`: creating `MemoryDataProviderMock`, registering schemas, and building `IAppDataContext`
- `references/data-seeding-patterns.md`: `AddModel`, `AddModelRawData`, fixed ids, defaults, and relation graph setup
- `references/assertion-patterns.md`: AAA layout, local assertion style, and how to verify query and save outcomes
- `references/build-and-verify.md`: build and test command patterns for repository test work

## Build And Verify

Use `references/build-and-verify.md` when you need concrete command patterns.

Use the configuration that matches the current target framework and repository setup, and run build and test sequentially unless the current environment has a documented reason to do otherwise.

## What To Report Back

- test files changed, with one-line reason per file
- which repository models were registered in the mock data store
- how the test data was seeded
- what repository behavior each test proves
- build/test commands run, or the exact blocker if not run
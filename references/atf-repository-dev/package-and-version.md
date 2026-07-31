# Package And Version

## Package Acquisition

Use this only when the project does not already reference `ATF.Repository` or when the existing package version looks uncertain.

- For standalone apps, external utilities, and projects that do not already reference `ATF.Repository`, use NuGet as the default acquisition path.
- Verify the latest stable `ATF.Repository` version on NuGet at task time, then install that exact version instead of hunting for local DLLs, package caches, or unrelated project references.
- Prefer a normal package install such as `dotnet add <project> package ATF.Repository --version <latest-stable>` or an explicit `<PackageReference />` with the verified latest stable version.
- If the user explicitly asks to use a local repository checkout, a bundled DLL, or an existing solution reference, follow that instead and state the reason.

## Version Check

Confirm the actual `IAppDataContext` surface available in the current codebase or package version before relying on helper methods.

Common members include:

- `CreateModel<T>()`
- `GetModel<T>(Guid id)`
- `DeleteModel(T model)`
- `Models<T>()`
- `Save()`

Use the verified API surface exposed by the referenced `ATF.Repository` version.

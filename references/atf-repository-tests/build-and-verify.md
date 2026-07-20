# Build And Verify

Use the configuration that matches the current target framework and repository setup.

Typical commands:

```powershell
dotnet build <solution-or-project> -c <configuration>
dotnet test <test-project> -c <configuration> --no-build
```

Run build and test sequentially unless the current environment has a documented reason to do otherwise.

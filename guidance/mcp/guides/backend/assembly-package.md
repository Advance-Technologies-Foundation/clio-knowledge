clio MCP assembly-package guide - server-side C# in a package that compiles into its own assembly

Scope
This guide owns the assembly package (`SysPackage.Type` = 1, the designer's "Compile into a separate assembly"):
when to choose one, how it is registered, how its C# gets built or shipped, what it builds against, what changes at
runtime, and C# background jobs (`IJobExecutor`). Other owners: `configuration-webservice` (writing a web service
class, routes, the CSRF header), `core-rules` (the compile confirmation and the restart that activates a DLL),
`core-rules-details` (precompiled delivery, `compile-package --wait` exit codes), `backend-deployment-troubleshooting`
(the running code differs from the source), `process-timer-start` (running a process on a schedule),
`process-script-task` (C# inside a process).

Delivery checklist (each step is explained below): keep `"Type": 1` and `ProjectPath` in `descriptor.json` -> give
the package compilable content, or ship a freshly built DLL -> install it (`push-pkg` / `push-workspace`; for a
workspace linked in file system mode, `fsm-mode`) -> read `type`/`projectPath` back -> compile and restart as
`core-rules` says -> call the changed service with `clio call-service`, which handles the login and the CSRF
header. Never copy files into the site's `Pkg` folder by hand.

== When to use one ==
- Default: C# that runs on the server goes into SourceCode schemas of an ordinary package. A package compile builds
  them into the configuration assembly, and only the Background work section of this guide applies. An ordinary
  package can also carry
  imported reference assemblies.
- Choose an assembly package only when the code needs its own project: references you manage in your own csproj, a
  csproj you build and test locally, or compiling the package without rebuilding the configuration assembly. Each of
  those brings the registration and compile rules below; an ordinary package has none of them.

== Registration ==
- An assembly package is `Type` 1 with `ProjectPath` `Files/<PackageName>.csproj` (relative to the package folder;
  every product assembly package uses that form). Keep the csproj named after the package: the csproj name becomes
  the DLL name, and the platform loads `Files/Bin/<PackageName>.dll`. Only a `Type` 1 package has its csproj built
  (while the `AllowStandaloneAssembly` setting is on, the default): the csproj of a `Type` 0 package is ignored, with
  no warning.
- A push or install writes `Type` and `ProjectPath` from the package's `descriptor.json`, but only when the
  descriptor counts as changed: a new package, a `ModifiedOnUtc` different from the stored one, or (Creatio 10.2.710
  or later) a different dependency set. The version is not compared, so an edit of `Type`, `ProjectPath` or the
  version alone, with the same `ModifiedOnUtc`, is silently ignored. Keep `"Type": 1` and `"ProjectPath"` in it:
  `add-package` writes both, and `pull-pkg` of an assembly package exports both (with the stored `ModifiedOnUtc`, so
  pushing it back unchanged changes nothing). `create-package` creates an ordinary package (`Type` 0).
- After every install read both back. When the `list-packages` result carries `type` and `projectPath`, read them
  there (`type` 1 = assembly package). An older clio does not return them; read `SysPackage` `Type`/`ProjectPath`
  with the read-only `execute-esq` instead. A current clio omits them only when the environment does not store them.
- A push whose descriptor counts as changed and carries no `"Type": 1` (absent or 0) resets the package to `Type` 0,
  and `ProjectPath` becomes whatever the descriptor carries (empty when absent).
- Repair a reset through the descriptor: put `"Type": 1` and `ProjectPath` back, set a new `ModifiedOnUtc`, push
  again and read back. Then run ONE full compile (asking first, `core-rules`): the install that reset the type
  compiled the package into the configuration assembly, and the restoring push leaves that copy there, so the same
  types would live in two assemblies.
- MUST NOT set `Type` or `ProjectPath` with SQL, OData or DataService writes: they skip the platform's type converter
  (it sets `ProjectPath` and resets the type cache), and the next push of a changed descriptor overwrites them
  anyway. The platform's own conversion (package properties) refuses a package installed from an archive
  (`push-pkg`: "installed from the file archive"), so it is not a repair path for pushed packages either.
- A `ProjectPath` that points at no file does not fail the compile: the platform creates a template project at that
  path and builds it instead of yours, into a DLL the platform never loads.

== Compile: source delivery ==
- On Creatio 10.0.0.171 or later (default features), a target-side compile builds an assembly package ONLY when it
  has compilable content: at least one SourceCode, process or user-task schema, or a reference assembly. C# files
  under `Files/` do not count, and in a customer package neither does an entity schema. Without it the package
  compile and the full compile log `skipped (no compilable schemas): ... [<Package>]`, the install compiles nothing
  for it, and no `Files/Bin` DLL is built: its services do not exist, or an older DLL already in `Files/Bin` keeps
  running. Creatio 8.x builds every assembly package.
- `add-package` without `--as-app` makes such a package: its C# is under `Files/src/cs` and it has no schema
  (`--as-app` adds one). For source delivery add one SourceCode schema before the push: in the workspace run
  `clio add-schema <Package>Marker -t source-code -p <path to the package folder>`.
- For source delivery the pushed package MUST NOT carry `Files/Bin/<Package>.dll` (a local `dotnet build` writes it
  there, and `push-pkg` ships it): when that DLL loads, the install skips the compile and the old DLL runs. Before
  the push remove only the package's own build output - `Files/Bin/<Package>.dll` and `.pdb`, and the same under
  `Files/Bin/netstandard` - never other DLLs, and ask the user first about files this session did not create.
  Without it the install compiles the package (the compile log lists it under `compiled:`). Then do not compile it
  again: a package compile with nothing to build logs `compiled: 0` and writes no compilation history, so clio's wait
  reports a failure although nothing failed. Call the service to see whether it runs.
- With a skipped package, `compile-package --wait` waits for compilation history that is never written and exits
  non-zero after its timeout ("wrote no compilation history"). Before recompiling, look for the package in the
  compile log's `skipped (no compilable schemas)` list; a timeout alone does not prove a skip.
- A package created as an assembly package needs a package compile of itself, and of the packages that use its
  types. One converted from an ordinary package, or installed as an ordinary package even once, also needs ONE full
  compile (asking first, `core-rules`): its package compile built the DLL and, after a restart, served the new code,
  but left its old types in the configuration assembly until a full compile removed them.
- An install that compiled the package reloaded (.NET Framework) or restarted (.NET) the application by itself in the
  measurements; verify with a call. After an explicit compile, a new or changed DLL or service runs only after the
  restart in `core-rules`.

== Compile: precompiled delivery ==
- The other path: build `Files/<PackageName>.csproj` yourself and ship the DLL as `Files/Bin/<PackageName>.dll`
  (`Files/Bin/netstandard` on .NET; `core-rules-details`, Precompiled delivery). After a restart the platform loads
  the shipped DLL, whatever the package's type and compile state.
- The shipped DLL is then the running code (this guide owns that rule). On 10.0.0.171 or later a compile of a
  package without compilable
  content skips it and leaves the shipped DLL as it is, so a source change pushed without a rebuilt DLL keeps the old
  behavior. Rebuild the DLL from the current source on every change; a DLL carried over from an earlier build or
  `pull-pkg` is stale.
- Pick one path per package: either compilable content and target-side compile with no shipped DLL, or no
  compilable content and a shipped DLL you rebuild.

== References ==
- The project targets `net472` (.NET Framework, output `Files/Bin`) or `netstandard2.0` (.NET, output
  `Files/Bin/netstandard`) - never `net8.0` or later. A DLL shipped for both runtimes needs both builds.
- A compile references, from the package's DIRECT dependencies, each assembly package's own DLL, the configuration
  assembly for an ordinary one, and the public reference assemblies of both. From Creatio 10.1.391 the public
  reference assemblies of indirect dependencies are referenced too, never an indirect assembly package's DLL: add a
  package whose types you use as a direct dependency (`package-dependencies`). Not referenced: non-public
  assemblies; on .NET, assemblies that are not netstandard; from Creatio 10.2.730, a dependency assembly package
  without compilable content.
- A compile regenerates the project's generated item groups (package references, core references it knows,
  dependency assemblies) and deletes every `<ProjectReference>`. Put your own third-party references as
  `<Reference>` with a `HintPath` in your own item group (`3rd Party References` by convention); never a
  `ProjectReference`.
- JSON: the default reference is `Terrasoft.NewtonsoftWrapper`, a small subset of Json.NET under the namespace
  `Newtonsoft.Json`: `JsonConvert.SerializeObject` / `DeserializeObject<T>` and a settings class, with no attributes
  (`[JsonProperty]`), converters, `JsonSerializer` or `JObject`/`JToken`. When you need those, add
  `<Reference Include="Newtonsoft.Json">` (exactly that `Include`, a `HintPath` to the platform's
  `Newtonsoft.Json.dll`); the regenerated project then drops the wrapper. Use one of the two, never both.

== Runtime ==
- By default (`LoadAssemblyFromByteArray` on) package assemblies are loaded from bytes, so `Assembly.Location` is an
  empty string, for the package assembly and for the configuration assembly alike. Build file paths from
  `AppDomain.CurrentDomain.BaseDirectory` plus `Terrasoft.Configuration/Pkg/<Package>/Files/...` (on .NET Framework
  the configuration folder sits under `Terrasoft.WebApp`), never from an assembly's location, and never join request
  input into such a path. An embedded resource read with `GetManifestResourceStream` also works, but a change to the
  file then needs a rebuild.
- A service in a precompiled DLL is found only when the assembly carries
  `[assembly: PackageReferenceAssembly(...)]` with service routes, which the generated csproj adds (`All`); keep it
  in your own csproj. Routes, `.svc` files, CSRF and the base class are `configuration-webservice`.

== Background work in C# ==
- An `IJobExecutor` works in an ordinary package (a SourceCode schema) as well; this section applies to both.
- Work that runs on a schedule is a timer-start process: read `process-timer-start`. Write an `IJobExecutor` only
  when a timer start cannot express the work: each run needs its own parameters, the schedule is computed at
  runtime, or a request hands work off to run later. A timer-start job carries no process parameters.
- The contract is synchronous: `void Execute(UserConnection userConnection, IDictionary<string, object> parameters)`.
  MUST NOT be `async void`: the platform closes the job's connection and DI scope as soon as `Execute` returns, which
  is at the first `await` that does not complete at once, and an exception after that escapes the job's error
  handling.
- Make `Execute` total: catch everything inside it. A throw is logged to `Scheduler.log` (`JobFail`, then
  "Error executing") and swallowed: nothing retries it, a repeating job fires again at its next time, and a one-time
  job is gone. Write retries yourself, and set your own status row to a final state in `finally`.
- Make `Execute` idempotent: jobs are stored with recovery on, so a run interrupted by a crash (or, on .NET
  Framework, an application-domain unload) runs again.
- Use the `UserConnection` passed to `Execute`: it is created for this run and closed when `Execute` returns.
  Resolve services inside `Execute`, within its `try`. The executor is created anew for every run and needs a public parameterless
  constructor (unless it has a named `DefaultBinding`). `[DefaultBinding(typeof(IJobExecutor))]` is not required.
- The stored job names the executor's assembly-qualified type: renaming its class, namespace or assembly makes
  every later run fail (`JobFail`) until the job is scheduled again.
- Pass only primitives, strings and Guids as job parameters; never the caller's `UserConnection` or entities, the job
  gets its own connection. Parameters are stored in the job store; on Creatio 10.2.26 or later its type allow-list
  reports, and once enforcement is on refuses, types from your own package's assembly. The keys `ClassName`,
  `ProcessName`, `ProcessElementUId`, `TimeZoneId`, `WorkspaceName`, `UserName`, `WorkspaceNumber`,
  `WorkspaceBaseUrl` and `IsSystemUser` are the platform's: do not use them for your data.
- Identity: `CreateAndScheduleJob<T>` always runs as the `SystemUser` system setting's user, with no rights checks.
  `CreateClassJob<T>(jobName, group, userConnection, ...)` does too unless `isSystemUser` is false.
  `ScheduleMinutelyJob<T>`, `ScheduleImmediateJob<T>` and `TriggerJob<T>` default `isSystemUser` to false and run as
  the `userName` you pass, with that user's rights. Prefer running as the user who asked for the work
  (`isSystemUser` false and that user's name); use the system user only when the task needs it, and say so. A job
  with system access checks no rights, so `Execute` MUST check rights and validate its parameters itself, and request
  input MUST NOT choose the executor type or the user.
- Scheduling: `CreateAndScheduleJob<T>` and `ScheduleMinutelyJob<T>` replace the job with the same key, so you do not
  need to remove it first. The key of `ScheduleMinutelyJob<T>` is the executor type and group: one minutely schedule
  per executor type and group. `ScheduleImmediateJob<T>` names every call's job with a new GUID (whatever its doc
  comment says), and `TriggerJob<T>` adds a run on every call: each call is one more run, never a replacement. A raw
  Quartz trigger built without `WithIdentity` gets a random key, so each save adds a trigger.
- Cron (`CreateAndScheduleJob<T>` only): Quartz syntax, 6 fields with seconds first plus an optional year; a 5-field
  Unix expression is refused; `?` in day-of-month or day-of-week; UTC when no time zone is given. A one-time
  (non-cron) `CreateAndScheduleJob<T>` whose start is not in the future is silently not registered.
- One job key never runs twice at the same time (each `ScheduleImmediateJob<T>` call is a new key, so those can),
  and the scheduler has 5 threads per node by default, shared with every platform job and timer start: a long
  `Execute` holds one of them, so split long work into chunks.
- `Task.StartNewWithUserConnection` (`IBackgroundTask`) runs in memory and is lost on a restart; a job from
  `ScheduleImmediateJob<T>` / `TriggerJob<T>` is stored and survives one.

Evidence
- Measured 2026-11 on Creatio 10.2 (.NET Framework, MSSQL), a 10.x trunk build (.NET Framework, MSSQL) and Creatio
  10.3 (.NET 10, PostgreSQL; compile filter, install compile, routes, CSRF, `Location`), clio 8.1.0.135, with
  disposable assembly packages: install copies `Type`/`ProjectPath` from the descriptor; a changed
  descriptor without `Type` reset it and a changed descriptor with it restored it; the reset install left a copy in
  the configuration assembly; the platform conversion refused a pushed package; a converted package kept its old
  types until a full compile; the `add-package` template package was skipped by the package compile and the full
  compile and not compiled by the install, and one `add-schema` SourceCode schema made the install compile it; a
  shipped `Files/Bin` DLL was served after a restart and left unchanged by a skipping compile; services answered at
  `/0/rest/...` without a `.svc`; `Assembly.Location` was empty.
- Read in Creatio platform source (10.2 and trunk), not measured: the version boundaries, the install skipping a
  package whose shipped DLL loads, references, the wrong `ProjectPath`, and every statement about background jobs.

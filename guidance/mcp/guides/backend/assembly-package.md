clio MCP assembly-package guide - server-side C# in a package that compiles into its own assembly

Scope
This guide owns the assembly package (`SysPackage.Type` = 1, the designer's "Compile into a separate assembly"):
when to choose one, registration, how its C# gets built or shipped, references, runtime paths, and C# background jobs
(`IJobExecutor`). Other owners: `configuration-webservice` (service classes, routes, CSRF), `core-rules` (compile
confirmation, restart), `core-rules-details` (precompiled delivery, `--wait` exit codes),
`backend-deployment-troubleshooting`, `process-timer-start` (schedules), `process-script-task`.

Delivery checklist (each step is explained below): keep `"Type": 1` and `ProjectPath` in `descriptor.json` -> give
the package compilable content, or ship a freshly built DLL -> install it (`push-pkg` / `push-workspace`; a workspace
linked in file system mode: `fsm-mode`) -> read `type`/`projectPath` back -> compile the package only if the install's
compile log does not list it under `compiled:` -> restart as `core-rules` says (clio's `push-pkg` / `push-workspace`
already restart a .NET environment after a successful install) -> call the service with `clio call-service`, which
handles the login and the CSRF header. MUST NOT copy files into the site's `Pkg` folder by hand.

== When to use one ==
- Default: server-side C# goes into SourceCode schemas of an ordinary package; a package compile builds them into the
  configuration assembly. Then only the Background work section and the Runtime path rule apply. An ordinary package
  can also carry imported reference assemblies.
- Choose an assembly package only when the code needs its own project: references managed in your csproj, a csproj
  you build and test locally, or compiling without rebuilding the configuration assembly.

== Registration ==
- An assembly package is `Type` 1 with `ProjectPath` `Files/<PackageName>.csproj` (relative to the package folder;
  every product assembly package uses that form). Keep the csproj named after the package: its name becomes the DLL
  name, and the platform loads `Files/Bin/<PackageName>.dll`. Only a `Type` 1 package has its csproj built (while the
  `AllowStandaloneAssembly` setting is on, the default); a `Type` 0 package's csproj is ignored, with no warning.
- A push or install writes `Type` and `ProjectPath` from `descriptor.json` only when the descriptor counts as
  changed: a new package, a `ModifiedOnUtc` different from the stored one, or (Creatio 10.2.710 or later) a
  different dependency set. The version is not compared: an edit of `Type`, `ProjectPath` or the version alone, with
  the same `ModifiedOnUtc`, is silently ignored. Keep `"Type": 1` and `"ProjectPath"` in it: `add-package` writes
  both, and `pull-pkg` of an assembly package exports both (with the stored `ModifiedOnUtc`, so pushing it back
  unchanged changes nothing). `create-package` creates an ordinary package (`Type` 0).
- After every install MUST read both back, even when the service already answers: an answering endpoint does not
  show the package type. Read them from `list-packages` (`type` 1 = assembly package). An older clio does not return
  them: read `SysPackage` `Type`/`ProjectPath` with the read-only `execute-esq`. A current clio omits them when the
  environment does not store them or stores a type it does not know.
- A push whose descriptor counts as changed and carries no `"Type": 1` (absent or 0) resets the package to `Type` 0,
  and `ProjectPath` becomes whatever the descriptor carries (empty when absent).
- Repair a reset through the descriptor: put `"Type": 1` and `ProjectPath` back, set a new `ModifiedOnUtc`, push
  again and read back. The restoring push compiles the package itself when it has a compilable schema and ships no
  `Files/Bin/<Package>.dll`; compile the package only if the install's compile log does not list it under
  `compiled:`. Then run ONE full compile (asking first, `core-rules`): the install that reset the type compiled the
  package into the configuration assembly, and the restoring push leaves that copy there.
- MUST NOT set `Type` or `ProjectPath` with SQL, OData or DataService writes: they skip the platform's type converter
  (it sets `ProjectPath`, resets the type cache), and the next changed push overwrites them anyway. The platform's
  package-properties conversion refuses a package installed from an archive (`push-pkg`: "installed from the file
  archive"), so it is no repair path for pushed packages either.
- A `ProjectPath` that points at no file does not fail the compile: the platform creates a template project there and
  builds it without your code. Its DLL is never loaded unless that file is named `<Package>.csproj`; then it
  replaces yours.

== Compile: source delivery ==
- On Creatio 10.0.0.636 or later (default features; some 10.0.0.171-635 builds too), a target-side compile builds an
  assembly package ONLY when it has compilable content: at least one SourceCode, process or user-task schema, or a
  reference assembly. C# under `Files/` does not count, nor (in a customer package) an entity schema. Without it the
  package and full compiles log `skipped (no compilable schemas): ... [<Package>]`, the install compiles nothing, and
  no `Files/Bin` DLL is built: its services do not exist, or an older DLL in `Files/Bin` keeps running. Creatio 8.x
  builds every assembly package.
- `add-package` without `--as-app` makes such a package (C# under `Files/src/cs`, no schema; `--as-app` adds one).
  For source delivery add one SourceCode schema before the push: in the workspace run
  `clio add-schema <Package>Marker -t source-code -p <path to the package folder>`.
- For source delivery the pushed package MUST NOT carry `Files/Bin/<Package>.dll` (a local `dotnet build` writes it,
  `push-pkg` ships it): when it loads, the install skips the compile and the old DLL runs. Remove only the package's
  own build output - `Files/Bin/<Package>.dll` and `.pdb`, and the same under `Files/Bin/netstandard` - never other
  DLLs; ask the user first about files this session did not create and before changing how a package is delivered.
  Then the install compiles the package (its compile log lists it under `compiled:`): do not compile it again. A
  package compile with nothing to build logs `compiled: 0` and writes no compilation history, so clio's wait reports
  a failure although nothing failed.
- With a skipped package, `compile-package --wait` waits for history that is never written and exits non-zero after
  its timeout ("wrote no compilation history"). Before recompiling, look for the package in the compile log's
  `skipped (no compilable schemas)` list; a timeout alone does not prove a skip.
- A package created as an assembly package needs a package compile of itself (the install's compile counts) and of
  the packages that use its types (asking first, `core-rules`). One converted from an ordinary package, or installed as
  an ordinary package even once, also needs ONE full compile: its package compile built the DLL and, after a restart,
  served the new code, but left its old types in the configuration assembly until a full compile removed them.
- On .NET Framework an install that compiled or changed an assembly package reloads the application itself; on .NET
  the platform does not restart, but clio's `push-pkg` / `push-workspace` restart it after a successful install.
  After an explicit compile the restart rule in `core-rules` applies.

== Compile: precompiled delivery ==
- The other path: build `Files/<PackageName>.csproj` yourself and ship the DLL as `Files/Bin/<PackageName>.dll`
  (`Files/Bin/netstandard` on .NET; `core-rules-details`). After a restart the platform loads the shipped DLL,
  whatever the package's type and compile state.
- While the package has no compilable content the shipped DLL is the running code (this guide owns that rule): a
  compile skips the package and leaves the DLL as it is, so a source change pushed without a rebuilt DLL keeps the old
  behavior. Rebuild the DLL from the current source on every change; a DLL from an earlier build or `pull-pkg` is stale.
- Pick one path per package: compilable content and target-side compile with no shipped DLL, or no compilable content
  and a shipped DLL you rebuild.

== References ==
- The project targets `net472` (.NET Framework, output `Files/Bin`) or `netstandard2.0` (.NET, output
  `Files/Bin/netstandard`) - never `net8.0` or later. A DLL shipped for both runtimes needs both builds.
- A compile references, from the package's DIRECT dependencies, each assembly package's own DLL, the configuration
  assembly for an ordinary one, and the public reference assemblies of both. From Creatio 10.1.391 the public
  reference assemblies of indirect dependencies too, never an indirect assembly package's DLL: add a package whose
  types you use as a direct dependency (`package-dependencies`). Not referenced: non-public assemblies; on .NET, ones
  that are not netstandard; from Creatio 10.2.730, a dependency assembly package without compilable content.
- A compile regenerates the generated item groups (package references, the core references it knows, dependency
  assemblies) and deletes every `<ProjectReference>`. Put third-party references as `<Reference>` with a `HintPath`
  in your own item group (`3rd Party References` by convention), never a `ProjectReference`. A third-party DLL comes
  from the user or a source they approved; never download one ad hoc.
- JSON: the default reference is `Terrasoft.NewtonsoftWrapper`, a small subset of Json.NET under the namespace
  `Newtonsoft.Json`: `JsonConvert.SerializeObject` / `DeserializeObject<T>` and a settings class, with no attributes
  (`[JsonProperty]`), converters, `JsonSerializer` or `JObject`/`JToken`. When you need those, add
  `<Reference Include="Newtonsoft.Json">` (exactly that `Include`, a `HintPath` to the platform's
  `Newtonsoft.Json.dll`); the regenerated project then drops the wrapper. Use one of the two, never both.

== Runtime ==
- By default (`LoadAssemblyFromByteArray` on) package assemblies are loaded from bytes, so `Assembly.Location` is an
  empty string, for the package assembly and for the configuration assembly alike. Build file paths from
  `AppDomain.CurrentDomain.BaseDirectory` plus `Terrasoft.Configuration/Pkg/<Package>/Files/...` (on .NET Framework
  under `Terrasoft.WebApp`), never from an assembly's location; MUST NOT join request input into such a path. An
  embedded resource read with `GetManifestResourceStream` also works, but a change to the file then needs a rebuild.
- A service in a precompiled DLL is found only when the assembly carries `[assembly: PackageReferenceAssembly(...)]`
  with service routes, which the generated csproj adds (`All`); keep it in your own csproj. Routes, `.svc` files, CSRF
  and the base class are `configuration-webservice`.

== Background work in C# ==
- An `IJobExecutor` works in an ordinary package (a SourceCode schema) as well; this section applies to both.
- Work that runs on a schedule is a timer-start process: read `process-timer-start`. Write an `IJobExecutor` only
  when a timer start cannot express the work: each run needs its own parameters, the schedule is computed at runtime,
  or a request hands work off to run later. A timer-start job carries no process parameters.
- The contract is synchronous: `void Execute(UserConnection userConnection, IDictionary<string, object> parameters)`.
  `Execute` MUST NOT be `async void`: the platform closes the job's connection and DI scope as soon as `Execute`
  returns, which is at the first `await` that does not complete at once, and a later exception escapes the job's
  error handling.
- Make `Execute` total: catch everything inside it. A throw is logged to `Scheduler.log` (`JobFail`, then
  "Error executing") and swallowed: nothing retries it, a repeating job fires again at its next time, a one-time job
  is gone. Write retries yourself, and set your own status row to a final state in `finally`.
- Make `Execute` idempotent: jobs are stored with recovery on, so a run interrupted by a crash (or, on .NET Framework,
  an application-domain unload) runs again.
- Use the `UserConnection` passed to `Execute`: it is created for this run and closed when `Execute` returns. Resolve
  services inside `Execute`, within its `try`. The executor is created anew for every run and needs a public
  parameterless constructor (unless it has a named `DefaultBinding`). `[DefaultBinding(typeof(IJobExecutor))]` is not
  required.
- The stored job names the executor's assembly-qualified type: renaming its class, namespace or assembly makes every
  later run fail (`JobFail`) until the job is scheduled again.
- Pass only primitives, strings and Guids as job parameters; never the caller's `UserConnection` or entities, and
  never passwords, tokens or keys (parameters are stored in plain text in the job store; read secrets inside
  `Execute`). On Creatio 10.2.26 or later the store's type allow-list reports, and once enforcement is on refuses,
  types from your own package's assembly. The keys `ClassName`, `ProcessName`, `ProcessElementUId`, `TimeZoneId`,
  `WorkspaceName`, `UserName`, `WorkspaceNumber`, `WorkspaceBaseUrl` and `IsSystemUser` are the platform's.
- Identity: `CreateAndScheduleJob<T>` (the only cron API) always runs as the `SystemUser` system setting's user, with
  no rights checks. `CreateClassJob<T>(jobName, group, userConnection, ...)` does too unless `isSystemUser` is false.
  `ScheduleMinutelyJob<T>`, `ScheduleImmediateJob<T>` and `TriggerJob<T>` default `isSystemUser` to false and run as
  the `userName` you pass, with that user's rights. Prefer the user who asked for the work (`isSystemUser` false, the
  name from their `UserConnection`, never from request input). For a system-user job, check the caller's rights in the
  request handler before scheduling, validate the parameters inside `Execute`, and tell the user in your summary that
  the job runs without rights checks. Request input MUST NOT choose the executor type or the user.
- Scheduling: `CreateAndScheduleJob<T>` and `ScheduleMinutelyJob<T>` replace the job with the same key, so you do not
  remove it first. The key of `ScheduleMinutelyJob<T>` is the executor type and group: one minutely schedule per type
  and group. `ScheduleImmediateJob<T>` names every call's job with a new GUID (whatever its doc comment says), and
  `TriggerJob<T>` adds a run on every call: each call is one more run, never a replacement. A raw Quartz trigger
  without `WithIdentity` gets a random key, so each save adds a trigger.
- Cron (`CreateAndScheduleJob<T>` only): Quartz syntax, 6 fields with seconds first plus an optional year; a 5-field
  Unix expression is refused; `?` in day-of-month or day-of-week; UTC when no time zone is given. A one-time
  (non-cron) `CreateAndScheduleJob<T>` whose start is not in the future is silently not registered.
- One job key never runs twice at the same time (each `ScheduleImmediateJob<T>` call is a new key, so those can), and
  the scheduler has 5 threads per node by default, shared with every platform job and timer start: split long work.
- `Task.StartNewWithUserConnection` (`IBackgroundTask`) runs in memory and is lost on a restart; a job from
  `ScheduleImmediateJob<T>` / `TriggerJob<T>` is stored and survives one.

Evidence
- Measured 2026-11 with disposable packages, clio 8.1.0.135, on Creatio 10.2 and a trunk build (.NET Framework):
  install copies `Type`/`ProjectPath`; a changed descriptor without `Type` reset it, one with it restored it, and the
  reset install left a copy in the configuration assembly; the conversion refused a pushed package; a converted package
  kept its old types until a full compile; the template package was skipped by package and full compiles and not
  compiled by the install, and one `add-schema` schema made the install compile it; a shipped DLL was served after a
  restart and left unchanged by a skipping compile. On Creatio 10.3 (.NET Framework, database mode) `push-workspace`
  compiled a package with a marker schema and its service answered without a restart; a second package compile logged
  `compiled: 0` and clio's wait reported failure. On Creatio 10.3 (.NET 10): the same compile filter, routes at
  `/rest/` and `/0/rest/` without `.svc`, both CSRF header names, empty `Assembly.Location`.
- Read in platform source (10.2 and trunk), not measured: version boundaries, the install's compile decision,
  references, the wrong `ProjectPath`, who restarts after an install, and every statement about background jobs.

clio MCP assembly-package guide - server-side C# in a package that compiles into its own assembly

Scope
This guide owns the assembly package (`SysPackage.Type` = 1, the designer's "Compile into a separate assembly"):
when to choose one, how it is registered, how its C# gets built or shipped, what it builds against, what changes at
runtime, and C# background jobs (`IJobExecutor`). Other owners: `configuration-webservice` (writing a web service
class, routes, the CSRF header), `core-rules` (the compile confirmation and the restart that activates a DLL),
`core-rules-details` (precompiled delivery, `compile-package --wait` exit codes), `backend-deployment-troubleshooting`
(the running code differs from the source), `process-timer-start` (running a process on a schedule),
`process-script-task` (C# inside a process).

== When to use one ==
- Default: C# that runs on the server goes into SourceCode schemas of an ordinary package. A package compile builds
  them, and nothing in this guide applies.
- Choose an assembly package only when the code needs its own project: a third-party assembly reference, a csproj
  you build and test locally, or compiling the package without rebuilding the configuration assembly. Each of those
  brings the registration and compile rules below; an ordinary package has none of them.

== Registration ==
- An assembly package is `Type` 1 with `ProjectPath` `Files/<PackageName>.csproj` (relative to the package folder;
  every shipped assembly package uses that form). Only a `Type` 1 package has its csproj built: the csproj of a
  `Type` 0 package is ignored, with no warning.
- Install copies both values from the package's `descriptor.json`. Keep `"Type": 1` and `"ProjectPath"` in it:
  `add-package` writes both, and `pull-pkg` of an assembly package exports both. `create-package` creates an
  ordinary package (`Type` 0).
- After every install read both back. When the `list-packages` result carries `type` and `projectPath`, read them
  there (`type` 1 = assembly package); otherwise read `SysPackage` `Type`/`ProjectPath` with the read-only
  `execute-esq`.
- A push resets the package to `Type` 0 and empties `ProjectPath` when its descriptor does not carry `"Type": 1`
  and counts as changed (a new package, a new `ModifiedOnUtc`, version or dependencies). A push with an unchanged
  descriptor keeps what is stored.
- Repair a reset through the descriptor: put `"Type": 1` and `ProjectPath` back, set a new `ModifiedOnUtc`, push
  again and read back. Then run ONE full compile (asking first, `core-rules`): the install that reset the type
  compiled the package into the configuration assembly, and the restoring push leaves that copy there, so the same
  types would live in two assemblies.
- MUST NOT set `Type` or `ProjectPath` with SQL, OData or DataService writes: they skip the platform's type
  converter (`ProjectPath`, the type cache, compile tracking), and the next push of the descriptor decides the value
  anyway. The platform's own conversion (package properties) refuses a package installed from an archive
  (`push-pkg`: "installed from the file archive"), so it is not a repair path for pushed packages either.
- A wrong `ProjectPath` does not fail the compile: the platform creates a template project at that path and builds
  it instead of yours.

== Compile: source delivery ==
- A target-side compile builds an assembly package ONLY when it has compilable content: at least one SourceCode,
  process or user-task schema, or a reference assembly. C# files under `Files/` do not count, and neither does an
  entity schema. Without it the package compile, the full compile and the install all skip the package: the compile
  log says `skipped (no compilable schemas): ... [<Package>]`, no `Files/Bin` DLL is built, and its services do not
  exist.
- The `add-package` template is such a package: its C# is under `Files/src/cs` and it has no schema. For source
  delivery add one SourceCode schema before the push: in the workspace run
  `clio add-schema <Package>Marker -t source-code -p <path to the package>`. The install then compiles the package
  (the compile log lists it under `compiled:`).
- With a skipped package, `compile-package --wait` waits for compilation history that is never written and exits
  non-zero after its timeout ("wrote no compilation history"). Before recompiling, look for the package in the
  compile log's `skipped (no compilable schemas)` list.
- A package created as an assembly package needs a package compile only. One converted from an ordinary package,
  or installed as an ordinary package even once, needs ONE full compile (asking first, `core-rules`): a package
  compile builds its DLL and, after a restart, serves the new code, but leaves its old types in the configuration
  assembly; only a full compile removes them.
- Every compile that adds or changes a DLL or a service needs the restart in `core-rules` before the change runs.

== Compile: precompiled delivery ==
- The other path: build `Files/<PackageName>.csproj` yourself and ship the DLL in `Files/Bin` (`core-rules-details`,
  Precompiled delivery). After a restart the platform loads the shipped DLL; it compiles nothing.
- The shipped DLL is then the running code. A compile of a package without compilable content skips it and leaves
  the shipped DLL as it is, so a source change pushed without a rebuilt DLL keeps the old behavior. Rebuild the DLL
  from the current source on every change; a DLL carried over from an earlier build or `pull-pkg` is stale by
  default.
- Pick one path per package: either compilable content and target-side compile, or a shipped DLL you rebuild.

== References ==
- The project targets `net472` (.NET Framework, output `Files/Bin`) or `netstandard2.0` (.NET, output
  `Files/Bin/netstandard`) - never `net8.0` or later.
- It references the public reference assemblies and `Files/Bin` DLLs of the package's DIRECT dependencies (Creatio
  8.1.3 or later). Not referenced: non-public assemblies; on .NET, assemblies that are not netstandard; dependencies
  of dependencies before Creatio 10.1.391; on 10.0.0.171 or later, a dependency assembly package without compilable
  content. Add a package whose types you use as a direct dependency (`package-dependencies`).
- A compile regenerates the project's references. Put your own third-party references in the
  `3rd Party References` item group; references elsewhere are overwritten.
- JSON: the default reference is `Terrasoft.NewtonsoftWrapper`, a subset of Json.NET under the namespace
  `Newtonsoft.Json` (`JsonConvert`, serializer settings, contract resolvers) with no `Newtonsoft.Json.Linq`
  (`JObject`, `JToken`). When you need those, reference `Newtonsoft.Json` directly; the regenerated project then
  drops the wrapper. Use one of the two, never both.

== Runtime ==
- Package assemblies are loaded from bytes, so `Assembly.Location` is an empty string, for the package assembly and
  for the configuration assembly alike. Build file paths from `AppDomain.CurrentDomain.BaseDirectory` plus
  `Terrasoft.Configuration/Pkg/<Package>/Files/...`, never from an assembly's location.
- An authenticated `[ServiceContract]` service in an assembly package needs no `.svc` file; routes, CSRF and the
  base class are `configuration-webservice`.

== Background work in C# ==
- Work that runs on a schedule is a timer-start process: read `process-timer-start`. Write an `IJobExecutor` only
  when a timer start cannot express the work: each run needs its own parameters, the schedule is computed at
  runtime, or a request hands work off to run later. A timer-start job carries no process parameters.
- The contract is synchronous: `void Execute(UserConnection userConnection, IDictionary<string, object> parameters)`.
  MUST NOT be `async void`: it returns at its first `await`, the platform then closes the job's connection and DI
  scope, and an exception after that is unhandled on a thread-pool thread, where it can end the worker process.
- Make `Execute` total: catch everything inside it. A throw is logged (`JobFail`, "Error executing") and swallowed:
  nothing retries it, a repeating job fires again at its next time, and a one-time job is gone. Write retries
  yourself, and set your own status row to a final state in `finally`.
- Use the `UserConnection` passed to `Execute`: it is created for this run and closed when `Execute` returns.
  Resolve services inside `Execute`. The executor is created anew for every run and needs a public parameterless
  constructor. `[DefaultBinding(typeof(IJobExecutor))]` is not required.
- Pass only primitives, strings and Guids as job parameters, never a `UserConnection`, an entity or your own types:
  parameters are stored by a serializer with a type allow-list, which reports other types now and refuses them once
  the allow-list is enforced.
- Identity: with `isSystemUser` true the job runs as the `SystemUser` system setting's user with no rights checks.
  It is the default of `CreateClassJob<T>` and `CreateAndScheduleJob<T>`; `ScheduleMinutelyJob<T>`,
  `ScheduleImmediateJob<T>` and `TriggerJob<T>` default to false and run as the scheduling user.
- Scheduling: `AppScheduler` APIs that take a job name are idempotent per (job name, group), so scheduling the same
  name again replaces the job; you do not need to remove it first. `ScheduleImmediateJob<T>` names every call's job
  with a new GUID (whatever its doc comment says): each call is one more run, never a replacement. A raw Quartz
  trigger built without `WithIdentity` gets a random key, so each save adds a trigger.
- Cron (`CreateAndScheduleJob<T>` only): Quartz syntax, 6 fields with seconds first plus an optional year; a 5-field
  Unix expression is refused; `?` in day-of-month or day-of-week; UTC when no time zone is given.
- One job never runs twice at the same time, and the scheduler has 5 threads per node by default: a long `Execute`
  holds one of them, so split long work into chunks.
- `Task.StartNewWithUserConnection` (`IBackgroundTask`) runs in memory and is lost on a restart; a job from
  `ScheduleImmediateJob<T>` / `TriggerJob<T>` is stored and survives one.

Evidence
- Measured 2026-11 on Creatio 10.2 (.NET Framework, MSSQL) and on a 10.x trunk build (.NET Framework, MSSQL), clio
  8.1.0.135, with disposable assembly packages: install copies `Type`/`ProjectPath` from the descriptor; a changed
  descriptor without `Type` reset it and a changed descriptor with it restored it; the reset install left a copy in
  the configuration assembly; the platform conversion refused a pushed package; a converted package kept its old
  types until a full compile; the `add-package` template package was skipped by the package compile, the full
  compile and the install, and one `add-schema` SourceCode schema made the install compile it; a shipped `Files/Bin`
  DLL was served after a restart and left unchanged by a skipping compile; services answered at `/0/rest/...`
  without a `.svc`; `Assembly.Location` was empty.
- Read in Creatio 10.2 platform source, not measured: the same compile filter on .NET (both compile paths, Creatio
  10.0.0.171 or later), references, the wrong `ProjectPath`, and every statement about background jobs.

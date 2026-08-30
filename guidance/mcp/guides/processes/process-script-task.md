clio MCP process ScriptTask C# guide

Scope
Use this guide when writing or repairing C# inside a ScriptTask of an existing Creatio business process. It covers the generated process parameter API and the namespace/assembly differences that commonly break otherwise valid snippets. It does not design the BPMN graph; use the process tools and their currently available contract for graph changes.

Inspect before authoring
- Call `get-process-signature process-name=<code-or-caption> environment-name=<env>` first. Copy `processCode` and each parameter `name` returned by the environment; never derive a code from a display caption or copy the sample names below. If a caption is ambiguous, choose one of the returned candidate codes and call the tool again with that exact code.
- Match each C# generic type to the parameter's declared CLR type. A Lookup parameter is a `Guid`, not its display text.

Read and write process parameters
Inside a ScriptTask, the generated process class exposes parameters by code:

```csharp
Guid accountId = Get<Guid>("UsrAccountId");
string summary = Get<string>("UsrSummary");

Set<string>("UsrResult", summary);
Set<bool>("UsrSucceeded", true);
```

An unknown code is not a safe fallback: use the exact code from the process signature. Set output parameters before the ScriptTask completes so later elements can consume them.

Treat every process parameter as untrusted input even when its code and CLR type match the signature. Validate expected formats and ranges, and re-check record access or business authorization before using an identifier for reads, writes, or external calls. A process running with elevated context must not turn a caller-supplied record id into an authorization bypass.

Backend query namespaces
For `EntitySchemaQuery` recipes also read `esq-filters-backend`. `AggregationTypeStrict` and `LogicalOperationStrict` belong to `Terrasoft.Common`, while ESQ types belong to `Terrasoft.Core.Entities`. A ScriptTask's generated ambient imports vary, so fully qualify these types when the compiler cannot resolve them; do not move `AggregationTypeStrict` to `Terrasoft.Core.DB`.

Logging without the `Common` namespace collision
Generated process code commonly imports `Terrasoft.Common`. In that context an unqualified `Common.Logging` can bind through `Terrasoft.Common` and fail to compile. Anchor the logging namespace at the global root:

```csharp
global::Common.Logging.ILog log =
    global::Common.Logging.LogManager.GetLogger("UsrAccountProcess");
log.Info("Account ScriptTask started.");
```

Log operational milestones and non-sensitive correlation identifiers only. Treat user and business-record identifiers as potentially sensitive; do not log credentials, tokens, raw parameter values, or serialized payloads unless their fields are explicitly allowlisted or redacted.

Portable Newtonsoft.Json calls
Creatio installations can carry older Newtonsoft.Json assemblies. For portable ScriptTask code, call the widely available one-argument overload:

```csharp
string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
```

Do not require the `SerializeObject(object, Formatting)` overload merely for pretty printing; it may be absent on an older target assembly. Formatting is diagnostic presentation, not process behavior.

Evidence and applicability
- `AggregationTypeStrict` namespace ownership is verified in Creatio core source and in Creatio 10.0.0.858 assemblies; the same split is also documented by `esq-filters-backend` for later 10.x builds.
- The `global::Common.Logging` qualification and one-argument Newtonsoft.Json call are compatibility remedies observed while compiling a ScriptTask on the issue's .NET Framework target. They are deliberately conservative for mixed-version installations, not claims that every newer runtime lacks the shorter names or overloads.
Verification
Compile the process on the target environment, run it with known parameter values, and verify the expected output parameter or persisted side effect. Treat a compile on a different Creatio version as supporting evidence only: the target environment's generated process and referenced assemblies are authoritative.

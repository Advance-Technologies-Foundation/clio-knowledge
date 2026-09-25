clio MCP process ScriptTask C# guide

Scope
Use this guide before adding a ScriptTask to a process, and when writing or repairing the C# inside one. It covers when a ScriptTask is the right element at all, how clio builds one, the generated process parameter API, the namespaces the generated code imports and how to add more, and the namespace/assembly differences that commonly break otherwise valid snippets. The graph itself belongs to `process-modeling`.

Decide first: a ScriptTask costs a compile
A ScriptTask is the ONE element of a clio-built process that makes it need `compile-creatio` before it runs: the platform turns every ScriptTask body into a method of a generated class, and the process refuses to start until that class is compiled. Every other element runs without one. So choose it last, in this order:
1. A no-code element does the job: Read data (including count and aggregation), Add / Modify / Delete data, Send email, a gateway with a condition, a Sub-process. Prefer these.
2. One computed value: an `expression` mapping or a Formula task (`process-element-catalog`) - no compile.
3. Logic that more than one process needs, or that deserves its own tests: put it in a C# source-code schema (`create-schema`, compiled once), and call it from a custom user task (`create-user-task`, then `userTaskName` in the descriptor) - a process that USES an already-compiled user task needs no compile of its own. clio creates the user task's schema and parameters but not its C# body, which is written by hand.
4. A ScriptTask: a few lines of glue that only this process needs and that no element expresses - calling an existing configuration class or service, a loop or string/JSON shaping over values the process already holds, a system setting read. Shipped processes use it mostly this way: most bodies are under ten lines and call one configuration class.
Tell the user that the process will need a compile, and why, before building it.

Build one through clio
- Element: `{ "name": "CalcDiscount", "type": "scriptTask", "caption": "...", "scriptTask": { "body": "..." } }`. The name becomes a C# method name (`<name>Execute`), so it must be a letter followed by letters, digits or `_`.
- The body is the method's STATEMENTS, not a method: end it with `return true;` (`return false;` stops the flow at the element). It is built as the designer's "For interpreted process" variant, which is what makes `Get` / `Set` available.
- `setElement` with `elementUpdate.scriptTask.body` replaces the body of an existing one. `describe-business-process` reads it back as `scriptTask: { body, forInterpretedProcess }`; `forInterpretedProcess: false` is an older compiled-variant task whose parameters are plain properties - Get/Set do not compile there, so keep its style when editing it.
- A successful save that added a ScriptTask, replaced a body, changed a using or changed the process methods returns a warning that the process cannot run "until the configuration is compiled" instead of the usual compile-not-required note. Then, with the user's confirmation that tool requires, run `compile-creatio` with `process-name` set to the process (CrtProcessBuilder 1.6.6.33+): it compiles the package the process is in and returns the compiler errors, the process's own first - C# that does not compile is reported by the compile, never by the save. Do not use a `package-name` compile-creatio for this: on Creatio 10.x it does not pick such a save up, and a full one does but takes about 20 minutes.
- The compile-required warning and the compile-not-required note both speak for the call that returned them: a later edit that changes no C# gets the note back, while a compile an earlier save made owed is still owed.
- Until that compile, a process that was never compiled refuses to start ("Publish the ... process before starting it"), and one compiled before keeps running its PREVIOUS body with nothing saying so. So after the compile, verify the new behaviour on a run rather than trusting the save.
- The save also warns about a `Get`/`Set` name that will not resolve at run time - a parameter that does not exist, or one spelled in a different case (the lookup is case-sensitive).

Inspect before authoring
- On a NEW process the parameter names are the ones your own descriptor declares; for an existing one, read them from the environment:
- Call `get-process-signature process-name=<code-or-caption> environment-name=<env>` first. Copy `processCode` and each parameter `name` returned by the environment; never derive a code from a display caption or copy the sample names below. If a caption is ambiguous, choose one of the returned candidate codes and call the tool again with that exact code.
- That is a rule about not INVENTING a code, not a reason to avoid captions: a caption resolves to the ACTIVE version of a family while a code names one member, so on a versioned process the caption is the better thing to hand the tool. `process-versions` owns which identity means what.
- Match each C# generic type to the parameter's declared CLR type. A Lookup parameter is a `Guid`, not its display text.

Read and write process parameters
Inside a ScriptTask, the generated process class exposes parameters by code:

```csharp
Guid accountId = Get<Guid>("UsrAccountId");
string summary = Get<string>("UsrSummary");

Set<string>("UsrResult", summary);
Set<bool>("UsrSucceeded", true);
```

An unknown code is not a safe fallback: use the exact code from the process signature, in its exact CASE - the run-time lookup is case-sensitive, so `Get<int>("amount")` does not find `Amount`. `"ElementName.ParameterName"` reads another element's parameter, for example `Get<ICompositeObjectList<ICompositeObject>>("ReadOrders.ResultCompositeObjectList")`. `UserConnection` is a property of the generated class and needs no `Get`. Never write a parameter as a bare field: that is the compiled-variant style and does not compile in a ScriptTask clio builds. Set output parameters before the ScriptTask completes so later elements can consume them.

Treat every process parameter as untrusted input even when its code and CLR type match the signature. Validate expected formats and ranges, and re-check record access or business authorization before using an identifier for reads, writes, or external calls. A process running with elevated context must not turn a caller-supplied record id into an authorization bypass.

Namespaces: what is imported, and how to add more
The generated code always imports `System`, `System.Collections.Generic`, `System.Collections.ObjectModel`, `System.Drawing`, `System.Globalization`, `System.Text`, `Terrasoft.Common`, `Terrasoft.Core`, `Terrasoft.Core.Configuration`, `Terrasoft.Core.DB`, `Terrasoft.Core.Entities`, `Terrasoft.Core.Process` and `Terrasoft.Core.Process.Configuration`, plus `Newtonsoft.Json` and `Newtonsoft.Json.Linq` unless the package compiles into its own assembly. There is NO `System.Linq`, and no `Terrasoft.Configuration` - the namespace of every configuration class, entity class and source-code schema.
- Anything else goes into the PROCESS-level usings - the designer's Process properties -> Methods -> Usings - never into the body: `usings: [{ "namespace": "System.Linq" }]` on `create-business-process`, `addUsing` / `removeUsing` with `using: { namespace, alias? }` on `modify-business-process`. They are shared by every ScriptTask of the process and read back by describe as `usings[]`.
- Listing a default namespace is harmless and does nothing. An ALIAS on a default namespace is refused: the platform's generator drops such an entry together with its alias, so the alias would not exist at compile time.
- An alias exists to break a NAME COLLISION between two imported namespaces. The common one: importing `Terrasoft.Configuration` makes `SysSettings` ambiguous (CS0104) between the entity class `Terrasoft.Configuration.SysSettings` and `Terrasoft.Core.Configuration.SysSettings`. Alias the TYPE you mean: `{ "namespace": "Terrasoft.Core.Configuration.SysSettings", "alias": "SysSettings" }`. An alias can also shorten a long prefix (`TSConfiguration` = `Terrasoft.Configuration`); a fully qualified name needs no using at all and is always an alternative.
- Inside the generated `namespace Terrasoft.Core.Process`, a bare `Configuration.X` means `Terrasoft.Core.Process.Configuration.X`. Write `Terrasoft.Configuration.X`, or alias it.

Process methods
Helpers that several ScriptTasks of ONE process share go into the process methods - the designer's Process properties -> Methods text: `methods` on `create-business-process`, `setMethods` (a whole-text replace; an empty string clears) on `modify-business-process`, read back by describe as `methods`.
- The text is C# CLASS MEMBERS, not statements: `private decimal Discount(decimal amount) => amount * Get<decimal>("Rate");`. It compiles into the same generated class as the ScriptTasks, so they call it by name; it can use `Get`/`Set` and `UserConnection`, and it compiles under the same usings.
- Only interpreted ScriptTasks (and a user task's after-save script) can call it; formulas cannot. Logic that more than one PROCESS needs belongs in a source-code schema instead.
- Changing the methods owes the same compile as a changed body. Describe's `compiledMethods` is the older compiled variant's text, read-only; a `legacyMethodCount` means the process still keeps methods in the older per-method format, and `setMethods` is refused there while one of them is interpreted - edit those in the designer.

Backend query namespaces
For `EntitySchemaQuery` recipes also read `esq-filters-backend`. `AggregationTypeStrict` and `LogicalOperationStrict` belong to `Terrasoft.Common`, while ESQ types belong to `Terrasoft.Core.Entities`. Both namespaces are default imports; if the compiler still cannot resolve a type, fully qualify it; do not move `AggregationTypeStrict` to `Terrasoft.Core.DB`.

Logging without the `Common` namespace collision
Generated process code always imports `Terrasoft.Common`. In that context an unqualified `Common.Logging` can bind through `Terrasoft.Common` and fail to compile. Anchor the logging namespace at the global root:

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
Compile the process on the target environment, run it with known parameter values, and verify the expected output parameter or persisted side effect. A value returned by a run is not proof on its own: a ScriptTask that throws after its `Set` still leaves that value behind, so also check that the process instance completed rather than ended in error. Treat a compile on a different Creatio version as supporting evidence only: the target environment's generated process and referenced assemblies are authoritative.

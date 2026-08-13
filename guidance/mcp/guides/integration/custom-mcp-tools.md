# Creatio custom C# MCP tools

Use this guide when implementing, publishing, reviewing, or troubleshooting a custom C# MCP tool
hosted directly by Creatio through `CrtMCPPublishingApp`. It covers source-code actions, `McpServer`
and `McpTool` package data, direct Streamable HTTP MCP consumption, catalogue refresh behavior, and
tests.

## Applicability and boundaries

- Verified with a source-built Creatio 10.1.0 environment and a `CrtMCPPublishingApp` version that
  supports the `Source code action` tool source type.
- This guide does not cover business-process-backed MCP tools.
- A separate TypeScript MCP server is not required to consume a Creatio-published server directly.
  A gateway can still add multi-tenant credential brokering, policy, or a stable cross-instance tool
  surface, but it is a separate deployment decision.
- If the same application behavior is also exposed through a configuration web service, follow
  `configuration-webservice` for that adapter. Keep both entry points over one application handler.

## Platform requirements

- Derive the entry point from `Creatio.Copilot.Actions.BaseExecutableCodeAction`.
- Make it a public, concrete class with a usable parameterless constructor; the publisher resolves the
  stored type and instantiates it at runtime.
- Implement its three abstract members: `Execute`, `GetCaption`, and `GetDescription`.
- Declare only source-code action parameters whose data types the publishing schema builder supports.
- Keep the stored `SourceCodeAction` assembly-qualified type synchronized with namespace, class, and
  assembly renames.
- Give callers the required Creatio operation permission; source-code actions are gated by
  `CanRunBusinessProcesses` in the verified publishing app.
- Never put credentials, tokens, cookies, or tenant-specific values in package data, tool results,
  logs, tests, or Allure attachments.

## Reference conventions

The executable reference uses these reusable application conventions. They are not Creatio platform
requirements:

- Put the MCP entry point under
  `packages/<PACKAGE_NAME>/Files/src/cs/EntryPoints/McpTool/<ToolName>.cs`.
- Keep the entry point thin: parse transport values, resolve an application handler from package DI,
  and map its result to `CopilotActionExecutionResult`.
- Use a shared enum or another closed application contract for finite choices. Do not dispatch on
  open-ended strings such as `"add"`, `"subtract"`, or `"multiply"`.
- Validate every enum integer at the MCP boundary with `Enum.IsDefined`; casting an undefined integer
  is not validation, and the platform does not enforce enum membership (see the current verified
  limitation below).
- Represent expected validation and business failures as values, for example with `ErrorOr<T>`, rather
  than throwing exceptions.
- Test the handler and entry point with NUnit and FluentAssertions, and use direct E2E tests for
  catalogue discovery and execution.

## Required implementation shape

### 1. Define the application contract first

Put the enum, request/result types, and handler outside the transport entry point. The MCP action and
an optional web service should resolve the same handler.

```csharp
public enum ArithmeticOperation {
	Add = 1,
	Subtract = 2,
	Multiply = 3,
	Divide = 4
}

internal interface IArithmeticHandler {
	ErrorOr<ArithmeticResult> Calculate(
		ArithmeticOperation operation,
		double leftOperand,
		double rightOperand);
}
```

Use explicit nonzero enum values when a missing or default zero must not silently select a valid
operation.

### 2. Implement a thin source-code action

Declare MCP inputs through `SourceCodeActionParameter`. `Input` and `Var` parameters become inputs;
`Output` and `Var` parameters can describe structured output.

```csharp
public sealed class ArithmeticMcpTool : BaseExecutableCodeAction {
	public ArithmeticMcpTool() {
		Parameters = new List<SourceCodeActionParameter> {
			new SourceCodeActionParameter {
				Name = "operation",
				Caption = new LocalizableString("Operation"),
				Description = new LocalizableString(
					"ArithmeticOperation value: Add = 1, Subtract = 2, Multiply = 3, Divide = 4."),
				DataValueTypeUId = DataValueType.IntegerDataValueTypeUId,
				Direction = ParameterDirection.Input,
				IsRequired = true
			}
		};
	}

	public override LocalizableString GetCaption() {
		return new LocalizableString("Arithmetic calculator");
	}

	public override LocalizableString GetDescription() {
		return new LocalizableString(
			"Performs Add, Subtract, Multiply, and Divide operations on two numbers.");
	}

	public override CopilotActionExecutionResult Execute(ActionExecutionOptions options) {
		Dictionary<string, string> values = options?.ParameterValues;
		if (!TryGetOperation(values, out ArithmeticOperation operation)) {
			return Failed("The operation parameter must be a defined ArithmeticOperation value.");
		}
		if (!TryGetNumber(values, "leftOperand", out double left)
				|| !TryGetNumber(values, "rightOperand", out double right)) {
			return Failed("Both operands must be numbers.");
		}

		using (IServiceScope scope = CustomMcpToolApp.Instance.CreateScope()) {
			IArithmeticHandler handler = scope.ServiceProvider
				.GetRequiredService<IArithmeticHandler>();
			ErrorOr<ArithmeticResult> result = handler.Calculate(operation, left, right);
			return result.IsError
				? Failed(result.FirstError.Description)
				: Completed(result.Value.Value);
		}
	}
}
```

`BaseExecutableCodeAction` supplies values as strings. Parse numbers with invariant culture. For
finite integer choices, parse the integer, cast it to the shared enum, and then call `Enum.IsDefined`.
Keep helpers such as `TryGetOperation`, `TryGetNumber`, `Failed`, and `Completed` private to the
transport adapter; the pinned reference shows their complete implementations.

The action caption and description come from `GetCaption()` and `GetDescription()`. Set
`CopilotActionExecutionStatus.Failed` for expected failures and `Completed` for success. The publishing
layer may mask detailed action failures from the MCP client as a generic safe message; keep useful
details in server diagnostics without leaking secrets.

### 3. Register application services in package DI

Register handlers in the package composition root and resolve them from a scope inside the action.
Do not duplicate business calculations or validation in the MCP entry point. The reference registers
its stateless handler with `AddScoped<IArithmeticHandler, ArithmeticHandler>()`; both the MCP action
and web-service adapter create and dispose one scope per execution. Use another lifetime only when the
application behavior requires it.

## Publish the server and tool through package data

Create two package data bindings:

1. An `McpServer` row defines the published server.
2. An `McpTool` row defines one tool belonging to that server.

The important fields are:

| Record | Field | Contract |
|---|---|---|
| `McpServer` | `Code` | Stable URL segment for the MCP server. |
| `McpServer` | `IsOnline` | Must be enabled for external use. |
| `McpTool` | `ExternalName` | Stable MCP tool name used by `tools/call`. |
| `McpTool` | `McpServer` | Lookup to the owning server row. |
| `McpTool` | `ToolSourceType` | Select `Source code action`. |
| `McpTool` | `SourceCodeAction` | Exact `<Namespace>.<Class>, <Assembly>` value. |
| `McpTool` | `IsEnabled` | Must be true to participate in `tools/list`. |
| `McpTool` | `InputSchema` | Optional advertised-schema override. |
| `McpTool` | `OutputSchema` | Optional advertised output contract. |
| `McpTool` | `Annotations` | MCP behavior hints; they must match actual behavior. |

Example action binding:

```text
CustomMcpToolApp.EntryPoints.McpTool.ArithmeticMcpTool, CustomMcpTool
```

The stored `InputSchema` override is advertisement-only: it controls the schema returned by
`tools/list`. During `tools/call`, the publisher resolves the action and validates arguments against a
separate runtime schema derived from its `SourceCodeActionParameter` declarations. Therefore:

- an override cannot make an unsupported or unresolvable action runnable;
- a constraint present only in the override, such as `enum`, is not enforced by the call validator;
- a property advertised only by the override is rejected because the runtime action did not declare
  it.

Keep the override aligned with the runtime parameters. Prefer a closed advertised object schema with
required parameters and `additionalProperties: false` so the catalogue clearly communicates the same
contract the C# action enforces.

Current verified limitation: the publishing-app input-schema wire DTO does not emit a stored JSON
Schema `enum` keyword. Preserve the integer type and value mapping in the parameter description, but
enforce the actual enum membership in C#. Do not rely on the catalogue schema as the only guard.

For an action with no declared output parameters, the runtime can expose a conventional string
`result`. When declaring output parameters, return a JSON object that conforms to them. A stored
`OutputSchema` override wins over runtime derivation, so keep the action response and override aligned.

## Package-data upgrade policy

The package data row is executable configuration. Decide ownership explicitly:

- If the package owns the complete `McpTool` definition, set `IsForceUpdate: true` on its mutable
  non-key columns so upgrades converge across environments.
- For the reference `McpServer` row, `Code`, `Name`, and `Description` are package-owned and use force
  update. `IsOnline` is administrator-owned and does not, so reinstalling the package does not
  silently reverse an operational decision to take the server offline.
- Advance the binding descriptor's `ModifiedOnUtc` whenever its row or column policy changes. Changing
  only `data.json` or only `IsForceUpdate` under an unchanged descriptor timestamp may not reinstall
  the binding.
- Keep the primary-key `Id` column as the key; it does not need force update.
- Do not use force update when administrators are expected to customize and retain those same fields.
  Force update deliberately replaces the target value during package installation.

This policy is especially important after renaming the package assembly or namespace. If the live row
still contains the old `SourceCodeAction`, the publisher excludes the tool because `Type.GetType`
cannot resolve it. Restart, Redis invalidation, and compilation cannot repair a stale database value.

## Build and deploy

Build the standalone package assembly before installing the workspace:

```powershell
dotnet build MainSolution.slnx -c dev-n8
clio pushw -e <environment-name>
```

Follow the target environment's current clio guidance for compilation and application restart.
Compilation produces the assembly; restart reloads deployed runtime binaries when required by that
deployment mode. Neither operation substitutes for installing corrected package data.

## Direct MCP endpoint and authentication

Use the server code from `McpServer.Code`:

```text
https://<creatio-host>/rest/ToolServiceMcp/<server-code>/v1/mcp
```

Some classic deployments use the `/0/rest/...` prefix. Use the route exposed by the target
environment or publishing UI instead of assuming one prefix.

### OAuth client credentials

Read `server-to-server-oauth` for OAuth app creation, token minting, rotation, and secret handling.
Send the resulting token on every MCP request:

```http
Authorization: Bearer <access-token>
Accept: application/json, text/event-stream
Content-Type: application/json
```

### Username and password

For a controlled test client, POST credentials to:

```text
/ServiceModel/AuthService.svc/Login
```

Classic deployments can require the `/0/ServiceModel/...` prefix; use the target environment's route.
Reuse the returned authentication cookies and copy the `BPMCSRF` cookie value into a `BPMCSRF`
request header. Prefer OAuth for service-to-service deployments. Never expose Forms credentials to
an agent model as tool arguments.

The authenticated Creatio user must have the `CanRunBusinessProcesses` operation permission. The
publisher uses that operation as the execution gate for source-code actions.

## MCP request sequence

Use JSON-RPC 2.0 over HTTP POST:

1. Send `initialize` with the desired MCP protocol version.
2. Preserve `Mcp-Session-Id` when the response supplies it.
3. Send the negotiated version as `MCP-Protocol-Version` on following requests.
4. Send the `notifications/initialized` notification.
5. Send `tools/list` or `tools/call`.

Initialize:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-03-26",
    "capabilities": {},
    "clientInfo": { "name": "my-client", "version": "1.0" }
  }
}
```

List tools:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list",
  "params": {}
}
```

Call a tool:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "calculate_arithmetic",
    "arguments": {
      "operation": 1,
      "leftOperand": 20,
      "rightOperand": 22
    }
  }
}
```

Accept both ordinary JSON and `text/event-stream`; a Streamable HTTP response may carry JSON-RPC
messages in SSE `data:` records.

## Catalogue refresh behavior

`CrtMCPPublishingApp` does not keep a global tool catalogue cache in the verified implementation:

- each `tools/list` request constructs a new list service;
- the repository reads enabled `McpTool` rows for the requested server from the database;
- source-code action resolution is cached only inside that request;
- a direct client can send `tools/list` again in the same MCP session and receive current persisted
  metadata.

This does not mean every publisher discovery surface is uncached. The administration action picker
uses `McpSourceCodeActionQueryExecutor`, whose reflected source-code action inventory is cached for the
application process lifetime. Adding a brand-new action class can therefore require an application
restart before that class appears in the administration picker. This is separate from refreshing
persisted `McpTool` metadata through `tools/list`.

Therefore:

- application restart is not a metadata-catalogue refresh control;
- Redis flush is not a metadata-catalogue refresh control;
- package compilation is not a metadata-catalogue refresh control;
- install corrected package data, then request `tools/list` again.

An agent host can maintain its own model-facing tool registry. If it does not expose an in-session
refresh, reconnect that MCP server or start a new agent session. That is a client limitation, not a
Creatio catalogue cache.

## Troubleshoot a missing tool

Check in this order:

1. The endpoint URL uses the intended `McpServer.Code`, and the server is online.
2. The `McpTool` row belongs to that server and `IsEnabled` is true.
3. `ToolSourceType` is `Source code action`.
4. `SourceCodeAction` contains the current namespace, concrete class, and assembly name.
5. The class derives from `BaseExecutableCodeAction`, is non-abstract, can be instantiated, and
   reports `IsEnabled`.
6. The authenticated user has `CanRunBusinessProcesses`.
7. Every declared parameter uses a data type the schema builder supports.
8. The package data binding actually installed the current row; inspect `IsForceUpdate` and
   `ModifiedOnUtc` before touching caches.
9. Inspect publishing-app warning logs. A configured tool that cannot resolve or authorize its action
   is intentionally omitted from `tools/list`.

## Test and evidence conventions

The reference uses NUnit and FluentAssertions. Follow the target repository's established test
framework and assertion conventions when they differ.

Unit coverage should include:

- every defined enum operation;
- missing, malformed, and undefined enum values;
- numeric parsing with invariant culture;
- handler delegation through DI;
- success and `ErrorOr` failure mapping;
- action parameter metadata.

Keep direct E2E scenarios separate so reports show each contract independently:

1. OAuth authentication plus `tools/list` contract assertion.
2. OAuth authentication plus successful `tools/call`.
3. OAuth authentication plus an expected tool failure.
4. Forms authentication plus `tools/list` contract assertion.
5. Forms authentication plus successful `tools/call`.

Use explicit Arrange, Act, and Assert steps in Allure. Attach complete non-secret initialize,
catalogue, and call responses to their protocol steps. Never attach authorization headers, tokens,
client secrets, passwords, cookies, or BPMCSRF values.

## Executable reference

Use `list-knowledge-examples` or read:

```text
docs://knowledge/com.creatio.clio/atf.creatio.custom-mcp-tool-reference
```

The catalog entry pins the complete `CustomMcpTool` learning lab at an immutable commit. It contains
the C# action, shared enum and handler, optional configuration web service, package data, 21 unit
tests, and 5 direct OAuth/Forms MCP E2E scenarios. The reference is tested educational evidence; its
incidental implementation choices are not universal platform policy.

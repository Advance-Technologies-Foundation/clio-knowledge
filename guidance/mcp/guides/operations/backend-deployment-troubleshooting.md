clio MCP backend deployment troubleshooting guide

Scope
- Owns diagnosis when saved C# source, package compilation, installed binaries and running behavior disagree. `core-rules` owns the canonical DLL activation cycle; `fsm-mode` owns filesystem synchronization. Read those articles for the normal path.
- Applies to source-backed package development and precompiled package delivery. Paths and assembly layouts vary by Creatio/runtime; inspect the actual deployment instead of assuming one directory.

Establish the boundary before changing anything
1. Record the environment, Creatio/runtime version, package, schema, current FSM state, intended source revision and one deterministic behavioral expectation.
2. Read the live tool contract. `get-schema` reads C# source; `update-schema` saves its body. Saving is not compilation or activation. For package compilation, `compile-creatio` takes `package-name`; omitting it broadens to the configuration. Do not broaden scope merely because the old behavior remains.
3. Serialize source synchronization, compilation, installation and restart on this environment. If a tool reports an accepted/in-progress operation, follow `core-rules` and poll the corresponding status tool; do not submit a duplicate or restart during compilation.

Diagnose the first boundary that fails
| Observation | Evidence to collect | Next action |
| --- | --- | --- |
| Server source differs from the intended edit | `get-schema`, schema/package identity; in FSM also the linked source path and import result | Correct the source/synchronization path before building. |
| Compile reports an error | Complete result, compiler diagnostics, source filename/line and fresh build-log timestamps | Fix that diagnostic, then verify the saved/generated source before a new compile. |
| Compile says Done but behavior remains old | Current operation status, compiler log entries from that invocation, source and artifact identity | Treat success as unproven. A CLI exit code alone is insufficient on affected versions. |
| Assembly exists or its timestamp changed | Expected output path, package/runtime, hash and build diagnostics | Existence/timestamp does not establish which source compiled or which assembly the process loaded. |
| Build/install completed but process serves the old result | Restart completion/readiness and an authenticated call of the changed behavior | Follow the activation cycle in `core-rules`, then assert the expected result. |
| Restart completed but endpoint fails | Authentication response, actual route, application logs and runtime compatibility | Separate authentication/routing/startup failure from a source compilation failure. Do not restart repeatedly without new evidence. |

Compiler diagnostics
- Preserve error text, schema/file, line, column, invocation time and the relevant build-log excerpt. Redact credentials and customer data before posting evidence.
- Match logs to the current invocation. A previous compilation's diagnostics may still be visible; do not infer that a corrected source still contains the old error without reading the source and current status.
- Where accessible, inspect the deployment's compiler logs (for example a `Build.log`) and generated package source. Discover their real locations; do not invent a universal log path or delete logs to make a run look clean.
- A changed DLL hash is useful artifact evidence, but the final gate is the changed behavior in the restarted process. Do not mark deployment successful from a healthy login, file timestamp, or an unrelated endpoint.
- Do not use a full compile, Redis flush, reinstall, package deletion or manual DLL copying as a generic recovery sequence. Each needs evidence that it addresses the failing boundary and authorization for its actual scope.

Acceptance and applicability
- Disposable Creatio 10.1.585, .NET 8, PostgreSQL, database mode; clio source revision `e53009498`: created a package service returning `guidance-v1`, compiled/restarted and verified the authenticated response.
- Deliberately introduced an undefined symbol. One package compile returned exit code 0 / Done while the compiler log recorded CS0103 and the endpoint still returned v1. This demonstrates a reporting hazard, not a claim that all versions or all compile calls fail silently. Executable repair is tracked separately in [clio #1633](https://github.com/Advance-Technologies-Foundation/clio/issues/1633).
- Corrected source to v2. The first subsequent response surfaced the prior CS0103; inspection showed generated source already contained v2. A later completed package compile followed by restart/readiness and an authenticated service call returned `guidance-v2`.
- This lab validates the diagnostic boundaries and source-package activation. It does not prove arbitrary precompiled package compatibility or every FSM build configuration.

Evidence: [disposable validation and issue](https://github.com/Advance-Technologies-Foundation/clio-knowledge/issues/211), [parent report](https://github.com/Advance-Technologies-Foundation/clio/issues/1638).

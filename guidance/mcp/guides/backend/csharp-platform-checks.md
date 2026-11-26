# clio MCP Creatio C# platform checks

## Scope
Read this when you are about to write, change or review server-side C# for Creatio: a script task, a configuration web service, an entity or application listener, a query executor, a background job, a custom MCP action or any other source-code schema. Work that writes no C# (pages, the process designer, data, settings) does not need it.

This guide owns the eight checks below. The patterns around them stay with their owners: `websocket-messaging` (a message to one user), `configuration-webservice` (the service class), `sys-settings` and `sys-setting` (system settings and `SecureText`), `record-permission-extensions` (record rights), `assembly-package` (background jobs), `server-to-server-oauth` (OAuth clients), `esq-filters-backend` (query filters).

## How to apply
- Writing: produce the correct form from the start. Do not leave a check for a later review.
- Reviewing: search the code for each signature. Report every hit with its file and line, the consequence and the correct form.
- Check 2 is advice, not a defect: report it as advice, and only for code that runs on MS SQL.
- Code that already has the correct form gets no finding.

## Checks
| # | Signature | Why it is a defect on Creatio | Correct form | Applies to |
|---|---|---|---|---|
| 1 | `AddAllSchemaColumns()` on an `EntitySchemaQuery` | the query reads every column of the schema for every row | `AddColumn("<column path>")` for each column the code reads | all databases |
| 2 | a long read of a busy table through `new Select(UserConnection)` with no hint | without read-committed snapshot, MS SQL takes shared locks for the read and makes writers wait | advice: `.WithHints(Hints.NoLock)` on that `Select` when a dirty read is acceptable. A `CustomQuery` cannot take a hint | MS SQL only: PostgreSQL and Oracle render no hint |
| 3 | `StartTransaction()` on a `DBExecutor` that is never disposed | the open transaction keeps its locks and blocks other sessions | `using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())`, then `StartTransaction()`, `CommitTransaction()` on success and `RollbackTransaction()` in the catch. Disposing the executor rolls back what is still open | all databases |
| 4 | `MsgChannelUtilities.PostMessageToAll(` | the message goes to every user connected to the environment | `MsgChannelUtilities.PostMessage(UserConnection, sender, text)` for the current user, or the per-user channel in `websocket-messaging` | all |
| 5 | `HttpContext` read in background code, or a `static` field that holds a `UserConnection` | a background thread has no request; a static connection serves one user's identity and rights to every later caller | a web service inherits `BaseService` and uses its `UserConnection` (`configuration-webservice`). A background job uses the `UserConnection` passed to `Execute` (`assembly-package`). There is no DI registration for `UserConnection` | all |
| 6 | a credential or key in a literal, a config entry or a plain-text setting | anyone who reads the code, the package or the configuration gets it | a `SecureText` system setting read through `SysSettings` (`sys-setting`) | all |
| 7 | `Supervisor` / `Supervisor` or another default credential | full administrator access for anyone who reaches the host | a dedicated user or an OAuth client with only the rights the integration needs; credentials as in check 6 | every host beyond `localhost` |
| 8 | `UseAdminRights = false` on a query or entity, or a raw `Select` or `Update`, in an operation done for a user | record rights are not checked, so the user reads or changes records hidden from them | keep the default `UseAdminRights = true`. Set `false` only for system work that never returns or changes records on a user's behalf | all |

## Verification boundary
Read in the Creatio platform source in November 2026: the `UseAdminRights` default, `WithHints` and `Hints.NoLock` with the hint rendered only by the MS SQL engine, `DBExecutor.Dispose` rolling back an open transaction, `BaseService.UserConnection` read from the session, and the `SysSettings` read methods. `MsgChannelUtilities` was read in the `CrtCoreBase` package. None of the eight was measured on a stand.

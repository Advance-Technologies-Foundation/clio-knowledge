clio MCP deploy lifecycle guide

Scope
- This guide owns the executable mechanics for provisioning a Creatio instance through clio MCP,
  adding IdentityService, and getting it ready for design-time and workspace tooling.
- For schema/page modeling once an instance is registered, follow `docs://mcp/guides/app-modeling`.
- Always read each tool's executable contract through `get-tool-contract` before its first call.

Deploy preflight (run in this order)
1. `assert-infrastructure` - full sweep across Kubernetes, local infrastructure, and filesystem.
   Read `status` (pass/partial/fail) and `database-candidates`.
2. `show-passing-infrastructure` - narrow to only the choices that are safe to deploy against and read
   `recommendedDeployment` (and `recommendedByEngine`) for the deploy-creatio argument bundle.
3. For local IIS, omit `sitePort` to let `deploy-creatio` reserve the first available port from
   `deploy-creatio-defaults.site-port-range`. Fresh and upgraded settings default to the inclusive range
   `[40100, 40199]`. Use `find-empty-iis-port` only when you need to inspect or explicitly choose a port;
   its result is a point-in-time recommendation and the deploy command still reserves and revalidates it
   across concurrent clio processes before it changes files, databases, or IIS. Do not treat
   `find-empty-iis-port` as a required step for a dotnet deployment.
   `deploy-identity` can call the same IIS port scanner internally when `identitySitePort` is omitted.
4. Resolve the build archive - `deploy-creatio` needs an absolute `zipFile` path. `deploy-identity`
   accepts either a standalone `IdentityService.zip` or the same Creatio distribution bundle when it
   contains `IdentityService.zip` at the configured `identityArchivePathInBundle` path. When `zipFile`
   is omitted, `deploy-identity` finds `IdentityService.zip` under the registered environment's
   `EnvironmentPath`.

Deploy
- `deploy-creatio` is the most consequential, hardest-to-reverse tool: it drops and recreates the target
  site. Required args: `siteName`, `zipFile` (absolute build archive path). Optional: `sitePort`,
  `dbServerName`, `redisServerName` (omit to keep the default Kubernetes deployment path), and
  `useHttps` (local IIS only). HTTPS is opportunistic: clio uses a pinned or deterministically
  selected usable LocalMachine/My certificate matching the host, and warns then continues with
  HTTP when no usable certificate is installed.
- `deploy-creatio` has no argument that selects the deployment method. Choosing IIS or dotnet
  explicitly is a CLI-only option of `install-creatio` (`--deployment auto|iis|dotnet`); it is not
  reachable over MCP. `find-empty-iis-port` and the configured site-port range are relevant to the
  IIS path only.
- Prefer the recommended bundle from `show-passing-infrastructure`. For local IIS, omit `sitePort` to use
  the configured range. An explicit `sitePort` overrides both the configured fixed port and range.
- An explicit port collision is a failed deployment, not permission to continue on the same port. Automatic
  selection atomically skips a candidate claimed by another deployment and continues through the configured
  range. Deployments using different ports can run in parallel.
- Do not proceed if assert-infrastructure left the targeted database/Redis sections failing.
- Deployments preserve the build database's existing forced-password-change state by default and do
  not clear it automatically. deploy-creatio does not assign a new Supervisor password.

IdentityService
- `deploy-identity` deploys IdentityService to IIS for an already registered local Creatio environment,
  updates `OAuth20IdentityServerUrl`, `OAuth20IdentityServerClientId`, and
  `OAuth20IdentityServerClientSecret` through the platform sys-settings endpoint, creates a fresh clio
  OAuth client bound to the existing `Supervisor` user by default, and stores only the returned clio
  OAuth credentials in local clio appsettings.
- `noApp` deploys IdentityService and connects Creatio without creating any OAuth app. In that mode,
  the command skips local clio credential persistence and `connect/token` verification because no
  client exists. Do not combine `noApp` with `createTechUser` or `user`.
- `createTechUser` is opt-in. Use it only when a new technical user should be created for the fresh
  OAuth app instead of binding the app to an existing user.
- `zipFile` is optional for `deploy-identity`: when omitted, the command finds `IdentityService.zip`
  under the registered environment's `EnvironmentPath`.
- `identitySitePort` is optional for `deploy-identity`: when omitted, the command uses the first free
  IIS port in range 40001-40100. Use `find-empty-iis-port` only when you need to inspect or override
  the chosen port before deployment.
- Never echo the generated client secret in an MCP response, public room message, or log summary.
- The default `configurationMode` is `db-first`, but until direct DB seeding is fully proven the command
  falls back to the supported REST/sys-settings configuration path.

Identity attachment and removal (experimental)
- Evidence: [Clio lifecycle implementation and validation](https://github.com/Advance-Technologies-Foundation/clio/blob/fdb88217c65b1d31c1465f1a4d72cf8ebf11fd57/spec/identity-uninstall/identity-uninstall-validation.md).
- Applicability: require the installed `uninstall-identity` contract from `get-tool-contract` and the
  `deploy-identity` feature. Older clio versions without that contract do not implement this lifecycle.
- Deployment records the resolved absolute folder, exact IIS target, application pool and base URL in
  the environment's optional `IdentityService` component before creating artifacts. This includes
  `noApp` and partial deployments. Empty fields mean no recorded identity; credentials or similar names
  never establish local deletion authority. Overwrite reuses the recorded port unless explicitly overridden.
- Invoke `uninstall-identity` with `environment-name` to retain CRM and its database. It clears only
  matching CRM settings before stopping identity, removes the verified IIS target, unused pool and
  folder, then empties the attachment and matching clio credentials. Other integrations remain.
- Named `uninstall-creatio` prevalidates the attachment before destructive work, removes identity after
  reading CRM connection configuration and before CRM IIS/database/files, and drops the CRM database
  only in its normal stage. An identity failure stops CRM cleanup. Show both resolved targets within
  the existing removal confirmation/reporting flow.
- Preserve shared pools. Incomplete metadata, shared attachments, overlapping CRM folders, changed IIS
  targets or conflicting virtual-directory mappings block cleanup. For older deployments without an
  attachment, record verified details explicitly; do not discover deletion authority from a database.
- A failed removal retains its attachment and cleanup checkpoint for retry, including when the IIS target
  is already gone. `skip-crm-cleanup` is an explicit recovery option for unavailable authentication: it
  leaves CRM settings untouched and reports a warning. Use it only when that consequence is authorized.

Post-deploy readiness
1. `reg-web-app` - register the freshly deployed instance as a named clio environment.
2. `install-gate` - install the cliogate package into the new environment. Downstream workspace and
   package tooling (`restore-workspace`, `push-workspace`, `unlock-package`, ...) depend on it. If a
   gate-dependent tool fails with "you need to install the cliogate package version ... or higher",
   run `install-gate -e <environment>` and retry.
3. `restore-workspace` / `push-workspace` - move packages between the environment and a local workspace.

Failure policy
- If `assert-infrastructure` returns fail with no passing database candidates, stop with a blocker and
  report the failing sections rather than guessing a deployment target.
- If `deploy-creatio` returns a non-zero `exit-code`, persist `execution-log-messages` and stop.
- If an explicit `sitePort` is occupied or reserved, verify that the target was not created, discover another
  port, and retry or omit the argument to use the configured range. If the configured range is exhausted,
  free a port or configure a different valid inclusive range. Do not bypass the port check.
- If the configured `creatio-products` folder is missing or empty, fix the path (or place a build
  there) before retrying deploy-creatio.

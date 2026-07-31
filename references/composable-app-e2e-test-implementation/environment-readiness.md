# Environment Readiness

## Goal

Confirm the stand is usable for E2E execution before test implementation and run.

## Prerequisites

- Local tools are available (or auto-installable): `git`, `ssh`, `gh`, `clio`, `svn`, `node`, `npm`, `npx`.
- Clio environment credentials are available (direct args or registered env alias).
- Skill-local `.env` is configured (default path: `../.env` relative to this references folder).

Recommended:

1. Copy `.env.example` to `.env`.
2. Fill all required keys (`SVN_USERNAME`, `SVN_PASSWORD`) and repo host values.
3. Set either `CLIO_ENV_NAME` or `CREATIO_APP_URL`.
4. If URL-based auto-registration is needed, provide `CREATIO_LOGIN` and `CREATIO_PASSWORD` (or ensure active clio env credentials are valid).
5. Use built-in default artifact URLs from `references/sources.md` unless override is needed in `.env`.
6. Run `python scripts/validate_app_testability.py` before readiness checks.
7. Do not continue to any Jira/repo/test implementation steps until Step 0 passes.
8. Optional: tune SSH check timeout with `SSH_CONNECT_TIMEOUT_SECONDS` (default `15`).
9. Repository access checks are SSH-first with HTTPS fallback (using SVN credentials for HTTPS auth).

## 0. Local System Components Gate

Preflight starts with local component verification before any stand/repo actions.

Defaults:

- `AUTO_INSTALL_LOCAL_COMPONENTS=true`
- `LOCAL_COMPONENTS_REQUIRED=git,ssh,gh,clio,svn,node,npm,npx`
- `LOCAL_COMPONENTS_ALLOW_ELEVATION=false`
- `WINDOWS_PM_ORDER=winget,choco`

Behavior:

- On macOS/Windows, preflight attempts auto-install for missing local components.
- If installation requires elevated privileges, preflight stops and returns manual command blocker.
- If component remains unavailable after attempts, preflight fails immediately.

## 1. Stand Components Gate

### 1.1 Clio Connectivity and Registration

Check registered environments:

```bash
clio show-web-app-list
clio show-web-app-list <ENV_NAME>
```

If environment is missing, register it:

```bash
clio reg-web-app <ENV_NAME> -u <STAND_URL> -l <LOGIN> -p <PASSWORD>
```

If `CLIO_ENV_NAME` is empty and app URL is provided, preflight auto-resolves and persists environment alias.
If `CLIO_ENV_NAME` is filled but points to a different stand than app URL, preflight normalizes it to URL-resolved alias.

Verify `cliogate` availability for SQL-based checks:

```bash
clio get-info -e <ENV_NAME>
```

If it reports missing/outdated cliogate, install/update:

```bash
clio install-gate -e <ENV_NAME>
```

Skill preflight attempts this installation automatically by default.
Disable auto-install only if needed:

```bash
AUTO_INSTALL_CLIOGATE=false
```

### 1.2 Required Package Checks

Use `clio sql` (alias of `execute-sql-script`).

```bash
clio sql "SELECT Id FROM SysSettings WHERE Code = 'CustomPackageId'" -e <ENV_NAME>
```

#### Package presence check (example)

```sql
SELECT Name
FROM SysPackage
WHERE Name IN ('AutoTest');
```

For this skill, these test packages are mandatory:

- `AutoTest`

You can override the default list with `.env` key:

- `REQUIRED_TEST_PACKAGES=AutoTest`

Auto-install behavior:

- Preflight tries to install missing packages when `AUTO_INSTALL_TEST_PACKAGES=true`.
- Provide package artifacts location:
  - `TEST_PACKAGES_DIR=/absolute/path/to/test-package-artifacts`
- Repo-based fallback (no `.env` override needed):
  - skill-local `artifacts/test-packages`
- SVN fallback:
  - `AUTO_FETCH_TEST_PACKAGES_FROM_SVN=true` (default)
  - `SVN_TEST_PACKAGE_REPOS` (comma-separated SVN repo URLs) used for artifact discovery/export into local cache
  - Default package repo (if not overridden): `http://tscore-svn.tscrm.com:8050/svn/ts5conf/PackageStore/`
  - `SVN_TEST_PACKAGE_CACHE_DIR` optional cache directory (default: skill-local `artifacts/svn-package-repos`)
  - Preflight auto-cleans legacy full-checkout cache folders (directories containing `.svn`) inside cache root.
- Expected artifact names:
  - `<PackageName>.gz` or `<PackageName>.zip` or directory `<PackageName>`
  - On installation failure, full `clio push-pkg` output is saved to:
    - skill-local `artifacts/install-logs/<Package>-install.log`

Notes:

- This skill does not auto-install licenses.
- This skill does not auto-create/import test users.
- For SVN CLI operations, credentials are passed via `--username/--password` flags (tool behavior).

## 2. Composable App Sync Gate (workspace -> stand)

This step is executed only when `PUSH_APP_TO_STAND=true` (default depends on skill variant).

After successful stand components gate (1.1 + 1.2), preflight runs:

```bash
clio pushw -e <ENV_NAME>
```

This ensures current repository workspace code is pushed to the stand.

Control via `.env`:

- `PUSH_APP_TO_STAND=true` (composable app variant) or `false` (universal variant)
- `CLIO_PUSHW_WORKSPACE_DIR` (optional; default = current working directory)
- `CLIO_PUSHW_USE_APPLICATION_INSTALLER=false` (optional flag)

If push fails, preflight stops and stores full output in:

- skill-local `artifacts/install-logs/push-workspace.log`

## 3. Failure Handling

When readiness fails, stop and return blocker report with:

- failed step,
- exact command,
- exact stderr/stdout summary,
- missing access details,
- required user action.

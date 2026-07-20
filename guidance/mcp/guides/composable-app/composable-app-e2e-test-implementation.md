# Cross-Repo E2E Delivery (Composable App)

## Goal

Execute a full, repeatable cross-repo E2E delivery flow from feature analysis to test delivery, with mandatory environment readiness checks and composable app workspace deployment.

This is the Composable App variant -- it pushes the composable app workspace to stand before testing (`PUSH_APP_TO_STAND=true`). This ensures the stand has the latest feature code from the composable app repository before running E2E tests.

## Required Inputs

Collect these before implementation:

- Source repository path (default: current working directory -- expected to be a Composable App repo).
- Target E2E repository URL or name.
- Jira issue URL or Jira issue key.
- If Jira URL/key is unavailable, explicit tested-case description from user (business flow, expected behavior, affected pages/entities/events).
- Delivery depth: `patch only` | `branch + commit` | `branch + push + PR` (default).
- Target stand URL from `.env` (`CREATIO_APP_URL` preferred; legacy `APP_URL` / `CLIO_APP_URL` accepted).
- Credentials and environment alias for Clio/SVN when needed.
- Local env file path (default: skill-local `.env`).

If target repo URL is not provided, default to `https://creatio.ghe.com/engineering/creatio-playwright-tests.git`.

## Workflow Overview

The detailed step-by-step flow with exact commands lives in `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.flow`. The readiness check command matrix lives in `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.environment-readiness`. Read those files when executing each step.

### 0. Application testability preflight

Run `python scripts/validate_app_testability.py` to verify local tools, stand components, and composable app sync.

Key gates:
- **0.0 Local system components** -- verifies `git`, `ssh`, `gh`, `clio`, `svn`, `node`, `npm`, `npx` with auto-install on macOS/Windows.
- **0.1 Stand components** -- cliogate availability, required test packages (default: `AutoTest`) with auto-install.
- **0.2 Composable app sync** -- pushes current workspace to stand via `clio pushw` (`PUSH_APP_TO_STAND=true` by default in this variant). This ensures the tested composable app code is deployed to the stand.

Do not start any parallel analysis/implementation work before Step 0 passes.

### 1. Analyze source repository

Resolve context source (Jira URL/key or manual tested-case description). Inspect ticket/commits/diffs to extract testable behavior. Build implementation targets. See `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.flow` for details.

### 2. Bootstrap target E2E repository

Search one level above source repo. Clone or pull + setup. Verify TestKit docs exist. See `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.flow` for exact commands.

### 3. Test scope selection

Default: `Extended`. Override only if user explicitly requested `Basic` or `Full`.

### 4. Stand readiness gate

Use stand URL from `.env`. Run readiness checks via Clio and SQL probes per `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.environment-readiness`. If readiness fails, stop and emit blocker details -- do not auto-install licenses or test users.

### 5. Implement scenarios and tests

Translate validated feature behavior into scope-driven scenarios. Reuse target repo conventions and existing page objects first. Add/modify minimal required files only.

### 6. Execute agreed run scope

Run lint/type checks, then tests with `PW_USE_DYNAMIC_USERS=false`. On failures: fix, rerun, capture final status. If still failing, stop before git delivery and ask user.

### 7. Deliver results

Default delivery: `branch + push + PR` (only after successful checks/tests). If checks/tests fail, ask user whether to continue with patch only, commit without PR, or additional fixes.

Return a structured report covering: context source, repository bootstrap, readiness results, test implementation, execution results, git delivery state, and any blockers/risks.

## Mandatory Rules

- Do not start parallel work before Step 0 passes.
- Resolve analysis context only in Step 1; do not proceed without valid context.
- Always run with `PW_USE_DYNAMIC_USERS=false` unless user explicitly requested otherwise.
- If tests fail, do not push and do not open PR automatically.
- Always spawn subagents for parallelizable independent work when safe and tool-allowed.
- Default test scope is `Extended` -- use without asking.

## Reference Files

- `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.flow` -- detailed step-by-step workflow with commands
- `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.environment-readiness` -- readiness check command matrix
- `docs://knowledge/com.creatio.clio/reference.composable-app-e2e-test-implementation.sources` -- fixed artifact sources
- `.env.example` -- environment configuration template
- `scripts/validate_app_testability.py` -- preflight validation script
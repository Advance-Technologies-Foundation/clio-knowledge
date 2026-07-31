# Cross-Repo E2E Flow

## 0. Parallel Execution Policy

- Use spawned subagents for independent tasks whenever possible and allowed by tools.
- Keep parent agent as coordinator: assign scoped ownership, gather results, resolve conflicts.
- Do not parallelize stateful dependent operations (single branch writes, sequential setup/apply/re-check, final delivery).
- Do not start any parallel work (Jira/source analysis, target repo bootstrap, drafting tests) until Step 0 credential preflight has passed.

## 1. Preflight Gates

### 1.0 Local system components gate

- Verify local tools: `git`, `ssh`, `gh`, `clio`, `svn`, `node`, `npm`, `npx`.
- If missing, attempt auto-install on macOS/Windows.
- Fail-fast before any repo/bootstrap/test action if unresolved.
- Verify repository endpoint access in SSH-first mode with HTTPS fallback (SVN credentials for HTTPS auth).

### 1.1 Stand components gate

- Validate clio environment reachability.
- Validate/install `cliogate`.
- Validate required test packages (default `AutoTest`) with auto-install/re-check.

### 1.2 Composable app sync gate

- Run `clio pushw` only after 1.1 passes.
- This step runs only when `PUSH_APP_TO_STAND=true` (default depends on skill variant).
- When `PUSH_APP_TO_STAND=false`, this step is skipped entirely.

## 2. Source Context

1. Open source repo in current working directory.
2. Extract behavior under test from task context and source code.
3. Build concise test objective list tied to ticket id (or Jira URL-resolved key).

## 3. Target Repo Discovery and Setup

Only search one level above source repo.

Default repo URL when none provided by user:

- `https://creatio.ghe.com/engineering/creatio-playwright-tests.git`

```bash
source_repo="$(pwd)"
parent_dir="$(dirname "$source_repo")"
repo_name="creatio-playwright-tests"
target_repo="$parent_dir/$repo_name"
```

### If target repo exists

```bash
git -C "$target_repo" status --short
# if local changes are risky, ask user before pull
git -C "$target_repo" pull
cd "$target_repo"
npm install
npx playwright install chromium webkit --with-deps
```

### If target repo does not exist

```bash
git clone https://creatio.ghe.com/engineering/creatio-playwright-tests.git "$target_repo"
cd "$target_repo"
npm install
npx playwright install chromium webkit --with-deps
```

### Required post-check

```bash
test -d "$target_repo/node_modules/@creatio/playwright-testkit/dist/docs"
```

If missing, stop and report setup blocker.

## 4. Scope Decision

Use default scope `Extended` without asking.
If user explicitly requested `Basic` or `Full`, use that explicit requirement.

## 5. Stand URL Gate

Use stand URL from `.env` (`CREATIO_APP_URL`, fallback `APP_URL` / `CLIO_APP_URL`).
If URL is missing in `.env` and not provided by user, request it and wait.

## 6. Readiness Gate

Run readiness checks with Clio before implementation and execution.
Use `references/environment-readiness.md` command matrix.

## 7. Readiness Failure Handling

If readiness fails:

1. Stop and return blocker report with exact command/output and required next action.
2. Do not auto-install licenses.
3. Do not auto-create/import test users.

## 8. Implement and Execute

1. Implement test files in target E2E repo according to selected scope.
2. Run required checks and tests with `PW_USE_DYNAMIC_USERS=false` unless user explicitly requested a different value.
3. Iterate on failures until pass or hard blocker.

## 9. Delivery

If delivery depth is missing, default to:

- `branch + push + PR` (only after successful checks/tests)

Provide:

- changed files,
- run commands and outcomes,
- branch/commit,
- push/PR links when requested,
- blockers/risks.

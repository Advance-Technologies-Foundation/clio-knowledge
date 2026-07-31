# Composable App Repo Bootstrap

## Goal

Turn a fresh repository created from this starter template into an app-specific working repository without overwriting local files that were already customized.

## Required Inputs

- `AppName` in PascalCase, for example `Pulse`
- `ModuleName` in PascalCase, for example `JobHealth`
- `module-slug` in lowercase kebab-case, for example `job-health`

## Workflow

1. Confirm the current repository still looks like an uninitialized starter-kit clone:
   - root `AGENTS.md`
   - root `README.md`
   - `.agents/skills/`
2. Run the bundled helper with the platform-appropriate entrypoint:

```text
macOS/Linux: python3 .agents/skills/composable-app-repo-bootstrap/scripts/bootstrap_repo.py <AppName> <ModuleName> <module-slug>
Windows: py -3 .agents/skills/composable-app-repo-bootstrap/scripts/bootstrap_repo.py <AppName> <ModuleName> <module-slug>
```

3. Read the summary from the helper and continue only after it reports successful validation.

## What The Helper Does

- Validate the local toolchain before changing files:
  - `python 3.9+`
  - `clio`
  - `node.js 22+`
- Generate docs/spec/UI/package scaffold files and root templates from the skill `assets/` directory.
- Create `.env.example`, create `.env`, and auto-fill any safely discoverable values from local environment variables, a single registered `clio` web app, or an unambiguous local `PackageStore` path.
- Render root `README.md` from the bundled README template when the root README is still the generic starter-kit README.
- Render the root `AGENTS.md` from the bundled `assets/AGENTS.md.template` when it still contains starter placeholders, and remove duplicate `AGENTS.*.md` files on rerun so the repo ends with exactly one root `AGENTS.md`.
- Validate the expected bootstrap outputs and verify that app-specific generated files no longer contain `<AppName>`, `<ModuleName>`, or `<module-slug>`.

## Operating Rules

- Preserve an existing `.env`; never overwrite it unless the user explicitly asks.
- Preserve a user-customized root `README.md`; the helper skips it when it no longer matches the starter-kit marker.
- If a customized `AGENTS.md` conflicts with a duplicate `AGENTS.*.md` file, stop and report the blocker instead of leaving two AGENTS files in the root.
- Treat reruns as normal. Existing generated files are expected, and the helper should remain safe to run again with the same arguments.
- Keep generated documentation in English.
- Prefer the Python entrypoints in automated flows; keep `.sh` and `.cmd` files as thin convenience wrappers for shell-specific environments.
- If the toolchain check fails, stop before any bootstrap changes and report the missing or outdated utility explicitly.
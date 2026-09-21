clio MCP file system mode (FSM) guide

Scope
- Owns switching a registered Creatio environment's file design mode (FSM) on and off through
  `get-fsm-mode`/`set-fsm-mode`, and the ordering rules for combining that switch with
  `link-from-repository-*` package linking. `core-rules` cites this article for FSM's
  workspace-specific build/deploy rules; this article is their owner.
- For package compilation itself once FSM is on, follow the canonical DLL activation cycle owned
  by `core-rules`; this article does not restate it.

Check state before switching
- Call `get-fsm-mode` before `set-fsm-mode`. Do not assume an environment's current mode from
  this session's history — another process or operator may have changed it since.

Complete local package flow
- Before export, identify the package and preserve any repository edits. Export is database-to-filesystem; `pkg-to-db` is filesystem-to-database. Neither direction is a merge of competing edits.
- Enable FSM and verify its effective state. Unlock the intended editable package when required, complete `pkg-to-file-system`, and verify its `descriptor.json`, `Schemas/` and resources actually exist. A folder containing only generated source/binaries is not a complete editable package export.
- Preserve the exported package in the intended repository before linking it. Check `repoPath`, package selection and resolved environment path with the link tool's dry run, then verify the resulting link target. Follow the unlink/export rules below whenever exporting again.
- Edit the linked source and import package definitions with `clio pkg-to-db -e <ENV>`. Read the schema back to prove the expected edit reached the environment. This imports definitions, not package data rows; use the package installation/data-binding workflow for data.
- Client-only edits require browser reload/readback, not a C# compile. For changed backend source use the workspace's supported build/deploy path and the activation cycle in `core-rules`; a successful metadata import does not build or activate a DLL. For disagreements read `backend-deployment-troubleshooting`.
- Before deleting a disposable environment, remove its repository link and restore the real package directory so cleanup cannot traverse into preserved repository content.

Turning FSM on/off (`set-fsm-mode`)
- The two directions fail differently because the configuration write and the package
  load/export are not one atomic step:
  - `on` writes the configuration FIRST, then exports packages. A non-zero result can mean FSM is
    already enabled while the export did not happen. Re-check with `get-fsm-mode`; if it reports
    `on`, finish the export instead of calling `set-fsm-mode` again.
  - `off` imports packages FIRST, then writes the configuration. A non-zero result means the
    configuration was NOT changed and the environment is still in FSM. An environment that
    already reports FSM as off is not an error for `off`.
- On a .NET Core / .NET8 host, do NOT restart the application yourself before or after
  `set-fsm-mode on`. The tool restarts the application and retries login for up to 90 seconds on
  its own; a manual restart races it and is redundant.
  Evidence: [`TurnFsmCommand.cs`](https://github.com/Advance-Technologies-Foundation/clio/blob/9e730f01b24ff90798b8262775776ad7d4167fb9/clio/Command/TurnFsmCommand.cs).

Packages linked from a repository (`link-from-repository-*`)
- MUST unlink packages linked with `link-from-repository-*` (restore the real package directory
  or remove the symlink) BEFORE calling `set-fsm-mode on` on that environment, or before any
  standalone file-system export. The export writes generated files into whatever the package
  folder currently is, with no check for a symlink, so an environment whose packages are still
  linked has its export written straight into the linked repository's working tree instead of
  into the environment. Re-link with `link-from-repository-*` after the export completes.
  Evidence: [`LoadPackagesToFileSystemCommand.cs`](https://github.com/Advance-Technologies-Foundation/clio/blob/9e730f01b24ff90798b8262775776ad7d4167fb9/clio/Command/LoadPackagesToFileSystemCommand.cs),
  [`Link4RepoCommand.cs`](https://github.com/Advance-Technologies-Foundation/clio/blob/9e730f01b24ff90798b8262775776ad7d4167fb9/clio/Command/Link4RepoCommand.cs).
- `link-from-repository-by-environment` and `link-from-repository-by-env-package-path` run an
  automatic preparation step (Maintainer check, unlock, 2fs sync) before linking unless
  `skipPreparation` is set. Preparation is normal; its log lines are not an error. Pass
  `skipPreparation: true` only when preparation already ran in a prior call against the same
  packages — skipping it against packages that were never exported leaves symlinks pointing at
  content that does not exist yet.
- Compiling a package while its directory is linked into a repository regenerates that package's
  `.csproj` on every compile, which shows as repository churn (observed: on the order of 2,500
  changed lines for one package). This is expected platform behavior, not something the agent's
  own edits caused. If the user maintains the linked repository under git, suggest
  `git update-index --skip-worktree` on the generated `.csproj` and adding the package's
  `Assemblies/` output folder to `.gitignore`, rather than treating the diff as a regression to
  investigate.

Startup latency after switching FSM on
- An environment's first successful response after `set-fsm-mode on` (or a restart while FSM is
  on) has been observed to take roughly a minute longer than the same environment starting in
  database mode, because FSM resolves package content from disk — including through any linked
  package directories — instead of from pre-loaded database rows. `set-fsm-mode` already retries
  login through this window; when polling readiness yourself (for example after a manual
  `restart-by-environment-name` on an FSM environment), allow materially more time than a
  DB-mode environment before treating a non-responsive site as failed. Do not restart again to
  "unstick" it — restarting during this window compounds the wait instead of shortening it.

Disposable acceptance
- Creatio 10.1.585, .NET 8, PostgreSQL; clio source revision `e53009498`: switching on returned a partial-success error after writing configuration; a later `get-fsm-mode` reported on. After unlocking the package and completing export, schema metadata was present. Copied the exported package into a local repository, linked it with preparation already complete, edited one client schema on disk, ran `pkg-to-db`, and read the new body through `get-client-unit-schema`. Restored the physical package directory before teardown.
- This proves switch recovery, complete export, link and definition import. It does not claim package data installation or backend DLL activation from `pkg-to-db`. Evidence: [clio #1638](https://github.com/Advance-Technologies-Foundation/clio/issues/1638).

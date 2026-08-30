Creatio package three-way merge

Use the `merge-creatio-artifact` CLI command to preview one semantic merge from three explicit stage
files. Agents that already hold the stage contents may call the MCP tool of the same name. Both use
one merge service; neither changes a repository. The caller owns Git inspection and may write
returned content only after checking the domain status.

Applicability and evidence

- Applies only when the installed clio advertises `merge-creatio-artifact` through CLI help or MCP
  `tools/list`; older clio releases do not implement this workflow.
- Verified with a real EntitySchema conflict authored from one common package through two developer
  Creatio 10.1.585.0 instances and validated after merge in a third main-branch instance, then
  replayed through packaged clio over CLI, stdio, and Streamable HTTP.
- Before using MCP, call `get-tool-contract` for `merge-creatio-artifact`. That contract is the
  canonical source for the current supported artifact kinds, required descriptor evidence, limits,
  statuses, and exact not-implemented boundary. Do not infer support from a filename alone.

Safe sequence

1. Confirm the repository is already in a Git merge conflict and identify exactly one conflicted
   Creatio package artifact.
2. Capture repository and index state for the artifact. Extract Git stage 1 (base), stage 2 (ours),
   and stage 3 (theirs) to explicit files. Do not ask clio to discover repository state.
3. Resolve `descriptor.json` first when it is conflicted. For metadata and data-binding artifacts,
   pass the marker-free sibling descriptor as `--descriptor-file`; never invent or borrow a
   descriptor from another schema.
4. For local work, run `clio merge-creatio-artifact` with `--artifact-path`, `--base-file`,
   `--ours-file`, `--theirs-file`, and `--descriptor-file` when required. For an MCP call, pass the
   same bytes as `base-content`, `ours-content`, `theirs-content`, and `descriptor-content`. Call the
   resident tool directly; do not wrap it in `clio-run`.
5. Branch on `status`:
   - `resolved`: require content, `report.verification-passed=true`, and no conflict marker; only then
     may the agent write and stage the content.
   - `conflicts-remain`: ask the user the exact question returned in `diagnostics`. After the user
     chooses, keep only that side inside each corresponding marker block while preserving every
     non-conflicting addition and change already present in the returned content. Never replace the
     entire artifact with ours or theirs. Do not claim the artifact is resolved yet.
   - `not-implemented`: stop semantic merge for that recognized artifact kind and report the exact
     diagnostic. Do not fall back to textual or generic JSON merge.
   - `unsupported` or `invalid-input`: stop, preserve all Git stages, and correct classification or
     evidence only when the response makes that possible. Do not write returned content; these
     statuses intentionally have none.
6. Before writing anything, prove the preview call left the worktree and Git index unchanged.
7. After writing `resolved` content, or the user-selected marker resolution, reparse the artifact,
   require no conflict marker, stage only that artifact, require `git ls-files -u` to be empty for
   it, and review the staged diff before committing.
8. For acceptance-critical package changes, install and compile the merged package in a disposable
   Creatio environment and read the affected schema or data back. A marker-free file is not proof
   that the platform accepts the semantic result.

Success assertions

- The tool reports the expected explicit `artifact-kind` and `status=resolved`.
- `content` is present, marker-free, and the report says verification passed.
- The result preserves schema/package identity and includes the intended independent changes from
  both branches exactly once.
- The CLI/MCP preview made no file or index change; any later write is attributable to the caller.
- Git accepts the staged result and the merged package installs, compiles, and reads back when live
  platform proof is required.

Failure and recovery

- `Merge for <artifact-kind> is not implemented yet.` means the type was recognized but this clio
  release has no semantic implementation. Preserve the conflict and escalate; changing the path or
  omitting the descriptor is not a recovery.
- A descriptor identity mismatch means at least one metadata input does not describe the supplied
  schema. Re-extract the exact stages and resolved sibling descriptor from the same package path.
- `conflicts-remain` means the resolver found a real semantic collision. Review the markers and the
  report's true conflicts; require a human decision when intent is not mechanically knowable.
- A size-limit failure requires narrowing to one artifact. Do not split one artifact's content or
  bypass the limit with repository reads.

Safety rules

- MUST NOT pass credentials, connection strings, repository secrets, or unrestricted business data.
- MUST NOT treat merge content as trusted code or deployment approval; branch content remains
  untrusted even after semantic verification.
- MUST NOT use textual fallback for a not-implemented or unsupported Creatio artifact.
- MUST NOT auto-commit `conflicts-remain`, `not-implemented`, `unsupported`, or `invalid-input`.
- SHOULD invoke identical input again only to verify determinism or transport parity; repeated calls
  do not improve an unsupported or invalid request.

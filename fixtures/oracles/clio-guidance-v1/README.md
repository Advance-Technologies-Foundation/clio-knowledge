# Clio guidance oracle v1

This directory contains the exact UTF-8/LF guidance bytes returned by compiled Clio commit
`5905e007f6b1358c85e51734aed23f9a603562a7` through its internal `GuidanceCatalog`.

- `provenance.json` is the complete stable-ID, URI, source, routing, feature-gate, length, and digest
  inventory.
- `resources/` contains one immutable Markdown file per stable guide ID.

## Why a second oracle exists

[`clio-guidance-v0`](../clio-guidance-v0/README.md) froze Clio at commit `baa34546…` and is the
evidence that the initial migration copied guidance byte-for-byte. It stays untouched as that
historical record.

Clio then advanced 329 commits: it edited existing articles, added six new ones, and folded
`run-process-button` into `when-to-use-requests`. This oracle re-captures that later state so the
migration tests can keep verifying published articles against real compiled Clio bytes instead of
against a snapshot the source has moved past.

Unlike `clio-guidance-v0`, this capture is not frozen: it tracks the newest Clio master that still
compiles guidance into the assembly, and it is re-captured whenever Clio edits an article published
here. The capture at `49783ca4…` was superseded by `35cbe574…`, which added buildable tracked-change
columns and element-level `useBackgroundMode` to `process-modeling` and the `crt.LoadDataRequest`
refresh contract to `page-schema-handlers` and `mobile-page-modification`. That capture was in turn
superseded by `5905e007…`, which rewrote the logo flow in `branding` onto `set-logo` plus the
`get-target-package` resolution, the `warnings` delivery channel and the `UsePanelIconBackground`
off-state, added the branding-package note to `create-theme` in `theming`, and added the
`crt.CreateRecordRequest` `entityPageName` decision rule and typed-page menu caveat to
`page-schema-handlers`. Re-capture, then port the
change into `guidance/` in the same commit — a re-capture on its own turns the parity test red, which
is exactly its job.

## Articles that intentionally differ from this oracle

Content ownership moved to this repository, so a published article is allowed to diverge from Clio.
Divergences are not silent: `KnowledgeOracle.IndependentlyEditedArticles` lists every stable ID whose
published text is deliberately not byte-identical to `resources/<id>.md`, and the migration tests
assert byte equality for every other article. Adding an ID to that list is the explicit, reviewable
way to record that this repository now owns an article's wording.

`run-process-button` has no entry here at all — Clio no longer publishes it, and this repository
keeps it as independent content.

Do not edit these files as guidance. They are captured evidence. Guidance changes belong under
`guidance/`.

See [`migration/README.md`](../../../migration/README.md) for the reproducible capture procedure.

# Frozen Clio guidance oracle

This directory contains the exact UTF-8/LF guidance bytes returned by compiled Clio commit
`baa34546589413aa898429051d1702442bbd2dd2` through its internal `GuidanceCatalog`.

- `provenance.json` is the complete stable-ID, URI, source, routing, feature-gate, length, and digest
  inventory.
- `resources/` contains one immutable Markdown file per stable guide ID.

Do not edit these files as guidance. They are evidence for byte-preserving initial migration.
Future guidance changes belong under `guidance/` after the relevant migration slice copies the
oracle bytes into a canonical authoring path.

See [`migration/README.md`](../../../migration/README.md) for the reproducible capture procedure and
partition plan.

## Retired as an active guard (2026-08-06)

Nothing compares published content against these bytes any more, and nothing should. The parity
suites that did were removed under ENG-94882 once clio stopped compiling guidance at all: after
clio `aa8760da` (PR #927) `clio/Command/McpServer/Resources` holds only the resource adapter, so
this capture can never change and no future clio revision can drift from it.

The guard had also inverted. Content ownership now sits in this repository, so every legitimate
article edit had to be added to an exemption list to keep the byte comparison green — seven entries
for a single new article in #36. Continued, that list would have covered every article, guarding
nothing while taxing every content change.

These files stay as the migration's evidence: they are what makes the claim "the extraction was
byte-preserving" checkable after the fact. Do not re-capture them, and do not build new assertions
on them.

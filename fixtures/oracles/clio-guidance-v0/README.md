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

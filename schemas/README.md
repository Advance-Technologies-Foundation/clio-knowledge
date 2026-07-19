# Schemas

Compatibility bounds use exact inclusive `MAJOR.MINOR.PATCH` versions. Wildcards and prerelease
labels are intentionally outside the bundle contracts so producers and consumers apply the same
comparison rule.

Every manifest contains at least one resource. A "valid empty" conformance fixture therefore
means a zero-length resource payload, not an empty `resources` array.

This directory contains machine-readable contracts for guidance manifests and will later cover advisories, capabilities, reference-example metadata, and routing.

Schemas must remain versioned and backward-compatible where practical. A Clio client must be able to reject a bundle schema it cannot understand without replacing its active verified guidance.

The canonical multi-source candidate is
[`v1/knowledge-bundle.schema.json`](v1/knowledge-bundle.schema.json). It makes the immutable
generation identity explicit with `libraryId`, `libraryVersion`, and `sequence`; every resource has
an `itemId`, logical `topicId`, `role`, and exact `docs://knowledge/<library-id>/<item-id>` route.
Optional `legacyUris` are signed transition metadata and never replace the canonical identity.
Source provenance uses a complete 40-character SHA-1 or 64-character SHA-256 Git object ID; abbreviated
commit names are not immutable publication evidence.

[`v0/knowledge-bundle.schema.json`](v0/knowledge-bundle.schema.json) is retained as the legacy
single-library POC contract for existing conformance fixtures. New publications must use v1. Both
schemas remain experimental candidates rather than production-stable contracts.

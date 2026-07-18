# Schemas

Compatibility bounds use exact inclusive `MAJOR.MINOR.PATCH` versions. Wildcards and prerelease
labels are intentionally outside the v0 contract so producers and consumers apply the same
comparison rule.

The v0 manifest contains at least one resource. A "valid empty" conformance fixture therefore
means a zero-length resource payload, not an empty `resources` array.

This directory contains machine-readable contracts for guidance manifests and will later cover advisories, capabilities, reference-example metadata, and routing.

Schemas must remain versioned and backward-compatible where practical. A Clio client must be able to reject a bundle schema it cannot understand without replacing its active verified guidance.

The experimental manifest contract is [`v0/knowledge-bundle.schema.json`](v0/knowledge-bundle.schema.json). It is a P1 candidate, not a production-stable schema.

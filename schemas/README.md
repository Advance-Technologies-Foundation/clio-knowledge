# Schemas

This directory contains machine-readable contracts for guidance manifests and will later cover advisories, capabilities, reference-example metadata, and routing.

Schemas must remain versioned and backward-compatible where practical. A Clio client must be able to reject a bundle schema it cannot understand without replacing its active verified guidance.

The experimental manifest contract is [`v0/knowledge-bundle.schema.json`](v0/knowledge-bundle.schema.json). It is a P1 candidate, not a production-stable schema.

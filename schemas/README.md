# Schemas

Compatibility bounds use exact inclusive `MAJOR.MINOR.PATCH` versions. Wildcards and prerelease
labels are intentionally outside the bundle contracts so producers and consumers apply the same
comparison rule.

Every manifest contains at least one resource. A "valid empty" conformance fixture therefore
means a zero-length resource payload, not an empty `resources` array.

This directory contains machine-readable contracts for guidance manifests and will later cover advisories, capabilities, reference-example metadata, and routing.

Schemas must remain versioned and backward-compatible where practical. A Clio client must be able to reject a bundle schema it cannot understand without replacing its active verified guidance.

[`v1/knowledge-repository.schema.json`](v1/knowledge-repository.schema.json) is the source contract
for `bundle-source.json` in a trusted Git repository. It deliberately omits repository, commit,
timestamp, and signature fields: Git supplies provenance and time, while a transport publisher
supplies any transport-specific signing metadata. Every resource owns a concise `title` and
`description`; consumers use this metadata to advertise and select knowledge before loading the
resource body.

[`v1/knowledge-bundle.schema.json`](v1/knowledge-bundle.schema.json) is the generated NuGet bundle
contract. It makes the immutable generation identity explicit with `libraryId`, `libraryVersion`,
and `sequence`; every resource has an `itemId`, logical `topicId`, `role`, and exact
`docs://knowledge/<library-id>/<item-id>` route. Optional `legacyUris` are signed transition metadata
and never replace the canonical identity. The producer-owned `title` and `description` are preserved
in the generated manifest so packaged transports expose the same discovery experience as Git.
Generated source provenance uses a complete 40-character
SHA-1 or 64-character SHA-256 Git object ID; abbreviated commit names are not immutable publication
evidence. The source commit itself supplies the timestamp.

[`v0/knowledge-bundle.schema.json`](v0/knowledge-bundle.schema.json) is retained as the legacy
single-library POC contract for existing conformance fixtures. New publications must use v1. Both
schemas remain experimental candidates rather than production-stable contracts.

# Workspace diary

Append-only engineering notes. Newest entries at the bottom.

## 2026-08-02 19:30 – Release delivery: signed GitHub Release asset replaces the Git checkout
Context: Clio's built-in `creatio-curated` source cloned this repository's
`master` at runtime. That needed a Git CLI on the user's machine, depended on a
mutable branch, and carried no publisher signature. This repository becomes the
publisher of an immutable signed artifact instead.
Decision: publish `clio-knowledge-bundle.zip` as a GitHub Release asset on a tag
equal to `libraryVersion`, built by the existing deterministic builder. Added
`.github/workflows/release-bundle.yml` (tag or explicit dispatch only), a
`verify` verb backed by `BundleVerifier`, and `ReleaseArtifactTests`. The
consumer half is clio PR #927.
Discovery: three constraints that shape the pipeline.
(1) The release tag MUST equal `libraryVersion`: Clio records the tag as the
installed revision and its runtime refuses a bundle whose manifest declares a
different version. The workflow checks this before it builds anything.
(2) The artifact is NOT byte-reproducible, and that is fine. Detached ECDSA
signatures embed a random nonce, so `manifest.sig` and therefore the archive
hash differ per build. Reproducibility is asserted on manifest bytes, resource
bytes, entry order, timestamps, and attributes; consumer identity is
`(libraryId, sequence, bundleDigest)` over the manifest, and transport integrity
is the digest GitHub publishes for the one uploaded artifact.
(3) Draft-then-verify-then-publish is load-bearing: the asset is downloaded back
from the draft and re-verified against the public key and GitHub's advertised
digest before the release becomes visible, so a consumer can never observe a
release whose asset is missing, truncated, or still uploading.
Files: .github/workflows/release-bundle.yml,
automation/Clio.Knowledge.Bundle/BundleVerifier.cs, Program.cs,
automation/Clio.Knowledge.Bundle.Tests/ReleaseArtifactTests.cs,
distribution/RELEASING.md, distribution/keys/clio-knowledge-2026-08-public.pem,
README.md, CONTRIBUTING.md, automation/README.md
Impact: merging to master no longer reaches a user — publishing is a deliberate
release. Every content change needs BOTH a new `libraryVersion` and a new
`sequence`; reusing a sequence with different content makes Clio reject the
whole library. Key rotation is consumer-first: ship the successor public key in
a Clio release before signing with it, or every new release becomes unusable
while the old one silently stays active.

## 2026-08-06 18:30 – DataService UPSERT guidance
Context: Added canonical guidance for Creatio DataService `UpdateQuery.IsUpsert`.
Decision: Kept UPSERT as its own focused guide and registered it in the bundle catalog and routing map; dedicated UpdateQuery and InsertQuery cross-references remain future work.
Discovery: `IsUpsert` is a query-then-update-or-insert path, not an atomic database merge. Creatio Core revision `e0d0f98b80c8fd26e305804c7cb3242b76baf072` establishes the request contract and zero-match insert branch.
Files: guidance/mcp/guides/backend/dataservice-upsert.md, guidance/mcp/guides/routing.md, bundle-source.json, automation/Clio.Knowledge.Bundle.Tests/PublishedGenerationTests.cs, distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj
Impact: Agents can discover the UPSERT safety rules by the stable `dataservice-upsert` item and route.

## 2026-08-06 20:30 – Entity listener changed-column guard
Context: A Forester lab exposed that the canonical entity-listener guide lacked a supported way to prevent a listener from repeating its own same-entity calculated-field update.
Decision: Make `configuration-entity-event-listener` the sole owner of the changed-column / self-triggering rule and add the `Entity.GetChangedColumnValues()` guard pattern. Bump the direct-source library generation.
Discovery: Core yields values with `IsChanged` through `GetChangedColumnValues()` in before and after hooks. The after-event changed collection also contained `ModifiedOn` and `ModifiedById`, so audit values must not be used as business triggers.
Files: guidance/mcp/guides/composable-app/configuration-entity-event-listener.md, bundle-source.json
Impact: agents can now distinguish a business-input update from their own output write and avoid unnecessary recursive listener work.

## 2026-08-11 11:29 – Web→mobile drop reasons: remove multi-data-source, add the explicit non-rule
Context: ENG-94929 removes the converter's "multi-data-source" drop, so the conversion guide's drop enumeration named a reason clio will never emit again.
Decision: In `web-to-mobile-conversion.md`, replace `multi-data-source` in the drop-reason list with the unsupported-button-request reason that clio DOES emit, and state the non-rule positively — an element is never dropped for the data source it is bound to. A reader who only sees a reason removed cannot tell whether the case disappeared or was renamed.
Discovery 1: the false premise was NOT in live guidance. The only "mobile disables multi-data-source" claim sits in `fixtures/oracles/clio-guidance-v0/resources/mobile-page-modification.md` — a frozen oracle of an older generation, deliberately left untouched.
Discovery 2: the digest guard cannot be run on a host without a .NET 10 SDK (`automation/` targets net10.0; NETSDK1045). Reproduced `ComputeContentDigest` independently instead — SHA-256 over framed `bundle-source.json` then framed resource bodies in manifest declaration order, 8-byte little-endian length prefixes — and VALIDATED the reimplementation by recomputing the previous generation from an unmodified checkout, which returned the recorded D5B08C59… exactly. Only then was the new value trusted. `Clio.Knowledge.Package.csproj` is not a hashed resource, so bumping it does not move the digest.
Files: guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md, bundle-source.json, automation/Clio.Knowledge.Bundle.Tests/PublishedGenerationTests.cs, distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj
Impact: generation 1.13.9 / sequence 24. When the pinned-digest test cannot run locally, reproducing its formula is viable — but validate the reproduction against the CURRENT pin first, or a wrong digest ships looking deliberate.

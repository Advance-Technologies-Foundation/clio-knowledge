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

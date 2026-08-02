# Releasing the knowledge bundle

Clio's built-in `creatio-curated` source is delivered as a signed GitHub Release asset. This is the
supported production path: it needs no Git CLI on the consumer machine, it never depends on a mutable
`master`, and every generation a consumer activates is an immutable, signed, digest-verified artifact.

## What a release is

| Property | Value |
|---|---|
| Repository | `Advance-Technologies-Foundation/clio-knowledge` |
| Asset name | `clio-knowledge-bundle.zip` (exact, one per release) |
| Tag | the `libraryVersion` from `bundle-source.json`, e.g. `1.9.0` |
| Signature | detached ECDSA P-256 / SHA-256 over the manifest bytes |
| Key ID | `clio-knowledge-2026-08` |
| Public key | [`distribution/keys/clio-knowledge-2026-08-public.pem`](keys/clio-knowledge-2026-08-public.pem) |

The archive contains only runtime content: `manifest.json`, `manifest.sig`, and one `resources/*`
entry per declared resource. Automation sources, test projects, fixtures, migration evidence, and key
material are never part of it, and `ReleaseArtifactTests` fails the build if any of them appear.

## Identity rules

Generation identity is the triple `(libraryId, sequence, bundleDigest)`. `libraryVersion` is a
publisher label and the release tag; it never substitutes for the monotonic `sequence`.

- A consumer refuses a lower sequence, so a release can never downgrade active content.
- A consumer refuses the same sequence carrying a different digest, and rejects the whole library
  when it sees one. Reusing a sequence is therefore a breaking mistake, not a cosmetic one.
- The same identity and digest is an idempotent no-op.
- The release tag must equal `libraryVersion`. Clio records the tag as the installed revision and
  refuses a bundle whose manifest declares a different version, so the pipeline checks this before it
  builds anything.

Every content change therefore needs **both** a new `libraryVersion` and a new `sequence`.

## How to publish

1. Land the content change on `master` and make sure `PublishedGenerationTests` records the new
   sequence and content digest.
2. Run the release workflow, either by pushing the version tag:

```bash
git tag 1.10.0 && git push origin 1.10.0
```

   or by dispatching **Release knowledge bundle** and typing the same version into the confirmation
   input.

The workflow then, in order: runs the producer contract suite, checks the tag against
`bundle-source.json`, refuses to continue if a **published** release for that tag already exists,
builds and signs the bundle, verifies the artifact against the committed public key, creates a
**draft** release, uploads the asset, downloads it back and verifies both the signature and the
digest GitHub published for it, and only then publishes. A failure at any step leaves the release a
draft, so a consumer never observes a half-published generation.

Re-running after a failure is safe: a leftover draft for the same tag was never visible to a
consumer, so the run clears it and starts over. A published release is never touched.

Note that the artifact's SHA-256 differs between builds of the same commit — the detached ECDSA
signature embeds a random nonce. The digest that matters is the one GitHub publishes for the single
uploaded asset, which the workflow compares against the bytes it downloads back.

## Signing key

The private key exists only as the `KNOWLEDGE_SIGNING_PRIVATE_KEY` repository secret. It is written
to a `umask 077` temporary file inside the runner, never into the workspace, the logs, or the asset,
and is shredded when the step ends. It is not the repository's `fixtures/keys/p1-test-private.pem`,
which is disposable test material and must never sign a public release.

### Rotation

Rotation is additive and consumer-first, because a Clio release older than the rotation must still be
able to verify what it already installed:

1. Add the successor public key to `BuiltInKnowledgeBundleTrustStore` in Clio, alongside the
   incumbent, and ship that Clio release.
2. Commit the successor public key here and switch `SIGNING_KEY_ID` and `PUBLIC_KEY_PATH` in the
   workflow, and the repository secret, to the successor.
3. Remove the retired key from Clio only once no supported Clio version still needs it.

Never reverse this order: signing with a key no shipped Clio trusts makes every new release
unusable while leaving the old one active — a silent freeze rather than a visible failure.

## Immutable releases

GitHub's immutable-releases repository setting prevents a published release's tag and assets from
being replaced. Clio reads the `immutable` flag on the release it installs and, when it is not set,
appends an advisory to the `install-knowledge` / `update-knowledge` result for that source, so
enabling the setting is strongly recommended for this repository. Changing repository settings is an
owner decision and is deliberately not automated here.

## Verifying a release by hand

```bash
gh release download 1.9.0 --pattern clio-knowledge-bundle.zip --dir /tmp/knowledge
dotnet run --project automation/Clio.Knowledge.Bundle -- verify \
  /tmp/knowledge/clio-knowledge-bundle.zip \
  distribution/keys/clio-knowledge-2026-08-public.pem \
  clio-knowledge-2026-08 \
  1.9.0
```

## Relationship to the other transports

NuGet delivery stays available for private or separately signed third-party libraries, and generic
Git sources stay available for partner and customer repositories — those still require a Git CLI on
the consumer machine. Neither is used for the built-in curated source any more.

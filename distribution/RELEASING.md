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

Generation identity is the triple `(libraryId, sequence, bundleDigest)`.

- A consumer refuses a lower sequence, so a release can never downgrade active content.
- A consumer refuses the same sequence carrying a different digest. The refusal keeps the generation
  it already installed active, so the edit reaches nobody who synced the earlier bytes until the
  sequence moves forward. That decision is a high-water mark on the consumer's disk and survives a
  restart, which is why reusing a sequence is a breaking mistake rather than a cosmetic one.
- The same identity and digest is an idempotent no-op.
- The release tag must equal `libraryVersion`. Clio records the tag as the installed revision and
  refuses a bundle whose manifest declares a different version, so the pipeline checks this before it
  builds anything.

`sequence` is **derived from `libraryVersion`, never authored**. `bundle-source.json` declares no
`sequence` field, and the schema rejects one. The builder maps one to four numeric version components
onto fixed decimal slots — `1.13.9` becomes `1013009000`, a date-style `2026.07.19.1` becomes
`2026007019001` — so the sequence rises with the version and omitted trailing components read as zero.

That leaves **one** number to maintain: `libraryVersion`. Because the release tag must equal it and a
published tag is never overwritten, different content cannot reach a consumer under a sequence it
already accepted. Bumping the version is therefore the whole obligation, and the **Producer contract
suite** enforces it: a pull request that changes `bundle-source.json` or any body it declares fails
while the derived sequence matches the base branch, or moves backwards from it. Two labels can derive
the same sequence — `1.13` and `1.13.0.0` both give 1013000000 — so that check compares derived
sequences rather than version strings.

## Who can publish, and from where

Merging a pull request into `master` publishes. `master` is protected — no direct push, no force
push, no branch deletion — so a release always follows a pull request whose **Producer contract
suite** check passed. Repository administrators can bypass that protection; nothing else can.

**Auto-release on merge** runs on every push to `master`, reads `libraryVersion` from
`bundle-source.json`, and starts **Release knowledge bundle** for that version. When a published
release for the version already exists it skips and says so, so a merge that bumped no version ships
nothing. It never chooses a version and never commits: the version comes from the merged pull request,
and everything else about the generation — the sequence, the packed transport version — is derived
from it.

It starts the release through `workflow_dispatch` rather than by pushing the tag itself, because a tag
pushed with the default `GITHUB_TOKEN` does not start another workflow — the release would silently
never run.

Two manual entry points remain, for re-publishing after an infrastructure failure or for a generation
that landed before this automation existed:

- **From the GitHub UI** — Actions → *Release knowledge bundle* → *Run workflow*, branch `master`,
  and type the `libraryVersion` from `bundle-source.json` into the confirmation box. The run fails
  fast if it does not match, so a mistyped version cannot publish the wrong generation. This path
  needs nothing installed at all.
- **By pushing a version tag** — for anyone already at a terminal. The branch protection targets
  `master`, so it does not block a tag push.

Everything runs on GitHub — no local .NET, no local clone, and no access to the signing key. The key
lives only in the `KNOWLEDGE_SIGNING_PRIVATE_KEY` repository secret and is read by the runner, never
by a person.

## How to publish

1. In the pull request, bump `libraryVersion` in `bundle-source.json`. Nothing else carries the
   generation number: the `sequence` is derived from that label, and the NuGet transport version is
   read out of the same field at pack time.
2. Merge the pull request. **Auto-release on merge** starts **Release knowledge bundle** for the new
   `libraryVersion`, and publishing the release creates the tag.

To publish without a merge — a retry, or a version already on `master` — push the tag:

```bash
git tag 1.10.0 && git push origin 1.10.0
```

or dispatch **Release knowledge bundle** from the Actions tab and type the same version into the
confirmation input.

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

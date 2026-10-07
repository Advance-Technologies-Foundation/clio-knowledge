# Automation

## Frozen runtime oracle

`Clio.Knowledge.OracleCapture` reads the compiled Clio guidance resource fields through reflection
and writes canonical UTF-8/LF payloads plus provenance and SHA-256 hashes. This avoids maintaining
a hand-copied baseline that may differ from what the MCP server actually serves.

```powershell
dotnet run --project automation/Clio.Knowledge.OracleCapture -- `
  C:\path\to\clio.dll `
  <clio-commit> `
  C:\path\to\GuidanceGetTool.cs `
  oracle\esq
```

This directory contains the experimental v1 multi-source bundle builder and will later contain
publication and compatibility automation. The legacy v0 schema and conformance fixture remain
available as migration evidence, but the builder accepts only canonical v1 source descriptors.

`Clio.Knowledge.Bundle` canonicalizes text as UTF-8 without BOM with LF newlines, orders items by
`itemId`, sorts requirements, aliases, and optional per-resource `requiredFeatures` ordinally,
validates exact namespaced routes, computes resource digests, preserves each resource's required
discovery `title` and `description`, rejects control characters in discovery text, signs
deterministic manifest bytes with a detached P1 test signature, and writes
a fixed-layout uncompressed ZIP through a sibling temporary file and atomic destination replacement.
Failed builds preserve an existing destination and remove their temporary file. Producer bounds match
the Clio consumer: 1,024 total archive entries, 4 MiB per resource, 32 MiB total resource bytes, and
40 MiB compressed archive bytes. Source provenance accepts only complete 40-character SHA-1 or
64-character SHA-256 object IDs. Detached ECDSA signatures are intentionally not byte-deterministic,
so reproducibility is asserted on canonical resource and manifest bytes rather than the final archive
hash.

Build the current canonical guidance bundle from the repository root with:

```powershell
dotnet run --project automation/Clio.Knowledge.Bundle -- build `
  bundle-source.json `
  fixtures/keys/p1-test-private.pem `
  artifacts/knowledge-bundle.zip `
  p1-test `
  Advance-Technologies-Foundation/clio-knowledge `
  (git rev-parse HEAD)
```

The `build` verb may be omitted for backward compatibility. The P1 key is disposable test material
and must not be reused for production publication; production releases are signed in CI with the key
described in [distribution/RELEASING.md](../distribution/RELEASING.md).

Check a built or downloaded artifact the way the consumer will, before publishing or after
downloading it:

```powershell
dotnet run --project automation/Clio.Knowledge.Bundle -- verify `
  artifacts/knowledge-bundle.zip `
  distribution/keys/clio-knowledge-2026-08-public.pem `
  clio-knowledge-2026-08 `
  1.9.0
```

`verify` confirms the manifest signature against the trusted public key, the declared key ID, the
optional expected library version, that every declared resource is present exactly once with its
declared length and digest, and that the archive contains no entry the manifest does not declare.
The release workflow runs it twice: on the artifact it built, and on the artifact downloaded back
from the draft release.
`bundle-source.json` must reference canonical files under `guidance/` or `catalog/`. Files under
`fixtures/oracles/` are immutable migration evidence and must never become the publication source.

The v1 manifest identity is `(libraryId, sequence, bundleDigest)`. `libraryVersion` is the publisher
release version and must equal the stable NuGet package version when the same artifact is distributed
through NuGet. Each item has one exact `docs://knowledge/<library-id>/<item-id>` URI plus optional
signed `legacyUris`; aliases are compatibility metadata and are not eligible as canonical identity.

Git transport does not use this builder; it reads the repository manifest and source files directly.
The NuGet distribution project invokes the builder into its intermediate output directory while
packing. Generated ZIP files are build artifacts and must not be committed.

## Reference integrity

Identifiers and the references between published items are checked by `dotnet test` on
`automation/Clio.Knowledge.Bundle.Tests`. That is the suite the required **Producer contract suite**
pull-request check runs, and the release workflow runs it again before it builds a bundle, so the same
command fails locally and in CI. A reference counts as resolved only when Clio would resolve it the same
way: a `get-guidance` name must be the `itemId` or `topicId` of a `guidance`-role item, and a route must
equal a declared `uri` or `legacyUris` entry exactly.

| Identifier or reference | Resolves against | Checked by |
|---|---|---|
| `itemId`, `topicId`: format, uniqueness, unique topic and role pair | — | `BundleBuilder` (`BundleBuilderTests`) |
| `uri` derived from `libraryId` and `itemId`; `legacyUris` unique across items | — | `BundleBuilder` |
| `requirements.itemIds`, `requirements.resourceUris` | the declared resources; a mismatch names the ids on each side | `BundleBuilder` |
| `requiredFeatures`: format and uniqueness | — | `BundleBuilder` |
| `sourcePath` | an existing file under the role's root; every guidance and reference file is declared | `BundleBuilder`, `GuidanceInventoryTests`, `ReferenceGuidanceMigrationTests` |
| `get-guidance` names in any published body: `name=<id>` and ``get-guidance … `<id>` `` | `itemId` or `topicId` of a `guidance`-role item | `ReferenceIntegrityTests` |
| `docs://knowledge/…` and `docs://mcp/…` routes in any published body | a declared `uri` or `legacyUris` entry | `ReferenceIntegrityTests` |
| Relative markdown links in any published body | a file declared as a `sourcePath` | `ReferenceIntegrityTests` |
| Top-level `id:` of a catalog entry | the entry's manifest `itemId` | `ReferenceIntegrityTests` |
| Every supporting reference is linked from a primary guide | the declared `reference` items | `ReferenceGuidanceMigrationTests` |
| Section citations between process articles | the cited article's headings | `ProcessGuideCrossReferenceTests` |

`ReferenceIntegrityTests` reports every dangling reference in one run, one per line, as
`<source file>:<line>: <reference> -> <what is missing>`.

Not checked yet:

- Catalog `primaryUseCase.id` and `supportingCapabilities`: `capabilities/` holds no registry yet, so
  there is no declared set to resolve them against. They become checkable when the registry lands.
- Advisory IDs: no advisory is published yet.
- `requiredFeatures` values: they name Clio feature toggles, which Clio declares, not this repository; no
  item declares one today.
- Anchors in relative links (`#section`): no published body has a relative link today, and heading
  slugs differ between renderers and the `== … ==` heading style.
- A guide named in other prose forms: a bare backticked name (``the `run-process-button` guide``), a
  table cell under a "get-guidance with name" header, or an unquoted name after `get-guidance`. Without a
  marker these cannot be told apart from tool or component names.
- `docs://help/…` routes: Clio serves them itself; they are not part of this library.
- A route into another library (`docs://knowledge/<other-library>/…`): none exists. The scan reports one as
  dangling, because this repository cannot vouch for another library's items.
- Feature gating: a reference from an ungated article to a gated one resolves here but is hidden where the
  feature is off.

## NuGet runtime spike

`Clio.Knowledge.NuGetSpike` uses the official NuGet Client SDK in-process to discover, download,
and extract the signed knowledge payload. It intentionally does not verify or activate the inner
bundle; that remains the consumer runtime's responsibility.

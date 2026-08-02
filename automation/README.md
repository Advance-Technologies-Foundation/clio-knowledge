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

## NuGet runtime spike

`Clio.Knowledge.NuGetSpike` uses the official NuGet Client SDK in-process to discover, download,
and extract the signed knowledge payload. It intentionally does not verify or activate the inner
bundle; that remains the consumer runtime's responsibility.

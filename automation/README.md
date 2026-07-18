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

This directory contains the experimental v0 bundle builder and will later contain publication and compatibility automation.

`Clio.Knowledge.Bundle` canonicalizes text as UTF-8 without BOM with LF newlines, orders resources ordinally, computes resource digests, signs deterministic manifest bytes with a detached P1 test signature, and writes a fixed-layout uncompressed ZIP. Detached ECDSA signatures are intentionally not byte-deterministic, so reproducibility is asserted on canonical resource and manifest bytes rather than the final archive hash.

Build a bundle with:

```powershell
dotnet run --project automation/Clio.Knowledge.Bundle -- <bundle-source.json> <test-signing-key.pem> <output.zip>
```

The P1 key is disposable test material and must not be reused for production publication.

## NuGet runtime spike

`Clio.Knowledge.NuGetSpike` uses the official NuGet Client SDK in-process to discover, download,
and extract the signed knowledge payload. It intentionally does not verify or activate the inner
bundle; that remains the consumer runtime's responsibility.

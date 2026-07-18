# Automation

This directory contains the experimental v0 bundle builder and will later contain publication and compatibility automation.

`Clio.Knowledge.Bundle` canonicalizes text as UTF-8 without BOM with LF newlines, orders resources ordinally, computes resource digests, signs deterministic manifest bytes with a detached P1 test signature, and writes a fixed-layout uncompressed ZIP. Detached ECDSA signatures are intentionally not byte-deterministic, so reproducibility is asserted on canonical resource and manifest bytes rather than the final archive hash.

Build a bundle with:

```powershell
dotnet run --project automation/Clio.Knowledge.Bundle -- <bundle-source.json> <test-signing-key.pem> <output.zip>
```

The P1 key is disposable test material and must not be reused for production publication.

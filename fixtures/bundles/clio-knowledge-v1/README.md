# Clio Knowledge v1 conformance bundle

`valid.zip` is a byte-identical copy of the ready repository-root artifact. Generate and sign once,
then copy those exact bytes:

```powershell
dotnet run --project automation/Clio.Knowledge.Bundle -- `
  bundle-source.json `
  fixtures/keys/p1-test-private.pem `
  knowledge-bundle.zip
Copy-Item knowledge-bundle.zip fixtures/bundles/clio-knowledge-v1/valid.zip
```

The manifest declares library `com.creatio.clio` at sequence 3, following the implicit v0 sequence
1 generation and the initial guidance-only v1 sequence 2 generation. It carries exact namespaced
item routes, logical topics and roles, signed legacy URI aliases, and the signed reference-example
catalog. Consumers trust only `p1-test-public.pem`. The committed
private key is test material, not a production publishing credential.

The older `fixtures/bundles/esq-v0/valid.zip` remains immutable compatibility evidence for the
legacy implicit-library contract and is not the current distribution payload.

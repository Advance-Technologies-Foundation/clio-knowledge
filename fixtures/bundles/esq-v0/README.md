# ESQ v0 conformance bundle

`valid.zip` is generated from the frozen compiled-Clio oracle with the disposable P1 test key:

```powershell
dotnet run --project automation/Clio.Knowledge.Bundle -- `
  fixtures/esq-v0-bundle-source.json `
  fixtures/keys/p1-test-private.pem `
  fixtures/bundles/esq-v0/valid.zip
```

Consumers trust only `p1-test-public.pem`. The committed private key is test material, not a
production publishing credential.

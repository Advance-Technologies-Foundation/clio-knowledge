# ESQ v0 conformance bundle

This fixture is retained as immutable compatibility evidence for consumers that support the legacy
implicit `com.creatio.clio` source. The canonical builder and distribution package now use v1; do
not regenerate this archive with the v1-only builder.

`valid.zip` was generated from `fixtures/esq-v0-bundle-source.json` and the disposable P1 test key
by the historical v0 builder. Its detached signature and bytes are intentionally frozen.

Consumers trust only `p1-test-public.pem`. The committed private key is test material, not a
production publishing credential.

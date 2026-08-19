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

## 2026-08-06 18:30 – DataService UPSERT guidance
Context: Added canonical guidance for Creatio DataService `UpdateQuery.IsUpsert`.
Decision: Kept UPSERT as its own focused guide and registered it in the bundle catalog and routing map; dedicated UpdateQuery and InsertQuery cross-references remain future work.
Discovery: `IsUpsert` is a query-then-update-or-insert path, not an atomic database merge. Creatio Core revision `e0d0f98b80c8fd26e305804c7cb3242b76baf072` establishes the request contract and zero-match insert branch.
Files: guidance/mcp/guides/backend/dataservice-upsert.md, guidance/mcp/guides/routing.md, bundle-source.json, automation/Clio.Knowledge.Bundle.Tests/PublishedGenerationTests.cs, distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj
Impact: Agents can discover the UPSERT safety rules by the stable `dataservice-upsert` item and route.

## 2026-08-06 20:30 – Entity listener changed-column guard
Context: A Forester lab exposed that the canonical entity-listener guide lacked a supported way to prevent a listener from repeating its own same-entity calculated-field update.
Decision: Make `configuration-entity-event-listener` the sole owner of the changed-column / self-triggering rule and add the `Entity.GetChangedColumnValues()` guard pattern. Bump the direct-source library generation.
Discovery: Core yields values with `IsChanged` through `GetChangedColumnValues()` in before and after hooks. The after-event changed collection also contained `ModifiedOn` and `ModifiedById`, so audit values must not be used as business triggers.
Files: guidance/mcp/guides/composable-app/configuration-entity-event-listener.md, bundle-source.json
Impact: agents can now distinguish a business-input update from their own output write and avoid unnecessary recursive listener work.


## 2026-08-10 23:25 - ENG-92706 process-modeling guidance: full sendEmail configuration
Context: extends the ENG-95064 body-only sendEmail guidance (this branch is based on feature/ENG-95064-process-modeling-send-email) to the full email block the server now supports.
Decision: process-modeling.md "What you can build today" sendEmail bullet rewritten - full email block (mode/sender/subject/body/to-cc-bcc source triple/importance/ignoreErrors/performer), auto-vs-manual rules, append-only setElement recipients, describe hasBody; catalog note updated. bundle-source.json -> libraryVersion 1.13.10 / sequence 25 + item description updated; csproj Version bumped.
Discovery: PublishedContentDigest could NOT be regenerated on this machine - the bundle automation targets net10.0 and only SDK 8.0.423 is installed; the constant is marked with a TODO and the test failure message supplies the correct digest on any .NET 10 machine/CI. SUPERSEDED 2026-08-14 - and the stale sentence outlived its truth long enough for a reviewer to find it sitting next to a real digest, which is the actual lesson here. A .NET 10 SDK (10.0.400) was installed locally and PublishedGenerationTests was RUN: 56/56, including PublishedContent_ShouldMatchDeclaredGeneration. The digest is regenerated and confirmed by the repo's own gate at every bump since, and the constant now carries that provenance rather than a TODO. Running it also caught what no hand-computation could: the pack test fails when libraryVersion moves without the csproj transport <Version>, so FOUR values move together, not two.
Files: guidance/mcp/guides/processes/process-modeling.md, bundle-source.json, distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj, automation/Clio.Knowledge.Bundle.Tests/PublishedGenerationTests.cs
Impact: get-guidance name=process-modeling will teach AI callers the full sendEmail contract once published. Publishing no longer waits on a digest regeneration - it is done and verified locally; what remains is confirming the required check is green on the PR head.


## 2026-08-19 – ENG-95429 mobile operation-shape and button-placement rules
Context: clio#1086 shipped two mobile page validators, and all three MCP tools plus both command docs route the agent to `get-guidance name=mobile-page-modification` — an article that taught neither rule. An agent following it could author a body clio now rejects.
Decision: teach both rules in the mobile guide. OPERATION SHAPE sits next to BODY FORMAT (it is a property of every body, not of the Scaffold); BUTTON PLACEMENT got its own heading in the Scaffold neighbourhood. Both carry an outcome table, an enforcement-surface caveat, and an explicit list of what is NOT claimed. Pinned by `MobileOperationShapeRuleTests`, following the `ElementPlacementRuleTests` precedent.
Discovery: an insert whose `parentName` does not resolve is NOT dropped. `FindInsertItemParent` returns the root `_sourceObject`, so the element is inserted at the ROOT of viewConfig and persists there; `Insert` returns false, and the retry pass drops only the queue ENTRY (the parent is still missing), never the element. The earlier "silently dropped by the differ" wording was wrong in this guide AND is still wrong in clio's own warning text (`SchemaValidationService.cs:863`) and in the pre-existing `path` paragraph here — both need a follow-up.
Discovery: only `libraryVersion` moves on a content change. The 2026-08-10 entry's "FOUR values move together" is SUPERSEDED — the csproj transport version is regex-derived from bundle-source.json and the digest pin no longer exists; a single bump passes all 79 tests.
Discovery: `validate-page` does not reach `ValidateRunProcessButtonStructure` on a mobile body, so an example body can be validate-green and still be refused by `update-page`. Any button example in guidance must carry `params.processName` and `params.processRunType` or it teaches a false green.
Files: guidance/mcp/guides/platform/mobile/page-modification.md, guidance/mcp/guides/processes/run-process-button.md, bundle-source.json, automation/Clio.Knowledge.Bundle.Tests/MobileOperationShapeRuleTests.cs
Impact: the two rules are now reachable from the entry point that the reported scenario actually takes, and a regression in their wording fails a test instead of silently re-recording the content digest.

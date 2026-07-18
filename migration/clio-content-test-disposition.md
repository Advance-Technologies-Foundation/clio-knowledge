# Clio guidance-content test disposition

This inventory classifies tests at Clio commit
`baa34546589413aa898429051d1702442bbd2dd2`. MIG6 must perform the actual Clio changes on
`krylov/knowledge-bundle-poc`; MIG0 changes no Clio source.

The governing split is deterministic:

- exact wording, headings, snippets, per-article URI/text pairs, length limits, and real article
  snapshots move to clio-knowledge or are deleted after byte-oracle coverage replaces them;
- download, verification, compatibility, activation, typed failure, stable lookup, and generic
  resource-read behavior stay in Clio and use synthetic fixtures;
- executable prompts and tool behavior that merely point to a stable guide ID remain Clio behavior.

## Entire files whose assertions move out of Clio

| Clio test file | Disposition | Replacement owner |
|---|---|---|
| `clio.tests/Command/McpServer/CoreRulesGuidanceResourceTests.cs` | Remove after the safety article is migrated; it asserts a sentence in the real article. | MIG-SAFETY |
| `clio.tests/Command/McpServer/IdentityAssertionGuidanceTests.cs` | Remove real article/URI assertions. | MIG5 |
| `clio.tests/Command/McpServer/IntegrationTestingGuidanceResourceTests.cs` | Remove wording assertions. | MIG4 |
| `clio.tests/Command/McpServer/McpGuidanceResourceTests.cs` | Remove the real-resource and catalog-content suite; its 43 tests are covered by per-slice byte equality and the complete MIG0 inventory. | MIG1-MIG5, MIG-SAFETY, MIG-ESQ |
| `clio.tests/Command/McpServer/ProcessModelingGuidanceResourceTests.cs` | Remove its 15 wording/catalog tests. | MIG5 |
| `clio.tests/Command/McpServer/RunProcessButtonGuidanceTests.cs` | Remove the real article assertion. | MIG5 |
| `clio.tests/Command/McpServer/ThemingGuidanceResourceTests.cs` | Remove its three wording assertions. | MIG3 |

## Mixed unit-test files that must be split

### `clio.tests/Command/McpServer/GuidanceGetToolTests.cs`

Remove the per-article return tests and the `PageModification` 15 KB content-limit assertion.
Retain or rewrite against synthetic bundle entries only:

- stable tool name and argument contract;
- case-insensitive stable-ID lookup;
- legacy alias handling;
- typed missing-name and unknown-guide results;
- available-ID enumeration;
- visibility/lookup mechanics for a gated synthetic entry, if feature gates remain part of the
  external catalog contract.

### `clio.tests/Command/McpServer/McpGuidanceForcingTests.cs`

Move router character/ASCII limits, core-rule wording, routing-name extraction, guide-body
cross-reference, and analytics/business-rule trigger wording to clio-knowledge. Keep the response
`Note` serialization and command-result behavior tests. Keep only the minimal Clio-instruction test
that proves executable bootstrap/safety behavior without embedding the routing table or article
phrasing.

### `clio.tests/Command/McpServer/ProfileLanguageGuidanceTests.cs`

Move the `CoreRulesGuide` and `GetGuide` article assertions to clio-knowledge. Keep the four prompt
tests because those prompts remain executable Clio behavior rather than external Markdown content.

## MCP end-to-end tests

### `clio.mcp.e2e/GuidanceGetToolE2ETests.cs`

Remove every real per-article scenario. Retain one small synthetic mechanics surface that proves:

- `get-guidance` is discoverable;
- a stable ID returns an active article;
- unknown, unavailable, and incompatible states have typed results;
- no article wording is asserted.

The baseline file contains 28 tests: one discovery test can remain in mechanics form; the other 27
are real-article or real-catalog scenarios and must be deleted or replaced by the synthetic surface.

### `clio.mcp.e2e/McpGuidanceResourceE2ETests.cs`

Replace the 17 real-resource scenarios with one generic synthetic list/read test that proves stable
`docs://` routing and typed failure behavior. Do not preserve real headings, snippets, reference
payloads, or topic phrases in Clio fixtures.

## Tests that remain Clio-owned

The following nearby tests are not content snapshots and remain in Clio:

- tool/prompt descriptions that route agents to stable guide IDs, provided they do not duplicate
  article rules;
- `WorkspaceTemplateGuidanceDriftTests` checks for resident-or-bridged executable tool names and
  its scanner's synthetic inputs;
- tool behavior tests where an error/result carries a stable guide ID;
- bundle discovery, signature/digest/schema/compatibility rejection, atomic activation,
  last-known-good retention, and typed active/not-found/unavailable tests added by the external
  delivery branch.

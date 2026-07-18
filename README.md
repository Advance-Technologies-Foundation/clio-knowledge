# Clio Knowledge

Clio Knowledge is the canonical source repository for independently published guidance, safety advisories, capability definitions, and the catalog of vetted reference implementations used by [Clio](https://github.com/Advance-Technologies-Foundation/clio).

The repository separates knowledge content from Clio's executable delivery mechanics. Clio owns MCP tools, lazy bundle download, verification, caching, and compatibility selection. This repository owns the content those tools deliver.

The intended result is that correcting an article, publishing a safety advisory, or registering a reference implementation does not require a Clio binary release.

## Status

Content migration is active. The first canonical articles live under `guidance/mcp/guides/` and the
root `bundle-source.json` builds them into the v0 signed bundle.

The remote publication, production signing, and automatic Clio update path remain under review for
the Monday architecture decision. Do not treat content as released Clio guidance until it is included
in a versioned, validated, published knowledge bundle.

## Responsibilities

Clio Knowledge is intended to contain:

- concise, prescriptive guidance consumed through Clio MCP;
- safety advisories and known-dangerous patterns;
- stable capability and knowledge-pattern identifiers;
- metadata for independently maintained reference implementations;
- compatibility and evidence relationships;
- schemas and automation for validating and publishing knowledge artifacts.

It is not intended to contain:

- Clio executable source code;
- complete reference implementation workspaces;
- arbitrary repositories discovered from the internet;
- secrets, customer-specific content, or environment credentials;
- hard runtime enforcement that belongs in Clio itself.

## Repository layout

| Path | Purpose |
|---|---|
| [`guidance/`](guidance/README.md) | Canonical human-readable guidance articles. |
| [`advisories/`](advisories/README.md) | High-visibility safety rules, limitations, and urgent corrections. |
| [`capabilities/`](capabilities/README.md) | Controlled identifiers for features, patterns, and architectural choices. |
| [`catalog/`](catalog/README.md) | Trusted metadata pointing to independently versioned reference repositories. |
| [`schemas/`](schemas/README.md) | Machine-readable contracts for articles, advisories, and catalog entries. |
| [`automation/`](automation/README.md) | Validation, deterministic packaging, and publication. |
| [`bundle-source.json`](bundle-source.json) | Current stable ID, URI, compatibility, and canonical source mapping. |

Complete examples remain in independent repositories. This repository records their immutable source revision, compatibility, validation evidence, and relationship to guidance.

## Design principles

1. **One delivery contract.** Published knowledge content and conformance fixtures use the same bundle format. Clio contains no embedded knowledge content.
2. **Stable identifiers.** Article, capability, advisory, and example IDs remain stable while their content evolves.
3. **Immutable publication.** Clio consumes versioned artifacts, never mutable content from a default branch.
4. **Evidence over assertion.** Prescriptive behavioral claims identify the source, test, lab, or version boundary that supports them.
5. **Clear authority.** Canonical guidance, tested reference patterns, observed implementations, and experimental ideas are labeled distinctly.
6. **Safe failure.** An invalid or incompatible bundle must never replace Clio's active verified guidance.
7. **Independent examples.** Every reference implementation remains directly downloadable, testable, and deployable on its own.
8. **No combinatorial portfolio.** Examples declare their primary use case and supporting decisions without requiring every possible technology combination.

## Publication model

The source files in this repository will eventually produce independently versioned artifacts such as:

```text
clio-guidance-2026.07.18.1.zip
clio-reference-catalog-2026.07.18.1.json
```

Clio will download the newest compatible trusted artifact, verify it, cache it locally, and serve its articles through stable MCP resource URIs and `get-guidance` names.

The ESQ family is the first real migration slice. Additional guidance families will move from Clio
incrementally while stable MCP names and `docs://` URIs remain unchanged.

## Contributing

Start with [CONTRIBUTING.md](CONTRIBUTING.md). All coding agents must also follow [AGENTS.md](AGENTS.md).

Discussion and design feedback belong in [Clio discussion #924](https://github.com/Advance-Technologies-Foundation/clio/discussions/924) while the architecture remains experimental.

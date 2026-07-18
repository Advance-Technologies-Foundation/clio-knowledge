# Clio Knowledge

Clio Knowledge is an experimental home for independently published guidance, safety advisories, capability definitions, and the catalog of vetted reference implementations used by [Clio](https://github.com/Advance-Technologies-Foundation/clio).

The experiment separates knowledge content from Clio's executable delivery mechanics. Clio should own MCP tools, lazy bundle download, verification, caching, and compatibility selection. This repository should own the content those tools deliver.

The intended result is that correcting an article, publishing a safety advisory, or registering a reference implementation does not require a Clio binary release.

## Status

This repository is an experiment supporting [Clio discussion #924](https://github.com/Advance-Technologies-Foundation/clio/discussions/924). Its schemas, publication format, and governance model are not stable yet.

Do not treat content in this repository as released Clio guidance until it is included in a versioned, validated knowledge bundle.

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
| [`automation/`](automation/README.md) | Validation, packaging, publication, and knowledge-gap analysis. |

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

## Envisioned publication model

The source files in this repository will eventually produce independently versioned artifacts such as:

```text
clio-guidance-2026.07.18.1.zip
clio-reference-catalog-2026.07.18.1.json
```

Clio will download the newest compatible trusted artifact, verify it, cache it locally, and serve its articles through stable MCP resource URIs and `get-guidance` names.

The initial proof of concept will establish the format and delivery contract before attempting to migrate every existing article.

## Contributing

Start with [CONTRIBUTING.md](CONTRIBUTING.md). All coding agents must also follow [AGENTS.md](AGENTS.md).

Discussion and design feedback belong in [Clio discussion #924](https://github.com/Advance-Technologies-Foundation/clio/discussions/924) while the architecture remains experimental.

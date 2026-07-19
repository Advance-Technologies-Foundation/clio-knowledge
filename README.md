# Clio Knowledge

Clio Knowledge is the canonical source repository for independently published guidance, safety advisories, capability definitions, and the catalog of vetted reference implementations used by [Clio](https://github.com/Advance-Technologies-Foundation/clio).

The repository separates knowledge content from Clio's executable delivery mechanics. Clio owns MCP tools, explicit bundle installation and update, verification, caching, and compatibility selection. This repository owns the content those tools deliver.

The intended result is that correcting an article, publishing a safety advisory, or registering a reference implementation does not require a Clio binary release.

## Status

Content migration is active. The first canonical articles live under `guidance/mcp/guides/` and the
root `bundle-source.json` builds them into the v1 library-scoped signed bundle.

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
| [`bundle-source.json`](bundle-source.json) | Canonical library, generation, item/topic, URI, compatibility, and source mapping. |
| [`knowledge-bundle.zip`](knowledge-bundle.zip) | Ready signed v1 artifact consumed directly by Git transport without executing repository code. |

Complete examples remain in independent repositories. This repository records their immutable source revision, compatibility, validation evidence, and relationship to guidance.

## Design principles

1. **One delivery contract.** Published knowledge content and conformance fixtures use the same bundle format. Clio contains no embedded knowledge content.
2. **Stable identifiers.** Article, capability, advisory, and example IDs remain stable while their content evolves.
3. **Immutable activation.** A source may follow a Git branch, but Clio serves only a verified bundle generation with a signed monotonic sequence and recorded resolved commit.
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

Clio will obtain compatible trusted artifacts from independently configured transports, verify and
cache each library generation, and serve articles through exact namespaced resource URIs and
logical topic resolution. The canonical identity is `(libraryId, sequence, bundleDigest)`; NuGet
versions, Git branches, tags, and commits describe retrieval and provenance rather than identity.

The ESQ family is the first real migration slice. Additional guidance families will move from Clio
incrementally. Its canonical routes are
`docs://knowledge/com.creatio.clio/<item-id>`. Signed `legacyUris` preserve the former
`docs://mcp/guides/...` routes during the POC transition without making them canonical v1 identity.

## Multi-source bundle identity

Every v1 bundle declares a reverse-DNS `libraryId`, publisher-facing `libraryVersion`, and positive
monotonic `sequence`. Every item declares a stable `itemId`, cross-library `topicId`, `role`, and
exact route. The builder derives the expected route from the library and item IDs and rejects a
mismatch, duplicate item, duplicate route or alias, and duplicate topic/role pair within a library.

Logical selection policy belongs to Clio and operator configuration. A library publishes identity,
content, compatibility, and provenance; it does not publish its own priority or override rights.

Git transport reads the ready repository-root `knowledge-bundle.zip`. It must never run the builder
or another repository script while retrieving knowledge. Publication automation generates the root
artifact once; the committed v1 conformance fixture and NuGet payload retain those exact bytes.

## Contributing

Start with [CONTRIBUTING.md](CONTRIBUTING.md). All coding agents must also follow [AGENTS.md](AGENTS.md).

Discussion and design feedback belong in [Clio discussion #924](https://github.com/Advance-Technologies-Foundation/clio/discussions/924) while the architecture remains experimental.

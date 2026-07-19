# Guidance

This directory contains canonical, human-readable articles served through stable Clio MCP guidance IDs and resource URIs.

Guidance states concise rules, workflows, applicability, and safety boundaries. Complete implementations belong in independent reference repositories and are linked through catalog metadata.

## Authoring layout

The filesystem preserves the human-readable migration layout beneath `guidance/`:

```text
guidance/
  mcp/
    guides/
      esq.md
      esq-filter-parsing.md
      esq-filters/
        index.md
        backend.md
        frontend.md
```

An `index.md` file owns the topic router for a directory and sibling files own focused items. The
layout intentionally remains readable and does not encode publisher selection rules.

Developers edit these files. Files under `fixtures/oracles/` are immutable migration snapshots used
to prove that the first external bundle preserves the previous Clio output; they are not the
authoring source.

`bundle-source.json` at the repository root maps stable item IDs, logical topic IDs, roles, exact
`docs://knowledge/com.creatio.clio/<item-id>` routes, and transitional legacy routes to these
canonical files. The bundle builder must consume the canonical `guidance/` paths, never the oracle
snapshots. Renaming a file does not rename its item or topic identity.

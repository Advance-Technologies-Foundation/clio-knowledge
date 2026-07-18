# Guidance

This directory contains canonical, human-readable articles served through stable Clio MCP guidance IDs and resource URIs.

Guidance states concise rules, workflows, applicability, and safety boundaries. Complete implementations belong in independent reference repositories and are linked through catalog metadata.

## Authoring layout

The filesystem mirrors the stable `docs://` hierarchy beneath `guidance/`:

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

An `index.md` file owns the resource at the directory URI. Sibling files own child resources. For
example, `esq-filters/index.md` is `docs://mcp/guides/esq-filters`, while `backend.md` is
`docs://mcp/guides/esq-filters/backend`.

Developers edit these files. Files under `fixtures/oracles/` are immutable migration snapshots used
to prove that the first external bundle preserves the previous Clio output; they are not the
authoring source.

`bundle-source.json` at the repository root maps stable guidance IDs and URIs to these canonical
files. The bundle builder must consume the canonical `guidance/` paths, never the oracle snapshots.

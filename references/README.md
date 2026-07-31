# Supporting references

This directory contains focused supporting articles linked from primary guidance. References keep
large examples, checklists, and implementation patterns out of the primary routing surface while
remaining directly readable through canonical `docs://knowledge/<library-id>/<item-id>` resources.

Every reference must:

- be declared in `bundle-source.json` with role `reference`;
- use a stable item and topic identity;
- retain any former `docs://mcp/references/...` route in `legacyUris`;
- be linked from at least one primary guide through its canonical namespaced URI;
- remain concise enough that an agent can load it only when the parent guide requires it.

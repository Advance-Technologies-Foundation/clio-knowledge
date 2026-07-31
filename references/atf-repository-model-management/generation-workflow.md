# Generation Workflow

## Preferred Tool Path

Use `add-item-model` through the CLIO MCP server when models are missing or clearly incomplete for the task.

Do not generate by default for trivial read-only tasks such as:

- a console app that prints a few scalar fields
- a report that needs one lookup navigation
- a report that needs one reverse relation such as `Contact -> Account by Owner`

In those cases, hand-author the minimal models unless the exact relation shape is unclear.

Fallback only if MCP is unavailable:

- `clio add-item model`

## Folder Rules

The `folder` argument for `add-item-model` must be:

- a local absolute path
- a staging folder dedicated to generated output

Do not use:

- relative paths
- UNC or network paths
- mixed source folders that already contain hand-maintained models

The output folder does not need to exist before calling the MCP tool; the MCP layer is expected to create it when possible.

## Broad Generation Warning

Treat `add-item-model` as potentially broad generation. It may generate the full environment model set rather than only the schema you currently care about.

After generation:

- verify the requested schemas are present
- check whether extra models were produced
- keep, copy, or reference only the models the task actually needs
- if the generated set reveals the exact relation/property name you needed, copy only that minimal shape into the hand-maintained model instead of adopting the full generated file

If generation is broader than expected, report that explicitly.

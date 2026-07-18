# ATF.Repository Model Management

## Use This Skill When

- A task needs one or more ATF.Repository models and names the required schemas or members.
- You need to decide whether to reuse existing models, extend them, or generate new ones.
- Generated models may already exist, but you need to verify whether they are suitable for the current task.
- A task depends on lookup or detail navigation, and you need to identify the minimal model graph that supports it.

## Core Rules

- Reuse existing project models before generating new ones.
- For trivial read-only tasks, prefer hand-authoring a minimal model instead of generating a broad model set.
- Generate models with `add-item-model` (`mcp__clio__add-item-model`) when source models are missing or clearly incomplete for the task.
- If the MCP server is unavailable, use `clio add-item model` in the terminal as the fallback.
- Generate into a local absolute staging folder, not a mixed source folder.
- Treat generation as potentially broad, verify the requested schemas after generation, and keep only the models the task actually needs.
- Treat generated code as project code: review names, relations, and collisions before using it in feature code.
- If a generated staging set already exists, mine it for the exact relation/property names you need before deciding to generate again.

## Standard Workflow

1. Identify the exact schemas and members the task needs.
2. Inspect the current project for existing matching models.
3. Reuse or extend existing models if they already satisfy the task.
4. If the task is a small report or utility and only needs a few scalar fields or one obvious relation, hand-author the minimal model set instead of generating.
5. If models are still missing, call `add-item-model` with a local absolute output folder dedicated to generated output.
6. Review the generated result and confirm the requested models are present.
7. Check for side effects such as duplicate models, unexpectedly broad output, type mismatches, or naming collisions.
8. Keep or reference only the models needed for the task, and avoid broad namespace imports when they create ambiguity.

## References

Read only what you need:

- `references/model-graph-selection.md`: choosing the minimal schema and member graph for the task
- `references/generation-workflow.md`: `add-item-model` usage, folder constraints, fallback behavior, and broad-generation checks
- `references/collision-and-cleanup.md`: duplicate models, naming collisions, namespace ambiguity, and cleanup decisions

## What To Report Back

- which models were reused, extended, or generated
- the output folder used for generation
- whether generation produced only the requested models or a broader set
- any collisions, duplication, or cleanup decisions that affected implementation
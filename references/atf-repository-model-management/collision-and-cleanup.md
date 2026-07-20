# Collision And Cleanup

## What To Check

Review generated or reused models for:

- duplicate or overlapping models
- type mismatches
- broad namespace imports that create ambiguity
- naming collisions such as `File`, `Task`, `Environment`, or other common framework names

## Collision Handling

When a generated model name collides with framework or project types:

- prefer aliases or explicit qualification
- prefer narrower `using` directives
- avoid renaming unrelated code just to accommodate one generated type

## Cleanup Rule

If generation produces more files than the task needs, do not treat that as proof that every generated model should now be used.

Keep the smallest set that supports the task and report:

- what was kept
- what was ignored or left staged
- any cleanup or qualification decisions that affected implementation

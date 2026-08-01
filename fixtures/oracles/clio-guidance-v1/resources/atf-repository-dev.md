# ATF.Repository Development

## Use This Skill When

- You need to read data with ATF.Repository model queries.
- You need to create, update, or delete records through repository models.
- You have `UserConnection` available directly, or you can register repository services in the package container.
- You need to access Creatio remotely through `RemoteDataProvider`.
- You need to write repository code against existing project models.

## Non-Negotiable Rules

- Every repository model must inherit `ATF.Repository.BaseModel`.
- Every repository model must be marked with `[Schema("<EntitySchemaName>")]`.
- Every mapped scalar column must use `[SchemaProperty("<ColumnName>")]`.
- Property CLR types must match the connected Creatio column types.
- Do not use `Nullable<T>` or `T?` in repository models. Nullable model properties can cause runtime errors.
- Navigation properties for lookups and details must be `virtual`.
- Prefer `IAppDataContext` as the primary application-facing API because it creates tracked models, exposes `Models<T>()`, and applies changes through `Save()`.
- If a task creates, extends, or selects repository models, apply `atf-repository-model-management` first instead of handcrafting models here.
- Keep repository creation close to composition root boundaries. Business logic should depend on injected abstractions, not construct remote providers ad hoc inside deep domain code.
- Match the provider pattern to the host application. Package code, tests, and external utilities may construct or inject providers differently, but feature logic should still depend on repository abstractions rather than on ad hoc transport details.
- Stay within the supported `Models<T>()` query surface documented in `references/query-patterns.md`.
- `RemoteDataProvider` is not `IDisposable`. Do not wrap it in `using` or `await using`.
- For small remote read-only reports, prefer tiny hand-authored models over broad generated model sets when the task only needs a few scalar fields or one clearly defined reverse relation.
- When the feature reads child rows from an already loaded master model, prefer a `DetailProperty` relation over issuing a second top-level query and grouping client-side.

## Standard Workflow

1. Confirm whether existing models are sufficient; if not, apply `atf-repository-model-management`.
2. Create or review the model class and its attributes.
3. Decide whether the feature needs local Creatio access (`LocalDataProvider`) or remote access (`RemoteDataProvider`).
4. Create or inject `IDataProvider` at the infrastructure boundary.
5. Create `IAppDataContext` from the current `IDataProvider` close to the operation that uses it.
6. Query with `Models<T>()` or load a single tracked model.
7. If the task is a console/reporting utility, keep models minimal and favor relation traversal from the loaded model graph.
8. Apply create, update, or delete changes to models.
9. Persist changes with `Save()` and check the result.

## References

Read only what you need:

- `references/package-and-version.md`: package acquisition rules, version checks, and validating the current `IAppDataContext` API surface
- `references/models-and-relations.md`: minimal model pattern plus direct, reverse, and many-to-many relation mapping
- `references/provider-and-context.md`: local provider creation, remote provider creation, DI guidance, and standalone console-app patterns
- `references/query-patterns.md`: querying with `Models<T>()`, allowed LINQ surface, filtering, ordering, paging, and loading tracked models
- `references/write-operations.md`: insert, update, delete, and save-result handling

Suggested loading order:

1. `references/models-and-relations.md` when you need to create or review model mappings.
2. `references/provider-and-context.md` when you need local DI setup or remote utility/console setup.
3. `references/query-patterns.md` or `references/write-operations.md` only for the operation style you are implementing.
4. `references/package-and-version.md` only when package acquisition or API-surface validation is relevant.

## Review Checklist

1. Model inherits `BaseModel` and has `[Schema(...)]`.
2. Every mapped property uses the correct repository attribute.
3. Lookup/detail navigation properties are `virtual`.
4. Scalar property types match schema column types.
5. Nullable model property types are not used.
6. `IDataProvider` creation matches the execution context: local vs remote.
7. DI registers `IDataProvider`, not `IAppDataContext`.
8. `IAppDataContext` is created from `IDataProvider` close to the usage boundary and reused through the operation.
9. Query code stays within the allowed `Models<T>()` LINQ surface.
10. Reverse relations use `DetailProperty(nameof(DetailModel.ForeignKeyProperty))`, not the raw schema column name.
11. Console/reporting code does not issue avoidable second top-level queries for data already reachable through a modeled relation.
12. Insert/update/delete logic calls `Save()` and checks the result when `IAppDataContext` is used.
13. Generated model namespaces or type names do not create unresolved ambiguity with framework or project types.

## What To Report Back

- Files changed, with one-line reason per file
- Which models or relations were added or updated
- How `IDataProvider` and `IAppDataContext` were created or injected
- Which query or CRUD pattern was used
- Tests added or updated, or the exact reason tests were not changed
- Build/test commands run, or the precise blocker if they were not run
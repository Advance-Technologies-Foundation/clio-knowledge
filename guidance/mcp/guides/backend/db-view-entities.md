# SQL views as Creatio entities

Owns packaging a SQL view with matching entity metadata. `IsDBView` tells Creatio not to generate a table; it neither creates the SQL view nor converts an existing table. It is independent of `IsVirtual` (`virtual-entities`).

## Before creating anything

- Discover the live `create-entity-schema` contract. It MUST expose `is-db-view`; do not send an unknown argument and assume it was applied. Read `get-entity-schema-properties` after creation and require `db-view: true` and the intended `virtual` value. An older server tested in this lab silently ignored the unknown argument and created a table.
- Choose the package/schema name using `app-modeling`. Inspect the target database catalog for a same-name object. If a table exists, stop and plan a data-preserving migration; do not drop it or assume flipping metadata will convert it.
- Decide the parent and columns before writing SQL. Every inherited column used by the entity must have the matching physical name/type in the view, including lookup `...Id` columns. `Id` must be a stable, unique, non-null identifier for each result row. Do not generate a new random Id on every query.
- SQL is provider-specific. The example below is PostgreSQL, not portable MSSQL SQL.

## Author and verify

1. Through `clio-run`, invoke `create-entity-schema` with `environment-name`, `package-name`, `schema-name`, `title-localizations` including `en-US`, the intended parent/columns, and `is-db-view: true` **on the first creation**. Verify the saved properties. Do not start with an ordinary entity and toggle it later.
2. Create a package SQL script with `create-sql-schema`, then save its body with `update-sql-schema`. Use the same owning package, and ensure the native script settings run it during the intended installation stage for the correct DB engine. `get-sql-schema` reads the artifact; creation alone is not execution.
3. Provision referenced tables before the view. For a deterministic BaseLookup-derived test entity, this complete PostgreSQL view supplies its inherited fields:

```sql
CREATE OR REPLACE VIEW "UsrExampleView" AS
SELECT '8fc052bf-c8eb-421c-a346-abcafce61638'::uuid AS "Id",
       'Example row'::varchar(250) AS "Name",
       ''::varchar(250) AS "Description",
       CURRENT_TIMESTAMP::timestamp AS "CreatedOn",
       NULL::uuid AS "CreatedById",
       CURRENT_TIMESTAMP::timestamp AS "ModifiedOn",
       NULL::uuid AS "ModifiedById",
       0::integer AS "ProcessListeners";
```

This constant-row fixture proves the plumbing; replace its SELECT with the required business query. Confirm the effective inherited column contract for the target version instead of copying this column list onto an unrelated parent.

4. Run `install-sql-schema` once for the named script, then inspect the database catalog and query the expected row through `odata-read` or the application's ordinary entity-query path. For PostgreSQL, `pg_class.relkind = 'v'` distinguishes a view from a table (`'r'`). A successful schema save or script-save result alone is insufficient.
5. After new entity publication, follow `core-rules` for asynchronous OData registration. A temporarily unavailable route is not evidence that the SQL is wrong; wait for publication to finish, check readiness/logs, and retry the read without replaying writes.

## Ship the result

- Capture the entity metadata **and** the package SQL script. For DB-first changes, `app-modeling` owns capture-before-push ordering. Keep dependencies on source tables/packages explicit.
- Export and install the package into a clean validation target, then repeat metadata, physical-object and entity-query checks there. A manually created view on the authoring database is not deployment proof.
- `pull-pkg` may return a ZIP containing the package `.gz`. Inspect the archive format and install the contained package; renaming the outer ZIP to `.gz` does not convert it.
- Make scripts deliberate about repeated installation and upgrades. `CREATE OR REPLACE VIEW` is useful for a compatible definition change, but it does not make arbitrary column removal/type changes safe. Do not hide errors by dropping/recreating an existing object with data/dependencies you have not inspected.
- Treat the view as read-only unless its write semantics are separately designed and tested. The metadata flag is not a promise that create/update/delete operations work.

## Evidence boundary

Validated through Clio MCP on two disposable Creatio 10.1.585 / .NET 8 / PostgreSQL instances using the Clio implementation at `e53009498`: saved `db-view: true`, a package SQL script, the PostgreSQL view and deterministic OData row, and package transfer. The older-contract silent-ignore failure was reproduced separately. MSSQL view DDL, schema-changing view upgrades and writable views are not validated here. [Acceptance evidence](https://github.com/Advance-Technologies-Foundation/clio-knowledge/issues/209).

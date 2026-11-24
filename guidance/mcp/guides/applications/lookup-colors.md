clio MCP lookup-colors guide

Use this guide when a user asks to make the values of a lookup colorful, to color-code lookup options, or to add a color column to a lookup. It owns the rules for coloring lookup VALUES: which column carries the color, the palette, the order colors are picked in, the write tool, and the read-back. `app-modeling-details` owns which schema tools accept the `Color` column type; `data-bindings` owns the binding rules the write inherits (editable package, `binding-name`, transfer to another environment).

Status and applicability
- Candidate guidance. The mechanism is read from Creatio and clio source and was partly observed on a live environment (Evidence). Not yet verified on a stand: writing a color through a replacing object of an out-of-the-box lookup, the colors appearing on a Freedom UI page, and the colors reaching another environment with the package (Open checks).
- Freedom UI pages only. Coloring in Classic UI is out of scope.
- Write tools: `upsert-data-binding-row-db` and `create-data-binding-db` write a `Color` column in clio 8.1.0.118 or later.

Core rule
- Freedom UI colors a lookup value from the lookup object's color column: the column the object's Color setting points to (Object Designer, object settings, "Color": "Column that stores the color for object records"; the platform's `PrimaryColorColumn`). The platform reads it only when that column's type is `Color` (dataValueType 18); a text column holding hex codes colors nothing, whatever its name. Pages need no change.
- The column NAME does not decide anything. Read on Creatio 10.2.965: the color column of `ContactType` is a column named `Color` of type `Color`; `CaseStatus` has a text column named `Color`, and its color column is `StatusColor` of type `Color`; `ActivityCategory` has only a text `Color` column and no color column.
- clio has no tool that reads or sets the object's Color setting. Find the candidate column by type (Preferred workflow step 1); when you add one, the setting is a manual step (step 2).

Preferred workflow
1. Find the lookup object and its color column. Read `get-entity-schema-properties` WITHOUT `package-name` (the merged view) and list the columns whose `type` is `Color`.
   - Exactly one: use it as the color column. If the colors do not show on a page after step 5, the object's Color setting may not point to it: ask the user to check it (step 2).
   - More than one: ask the user which one the lookup shows; do not guess.
   - None (a text column named `Color` counts as none): go to step 2.
2. No color column: MUST ask the user for the target package and get explicit confirmation before any schema change. Without confirmation, change nothing and stop. Then:
   - Add the column in that package through a replacing object: `create-entity-schema` with `extend-parent: true` and `parent-schema-name` equal to `schema-name`; when the package already holds the replacement, add the column with `modify-entity-schema-column`. Column type `Color`; the column name follows the schema name prefix rule of those tools (for example `UsrColor`), and it cannot reuse the name of an existing column such as a text `Color`.
   - Tell the user the one step clio cannot do: in the Object Designer, open the lookup object's settings and set its Color field to the new column, then save. Without it the platform has no color column to read, so the written values stay uncolored on pages (read from source; not yet observed on a page).
3. Choose the colors.
   - The user named colors: use them. Each MUST match `#RRGGBB` (`#` and six hex digits); write it in upper case. clio passes any text through as a color, so `red` or `#FFF` would be stored as is: refuse it and ask the user for a hex code instead.
   - The user named none: pick ONLY from the palette below, never an invented color. Take the 16 recommended colors in their order; when there are more than 16 values, continue with the auxiliary colors in their order. Order the values first: in the order the user gave them, otherwise by `Name` ascending. The same request then gives the same colors on a rerun.
   - Change only the values the user asked to color. Keep a value's existing color unless the user asks to recolor it; skip such a value in the palette order rather than shifting the others.
4. Write each value with `upsert-data-binding-row-db` (clio 8.1.0.118 or later), one call per value, with `values` = `{"Id": "<record Id>", "<color column>": "#RRGGBB"}`. The `Id` makes it an update of the existing record; `null` clears a color; a number or an object is refused before the first write. Read the exact arguments with `get-tool-contract` first.
   - Find the record `Id`s with `dataforge-find-lookups` or `execute-esq`.
   - The write also binds each record into a package data binding, so it needs an editable package and an existing binding (`package-name`, `binding-name`): follow `data-bindings`, which owns finding the binding the package already ships for this object and creating one with `create-data-binding-db` when there is none. Its column-projection rule applies: a binding carries only the columns written into it, so for a record that no package ships yet (a value added in the UI), also pass `Name` in `values`, or the record arrives on the next environment without it. Use the package confirmed in step 2; when no schema change was needed, ask the user which package carries the colors unless the task names it.
   - MUST NOT use `odata-update` or `odata-create`: a `Color` column is absent from OData `$metadata`, and the write is refused. MUST NOT use `execute-dataservice-batch`: its value types have no `Color` type.
5. Verify and report. Read the values back with `execute-esq` (a `Color` column is readable there), compare each with what you wrote, and show the user a table of value -> color. When step 2 added the column, repeat the Object Designer step in the report.

Palette
Copied verbatim from Creatio Academy, "Manage lookup values" (Creatio 10), read on 2026-11-24. The page sets the recommended selection order as left to right, top to bottom. This is not the section icon palette; do not mix the two.
- Recommended, in order: `#EDF9CC`, `#CCF4DC`, `#D2EED0`, `#FFEECD`, `#FFE7CC`, `#FED6E5`, `#FFE0D6`, `#FFD9D0`, `#F1E5F5`, `#E4DAFC`, `#CCE9FB`, `#CCDEFC`, `#CDEEFD`, `#DCD9F3`, `#CCEAE7`, `#CCF2ED`.
- Auxiliary, in order: `#A6DE00`, `#00C853`, `#22AC14`, `#FFAC07`, `#FF8800`, `#F9307F`, `#FF6534`, `#FF4013`, `#B87CCF`, `#7848EE`, `#0091EA`, `#0058EF`, `#03A9F4`, `#4F43C2`, `#009688`, `#00BFA5`.

Open checks
Not verified on a stand; state them as unverified when they matter to the user:
- Writing a color with `upsert-data-binding-row-db` into a replacing object of an out-of-the-box lookup, end to end.
- The written colors appearing on a Freedom UI page after the Object Designer step, and whether the designer sets the Color field by itself when a `Color` column is added there.
- Which out-of-the-box lookups already have a color column beyond the three named in Core rule, on other products and versions.
- The colors reaching another environment with the package: the binding columns default to `IsForceUpdate: false` (`data-bindings`), so a record that already exists on the target may keep its old color; and whether a record written with only `Id` and the color column arrives usable.

Evidence
- Color comes from the color column, and only when its type is `Color`: Creatio Core `Terrasoft.Core/Entities/EntitySchemaQuery.cs` (`CreatePrimaryColorExpression`), `Terrasoft.Nui.ServiceModel/Extensions/QueryExtension.cs` and `Terrasoft.Core.ServiceModelContract/EntitySerializer.cs` at Creatio Core commit `bc3c7c625562f155244a09b03f1e189c208b5832` (2026-11-24). Freedom UI reads `primaryColorAttributeName` of the lookup schema (`libs/studio-enterprise/ui/components/src/lib/select/preprocessors/base-select-preprocessor.ts` at Creatio UI commit `b2684c6793f26ba082c3b1db764217ac89641167`); the Object Designer edits it as the object's Color setting (`ObjectSettings.ColorCaption`, same commit).
- `ContactType`, `CaseStatus` and `ActivityCategory`: column types read with `get-entity-schema-properties` (clio 8.1.0.149) and the color column read from `RuntimeEntitySchemaRequest` on Creatio 10.2.965 (.NET), 2026-11-24, read only.
- clio has no tool that sets the color column: the entity schema tools carry it only to clear it when its column is removed (`clio/Command/EntitySchemaDesigner/RemoteEntitySchemaColumnManager.cs`, clio commit `a35201cdbd3ad82f6f1855fae0252e36ffa0defe`).
- `Color` writes in the DB-first binding tools: `clio/Command/DataBindingDbCommand.cs` at clio tag 8.1.0.118 (commit `fc4bc5fc09f4bc14f2e50a4f8344e79a36098c16`) passes a string verbatim, keeps `null`, and refuses any other value before the first write; absent in 8.1.0.117. The end-to-end test `DataBindingDb_Should_RoundTrip_Color_Column_And_Keep_Color_DataValueType` (`clio.mcp.e2e/DataBindingDbColorSchemaE2ETests.cs`, same tag) writes a `UsrColor` column with `create-data-binding-db`, updates it with `upsert-data-binding-row-db` and reads it back.
- OData refusal: `clio/Command/McpServer/Tools/ODataUpdateTool.cs` and `ODataFieldValidation.cs` at clio commit `a35201cdbd3ad82f6f1855fae0252e36ffa0defe`. Batch value types: `clio/Command/McpServer/Tools/DataServiceBatchTool.cs`, same commit.
- Palette and adding the column through a replacing object: Creatio Academy, "Manage lookup values" (Creatio 10), read on 2026-11-24.

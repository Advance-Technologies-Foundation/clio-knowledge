# One process element with separate operations and pages

This guide owns design-time selection between separate native user tasks behind one toolbox entry. Read `process-custom-elements` first for the artifact, error-output, registration, and testing contracts. Use the immutable `atf.creatio.custom-process-element-reference` catalog entry for the working Arithmetic family.

Verified on Creatio 10.1.585.0 / .NET 8 / PostgreSQL with Clio 8.1.0.130, and 10.1.784.0 / .NET Framework 4.8 / SQL Server 2025 Express with Clio 8.1.0.131. The Classic designer override below is version-sensitive internal behavior, not a documented cross-version API. Recheck it in the browser after an upgrade.

## Structure

| Operation | Native task | Separate child page | Inputs |
|---|---|---|---|
| Add | UsrAddNumbersUserTask | UsrAddNumbersPropertiesPage | FirstAddend, SecondAddend |
| Subtract | UsrSubtractNumbersUserTask | UsrSubtractNumbersPropertiesPage | Minuend, Subtrahend |
| Multiply | UsrMultiplyNumbersUserTask | UsrMultiplyNumbersPropertiesPage | Multiplicand, Multiplier |
| Divide | UsrDivideNumbersUserTask | UsrDivideNumbersPropertiesPage | Dividend, Divisor |

Each task resolves its own typed DI handler returning ErrorOr<decimal>. Each has Result, IsError, and unlimited ErrorMessage outputs. Division rejects zero; the handlers report decimal overflow as an error. Do not replace these separate implementations with a runtime operation switch when demonstrating this pattern.

Each child page inherits a shared Classic selector page. The shared parent owns the operation collection, selection, confirmation, and replacement guard; the child owns its operation-specific MAPPING fields and help. Point each task's parameter-page association at its own child schema.

When the live contract exposes the scaffold primitives described in `process-custom-elements`, create each task's initial parameter page independently. Customize the generated pages to inherit the shared selector schema while preserving their own parameter bindings. The primitive creates the basic native process-page parent; it does not create the family selector, retarget inheritance, or hide alternate toolbox entries for you.

## Native replacement

1. Initialize the selector's `Terrasoft.Collection` during element loading and select the operation corresponding to the actual task schema. Preserve the parent's element-loading callback.
2. Map selector values to a fixed allowlist of registered user-task diagram types. The reference uses lower-camel names such as `usrDivideNumbersUserTask`.
3. Before replacement, ask `parentSchema.canRemoveElements([element.name])`. If existing output references prevent removal, explain the constraint and retain the current task.
4. Confirm that changing operation replaces the task and clears its configured inputs. Cancel must keep the current task and values. Restore the displayed selection in the dialog's dismissal callback; restoring it earlier was overwritten by the pending control event.
5. Publish native `ChangeElementType` with the current element UId as id, type `userTask`, and the chosen userTaskType. Declare `Terrasoft.MessageMode.BROADCAST`, matching the designer subscriber. PTP silently failed to replace the element in the lab.
6. Let the native designer replace the element, reconnect flows, and load the selected task's child page. Save and reopen to prove that the backend schema and page selection persisted.

This selects the implementation at design time. It is not runtime branching. Configure the operation before downstream output mappings. Different parameter identities are not interchangeable; do not silently copy input mappings into unrelated operation parameters.

## One toolbox entry

Register all four schemas so the designer can instantiate each replacement. Keep them active/nondeprecated. Marking alternate tasks inactive is not the verified hiding mechanism.

The reference loads `src/js/arithmetic-designer-bootstrap.js` through package `Files/descriptor.json`. Its small Ext override extends `Terrasoft.Designers.ProcessSchemaDesignerViewModelNew.getExcludedMenuItems`, preserving `this.callParent(arguments)` and adding the three alternate task names. Only Add's Arithmetic caption remains in the toolbox; the selector can still choose every registered task.

This follows the native file-processing family's exclusion mechanism. Do not edit platform source or replace the entire designer. If an upgraded designer changes this method, stop claiming a single-entry family until the override is adapted and all browser checks pass. Backend execution alone cannot verify this behavior.

## Acceptance

Require one toolbox entry and four distinct page schemas. Exercise every operation, cancel a switch, accept a switch, check input reset, save/reopen, verify flow reconnection, and try changing an element whose outputs are already referenced. The guard must retain both the backend task and visible selector value.

Keep a selector demonstration without downstream mappings separate from execution fixtures. The reference ships four execution processes with five process parameters each, mapping both inputs and all three outputs. Regenerate their typed models after changing process parameters. Its separate selector process intentionally has no downstream mappings.

The reference passed 13 arithmetic unit cases and five arithmetic live cases, plus the text example's cases. Native Float2 generated decimal server properties but Single properties in the tested Clio process models; exact binary-representable test values do not prove arbitrary decimal precision across that boundary.

Non-FSM PostgreSQL and MSSQL installation verified packaged registration and all ten live cases. MSSQL also verified forced registration re-execution, one Arithmetic toolbox entry, and switching among four distinct pages. Save/reopen and output-reference guards retain PostgreSQL-only evidence. See the pinned reference's installation record for exact artifacts and boundaries. DCM, older designers, localization, and asynchronous execution remain unverified. No additional page-creation or registration MCP primitive is implemented by this reference.

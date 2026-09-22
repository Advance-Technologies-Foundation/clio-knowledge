# Entity access through system operations

Owns the connection between entity CRUD permissions and system operations. `administration` owns user/role membership and operation grants; `record-permissions` owns record rights. These are separate layers and can all affect the same request.

## Semantics before configuration

- `AdministratedByOperations` enables entity operation-right checks. It is not itself a mapping to a particular system operation.
- `SysEntityRightByAdminOperation` maps an entity schema to a system operation and selects the rights it contributes: `CanRead`, `CanAppend`, `CanEdit`, `CanDelete`.
- Despite its name, the mapping's SQL column `SysSchemaId` stores the schema **UId**, not the environment-local `SysSchema.Id`. The native provider joins it to `SysSchema.UId`. Resolve the actual schema identity; never substitute a schema table row Id.
- These mappings **add** rights when the user can execute the operation. They are not an exclusive gate or a deny that overrides every other grant. Existing object grants, inherited roles, broad administrator operations, record rights and licenses still matter. Denying one operation does not prove the user has lost access from every other source.
- The native feature/configuration boundary `AddAdminOperationRightsToEntityRights` must permit operation-derived rights. Inspect the target's effective configuration when mapping/grant readback does not match behavior; do not silently change a shared environment's security configuration.

## Configure a deliberate mapping

1. Read `administration` and inspect the target entity's effective properties with `get-entity-schema-properties`. Record existing operation/record/column security flags and intended CRUD scope. For whether the object is actually administered and which roles hold grants, read `get-object-rights` rather than the design-time flag. Use a dedicated test entity and non-admin identity for validation.
2. Enable the entity's operation administration. `set-object-rights` owns this flag: granting the first role turns operation permissions ON, and revoking the last remaining grant turns them back OFF — read `get-guidance name=object-rights` for that layer. The checked Clio `set-entity-schema-properties` contract does **not** expose the flag: do not invent an argument. Where that tool is unavailable, use the native object designer and publish; or, for an authorized developer workflow, `clio call-service` can read the complete `EntitySchemaDesignerService.svc/GetSchemaDesignItem` result, preserve its schema body, set `administratedByOperations`, and submit that complete schema to `SaveSchema`, followed by native publication. Do not send a sparse guessed schema or overwrite unrelated metadata. Read the flag back afterward.
3. Define the system operation and the entity mapping using supported Creatio administration or a reviewed package installation script. For a package script, preserve stable operation/mapping IDs, reference the schema UId, choose only the intended CRUD bits, and declare install dependencies/order. Read `create-sql-schema`, `update-sql-schema` and `install-sql-schema` contracts before using that native package path.
4. Do not write protected system objects with `odata-create` or DB-first data-binding tools as an assumed workaround. In the lab, creating a SysAdminOperation through `create-data-binding-db` was refused by object security even for Supervisor. Use the administration or package-install path above.
5. Through `manage-access`, grant the operation to the intended identity or role and inspect the stored grant. Keep environment-local user accounts and memberships out of reusable package data. `administration` owns grant/deny ordering and caches.
6. Refresh native security caches after changing the entity-to-operation mapping. In the lab, a restart alone left the old mapping cached; clearing the exclusively owned disposable instance's Redis database made the new mapping effective. This is evidence of cache scope, **not** permission to flush a shared Redis database. In a shared environment, arrange a supported, target-specific cache refresh with its operator before testing.

## Prove actual access

- Use a non-admin account with normal role membership and the required access to the chosen API. For OData, `CanUseODataService` is a separate prerequisite. An OData-level refusal does not test entity authorization.
- Seed a known row as an authorized administrator. Query it as the test identity with the operation granted, denied, and granted again, keeping every other access source constant.
- Require the expected row in the allowed cases and its absence/refusal in the denied case. Do not assume denial is always HTTP 403: on the validated OData read, entity denial returned a successful empty collection.
- Verify requested write operations separately. A passing read test does not prove append/edit/delete behavior, and an administrator query cannot prove that the restricted user is blocked.
- Test interactions with existing object and record grants if the requirement is exclusive access. If another grant still allows the action, resolve the policy deliberately rather than adding more deny records blindly.

## Evidence boundary

Disposable Creatio 10.1.585 / .NET 8 / PostgreSQL: native schema save/publish and MCP property readback; package SQL installation of operation and mapping; MCP user/membership and grant management; non-admin OData read produced one known row, then zero under deny, then the same row under re-grant. Mapping-cache persistence and the independent OData gate were observed. Native `DbEntityRightByAdminOperationProvider` and `DBSecurityEngine.AddAdminOperationRights` explain the UId join and additive semantics. Append/edit/delete, every license combination and older Creatio versions were not tested. [Acceptance evidence](https://github.com/Advance-Technologies-Foundation/clio-knowledge/issues/210).

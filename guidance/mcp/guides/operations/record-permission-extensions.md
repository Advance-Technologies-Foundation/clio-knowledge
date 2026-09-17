clio MCP record-permission-extensions guide

Implement runtime record predicates only after reading `record-permissions`. This article owns the API,
activation, combination and failure contract. `record-rights` owns stored-grant commands;
`process-access-rights` owns process grant changes; `administration` owns identity/operation management.

APPLICABILITY: live-verified on Creatio 10.1.585.0, .NET 8, PostgreSQL, single node, using clio 8.1.0.130.
This is a verified compatibility point, NOT an earliest-supported-version claim. Before targeting another
version, inspect its installed public API and rerun the reference's denied AND allowed cases. .NET Framework,
other databases, portal users, browser UI, web-farm propagation and alternate feature settings are unverified.

API AND IMPLEMENTATION

Reference the installed `Terrasoft.Core` assemblies through the normal clio workspace. Required namespaces:
`Terrasoft.Core`, `Terrasoft.Core.DB`, `Terrasoft.Core.Factories`,
`Terrasoft.Core.RecordPermissionExtension`; condition grouping also uses `Terrasoft.Common`.
The exact contracts are:

```csharp
// Members of IReadRecordPermissionExtension, IEditRecordPermissionExtension,
// and IDeleteRecordPermissionExtension respectively:
QueryCondition GetReadRightCondition(UserConnection userConnection, PermissionExtensionContext context);
QueryCondition GetEditRightCondition(UserConnection userConnection, PermissionExtensionContext context);
QueryCondition GetDeleteRightCondition(UserConnection userConnection, PermissionExtensionContext context);
```

Return a SQL predicate, not a boolean decision, ESQ filter collection or stored grant. `context.SchemaName`
is the object name OR query alias; qualify columns with it. The context does not supply a record ID.
Use the supplied connection's current user/contact; do not capture a user in static or cached state.
Implementations are cached by the provider. Keep them stateless and construct conditions per call.
Parameterize values. Prefer SQL predicates/EXISTS over per-row queries or external calls; index actual
join/filter columns and measure the query on representative volume. The reference is not a load benchmark.
For backend ESQ construction outside this QueryCondition contract, read `esq-filters-backend`.

Give each interface a named binding and keep the configured name identical:

```csharp
[DefaultBinding(typeof(IReadRecordPermissionExtension), Name = "DocumentPolicy")]
[DefaultBinding(typeof(IEditRecordPermissionExtension), Name = "DocumentPolicy")]
[DefaultBinding(typeof(IDeleteRecordPermissionExtension), Name = "DocumentPolicy")]
public sealed class DocumentPolicy : IReadRecordPermissionExtension,
    IEditRecordPermissionExtension, IDeleteRecordPermissionExtension {
    // Implement all three exact members above. See the pinned reference for complete code.
}
```

Do not copy that incomplete skeleton as a deployable class. The reference's `DocumentPermissions` is the
complete minimal implementation: read = owner OR published; edit = owner AND not locked;
delete = owner AND not locked AND not published. Its real-user tests validate each interface separately.
A typical leaf uses `Column.SourceColumn(context.SchemaName, "UsrOwnerId")` and
`Column.Parameter(userConnection.CurrentUser.ContactId)` in a `QueryCondition.IsEqual` expression.
Do not hard-code the root alias or insert a contact ID as SQL text.

ACTIVATION

1. Use an authorized, exclusively owned test environment first. Confirm the installed feature
   `UseRecordPermissionExtensions` is enabled; a class merely compiling does not prove activation.
   Confirm the object is published with `AdministratedByRecords=true` and has its rights table.
   Changing only a cached schema object's property is insufficient. Inherited record-right configurations
   require separate validation; the reference tests an object with its own record rights.
2. Build/deploy the implementation and load its assembly using the current workspace mode. Read live
   `core-rules`, `routing` and relevant tool contracts. In linked FSM, build local C# then restart;
   do not overwrite it with compilation from a stale database package.
3. Create one native `SysRecordPermissionExtension` row per object/operation. Set `SchemaNames` to the
   exact object name, `ExtensionName` to the named binding, `IsActive=true`, and an intentional `Position`.
   Resolve `RecordPermissionOperationTypeId` from `EntitySchemaRecRightOperation.Value`:
   Read=0, Edit=1, Delete=2. Resolve `ExtensionCombinationModeId` from
   `SysExtensionCombinationMode.Code`: And=1, Or=2, InsteadOf=3. These fields are lookup GUIDs, not the
   enum integers. Resolve native rows; do not invent environment-specific lookup IDs.
4. Save through the native entity lifecycle with an authorized administrator. The listener requires
   `CanManageSolution` OR `CanManageAdministration` and invalidates the provider cache after writes.
   Raw SQL skips that lifecycle. The reference's fixed-schema Configure fixture demonstrates this path;
   it is lab infrastructure, not a general production permission-management service.
5. Verify persisted registrations and execute allowed/denied operations under the ordinary user's own
   session. Verify edit values and deletion independently afterward. A successful Configure response,
   class binding, process completion or stored-grant listing alone does not establish enforcement.

The provider selects ONE active row per object and operation: lowest Position, then newest CreatedOn.
It does not combine every configured extension. Avoid competing rows and ties. A misspelled binding on
that selected row is not repaired by trying the next row. Maintain separate registrations for all three
interfaces; a read-only sample does not establish edit/delete policy.

COMBINATION AND FAILURE

For an ordinary caller at the tested record-security boundary, let S be the stored record permission
and P the applicable extension predicate:

| Native mode | Record-level result |
|---|---|
| And (1) | S AND P: adds a restriction |
| Or (2) | S OR P: adds an alternative grant |
| InsteadOf (3) | P: replaces the stored record decision |

These modes do not replace authentication, licensing or separately enforced object-operation rights.
The reference grants all three stored operations or none and verifies the matrix through Entity and
DataService. It does not certify conflicting stored Deny/Delegate grants or operation-denial precedence.

CRITICAL: an inactive/unresolved binding or a returned `null` condition means no applicable extension;
the tested behavior falls back to stored rights, including in InsteadOf mode. `null` is NOT denial.
Return an explicit false predicate when business policy denies access. Missing deployment must fail your
acceptance checks, not silently pass because some stored grant allowed the operation. Do not intentionally
throw exceptions to implement denial; exception behavior is an error path, not a permission policy.
Feature-disabled behavior is source-derived and must be tested on the target before relying on it.

BOUNDARIES AND VERIFICATION

- `Entity.UseAdminRights=true` enables permission checking; false bypasses it. Raw `Select`/`Update` SQL
  and privileged users are not ordinary-user acceptance paths. Keep lab setup/readback elevation separate
  from the operation being tested.
- The current-user rights path evaluates the distinct read/edit/delete interfaces. An overload asking about
  another user may use stored rights only; use separate authenticated sessions instead.
- In the inspected version, a generic record-condition builder selects the read extension for some
  edit/delete filters. The tested native Entity/DataService paths also check operation-specific rights.
  Their passing tests do not certify custom bulk SQL, alternate feature paths or every integration endpoint.
- Extension predicates can also affect native delegation bits. A rule allowing edit is not proof that the
  caller cannot delegate access. If delegation matters, add a test through the actual delegation endpoint.
- A record-context change must change results without relying on a cached per-user decision. Include
  ownership/publication/relationship changes and both permitted and denied outcomes in acceptance.
- If JSON parsing fails on an HTML response, first verify login, password expiry/change requirements and
  deployment. Do not interpret it as a permission denial or dump authentication response bodies into logs.

Evidence: catalog example `atf.creatio.record-permissions-reference` pins complete implementation,
NUnit unit/E2E fixtures and `docs/verification.md`. Backend verification traced
`CachedPermissionExtensionsProvider`, `DBSecurityEngine`, `Entity.CheckEditingRights` and
`ChangeAdminRightsUserTask`; proprietary source is not required to consume or run this reference.
The process element's target selection/grant application caveats remain owned by `process-access-rights`.

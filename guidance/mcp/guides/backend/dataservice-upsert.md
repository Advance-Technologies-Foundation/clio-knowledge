clio MCP DataService UPSERT guide

Use this guide when authoring or reviewing a Creatio DataService `UpdateQuery` with `IsUpsert: true`.

Status and applicability
- This is verified canonical guidance for the long-standing Creatio DataService `UpdateQuery.IsUpsert` feature. The behavior was verified against Creatio Core revision `e0d0f98b80c8fd26e305804c7cb3242b76baf072` (2026-06-25): `Terrasoft.Nui.ServiceModel/ServiceBase/BaseCrudDataService.cs` and `DataContract/DataContract.cs`.
- It applies to standard DataService `UpdateQuery` requests with `IsUpsert: true`. Validate integration-specific response serialization and database constraint behavior on the target deployment.

Core rule
- `IsUpsert: true` is an entity-level query-then-update-or-insert flow. It is NOT a database `MERGE`, an atomic compare-and-insert, or an exactly-once guarantee.
- Creatio first loads every entity matching `filters`. One or more matches means it applies `columnValues` and saves EVERY match. Zero matches means it creates a normal insert from `rootSchemaName` and `columnValues` only.

Required request discipline
- Confirm the exact target schema, match-column path, type, required columns, and defaults in the target environment before writing.
- Use a stable key intended to match zero or one record, preferably an immutable external identifier or primary key. Do not use a mutable display value such as `Name` as an identity key.
- Put every value required for an insert in `columnValues`, including the match key. Filters decide what to find; they are not copied to the inserted entity.
- Supply required fields that do not have reliable schema defaults. A supplied primary key is used; otherwise the entity save pipeline generates one.
- POST to `/0/DataService/json/SyncReply/UpdateQuery` and preserve `"IsUpsert": true`. The non-synchronous `Reply/UpdateQuery` route uses the same CRUD pipeline; use the response mode required by the caller. Check both the HTTP result and the DataService `success` / `responseStatus` envelope.

Safe request shape
```json
{
  "rootSchemaName": "Contact",
  "filters": {
    "items": {
      "ExternalIdEquals": {
        "filterType": 1,
        "comparisonType": 3,
        "isEnabled": true,
        "leftExpression": { "expressionType": 0, "columnPath": "UsrExternalId" },
        "rightExpression": {
          "expressionType": 2,
          "parameter": { "dataValueType": 1, "value": "CRM-000042" }
        }
      }
    },
    "logicalOperation": 0,
    "isEnabled": true,
    "filterType": 6,
    "rootSchemaName": "Contact"
  },
  "columnValues": {
    "items": {
      "UsrExternalId": {
        "expressionType": 2,
        "parameter": { "dataValueType": 1, "value": "CRM-000042" }
      },
      "Name": {
        "expressionType": 2,
        "parameter": { "dataValueType": 1, "value": "Example Contact" }
      }
    }
  },
  "IsUpsert": true
}
```

Concurrency and response handling
- A broad filter materializes every match and saves entities one by one. Treat any possible multi-match as unsafe until the filter is narrowed or the bulk effect is explicitly intended, load-tested, and reviewed.
- Two callers can both observe zero matches and then both insert. Protect a business key with a UNIQUE database constraint or unique schema index; a non-unique index is insufficient. Define duplicate-key handling: re-read, then retry as an update when appropriate.
- On update, `rowsAffected` is the number of matched entities saved. On insert, the response reports `rowsAffected: 1` and normally includes the new `id`. Do not use `queryId` to distinguish the branches.
- Entity validation, defaults, event handlers, and save-time business logic still run. Validate side effects on the target Creatio version and deployment.

Verification checklist
- Prove the filter produces zero or one record in a safe environment.
- Test existing-match update, zero-match insert, and an accidental multi-match case.
- Test the duplicate-key race or documented conflict handling when the caller needs idempotency.
- Read the structured response before retrying; do not infer success from an HTTP response alone.

Evidence
- Creatio Core revision `e0d0f98b80c8fd26e305804c7cb3242b76baf072` (2026-06-25), `Terrasoft.Nui.ServiceModel/ServiceBase/BaseCrudDataService.cs` verifies the zero-match `IsUpsert` insert branch; `Terrasoft.Nui.ServiceModel/DataContract/DataContract.cs` declares the request contract. The exact result serialization and constraint behavior remain deployment-specific and must be verified when they affect an integration contract.

clio MCP guide: customizing the Creatio.ai add-in for Microsoft 365

Scope and compatibility
- This guide owns configuring the CRM panel of the Creatio.ai add-in for Microsoft 365 (Outlook and Teams) from an agent. It covers which Creatio objects the panel offers, where each one appears, which fields each panel surface shows, and shipping that configuration in a package. It does not cover installing the add-in, its OAuth setup, or its users' access to the add-in.
- The add-in's own editor is the "Customize add-in" pane. It saves through a package web service, `AIEmbeddedFieldConfigService.SaveConfiguration`, that validates the whole change and applies it in one transaction. clio MCP cannot call a configuration web service, so this guide writes the two configuration tables directly. A direct write is NOT validated: the rules in "Rules the editor enforces" are yours to keep. If you break one, nothing rejects the write and every add-in user gets the broken panel.
- Applicability: Creatio .NET Framework environments with the `CrtAIEmbedded` package. Confirm the build supports this configuration (see "Discover once") instead of assuming a package version.
- Tools: `get-entity-schema-properties`, `get-entity-schema-column-properties`, `execute-esq`, `inspect-access`, and `execute-dataservice-batch` (clio 8.1.0.131 or later). Data bindings use the tools `data-bindings` names. Call long-tail tools through `clio-run`. Read `core-rules` for transport and verification rules, `esq` for queries, `data-bindings` for binding files, and `administration` for operation grants.
- EXPERIMENTAL: the rules below are derived from the `CrtAIEmbedded` source. The write recipe has not yet been executed end to end through clio. Read every write back, and verify the result in the add-in (see "Verify").

Discover once
1. `get-entity-schema-properties` on `CrmPanelObject` and `CrmPanelField`. Both must exist. `CrmPanelObject` must have `TabPosition`, `SearchPosition`, `CreatePosition`, `HideFromTabs`, `HideFromSearch`, `HideFromCreateMenu`, `SkipCreateContact`, `SkipCreateAccount` and `SkipSaveEmail`. `CrmPanelField` must have `EntityPagePosition`, `ListItemPosition` and `CreatePopupPosition`. If any is missing, the environment predates this configuration model: stop and report it. Do not create the columns.
2. Confirm authorization with `inspect-access`. Customizing is meant for users who can execute `CanCustomizeEmbeddedAddin` and `CanManageLookups`. Both tables are administered by operations, so writing needs Add, Edit and Delete on both; the package grants these to System administrators. If the identity lacks them, report it. Do not grant operations or object rights to make a write pass; `administration` and `entity-operation-access` own that decision.
3. Read the current configuration with `execute-esq`:
   - every `CrmPanelObject` row: `Id`, `Name`, `ObjectCode`, `CreatedOn` and the placement columns;
   - every `CrmPanelField` row: `Id`, `EntitySchema`, `EntityColumn`, `Position`, the three `ShowOn*` flags and the three surface positions.

   Keep this snapshot. Every write below is planned against it.
4. The feature `OutlookAddin.EmbeddedCustomizationEnabled` gates only whether the pane offers its Customize entry. Direct writes do not need it. Do not change it, and never delete its `Feature` row: the pane's save locks on that row and refuses to run without it.

Model
- `CrmPanelObject` is a lookup. One row registers one Creatio object for the panel.
  - `ObjectCode` is the entity schema name and the identity.
  - `Name` is the caption the panel's tabs use.
  - `TabPosition`, `SearchPosition` and `CreatePosition` order the object among the contact/account related-record tabs, the search tabs and the Create menu.
  - `Hide*` takes the object off that list. `Skip*` turns off a background action when a record is created from an email.
  - Everything else the panel needs (title column, Account/Contact/Activity links, form page, icon) is derived from the platform at read time. Do not store it anywhere.
- `CrmPanelField` holds one row per object column the panel shows.
  - `EntitySchema` is a lookup to the `CrmPanelObject` row, and `EntityColumn` is the column name.
  - The surfaces are the record page (`ShowOnEntityPage`, `EntityPagePosition`), the list item (`ShowOnCompactListItem`, `ListItemPosition`) and the Create popup (`ShowOnCreatePopup`, `CreatePopupPosition`).
  - A surface position of 0 falls back to the shared `Position`.
  - A row with all three flags off is a hidden field, not an unconfigured one. Once an object has any field rows, a surface shows only the columns flagged for it.
- Neither index is unique. One object can have several `CrmPanelObject` rows, and one column several `CrmPanelField` rows; the add-in reads each as one. Any change MUST reach every copy: a flag cleared on one copy stays visible through another.
- Neither table raises entity events, so nothing else validates or cascades a direct write.

Rules the editor enforces (MUST hold after your write)
- Objects:
  - Contact, Account, Lead, Opportunity and Case ship with the product. They MUST NOT be removed; use the `Hide*` flags to take them off a list.
  - A new object MUST be an entity with a section: a `SysModule` row whose `SysModuleEntity` points at the schema UId.
  - It MUST NOT be `Activity`, `Employee`, `MktgActivity`, `Touch`, `FormSubmit` or `OmniChat`, nor have a name starting with `Sys` or `Vw`.
  - It MUST have a primary display column and a lookup column to `Account` or to `Contact`. Where several columns point at that schema, one must be named `Account` or `Contact`.
  - It MUST be readable by the users meant to see it, and MUST NOT already be registered.
- `Name` is at most 250 characters. If omitted, use the `ObjectCode`. Keep a registered object's existing `Name`.
- Positions: use positive values. The editor writes multiples of 10 in list order; to put a new object at the end of a list, use the current maximum plus 10. A placed object has a position above 0 and its `Hide*` flag off.
- Fields:
  - Name only columns the object has.
  - No column twice per object.
  - The primary display column MUST NOT be on the record page or the list item (the panel draws it as the title), but it may be on the Create popup.
  - Every surface you flag needs its position of at least 1; an unflagged surface's position is 0.
  - On insert, set `Position` to the first positive position among record page, list item and Create popup.
- Required columns: every column that is required and has no default that fills it MUST be on the Create popup. Otherwise the panel offers a form that cannot be saved. Read requirement and default with `get-entity-schema-column-properties`.
  - These defaults fill the column: a system value, a sequence, and a non-empty constant.
  - A default read from a system setting counts as filling only if the setting is shared (not personal) and has a non-empty value.
  - A default an administrator disabled does not fill.
- Defaults for a newly added object (the editor's choice; follow it unless the user asks otherwise):
  - record page and list item: the object's required columns plus `Owner` and `CreatedOn`, leaving out the title column;
  - Create popup: the required columns plus `Owner`.
- Caps per change: 50 objects, 200 fields per object, 1,000 fields in total.

Write
- Plan the complete change from the snapshot, check it against every rule above, and show it to the user before writing.
- Use `execute-dataservice-batch`, as `core-rules` prescribes. It is not atomic: it continues after a failed item. Write the object row first, read it back, then write its fields, and resolve any failed item before the next step.
- Never insert a row the snapshot already has; update every existing copy instead.

```json
{"command":"execute-dataservice-batch","args":{"environment-name":"<env>","operations":[
 {"operation":"insert","schema-name":"CrmPanelObject","record-id":"<new-object-guid>","values":{
  "ObjectCode":{"data-value-type":1,"value":"UsrProject"},"Name":{"data-value-type":1,"value":"Projects"},
  "TabPosition":{"data-value-type":4,"value":60},"SearchPosition":{"data-value-type":4,"value":60},"CreatePosition":{"data-value-type":4,"value":60}}},
 {"operation":"insert","schema-name":"CrmPanelField","record-id":"<new-field-guid>","values":{
  "EntitySchema":{"data-value-type":10,"value":"<new-object-guid>"},"EntityColumn":{"data-value-type":1,"value":"Owner"},
  "Position":{"data-value-type":4,"value":10},"ShowOnEntityPage":{"data-value-type":12,"value":true},"EntityPagePosition":{"data-value-type":4,"value":10},
  "ShowOnCompactListItem":{"data-value-type":12,"value":true},"ListItemPosition":{"data-value-type":4,"value":10}}}]}}
```

- Reorder or re-flag: update the affected rows, every copy of each. To take a field off the panel, clear all three flags; do not delete the row, because a package reinstall would bring a seeded row back.
- Remove an added object: delete every `CrmPanelField` row of every one of its `CrmPanelObject` rows, then every `CrmPanelObject` row with its `ObjectCode`. Name the rows to the user and get confirmation first, because this cannot be undone.

Verify
- Read both tables back and compare them with the plan, including that no duplicate pair was created.
- The add-in caches the configuration for up to 5 minutes, and only its own save clears that cache. A direct write appears there within 5 minutes, or at once after the user presses Refresh in the pane. This timing is read from the add-in source, not measured; do not report the change as visible before then. No compile or restart is needed.
- Every write changes the configuration version, so a pane Customize session already open when you write is answered with a conflict on save. That is by design: tell the user to reopen Customize.
- Verify the panel itself according to `core-rules`' saved browser-verification preference: the added object's tab, its list items, its record page and its Create form.
- An added object's search tab returns records only if the object is indexed in Global Search. Report that dependency; configuring the index is out of scope.

Ship in a package
- A configuration written as above lives only in that environment. To deliver it, bind both tables in the customer's package. `data-bindings` owns the mechanics. The DB-first tools also write the live row, but they have not been verified against these tables' rights.
- Key every row by `Id`, never by (`EntitySchema`, `EntityColumn`). The installer writes lookup columns after all rows exist, so a pair key misses rows and then inserts a duplicate primary key.
- Bind every column the row needs: placement and `Skip*` columns, all three flags and all three surface positions. A column you do not bind arrives empty.
- A field row for a shipped object names that object by the `Id` of its seeded `CrmPanelObject` row. Add the `CrtAIEmbeddedIn*` package that seeds the row to `DependsOn`: `CrtAIEmbeddedInC360` for Contact and Account, `CrtAIEmbeddedInLead`, `CrtAIEmbeddedInOpportunity`, `CrtAIEmbeddedInCaseMgmt`. Depend on `CrtAIEmbedded` for an object your package registers itself.
- Bindings default to `IsForceUpdate: false`, so an upgrade keeps the customer's later edits. Force-update a column only if your package owns it, and advance the descriptor's `ModifiedOnUtc` whenever rows or column policy change.

Evidence and bounds
- Derived from the source of `CrtAIEmbedded` (crt-ai-embedded-app commit 515430137fce076fd8edfd5bc5d81be94efba8fe):
  - `AIEmbeddedFieldConfigService`: the save plan, candidate filter, required-column rule, version token and row writes;
  - the `CrmPanelObject` and `CrmPanelField` schemas and their seed bindings.
- The cache and refresh behavior comes from the add-in back end at creatio-ai-addins commit 9256c24f078e6e83dbf5751030d4cf33e878f687.
- Not run against a live environment through clio. The batch write, the DB-first binding path on these tables, and the reported timings are unverified.
- Unknown: MSSQL versus PostgreSQL differences beyond the non-unique indexes; .NET 8 environments, which the add-in does not support.

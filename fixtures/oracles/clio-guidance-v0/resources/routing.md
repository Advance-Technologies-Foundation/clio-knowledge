clio MCP routing map

Map the task to the guide(s) you MUST read with get-guidance before planning or mutating.
Pick the domain, then the row (get-guidance name=...; an unknown name returns availableGuides).

- Pages (Freedom UI): create/edit -> get-component-info (read resolvedFrom) + name=page-modification
  - page-modification is the entry; after its GATE read the ONE matching sub-guide: name=page-modification-overview (save lifecycle), name=page-modification-field-contract (insert a data-bound field), name=page-modification-containers (parentName / bundle.json), name=page-modification-components (button/handler/viewConfigDiff rules)
  - dashboards (create a dashboard page, or lay out / size / style analytics widgets) -> name=dashboards (routes onward to dashboard-creation / dashboard-design)
  - desktop pages (create/edit a desktop-selector workspace, CentralAreaDesktopTemplate, group Desktop) -> name=desktop-page
  - page business rules (create/change/remove; visibility/required/value) -> name=business-rules
  - bind which page opens for a record / which page adds a record (related pages) -> name=related-page-binding
  - add a button/menu item that runs a business process -> name=run-process-button
- Entities & schemas: create/modify schema, app / schema modeling -> name=app-modeling
  - virtual entity object, IEntityQueryExecutor reads, or EntityEventListener writes -> name=virtual-entities
  - schema designer fails with "GetSchemaDesignItem returned an HTML error page" / package dependencies -> name=package-dependencies
  - entity business rules (create/change/remove) / lookup filtering / dependent fields -> name=business-rules; static filters -> name=business-rule-filters
- Data: raw ESQ queries or filter work -> name=esq AND name=esq-filters
  - esq-filters is the entry router; it selects name=esq-filters-frontend (JavaScript/page JSON/DataService), name=esq-filters-backend (native backend C# construction), or name=esq-filter-parsing (runtime C# interpretation)
  - lookup seeding / data bindings -> name=data-bindings
- Applications, deploy & ops: deploy & provisioning -> name=deploy-lifecycle
  - integration tests / ATF.Repository / Allure / process tests -> name=integration-testing
  - environment inspection (version / db engine / framework / product / license) -> name=describe-environment
  - executing an approved plan -> name=agent-execution
  - identity assertion / Identity Service V3 -> name=identity-assertion
- Theming & branding: brand colours / fonts / custom themes (create, restyle, delete, list, set the default) -> name=theming
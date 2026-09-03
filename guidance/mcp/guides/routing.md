clio MCP routing map

Map the task to the guide(s) you MUST read with get-guidance before planning or mutating.
Pick the domain, then the row (get-guidance name=...; an unknown name returns availableGuides).

- Knowledge feedback: observed behavior contradicts or requires deviation from guidance -> name=knowledge-feedback

- Pages (Freedom UI): create/edit -> get-component-info (read resolvedFrom) + name=page-modification
  - page-modification is the entry; after its GATE read the ONE matching sub-guide: name=page-modification-overview (save lifecycle), name=page-modification-field-contract (insert a data-bound field), name=page-modification-containers (parentName / bundle.json), name=page-modification-components (button/handler/viewConfigDiff rules)
  - MOBILE pages (schemaType 10: `*_MobileListPage` / `*_MobileFormPage`, anything opened in the Mobile
    Designer) -> name=mobile-page-modification FIRST; it overrides the web page rules, and web rules applied
    to a mobile page can leave it unopenable. Analytics widgets on a mobile page are covered there too.
  - convert a web Freedom UI page to a mobile page -> name=freedom-page-web-to-mobile-conversion
  - decode an elementMap[].reason code from that conversion -> name=freedom-page-mobile-reason-codes
  - dashboards (create a dashboard page, lay out / size / style analytics widgets, or set who can access a dashboard) -> name=dashboards (routes onward to dashboard-creation / dashboard-and-home-page-layout / dashboard-design / dashboard-rights)
  - create a home page, or set a workplace's home page (BaseHomePage + SysWorkplace.HomePageUId binding) -> name=home-page
  - desktop pages (create/edit a desktop-selector workspace, CentralAreaDesktopTemplate, group Desktop) -> name=desktop-page
  - page business rules (create/change/remove; visibility/required/value) -> name=business-rules
  - wire a button/menu action to a platform request (crt.*Request: print, close, cancel, ...) -> get-request-info + name=when-to-use-requests
  - add a button/menu item that runs a business process -> name=run-process-button, plus get-process-signature FIRST + get-request-info (crt.RunBusinessProcessRequest)
  - bind which page opens for a record / which page adds a record (related pages) -> name=related-page-binding
  - add/update a NAMED or PREDEFINED filter that a list/section page always applies (e.g. an "Active Requests" list) -> name=page-modification-overview + name=esq-filters-frontend
  - send or receive page messages through WebSockets / `MessageChannelService` -> name=websocket-messaging; add name=page-schema-handlers and name=page-schema-creatio-devkit-common for page-body mechanics
- Business processes (BPMN): build or change a process — elements, flows, parameters, mappings, formulas,
  filters, record signals, and the "Connected to" links of the activity a task creates -> name=process-modeling
  - process-modeling is the ENTRY and owns the build lifecycle (tools, descriptor, what is buildable, the
    recipe, the modify-safety rules, the element catalog). After it, read the ONE matching sub-guide:
  - name the process, its elements, or its parameters (the N1-N10 rules) -> name=process-naming
  - start a process on a record add/modify/delete, read data, modify data, or restrict which records an
    element acts on -> name=process-data-elements
  - process parameters, element-parameter mappings, type compatibility, or a date/time/lookup default
    value -> name=process-parameters
  - the Perform task element — a human step, who performs it, its parameter table -> name=process-perform-task
  - the Send email element — mode, sender, recipients, subject, HTML body macros -> name=process-send-email
  - the "Connected to" links of the activity a task creates, and the R1-R17 connection rules ->
    name=process-activity-connections
  - includes "create a task/activity attached to THIS record": that is a connection, and for a custom entity it
    needs a data-model step first — name=process-activity-connections carries the three-step recipe
  - write or repair C# inside an existing process ScriptTask -> name=process-script-task; add name=esq-filters-backend when the code builds an EntitySchemaQuery
- Entities & schemas: create/modify schema, app / schema modeling -> name=app-modeling
  - resolve a Git conflict in a Creatio package artifact -> name=creatio-three-way-merge
  - virtual entity object, IEntityQueryExecutor reads, or EntityEventListener writes -> name=virtual-entities
  - schema designer fails with "GetSchemaDesignItem returned an HTML error page" / package dependencies -> name=package-dependencies
  - entity business rules (create/change/remove) / lookup filtering / dependent fields -> name=business-rules; static filters -> name=business-rule-filters
- Data: raw ESQ queries or filter work -> name=esq AND name=esq-filters
  - esq-filters is the entry router; it selects name=esq-filters-frontend (JavaScript/page JSON/DataService), name=esq-filters-backend (native backend C# construction), or name=esq-filter-parsing (runtime C# interpretation)
  - DataService UpdateQuery with IsUpsert, update-or-insert, external-key matching, or duplicate-key handling -> name=dataservice-upsert
  - lookup seeding / data bindings -> name=data-bindings
- Email content: read, edit, or copy a marketing email (`BulkEmail`) or message template (`EmailTemplate`), including Beefree `BfEmailTemplate` and legacy `TemplateConfig` variants -> name=email-templates
- Applications, deploy & ops: deploy & provisioning -> name=deploy-lifecycle
  - implement application or session lifecycle hooks with IAppEventListener / AppEventListenerBase -> name=application-listener
  - backend localizable values, schema ownership, culture fallback, or localization tests -> name=localizable-values; for Freedom UI page resources also read name=page-schema-resources
  - create or test a Freedom UI Angular remote-module project with new-ui-project -> name=ui-project
  - integration tests / ATF.Repository / Allure / process tests -> name=integration-testing
  - create, publish, consume, or troubleshoot a custom C# MCP source-code action -> name=custom-mcp-tools; add name=server-to-server-oauth when creating or using OAuth client credentials
  - send a Creatio backend C# message to Freedom UI, bridge a frontend message to the same user's connections, or broadcast a frontend announcement through WebSockets / MessageChannelService -> name=websocket-messaging
  - manage navigation workplaces (create/update/delete a workplace, grant/remove role visibility, add/remove/move sections) -> name=workplaces
  - environment inspection (version / db engine / framework / product / license) -> name=describe-environment
  - executing an approved plan -> name=agent-execution
  - identity assertion / Identity Service V3 -> name=identity-assertion
- Branding & theming: product logos / browser-tab favicon / shell background image -> name=branding
  - brand colours / fonts / custom themes (create, restyle, delete, list, set the default) -> name=theming
- Access rights (record-level): who can read/edit/delete a record, or grant/revoke that access -> name=record-rights; for a DASHBOARD's access rights (and shipping them with the package so they survive a transfer) -> name=dashboard-rights

clio MCP dashboards router

Pick the dashboard guide that matches the task (a dashboard is a page inheriting
`BaseDashboardTemplate`) and read it with get-guidance before planning or mutating:

- CREATE a dashboard page — the `BaseDashboardTemplate` schema and its link-back optional properties
  (`DashboardsEntitySchemaName`, `DashboardsElementName`, `DashboardsClientUnitSchemaUId`),
  including how to retrieve each value -> get-guidance name=dashboard-creation
- LAY OUT / size / group / style the analytical widgets — the 12-column grid, the
  metric-band-then-chart-grid skeleton, per-surface sizes and card themes ->
  get-guidance name=dashboard-and-home-page-layout
- FILTER a dashboard's widgets by its page data (the hidden `DashboardDS` source) ->
  get-guidance name=dashboard-design
- A single widget's runtime payload — get-guidance name=indicator-widget (metrics) or
  name=chart-widget (charts), plus get-component-info for its exact contract
- READ or CHANGE who can access a dashboard (grant/revoke read/edit/delete), and ship those grants
  with the dashboard's package so they survive a transfer -> get-guidance name=dashboard-rights
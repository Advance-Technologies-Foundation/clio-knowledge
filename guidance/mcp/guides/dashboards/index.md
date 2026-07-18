clio MCP dashboards router

Pick the dashboard guide that matches the task (a dashboard is a page inheriting
`BaseDashboardTemplate`) and read it with get-guidance before planning or mutating:

- CREATE a dashboard page — the `BaseDashboardTemplate` schema and its link-back optional properties
  (`DashboardsEntitySchemaName`, `DashboardsElementName`, `DashboardsClientUnitSchemaUId`),
  including how to retrieve each value -> get-guidance name=dashboard-creation
- LAY OUT / size / group / style the analytical widgets on a dashboard — the 12-column grid, the
  metric-band-then-chart-grid skeleton, per-widget-type sizes, the plain-white card style, and the
  hidden `DashboardDS` page data source widgets filter by -> get-guidance name=dashboard-design
- A single widget's runtime payload — get-guidance name=indicator-widget (metrics) or
  name=chart-widget (charts), plus get-component-info for its exact contract
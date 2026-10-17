clio MCP page-creation guide

Canonical flow
- Prefer `list-page-templates -> create-page -> get-page` when adding a brand-new Freedom UI page.
- Use `list-page-templates` to discover valid `template` values in the target environment. The catalog is sourced from `/rest/schema.template.api/templates` and may differ per environment because of platform feature flags (`ShowSidebarTemplate`, `UseListPageV3Template`, `UseMobilePageDesigner`).
- Use `create-page` to create the schema. After success, call `get-page` to verify the page reads back through the canonical page flow.
- Prefer registered clio environments; use `reg-web-app` first if the target environment is not yet registered.

Supported templates (subject to platform feature flags)
- Web (schema-type: web, schemaType=9):
  * `PageWithTabsAndProgressBarTemplate` — Tabbed page with progress bar
  * `PageWithTabsFreedomTemplate` — Tabbed page with left area
  * `PageWithRightAreaAndTabsFreedomTemplate` — Tabbed page with right area
  * `PageWithTopAreaAndTabsFreedomTemplate` — Tabbed page with area on top
  * `BaseMiniPageTemplate` — Mini page
  * `PageWithAreaFreedomTemplate` — Grid page
  * `BaseHomePage` — Home page
  * `BaseDashboardTemplate` — Dashboard
  * `BaseSidebarTemplate` — Sidebar page (feature-flagged)
  * `ListPageV3Template` or `ListPageV2Template` — List page (feature-flagged)
  * `BlankPageTemplate` — Blank page
  * `CentralAreaDesktopTemplate` — Desktop (a desktop-selector workspace; read get-guidance name `desktop-page` FIRST)
- Mobile (schema-type: mobile, schemaType=10):
  * `MobilePageWithTabsFreedomTemplate`
  * `BaseMobilePageTemplate`
  * `BaseMobileListTemplate`
  * `BlankMobilePageTemplate`

Required inputs
- `schema-name`: new schema name. Must start with a letter and contain only letters, digits, or underscores. Prefer the `Usr*` prefix for custom schemas.
- `template`: template name or UId as returned by `list-page-templates`.
- `package-name`: target package name. The package must already exist in the environment.

Optional inputs
- `caption`: human-readable caption. Defaults to `schema-name` when omitted.
- `description`: free-form description.
- `entity-schema-name`: existing entity schema the page is bound to; leave blank for template-pure pages (dashboards, desktops, blank pages, custom pages). From the clio release after 8.1.0.141, a WEB page created with it carries a data source bound to the entity, chosen by the template:
  * a template with no primary data source (tabbed, mini, blank page templates) gets a page-scoped `PDS` that becomes `primaryDataSourceName` — the shape of stock record pages such as `Accounts_FormPage`;
  * a template that names a primary data source gets that one bound;
  * `ListPageV3Template` declares its own `PDS` as a `viewElement` list source; it is bound to the entity and NOT made primary (stock list pages have no primary data source).
  A mobile page, or a template clio could not read, gets no data source: the entity is only recorded in the page dependencies and the response `note` says so — bind it with `update-page`. On an older clio the entity only reaches the dependencies on every page, so the page has no primary data source and fields, `crt.Connections`, `crt.Feed` and the template's `#DataSourceEntityName()#` show nothing until you add `primaryDataSourceName` and `PDS` with `update-page`.

Validation and failure modes
- Unknown template: `create-page` rejects the call with a readable error; always call `list-page-templates` first when unsure.
- Missing package: `create-page` reports the missing package by name.
- Duplicate schema-name: `create-page` refuses to overwrite an existing ClientUnit schema. Pick a new name or delete the existing schema first.
- Missing entity: if `entity-schema-name` is supplied but the entity schema does not exist, the call is rejected before the page is created.
- Post-create verification: `create-page` does not run AI sampling. Use `get-page` after success to confirm the schema loads.
  For mobile pages (schemaType=10): call get-guidance with name `mobile-page-modification` BEFORE editing the body — mobile bodies use plain JSON (not AMD), with different component types and no handlers/validators.

create-page response fields
- `schemaType` — integer schema type from the template used: 9 = Freedom UI web page, 10 = mobile page.
- `templateName` — name of the template (e.g. `BlankPageTemplate`, `BaseHomePage`).
- `dataSourceName` / `primaryDataSourceName` — the data source bound to `entity-schema-name`, and the page's primary data source when it is one; absent when nothing was bound.

Designer type mapping
- `BaseHomePage` (schemaType=9): opens in the **Homepage designer**. Do NOT use the standard Freedom UI page designer URL for this template. A `BaseHomePage` page becomes a workplace's home page only after you bind it — read get-guidance name `home-page`.
- All other schemaType=9 templates (BlankPageTemplate, PageWithTabsFreedomTemplate, BaseMiniPageTemplate, ListPageV3Template, etc.): open in the **Freedom UI page designer**.
- schemaType=10 mobile templates: open in the **Mobile page designer**.
- IMPORTANT: Do NOT guess or construct a browser designer URL based only on `schemaUId`. Use `templateName` and `schemaType` from the response to determine the correct designer. If unsure, call `get-page` to verify the created page loaded correctly — that is sufficient; the user can open the designer themselves from Workspace Explorer.

Do NOT
- Do not create `__FormPage`-style clone names when a matching page already exists. Prefer editing the existing page via `sync-pages`.
- Do not bypass `list-page-templates`; the catalog reflects the active environment feature flags and may change.
- Do not assume `create-page` creates an app section. Use `create-app-section` when the requested outcome is "add a new section" rather than "add a standalone Freedom UI page".
- Do not construct a browser designer URL after `create-page` without checking `templateName` in the response. Using the Homepage designer URL for non-homepage pages (or vice versa) opens the wrong designer.
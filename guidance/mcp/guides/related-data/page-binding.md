clio MCP page-to-object binding guide

Use this guide to BIND a page to an object: configure which Freedom UI page opens by default when a
record is opened through that object's page resolver, and which page is used when a record is added. In Creatio this is the
object's "related pages" / "page settings" configuration. It is a different task from adding a
related/child list to a page (see `related-list`): this guide is about WHICH page represents a record
of the object, not about a detail shown inside a page.

The tools: `create-related-page-addon` (write) and `get-related-page-addon` (read)
`create-related-page-addon` binds pages by writing the object's `RelatedPage` add-on through
`AddonSchemaDesignerService`. One call performs the whole round-trip the Interface Designer does when
you save related pages: read the (server auto-provisioned) add-on, replace its page list, save, reset
the client script cache, and rebuild static content. A successful write and read-back prove stored
configuration, not that every grid, lookup link, or explicit page-opening request uses that binding.
Because the write FULLY REPLACES the page set, READ FIRST with `get-related-page-addon` whenever the
object may already have a configuration: it returns the current entries (raw UIds + resolved page/role
names + flags + type-column-uid). Then modify that set and send the full set back — read → modify →
write. Skipping the read on an already-configured object silently drops the entries you omit.
Replaying a get result verbatim is supported: an entry may carry BOTH `role` and `role-name` (they are
reconciled when they agree), and you should pass each entry's `page-schema-uid` back — it is used as-is,
so a page whose name did not reverse-resolve (null `page-schema-name`) still round-trips instead of
being silently dropped.

Inputs
- `entity-schema-name` (required) — the OBJECT (entity schema) the pages belong to, e.g. `UsrDelivery`.
- `package-name` (required) — the package that owns the configuration, e.g. `Custom` or your app package.
- `pages` (required) — the full set of page entries. This FULLY REPLACES the object's current
  related-page configuration (it is not a merge), so always send every entry you want to keep.
  Each entry:
  - `page-schema-name` — an existing Freedom UI page schema name. Required UNLESS `page-schema-uid` is
    given; when both are present `page-schema-uid` wins and the name is ignored.
  - `page-schema-uid` — optional explicit page schema UId; prefer it when replaying a get result (used
    verbatim, robust to a name that no longer resolves).
  - `is-default` — true marks the page opened by default when a record is opened.
  - `is-add` — true marks the page used when ADDING a record (the same page may be both default and add).
  - `is-ssp-default` — low-level RelatedPagesMetadata flag; leave false. This is NOT how the portal
    audience is set (see "Audiences" below — the portal audience is the role "All external users").
  - `role` — optional audience role UId (or use `role-name`); only "All employees" and the portal "All external users" are supported, any other role is rejected (omit for all users).
  - `type-column-value` — optional value (used with `type-column-uid`) for a type-specific page set.
- `type-column-uid` (optional) — UId of the type column that drives type-specific page sets. Omit for a
  single page set.

The CLI verb (`create-related-page-addon`) exposes the common case via scalar options instead of a list:
`--entity-schema-name`, `--package-name`, `--default-page`, `--add-page` (defaults to `--default-page`
when omitted), `--portal-default-page`, `--portal-add-page`, and `--type-column-uid`.

Audiences — internal vs portal (this is how portal pages are bound)
The audience of a page set is the page entry's ROLE, not a separate flag (IsSspDefault is not used to
mark the portal audience). The designer ships two out-of-the-box audiences, each a separate
default+add set distinguished by role:
- internal users -> role "All employees";
- portal / self-service users -> role "All external users".
To bind PORTAL pages, give the entry the role "All external users". Two ways:
- per `pages` entry, set `role-name` to "All external users" (or pass the role UId via `role`);
- on the CLI, use `--portal-default-page` / `--portal-add-page` (the portal add page defaults to the
  portal default page). When any portal page is given on the CLI, the `--default-page` / `--add-page`
  set is scoped to the "All employees" role — the general base default — and the portal pages are layered on top as the "All external users" override.
Only these two audiences are supported: "All employees" and "All external users". A custom role (by name or UId) is REJECTED — the designer offers no other audience, and runtime support for an arbitrary role in a related-page set is unverified, so it is not written. Within each (audience, type) cell there may be at most ONE is-default page and ONE is-add page (the same page may be both).
Omitting the role entirely binds a single set that applies to all users. A general base default (is-default, no type-column-value, scoped to "All employees" or no role) is REQUIRED in every configuration — the page opened for a record and the fallback for any record TYPE with no dedicated set; the default page also serves record creation, so add a separate is-add page only for a DIFFERENT add page. Portal ("All external users") and type-specific sets are layered on top; the general base is the INTERNAL audience, so add an explicit "All external users" default whenever portal users open the record — the internal base is not a verified portal fallback. A portal-only (or other audience-scoped-only) binding is rejected — it would leave the general audience with no page to open.

Typed pages — a different page set per record type
A page set can also be scoped to a record TYPE, on top of (or instead of) the audience. Two pieces:
- top-level `type-column-uid`: the UId of the lookup COLUMN on the object that classifies the record
  (e.g. Case.Category). Discover it with `get-entity-schema-properties` for the object — every column
  carries its `u-id`; pick the lookup column you type by.
- per `pages` entry, `type-column-value`: the lookup RECORD Id of the type the entry applies to (e.g.
  the CaseCategory "Incident" record Id). Read it from the column's reference lookup (e.g. odata-read
  on CaseCategory by Name).
The pages list is a flat Role x type matrix: each (audience, type) cell has its own default and add
entry. Keep an untyped set (entries with no `type-column-value`) as the fallback used when a record's
type has no dedicated set. Omit `type-column-uid` entirely for objects without typed pages.

Discovery — resolve names before binding
- Resolve the object and its owning package with `get-app-info` (or `find-entity-schema` when you only
  have the object name). Use the object's app package as `package-name` unless you intentionally own the
  customization elsewhere.
- Resolve page names with `list-pages` (filter by app `code` or `search-pattern`). Pass page SCHEMA
  NAMES; the tool resolves each to its `PageSchemaUId` for you.

View-backed grids — inspect the navigation target before writing
- A `Vw*` name is not a schema classification. Inspect `get-entity-schema-properties`: `db-view`
  and `virtual` are independent flags. For virtual entities, also read `virtual-entities`; do not
  apply virtual-entity rules merely because a name starts with `Vw`.
- Do NOT add a binding to a view just because a grid uses it and `get-related-page-addon` returns
  `pageCount: 0`. First inspect the original page's data source, primary column, clicked link or
  request, and any explicit page override. Establish which entity and record Id the click actually
  targets; a view row Id is not necessarily the underlying object's record Id. Inspect that target's
  existing binding before choosing a change. Do not infer the target or the fallback cause from the
  Classic page that happened to open.
- Database views are not categorically unsupported. On a disposable Creatio Studio 10.1.585.0
  instance (PostgreSQL, .NET 8.0.31; clio 8.1.0.126), `VwSysAdminUnit` reported `db-view: true`,
  `virtual: false`. Its binding read back, appeared in the browser's entity page configuration after
  logout/login, and a native Freedom UI grid link opened the bound Freedom form with the same record
  Id; the form loaded the selected record's name. This verifies that path, not every view or version.
- [Clio issue #1301](https://github.com/Advance-Technologies-Foundation/clio/issues/1301) reports a
  different result for a campaign's `VwBulkEmailInCampaign` grid on Creatio 10.0.0.858 / PostgreSQL /
  .NET Framework: a stored binding did not change navigation after re-login. That campaign path was
  not reproduced by the Studio fixture above. The reporter's follow-up leaves the schema classification
  and proposed real-object datasource workaround unverified. Do not turn this report into a blanket
  view rejection or claim the workaround is tested.
- If considering a real-object datasource instead, first prove the record identity and equivalent
  relationship filters, columns, and access behavior. Test the original navigation path on a disposable
  fixture before applying it elsewhere; do not rewrite a stock grid as an assumed fix.

Typical flow
1. `get-app-info` → object name + package name (and schema-name-prefix for any new pages).
2. `list-pages` → the form page (and, if different, the add page) schema names for that object.
3. `create-related-page-addon` with `entity-schema-name`, `package-name`, and a `pages` list marking the
   default page (`is-default: true`) and the add page (`is-add: true`). The same page may carry both.
4. Read the binding back, then log out and log in again to refresh session page metadata. Open a record
   from the ORIGINAL failing grid/link and verify both the selected page schema and record identity.
   Opening a section list or navigating directly to the desired form is not equivalent proof. No manual
   compile is needed for the add-on write.
5. If that path still opens the wrong page, stop repeating binding writes. Capture the effective
   entity/record target, page override, stored binding and post-login browser result; investigate the
   navigation path. Restore the pre-test page set when abandoning an experiment (an empty list clears
   ALL entries, so use it only when the pre-test set was empty).

Errors
- If the object, package, or any page name does not exist, the tool fails fast with a clear message
  naming the missing artifact (e.g. "Package 'X' not found", "Entity schema 'Y' not found.",
  "Page schema 'Z' not found.") and writes nothing. Fix the name (re-run discovery) and retry.

Behavior and safety
- The object's `RelatedPage` add-on descriptor is auto-provisioned by the server, so binding works the
  same whether or not related pages were configured before — no separate "create addon" step is needed.
- Because `pages` REPLACES the configuration, to add one page to an object that already has role- or
  type-specific sets you must include the existing entries too. For a fresh object a single default
  (and usually add) page is enough. To reset the object to inline editing (dedicated pages no longer wanted), send an EMPTY pages list: it clears every entry and is the effective delete (the platform has no add-on delete; an unconfigured object likewise reports an empty set).
- Existing page and object schemas are NOT modified; only the object's related-page add-on metadata is
  written, so other configurations are unaffected. This tool manages the object's DESKTOP related-page add-on only — mobile pages (the SchemaGroup.MobilePage set) are a SEPARATE add-on, neither read nor written here, so a desktop write (including an empty-clear reset) never affects the object's mobile page configuration.

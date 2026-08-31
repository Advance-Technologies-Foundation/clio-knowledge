clio MCP mobile page modification guide

CALL THIS GUIDE BEFORE EDITING ANY MOBILE PAGE BODY
Mobile pages (schemaType=10) have fundamentally different rules from web pages.
Applying web page patterns to mobile pages will cause broken schemas or silent no-ops.
In the worst case the page becomes unrenderable and will not open at all.

─────────────────────────────────────────────────────────────
WHAT CARRIES OVER FROM THE WEB `page-modification` GUIDE
─────────────────────────────────────────────────────────────
PRECEDENCE: If anything in the web guide conflicts with this mobile guide,
the mobile guide wins — unconditionally. Treat the web guide as a mechanics
reference only; never copy a web page body, AMD wrapper, section name, or
component `type` value into a mobile page.

Read the web guide for the mechanics; this table tells you which sections to apply, skip, or treat with a mobile-specific twist. Anything not listed here is web-only.

| Web-guide section | On mobile |
| --- | --- |
| PRE-EDIT GUIDANCE CHECKLIST: `page-schema-resources`, entity-level `business-rules` (`create-entity-business-rules`), page-level `business-rules` (`create-page-business-rules`) | Applies with mobile-specific limits |
| PRE-EDIT: `page-schema-converters` | Concept only — page body must not declare a `converters` section; reference OOTB converters inline (see below) or an already-existing custom converter in a remote module |
| PRE-EDIT: `page-schema-handlers` | Concept only — page body must not declare a `handlers` section; only reference an already-existing remote handler when the user explicitly asks |
| PRE-EDIT: `page-schema-validators`, `page-schema-creatio-devkit-common` | Does NOT apply — no validator support on mobile (even OOTB), no devkit-common AMD dependency |
| Replacing-schema concept, Design-package resolution, Virtual package materialization | Applies in full — same backend; `page.designPackageUId` / `page.willCreateReplacingInDesignPackage` are returned for mobile pages too |
| Canonical page modification flow (list-pages → get-page → edit → update-page → verify) | Applies in full |
| `update-page optional-properties`, `update-page verify` flag, `sync-pages optional-properties` | Applies in full — identical semantics |
| Body formatting (match existing indentation, do not reformat existing code) | Applies in full — mobile JSON bodies are also saved verbatim; match the indentation style already present in the page |
| Known limitations (fail-closed design-package resolution on writes, best-effort fallback on reads) | Applies in full |
| `bundle.json` shape and `jq` recipes | Applies, with one caveat: `handlers` / `converters` / `validators` source strings are always empty (`'[]'` / `'{}'`) because mobile bodies do not author these sections |
| Web `Rules for viewConfigDiff` section — every bullet EXCEPT the FormPage `DataValueType → component` mapping in the next row (i.e. `operation` / `name` / `parentName` / `propertyName` / `index`, the view-engine `visible` property, and the user-visible string → `$Resources.Strings.*` rule) | Applies in full |
| Rules for viewConfigDiff: FormPage `DataValueType → component` mapping (web component registry) | Does NOT apply — pick the control from the MOBILE registry via `get-component-info schema-type: "mobile"`; the COMPONENT REGISTRY section below is the authoritative type list (note the canonical gotcha: Boolean → `crt.Toggle`, not `crt.Checkbox`) |
| Finding a container for a new component (`parentName`) | Applies in full — same `bundle.containers` lookup; common mobile container types: `crt.Scaffold` (root), `crt.GridContainer`, `crt.FlexContainer`, `crt.TabPanel`, `crt.TabContainer`, `crt.ExpansionPanel` |
| update-page write modes (`replace` / `append`) — including the "do NOT resend `raw.body`" CRITICAL warning and the `ownBodySummary.viewConfigDiffOperations > 0 → use append` rule of thumb, and the own-body `insert`-to-`merge`/`move`/`remove` downgrade warning | Applies, but mobile bodies have no handlers/converters/validators sections (those merge rules do not apply) and the append fragment is plain JSON, not AMD (see BODY FORMAT below) |

If a web guide tells you to add a section this mobile guide forbids (validators / inline handlers or converters declared in the page body / AMD deps), the mobile rule wins.

─────────────────────────────────────────────────────────────
BUSINESS RULES — mobile support boundary
─────────────────────────────────────────────────────────────
Mobile Freedom UI pages do support business rules. Business rule generation works
identically to web — mobile pages do not affect how rules are created or stored.
The same `create-page-business-rules` / `create-entity-business-rules` tools produce
valid rules for both web and mobile pages without any mobile-specific parameters.

Read `business-rules` for rule semantics; this guide adds only the mobile-specific boundary:
  - Use `create-page-business-rules` for UI state changes on one mobile page.
  - Use `create-entity-business-rules` when the rule should apply everywhere the entity is used,
    or when the user needs validation-like enforcement that mobile page bodies cannot provide.
  - Business rules are separate artifacts. Do not implement business-rule logic in the mobile page
    body itself. Use `get-page` first when you need page attribute or element names for a page-level rule.

OFFLINE LIMITATION:
  Mobile business rules support a limited set of conditions and actions compared
  to the web runtime. In offline mode, not all business rule conditions and actions
  are guaranteed to work.
  When creating or modifying business rules for a mobile page, always ask the user
  whether the page is used in offline mode. If yes, warn the user that some rules
  may not function correctly offline and recommend verifying each rule on a real
  device in offline mode after deployment.

─────────────────────────────────────────────────────────────
WHEN A REQUEST CANNOT BE IMPLEMENTED ON MOBILE — NO INVENTION RULE
─────────────────────────────────────────────────────────────
If the user asks for behavior that this guide forbids or that the mobile
runtime does not support, STOP and tell the user. Do NOT silently substitute,
approximate, or fabricate to make the request "work on paper".

Specifically, NEVER:
  - Invent a `crt.*` component, request, or converter name that is not listed
    in this guide or returned by `get-component-info schema-type: "mobile"`.
  - Use a `crt.*` value that exists on web "because it sounds reasonable" —
    if it is not in the mobile component/request/converter catalogue, treat
    it as not implemented.
  - Author a `handlers` / `converters` / `validators` section in the body to
    emulate a missing feature.
  - Add a second data source in `modelConfigDiff` to work around the
    one-data-source-per-page constraint.
  - Add a `crt.Scaffold` insert to "make the layout match" a web template.

Instead, report the limitation explicitly to the user. Use this shape:
  - What was requested.
  - Why mobile cannot do it (cite the rule: e.g. "validators are not supported
    on mobile", "crt.DataGrid is web-only", "page body must not declare
    handlers").
  - The closest supported alternative, if any (e.g. entity-level business rule
    via `create-entity-business-rules`, an OOTB converter from the allowed list,
    a remote handler the user must implement separately and reference by name).
  - Stop and ask the user how to proceed before writing the body.

─────────────────────────────────
BODY FORMAT — plain JSON, not AMD
─────────────────────────────────
Mobile page bodies are plain JSON objects. They are NOT AMD define(...) modules.
The top-level object has exactly three keys:

    {
      "viewConfigDiff": [],
      "viewModelConfigDiff": [],
      "modelConfigDiff": []
    }

DO NOT add: "handlers", "converters", "validators" — these are AMD/web-only constructs.
DO NOT wrap the body in define(...).
DO NOT add any AMD marker pairs (SCHEMA_DEPS, SCHEMA_VIEW_CONFIG_DIFF, etc.).
For append mode, send a fragment containing only the `*Diff` arrays you want to merge; missing keys are skipped.

─────────────────────────────────────────────────────────────
OPERATION SHAPE — the component "type" MUST be inside "values"
─────────────────────────────────────────────────────────────
The differ builds the element from "values" ALONE and then stamps "name" onto it. Every other key
on the operation object — "operation", "parentName", "propertyName", "index" and "type" — is
addressing metadata that is never copied into the element. A "type" written NEXT TO "values"
instead of inside it is DISCARDED: the save reports success and the page keeps an element with no
type, which the runtime cannot render. The request bindings survive, so the button looks correctly
wired while being invisible — which is why this misreads as a placement problem.

  // WRONG — type is a sibling of values; it is dropped
  { "operation": "insert", "name": "RunProcessButton",
    "type": "crt.Button",
    "parentName": "AreaProfileContainer", "propertyName": "items",
    "values": { "clicked": { "request": "crt.RunBusinessProcessRequest",
                             "params": { "processName": "UsrProcess_e629820",
                                         "processRunType": "ForTheSelectedPage" } },
                "layoutConfig": { "column": 1, "row": 1, "colSpan": 1, "rowSpan": 1 } } }

  // RIGHT — type inside values
  { "operation": "insert", "name": "RunProcessButton",
    "parentName": "AreaProfileContainer", "propertyName": "items",
    "values": { "type": "crt.Button",
                "clicked": { "request": "crt.RunBusinessProcessRequest",
                             "params": { "processName": "UsrProcess_e629820",
                                         "processRunType": "ForTheSelectedPage" } },
                "layoutConfig": { "column": 1, "row": 1, "colSpan": 1, "rowSpan": 1 } } }

The "layoutConfig" is not optional decoration: a grid container positions its children with it. See
BUTTON PLACEMENT below for where a button may go and the full shape it needs.

processName / processRunType appear in both examples because update-page REJECTS a
crt.RunBusinessProcessRequest button that omits them (see the run-process-button guide for how to
resolve the code). validate-page does not reach that check on a mobile body, so a body can pass
validate-page and still be refused by update-page — do not treat validate-page green as sufficient.

The same defect applies to "set": "set" is a remove followed by an insert on the same payload, so
it drops the type identically AND destroys the element that did carry a valid one. An "insert"
carrying a type at operation level with NO "values" object at all is the same defect too — "insert"
declares no required parameters, so the differ does not refuse it; it persists an element with
nothing but a name. A flat "set" is the exception: "set" requires "values", so the differ refuses
the whole operation and nothing is written.

What clio does with each shape — mobile bodies only, on validate-page / update-page / sync-pages:

  REJECTED  a "values" object carrying no usable type, while a type sits on the operation object
  REJECTED  a flat "insert" — type at operation level, no "values" object at all
  WARNED    an insert/set whose "values" carries element properties but declares no type anywhere
  WARNED    two DIFFERENT types (the element takes the copy inside "values")
  WARNED    an operation spelled in the wrong letter case — the differ dispatches case-sensitively,
            so "Insert" matches nothing and the operation is discarded whole
  SILENT    two IDENTICAL types; an empty or absent "values"; "merge" (not checked)

If a REJECTED entry came back from get-page, the page already carries the defect — correct it in
the body you send back. Not every surface enforces this: the CLI `update-page` verb does not run
these checks, and sync-pages with validate:false skips them.

Scope: the differ is shared with the web client, so the same shape defect breaks web pages
identically — but clio enforces this rule on MOBILE bodies only.

Evidence: verified on a live stand for ENG-95429 — writing the wrong shape to a real mobile page
and re-reading the server-merged viewConfig returned the element with NO "type", while a
neighbouring template button kept its own. Platform build not pinned; verify against your target
platform version.

─────────────────────────────────────────────────────────────
AUTHORING CHILDREN — a "merge" does not create child elements
─────────────────────────────────────────────────────────────
A "merge" carries PROPERTY values onto an element that already exists. It is not a way to create
child elements. Putting them in its "values" — an array, or a lone object, of configs that carry a
"name" — fails, and how it fails depends on something the body cannot tell you:

  Target slot already holds ELEMENTS  -> the differ STRIPS the whole property out of the merge
                                         before copying anything. The save reports success, the
                                         operation stays in the page body, and the children reach
                                         NOTHING. This is the ENG-95429 report.
  Target slot absent or empty         -> the merge applies and you get what you asked for.

"Elements" is the applier's own test, not a guess about shape: it looks at the target slot's FIRST
child and asks whether it is an item config — an object carrying a non-empty "name". A slot holding
plain data rows (a file list's column descriptors) is not stripped.

  // NOT this — the button reaches nothing on any page whose Scaffold already has actions
  { "operation": "merge", "name": "Scaffold",
    "values": { "actions": [ { "type": "crt.Button", "name": "RunProcessButton", ... } ] } }

  // this — one insert per child, into a container the page declares
  { "operation": "insert", "name": "RunProcessButton",
    "parentName": "AreaProfileContainer", "propertyName": "items", "index": 0,
    "values": { "type": "crt.Button", ...,
                "layoutConfig": { "column": 1, "row": 1, "colSpan": 1, "rowSpan": 1 } } }

If the target genuinely has no such slot, ONE insert is not enough: an insert into a property the
element does not carry throws, because the parent is not a container yet. The platform's own idiom
is two operations, and the merge group runs before inserts, so one body does it:

  { "operation": "merge",  "name": "UsrActionsButton", "values": { "menuItems": [] } },
  { "operation": "insert", "name": "UsrExport", "parentName": "UsrActionsButton",
    "propertyName": "menuItems", "values": { "type": "crt.MenuItem", ... } }

A merge whose slot value is an EMPTY array is exactly that first step and is never flagged.

SINGLE-ELEMENT SLOTS: THE TWO-STEP IDIOM DOES NOT APPLY. A slot that holds one element rather than
a collection — Scaffold "floatAction" — is not a container, so an insert cannot address it and a
merge is the only route. Keep it an object; do not convert it to an array to follow the idiom
above. This is NOT a safe harbour: the strip rule above still applies, so the merge is discarded
when the target already holds an element there, and applies only when the slot is null or absent.
(Do not generalise this to every input that happens to hold an object. A list's "itemLayout" in
particular must NOT be merged onto the parent crt.List — see the web-to-mobile-conversion guide,
which records that the client then reports "is not a container for other items" and the whole
schema fails to build.)

What clio does — mobile bodies only, on validate-page / update-page / sync-pages:

  REJECTED  a merge authoring children on Scaffold's "actions", "leading" or "items". Every
            shipped form template fills the first two (Save in "actions", Close/Cancel in
            "leading") and every non-blank template fills "items" with a MainContainer, so on the
            Scaffold these are the strip case. The same slot names on ANY OTHER element are not
            affected — the check only applies when the merge targets the Scaffold itself.
  WARNED    the same authoring in any other slot. clio applies viewConfigDiff against an EMPTY
            base and cannot tell the two outcomes apart, so it steers instead of refusing: the
            slot may be one the target legitimately lacks (menuItems on crt.Button or
            crt.FloatingActionButton; items on crt.QuickFilterGroup, crt.Sort, crt.Timeline).
  SILENT    an empty array; an array of plain data rows carrying no "name" (a file list's column
            descriptors, for example); and "insert"/"set", where the values object BECOMES the
            element so children declared inside it ARE created — that is the documented container
            pattern, see FIELD GROUPING below.

Because clio cannot see the target, a page built from BlankMobilePageTemplate — a bare Scaffold
whose slots may be empty — is refused too, even though the merge would have applied there. Use the
two-step idiom above rather than reaching for sync-pages validate:false: a plain insert into a slot
that template genuinely lacks is the shape that throws.

Evidence: the STRIP outcome is verified on a live stand for ENG-95429 — a merge on Scaffold
carrying a crt.Button in values.actions saved successfully, stayed in the page's own body, and
appeared ZERO times in the server-merged viewConfig, which still held only the template's own
buttons. Platform build not pinned; verify against your target platform version.

The rest of this section is READ FROM THE APPLIER, not observed on a stand: that an insert throws
on a property the element does not carry, that the merge group runs before inserts, and that a
single-element slot is reachable only by a merge. clio ships the same reading as a validator
(ENG-95429), so guide and tool agree by construction rather than by independent confirmation.

─────────────────────────────────────────────────────────────
VALIDATORS, CONVERTERS, HANDLERS — mobile constraints
─────────────────────────────────────────────────────────────

Validators — not supported on mobile:
  Mobile pages do not support validators at all, including OOTB validators.
  Do not add a "validators" section. Do not reference any validator (custom or OOTB)
  from a mobile page body. If the user asks for validation, implement it via entity-level
  business rules (`create-entity-business-rules`) instead.

Converters — OOTB inline, or reference an existing custom converter:
  Do not add a "converters" section to the page body.
  Reference OOTB converters as inline binding expression strings in viewConfigDiff values, e.g.:
    "visible": "$HasUnsavedData | crt.InvertBooleanValue"
    "visible": "$CardState | crt.IsEqual : 'edit'"
  Converters can be chained: ""$Attr | crt.ConverterA : arg | crt.ConverterB""
  All converters receive the piped value as the first argument.

  Allowed OOTB converters:
    crt.ToObjectProp      — extracts a property from an object
      Arg: propertyName (string)
      ""$FileAttr | crt.ToObjectProp : 'displayValue'""
    crt.InvertBooleanValue — negates a boolean (true → false)
      No args.
      ""$HasUnsavedData | crt.InvertBooleanValue""
    crt.IsEqual           — compares piped value for equality, returns boolean
      Arg: compareValue (string | attribute ref)
      ""$CardState | crt.IsEqual : 'edit'""
    crt.AndBooleanValue   — logical AND of piped boolean with another boolean
      Arg: otherValue (boolean attribute ref)
      ""$SomeFlag | crt.AndBooleanValue : $AnotherFlag""
    crt.IsInArray         — checks if piped value is in an array, returns boolean
      Arg: array (attribute ref)
      ""$Name | crt.IsInArray : $AllowedNames""
    crt.Concat            — concatenates piped string with another value
      Arg: appendValue (string | attribute ref)
      ""$Prefix | crt.Concat : $Suffix""
    crt.ToCollectionFilters — converts selected collection items into filters
      Args: collectionName (string), selectionState (attribute ref)
      ""$Items | crt.ToCollectionFilters : 'Items' : $DataTable_SelectionState""

  Custom converters: only reference one if the user explicitly asks for it AND it already
  exists in a remote module (the page body still must not declare a `converters` section).

Handlers — reference-only, from an existing remote module:
  Do not add a "handlers" section to the page body.
  Custom request handlers can be referenced (e.g. via `clicked` binding to a custom request
  type) only if the user explicitly asks for them AND the handler is already implemented in
  a remote module. Do not author handler code inside the mobile page body.

────────────────────────────────────────────────────────
crt.Scaffold — do NOT re-insert
────────────────────────────────────────────────────────
All five mobile templates already insert one crt.Scaffold.
Your page inherits it. DO NOT add another Scaffold insert — it will create
a duplicate root element.

To add content inside the Scaffold:
- Patch it:  { "operation": "merge", "name": "Scaffold", "values": { ... } }   <- PROPERTIES only;
             putting child elements in there is blocked, see AUTHORING CHILDREN above
- Add child: { "operation": "insert", ..., "parentName": "Scaffold", "propertyName": "items" }

viewConfigDiff INSERTS ADDRESS THE SLOT BY propertyName ONLY — never use "path" in a
viewConfigDiff insert (e.g. NOT "path": ["tools"]; use "propertyName": "tools"). "path" is
the addressing mechanism for viewModelConfigDiff / modelConfigDiff only; a viewConfigDiff
insert that uses "path" does not place the element. (The exact mechanism here is unverified and is
DISTINCT from an unresolved "parentName", which is described under BUTTON PLACEMENT below and does
persist the element at the root.)

─────────────────────────────────────────────────────────────
BUTTON PLACEMENT — not the Scaffold "actions"/"leading" slots
─────────────────────────────────────────────────────────────
DO NOT insert a button into the Scaffold "actions" or "leading" slots. Those slots are real and
the platform fills them itself — the merged Scaffold of a mobile form page carries the template's
own Save button in "actions" and Close / Cancel in "leading". The problem is narrower: a button
YOU insert there does not appear on the mobile designer canvas, so nobody can see or edit it
afterwards.

Put your buttons in the page body instead: a container's "items", with a "layoutConfig". That is
what the mobile designer itself emits when a person drags a button onto the page.

  // NOT this — saves, but is invisible in the designer
  { "operation": "insert", "name": "RunProcessButton",
    "parentName": "Scaffold", "propertyName": "actions",
    "values": { "type": "crt.Button", ... } }

  // this — the shape the designer produces
  { "operation": "insert", "name": "RunProcessButton",
    "parentName": "AreaProfileContainer", "propertyName": "items", "index": 0,
    "values": { "type": "crt.Button", ...,
                "layoutConfig": { "column": 1, "row": 1, "colSpan": 1, "rowSpan": 1 } } }

Use a container the page or its template actually declares — read the names from get-page. An
insert whose parentName does not resolve is NOT refused: the differ falls back to the root of
viewConfig and the element lands THERE, outside every container. It is saved, just not where you
asked, so get-page will show it and the designer will not.

Scope and limits of this rule:
- It is about an INSERT of your own button. Do NOT read that as "use a merge instead": a merge
  carrying the button inside values.actions is a WORSE failure and has its own rule above
  (AUTHORING CHILDREN). A merge that patches PROPERTIES of an element the template already owns
  in that slot is a different case again, and nothing here applies to it. ("set" is no longer named
  here because it behaves as an insert — see OPERATION SHAPE — so the insert rule already covers it.)
- It governs a body YOU author. A page produced by web-to-mobile conversion follows the elementMap
  placement of that guide instead.
- clio warns — never blocks — on exactly one shape: an insert of crt.Button into Scaffold/"actions".
  "leading" is advice; no validator checks it.
- Verified for "actions" (ENG-95429, live stand): a button inserted there is absent from the
  designer canvas. "leading" is an extrapolation from the same observation — the designer omits
  that entire navigation bar, the template's own buttons included — and was not tested separately.
- NOT claimed: that either slot fails to render in the Creatio Mobile app. That was never tested.
- crt.FloatingActionButton on Scaffold "floatAction" (see the component list below) was not tested
  either way; this rule says nothing about it.

─────────────────────────────────────────────────────────────
COMPONENT REGISTRY — MOBILE COMPONENTS ONLY (CRITICAL)
─────────────────────────────────────────────────────────────
ONLY components registered in the MOBILE registry can be used on a mobile page.
Web components WILL NOT WORK on mobile — they are not loaded by the mobile runtime
and will result in a broken page (blank slot, runtime error, or silent no-op).
This is a hard platform boundary, not a styling difference:
  - Mobile and web have separate component registries and separate runtimes.
  - A component name that exists on web (e.g. `crt.Checkbox`, `crt.DataGrid`)
    is NOT automatically available on mobile even if it sounds generic.
  - Do NOT copy `type` values from a web page body into a mobile page body.

MANDATORY before inserting any component into a mobile page:
  get-component-info schema-type: "mobile"

Key mobile-specific types:
  crt.Toggle          — Use for Boolean fields (NOT crt.Checkbox — mobile designer maps Boolean → Toggle)
  crt.BarcodeScanner  — Barcode/QR scanner; mobile-only
  crt.FloatingActionButton — Floating action button (set on Scaffold.floatAction)
  crt.Sort            — Sort control for list pages; mobile-only
  crt.QuickFilterGroup — Group of quick filter chips; mobile-only. Model-driven: the chips are built at
                         runtime from the QuickFilterGroup_Value attribute (via
                         crt.QuickFilterGroupAttributeConverter), so do NOT author them as static
                         crt.QuickFilter children of its `items`.

NOT available in mobile (web-only):
  crt.DataGrid, crt.HtmlEditor, crt.PasswordInput, crt.EncryptedInput,
  crt.ColorPicker, crt.TagSelect, crt.MultiSelect, crt.IFrame,
  crt.Chat, crt.Dashboards

VERIFY, DON'T DOWNGRADE. Neither list above is exhaustive — `get-component-info
schema-type: "mobile"` is the authoritative source for what runs on mobile. A type
existing on web does NOT make it web-only: if it IS in the mobile catalog it IS
implemented, so use it (the inverse of the NO INVENTION rule above). When a column
has a specific data type (phone, email, number, date, etc.), do NOT default it to a
generic input out of caution — check the catalog you already retrieved and PREFER
the matching specialized input. Fall back to a generic input only when the catalog
has no specialized match.

─────────────────────────────────────────────────────────────
ANALYTICS WIDGETS ON MOBILE (charts and metrics)
─────────────────────────────────────────────────────────────
`crt.ChartWidget` and `crt.IndicatorWidget` ARE mobile components — they are in the mobile
registry and the Mobile Designer offers them under "Widgets". `crt.Dashboards` is not, and mobile
has no dashboard / home-page template, so an analytics request always lands on an EXISTING mobile
page: a record page (`*_MobileFormPage`) or a section page (`*_MobileListPage`).
Read `chart-widget` / `indicator-widget` for the widget payload; this section owns only what mobile
does differently.

NO DATA SOURCE NEEDED — an analytics widget fetches its own data from `config.…data.providing`, so the
  page's `modelConfigDiff` stays untouched and the one-data-source-per-page constraint does not apply to it.

ONE SERIES PER CHART — the mobile runtime loads data for the FIRST series only. Its renderer can draw
  several same-type series and the Mobile Designer canvas does, but the later series' collections are never
  fetched, so on the device they arrive empty (multi-series is ENG-87329, still open). Mixed types collapse
  further: the client keeps the first series alone. A multi-category ask therefore becomes one chart PER
  category, or ONE series grouped by the category column — a TOTAL cannot be a grouping value. Neither the
  canvas nor a saved-body read-back catches this; only the app does.

RUNTIME GAPS TO DESIGN AROUND — the mobile analytics runtime trails the web one, and no design-time
  check reveals it. A DATE column under SUM/AVG fails the runtime request outright (server
  `UnsupportedTypeException`) and under MIN/MAX draws an empty chart, so aggregate a number or COUNT the
  primary column and put the date in the grouping instead (ENG-89575); negative values do not render
  (ENG-88682); grouping through a related object can disagree with the web result (ENG-89059). Tell the
  user when their request needs one of these rather than shipping a widget that renders blank on the
  device.

PARENT — insert into a grid container the page already has: `MainContainer` (root grid of every
  mobile template), `ListContainer` (list/section pages), or a tab's `…TabContainer` /
  `AreaProfileContainer` (record pages). Take the exact name from `bundle.containers` (`get-page`).
  `crt.DetailsGrid` does not exist on mobile.

RE-ROW THE SIBLINGS — every child of a mobile grid carries an explicit `layoutConfig.row`, so a
  widget insert must also `merge` `layoutConfig` on the existing siblings; two children must never
  claim the same row (chart at `row: 1` → the list moves to `row: 2`). Skipping this collapses the
  layout.

NO SIZE FLOOR — the mobile runtime owns widget height: ship `rowSpan: 1` and ignore the web and
  dashboard floors (`rowSpan >= 6`, flex `height >= 350`).

THEME — `theme: "without-fill"` plus a visible `config.color` (the Mobile Designer's "Plain white").
  Dashboard-only and desktop-only themes/slots do not apply.

STRINGS — widget `config` strings use `#ResourceString(<key>)#` AND must be registered in the page
  resources; component labels on the same page use `$Resources.Strings.<key>`. Mixing them up
  renders a header with no title.

REACHABILITY FIRST — a section reaches the mobile app only when BOTH a `SysModuleInWorkplace` row in a
  workplace with `SysApplicationClientType` = Mobile AND a non-empty `SysModule.MobileSectionSchemaUId`
  exist for it. `create-app` sets both up itself, so a scaffolded app is already reachable; the check
  matters for apps whose package was built without mobile pages. Without them the page and its widget
  exist in the Mobile Designer only. Registering a mobile section is separate work, not part of adding a
  widget. Workplace-driven registration is the 8.3.3+ mechanism and is gated by the feature
  `Mobile.UseSysWorkplaceForModuleRegistration`; with that feature OFF the section list comes from the
  legacy `MobileApplicationManifest`, so check the feature before trusting the workplace query. A widget
  insert never touches the manifest.

DIFF THE BODY AROUND AN APPEND WRITE — `viewConfigDiff` dedupes by element name and keeps the last entry,
  so an append can silently drop a PRE-EXISTING operation your fragment never names (observed three
  times: a page with two `FolderTreeActions` merges lost `rootSchemaName`, with `success: true`). Dump
  the own body before writing, diff the read-back after, restore losses as one consolidated merge — and
  note that the write-path validator rejects inline user-visible literals that the templates themselves
  ship, so a faithful restore is not always possible.

NAMING CONFLICT — the mobile runtime resolves pages by `<Entity>_MobileListPage` /
  `<Entity>_MobileFormPage`, while `create-page` / `update-page` enforce the environment's
  `SchemaNamePrefix`. Report the conflict; never flip the `SchemaNamePrefix` setting to work around it.

RECORD SCOPE — on a record page each series is scoped through `data.providing.dependencies`
  (`{ attributePath: "<chart entity column>", relationPath: "<primaryDataSourceName>.<attribute>" }`).
  `relationPath` is NOT restricted to `Id`: the platform supports lookup columns and the primary
  attribute only, so `PDS.Id` (this record) and `PDS.<SomeLookup>` (rows sharing that lookup) are both
  valid, while a text/number/date column is rejected. The Mobile Designer wires this automatically;
  `update-page` does NOT — author it or the widget shows every row. A section page has no record
  context: emit no `dependencies` there.

SCOPING CANNOT BE VERIFIED ON THE CANVAS — the relation resolver returns null at design time, so a
  scoped chart draws the unscoped distribution in the designer. That is expected. The canvas proves the
  widget resolves, the layout holds and the aggregation returns numbers; the scope itself is provable
  only in the mobile app (device/emulator) or by replaying the runtime request.

PAGE FILTERS come free — both widgets declare `PlatformAPIs.Filtration`, so the page's quick filters
  narrow them with no extra wiring.

DRILL-DOWN — an optional `drilldownConfig` on the widget ROOT overrides the drill-down list layout
  (an array position-aligned with `config.series[]` for a chart, a single object for a metric).
  Omit it for the platform default.

REPLACING CHAINS — a customized mobile page is usually a chain (template → vendor package → your
  design package). `get-page-hierarchy` is the tool that settles it; `parentSchemaName` from `get-page` /
  `list-pages` is ambiguous (it may name the TEMPLATE or the replaced schema). Author into the leaf
  (`get-page`'s `designPackageName`) — the runtime merges every level. Two consequences: the section is
  registered against the ROOT UId, and a replacing schema inherits its parent's name, so the
  `SchemaNamePrefix` check never fires when you edit an existing page (only when creating one).

DESIGNER URL — `/0/ClientApp/#/MobilePageDesigner/<ROOT schemaUId>`. The leaf UId opens an empty
  designer shell, and changing only the hash does not remount the designer — reload the page.

BROWSER LOGIN INVALIDATES THE clio SESSION — signing into the same stand as the same user in a browser
  can invalidate clio's session; clio calls then return IIS 401 HTML and surface as
  "Unexpected character encountered while parsing value: <". It is an expired session, not a malformed
  request. Do the machine read-backs first, or use a separate account for the browser.

ORPHANED RESOURCE KEYS — the resources payload only adds/updates. Replacing a widget leaves its
  `<oldName>_title` / `<oldName>_series_0` keys registered with nothing referencing them, and there is
  no documented way to delete a key. Harmless, but say so instead of claiming a clean replacement.

VERIFY IN THE APP — a mobile widget cannot be verified in a browser. Check the saved body with
  `get-page`, the layout on the Mobile Designer canvas, and the rendered result in the mobile
  application (device or emulator). The app is offline-first: it serves the local cache first, so
  synchronize after a schema change and re-login before checking workplace or section lists. An emulator
  also exposes the page metadata, business-rule metadata and merged manifest the client actually
  received. `/0/ClientApp/` is the designer app and `/0/Shell/` the runtime shell; the mobile client
  may be Flutter-first (`UseMobileFlutterFirst`) and `/0/Nui/mobile.aspx` may be absent — that says
  nothing about the widget. A `success: true` save proves persistence only: re-read with `get-page` and
  check `willCreateReplacingInDesignPackage`.

Evidence: schemas produced by the Mobile Designer on Creatio 8.3.4 / 10.0 for a record page
(`MobilePageWithTabsFreedomTemplate`) and a section page (`BaseMobileListTemplate`), read back with
`get-page` (ENG-95577).

─────────────────────────────────────────────────────────────
ADAPTIVE BREAKPOINTS
─────────────────────────────────────────────────────────────
The web→mobile conversion PROPOSES an adaptive layout for containers that group 2+ fields
(stack on phone, 2 columns on tablet) — present it to the user, who can adjust or decline.
Apply this section whenever you want per-screen placement; otherwise rely on the static
`columns` / `layoutConfig` values.

Breakpoints: "small" (phone portrait), "medium" (landscape / tablet portrait), "large" (tablet landscape).
The mobile designer "Tablet portrait" / "Tablet landscape" preview switcher maps to `medium` / `large`.

Adaptive layout has TWO sides — you must edit both for a real responsive layout:

(1) CONTAINER side — `adaptive` on crt.GridContainer (sibling of `columns`).
    Defines how many grid columns exist at each breakpoint.
    Both string ("1fr 1fr") and array (["1fr","1fr"]) forms are accepted by the runtime.

    {
      "operation": "merge",
      "name": "AreaProfileContainer",
      "values": {
        "adaptive": {
          "small":  { "columns": ["1fr"] },
          "medium": { "columns": ["1fr", "1fr"] },
          "large":  { "columns": ["1fr", "1fr", "1fr"] }
        }
      }
    }

(2) CHILD side — `layoutConfig.adaptive` on each child component (button, field, label, ...).
    Defines the explicit grid cell the child occupies at each breakpoint.
    Per breakpoint keys: `row` (1-based), `column` (1-based), `colSpan`, `rowSpan`.

    Without per-breakpoint `layoutConfig.adaptive`, children fall back to the static
    `layoutConfig.{row,column,colSpan,rowSpan}` values and the container's adaptive
    `columns` change alone will NOT reflow them — the result is a wider container with
    items still pinned to their original cells.

Worked example — three buttons that stack on phone, wrap 2+1 on tablet portrait, and
line up 1×3 on tablet landscape (see UsrMobilePage_07zg9nr for a live reference):

    // Container: 1 / 2 / 3 columns by breakpoint
    { "operation": "merge", "name": "AreaProfileContainer", "values": {
        "adaptive": {
          "small":  { "columns": ["1fr"] },
          "medium": { "columns": ["1fr","1fr"] },
          "large":  { "columns": ["1fr","1fr","1fr"] }
        }
    }}

    // Button 1 — always top-left
    { "operation": "insert", "name": "Button1", "parentName": "AreaProfileContainer",
      "propertyName": "items", "index": 0,
      "values": { "type": "crt.Button", "caption": "...",
        "layoutConfig": { "adaptive": {
          "small":  { "row": 1, "column": 1, "colSpan": 1, "rowSpan": 1 },
          "medium": { "row": 1, "column": 1, "colSpan": 1, "rowSpan": 1 },
          "large":  { "row": 1, "column": 1, "colSpan": 1, "rowSpan": 1 }
        }}
      }}

    // Button 2 — below on phone, right on tablet portrait, middle on tablet landscape
    { "operation": "insert", "name": "Button2", "parentName": "AreaProfileContainer",
      "propertyName": "items", "index": 1,
      "values": { "type": "crt.Button", "caption": "...",
        "layoutConfig": { "adaptive": {
          "small":  { "row": 2, "column": 1, "colSpan": 1, "rowSpan": 1 },
          "medium": { "row": 1, "column": 2, "colSpan": 1, "rowSpan": 1 },
          "large":  { "row": 1, "column": 2, "colSpan": 1, "rowSpan": 1 }
        }}
      }}

    // Button 3 — third row on phone, second row on tablet portrait, third column on landscape
    { "operation": "insert", "name": "Button3", "parentName": "AreaProfileContainer",
      "propertyName": "items", "index": 2,
      "values": { "type": "crt.Button", "caption": "...",
        "layoutConfig": { "adaptive": {
          "small":  { "row": 3, "column": 1, "colSpan": 1, "rowSpan": 1 },
          "medium": { "row": 2, "column": 1, "colSpan": 1, "rowSpan": 1 },
          "large":  { "row": 1, "column": 3, "colSpan": 1, "rowSpan": 1 }
        }}
      }}

Rules of thumb:
  - Define `adaptive` on the GridContainer AND `layoutConfig.adaptive` on every child
    that needs to move between breakpoints. Skipping either side breaks the layout.
  - Use 1-based `row` and `column` — the runtime reflows children by these per breakpoint
    (one item per cell). `colSpan` / `rowSpan` are serialized as 1 to match the mobile
    designer's format, but are NOT honored per-item by the runtime; do not rely on them to
    span cells. To make an item wider, give the container fewer columns at that breakpoint.
  - Keep all three breakpoints (`small`, `medium`, `large`) populated even when two
    share the same cell — the designer always serialises the full set and partial maps
    may render as empty cells on the missing breakpoint.
  - Adaptive child placement is supported inside crt.GridContainer. For crt.FlexContainer,
    reflow happens through the container's own `direction` / `wrap` properties, not
    through child `layoutConfig.adaptive`.

─────────────────────────────────────────────────────────────
FIELD GROUPING IN CONTAINERS (mobile layout convention)
─────────────────────────────────────────────────────────────
On mobile pages, fields MUST be grouped inside crt.GridContainer instances
with the "primary" color. This gives each field group a visually distinct
card-like surface that matches the native mobile design language.

Default field container pattern:
  {
    "operation": "insert",
    "name": "<GroupName>Container",
    "values": {
      "type": "crt.GridContainer",
      "columns": "1fr",
      "color": "primary",
      "borderRadius": "medium",
      "padding": { "top": "medium", "right": "medium", "bottom": "medium", "left": "medium" },
      "items": []
    },
    "parentName": "GeneralTabContainer",
    "propertyName": "items",
    "index": 0
  }

Rules:
  - Always set "color": "primary" on field-group containers (not "default").
  - If the page bundle already provides containers with "color": "primary"
    (visible in the existing viewConfigDiff from `get-page`), reuse them as
    field parents instead of inserting new ones.
  - Group logically related fields together in one container.
  - Use separate containers for distinct field groups (e.g. general info,
    communication details, address fields).
  - Insert individual field components as children of these containers, NOT
    directly into the Scaffold items.

─────────────────────────────────────────────────────────────
REQUESTS AVAILABLE ON MOBILE
─────────────────────────────────────────────────────────────
Use these built-in requests in viewConfigDiff event bindings (e.g. `clicked`,
`valueChange`, `updated`). Do NOT invent request types not on this list.
Params listed below go into the `params` object of the binding:
  "clicked": { "request": "crt.<Name>", "params": { ... } }

── Navigation ──────────────────────────────────────────────

crt.OpenPageRequest
  Opens a Freedom UI page by schema name.
  params: schemaName (string, required)

crt.ClosePageRequest
  Closes the current page. Triggers discard-changes confirmation when
  there are unsaved changes.
  params: (none)

── Record operations ───────────────────────────────────────

crt.CreateRecordRequest
  Creates a new record and opens the edit page.
  params: entityName? (string), defaultValues? ([{attributeName, value}])

crt.UpdateRecordRequest
  Opens an existing record for editing.
  params: entityName? (string), recordId? (string)

crt.DeleteRecordRequest
  Deletes a record.
  params: recordId? (string)

crt.CopyRecordRequest
  Creates a copy of an existing record.
  params: entityName? (string), recordId (string, required)

crt.SaveRecordRequest
  Saves all pending changes on the current page.
  params: preventCardClose? (boolean — keep the page open after save)

crt.CancelRecordChangesRequest
  Reverts unsaved changes on the current record; page stays open.
  params: (none)

── Data ────────────────────────────────────────────────────

crt.LoadDataRequest
  Reloads the page's data sources - the Designer's "Refresh data" action; also loads a
  single named data source in its programmatic form. FULL parameter contract is the
  request catalog (single source of truth): get-request-info request-type=crt.LoadDataRequest schema-type=mobile.

crt.QuickFilterRequest
  Applies a quick filter to a list/value attribute.
  params: filterValue? (depends on the filter attribute)

── List items ──────────────────────────────────────────────

crt.CreateListItemRequest
  Creates an item in a list/collection.
  params: defaultValues? ([{attributeName, value}])

crt.UpdateListItemRequest
  Updates an item in a list/collection.
  params: recordId? (string)

crt.DeleteListItemRequest
  Deletes an item from a list/collection.
  params: recordId? (string)

── Advanced ────────────────────────────────────────────────

crt.ExecuteExpressionRequest
  Evaluates an expression via the mobile expression engine.
  params: expression (string, required)

── Business process ────────────────────────────────────────

crt.RunBusinessProcessRequest
  Starts a business process. processName AND processRunType are both REQUIRED
  (e.g. 'ForTheSelectedPage' for the current record; 'RegardlessOfThePage' for none).
  "For the selected page" maps to processRunType: 'ForTheSelectedPage' — setting
  recordIdProcessParameterName alone does NOT select the run type.
  FULL parameter contract is the request catalog (single source of truth): call
  get-request-info request-type=crt.RunBusinessProcessRequest schema-type=mobile and resolve
  the process with get-process-signature FIRST. On mobile pages ALWAYS pass
  schema-type=mobile — the mobile request catalog is a separate registry scoped to the
  requests available on Freedom UI mobile, and their parameters can differ from desktop
  (e.g. crt.RunBusinessProcessRequest adds mobile-only activeRow / activeRowAttributeName).
  Keys in processParameters / parameterMappings /
  recordIdProcessParameterName are process parameter CODES, NOT captions — a wrong code
  is silently dropped. The run-process-button guide (get-guidance run-process-button)
  carries the authoring recipe around that contract.

── Files ───────────────────────────────────────────────────

crt.UploadFileRequest
  Uploads files into a crt.FileList on the page and attaches them to its record; the
  file-source picker is always used. FULL parameter contract is the request catalog
  (single source of truth): get-request-info request-type=crt.UploadFileRequest schema-type=mobile.

crt.DeleteFileRequest
  Deletes an uploaded file.
  params: fileName? (string),
          entityName? (string, defaults to 'SysFile'),
          recordId? (string),
          itemsAttributeName? (string)

── Dialogs and lookups ─────────────────────────────────────

crt.ShowDialogRequest
  Shows a confirmation or informational dialog.
  params: title? (string),
          message (string, required),
          actions (DialogAction[], required — each has caption, key)

crt.OpenLookupPageRequest
  Opens a lookup selection page.
  params: entitySchemaName? (string),
          filtersConfig? ({filterAttributes: [{name, value?}]}),
          features? ({create?: {enabled}, select?: {multiple?}}),
          selectionState? ({type: 'specific', selected: []}),
          caption? (string)

── Communication options ───────────────────────────────────

crt.AddCommunicationOptionsRequest
  Opens add-communication-option UI (phone, email, etc.).
  params: viewElementName (string, required)

crt.CreateCommunicationOptionRequest
  Creates a communication option record.
  params: masterRecordColumnName (string, required),
          componentName (string, required),
          values (Record<string, unknown>, required),
          indexToInsert? (number)

crt.RemoveCommunicationOptionRequest
  Removes a communication option.
  params: recordId (string, required),
          collectionAttributeName (string, required)

── Mobile-only requests (native device capabilities) ──────

crt.SetAttributeFromBarcodeRequest
  Triggers the barcode/QR scanner and writes the result to an attribute.
  params: attributeName (string, required),
          lookupFilterColumn? (string — filter lookup values by scanned code)

crt.OpenAddressOnMapRequest
  Opens the native maps app with the specified address.
  params: query (string[], required — address parts)

crt.OpenCustomWebViewPageRequest
  Opens an in-app web view.
  params: controllerName? (string),
          viewXClass? (string),
          extensionsModel? (string),
          parameters? (Record<string, JsonData>)

─────────────────────────────────────────────────────────────
MOBILE PAGE NAMING CONVENTIONS (Creatio standard)
─────────────────────────────────────────────────────────────
  Mobile form page:  Usr<Entity>_MobileFormPage
  Mobile list page:  Usr<Entity>_MobileListPage

─────────────────────────────────────────────────────────────
TEMPLATE HIERARCHY (for reference)
─────────────────────────────────────────────────────────────
BaseMobileTemplate       — root template shared by page and list branches (inserts Scaffold + MainContainer)
├── BaseMobilePageTemplate   — record/form pages (adds CancelButton, SaveButton, FAB)
│     └── MobilePageWithTabsFreedomTemplate — tabbed record pages (GeneralInfoTab, FeedTab, AttachmentsTab)
└── BaseMobileListTemplate   — list/section pages (adds search, FAB, HeaderContainer, crt.List)
BlankMobilePageTemplate      — standalone bare Scaffold, no parent, no content

`list-page-templates schema-type=mobile` returns 4 selectable templates (the abstract `BaseMobileTemplate` shown above is a parent only, not user-selectable). All of them already provide `crt.Scaffold` — do NOT insert another one.
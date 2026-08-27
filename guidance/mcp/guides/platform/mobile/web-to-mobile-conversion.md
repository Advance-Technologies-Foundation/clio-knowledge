clio MCP — Freedom UI WEB → Freedom UI MOBILE page conversion guide

PURPOSE
Convert an existing Freedom UI WEB page into a Freedom UI MOBILE page for the Creatio
Mobile app. The conversion is MODEL-DRIVEN: a tool gives you a deterministic advisory
guide, and YOU build the mobile page body and persist it with the standard page tools.
The tool decides nothing about the body — you do.

SCOPE: Freedom UI ONLY. This does NOT handle Classic UI pages. A Classic UI page must
first be converted to a Freedom UI WEB page (with the dedicated classic-web ->
freedom-web converter), and only then converted to mobile.

TOOL: get-mobile-page-conversion-guide (ADVISORY-ONLY — builds nothing, writes nothing)
It detects the source page type (today only Freedom UI web, sourceType "freedom-web", is
supported) and returns a conversion GUIDE. It does NOT generate a body and does NOT save to
Creatio or disk. The guide contains:
  - recommendedMobileTemplate + templateNote — the mobile template to create the page from.
  - containerMap — web→mobile container-name correspondence; use it to set each
    component's parentName to the correct mobile container.
  - sourceStructure — the full resolved component tree (incl. components inherited from the
    base template), with name / type / parentName / isContainer.
  - componentSuggestions — per source component TYPE: a category (directMapping /
    withAdaptation / alternativeAvailable / unsupported / requiresManualDecision), the
    suggested mobile type(s), and a primaryWebMerge note for many→one mappings.
  - elementMap — per NAMED ELEMENT, the exact instance-level decision (operation =
    merge / insert / drop / relocate-children). Iterate this to build the body; it already
    encodes merge-vs-insert, the mobile parent, survivability and caption resources. Do NOT
    re-derive placement from containerMap + componentSuggestions, and do NOT override the entry's
    parentName/propertyName with get-component-info's parent/container advice — see ELEMENT PLACEMENT
    IS AUTHORITATIVE in HARD MOBILE RULES.
  - mobileContracts — for each suggested mobile type: allowedProperties + example +
    designerDefaults, so you can build the component's values inline.
  - modelConfigDiff / viewModelConfigDiff — READY-TO-PASTE diffs. BOTH are a set of FOCUSED
    targeted merges, NOT a single root merge: modelConfigDiff carries one merge per top-level key
    (e.g. ["dataSources"]) plus a per-array override unioned with the template's own natives;
    viewModelConfigDiff carries a page-owned ["attributes"] merge + per-collection augments +
    per-array modelConfig overrides unioned with the template's own natives. Paste them VERBATIM as
    the page's modelConfigDiff / viewModelConfigDiff (see DATA SECTIONS below). This is the
    supported way to apply the data sections.
  - modelConfig / viewModelConfig — the same configs in full-object form, for REFERENCE only.
    viewModelConfig is already FILTERED (attributes used only by dropped components removed).
  - adaptiveLayout — the responsive layout for each MULTI-column grid container (phone collapses to
    1 column and stacks; tablet/desktop keep the web columns). BOTH sides are already baked into
    mobileValues (the container's adaptive columns into its own values, each child's placement into
    elementMap[].mobileValues.layoutConfig.adaptive) — nothing separate to apply. Present it at the
    gate so the user can adjust or decline. Null when there is no multi-column grid container.
  - tabAreaLayers — the mobile designer's two-layer body synthesized inside every tab the CONVERTER
    creates: a tab-body grid holding the tab's Area card (the guide gives you both names — take them
    from here, never build them from a name pattern), with ALL of the tab's top-level content
    (expansion panels included) already retargeted into the Area and stacked in web order. Both
    layers are ORDINARY elementMap inserts placed right after the tab's own entry —
    nothing separate to apply. This structure is MANDATORY (a team standard), NOT a proposal: report
    it at the gate so the user knows what the tab bodies look like, but never offer to skip or
    replace it. Null when the converter creates no tab, or every converted tab is empty (an empty tab
    gets no layers, so an empty Area is never created in the first place).
  - normalizations — ONE SECTION PER STANDARD the converter NORMALIZED to, keyed by the standard's
    group. Each section carries a caller-facing `note`, `normalized[]` — one entry per element with its
    `name`, its `type` and the EXACT `properties` written, a leaf ALREADY at the standard being left out
    of that list — and `skipped[]` when the standard could not be applied somewhere, with the
    `properties` paths refused and the `reason`. For a normalized element
    the converter WRITES the mobile standard instead of translating the web page's own value — the web
    value is discarded, even when the web element carried none — and the result is already baked into
    elementMap[].mobileValues, so there is nothing separate to apply. A SKIPPED element is the opposite:
    it keeps its WEB values and may need a manual pass in the designer, so never read an empty
    `normalized[]` as "nothing to normalize" without checking `skipped[]`. WHICH sections exist, WHICH
    element types each covers and WHICH properties it writes is converter configuration resolved at RUN
    TIME: read the sections and entries the response carries instead of assuming a fixed set, and treat a
    section, type or property you do not recognize as one more standard rather than folding it into a
    known one. A merging standard rewrites ONLY the leaves it reports and leaves the sibling subtrees of
    what it stamps in place, so never rebuild a stamped object from its reported keys alone — doing that
    to a metric drops config.data, the aggregation subtree without which the widget renders nothing.
    Merge twins the mobile template provides are untouched. SILENT — never a gate question:
    state EACH section in the plan and the final report as ONE aggregated line. Never restore the web
    value. Null only when no standard normalized or skipped anything at all.
  - spacingNormalization — BACK-COMPAT ALIAS of the "spacing" section, shape unchanged for callers that
    already read it, and it mirrors that section's `normalized[]` ONLY. Prefer normalizations, which also
    carries `skipped[]` and the standards this one cannot express. Read it only when the clio you are
    talking to returns no normalizations.
  - resourceStrings — every localized string the converted body references (top-level captions AND
    nested tokens like config.title / text.template), keyed by resource name and resolved to its
    en-US text. Register this whole map via update-page `resources` so every #ResourceString token renders.
  - constraints + nextSteps — the hard mobile rules and the ordered flow.

─────────────────────────────────────────────────────────────
GATES — MANDATORY HARD STOPS (analysis-first: nothing is written until the developer approves)
─────────────────────────────────────────────────────────────
This conversion is advisory-first. Running the guide and presenting the plan WRITE NOTHING.
Persistence and section registration each require the developer's EXPLICIT approval, given as a
separate response AFTER you show a plain-language plan:
- Gate M (before ANY write): after running get-mobile-page-conversion-guide, present the
  plain-language plan (what transfers / is adapted / is unsupported / needs a decision, plus the
  section-registration intent) and STOP. Do NOT call create-page, update-page, validate-page, or
  create-page-business-rule until the developer explicitly approves the plan.
- Gate S (before ANY section/workplace registration): do NOT call odata-update / odata-create
  (SysModule / SysModuleInWorkplace / SysWorkplace) or create-related-page-addon (schema-type=mobile,
  the default mobile edit page) until the developer SEPARATELY approves the registration. Registering
  as a section is always the user's decision.
- The user's initial request is NOT approval. "convert page X to mobile and register it as a
  section" states the request, not approval of the plan. Present the plan, then wait for a
  separate explicit go-ahead.
- Headless / autonomous mode: never self-approve. Produce the plan, ask for confirmation, and END
  THE TURN without writing or registering anything.
These gate rules are SELF-SUFFICIENT and mandatory on their own: running the guide and presenting the
plan write nothing, and every persistence or registration step needs the developer's explicit approval
first — never do less than this. A higher-level workflow that invoked the conversion MAY layer a richer
approval process on top (for example a structured, plan-first review with an explicit approve step
before any write); follow that when it is present. This article does not depend on any such workflow —
it stays focused on the conversion itself, and the body-building mechanics are the rest of it below.

─────────────────────────────────────────────────────────────
FLOW
─────────────────────────────────────────────────────────────
1. Run get-mobile-page-conversion-guide with the source page schema-name.
   - Check the returned sourceType. If it is not "freedom-web" (e.g. a Classic UI page) the
     tool reports it as not yet supported: convert the page to a Freedom UI WEB page first
     (classic-web -> freedom-web converter), then run this tool. Explain this to the user.
2. Read the guide. Present its summary to the user: the recommended template, what maps
   directly, what has a mobile alternative, what is UNSUPPORTED, and what REQUIRES A MANUAL
   DECISION. Resolve the unsupported / requiresManualDecision items WITH THE USER.
   — then STOP at Gate M (see GATES above): present the plain-language plan and do NOT proceed to
   step 3 until the developer explicitly approves. The user's initial request is not approval.
3. Create the target mobile page from recommendedMobileTemplate — ONLY after Gate M — (list-page-templates with
   schema-type "mobile" to confirm; create-page). The template provides the Scaffold root —
   do NOT add a second Scaffold. CAPTURE the schemaUId from the create-page result and pass it as
   target-schema-uid on every later update-page (see step 7): otherwise, when the chosen package is not
   the app's design package, update-page writes a REPLACING schema in the design package and leaves this
   mobile schema EMPTY — the Mobile app then loads the empty schema and crashes. (create-page returns
   willCreateReplacingInDesignPackage + designPackageUId when this split would happen.)
4. Build the mobile body (plain JSON: viewConfigDiff / viewModelConfigDiff / modelConfigDiff)
   by iterating elementMap. For each entry act on its operation:
   - merge — the element is provided by the mobile template (a "twin", e.g. Tabs→Tabs,
     FeedTabContainer→FeedContainer). REUSE the existing mobileName; do NOT insert it. (Insert
     vs merge is the #1 mistake — the template already contains these elements.) A merge entry MAY
     also carry a prebuilt mobileValues — paste it onto the merged element verbatim, deterministically,
     as part of this same step (no separate confirmation beyond Gate M — a mechanical property fill-in,
     not a new decision). A merge carries prebuilt mobileValues in two twin shapes:
       • whitelist twin — the rule declares carryProperties (e.g. FolderTree→FolderTreeActions carrying
         sourceSchemaName/rootSchemaName): only those keys are carried.
       • same-component twin — the mobile template provides the SAME component the page changed, either
         under a DIFFERENT name via a components mapping (AttachmentList→AttachmentFileList) or,
         AUTOMATICALLY, under the SAME name with no mapping needed (Feed→Feed). Its mobileValues carry
         ONLY what the page CHANGED from the web template — e.g. the attachments detail's recordColumnName
         (the object-specific link column), or Feed's dataSourceName/entitySchemaName. A property the page
         left at the web-template default is deliberately OMITTED so the mobile element keeps its OWN
         default (an unset attachments recordColumnName stays the mobile default RecordId); a template
         component the page did not change gets no elementMap entry at all. Paste the carried mobileValues
         as-is — never add the omitted defaults yourself; the mobile element already supplies them.
     If the mobile list template already provides the List / ListItem elements, configure
     them by MERGE-BY-NAME (the row goes on the ListItem element: title + body) — do NOT insert a
     second crt.List and do NOT put itemLayout inside a merge of the parent List (silent no-op;
     ListItem is a separate named element).
   - insert — add mobileType under parentName/propertyName (propertyName defaults to "items"). Use the
     entry's parentName VERBATIM — never substitute a parent the component "belongs in" by type or per
     get-component-info (see ELEMENT PLACEMENT IS AUTHORITATIVE in HARD MOBILE RULES).
     When elementMap[].index is present, add it to the insert op at that 0-based position VERBATIM
     (a positional element mapped above/below an anchor, e.g. above the mobile Tabs — or a converted
     web tab, below); otherwise omit index and append. On a tabbed record page EVERY web tab inserts
     as its OWN new mobile tab under Tabs (no general-tab collapse); the web wrapper's non-tab
     (side/profile) content fills the mobile general tab's grid, EXCEPT the profile island itself:
     it merges into the template's profile Area card rather than landing in that grid — its children
     go INSIDE that Area card, never directly into the general tab's grid, and it must NOT be left
     empty. Take both container names from guide.containerMap, which already carries the pair for the
     chosen template (e.g. SideAreaProfileContainer→AreaProfileContainer); do not assume a fixed
     pair. Tab ORDER is already deterministic: every converted web tab arrives with
     an explicit index (1, 2, … — right after the template's general tab), so applying the inserts
     verbatim yields general tab, converted web tabs, Feed, Attachments, with the template's
     FeedTab/AttachmentsTab staying last automatically — do NOT reorder tabs or invent indexes
     yourself.
     START from elementMap[].mobileValues: paste it as the component's values VERBATIM. It already
     carries the type and EVERY source property the mobile component supports — never drop any of
     them. It also already carries the CONVERTED event-binding requests (a button's `clicked`, a
     field's `valueChange`/`updated`): supported requests are kept (remapped when the mobile name
     differs). A component whose request the mobile app does NOT support is not inserted at all — it
     was already DROPPED (see the elementMap `drop` entry), so you never see it here. Do NOT re-add or
     hand-edit these bindings — paste mobileValues as-is. Then add ONLY
     what mobileValues deliberately leaves out:
       • the value binding (control, or value for lookups) — type-specific, so it is not prebuilt;
         (the row of a grid → crt.List insert is NOT one of these — see the next paragraph.)
     A grid → crt.List INSERT arrives with its row ALREADY BUILT: mobileValues carries the
     crt.ListItem under itemLayout (title = the first grid column, body = the rest) AND every source
     property the grid carried, each already shaped to what the mobile component accepts. Paste it as-is;
     do NOT rebuild the row and do NOT strip properties. This is prebuilt only for an INSERT — when the
     mobile list TEMPLATE already provides the List/ListItem elements, the row is still yours to
     configure by merge-by-name (see the merge branch).
     The mobileValues carry every localized string verbatim as #ResourceString(key)# tokens — both a
     top-level caption AND nested ones (e.g. config.title, text.template). Register them ALL: pass
     guide.resourceStrings (a { key: en-US text } map covering the whole converted body) to update-page
     `resources` in one call — do NOT register a #ResourceString(...)# token as the value, and do not
     hand-pick individual keys. A token whose key is not registered renders blank. Consult
     mobileContracts / get-component-info (schema-type "mobile") only
     for those not-prebuilt parts. validate-page is the backstop — it
     rejects an insert that drops a required property (e.g. a field caption, or a lookup-path
     attribute's type) and update-page refuses to save.
   - relocate-children — do NOT recreate this container; its children are placed in parentName
     instead (each child has its own entry whose parentName already points there).
   - drop — skip the element entirely (reason explains why: unsupported type, an unsupported button
     request, "empty container", or an "excludedComponents rule matched"). Tell the user what was dropped.
     Empty containers are already handled FOR you:
     a converter-created layout container whose every child dropped was removed deterministically by
     the converter and arrives as a drop entry with reason "empty container". WHICH container types are
     eligible is converter configuration, not a fixed list — read the drop entries rather than assuming
     one. Do NOT re-create such a container, do NOT re-parent anything into it, and do NOT ask the user
     about it — just report it with the other drops.
     An "excludedComponents rule matched" drop is a POSITIONAL exclusion the converter applied by rule
     (the reason names the removed type, the host type, and the host property when the rule scopes one —
     e.g. a search filter the rule excludes from an expansion panel's compact tools strip). It is NOT
     conversion loss: do NOT re-insert the component — not into that host, not
     anywhere else on the page — and do NOT ask the user whether to keep it. WHICH types are excluded
     from WHICH hosts is converter configuration, not a fixed list — read the drop reasons rather than
     assuming one. The same type OUTSIDE the excluded position converts normally, so seeing it dropped
     in one place and kept in another on the same page is correct, not an inconsistency. Just report it
     with the other drops.
     A positional exclusion emits a SECOND reason shape for everything that hung below the removed
     component: "parent removed by an excludedComponents rule: ancestor 'X' was removed and this element
     has no mobile parent left". Treat it exactly like the reason above — the element is gone because its
     parent is gone, so re-creating it would rebuild the branch the rule exists to remove. Do NOT
     re-insert it, do NOT re-parent it to a surviving container, and do NOT ask the user about it. Match
     an excludedComponents drop on BOTH shapes: a rule that targets a container type produces mostly this
     one, and the elements it names are the ones a user is most likely to ask about by name.
   For many→one suggestions (primaryWebMerge set, e.g. crt.FolderTree + crt.FolderTreeActions
   -> crt.FolderTreeActions), emit a SINGLE mobile component and merge in the secondary
   component's properties; do not emit the secondary as a separate component.
5. Apply the data sections — paste guide.modelConfigDiff and guide.viewModelConfigDiff VERBATIM as
   the page's modelConfigDiff / viewModelConfigDiff (see DATA SECTIONS below). Do NOT rebuild them
   by hand, and NEVER copy the data-source section from a pre-existing / reference body.
5b. Adaptive layout (when guide.adaptiveLayout is present): for every MULTI-column crt.GridContainer the
   guide has ALREADY baked both sides into mobileValues you pasted in step 4 — the container's per-breakpoint
   columns (small = 1, medium/large = the web columns) and each child's layoutConfig.adaptive (phone stacks
   in one column; tablet/desktop keep the web placement). A single-column grid gets no adaptive (the mobile
   client renders the plain layout). Nothing extra to apply — do NOT emit a separate merge for the
   container's adaptive (it is already inside the container's inserted mobileValues; a separate merge
   would duplicate the operation). Just PRESENT it to the user in plain language ("fields in <container>
   stack on the phone, keep <n> columns on a tablet — adjust?"); they may change it or decline.
5c. Tab body + Area (when guide.tabAreaLayers is present): every tab the CONVERTER creates already carries
   its synthesized inserts in the element map — the tab-body grid, then its Area card — because on
   mobile a tab's content lives in an Area card, not directly in the tab body. Each of that tab's
   top-level components (expansion panels included — a panel is an ordinary component here) already has
   parentName = the Area and a sequential single-column layoutConfig
   (a component the adaptive pass placed per breakpoint keeps that adaptive placement instead).
   Apply the inserts in element-map order (a parent always precedes its children) and do NOT reparent,
   reorder or re-place anything yourself, do NOT add an Area of your own, and do NOT touch a tab the mobile
   template provides (it arrives as a merge twin and gets no layers). The synthesized entries have no
   webName — they have no web counterpart. This structure is MANDATORY — do NOT ask whether to apply it,
   do NOT offer to keep the web structure instead, and do NOT treat it as a decision at the gate. STATE it
   in the plain-language plan as a fact ("the content of <tab> goes into one Area card, stacked in the web
   order"), the way you state which components transfer.
6. Validate the body with validate-page; resolve any findings (e.g. a binding whose attribute
   is not declared) before treating the page as done.
7. Persist with update-page — pass target-schema-uid=<create-page schemaUId> so the body lands in the
   created schema, not a replacing schema in the design package. Recreate the page-level business rules: for each
   guide.pageBusinessRules.convertedRules entry, pass its `rule` VERBATIM to
   create-page-business-rule on the MOBILE page (after the user approves). Surface any
   droppedRules to the user (they did not convert). Then tell the user to open the result in
   Freedom UI Mobile Designer for final layout review.

─────────────────────────────────────────────────────────────
COMPONENT CLASSIFICATION (5 categories — in componentSuggestions.category)
─────────────────────────────────────────────────────────────
- directMapping          : same component type exists on mobile — carry it over as-is.
- withAdaptation         : transferred, but layout/properties need adjusting.
- alternativeAvailable   : maps to a different mobile type (e.g. crt.Checkbox → crt.Toggle).
- unsupported            : NOT available on mobile; replace it or configure manually.
- requiresManualDecision : unknown/custom or ambiguous UX; decide with the user.

─────────────────────────────────────────────────────────────
DATA SECTIONS — modelConfigDiff / viewModelConfigDiff (paste, don't rebuild)
─────────────────────────────────────────────────────────────
Both metadata sections have IDENTICAL structural support in the mobile runtime, and the guide
already hands them to you as ready-to-paste diffs.

HARD RULE — NEVER source data-source attributes (modelConfigDiff) from a pre-existing or reference
mobile body. That is exactly how an attribute's "type" (e.g. ForwardReference on a related/lookup
column) gets dropped, and the binding then resolves to nothing in Mobile Designer ("Item with the
path … not found"). Always build modelConfigDiff from the guide. If a target page already exists,
DISCARD its data-source section and rebuild it from guide.modelConfigDiff.

- modelConfigDiff (guide.modelConfigDiff): paste it VERBATIM as the page's modelConfigDiff. It is a
  set of FOCUSED targeted merges (one per top-level key, e.g. ["dataSources"], plus a per-array
  override unioned with the mobile template's own natives) — NOT a single root merge, so the mobile
  diff engine cannot replace a data source's native array and drop entries. It carries the full
  modelConfig (data sources + attributes) with every attribute's "type" and "path" intact. Do not
  omit, rename, reconstruct, or collapse it back into one root merge. (Own columns that are not
  declared in attributes resolve automatically; only related/lookup-path columns are declared, and
  each MUST keep its "type".)
- viewModelConfigDiff (guide.viewModelConfigDiff): paste it VERBATIM as the page's
  viewModelConfigDiff. The guide ALREADY removed attributes referenced only by dropped/unsupported
  components. Converters: reference only OOTB mobile converters; a definitive mobile converter list
  is forthcoming — flag any custom converter for manual review.
- guide.modelConfig / guide.viewModelConfig are the same data in full-object form, for reference.

CHECKLIST before validate-page: confirm no insert dropped a property the mobile component supports
(you pasted mobileValues verbatim). validate-page enforces the critical ones — a data-source
attribute whose "path" contains a "." must keep its "type", and an inserted field must keep its
caption ("label"); both are errors that block update-page.

─────────────────────────────────────────────────────────────
HARD MOBILE RULES (see also get-guidance `mobile-page-modification`)
─────────────────────────────────────────────────────────────
- Mobile body is plain JSON with only viewConfigDiff / viewModelConfigDiff / modelConfigDiff.
- NO handlers, NO validators, NO custom converters in the mobile body.
- viewConfigDiff INSERTS address the slot by parentName + propertyName ONLY — never use "path" in a
  viewConfigDiff insert (e.g. NOT "path": ["tools"]; use "propertyName": "tools"). "path" is valid
  only in viewModelConfigDiff / modelConfigDiff; a viewConfigDiff insert that uses "path" is silently
  dropped by the differ.
- LIST ROW (grid → crt.List + crt.ListItem): the row lives on a crt.ListItem in the crt.List's
  itemLayout — title = the FIRST grid column, body = every other column in source order.
  For an INSERT the converter has already built the row into mobileValues; paste it, do NOT rebuild it.
  It is NOT prebuilt when the mobile list TEMPLATE already provides the List/ListItem elements: then
  configure the row by MERGE-BY-NAME onto the ListItem element (title + body). NEVER insert a second
  crt.List, and NEVER put itemLayout inside a merge of the parent List — crt.List is not a container and
  itemLayout is an input, so addressing it as a child slot makes the client answer "is not a container
  for other items" and the WHOLE schema fails to build (ListItem is a separate named element). When you
  build the row, a title is a plain "$Binding" STRING; the { "value": "$Binding" } shape is for body
  entries only — using it for the title renders an empty Title column while the body looks correct.
  A title binds only a DIRECT TEXT column of the collection's entity — a lookup column, or a
  ForwardReference projection of its display column, leaves the Title column empty. The converter does
  NOT select around this: the row leads with the first column whatever its type, so a grid whose first
  column is a lookup ships a title that renders as an empty Title column and nothing reports it. Tell the
  user when you see one, and set the row's leading value in the designer. The row still renders
  otherwise: body entries show as labeled value rows, lookups included.
- PAGE-level business rules ARE converted for you in guide.pageBusinessRules: each rule keeps
  its condition and only the actions that survive on mobile. Page rules carry ONLY element
  actions — hide / show / make-editable / read-only / required / optional — and an action
  survives only for the referenced elements whose component converts (set-values / apply-filter /
  apply-static-filter do not exist at page level). The condition ALWAYS converts verbatim — every
  operand type is supported in a mobile page-rule condition (attribute, const, formula, system-value,
  system-setting). Recreate each convertedRules[] entry by
  passing its `rule` VERBATIM to create-page-business-rule on the MOBILE page (after approval).
  droppedRules[] did not convert (every referenced element drops) — report them.
  OBJECT-/entity-level business rules are shared across web and mobile — do NOT re-create or touch them.
- REQUESTS (actions) on component event bindings (a button's `clicked`, a field's `valueChange`/`updated`)
  ARE handled for you. ONLY a `crt.Button` whose request the Creatio Mobile app does NOT support (and
  that does not remap to a supported one) is DROPPED (elementMap operation `drop`, reason names the
  request) — a dead button is not shipped. Other component types are NOT dropped for an unsupported
  request (some legitimately use a system request absent from the list): their binding is kept verbatim
  and flagged. A supported request is kept in
  elementMap[].mobileValues (remapped when the mobile name differs) — paste mobileValues verbatim.
  guide.requestConversions is the advisory summary (convertedRequests / flaggedRequests); dropped
  components appear in elementMap as `drop`. Tell the user which action components were removed.
  Page `handlers` (the web-only AMD section) are NEVER transferred — re-implement that behavior as entity-level business rules.
- ELEMENT PLACEMENT IS AUTHORITATIVE (scope: placing elementMap entries when building a page from
  get-mobile-page-conversion-guide — this rule owns per-page placement on a converted page; get-component-info
  stays authoritative for component SHAPE) — apply each elementMap entry's `parentName` + `propertyName`
  EXACTLY as the guide gives them, for EVERY component type. The guide already resolved the correct
  mobile parent for THIS page; that decision is final. NEVER relocate a component to a different parent
  because of its type, because get-component-info calls some other component its "typical parent" /
  "container" / lists it under "parent types", or because a component "usually" lives somewhere else.
  get-component-info describes a component's SHAPE in ISOLATION — it is generic and does NOT override the
  per-page placement in elementMap; when the two disagree, elementMap wins, always. Overriding the
  guide's placement (improvising a "better" parent) is the #1 cause of a component that renders but does
  not work. Worked example (illustration only — the parent is whatever the ENTRY names, never a fixed
  value): when the guide returns a quick filter with `parentName: HeaderContainer, propertyName: items`,
  insert it under exactly that parent — and under whatever parent the entry names in any other conversion.
  Do NOT relocate it into crt.QuickFilterGroup because get-component-info (mobile) calls crt.QuickFilterGroup
  the container for crt.QuickFilter. Mechanism (per the ENG-94937 investigation on Creatio Mobile — verify
  against your target platform version): crt.QuickFilterGroup is model-driven, so it builds its chips at
  RUNTIME from the `QuickFilterGroup_Value` attribute via `crt.QuickFilterGroupAttributeConverter` (driven by
  the `FilterGroupButton` in HeaderContainer); a crt.QuickFilter inserted as a static child of its `items` is
  never bound. Placement alone is necessary but NOT sufficient here: a working page ALSO needs that model side
  (the `QuickFilterGroup_Value` attribute + the converter's `target.items`). Confirm the guide's data-section
  diffs (guide.modelConfigDiff / guide.viewModelConfigDiff — apply VERBATIM, see DATA SECTIONS) carry that
  model side. If they do NOT, STOP and report it as an incomplete guide output (a converter gap) — do NOT
  hand-author modelConfigDiff / viewModelConfigDiff for it: inventing the `QuickFilterGroup_Value` attribute
  and the converter's `target.items` by hand is the deviation-from-tool-output this rule forbids, and their
  shape is defined nowhere in this guide. "Do NOT move the chip into crt.QuickFilterGroup" is about the VIEW
  tree — it is not a ban on the model-side wiring the OOTB page carries.
- RETARGET INTO A TEMPLATE-PROVIDED PARENT — INSERT ONLY THE CHILDREN, NEVER THE PARENT. When an
  elementMap insert RETARGETS an element into a container the mobile template ALREADY provides, the guide
  flags that entry with `parentExistsOnTemplate: true` and repeats the instruction in guide.constraints.
  Insert ONLY the flagged children into the named parent; do NOT insert, merge, or re-declare the parent
  container or its slot — the template supplies it, and authoring your own OVERRIDES the native one
  (wrong configuration, lost children). This is the single-element-slot / strip rule from get-guidance
  `mobile-page-modification` (a template-provided slot is merge-only and the merge is discarded when the
  slot is already filled) applied to conversion — that article owns the rule; this is only its
  conversion-time reminder. A guide that predates the flag omits it and the constraint: fall back to the
  same rule and never author a parent the mobile template already carries. And a source element INHERITED FROM
  THE WEB TEMPLATE (chrome the mobile template provides natively) is NOT retargeted at all — the guide drops it
  (reason names it "inherited from the web template"), because a duplicate would shadow the native element. A
  page-AUTHORED element (above the web-template baseline) is not chrome and DOES convert.
- ADAPTIVE LAYOUT (multi-column crt.GridContainer) is two-sided and the guide builds AND bakes both sides
  into mobileValues for you: the container's per-breakpoint columns (small = 1, medium/large = the web
  columns) and each child's layoutConfig.adaptive (small = single-column stack; medium/large = the web
  placement). A single-column grid gets NO adaptive — the mobile client renders the plain config. Just
  paste mobileValues verbatim; do not hand-build adaptive. The mobile runtime reflows children by
  `row` / `column`. adaptiveLayout is a PROPOSAL — let the user adjust or decline it at the gate.
- TAB BODY + AREA for every tab the CONVERTER creates is baked into the element map the same way, and
  unlike adaptiveLayout it is NOT a proposal: the tab body + Area card are the REQUIRED mobile
  structure for a converted tab — report it at the gate, never put it up for the user's approval, and
  apply the map as it is. What the layers are is described once in the tabAreaLayers field entry
  above; what to do with them, in FLOW step 5c.
- SOME PROPERTIES ARE NORMALIZED, NOT CONVERTED: for certain element types the converter writes the
  mobile standard instead of translating the web page's own value. Do NOT restore the web value and do
  NOT treat the difference from the web page as a defect. Like tabAreaLayers this is NOT a proposal —
  SILENT, never a gate question: state EACH standard as ONE aggregated line in the plan and the final
  report, and call out separately anything the standard could NOT be applied to, which keeps its web
  values. WHICH standards ran, WHICH elements and WHICH properties took part is converter configuration,
  read per conversion from guide.normalizations — described once in the normalizations field entry above.
- NEVER drop a property the mobile component supports. The guide already prebuilds each insert's
  values (elementMap[].mobileValues) by carrying every source property valid on mobile (per the
  registry) — paste it verbatim and add only the value binding. validate-page is the backstop and
  rejects an insert that drops a required property (e.g. a field's caption, or a lookup-path
  attribute's type), and update-page blocks the save.
- Mobile layout is a simplified vertical flow; complex multi-column desktop layout will likely
  need manual adaptation in the designer.

LIMITATIONS (be transparent)
This does not guarantee a pixel-perfect or behavior-perfect migration. It guarantees a
deterministic guide: the recommended template, container correspondence, classified components,
and mobile contracts. The result is a starting point that the user finishes in Freedom UI
Mobile Designer.
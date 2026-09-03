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
  - recommendedMobileTemplate + templateNote — the mobile template to create the page from. When the
    source page's web template matches no conversion rule, this is a GENERIC mobile base rather than a
    matched counterpart, and templateNote says so: no container or component name correspondence is
    known, so every element lands where the source tree puts it. Read the note before treating the
    recommendation as a pair, and review that page in the designer more closely.
  - containerMap — web→mobile container-name correspondence; use it to set each
    component's parentName to the correct mobile container.
  - sourceStructure — the full resolved component tree (incl. components inherited from the
    base template), with name / type / parentName / isContainer.
  - componentSuggestions — per source component TYPE: a category (directMapping /
    withAdaptation / alternativeAvailable / unsupported / requiresManualDecision), the
    suggested mobile type(s), and a primaryWebMerge note for many→one mappings.
  - elementMap — per NAMED ELEMENT, the exact instance-level decision (operation =
    merge / insert / drop / relocate-children). Iterate this to build the body; it already
    encodes merge-vs-insert, the mobile parent, survivability and caption resources. Every insert
    also carries `parentSource` — whether its parent is created by this map (`"page"` / `"converter"`),
    already exists on the target page (`"template"`), or is provided by NEITHER (`"unknown"`, which you
    must report rather than work around), see NEVER AUTHOR A PARENT THIS MAP DOES
    NOT CREATE in HARD MOBILE RULES. Do NOT
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
  - resourceStrings — every localized string the SOURCE PAGE DECLARES for the tokens the converted body
    references (top-level captions AND nested ones like config.title / text.template), keyed by resource
    name and resolved to its en-US text. Register this whole map via update-page `resources`. A key whose
    declared text is EMPTY is included on purpose — that is the page's own "no visible label", and
    reproducing it is what makes the mobile page match the web one.
    NOT EVERY #ResourceString TOKEN IN THE BODY HAS AN ENTRY HERE, and that is correct — do NOT invent
    one. The platform resolves a list column's caption from the entity column itself, so a page declares
    only the ones it RENAMED (its own MobilePageWithTabsFreedomTemplate references AttachmentListDS_Name
    and friends while declaring none of them). Inventing a key would REPLACE a localized column title
    with one hardcoded culture. If a token still renders raw on the device, the fix is the entity column
    or the source page's resources — not a key added here.
  - dataSectionConflicts — one entry per template-owned data-section value the page changed that
    NEITHER diff can express, each with its `section` (which diff to hand-edit), `path` (segments),
    `entry` (the array element's name, when it has one) and `kind`. Null when there are none, which is
    normal. READ THE KIND — the three do not share an outcome and two need OPPOSITE remedies:
    `changed-named-element` and `changed-scalar` mean the page's value is NOT applied and the template's
    wins (if the page's value must win, edit that entry in the diff by hand before pasting), while
    `nameless-changed-in-place` drops NOTHING — the page's element is inserted and will DUPLICATE the
    template's own at runtime, so remove one of the two. Treating them as one warning sends you to the
    wrong fix. Report them at the gate; none is silently absorbed.
  THERE IS NO `diagnostics` FIELD, and none of its four codes survives — do not look for it, and do not
  read its absence as "nothing to weigh". Each went somewhere:
    - a twin with no prebuilt delta → that entry's own `reason` codes. See REASON CODES below.
    - the root-merge fallback → the cause that mattered now REFUSES the conversion (see DEGRADED CASE).
      Detect the benign remainder structurally: a data-section diff that is one op with `path: []`.
    - the two rules-file codes → clio's CI, for whoever authored the typo. You get no signal, because
      you could never fix a published rules file from here.
  There is deliberately NO prose section in the guide — no constraints, no diagnostics, no nextSteps.
  The standing rules are in THIS article and are enforced by validate-page / update-page; the ordered
  flow is the FLOW section below and the conversion skill's gated steps. The guide carries facts about
  the page in front of you, and nothing that would fire the same way on every conversion. If a guide you
  are handed still has those arrays, it predates this and you may read them — but this article wins.

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
     FeedTabContainer→FeedContainer, GeneralInfoTab→GeneralInfoTab,
     GeneralInfoTabContainer→GeneralTabContainer). REUSE the existing
     mobileName; do NOT insert it. (Insert
     vs merge is the #1 mistake — the template already contains these elements.) A merge entry MAY
     also carry a prebuilt mobileValues — paste it onto the merged element verbatim, deterministically,
     as part of this same step (no separate confirmation beyond Gate M — a mechanical property fill-in,
     not a new decision). Concretely: EMIT A MERGE OPERATION in viewConfigDiff on that mobileName with
     those values. mobileValues is NOT an insert-only concern — a merge is the only way some values reach
     the page at all. An anchor the converter moved down a row, to make room for content placed above it,
     arrives this way: its whole new layoutConfig lives only in the merge's mobileValues, so skipping
     that merge silently reproduces the misplacement and nothing reports it. Two twin shapes:
       • whitelist twin — the rule declares carryProperties (e.g. FolderTree→FolderTreeActions carrying
         sourceSchemaName/rootSchemaName): only those keys are carried.
       • same-component twin — the mobile template provides the SAME component the page changed, either
         under a DIFFERENT name via a components mapping (AttachmentList→AttachmentFileList) or,
         AUTOMATICALLY, under the SAME name (Feed→Feed) — the automatic route also requires the element
         to be INHERITED FROM THE WEB TEMPLATE; a page-authored element merely sharing a name and type
         stays an `insert`, keeping its parent, index, caption and bindings. Its mobileValues carry ONLY
         what the page CHANGED from the web template — the attachments detail's recordColumnName, or
         Feed's dataSourceName/entitySchemaName. A property left at the web-template default is
         deliberately OMITTED so the mobile element keeps its OWN default (an unset recordColumnName
         stays the mobile default RecordId). Paste mobileValues as-is; never add the omitted defaults.
         A template component the page did NOT change still gets an entry — an advisory `merge` with
         `mobileValues: null` — so a page business rule targeting it still converts. That entry is not
         necessarily work: its `reason` code says which of the twin cases it is (see REASON CODES).
     If the mobile list template already provides the List / ListItem elements, configure
     them by MERGE-BY-NAME (the row goes on the ListItem element: title + body) — do NOT insert a
     second crt.List and do NOT put itemLayout inside a merge of the parent List (silent no-op;
     ListItem is a separate named element).
   - insert — add mobileType under parentName/propertyName (propertyName defaults to "items"). Use the
     entry's parentName VERBATIM — never substitute a parent the component "belongs in" by type or per
     get-component-info (see ELEMENT PLACEMENT IS AUTHORITATIVE in HARD MOBILE RULES).
     When elementMap[].index is present, add it to the insert op at that 0-based position VERBATIM
     (a positional element mapped above/below an anchor, e.g. above the mobile Tabs — or a converted
     web tab, below); otherwise omit index and append. On a tabbed record page every web tab the PAGE
     authored inserts as its OWN new mobile tab under Tabs. The web TEMPLATE's own
     general-information tab is the exception: it is a MERGE twin (GeneralInfoTab→GeneralInfoTab), so
     no second general tab is ever inserted, and its content grid is a separate merge twin
     (GeneralInfoTabContainer→GeneralTabContainer). Content lands where the WEB page put it — in
     GeneralTabContainer if the page kept that grid, in GeneralInfoTab if it removed it. Both are
     valid receivers: take parentName as given and do not normalise one shape into the other.
     The web card wrapper's non-tab (side/profile) content fills the mobile general tab's content GRID
     (CardContentWrapper→GeneralTabContainer), EXCEPT the profile island itself:
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
     The mobileValues carry every localized string verbatim as #ResourceString(key)# tokens. Pass
     guide.resourceStrings to update-page `resources` in ONE call, exactly as given — do not hand-pick
     keys, do not register a #ResourceString(...)# token as a value, and do not add keys the map omits
     (see the resourceStrings field above for why an omission is correct). Consult
     mobileContracts / get-component-info (schema-type "mobile") only
     for those not-prebuilt parts. validate-page is the backstop — it
     rejects an insert that drops a required property (e.g. a field caption, or a lookup-path
     attribute's type) and update-page refuses to save.
   - relocate-children — do NOT recreate this container; its children are placed in parentName
     instead (each child has its own entry whose parentName already points there).
   - drop — skip the element entirely; its `reason` codes say why. Tell the user what was dropped.
     `drop-empty-container` is already handled FOR you: a converter-created layout container whose every
     child dropped was removed deterministically. Do NOT re-create it, do NOT re-parent anything into it,
     and do NOT ask the user about it — just report it with the other drops.
     `drop-excluded-by-rule` is a POSITIONAL exclusion the converter applied by rule; its params name the
     removed type, the `hostType`, and the `slot` when the rule scopes one (e.g. a search filter excluded
     from an expansion panel's compact tools strip). It is NOT conversion loss: do NOT re-insert the
     component — not into that host, not anywhere else — and do NOT ask whether to keep it. The same type
     OUTSIDE the excluded position converts normally, so seeing it dropped in one place and kept in another
     on the same page is correct.
     `drop-parent-excluded` covers everything that hung below such a component (`ancestor` names it).
     Treat it identically — the element is gone because its parent is gone, so re-creating it would rebuild
     the branch the rule exists to remove. Match an exclusion on BOTH codes: a rule targeting a container
     type produces mostly the second, and the elements it names are the ones a user asks about by name.
     WHICH types are excluded from WHICH hosts is converter configuration, not a fixed list — read the
     codes rather than assuming one.
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
- WHAT THE DIFFS CANNOT CARRY — no operation in the mobile vocabulary edits an existing array element
  IN PLACE: the path applier matches elements by `_id` while these config elements are keyed by `name`,
  so a name-addressed merge has no `_id` to resolve and an insert would duplicate the name. So when the
  page changes a value the mobile template already owns, the converter lets the template win and reports
  the loss in guide.dataSectionConflicts rather than shipping a silently lossy body. Read each entry's
  `kind` (see the field above) — a changed named element or a changed collection scalar loses the page's
  value, while a nameless element edited in place loses nothing and duplicates instead.
- DEGRADED CASE — a diff can only be targeted when there was a base to diff against. An UNOBTAINABLE
  template no longer degrades the guide: the tool REFUSES with `success: false` and an error naming the
  cause and its fix — a named-but-unreadable MOBILE template or an unreadable WEB template are
  environment checks (the mobile package / the source package), while no mobile template named at all is
  a rules-file fix (`templates` entry or `defaultMobileTemplate`) that re-running cannot help. Never
  convert around a refusal: each of those states ships inserts DUPLICATING elements the template
  provides.
  What remains is the BENIGN root merge — a template read successfully that simply declares no such
  config section. Deliberately not reported, because a base owning nothing there has nothing to lose.
  Detect it structurally (one op with `path: []`) and check it only because a root merge REPLACES arrays
  wholesale: any array the mobile template also owns (a data source's own sort/filter array, or
  Items.modelConfig.filterAttributes' built-in QuickFilterGroup_Filters on BaseMobileListTemplate) loses
  entries. There is no diagnostic to read for this, and its absence is not reassurance — look at the
  `path`.

CHECKLIST before validate-page: confirm no insert dropped a property the mobile component supports
(you pasted mobileValues verbatim). validate-page enforces the critical ones — a data-source
attribute whose "path" contains a "." must keep its "type", and an inserted field must keep its
caption ("label"); both are errors that block update-page.

─────────────────────────────────────────────────────────────
REASON CODES — elementMap[].reason is a LIST of {code, params?}, never prose. Every code, what it
means and what (if anything) to DO about it: get-guidance `freedom-page-mobile-reason-codes`. Load it
once per run when you are about to read elementMap; branch on `code`, read `params` for the values,
and REPORT an unrecognised code instead of guessing at it.

HARD MOBILE RULES (see also get-guidance `mobile-page-modification`)
─────────────────────────────────────────────────────────────
- Mobile body is plain JSON with only viewConfigDiff / viewModelConfigDiff / modelConfigDiff.
- NO handlers, NO validators, NO custom converters in the mobile body.
- USE ONLY MOBILE-REGISTERED COMPONENT TYPES (get-component-info schema-type "mobile"). The converter
  never hands you one that is not: a source component whose type is absent from the mobile registry is
  DROPPED with reason code `drop-type-not-in-mobile-registry`. So this rule only ever binds a
  type YOU introduce. validate-page reports a deviation rather than blocking it, because a custom mobile
  component registered in your own package is legitimately absent from the registry — so read WHICH of
  the two diagnostics you got: a type that exists in the WEB registry but not the mobile one almost
  always has a mobile alternative to look up instead, while a type in NEITHER registry is either your
  own registered custom component (ignore it) or a misspelled / invented type that will not render.
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
- NEVER AUTHOR A PARENT THIS MAP DOES NOT CREATE — read `elementMap[].parentSource`. Every insert that
  names a parent carries it, and it has four values: `"template"` means nothing in the element map creates
  that parent AND the probed mobile template does provide it (`MainContainer`, or `FloatingActionButton`
  via the Scaffold's `floatAction` slot); `"page"` and `"converter"` mean the parent IS inserted by this
  map, by its own entry, having come from the source page or been synthesized by the converter
  respectively; `"unknown"` means NEITHER provides it.
  For `"template"`: never author, recreate or duplicate the parent ELEMENT — your copy OVERRIDES the
  native one (wrong configuration, lost children). Two things this does NOT forbid: the parent's own
  `merge` entry, if the map carries one, must still be applied (that is how a container's per-breakpoint
  `columns` and a shifted `layoutConfig` reach the page — FLOW step 4); and when the parent does not yet
  carry the SLOT you insert into, the two-step idiom from get-guidance `mobile-page-modification` applies
  — `merge` declaring the empty slot, then the insert — because an insert into a property the element
  lacks THROWS (`menuItems` on `crt.FloatingActionButton`, which this converter emits). The
  single-element-slot rule that article owns forbids REPLACING a filled template slot (the merge is
  discarded when the slot is already filled), not initializing an absent one; this is only its
  conversion-time reminder.
  For `"unknown"`: STOP and report the parent name. Inserting into it throws and authoring it may
  duplicate something the template owns under another name. It is a conversion-RULES defect — a
  `containers` mapping naming a container the target template lacks — not yours to work around.
  An older guide carries a retarget-only `parentExistsOnTemplate: true`
  boolean instead, and it was NOT stamped on an ordinary insert into a template-provided parent: on such a
  guide do not read its absence as "safe to author", fall back to the same rule and never author a parent
  the mobile template already carries. And a source element INHERITED FROM
  THE WEB TEMPLATE (chrome the mobile template provides natively) is NOT retargeted at all — the guide drops it
  (reason code `drop-inherited-chrome`), because a duplicate would shadow the native element. A
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
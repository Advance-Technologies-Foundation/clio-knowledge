clio MCP process-preconfigured-page guide — the Pre-configured page element (PreconfiguredPageUserTask)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Pre-configured page element: the page facts you must read
before building one, the completing buttons, the data sources and the record they carry, the performer and
its showPage rule, and what a later edit re-synchronizes. A rule that lives in another article is cited by
its article NAME and never repeated here, so a name in backticks is a get-guidance topic to fetch, not a
section to scroll to.

== Element: Pre-configured page (preconfiguredPage -> PreconfiguredPageUserTask) ==
- `preconfiguredPage` — Pre-configured page: shows a Freedom UI page to a user and resumes when the user
  presses a completing button. It is the only page element that can hand a user a purpose-built page.
  CRITICAL: the page's buttons and data sources are FACTS you must read first, not values you may invent —
  a page inherits its buttons from its template chain, so they are only knowable from the merged page and
  the server cannot see them. Call `get-process-page-facts --schema-name <page>` and pass its
  `completingButtonCandidates` / `dataSources` entries through unchanged.
    * `{"type":"preconfiguredPage","name":"ApproveRequest","caption":"Approve the request","preconfiguredPage":{
      "page":"UsrRequestReview_FormPage",
      "performer":{"type":"user|manager|role","contact":"…","role":"…","showPage":true},
      "buttons":[{"name":"SaveButton","caption":"Save | SaveButton","event":"clicked","validate":true}],
      "dataSources":[{"name":"PDS","entitySchemaName":"UsrRequest"}],
      "recommendation":"single line"}}`
    * NO SUITABLE PAGE? The element is NEVER built without one, and the server NEVER creates a page for you —
      a build naming a page that does not exist is refused, and so is one naming a Classic UI page. Creating the
      page is a SEPARATE, EARLIER step: propose it to the user (name + which fields it needs), and once they
      agree use the existing create-page flow, then build the element referencing the new page. Do not create a
      page unprompted: it is a lasting artifact in the user's app, it must be named to the no-code standards, and
      only the user knows which fields belong on it. When the only candidates are Classic UI pages, say so and
      recommend a new Freedom UI page — but leave the choice to the user.
    * AT LEAST ONE completing button is REQUIRED on a BUILD and is not defaulted for you — the server now
      REFUSES a new Freedom UI element whose `buttons` is missing or empty, naming the field. The visual
      designer leaves a newly discovered button unselected, so an element built without one would pass
      validation in the designer and then hang forever at run time, which nothing downstream catches.
      Choose from the candidates the facts tool returned. (On `setElement` the same omission still means
      "keep the buttons the element already has" — the refusal is build-only.)
    * `performer` OMITTED on a build defaults to the CURRENT USER — the server writes the same performer the
      designer's card does for a new element, so you do not have to send one. Send it when the task belongs
      to somebody else.
    * `showPage` is accepted ONLY for a `user` performer. The runtime ignores it when the task runs for
      somebody else or in background mode, so it is refused for `role`/`manager` rather than stored inert.
      Omit it and the page IS still shown automatically: that is the task's own default and the designer
      leaves it inherited, so DO NOT send `showPage: true` to "make sure" — sending it stores a value where a
      designer-built element stores none.
    * `recommendation` must be a single line — a line break is rejected, because the platform renders the
      text as one line regardless of syntax.
    * The page's PARAMETERS are deliberately absent from the descriptor: the server reads them from the page
      itself and copies them onto the element, where they can then be set or mapped like any element
      parameter. They are bidirectional — a value you set pre-fills the page, and what the user entered is
      readable downstream.
    * WHICH RECORD did the user save? Each data source gets its OWN element parameter carrying that record's
      id, and `describe-business-process` reports it inside `preconfiguredPage.dataSources` as
      `{name, entitySchemaName, parameter}` — take the name from `parameter` (it looks like
      `DataSource_PDS_Id`) and NEVER compose it yourself. It is bidirectional like the page parameters: map
      something INTO it and the page opens on that record for editing; read it AFTER the step and you have the
      record the user saved. It does NOT appear in the element's own `parameters` list — that list carries
      only results, outputs and values already set, and this one is filled at run time — so `dataSources` is
      the only place it surfaces. An element built without `dataSources` has no such parameter at all.
    * Change it later with `setElement` → `elementUpdate.preconfiguredPage`; every field is optional there.
      OMITTING `buttons` or `dataSources` means "leave them alone", NOT "the page has none" — with ONE
      exception: changing `page` TO a Freedom UI page REQUIRES `buttons` in the same call. The stored buttons
      name the PREVIOUS page's buttons, so the operation is refused rather than carried across — re-read
      `get-process-page-facts` for the new page first. Changing `page` to a Classic UI page is refused
      outright: the contract cannot configure one. An element that already references a Classic UI page keeps
      that reference (re-asserting the SAME page is fine) and is edited within the fields both page types share. `dataSources` have no removal path through this contract,
      so a page change carries the previous page's data-source parameters forward as inert leftovers.
    * RE-SYNC: any `setElement` touching the element re-reads the page and reconciles its parameters —
      added ones appear, values and mappings for unchanged ones survive, a renamed parameter keeps its
      value, and a parameter whose data type changed loses its value and is reported. This mirrors the
      designer refreshing the element when its card is opened. `describe-business-process` reports
      `preconfiguredPage.inSync` and never fixes drift itself.
    * `inSync: true` means "nothing left to synchronize", NOT "the element carries every page parameter". A page
      parameter whose NAME collides with one the element already owns (its own `Title`, `Buttons`, a
      `DataSource_*`) is SKIPPED — it is never copied onto the element, and a re-sync would skip it again, so it
      is correctly not drift. Read `preconfiguredPage.shadowedPageParameters` beside `inSync`: anything listed
      there is NOT reachable, and a mapping naming it would silently hit the element's own parameter instead.
      The fix is on the page — rename the parameter there. Both fields go NULL together when the page's
      parameters could not be read: `null` is "unknown", never "nothing is shadowed".
    * DRIFT IS REPORTED as `message-type: "Warning"` entries in `execution-log-messages` — on a MODIFY and on a
      BUILD alike. There is NO separate `warnings` field on the response, so looking for one and finding nothing
      is not evidence there were none. A cleared value, a removed parameter, a rename, and a page that could not
      be read each raise one line naming the element and the parameters. Read them: the operation SUCCEEDS, so a
      mapping invalidated by a page change is visible ONLY there. A page that could not be read removes NOTHING —
      the element keeps a stale parameter list rather than a pruned one, and the line says so.
    * Creating a Pre-configured page on a CLASSIC UI page is not supported. An element that already
      references one keeps it: `describe-business-process` reports it (including `connectedObject` and
      `connectedObjectRecord`) and never rewrites it, and `setElement` is limited to the fields both page
      types share. Say so rather than silently switching page type.

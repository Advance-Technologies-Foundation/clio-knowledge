clio MCP process-modeling guide — design Creatio business processes (BPMN)

== How clio builds processes (read first) ==
- clio makes no LLM call. You own the intent->BPMN translation: decide which elements the process
  needs, their parameters, and how they connect. The server-side ProcessDesignService package owns
  metadata serialization — you NEVER hand-author process metadata, filters, or column mappings.
- The build is DECLARATIVE: you describe the process (elements + flows + parameters + mappings) and
  clio builds + saves it in one call. Diagram layout is automatic (start leftmost, end rightmost, no
  overlap) — do not set positions.
- Tools:
  * list-user-tasks         — the user-task palette (name + uid); pass a name as `userTaskName`.
    CAVEAT: it lists RETIRED schemas as equal peers with no marker — `CallUserTask`, `EmailUserTask` and
    `SendEmailUserTask` are all still returned. And TWO shipped schemas share the caption "Send email":
    `EmailTemplateUserTask` is the live one (17 connections, creates an Activity), `SendEmailUserTask` has
    none and creates nothing. Always key on the schema NAME the tool returns, never on a caption.
  * create-business-process — build a NEW process from a JSON descriptor, and save it.
  * modify-business-process — edit an EXISTING process by an ordered list of operations.
  * describe-business-process        — read a process back as a structured graph (verify / explain).
    Also returns, per element: `connections[]` (bound "Connected to" links, raw + decoded), `deprecated`
    (the user-task schema is retired), and `writesConnectionsAtRuntime` — where FALSE is the answer that
    matters: it marks a process whose connections persist and compile while writing nothing. FALSE has two
    causes, fixed differently: the user task's runtime never writes them (change the element kind), or this
    element's activity-creation gate is shut (set `CreateActivity` true). `null` means not established — not
    false, and NOT permission: a non-user-task element, an unresolvable user-task schema and a user task
    outside the supported six all report `null`. `setConnections` is refused on `false` AND on `null`; only
    `true` means it is accepted.
  * validate-process-graph  — pre-check a planned graph against the connection rules R1-R17.

== What you can build today (create-business-process) ==
- NOT in a build descriptor: the "Connected to" links of an Activity a task creates. Add the element
  first, then bind them with `modify-business-process` → `setConnections` (see "Activity connections").
- Events: `startEvent` (Simple start), `signalStart` (record signal: add/modify/delete), `endEvent`.
- Activities: `userTask` referencing any task from list-user-tasks via `userTaskName`
  (aliases `readData`->ReadDataUserTask, `changeData`->ChangeDataUserTask, `performTask`->ActivityUserTask).
  A `readData` element is CONFIGURABLE via its `readData` block — source object, first-record mode, result
  columns, sort, plus a record `filter` (see the "Read data element" section below). A `changeData` element
  is CONFIGURABLE via its `changeData` block — target object + column values, plus a record `filter` (see
  the "Modify data element" section below). CAVEAT: Add data and Delete data still place an UNCONFIGURED
  element — their target object and values cannot be set yet, so those steps do nothing useful until a
  human configures them in the designer. Say so when you use one; do not present such a result as a working
  data operation.
- Send email: `sendEmail` (the Send email element / EmailTemplateUserTask), CUSTOM MESSAGE only (email
  TEMPLATES are not supported — say so if the user asks for one). The `email` block configures everything:
  `{ "name": "SendWelcomeEmail", "type": "sendEmail", "caption": "Send the welcome email", "email": {
     "mode": "auto"|"manual", "sender": "<MailboxSyncSettings record id OR a sender email address configured
     on the environment>", "subject": "plain text", "body": "<html>…</html>", "bodyFormat": "html",
     "to"/"cc"/"bcc": [ one of {"value": "a@b.com"} | {"processParameter": "<Name>"} |
       {"expression": "[#…#]", "referenceSchema": "Contact"} , … ],
     "importance": "none"|"normal"|"high"|"low", "ignoreErrors": true|false,
     "performer": { "type": "user"|"manager"|"role", "contact"?: "<formula; defaults to the current user's
       contact>", "role"?: "<SysAdminUnit role name or record id>", "showPage"?: true|false } } }`.
  Rules: `mode:"auto"` sends automatically and its `sender` is required AT RUN TIME, not to save — it is NOT
  a design-time required field: the server saves without one, the designer's card validates `Sender` only
  while auto mode is selected (any filled formula satisfies it), and the field whose absence blocks saving a
  Send email element is `BodyTemplateType`, not `Sender`. With no resolvable sender the RUN fails with
  `Terrasoft.Mail.Sender.EmailException: Sender is not specified` — UNLESS the `SkipSenderValidation` feature
  flag is on, where the identical setup completes. So configure a `sender` for `auto`, but do NOT report a
  missing one as a save-time error. Verified against the platform's own acceptance tests —
  `process_elements_validation.feature` (the element's validation field is `BodyTemplateType`) and
  `exchange_process_send_error_v2.feature` (RND-T26743: auto mode with `Sender` = a `Guid.Empty` formula
  SAVES with no validation dialog and fails only at run time; RND-T26744 `@ft_SkipSenderValidation`: the same
  setup completes) — plus the card's auto-mode-only `senderValidator`.
  `mode:"manual"` creates an email activity for the `performer` (manual-only; `type:"role"` requires `role`).
  A `processParameter` recipient mirrors that parameter's type — a Contact-lookup parameter is resolved to
  the contact's email at send time; an entity-COLUMN recipient is reachable IN THIS CONTRACT only as a raw
  `expression` formula — a CONTRACT limit, not a platform one: the designer's own recipient menu offers
  Contact/Account lookups, the current-user contact, a system setting and a formula (designer specimen
  capture), so say "not through this tool yet", never "Creatio cannot".
  A SYSTEM SETTING is reachable today and is the RIGHT default for an address that belongs to a team rather
  than a person (an HR inbox, a support alias): send the recipient as an `expression` whose formula is
  `[#SysSettings.<Code>#]` — e.g. `[#SysSettings.UsrHrNotificationEmail#]`. Prefer it over a literal address
  and over a named Contact: the setting is what an administrator can change afterwards without reopening the
  process, while a hard-coded address silently keeps mailing the old destination. Discover the code with
  `list-sys-settings` (pass `search-pattern` — the unfiltered catalog is hundreds of rows), and create the
  setting with `create-sys-setting` when it does not exist yet rather than falling back to a literal. When a
  request names a recipient by ROLE rather than by address ("notify HR", "tell support"), treat a system
  setting as the expected answer and offer it explicitly — an option set of literal / contact / parameter
  omits the one source that survives a change of staff. The HTML body is stored verbatim;
  `bodyFormat` accepts ONLY `"html"` — any other value is REJECTED at build even when no `body` is sent (the
  applier validates the format first, so it is a contract guarantee, not a convention). VERIFIED on a stand
  (2026-08-13, a `CrtProcessBuilder` that supports `sendEmail`): `bodyFormat:"text"` and `bodyFormat:"markdown"`
  both FAIL the build with `Send email element '<name>': 'bodyFormat' must be 'html' (only HTML custom-message
  bodies are supported). Got '<value>'.` — and the `markdown` case carried NO `body` at all, which is the half
  that proves the format is checked on its own rather than only alongside a body. To put PROCESS DATA in the
  body, author BY NAME with friendly macros the server resolves into the platform's
  `<img data-value="[#…#]">` image tokens — NO UID needed: `[[param:<Name>]]` (a whole process parameter),
  `[[element:<ElementName>.<OutputParameter>]]` (a whole element output, e.g. a `readData` element's
  `ResultEntity`), and `[[element:<ElementName>.<OutputParameter>.<Column>]]` (ONE direct column of that
  output record). A LOOKUP column in a body macro renders the referenced record's **Id**, not its display
  value: `[[element:Read.ResultEntity.Job]]` mails `11d68189-…`, and an EMPTY lookup mails
  `00000000-0000-0000-0000-000000000000` rather than a blank. This is a PLATFORM limit, not a contract one, and
  it cannot be worked around by drilling deeper — the token is one column deep by construction and BOTH deeper
  routes are refused by core, verified 2026-08-21: a chained `[EntityColumn:{…}].[EntityColumn:{…}]` is read
  only to its LAST segment and resolved against the ROOT schema, and a chained meta path in a `readData`
  element's `EntityColumnMetaPathes` is REJECTED on save (`Column with identifier "<uid>" not found in the
  entity schema "<root>"`). So do NOT put a lookup column in a body a human reads. Email a TEXT column that
  carries the same information instead — on `Employee`, `FullJobTitle` mails `Developer` while `Job` mails a
  GUID — and when only the lookup exists, say the value cannot be rendered rather than shipping an Id.
  Reviewing this needs a SENT message: the schema validates, the process runs green, and no macro is left
  unresolved, so every check short of reading the delivered email passes. A process parameter can only be
  inserted WHOLE — Creatio has NO column drill on a bare
  parameter (verified: zero specimens of `[Parameter].[EntityColumn]` without an `[Element]`; the designer
  offers column drill only on the Elements tab), so to email a record's column read it with a data element
  FIRST and drill THAT output. An unknown parameter/element/column is REJECTED naming what was missing, so
  DISCOVER exact names with `describe-business-process` (or define the parameter) first — do not guess; column
  names are matched case-sensitively. A whole raw `<img data-value="[#…#]">` token (or a bare `[#…#]` formula)
  written by hand passes through unchanged (the escape hatch). NOTE `{{…}}` is NOT clio macro syntax — that is
  the content designer's editable template fields (`{{#index::Title#}}`, New String/Text/Picture/Color), a
  different, design-time feature that is not process data. `importance` has NO `medium` token: the designer LABELS
  `normal` as "Medium" (its caption in the element's card — the product's acceptance tests assert `EN=Medium`),
  so a user's "medium importance" is `normal`. A formula SUBJECT goes through `mappings` against the element's
  `Subject` parameter instead of `email.subject`. Sending BOTH is accepted and does NOT merge — they write the
  same parameter, so the LAST write wins, and which one that is depends on the PATH: in a BUILD the
  descriptor's `mappings` are applied BEFORE the elements' `email` blocks, so `email.subject` overwrites the
  mapped formula whatever order you wrote them in; in a MODIFY the operations run strictly in the order you
  list them, so the LATER of `addMapping` / `setElement`(`email.subject`) wins. Deterministic on each path but
  opposite by default, so send exactly ONE of the two rather than relying on it. This is now a STATED CONTRACT
  rather than an observed implementation order: the server's `email.subject` member documents both paths, and
  two tests pin them — a build asserting the mapping phase runs before the email block, and a modify asserting
  operations dispatch in array order — so reordering either phase is a breaking change that fails the suite
  instead of silently inverting this guide.
  Works in `create-business-process`, `modify-business-process` `addElement` (same block) and `setElement`
  (`elementUpdate.email` — an in-place partial update). Recipients are MATCH-OR-APPEND: an entry whose
  resolved source and value already match an existing line under the same prefix is a NO-OP (re-application
  is idempotent now — older builds appended a duplicate), a genuinely new address APPENDS, and there is NO
  removal path THROUGH THIS TOOL — a wrong recipient cannot be replaced or removed through `modify`.
  The DESIGNER can remove one, so route a removal request there and never say Creatio cannot do it: clearing a
  recipient's value and saving DELETES the parameter (`saveRecipients` calls `removeRecipient` on an emptied
  row, which calls `removeParameter`, which removes it from the element). Two exceptions persist as valueless
  parameters instead — the LAST `To` row (the guard keeps one To row alive), and a parameter something else
  still references (`canRemoveParameter`). That last-`To` case is why a designer capture can show an unfilled
  recipient row surviving; it is a special case, NOT evidence that removal is impossible.
  VERIFIED on a stand (2026-08-13): the SAME `to:[{"value":"…"}]` entry applied three times over `setElement`
  left exactly ONE recipient parameter, and a different address then appended as a second — so "idempotent" is
  measured behaviour here, not an inference from the applier's source. The tool's no-removal half is a
  limitation of the operation set (there is no removeRecipient op), not a platform limit — the designer
  behaviour above is read from `EmailTemplateUserTaskPropertiesPage.js` in `CrtProcessDesigner` 7.8.0
  (`saveRecipients` :645, `removeRecipient` :1410, `removeParameter` :1390).
  `describe-business-process` reads the configuration back as the element's `email` block: `hasBody` is a
  presence flag, and `body` echoes the HTML with the process-macro tokens DECODED back into the same
  `[[param:…]]` / `[[element:…]]` author form — so on a MODIFY you can read the current body and edit it in
  place. A macro whose UIds no longer resolve to names is left as the raw `<img>` token (best-effort decode).
- Sequence flows; process-level parameters (with an optional constant default value); element-parameter mappings.
- `useBackgroundMode` on any element that OFFERS it (it is not signal-specific, but neither is it universal —
  four element kinds REMOVE the control outright, so a rule of the form "tick it on every element" states an
  impossible requirement). Verified against the designer's own property pages (`CrtProcessDesigner`,
  2026-08-21): `ProcessTerminateEventPropertiesPage`, `ProcessTimerStartEventPropertiesPage`,
  `IntermediateThrowMessagePropertiesPage` and `SendEmailUserTaskPropertiesPage` each apply a schema-diff
  `remove` operation against the background-mode control; a Terminate element therefore CANNOT be put in
  background mode and its `false` is correct, not an oversight. `EmailTemplateUserTask` — the `sendEmail`
  element kind — INSERTS the control and so does take the flag; do not confuse it with `SendEmailUserTask`,
  which does not. For a SIGNAL-STARTED process set the flag on every element that offers it — the trigger fires
  with no one waiting at a screen, so there is nothing for inline execution to return to. The designer gates the control on
  `canUseBackgroundProcessMode()` = the `UseBackgroundProcessMode` feature enabled AND the schema not embedded,
  so on an environment with that feature off the control is absent everywhere and there is nothing to set;
  change it later on an EXISTING element with the `setElement` op
  (`{ "op": "setElement", "elementName": "NotifyAccountOwner", "elementUpdate": { "useBackgroundMode": false } }`):
  `true` runs it asynchronously via the background scheduler, `false` inline. OMIT it to keep the element
  kind's own default, which mirrors the visual designer's palette — a `signalStart` defaults to background
  mode, so a signal-started process runs asynchronously and its effects appear a moment after the record is
  saved. The platform ANDs the flag with the global `UseBackgroundProcessMode` setting (on by default), so
  with that setting off background mode is inactive regardless — and since the platform then does not
  persist the flag at all, `useBackgroundMode: true` is REJECTED with a clear error on such an environment
  instead of being silently dropped. `false` is always accepted (inline execution is what that environment
  already does). `describe-business-process` reports the effective value per element, so it round-trips.
- A data source `filter` on a `signalStart` to restrict WHICH records fire the trigger (see the
  "Data source filters" section below).
- NOT yet buildable: gateways, conditional/default flows, timer/message start, intermediate events,
  sub-process, the Add/Delete-data target object + values (a `filter` on THOSE tasks is serialized
  but not end-to-end usable — the buildable filters are `signalStart`, `readData` and `changeData`), and the Read data
  collection / count / aggregation modes (only the first-record mode builds; the others are designer-only).
  Use the catalog below to reason about a solution and to READ existing processes
  (`describe-business-process`); don't expect to build those types in this increment.

== Descriptor (create-business-process) ==
{
  "name": "UsrAccount_Onboard", "caption": "Account onboarding", "packageName": "Custom",
  "elements": [
    { "name": "OnboardingRequestedStart", "type": "startEvent",  "caption": "Onboarding requested" },
    { "name": "NotifyAccountOwner",       "type": "performTask", "caption": "Notify the account owner" },
    { "name": "EndOnboardingHandedOff",   "type": "endEvent",    "caption": "Onboarding handed off" }
  ],
  "flows":      [ { "source": "OnboardingRequestedStart", "target": "NotifyAccountOwner" },
                  { "source": "NotifyAccountOwner", "target": "EndOnboardingHandedOff" } ],
  "parameters": [ { "name": "AccountNameParameter", "type": "Text", "direction": "In",
                    "caption": "Account name" } ],
  "mappings":   [ { "elementName": "NotifyAccountOwner", "elementParameter": "<ParamName>",
                    "processParameter": "AccountNameParameter" } ]
}
- `name` is the local element handle (the schema element Name, a string code) used by flows
  (`source`/`target`) and mappings (`elementName`). Creatio identifies an element by this Name plus a
  UId GUID; the platform reserves "Id" for the GUID, so the handle is `name`, not `id`. A `userTask`
  element auto-carries the task's parameters; map values into them with `mappings`. For a record trigger
  use `signalStart` (next section).
- EVERY code and caption in the examples of this guide is N1-N10 compliant on purpose (see "Naming and
  codes" below): copy their SHAPE, not just their fields. A generated `Start1` / `task1` / `End1` is the
  failure those rules exist to prevent, and an example is what a model copies first.

== Naming and codes (N1-N10) ==
(AUTHORING rules for the names and codes you choose. They are numbered N-, deliberately NOT R-: nothing
pre-checks them — `validate-process-graph` enforces a subset of the R1–R17 connection rules and enforces
none of these. The reader they are written for is a no-code team opening the result in the Process
Designer, so a generated process has to read as though a person named it.)
Field map — each rule below names the descriptor field it governs:
  process title    -> `caption` (top level)
  process code     -> `name` (top level)
  element label    -> `elements[].caption`
  element code     -> `elements[].name` — also the flow `source`/`target` and the mapping `elementName` handle
  parameter code   -> `parameters[].name`
  parameter label  -> `parameters[].caption`
N1  Process `caption`: SENTENCE CASE — first word capitalized, the rest lower case except proper nouns.
    "Corporate customer onboarding", NOT "Corporate Customer Onboarding".
N2  Process `name`: `<prefix><Object>_<Action>` in PascalCase segments — `UsrAccount_Onboard`,
    `UsrOrder_Approve`. The prefix is NOT applied for you, and WHICH prefix to apply is not this guide's
    to decide: `app-modeling` owns it, in the bullet that reads "use the `schema-name-prefix` value from
    `create-app` (or from `get-schema-name-prefix`) as the prefix for ALL custom schema codes" — whose
    enumeration names business-process codes. Read the prefix from there and apply what it yields:
    * The environment DECLARES a prefix -> the server REFUSES a code without it, with `The
      "Account_Onboard" code of the "<caption>" object must start with the "Usr" prefix` (ENG-94378,
      observed 2026-08-19 on a 7.8.0 stand whose `SchemaNamePrefix` is `Usr`; the refusal names whatever
      prefix THAT environment declares, so never hard-code `Usr`).
    * The environment declares an EMPTY prefix -> add none, as `app-modeling` states outright. The
      refusal above is evidence about a prefix-declaring environment only; an empty-prefix stand was not
      probed, so do not read it as "the platform always demands a prefix".
    After the prefix use two `_`-separated PascalCase segments, the object then the action. A further
    `_<Qualifier>` segment IS accepted — `UsrProbe_Check_Naming` saved on a 7.8.0 stand (ENG-94378,
    probed 2026-08-20) — but add one only when the action genuinely needs it: of 427 process schemas on
    that stand, 90 carry exactly one `_` and NONE carry two, so two segments is the house shape. Add the
    package name only to break a real collision, never as blanket disambiguation. NO autonumber, NO
    random suffix, NO GUID fragment — the designer's own `Process_3d0825b` shape is what this prevents.
N3  A process meant to be CALLED as a sub-process ends its code with `SubProcess`
    (`UsrInvoice_ValidateSubProcess`), so a caller can tell what it is from the code alone.
N4  `elements[].caption`: ALWAYS set one explicitly on EVERY element — never leave it to a default.
    Sentence case, <= 60 characters, short enough to read inside the diagram box. The SHAPE follows what
    the element IS:
    * ACTIVITIES (`userTask` / `performTask` / `readData`, `sendEmail`) — VERB FIRST, the action the
      process performs: "Read primary contact", NOT "Read the account's primary contact" (padded) and
      NOT "Primary contact reading" (nominalized).
    * EVENTS (`startEvent`, `signalStart`, `endEvent`) — the TRIGGER or the OUTCOME, a noun phrase:
      "Record is modified", "Order amount or status changed", "Onboarding handed off". Verb-first
      MISDESCRIBES an event: "Modify record" reads as an action the process performs rather than the
      condition that starts it, and a Terminate end performs no action at all.
    Prefer the plainest statement of the action or the outcome over a stylistic variant. The element CODE
    is derived from this caption (N5), so the caption is the only free choice in the pair and a reworded
    caption drags its code with it — which is why N9's stability rule reaches back to this one.
    This is the only text a no-code reviewer sees on the diagram, so an unset or padded caption is what
    makes a generated process unreviewable.
    EVERY element type accepts one — verified across the whole buildable slice, events included:
    `startEvent`, `signalStart`, `endEvent`, `userTask` (incl. `performTask` / `readData`) and `sendEmail`
    were each built WITH a caption and each read the caption back verbatim through
    `describe-business-process` (ENG-94378, probed 2026-08-20 on a 7.8.0 stand). So there is no element
    on which this rule is dead text. OMIT a caption and the platform falls back to THE ELEMENT CODE as the
    caption — the same graph built without captions read back `"caption": "ProbeStart"` on its start event
    — so an unset caption is not a friendly default: it puts a raw code on the diagram, which is exactly
    how `Start1` reaches a no-code reviewer's screen.
N5  `elements[].name`: PascalCase, a meaningful verb+object, no spaces. NO autonumber and NO random
    suffix — `StartSignal1` and `Task2` are the failure this rule names. Do not pad a code with the
    element's type name either. Events: a `signalStart` is `<Trigger>Signal` (`AccountAddedSignal`), a
    `startEvent` is `<Reason>Start` (`OnboardingRequestedStart`), an end event is `End<Reason>`
    (`EndOnboardingStarted`). A shape is a prefix or a suffix, NOT a licence to reword: it is added to
    what the derivation below produces, and never replaces it.
    DERIVE the code from the element's own `caption`, do not compose it separately. The derivation, in
    order:
    (1) take the caption's words in order;
    (2) drop ONLY these words: `a`, `an`, `the`, `is`, `are`, `was`, `were`, `be`, `been`, `has`,
        `have`, `had`. That list is EXHAUSTIVE — KEEP every other word, including `and`, `or`, `to`,
        `of`, `for` and numerals, so "Contact has been created" is `EndContactCreated` and "Order
        amount or status changed" is `OrderAmountOrStatusChangedSignal`;
    (3) treat every punctuation mark as a WORD BOUNDARY: the mark itself is removed, but the words on
        BOTH sides of it are capitalized separately — "Follow-up" is `FollowUp`, never `Followup`;
    (4) PascalCase what remains, then add only the prefix or suffix the shape above requires.
    "Account is added" -> `AccountAddedSignal`; "Create the follow-up task" -> `CreateFollowUpTask`;
    "Follow-up task created" -> `EndFollowUpTaskCreated`. Do NOT paraphrase, abbreviate, or drop a
    content word on the way. Two independent runs of one request wrote the SAME caption "Follow-up task
    created" and produced `EndFollowUpTaskCreated` and `EndFollowUpCreated`
    (ENG-94378, clean-room re-run 2026-08-21) — the drift came from shortening, and it is what makes
    two generations of the same request undiffable. Every element example in this guide is derivable
    this way, and `GuideExamples_ShouldDeriveEveryElementCodeFromItsCaption` holds them to it — where a
    caption and a code disagree, the caption is the input and the code is what is wrong.
N6  An element code MUST NOT contradict the element's RUNTIME type. `endEvent` currently builds a
    `ProcessSchemaTerminateEvent` — a Terminate end, not a Simple end — so `EndNormal` on one is a lie the
    code tells about the element (ENG-94378: the baseline run produced exactly that). The catalog below
    lists `endEvent` as "End/Terminate" because BPMN has both; what THIS API builds today is Terminate.
    SCOPE: the rule forbids only a code that ASSERTS a type — `EndNormal` on a Terminate end, or
    `Terminate…` on an element that is not one. It does NOT ban the `End<Reason>` shape N5 prescribes:
    `EndOnboardingStarted` names the REASON, not the type, and is correct on a Terminate end. Read the
    runtime type back with `describe-business-process` (`type`) and name the element after what it IS.
N7  The `UserTask` postfix belongs to a user-task SCHEMA you author, NEVER to an element code. At runtime
    Read data, Perform task, Send email and Modify data are all user tasks, so applying the postfix to
    element codes would put it on nearly every element. `CallTask` is right, `CallTaskUserTask` is wrong.
N8  `parameters[].name`: PascalCase plus a `Parameter` suffix — `TargetAccountParameter`,
    `CallDueDaysParameter`. The `caption` carries NO suffix ("Target account"). EXCLUSION: the parameters
    the platform auto-creates on an ELEMENT (`Duration`, `ShowInScheduler`, `RemindBefore`,
    `ResultEntity`, ...) belong to the task, not to you — never rename them, never expect the suffix on
    them. The rule governs `parameters[]`, the process-level list you author.
N9  Codes are STABLE: regenerating from the same request must yield the same codes. Never derive a code
    from the clock, a GUID, a counter, or anything else that varies between runs — stability is what
    makes two generations diffable and a review repeatable.
    Stability is not a hope, it FOLLOWS from N5's derivation rule: a code that is a function of the
    caption cannot drift unless the caption does. Measured on one request across two independent runs,
    every code backed by a formula was byte-identical — the process code (N2's `Object_Action`) and the
    start-event code (N5's `<Trigger>Signal`) — while the one rule that gives a shape and leaves the
    wording free, `End<Reason>`, drifted (ENG-94378, 2026-08-21). So the caption wording is part of
    what this rule constrains: a drifting caption drags its derived code with it, and N4 owns the rule
    that keeps it still (prefer the plainest statement of the action or the outcome).
    SCOPE: N9 governs the codes of elements and parameters PRESENT IN BOTH runs. Two runs may
    legitimately model one request with different process parameters — that is a modelling choice, not
    naming drift, as long as each code obeys N8. But nothing in N1-N10 constrains that structural
    choice: this catalog governs NAMES, so a structural difference between two runs is OUT OF SCOPE
    here rather than approved here, and two runs whose parameter sets differ stay hard to diff for a
    reason no naming rule can fix.
N10 Sequence-flow labels — NOT YET BUILDABLE (conditional and default flows are outside the buildable
    slice; ENG-91853 is the ticket that extends it). Recorded here so the catalog is complete, the same
    way the R1–R17 header separates the full catalog from the buildable slice. When they land: label a
    conditional flow with the decision outcome it represents (`Budget > 10 000`), and label the default
    flow explicitly rather than leaving it blank.
    "Connections" in a naming review means these SEQUENCE FLOWS. The Activity "Connected to" links are a
    different feature with its own section below ("Activity connections") and no naming surface at all.

== Trigger a process on a record event ("run on save" of a page/record) — READ THIS ==
- When the goal is "run a process when a record is saved / added / changed / deleted" (e.g. on a page
  like UsrXxx_FormPage), that is a PROCESS trigger, NOT page logic. Make the process START with a
  Signal start element bound to the object. Do NOT add a client-side save handler
  (`crt.SaveRecordRequest` / any page handler) to launch a process on save — that is the wrong tool and
  a fragile workaround. The signal start is the platform-native, declarative trigger.
- Build it with `create-business-process`. The start element is:
    { "name": "RecordModifiedSignal", "type": "signalStart", "caption": "Record is modified",
      "signal": { "entity": "<EntityName>", "on": "modified" } }
  then the activity (e.g. a Perform task / `performTask` that shows a Task), then an `endEvent`,
  wired RecordModifiedSignal -> activity -> end. (`entity` is the page's object, e.g. UsrTestRunButton.)
- `on` is a SINGLE event: "added" | "modified" | "deleted" (the designer has no combined
  "added or modified"). "On save" of a record edited on a page = "modified"; a brand-new record = "added".
- A "modified" trigger fires on ANY field change by default. To restrict it to fire ONLY when specific
  columns change, add `changedColumns` (an array of column NAMES on the trigger entity) to the signal:
    { "name": "OrderAmountOrStatusChangedSignal", "type": "signalStart", "caption": "Order amount or status changed",
      "signal": { "entity": "Order", "on": "modified", "changedColumns": ["Amount", "StatusId"] } }
  `changedColumns` is valid ONLY for `on: "modified"` (the designer's "expect changes" case) — the server
  rejects it for "added"/"deleted", and rejects a name that is not a column on the entity. Use entity COLUMN
  names (e.g. `Amount`), not field captions; omit `changedColumns` (or pass []) to fire on any change — but an
  array that contains ONLY blank entries is rejected, since that reads as a mistake rather than as a request
  to widen the trigger (blanks mixed with real names are simply ignored). This is
  INDEPENDENT of `filter`: `changedColumns` narrows WHICH columns count as a change, `filter` narrows WHICH
  records qualify — combine them freely.
- To fire the trigger ONLY for records matching a condition (e.g. only when Name = "Start"), add a
  `filter` to the signalStart element (full shape in "Data source filters" below):
    { "name": "RunButtonPressedSignal", "type": "signalStart", "caption": "Run button is pressed",
      "signal": { "entity": "UsrTestRunButton", "on": "modified" },
      "filter": { "object": "UsrTestRunButton",
        "conditions": [ { "column": "UsrName", "comparison": "equal", "value": "Start" } ] } }
  Use the entity COLUMN name (here `UsrName`), not the field caption ("Name").
- To convert an EXISTING process to start on a record event, use `modify-business-process`:
  removeElement the current start, addElement a `signalStart`, addFlow signalStart -> (first activity).
- To change an EXISTING signal's trigger or tracked columns IN PLACE (without re-adding it), use the
  `setSignal` op — it preserves the element and its flows:
    { "op": "setSignal", "elementName": "OrderAmountOrStatusChangedSignal",
      "signal": { "on": "modified", "changedColumns": ["Amount"] } }
  Partial update: omit `changedColumns` to clear column tracking (fire on any change), omit `on` to keep the
  current change type, and include `entity` only to retarget the trigger object (retargeting clears any
  filter bound to the old entity).

== Read data element (readData) — first-record mode ==
- A `readData` element reads the FIRST record of a sorted selection into its `ResultEntity` output
  parameter (the whole record). Configure it with the element's `readData` block:
    { "name": "ReadNewestContact", "type": "readData", "caption": "Read newest contact",
      "readData": {
        "source": "Contact",                                  // REQUIRED at create: the entity to read
        "mode": "first",                                      // optional; "first" is the only buildable mode
        "columns": ["Name", "Email"],                         // optional; omit or [] = read ALL columns
        "sort": { "column": "CreatedOn", "direction": "desc" } // optional; direction defaults to "asc"
      },
      "filter": { "object": "Contact",
        "conditions": [ { "column": "Name", "comparison": "contains", "value": "Creatio" } ] } }
- `mode`: only `first` (first record of the sorted selection). The designer's other read modes —
  collection, count, aggregation — are NOT buildable yet and are REJECTED with a clear error. An element a
  human configured in one of those modes CANNOT be converted to first-record through this API at all — an
  explicit `"mode": "first"` is refused too, because the conversion would leave the element's collection
  item parameters behind. Remove the element (`removeElement`) and add a new `readData` one instead.
- `columns` are TOP-LEVEL entity COLUMN names (not captions); an unknown name is rejected at build. Omit the
  list (or pass `[]`) to read all columns. A dot-separated path into a linked object (`Owner.Name`) is NOT
  supported and is rejected — such paths exist only in hand-authored metadata (the Read data card's own
  picker lists top-level columns only); read the whole record (omit `columns`) if you need them. `sort`
  makes "the first record" deterministic — without it the platform reads an arbitrary first record; single
  column only (multi-column ordering is designer-only), and the sort column must be top-level too.
- WHICH records qualify is the element's separate `filter` block (full shape in "Data source filters"
  below). Unlike a signalStart filter, a readData filter MAY reference `processParameter` /
  `elementParameter` — the element runs inside a live process instance.
- LIMITATION — the read record's individual COLUMN values are NOT referenceable downstream yet. The
  element's only output parameter is `ResultEntity` (the whole record, `isResult:true` in describe);
  the record's columns are NOT element parameters, so a mapping, `changeData` value or filter condition
  that references them (e.g. `sourceElementParameter: "Email"` on the read element) FAILS the build with
  "element has no parameter". Entity-column access needs meta-path support (planned; ENG-91844). To key
  work off a specific record today, use a `signalStart` trigger output (`RecordId`) or a process parameter.
- Change an EXISTING element in place with the `setElement` op's `readData` field (preserves the element
  and its flows):
    { "op": "setElement", "elementName": "ReadNewestContact",
      "elementUpdate": { "readData": { "sort": { "column": "ModifiedOn", "direction": "desc" } } } }
  Partial update: omit `source` to keep the current source object, omit `columns`/`sort` to keep the
  current selection/order, pass `columns: []` to reset to ALL columns. RETARGETING `source` to a different
  object is REFUSED while any other parameter still maps from the element (the refusal names each
  dependent — re-map or remove them first, the same block the designer applies); a retarget that proceeds
  clears the columns, sort AND record filter bound to the old entity — re-supply them (and issue a
  `setFilter`) in the same operations array. `describe-business-process` reads the whole block back
  (`source`, `mode`, `columns` as names, `sort`), so anything the builder made round-trips into
  create/modify. Read-back limits on a HUMAN-made element: a linked-object column is omitted from
  `columns` (it cannot be expressed here), and `sort` is the EFFECTIVE PRIMARY entry — the one the
  runtime's ORDER BY actually ranks first — while any further ACTIVE secondary sort entries are not
  reported, and a `sort` write replaces the whole stored order. So for such an element the described
  block is narrower than what it really does — do not feed it back as a full replacement.
- A HUMAN SAVE quietly changes a builder-made element's plumbing: opening the element card and clicking OK
  always writes `ReadSomeTopRecords = true` + `NumberOfRecords`, which a builder-made element leaves unset
  (row count stays 1 — what "first record" means). Under the `FeatureReadDataUserTaskEntityReadOldMode`
  feature that flag changes how many rows the element reads, and the drift is INVISIBLE to
  `describe-business-process` (unset parameters are omitted) — a builder-made and a human-touched element
  look identical there. Nothing to do about it at build time; know it when diagnosing a stand.

== Modify data element (changeData) ==
- A `changeData` element updates every record matching its `filter` with the declared column values:
    { "name": "UpdateContact", "type": "changeData", "caption": "Update the contact",
      "changeData": {
        "source": "Contact",                                   // REQUIRED at create: the entity to update
        "values": [                                            // REQUIRED at create: one entry per column
          { "column": "JobTitle", "value": "Manager" },        // plain constant — TEXT columns ONLY (see below)
          { "column": "Notes", "processParameter": "NoteTextParameter" }, // a process parameter's value
          { "column": "AccountId", "sourceElement": "RecordModifiedSignal", "sourceElementParameter": "RecordId" }
        ] },
      "filter": { "object": "Contact",
        "conditions": [ { "column": "Name", "comparison": "contains", "value": "Creatio" } ] } }
- Each `values` entry sets `column` (entity COLUMN name) + exactly ONE source: `value` | `processParameter` |
  `sourceElement` + `sourceElementParameter` | `expression` — the mapping source vocabulary. One entry per
  column (duplicates rejected); unknown columns/parameters rejected at build.
- `value` is a plain constant for TEXT columns ONLY, and non-empty. The platform stores it as the raw string
  and the runtime reads every non-text column TYPED — a date/lookup/numeric constant would save green and
  fail at run time, so the server REFUSES it at build. Assign non-text columns via `processParameter`,
  `sourceElement`+`sourceElementParameter`, or an `expression` macro (`[#DateValue.…#]` / `[#Lookup.…#]` —
  same macro grammar as parameter defaults, see "DEFAULT-value macro rules"). An empty `value` is refused
  for every type: the runtime silently discards an empty assignment.
- The `filter` is EFFECTIVELY MANDATORY: the runtime refuses to update with an empty filter (it would mean
  "update every record of the object"). To target ONE record, filter on `Id` against a process parameter
  or a trigger output such as a `signalStart` element's `RecordId`:
    "filter": { "object": "Contact",
      "conditions": [ { "column": "Id", "comparison": "equal",
        "elementParameter": { "elementName": "RecordModifiedSignal", "parameter": "RecordId" } } ] }
  LIMITATION: the record read by a preceding `readData` element is NOT referenceable here — its column
  values (including `Id`) live inside the `ResultEntity` output, not as element parameters (see the
  readData LIMITATION above; ENG-91844).
- Change an EXISTING element in place with the `setElement` op's `changeData` field: omit `source` to keep
  the current target; a supplied `values` array REPLACES the whole assignment set. Retargeting `source` to a
  different object REQUIRES `values` for the new entity in the same update — the server REFUSES a values-less
  retarget, because the cleared element would be silently skipped by the runtime (the same fact that makes
  `values` mandatory at create); the same refusal covers a values-less update on an element with no stored
  values yet, and a retarget is refused while another parameter still maps from the element. On ANY target change
  (FIRST configuration included) the stored record filter clears UNLESS its root already targets the incoming
  object — `setFilter` never validates its `object` against the element, so a same-object filter set before the
  target survives; issue a `setFilter` in the same operations array when it cleared. Same rule on `readData`. `describe-business-process` reads the block back (`source` is null when the element's target object is set by a formula/mapping instead of a constant — the block is still reported, and retargeting such an element needs an explicit `source`; constants in `value`; a
  `processParameter` / `sourceElement` binding decodes back to its NAME, so the block re-applies in another
  process — a decoded `sourceElement` still obeys the create-time rule that its element appear EARLIER in
  `elements[]`, and describe emits stored order, so a described block may need reordering before it re-creates.
  A stored value the write path would refuse — a non-text or empty constant, or a binding that fails the type
  check — reads back as its COLUMN ALONE rather than as something you cannot write back, and any other formula
  comes back as its raw `[#…#]` in `expression`).

== Data source filters (signalStart trigger condition / readData + changeData record filter) ==
- A `filter` declares, high-level, WHICH records a filtered element acts on. The server serializes it to
  the platform Terrasoft.FilterGroup — you NEVER hand-write the escaped filter JSON.
- Usable today on a `signalStart` (restrict the record trigger), on a `readData` element (restrict which
  records the read selects from) and on a `changeData` element (restrict which records are updated —
  effectively mandatory there). Shape:
    "filter": {
      "object": "<EntityName>",        // root object; defaults to the signal entity if omitted
      "logicalOperation": "and",       // "and" (default) | "or"
      "conditions": [
        { "column": "UsrName",      "comparison": "equal", "value": "Start" },
        { "column": "Account.Code", "comparison": "equal", "value": "1" }   // dot-path traverses a lookup
      ],
      "groups": [                       // optional nested groups, each with its own logicalOperation
        { "logicalOperation": "or", "conditions": [ /* ... */ ] }
      ]
    }
- `column` is the entity COLUMN name (e.g. `UsrName`, not the caption "Name") and may be a dot-path
  through lookups (`Account.Code`, `Account.Owner.Name`); the server resolves the column type from the
  object's schema (so you don't supply types).
- `comparison`: equal (default) | notEqual | greater | greaterOrEqual | less | lessOrEqual | contains |
  notContains | startWith | notStartWith | endWith | notEndWith | isNull | isNotNull.
- The right-hand value of a condition is exactly ONE of: `value` (a constant as a string — the server
  types it by the column; for a Date/DateTime/Time column pass ISO-8601, e.g. `2026-05-01` or
  `2026-05-01T12:00:00Z`), `processParameter` (a process parameter by name), `elementParameter`
  ({ elementName, parameter } — another element's output; the parameter must EXIST on that element — a
  `readData` element exposes only `ResultEntity`, so `{ "elementName": "ReadNewestContact", "parameter": "Id" }` is
  refused, see the readData LIMITATION), `expression` (a raw token), or `macro` (a
  relative-date / system macro — the complete set is in the next bullet). isNull/isNotNull take none.
- `macro` vocabulary (COMPLETE set — an unknown name is rejected at BUILD, validated against the platform
  macro catalog, never silently accepted): **relative periods** `Yesterday` | `Today` | `Tomorrow`, plus
  `Previous`/`Current`/`Next` for each of `Week` | `Month` | `Quarter` | `HalfYear` | `Year` | `Hour`
  (so `CurrentHalfYear`, `NextWeek`, `PreviousQuarter`, `CurrentHour`, … are ALL valid); **argument macros**
  (require an integer `macroArgument`) `NextNDays` | `PreviousNDays` | `NextNHours` | `PreviousNHours` |
  `NextNDaysOfYear` | `PreviousNDaysOfYear` | `DayOfYearTodayPlusDaysOffset`; **recurring "every year"**
  `DayOfYearToday` (the ONLY DayOfYear macro that takes NO argument); **system / lookup** `CurrentUser` |
  `CurrentUserContact`.
- SIGNAL-START RESTRICTION (important): on a `signalStart` filter the right-hand side may ONLY be a constant
  `value`, a `macro`, a `datePart`, or isNull/isNotNull — NOT `processParameter` / `elementParameter` /
  `expression`. The signal is evaluated to decide WHICH records start the process, BEFORE any process
  instance exists, so a parameter / element output / meta-path reference has no value yet. The server
  REJECTS a parameter reference on a signal filter (the visual designer likewise hides the "select
  parameter" option for signal starts). Parameter references ARE valid on a data-operation element filter —
  the element runs inside a live process instance — and are end-to-end buildable on a `readData` element
  (e.g. filter the read by a process parameter's value) and on a `changeData` element, where the filter is
  effectively MANDATORY (the runtime refuses to update with an empty one); on Add/Delete data they serialize
  but the task itself is not buildable yet (see below).
- `datePart` (optional, LEFT-hand modifier — NOT a right-hand source): extract a calendar/clock part from a
  Date/DateTime `column` and compare that part instead of the whole date. `Year` | `Month` | `Day` |
  `Week` | `Weekday` | `Hour` extract an INTEGER — pair with an integer `value` (a signalStart filter
  allows only a constant `value`/`macro`/`datePart`, never a `processParameter` — see the restriction above):
  `{ "column": "CreatedOn", "datePart": "Year", "comparison": "equal", "value": "2026" }` reads
  `Year(CreatedOn) = 2026`. `HourMinute` is the exception — it extracts the TIME-OF-DAY and compares it to a
  `value` in `HH:mm[:ss]` form: `{ "column": "CreatedOn", "datePart": "HourMinute", "comparison": "equal",
  "value": "14:30" }` reads `HourMinute(CreatedOn) = 14:30`. Combines with any comparison (`greaterOrEqual`,
  …); it modifies the left side, so it is independent of the right-hand source choice (but do not use it with
  a `macro`).
- Groups nest to any depth: A AND (B OR C) = conditions:[A] + groups:[{ "logicalOperation":"or",
  conditions:[B, C] }].
- A `filter` on a `readData` element is end-to-end usable (pair it with the element's `readData` block —
  see the "Read data element" section), and on a `changeData` element it is effectively MANDATORY — the
  runtime refuses to update with an empty filter (see the "Modify data element" section). A `filter` on an
  Add/Delete-data task is serialized too, but those tasks' target object / values are not buildable yet, so
  THEIR filters are not end-to-end usable in this increment.
- On an EXISTING process, set/clear a filter via `modify-business-process` ops `setFilter`
  ({ op:"setFilter", elementName, filter }) and `clearFilter` ({ op:"clearFilter", elementName }).
  `setFilter` REPLACES the element's whole filter (there is no add-one-condition op); to add a condition,
  read the current filter first (below) and send the complete new filter.
- `describe-business-process` reads a filter back: an element carries a decoded `filter` (the same
  object / logicalOperation / conditions / groups shape) when it has one, so you can inspect it or
  round-trip it into a `setFilter`. A parameter reference comes back as its raw meta-path `expression`.
  A lookup value reads back as the raw id in `value` plus its resolved caption in `displayValue` (so
  `UsrStage` shows `Approved`, not a bare GUID); `displayValue` is read-only — omit it on `setFilter`.

== Build recipe (intent -> running process) ==
1. Translate the request into a graph: one start event, the activities, the sequence flows, one or
   more end events; plus process parameters and the value mappings between them — and name them per
   N1-N10 in "Naming and codes", which is what makes the result reviewable in the Process Designer.
2. (recommended) `validate-process-graph(graph)` -> fix every error-severity finding.
3. `list-user-tasks` -> pick the exact `userTaskName`(s) for your activities.
4. `create-business-process(descriptor)` -> builds + saves in one call (layout is automatic).
5. Verify: `describe-business-process` (element types, user-task names, parameter sources + direction + isResult
   — an output you can map FROM has `isResult:true` or `direction:"Out"`; the signal trigger). Verify through
   `describe-business-process`, not a raw `execute-esq`/`odata-read` of the process record (see the readiness
   bullet below).
6. Change it later with `modify-business-process` (ops: addElement / removeElement / addFlow / removeFlow /
   addParameter / addMapping / setParameter / removeParameter / setFilter / clearFilter / setSignal /
   setElement / setConnections / clearConnections — same parameter/mapping/filter/signal/readData/
   changeData/email shapes as a build; setSignal reconfigures an existing signalStart's record trigger +
   tracked columns in place, setElement changes element-level fields in place: `useBackgroundMode` on any
   element kind, `readData` / `changeData` on the matching data element only (see the "Read data element" /
   "Modify data element" sections for their partial-update and source-retarget rules), and a sendEmail
   element's `email` block (a partial update; to/cc/bcc recipients MATCH-OR-APPEND — a new address is added,
   an identical one is a no-op, and none can be removed); setConnections/clearConnections bind and unbind an
   Activity's "Connected to" links (see below)).
- File-design-mode caveat: on an FSD stand a built process is saved to the file system (the designer
  sees it) but is NOT runtime-active until it is loaded FS->DB and published — so a signal won't
  physically fire yet.
- Do NOT run `compile-creatio` to "make a process runnable", and do NOT read a raw system record
  (`odata-read`/`execute-esq`) to decide readiness — read status back with `describe-business-process`.
  Inferring "needs a compile" from a raw column NAME is the trap here: a raw read of `VwSysProcess` (what
  `odata-read`/`execute-esq` returns for a process — verified: run_20260820_133837) surfaces per-process
  DIRTY flags — `NeedInstall`, `NeedUpdateSourceCode`, `NeedUpdateStructure` — that are ALL `true` on a
  freshly-saved process. None of them is a `compile-creatio` instruction (`NeedInstall` in particular is a
  DB-install marker meaning "finish installing this into the DB", never "compile"), and the same caution
  applies to any `NeedXxx` / `IsXxx` column reached through a raw read.
  WITHIN A PROCESS exactly two things pull a compile in, and both are C# YOU authored: a `scriptTask`,
  and a `userTask` carrying an after-activity-save script. Everything else — add/read/modify data,
  formulas, connections, signals, and USING an already-compiled user task — is applied and runs with no
  compile. This bullet scopes compilation to the PROCESS; other configuration schemas (source code,
  business objects, DCM, value lists, and a CUSTOM user-task schema — see the user-task note below) carry
  their own compile obligations and are NOT covered here.

== Modifying an existing process — safety rules (modify-business-process) ==
- ALWAYS `describe-business-process` first, and re-describe after the edit to verify the result.
- The modify path runs NO structural validation (only the create path validates the graph):
  `removeElement` / `removeFlow` can leave the process unreachable or with dangling paths and the save
  still succeeds. `removeElement` also CASCADES — it deletes every flow touching the element and the
  mappings TARGETING it, but does NOT re-join the flow across the gap, and mappings/values READING the
  removed element's outputs may survive as dangling references. Add the bridging `addFlow` in the same
  operations array, then re-describe and clean up any leftover references to the removed element.
- Before removals, run `validate-process-graph` on the graph AS IT WILL BE after your operations
  (describe output + your planned ops applied), and confirm destructive removals with the user.
- If describe shows constructs the builder cannot create (gateways, conditional/default flows,
  sub-process, timer/message/intermediate events), they survive a save untouched as data — but you CAN
  still remove or rewire them by name and nothing will warn you. Treat such a process as high-risk:
  prefer additive edits, do not remove or rewire those elements, and tell the user what you left alone.
- Every modify re-applies the automatic layout to the WHOLE diagram: a hand-arranged multi-lane or
  branched diagram is flattened into generated left-to-right rows (process data intact, manual layout
  lost). Warn the user before editing a process with a curated diagram.

== Element catalog (data-id -> label -> purpose) ==
(The `data-id` strings below are the vocabulary for `validate-process-graph` and for reasoning about /
reading processes. To BUILD, map them to the create-business-process `type` + `userTaskName`: events
`startEvent`/`startEventSignal`->`signalStart`/`endEvent`; a user/system task -> `type:"userTask"` with
`userTaskName` from list-user-tasks, e.g. Perform task = `performTask`/ActivityUserTask, Read data =
`readData`/ReadDataUserTask. Send email is the ONE user task with its own dedicated build type:
`emailTemplateUserTask` -> `type:"sendEmail"` (NOT a generic `userTask`) — full custom-message configuration
(mode/sender/recipients/subject/body/options/performer; no email templates), see "What you can build today".)
System actions (palette group "System actions"):
- `readDataUserTask`  Read data    — read first record / aggregate / count / collection of an object.
    FIRST-RECORD mode is buildable via the element's `readData` block (source object, columns, sort) plus
    a `filter` — see the "Read data element" section. The other read modes (collection / count /
    aggregation) remain designer-only; describe reports them as `mode: "collection"` / `"function"`.
- `addDataUserTask`   Add data     — create record(s) in background; one-record mode returns only the Id.
- `changeDataUserTask` Modify data — bulk-update matched records (same values to all). BUILDABLE via the
    element's `changeData` block (target object + column values) plus a `filter` — see the "Modify data
    element" section.
- `deleteDataUserTask` Delete data — delete matched records.
- `formulaTask`       Formula      — compute a value (math/string/date/bool) into an output param.
- `scriptTask`        Script task  — custom C# (ends with `return true;`; needs publication).
  - Compile note: a `scriptTask`, and a `userTask` carrying an after-activity-save script, are the two
    IN-PROCESS elements whose authored C# makes the process itself need a compile before it runs.
- `webService`        Call web service — call a registered service; outputs Success + Http status code.
- `callActivity`      Sub-process  — run another process (must start with a Simple start); multi-instance over a collection.
- `userTask`/`*UserTask` — user/system tasks (Perform task, Open edit page, Send email, Approval, etc.).
User actions: `activityUserTask` Perform task, `userQuestionUserTask` User dialog,
  `openEditPageUserTask` Open edit page, `autoGeneratedPageUserTask` Auto-generated page,
  `preconfiguredPageUserTask` Pre-configured page, `emailTemplateUserTask` Send email, `approvalUserTask` Approval.
Events: `startEvent` Simple start, `startEventSignal` Signal start (record add/modify/delete or custom
  signal), `startEventTimer` Start timer (schedule/CRON), `startEventMessage` Start message, intermediate
  catch/throw (`intermediateCatchEvent*`/`intermediateThrowEvent*`), `endEvent` End/Terminate — the
  BPMN catalog has both, but a `create-business-process` `endEvent` builds Terminate today (see N6).
Gateways: `exclusiveGateway` (OR), `parallelGateway` (AND), `inclusiveGateway` (OR), `eventBasedGateway`.
Flows: sequence (default `connect`), conditional (setup -> conditionalConnection), default (setup -> defaultConnection).
- Custom user-task compile rule: a CUSTOM user task is a `ProcessUserTask` SCHEMA, not a process element —
  its own C# methods are generated into the package assembly (it has no `IsInterpretable`; that property
  exists only on `ProcessSchema`), so CREATING or CHANGING one needs a compile before any process can use
  it. Merely REFERENCING an already-compiled user task by `userTaskName` needs nothing. (This is a
  user-task-schema obligation, separate from the in-process compile note under `scriptTask` above.)

== Parameters / mapping / formulas ==
- Process parameters (`parameters[]`): { name, type (Text/Long text/Integer/Float/Money/Boolean/Date/Date-time/Time/Guid/Lookup),
  direction (In/Out/Variable/Internal), caption, description, or referenceSchema = an object name (e.g. City) to make
  it a Lookup to that object }, and an optional value (a constant default; NOT valid for Date / Date-time /
  Time — those defaults are formula macros, see the date macro rule below. A LOOKUP default takes a bare
  record Guid in `value` — stored as the ConstValue the runtime reads. The route ships from
  CrtProcessBuilder 1.3.1.1, and a current clio additionally refuses any environment older than the version
  it bundles (up front, via its package-convergence message), while an older clio surfaces the old package's
  [#Lookup…#]-macro rejection — either refusal means the package is behind, not that the default is
  unsettable). A user-task
  element's own parameters come from the task. The same shape is
  used by modify-business-process `addParameter`. Supported types: Text, Long text, Integer, Float, Money,
  Boolean, Date, Date-time, Time, Guid, and Lookup — other types (composite / entity / file / ...) are not
  supported yet. Name a process parameter per N8 in "Naming and codes".
- To create a process parameter that mirrors an element parameter's EXACT type (e.g. expose a user-task
  OUTPUT for mapping with NO conversion), set `typeFromElement` + `typeFromElementParameter` instead of
  `type`/`referenceSchema` — the data value type (and lookup reference object) is copied verbatim.
- Edit a parameter with `setParameter` (parameterName + parameterUpdate: any of caption/description/code/
  direction/referenceSchema/value, applied in place — the UId and its references are preserved). A
  data-type change is rejected, and referenceSchema can only RE-TARGET a parameter that is already a
  Lookup (it cannot convert a scalar to a Lookup). Do NOT set a Date / Date-time / Time default
  through setParameter `value` — those defaults are formula macros, not plain constants; use the
  mapping + `expression` path below (addMapping overwrites, so it edits a default exactly as it
  creates one). A Lookup default IS settable through `value` as a bare record Guid
  (same version story as the parameters note above).
- Remove a parameter with `removeParameter` (parameterName; blocked when another parameter's value, an
  element mapping, an execution-context parameter or a CONDITIONAL FLOW'S CONDITION still references it —
  sub-processes included. The refusal names each usage site. The scan is a SUPERSET of the designer's: it
  matches a parameter UId case-insensitively where the designer matches case-sensitively, so it can refuse
  a delete the designer would allow. Broader is the safe direction — the failure it prevents is a dangling
  reference that surfaces at run time.).
- Mappings (`mappings[]`): bind a TARGET parameter to a SOURCE.
  TARGET — `elementName` + `elementParameter` (an element input) OR `targetProcessParameter`
  (a process parameter, e.g. expose an element's OUTPUT as a process output).
  SOURCE — exactly ONE of: `sourceElement` + `sourceElementParameter` (another element's OUTPUT parameter) |
  processParameter (a process parameter by name) | value (a constant) | expression (a raw formula).
  Identifying an OUTPUT for `sourceElementParameter`: in `describe-business-process` output an element parameter
  is usable as a mapping source when `isResult: true` OR `direction: "Out"`. Most user-task outputs come back as
  `isResult: true` with `direction: "Variable"` (the platform reports element params as Variable), so detect
  outputs by `isResult`, NOT by `direction` alone.
  Parameter-to-parameter mappings require COMPATIBLE TYPES (target-driven, mirroring the visual designer);
  incompatible pairs are rejected:
  * text -> text: any text source into a base-text target; Phone/Email/Web/Rich targets accept only the
    SAME extra type or a base-text source (never a different extra type);
  * Money <-> Float map to each other; Integer maps ONLY to Integer (NOT to Float or Money);
  * date/time is asymmetric via Date-time: a Date-time target accepts Date/Date-time/Time; a Date target
    accepts Date/Date-time; a Time target accepts Time/Date-time; Date <-> Time is NOT allowed;
  * Lookup: the same reference object on both sides; a Guid source INTO a lookup target IS allowed;
  * Boolean only from Boolean; any other type: exact match only. When the target must match a source
    exactly, mirror it with `typeFromElement` instead of guessing.
  `processParameter` flows a process input into the
  field (the server builds the correct reference); `expression` is a FORMULA — see "Formulas" below for the
  vocabulary and what is checked. Still PREFER `value` / `processParameter` / `sourceElement` when one of
  them expresses the intent: they are structural, so the server builds the reference and a rename cannot
  break it. Reach for `expression` when the value has to be COMPUTED, or for the constant families that are
  only expressible as a macro (date/time, lookup, system variable, system setting).
- UNBOUND element INPUT parameters are NOT listed by `describe-business-process` (it returns only
  value-bearing parameters and outputs) — absence from describe does NOT mean the parameter does not
  exist. Input parameter names come from the user task's schema (for a custom task, the parameters it
  was created with); a wrong `elementParameter` name fails the build with a clear error and nothing is
  saved — never invent names silently.
- To CHANGE a bound value, send `addMapping` again for the same target — it overwrites the binding in
  place (like the designer). There is NO clear/unbind operation (no removeMapping): if asked to
  "remove" a value, say clearing is not supported yet and offer to overwrite it instead.
- Date / Date-time / Time DEFAULT VALUES must be a formula, not a constant: the designer stores a
  date/time constant as a formula macro (a Script source), NOT a plain `value` (a `ConstValue`). Set it via `expression` — for a process-parameter
  default, a mapping with `targetProcessParameter` + `expression`. The inner format is FIXED (NOT ISO,
  NOT locale): `dd.MM.yyyy` and 24-hour `HH:mm`.
  Date → `[#DateValue.dd.MM.yyyy#]` (e.g. `[#DateValue.03.07.2026#]`);
  Date-time → `[#DateTimeValue.dd.MM.yyyy HH:mm#]` (e.g. `[#DateTimeValue.03.07.2026 02:15#]`);
  Time → `[#TimeValue.HH:mm#]` (e.g. `[#TimeValue.12:20#]`). A LOOKUP value is DIFFERENT: prefer a bare record
  Guid in `value` (route ships from CrtProcessBuilder 1.3.1.1 — stored as the ConstValue the runtime reads; on an
  ActivityUserTask category the ConstValue encoding is REQUIRED, see the Perform-task section's NOTE-2). The
  `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` expression form (both GUIDs: the referenced OBJECT's
  schema UId, NOT its name, then the RECORD's Id) still exists, but reach for it only on a pre-1.3.1.1 package
  that rejects the bare Guid — and never for a parameter whose consumer reads ConstValue only.
  EXCEPTION — an Activity CONNECTION: there you send a bare `recordId` to `setConnections` and the server
  composes the token from the target column, so hand-writing it is both unnecessary and easy to get wrong.
- To read another element's output, PREFER the structured `sourceElement` + `sourceElementParameter` mapping (above) — the server builds the correct reference. Do NOT hand-write an element-output reference —
  in the saved metadata it is a server-generated UId meta-path (`[#...[Element:{uid}].[Parameter:{uid}].[EntityColumn:{uid}]#]`), NOT a friendly `Element.Property` path, so you cannot author it — ALWAYS use `sourceElement`. Formulas are strictly typed (convert with `.ToString()` etc.).

== Formulas (`expression` sources and flow conditions) ==

A formula is NOT C#, and knowing what it actually is stops most wrong guesses. Creatio evaluates it with an
EXPRESSION INTERPRETER over a flat, case-sensitive name registry. That means, concretely:

- `Math.Round(1.5)` resolves; `System.Math.Round(1.5)` does NOT (no namespace-qualified names) and
  `math.Round(1.5)` does NOT (case-sensitive);
- no lambdas, no generics, no `new`, no statements — ONE expression, on ONE line;
- the Creatio function library in scope is `FormulaUtilities`, and it has exactly FOUR members:
  `Min`, `Max`, `Avg`, `Mod`. There is no other Creatio helper library. If a caller asks for a function that
  is not here and not on `DateTimeUtilities` or `Math`, the answer is that formulas cannot do it — not a
  guess at a name;
- date helpers live on `DateTimeUtilities` and are spelled WITHOUT a `Get` prefix: `StartOfMonth`,
  `StartOfWeek`, `StartOfYear`, `StartOfQuarter`, `StartOfHalfYear`, `StartOfHour`, plus `Day`, `Month`,
  `Time`, `DayOfWeek`, `DayInRange`. (`GetQuarter` is one of the few that really does carry the prefix, and
  it works in both forms: `DateTime.Now.GetQuarter()` and `DateTimeUtilities.GetQuarter(DateTime.Now)`.)
- `Math`, `DateTime`, `Guid`, `string` and the ordinary operators are available, including the ternary
  `? :` and the null-coalescing `??`.

MACRO FAMILIES — the `[# … #]` tokens a formula may reference:

| family | literal form |
|---|---|
| process / element parameter | the server-generated meta-path; get it from `describe-business-process`, never hand-write it |
| system variable | `[#SysVariable.CurrentUserContact#]`, `[#SysVariable.CurrentDateTime#]` |
| system setting | `[#SysSettings.Code#]` (a legacy form without the type suffix also still works) |
| lookup record | `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` — both GUIDs |
| date / date-time / time | `[#DateValue.dd.MM.yyyy#]` / `[#DateTimeValue.dd.MM.yyyy HH:mm#]` / `[#TimeValue.HH:mm#]` |
| boolean constant | `[#BooleanValue.False#]` (a bare `false` also still works) |

WHAT IS CHECKED, from `CrtProcessBuilder` 1.4.0.0. Before an `expression` mapping or a flow condition is
stored, the server validates it and REFUSES a bad one, naming what is wrong:

- it must parse;
- every `[# … #]` parameter reference must resolve to a parameter IN THAT PROCESS — a dangling one is
  refused with the offending token named. This is what makes an `expression` referencing a parameter safe to
  author rather than a runtime gamble;
- an unknown identifier is refused with the identifier named;
- the result must fit the target. Numeric targets are all checked as `decimal`, so ANY numeric formula fits
  ANY numeric parameter — do not expect an Integer target to reject a fractional formula, the runtime
  coerces on assignment and the check matches the runtime rather than the declared type. The checks that DO
  bite are cross-family: text into a number, a number into a Boolean condition, a date into a Guid;
- a macro family the package does not recognise is ACCEPTED with a warning rather than refused, so a
  process using a dialect this version has not seen still round-trips.

On an environment OLDER than 1.4.0.0 none of this happens — the formula is stored unchecked and a wrong
token fails only at run time. clio refuses `create-business-process` / `modify-business-process` against such
an environment for exactly that reason; the fix is `install-process-builder`, not a workaround.

PARENTHESISE rather than relying on precedence. A condition like `a && b || c` is legal and its meaning is
not obvious to the next reader; write `(a && b) || c`.

== Conditional flows and branch conditions ==

A branch is a flow with a CONDITION. You do not build one — you build a plain flow and then set its
condition:

1. `create-business-process` (or `addFlow`) makes the flow. `flows[].kind` is still refused on the build
   path: a conditional branch cannot be declared there.
2. `modify-business-process` with `setFlowCondition` (`source`, `target`, `condition`) turns that flow into a
   conditional one.

NO GATEWAY IS NEEDED and none is created. The platform synthesizes an exclusive gateway for a conditional
flow whose source is not one, so a branch straight off an activity is legitimate — it is what the platform's
own tests rely on. Gateway ELEMENTS are still not buildable.

The condition must evaluate to a BOOLEAN. An integer is refused: the interpreted engine, which is the
default, does not coerce it the way the older compiled engine did.

An EMPTY condition is refused, and the reason is worth knowing because it is silent otherwise: the platform
stores an empty condition as the literal `true`, so an "empty" branch is an ALWAYS-TAKEN branch. To drop a
condition, remove the flow and add a plain one — there is no clear-condition operation.

BRANCH PRECEDENCE IS FLOW ORDER, and nothing in the metadata records it. Where two conditional branches leave
the same element, they are evaluated in the order the flows were added and the FIRST whose condition is true
is taken. So `Amount > 100` and `Amount > 1000` resolve differently purely by which flow was added first,
with no diagnostic and nothing a human can inspect. Add the most specific branch FIRST, and say so when you
report what you built. `setFlowCondition` keeps a flow's position when it converts it, so setting a condition
never silently reorders your branches.

A conditional flow reads back through `describe-business-process` as `kind: "conditional"` with its
`condition` text, so you can verify what you wrote.

Corpus-attested condition shapes, most common first — these are what real processes use:
`X != Guid.Empty`, `X == true`, `X == "text"` / `X.Equals("text")`, `A && B`, numeric comparisons, a bare
boolean parameter, lookup-record equality, parameter-to-parameter comparison, `!string.IsNullOrEmpty(X)`,
`A || B`, `.Contains("x")`, `X != null`, `!X`, and date comparisons against `DateTime.MinValue`.

== Element: Perform task (userTask / performTask -> ActivityUserTask) ==
- WHAT IT IS: the "Perform task" element. Type alias `performTask` (equivalently `userTask` with
  `userTaskName: "ActivityUserTask"`). It creates an Activity of type Task, assigns it to a person, and then
  PAUSES the process until that person completes the activity with a result. It is the way to put a human step
  inside an automated flow.
- USE IT FOR: call a client, review a document, follow up, prepare paperwork, a manual check — any step where the
  process must wait for a person to act outside the process.
- DO NOT USE IT FOR approvals. Creatio has a dedicated Approval element that creates an Approval record (not an
  Activity), emails approver and author, supports delegation, and branches on the verdict. Perform task has no
  approved/rejected semantics. Approval is not buildable from clio yet — say so rather than emulating it with a
  task.
- A "CALL TASK" IS THIS ELEMENT WITH A CALL CATEGORY, NOT THE CALL ELEMENT. `CallUserTask` (the "Call" entry
  in the list-user-tasks palette) is RETIRED: the product removed it from the designer palette and keeps the
  schema only for backward compatibility with old processes. NEVER build a new element with
  `userTaskName: "CallUserTask"` — describe reports it as `deprecated: true`, and it is a dead end in two ways
  a caller cannot see: it builds its Activity through its own private code path instead of the shared user-task
  one, so the element-level performer-assignment object is IGNORED there (a role or manager performer written
  on it is a silent no-op, and its properties page hard-disables the option), and no future clio capability for
  user tasks will reach it. Same for `SendEmailUserTask` and `EmailUserTask` — use the `sendEmail` element.
- ACTIVITY TYPE IS NOT SETTABLE, AND THAT IS NORMAL. A Perform task ALWAYS produces an Activity of type
  **Task** (the platform writes `TypeId = Task` unconditionally; no parameter carries the type). "Call",
  "meeting", "paperwork" intent is expressed through ActivityCategory — the platform's own convention for
  every API that creates activities. When a user asks for "a call task", build a Perform task with
  ActivityCategory = Call and SAY that the activity's type is Task with category Call. Do not reach for the
  retired Call element to satisfy the word "type", and do not claim the type was set.
- WHAT IT PRODUCES: one Activity row — Title, Owner, Category, Priority, Start date (= now + StartIn),
  Due date (= start + Duration), reminder, and any "Connected to" links. It appears in the performer's
  "Business process tasks" tab. The next element runs only after the activity is completed WITH A RESULT.
- READ-BACK CAVEAT: describe-business-process shows an element parameter only when it is BOUND (or is a result).
  A fresh Perform task therefore shows only 11 parameters out of the full set it actually carries (37 declared
  on a stock environment). Absence from describe does NOT mean the parameter does not exist — every parameter
  in the table below is settable by name with `addMapping`. The element's full set is also not a fixed number:
  the platform can derive one extra connection parameter per Activity lookup column that exists on YOUR
  environment, so a custom column adds a parameter.
- IDENTIFY IT IN describe OUTPUT by `buildType: "usertask"` PLUS `userTaskName: "ActivityUserTask"`. It never
  reads back as `performTask`.

--- Parameters you set (addMapping, target = elementName + elementParameter) ---
  Recommendation      LocalizableString. The task subject ("What should be done?"). Becomes the Activity Title
                      (truncated to 500 chars). Works as a plain `value`: the server materializes it into the
                      process schema resource, exactly where the designer stores it (live-verified end to end:
                      the constant reached Activity.Title on a running process). ALWAYS also give the element a
                      meaningful `caption` — the Title falls back to the caption when Recommendation is empty,
                      which makes a good caption a free safety net.
  OwnerId             Lookup -> Contact. THE PERFORMER ("Who performs the task?").                   [see NOTE-1]
  ActivityCategory    Lookup -> ActivityCategory. Task category. Required by the designer UI.        [see NOTE-2]
                      "To do" = F51C4643-58E6-DF11-971B-001D60E938C6 (also the runtime default).
                      "Call" is TWO rows and the element needs the TASK-typed one — the platform names both:
                      03DF85BF-6B19-4DEA-8463-D5D49B80BB28 is ActivityType Task
                      (ConfigurationConstants.Activity.ActivityCategory.CallAsTask) and is the one to use;
                      E52BD583-7825-E011-8165-00155D043204 is ActivityType Call (the constant plainly named
                      Call) and is the wrong row here. Why: a Perform task ALWAYS creates a Task-typed
                      Activity, and the designer's own category dropdown filters ActivityCategory by
                      ActivityType = Task, so the Call-typed row is one it never offers on this element —
                      resolving "Call" by NAME is a coin flip whose wrong side no human designer can produce.
                      Verify against the environment before trusting either id.
                      Set it as a bare record Guid in `value`. The route ships from CrtProcessBuilder
                      1.3.1.1, and a CURRENT clio additionally refuses any environment older than the
                      version it bundles.
                      A stale environment surfaces as ONE OF TWO refusals, and both mean YOUR ENVIRONMENT IS BEHIND,
                      not that the parameter is unsettable: a current clio refuses the call UP FRONT with its
                      package-convergence message naming both versions and the install hint; an older clio
                      lets the call through and the old package rejects it with "Value '...' is not valid for
                      parameter 'ActivityCategory' of type Lookup: a Lookup constant is a formula token, not a
                      plain value. Set it via a mapping 'expression' instead...". Either way: update the
                      package (install-process-builder); do NOT fall back to the expression form (see NOTE-2).
  ActivityPriority    Lookup -> ActivityPriority. Default = ab96fa02-7fe6-df11-971b-001d60e938c6 (Medium).
                      Same bare-Guid `value` route and same version story as ActivityCategory.
  Duration            Integer, default 20.  Planned duration.        DueDate = StartDate + Duration
  DurationPeriod      Integer, default 0.   Unit for Duration.
  StartIn             Integer, default 0.   Delay before the task starts. StartDate = now + StartIn
  StartInPeriod       Integer, default 0.   Unit for StartIn.
  RemindBefore        Integer, default 0.   Remind the owner this long before the start. 0 = no reminder.
                      Non-zero sets RemindToOwner and RemindToOwnerDate = StartDate - offset.
  RemindBeforePeriod  Integer, default 0.   Unit for RemindBefore.
  ShowExecutionPage   Boolean, default true.  Open the task page automatically for the current user.
  ShowInScheduler     Boolean, default false. Show the task in the Activities calendar. The designer exposes
                      it as the "Show in calendar" checkbox (inherited from the base user-task properties
                      page); addMapping sets the same parameter.
  InformationOnStep   LocalizableString. Designer label "Hint for user" — shown behind the info button on the
                      task page. Works as a plain `value` (same schema-resource materialization as
                      Recommendation).

  ALL THREE *Period PARAMETERS USE THE SAME ENUM:  0=minutes  1=hours  2=days  3=weeks  4=months

--- Parameters the RUNTIME sets — read them, never write them as targets ---
  ActivityResult      Guid. The element's RESULT (the completed activity's result record). Visible in describe
                      from the start (isResult: true). Usable as a mapping SOURCE for a downstream element via
                      `sourceElement` + `sourceElementParameter` (verified: saves, reads back as a
                      server-built `[Element:{uid}]` metapath, and resolves at run time). NOTE: conditional
                      flows are NOT buildable from clio yet, so a clio-built process can READ the result but
                      cannot BRANCH on it — say so instead of promising branching.
  CurrentActivityId   Guid. The created Activity's Id.
                      It is INVISIBLE in describe until bound — the name above is the only way to find it.
                      It resolves as a mapping SOURCE for a downstream element (verified end to end).
                      TRAP: mapping it INTO a later Perform task's own CurrentActivityId makes that task ADOPT
                      the referenced activity instead of creating its own — the platform pattern for updating
                      ONE activity across steps. If the adopted activity is already completed when the later
                      task starts, that task waits FOREVER (completion events route through the activity's
                      ProcessElementId, which is cleared when it first completes). Map the id into a plain Guid
                      parameter or a process parameter unless adopting is exactly what you want.
  IsActivityCompleted Boolean. The runtime sets false at creation and true at completion.
                      It looks writable (it ships a default) — setting it does NOTHING. Do not.
  ExecutionContext    Technical (not serializable). Ignore.

--- Out of scope for parameter mapping ---
  The "Connected to" lookups are CONNECTIONS. Bind them with the `setConnections` op — see "Activity
  connections" below — NOT with addMapping. THE SHIPPED SET IS THESE 19 (Lead, Account, Contact, Opportunity,
  Invoice, Document, Incident, Case, Order, Requests, Listing, Property, Contract, Project, Problem, Change,
  Release, Application, FinApplication) — AN ENVIRONMENT MAY HAVE MORE: the platform derives one connection
  parameter per Activity lookup column, so a custom column appears as an extra one.
  Careful: ActivityCategory, OwnerId and ShowInScheduler look like connections (same internal tag) but are
  ORDINARY parameters and must be set with addMapping.
  QueueItem: do not use it — no consumer of this parameter is known in the platform runtime or the designer
  package (searched, not proven absent), so a written value has no known effect.

NOTE-1 (the performer): "Who performs the task?" has TWO layers, and picking the right one is the whole game.
  LAYER 1 — the element-level `performer` block (ships from CrtProcessBuilder 1.3.1.1, same version story as
  ActivityCategory). Set it on the performTask element in create/addElement, or in place via setElement's
  `elementUpdate.performer`: { "type": "user"|"manager"|"role", "contact"?, "role"?, "showPage"? }.
  * type "role" is THE way to assign to a TEAM: pass a role name or record id in `role`. The created
    Activity carries the role in its own OwnerRole column and its Owner stays EMPTY — the claim model:
    every user of the role sees the task, whoever takes and completes it is recorded. Do not read the
    empty Owner back as "unassigned". The role is CHECKED TO EXIST on either route, against the same role
    set the designer's picker offers — so a typo'd name, an invented Guid and a USER's own SysAdminUnit id
    are refused instead of stored (a user is not a role; for one person use type "user"), and so is a name
    that matches MORE THAN ONE role - a name cannot say which group performs the task, so pass the id. Look
    the role up on the environment rather than guessing an id.
  * type "manager" resolves the contact's MANAGER at RUN time (default contact = the process starter); when
    the contact's employee record has no manager the process raises an error at run time — say so when the
    org data may be incomplete.
  * type "user" with `contact` is the single-person form: pass a bare Contact record Guid (checked to exist,
    and stored as the encoding the designer produces) or a formula like [#SysVariable.CurrentUserContact#];
    an omitted contact defaults to the process starter.
  * `showPage` omitted defaults to false for manager/role (designer parity — a role activity has no single
    performer to open the page for) and stays untouched for user.
  * describe reads the block back top-level on the element (`performer`: type + the stored formula +
    roleDisplay) and it is re-appliable verbatim. REFUSED on any element other than performTask — the
    retired CallUserTask by name (its runtime IGNORES the assignment).
  LAYER 2 — the OwnerId parameter (Lookup -> Contact), for a SPECIFIC PERSON only. Four working ways:
  * a bare Contact record Guid in `value` — the Guid must be an EXISTING Contact record: an id of another
    entity (a ROLE id is the classic mistake) is REFUSED naming the reference object, because before this
    guard it persisted as a well-formed ConstValue referencing nothing at run time;
  * a process parameter: create it with `typeFromElement` + `typeFromElementParameter: "OwnerId"` so the types
    are guaranteed compatible, then map it in;
  * another element's Contact/Guid output parameter;
  * `expression: "[#SysVariable.CurrentUserContact#]"` for "whoever started the process".
  A Lookup -> SysAdminUnit PARAMETER source is likewise REJECTED (incompatible reference object). NEVER route
  a team through OwnerId — that is what the `performer` block's type "role" is for.
  Leaving both layers unset is NOT an unassigned task — at run time the task silently falls to the current
  user's contact (whoever started the process). There is no "nobody" state; omitting the performer is a choice.

NOTE-2 (ActivityCategory): it MUST be a constant (`value`, stored as ConstValue), not a formula. The element's
  allowed-results list is computed from the category ONLY when the category's source is ConstValue (the
  platform's `GetResultParameterAllValues` reads `SourceValue.Value` only for a ConstValue source); writing it
  as a `[#Lookup...#]` expression sets the Activity's category column but SILENTLY DEGRADES the allowed-results
  list the task page / designer result dropdown offers, falling back to the default set. Do NOT try to verify
  the degradation through the `Activity.AllowedResult` column — that column derives from outgoing CONDITIONAL
  flows, not from the category, and is empty either way on a process without them. So the bare-Guid `value` is
  the only correct route; on a pre-1.3.1.1 package the parameter cannot be set correctly — update the package
  rather than using the expression form.

--- Worked example: "Call the client, due in 2 days, assigned to the process starter" ---
1) create-business-process
   { "name": "UsrClient_Call", "caption": "Call client about renewal",
     "elements": [
       { "name": "RenewalCallRequestedStart", "type": "startEvent", "caption": "Renewal call requested" },
       { "name": "CallClientAboutRenewal", "type": "performTask", "caption": "Call the client about the renewal" },
       { "name": "EndClientCalled", "type": "endEvent", "caption": "Client called" } ],
     "flows": [ { "source": "RenewalCallRequestedStart", "target": "CallClientAboutRenewal" },
                { "source": "CallClientAboutRenewal", "target": "EndClientCalled" } ] }

2) modify-business-process  (operations, in this order)
   [ { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "Recommendation",
       "value": "Call the client about the renewal" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "OwnerId",
       "expression": "[#SysVariable.CurrentUserContact#]" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "Duration",       "value": "2" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "DurationPeriod", "value": "2" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "RemindBefore",       "value": "30" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "RemindBeforePeriod", "value": "0" } },
     { "op": "addMapping", "mapping": { "elementName": "CallClientAboutRenewal", "elementParameter": "ActivityCategory",
       "value": "F51C4643-58E6-DF11-971B-001D60E938C6" } } ]

3) describe-business-process -> every parameter you bound now appears with its source and value.
   The ones you did NOT bind stay hidden. That is expected; it is not a failure.

Variant — the same task ASSIGNED TO A TEAM ("the sales department calls the client"): drop the OwnerId
mapping from step 2 and set the element-level performer instead (works inline in step 1's element too):
   [ { "op": "setElement", "elementName": "CallClientAboutRenewal",
       "elementUpdate": { "performer": { "type": "role", "role": "Sales Department" } } } ]
Look the role name up on the environment first (SysAdminUnit; a role with no users means a task nobody
sees). And when the request says "a CALL task", set ActivityCategory to the environment's Call category and
SAY the Activity's TYPE is still Task — see the Type-is-not-settable rule at the top of this section.

== Activity connections ("Connected to") ==
- WHAT: which records the Activity a task creates is attached to — a contact, an account, and whatever else
  the environment registers as a connection; the set is per-environment, never a fixed list.
  It is functional, not decorative: set, the task appears on the connected record's Activities detail and
  its Timeline and the page fields are pre-filled; unset, none of that happens. An email counts as
  "processed" only with Account or Contact PLUS one further connection.
- HOW: `modify-business-process` → `setConnections` with `elementName` and
  `connections:[{ column, <exactly ONE source> }]`. Sources: `recordId` (a fixed record) |
  `processParameter` | `sourceElement` + `sourceElementParameter` | `expression` (a raw macro — for the
  CURRENT USER see the dedicated rule below, it is the one macro you may author here).
  `referenceSchema` is optional, belongs to `recordId` ALONE, and is a
  CHECK rather than a source — sending it with any other source is refused, because the entity of those is
  whatever the source resolves to.
- `recordId` NEEDS NO SCHEMA UId. The server composes `[#Lookup.{schemaUId}.{recordId}#]` from the target
  column's own reference entity, so send the bare record id. This is the one place the "you cannot guess
  these ids" warning ABOVE does not apply — for a connection, do NOT hand-write the Lookup token.
- CURRENT USER — "link it to me / to my contact / to my account". This is the ONE macro you may author on a
  connection, because the set is CLOSED and named here. Send it as `expression`, chosen by the target
  column's own entity: a Contact column -> `[#SysVariable.CurrentUserContact#]`; an Account column ->
  `[#SysVariable.CurrentUserAccount#]`; a SysAdminUnit (user) column -> `[#SysVariable.CurrentUser#]`.
  Those three are the WHOLE set usable as a connection. Do not invent a fourth (`CurrentUserAccountId`,
  `CurrentAccount`, …), and do not go looking one up: system variables are neither an entity nor an entity
  schema, so `odata-read` answers 404 and `find-entity-schema` answers empty for them — that is those tools
  being right, not the variable being absent. Spell them EXACTLY as above, because what a wrong name costs
  depends on the environment's CrtProcessBuilder and BOTH outcomes are bad: a current build refuses it at the
  write, naming the valid alternatives; an older one stores it unchecked, and the process then fails to
  COMPILE later, far from the edit and with nothing pointing back at the connection.
  One caveat that is data rather than syntax: `CurrentUserAccount` writes EMPTY when the running user's
  contact has no account — where `CurrentUserContact` raises an error in the same situation, the Account
  side stays silent. If the Account link comes back unset, check the user's contact before suspecting the
  macro.
- UPSERT, keyed on `column`. The columns you list are set or re-set; every column you do NOT list is left
  alone. There is no collection-replace and no implicit clearing — so changing one connection can never
  disturb another, and clearing is only ever explicit via `clearConnections`.
- Changing a connection, INCLUDING across dialects (a process parameter → a fixed record), is the same
  `setConnections` call with a new source. Re-sending an unchanged request is idempotent.
- `clearConnections` takes `connections:[{ column }]` and UNBINDS — the element parameter stays. A source
  on a clear entry is rejected. Clearing an already-unbound column is a no-op, not an error. It REPORTS
  what it cleared, and you need that: a cleared connection disappears from describe, so afterwards
  "cleared" and "never bound" are indistinguishable from the read-back alone.
- READ IT BACK with `describe-business-process`: each element carries `connections[]`, every entry giving
  both the raw macro (`value`) and a decoded source in exactly the shape `setConnections` accepts, so you
  can feed it straight back — with FOUR exceptions that refuse on re-apply, none of them values you wrote:
  they are what a designer, an older build, a hand edit or another environment left behind.
  (1) a fixed-record connection whose stored macro names a different entity than its column. TWO remedies,
      and they are not interchangeable: re-send the raw `value` as `expression` to keep the stored macro
      exactly as it is, or omit `referenceSchema` to re-point the connection at the column's OWN entity —
      which rewrites the macro and is a repair, not a re-apply. Choose deliberately;
  (2) a stored value with no macro shape at all (check `source`; it comes back as `expression`) — refused as
      "not a platform macro", because a bare value cannot be a source. Use `recordId`;
  (3) a stored value that IS macro-shaped but from a family that cannot hold a record id — `DateValue`,
      `DateTimeValue`, `TimeValue`, `BooleanValue`. `[#SysSettings...#]` is the one family accepted instead
      of refused, with a warning (below), precisely so designer-authored processes stay re-appliable;
  (4) a `[#SysVariable...#]` whose name does not resolve on THIS environment, or resolves to a variable that
      cannot hold a record id (`CurrentDate`, `CurrentUserRoles`, …). Unlike (1)-(3) this one depends on where
      you are: a current `CrtProcessBuilder` checks the name against the platform's own vocabulary, an older
      one does not, so the same read-back re-applies on one environment and is refused on another. It appears
      when process metadata travelled from a different platform version, or when a connection was hand-edited
      — a designer cannot produce it. Re-point the connection rather than forcing the stored value through.
  Each entry also carries `registered` — `false` means the value IS written at run time but the connection
  is invisible to every registry-reading feature, the same caveat as the write warning below — and `source`,
  the platform value source. Only BOUND connections appear, so absence does NOT mean the column cannot be
  connected; and the WHOLE array is absent when the host entity cannot be resolved or the registry cannot be
  read, so "no connections" is never verified-empty. A macro this build does not recognise degrades to
  `expression` rather than breaking the read.
- WHEN IT IS REFUSED, and why each refusal is worth reading rather than retrying:
  * the user task is not one connections are supported on. The supported set is exactly SIX —
    `ActivityUserTask` (`performTask`), `EmailTemplateUserTask`, `UserQuestionUserTask`,
    `OpenEditPageUserTask`, `AutoGeneratedPageUserTask`, `PreconfiguredPageUserTask` — and anything else,
    including a CUSTOM user task and `approvalUserTask`, is refused with the supported list quoted. A
    non-user-task element is refused too (it creates no record), as is one whose user-task schema does not
    resolve on the environment. This is the most likely refusal in practice, so check it first;
  * the user task's runtime never writes connections (`CallUserTask` builds its Activity directly;
    `EmailUserTask` and `SendEmailUserTask` have none; `readData` creates no activity at all) — model a
    call as `performTask` with the Call activity category instead, and set that category with `addMapping`
    (see the next bullet — `ActivityCategory` is not a connection);
  * the column is not a CONNECTION at all — `ActivityCategory` and `ShowInScheduler` are written through
    their own path and with their own encoding, so use `addMapping` for them. Binding one as a connection
    would set the column and silently degrade the element, which is why it is refused rather than accepted;
  * they would not TAKE EFFECT on this element — almost always `CreateActivity` left at its `false`
    default, which produces a process that saves, compiles, runs green and writes nothing. The refusal
    quotes the exact operation to PREPEND to your own array, so the fix costs one array element, not
    another round trip. `performTask` never hits this: it has no such parameter. A manual-send
    `EmailTemplateUserTask` does not either — the manual sender has no gate;
  * the column is not one this element can carry, or the host entity has no such column at all. Those are
    DIFFERENT diagnoses: the second needs a data-model change (add the lookup column to Activity and
    register it), which `setConnections` deliberately does not make;
  * an `expression` that is not a platform macro at all (it must look like `[#...#]`; a bare value is
    refused — use `recordId`), or one whose macro family cannot hold a record reference (a date, time or
    boolean constant);
  * `referenceSchema` sent without `recordId` — it is a check on the fixed-record source only, so accepting
    it elsewhere would ignore it;
  * a malformed `recordId`, a column that references no entity, or a `processParameter` / `sourceElement`
    of an incompatible type (same type group, and for a lookup the same reference entity — a `Guid` or a
    same-entity Lookup parameter is what works).
- WHEN THE CONNECTION DOES NOT EXIST YET — linking an activity to a record of YOUR OWN entity. This is the
  common ask ("add a button that creates a task linked to this record"), and it is the ONE case that needs a
  DATA-MODEL change, which `setConnections` deliberately does not make for you.
  Do NOT decide whether you are in that case by inspecting the object first — let the OPERATION tell you,
  because the surfaces disagree with each other. Measured for ONE lookup column on one environment: the
  physical `Activity` table carried it, `get-entity-schema-properties` listed it, the object designer did not
  show it, and a process wrote its value successfully. In the other direction, several connection columns
  existed physically while being ABSENT from the schema. WHICH columns those are is a property of the product
  and the installed package chain, not of Creatio — so no list belongs here, including a list of "the
  connections Creatio ships": whatever it named would be wrong on some environment. The refusals ARE the
  check, and they distinguish three states:
  * `<host> has no '<column>' column` — the data-model change below is required;
  * `the column exists on <host> but no connection-registry row registers it and this element's user task
    declares no parameter for it` — only step 2 is required;
  * anything else, including success — there was nothing to add.
  1. add a Lookup column to `Activity` IN THE PACKAGE THAT OWNS THE REFERENCED ENTITY — not in `Custom`, and
     not as a matter of taste. `Custom` is the LAST package: it depends on the others and nothing depends on it
     (measured — `Custom` depends on the platform core, the app package and a product package; no edge points
     back). So a schema in the
     entity's own package cannot reference a column placed in `Custom` without adding the REVERSE edge, and that
     inverts an existing one: the save is refused with "Cyclic dependencies detected", naming
     `EntityColumnValues.Column.<yours>`. Placed in the referenced entity's own package the column needs NO new
     dependency at all, and the environment's existing custom sections show the same shape — each carries its
     own replacing `Activity` layer.
     The call is `update-entity-schema`, which is NON-RESIDENT, so send it through `clio-run`. Args:
     `environment-name`, `package-name` (the REFERENCED entity's), `schema-name: "Activity"`, and
     `operations` — an ARRAY of operation objects, one here:
     `{"action":"add","column-name":"Usr<YourEntity>","type":"Lookup","reference-schema-name":"<your entity>","indexed":true}`.
     The first four keys are all required — omitting `column-name` is the easy mistake, since the column being
     added is named nowhere else; `indexed` is optional and worth setting on a column you will filter by.
     Measured: the column lands and the schema republishes in ~13 s, and reads back as `source: own`.
     A `Reference schema '<your entity>' was not found` refusal means the TARGET package cannot see the
     REFERENCED entity — that, not the `Activity` side, is the dependency that blocks, and the placement above
     is what makes it a non-issue.
     CAVEAT — measured only where that package ALREADY had a replacing `Activity` layer; with no layer yet
     this step takes a path nothing has exercised.
  2. register the column as a connection — ONE bound row in `EntityConnection`, through the
     `create-data-binding-db` tool (also non-resident, also via `clio-run`). Args: `package-name` (yours),
     `schema-name: "EntityConnection"`, `binding-name` (e.g. `"EntityConnectionUsr<YourEntity>"`), and
     `rows`: `[{"values":{"SysEntitySchemaUId":"c449d832-a4cc-4b01-b9d5-8a12c42a9f89","ColumnUId":"<u-id>"}}]`.
     `SysEntitySchemaUId` is Activity's ROOT schema UId — that literal. The column's `u-id` comes from
     `get-entity-schema-properties` (resident, call it natively), NOT from
     `get-entity-schema-column-properties`, whose response carries no `u-id` at all. `rows` is load-bearing:
     without it the tool creates an EMPTY binding and nothing is registered. The package must be non-foreign.
  3. `setConnections` on the element. The element may predate the column by any amount — the operation
     creates the element parameter when the user task declares none.
  Skipping step 2 is not fatal, and the mechanism is worth knowing rather than guessing: the binder resolves a
  column through the registry OR through a parameter the user task already DECLARES, so a declared connection
  binds and writes — with a caveat in the log — even with no registry row. Measured: an `Opportunity`
  connection written by a process on an environment whose registry carried 17 rows, with a Next Steps
  component then displaying the activity. What registration buys is availability to EVERY element rather than
  only to a task that happens to declare that parameter, plus visibility to the surfaces that read the
  registry. After step 2 the designer may keep showing the old set until its caches refresh; the run-time
  write is unaffected.
  Do NOT offer, as a lighter alternative, writing the record's NAME into the activity's title or description.
  That produces no link — no Activities detail, no Timeline, no pre-filled fields — and the ask was a link.
- SUCCEEDS WITH A WARNING, two cases. A column that exists but has no connection-registry row IS written at
  run time, yet the connection is ignored by the record page's connections detail, Next Steps, email
  auto-relation rules and quick-add, and is normally absent from the designer's "Connected to" as well —
  except `Project`, which the designer injects client-side and DOES display. And an `expression` in the
  `[#SysSettings...#]` family is accepted unchecked: its value type cannot be read at design time, so a
  setting that does not hold a record id leaves the column empty at run time. Read the caveats — they arrive
  as `message-type: "Warning"` entries in `execution-log-messages`, NOT as a `warnings` field on the
  response, so finding no such field is not evidence there were none. Some are neutral acknowledgements (a
  column that was already unbound), not failures.
- `addMapping` is NOT deprecated — it remains the general primitive — but it only reaches a connection the
  element ALREADY declares as a parameter and fails with "has no parameter" otherwise; the two page tasks
  (`AutoGeneratedPageUserTask`, `PreconfiguredPageUserTask`) declare none, and since unbound element inputs
  are omitted from describe you cannot tell in advance. Prefer `setConnections`: it creates the parameter
  when one is needed, and adds the validation, the `recordId` ergonomics and the read-back.
- Connections are NOT graph edges. `validate-process-graph` neither checks nor is affected by them; R1-R17
  below are about sequence flows only.

== Connection rules R1–R17 (validate-process-graph enforces the structural subset: R1–R3, R7,
   R9–R15, R17; R4–R6, R8 and R16 are semantic or not yet enforced — verify those yourself.
   Validation pass ≠ buildable: the rules cover the FULL catalog incl. gateways and conditional
   flows, but only the "What you can build today" slice above can actually be built) ==
R1  Start event: no incoming flow; exactly one outgoing.
R2  End event: no outgoing flow; one or more incoming.
R3  Exactly one top-level start event; every path reaches an end event.
R4  Terminate end kills the whole instance; Simple end ends only its path.
R5  Start triggers: Simple=user/run; Signal(object)=record add/modify/delete; custom signal=broadcast; message=directed; timer=schedule/CRON.
R6  Diverging gateway: 1 in, >=2 out. Converging gateway: >=2 in, 1 out.
R7  Exclusive(OR) diverge: conditional flows + exactly one default; one path taken. Converge: first arrival, no sync.
R8  Parallel(AND) diverge: all out fire, plain sequence flows only. Converge: waits for all incoming.
R9  Inclusive(OR) diverge: conditional flows + required default; >=1 path. Converge: syncs active branches.
R10 Event-based gateway: each outgoing sequence flow leads directly to an intermediate catch event; first event wins.
R11 Parallel and event-based gateways must not carry conditional/default flows.
R12 Sequence flow: target runs after source. Multiple outgoing sequence flows = implicit parallel split.
R13 Conditional flow originates only from a gateway or an activity.
R14 Default flow is legal only if >=1 conditional flow leaves the same element; diverging Exclusive/Inclusive require a default.
R15 No orphan/unreachable nodes; every flow needs a valid source and target.
R16 Sub-process (callActivity) target must begin with a Simple start; collection mapping => multi-instance.
R17 (advisory) Add data one-record mode outputs only Id; chain a Read data for other fields.

Quick can/can't (source -> target via sequence flow): start->{activity,gateway,intermediate,end} ok,
never ->start (R1); end is a sink, never a source (R2); event-based gateway out must hit a catch event (R10).
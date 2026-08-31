clio MCP process-naming guide — name a business process, its elements and its parameters

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the N1-N10 rules for the process caption and code, element captions and codes, and parameter codes.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

== Naming and codes (N1-N10) ==
(AUTHORING rules for the names and codes you choose. They are numbered N-, deliberately NOT R-: nothing
pre-checks them — `validate-process-graph` enforces a subset of the R1–R17 connection rules (which live
in `process-activity-connections`) and enforces none of these. The reader they are written for is a no-code team opening the result in the Process
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
    two generations of the same request undiffable. Every element example across the process guides is derivable
    this way, and `GuideExamples_ShouldDeriveEveryElementCodeFromItsCaption` holds them to it — where a
    caption and a code disagree, the caption is the input and the code is what is wrong.
N6  An element code MUST NOT contradict the element's RUNTIME type. `endEvent` currently builds a
    `ProcessSchemaTerminateEvent` — a Terminate end, not a Simple end — so `EndNormal` on one is a lie the
    code tells about the element (ENG-94378: the baseline run produced exactly that). The element catalog in `process-modeling`
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
N10 Sequence-flow labels — NOT YET BUILDABLE. There is no label field on a flow: `flows[]` takes
    `source` and `target` and nothing else, so this rule cannot be applied yet by any route. Recorded
    here so the catalog is complete, the same way the R1–R17 header in `process-activity-connections`
    separates the full catalog from the buildable slice. When labels land: label a conditional flow with
    the decision outcome it represents (`Budget > 10 000` — a human-readable caption, not the condition's
    own text), and label the default flow explicitly rather than leaving it blank.
    Do not read this rule as a statement about FLOWS. A conditional flow itself IS buildable — build it
    plain, then `setFlowCondition`; see `process-formulas`. Only the LABEL is missing, along with default
    flows and gateway ELEMENTS (ENG-91853 extends that).
    "Connections" in a naming review means these SEQUENCE FLOWS. The Activity "Connected to" links are a
    different feature with its own article (`process-activity-connections`) and no naming surface at all.

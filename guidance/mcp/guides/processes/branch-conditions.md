# Conditional flows and branch conditions

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
Split out of `process-formulas` because that article had no budget headroom left: the two halves are
read at different times - the vocabulary while authoring any formula, this while planning a BRANCH.
The formula vocabulary, the reference syntax, what each refusal names and the length bound stay there
and are NOT restated here; fetch both when you author a condition.

== Conditional flows and branch conditions ==

HOW A CONDITION NAMES A PARAMETER DEPENDS ON WHICH CALL YOU ARE IN, and getting it wrong costs the
whole call. The RUNTIME only ever resolves a UId meta-path — `[#[Parameter:{uid}]#]`. On
`modify-business-process` that is what you write, and `describe-business-process` gives you the UIds.
On `create-business-process` you write the NAME instead — `[#Amount#]`, `[#Element.Parameter#]` — and
the server expands it, because on create the UId does not exist yet. Both are spelled out below; read
"REFERENCING A PARAMETER" under FORMULAS for the meta-path form itself.

A branch is a flow with a CONDITION, and you DECLARE it where you declare the flow:

    "flows": [
      { "source": "Check", "target": "Approve", "kind": "conditional", "condition": "...",
        "label": "Approved" },
      { "source": "Check", "target": "Reject",  "kind": "default", "label": "Everything else" }
    ]

`kind` is `sequence` (the default) | `conditional` | `default`, and a `conditional` flow REQUIRES a
`condition`. The same fields are on `addFlow`.

**LABEL BOTH ARMS.** `flows[].label` is the text the designer draws ON the connector, and on a branch it
is not decoration: without it a two-branch decision renders as two identical unlabelled arrows and a
reader has to open each one's properties to tell them apart. It is what the shipped product does on the
large majority of its conditional flows and a quarter of its default ones, and almost never on a plain
sequence flow — so label the arms and leave an ordinary continuation bare. `process-naming` N10 carries
the measured figures and their population.

Name the OUTCOME, never the condition. `Approved`, `User Not Found`, `no record found`, plain
`Yes` / `No` — the corpus is business phrases, and repeating the expression is the thing to avoid,
because it is already one click away on the flow itself. `process-naming` N10 owns the wording rule in
full. EDITING one is destructive and this article owns that: an EMPTY `label` CLEARS a designer's
caption, so read the three-state contract under **ON THE MODIFY PATH** below before any edit, and
`describe-business-process` first — it reports each flow's `label`, which is the only way to learn a
human's label is there to be erased.

**ON THE BUILD PATH, WRITE THE NAME.** This is the one exception to the UId rule above and it exists
because it has to: on `create-business-process` the UIds do not exist yet — the parameters and elements
are made by that same call — so `flows[].condition` takes a name and the server expands it once
everything exists.

    { "source": "Check", "target": "Approve", "kind": "conditional",
      "condition": "[#Amount#] > 100", "label": "Approved" }        // a process parameter
    { "source": "Check", "target": "Escalate", "kind": "conditional",
      "condition": "[#Priority#] == \"High\"", "label": "Escalated" } // and another

`[#Element.Parameter#]` is expanded too, for an element's OWN output parameter — but read what that reaches
before you rely on it. A `readData` in `first` mode, the only mode clio builds, exposes exactly one output
and it is a RECORD (`ResultEntity`). Testing one of its COLUMNS needs a third meta-path segment
(`[EntityColumn:]`) that no name can express, so a column test goes through the modify path. That is not a
corner: of the 487 element-output conditions in the shipped corpus, 242 are column tests and 245 are not.

> Do NOT reach for `[#Read.ResultCount#]`. `ResultCount` is a declared parameter, so the name resolves, the
> condition stores, and `describe` reads back clean — but `ReadDataUserTask.HandleResult` assigns it only in
> `function` mode with `FunctionType == Count`, and returns before it in every other mode. On anything clio
> builds it stays 0, so `> 0` never fires and the fallback always runs. See `process-data-elements`.

`[#SysSettings.Code<Type>#]`, `[#Lookup.Schema.Record#]` and an already-written meta-path are passed
through untouched. A name that resolves to nothing is refused before anything is saved, naming the flow
and listing the parameters that do exist — which is the whole reason to write the name rather than
guess: without it the platform answers `Formula value error: Expression expected (at index 0)`, naming
neither, and the entire call is aborted.

**ON THE MODIFY PATH, WRITE THE META-PATH.** There is no expansion there and none is needed: the process
exists, so `describe-business-process` reports every UId. The two-step route — build the flow plain,
then `setFlowCondition` — is what you use on a flow that ALREADY exists, including a designer-authored
one. To change an existing flow's kind in either direction, `setFlow` takes `source`, `target`, `kind`,
(for a conditional one) `condition`, and an optional `label`.

The `label` argument has THREE states and the difference is destructive: OMITTING it keeps whatever
label the flow has, an EMPTY string CLEARS it, and text replaces it. Omitting is what you want on any
edit that is not about the label, because a modify normally lands on a designer-authored process where
most conditional flows already carry a human's wording. Since `kind` is mandatory, RELABELLING ALONE
means passing the kind the flow already has — and on a CONDITIONAL flow you must pass its existing
`condition` back too, or the call is refused: `kind: conditional` with no condition is rejected before
anything is written, because an omitted condition would be stored as the literal `true` and make the
branch always taken. So read the flow first and echo both fields. `process-naming` N10 owns the WORDING
rule and the corpus figures.

`removeFlow` takes `source` and `target` ONLY, and `setFlowCondition` takes those plus a `condition`.
Both resolve the flow by the PAIR, so a `kind` (or, on `removeFlow`, a `condition`) carried over from a
`describe` is REFUSED rather than ignored — and the refusal ABORTS THE WHOLE BATCH, which is atomic.
Deliberate, not tidiness: where two flows join the same pair, honouring the edit while ignoring a
`kind` acts on a flow the caller did not name. **MUST strip `kind` and `condition` before a
`removeFlow`.** Version-dependent, so do not rely on it as a backstop: the `removeFlow` refusal ships
from `CrtProcessBuilder` **1.6.0.10** and the `setFlowCondition` one from **1.6.0.11**; below those the
extra fields are accepted and silently dropped, clio does no client-side check, and stripping them
yourself is the whole protection. `process-modeling` owns `removeFlow`'s destructive rule in full.

Both need `CrtProcessBuilder` **1.6.0.3** or newer; below it the build path takes a system setting
only. State the version that SHIPS the behaviour, not the one it first appeared under: the gateway
line was numbered 1.4.0.58 through 1.4.0.70 while it was being built, and no released archive ever
carried those numbers - a 1.5.0.0 minor was cut elsewhere and outranks all of them, so a 1.4.x floor
is satisfied by a server carrying none of this. 1.6.0.3 is the first released archive with the whole
line in it, and it is what clio's own [RequiresPackage] floor demands.

NO GATEWAY IS NEEDED. The platform synthesizes an exclusive gateway for a conditional flow whose source
is not one, so a branch straight off an activity is legitimate — 485 of the 1 406 conditional flows in
the shipped product are exactly that. `exclusiveGateway` and `parallelGateway` ELEMENTS are buildable
too, and adding one is about the DIAGRAM being readable, not about making the branch work.

Three rules apply once you place a gateway element, all enforced server-side and none of them visible
in the descriptor schema:

1. **Out of a gateway that CHOOSES** (`exclusiveGateway`, and a designer-made `inclusiveGateway`), every
   outgoing flow must be `conditional` or `default` — the designer cannot draw a plain flow out of a
   gateway either. An unconditional one is written as the default branch, in ANY declaration order, and
   a notice tells you it was. It is REFUSED only when the gateway already HAS a default: that flow has
   nothing left to become. **Say `kind: "default"` explicitly** and you never read the notice.
2. **At most one `default` per element.** It is "the branch taken when nothing matched", so two make
   that undecidable.
3. **Out of a `parallelGateway`**, which starts every branch, all outgoing flows are plain `sequence`,
   and the same holds for an `eventBasedGateway`, decided by which event arrives. A `conditional` or
   `default` flow off either is REFUSED - the server names the gateway and tells you to use an
   exclusive one. It is not stored and quietly ignored; that is the reason the refusal gives, not
   what happens if you try.

What a condition must satisfy is in `process-formulas`, under WHAT IS CHECKED — it is validated as a formula whose target
type is `bool`, so an integer is refused, and an empty one is refused because the platform stores it as
the literal `true`. Specific to a branch: a condition on a DEFAULT branch is refused.

**A PLAIN sibling flow IS the else branch — off an ACTIVITY.** This is the single most useful fact about
branching here and it is easy to miss: the platform treats any non-conditional flow leaving the element as
the default, and takes it only when no condition matched. So `if/else` off an activity is one `conditional`
flow plus a plain one; you do not need a "default flow" element. Out of a GATEWAY element the plain flow does not
stay plain: rule 1 above applies, and it is written as that gateway's `default` branch with a notice
saying so. Declaration order does not change the outcome — it did until the fix that shipped in
1.6.0.3 (numbered 1.4.0.65 on the delivering branch),
where declaring the conditional arm first (which the precedence advice below tells you to do) aborted
the whole `create-business-process` call. Off a gateway, write the else branch as `kind: "default"` and
the notice does not arise. R7 does NOT apply to this shape - not "is satisfied by it":
`process-activity-connections` owns R1-R18 and states why, and the difference is operational. The
gateway is synthesized at generation time and never appears as a graph node, so there is no
exclusive-diverge node for R7 to judge. Read "satisfied" and you would dismiss a genuine R7 finding
elsewhere in the graph as already handled.

Two consequences worth having before you build:

- **Give every branching element ONE fallback, and write it the way that element takes.** If no condition
  matches and there is no fallback, the run FAILS rather than falling through. Two mutually-negated
  conditions look safe and are not: when the parameter is null both are false and the process throws.
  Off a gateway that CHOOSES — `exclusiveGateway`, or a designer-made `inclusiveGateway` — the fallback
  is the `default` branch: write `kind: "default"`, or write `sequence` and the server normalises it,
  because such a gateway has no other kind of unconditional branch. This does NOT generalise to every
  gateway: a `parallelGateway` or `eventBasedGateway` starts or selects every branch itself, takes
  `sequence` ONLY, and refuses a `default` — it has no fallback to give because it never chooses. Off an
  ORDINARY element the fallback is a single plain flow.
  **A fallback catches an unmatched VALUE, not a missing one.** An unset numeric parameter does not
  arrive as "no value" — it arrives as its type's default, and for Integer that is `0`
  (`IntegerDataValueType.DefValue`). So a gateway whose first branch is `[#Amount#] < 100` takes that
  branch when nothing was passed, and a `default` written to catch the empty case never fires. Measured
  on a stand: four runs of one three-way gateway routed 50, 500 and 5000 correctly and routed the
  no-value run to `< 100`. Nothing errors, so the branch is silently unreachable rather than broken.
  Test the empty case explicitly, or make the first condition exclude the default value.
  ONE either way: a conditional branch beside TWO flows that have none is refused, because the platform
  drops one of them and runs the other alongside the branch the condition chose. Refused on BOTH
  paths, from the 1.6.0.3 archive on: the check lives in the flow-kind rules, which `addFlow` and
  `setFlow` go through exactly as the create path does. Do not read this as covered by graph
  validation — `process-modeling` is right that the modify path runs none; this refusal is a
  different mechanism that happens to guard the same shape, which is why modify is not the hole
  here that it is for the structural rules.
- **A diverging gateway with no fallback still BUILDS — the warning does not block it.**
  `validate-process-graph` reports R7/R9 and `create-business-process` saves the process anyway, so a
  clean build is NOT evidence that the conditions cover every case. Only a run is. What the warning
  predicts is exact — on a stand, an unmatched value produced `status: error` and a
  `SysProcessLog.ErrorDescription` reading "None of the conditions were met after the element ...",
  word for word what R7 said it would — but the prediction arrives as advice, not as a refusal.
- **Do not leave a branching element with only plain flows.** The platform synthesizes the exclusive gateway
  only when at least one outgoing flow is conditional; with all of them plain there is no gateway and EVERY
  outgoing flow is taken. That is a parallel split, silently.

That second point is what makes CLEARING a condition the dangerous edit, and it is why the clear-condition
operation is `setFlow` rather than remove-and-add. `setFlow` re-kinds the flow in place: it keeps the flow's
position in `flows[]` — which is its precedence — and it REFUSES the one edit that would silently reshape
the process, namely dropping the LAST conditional flow off an element that still has other outgoing flows.

**Off a gateway, `sequence` does not survive as `sequence` — it becomes the default.** A deciding
gateway's outgoing flows must each say how they are chosen, and the designer cannot draw a plain flow out
of one, so `kind: "sequence"` there is normalised to `default` and a notice says so. It is REFUSED only
when the gateway already has a default: that flow has nothing left to become, and you must give it a
condition or re-kind the existing default first. Re-kinding the gateway's OWN default to `sequence` is a
no-op — it stays the default, silently, because clearing the marker would leave the gateway with no
fallback at all. Remove-and-add is guarded by nothing and loses the position as well: do it and the
exclusive branch becomes a parallel one, with `describe` reporting `kind: "sequence"` on both flows — which
reads exactly like "condition cleared, as asked".

To CHANGE a condition rather than clear it, call `setFlowCondition` again: it overwrites in place and keeps
the flow's position. To make a branch always taken, set its condition to `true` and leave the kind alone —
which is also the way past the refusal above, and the only one that keeps the gateway.

BRANCH PRECEDENCE IS FLOW ORDER among the formula-bearing siblings, and the order is inspectable: `flows[]`
in `describe-business-process` is emitted in the stored order, which is the order the runtime builds. Where
two formula branches leave the same element, the FIRST whose condition is true is taken — so a branch that
fires above 100 and one that fires above 1000 resolve differently purely by which was added first. Add the
most specific FIRST, and say which order you chose and why, because nothing but the order records the
intent.

One exception, and it matters on a Perform-task element: a branch chosen by the activity's RESULT is
evaluated BEFORE any formula branch, whatever the flow order says. So on an element that already has a
result-driven branch, adding a formula branch does not put you in a race you control by ordering — the
result branch wins. `describe` marks those with `branchesOnActivityResult: true`, and `setFlowCondition`
refuses to write a condition onto one.

A conditional flow reads back through `describe-business-process` as `kind: "conditional"` with its
`condition` text. That confirms what was STORED, not what will run: a flow with
`branchesOnActivityResult: true` reports its text and ignores it.

Corpus-attested condition shapes, most common first — these are what real processes use. `X`, `A` and
`B` stand for a REFERENCE TOKEN (`[#[Parameter:{uid}]#]`, a system variable, a system setting), never for a
parameter's name:
`X != Guid.Empty`, `X == true`, `X == "text"` / `X.Equals("text")`, `A && B`, numeric comparisons, a bare
boolean parameter, lookup-record equality, parameter-to-parameter comparison, `!string.IsNullOrEmpty(X)`,
`A || B`, `.Contains("x")`, `X != null`, `!X`, and date comparisons against `DateTime.MinValue`.

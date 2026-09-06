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
      { "source": "Check", "target": "Approve", "kind": "conditional", "condition": "..." },
      { "source": "Check", "target": "Reject",  "kind": "default" }
    ]

`kind` is `sequence` (the default) | `conditional` | `default`, and a `conditional` flow REQUIRES a
`condition`. The same two fields are on `addFlow`.

**ON THE BUILD PATH, WRITE THE NAME.** This is the one exception to the UId rule above and it exists
because it has to: on `create-business-process` the UIds do not exist yet — the parameters and elements
are made by that same call — so `flows[].condition` takes a name and the server expands it once
everything exists.

    { "source": "Check", "target": "Approve", "kind": "conditional",
      "condition": "[#Amount#] > 100" }                              // a process parameter
    { "source": "Read", "target": "Handle", "kind": "conditional",
      "condition": "[#Read.ResultCount#] > 0" }                      // an element's output

`[#SysSettings.Code<Type>#]`, `[#Lookup.Schema.Record#]` and an already-written meta-path are passed
through untouched. A name that resolves to nothing is refused before anything is saved, naming the flow
and listing the parameters that do exist — which is the whole reason to write the name rather than
guess: without it the platform answers `Formula value error: Expression expected (at index 0)`, naming
neither, and the entire call is aborted.

**ON THE MODIFY PATH, WRITE THE META-PATH.** There is no expansion there and none is needed: the process
exists, so `describe-business-process` reports every UId. The two-step route — build the flow plain,
then `setFlowCondition` — is what you use on a flow that ALREADY exists, including a designer-authored
one. To change an existing flow's kind in either direction, `setFlow` takes `source`, `target`, `kind`
and (for a conditional one) `condition`.

Both need `CrtProcessBuilder` 1.4.0.60 or newer; below it the build path takes a system setting only.

NO GATEWAY IS NEEDED. The platform synthesizes an exclusive gateway for a conditional flow whose source
is not one, so a branch straight off an activity is legitimate — 485 of the 1 406 conditional flows in
the shipped product are exactly that. `exclusiveGateway` and `parallelGateway` ELEMENTS are buildable
too, and adding one is about the DIAGRAM being readable, not about making the branch work.

Three rules apply once you place a gateway element, all enforced server-side and none of them visible
in the descriptor schema:

1. **Out of a gateway that CHOOSES** (`exclusiveGateway`, and a designer-made `inclusiveGateway`), every
   outgoing flow must be `conditional` or `default` — the designer cannot draw a plain flow out of a
   gateway either. An unconditional one is written as the default branch ONLY while it is the gateway's
   ONLY outgoing flow; once any sibling exists it is REFUSED, and that includes the ordinary if/else
   shape where a `conditional` arm was declared first. **Say `kind: "default"` explicitly** and the
   question never arises.
2. **At most one `default` per element.** It is "the branch taken when nothing matched", so two make
   that undecidable.
3. **Out of a `parallelGateway`**, which starts every branch, all outgoing flows are plain `sequence`.
   A condition there would be stored and never evaluated.

What a condition must satisfy is in `process-formulas`, under WHAT IS CHECKED — it is validated as a formula whose target
type is `bool`, so an integer is refused, and an empty one is refused because the platform stores it as
the literal `true`. Specific to a branch: a condition on a DEFAULT branch is refused.

**A PLAIN sibling flow IS the else branch — off an ACTIVITY.** This is the single most useful fact about
branching here and it is easy to miss: the platform treats any non-conditional flow leaving the element as
the default, and takes it only when no condition matched. So `if/else` off an activity is one `conditional`
flow plus a plain one; you do not need a "default flow" element. Out of a GATEWAY element it is different, and
in a way that costs a round trip if you get it wrong: rule 1 above applies, and a plain flow is written
as a `default` one only while it is the gateway's ONLY outgoing flow. Declare the conditional arm first —
which the precedence advice below tells you to do — and the plain one is refused outright, aborting the
whole `create-business-process` call. Off a gateway, write the else branch as `kind: "default"`. R7 does NOT apply to this shape - not "is satisfied by it":
`process-activity-connections` owns R1-R17 and states why, and the difference is operational. The
gateway is synthesized at generation time and never appears as a graph node, so there is no
exclusive-diverge node for R7 to judge. Read "satisfied" and you would dismiss a genuine R7 finding
elsewhere in the graph as already handled.

Two consequences worth having before you build:

- **Give every branching element a plain sibling.** If no condition matches and there is no plain flow, the
  run FAILS rather than falling through. Two mutually-negated conditions look safe and are not: when the
  parameter is null both are false and the process throws.
- **Do not leave a branching element with only plain flows.** The platform synthesizes the exclusive gateway
  only when at least one outgoing flow is conditional; with all of them plain there is no gateway and EVERY
  outgoing flow is taken. That is a parallel split, silently.

That second point is what makes CLEARING a condition the dangerous edit, and it is why the clear-condition
operation is `setFlow` with `kind: "sequence"` rather than remove-and-add. `setFlow` re-kinds the flow in
place: it keeps the flow's position in `flows[]` — which is its precedence — and it REFUSES the one edit
that would silently reshape the process, namely dropping the LAST conditional flow off an element that still
has other outgoing flows. Remove-and-add is guarded by nothing and loses the position as well: do it and the
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

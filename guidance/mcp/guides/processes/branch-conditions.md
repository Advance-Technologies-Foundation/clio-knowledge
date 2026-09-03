# Conditional flows and branch conditions

Split out of `process-formulas` because that article had no budget headroom left: the two halves are
read at different times - the vocabulary while authoring any formula, this while planning a BRANCH.
The formula vocabulary, the reference syntax, what each refusal names and the length bound stay there
and are NOT restated here; fetch both when you author a condition.

== Conditional flows and branch conditions ==

READ "REFERENCING A PARAMETER" UNDER FORMULAS BEFORE WRITING A CONDITION. Every parameter a condition
names is referenced by its UId meta-path — `[#[Parameter:{uid}]#]` — and never by its name; a bare
`Amount` is refused. Short names appear below to keep the rules readable: they describe the DECISION,
they are not the text you write.

A branch is a flow with a CONDITION. You do not build one — you build a plain flow and then set its
condition:

1. `create-business-process` (or `addFlow`) makes the flow. `flows[].kind` is still refused on the build
   path: a conditional branch cannot be declared there.
2. `modify-business-process` with `setFlowCondition` (`source`, `target`, `condition`) turns that flow into a
   conditional one.

NO GATEWAY IS NEEDED and none is created. The platform synthesizes an exclusive gateway for a conditional
flow whose source is not one, so a branch straight off an activity is legitimate — it is what the platform's
own tests rely on. Gateway ELEMENTS are still not buildable.

What a condition must satisfy is above, under WHAT IS CHECKED — it is validated as a formula whose target
type is `bool`, so an integer is refused, and an empty one is refused because the platform stores it as
the literal `true`. Specific to a branch: a condition on a DEFAULT branch is refused.

**A PLAIN sibling flow IS the else branch.** This is the single most useful fact about branching here and it
is easy to miss: the platform treats any non-conditional flow leaving the element as the default, and takes
it only when no condition matched. So `if/else` is *one* `setFlowCondition` plus a plain `addFlow` — you do
not need a "default flow" element. R7 does NOT apply to this shape - not "is satisfied by it":
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

That second point is why there is no clear-condition operation and why you should not reach for
remove-and-add to get one. If you remove the only conditional flow and add a plain one, an exclusive branch
becomes a parallel one and `describe` shows `kind: "sequence"` on both — which reads exactly like "condition
cleared, as asked". To CHANGE a condition, call `setFlowCondition` again: it overwrites in place and keeps
the flow's position. To make a branch unconditional, set its condition to `true` and leave the kind alone.

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

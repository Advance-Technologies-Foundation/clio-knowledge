clio MCP process-branch-conditions-details guide — the evidence, provenance and rare cases behind the branch rules

A details article of the process guide set, reached through `process-branch-conditions`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the evidence, version provenance and rarer cases behind the
branch rules, in the order the core states them: why both arms of a branch are labelled (the wording
rule itself is `process-naming` N10's), the CrtProcessBuilder floor that both condition paths need (the
paragraph opening "Both need" means the build-path NAME and the modify-path META-PATH), a diverging
gateway that builds with no fallback, the build NOTICE that reports a parallel split (the "It" opening
those paragraphs is that split) and its three caveats, how the diagram draws evaluation order, and the
corpus-attested condition shapes; and, since a second split, the reasoning and counts behind the core's
rules (why the article was split from `process-formulas`, the column-test count, why a pair-resolved edit
refuses a stray `kind`, why R7 has no node to judge off an activity, the no-value gateway measurement) and
a gateway's own default re-kinded to `sequence`. Split out of `process-branch-conditions`, which keeps the rules for
setting a condition, the gateway and default-flow rules, branch precedence, the flow-EDIT contract and
the parallel-split hazard itself; read that first.

**LABEL BOTH ARMS.** `flows[].label` is the text the designer draws ON the connector, and on a branch it
is not decoration: without it a two-branch decision renders as two identical unlabelled arrows and a
reader has to open each one's properties to tell them apart. It is what the shipped product does on the
large majority of its conditional flows and a quarter of its default ones, and almost never on a plain
sequence flow — so label the arms and leave an ordinary continuation bare. `process-naming` N10 carries
the measured figures and their population.

Name the OUTCOME, never the condition. `Approved`, `User Not Found`, `no record found`, plain
`Yes` / `No` — the corpus is business phrases, and repeating the expression is the thing to avoid,
because it is already one click away on the flow itself. `process-naming` N10 owns the wording rule in
full. EDITING one is destructive and `process-branch-conditions` owns that: an EMPTY `label` CLEARS a
designer's caption, so read the three-state contract under **ON THE MODIFY PATH** in
`process-branch-conditions` before any edit, and `describe-business-process mode=full` first — it reports each
flow's `label`, which is the only way to learn a human's label is there to be erased.

Both need `CrtProcessBuilder` **1.6.0.3** or newer; below it the build path takes a system setting
only. State the version that SHIPS the behaviour, not the one it first appeared under: the gateway
line was numbered 1.4.0.58 through 1.4.0.70 while it was being built, and no released archive ever
carried those numbers - a 1.5.0.0 minor was cut elsewhere and outranks all of them, so a 1.4.x floor
is satisfied by a server carrying none of this. 1.6.0.3 is the first released archive with the whole
line in it, and it is what clio's own [RequiresPackage] floor demands.

- **A diverging gateway with no fallback still BUILDS — the warning does not block it.**
  `validate-process-graph` reports R7/R9 and `create-business-process` saves the process anyway, so a
  clean build is NOT evidence that the conditions cover every case. Only a run is. What the warning
  predicts is exact — on a stand, an unmatched value produced `status: error` and a
  `SysProcessLog.ErrorDescription` reading "None of the conditions were met after the element ...",
  word for word what R7 said it would — but the prediction arrives as advice, not as a refusal.

It is no longer a SILENT one at build time. `validate-process-graph` has always reported the shape as
an R12 warning, and from **CrtProcessBuilder 1.6.2.10** `create-business-process` and
`modify-business-process` raise a matching NOTICE — so a caller who skipped the pre-flight is no longer
told less than one who ran it. Both sides WARN rather than refuse, because the shape ships and runs: 74
non-gateway sources in the 7.8.0 corpus carry it, 33 of them on ordinary activities. Read the notice as
"confirm you meant a fan-out", not as a defect.

It is raised ONCE per request, over the graph that is actually saved — so a batch that builds the split
and then dissolves it (`addFlow` twice, then `setFlow kind:"conditional"`, which is the remedy in
`process-branch-conditions`) correctly reports nothing.

Three caveats, each a way you would otherwise be told something untrue:

- **On a stand below 1.6.2.10, you do not get silence — you get a REFUSAL.** No `[RequiresPackage]`
  floor was raised for this notice, but the floor is not what decides: clio refuses whenever the
  environment records a package version *older than the one this clio ships*, whatever that version
  is. So a clio carrying 1.6.2.10 answers `create-business-process`, `modify-business-process`,
  `describe-business-process` **and `validate-process-graph`** with *"This clio carries
  CrtProcessBuilder 1.6.2.10, but the target environment has X. Update the package in the target
  environment and retry."* The pre-flight is gated too, so it is not a fallback.
  `install-process-builder`, once the user approves it (`core-rules`), is the whole remedy. The build is silent about the split only when
  your clio ALSO predates 1.6.2.10, so the two versions converge and nothing is refused — and on that
  stand `validate-process-graph` still reports the shape as R12, which it always has. An
  un-upgraded stand is exactly where the pre-flight earns its keep, not a stand where nothing can
  see the split.
- **On modify it reports the sources your request TOUCHED**, not the whole graph. A split already
  present in a designer-authored process you did not edit stays unreported here — `validate-process-graph`
  is what reads the whole graph, and a clean modify is therefore not evidence of a clean process.
- **The notice covers plain flows only.** A `default` flow beside a plain one off an ordinary element
  is an implicit split too, by exactly the same mechanism, and neither side reports that one.

THE DIAGRAM NOW SHOWS THAT ORDER, which is the one place a human can read it. The layout in
`process-diagram-layout` draws a split's branches top to bottom in evaluation order, with one deliberate
exception: the DEFAULT branch keeps the gateway's own row, because it is the path that runs when nothing
matched and it is what the eye follows across the picture. So the conditional branches read downward in
the order you declared them, and the default is on the spine rather than at the bottom. Two consequences
worth knowing before you write: declare the main path first when a gateway has no default, and expect a
gateway that GAINS a default later to move its existing branches down a row.

== Conditional flows and branch conditions — evidence and reasoning ==
Each paragraph below is the reasoning, evidence or rare case behind a rule `process-branch-conditions`
states; the rule itself stays there.

Why the branch article exists. Split out of `process-formulas` because that article had no budget headroom
left: the two halves are read at different times - the vocabulary while authoring any formula, this while
planning a BRANCH.

A column of a read record in a condition (`[#Element.Parameter.Column#]`). Column tests are not a corner case:
of the 487 element-output conditions in the shipped corpus, 242 are column tests and 245 are not.

Why `removeFlow` and `setFlowCondition` refuse a carried-over `kind` or `condition`. Deliberate, not
tidiness: where two flows join the same pair, honouring the edit while ignoring a `kind` acts on a flow the
caller did not name.

Why R7 does NOT apply to a plain else-flow off an activity. The gateway is synthesized at generation time
and never appears as a graph node, so there is no exclusive-diverge node for R7 to judge. Read "satisfied"
and you would dismiss a genuine R7 finding elsewhere in the graph as already handled.

A fallback catches an unmatched VALUE, not a missing one — the evidence. Measured on a stand: four runs of
one three-way gateway routed 50, 500 and 5000 correctly and routed the no-value run to `< 100`.

A deciding gateway's OWN default — why re-kinding it to `sequence` is a silent no-op (the core states the
trap). It stays the default because clearing the marker would leave the gateway with no fallback at all.

Corpus-attested condition shapes, most common first — these are what real processes use. `X`, `A` and
`B` stand for a REFERENCE (a meta-path token, a `[#Name#]` the server expands, a system variable, a system
setting):
`X != Guid.Empty`, `X == true`, `X == "text"` / `X.Equals("text")`, `A && B`, numeric comparisons, a bare
boolean parameter, lookup-record equality, parameter-to-parameter comparison, `!string.IsNullOrEmpty(X)`,
`A || B`, `.Contains("x")`, `X != null`, `!X`, and date comparisons against `DateTime.MinValue`.

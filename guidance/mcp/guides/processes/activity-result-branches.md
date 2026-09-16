# Branching on an activity result

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
Split out of `process-branch-conditions`, which had no budget headroom left and owns the FORMULA
branch - flow kinds, precedence, the gateway rules and the hazard of clearing a condition. This
article owns the OTHER predicate dialect and is the authority on when a formula is the wrong one.

== Branching on an activity result ==

A conditional flow carries its predicate in ONE OF TWO disjoint slots, and the flow's SOURCE decides
which - not you, and not the operation you reach for. When the source enumerates ACTIVITY RESULTS the
designer edits that connector through a checkbox list headed `What is the result of an element
"<name>"?` and offers no formula field at all. Everywhere else it offers a formula and no checkboxes.
Among the elements clio can BUILD, four do: Perform task, Open edit page, Pre-configured page and
Approval. User dialog, Auto-generated page and the retired Call enumerate results too, but clio cannot
build them - `process-element-catalog` is the authority on what is buildable, and it says so - which
matters because planning a result branch off one of those fails at `addElement`, before this dialect is
ever reached. Each of the four still has to be CONFIGURED into it - a Perform task whose category
carries no result entries, or an Open edit page with the result list off, enumerates nothing and takes
a formula. `process-element-catalog` marks them; each element's own guide
says what its results are.

That list is NOT closed, and do not treat it as one. Other platform elements qualify - Copilot's Execute
intent and lending's Application validation both do - and so does a CUSTOM user task, which does not need
a page of its own: it can point at a platform one, as `CustomActivityUserTask` does. Ask the connector,
not a list: if the designer shows a checkbox editor, it is this dialect.

SEND EMAIL IS THE EXCEPTION IN THE OTHER DIRECTION, and it is worth knowing because every server-side
signal says otherwise. `EmailTemplateUserTask` really does declare the results of its activity category,
so an implementation that asks the SERVER would offer you a selection - but its properties page shows a
FORMULA field, so a selection written there runs on results nobody can see, and opening the connector's
card erases it. Use `condition` on a Send email branch.

WRITE THE SELECTION, NOT A FORMULA. `flows[].results` on `create-business-process` and `setFlowResults`
(`source` + `target` + a non-empty `results`) on `modify-business-process` take the result CAPTIONS -
`["Positive"]` on an approval - or their record ids. From `CrtProcessBuilder` **1.6.2.18**; an older
package has no build-path field and refuses the operation as unknown.

- An UNKNOWN caption is refused WITH the set the element offers. That refusal is the only way to
  discover them - no read API lists an element's results - so budget one refused call rather than
  guessing, and never infer the set from the element's caption.
- An AMBIGUOUS caption, two results sharing one name, is refused pointing at the record id. Result
  captions come from lookups a customer edits, and duplicates there are ordinary.
- The two slots are MUTUALLY EXCLUSIVE on one flow, asymmetrically. Writing `results` CLEARS a stored
  condition - the platform reads the selection FIRST, so an expression left beside it would be
  unreachable metadata that `describe` still reports as a live `condition`. The other direction is
  REFUSED rather than silent, and so is the formula in the first place: a condition written onto ANY
  connector whose source enumerates results is rejected, whether or not a selection is already stored.
  The refusal names the deciding activity and lists what it offers.
- There is no way to CLEAR a selection. A conditional flow carrying neither slot is stored as the
  literal `true` and is then always taken, so `setFlowResults` overwrites in place - call it again to
  change which results select the branch, and it keeps the flow's position, which is its precedence.
- Two sibling branches off one source may not claim the SAME result. The designer cannot express that
  shape - it removes a taken result from the list before drawing it - and the runtime would take both
  branches, a parallel split wearing the clothes of a decision.
- `describe-business-process` reads the selection back as `results` plus `resultsActivity`, the element
  whose results they are. Both are ABSENT below 1.6.2.18, which is the same bytes as a formula branch:
  an all-absent read is not evidence that nothing in the process branches on a result.

A FORMULA THERE IS REFUSED, and the trap it prevents is why. Nothing about the formula fails: it saves,
the schema saves CLEAN, and it RUNS - 7.8.0 falls back to the stored expression whenever the selection
map is empty - so every automated signal would say the branch is finished. It is not. Element validation
runs only when a human opens that element's card, and the connector then opens as an EMPTY panel with no
formula field and nothing ticked, because the designer resolves it as a selection.

The decisive evidence is what the designer itself will not do. Change type -> Conditional flow on such a
connector offers the checkbox list and NO formula option at all - so nothing in the UI invites you to
write a formula there, and a write surface that lets you is being more permissive than the product.

That is a fact about what the designer OFFERS, not about what can exist. Shipped 7.8.0 content does
contain conditional flows in exactly this state, so expect to meet one in a process you did not write.
The refusal is AUTHORING-only for that reason: an existing branch like this still describes, lays out
and re-saves unchanged, and only a new formula on such a connector is refused - naming the deciding
activity and listing what it offers, which is the same list `results` takes.

WHICH DIALECT WINS AT RUN TIME, when a source has siblings of both kinds. A SELECTION branch is
evaluated BEFORE any formula branch, whatever order the flows are written in - and this is a partition
by dialect rather than a tie-break, so flow order cannot change it. `FlowConditionalGateway.Accept`
walks the source's outgoing flows once and puts every flow carrying expression text into a SECOND list
instead of evaluating it; the flows left - the selection branches - are evaluated in that first pass,
and on an exclusive gateway the first one that matches returns immediately, before a single formula has
been read. Only if no selection branch matched are the queued formulas processed at all.

This matters because the authoring refusal is authoring-ONLY: shipped and designer-authored processes
do carry both kinds of sibling off one source, and they still describe, lay out and re-save. When you
MODIFY one, adding a formula sibling beside a result branch does not put you in a race you control by
ordering - the result branch wins. `describe` marks those flows with `branchesOnActivityResult: true`.

(This rule was briefly deleted from `process-branch-conditions` during ENG-91853 and restored here after
a review caught its absence. It was nearly deleted a second time on the grounds that sibling precedence
is array order - which is true WITHIN the formula dialect and says nothing about the partition above.)

WHEN THE CHECKBOX EDITOR APPEARS, in the designer's own terms:

- NO selection recorded yet. The editor appears when the connector's source resolves to exactly ONE
  activity carrying a result parameter - ONE intervening gateway is walked through, via its
  non-conditional incoming flows, one hop only, so two chained gateways escape - AND that activity's
  result set is NON-EMPTY. An empty set brings the formula field back.
- A selection ALREADY recorded. The designer finds the activity by the UId stored IN the selection, so
  the topology test above is skipped and the connector keeps its editor however the diagram changes
  around it. Re-routing is not a way to recover such a branch.

A CONNECTOR LEAVING A GATEWAY works, and it is keyed on the activity BEHIND the gateway rather than on
the gateway itself - which is what `resultsActivity` reports and why you must not assume the source.
The walk is the designer's own and so are its limits: ONE hop, so two chained gateways fall back to a
formula; a gateway's CONDITIONAL incoming flows are skipped, since a branch already decided is not what
decides this one; and EXACTLY ONE upstream activity must enumerate results, because with two there is no
single set to offer. Outside those, the gateway enumerates nothing and a formula is the right dialect.

CAPTIONS AND CULTURE. For an approval the captions are resolved the way the designer resolves them and
match what a human sees. For every other element they come from the platform's own seam, which reads
the localizable result name with a raw database select and therefore answers in the BASE culture - so
on a stand whose culture is not the base one, the captions accepted and listed here may differ from the
ones on screen. Pass the record id when they disagree.

Do NOT read `condition` to tell the dialects apart. The two slots are disjoint in practice as well as in
principle: of the 344 conditional flows in the shipped 7.8.0 corpus that carry no formula, 337 are
selection branches, and every conditional flow there carries a formula OR a result set, never both. The
write path keeps it that way - `results` clears any stored expression. So a selection branch normally
reports `condition: null`, and the rare flow carrying both is designer-authored leftovers whose
expression the runtime ignores. `branchesOnActivityResult` says THAT a selection decides the branch;
`results` says WHICH.

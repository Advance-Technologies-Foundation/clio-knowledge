clio MCP process-diagram-layout guide — how the diagram is drawn, what you can read back, and what an edit does to it

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the PICTURE: where elements are placed, how connectors are
routed, what `describe-business-process` reports about them, and what happens to a diagram somebody
arranged by hand when you edit the process. A rule that lives in another article is cited by its
article NAME and never repeated here, so a name in backticks is a get-guidance topic to fetch.

It exists as an article of its own because `process-modeling` had no budget headroom left, the same
reason `process-element-catalog` moved out before it. Read it whenever a task touches the diagram rather
than the graph — "why does that arrow cross the block", "why is that branch below the other one",
"the customer rearranged this process and now an edit is refused".

== You do not set the layout, but you DO decide it ==
Do NOT set positions or connector geometry: no argument on any tool carries them, and every save
re-derives the whole picture. That is not the same as having no influence. The flow ORDER you declare
is exactly what decides the arrangement, so the rules below are the ones to author against.

== Where elements go ==
- Columns come from distance to the start.
- Each branch of a split gets a row of its own. The DEFAULT flow keeps the split's own row; when there
  is no default, the FIRST flow you declared keeps it. The rest stack below in declaration order.
- A merge returns to the row of the split it closes.
- An end event is drawn beside whatever reaches it. One that a SINGLE flow reaches is pulled to the
  right edge when that row is clear to the last column; one that TWO branches reach is pulled back onto
  the column of the deeper of them, so the branch above drops straight down onto it and the other
  arrives at its left edge. An end that three or more branches reach stays where the columns put it.

Two consequences worth planning for, both of them yours to decide rather than the server's:
- Declare the main path FIRST when there is no default flow, because that is the one that keeps the
  trunk row.
- Expect a gateway that GAINS a default flow later to move its existing branches down a row — the
  default takes the trunk and everything else is re-stacked under it.

== Where connectors go ==
Connectors are computed and stored too — straight, L, Z, U or a longer way round, with loops on a row
of their own.

The guarantee is narrow and worth reading literally: no connector crosses a SHAPE it does not enter.
Arrows MAY cross each other, may run along one line, and may leave a gateway by a vertex a sibling also
uses — a fan-out of two or three gets an exit point each, wider ones share the bottom vertex and
separate at their own rows.

== Reading the diagram back ==
`describe-business-process` returns the DIAGRAM as well as the graph: `position` (a shape's top-left
corner) and `size` per element, and `geometry` per flow — `start`, `points[]`, `end`, `exitSide`,
`entrySide`, i.e. where the connector actually runs.

That is what a question about the picture is answered from, and reading `size` is what lets you turn a
position back into a ROW, since elements of different heights share a row by its centre line — the row
cannot be recovered from the top-left corner alone.

All three are read-only. `position` is always reported; `size` and `geometry` are newer members, and
`geometry` is ALSO absent for any flow the server stored no geometry for, with `points[]` empty on a
straight connector.

== What an edit does to a diagram somebody arranged ==
Every modify re-applies the automatic layout to the WHOLE diagram AND re-routes every connector: a
hand-arranged multi-lane or branched diagram is redrawn as generated rows, and hand-routed arrows are
redrawn with it (process data intact, manual layout lost).

That is not a side effect to work around — stored connector geometry is absolute canvas coordinates, so
anything the engine did not recompute would stay frozen where no shape stands any more. From
CrtProcessBuilder 1.6.5.6 a caption the designer recorded a position for is released with the rest and
returns to the middle of what it names; an OLDER server leaves it pinned, which strands a branch label
at the coordinates the arrow used to pass through.

== The refusal, and the two questions behind it ==
From CrtProcessBuilder 1.6.5.6 the server REFUSES an in-place edit that would re-draw the diagram
rather than extend it. An OLDER server asks nothing and applies it, so on one of those the warning
above is the whole protection.

Two cases reach the refusal: the diagram is not the one the builder lays out, so somebody arranged it
by hand (or an older version drew it) and applying anything replaces that arrangement; or the edit
changes which elements sit above which, so the branches swap places — which is what making a branch the
DEFAULT one does. Shifting elements and inserting one between others are ordinary and never ask.
Nothing is written when it refuses, so there is no half-applied edit to undo.

The refusal is not a yes/no on re-drawing — that question has no good answer, since no loses the edit
and yes loses the picture. It opens a TWO-question sequence, and BOTH answers are the user's:
  1. Show them the sentence it came back with and the elements it names, and ask whether to apply the
     edit as a NEW VERSION. On yes, send the SAME operations to
     `modify-business-process-as-new-version`. That tool never refuses over layout — it reports how the
     new version's diagram differs and creates it anyway — because refusing the remedy the other path
     recommends would leave you with nowhere to go. Their process and its diagram are untouched.
  2. The version is created NOT actual, so nothing runs differently yet, and its response says so on
     every success. Ask the user to open it, look at the diagram, and say whether to make it actual;
     only then call `set-active-business-process-version`. NEVER chain the two — a version is created
     inactive precisely so they get to look first.

Re-drawing the process IN PLACE is the other answer and stays available: re-send the same operations
with `confirm-layout-change`. Offer it second and never send it on the first attempt — it is the
destructive one, and a caller that always confirms has taken the decision away from the person whose
diagram it is.

`process-version-writes` owns how the two questions interact with a session-level answer about
versions; `process-branch-conditions` owns what a default flow means for evaluation order.

clio MCP process-activity-connections-details guide — the R1-R20 connection rules and the connection read-back

A details article of the process guide set, reached through `process-activity-connections`, which points here; `process-modeling` is the set's entry point.
This article is the authoritative owner of the R1-R20 connection rules (the ids `validate-process-graph`
reports), the four stored connection shapes that read back from `describe-business-process` but refuse
on re-apply together with what else a `connections[]` entry carries, and the mechanism that lets a
connection write when the registration step is skipped. Split out of `process-activity-connections`,
which keeps the "Connected to" links themselves — `setConnections` and `clearConnections`, the sources,
the current-user macros, the refusals and warnings, and the three-step recipe for a custom entity; read
that first.

== Reading connections back: the four exceptions that refuse on re-apply ==
  (1) a fixed-record connection whose stored macro names a different entity than its column. TWO remedies,
      and they are not interchangeable: re-send the raw `value` as `expression` to keep the stored macro
      exactly as it is, or omit `referenceSchema` to re-point the connection at the column's OWN entity —
      which rewrites the macro and is a repair, not a re-apply. Choose deliberately;
  (2) a stored value with no macro shape at all (check `source`; it comes back as `expression`) — refused as
      "not a platform macro", because a bare value cannot be a source. Use `recordId`;
  (3) a stored value that IS macro-shaped but from a family that cannot hold a record id — `DateValue`,
      `DateTimeValue`, `TimeValue`, `BooleanValue`. `[#SysSettings...#]` is the one family accepted instead
      of refused, with a warning (see SUCCEEDS WITH A WARNING in `process-activity-connections`), precisely
      so designer-authored processes stay re-appliable;
  (4) a `[#SysVariable...#]` whose name does not resolve on THIS environment, or resolves to a variable that
      cannot hold a record id (`CurrentDate`, `CurrentUserRoles`, …). Unlike (1)-(3) this one depends on where
      you are: a current `CrtProcessBuilder` checks the name against the platform's own vocabulary, an older
      one does not, so the same read-back re-applies on one environment and is refused on another. It appears
      when process metadata travelled from a different platform version, or when a connection was hand-edited
      — a designer cannot produce it. Re-point the connection rather than forcing the stored value through.
  Each entry also carries `registered` — `false` means the value IS written at run time but the connection
  is invisible to every registry-reading feature, the same caveat as the write warning in
  `process-activity-connections` — and `source`, the platform value source. Only BOUND connections appear,
  so absence does NOT mean the column cannot be connected; and the WHOLE array is absent when the host
  entity cannot be resolved or the registry cannot be read, so "no connections" is never verified-empty. A
  macro this build does not recognise degrades to `expression` rather than breaking the read.

== Skipping the registration step ==
  Skipping step 2 (the `EntityConnection` registration in the three-step recipe of
  `process-activity-connections`) is not fatal, and the mechanism is worth knowing rather than guessing:
  the binder resolves a column through the registry OR through a parameter the user task already DECLARES,
  so a declared connection binds and writes — with a caveat in the log — even with no registry row.
  Measured: an `Opportunity` connection written by a process on an environment whose registry carried 17
  rows, with a Next Steps component then displaying the activity. What registration buys is availability to
  EVERY element rather than only to a task that happens to declare that parameter, plus visibility to the
  surfaces that read the registry. After step 2 the designer may keep showing the old set until its caches
  refresh; the run-time write is unaffected.

== Connection rules R1–R20 (validate-process-graph enforces the structural subset: R1–R3, R7–R15,
   R17–R20; R4–R6 are semantic, verify yourself; R16 fires at build time, see its line below.
   Validation pass ≠ buildable: only the "What you can build today" slice in `process-element-catalog`
   can be built — conditional flows, DEFAULT flows, the exclusive/parallel gateway ELEMENTS and Sub-process
   are all in that slice now; inclusive/event-based gateways, timers and intermediate events are not. The
   exclusive gateway the platform
   synthesizes for a conditional branch is still a GENERATION-TIME construct and never appears as a
   graph node, so R7 and R14 do not apply to it: do not model one when you validate a planned branch,
   and do not report a process as violating them because it has one) ==

R1  Start event: no incoming flow; exactly one outgoing.
R2  End event: no outgoing flow; one or more incoming.
R3  At least one start event, at most ONE SIMPLE start; every path reaches an end event. Signal, timer
    and message starts may be SEVERAL - one per trigger - which is how one process runs both when a
    record is added and when it is changed; only the manual launch is capped, a second one being a
    second way to start by hand with nothing to tell them apart. The rule used to read "exactly
    one start event" and refused a shape the platform ships (PublishDraftToArticle has two start
    signals). Two independent floors: CrtProcessBuilder 1.6.2.24 on the environment (older refuses the
    build) and clio 8.1.0.131 or later (older reports an R3 error first).
R4  Terminate end kills the whole instance; Simple end ends only its path.
R5  Start triggers: Simple=user/run; Signal(object)=record add/modify/delete; custom signal=broadcast; message=directed; timer=schedule/CRON.
R6  Diverging gateway: 1 in, >=2 out. Converging gateway: >=2 in, 1 out.
R7  Exclusive(OR) diverge: conditional flows + exactly one default; one path taken. Converge: first arrival, no sync.
    Two WARNINGS, not errors, because the shipped product contains both shapes: a diverging one with no
    default (65 shipped), and one carrying a plain sequence flow (7 shipped) - at run time that flow is
    taken as the default branch, so say so with kind 'default' or give it a condition.
R8  Parallel(AND) diverge: all out fire, plain sequence flows only. Converge: waits for all incoming.
    ENFORCED as a warning: a parallel JOIN fed by two branches that leave one or-gateway BY DIFFERENT
    FLOWS can never fire, because that gateway takes one of them. The instance hangs in Running with no
    error - merge with an exclusive gateway instead.
R9  Inclusive(OR) diverge: conditional flows + required default; >=1 path. Converge: syncs active branches.
    Same two warnings as R7, reported under this id for an inclusive gateway.
R10 Event-based gateway: each outgoing sequence flow leads directly to an intermediate catch event; first event wins.
R11 Parallel and event-based gateways must not carry conditional/default flows.
R12 Sequence flow: target runs after source. Multiple outgoing sequence flows = implicit parallel split.
R13 Conditional flow originates only from a gateway or an activity - a WARNING, not an error. Four
    shipped conditional flows leave an EVENT (two a start event, two an intermediate catch signal
    event) and they run; the server builds the shape without complaint. The finding stays because
    the designer offers no such connection. An EMPTY condition on a conditional flow IS an error:
    the platform substitutes the literal `true`, giving a branch that always fires.
R14 Default flow needs a sibling conditional flow only where the element actually BRANCHES (>1 outgoing),
    and not when a plain sibling leads into a gateway - the decision is then one element further on, which
    is what the platform's own GetOutgoingsDefFlows does. Unscoped this rule called 45 shipped gateways
    invalid: a CONVERGING or-gateway's single outgoing flow is a default one by construction, because the
    designer offers no plain flow out of an or-gateway at all. At most ONE default per element - a second
    is an error. Diverging Exclusive/Inclusive SHOULD have a default (warning, see R7).
R15 No orphan/unreachable nodes; every flow needs a valid source and target, and no flow may connect
    an element to ITSELF. Both halves refuse a self-loop: the build path names the element, and
    `validate-process-graph` reports it under this same id. To repeat an element, route the flow back
    through a gateway that decides whether to repeat it.
R16 Sub-process (callActivity) target must begin with a Simple start; collection mapping => multi-instance.
    Enforced at build time — see `callActivity` in `process-element-catalog`.
R17 (advisory) Add data one-record mode outputs only Id; chain a Read data for other fields.
R18 A conditional flow may have at most ONE outgoing sibling that carries no condition. The platform
    synthesizes a gateway for any element that branches, and that gateway's fallback is every flow
    that is not conditional, of which it removes exactly one before running the rest - so a second
    unconditional flow always starts, beside the branch the condition chose, and both start when
    nothing matched. An ERROR, and the only rule here the shipped corpus does not contradict: 736
    sources carry a conditional flow beside an unconditional one, ZERO carry two, because the
    designer turns the second connection into a conditional rather than drawing it plain.
R19 `results` belongs to a CONDITIONAL flow only. A result selection IS the branch's predicate, so a
    'sequence' or 'default' flow carrying one is refused by the build outright - the server answers
    "A '<kind>' flow cannot carry 'results'". An ERROR. Set 'flow-kind' to 'conditional', or drop
    'results'.
R20 A conditional flow carries a condition OR `results`, never both. The two predicate slots are
    mutually exclusive in the metadata: the platform reads the selection and never the expression once
    the map is non-empty, so the condition would be stored and never evaluated. An ERROR, and the
    message names WHICH slot is ignored - the obvious fix, keeping the text a human can read, is the
    one that silently changes the branch. See `process-activity-result-branches` for the dialect.
    CrtProcessBuilder refuses to build it, from the 1.6.0.3 archive on (the refusal was numbered
    1.4.0.64 on the delivering branch, a number no release carries). Two unconditional flows with NO conditional
    sibling stay legal - that is the R12 parallel split.

Quick can/can't (source -> target via sequence flow): start->{activity,gateway,intermediate,end} ok,
never ->start (R1); end is a sink, never a source (R2); event-based gateway out must hit a catch event (R10).

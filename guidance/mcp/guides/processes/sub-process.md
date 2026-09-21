clio MCP process-sub-process guide — the Sub-process element (callActivity / subProcess)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Sub-process element (`callActivity`, built via
`type:"subProcess"`): the `subProcess` block, naming the callee, how values cross through the element's
own mirrored parameters, `resync`, the refusals, describe's read-back, and what is NOT supported
(multi-instance, event and expanded sub-processes). `process-element-catalog` says the element is
buildable and from which CrtProcessBuilder version; `process-parameters` owns what happens to a CALLER
when the called process's own parameters change. Split out of process-element-catalog.
Naming anything here? Every element, parameter and process code and caption is governed by N1-N10,
owned by `process-naming` — read it BEFORE you name anything, including when you entered at this
leaf rather than through `process-modeling`.

== Element: Sub-process (callActivity -> subProcess) ==
- `callActivity` Sub-process — call ANOTHER process (the BPMN call activity) and run it once, passing
    values through THAT process's own parameters.
    BUILDABLE from CrtProcessBuilder **1.6.3.26** via `type:"subProcess"` with a `subProcess` block:
    `{processName | processUId, resync}`. Not guessable from the schema:
      * naming the callee — `processName` (schema NAME or display CAPTION) or `processUId` — is what
        COPIES that process's parameters onto the element; that is the whole block. The element's own
        parameters are never declared here — they are DERIVED, and re-derived whenever the platform builds
        a schema instance, which is not every read, so describe can lag the callee (`process-parameters`).
        Exactly one of the two is required on CREATE; both together is fine while they agree and REFUSED
        when they do not. An ambiguous caption is REFUSED with the names listed, never resolved to the first.
      * values are mapped IN/OUT through the element's OWN parameters (which mirror the callee's) by the
        ordinary `mappings[]` / `addMapping` route, with one rule: only an `In` or `Variable` parameter
        keeps a value — the platform clears the rest on every synchronization, so a mapping onto any other
        direction is REFUSED rather than written and silently lost. Direction is only half of what decides
        survival — `process-parameters` has the other half. The clearing is feature-gated and ON by
        default; where it is off the value would survive and the refusal still fires, because the toggle is
        internal and the server cannot read it.
      * `resync: true` re-synchronizes against the ALREADY-called process without changing the selection,
        and is how you ASK for that refresh — not the only shape that performs one, see
        `process-parameters`. A `setElement` carrying NO `subProcess` block does not write at all: it
        reports whether a re-synchronization is OWED and leaves the element alone.
        Deliberate: the write that refreshes is the same one that removes the element's parameters when
        the platform cannot deliver the callee, so an edit that did not ask for it must not do that and
        then save. `resync: true` REFUSES rather than saving when the copy does not land, so treat an
        OWED notice as "send it once you have looked".
        `resync: false` is accepted and inert everywhere, including on CREATE; `resync: true` is REFUSED
        on CREATE, and REFUSED combined with a `processName`/`processUId` naming a DIFFERENT process (a
        resync and a retarget are different requests); naming the one already called is accepted.
      * REFUSALS, each stated as what to do instead: the named process is the one the element lives in, or
        another VERSION of it (self-reference — the runtime resolves the family's active version, so that
        is a self-call) — point it elsewhere; retargeting while a parameter or flow condition still reads
        from this element (live dependents) — remove or re-point them first; the called process has no
        Simple start event (rule R16, enforced at BUILD time in this element's own applier, not by
        `validate-process-graph` — see `process-activity-connections`) — add one to it, or call another;
        an ambiguous caption or disagreeing `processName`/`processUId` — see above; the element is already
        MULTI-INSTANCE — see NOT SUPPORTED.
      * DESCRIBE reports the callee under `subProcess`: `process` (name, falling back to the raw UId if
        deleted), `processUId`, `processCaption`, `multiInstance`, `inSync`. `inSync` is one-directional
        and instance-dependent, so it is NOT a drift report — see `process-parameters`.
    NOT SUPPORTED: MULTI-INSTANCE (the callee once per item of a collection — the element then carries
    collections and counters instead of the callee's parameters, so no name here addresses anything on it;
    edit it in the designer, and see `process-parameters`), and the EVENT and EXPANDED (embedded)
    sub-processes, which share this platform class but call no other process. Their children live in their
    OWN collection and the delete guards see them (they walk it recursively), while
    `describe-business-process` and `setElement` do not — so a refusal can name a flow no read call shows
    you.

clio MCP process-sub-process guide — the Sub-process element (callActivity / subProcess)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Sub-process element (`callActivity`, built via
`type:"subProcess"`): the `subProcess` block, naming the callee, how values cross through the element's
own mirrored parameters, `resync`, MULTI-INSTANCE (running the callee once per item of a collection),
the refusals, describe's read-back, and what is NOT supported (the event and expanded sub-processes). `process-element-catalog` says the element is
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
        an ambiguous caption or disagreeing `processName`/`processUId` — see above. Being MULTI-INSTANCE is
        NOT one of them any more: such an element is de-converted, the work is done against a
        single-instance element with every guard above, and it is re-converted — see MULTI-INSTANCE below.
      * DESCRIBE reports the callee under `subProcess`: `process` (name, falling back to the raw UId if
        deleted), `processUId`, `processCaption`, `multiInstance`, `inSync`, and — on a multi-instance
        element only — `multiInstanceOptions`. `inSync` is one-directional and instance-dependent, so it
        is NOT a drift report — see `process-parameters`. On a MULTI-INSTANCE element it carries no
        information at all, in EITHER direction: it compares the callee against the element's ROOT
        parameters, which there are the five service ones, so it is `false` whenever the callee declares
        anything and VACUOUSLY TRUE when the callee declares nothing (the test is an "all of the callee's
        parameters are present" and an `all` over an empty set is true). Do not read a `true` there as
        "in sync". Read `multiInstanceOptions.calleeInSync` instead — the same one-directional test asked
        one level down, where the contract lives. `null` there means the callee could not be read:
        UNKNOWN, never "out of sync".
    MULTI-INSTANCE — run the callee ONCE PER ITEM of a collection. BUILDABLE from CrtProcessBuilder
    **1.6.6.13** through `subProcess.multiInstanceOptions` `{enabled?, executionMode?, ignoreErrors?}`.
      * `enabled: true` converts, `false` de-converts. De-conversion is a DESTRUCTIVE write in SHAPE:
        the element stops carrying the five and carries the callee's parameters again, so every dotted
        name stops addressing anything. A value you MAPPED survives it — the mapping row pairs source and
        target by UId, and the de-conversion clones the item properties back out with their UIds and
        their values, which is also what the server's own notice says. What does not come back is the
        OUTPUT collection's non-`Out` items: each is a derived copy of an input item whose value the
        platform had already cleared, and the original returns from the input side. Anything that read one
        of the five — `OutputRecordCollection` or a counter — is left dangling by the de-conversion, and
        the server NAMES those readers in a notice: re-point or remove them.
      * OMIT `enabled` on an element that is ALREADY multi-instance and the other two fields still apply —
        that is how you change how it iterates without re-converting. FIVE shapes are REFUSED rather than
        accepted-and-ignored, and this is the whole list: a mode field on an element that is NOT
        multi-instance and is not being converted (say `enabled: true` if that is what you meant — writing
        an options object to hold the field would change how the element RUNS); a block naming NO field at
        all; an `executionMode` that is not one of the two names, INCLUDING a blank string (an omitted
        mode is `null`, a blank one is a mistake); a mode field combined with `enabled: false`, which asks
        for two opposite things; and `enabled: false` on a CREATE, where there is nothing to de-convert.
        All five are decided before anything is written.
      * `executionMode` is the STRING `Sequential` or `Parallel`, case-insensitive. The raw metadata's
        `0`/`1` is REFUSED — a number read out of stored metadata would otherwise select the other mode in
        silence. Omitted on an update it is left as it is, never reset to `Sequential`. Parallel does not
        by itself mean concurrent threads: it changes the generated flow topology.
      * `ignoreErrors` changes only what happens AFTER a failed iteration; the failed-iteration counter is
        incremented either way.
      * `useBackgroundMode` IS NOT TOUCHED by a conversion — not here, and NOT in the process designer
        either. An earlier revision of this article said the designer switches it ON; it does not.
        `convertToMultiInstance` creates the five parameters and the options object and nothing else, and
        the current designer component never mentions the flag. So there is no parity gap to close.
        What the flag does on such an element is also not what it does elsewhere: the platform excludes a
        multi-instance sub-process from the element background token explicitly, and the flow generator
        reads the flag to build a different ITERATION flow instead. That one is SLOWER — measured 1282 ms
        against 105 ms for three iterations — because the continuations are consumed under a per-process
        lock and serialise anyway. It buys no concurrency.
      * THE SHAPE CHANGES, and every mapping afterwards depends on it. A converted element carries FIVE
        parameters instead of the callee's: `InputRecordCollection`, `OutputRecordCollection` and the
        counters `CompletedIterationsCount`, `TerminatedIterationsCount`, `TotalIterationsCount`. The
        callee's contract moves ONE LEVEL DOWN, into the collections' `itemProperties` — the callee's
        `In`/`Variable` parameters into the input collection, its `Out` parameters into the output one.
      * BIND the collection to iterate with the ordinary `addMapping` / `mappings[]` route onto
        `InputRecordCollection`. There is no new operation. From a Read data element in `collection` mode
        the source is `ResultCompositeObjectList` — the output whose data value type matches;
        `ResultEntityCollection` does NOT and is refused by the type check.
      * ADDRESS A PER-ITEM VALUE with a DOTTED name, on both sides:
        `elementParameter: "InputRecordCollection.<CalleeParam>"` and, when the source is a column of
        another element's collection output, `sourceElementParameter: "ResultCompositeObjectList.<Column>"`.
        A flat name is tried FIRST and the dotted walk runs only when the whole string matches nothing, so
        a parameter whose own name contains a dot still resolves as it always did.
      * A TARGET INSIDE THE OUTPUT COLLECTION IS REFUSED, at any depth. Map FROM it instead: the platform
        derives those values per completed iteration and clears them on every synchronization, so a write
        there is erased with no error at any layer — the element would look configured and deliver nothing.
      * COUNTERS read mid-run are not what they look like: the parallel barrier uses
        `CompletedIterationsCount` as an arrival counter and the End token overwrites it with total minus
        failed before persisting. Read them on a flow LEAVING the element.
      * A RETARGET and a pure `resync: true` both work on a multi-instance element: it is de-converted,
        the ordinary applier does the work with every guard it carries, and it is re-converted around the
        SAME five parameter objects, so their UIds survive. That is what the process designer does for the
        same edit.
      * AN `Internal`-DIRECTION PARAMETER on the callee is DROPPED by a conversion and REPORTED by name,
        not refused. The platform's fill routes `In`, `Out` and `Variable` and has no `Internal` branch, so
        such a parameter is routed nowhere. Refusing instead would make 12 of the 327 shipped
        single-instance sub-process elements — production Copilot flows among them — permanently
        unconvertible through this contract.
      * ONE ASYMMETRY, so it does not read as an oversight: a conversion WRITES all five parameters with
        their directions, but the guard that runs before a de-conversion, a retarget or a re-synchronization
        VALIDATES only the two collection UIds and that they are `CompositeObjectList`. Deliberate. The
        three counters are re-derivable and self-heal; the collections are not — they carry the callee's
        contract and every mapping written against it, so a missing or mistyped collection is the one state
        nothing can be reconstructed from, and the only one worth refusing on.
    NOT SUPPORTED: the EVENT and EXPANDED (embedded)
    sub-processes, which share this platform class but call no other process. Their children live in their
    OWN collection and the delete guards see them (they walk it recursively), while
    `describe-business-process` and `setElement` do not — so a refusal can name a flow no read call shows
    you.

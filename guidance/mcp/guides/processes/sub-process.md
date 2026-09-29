clio MCP process-sub-process guide — the Sub-process element (callActivity / subProcess)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Sub-process element (`callActivity`, built via
`type:"subProcess"`): the `subProcess` block, naming the callee, how values cross through the element's
own mirrored parameters, `resync`, MULTI-INSTANCE (running the callee once per item of a collection),
the refusals, describe's read-back, and what is NOT supported (the event and expanded sub-processes). `process-element-catalog` says the element is
buildable and from which CrtProcessBuilder version; `process-parameters` owns what happens to a CALLER
when the called process's own parameters change (their CAPTIONS on a re-sync are covered here, under
`resync`). Split out of process-element-catalog.
WHETHER a request calls for this element at all — the one-process default, per-item work, a repeated
fragment, long phases — and which execution mode a per-item loop needs is owned by
`process-sub-process-when`: read it before you plan one.
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
      * CAPTIONS. Every synchronization - a re-sync, and any OTHER save of a caller - copies the called
        process's parameter captions onto the element, current culture only, with NO notice, so a caption
        edited on the element does not survive one: the platform's rule, and the process designer's card does
        the same. Captions are display text; nothing binds by them. Before CrtProcessBuilder 1.6.6.20 a callee
        saved through this toolset may still hand its callers the captions from before that save, and saving
        it again through this toolset only moves that lag. So if a caller still shows a caption the called
        process no longer has after a re-sync, run `install-process-builder` - the install restarts the
        application, which drops the stale cache - then re-sync. Only when the package cannot be updated, ask
        the USER to open the called process in the process designer and save it (no change needed), then
        re-sync; that cure lasts only until the next save of the callee through an older toolset.
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
    **1.6.6.14** through `subProcess.multiInstanceOptions` `{enabled?, executionMode?, ignoreErrors?}`.
      * `enabled: true` converts, `false` de-converts. De-conversion is a DESTRUCTIVE write in SHAPE:
        the element stops carrying the five and carries the callee's parameters again, so every dotted
        name stops addressing anything. A value you MAPPED survives it — the mapping row pairs source and
        target by UId, and the de-conversion clones the item properties back out with their UIds and
        their values — UNLESS the called process has dropped that parameter since: the de-conversion then
        re-derives the element from it, removes the parameter with its value, and the notice names it.
        What does not come back is the OUTPUT collection's non-`Out` items: each is a derived copy of an
        input item whose value the platform had already cleared, and the original returns from the input
        side. Anything that still READS what the de-conversion removes — `OutputRecordCollection`, a
        counter, or a Variable parameter's output copy — makes the platform's pre-save validation REFUSE
        the whole edit and name the reader (measured for an element iterating the output collection and
        a process parameter reading a counter): re-point it first (a copy's reader at the restored Variable
        of the same name) or remove it, then de-convert. Nothing is saved by the refused edit.
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
        silence. Omitted on a CONVERSION it is `Sequential`, which starts the next item only after the
        previous item's called process has finished; omitted on an update it is left as it is, never reset
        to `Sequential`. Parallel does not by itself mean concurrent threads: it changes the generated flow
        topology. Which mode a request needs is decided in `process-sub-process-when` (D1).
      * `ignoreErrors` changes only what happens AFTER a failed iteration; the failed-iteration counter is
        incremented either way.
      * ONE DIFFERENCE FROM THE DESIGNER, so a comparison does not read as a defect: converting in the
        process designer also switches `useBackgroundMode` ON, and de-converting switches it back OFF. The
        element's properties page does it right after the conversion — `convertToMultiInstance` itself
        touches only the five parameters and the options object, which is why reading that method alone
        says the opposite. This contract leaves the flag alone, because it changes how the element RUNS and
        nobody asked for that. For parity, send `setElement` with `useBackgroundMode: true`: the flag IS
        written, but the reply still carries the multi-instance notice that the element "was NOT
        re-synchronized as part of this edit and nothing was changed on it". That notice speaks for the
        parameter refresh the edit skipped, not for the flag — do not retry on it; read the flag back with
        `describe-business-process` (`useBackgroundMode`). On such an element the flag does not do what it
        does elsewhere: the platform excludes a multi-instance sub-process from the element background
        token, and the flow generator reads the flag to build a different ITERATION flow. It buys no
        concurrency — the continuations are consumed under a per-process lock and serialise anyway: on a
        stand, three `Parallel` iterations with background mode did not overlap, each queued behind the
        previous one.
      * THE SHAPE CHANGES, and every mapping afterwards depends on it. A converted element carries FIVE
        parameters instead of the callee's: `InputRecordCollection`, `OutputRecordCollection` and the
        counters `CompletedIterationsCount`, `TerminatedIterationsCount`, `TotalIterationsCount`. The
        callee's contract moves ONE LEVEL DOWN, into the collections' `itemProperties` — the callee's
        `In`/`Variable` parameters into the input collection, its `Out` parameters into the output one.
      * BIND the collection to iterate with the ordinary `addMapping` / `mappings[]` route onto
        `InputRecordCollection`. There is no new operation. From a Read data element in `collection` mode
        the source is `ResultCompositeObjectList` — the output whose data value type matches;
        `ResultEntityCollection` does NOT and is refused by the type check.
        MUST: send THIS mapping as well as the per-item ones below — the collection decides HOW MANY
        iterations run, the dotted mappings only what each one receives. Without it nothing refuses or
        warns: the build succeeds, describe shows `InputRecordCollection` with `source: "None"`, and at run
        time the element runs ONE iteration with every per-item value empty (measured, CrtProcessBuilder
        1.6.6.22: one task with no contact instead of one per contact).
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
        their directions, but the guard that runs before a mode change, a retarget or a re-synchronization
        VALIDATES only the two collection UIds and that they are `CompositeObjectList`. Deliberate. The
        three counters are re-derivable and self-heal; the collections are not — they carry the callee's
        contract and every mapping written against it, so a missing or mistyped collection is the one state
        nothing can be reconstructed from, and the only one worth refusing on. A DE-CONVERSION runs NO such
        guard, and that is the point: it is the repair for exactly that state. It only reads what the
        collections hold and then drops the options, so `multiInstanceOptions: {enabled: false}` works on
        an element whose collection UId dangles or whose collection is not a collection — which is what
        each of that guard's refusals tells the caller to send.
    NOT SUPPORTED: the EVENT and EXPANDED (embedded)
    sub-processes, which share this platform class but call no other process. Their children live in their
    OWN collection and the delete guards see them (they walk it recursively), while
    `describe-business-process` and `setElement` do not — so a refusal can name a flow no read call shows
    you.

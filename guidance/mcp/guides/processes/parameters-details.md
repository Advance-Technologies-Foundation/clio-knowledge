clio MCP process-parameters-details guide — a Sub-process element's parameters at run time and after a re-sync

A details article of the process guide set, reached through `process-parameters`, which points here; `process-modeling` is the set's entry point.
This article is the authoritative owner of what a SUB-PROCESS element's parameters do at RUN TIME: the rule to
re-synchronize every caller, which `setElement` shapes re-synchronize, what a re-synchronization costs and
reports, how values cross by name, which version of a versioned callee runs, what `inSync` and describe can and
cannot show, how a rename is followed, and why the designer card cannot check any of it. Split out of
`process-parameters`, which keeps process parameters, the mappings that bind them, type compatibility and the
date/time/lookup default-value rule; read that first.

== Sub-process element parameters at run time ==
- A SUB-PROCESS element's parameters are the CALLED process's contract, copied onto the element and
  re-derived whenever the platform builds a schema instance. `process-element-catalog` owns the BUILD
  contract - how the callee is named, which directions keep a value, what `resync` refuses. This guide
  owns what the call does at RUN TIME and how little of it you can observe:
  * THE RULE: after any change to a called process's parameters, re-synchronize every caller with
    `subProcess: {resync: true}`. No per-element exception: a MULTI-INSTANCE element takes the
    same request - de-converted, re-synchronized, re-converted around the same five. Skip one and its
    per-item values, bound by NAME, quietly stop arriving. Nothing here lists a process's callers: `execute-esq` over `VwProcessLib`
    gives you the candidates, then `describe` each and look for a `subProcess.process` naming it.
  * A `setElement` carrying NO `subProcess` block does not write THE ELEMENT - the rest of the edit is
    applied as asked, and the element is only reported on, as a re-synchronization OWED. Three shapes DO
    re-synchronize, and two are easy to send by accident: `{resync: true}`, any block naming the process
    ALREADY called (`resync: false` does not decline it - that only declines when no process is named),
    and an EMPTY `subProcess: {}`. A block naming a DIFFERENT process is a retarget: it replaces the whole
    contract, and `process-element-catalog` owns its refusals.
  * WHAT A RE-SYNCHRONIZATION COSTS, so you can weigh sending one. Where the callee legitimately DROPPED a
    parameter and the write LANDS, the platform removes that parameter AND DELETES ITS MAPPING ROW, flags
    every element that referenced it invalid, and any replacement it creates carries a NEW UId - so
    re-selecting the process afterwards does not restore the binding. Where the callee cannot be delivered
    at all, the same removal happens in memory and is then DISCARDED with the refusal: nothing is saved,
    and the refusal is the protection rather than the damage. A re-sync that was not needed is otherwise
    safe, with one cost: it clears values that came from the callee's own defaults, keeps the ones the
    caller wrote, and says so in its notices.
  * READ ALL THE NOTICES, not one. A re-sync answers with what the platform's diff moved - parameters
    ADDED, REMOVED, RENAMED, RETYPED, values CLEARED - separately with references left DANGLING (the sites
    still bound to a parameter the element no longer carries), and separately again if it SKIPPED the
    element, for a self-reference or a callee it could not read. Two answers look like success and are
    not. An EMPTY diff is what you get when the load already converged the element: "nothing left to see",
    not "nothing changed". And a notice saying the dangling scan could not complete does not mean nothing
    broke. THE ONE POSITIVE SIGNAL: a `resync: true` that RETURNS AT ALL guarantees every parameter the
    callee currently declares is now present on the element - the write refuses rather than saving a copy
    that did not land.
  * WHAT THE DANGLING NOTICE CATCHES, from CrtProcessBuilder 1.6.3.26, is the sites the platform's own
    pre-save validation does not catch first - a stored blob such as a Modify-data element's column
    bindings. A reference this package wrote (a formula, or a mapping - it stores both as `Script`) is
    usually refused by that validation before the re-sync can report it; usually, because that refusal is
    gated on an application-CONFIGURATION key, `Feature-UseVerificationOfProcessParameterDirection`,
    default ON and not switchable from the product UI. Where it is off the notice reaches those sites too,
    and it can name several in one message.
  * WHY IT MATTERS: values cross by parameter NAME, matched CASE-SENSITIVELY, and direction is ignored on
    the receiving side - the copy-back writes into whatever element parameter carries the callee's name,
    `In` and `Variable` alike. A name present on only one side is SKIPPED, on the interpreted path with no
    exception and no log line. Requiredness is never validated on either side. A COLLECTION-typed
    parameter crosses like any other; what does not cross is a member of a collection ITEM's structure. So
    the cases that bite are narrow: a parameter the callee DROPPED, one it renamed while the element
    stayed BEHIND, and - for that parameter alone - one it ADDED that nobody mapped. A RETYPE still
    crosses. To USE an output, map FROM it: name the element and the parameter as the SOURCE of a mapping
    onto a process parameter or another element's input.
  * AND WHICH PROCESS ACTUALLY RUNS is not the one you synchronized against, if the callee is VERSIONED.
    Design time uses the stored UId; run time resolves that UId's family and executes its ACTIVE VERSION.
    A caller re-synchronized against the version you edited can therefore still call a different one, and
    nothing in a read tells you so - check which version of the callee is active before concluding that a
    correct re-sync fixed the delivery.
  * `inSync` IS NOT A DRIFT REPORT. It compares NAMES only, CASE-INSENSITIVELY, in ONE direction: whether
    every parameter the callee declares is PRESENT on the element. So it catches an ADD, and misses a
    REMOVE, a caption change, and a CASE-ONLY code rename, which the runtime does bind on. On a
    MULTI-INSTANCE element it says nothing EITHER way: it reads the ROOT parameters, which there are the
    five service ones, so it is `false` whenever the callee declares anything and VACUOUSLY TRUE when it
    declares nothing - an `all` over an empty set. Check `multiInstance` first, then
    `multiInstanceOptions.calleeInSync`: the same test one level down, in the collections' item
    properties, where that element's contract lives. `false` there DOES mean a
    re-synchronization is owed; ask with `subProcess.resync: true`. A `true` means nothing on its own -
    besides the vacuous case, the read repairs the element on its way to you: materialising a schema
    instance from metadata, and every fetch of the DESIGN instance, re-synchronizes every sub-process
    element on it. Never read `true` as "intact".
  * A parameter on the element that the callee does NOT declare is not necessarily damage. One the CALLER
    created, with no mapping row, is a legitimate and permanent state: no re-synchronization removes it.
  * IF YOU NEED `inSync` AS EVIDENCE, use the recipe - save the caller, `describe` it once, change the
    callee, `describe` again - and know its limits. Step 1 is a WRITE that converges and persists the
    element, so it erases any drift that had already happened. The caller must not be saved again in
    between, because a second save re-converges and re-persists it. If those limits do not fit, skip the
    recipe and just re-synchronize - its notices are computed against the callee as the manager currently
    holds it, which a save of the CALLEE refreshes.
  * A RENAME is followed automatically. The element parameter is paired to the callee's through the
    mapping row's source UId rather than by name, so the element's copy keeps its own UId while its name,
    caption, data type, direction and five other properties are overwritten from the callee. Whether its
    VALUE survives needs BOTH a provenance stamp saying the caller wrote it and a direction of `In` or
    `Variable`; the notice says whether it survived, not which condition failed. What is NOT followed is
    anything YOU wrote naming the old parameter; that has to be updated by hand.
  * THE DESIGNER CARD cannot check any of this. Its parameter row has no code column: a type icon, a
    direction icon, the caption - or the CODE, when a parameter has no caption - and the mapping value.
    Opening the card re-derives the element's parameters and re-attaches the stored values BY NAME, so a
    code rename can leave the row EMPTY while the caller's stored mapping is intact. An empty row means
    "the carry-over did not match", not that your data is gone. Do not re-map on the strength of one, and
    DO NOT SAVE the caller from that card - re-deriving drops the unmatched parameter's mapping row in
    memory, and saving persists that. Close it and read `describe-business-process`, which reports every
    parameter of a sub-process element with its `source` and `value`. (Observed once, 2026-09-17, and only
    when the caption was renamed alongside the code. Nothing in the client reads the caption on this path,
    so the condition is unexplained - assume EITHER rename can do it until it is re-measured.)
  `describe-business-process` reports the element's parameters as the instance it was handed carries them.
  `direction` and `isRequired` are re-copied from the callee on every synchronization, so they track it;
  `isResult` is copied only when the element parameter is CREATED and is never refreshed, so a callee that
  later flips it leaves the element reporting the old value, and no re-sync reports or fixes that.

clio MCP process-element-catalog-details guide — the element catalog: data-id -> label -> purpose

A details article of the process guide set, reached through `process-element-catalog`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the element catalog: every process element as the `data-id`
`validate-process-graph` speaks and `describe-business-process` reports, its designer label, what it does,
whether `create-business-process` builds it and with which block, and its READ-ONLY marker. Split out of
`process-element-catalog`, which keeps what builds today, what does not, how a `data-id` maps to a build
`type`, and the custom user-task compile rule; read that first.

== Element catalog (data-id -> label -> purpose) ==
System actions (palette group "System actions"):
- `readDataUserTask`  Read data    — read first record / aggregate / count / collection of an object.
    FIRST-RECORD, COLLECTION, COUNT and AGGREGATION modes are buildable via the element's `readData` block (source
    object, mode, columns/sort — `first` / `collection`, refused for `count`/`aggregation` — a
    `numberOfRecords` top-N (`collection` ONLY) and an aggregation function + column —
    `aggregation` ONLY) plus a `filter` — see `process-read-data` for the block
    and `process-data-source-filters` for the filter; describe reads them back as `mode: "first" |
    "collection" | "count" | "aggregation"`. `collection` reads every match into `ResultEntityCollection`
    and the shaped `ResultCompositeObjectList`, which a `Collection` process parameter mirrors; it requires
    an explicit `columns` selection.
- `addDataUserTask`   Add data     – create record(s) in background; BUILDABLE via the `addData` block in
                                     both modes. Returns ONLY the new record's Id, on `RecordId`.
- `changeDataUserTask` Modify data — bulk-update matched records (same values to all). BUILDABLE via the
    element's `changeData` block (target object + column values) plus a `filter` — see
    `process-data-elements` for the block and `process-data-source-filters` for the filter.
- `changeAdminRightsUserTask` Change access rights - grant/revoke record permissions on matched
    records. BUILDABLE via `accessRights` (alias `changeAccessRights`) plus a `filter`; no outputs.
    `process-access-rights` owns the shape and the hazards. Both no-op states are worth naming here
    because they build green: `add` and `remove` both empty changes nothing, and a filter that is
    PRESENT but carries no conditions is the inert state (the package refuses that one at build);
    a record filter that is ABSENT is the opposite hazard and acts on every record of the object -
    nothing refuses or warns it. The element has no output parameters,
    so a clean build does NOT mean the element will do anything - check the filter and the entries.
- `deleteDataUserTask` Delete data — delete matched records. BUILDABLE via the element's `deleteData`
    block (target object — the only field it has) plus a `filter` — see `process-delete-data` for the
    block and the confirmation duty, `process-data-source-filters` for the filter. Unlike Modify data there
    is no mode flag: the runtime always applies the filter and throws its empty-filter error without one,
    so a filterless element deletes nothing and fails rather than deleting everything. DESTRUCTIVE and
    irreversible, and it repeats on EVERY run — confirm the object and the selected records with the user
    before you plan one in.
- `formulaTask`       Formula      — compute a value (math/string/date/bool) into ONE parameter.
    BUILDABLE from CrtProcessBuilder **1.6.3.16** with a `formula` block:
    `{body, and exactly ONE of resultProcessParameter | elementName + elementParameter}`.
    Four things about it are not guessable from the schema:
      * `body` is the SAME dialect as a flow condition (`process-formulas` owns the vocabulary). A process
        parameter may be referenced by NAME — `[#Amount#]` — on EVERY route that writes a body: create,
        `addElement` and `setElement` alike, because the expansion lives in the applier they all go
        through. A body already in the meta-path form is never rewritten: under the contract
        `process-branch-conditions` names (ON THE MODIFY PATH) it is checked instead: a read-back passes
        unless it reads a column its Read data does not load - then the refusal names it, and that
        reference reads EMPTY at run time today.
      * BOTH the body and a target are required when the element is created, and naming two targets is
        refused rather than resolved by precedence. On modify the block is a PARTIAL update: a body-only
        edit keeps the target, a target-only edit keeps the expression.
      * the target is NOT a mapping and needs none of the mapping sources. The platform stores it as a
        map path on the element itself, which is why `describe-business-process` reports it under
        `formula.target` — resolved back to names — rather than among the process mappings. A three-part
        target (a COLUMN of an element parameter's record) reports that column as `entityColumnUId`,
        because the process cannot name it. `formula.target.unresolved` means the stored path could not
        be decoded into names — NOT that the element writes nowhere: the platform's reader accepts
        shapes a read-back may not, so treat it as "not named here" and look at the raw path.
      * an element the DESIGNER built reads back the same way, so a described formula feeds straight
        into a build.
    Still true, and still the cheaper answer for a one-off value: a mapping with an `expression` source
    computes a value without an element at all. Reach for this element when the computation deserves to
    be visible on the diagram, or when the result must be written between two steps.
- `scriptTask`        Script task  — custom C# (ends with `return true;`; needs a compile), from 1.6.6.30.
  - Compile note: a `scriptTask`, and a `userTask` carrying an after-activity-save script, are the two
    IN-PROCESS elements whose authored C# makes the process itself need a compile before it runs.
- `webService`        Call web service — call a registered service; outputs Success + Http status code.
    READ-ONLY here.
- `callActivity`      Sub-process  — call ANOTHER process (the BPMN call activity) and run it once, passing
    values through THAT process's own parameters. BUILDABLE from CrtProcessBuilder **1.6.3.26** via
    `type:"subProcess"` with a `subProcess` block
    (`{processName | processUId, resync, multiInstanceOptions}`); the block, the parameter
    mirroring/mapping rule, `resync`, MULTI-INSTANCE (running the callee once per item of a collection,
    from **1.6.6.14**), the refusals and what is NOT supported (the event/expanded sub-processes) are owned
    by `process-sub-process` — go there before writing any of it; WHETHER a request needs one at all is
    owned by `process-sub-process-when`. Its EVENT and EXPANDED variants keep
    their children in their OWN collection, which `describe-business-process` does not walk, but
    the delete guards see them, walking it recursively so a reference from inside one still blocks a delete.
- `userTask`/`*UserTask` — user/system tasks (Perform task, Open edit page, Send email, Approval, etc.).
User actions: `activityUserTask` Perform task, `userQuestionUserTask` User dialog,
  `openEditPageUserTask` Open edit page (BUILDABLE via `type:"openEditPage"` — see "What you can build today" in `process-element-catalog`), `autoGeneratedPageUserTask` Auto-generated page,
  `preconfiguredPageUserTask` Pre-configured page, `emailTemplateUserTask` Send email, `approvalUserTask` Approval.
  Six of these ENUMERATE RESULTS, which changes how a branch leaving them is authored: Perform task, User
  dialog, Open edit page, Auto-generated page, Pre-configured page and Approval. A conditional flow off one
  is edited in the designer as a result SELECTION rather than a formula — `process-activity-result-branches` owns
  that rule, and no guide here owns User dialog or Auto-generated page, so treat a branch off either the
  same way.
Events: `startEvent` Simple start, `startEventSignal` Signal start (record add/modify/delete or custom
  signal), `startEventTimer` Start timer (schedule/CRON), `startEventMessage` Start message, intermediate
  catch/throw (`intermediateCatchEvent*`/`intermediateThrowEvent*`), `endEvent` End/Terminate — the
  BPMN catalog has both, but a `create-business-process` `endEvent` builds Terminate today (see N6 in `process-naming`).
  Timer start, message start and the intermediate catch/throw events are READ-ONLY here.
Gateways: `exclusiveGateway` (XOR, BUILDABLE), `parallelGateway` (AND, BUILDABLE),
  `inclusiveGateway` (OR, read-only), `eventBasedGateway` (read-only) -- see
  `process-branch-conditions`.
Flows: sequence (default `connect`), conditional (setup -> conditionalConnection), default (setup -> defaultConnection).
  All three are BUILDABLE: declare the kind with the flow (`flows[].kind`, plus `flows[].condition`
  on a conditional one) rather than drawing it and setting it afterwards. Each also takes
  `flows[].label`, the connector text — the member first shipped in `CrtProcessBuilder` 1.6.0.8 and is
  reported by `describe-business-process`. Read that as provenance, not as a check to run: see
  `process-naming` N10 for why a version number cannot tell you whether an archive carries it.

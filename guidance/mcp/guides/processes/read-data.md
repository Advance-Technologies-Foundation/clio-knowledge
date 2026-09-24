clio MCP process-read-data guide — the Read data element

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.

== What this article owns ==
This article owns the Read data element: its `readData` block, all four read modes and what each one
outputs, the column selection and sort, the collection shape a `Collection` process parameter mirrors,
the top-N, the refusals each mode carries, and what a mode conversion clears. Split out of
`process-data-elements` when that article outgrew a single get-guidance response; the sibling data
elements (record triggers, Modify data) stay there, and Add data and Delete data have their own
articles, `process-add-data` and `process-delete-data`.
The record `filter` this element carries is owned by `process-data-source-filters` — this article says
WHETHER it takes one and what it means here, that one says what a filter may contain. The build
lifecycle and descriptor shape live in `process-modeling`; what is buildable today lives in
`process-element-catalog`.

== Read data element (readData) — first / collection / count / aggregation modes ==
- A `readData` element reads from one object in one of FOUR modes, all buildable (count and aggregation from
  CrtProcessBuilder 1.6.2.6 — not the earlier 1.6.0.9, a pre-merge cut that never shipped — and collection
  from the version this guidance ships with). Configure it with the element's `readData` block:
    { "name": "ReadNewestContact", "type": "readData", "caption": "Read newest contact",
      "readData": {
        "source": "Contact",                                  // REQUIRED at create: the entity to read
        "mode": "first",                                      // optional; first (default) | collection | count | aggregation
        "columns": ["Name", "Email"],                         // optional for first (omit or [] = ALL columns); REQUIRED for collection; refused for count / aggregation
        "sort": { "column": "CreatedOn", "direction": "desc" } // optional; direction defaults to "asc"; first / collection only
      },
      "filter": { "object": "Contact",
        "conditions": [ { "column": "Name", "comparison": "contains", "value": "Creatio" } ] } }
- `mode` and what each one produces (the output is what `describe-business-process` marks `isResult: true`,
  and what a downstream mapping's `sourceElementParameter` names):
  * `first` — the FIRST record of the sorted selection → `ResultEntity` (the whole record).
  * `collection` — EVERY matching record, into TWO outputs: `ResultEntityCollection` (the raw list) and
    `ResultCompositeObjectList` (one column per selected column). Mirror the second into a `Collection` process
    parameter (`parameters[]` `typeFromElement`, see `process-parameters`): that per-column shape is the only
    thing a consumer can bind to. `columns` is REQUIRED — an omitted selection is not "no columns": the runtime
    reads EVERY column of the object. A column the collection cannot carry (Binary, say) is REFUSED: the
    runtime drops it from the query without a word, so the shape would advertise a value that never arrives.
    Both rules judge the EFFECTIVE selection, so an update naming no columns is judged on the STORED one — a
    designer-made element can be refused over a column you never sent, until you re-send `columns` without it.
    `numberOfRecords` is the top-N: positive, refused in every other mode, which is why the block above shows
    none. Omitting it KEEPS the stored one (re-selecting columns must not turn a top-25 read into a
    read-everything one) and reads every match when ENTERING the mode; it cannot be REMOVED while the element
    stays here (0 is refused), and a top-N without a sort takes an arbitrary slice.
    Entering SHAPES the output at once, which is what lets a collection parameter mirrored in the SAME
    `create-business-process` call find a shape rather than an empty list; re-selecting re-shapes in place,
    keeping surviving item ids. A multi-instance Sub-process element CONSUMES a collection - map
    `ResultCompositeObjectList` onto its `InputRecordCollection` and the called process runs once per item;
    see `process-sub-process`. Reading one column out of the list into a scalar still needs ENG-91844.
  * `count` — how many records match → `ResultCount` (Integer). Takes NO column and NO `columns`/`sort`.
    MUST map `ResultCount`, NOT `ResultRowsCount`. Both are Integer outputs of the element, but `describe`
    does NOT list `ResultRowsCount` on a builder- or designer-made count element (see the `describe` coverage
    note below) — it still exists and is mappable. `ResultCount` is the aggregate the element computed;
    `ResultRowsCount` is how many ROWS the query returned — confirmed on a stand at `1`, since a function-mode
    query selects one aggregate column with no grouping (the sibling `ReadEntityCollectionItemsUserTask`
    assigns the same literal on its own function path). `ResultCount` is what the runtime fills with the
    answer; mapping the other gives a value that never tracks the data.
  * `aggregation` — `"aggregation": { "function": "sum" | "avg" | "min" | "max", "column": "Amount" }` is
    REQUIRED. The OUTPUT is chosen by the column's TYPE, exactly as the runtime writes it: an Integer column →
    `ResultIntegerFunction`; a Float / Money column → `ResultFloatFunction`; a Date / Date-time / Time column
    (min/max only) → `ResultDateTimeFunction`. Any other column type — and sum/avg over a date — is REFUSED,
    because the runtime writes NO result for it, silently. `columns` and `sort` are refused in count /
    aggregation (the runtime ignores both there, so accepting them would be a silent no-op). `avg` over an
    Integer column TRUNCATES the fraction — three values 5/10/17 (average 10.67) measured storing `10`, not
    `11`: T-SQL's own `AVG(int)` integer-division, not the package's doing. Use Float / Money when the
    fraction matters.
  Omit `mode` at create for `first`; omit it on a `setElement` update to KEEP the element's current mode.
  Changing the mode through `setElement` is a real conversion, and it is REFUSED while any other parameter still
  maps FROM the element — the refusal names each dependent. Each mode produces a different output parameter, so a
  mapping that names the current one would survive pointing at a parameter the runtime no longer fills; the
  designer reverts the same edit for the same reason. Re-map or remove the dependents first (or remove and re-add
  the element). A conversion that proceeds clears the previous mode's parameters, moves the
  result flag to the new mode's output, and clears the column selection / sort on entering count /
  aggregation. The record `filter` is KEPT — it is the one block every mode carries (the designer shows "How to
  filter records?" in all of them), so a mode change does not need a `setFilter` after it; only a `source`
  retarget clears the filter. LEAVING collection additionally clears its top-N pair and empties
  `ResultCompositeObjectList`'s item properties, because the platform rebuilds those only WHILE the element is
  in collection mode — the designer clears them the same way, and a stale top-N is not inert: under
  `FeatureReadDataUserTaskEntityReadOldMode` it still decides how many rows a `first` read takes. A
  `setElement.readData` update naming another mode performs that conversion — it is NOT remove+recreate.
  Re-aggregating in place counts as a conversion too, even though the mode does not change: `aggregation`'s
  output follows the COLUMN TYPE, so switching `{sum, Amount}` to `{min, CreatedOn}` moves the result flag from
  `ResultFloatFunction` to `ResultDateTimeFunction`. A mapping that named the old output STOPS BEING FILLED
  (the parameter still exists and is still mappable) — re-read the element with `describe-business-process`
  after such a change and re-point anything that consumed it.
- `describe-business-process` reports only FLAGGED / value-bearing parameters. An omitted one may still
  exist and be mappable (`ResultRowsCount` above) — omission means "not reported", never "does not exist".
- `columns` are TOP-LEVEL entity COLUMN names (not captions); an unknown name is rejected at build. Omit the
  list (or pass `[]`) to read all columns. A dot-separated path into a linked object (`Owner.Name`) is NOT
  supported and is rejected — such paths exist only in hand-authored metadata (the Read data card's own
  picker lists top-level columns only); read the whole record (omit `columns`) if you need them. `sort`
  makes "the first record" deterministic — without it the platform reads an arbitrary first record; single
  column only (multi-column ordering is designer-only), and the sort column must be top-level too.
- WHICH records qualify is the element's separate `filter` block (full shape in the "Data source filters"
  section of `process-data-source-filters`). Unlike a signalStart filter, a readData filter MAY
  reference `processParameter` /
  `elementParameter` — the element runs inside a live process instance.
- A read record's individual COLUMN values ARE reachable in a flow condition and in a formula — the platform parses a
  third meta-path segment (`FillMatchedData` routes an `EntityColumn` segment into `SubParameterMetaPath`
  and `TryGetParameterMapPath` carries it). describe reports no column UIds, but that is a
  DISCOVERABILITY gap, not a refusal: `get-entity-schema-properties` supplies the UId describe does not.
  `process-data-elements` owns both recipes (steps, verified evidence).
  One exception, whose form `process-send-email` owns: a Send email BODY macro reaches a column by NAME,
  `[[element:Read.ResultEntity.Column]]`. Outside a flow condition, the element's only output parameter
  is `ResultEntity` (the whole record, `isResult:true` in describe); the record's columns are still NOT
  element parameters, so a STRUCTURED reference to one (e.g. `sourceElementParameter: "Email"` on the
  read element) FAILS the build with "element has no parameter". To carry a column onward, put it into a
  process parameter with a formula (`process-data-elements`). To key work off a specific record, use a
  `signalStart` trigger output (`RecordId`) or a process parameter.
- Change an EXISTING element in place with the `setElement` op's `readData` field (preserves the element
  and its flows):
    { "op": "setElement", "elementName": "ReadNewestContact",
      "elementUpdate": { "readData": { "sort": { "column": "ModifiedOn", "direction": "desc" } } } }
  Partial update: omit `source` to keep the current source object, omit `columns`/`sort` to keep the
  current selection/order, pass `columns: []` to reset to ALL columns. RETARGETING `source` to a different
  object is REFUSED while any other parameter still maps from the element (the refusal names each
  dependent — re-map or remove them first, the same block the designer applies); a retarget that proceeds
  clears the columns, sort AND record filter bound to the old entity — re-supply them (and issue a
  `setFilter`) in the same operations array. MUST, before any `setFilter` on a live process: `setFilter`
  REPLACES the element's whole filter and there is no add-one-condition op, so read the current filter
  back with `describe-business-process` and send it complete. `process-data-source-filters` owns the op
  and the read-back shape. `describe-business-process` reads the whole block back
  (`source`, `mode`, `columns` as names, `sort`, and `aggregation` as `{function, column}` in aggregation
  mode), so anything the builder made round-trips into create/modify. Read-back limits on a HUMAN-made
  element: a linked-object column is omitted from
  `columns` (it cannot be expressed here), and `sort` is the EFFECTIVE PRIMARY entry — the one the
  runtime's ORDER BY actually ranks first — while any further ACTIVE secondary sort entries are not
  reported, and a `sort` write replaces the whole stored order. So for such an element the described
  block is narrower than what it really does — do not feed it back as a full replacement.
- A HUMAN SAVE quietly changes a builder-made element's plumbing: opening the element card and clicking OK
  always writes `ReadSomeTopRecords = true` + `NumberOfRecords`, which a builder-made element leaves unset
  (row count stays 1 — what "first record" means). Under the `FeatureReadDataUserTaskEntityReadOldMode`
  feature that flag changes how many rows the element reads, and the drift is INVISIBLE to
  `describe-business-process` (unset parameters are omitted) — a builder-made and a human-touched element
  look identical there. You cannot SEE it, but you can clear it: ANY `setElement.readData` update clears that
  pair unless the element is in collection mode, mode change or not. So the repair for a human-touched
  element is to re-send its `readData` block (even unchanged in substance) rather than to hunt the flag.
  The same OK also writes `FunctionType = 0` on EVERY element, the card saving the function unconditionally.
  Harmless — `ResultType` alone decides the mode — so read it as a human fingerprint, not a corrupted mode.


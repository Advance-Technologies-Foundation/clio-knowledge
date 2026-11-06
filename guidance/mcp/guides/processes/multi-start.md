clio MCP process multi-start guide

Scope
Use this guide when a process reacts to SEVERAL record events (for example a record added AND a column of it changed) and every one of them must run the SAME steps. The graph itself belongs to `process-modeling`; parameters in general to `process-parameters`.

Gate: use `mergeFrom` only when the FULL `create-business-process` contract (`get-tool-contract` with `detail=full`) names `mergeFrom`. Otherwise this clio does not carry it: give each start's branch a Formula that sets the shared parameter (the multi-start example in `process-parameters-details`), and tell the user that a clio update replaces those Formulas with `mergeFrom`.

Why a shared chain needs a merge
A `signalStart` fires on ONE event, so each event needs its own start, and each start fills only its OWN `RecordId`. An element after both starts that reads one start's `RecordId` gets an empty Id when the other start fired: the run completes with no error and changes nothing. Measured in November 2026 on .NET Framework and .NET 8 stands: the second trigger ran, completed, and updated no record.

The shape
- Declare ONE process parameter with `mergeFrom`, listing each start's `RecordId`. It takes the Id of whichever start fired.
- Flow every start STRAIGHT into the shared chain - no Formula per start, no copy of the chain.
- Read the record by that parameter (a filter `processParameter`, a mapping source), never by one start's `RecordId`.
```
"parameters": [
  { "name": "OpportunityProductIdParameter", "type": "Guid",
    "mergeFrom": ["OpportunityProductAddedSignal.RecordId", "OpportunityProductChangedSignal.RecordId"] }
],
"elements": [
  { "name": "OpportunityProductAddedSignal", "caption": "Opportunity product added", "type": "signalStart",
    "signal": { "entity": "OpportunityProductInterest", "on": "added" } },
  { "name": "OpportunityProductChangedSignal", "caption": "Opportunity product changed", "type": "signalStart",
    "signal": { "entity": "OpportunityProductInterest", "on": "modified", "changedColumns": ["Product"] } },
  { "name": "ReadOpportunityProduct", "caption": "Read opportunity product", "type": "readData",
    "readData": { "source": "OpportunityProductInterest", "mode": "first" },
    "filter": { "object": "OpportunityProductInterest", "logicalOperation": "and", "conditions": [
      { "column": "Id", "comparison": "equal", "processParameter": "OpportunityProductIdParameter" } ] } }
],
"flows": [
  { "source": "OpportunityProductAddedSignal", "target": "ReadOpportunityProduct" },
  { "source": "OpportunityProductChangedSignal", "target": "ReadOpportunityProduct" }
]
```

Rules (each is refused by name, nothing is saved)
- Two or more entries, each `"<StartName>.RecordId"`; only a signal start's `RecordId`; each start once.
- All starts on ONE object. A start on another object (for example a product's name changed, for opportunity products) keeps its OWN chain: its Id is another object's record.
- Type `Guid` (the default when the type is left out) or a `Lookup` to the starts' object. Direction is always `Internal`; no `value`, `typeFromElement` or `itemProperties`.
- No mapping onto the parameter: `mergeFrom` is its one value source.
- The order of the list does not matter: the process stores the starts in the order they appear in it, so the same request always builds the same process.

Modify
- Add one to an existing process: `addParameter` with `mergeFrom`, after the starts exist.
- `setParameter` with `mergeFrom` replaces the list; `mergeFrom: []` removes the merge (send a `value` in the same update to give it a constant instead).
- `removeElement` of a listed start is refused: take it out of the list first. Moving a listed start to another object (`setSignal` with another `entity`) is refused unless every listed start moves with it in the same request; changing its event is allowed.
- `describe-business-process` reports the parameter as `mergeFrom`, with no source or value beside it; send it back as reported and nothing changes. A formula hand-edited in the process designer is reported as stored; `setParameter` with `mergeFrom` restores the merge.

Warnings on a saved build or edit
- "Element 'X' runs after the starts ... but reads the record Id of ... only": a naive join. Merge the starts as the warning says, and read the parameter instead.
- "... reaches it without being in 'P'.mergeFrom": a start on the same object reaches an element that reads the merged parameter but is not in the list, so on its runs the parameter is empty. Add it to the list.

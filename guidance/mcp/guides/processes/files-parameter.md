clio MCP process-files-parameter guide — the Process file element's Process parameter variant (the `files` group)

Scope
This guide owns the `files` group of the Process file element (`fileProcessing`): copying the files the process
holds to a record's attachments, its one source, the levels it binds, its outputs and its read-back. `process-files`
owns the rest of the element (the contract gate, the `attachments` group, `action`, `saveTo`, the setElement merge,
the describe-only members other than `bindingLevels`, the consumers), the File and FileCollection parameters and
the two-level rules (P1-P3, R-M1-R-M3) this group applies: read it first. The report group: `process-files-report`.

== The Process parameter variant: the `files` group ==
It copies files the process holds to a record's attachments, and only that: saveTo is required on create and
useInProcess is refused.
{"type": "fileProcessing", "fileProcessing": {"files": {"processParameter": "DocumentsParameter"}, "saveTo":
{"object": "Contact", "recordId": {"processParameter": "ContactParameter"}}}}
- `files` takes exactly ONE of processParameter, sourceElement + sourceElementParameter, or expression. A value is
  refused (a file has no constant form), and so are a source with no file item or with several, a parameter of the
  element itself, and any mapping FROM its Files.
- A collection (a FileCollection, or an element's ObjectFiles or ReportFiles) binds BOTH levels, Files and
  Files.File: one copy per row. A single File binds Files.File only (P3, `process-files`) and copies that one file.
  A single File must be set before the element runs: an unset one stops the process with an error. An empty
  collection copies nothing, silently.
- An expression goes onto Files.File as given, with a notice: nothing checks that it yields one file.
- Outputs: ObjectFiles (item ObjectFile) holds the COPIES, and CreatedObjectFileIds their ids. It has no filter.
- Describe adds the describe-only fileProcessing.bindingLevels (both, itemOnly, outerOnly or none), with an issue
  for a stored shape that would not resubmit as stored (none: nothing bound, the platform refuses to start the
  process) and for an action other than save: a stored useInProcess or an invalid one fails at run time, a mapped
  one is decided at run time. Remove it before resubmitting.

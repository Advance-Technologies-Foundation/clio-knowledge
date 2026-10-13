clio MCP process-data-elements-details guide — the run-time evidence for a Read data column as a source, and how describe reads such values back

A details article of the process guide set, reached through `process-data-elements`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the RUN-TIME EVIDENCE behind a column of a Read data record
used as a value source (the end-to-end run, the UId token measured in an `expression` mapping and a
Formula body, and the modify-path condition run), of HOW the server resolves such a source, of why a
record trigger is a signal start, and of how
`describe-business-process` reads back a column source and a Modify data value the write path would
refuse. Split out of `process-data-elements`, which keeps the record triggers, the Modify data block,
every form, refusal, version gate and MUST, and the read-back-proves-authoring-not-routing rule; read
that first. Nothing here relaxes a rule stated there.

== Trigger a process on a record event — why a signal start ==
`process-data-elements` makes a record-event process START with a Signal start element, not a page
save handler. The signal start is the platform-native, declarative trigger.

== A column of a Read data record as a source — how it resolves ==
The server resolves a column source (`sourceElement` + `sourceColumn`) on the read object, type-checks it
against the target and writes the platform's three-segment meta path, so there are no UIds to find.

== A column of a Read data record as a source — evidence ==
- Verified at run time on Creatio 10.1.37 (.NET Framework, MSSQL), CrtProcessBuilder 1.6.6.27, 2026-09-25: one
  create call built signalStart (Contact added) -> readData (Id = RecordId) -> exclusiveGateway on
  `[#ReadContact.ResultEntity.DoNotUseCall#] == false` -> Perform task with `OwnerId` <- `sourceColumn: "Owner"`.
  A contact with `DoNotUseCall = false` ran the call branch and its Activity got the contact's owner (not the
  process starter); one with `true` ran the fallback.
- The UId form of a column reference, which the MODIFY path uses (`process-data-elements` states its spelling
  and checks), was measured in a raw `expression` mapping onto a process parameter and in a Formula body: that
  UId token (in its prefix-less form) delivered a Text (`Contact.Name`) and a Lookup (`Contact.Account`) column at run time
  (Creatio 10.1.37, .NET Framework, MSSQL; CrtProcessBuilder 1.6.6.22; 2026-09-24).
- A modify-path condition written by name was described and run with a matching and a non-matching record,
  for `Contact.DoNotUseCall` true/false, on Creatio 10.1.585 (.NET 8, PostgreSQL), clio 8.1.0.131, CrtProcessBuilder 1.6.2.24; 2026-09-21.

== A column of a Read data record as a source — read-back ==
`describe-business-process` reports such a value's `sourceElement` / `sourceElementParameter` /
`sourceColumn` beside the raw `value`, only when those names would re-apply to the identical value
(same spelling, a fitting type, a loaded column).

== Modify data element (changeData) — read-back of a value the write path would refuse ==
When `describe-business-process` reads a `changeData` block back, a stored value the write path would refuse —
a non-text or empty constant, or a binding that fails the type check — reads back as its COLUMN ALONE rather
than as something you cannot write back, and any other formula comes back as its raw `[#…#]` in `expression`.

clio MCP process-send-email-attachments guide - the files a Send email element sends (`email.attachments`)

Scope
This guide owns `email.attachments` of the Send email element (`sendEmail`): entries, refusals, order and replace
rules, notices, read-back, missing files. `process-send-email` owns the rest
of the `email` block; `process-files` owns the File and FileCollection types and the Process file element whose
output an email usually sends. Two words clash: `email.attachments` are the files an email SENDS;
`fileProcessing.attachments` is the Object attachments group of a Process file element, which READS a record's files.
Needs CrtProcessBuilder 1.6.6.127 or later on the environment. Use it only when the FULL create-business-process
contract (`get-tool-contract` with `tool-names: ["create-business-process"]`, `detail: "full"`) names `attachments`
in the `email` block. Whatever the versions, read the element back: an `email` block without the `attachments` you
sent means the list did not land, so never report those files as attached.

== The entry ==
`"email": { ..., "attachments": [
   { "sourceElement": "ReadContactFiles", "sourceElementParameter": "ObjectFiles" },
   { "sourceElement": "GenerateInvoiceReport", "sourceElementParameter": "ReportFiles" },
   { "processParameter": "DocumentsParameter" } ] }`
- One entry is one attachment row of the designer and names exactly ONE source: `processParameter`, `sourceElement` +
  `sourceElementParameter` (both halves), or `expression` (a formula written onto the row's file as given). `value`
  is refused: a file has no constant form. A dotted path names an item (`ObjectFiles.File`).
- Sources that hold files: a Process file element's output - `ObjectFiles` (Object attachments, Process parameter)
  or `ReportFiles` (Generated report); another element's FileCollection or File output; a process FileCollection
  (for files a calling process passes in); a single File. Each row sends every file its collection holds, so one
  entry per source, however many files it carries.
- REFUSED, naming the entry by its zero-based position (`email.attachments[1]`): no source or more than one; `value`;
  an element or parameter that does not exist; a source that holds no files (neither a file nor a collection of
  files, or a collection with no File item); a collection with several File items (name the one to attach,
  `<collection>.<item>`); a collection nested inside another; the element's own parameter; a Process file element's
  `Files` input (the files it is GIVEN: attach what supplies them, or its `ObjectFiles`). More than 1000 entries is
  refused before any entry is checked, naming the count. A refused entry writes none of the list.
- A Send email made multi-instance in the designer refuses `email.attachments` whole, `[]` included: multi-instance
  moves the element's parameters into record collection items, and the run time reads only its top-level rows.
- The rows are `Attachments<N>` parameters of the element, each with one `Attachments<N>FileLocator` item. Write
  them only through `email.attachments`: a mapping (`mappings[]`, `addMapping`) or a Formula element result onto one
  is refused, and so is a mapping (structured source or `expression`) or an `email.attachments` entry that READS
  one (a row only receives files).

== Order: the source runs before the email ==
- `create-business-process`: declare the source element EARLIER in `elements[]` than the Send email, or the entry is
  refused with the forward-reference sentence. For an `expression` entry, every element its formula references must
  be declared earlier too. `modify-business-process`: the source must exist when the operation runs - add it in an
  earlier operation of the batch or an earlier request.
- On the FLOW the source must run before the email. When the source element cannot reach the Send email along
  sequence flows, the answer warns that it "does not lie on a path before" the email: fix the flows. The warning is
  a hint, not a proof - a source on a gateway branch the run does not take still reaches the email and is not
  warned about. A collection source that has not run sends no files and no error; a single File (or `expression`)
  source that has not run is empty, so the element fails.

== Modify: a sent list REPLACES ==
- `setElement` with `elementUpdate.email.attachments` replaces the element's attachments. Name it in `elementName`,
  not `element`. A row is kept as it is only when its stored binding is exactly what the entry would write (one row
  per entry: two equal entries keep or make two rows). A designer row in a shape this tool does not write (a
  collection level only, levels reading different sources, a stale collection level) is replaced by a new row for the
  same source. Every other entry gets a new row; every row of this process the list does not name is REMOVED.
  `attachments` omitted: the rows are untouched. `[]`: every row is removed. Recipients differ: they
  MATCH-OR-APPEND (`process-send-email`).
- To add or drop one: `describe-business-process element=<email>` IMMEDIATELY before, take its
  `email.attachments`, add or drop the entry, send the whole list. Modify has no concurrency check, so a list from an
  older read deletes the rows added since, in the designer too.
- A removed row gets a notice naming what it read and the entry, in the write shape, that restores it ("To
  restore it, add { ... } to the list."). None for a row the same request gives back in the same write shape, or
  one an earlier list of it created; a row with a formula on its collection level is rebuilt in the
  designer; a row that read nothing, as attaching nothing. On modify clio also reports the removed rows
  from its own before/after read-back (a warning; Info when you sent `[]`). A row something still reads is not
  removed: the request is refused, naming the readers.

== Notices (warnings: the process saves) ==
Judged at the end of the request for every entry of the last list sent; each starts `Send email '<element>': `.
Tell the user each: the build cannot settle it.
- a single File: if it is empty when the email runs, the element fails;
- an `expression`: not checked; it must yield one file, and none fails the element;
- two entries naming the same source (a collection and its item path count as the same): each file is attached
  twice; the same files reached by two different routes are not detected;
- a source not on a path before the email (above);
- the files a Sub-process or a custom user task returns: this tool cannot see which records that code reads them
  from, so make sure it reads only the records it should.

== An unscoped record source is refused ==
The email reads its files with rights disabled and sends them outside Creatio. So a Process file element whose files
an email sends - directly, or handed on through a process parameter, a Process parameter element, a formula or a
Sub-process input - MUST select its records: an Object attachments element by `attachments.recordId` or a filter
condition, a Generated report by `report.recordId` or a filter condition. Otherwise the request is refused at its
END ("has no record scope and no filter condition ... sends them outside Creatio"), whatever order the batch used.
A stored filter the check cannot read is refused too. EVERY save through this tool is judged, so a designer-built
process whose email already sends an unscoped source is refused on its next save of any kind, until the source is
scoped or the attachment removed. A filter that selects something passes, even one by file name only.

== Read back ==
`describe-business-process element=<email>` (or `mode=full`) returns `email.attachments`, each entry in
the write shape plus the describe-only `slot` (`Attachments<N>`), `bindingLevels` (`both` | `outerOnly` | `itemOnly`
| `none`) and `issues` (a row that fails or surprises at run time, an entry whose re-send would be refused, a source
that makes the next save refuse, a multi-instance element). An element with no rows OMITS `attachments`. Send the
entries back unchanged; clio ignores `slot`, `bindingLevels` and `issues` with a warning, so drop them. An entry
with an issue may be refused when sent back: the issue says what to send instead. A row created in another process
schema is listed with an issue - this process never sends it and a list never touches it: leave it out.
- Order (CrtProcessBuilder 1.6.6.128+): the designer's row order, the order the email attaches them; a new row goes
  last, so `slot` numbers can have gaps and need not ascend.

== At run time ==
- The rows work alike in auto and manual mode and with a custom or a template message: auto mode attaches the files
  to the message (and, with `CreateActivity` on, to its email activity); manual mode copies them onto the email
  activity it creates.
- A row with no file fails the element: an empty single File, or an `expression` that yields none.
- An email that arrives WITHOUT its files: check that the source ran before the email, that its record scope or
  filter selected files, and feature `UseProcessEmailAttachments` for the user who runs the process - with it off,
  the platform sends the email, or creates the manual email activity, without the files and without an error. No
  build or save checks that feature.

Evidence: the build-time rules are pinned by CrtProcessBuilder 1.6.6.128 unit tests, compared with rows the
designer saved on a stand on 2026-10-29. The run-time behaviour is read from the CrtProcessDesigner 7.8.0 and core
(Terrasoft.Core, Terrasoft.File) run-time sources. Measured there: auto mode, custom and template message, delivered
every file (seen in the mailbox), and with `CreateActivity` on the email activity carried them; manual mode's
prepared email activity carried every file, original names; its sending was not measured.

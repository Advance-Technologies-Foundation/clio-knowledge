clio MCP process-send-email-attachments guide - the files a Send email element sends (`email.attachments`)

Scope
This guide owns `email.attachments` of the Send email element (`sendEmail`): the entry and its sources, the order
rule, the replace rule on modify, the refusals and notices, the read-back, and why an email can arrive without its
files. `process-send-email` owns the rest of the `email` block; `process-files` owns the File and FileCollection types
and the Process file element whose output an email usually sends. Two words clash: `email.attachments` are the files
an email SENDS; `fileProcessing.attachments` is the Object attachments group of a Process file element, which READS a
record's files.
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
  `Files` input (the files it is GIVEN: attach what supplies them, or its `ObjectFiles`); more than 1000 entries. A
  refused entry writes none of the list.
- A Send email made multi-instance in the designer refuses `email.attachments` whole, `[]` included: the run time
  does not look for attachments there.
- The rows are `Attachments<N>` parameters of the element, each with one `Attachments<N>FileLocator` item. Write
  them only through `email.attachments`: a mapping (`mappings[]`, `addMapping`) or a Formula element result onto one
  is refused, and so is any mapping, formula or attachment that READS one (it only receives files).

== Order: the source runs before the email ==
- `create-business-process`: declare the source element EARLIER in `elements[]` than the Send email, or the entry is
  refused with the forward-reference sentence. `modify-business-process`: the source must exist when the operation
  runs - add it in an earlier operation of the batch or an earlier request.
- On the FLOW the source must run before the email. When the source element cannot reach the Send email along
  sequence flows, the answer warns that it "does not lie on a path before" the email: fix the flows. The warning is
  a hint, not a proof - a source on a gateway branch the run does not take still reaches the email and is not
  warned about. An email whose source has not run goes out WITHOUT those files and WITHOUT an error.

== Modify: a sent list REPLACES ==
- `setElement` with `elementUpdate.email.attachments` replaces the element's attachments. A row that already reads
  an entry's source is kept as it is (one row per entry: two equal entries keep or make two rows); every other entry
  gets a new row; every row the list does not name is REMOVED. `attachments` omitted: the rows are untouched. `[]`:
  every row is removed. Recipients differ: they MATCH-OR-APPEND (`process-send-email`).
- To add or drop one: `describe-business-process element=<email>` IMMEDIATELY before, take its
  `email.attachments`, add or drop the entry, send the whole list. Modify has no concurrency check, so a list from an
  older read deletes the rows added since, in the designer too.
- Each removed row gets a notice naming what it read and the entry, in the write shape, that restores it ("To
  restore it, add { ... } to the list."); clio repeats them after the read-back. A row something still reads is not
  removed: the request is refused, naming the readers.

== Notices (warnings: the process saves) ==
Judged at the end of the request for every entry of the last list sent, kept or new; each starts
`Send email '<element>': `. Tell the user each one: it is a run-time risk the build cannot settle.
- a single File: if it is empty when the email runs, the element fails;
- an `expression`: not checked; it must yield one file, and none fails the element;
- two entries reading the same files: each file is attached twice;
- a source not on a path before the email (above);
- the files a Sub-process or a custom user task returns: this tool cannot see which records that code reads them
  from, so make sure it reads only the records it should.

== An unscoped record source is refused ==
The email reads its files with rights disabled and sends them outside Creatio. So a Process file element whose files
an email sends - directly, or handed on through a process parameter, a Process parameter element, a formula or a
Sub-process input - MUST select its records: an Object attachments element by `attachments.recordId` or a filter
condition, a Generated report by `report.recordId` or a filter condition. Otherwise the request is refused at its
END ("has no record scope and no filter condition ... sends them outside Creatio ... Scope it first"), whatever order
the batch used. EVERY save through this tool is judged, so a designer-built process whose email already sends an
unscoped source is refused on its next save of any kind, until the source is scoped or the attachment removed. A
filter that selects something passes, even one by file name only.

== Read back ==
`describe-business-process element=<email>` (or `mode=full`) returns `email.attachments` by row number, each entry in
the write shape plus the describe-only `slot` (`Attachments<N>`), `bindingLevels` (`both` | `outerOnly` | `itemOnly`
| `none`) and `issues` (only when a stored row fails or surprises at run time). An element with no rows OMITS
`attachments`. Send the entries back unchanged; clio ignores `slot`, `bindingLevels` and `issues` with a warning, so
drop them. An entry with an issue may be refused when sent back: the issue says what to send instead. A row created
in another process schema is listed with an issue - this process never sends it, and a list neither replaces nor
removes it: leave it out.

== At run time ==
- The rows work alike in auto and manual mode and with a custom or a template message: auto mode attaches the files
  to the message; manual mode copies them onto the email activity it creates.
- A row with no file fails the element: an empty single File, or an `expression` that yields none.
- An email that arrives WITHOUT its files: check that the source ran before the email, that its record scope or
  filter selected files, and feature `UseProcessEmailAttachments` for the user who runs the process - with it off,
  the platform sends the email, or creates the manual email activity, without the files and without an error. No
  build or save checks that feature.

Evidence: the contract, the refusals, the notices, the replace rule and the end-of-request scope check are pinned by
CrtProcessBuilder 1.6.6.127 unit tests, which also compare the rows it writes with rows the designer saved on a
stand on 2026-10-29. The run-time behaviour - both modes, rights disabled, the feature, an unrun source, an empty file - is
read from the CrtProcessDesigner 7.8.0 run-time sources and has not been measured through this tool.

clio MCP process-send-email-attachments guide - the files a Send email element sends (`email.attachments`)

Scope
This guide owns `email.attachments` of the Send email element (`sendEmail`). `process-send-email` owns the rest
of the `email` block; `process-files`, the File types and the Process file element. Two
words clash: `email.attachments` are the files an email SENDS;
`fileProcessing.attachments` is the Object attachments group of a Process file element, which READS a record's files.
Needs CrtProcessBuilder 1.6.6.127 or later on the environment. Use it only when the FULL create-business-process
contract (`get-tool-contract` with `tool-names: ["create-business-process"]`, `detail: "full"`) names `attachments`
in the `email` block. Always read the element back: an `email` block without the `attachments` you
sent means the list did not land, so never report those files as attached.

== The entry ==
`"email": { ..., "attachments": [
   { "sourceElement": "ReadFiles", "sourceElementParameter": "ObjectFiles" },
   { "processParameter": "DocumentsParameter" } ] }`
- An entry is one attachment row and names exactly ONE source: `processParameter`, `sourceElement` +
  `sourceElementParameter` (both halves), or `expression` (a formula written onto the row's file as given). `value`
  is refused: a file has no constant form. A dotted path names an item (`ObjectFiles.File`).
- Sources that hold files: a Process file element's output - `ObjectFiles` (Object attachments, Process parameter)
  or `ReportFiles` (Generated report); another element's FileCollection or File output; a process FileCollection;
  a single File. Each row sends every file its collection holds, so one
  entry per source.
- REFUSED, naming the entry by its zero-based position (`email.attachments[1]`): no source or more than one;
  a missing element or parameter; a source that holds no files (neither a file nor a collection of
  files, or a collection with no File item); a collection with several File items (name the one to attach,
  `<collection>.<item>`); a collection nested inside another; the element's own parameter; a Process file element's
  `Files` input (the files it is GIVEN: attach what supplies them, or its `ObjectFiles`). More than 1000 entries is
  refused before any entry is checked, naming the count. A refused entry writes none of the list.
- A Send email made multi-instance in the designer refuses `email.attachments` whole, `[]` included: multi-instance
  moves the element's parameters into record collection items, and the run time reads only its top-level rows.
- The rows are the element's `Attachments<N>` parameters, each with one `Attachments<N>FileLocator` item. Write
  them only through `email.attachments`: a mapping (`mappings[]`, `addMapping`) or a Formula element result onto one
  is refused, and so is a mapping (structured source or `expression`), an `email.attachments` entry or
  (CrtProcessBuilder 1.6.6.129+) a Formula element body that READS one (a row only receives files).

== Source order ==
- `create-business-process`: declare the source element EARLIER in `elements[]` than the Send email, or the entry is
  refused with the forward-reference sentence. For an `expression` entry, every element its formula references must
  be declared earlier too. `modify-business-process`: the source must exist when the operation runs - add it in an
  earlier operation of the batch or an earlier request.
- On the FLOW the source must run before the email. If it cannot reach the email along sequence flows, the
  answer warns that it "does not lie on a path before" the email: fix the flows. The warning is
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
- A list cannot REORDER rows: position is ignored, a kept row stays put, a new row goes last; a reordered list
  changes nothing, without a notice. For another order send `[]`, then the list in that order (new rows; refused
  while something reads a row).
- To add or drop one: `describe-business-process element=<email>` IMMEDIATELY before, take its
  `email.attachments`, add or drop the entry, send the whole list. Modify has no concurrency check, so a list from an
  older read deletes the rows added since, in the designer too.
- A removed row's notice names what it read and the entry, in the write shape, that restores it ("To
  restore it, add { ... } to the list."). None for a row the same request gives back in the same write shape, or
  one an earlier list of it created; a row with a formula on its collection level: rebuild it in the designer to
  restore it; one that read nothing, as attaching nothing. On modify clio also reports the removed rows
  from its own before/after read-back (a warning; Info when you sent `[]`). A row something still reads is not
  removed: the request is refused, naming the readers.

== Notices (warnings: the process saves) ==
Judged when the request ends, on every entry of the last list sent; each starts `Send email '<element>': `.
Tell the user each: the build cannot settle it.
- a single File, or an `expression` (not checked; it must yield one file);
- two entries naming the same source (a collection and its item path count as the same): each file is attached
  twice; the same files reached by two different routes are not detected;
- the files a Sub-process or a custom user task returns: this tool cannot see which records that code reads them from:
  make sure it reads only the records it should;
- (CrtProcessBuilder 1.6.6.130+) the files of a process parameter that is In, or Variable that no mapping or Formula
  result writes: a caller picks them and must pass only scoped ones.
On EVERY save (1.6.6.130+), a designer-built process too, a Sub-process gets a notice when the files of an unscoped
Process file source reach its In/Variable file input: the called process may send them out, so scope it.

== An unscoped record source is refused ==
The email reads its files with rights disabled and sends them outside Creatio. So a Process file element whose files
an email sends - directly or handed on (process parameter, Process parameter element, formula, Sub-process input) -
MUST select its records: an Object attachments element by `attachments.recordId` or a filter
condition, a Generated report by `report.recordId` or a filter condition. Otherwise the request is refused at its
END ("has no record scope and no filter condition ... sends them outside Creatio"), whatever order the batch used.
A stored filter the check cannot read is refused too. EVERY save through this tool is judged, so a designer-built
process whose email already sends an unscoped source is refused on its next save of any kind, until the source is
scoped or the attachment removed. A filter that selects something passes, even one by file name only.

== Read back ==
Describe (`element=<email>` or `mode=full`) returns `email.attachments`, each entry in the write shape plus the
describe-only `slot` (`Attachments<N>`), `bindingLevels` (`both` | `outerOnly` | `itemOnly` | `none`) and `issues`
(a row that fails or surprises at run time, an entry a re-send refuses, a source the next save refuses, a
multi-instance element). An element with no rows OMITS `attachments`. clio ignores `slot`,
`bindingLevels` and `issues` with a warning, so drop them. An entry with an issue may be refused when sent back: the
issue says what to send instead. A row created in another process schema is listed with an issue; this process never
sends it, no list touches it: leave it out.
- Order (CrtProcessBuilder 1.6.6.128+): the designer's row order, the order the email attaches them; `slot` numbers
  can have gaps and need not ascend.

== At run time ==
- The rows work alike in auto and manual mode and with a custom or a template message: auto mode attaches the files
  to the message (and, with `CreateActivity` on, to its email activity); manual mode copies them onto the email
  activity it creates.
- A row with no file fails the element: an empty single File, or an `expression` that yields none.
- An email that arrives WITHOUT its files: check that the source ran before the email, that its record scope or
  filter selected files, and feature `UseProcessEmailAttachments` for the user who runs the process - with it off, the
  email goes out, or the manual email activity is created, without the files or an error. No
  build or save checks that feature.

Evidence: the build-time rules are pinned by CrtProcessBuilder 1.6.6.130 unit tests, compared with rows the
designer saved on a stand on 2026-10-29. The run-time behaviour is read from the CrtProcessDesigner 7.8.0 and core
(Terrasoft.Core, Terrasoft.File) run-time sources. Measured on a stand on 2026-10-29: auto mode, custom and
template message, delivered every file (in the mailbox), and the CreateActivity email activity carried them; manual
mode's prepared email activity carried every file, original names; its sending was not measured.

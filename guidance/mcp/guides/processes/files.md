clio MCP process-files guide — File and FileCollection parameters, two-level collection binding, the Process file element

Scope
This guide owns the `File` and `FileCollection` process parameter types, the two-level rules (P1-P3, P2-MI, R-M1-R-M3) every structured mapping onto a collection or an item of one goes through, and the Process file
element (`fileProcessing`, last section; its report group: `process-files-report`, its `files` group:
`process-files-parameter`). Mappings in general: `process-parameters`.
Use only when the FULL create-business-process contract (`get-tool-contract` with
`tool-names: ["create-business-process"]`, `detail: "full"`) names type File in `parameters[]`; the default reply is
shortened and lists no types, so its silence proves nothing. Such a clio is called File-aware below and in the guides
that cite this one. An environment whose CrtProcessBuilder predates this support refuses `type: File` as not
supported and saves nothing, but it does NOT apply the rules below either: it stores an outer-only mapping as sent,
binds an item mapping to the item alone and reads a file collection back as `CompositeObjectList`.
A File-aware clio refuses such an environment up front; the remedy it names is `install-process-builder`, which
you run only after the user approves it (`core-rules`).

== The two types ==
- `File` — ONE file, carried by reference: the platform's FileLocator type. Aliases `file`, `filelocator` and
  `file locator`, case-insensitive. The platform's own type NAME "File" is a different, byte-carrying type the flow
  cannot store, so the alias never reaches it. `Binary` and `Blob` are refused: a process parameter carries a file
  by reference, not its bytes.
- `FileCollection` — SEVERAL files: a record collection (`CompositeObjectList`) with exactly ONE item, `File`
  (FileLocator, Variable, no tag). Aliases `filecollection`, `file collection` and `collection of files`. The alias
  builds the shape.
- Direction: a FileCollection defaults to Variable; a generic Collection to Out. A File defaults to Variable too;
  declare In or Out only to narrow it. An explicit direction always wins.
- Neither takes a `value` (a file has no constant form) nor a `referenceSchema` (it is not a lookup): both are
  refused.
- Declare them in parameters[] (create) or with addParameter (modify), for example
  "parameters": [{"name": "ContractFileParameter", "type": "File"},
    {"name": "DocumentsParameter", "type": "FileCollection", "direction": "In"}]

== Reading them back ==
- Read a process back with `describe-business-process mode=full`, or `element=<name>` for one element (`mode`
  is older than the File types, so every File-aware clio takes it). A `File` reads back as type
  `FileLocator`. A PROCESS parameter that is a collection with no tag and exactly one FileLocator item reads back as
  type `FileCollection`, with its `itemProperties`. Both types re-submit as written: `FileLocator` is an accepted
  type name, and `FileCollection` rebuilds the same shape. An element's file collection keeps the platform name
  `CompositeObjectList`.
- ITEM NAME: a file collection made in the designer may name its one item otherwise (`Attachment`). It
  still reads back as `FileCollection`, with its real item name (measured on CrtProcessBuilder 1.6.6.98 on
  2026-10-14). On THAT process, address the item by that name. A REBUILD re-sent with its `itemProperties` keeps
  that item. Rebuilt from the type alone, it names the item `File`,
  and a mapping naming `AttachmentsParameter.Attachment` is refused ("has no item 'Attachment'", followed by the
  items it has): rename it to `File`.
- A value that is exactly one reference to an element output is reported with sourceElement +
  sourceElementParameter (dotted for an item: ObjectFiles.File) beside the raw value, when re-sending those names
  would change no other level; anything else, a process parameter reference included, comes back only as the raw
  formula. Re-send a binding by name so the rules below run.

== Where the files come from ==
- The Process file element outputs: Object attachments `ObjectFiles` (item `File`, the files it read), Generated
  report `ReportFiles` (item `File`), Process parameter `ObjectFiles` (item `ObjectFile`, the copies it made). A
  saving element also lists the new files in `CreatedObjectFileIds` (item `Id`, a Guid). Map from these whatever the
  action.
- Mirroring a file output (`typeFromElement`, dotted or not) is refused: declare a FileCollection and map it from
  the output, which binds both levels.

== Binding a collection: both levels ==
When an element or a sub-process reads a collection input, the run time builds each row from the items of that
input (platform source): an unbound item reads empty, and the Process file element fails on it. So a collection
is bound at two levels, the collection (which rows) and its item (what each row holds), as the designer does at
once. With a File-aware clio every structured mapping
(`sourceElement` + `sourceElementParameter`, or `processParameter`, without `sourceColumn`) applies the rules below,
through mappings[], addMapping, setConnections and the approval block; a refused mapping writes no value. A dotted
name addresses an item on either side: DocumentsParameter.File as processParameter or targetProcessParameter,
ObjectFiles.File as sourceElementParameter, InputRecordCollection.<CalleeParam> as elementParameter.
A collection TAKES FILES when it is (a) the `Files` input of the Process parameter variant
(`ProcessFileProcessingUserTask`), or (b) a process parameter of the FileCollection shape. P2 and R-M2 apply to these
targets only.
- P1 — an ITEM from an ITEM (InputRecordCollection.ContractFileParameter from DocumentsParameter.File): the target's
  collection is bound to the source's as well, innermost first, for as many levels as the source has.
  Every collection, a multi-instance `InputRecordCollection` included (`process-sub-process`). A
  target collection already bound to that source is kept; one bound elsewhere is refused:
  a collection cannot take rows from two. A source nested deeper than the target is refused: outside its row
  it reads empty.
- P2 — a COLLECTION onto a collection that takes files: each file item of the target is bound to the source item of
  the same name (case-insensitive) or, when each side has exactly one file item, to that one (`Files.File` from
  `ObjectFiles.ObjectFile`). A notice names every item it binds or rebinds. An item it cannot pair is reported ("at
  run time it will be empty"), and cleared first when it was bound elsewhere. An item already bound into the new
  source is kept. Only file items are paired.
- R-M2 — a collection with NO file item onto a collection that takes files is refused ("has no File item"): map a
  file output or a FileCollection instead.
- P2-MI — a multi-instance `InputRecordCollection` a request binds (as its target, or through P1) with a file
  item, a File parameter of the called process, still unbound when the request ends gets a notice: that parameter
  "will be empty on every iteration" unless you map `InputRecordCollection.<F>`. Records without files are
  legitimate.
- P3 — a SINGLE file (not an item) onto a file item of a single-instance owner (any element but a multi-instance
  sub-process, or the process itself: `DocumentsParameter.File`) clears every bound collection above that item, one
  notice per level: the collection now carries that one file instead of one row per source row, and reads as one
  row at run time (measured on 2026-10-19 with the Process parameter element: one copy).
- R-M1 — a single File (a process parameter, or a flat File input of an element, a single-instance sub-process's
  included) from an ITEM of a collection is refused ("use a FileCollection, or a multi-instance sub-process"):
  outside a row the item reads empty. Measured: written by an older package that accepted it, it delivered no
  file from two.
- R-M3 — a collection that is an ITEM of another collection is refused as a ROOT collection's source (an item
  target is P1's): it reads empty outside a row.
- Order matters. Onto a collection that takes files, the LAST accepted mapping decides (an item taken from another
  collection is refused by P1). Item then outer: the outer one re-pairs an item bound outside its source (P2), and
  its notice names the old binding. Outer, then followed by a single-file mapping onto its item: the collection is
  cleared (P3). So copy a whole collection with the outer mapping ALONE, and pass one file with the item mapping
  alone. Onto any other collection (a multi-instance InputRecordCollection, a sub-process input) a later outer
  mapping is NOT checked against its items: from another source it silently leaves them reading the old one. There,
  map the outer level first: P1 then refuses an item from another collection.
- An `expression` goes through as given; only its meta-path tokens are checked, and none of the rules above runs on
  it (the stored text has its GUIDs lower-cased). A misspelled token, a token from another process and a column of a
  collection item are refused; a token that names an item (`Files.File`) is accepted. Prefer a structured source.

== Recipes ==
1. Keep the files an element produced:
   {"op": "addParameter", "parameter": {"name": "DocumentsParameter", "type": "FileCollection"}}, then
   {"op": "addMapping", "mapping": {"targetProcessParameter": "DocumentsParameter", "sourceElement": "ReadFiles",
   "sourceElementParameter": "ObjectFiles"}}. P2 binds DocumentsParameter.File to ReadFiles.ObjectFiles.File,
   with a notice.
2. Hand them to a Process parameter element: its `files` group (`process-files-parameter`), or onto one built in
   the designer
   {"op": "addMapping", "mapping": {"elementName": "CopyFiles", "elementParameter": "Files",
   "processParameter": "DocumentsParameter"}}; P2 binds Files.File to DocumentsParameter.File. Measured on
   CrtProcessBuilder 1.6.6.98 on a stand, on designer-made elements: one run of recipes 1 and 2 copied the three
   source files to the target record, names and bytes equal.
3. One called-process run per file: the callee declares a File input; the calling Sub-process element is
   multi-instance (`process-sub-process`) and maps InputRecordCollection.ContractFileParameter from
   DocumentsParameter.File (or from ReadFiles.ObjectFiles.File). P1 binds InputRecordCollection too.
   Send the InputRecordCollection mapping that guide asks for FIRST, from the SAME collection, the item
   mapping in the same request (see Order matters).
4. Into a SINGLE-instance sub-process's FileCollection input: map its ITEM from the source's file item; P1 binds the
   input too. That input does not take files: an outer-only mapping onto it is stored unpaired. Neither form has
   been run on a stand.
5. Verify with `describe-business-process element=<name>` (an element input) or `mode=full` (a process
   parameter): each level shows its source.

== Other refusals ==
- An addMapping value onto any file or collection target: a file has no constant form, so map it.
- setParameter with type FileCollection on a collection of another shape: remove it and add it again.
- removeParameter of a collection while a mapping reads one of its ITEMS (`process-parameters` owns that guard).
Every refusal here and above names the way forward and saves nothing: fix the call before re-sending.

== The Process file element: `fileProcessing` ==
Build it only when the FULL contract (as above) names `fileProcessing` (server alias `processFile`); such a clio
refuses a CrtProcessBuilder older than 1.6.6.103 up front (1.6.6.109, for every group, if it also offers report
and files), as above. The block carries ONE group, and the group IS the variant: `attachments` (Object
attachments), `report` (Generated report: `process-files-report` owns it) or `files` (Process parameter:
`process-files-parameter` owns it); 1.6.6.103 and 1.6.6.104 write attachments only and refuse the other two by
name. Omit `userTaskName`. The generic userTask route naming any of the three Process file tasks is refused: build
them only here.
{"type": "fileProcessing", "fileProcessing": {"attachments": {"object": "Account", "recordId": {"processParameter":
"AccountParameter"}}, "saveTo": {"object": "Contact", "recordId": {"processParameter": "ContactParameter"}}}}
- `attachments.object`: the record object or its attachment object (`AccountFile`). Storage is derived as in the
  designer, by name: dedicated when an attachment object named after the object exists (`<X>File`; Lead's is
  `FileLead`), otherwise SysFile. Name an attachment object no object pairs with by name (`DNSGuideFile`) directly,
  as `attachments.object` (no `recordId` then: select its files with setFilter), or as `saveTo.object` with
  `saveTo.linkColumn`: naming the record object keeps that object's own storage, with a notice naming the attachment
  object the element then does not read or save to (when one of its own lookup columns references the record). The
  same rule derives `saveTo.object`; a SysFile target links by `RecordId`, so omit `saveTo.linkColumn`.
  A CrtProcessBuilder of 1.6.6.109 or older refuses SysFile, and the clio that bundles it cannot lift that: tell the
  user SysFile storage needs a newer clio (one that bundles a newer package refuses the old one up front, as above).
- On a SysFile source, `attachments.sort` and setFilter conditions name SysFile columns (`Name`): a column of the
  record object (Account address's `GPSE`, which the designer's sort list offers) is refused, naming SysFile.
  Send the record as `attachments.recordId`, never as a `RecordId` equality in setFilter (refused as a scope).
- The run time reads only the SysFile rows whose `RecordSchemaName` is the record object and whose type is File: a
  file stored without `RecordSchemaName` is never read, and the element completes and copies nothing. Verify a run in
  the record's Freedom UI attachment list: SysFile copies the element saved show there (measured on 1.6.6.110,
  2026-10-20).
- `attachments.recordId`, WHOSE files: `value` (an existing record's id), `processParameter`, `sourceElement` +
  `sourceElementParameter` (+ `sourceColumn`), or the `expression` `[#SysVariable.CurrentUserContact#]` for Contact.
  A structured source naming an item is refused here and in `saveTo.recordId`: the element reads its record once per
  run. It becomes the filter's record scope: `setFilter` REPLACES the other conditions and keeps the scope - send
  them all in one call, never the scope column.
- `attachments.numberOfRecords`: 1..5000; omitted on create, it defaults to 50 and does NOT read all; omitted on
  `setElement`, the stored count stays.
- `attachments.sort`: ONE object `{column, direction}`, a direct column of the attachment object. Never an array.
- `action`: `saveToAttachments` copies the files to the `saveTo` record (`object`, `recordId`, optional
  `linkColumn`); `useInProcess` hands them on. Omitted on create, it follows from `saveTo`. A constant
  `saveTo.recordId` must be an existing record; a mapped one gets a notice. After saving, `ObjectFiles` points at the
  SOURCE files; the copies are only in `CreatedObjectFileIds` (measured on 1.6.6.100, 2026-10-15), so switching to
  `useInProcess`, or moving `attachments.object` or `saveTo.object` to another object, is refused while an element
  reads it.
- A saving element with neither `recordId` nor a filter condition would copy arbitrary files: refused at the end of
  the request (`failedOperationIndex: null`).
- `setElement` merges member by member: an omitted member keeps its value; `{}` clears `attachments.recordId` or
  `attachments.sort`; `saveTo.linkColumn: ""` derives the column again, never clears it. Another `attachments.object`
  clears the stored filter and sort: resend `recordId`, `sort` and setFilter; another `saveTo` object needs its
  `recordId`.
- Read it back with `describe-business-process element=<name>` or `mode=full`. `source`, `storage`, `fileObject` and
  `attachments.linkColumn` are identity checks, never settings: drop them (and `saveTo.linkColumn`) when changing the
  object. Remove the describe-only members before resubmitting: `actionStored`, `outputs`, `issues` and `display` on
  every value source.
- Shipped product processes end in a record's attachments (Generated report); when a following element needs the
  files, map its output collection (Recipes). Send email attachments and Creatio.ai file inputs are not buildable
  here yet.
- The saved copies: iterate `CreatedObjectFileIds` with a multi-instance sub-process (its callee takes a Guid), or
  re-select them with Read data (not Modify data) on the target's attachment object, filtered on its link column =
  the target record and `CreatedOn` `CurrentHour`. Hazards: it also matches others' files from that hour, and misses
  a copy made before the hour turned.

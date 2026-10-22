clio MCP process-files-report guide — the Process file element's Generated report variant (the `report` group)

Scope
This guide owns the `report` group of the Process file element (`fileProcessing`): the printable, the records a
report is generated for, the file names, where the files go, its notices and its read-back. `process-files` owns the
rest of the element (the contract gate, the `attachments` group, `action`, `saveTo`, the setElement merge, the
describe-only members other than `bindingLevels` (`process-files-parameter`), the consumers) and the File and
FileCollection parameters: read it first. Filter conditions beyond the
record scope: `process-data-source-filters`. Add data: `process-add-data`.

== What it does ==
The `report` group (Generated report, `ReportFileProcessingUserTask`) generates an MS Word printable for each record
its filter selects, then saves the files to a record's attachments or hands them to the following elements as
`ReportFiles` (item `File`). Shipped product processes use it to attach a generated document to a record.
{"type": "fileProcessing", "fileProcessing": {"report": {"printable": "<templateId>", "recordId": {"processParameter":
"InvoiceParameter"}, "fileNameSuffixColumn": "Number"}, "saveTo": {"object": "Invoice", "recordId":
{"processParameter": "InvoiceParameter"}}}}

== The printable: `report.printable` ==
- Pass the `templateId` that `list-printables` returns (preferred), a `[#Lookup...#]` macro, or a caption (letter
  case ignored; matched in the culture of the user clio connects as). A caption several printables carry is narrowed
  to the one of the object of `report.recordId` (a Lookup process parameter or element output, without
  `sourceColumn`) when exactly one matches; otherwise it is refused, listing the candidates (up to 10) with their id,
  type and object.
- Only MS Word printables of an object are accepted. FastReport and DevExpress printables are refused, and so is a
  printable with no object (neither its own nor its section's).
- A printable that does not exist on this environment is refused with "create it in System Designer -> Report setup":
  the element cannot create one, so ask the user to, then retry. The refusal lists up to 10 MS Word printables by
  `templateId`, of the record's object when it can be told. Nothing is created or picked for you.
- `report.type` and `report.object` are identity checks derived from the printable, never settings: drop them, or
  send the derived value.
- Rights: the printable lookup and the record selection apply NO record permissions (`UseAdminRights = false` in
  the query means that no rights are checked at all), so every user sees the same printables. Generating the Word
  file is different: it reads the printable and the record's columns WITH rights checks, for the user the run time
  connects as. So at run time the element fails when the printable row is missing on that environment or that user
  cannot read it, and a record whose columns that user cannot read is generated with those fields blank, reporting
  success. A printable missing on the environment you build on is refused (above) and reported by describe; no
  design-time check sees the rights cases, or a printable missing only on another environment the process runs on.

== Which records: `report.recordId` and the filter ==
- `report.recordId` is the record scope: it is stored as `Id = value` on the report's object and takes the value
  sources of `attachments.recordId` (`process-files`), except the `[#SysVariable.CurrentUserContact#]` expression: the
  scope column `Id` is no lookup, so it is refused; pass the Contact record from the process. `{}` clears it.
- `setFilter` writes the other conditions on that object and keeps the scope; another `filter.object` is refused,
  and so is an `Id` equality condition beside `report.recordId` (the same scope set twice). `clearFilter` keeps the
  scope too. Both are refused while the stored printable is missing on this environment: set `report.printable` first.
- A Word report with neither a record scope nor a filter condition would generate one for EVERY record of its object:
  refused at the end of the request (`failedOperationIndex: null`), whatever the action.
- The records are not sorted: their order, and so the order of the files and their `(n)` numbers, is unspecified.
- Another printable of the SAME object keeps the filter, the scope and the suffix column, and checks the stored scope
  again. One of ANOTHER object clears those of the three that are stored, with a notice: a `report.recordId` sent
  with the change becomes the new scope; send setFilter after it. That move is refused while another element reads
  the element's `CreatedObjectFileIds`.

== File names ==
- `report.separateReports`: an MS Word printable generates one file per record. Omitted, it is written `true` on
  create or when the printable changes, as the designer forces, and the stored value is kept otherwise; an explicit
  `false` is kept, with a notice that the designer turns it on at its next save and the file names change then
  (stored `false`, every file gets the same name). `false` also clears a stored `fileNameSuffixColumn`, with a notice.
- A file is named after the printable's caption, read in the culture of the user the process runs as: with
  `fileNameSuffix`, `<caption>. <suffix>`; separate reports then add ` (<n>)`, or, with `fileNameSuffixColumn`,
  `. <the record's value>` (` (<n>)` when it is empty); then the extension.
- `fileNameSuffix` takes ONE value source (`value`, `processParameter`, `sourceElement` + `sourceElementParameter`
  (+ `sourceColumn`), `expression`); a constant is stored as a localizable value, as the designer stores it (how it
  reads under another culture is not measured). `{}` clears it; sending one clears a stored `fileNameSuffixColumn`.
- `fileNameSuffixColumn`: the code of a TEXT column of the report's object, with separate reports on and without
  `fileNameSuffix`. Only the text types the designer offers are accepted (localizable text too); a hashed, secure or
  color column is refused: the designer does not offer them, and the run time reads the column with no rights checks
  and writes its value into every file name. `""` clears it.

== Where the files go ==
- Save: `saveTo` as in `process-files`. A filter that can select several records attaches every report to the ONE
  `saveTo` record, with a notice: set `report.recordId` to generate one. After a save, `ReportFiles` points at the
  saved attachments.
- Use in process: the files are temporary and go on as `ReportFiles`; map that collection to a Process parameter
  element (`files`: `process-files-parameter`) or to a FileCollection (`process-files`). They live while the process instance runs and are
  deleted when it completes (measured on a stand: one temporary file while the process waited in a Perform task,
  none after it completed, and the copy a Process parameter element saved from it stayed). Read from source, not
  measured: a sub-process deletes the files it generated when IT completes, so one that hands `ReportFiles` to its
  caller through an Out or a Variable parameter (Variable is the default, for a FileCollection too) hands over deleted
  files: save them inside the sub-process. Also from source: an instance ending in an error keeps its temporary files
  (only completion and cancel delete them). A notice warns when they flow into an Out or a Variable process
  parameter: save them, or consume them in this process. Another warns when the element has a filter but no
  `report.recordId`: it generates one temporary file per matching record.
- PDF: a printable that asks for PDF (`ConvertInPDF`) gets it only with a Word-to-PDF converter package installed;
  otherwise the files are .docx (a notice says so).
- The group has no `sort`: do not send one.
- A notice also says when feature `EnableReportFileProcessingUserTask` is off on the environment: its process
  designer then only hides the source choice of the Process file element. The run time does not check the feature.

== Add data creates the report's data row first ==
When the record a printable reports on is made by the process itself, an Add data element inserts the row first,
with the columns the printable prints, and the report takes its id from the Add data output `RecordId`. For example,
a DNSGuideReport row, then the "DNS Requirements" printable generated for it and saved to DNSGuideFile, linked to
the sender domain. The shipped process that generates it saves to DNSGuideFile too, naming the sender domain as the
record but no link column, and links the files to it afterwards with a Modify data on the file name. As a whole
create-business-process descriptor (`<package>` from `get-target-package`, `<templateId>` from `list-printables`):
{"name": "UsrSenderDomain_GenerateDNSGuide", "caption": "Generate DNS guide", "packageName": "<package>",
"parameters": [{"name": "SenderDomainParameter", "type": "Lookup", "referenceSchema": "SenderDomain"}, {"name":
"SiteUrlParameter", "type": "Text"}], "elements": [{"name": "DNSGuideRequestedStart", "type": "startEvent", "caption":
"DNS guide requested"}, {"name": "AddDNSGuideReport", "type": "addData", "caption": "Add DNS guide report", "addData":
{"source": "DNSGuideReport", "values": [{"column": "SenderDomain", "processParameter": "SenderDomainParameter"},
{"column": "SiteUrl", "processParameter": "SiteUrlParameter"}]}}, {"name": "GenerateDNSRequirements", "type":
"fileProcessing", "caption": "Generate DNS requirements", "fileProcessing": {"report": {"printable": "<templateId>",
"recordId": {"sourceElement": "AddDNSGuideReport", "sourceElementParameter": "RecordId"}}, "saveTo": {"object":
"DNSGuideFile", "linkColumn": "SenderDomain", "recordId": {"processParameter": "SenderDomainParameter"}}}}, {"name":
"EndDNSGuideSaved", "type": "endEvent", "caption": "DNS guide saved"}], "flows": [{"source": "DNSGuideRequestedStart",
"target": "AddDNSGuideReport"}, {"source": "AddDNSGuideReport", "target": "GenerateDNSRequirements"}, {"source":
"GenerateDNSRequirements", "target": "EndDNSGuideSaved"}]}
Assign the other columns it prints the same way (`SPFHostname`, `SPFValue`, `DKIMHostname`, `DKIMValue`,
`TrackingValue`, `Username`): a column left unset prints blank, with no error. `saveTo.recordId` takes a Lookup of
SenderDomain, as declared, or a Guid. No object pairs with DNSGuideFile by name, so `saveTo` names its `linkColumn`,
and the save is accepted with a notice: opening and saving the element in the process designer clears that column,
and the files are then linked to no record. Change that element through clio, not in the designer.

== Reading it back ==
`describe-business-process element=<name>` (or `mode=full`) returns the `report` group: `printable` (the stored id),
`recordId`, `separateReports`, `fileNameSuffix` (a constant as `value`), `fileNameSuffixColumn`, the identity checks
`type` and `object`, and the describe-only `caption` and `convertToPdf`; its `outputs` are `ReportFiles` (item `File`)
and `CreatedObjectFileIds` (item `Id`). Remove `caption` and `convertToPdf`, with the describe-only members
`process-files` lists, before resubmitting.

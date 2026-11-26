clio MCP word-printable-troubleshooting guide — an MS Word printable fails, prints blank or wrong

Scope
This guide owns diagnosing an MS Word printable: where the error is recorded, the symptom-to-cause table, and the
settings and features that change generation. What a printable is and how it ships: `get-guidance name=word-printable`.
Template, field and converter rules: `get-guidance name=word-printable-template`. A printable generated from a business
process: `get-guidance name=process-files-report` first.

Applicability: read in the configuration source of Creatio 10.2.444 and 10.3.1152 and measured on a 10.3.1152 .NET
Framework / MSSQL environment. Creatio 8.x is not verified.

== Reproduce first ==
MUST reproduce with one record, AS the user who reported it (the generator reads with that user's rights), before
naming a cause, and keep the generated file. Do it without that user's password: ask the user to print the record and
share the file, or use an existing test user with the same roles; creating a user or giving one roles needs the owner's explicit
yes. MUST NOT ask for, reset or use another user's password. Unzip it and read `word/document.xml`: an empty `w:fldSimple` where a value was expected is a field
the generator saw and could not fill; an untouched field with its placeholder is a field it never recognised.

== Where the error is ==
- A failed generation returns to the caller (a page shows an error; `ReportService/CreateReport` answers HTTP 400)
  and the exception is written to `Error.log` of the day's log folder (`%TEMP%\Creatio\<site>\0\Log\<yyyy_MM_dd>` on
  an on-site .NET Framework host). Read the exception and the name it gives (a column UId, a converter, a record Id).
  Generator errors do NOT go to `MSWordLog.log` (measured).
- `MSWordLog.log` is a trace, not an error log (verified on 10.2.444 and 10.3.1152 source and on a 10.3.1152 run;
  earlier versions unknown, an 8.2.3 boundary from another source was not verified: an empty or missing file there
  proves nothing). Create feature `EnableMSWordLog` (it is
  not shipped) and turn it on;
  a run then writes `Start generating report for template …` and `Finish generating report …`, and for each table
  part `Count of records for table part with Entity <TableCaption> is <n>` (all measured); image load failures are
  logged there too. A Start line with no Finish line means the run failed: read `Error.log` for the reason. A count
  of 0 means the table part query found no record the generating user can read. Turn the feature off again
  afterwards.
- On Creatio cloud there is no file to read: the logs go to the database log appender. Ask whoever operates the
  environment for the error entry.

== Symptoms ==
| Symptom | Cause | Fix |
|---|---|---|
| `ItemNotFoundException` naming a column UId | a stale UId in `MacrosSettings` (any segment of a path), only when the template uses that column (measured) | re-pick the column in the designer, or remove the field |
| `ItemNotFoundException` naming the printable's Id | the row is missing on this environment, or the user cannot read `SysModuleReport` | check the row; for rights, see Rights below |
| `NullReferenceException` in `ExpressionConverterHelper.CreateExpressionConverter` | a converter that is not a top-level class in `Terrasoft.Configuration` (measured) | fix the namespace |
| `NullReferenceException` in `WordFieldUtility.FillWordFieldsInElement` | a converter returned `null` (measured), or a hand-written `MacrosSettings` entry lacks `caption` / `metaPathCaption` | return `""`; restore both keys |
| `ArgumentException` "An item with the same key has already been added" in `WordReportGenerator.AddColumnToEntitySchemaQuery` | a `<Caption> [#Token#]` field (a space before `[#`) after a bare `<Caption>` field (measured) | remove the space |
| `NullReferenceException` when printing several records | a list or filter generation with a missing template Id | pass a `templateId` from `list-printables` |
| error opening the template | `File` is empty or not a .docx | upload a .docx |
| a field prints nothing | no switch after the name; a caption that does not match exactly (case included); a space before `[#`; an unknown or mis-cased token first in a chain; a detail field with no records | compare the field with `GetWordReportSettings` captions and the token with the converter's attribute |
| a field stays a field with its placeholder | lower-case `mergefield`; a nested field; a field split by tracked changes, a hyperlink or a content control | re-insert it through the add-in or Insert -> Field |
| a table prints one empty template row | its table part found no record the user can read: the trace counts 0 (measured) | count the detail records as an administrator; if they exist, see Rights below |
| a field ignores the second macro | an unknown token later in a chain passes the value through | fix the token name |
| a macro argument is lost | the argument is only the second non-empty pipe-separated segment | use one segment; split values with another separator |
| values blank for a user, filled for an administrator | the generator reads the record, its joins and the table parts with the user's rights | name the objects and columns the user cannot read; see Rights below |
| table rows duplicated or growing | two table parts in one Word table, or a static picture in a field row | one table part per table; move pictures out of field rows |
| a detail value outside a table is wrong | pass 3 takes an arbitrary first record | put it in a table, filter the table part to one record, or set `DisableFillingTableFieldsOutsideTables` |
| an image does not print | its name does not equal a column caption; the column is an image LOOKUP (gives text); the value is empty (the picture is removed) | bind a binary column; rename the picture; check the value |
| an image is distorted | it is resized to its own pixel size and DPI | set `IgnorePictureAspectRatio` |
| PDF asked for, .docx delivered | no package binds `IPdfConverter` (synchronous paths fall back silently); in Classic with `AsyncPdfReportGeneration`, the FileConverter service (`ConvertServiceUrl`) | install a converter package, or fix the converter service |
| numbers misread by `NumberDigit` or a converter | `ReportDecimalSeparator` differs from the culture's separator | align them, or parse with an explicit culture |
| a converter works from a page and fails from a process | no HTTP context in the process run | null-check the context and fall back |
| a new converter has no effect | the package compile did not finish, or another type already claimed the token | check the compile; search the packages for the token |
| the printable is missing from a Freedom page | the browser-session cache, the `ShowInCard` / `ShowInSection` flags (product printables ship with both off), or no read on `SysModuleReport` | reload; check the flags; for rights, see Rights below |
| missing from a Classic menu, or template download / upload fails with error code 404 | the `MS Word` type name was renamed, or the user's culture has a non-matching translation of it, or `TypeColumnValue` excludes the record | restore the name; clear or correct `TypeColumnValue` |
| the upload answers HTTP 500 with error code 405 | no `Content-Range` header | send `Content-Range: bytes 0-<length-1>/<length>` |
| the target still prints the old template | the binding does not force-update `File`; its `ModifiedOnUtc` did not change; the binding was not re-saved after the upload, so it carries the old copy; or the target edited the binding locally and the install skipped it | see Ship it in a package in `get-guidance name=word-printable`; download the template on the target to confirm |
| the printable arrived without its type or object | the binding was generated by clio and lacks `Type` / `SysEntitySchema` | bind the row in the Configuration section with the full column list |

== Rights ==
A field that is blank only for some users is often the access model working as intended. MUST NOT widen object,
column or record rights to make a printable fill: report which objects and columns the user cannot read, ask the
owner whether that user should see them, and change rights only after an explicit yes. The owners: `object-rights`
(an object), `column-rights` (a column), `record-rights` (a record); `entity-operation-access` for an object
administered by operations, such as `SysModuleReport` itself.

== Settings and features that change generation ==
| Name | Kind | Effect |
|---|---|---|
| `Use7xFiltersForWordReports` | feature, shipped on | `MacrosSettings` / `FilterSettings` are authoritative; off, the legacy `MacrosList` is read |
| `EnableMSWordLog` | feature, not shipped | the trace in `MSWordLog.log`: start, finish, records per table part |
| `DisableUnusedFieldsRemoval` | feature | keeps the placeholder of an empty bound picture |
| `DisableFillingTableFieldsOutsideTables` | system setting, Boolean, not shipped | turns pass 3 off |
| `IgnorePictureAspectRatio` | system setting | keeps the template's picture size |
| `ReportDecimalSeparator` | system setting | decimal separator in values |
| `MoneyFieldDisplayPrecision` | system setting | rounding of two-decimal money values |
| `FileImportMaxFileSize` | system setting | template upload limit, MB |
| `AsyncPdfReportGeneration` | feature | Classic UI generates PDF through the FileConverter service |

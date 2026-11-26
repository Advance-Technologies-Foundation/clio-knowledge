clio MCP word-printable-template guide — the .docx template, merge fields, macros and custom converters

Scope
This guide owns what the MS Word generator reads from a .docx template and what it puts back: merge fields, field
names, how values look, out-of-the-box tokens, detail tables, images, PDF, and the contract of a custom
`IExpressionConverter`. The printable's rows, designer, display and shipping are owned by
`get-guidance name=word-printable`: read it first. Failures: `get-guidance name=word-printable-troubleshooting`.

Applicability: read in the configuration source of Creatio 10.2.444 and 10.3.1152 (identical generator code) and
measured on a 10.3.1152 .NET Framework / MSSQL environment; "measured" below means a template was generated on that
environment and the output `word/document.xml` was read. Creatio 8.x is not verified.

`[#…#]` in a template is a converter token of the Word generator. It is NOT a process-formula or business-rule macro
(`[#DateValue…#]`, `[#Lookup…#]`): different engine, different syntax. Never copy one into the other.

== How a report is generated ==
For one record, the generator (`WordReportGenerator`, resolved as `IReportGenerator` "Word"):
1. Scans the body, headers and footers. Every complex field whose instruction contains `MERGEFIELD` is collapsed into
   one run; field names and body picture names are collected.
2. Pass 1, the record: each name is matched to a `MacrosSettings` entry by `caption` or `metaPathCaption` (both
   trimmed, exact, case-sensitive). All matches go into one query on the object, filtered by the record Id, with the
   generating user's rights. Fields are filled.
3. Pass 2, table parts (only when `SysModuleReportTable` rows exist): each Word table holding a table field is filled
   from its table part, one cloned row per record.
4. Pass 3: a `<TableCaption>.<Column>` field outside a table takes the value of the first record the table part
   query returns, with no sort, so "first" is arbitrary. System setting `DisableFillingTableFieldsOutsideTables`
   (Boolean, not shipped) turns this pass off.
5. The result is `<Caption>.docx`, or PDF (see PDF).

== The field ==
Insert fields with the Creatio MS Word add-in or Word's Insert -> Field -> MergeField. In `word/document.xml` a field
the generator recognises is a complex field whose runs are siblings in one paragraph:
<w:r><w:fldChar w:fldCharType="begin"/></w:r>
<w:r><w:instrText xml:space="preserve"> MERGEFIELD Account \* MERGEFORMAT </w:instrText></w:r>
<w:r><w:fldChar w:fldCharType="separate"/></w:r><w:r><w:t>«Account»</w:t></w:r>
<w:r><w:fldChar w:fldCharType="end"/></w:r>
- MUST write `MERGEFIELD` in upper case and follow the name with a switch (`\* MERGEFORMAT`, or `\b`, `\f`, `\m`,
  `\v`). Without a switch the field is replaced by an empty `w:fldSimple` and prints NOTHING, placeholder text
  included (measured). Lower-case `mergefield` is not recognised: the field stays in the document with its
  placeholder (measured).
- A format switch works only when another switch follows it: `\* Upper \* MERGEFORMAT` upper-cases, a lone
  `\* Upper` is ignored (measured). It uses the current culture. Prefer the `[#Upper#]` macro.
- Word may split the instruction over several `instrText` runs; that is fine. A field SPLIT across containers
  (tracked changes, a hyperlink, a content control) is not paired: accept all changes before editing.
- Nested fields are not supported. Double quotes are stripped from the instruction. A name containing `#` outside a
  token cannot be filled.
- `\b` / `\f` text is inserted as a new paragraph and can make Word report the file as damaged; put such text in the
  template instead.
- A name nobody configured prints an empty field, with no error (measured).

== Names ==
| Field | Binds to |
|---|---|
| `<Caption>` | the printable's column whose `caption` or `metaPathCaption` equals it, exactly and case-sensitively |
| `<TableCaption>.<Caption>` | a column of the table part whose `SysModuleReportTable.Caption` is `<TableCaption>` |
| `<Caption>[#Token#]`, or with an argument (below) | the column, passed through a converter |
| two or more tokens in a row (below) | a chain, applied left to right (measured) |
With an argument: <Caption>[#Token|arg#]. A chain: <Caption>[#A#][#B|x#]. The pipe is written as is.
- One configured column serves the bare field and every macro variant of it.
- A mis-cased caption (`name` for `Name`) prints an empty field (measured).
- No space before `[#`: `Name [#Upper#]` matches nothing and prints empty (measured). When a bare `Name` field comes
  EARLIER in the template, it is worse: the whole document fails with HTTP 400 and an `ArgumentException` "An item
  with the same key has already been added" in `WordReportGenerator.AddColumnToEntitySchemaQuery` (measured).
- A macro needs a configured column. For a value that depends on none (today's date), add any column (for example
  `Id`) to the printable and write its caption before `[#`. Macros work on any column, not only `Id` (measured).
- Tokens are case-sensitive: `[#upper#]` prints empty (measured). An unknown token alone or FIRST in a chain prints
  empty; an unknown token LATER in a chain passes the previous result through unchanged (measured).
- The argument is the second non-empty `|`-separated segment: `[#T|a|b#]` passes `a`, `[#T||x#]` passes `x`, and
  `[#T#]` or `[#T|#]` pass `null` (measured). An argument cannot contain `|`, `(`, `)`, `#` or `]`; it may contain
  spaces. A multi-value argument needs another separator, as `Boolean` uses `,`.

== What a value looks like before a macro sees it ==
| Column | Text passed on |
|---|---|
| Text, Guid, Integer, Boolean | the value as text in the current culture; Boolean is `True` / `False`; null is `""` |
| Date | short date in the current culture (en-US: `1/20/1986`, measured) |
| DateTime, Time | full date-time text in the current culture; format it with the `Date` token and a format argument (below) |
| Decimal | the decimal as text, or with system setting `ReportDecimalSeparator` as the separator |
| Money (two decimals) | rounded to system setting `MoneyFieldDisplayPrecision`, then as Decimal. Money0/1/3 are printed as plain decimals, not rounded |
| Rich text | plain text |
| Lookup | its display value (measured) |
| Binary (Binary, File, Image content) | the bytes. An image LOOKUP column (the usual Image field) gives its display text, not bytes |
Formatting a date: BirthDate[#Date|yyyy-MM-dd#] printed 1986-01-20 (measured).
A `ReportDecimalSeparator` that differs from the culture's separator makes `NumberDigit`, `NumberRU` and any
converter that parses numbers read the value wrong.

== Out-of-the-box tokens ==
| Token | Does | Argument |
|---|---|---|
| `Upper` | upper case (invariant) | `FirstChar`: only the first character (measured) |
| `Lower` | lower case | — |
| `Date` | parses the text as a date and formats it; unparsable -> empty | a .NET format, default `dd.MM.yyyy` (`yyyy-MM-dd` measured) |
| `Boolean` | `CheckBox` -> ☑ / ☐; `Yes,No` -> one of the two | non-boolean text passes through unchanged (measured) |
| `NumberDigit` | groups digits | the group separator, default a space |
| `NumberRU` | the amount in Russian words | `Cent` or `Decimal` |
These six are the only built-in tokens. MUST NOT add a converter before checking them: before writing one, tell
the user which built-in token came closest and why it does not do the job.

== Detail tables ==
- A table part binds to the Word table whose rows hold its `<TableCaption>.<Column>` fields. EVERY row of that table
  holding a field or a picture is cloned once per record, and the originals are removed after the last record. A row
  holding a static logo is cloned too: keep pictures out of field rows.
- One table part per Word table. With two, the second part's fields repeat its arbitrary first record in every row
  the first part cloned (blank when pass 3 is off).
- Sort is the column sort set in the designer; the table part's own filter applies. Table parts are read with the
  generating user's rights.
- No records: with `IsEmptyTableHide` the whole table is removed; otherwise the template row stays with empty fields.

== Images ==
- A picture binds by its name (Word's Selection Pane; `wp:docPr` `name` in the XML) equal to the caption of a
  BINARY column (Image content, or `Data` of a file object). The add-in sets the name when it inserts the picture.
  An image lookup column gives text, so the picture is left as it is.
- Pictures are collected from the body only. Do not reuse a bound picture name in a header or footer: the header is
  processed with the body values and the outcome is not defined.
- The picture is resized to the image's own pixel size and DPI unless system setting `IgnorePictureAspectRatio` is on.
- A bound picture whose value is empty is removed; feature `DisableUnusedFieldsRemoval` keeps the placeholder.
- File `Data` for pictures is read with file rights disabled; the record query itself still applies rights.
Images were read in source only, not measured.

== PDF ==
A synchronous PDF needs a package that binds `IPdfConverter` under the name `PdfConverter`; without one a PDF request
returns the .docx with no error (read in source; no converter package was available to measure). A page button's
`convertInPDF` decides for that click; process generation and the menus use the row's `ConvertInPDF`. Classic UI with
`AsyncPdfReportGeneration` uses a different route, the FileConverter service (system setting `ConvertServiceUrl`),
not `IPdfConverter`.

== Custom converters ==
A new token is a C# class in a package that depends on `CrtNUI`, written only after the built-in tokens were ruled
out and the user told which one came closest (Out-of-the-box tokens, above). The contract is checked by reflection when the
first report uses the token, never at compile time.
namespace Terrasoft.Configuration                 // exactly this namespace, a top-level class
{
	[ExpressionConverterAttribute("UsrAmountInWords")]  // the token; case-sensitive, project-prefixed
	public class UsrAmountInWordsConverter : IExpressionConverter
	{
		public string Evaluate(object value, string arguments = "") {
			string text = value as string ?? string.Empty;   // bytes for a binary column
			string argument = arguments ?? string.Empty;     // null when the template gives no argument
			return Format(text, argument) ?? string.Empty;   // never null
		}
		public static string Format(string text, string argument) => text; // pure logic, unit-tested
	}
}
- Namespace: MUST be `Terrasoft.Configuration`, the class top-level. Any other namespace (including a nested one
  such as `Terrasoft.Configuration.Usr`) fails the WHOLE document with HTTP 400 and a `NullReferenceException` in
  `ExpressionConverterHelper.CreateExpressionConverter` (measured).
- Output: MUST NOT return `null`. A `null` fails the whole document with HTTP 400 and a `NullReferenceException` in
  `WordFieldUtility.FillWordFieldsInElement` (measured). The output is text; a macro cannot produce an image.
- Input: `value` is the text from the value table above, or the previous macro's result in a chain. `arguments` is
  `null` when there is no argument, never `""` (measured).
- Registration: types with the attribute are read once per configuration load, package assemblies first, then the
  main configuration; the FIRST type per token wins and later ones are ignored with no warning. So a package token can
  shadow a built-in (`Date`), and a subclass of a built-in converter inherits its attribute and registers under the
  built-in token unless it declares its own. Prefix the token with the project prefix and search the environment's
  packages for it before claiming it.
- Lifetime: a public parameterless constructor; a new instance per field, per record, per macro; no dependency
  injection. Constructor exceptions surface wrapped in a `TargetInvocationException`.
- A connection: the interface carries none. In a page or API request the session's `UserConnection` is reachable
  through `Terrasoft.Web.Http.Abstractions.HttpContext.Current`; a process run may have no HTTP context, so null-check
  it and fall back to the plain value. Inside `namespace Terrasoft.Configuration` the short name `SysSettings` is the
  entity class: alias the API as `using SysSettings = Terrasoft.Core.Configuration.SysSettings;`.
- Activation: compile the package (`get-guidance name=core-rules`, including its warning before every compile). On
  10.3.1152 .NET Framework a new converter was active in the next report right after the package compile (measured);
  follow the core-rules activation cycle anyway, and if the token still has no effect, check that the compile finished
  and that no other type claimed the token.
- Test it in two layers: `Format` with empty text, `True`, a short date, a lookup's display text, a Guid's text, and
  `argument` both null and set, asserting the result is never null; and the registration by reflection (namespace
  `Terrasoft.Configuration`, not nested, public parameterless constructor, token unique among the package's types).
  Then generate one report that uses the token.

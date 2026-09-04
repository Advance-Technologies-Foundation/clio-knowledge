clio MCP process-send-email guide — the Send email element (EmailTemplateUserTask)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Send email element in custom-message mode.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

== Element: Send email (sendEmail -> EmailTemplateUserTask) ==
- Send email: `sendEmail` (the Send email element / EmailTemplateUserTask), CUSTOM MESSAGE only (email
  TEMPLATES are not supported — say so if the user asks for one). The `email` block configures everything:
  `{ "name": "SendWelcomeEmail", "type": "sendEmail", "caption": "Send the welcome email", "email": {
     "mode": "auto"|"manual", "sender": "<MailboxSyncSettings record id OR a sender email address configured
     on the environment>", "subject": "plain text", "body": "<html>…</html>", "bodyFormat": "html",
     "to"/"cc"/"bcc": [ one of {"value": "a@b.com"} | {"processParameter": "<Name>"} |
       {"expression": "[#…#]", "referenceSchema": "Contact"} , … ],
     "importance": "none"|"normal"|"high"|"low", "ignoreErrors": true|false,
     "performer": { "type": "user"|"manager"|"role", "contact"?: "<formula; defaults to the current user's
       contact>", "role"?: "<SysAdminUnit role name or record id>", "showPage"?: true|false } } }`.
  Rules: `mode:"auto"` sends automatically and its `sender` is required AT RUN TIME, not to save — it is NOT
  a design-time required field: the server saves without one, the designer's card validates `Sender` only
  while auto mode is selected (any filled formula satisfies it), and the field whose absence blocks saving a
  Send email element is `BodyTemplateType`, not `Sender`. With no resolvable sender the RUN fails with
  `Terrasoft.Mail.Sender.EmailException: Sender is not specified` — UNLESS the `SkipSenderValidation` feature
  flag is on, where the identical setup completes. So configure a `sender` for `auto`, but do NOT report a
  missing one as a save-time error. Verified against the platform's own acceptance tests —
  `process_elements_validation.feature` (the element's validation field is `BodyTemplateType`) and
  `exchange_process_send_error_v2.feature` (RND-T26743: auto mode with `Sender` = a `Guid.Empty` formula
  SAVES with no validation dialog and fails only at run time; RND-T26744 `@ft_SkipSenderValidation`: the same
  setup completes) — plus the card's auto-mode-only `senderValidator`.
  AUTO-MODE CHECKLIST: treat `sender`, `to`, `subject` and `body` as MANDATORY and always send all four.
  That is an AUTHORING rule, not a tool contract — neither the build nor the platform save enforces any of
  them (the designer itself saves an auto element with unfilled required fields after a warning dialog —
  designer specimen capture), so every omission surfaces only at RUN time, on a process that
  built green. The `body` omission is the trap that motivates the rule: the build reports success,
  `describe-business-process` reports success too — `hasBody:false` is the ONLY trace, so read the element
  back and check it — and the run fails with `Localizable template not found for record
  00000000-0000-0000-0000-000000000000` (reported on ENG-95979; the mechanism is in the applier source:
  `subject` or `body` writes `BodyTemplateType="1"` — custom message — so a subject-only element points at
  a body template that does not exist). Note the asymmetry: an explicitly EMPTY `body` (`""` or whitespace)
  IS rejected at build (`requires a non-empty 'email.body'`); only an OMITTED one slips through.
  `sender` is resolved at BUILD time and takes exactly three shapes: a `MailboxSyncSettings` record id
  (a GUID), the `[#Lookup.{objectUId}.{recordId}#]` macro that describe echoes back (round-trip), or a
  sender EMAIL ADDRESS resolved against `MailboxSyncSettings.SenderEmailAddress`. ANY other string —
  including a `[#SysSettings.<Code>#]` formula — is treated as an address, matches no mailbox, and FAILS
  the build with `no MailboxSyncSettings record has sender email '<value>'. Configure the mailbox on the
  environment or pass the record id.` (`SendEmailApplier.ApplySender`, CrtProcessBuilder sources, read
  2026-09-01). So the system-setting source the recipient ranking below prefers is NOT usable for `sender`
  through this tool — the designer's own sender menu offers Process parameter / Lookup / System setting /
  Formula, so say "not through this tool yet", never "Creatio cannot". Keep the two sender failures apart:
  a MISSING sender saves and fails the RUN (rules above); a MALFORMED sender fails the BUILD. The sender
  choice that survives an address change is the environment mailbox itself: the address you pass is
  resolved to the mailbox RECORD at build, so an administrator can later change that mailbox's address
  without reopening the process.
  SENDER DISCIPLINE: rank `sender` sources the way the recipient ranking below ranks `to`/`cc`/`bcc`, with
  the mailbox RECORD standing in for the system setting as the indirection an administrator can change later.
  The build resolving a literal to a record is a platform side-effect, not a decision — the decision is
  WHICH record, and it has to be reasoned about out loud, exactly as for a recipient. (1) DISCOVER before
  deciding: read the mailboxes already configured (`odata-read` / `execute-esq` on `MailboxSyncSettings` —
  `Id`, `SenderEmailAddress`, `MailServer`) BEFORE choosing a sender, and when one already carries the
  address or the ROLE the user described ("the address we use for official notifications"), REUSE it by
  record id and say which one. (2) A user-supplied LITERAL sender address that matches no configured mailbox
  is NOT an instruction to create one: name the existing mailboxes as candidates first, and create a new
  `MailboxSyncSettings` record ONLY as the LAST rung, after the user confirms the address is a distinct
  sending identity rather than a synonym of a mailbox that exists. A second record for an address a first
  one already serves is the sender-side twin of a hard-coded recipient — two places to update later.
  (3) STATE the rung you took and why, in the reply — "reusing mailbox X, it already carries this address" or
  "no mailbox carries this address; creating one because the user confirmed it is a separate identity" —
  the same one-line justification the cc/bcc pushback gives. Creating a record is an ENVIRONMENT change:
  the build checks only `SenderEmailAddress`, so a bare record satisfies the build while whether it SENDS
  depends on the mailbox's server configuration, which this tool does not set up — say so when you create
  one. Observed on ENG-95979 (manual test, 2026-09-04): an agent that pushed back on a literal `cc` with a
  stated reason took a literal `sender` straight to a NEW mailbox record, without checking the mailbox it
  had configured minutes earlier — the correct build result, reached without the reasoning this rule asks for.
  `mode:"manual"` creates an email activity for the `performer` (manual-only; `type:"role"` requires `role`).
  A `processParameter` recipient mirrors that parameter's type — a Contact-lookup parameter is resolved to
  the contact's email at send time; an entity-COLUMN recipient is reachable IN THIS CONTRACT only as a raw
  `expression` formula — a CONTRACT limit, not a platform one: the designer's own recipient menu offers
  Contact/Account lookups, the current-user contact, a system setting and a formula (designer specimen
  capture), so say "not through this tool yet", never "Creatio cannot".
  RANK RECIPIENT SOURCES for `to`/`cc`/`bcc`: a SYSTEM SETTING first, then a lookup PROCESS PARAMETER or an
  entity lookup COLUMN (the raw `expression` route above), a CONSTANT address LAST. When the user supplies a
  literal address ("send it to hr@company.com"), ADVISE AGAINST storing it and offer the ranked alternatives
  — a system setting for a team address, a lookup parameter or column for a record-bound one — and take the
  literal only when the user declines them.
  A SYSTEM SETTING is reachable today and is the RIGHT default for an address that belongs to a team rather
  than a person (an HR inbox, a support alias): send the recipient as an `expression` whose formula is
  `[#SysSettings.<Code>#]` — e.g. `[#SysSettings.UsrHrNotificationEmail#]`. Prefer it over a literal address
  and over a named Contact: the setting is what an administrator can change afterwards without reopening the
  process, while a hard-coded address silently keeps mailing the old destination. Discover the code with
  `list-sys-settings` (pass `search-pattern` — the unfiltered catalog is hundreds of rows), and create the
  setting with `create-sys-setting` when it does not exist yet rather than falling back to a literal. When a
  request names a recipient by ROLE rather than by address ("notify HR", "tell support"), treat a system
  setting as the expected answer and offer it explicitly — an option set of literal / contact / parameter
  omits the one source that survives a change of staff. The HTML body is stored verbatim;
  `bodyFormat` accepts ONLY `"html"` — any other value is REJECTED at build even when no `body` is sent (the
  applier validates the format first, so it is a contract guarantee, not a convention). VERIFIED on a stand
  (2026-08-13, a `CrtProcessBuilder` that supports `sendEmail`): `bodyFormat:"text"` and `bodyFormat:"markdown"`
  both FAIL the build with `Send email element '<name>': 'bodyFormat' must be 'html' (only HTML custom-message
  bodies are supported). Got '<value>'.` — and the `markdown` case carried NO `body` at all, which is the half
  that proves the format is checked on its own rather than only alongside a body. To put PROCESS DATA in the
  body, author BY NAME with friendly macros the server resolves into the platform's
  `<img data-value="[#…#]">` image tokens — NO UID needed: `[[param:<Name>]]` (a whole process parameter),
  `[[element:<ElementName>.<OutputParameter>]]` (a whole element output, e.g. a `readData` element's
  `ResultEntity`), and `[[element:<ElementName>.<OutputParameter>.<Column>]]` (ONE direct column of that
  output record). A LOOKUP column in a body macro renders the referenced record's **Id**, not its display
  value: `[[element:Read.ResultEntity.Job]]` mails `11d68189-…`, and an EMPTY lookup mails
  `00000000-0000-0000-0000-000000000000` rather than a blank. This is a PLATFORM limit, not a contract one, and
  it cannot be worked around by drilling deeper — the token is one column deep by construction and BOTH deeper
  routes are refused by core, verified 2026-08-21: a chained `[EntityColumn:{…}].[EntityColumn:{…}]` is read
  only to its LAST segment and resolved against the ROOT schema, and a chained meta path in a `readData`
  element's `EntityColumnMetaPathes` is REJECTED on save (`Column with identifier "<uid>" not found in the
  entity schema "<root>"`). So do NOT put a lookup column in a body a human reads. Email a TEXT column that
  carries the same information instead — on `Employee`, `FullJobTitle` mails `Developer` while `Job` mails a
  GUID — and when only the lookup exists, say the value cannot be rendered rather than shipping an Id.
  Reviewing this needs a SENT message: the schema validates, the process runs green, and no macro is left
  unresolved, so every check short of reading the delivered email passes. A process parameter can only be
  inserted WHOLE — Creatio has NO column drill on a bare
  parameter (verified: zero specimens of `[Parameter].[EntityColumn]` without an `[Element]`; the designer
  offers column drill only on the Elements tab), so to email a record's column read it with a data element
  FIRST and drill THAT output. An unknown parameter/element/column is REJECTED naming what was missing, so
  DISCOVER exact names with `describe-business-process` (or define the parameter) first — do not guess; column
  names are matched case-sensitively. A whole raw `<img data-value="[#…#]">` token (or a bare `[#…#]` formula)
  written by hand passes through unchanged (the escape hatch). NOTE `{{…}}` is NOT clio macro syntax — that is
  the content designer's editable template fields (`{{#index::Title#}}`, New String/Text/Picture/Color), a
  different, design-time feature that is not process data. `importance` has NO `medium` token: the designer LABELS
  `normal` as "Medium" (its caption in the element's card — the product's acceptance tests assert `EN=Medium`),
  so a user's "medium importance" is `normal`. A formula SUBJECT goes through `mappings` against the element's
  `Subject` parameter instead of `email.subject`. Sending BOTH is accepted and does NOT merge — they write the
  same parameter, so the LAST write wins, and which one that is depends on the PATH: in a BUILD the
  descriptor's `mappings` are applied BEFORE the elements' `email` blocks, so `email.subject` overwrites the
  mapped formula whatever order you wrote them in; in a MODIFY the operations run strictly in the order you
  list them, so the LATER of `addMapping` / `setElement`(`email.subject`) wins. Deterministic on each path but
  opposite by default, so send exactly ONE of the two rather than relying on it. This is now a STATED CONTRACT
  rather than an observed implementation order: the server's `email.subject` member documents both paths, and
  two tests pin them — a build asserting the mapping phase runs before the email block, and a modify asserting
  operations dispatch in array order — so reordering either phase is a breaking change that fails the suite
  instead of silently inverting this guide.
  Works in `create-business-process`, `modify-business-process` `addElement` (same block) and `setElement`
  (`elementUpdate.email` — an in-place partial update). Recipients are MATCH-OR-APPEND: an entry whose
  resolved source and value already match an existing line under the same prefix is a NO-OP (re-application
  is idempotent now — older builds appended a duplicate), a genuinely new address APPENDS, and there is NO
  removal path THROUGH THIS TOOL — a wrong recipient cannot be replaced or removed through `modify`.
  The DESIGNER can remove one, so route a removal request there and never say Creatio cannot do it: clearing a
  recipient's value and saving DELETES the parameter (`saveRecipients` calls `removeRecipient` on an emptied
  row, which calls `removeParameter`, which removes it from the element). Two exceptions persist as valueless
  parameters instead — the LAST `To` row (the guard keeps one To row alive), and a parameter something else
  still references (`canRemoveParameter`). That last-`To` case is why a designer capture can show an unfilled
  recipient row surviving; it is a special case, NOT evidence that removal is impossible.
  VERIFIED on a stand (2026-08-13): the SAME `to:[{"value":"…"}]` entry applied three times over `setElement`
  left exactly ONE recipient parameter, and a different address then appended as a second — so "idempotent" is
  measured behaviour here, not an inference from the applier's source. The tool's no-removal half is a
  limitation of the operation set (there is no removeRecipient op), not a platform limit — the designer
  behaviour above is read from `EmailTemplateUserTaskPropertiesPage.js` in `CrtProcessDesigner` 7.8.0
  (`saveRecipients` :645, `removeRecipient` :1410, `removeParameter` :1390).
  `describe-business-process` reads the configuration back as the element's `email` block: `hasBody` is a
  presence flag, and `body` echoes the HTML with the process-macro tokens DECODED back into the same
  `[[param:…]]` / `[[element:…]]` author form — so on a MODIFY you can read the current body and edit it in
  place. A macro whose UIds no longer resolve to names is left as the raw `<img>` token (best-effort decode).

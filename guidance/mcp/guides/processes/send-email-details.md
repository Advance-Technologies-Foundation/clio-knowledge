clio MCP process-send-email-details guide — the evidence and rare cases behind the Send email rules

A details article of the process guide set, reached through `process-send-email`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the evidence and rare cases behind the Send email rules: what an
Add data emulation cannot write, the acceptance tests behind the run-time-only sender, why a no-mode element
runs as a template, the subject-only element, why the mailbox record survives an address change, the
sender-discipline observation, the `bodyFormat` stand run, why a lookup column cannot be drilled deeper, the
source path behind the as-is macro value, the tests that make the subject write order a stated contract, the
designer-removal exceptions, the recipient idempotence run, the source path and stand run behind the
empty-value rule, and the describe decode's fallback. Split out of `process-send-email`, which keeps the
`email` block, sender and recipients with their ranked sources, the auto-mode checklist, the custom body and
its macros, and every rule these facts back; read that first.

- What an Add data cannot write: in manual mode, the activity's binding to this process element with its
  completion listener, so nothing waits for the email; in manual mode, and in auto mode only with
  `CreateActivity` on and a sender, the activity's link to the process instance; in auto mode, the send itself
  (`EmailTemplateUserTask.CreateActivityEntity`, `ManualEmailUserTaskSender`, `AutoEmailUserTaskSender`,
  CrtProcessDesigner 7.8.0). Observed in an agent run (2026-09-24): asked to email each contact of an account
  with no mailbox configured, it built exactly that emulation.
- Verified against the platform's own acceptance tests —
  `process_elements_validation.feature` (the element's validation field is `BodyTemplateType`) and
  `exchange_process_send_error_v2.feature` (RND-T26743: auto mode with `Sender` = a `Guid.Empty` formula
  SAVES with no validation dialog and fails only at run time; RND-T26744 `@ft_SkipSenderValidation`: the same
  setup completes) — plus the card's auto-mode-only `senderValidator`.
- The mechanism of the missing-message trap in `process-send-email`, from the platform
  sources (`CrtProcessDesigner` 7.8.0, read 2026-09-09): the runtime dispatches on `BodyTemplateType`, an
  Integer with no default, so an element whose mode was never written RUNS in TEMPLATE mode with no template,
  and the trap's run-time text is the template provider's (`EmailTemplateUserTaskMessageProvider.GetEmailContent`, the only
  throw site of it in the sources); it was OBSERVED on a no-mode element, and an earlier version of `process-send-email`
  attributed it to "subject or body writes `BodyTemplateType="1"`" (the custom provider), which the sources do
  not support. A subject-only element with neither a mode nor a template still selects the custom mode
  (`BodyTemplateType="1"`) and has no body to send; that run was not made.
- The sender choice that survives an address change is the environment mailbox itself: the address you pass is
  resolved to the mailbox RECORD at build, so an administrator can later change that mailbox's address
  without reopening the process.
- Observed in a manual test (2026-09-04): an agent that pushed back on a literal `cc` with a
  stated reason took a literal `sender` straight to a NEW mailbox record, without checking the mailbox it
  had configured minutes earlier — the correct build result, reached without the reasoning the SENDER
  DISCIPLINE rule of `process-send-email` asks for.
- VERIFIED on a stand
  (2026-08-13, a `CrtProcessBuilder` that supports `sendEmail`): `bodyFormat:"text"` and `bodyFormat:"markdown"`
  both FAIL the build with `Send email element '<name>': 'bodyFormat' must be 'html' (only HTML custom-message
  bodies are supported). Got '<value>'.` — and the `markdown` case carried NO `body` at all, which is the half
  that proves the format is checked on its own rather than only alongside a body.
- The lookup-Id rendering of a body macro in `process-send-email` is a PLATFORM limit, not a contract one, and
  it cannot be worked around by drilling deeper — the token is one column deep by construction and BOTH deeper
  routes are refused by core, verified 2026-08-21: a chained `[EntityColumn:{…}].[EntityColumn:{…}]` is read
  only to its LAST segment and resolved against the ROOT schema, and a chained meta path in a `readData`
  element's `EntityColumnMetaPathes` is REJECTED on save (`Column with identifier "<uid>" not found in the
  entity schema "<root>"`).
  Reviewing this needs a SENT message: the schema validates, the process runs green, and no macro is left
  unresolved, so every check short of reading the delivered email passes.
- The as-is macro value in `process-send-email`, from the platform sources (read 2026-11-25): a custom message
  sends its stored `Body` with `IsHtmlBody = true` (`EmailProcessTemplateUserTaskMessageProvider.GetEmailBody`),
  `ProcessEmailUserTaskMacrosWorker.Proceed` turns each macro into `DataValueType.GetDisplayValue(value)` (the
  `Feature-EmailMacrosParametersEvaluation` path, on by default) or `value.ToString()`, and
  `ProcessEmailUserTaskMacrosHelper.ReplaceMacros` swaps the whole `<img data-value=…>` token for that string with
  `string.Replace`, in auto and manual mode alike (CrtProcessDesigner 7.8.0). In Terrasoft.Core `GetDisplayValue`
  returns `value.ToString()` and only the Date and Time types override it, so no text type encodes its value,
  Rich text included (Creatio 8.3.4, 10.0.0 and the development line). Observed in a manual test on a cloud stand
  (2026-11-23): a Script task joined record URLs with `<br>` into a Text parameter, and the custom body mailed one
  URL per line.
- The subject write order in `process-send-email` is now a STATED CONTRACT
  rather than an observed implementation order: the server's `email.subject` member documents both paths, and
  two tests pin them — a build asserting the mapping phase runs before the email block, and a modify asserting
  operations dispatch in array order — so reordering either phase is a breaking change that fails the suite
  instead of silently inverting `process-send-email`.
- Two exceptions to the designer removal in `process-send-email` persist as valueless
  parameters instead — the LAST `To` row (the guard keeps one To row alive), and a parameter something else
  still references (`canRemoveParameter`). That last-`To` case is why a designer capture can show an unfilled
  recipient row surviving; it is a special case, NOT evidence that removal is impossible.
  VERIFIED on a stand (2026-08-13): the SAME `to:[{"value":"…"}]` entry applied three times over `setElement`
  left exactly ONE recipient parameter, and a different address then appended as a second — so "idempotent" is
  measured behaviour here, not an inference from the applier's source. The tool's no-removal half is a
  limitation of the operation set (there is no removeRecipient op), not a platform limit — the designer
  behaviour in `process-send-email` is read from `EmailTemplateUserTaskPropertiesPage.js` in `CrtProcessDesigner` 7.8.0
  (`saveRecipients` :645, `removeRecipient` :1410, `removeParameter` :1390).
- The empty-value rule in `process-send-email`, from the platform sources (read 2026-11-25):
  `ProcessEmailUserTaskMacrosWorker.Proceed` starts each macro from `GetDefaultMacrosValue`, which is an empty
  string in auto mode and, in manual mode, the token's `alt` text in a yellow inline `<div>`. It keeps that
  default when the value is null or when its display string passes `EqualsDefaultValue<DateTime>`, which an
  empty string does because `DateTimeConverter` reads it as `DateTime.MinValue` (CrtProcessDesigner 7.8.0). A
  token that `create-business-process` writes carries the caption in `[#…#]` as its `alt`
  (`BodyMacroResolver.BuildToken`, CrtProcessBuilder). The literal that template mode leaves
  (`process-send-email-template`) comes from `MacrosHelperV2.GetMacrosValue`, which
  `ProcessEmailUserTaskMacrosHelper.ReplaceMacros`, a plain `string.Replace`, never calls. VERIFIED on a stand
  (Creatio 10.2.522, CrtProcessBuilder 1.6.6.172, 2026-11-25) by reading the stored body of the email activity
  each mode created, auto with `CreateActivity` on: a Formula set a Long text parameter to `""` and a Text
  parameter was never set; auto stored `Links=;` and `Notes=;`, manual stored
  `Links=<div style="background-color: yellow; display: inline;">[#Links#]</div>;` and the same for Notes, and a
  filled parameter was substituted in both. A manual test on a cloud stand (2026-11-23, body built in the
  content designer, send mode not recorded) received `[#Links#]` for an empty parameter, which matches the
  manual-mode output.
- A macro whose UIds no longer resolve to names is left as the raw `<img>` token (best-effort decode).

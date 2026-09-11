clio MCP process-send-email-template guide — the Send email element's TEMPLATE message

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Send email element's TEMPLATE message mode: the `template` and
`templateEntity` contract, the build-time refusals, the object requirement and the choose-or-ask rule, the
`subject` override, mode switching and the template read-back. `process-send-email` owns the element itself —
the `email` block, the auto/manual send mode, sender, recipients, the custom HTML `body` and its macros — and
its AUTO-MODE CHECKLIST applies to a template element unchanged (the `template` stands in for `body`). The
message mode selects ONLY the message: `mode`, `sender`, `to`/`cc`/`bcc`, `importance`, `ignoreErrors` and
`performer` behave identically in both modes and are owned there; `useBackgroundMode` is an element-level
field outside the `email` block. Split out of process-send-email.
Naming anything here? Every element, parameter and process code and caption is governed by N1-N10,
owned by `process-naming` — read it BEFORE you name anything, including when you entered at this
leaf rather than through `process-modeling`.

== Template message (messageSource "template") ==
- TEMPLATE MODE (CrtProcessBuilder 1.6.2.1 or newer): `template` names an existing template by NAME, by `EmailTemplate` record id, or as the
  `[#Lookup…#]` macro describe echoes. The server resolves it at BUILD against the templates of type "Email
  template" (the set the designer's picker offers) and REFUSES, naming the template: an unknown name (`no
  email template is named '<name>'`), an id no row carries, another type (`is not an email template` — a CHAT
  template is one), and an AMBIGUOUS name (`more than one email template is named '<name>'. Pass the record
  id of the one you mean.`). It is stored TOGETHER with the mode (`BodyTemplateType="0"` + `EmailTemplateId`),
  never one without the other — the missing-message trap in `process-send-email`'s AUTO-MODE CHECKLIST is why. `template` and `body`/`bodyFormat` in one block
  are REFUSED (`mutually exclusive`). `messageSource` is optional: omitted, the mode follows the content
  (`template` → template, `body` → custom) or, with neither, stays what the element has; sent, it must agree
  with the content. Macros are resolved by the PLATFORM at send time — process data is NOT injected into the
  template text, and the custom mode's `[[param:…]]` body macros (owned by `process-send-email`) mean nothing here. PERSONALIZATION NEEDS AN
  OBJECT: a template resolves its macros against ONE record of the object it was authored against ("Macro
  source" in Message templates), supplied as `templateEntity` — a Lookup process parameter, a Lookup/Guid
  element output such as a `signalStart` element's `RecordId`, or an expression (a `readData` element's
  `ResultEntity` is the WHOLE record and is REFUSED: address its `Id` column through an `expression`). For an
  object-bound template `templateEntity` is REQUIRED (`is authored against '<Object>', so it needs a 'templateEntity'` — every macro
  would render empty and nothing downstream would say so); for a template WITHOUT an object it is REFUSED
  (`has no macro source object`); a source of another object is refused when both are known. On a stock 10.1
  environment 33 of the 38 templates have NO object, so a template picked by name usually cannot be
  personalized — READ `templateObject` from describe (or the template's Object column) before promising
  personalized text, and offer a custom message with body macros when the fitting template has none.
  CHOOSING A TEMPLATE: resolve it from the lookup (`odata-read` on `EmailTemplate` — `Id`, `Name`, `Object`,
  `TemplateType`); when several could fit, or none is named and nothing obviously matches,
  ASK the user which template to use or offer a custom message — never guess, because the stored value is
  an id and a wrong choice is undetectable afterwards. `subject` in
  template mode is an OVERRIDE — the runtime uses the element's subject when set and the template's own
  otherwise, so OMIT it to send the template's subject. The template's language follows the first Contact-typed recipient
  (`EmailTemplateUserTaskMultiLanguageV2`); nothing to author. SWITCHING MODES through `setElement` clears
  what the other mode owns, so describe stays re-appliable: a `template` (or `messageSource:"template"`) on a
  custom element clears `Body` and a constant `Subject` you do not re-supply (the designer LEAVES the body;
  the server clears it so a described template element never carries a body refused beside a template); a
  `body` (or `messageSource:"custom"`) on a template element clears the template and its macro source, as the
  designer's card does on save. A `subject` sent ALONE never
  changes the mode — a template element keeps its template (the pre-ENG-95986 server flipped it to custom;
  fixed) — except on an element with NO mode yet, where it still selects custom. `templateEntity` alone
  rebinds the macro source of the template the element carries. READ-BACK: describe reports `messageSource`,
  `template` (the id, re-appliable as is), `templateDisplay` (the name), `templateObject` (OMITTED when none — clio drops null fields) and
  `templateEntity` (re-appliable as a `processParameter`/`sourceElement`/`expression`); in template mode
  `hasBody` is false and `body` is omitted even when a stale body is stored. clio WARNS after a build or modify when
  a sent `template` does not read back — a CrtProcessBuilder that predates template mode discarded it while
  answering success: `install-process-builder` and re-apply. RUN-VERIFIED on dev-local in BOTH send modes (manual on 10.1.503 with CrtProcessDesigner 7.8.0, 2026-09-09,
  read off the email activity; auto on 10.2.75 with CrtProcessBuilder 1.6.2.x, 2026-09-10, delivered through
  the Exchange Listener endpoint to an SMTP sink): macros
  resolve against the `templateEntity` record, the `subject` override wins, a template without `EmailTemplateLang`
  rows still renders, and a macro whose VALUE is empty on that record stays LITERAL (`[#Owner.Name#]` on a case
  with no owner), so check the record carries what the template names.

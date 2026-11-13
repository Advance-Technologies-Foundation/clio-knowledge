clio MCP process-send-chat-message guide - the Send chat message element (sendChatMessage)

Scope
This guide owns the `chatMessage` block of the Send chat message element (`SendChatMessageUserTask`, package
OmnichannelMessaging). `process-parameters` owns mappings, `process-formulas` formula syntax, `process-data-elements`
the Read data column a source can name. Email is `process-send-email`.
Needs CrtProcessBuilder <CRTPROCESSBUILDER-CHATMESSAGE-TBD> or later on the environment. Use it only when the FULL
contract (`get-tool-contract` with `tool-names: ["create-business-process"]`, `detail: "full"`) names
`sendChatMessage`; a clio whose contract lacks it refuses the block before sending. The default reply is shortened:
its silence proves nothing. Always read the element back.

== When to use it, and when not ==
The element sends ONE message to ONE contact through a Creatio chat channel (Telegram, WhatsApp, Facebook Messenger,
SMS and the like): a case update, a reminder, an appointment confirmation. It creates a chat in Completed state that
holds the message, so the message shows in the contact's chat history; a reply opens a new chat in the queue.
Not this element:
- a message to employees, or a bot broadcast with no contact: `process-send-email`; a bot API is
  called with `process-web-service`;
- a bulk or campaign send: it is not a campaign sender, so never loop it over a list of contacts as one;
- a contact who never wrote on the channel: the recipient needs an identity on that channel (Twilio SMS aside; == Before
  it can send ==);
- "send, then wait for the answer": the chat is created Completed, and a reply opens a NEW chat; model no wait for
  a reply in the same chat;
- a raw phone number or messenger id: `to` is a Contact;
- attachments: the element sends text only;
- a channel of a custom provider, or Web Chat: these cannot start a chat from a process, so the send fails.

== The block ==
`chatMessage {from, to, messageSource?, template?, message?, ignoreErrors?, columnValues?}`, on
`create-business-process`, `addElement` and `setElement` (`elementUpdate.chatMessage`):
"elements": [{ "name": "NotifyCaseContact", "type": "sendChatMessage", "caption": "Notify the case contact",
  "chatMessage": { "from": { "expression": "[#SysSettings.UsrSupportChatChannel#]" },
    "to": { "sourceElement": "ReadCase", "sourceElementParameter": "ResultEntity", "sourceColumn": "Contact" },
    "messageSource": "template", "template": "Case status update", "ignoreErrors": true,
    "columnValues": [ { "column": "Case", "processParameter": "CaseIdParameter" } ] } }]
"chatMessage": { "from": { "processParameter": "ChatChannelParameter" },
  "to": { "processParameter": "ContactIdParameter" }, "messageSource": "custom",
  "message": "Your appointment is confirmed.", "ignoreErrors": false }
- `from` (the channel) and `to` (the contact) each take exactly ONE source: `value`, `processParameter`,
  `sourceElement` + `sourceElementParameter` (+ `sourceColumn`, one column of a first-record read), or `expression`.
- `messageSource`: `template` or `custom`. Optional when the content implies it; one that disagrees with the
  content is refused, and so are `template` and `message` together.
- `columnValues`: column values of the CREATED CHAT (== Column values on the chat ==).
- The block on any other type is refused. Without the OmnichannelMessaging package the type is refused, naming it.
- The generic `userTask` route REFUSES `SendChatMessageUserTask` on create: build `sendChatMessage`. Describe of an
  existing element is not affected.
- An element still missing its channel, its contact or (template mode) its template saves, with a notice, and its
  send fails (== Ignore errors and the outputs ==): settle the missing part WITH the user.

== From: which channel ==
DISCOVER first: read the active channels (`odata-read` on `Channel`: `Id`, `Name`, `IsActive`) and look for an
existing Lookup setting on Channel (`list-sys-settings` with a `search-pattern`). When several channels could fit,
ASK which one; never pick one by guess. Then rank the sources the way `process-send-email` ranks a RECIPIENT, and
state in the reply which rung you took and why:
1. Prefer a Lookup SYSTEM SETTING that references the ROOT Channel object: an `expression` `[#SysSettings.<Code>#]`.
   An administrator can then move the process to another channel without reopening it. It is stored in the typed
   form the designer writes, `[#SysSettings.<Code><Guid>#]`, where `<Guid>` is literal text (the value type), as in
   `[#SysSettings.UsrSupportChatChannel<Guid>#]`; describe echoes that. The build checks the setting:
   no setting with that code, not a Lookup, or a Lookup of another object: REFUSED, naming the code. A Lookup of a
   replacing Channel schema: built with a notice (the designer lists only settings of the root object and will not
   show it; recreate it on the root object). No value yet: built with a notice; the run fails until it has one.
   Create a setting only after the user confirms (an environment change), then SET ITS VALUE.
2. Then a Lookup process parameter (Channel), or the channel column of a record the process read.
3. A constant channel LAST: `value` takes a channel id, its name or the `[#Lookup…#]` macro describe echoes; an
   unknown one is refused, an ambiguous name too (pass the id). An INACTIVE channel is built with a notice: the
   designer offers active channels only, and the run fails until the channel is activated.

== To: which contact ==
- ONE contact; an array is refused. `value` is a contact id or its `[#Lookup…#]` macro, never a name: read the
  contact first and pass its id.
- Rank: the contact of the record being processed - a Read data column (`sourceColumn` `Contact` of a case, lead or
  activity) or a Lookup process parameter - first. A system setting (the same build check, against Contact) only for
  a team contact. A fixed contact LAST: advise against it and take it only when the user insists.
- A constant contact with no identity on a constant channel other than SMS is built with a notice: the run cannot
  resolve the recipient (== Before it can send ==).

== The message: template or custom ==
TEMPLATE (`messageSource` `template`):
- `template` names a CHAT template - a message template of type "Chat template" (`TemplateType`
  `62215de1-23e0-4037-88c9-65ccf72904a4`) - by name, record id or the `[#Lookup…#]` macro describe echoes. REFUSED,
  naming it: an unknown name or id, another type (an email template), no type, and an ambiguous name (pass the id).
- CHOOSE OR ASK: read the chat templates (`odata-read` on `EmailTemplate`, filtered on `TemplateType` =
  `62215de1-23e0-4037-88c9-65ccf72904a4`: `Id`, `Name`). When one
  plainly fits, use it and say which. When you cannot tell which fits, ASK the user to name the template, or offer a
  custom message instead; never guess: the stored value is an id, and a wrong pick is invisible afterwards.
- No system setting selects the template. The template's language is chosen at run time: the contact's language,
  then the channel's, then the `DefaultMessageLanguage` setting, then the default language.
- Template macros resolve against the CREATED CHAT, not the process's record: they reach its contact and channel, and
  a case or lead only when `columnValues` links the chat to it.
- PROVIDER templates (WhatsApp approved templates) are not supported by this tool yet: `template` accepts only a chat
  template, and a provider template is refused. When the message must reach WhatsApp outside the 24-hour window,
  build the process here and let the user pick the provider template in the Process Designer. Describe reports such
  a stored template as its raw id; `setElement` keeps it unless `template` is sent.
- The designer copies the template text into the card's message field; this tool does not (the run ignores it in
  template mode), so the card's read-only preview is expected to stay empty until the template is picked again
  there.
CUSTOM (`messageSource` `custom`):
- `message` is plain text, sent as written; HTML is stripped. Nothing in a constant is resolved: a `[#...#]` macro
  or a `[[...]]` token in `message` is REFUSED. Personalise with a FORMULA on the element's `Message` parameter
  through a mapping, as the email subject does: `{ "elementName": "NotifyCaseContact", "elementParameter":
  "Message", "expression": "<formula>" }`; the parameter reference form is owned by
  `process-formulas`. A raw newline inside a formula fails validation. On create, send `messageSource: "custom"`
  without `message` and the formula mapping in the same request; the empty-message notice is cleared once the
  mapping lands - read the element back. On modify, a later `addMapping` onto `Message` replaces a constant, so to
  move a template element to a formula: `setElement` with `messageSource: "custom"` and a short `message`, then the
  `addMapping`, in that order in one request.
- An empty `message` is refused. Line breaks in a constant are kept, with a notice: the designer's message field is
  one line, so expect the card to show the text on one line and editing there to lose the breaks. A character XML
  cannot store (a control character other than tab, line feed and carriage return, U+FFFE, U+FFFF, a lone
  surrogate) is refused, naming its code point.
- The text is stored in the current culture; describe reads it in the profile's culture.

== Ignore errors and the outputs ==
- `ignoreErrors` omitted on `create-business-process` and `addElement` is FALSE: a failed send STOPS the process,
  and the element log carries the error. Ask the user whether a failed send should stop the process, and send
  `ignoreErrors` explicitly either way.
- With `ignoreErrors: true` the process goes on. `SendStatus` is `"Sent"` or `"Error"`; `SendStatusDetails` is empty
  on success, else the error text. Map them downstream (`targetProcessParameter` from `sourceElement` +
  `sourceElementParameter`) and branch on `SendStatus` (`process-branch-conditions`).
- It also swallows INPUT errors - an empty channel, contact, template or message, an inactive channel - so a
  completed run is not a delivered message: read `SendStatus`.
- The two outputs have no direction, so the generic `parameters` of describe omit them while unmapped; the block
  lists them under `outputs`.

== Column values on the chat ==
`columnValues` sets columns of the chat the element creates (object OmniChat), in the Modify data entry shape
(`column` + `value` | `processParameter` | `sourceElement` + `sourceElementParameter` (+ `sourceColumn`) |
`expression`; `process-data-elements`). Use it to link the chat to the record it is about (`Case`, `Lead`, where the
environment has the column), which also lets template macros read that record.
- An unknown column and `Id` are refused, and so is `selectionColumn`. A column the element fills itself (`Status`,
  `Channel`, `Contact`, `Operator`, `Queue`, `ChatPreview`) is accepted with a notice: your value overwrites it.
- On `setElement` an omitted list is kept, a sent list REPLACES the stored set, and `[]` clears it.

== Before it can send ==
None of this is checked at build (beyond the notices above); each fails the run, or with `ignoreErrors` yields
`SendStatus` `"Error"`:
- An ACTIVE channel that can deliver. A `Channel` row alone is not a working channel: it needs its messenger settings
  (the provider's credentials); an SMS channel row without them fails the send.
- A recipient on THAT channel: the contact needs a contact identity there, which exists once the contact has written
  on the channel. An SMS channel of the Twilio provider is the exception: the recipient is taken from the contact's
  PRIMARY "Mobile phone" communication (not the `MobilePhone` column) and an identity is saved on the first send.
- WhatsApp and Facebook Messenger: the provider's 24-hour window. A message is refused when the contact never chatted
  on the channel, when the window has expired, and when another operator holds an open chat with the contact.
  Outside the window only a provider template goes through, and that is designer-only (above).
- An empty system setting behind `from` or `to` fails the run.

== When a send fails ==
Recognise the class; never key on the exact text, which can change between builds:
- a required input empty: "... parameter is required ...";
- the channel: `Channel with Id=<id> not found.` or `Channel with Id=<id> is not active.`;
- the WhatsApp or Messenger gate: no conversation, window expired, an active chat of another operator;
- the template: "Failed to process template ..." or empty text;
- the recipient: "Failed to get recipient for contact ... ContactIdentity record may not exist.";
- the provider: its own text (an SMS channel without messenger settings answered `Username can not be null`).
On current builds a failed send leaves no chat (measured on 10.2.1122); older OmnichannelMessaging builds may leave
an empty Completed chat.

== Changing it (modify) ==
`setElement` with `elementName` and `elementUpdate.chatMessage` is a PARTIAL update: omitted members keep their
stored values.
- Template to custom: send `message` in the SAME call, or the switch is refused - even when the element shows text:
  a designer-built element holds the template's text with its macros, which would be sent literally. The switch
  clears the template.
- Custom to template: a stored constant message is cleared.
- A raw `addMapping` onto `Template`, `MessageType`, `RecordColumnValues` or `IgnoreErrors`, or of a CONSTANT onto
  `Channel` or `Contact`, is refused, naming the block member to use. Allowed: a formula onto `Message`; a process
  parameter, element output or formula onto `Channel` or `Contact`; mappings FROM `SendStatus` and
  `SendStatusDetails`.
- describe also lists this element's `MessageType` and `Contact` under `connections[]`. They are not "Connected to"
  links: ignore them and NEVER replay them - `setConnections` refuses this task and the edit aborts. Edit through
  `chatMessage`.

== Read back ==
`describe-business-process` (`element=<name>` or `mode=full`) returns the element's `chatMessage` block: `from` and
`to` in the source forms above, `messageSource`, `template` (the `[#Lookup…#]` macro) with the describe-only
`templateName`, `message` (custom mode only), `ignoreErrors` (always stated, `false` included: an untouched designer
checkbox reads false), `columnValues`, and the describe-only `outputs` (`SendStatus`, `SendStatusDetails`) and
`issues` (no channel, no contact, template mode with no template - an unset mode runs as template mode - and custom
mode with an empty message). To resubmit, drop `templateName`, `outputs` and `issues` and send the rest unchanged;
when `template` is a raw provider-template id, drop `template` too (`setElement` keeps it; sent, it is refused).

Evidence: the build-time rules are the CrtProcessBuilder <CRTPROCESSBUILDER-CHATMESSAGE-TBD> contract. Measured on
Creatio 10.2.1122 (.NET Framework) with Omnichannel Messaging 1.0.63 on 2026-11-13, on designer-built elements: an
untouched `ignoreErrors` failing the process on an inactive channel; `ignoreErrors` on giving `"Error"` and the
channel text in mapped outputs; a send on an SMS channel without messenger settings resolving the recipient from the
primary mobile, then failing (`Username can not be null`, placed at the provider send by inference) with no chat
left; the designer storing a typed system setting, lookup
macros, a one-line message field and an empty template after a switch to custom. Template macros against the chat,
column values on a delivered chat, the identity and 24-hour gates, HTML stripping, `ignoreErrors` covering empty
inputs, the not-found channel text, an empty system setting failing the run and the template language order are
read from the OmnichannelMessaging and Creatio core source and the product documentation, not measured. Inferred,
not yet measured: an unset mode running as template mode, how the card shows a multi-line text or a server-built
template element, and a run of the server's own `ignoreErrors` false write.

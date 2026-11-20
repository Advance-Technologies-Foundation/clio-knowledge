clio MCP process-send-chat-message guide - the Send chat message element (sendChatMessage)

Scope
This guide owns the `chatMessage` block of the Send chat message element (`SendChatMessageUserTask`, package
OmnichannelMessaging). `process-parameters` owns mappings, `process-formulas` formula syntax, `process-data-elements`
the Read data column a source can name.
Needs CrtProcessBuilder 1.6.6.186 or later on the environment. The default create contract lists
`chatMessage (sendChatMessage)`; a clio without it refuses the block before sending. Always read the element back.

== When to use it, and when not ==
The element sends ONE message to ONE contact through a Creatio chat channel (Telegram, WhatsApp, Messenger,
SMS and the like). It creates a Completed chat holding the message, shown in the contact's chat history.
Not this element:
- a message to employees: `process-send-email`; a bot broadcast with no contact (a bot API): `process-web-service`;
- a bulk send - a marketing broadcast to an audience: that is a campaign, not this element. A per-record
  notification is this element (a reminder per appointment: a multi-instance sub-process over the records,
  `process-sub-process`), and each send still needs the identity and the 24-hour window;
- "send, then wait for the answer": the chat is created Completed, and a reply opens a NEW chat; model no wait;
- a raw phone number or messenger id (`to` is a Contact); attachments (text only);
- a channel of a custom provider, or Web Chat: these cannot start a chat from a process.
SEND NOW ("send this to <contact> now"): clio sends only through a process; say so in one line. Do the discovery
first, then ask ONE question: build a reusable process whose `to` is a Lookup (Contact) input parameter and run it
once for this contact (`run-process` with `parameters`). The process is the deliverable, not scratch to delete. Ask
for the package only when `get-target-package` fails. With `ignoreErrors` true a failed send ends green: return
`SendStatus` and `SendStatusDetails` in output parameters (`result-parameters`) before saying "sent", or keep
`ignoreErrors` false.

== The block ==
`chatMessage {from, to, messageSource?, template?, message?, ignoreErrors?, columnValues?}`, on
`create-business-process`, `addElement` and `setElement` (`elementUpdate.chatMessage`):
"elements": [{ "name": "NotifyCaseContact", "type": "sendChatMessage", "caption": "Notify the case contact",
  "chatMessage": { "from": { "expression": "[#SysSettings.UsrSupportChatChannel#]" },
    "to": { "sourceElement": "ReadCase", "sourceElementParameter": "ResultEntity", "sourceColumn": "Contact" },
    "messageSource": "template", "template": "Case status update", "ignoreErrors": false,
    "columnValues": [ { "column": "Case", "processParameter": "CaseIdParameter" } ] } }]
"chatMessage": { "from": { "processParameter": "ChatChannelParameter" },
  "to": { "processParameter": "ContactIdParameter" }, "messageSource": "custom",
  "message": "Your appointment is confirmed.", "ignoreErrors": false }
- `from` (the channel) and `to` (the contact) each take exactly ONE source: `value`, `processParameter`,
  `sourceElement` + `sourceElementParameter` (+ `sourceColumn`, one column of a first-record read), or `expression`.
- `messageSource`: `template` or `custom`; optional when the content implies it. One that disagrees with the content
  is refused, and so are `template` and `message` together.
- The block on any other type is refused; without the OmnichannelMessaging package the type is refused, naming it.
- The generic `userTask` route REFUSES `SendChatMessageUserTask` on create: build `sendChatMessage`.
- An element missing its channel, its contact or (template mode) its template saves with a notice, and its send
  fails: settle the missing part WITH the user.

== Discovery reads ==
Shape: `entity`, never `entity-set`; `filters` is an OBJECT
(`{all|any: [{field, op, value}]}`), never a string or a raw `filter`; filter a lookup on its path `X/Id`, never
`XId` or `X`; `select` takes columns, never a path. Each call is
clio-run { command: "odata-read", args: { environment-name: "<env>", <one line below> } }:
- active channels: entity: "Channel", select: ["Id", "Name", "IsActive"], filters: { all: [ { field: "IsActive", op: "eq", value: true } ] }
- chat templates: entity: "EmailTemplate", select: ["Id", "Name"], filters: { all: [ { field: "TemplateType/Id", op: "eq", value: "62215de1-23e0-4037-88c9-65ccf72904a4" } ] }
- a contact by name: entity: "Contact", select: ["Id", "Name"], filters: { all: [ { field: "Name", op: "contains", value: "<name>" } ] }
Lookup settings on the ROOT Channel object (schema UId `a26e0c98-...`; odata-read cannot filter a GUID, and
`list-sys-settings` takes no filter):
clio-run { command: "execute-esq", args: { environment-name: "<env>", query: { rootSchemaName: "SysSettings", columns: { items: { Code: { expression: { expressionType: 0, columnPath: "Code" } }, Name: { expression: { expressionType: 0, columnPath: "Name" } } } }, filters: { filterType: 6, items: { r: { filterType: 1, comparisonType: 3, leftExpression: { expressionType: 0, columnPath: "ReferenceSchemaUId" }, rightExpression: { expressionType: 2, parameter: { dataValueType: 0, value: "a26e0c98-9a52-4e80-9a04-75221742fed7" } } } } } } } }
A Lookup setting's value is a record id: read it with `get-sys-setting` (`code`); its TextValue is always empty;
only an empty or all-zero id means unset.

== From: which channel ==
DISCOVER first (the reads above). When several channels could fit, ASK which one; never guess. Rank the sources
and say which rung you took and why:
1. Prefer a Lookup SYSTEM SETTING that references the ROOT Channel object: an `expression` `[#SysSettings.<Code>#]`.
   It is stored in the designer's typed form `[#SysSettings.<Code><Guid>#]` (`<Guid>` is literal text, the value type); describe echoes that. The build checks the setting:
   no setting with that code, not a Lookup, or a Lookup of another object: REFUSED, naming the code. A Lookup of a
   replacing Channel schema: built with a notice (the designer will not list it; recreate it on the root object).
   No value: built with a notice, and a run reaching the element stops the whole PROCESS: ignoreErrors does not
   catch it, and a condition on the macro throws too. An all-zero id fails the element instead (with ignoreErrors:
   SendStatus "Error"). Guard it as `process-formulas` says (system setting row),
   branching on GuidValue != Guid.Empty.
   Create a setting only after the user confirms (an environment change), then SET ITS VALUE.
2. Then a Lookup process parameter (Channel), or the channel column of a record the process read.
3. A constant channel LAST: `value` takes a channel id, its name or its `[#Lookup…#]` macro; an unknown or ambiguous
   one is refused (pass the id). An INACTIVE channel is built with a notice: the designer offers active channels only, and the run fails until the channel is activated.

== To: which contact ==
- ONE contact; an array is refused. `value` is a contact id or its `[#Lookup…#]` macro, never a name: read the
  contact's id first.
- Rank: the contact of the record being processed - a Read data column (`sourceColumn` `Contact` of a case, lead or
  activity) or a Lookup process parameter - first. A system setting (the same build check, against Contact) only for
  a team contact. A fixed contact LAST: advise against it and take it only when the user insists.
- A constant contact with no identity on a constant non-SMS channel is built with a notice: the run cannot resolve
  the recipient.

== The message: template or custom ==
TEMPLATE (`messageSource` `template`):
- `template` names a CHAT template by name, record id or the `[#Lookup…#]` macro describe echoes. REFUSED, naming it: an unknown name or id, another type (an email template),
  no type, and an ambiguous name (pass the id).
- CHOOSE OR ASK: read the chat templates. Use one only when the user names it exactly, or names none and only one
  chat template exists (say which). Otherwise ASK which one, or offer custom text, and build or save NOTHING until
  answered; never pick the closest name.
- No system setting selects the template. The template's language is chosen at run time: the contact's language,
  then the channel's, then the `DefaultMessageLanguage` setting, then the default language.
- Template macros resolve against the CREATED CHAT, not the process's record: they reach its contact and channel, and
  a case or lead only when `columnValues` links the chat to it.
- PROVIDER templates (WhatsApp approved templates) are not supported by this tool yet: a provider template is
  refused; build the process here and let the user pick the provider template in the Process Designer.
- The card's message preview stays empty for a template set here; the run does not use it.
CUSTOM (`messageSource` `custom`):
- `message` is plain text, sent as written; HTML is stripped. Nothing in a constant is resolved: a `[#...#]` macro
  or a `[[keyword:...]]` token in `message` is REFUSED; other bracket text is sent literally. Personalise with a FORMULA on the element's `Message` parameter through a mapping:
  `{ "elementName": "NotifyCaseContact", "elementParameter": "Message", "expression": "<formula>" }`; the parameter
  reference form is owned by `process-formulas`. A raw newline inside a formula fails validation. On create, send
  `messageSource: "custom"` without `message` plus the mapping in one request. To move a template element to a formula: `setElement` with `messageSource: "custom"` and a short
  `message`, then the `addMapping`, in that order in one request.
- An empty `message` is refused. Line breaks in a constant are kept, with a notice: the designer's message field is
  one line, and editing there loses the breaks. A character XML cannot store (a control
  character other than tab, LF or CR, U+FFFE, U+FFFF, or a lone surrogate) is refused, naming its code point.

== Ignore errors and the outputs ==
- `ignoreErrors` omitted on `create-business-process` and `addElement` is FALSE: a failed send STOPS the process,
  and the element log carries the error. Do not ask: on a new element send `ignoreErrors: true` only when the
  request says to go on after a failed send, else `false`; state which; on `setElement` omit it unless asked.
- With `ignoreErrors: true` the process goes on. `SendStatus` is `"Sent"` or `"Error"`; `SendStatusDetails` is empty
  on success, else the error text. Map them downstream and branch on `SendStatus`.
- It also swallows INPUT errors (an empty channel, contact, template or message; an inactive channel), NOT a system setting with no value in from/to (the process stops, SendStatus unset). `"Sent"` is
  "handed to the connector", NOT delivered: a provider rejection after that (a Telegram user who blocked the bot)
  still reads `"Sent"`. Say "sent", never "delivered".
- describe lists both outputs in the generic `parameters` (`isOutput: true`) and under the block's `outputs`.

== Column values on the chat ==
`columnValues` sets columns of the created chat (object OmniChat) in the Modify data entry shape (`column` + one
source; `process-data-elements`). Link the chat to its record (Case, Lead, where the column exists); template
macros can then read that record.
- `value` is for TEXT columns only: a record id there is refused, and the refusal quotes the macro to send.
  A Lookup column (`Case`, `Lead`, `Queue`) takes `expression: "[#Lookup.<object name or schema UId>.<record Id>#]"`,
  as in `[#Lookup.ChatQueue.<record Id>#]`. The server stores the designer's form, the object's schema UId, and
  says so in a "stored as ..." warning: expected, not a failure. A record known only at run time comes from
  `processParameter` or `sourceElement`.
- An unknown column and `Id` are refused, and so is `selectionColumn`. A column the element fills itself (Status,
  Channel, Contact, Operator, Queue, ChatPreview) is accepted with a notice: your value overwrites it.
- An EMPTY Lookup source at run time fails the send (a foreign-key error) and LEAVES the created chat, empty. Branch
  on its source (!= Guid.Empty): with it into the element, else into a copy without it.
- On `setElement` an omitted list is kept, a sent list REPLACES the stored set, and `[]` clears it.

== Before it can send ==
Not checked at build; each fails the run, or with `ignoreErrors` yields `SendStatus` `"Error"`:
- An ACTIVE channel. A `Channel` row alone is not a working channel: it needs its messenger settings
  (the provider's credentials).
- A recipient on THAT channel: a contact identity, which exists once the contact has written there; say so if they may
  not have. Twilio SMS is the exception: the recipient is taken from the contact's PRIMARY "Mobile phone" communication (not the `MobilePhone` column), and an identity is saved on the first send.
- WhatsApp and Facebook Messenger: the provider's 24-hour window. Refused when the contact never chatted on the
  channel, the window has expired, or another operator holds an open chat. With custom text or a chat template there,
  TELL the user: it reaches the contact only within 24 hours of their last message; after that, only a provider
  template picked in the designer.

== When a send fails ==
Recognise the class; never key on the exact text, which can change between builds:
- a required input empty ("... parameter is required ..."); the channel (`Channel with Id=<id> is not active.`, or
  not found); the WhatsApp or Messenger gate; the template ("Failed to process template
  ..."); the recipient ("Failed to get recipient for contact ..."); the provider's own text (`Username can not be
  null` on SMS).
On current builds a failed send leaves no chat (measured on 10.2.1122); older OmnichannelMessaging builds may leave an empty Completed chat.

== Changing it (modify) ==
`setElement` with `elementName` and `elementUpdate.chatMessage` is a PARTIAL update: omitted members keep their
stored values.
- Template to custom: send `message` in the SAME call, or the switch is refused - even when the element shows text:
  a designer-built element holds the template's text with its macros, which would be sent literally. The exception:
  `Message` already bound to a formula or another non-constant source (an `addMapping` in an earlier call). The
  switch clears the template. Custom to template needs `template` in the same call, or it is refused; it
  clears the message text from any source and names a dropped binding; before 1.6.6.191
  only a constant is cleared: read back.
- A switch that clears `Template` or `Message` while another element reads it is refused: re-point or remove those
  readers first, in a SEPARATE, earlier call.
- A raw `addMapping` onto `Template`, `MessageType`, `RecordColumnValues` or `IgnoreErrors`, or of a CONSTANT onto
  `Channel`, `Contact` or `Message` (use `chatMessage.message`), is refused, naming the block member to use. Allowed:
  a formula, parameter or element output onto `Message`; a parameter, element output or formula onto `Channel` or `Contact`; mappings FROM
  `SendStatus` and `SendStatusDetails`.
- describe lists this element's `MessageType` and `Contact` under `connections[]`. They are not "Connected to"
  links: ignore them and NEVER replay them - `setConnections` refuses this task and the edit aborts. Edit through `chatMessage`.

== Read back ==
`describe-business-process` (`element=<name>` or `mode=full`) returns the `chatMessage` block: `from` and `to` in the
source forms above - a constant reads back as `{value: <record id>, display: <name>}`, and the describe-only `display` names the channel and the contact
- `messageSource`, `template` (the `[#Lookup…#]` macro) with the
describe-only `templateName`, `message` (custom mode only), `ignoreErrors` (`false` included: an untouched designer checkbox reads false; `null` when mapped: do not re-send it), `columnValues`, and the describe-only
`outputs` and `issues` (no channel, no contact, template mode with no template - an unset mode runs as template
mode - or custom mode with an empty message). To resubmit, drop `templateName`, `outputs` and `issues` and send the rest unchanged (`display` is tolerated); when `template` is a raw provider-template id, drop `template` too: on `setElement` the same id sent back is a no-op and any other is refused, and on create it is refused.

Evidence: the build-time rules are the CrtProcessBuilder 1.6.6.191 contract. Measured on Creatio 10.2.1122, Omnichannel Messaging 1.0.63 (2026-11-13/16):
ignoreErrors off stopping the run and on giving "Error"; an SMS send
resolving the primary mobile; the designer's stored forms; column values on the chat; "Sent" for a blocked bot. QA 2026-11-18/20
(OmnichannelMessaging 7.8.0, 1.6.6.186): template macros against the chat, the empty preview, the empty-setting stop (a zero id caught)
and its guard, an empty Lookup parameter in from giving "Error", the empty lookup column failure and guard, no chat after a
missing recipient, the designer dropping a bound text on a template pick. From source, not measured: other empty inputs giving "Error", the identity
and 24-hour gates, HTML stripping, the language order. Inferred: an unset mode running as template mode.

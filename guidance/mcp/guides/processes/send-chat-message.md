clio MCP process-send-chat-message guide - the Send chat message element (sendChatMessage)

Scope
This guide owns the `chatMessage` block of the Send chat message element (`SendChatMessageUserTask`, package
OmnichannelMessaging). `process-parameters` owns mappings, `process-formulas` formula syntax, `process-data-elements`
the Read data column a source can name. Email is `process-send-email`.
Needs CrtProcessBuilder 1.6.6.176 or later on the environment. The default create contract lists
`chatMessage (sendChatMessage)`; a clio without it refuses the block before sending. Always read the element back.

== When to use it, and when not ==
The element sends ONE message to ONE contact through a Creatio chat channel (Telegram, WhatsApp, Facebook Messenger,
SMS and the like): a case update, a reminder, an appointment confirmation. It creates a Completed chat holding the
message, so it shows in the contact's chat history; a reply opens a new chat in the queue.
Not this element:
- a message to employees, or a bot broadcast with no contact: `process-send-email`; a bot API: `process-web-service`;
- a bulk send - a marketing broadcast to an audience: that is a campaign, not this element. A per-record
  notification is this element (a reminder per appointment: a multi-instance sub-process over the records,
  `process-sub-process`), and each send still needs the identity and the 24-hour window;
- a contact who never wrote on the channel: the recipient needs an identity there (Twilio SMS aside);
- "send, then wait for the answer": the chat is created Completed, and a reply opens a NEW chat; model no wait;
- a raw phone number or messenger id (`to` is a Contact); attachments (text only);
- a channel of a custom provider, or Web Chat: these cannot start a chat from a process, so the send fails.
SEND NOW ("send this to <contact> now"): clio sends only through a process; say so in one line. Do the discovery
first, then ask ONE question: build a reusable process whose `to` is a Lookup (Contact) input parameter and run it
once for this contact (`run-process` with `parameters`). The process is the deliverable, not scratch to delete. Ask
for the package only when `get-target-package` fails.

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
- `messageSource`: `template` or `custom`; optional when the content implies it. One that disagrees with the content
  is refused, and so are `template` and `message` together.
- The block on any other type is refused; without the OmnichannelMessaging package the type is refused, naming it.
- The generic `userTask` route REFUSES `SendChatMessageUserTask` on create: build `sendChatMessage`.
- An element missing its channel, its contact or (template mode) its template saves with a notice, and its send
  fails: settle the missing part WITH the user.

== Discovery reads ==
Run `odata-read` through `clio-run`. Shape: `entity`, never `entity-set`; `filters` is an OBJECT
(`{all|any: [{field, op, value}]}`), never a string or a raw `filter`; filter a lookup on its path `X/Id`, never
`XId` or `X`; `select` takes columns, never a path. Each call is
clio-run { command: "odata-read", args: { environment-name: "<env>", <one line below> } }:
- active channels: entity: "Channel", select: ["Id", "Name", "IsActive"], filters: { all: [ { field: "IsActive", op: "eq", value: true } ] }
- chat templates: entity: "EmailTemplate", select: ["Id", "Name"], filters: { all: [ { field: "TemplateType/Id", op: "eq", value: "62215de1-23e0-4037-88c9-65ccf72904a4" } ] }
- a contact by name: entity: "Contact", select: ["Id", "Name"], filters: { all: [ { field: "Name", op: "contains", value: "<name>" } ] }

== From: which channel ==
DISCOVER first: the active channels, and an existing Lookup setting on Channel (`list-sys-settings` with a
`search-pattern`). When several channels could fit, ASK which one; never guess. Then rank the sources the way
`process-send-email` ranks a RECIPIENT, and say which rung you took and why:
1. Prefer a Lookup SYSTEM SETTING that references the ROOT Channel object: an `expression` `[#SysSettings.<Code>#]`.
   An administrator can then move the process to another channel without reopening it. It is stored in the typed
   form the designer writes, `[#SysSettings.<Code><Guid>#]`, where `<Guid>` is literal text (the value type), as in
   `[#SysSettings.UsrSupportChatChannel<Guid>#]`; describe echoes that. The build checks the setting:
   no setting with that code, not a Lookup, or a Lookup of another object: REFUSED, naming the code. A Lookup of a
   replacing Channel schema: built with a notice (the designer will not list it; recreate it on the root object).
   No value yet: built with a notice; the run fails until it has one.
   Create a setting only after the user confirms (an environment change), then SET ITS VALUE.
2. Then a Lookup process parameter (Channel), or the channel column of a record the process read.
3. A constant channel LAST: `value` takes a channel id, its name or its `[#Lookup…#]` macro; an unknown or ambiguous
   one is refused (pass the id). An INACTIVE channel is built with a notice: the designer offers active channels only, and the run fails until the channel is activated.

== To: which contact ==
- ONE contact; an array is refused. `value` is a contact id or its `[#Lookup…#]` macro, never a name: read the
  contact first and pass its id.
- Rank: the contact of the record being processed - a Read data column (`sourceColumn` `Contact` of a case, lead or
  activity) or a Lookup process parameter - first. A system setting (the same build check, against Contact) only for
  a team contact. A fixed contact LAST: advise against it and take it only when the user insists.
- A constant contact with no identity on a constant non-SMS channel is built with a notice: the run cannot resolve
  the recipient.

== The message: template or custom ==
TEMPLATE (`messageSource` `template`):
- `template` names a CHAT template by name, record id or the `[#Lookup…#]` macro describe echoes. REFUSED, naming it: an unknown name or id, another type (an email template),
  no type, and an ambiguous name (pass the id).
- CHOOSE OR ASK: read the chat templates. When one plainly fits, use it and say which. When you cannot tell which
  fits, ASK the user to name the template, or offer a custom message instead; never guess: the stored value is an id,
  and a wrong pick is invisible afterwards.
- No system setting selects the template. The template's language is chosen at run time: the contact's language,
  then the channel's, then the `DefaultMessageLanguage` setting, then the default language.
- Template macros resolve against the CREATED CHAT, not the process's record: they reach its contact and channel, and
  a case or lead only when `columnValues` links the chat to it.
- PROVIDER templates (WhatsApp approved templates) are not supported by this tool yet: a provider template is
  refused. To reach WhatsApp outside the 24-hour window, build the process here and let the user pick the provider template in the Process Designer.
- The card's read-only message preview stays empty for a template set here; the run does not use it.
CUSTOM (`messageSource` `custom`):
- `message` is plain text, sent as written; HTML is stripped. Nothing in a constant is resolved: a `[#...#]` macro
  or a `[[keyword:...]]` token in `message` is REFUSED; other bracket text is sent literally. Personalise with a FORMULA on the element's `Message` parameter through a mapping:
  `{ "elementName": "NotifyCaseContact", "elementParameter": "Message", "expression": "<formula>" }`; the parameter
  reference form is owned by `process-formulas`. A raw newline inside a formula fails validation. On create, send
  `messageSource: "custom"` without `message` plus the mapping in one request. To move a template element to a formula: `setElement` with `messageSource: "custom"` and a short
  `message`, then the `addMapping`, in that order in one request.
- An empty `message` is refused. Line breaks in a constant are kept, with a notice: the designer's message field is
  one line, so the card shows one line and editing there loses the breaks. A character XML cannot store (a control
  character other than tab, LF and CR, U+FFFE, U+FFFF, a lone surrogate) is refused, naming its code point.
- The text is stored in the current culture; describe reads it in the profile's culture.

== Ignore errors and the outputs ==
- `ignoreErrors` omitted on `create-business-process` and `addElement` is FALSE: a failed send STOPS the process,
  and the element log carries the error. Ask the user whether a failed send should stop the process, and send
  `ignoreErrors` explicitly either way.
- With `ignoreErrors: true` the process goes on. `SendStatus` is `"Sent"` or `"Error"`; `SendStatusDetails` is empty
  on success, else the error text. Map them downstream and branch on `SendStatus` (`process-branch-conditions`).
- It also swallows INPUT errors (an empty channel, contact, template or message; an inactive channel), so a
  completed run is not a delivered message: read `SendStatus`.
- describe's generic `parameters` omit the two outputs while unmapped; the block lists them under `outputs`.

== Column values on the chat ==
`columnValues` sets columns of the created chat (object OmniChat) in the Modify data entry shape (`column` + one
source; `process-data-elements`). Link the chat to its record (`Case`, `Lead`, where the column exists); template
macros can then read that record.
- `value` is for TEXT columns only. A Lookup column (`Case`, `Lead`, `Queue`) takes `expression: "[#Lookup.<referenced object schema UId>.<record Id>#]"`: the object's schema UId, never its name.
  From CrtProcessBuilder 1.6.6.176 a name there is rewritten to the UId with a notice; an older package saves it
  green, and the designer card then shows `<entity not found>`. A record known only at run time comes from
  `processParameter` or `sourceElement`.
- No clio discovery tool returns a schema UId: `find-entity-schema` and `get-entity-schema-properties` give names
  only. Read it with `execute-esq` on `SysSchema`: `UId` where `Name` is the referenced object, `ManagerName` is
  `EntitySchemaManager` and `ExtendParent` is false (the root schema, not a replacement).
- An unknown column and `Id` are refused, and so is `selectionColumn`. A column the element fills itself (`Status`,
  `Channel`, `Contact`, `Operator`, `Queue`, `ChatPreview`) is accepted with a notice: your value overwrites it.
- On `setElement` an omitted list is kept, a sent list REPLACES the stored set, and `[]` clears it.

== Before it can send ==
Not checked at build; each fails the run, or with `ignoreErrors` yields `SendStatus` `"Error"`:
- An ACTIVE channel that can deliver. A `Channel` row alone is not a working channel: it needs its messenger settings
  (the provider's credentials).
- A recipient on THAT channel: a contact identity, which exists once the contact has written there. Twilio SMS is the
  exception: the recipient is taken from the contact's PRIMARY "Mobile phone" communication (not the `MobilePhone` column), and an identity is saved on the first send.
- WhatsApp and Facebook Messenger: the provider's 24-hour window. Refused when the contact never chatted on the
  channel, the window has expired, or another operator holds an open chat; outside it only a provider template goes
  through (designer-only).
- A value behind a system setting used by `from` or `to`.

== When a send fails ==
Recognise the class; never key on the exact text, which can change between builds:
- a required input empty ("... parameter is required ..."); the channel (`Channel with Id=<id> not found.`,
  `Channel with Id=<id> is not active.`); the WhatsApp or Messenger gate; the template ("Failed to process template
  ..."); the recipient ("Failed to get recipient for contact ..."); the provider's own text (`Username can not be
  null` from an SMS channel without messenger settings).
On current builds a failed send leaves no chat (measured on 10.2.1122); older OmnichannelMessaging builds may leave an empty Completed chat.

== Changing it (modify) ==
`setElement` with `elementName` and `elementUpdate.chatMessage` is a PARTIAL update: omitted members keep their
stored values.
- Template to custom: send `message` in the SAME call, or the switch is refused - even when the element shows text:
  a designer-built element holds the template's text with its macros, which would be sent literally. The switch
  clears the template; custom to template clears a stored constant message.
- A switch that clears `Template` or `Message` while another element reads it is refused: re-point or remove those
  readers first, in a SEPARATE, earlier call.
- A raw `addMapping` onto `Template`, `MessageType`, `RecordColumnValues` or `IgnoreErrors`, or of a CONSTANT onto
  `Channel`, `Contact` or `Message` (use `chatMessage.message`), is refused, naming the block member to use. Allowed:
  a formula onto `Message`; a parameter, element output or formula onto `Channel` or `Contact`; mappings FROM
  `SendStatus` and `SendStatusDetails`.
- describe lists this element's `MessageType` and `Contact` under `connections[]`. They are not "Connected to"
  links: ignore them and NEVER replay them - `setConnections` refuses this task and the edit aborts. Edit through `chatMessage`.

== Read back ==
`describe-business-process` (`element=<name>` or `mode=full`) returns the `chatMessage` block: `from` and `to` in the
source forms above - a constant reads back as `{value: <record id>, display: <name>}`, and the describe-only `display` names the channel and the contact
when you explain what the process sends and to whom - `messageSource`, `template` (the `[#Lookup…#]` macro) with the
describe-only `templateName`, `message` (custom mode only), `ignoreErrors` (always stated, `false` included: an untouched designer checkbox reads false), `columnValues`, and the describe-only
`outputs` and `issues` (no channel, no contact, template mode with no template - an unset mode runs as template
mode - or custom mode with an empty message). To resubmit, drop `templateName`, `outputs` and `issues` and send the rest unchanged (`display` is tolerated); when `template` is a raw provider-template id, drop `template` too: on `setElement` the same id sent back is a no-op and any other is refused, and on create it is refused.

Evidence: the build-time rules are the CrtProcessBuilder 1.6.6.176 contract. Measured on Creatio 10.2.1122 with Omnichannel Messaging 1.0.63 on 2026-11-13, on designer-built elements: an untouched `ignoreErrors`
failing the process; `ignoreErrors` on giving `"Error"` and the channel text in mapped outputs; an SMS send without
messenger settings resolving the primary mobile, then failing (`Username can not be null`) with no chat left; the
designer's typed system setting, lookup macros, one-line message field and empty template after a switch to custom.
On 2026-11-13/14: the discovery reads above as written, the `SysSchema` UId read (without `ExtendParent` false it
also returns a replacement's row), and a name-form Lookup macro in `columnValues` showing `<entity not found>`. Read from the OmnichannelMessaging
and core source and the product documentation, not measured: template macros against the chat, column values on a
delivered chat, the identity and 24-hour gates, HTML stripping, `ignoreErrors` covering empty inputs, the not-found
channel text, an empty system setting failing the run, the template language order. Inferred: an unset mode running
as template mode, the card's display of multi-line text or a server-built template, a run of a server-written
`ignoreErrors` false.

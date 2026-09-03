clio MCP process-approval guide — the Approval element (ApprovalUserTask)

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the Approval element and its `approval` block.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.

== Element: Approval (approval -> ApprovalUserTask) ==
- Approval: `approval` (the Approval element / ApprovalUserTask), which requests a visa on a record. It has
  its own dedicated build type and must NOT be built as a generic `userTask`. The `approval` block
  configures it:
  `{ "name": "ApproveDiscount", "type": "approval", "caption": "Discount approval", "approval": {
     "object": "<EntityName>", "recordId": { one of {"recordId": "<GUID or the [#Lookup…#] macro describe
       reports>"} | {"processParameter": "<Name>"} | {"sourceElement": "<Element>", "sourceElementParameter":
       "<Output>"} },
     "purpose"?: "text shown to the approver",
     "approver": { "type": "user"|"manager"|"role",
       "employee"?: "<user name, id, or [#SysVariable.CurrentUser#]>",  // user / manager only
       "role"?: "<role name or id>" },                                  // role only
     "allowDelegation"?: true|false,
     "notifyApprover"?: { "emailTemplate": "<template name or id>" },
     "notifyAuthor"?: { "emailTemplate": "<template name or id>",
       "recipient": { one of {"value": "a@b.com"} | {"processParameter": "<Name>"} } },
     "ignoreEmailErrors"?: true|false } }`.
  Name the element and its caption by the N1-N10 rules in `process-naming`; bind the record from another
  element's output only when that element appears EARLIER in `elements[]`.

== Rules ==
- `object` + `recordId` + `approver` are required on a FIRST configuration; the runtime cannot raise a visa
  without a record, and an approval nobody is assigned to cannot be acted on. A fixed `recordId` must be a
  record OF `object` — a foreign id is REFUSED, because the designer renders that field blank and the next
  human save wipes the element.
- `approver` sets WHO approves. `user` is a named user, `role` is a group (every user of the role sees the
  visa), and `manager` approves through the chain — for `manager`, `employee` names the person WHOSE
  manager approves, not the manager. The user or role is CHECKED TO EXIST against the same set the
  designer's own picker offers, and an ambiguous NAME is refused so you pass the id rather than silently
  get whichever row came back first. Omitting `approver` on a first configuration is REFUSED, because making
  whoever ran the build the approver routes real approvals to somebody nobody chose; on a modify, omitting
  it keeps the approver the element already has.
- Switching only the TYPE is a supported edit and does not need the person restated. `user` and `manager`
  share one stored field, so `{"approver":{"type":"manager"}}` against an element approved by Anna means
  "Anna's manager approves instead" and KEEPS Anna — the same thing the designer does across that switch.
  An omitted `employee` takes the designer's one-click default, the current user, only on an element that
  names nobody yet. Switching TO `role` clears the person, because a role approves and a leftover employee
  would be a value nothing reads; switching away from `role` clears the role for the same reason. Formula
  and system-setting sources are not offered.
- Supplying `notifyApprover` / `notifyAuthor` switches that notification ON; the flag and its template are
  written together because the runtime gates the send on the flag. EITHER is REFUSED without an
  `emailTemplate`, and `notifyAuthor` is ALSO refused without a `recipient` — unless the element already
  carries one, so `{}` is how you switch a notification back on using what it already has. Both refusals
  guard the same silent failure: the runtime checks neither before sending and ignores email errors by
  default, so either gap gives you an element that reports the notification as configured, saves, compiles,
  runs green and never sends. The template is also CHECKED TO EXIST, for that same reason — an id nobody can
  resolve fails inside the send and is swallowed — so a placeholder id, `00000000-0000-0000-0000-000000000000`
  included, is refused rather than stored. Pass a real template name or id, or leave the notification out. There is NO way to switch one off through the block — a cleared template is
  indistinguishable from one never set; use `addMapping` against the flag parameter instead (see
  `process-parameters`).
- **"Author" is a misnomer, and it is the field people get wrong.** The runtime does NOT resolve who authored
  the process or the record. `GetAuthorEmailAddresses` reads exactly one thing — the address `recipient`
  writes — and the send is gated on that list being non-empty. So "notify the person who created the order"
  is NOT something the element can work out on its own: give it an address, or a process parameter carrying
  one. That is why the recipient is required rather than optional.
- The `recipient` is a TEXT address: a constant (`{"value": "a@b.com"}`) or a process parameter carrying
  one. Several addresses in one string work — the runtime splits on `;` and `,`. What is NOT available here
  is what the designer additionally offers: a Contact or Account LOOKUP, which is how a human usually wires
  "notify the record's owner". That source is refused on type grounds, so do not spend turns trying to bind
  `Owner` — resolve the address into a text process parameter earlier in the process, or ask for one. A
  macro (`[#…#]`) in `value` is refused too: the field is stored as a plain constant nothing resolves, so it
  would be mailed with the brackets intact and the failure swallowed.
- `purpose` omitted writes the platform default "Approval required" — that is what the designer persists
  too, so an omitted purpose is not an empty one. On a MODIFY it is a partial update like the rest of the
  block: an element that already carries a purpose keeps it, so flipping some other field cannot silently
  replace the text the approver reads. `ignoreEmailErrors` is already `true` by platform default.
- **Do not set an optional field the request did not mention.** Three things go wrong when you do, and the
  first is the one that bites:
  * **It destroys the "not decided" signal.** `describe-business-process` reports what is WRITTEN, so
    absence means "nobody set this", never "off". Once you write `allowDelegation: false`, no later reader —
    human or agent — can tell a deliberate choice from a guess. Note what this bullet does NOT claim: on this
    element only `ignoreEmailErrors` carries a schema-level default, so an omitted `allowDelegation` is
    simply unset and the runtime reads it as false — the same behaviour writing `false` produces. The cost
    is the lost signal, not a changed outcome. (A DESIGNER-saved element does carry `true`, because the
    human's save writes it; a builder-made one does not, so the two are not interchangeable.)
  * **It puts words in the requester's mouth.** `purpose` is read by the person approving. The default is
    neutral; an invented sentence is wording the requester never approved.

  Set an unrequested field only when the request implies it unambiguously ("nobody else may sign it off" →
  `allowDelegation: false`). For the fields that genuinely cannot be left out — `object`, `recordId`,
  `approver`, and each notification's template and recipient — the server refuses rather than defaulting, so
  you will be told; do not pre-empt it by inventing a value.
- **When a REQUIRED field is a business decision the request did not make, ASK — do not carry one over.**
  `approver` is the case that matters: it is required precisely because it decides who receives real
  approvals, which is why nothing defaults it. A request that says what to approve but not who signs it off
  is incomplete, and the honest response is a question. Reusing the approver from another process you built
  earlier in the same session looks like context and is a guess: nobody chose it for THIS process, and the
  result is an approval routed to a team that was never asked. The same applies to the notification
  recipient — an address is a person, not a formatting detail.
- The visa schema, its master column and the section are DERIVED from `object` server-side (with the
  platform's `SysApproval` fallback when the object has no approval settings) and are never caller input.
- **The outcome cannot be branched on.** Approved / rejected / canceled arrives in the element's
  `ResultParameter` output, but routing it needs a gateway and conditional flows, which are not buildable —
  see "What you can build today" in `process-modeling`. So `approval` gives you a configured approval STEP,
  not an approval FLOW. Say so when you build one, and note the outcome set is three values, not two.

== Modifying an existing Approval element ==
- `modify-business-process` → `setElement` with an `approval` block reconfigures it IN PLACE; only the
  fields you pass change. Omit `object` to keep the current approval object, omit `recordId` to keep the
  current record, omit `approver` to keep the current approver.
- Retargeting `object` while the element still carries a record id bound to the PREVIOUS object is REFUSED —
  pass `recordId` in the same call.
- The block must set at least one field: an `approval` block that sets nothing is refused rather than
  reported as a successful no-op.

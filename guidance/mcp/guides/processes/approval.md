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
       "recipient"?: { one of {"value": "a@b.com"} | {"processParameter": "<Name>"} } },
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
  get whichever row came back first. An omitted `employee` takes the designer's one-click default, the
  current user; the block itself is NOT defaulted, though — omitting `approver` on a first configuration is
  refused, because making whoever ran the build the approver routes real approvals to somebody nobody
  chose. On a modify, omitting it keeps the approver the element already has. Supplying it REPLACES the
  approver whole — the field belonging to the type you switched AWAY from is cleared, exactly as the
  designer does, so a switch leaves no contradictory leftover. Formula and system-setting sources are not
  offered.
- Supplying `notifyApprover` / `notifyAuthor` switches that notification ON; the flag and its template are
  written together because the runtime gates the send on the flag. EITHER is REFUSED without an
  `emailTemplate` unless the element already carries one — so `{}` is how you switch a notification back on
  using the template it already has. The reason: the runtime does NOT check for a template before sending,
  and it ignores email errors by default, so a flag with no template gives you an element that reports the
  notification as configured, saves, compiles, runs green and silently never sends. There is NO way to
  switch one off through the block — a cleared template is indistinguishable from one never set; use
  `addMapping` against the flag parameter instead (see `process-parameters`).
- `purpose` omitted writes the platform default "Approval required" — that is what the designer persists
  too, so an omitted purpose is not an empty one. `ignoreEmailErrors` is already `true` by platform default.
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

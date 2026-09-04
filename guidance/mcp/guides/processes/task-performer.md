clio MCP process-task-performer guide — who performs a Perform task

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of "Who performs the task?" for a Perform task element: the two
layers it can be answered on, the element-level `performer` block that is the ONLY way to assign work to
a TEAM, the OwnerId parameter for one named person, which sources each layer accepts and which it
refuses, and what happens when neither layer is set. `process-perform-task` owns the element itself, its
parameter table and what the runtime writes back.
Split out of `process-perform-task` because that article had no response-budget headroom left, and
because "who does this" is a decision of its own -- it is asked in the request ("assign it to the sales
department") rather than discovered while filling in a parameter table.

== "Who performs the task?" has TWO layers ==
Picking the right layer is the whole game.
LAYER 1 — the element-level `performer` block (ships from CrtProcessBuilder 1.3.1.1, the same version
story as the ActivityCategory row in `process-perform-task`). Set it on the performTask element in
create/addElement, or in place via setElement's
`elementUpdate.performer`: { "type": "user"|"manager"|"role", "contact"?, "role"?, "showPage"? }.
* type "role" is THE way to assign to a TEAM: pass a role name or record id in `role`. The created
  Activity carries the role in its own OwnerRole column and its Owner stays EMPTY — the claim model:
  every user of the role sees the task, whoever takes and completes it is recorded. Do not read the
  empty Owner back as "unassigned". The role is CHECKED TO EXIST on either route, against the same role
  set the designer's picker offers — so a typo'd name, an invented Guid and a USER's own SysAdminUnit id
  are refused instead of stored (a user is not a role; for one person use type "user"), and so is a name
  that matches MORE THAN ONE role - a name cannot say which group performs the task, so pass the id. Look
  the role up on the environment rather than guessing an id.
* type "manager" resolves the contact's MANAGER at RUN time (default contact = the process starter); when
  the contact's employee record has no manager the process raises an error at run time — say so when the
  org data may be incomplete.
* type "user" with `contact` is the single-person form: pass a bare Contact record Guid (checked to exist,
  and stored as the encoding the designer produces) or a formula like [#SysVariable.CurrentUserContact#];
  an omitted contact defaults to the process starter.
* `showPage` omitted defaults to false for manager/role (designer parity — a role activity has no single
  performer to open the page for) and stays untouched for user.
* describe reads the block back top-level on the element (`performer`: type + the stored formula +
  roleDisplay) and it is re-appliable verbatim. This ELEMENT-LEVEL block is REFUSED on any element other
  than performTask — the retired CallUserTask by name (its runtime IGNORES the assignment). A sendEmail
  element has its own `email.performer`, which is a different field and is not refused.
LAYER 2 — the OwnerId parameter (Lookup -> Contact), for a SPECIFIC PERSON only. Four working ways:
* a bare Contact record Guid in `value` — the Guid must be an EXISTING Contact record: an id of another
  entity (a ROLE id is the classic mistake) is REFUSED naming the reference object, because before this
  guard it persisted as a well-formed ConstValue referencing nothing at run time;
* a process parameter: create it with `typeFromElement` + `typeFromElementParameter: "OwnerId"` so the types
  are guaranteed compatible, then map it in;
* another element's Contact/Guid output parameter;
* `expression: "[#SysVariable.CurrentUserContact#]"` for "whoever started the process".
A Lookup -> SysAdminUnit PARAMETER source is likewise REJECTED (incompatible reference object). NEVER route
a team through OwnerId: it is REFUSED, and the `performer` block's type "role" is what does it.
Leaving both layers unset is NOT an unassigned task — at run time the task silently falls to the current
user's contact (whoever started the process). There is no "nobody" state; omitting the performer is a choice.

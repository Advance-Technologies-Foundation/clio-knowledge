clio MCP default-record-rights guide

Owns: the "Use record permissions" switch of an object and its DEFAULT record rules — what they mean, the
set-default-record-rights rules, and the rule that applying them to existing records is the user's decision
(apply-default-record-rights).

Version boundary: set-default-record-rights and apply-default-record-rights require clio <SET-DEFAULT-RECORD-RIGHTS-CLIO-VERSION-TBD>
or later; from the same version get-object-rights also reports the record layer. On an older clio none of this
exists: use the "Use record permissions" page of the Object permissions designer.

## Which layer
| The requirement | Layer | Owner |
|---|---|---|
| Who may read/create/edit/delete ANY record of the object | object operation permissions | `get-guidance name=object-rights` |
| Which rights a NEW record gets, by who created it ("records created by role X are visible to role Y"); turning record permissions on or off | default record rules (this article) | `default-record-rights` |
| Who may access ONE particular record | stored grants on that record | `get-guidance name=record-rights` |
| Access computed at query time from the user or the record | runtime rules | `get-guidance name=record-permission-extensions` |
| A process changes a record's grants | Change access rights element | `get-guidance name=process-access-rights` |

For choosing between these, `get-guidance name=record-permissions` is the entry point. Operation permissions
decide first: a user without read on the object reaches no record, whatever the record rights say.

## How the platform decides — read this before any write
- The switch. While "Use record permissions" is OFF, record rights are not evaluated: every user with read
  operation rights on the object reaches every record. While it is ON, a record is reachable only through
  its record rights (plus its author/owner, their managers, and holders of "view any data").
- A default rule says: records created by members of the AUTHOR role get read / edit / delete rights for the
  GRANTEE role. Each operation has a level: not set, granted, or delegated (granted with the right to
  delegate it). "Do not apply for manager" stops the grantee role's managers from inheriting the rule. Rules
  have no order: the rights of several rules add up. A rule is identified by its author + grantee pair.
- ON with NO rule: every user sees only the records they create (and their managers and holders of "view
  any data" see them too). That is the built-in default, not an error.
- Rules apply to a NEW record when it is inserted. Turning the switch on or changing a rule does NOT touch
  existing records: after a first enable an existing record has no record rights at all, so only its
  author/owner, their managers and holders of "view any data" reach it — until the rules are applied to it
  (apply-default-record-rights, see below).
- Turning the switch OFF keeps the rules and every record's rights; they come back into effect when it is
  turned on again. A record created while it is OFF gets no record rights.
- Portal / external users see a record only through a right: a rule whose author or grantee is
  `All external users` (`720b771c-e7a7-4f31-9cfb-52cd21c3739f`) or a portal role. OOTB objects carry such
  rules (Account: `All external users` → `All employees`).
- The platform replaces the whole rule list on every save and checks nothing: a rule with no right is
  dropped, two rules for one author + grantee pair keep only the last one. set-default-record-rights
  therefore changes one rule per call, saves every other rule exactly as read, and refuses a stored list
  with duplicate pairs or invalid levels.

Evidence: Creatio Academy ("Record permissions"); reproduced on Creatio 10.2 stands (10.2.367 on 2026-10-02,
10.2.370 on 2026-10-04): the switch, the rules on insert, the full-list save, and the update process that
replaces default-origin rights and keeps manual grants.

## get-object-rights (the record part)
Per object, after the operation rows: `Record permissions: ON|OFF`, then every default rule — author → grantee,
the three levels, "do not apply for manager", with both SysAdminUnit ids. While the switch is OFF the rules
are listed as stored, not in effect. ON with no rule says the built-in default applies. grantee and author
filter the rules. For the named object it reports the number of existing records, counted under the calling
account (an account without "view any data" counts only what it can see).

## set-default-record-rights
Args: entity-schema-name (required); author + grantee + operations (a rule change, all three together);
level (granted|delegated, default granted); do-not-apply-for-manager (true|false); revoke;
enable-record-permissions; disable-record-permissions; preview.
- A call is EITHER a rule change (author, grantee and operations — read,edit,delete — together; nothing is
  granted by default) OR a switch-only change (none of them, plus enable-record-permissions or
  disable-record-permissions). Anything else is refused before any read. author and grantee are SysAdminUnit
  ids and must exist; names are NOT unique — resolve the id yourself and ask when several match.
- You MUST ask the developer in chat before every write, as for set-object-rights: call it with preview=true
  first, show the planned change, get the yes, then make the same call without preview. On the CLI: --preview
  first, then --confirm.
- Before an enable you MUST tell the developer what the object becomes: with no rule, every user sees only
  the records they create; with stored rules, each of them comes into effect (the preview names them);
  existing records get no rights until applied.
- A grant sets each named operation to level and leaves the other two as read; it can lower delegated to
  granted, and the result shows every level before → after. do-not-apply-for-manager, when given, sets the
  rule's flag; omitted, an existing rule keeps its flag and a new rule gets false.
- A revoke sets the named operations to "not set". A rule left with no right is REMOVED, and the result says
  so. A revoke is allowed while the switch is OFF: it is how a stale stored rule is cleaned up BEFORE an
  enable brings it into effect, and it changes nobody's access now.
- Refused, writing nothing: a grant on an object whose switch is OFF without enable-record-permissions;
  disable-record-permissions together with a grant; both switch flags; a stored list with two rules for one
  author + grantee pair, or with a level that is not not set / granted / delegated, when the call would save
  the list — repair those in the designer.
- disable-record-permissions opens every record to every user with read operation rights. You MUST NOT pass
  it unless that widening is the developer's intent.
- Results: the object is read back and compared with the plan — the switch and EVERY rule. Any difference,
  including a rule the call did not name, fails the call ("saved, but NOT verified"): re-read with
  get-object-rights. A save that got no answer may still be applied: re-read before retrying. A repeated
  call reports no change. It does NOT change operation or column permissions, and it NEVER applies the rules
  to existing records.

## Apply the rules to existing records — the user's decision
- You MUST NOT run apply-default-record-rights on your own initiative. After any enable or rule change, tell
  the user the number of existing records (in the result), that those records keep their current rights
  (none after a first enable) until the rules are applied, and that the update is heavy on large tables
  (minutes or more; run it at low load) — then ASK whether and when to run it. Run it only on a yes.
- Make all the rule changes first, then run it once: each call starts a full run.
- It deletes the rights that came from default rules (including rules deleted since) and applies the current
  rules to every existing record; rights granted by hand with set-record-rights stay.
- It is refused while the switch is OFF. With wait (the default) it reports completed, failed, or still
  running with the process id. A run still going is NOT a failure: do NOT start it again — check the
  SysProcessLog row with that Id later.

## Verify the effect
- Re-read with get-object-rights; check one record with get-record-rights (its rows show where each right
  came from).
- To prove what a user can do, test AS that user (not an administrator, not a holder of "view any data").
  A DataService read that is denied returns success with ZERO rows, not an error.

Scope boundary: the record-permissions switch and the default record rules of one object, and applying them
to its records. NOT operation permissions, NOT column permissions, NOT one record's grants, NOT putting the
switch or the rules into a package (they are environment settings).

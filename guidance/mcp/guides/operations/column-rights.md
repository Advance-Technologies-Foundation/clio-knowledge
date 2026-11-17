clio MCP column-rights guide

Owns: COLUMN (field-level) permissions of an object — who may see or edit ONE column, per role — the "Use
column permissions" switch, the row priority rule of a column, the get-column-rights / set-column-rights
rules for a grant, a move, a removal and the switch, and the fact that these rows are NOT bound to a
package.

Version boundary: get-column-rights and set-column-rights require a clio release newer than 8.1.0.155;
clio 8.1.0.155 and earlier have neither tool. How to tell: get-column-rights and set-column-rights are
missing from get-tool-contract. Then stop: tell the developer that clio needs updating for this, and that
the manual way is the "Column permissions" tab of the Object permissions designer. You MUST NOT write
column rights any other way (see "Never by hand").

Manage column permissions with two tools:
- get-column-rights — read the switch and, per column, the stored rows in priority order. Read-only.
- set-column-rights — set ONE role's access (denied, read or edit) to ONE column of ONE object, move or
  remove that role's row in the column, or turn the object's column permissions on or off. DESTRUCTIVE:
  it writes in one call, with no publish step.

Who may read/create/edit/delete ANY record of the object is `get-guidance name=object-rights`; who reaches
ONE record is `get-guidance name=record-rights`; the "Use record permissions" switch and default record
rules are `get-guidance name=default-record-rights`.

## How the platform decides — read this before any write
- Column permissions apply only while the object's "Use column permissions" switch is ON. While it is
  OFF, stored rows are kept but not applied: every column follows the object's operation and record
  permissions alone.
- Each column has its own rows; a row gives one role denied, read or edit. The rows of a column are a
  PRIORITY LIST: the higher the row, the higher its priority, and a user in several roles gets the
  access of the HIGHEST of their rows. Position 0 is the highest.
- A user or role with NO row in a column follows the object's operation permissions. A column with no
  rows is NOT locked: everyone follows the object's operation permissions there.
- Object operation and record permissions outrank column permissions: an edit row does not let a user
  edit a record the object or record permissions do not let them edit.
- Every internal user is in All employees (`a29a3ba5-4b0d-de11-9a51-005056c00008`). External / portal
  users are NOT, so an All employees row does not restrict them: a portal audience follows the object's
  operation permissions in that column unless `All external users`
  (`720b771c-e7a7-4f31-9cfb-52cd21c3739f`) or its portal role has a row of its own.
- The default row is All employees: edit. Creatio writes it to a column it is not told otherwise about
  (see "Never by hand"); it restricts no member of All employees. get-column-rights lists such columns
  apart.

Evidence. Documented, not measured: the priority rule, "no row follows operation permissions" and the
precedence of object and record permissions — Creatio Academy "Column permissions" (the 8.0 and 8.3 pages
agree). Measured on Creatio storage (the view VwSysEntitySchemaColumnRight; a 10.1.0 configuration,
2026-11-17): how a save writes rows, the default row, and what the service read hides — the tools are
built on these measurements. Who actually reaches a column was not measured: verify it as below.

## Never by hand
You MUST NOT write column rights with execute-esq, DataService or a hand-built call to
RightManagementService. A save there REPLACES the rows of EVERY column of the object: each column it
lists gets exactly the rows sent, and each column it leaves out gets the default row, erasing that
column's rows. The service read also FILTERS (it hides default-only columns while the switch is ON and
every column while it is OFF), so a list built from it loses rows. set-column-rights builds the full list
from storage, checks it against the service, and reads the rows back.

## Before any write
- entity-schema-name is the object's SCHEMA name (its code); a developer's word for an object can be its
  title instead (owner: `get-guidance name=object-rights`). You MUST name the object by schema name AND
  title in the preview you show, and when the developer's word is not exactly that object's title, you
  MUST ask which object is meant before reading or writing. set-column-rights refuses a title, naming
  the code. column is the column CODE.
- grantee is a SysAdminUnit id; names are NOT unique. When several roles or users match a name, you MUST
  NOT pick one: show each candidate's id, name and type and ask.
- You MUST ask the developer in chat before every write. On MCP the host's approval of the call is not the
  developer's yes, and an auto-approve mode skips it. So you MUST first call it with preview=true: it
  writes nothing and shows the column's rows before and after, every warning, and why the call would be
  refused. Show those facts, get the yes, then make the same call without preview. On the CLI: --preview
  first, ask, then --confirm.
- Calls for one object MUST NOT run in parallel: a write holds a lease on the object (shared with
  set-object-rights and set-default-record-rights), and a second write of it is refused while the lease
  is held.

## get-column-rights
Args: entity-schema-name (required); column, grantee (optional).
- Per object one of: `Column permissions: OFF` (stored rows listed, not applied); `Column permissions: ON`
  with the columns that have rows other than the default, then the default-only columns, then the columns
  with no rows; or `Column permissions: unknown` when the service did not return the switch — never take
  that for OFF.
- Rows are shown as `[position] role (id): denied | read | edit`, in priority order.
- grantee shows the rows whose grantee is exactly that id first, then the column's other rows. Role
  membership is NOT resolved: a user's id does not match the rows of the user's roles.
- A warning that storage disagrees with the service, or could not be read, means the rows shown are the
  service's filtered view: do not plan a write from it.
- It reports stored rows, not what a user can do.

## set-column-rights
Args: entity-schema-name (always); column + grantee + access for a grant; column + grantee + exactly one
of position, remove-row for a move or a removal; enable-column-permissions; disable-column-permissions;
preview.
- ONE object, ONE column and ONE grantee per call.
- Grant (access=denied|read|edit): a role with no row gets a new row at the TOP (position 0, which
  decides first), except All employees, whose new row goes to the BOTTOM so it does not override the
  specific rows; position=<n> places the row instead. A role that has a row keeps its place and gets the
  new level.
- When an All employees row ends ABOVE a row with another level, the preview and the result warn: for
  every internal user the All employees row decides, so the lower row changes nothing for them. You MUST
  tell the developer before the write; a row that must decide for internal users goes above All employees
  (position).
- To take access away, set access=denied (or read). You MUST NOT remove a row when the developer asked to
  take access away: remove-row=true deletes the row, and its members then follow the next matching row or
  the object's operation permissions, which can WIDEN their access. Removing a column's last row leaves
  the column with no rows: every user follows the object's operation permissions; the result says when
  that widens access. Name that effect to the developer before the write.
- position=<n> alone moves the grantee's row; the column's rows are renumbered 0..n-1. A position past the
  end is refused.
- A grant while column permissions are OFF is refused unless enable-column-permissions is passed with it;
  a move or a removal while they are OFF is refused.
- enable-column-permissions, with a grant or alone, turns the switch ON: every row kept since a disable
  APPLIES AGAIN, and every column with no stored row gets the default row. Before an enable you MUST name
  to the developer the rows that start to apply — each denied or read row, and each row of an external
  role — from the preview.
- disable-column-permissions is a call of its own. It keeps every stored row, not applied, and so lifts
  EVERY column restriction of the object at once — an access WIDENING. You MUST NOT disable unless that
  widening is the developer's intent, and before it you MUST name the columns whose restrictions stop
  applying: read them with get-column-rights first (the disable preview gives only their number).
- Refused, writing nothing: a grantee with several rows in the column; rows of the column that share a
  position; an unknown column or grantee; storage that disagrees with the service, is cut short or cannot
  be read; stored rows that changed between the plan and the save; another write of the object running.
  Read the refusal and act on it; never work around it by hand.

Results:
- After every save the switch and the stored rows are read back from storage and compared with the plan.
  A difference fails the call and says whether nothing was applied or the change was PARTIALLY applied;
  "saved, but NOT verified" means the read-back did not finish. In both cases re-read with
  get-column-rights before retrying or reporting.
- A repeated call that already landed reports no change.
- It does NOT change object operation or record permissions.

## No package binding
set-column-rights writes to this environment only: the rows are NOT bound to a package, and how Creatio
carries column rights from one environment to another is not established. You MUST tell the developer
that the column rights do not ship with the app: on every other environment they are set again (with
these tools or in the designer). A row names its role by SysAdminUnit id, so a role created on this
environment has another id elsewhere.

## Verify the effect
- Re-read with get-column-rights. It shows stored rows, not what a given user can do.
- To prove what a user can do, test AS that user — a non-administrator in a session opened after the
  write — on a record the user reaches through the object and record permissions. When the change does
  not show, log in again before you conclude anything.

Scope boundary: column permissions of one object, one column and one role per call. NOT object operation
permissions (`object-rights`), NOT record permissions (`default-record-rights`, `record-rights`), NOT
putting column rights into a package.

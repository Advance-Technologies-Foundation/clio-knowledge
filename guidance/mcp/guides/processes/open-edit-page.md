clio MCP process-open-edit-page guide — the Open edit page element

== What this article owns ==
This article owns the Open edit page element: its `openEditPage` block, every field in it, the rule for
WHEN to choose this element over its neighbours, and the limits. The build lifecycle, the descriptor
shape and the element catalog live in `process-modeling`; naming rules for the caption and code live in
`process-naming`; the record filter this element shares with the data elements lives in
`process-data-elements`. Do not infer a rule that lives in one of those from what this article says.

== The element and its block ==
The Open edit page element (`openEditPage` / OpenEditPageUserTask) shows a record's edit page to a user and
waits. The `openEditPage` block configures it:
`{ "name": "CollectAccountDetails", "type": "openEditPage", "caption": "Fill in the account details",
   "openEditPage": { "page": "<PageSchemaName>", "recordType"?: "<record type UId>",
     "editMode": "add"|"edit",
     "defaultValues"?: [ { "column": "<ColumnName>", <one value source> }, … ],
     "recordId"?: { <one value source> },
     "recommendation"?: "single-line text", "hint"?: "text",
     "performer"?: { "type": "user"|"manager"|"role", "contact"?: "<formula>",
                     "role"?: "<role name or id>", "showPage"?: true|false },
     "resultsByColumn"?: { "enabled"?: true|false, "column": "<LookupColumnName>" },
     "logActivity"?: { "enabled"?: true|false, "showInCalendar"?: true|false, "priority"?: "Medium",
                       "startIn"?: { "value": 0, "unit": "minutes" },
                       "duration"?: { "value": 20, "unit": "minutes" },
                       "remindIn"?: { "value": 0, "unit": "minutes" } },
     "completion"?: { "mode": "onSave"|"onConditions" } } }`
SCOPE — send the blocks the request asked for plus the ones that are REQUIRED, and nothing else. The REQUIRED
half is short: `page`, and `editMode` with its mode's payload (`defaultValues` for `add`, `recordId` for
`edit`). `recommendation` is NOT in it — the designer marks the field required, but the server fills it with the
element caption when you omit it, so send it only when the request gives you wording. Inside a block you DO
send, its own required fields apply — `resultsByColumn.column`, and a `unit` with every non-zero interval.
**But do not read "omitted" as "off".** Creating this element MATERIALIZES the user-task schema's own parameter
defaults onto it as CONSTANTS, so an omitted block is not absent — it is whatever that schema ships. Measured on
a 10.1.628 core: a freshly built element comes back from `describe` with `logActivity.enabled: true`, a 5-minute
duration and `Medium` priority, none of it requested. The shipped 7.8.0 copy of the same schema has the gate at
`false`, so the default is VERSION-DEPENDENT and cannot be assumed either way. Two consequences. To have NO
activity you must say so — `logActivity: { "enabled": false }`; omitting the block leaves whatever the
environment defaults to, and on the core measured above that is an activity with intervals nobody chose. And an
`enabled: true` in a read-back is NOT evidence the caller asked for one, so do not "preserve" it on a
read-modify-write and do not report it to a user as their configuration — `describe` reports what the element
STORES, which after a plain create already includes the platform's defaults.
Before turning it off, know what the gate does and does NOT do, because the obvious fear is wrong. It does NOT
decide whether the performer sees the step: an element built with `enabled: false` still shows up in the user's
"Business process tasks" list (verified on 10.1.628 against a purpose-built probe), so switching it off does not
strand the task. What it DOES decide is whether the step's activity is logged with the scheduling, calendar and
priority the block carries — and whether the element writes its "Connected to" links at run time: with the gate
off `describe` reports `writesConnectionsAtRuntime: false` and `setConnections` is refused. So the trade is
narrow and DECIDABLE without judgement: leave the gate on only when the request asks for an activity, or when you
are also writing this element's connections in the same build — otherwise turn it off. Do not reach for "a user
task usually needs its connections" as a reason: the shipped corpus says the opposite, with `CreateActivity` true
on 15 of the 120 Open edit page elements in `PackageStore`. Either way, say in one line what you did, rather than
letting the user find the answer on the card.
`useBackgroundMode` is covered by this rule too, ON THIS ELEMENT: leave it off unless the request asks for
background execution. Three things say so, and the third is the one that matters. The platform's own corpus:
of the 120 Open edit page elements shipped across `PackageStore`, 118 leave the flag off, and the only two
that carry it are `ProcessTests` fixtures rather than business processes (across all 469 shipped user tasks,
7). The general rule further down — set the flag on every element of a signal-started process — predates that
check and was never about a step that WAITS FOR A HUMAN. And the failure it produces is SILENT: measured on a
10.1.628 core, an Open edit page step with the flag ON did NOT complete after its completion condition was
satisfied and the record saved — no error, no log entry, the instance simply sits in `Running` and the
performer's task never clears; clearing the flag completed it. So do not set it here unless asked — and when a
step will not complete although its condition is met, suspect this flag before anything else.
PICKING THE PAGE is the part to get right, because it decides everything else: the target OBJECT and — for a
typed object — the RECORD TYPE are DERIVED from the page, never supplied. Only a page REGISTERED ON A SECTION
can be opened; any other page is REFUSED. That refusal is protecting you, not being strict: the designer
resolves an element's stored page against its own list, so a page outside it makes the card render
"Which page to open?" EMPTY and the next human save wipes the whole element. Discover valid pages with
`list-entity-client-schemas` for the object (union its `sections[]` and `editPages[]`) and PREFER an entry
whose `kind` is `freedom`, falling back to `classic` — but state the preference CONDITIONALLY: an
environment with the platform's 8.x-pages feature off offers Classic pages ONLY, so "we will use the Freedom
UI page" is a promise you cannot keep everywhere. Rank `kind: "unknown"` last and confirm with the user that
it really is an edit page rather than hiding it.
`recordType` is an optional CHECK, not a selector. The designer's page list carries ONE entry per page
(`_fillPageSchemaList` merges a repeat row instead of adding a second), so a page registered for several
record types is offered once and the type FOLLOWS the page. Pass `recordType` to assert which
registration you expect: a mismatch is refused naming the type the environment actually registers.
`editMode` decides which of two MUTUALLY EXCLUSIVE payloads applies, and they are exclusive IN STORAGE, not
only in the UI: `add` (the user creates a record) takes `defaultValues`, `edit` (the user edits an existing
one) REQUIRES `recordId`. Supplying the other mode's field is refused. On a `setElement` update, changing
`editMode`, or changing `page` to one of ANOTHER OBJECT, is DESTRUCTIVE — the designer itself warns that
changing the page loses every field value and filter setting — so those require the new mode-specific value in
the same update, and the old branch is CLEARED afterwards. A page swap WITHIN the same object requires nothing:
every stored reference still resolves, which is what makes the Classic-to-Freedom move cheap. The payload the
destructive case demands may be the EMPTY one: `defaultValues: []` states "no pre-filled values" and is
accepted, because an add-mode element without them is an ordinary shape, not a broken one. (At CREATE the same
field is simply optional for `add` — there is no stale payload to displace; only `edit` insists on its
`recordId`.) That clearing matters beyond tidiness: the runtime applies stored pre-filled
values in EITHER mode, so a leftover set would be live configuration nobody asked for.
`defaultValues` uses the SAME entry shape and the same stored format a Modify data element's `values` use:
per column, exactly ONE of `value` (a constant — TEXT columns only and non-empty; a date/lookup/numeric
constant is refused because the runtime reads those columns typed), `processParameter`,
`sourceElement` + `sourceElementParameter` (an EARLIER element's output), or `expression` (a raw macro — and
this is how a LOOKUP value is set: `[#Lookup.{objectSchemaUId}.{recordId}#]`).
`recordId` takes exactly ONE of `value` (a fixed record Id — the server wraps it into the lookup macro
against the page's own object, so you never need that object's UId), `processParameter`,
`sourceElement` + `sourceElementParameter` (e.g. a `signalStart` element's `RecordId`), or `expression`.
`recommendation` is the text shown on the opened page. The designer REQUIRES it, so it defaults to the
element caption; the platform stores a SINGLE line and a line break is REFUSED rather than silently
re-encoded. For a value taken from the process, map the element's `Recommendation` parameter with
`addMapping` instead — the same policy the email subject follows.
`completion.mode` — `onSave` (the default) completes the step as soon as the user saves the record;
`onConditions` completes it only when the saved record matches the element's separate `filter` block, and
REQUIRES that filter in the same request. The pairing is enforced BOTH ways because the runtime gates the
filter on the mode: a filter without `onConditions` (or the mode without a filter) would store, compile and
run GREEN while the condition is silently ignored. An EMPTY condition group counts as NO conditions, not as a
filter — the runtime evaluates an empty group as matching everything — so `onConditions` with an empty `filter` is
refused for the same reason — on a `setElement` update too, where the element's STORED group is the one measured.
(The designer permits that state, so a process read back with `onConditions` and no conditions completes on every
save regardless of what its card suggests; switching such an element to `onConditions` is refused until it has real
conditions.) The rule holds in the REVERSE direction too, which is the part
that surprises callers: switching an element back to `onSave` while its filter is still stored is refused, so
order the operations `clearFilter` then `setElement` (one ordered batch is atomic). On `setElement` the mode is
validated against the element's STORED filter, because that operation carries no filter field of its own. Where several Open edit page elements for the SAME object
sit in parallel branches, give each its own completion condition — otherwise they complete together.
`defaultValues` is the whole "Which default values to set in the fields of new records?" block: a supplied array
REPLACES the stored set (so removing ONE field means sending the others), and an EMPTY array `[]` removes them
ALL — the only way to empty the block, since an omitted field keeps what is stored and the runtime applies
whatever stays there. At create `[]` is simply a no-op. Per-entry value sources map onto the designer's own
menu, whose contents depend on the COLUMN's type: on a text column it offers "Process parameter", "System setting"
and "Formula"; on a lookup column it adds "Lookup value". `processParameter` = "Process parameter";
`expression` with `[#Lookup.{objectSchemaUId}.{recordId}#]` = "Lookup value"; `value` is a plain TEXT constant
(a typed constant is refused — the runtime reads those columns typed). "System setting" and "Formula" are NOT
supported on any field of this element. The `recordId` field's menu is the richest — it also offers
"Current user account" when the page's object is Account — and any such option is reachable through
`expression`, which is passed through verbatim.
`performer` is "Who performs the task?" together with "Show page automatically":
`{ "type": "user"|"manager"|"role", "contact"?: "<formula; defaults to the current user's contact>",
"role"?: "<SysAdminUnit record id or role NAME>", "showPage"?: true|false }`. A role NAME is resolved for you
against `SysAdminUnit.Name` and an unknown one is REFUSED — the element would otherwise store an assignment
with no resolvable performer. Omit the whole block to leave the step unassigned, which is the designer's own
initial state — and say what that means rather than offering to "fix" it: the platform resolves an unassigned
performer to the CURRENT USER's contact at run time, which is exactly why the designer's card shows "User" and the
current user for an element that stores neither. A `performer: null` in a read-back therefore means "not assigned
explicitly", not "nobody"; on a `setElement` update a supplied block REPLACES the assignment, so pass every part you want
kept. `showPage` is written explicitly at create (an inherited default would be
unreportable, since `describe` reports only what an element STORES) and its VALUE follows the performer: `true`
for a `user` performer or none at all, `false` for `manager`/`role`. That is not a policy of ours — the platform
opens the page automatically only for the user the step is assigned to, and the designer disables the checkbox for
the other two kinds. An explicit `showPage: true` on `manager`/`role` is refused rather than stored and ignored.
`logActivity` is the "Log activity" block: `{ "enabled"?: true|false, "startIn"?: { "value": N, "unit": "..." },
"duration"?: {…}, "remindIn"?: {…}, "showInCalendar"?: true|false }`, with `unit` one of `minutes`, `hours`,
`days`, `weeks`, `months`. Supplying the block turns the activity ON unless you pass `enabled: false`; scheduling
fields together with `enabled: false` are REFUSED, because the platform creates no activity for them to describe
and they would sit in the schema reading as live configuration. **The unit is required with a non-zero value and
always travels with it** — the platform keeps the number and the unit in two INDEPENDENT parameters, so a number
written alone is measured in whatever unit the element already had: stored, compiled and running green while
meaning something else. Zero needs no unit. Enabling the block has a second effect worth stating to the user: the
same flag decides whether this element's "Connected to" links are written at run time.
`resultsByColumn` is "Create a list of results by column": `{ "enabled"?: true|false, "column": "<ColumnName>" }`.
The step's outcome becomes one result per value of a LOOKUP column of the page's object, which is what lets
following elements branch on what the user chose. Only a lookup column qualifies — the runtime builds the list by
enumerating the column's REFERENCED object, so any other kind yields an empty list and is refused. `column` is
required unless you pass `enabled: false`. Two states to recognize when READING an existing element: the
designer lets a human switch the checkbox on and leave the required Column empty, and `describe` reports that
faithfully as `enabled: true` with `column: null` — a switched-on list that produces no results. Such a block
cannot be fed straight back (the write path requires the column), so supply a column or `enabled: false` when
re-applying.
On a `setElement` update, a `page` change that also changes the OBJECT is REFUSED while object-bound
configuration is stored — the completion conditions and this column both reference the OLD object's columns, and a
stranded column leaves the designer's field empty with the list still switched on. Swapping in another page of the
SAME object (the Classic-to-Freedom move) is fine: every reference stays valid. The refusal names the way out —
supply a column of the new object, or `enabled: false`; for the conditions, `clearFilter` before the retarget.
**State this limitation to the user before building one:** the conditional flows that would ROUTE those results are
not buildable from this contract yet (see the "NOT yet buildable" list in `process-modeling`), so the process carries the result list and
branches on it only after a human wires the flows in the designer. Building it is still useful — the list and the
column are the part a human cannot infer — but promising working branches would be wrong.
`priority` takes an `ActivityPriority` lookup NAME (`Medium`) or its record id; an unknown name is refused rather
than defaulted, because the designer marks the field required and a defaulted priority is indistinguishable from a
chosen one. It reads back as the name with the stored id alongside.
`describe-business-process` reads the configuration back as the element's `openEditPage` block,
round-trippable into a build/modify block with ONE asymmetry — the read reports pre-filled values AND a record
when the schema carries both (the write path refuses that pair), so drop the one that does not belong to the
reported `editMode` before re-applying, and feed `pageTypeUId` back as `recordType`. A `performer` of `null`
in the read-back means UNASSIGNED, never unsupported.
ROUTING between the three page elements — **Open edit page is the DEFAULT, not one of three equals.** Ask one
question: *is a user filling in COLUMNS of a record?* If yes, it is Open edit page, and no further deliberation
is needed. Signals that answer it yes, any one of which is enough: the request names fields or columns of an
object ("fill in the office, the start date and the reporting manager"); it says open/complete/check/correct/
specify a record, a card, or "that employee"; the record exists already or is being created by the same process.
Reach for another element ONLY on a positive signal for it:
- **Auto-generated page** — there is NO record whose columns are being edited. The user answers ad-hoc questions
  or presses one of several buttons, and the process branches on the answer. If you can name the object and the
  columns, this is the wrong element.
- **Pre-configured page** — the request names a SPECIFIC existing custom page to open as-is ("open the onboarding
  checklist page"). A wish for a nicer layout is not this signal; a named page is.
Two things follow from that asymmetry and both matter more than they look:
1. **Neither alternative is buildable through this contract** (only Open edit page is). So routing a
   record-editing request to one of them does not produce a different-but-working process — it produces NOTHING,
   and the user is left with a request you could have fulfilled. Never pick them for work Open edit page can do;
   if one is genuinely required, say so plainly and stop, rather than silently substituting another element.
2. **Do not ask the user which element to use.** Choosing the BPMN element is the modelling decision they
   delegated by asking for a process. Asking which OBJECT or COLUMN is meant is fine and often right; asking
   "should this be an Open edit page or an Auto-generated page?" hands back the job. When two readings are
   genuinely defensible, pick Open edit page, STATE the interpretation in one line, and continue.
A further tell: a request that also asks for a note, hint or instruction shown to the user on the page is Open
edit page — `recommendation` and `hint` are its fields, and nothing else in this contract carries them.

clio MCP process-versions guide — read which version of a process you are looking at, and which one runs

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the version MODEL and of the version fields of
`describe-business-process`. CREATING a version and making one actual live in
`process-version-writes`; this article is what that one assumes you have read. A rule that lives in
another article is cited by its article NAME and never repeated here, so a name in backticks is a
get-guidance topic to fetch, not a section to scroll to.

== The model (V1-V7) ==
(These are PLATFORM facts, not authoring rules. Nothing you write changes them, and every one of them
has bitten an agent that assumed the ordinary "one schema, many revisions" shape instead.)
Evidence, so you can weigh each one rather than trust the list: V1-V4 were read off a live
Creatio (core 10.1.448.0, ENG-94374, 2026-09-03) -- the stock family `InvoiceVisaProcess` /
`InvoiceVisaProcessInvoice1` in package `Invoice`, queried through the process-library view and then
through `describe-business-process`; 14 such families are visible in the process library on a stock
install (18 version schemas exist, and the view does not show them all), so this is the default
state of a fresh install rather than a contrived one. V5 and V6 are read from the platform's own
code, quoted where each is stated, and were NOT exercised -- neither an instance migration nor a
delete was attempted, because both are destructive and the second is what V6 forbids. Treat V5 and V6
as source-read, and if you find a platform build where either does not hold, that is a finding worth
reporting, not a licence to proceed.
V7 is DERIVED, not observed: it follows from V4 plus the fact that the root carries no marker, and the
two shapes it warns about were seen on that same stand. Nothing was probed to prove a name CANNOT carry
the information -- a negative like that is not observable, which is itself the reason to stop trying to
read it out of the name.
V1  A version is a SEPARATE SCHEMA, not a revision of one schema. Saving a new version of
    `UsrAccount_Onboard` produces a second schema with its own UId, its own Name, its own parameter
    list and its own graph. Both rows exist forever, side by side, in the process library.
V2  The family is FLAT. Every version points at the ROOT as its parent -- never at the version before
    it -- so there is no chain to walk and "the previous version" is not a relationship the platform
    stores. The root is its own family key. It is USUALLY version 0, and the implication runs one way
    only: version 0 means root, but a root is not obliged to be 0 -- the number is a stamped property,
    not one derived from the family, and stock stands carry parentless schemas numbered 1 and 2. So the
    number never settles whether a process has versions; the size of the family does.
V3  At most ONE member of a family is the ACTIVE version, and a well-formed family has exactly one --
    that is the one the runtime executes. Every other member is a readable, startable-by-code schema
    that the platform's own triggers and schedules will not choose. A family with NO active member is
    the not-well-formed case, and the read side below says what to do with it.
V4  A version's Name is `<rootName><PackageName><version>` WHEN the toolkit named it --
    `UsrProcess_0370312Custom1` is version 1 of `UsrProcess_0370312` in package `Custom`. The trailing
    `Custom1` is a PACKAGE NAME followed by a number, not a literal suffix, so it differs per package
    and a cross-package family has members whose names share no common tail. Treat this as the default,
    not a law: a schema author can name a version anything, and stock stands ship versions called
    `...V2`, `...Extended` and `...WithTracking`. V4 tells you how to READ a toolkit-made name; it is
    not a test you can apply, which is V7.
V5  A running INSTANCE stays on the version it started on. Changing which version is active therefore
    affects only runs that start afterwards: nothing in flight moves, nothing in flight is rewritten,
    and a rollback is not a repair of anything already running. Instances are never migrated between
    versions -- there is no operation that does it, and for a dynamic case the same gesture cancels the
    instance instead.
V6  DELETING a version does not exist. Not "not yet" -- the product exposes it nowhere (the process
    card's versions detail disables Add, Edit, Copy and Delete alike), and the platform's own removal
    path sets EVERY process log row of that schema to Cancelled, so a delete would rewrite the history
    of completed runs. Treat a superseded version as permanent and inert: make another version active
    instead of trying to remove the one you regret.

== Never infer versionhood from a name (V7) ==
V7  A schema Name tells you NOTHING about whether a process is a version, which version it is, or
    whether it runs. The reasons are independent and each is enough on its own:
    * The tail is a package name plus a number (V4), and a package is free to be called `Custom`,
      `Invoice` or anything else -- so `InvoiceVisaProcessInvoice1` and `UsrProcess_0370312Custom1`
      are the same shape with nothing in common to match on.
    * A process nobody ever versioned can be NAMED with a numeric tail. The designer's own
      autogenerated codes end in hex; a person may write `UsrOrder_Approve2` meaning "the second
      attempt at this idea". Neither is a version.
    * The ROOT of a versioned family carries no marker at all. Its name is exactly what it was before
      the family existed, so the member most likely to be handed to you is the one that looks least
      like a version -- and it is usually NOT the one that runs.
    Ask instead. `describe-business-process` reports the standing of whatever schema it read; that
    report is the only answer, and a regex over the name is a wrong answer that looks right.

== Reading the standing ==
`describe-business-process` carries the version fields beside the graph from clio
8.1.0.122 onward. Check by BEHAVIOUR rather than by number, because the behaviour is
the stronger test and the one you can perform: no version keys AND no `versionReadWarning` in the
response means the clio you are talking to does not report version standing at all -- so it is
unknowable from here, and upgrading clio is the fix rather than re-describing. That is one of three
states the read can be in, and the branch list below separates it from the other two. Everything here
and the two that follow assumes a clio that carries the fields; none of it is a statement about the
Creatio environment's own version.

The fields:
  `version`                 - this schema's own version number. 0 means THIS IS THE ROOT, which an
                              unversioned process and the root of a large family report alike; it is
                              not a count and never means "no versions". A root can also report a
                              NON-zero number (V2), so do not invert the rule.
  `isActiveVersion`         - whether THIS schema is the one the runtime executes.
  `activeVersionName`       - the Name of the version that does, ready to re-describe.
  `activeVersionSchemaUId`  - its UId, which identifies it unambiguously where the caption cannot.
  `versionRootSchemaUId`    - the family key.
  `versions[]`              - the family, ascending by version, each entry carrying `schemaUId`,
                              `name`, `caption`, `version`, `isActiveVersion`, `isRoot`, `packageUId`
                              and `enabled`. `packageUId` is the package identity, and it is all a
                              released clio returns: no shipped build carries a package NAME beside
                              it, so the absence of one is the shape of the response and never a
                              statement that the package could not be named. Resolve the name yourself
                              when a person asks where a version lives -- do not read a GUID out loud.
  `activeVersionSource`     - which authority answered.
  `versionsTruncatedAt`     - present only when the family was longer than the list published.
  `versionReadWarning`      - present only when the standing could NOT be established.

Read `isActiveVersion` BEFORE you explain or edit anything. That is the whole reason the fields exist:
resolving a versioned process by `process-name` returns the root, and the root is normally inactive.
Three outcomes, and only the first two are ordinary:
  * TRUE -- you hold the version that runs. Proceed.
  * FALSE WITH an `activeVersionSchemaUId` -- the graph you are holding is NOT the one that runs.
    Describe again by that UId and work from the result. This is the common case on a versioned process.
  * FALSE with NO `activeVersionSchemaUId`, or the version fields absent -- there is no graph to redirect
    to, and THREE states reach this branch, each with its own answer. Fields absent AND no
    `versionReadWarning` is the old-clio case: upgrade clio rather than re-describe. Fields absent WITH
    a `versionReadWarning` means the READ established nothing: say UNKNOWN and name the warning, which
    carries the remedy -- do not report anything about the family's data, because you read none. Fields
    present with no active member is the only one of the three that IS a statement about the data: the
    process library established no active version for this family, so say that and, WHEN `versions[]`
    is present and `versionsTruncatedAt` is absent, name its members so the user can choose one by
    code. In every state do NOT fall back to the graph you happen to be holding, and do not redirect by
    an `activeVersionSchemaUId` that is not in the response.

Four traps in those fields, each of which reads as good news if you skip it:
  * ABSENT is not zero, and zero is not "unversioned" either. `version: 0` is a real answer, but it says
    only THIS IS THE FAMILY ROOT -- and by V2 the root of a versioned family reports 0 as well, with no
    warning, which is the very row `process-name` hands you most often. So `version: 0` alone NEVER
    settles whether the process has versions. The fact that settles it is the LENGTH of `versions[]`:
    exactly one member (necessarily `isRoot` and, if the flag was established, `isActiveVersion`) is a
    process with no versions; more than one is a family, whatever this schema's own number is. Check
    `versionsTruncatedAt` is absent before trusting that length -- a capped list is not a count.
    Separately, MISSING fields are a third answer: the standing is UNKNOWN and `versionReadWarning`
    names which fact failed. Reporting an unknown or a root standing as "unversioned" is the exact
    defect these fields were added to stop.
  * `versions` is absent, never empty, when it could not be established. An empty list would read as
    "checked, and there are none".
  * `activeVersionSource` is stated rather than implied because it is `process-library-view`: the
    platform's own library view. The RUNTIME consults the schema manager instead, and the two rank a
    family by different tail keys. They agree wherever the explicit active flag discriminates, which is
    every family observed so far -- but a family that ties on the earlier keys can diverge, so the
    answer names its authority instead of promising the runtime's verdict.
  * `enabled` on a family entry is FAMILY state, not per-version state. The platform keys
    enable/disable on the root schema, so every member reports the same value; a disabled family is
    disabled whichever member you read.
  * `IsMaxVersion` is deliberately not surfaced, so do not go looking for a "latest" flag. The library
    view computes it with a lexicographic MAX over a character column across an asymmetric pool, and two
    versions of one root created in different packages BOTH report true.

== Choosing an identity ==
`describe-business-process` takes exactly one of three, and on a versioned process they do not mean
the same thing:
  `process-uid`     - ONE specific version, addressed unambiguously. Use it to follow
                      `activeVersionSchemaUId`, and whenever you must be certain which member you read.
  `process-name`    - ONE specific version, because a Name belongs to a single schema (V1). Given the
                      root's name you get the root, i.e. usually not the version that runs.
  `process-caption` - the ACTIVE version. A caption is shared by every member of a family, so it is
                      resolved to the one that runs. When a caption matches several DISTINCT processes,
                      or when no active version can be established, the call is refused with the
                      candidate codes rather than answering for an arbitrary one.
So: `process-caption` when you want what runs, `process-uid` when you want a specific member, and
`process-name` only when you know it is the member you mean.

Do NOT tell a builder to prefer the CODE over the caption for the question "what runs". That advice
inverts this section, and it has been given: in an ENG-94374 test session the assistant wrote "resolving
this process by its caption is ambiguous and silently picks v2 ... if you script anything against it,
use the schema code, not the caption". Both halves are wrong. Caption -> ACTIVE is the designed
resolution, not a silent pick; and the code resolves to ONE schema, which for the name a builder knows
is the root -- usually not the member the runtime executes. An ambiguous caption is REFUSED, with the
candidate codes, precisely so that nothing is picked silently: a refusal you can act on is the opposite
of a hazard.
Where a CODE is genuinely required -- `run-process`, a run-process button's `processName`, a script
task -- take it from `activeVersionName`, not from the code you were handed. Those articles say "never
the caption" about THEIR field, which is true and is not a statement about how to identify a process.
`get-process-signature` and `generate-process-model` apply the SAME resolution, but they do not have
these three arguments: each takes ONE value that is a code or a caption, and it is the caption reading
of that single argument which resolves to the active version. So the policy is one; the argument shapes
are not, and there is no `process-caption` to pass to either of them.

== Launching a versioned process ==
`run-process` takes a process CODE, and a code names ONE version (V1). The version the platform's own
triggers and schedules execute is the ACTIVE one (V3), which is usually not the root you reach by the
base name. Read `isActiveVersion` from `describe-business-process` and launch the code reported in
`activeVersionName` -- and only when the response carries that field. When it does not, because the
standing is UNKNOWN or because no active version was established, launch NOTHING: report the standing
and ask which code to run. Do not fall back to the code you are already holding -- on a versioned
process that is usually the root, which is usually not the one that runs.
Measured on a stand (2026-09-11): a launch resolves to the ACTIVE version and the process log records the
run against it, not against the root whose name is the familiar one. Whether the endpoint would fold a
NON-active code onto the active version is still not established, and you never need to know: pass the
active version's code explicitly and the answer is the same either way. `run-process` refuses a display
caption, and the refusal names the code it resolved to -- on a clio carrying this feature that is the
active version's code, but treat it as a lead to confirm with `describe-business-process` rather than an
answer to paste into a launch.

== File design mode ==
Under file design mode a process is absent from the process library until it has been loaded from the
file system into the database and published. Until then the version fields degrade to absent plus
`versionReadWarning` -- which per the ABSENT rule above means UNKNOWN, and specifically must not be
read as "this process has no versions".

== Where the other rules live ==
  * creating a version, making one actual, rolling back -> `process-version-writes`
  * consent before a high-impact write -- launching one is one -> `core-rules`
  * naming a process, its elements and its parameters    -> `process-naming`
  * building and editing a process at all                -> `process-modeling`
  * what a described element or parameter contains       -> `process-modeling`, then the article it routes to
This article owns the version model and the version fields, and nothing else. It does not restate the
descriptor, the element catalog (`process-element-catalog`) or the connection rules.

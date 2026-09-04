clio MCP process-versions guide — read which version of a process you are looking at, and which one runs

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the version MODEL and of what this build can and cannot do
with it. A rule that lives in another article is cited by its article NAME and never repeated here, so
a name in backticks is a get-guidance topic to fetch, not a section to scroll to.

== The model (V1-V7) ==
(These are PLATFORM facts, not authoring rules. Nothing you write changes them, and every one of them
has bitten an agent that assumed the ordinary "one schema, many revisions" shape instead.)
Evidence, so you can weigh each one rather than trust the list: V1-V4 were read off a live
Creatio (core 10.1.448.0, ENG-94374, 2026-09-03) -- the stock family `InvoiceVisaProcess` /
`InvoiceVisaProcessInvoice1` in package `Invoice`, queried through the process-library view and then
through `describe-business-process`; 14 such families are visible in the process library on a stock install (18 version schemas exist, and the view does not show them all), so this is the default
state of a fresh install rather than a contrived one. V5 and V6 are read from the platform's own code
(the versions detail disables Add/Edit/Copy/Delete; the removal path cancels every process-log row of the
schema) and were NOT exercised -- neither an instance migration nor a delete was attempted, because both
are destructive and the second is what V6 forbids. Treat V5 and V6 as source-read, and if you find a
platform build where either does not hold, that is a finding worth reporting, not a licence to proceed.
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
V3  Exactly ONE member of a family is the ACTIVE version, and that is the one the runtime executes.
    Every other member is a readable, startable-by-code schema that the platform's own triggers and
    schedules will not choose.
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
<CLIO-READBACK-VERSION-TBD> onward. Check by BEHAVIOUR rather than by number, because the behaviour is
the stronger test and the one you can perform: no version keys AND no `versionReadWarning` in the
response means the clio you are talking to does not report version standing at all -- so it is
unknowable from here, and upgrading clio is the fix rather than re-describing. That state is NOT the
"read failed" state described below, where the fields are absent but a warning says why, nor the
partial one, where a warning arrives beside the facts that WERE established. Everything in this section
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
                              and `enabled`.
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
    to, and the two causes are not the same answer. Fields absent AND no `versionReadWarning` is the
    old-clio case above: this client reports no standing at all, so it is unknowable from here and
    upgrading clio is the fix, not re-describing. Otherwise the process library established no active
    version for this family: say the standing is unknown and, WHEN `versions[]` is present, name its
    members so the user can choose one by code. Either way do NOT fall back to the graph you happen to
    be holding, and do not redirect by an `activeVersionSchemaUId` that is not in the response.

Four traps in those fields, each of which reads as good news if you skip it:
  * ABSENT is not zero, and zero is not "unversioned" either. `version: 0` is a real answer, but it says
    only THIS IS THE FAMILY ROOT -- and by V2 the root of a versioned family reports 0 as well, with no
    warning, which is the very row `process-name` hands you most often. So `version: 0` alone NEVER
    settles whether the process has versions. The fact that settles it is the LENGTH of `versions[]`:
    exactly one member (necessarily `isRoot` and, if the flag was established, `isActiveVersion`) is a
    process with no versions; more than one is a family, whatever this schema's own number is. Check
    `versionsTruncatedAt` is absent before trusting that length -- a capped list is not a count.
    Separately, the version fields being MISSING is a third answer -- the read established nothing or
    less than everything, `versionReadWarning` names which fact, and the standing is UNKNOWN. Reporting
    an unknown or a root standing as "unversioned" is the exact defect these fields were added to stop.
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
`get-process-signature` and `generate-process-model` apply the SAME resolution, but they do not have
these three arguments: each takes ONE value that is a code or a caption, and it is the caption reading
of that single argument which resolves to the active version. So the policy is one; the argument shapes
are not, and there is no `process-caption` to pass to either of them.

== Launching a versioned process ==
`run-process` takes a process CODE, and a code names ONE version (V1). The version the platform's own
triggers and schedules execute is the ACTIVE one (V3), which is usually not the root you reach by the
base name. Read `isActiveVersion` from `describe-business-process` and launch the code reported in
`activeVersionName`.
Whether the launch endpoint itself folds a non-active code onto the active version is NOT established.
Do not rely on it either way: pass the active version's code explicitly, which is correct whatever the
endpoint does. `run-process` refuses a display caption, and the refusal names the code it resolved to
-- which on a clio carrying this feature is the ACTIVE version's code. That resolution is code-read, not
run: like the fold above it has not been exercised end to end, so treat the code in a refusal as a lead
to verify with `describe-business-process`, not as an answer to paste into a launch.

== What this build cannot do ==
Reading the standing is the whole of it today. In particular:
  * There is no operation that CREATES a version. `modify-business-process` edits the schema you name,
    in place -- it does not fork one. Editing the active version of a process changes what the next run
    executes, with no version boundary and nothing to roll back to.
  * There is no operation that SETS the active version, so a rollback cannot be performed from here.
  * `IsMaxVersion` is deliberately not surfaced. The library view computes it with a lexicographic MAX
    over a character column across an asymmetric pool, and two versions of one root created in
    different packages BOTH report true -- so it is not a usable "latest" signal and is left out rather
    than passed on.
Say so plainly when a builder asks for a new version or a rollback -- and then tell them where the
product does it, because "I cannot" without "here is where you can" is half an answer. In the process
designer the SAVE button is a split button: `Save new version (Ctrl+Alt+N)` creates one, and
`Save current version (Ctrl+Alt+S)` is the in-place overwrite. After creating a version the platform
ASKS, in its own prompt, whether to make it the actual one -- so the person stays in control of which
version runs. Note what that means for you: creating a version and activating it are separate steps
even in the product, so never present them as one, and never assume a version someone just created is
the one running. `Set as actual version` in the designer ACTIONS menu activates an existing version.

Proposing a copy of the process under a new name is NOT an equivalent: a copy is a new root with its own
family, it does not become what the runtime executes, and it leaves the original running.

== File design mode ==
Under file design mode a process is absent from the process library until it has been loaded from the
file system into the database and published. Until then the version fields degrade to absent plus
`versionReadWarning` -- which per the ABSENT rule above means UNKNOWN, and specifically must not be
read as "this process has no versions".

== Where the other rules live ==
  * naming a process, its elements and its parameters -> `process-naming`
  * building and editing a process at all             -> `process-modeling`
  * what a described element or parameter contains    -> `process-modeling`, then the article it routes to
This article owns only the version model and the version fields. It does not restate the descriptor,
the element catalog or the connection rules.

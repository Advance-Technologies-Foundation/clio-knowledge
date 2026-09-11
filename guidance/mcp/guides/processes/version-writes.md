clio MCP process-version-writes guide — save a change as a new version, and roll back to another one

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article owns the two WRITE operations on a process version family and what they do and do not
change. The version MODEL they operate on — what a version is, what the family is, which member runs
and how to read that standing — is `process-versions`, and this article assumes it: V1-V7 below are
its numbered facts. A rule that lives in another article is cited by its article NAME and never
repeated here.

== Writing a version, and making it actual ==
Evidence for this section and the three that follow, graded the way V1-V7 are: both operations were
exercised END TO END on a stand (ENG-94374, 2026-09-11), and a three-member family was switched in every
direction -- v2 -> v1 -> v0 -> v2 -- with exactly one member active at each hop and the process-library
view agreeing independently. Numbering came out consecutive, every version was created inactive, and
activation warned about the whole-family re-save before doing it. One failure path remains CODE-READ:
the swallowed sibling deactivation, which is a state the platform hides and no prompt can provoke.
Report a divergence there rather than working around it.
Two tools, and they answer two different questions. Never treat them as one gesture.
  `modify-business-process-as-new-version`  applies edits to a NEW VERSION of a process instead of to
                                            the running one. This is the product's
                                            `Save new version (Ctrl+Alt+N)`.
  `set-active-business-process-version`     makes one member of a family the ACTUAL one. This is
                                            `Set as actual version` in the designer's ACTIONS menu, and
                                            it is the rollback gesture.
There is NO separate "create a version" step, and looking for one is the first wrong turn. ONE call
carries the edits AND produces the version; an EMPTY operations array is how you take a plain snapshot
of the source before editing it in place. So "make a restore point" and "put this change in a new
version" are the same tool, differing only in whether you pass operations.

The `operations` array is EXACTLY the one `modify-business-process` takes -- same vocabulary, same
descriptors, same order-and-abort rule. Read `process-modeling` for the operation reference; nothing
about it changes because the destination is a version.

What you get back is PROSE, not a keyed object -- unlike the read half, where a backticked name IS a
response key. One sentence names the created version's schema UId, the name the PLATFORM composed (root +
package + number, per V4 -- you MAY choose the package, never the number or the composed name), the number
allocated, that it is not active, the family root UId and the applied-operation count. Read those out of the sentence; do not parse it for field names,
which are server-side and never reach you.

Omit `package-name` and the version goes to the SOURCE's package -- always, with no design-package
fallback, so a package that refuses edits is refused rather than redirected. A version need not land in
the root's package (V4's cross-package families), so a family spread over packages is normal.

The new version is created INACTIVE. Creating it changes NOTHING about what the environment executes.
That is not a limitation to work around -- it is the point, and it is why the two tools are separate.

Both write tools require the `CrtProcessBuilder` package on the target environment from
1.6.1.0 onward -- the version the two operations first exist in. An environment behind that is refused up
front, naming the version the operation NEEDS, with `install-process-builder` as the remedy; one that
clears the floor but is older than the archive this clio carries is refused too, naming BOTH versions.
Either way the ENVIRONMENT is behind -- it says nothing about the process you named.

Issue these writes ONE AT A TIME, never as a parallel batch. Both are schema writes, and "take a restore
point of these six processes" is one instruction and six of them -- but concurrent schema writes on a .NET
Framework stand trip IIS rapid-fail and take the app pool down, so the failure is an environment outage
and six ambiguous transport errors rather than a call you can retry. The database half of the race is
handled (two writers on one number are refused after the save); the load is not.

== Ask once, then behave predictably ==
Two questions, asked ONCE, at the first edit of a session:
  1. Do edits go to the CURRENT version, or to a NEW one?
  2. Once a new version exists, do you want it made the ACTUAL one?
Then hold those answers for the rest of the session and say what you did in EVERY reply -- "edited the
current version", "saved version 3, the running one is unchanged", "version 3 is now actual". A builder
who has to re-derive which of those happened has lost the thing versioning was for.

The two answers do NOT carry the same authority, and this paragraph decides any sentence that seems
to say otherwise. Q1 is a ROUTING answer: it picks which tool every later edit uses, and picking a
tool changes nothing on the environment by itself. Q2 is a PREFERENCE, not a consent: it decides
whether you OFFER activation once a version exists, and it authorises no call. Every
`set-active-business-process-version` call needs its own request, naming the version to be made actual
and the environment -- a session answer is never that request, per `core-rules`, where an answer given
earlier in the session is not standing consent for a high-impact write. Before each such call, say
which version becomes actual and that the call re-saves the whole family; call it on the answer to
THAT, not on the answer to Q2.

That policy is YOUR behaviour, not a field on any request. Neither tool takes a "session mode", and no
call inherits anything from a previous one: every call states its own destination, and nothing is
carried over from the last one. So "the user said new versions from now on" changes which tool you
reach for -- it never changes what a call means.

Never activate on your own initiative. After creating a version the product ASKS, in its own prompt,
whether to make it the actual one -- in the designer, with the person answering -- so chaining
activation onto a create is not a convenience, it is taking a decision the product hands to the user.
Call `set-active-business-process-version` because the user asked for it.

== What a rollback does, and what it does not ==
Activating an earlier version IS the rollback, and it is bounded:
  * It reaches NEW instances only. Instances already running stay on the version they started with and
    finish on it (V5). A long-lived process keeps executing the old graph after the call, and that is
    correct rather than a failure to report.
  * ANY member is a valid target, the family ROOT included. "Go back to the original" IS activating
    the root, because the root is what ran before the family existed -- the platform accepts it and
    reports it as actual afterwards like any other member (measured, ENG-94374). Do not refuse it, and
    do not offer a copy or yet another version in its place.
  * It DELETES nothing (V6). The version you rolled back from stays in the family forever, visible and
    readable. "Roll back" here means activating an earlier member, never removing a newer one -- and if
    a builder asks you to delete the bad version, the answer is that no operation anywhere does it, not
    that you are missing a permission.
  * It is reported from a READ-BACK, not from the request. The platform logs and SWALLOWS a failure to
    deactivate a sibling, so the tool re-reads the family afterwards; a mismatch FAILS and names the
    version the environment really reports as actual. Trust that name over the one you asked for.
  * It re-saves EVERY member of the family in one transaction -- a write nobody asked for by name,
    reported as a warning. Nothing about the other members' graphs changes.

== A rejected edit saves nothing ==
Operations apply in order and any failure aborts the whole call. When the failure happens BEFORE the
save, nothing is written at all: there is no half-created version, no draft, and nothing to clean up.
Retry the corrected call -- do not go looking for wreckage from the failed one.

That is structural: the clone is registered nowhere until the save, so a pre-save refusal leaves nothing
any lookup can reach. If a pre-save failure claims a partial schema may still shadow the process you
named, that message is WRONG -- believe this section, and delete nothing on its advice.

Which paths still emit it, because an earlier version of this paragraph got that wrong and the mistake
is the one worth not repeating: `modify-business-process-as-new-version` has been clean since
CrtProcessBuilder 1.6.1.1. `create-business-process` has NOT -- the same fix was never applied to the
build path, and 1.6.1.9 still answers a descriptor-validation failure with "a partially created process
schema '<your process>' may still exist in the package and has to be deleted manually", with nothing in
the environment to delete (measured, ENG-94374). A fix is in flight; no version is named here until it
ships, because naming one in advance is exactly how this paragraph came to be wrong. The package
refusal path carries no such text at all and is clean: "Package '<name>' does not accept edits on this
environment, so a new version of '<process>' cannot be saved into it."
Either way the rule for you is the same, and does not depend on the version you are talking to: the
message is about a draft, not about your process, and a pre-save refusal created nothing.

That holds for a failure the tool REPORTED. A call that never answered reported nothing, so it is not
that case: the platform allocates the version number and by V6 an accidental extra version is
permanent, so re-describe the family and compare `versions[]` before re-sending anything. `core-rules`
owns the write-timeout rule. Activation is the same decision for a different reason -- it re-saves
every member of the family in one transaction, so its cost grows with the family and a slow answer is
not a failed one: settle it by re-reading the family with `describe-business-process`, which is the
authority for what is actual anyway, rather than by re-issuing the call.

The opposite case is the one to read carefully: once the save has succeeded the version EXISTS, and a
failure reported after that point still names it, because it cannot be taken back (V6). The message
says which case it is. When it names a version, that version is really on the environment.

Then there is the version you created and no longer want: it is permanent and inert (V6), so make
another version actual instead.

== What this build still cannot do ==
  * Nothing MIGRATES a running instance between versions (V5). No tool, no product gesture.
  * Nothing DELETES a version (V6).
  * Copying a process under a new name is NOT a version and never was: a copy is a new ROOT with its
    own family, it does not become what the runtime executes, and it leaves the original running. Do
    not offer it as an equivalent.

== Where the other rules live ==
  * consent for a high-impact write, and what an earlier answer does NOT authorise -> `core-rules`
  * naming a process, its elements and its parameters                              -> `process-naming`
  * the version model, the version fields, and which identity resolves to what     -> `process-versions`
  * the operations array these edits carry                                         -> `process-modeling`
This article owns only the two write operations and their outcomes. It does not restate the version
model, the descriptor or the element catalog (`process-element-catalog`).

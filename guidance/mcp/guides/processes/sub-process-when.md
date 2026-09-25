clio MCP process-sub-process-when guide — when a request calls for a Sub-process, and when it does not

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the DECISION to put logic into a separate, called process:
the one-process default, the three triggers D1, D2 and D4, what an existing process allows, how to
talk to the user about it, the never-list, and where a helper process lives. HOW to build the element
once the decision is made — the `subProcess` block, the mirrored parameters, multi-instance — is owned
by `process-sub-process`; every code and caption is governed by N1-N10 in `process-naming`. Read this
BEFORE you plan a graph: the decision changes how many processes you build and in which order.
Vocabulary: the CALLER is the process that holds the Sub-process element; the HELPER is the process
it calls.

== The default: one process ==
- A request produces ONE process. Split it only when D1, D2 or D4 below fires — nothing else is a
  reason: not a diagram that looks big while D4 does not fire, not tidiness, not a step that "might
  be reused one day". These rules govern the splits YOU would make: a split the user ASKS for ("put
  the approval in its own process", "call process X") is built as asked — `process-sub-process` owns how.
- Why the bar is high: every Sub-process element this toolset builds calls a SEPARATE process, with
  its own parameters, versions and place in the process library; nothing here folds steps in place.
  So a split is a second artifact to build, keep in step and review, and the callee must exist before
  the caller can name it. A business user checks the result by opening one diagram; spread over three,
  it is harder to verify, not easier. Creatio's own product processes agree: 82% use no sub-process at
  all, and the two largest of them, about 130 elements each, call no other process (585 processes of the
  shipped product packages, 7.8.0 branches, test packages excluded; scanned 2026-09-24).

== D1 — the same work for EACH item of a set: multi-instance, do not ask ==
- TRIGGER: the request applies the same steps to every record of a set — "for each contact of the
  account…", "every overdue invoice…", "all participants of the event…" — and at least one of those
  steps acts ONCE per run: a Perform task creates one task, an Approval one approval, a Send email one
  message (no recipient kind takes a set of records the process read, so an email to each contact of a
  set is D1 even when every contact gets the same text), an Open edit page one page for one record. A "call task" is a Perform task — `process-perform-task`
  owns that rule — so "create a call task for each contact" is D1.
- NOT D1 — work a set-based element does in one run, with no loop and no second process: Modify
  data updates every record its filter matches, Delete data deletes every record its filter matches,
  Add data in `selection` mode adds one record per record of a filtered selection, and Change access
  rights changes every record its filter matches. A request that is ONLY data work of that kind
  ("mark every overdue invoice as overdue") is one element with a filter — prefer it. Modify data
  writes the SAME values to every matched record, so a value computed from each record's own data is
  D1 again. An email to send or prepare, or a task to perform, is not data work, though each is stored
  as an Activity: one for each item stays D1, and an Add data of Activity records is no substitute
  (`process-send-email`, `process-perform-task`).
- ACTION: the per-item steps become the HELPER; the caller reads the set (Read data in `collection`
  mode, `process-read-data`) and calls the helper through ONE multi-instance Sub-process element bound
  to that read. The helper receives each item through its own input parameters (`In`, or `Variable`),
  and its steps read the item from there, never from the caller. That takes TWO kinds of mapping, and
  the first is the one that gets forgotten: the collection itself (`InputRecordCollection` from
  `ResultCompositeObjectList`), and each per-item value (a dotted name). Without the first the build
  succeeds and the loop runs ONCE with empty values. Converting the element, binding the collection and
  addressing a per-item value are owned by `process-sub-process` (MULTI-INSTANCE).
- EXECUTION MODE — choose it, do not inherit it. A conversion that names no `executionMode` is
  `Sequential` (`process-sub-process`), and Sequential starts the next item only after the previous
  item's called process has FINISHED. A helper with a human step parks until that person acts, so
  under Sequential the second contact's task is created only once the first one is completed. When
  the items are independent — the usual "for each…" — convert with `subProcess.multiInstanceOptions`
  `{enabled: true, executionMode: "Parallel"}`: an item waiting for a person no longer holds the next
  one back (measured on Creatio 10.1.37 with CrtProcessBuilder 1.6.6.22, 2026-09-24: three contacts,
  three tasks at once). It does not make automatic steps run concurrently — `process-sub-process` says
  why. Keep `Sequential` only when an item must wait for the one before it, and say so.
  In BOTH modes the caller moves past the element only when every item has finished — with a task in
  the helper, when every task is completed — so say that too when you describe the result.
- ORDER: create the helper FIRST, then the caller — naming the helper is what copies its parameter
  definitions onto the element. These are two `create-business-process` calls with no transaction
  between them: if the caller fails to build, the helper stays. Fix and retry the caller; if you stop,
  tell the user the helper exists and what it is.
- ASK: not about the loop. It is the only per-item loop the process designer and this toolset offer,
  so there is no design choice to put to the user; explain it in the result summary in business
  words. A flow routed back through a gateway (R15, `process-activity-connections`) repeats steps but
  hands them no item — do not hand-roll a per-item loop that way. Questions the request leaves open
  elsewhere — who performs the task, for example — are still asked, by the rules of the article that
  owns them.
- EXISTING process: the only sub-process a modify may add unprompted, and only when the requested
  change is itself per-item ("also create a call task for each contact"). The new per-item steps go
  into a new helper; the elements already there stay where they are. The version and modify rules
  under Existing processes still apply to that edit.

== D2 — the same fragment twice in the plan: one helper, do not ask ==
- TRIGGER: your plan contains the same fragment of AT LEAST 3 elements at AT LEAST 2 places — the
  same element kinds in the same order, differing only in values (a recipient, a text, a record).
  Two elements or fewer: keep both copies inline; the helper would cost more than the repetition.
- FIRST TRY A JOIN: when the fragment ENDS two EXCLUSIVE branches — only one of them runs, as after a
  gateway that chooses or an element's result branches — join them before it and keep one copy, no
  helper needed; a value that differs per branch is set on each branch, into a process parameter,
  before the join. NEVER after a PARALLEL split: both branches run, so a join either runs the fragment
  once instead of twice or lets the two branches overwrite each other's value. D2 covers every copy
  no such join can reach, parallel branches included.
- AND HOIST WHAT DOES NOT DEPEND ON THE BRANCH: a step at the fragment's HEAD that only reads or
  computes (a Read data, a Formula — "find the owner") and needs nothing from the branch may move
  before the split and run once. Judge D2 on what stays in the branches. Never hoist a step that acts
  — an email, a task, a data change — since it would then run on a branch that did not have it.
- ACTION: one helper holding the fragment, called by a Sub-process element at each place. The
  values that differ become the helper's `In` parameters, mapped at each call; a value the fragment
  hands back becomes an `Out` parameter. Take only a fragment with one way in and one way out — a
  helper is a separate process with one start and one run.
- ASK: no, for a NEW process; say it in the summary. An EXISTING process that repeats a fragment is
  left as it is — extract it only when the user asks for exactly that.

== D4 — two or more long phases: propose a split, and ask ==
- TRIGGER: the plan has AT LEAST 2 phases — or exceeds about 30 elements, which 98% of Creatio's
  product processes stay under (same scan). A PHASE is a stage the request names or clearly implies
  (qualification, approval, onboarding) that holds several steps around human work — a task, an
  approval or a page, each of which waits for a person. Count stages, not human elements: a lone task
  in an otherwise automatic flow is a step, not a phase, and does not fire D4. A wait with no person
  in it (a timer, a signal or message catch) is read-only in this toolset (`process-element-catalog`),
  so no plan built here holds one.
- SIZE WITHOUT PHASES: past about 30 elements with fewer than 2 phases, propose a split along the
  stages the request itself names (checking, updating, notifying), one sub-process each; when it names
  none, build one process and do not ask.
- ACTION: PROPOSE one sub-process per phase, each named after its phase, with the caller running the
  phases in order. Propose before you build, and build nothing split until the user has answered.
- ASK: yes — propose, never impose. "Keep it as one process" is a normal answer: do not split by
  phase — a D1 or D2 helper the plan needs still stays — and do not raise the split again.
- EXISTING process: never propose restructuring one on your own, whatever its size.

== Existing processes ==
- Never restructure an existing process without being asked. "Add a step to process X" adds the
  step in place and extracts nothing, even when the process already repeats a fragment or is past
  D4's size. Moving existing elements into a new process happens only on an explicit request.
- D1 is the one exception, stated above: a requested change that is itself per-item.
- Every edit of an existing process follows the modify safety rules in `process-modeling` and the
  version rules in `process-versions` — read them before you touch it.

== Talking to the user ==
- The user may not know what a process or a sub-process is. Never ask "do you want a sub-process?"
  Ask about the business consequence instead, for example for D4:
  "Qualification and approval are two long stages: first a sales rep works on the lead, then a
  manager decides. I can build each stage as its own process, so each one can be changed and checked
  separately, or keep everything in one process. Which do you prefer?"
- In the result, ALWAYS say which helper process was created and why, in business words:
  "I created the helper process 'Create a call task for one contact'; it runs once for each contact
  of the account."
  "The owner notification is needed at two points — before the contract draft and before the
  opportunity is closed — so I built it once as the process 'Notify the account owner', and both
  points use it."
- Say what follows from it being a process of its own: it appears in the process library, and a
  change to it applies wherever it is used.
- A question you owe anyway — which version to edit, who performs a task — is no reason to put a D1
  loop or a D2 helper up for approval (a D4 split is still proposed): say what you will build, and ask
  only what is owed.

== Never ==
- Extract a single element — except as a D1 helper, where one per-item step is the whole loop body.
- Extract a fragment that needs more than about 8 of the caller's values: that is not one unit.
- Give a helper a trigger of its own. It starts with ONE Simple start and nothing else: R16 requires
  the Simple start (the build refuses to call a process without one), and a signal start beside it
  would also run the helper on its own, outside its caller.
- Split a process for readability alone while D4 does not fire, or split an existing process to tidy
  it.
- Build helpers in the "agent tool" style — one small process per capability, each called from one
  place, unasked and for no D1, D2 or D4 reason: for a business user it adds processes and explains
  nothing.

== Naming and placement ==
- The helper lives in the CALLER's package.
- Its code is the caller's code, then the helper's own step in a word or two, then `SubProcess` (N3
  owns the suffix; the shared start is this article's rule, and the step keeps two helpers of one
  caller apart): the caller `UsrLead_Process` calls `UsrLead_ProcessQualificationSubProcess` and
  `UsrLead_ProcessApprovalSubProcess`; `UsrAccount_CallContacts` calls
  `UsrAccount_CallContactsTaskSubProcess`.
- Its caption says what it does for ONE item or ONE phase: "Create a call task for one contact",
  "Lead qualification".
- Everything else about names — the prefix, sentence case, the caption of the Sub-process element in
  the caller — is N1-N10 in `process-naming`.

clio MCP process-modeling-details guide — the evidence, history and rare cases behind the build-lifecycle rules

A details article of the process guide set, reached through `process-modeling`, which points here; a build starts at `process-digest`.
This article is the authoritative owner of the evidence, version history and reasoning behind the
`process-modeling` rules, and of one rare case, in the order the core states them: what the tool
read-backs report and why, why the element handle is `name`, why a second start used to be refused, what
the `VwSysProcess` dirty flags are, why an unrequested optional field is left out, the version floor of
the `removeFlow` refusal, the shipped-corpus figures and platform chain behind the Sub-process rewiring
caveat, and the stranded mapping row - an unguarded state of a Sub-process element. Split out of
`process-modeling`, which keeps the tools, the descriptor, the build recipe, the compile rule and every
modify safety rule; read that first. Nothing here relaxes a rule stated there.

== Which process article to read — why the catalog is separate ==
- What is buildable today lives in `process-element-catalog`, and the element catalog itself in
  `process-element-catalog-details`, because both grow with every element the platform gains.

== How clio builds processes (read first) — tool read-backs ==
- `list-user-tasks`: of the two shipped schemas captioned "Send email", `EmailTemplateUserTask` is the
  live one - it has 17 connections and creates an Activity. `SendEmailUserTask` has no connections and
  creates nothing. The caption cannot tell them apart, which is why the core keys on the schema NAME
  the tool returns.
- `describe-business-process` returns each bound "Connected to" link in `connections[]` both raw and
  decoded. In `writesConnectionsAtRuntime`, FALSE is the answer that matters: it marks a process whose
  connections persist and compile while writing nothing.
- `writesConnectionsAtRuntime` is `null` in three cases: a non-user-task element, a user-task schema
  that cannot be resolved, and a user task outside the supported six. All three mean "not
  established" - not false, and not permission - which is why `setConnections` is refused on `null` as
  on `false`.

== Descriptor (create-business-process) — why ==
- Creatio identifies an element by its Name plus a UId GUID. The platform reserves "Id" for that GUID,
  so the descriptor's local element handle is `name`, not `id`.
- The examples across the process guides are N1-N10 compliant on purpose (`process-naming` owns the
  rules) because an example is what a model copies first: a generated `Start1` / `task1` / `End1` is
  the failure those rules exist to prevent.

== Build recipe (intent -> running process) — evidence and history ==
- Read `process-element-catalog` before planning because there is no earlier signal than the build
  itself: a plan built around something `create-business-process` cannot build fails only at build
  time, so one fetch is cheaper than one wrong plan.
- Naming per N1-N10 in `process-naming` is what makes the result reviewable in the Process Designer.
- ONE START PER TRIGGER. The SIMPLE (manual) start may appear only once because a second one is a
  second way to start the same process by hand, with nothing to tell the two apart. Both
  `validate-process-graph` (R3) and `create-business-process` used to refuse ANY second start, so a
  process reacting to two triggers was unbuildable - although the shape is one the platform itself
  ships (`PublishDraftToArticle` carries two start signals). The change - several starts, the simple
  one still only once - is gated by the two floors `process-modeling` states; below either floor the
  second start is still refused, with the symptoms the core names.
- File-design mode: on an FSD stand a built process is saved to the file system, so the designer sees
  it, yet it is not runtime-active until it is loaded FS->DB and published.
- The readiness trap. Inferring "needs a compile" from a raw column NAME is the mistake the compile rule
  guards against: a raw read of `VwSysProcess` surfaces per-process DIRTY flags - `NeedInstall`,
  `NeedUpdateSourceCode`, `NeedUpdateStructure` - that are ALL `true` on a freshly-saved process. None
  of them is a `compile-creatio` instruction (`NeedInstall` in particular is a DB-install marker, never a
  "compile"), and the same caution applies to any `NeedXxx` / `IsXxx` column reached through a raw read.
- What runs without a compile, spelled out: add/read/modify data, formulas, connections, signals, and
  USING an already-compiled user task are applied and run with no compile. The compile rule scopes
  compilation to the PROCESS only; the other configuration schemas it names carry their own obligations.

== Set what was asked for, and nothing else — why ==
- Filling in an OPTIONAL field the request did not mention changes behaviour nobody chose: a flag left
  out keeps the platform's value, which is not always the falsy one. It also erases the "not decided"
  signal, because describe reports what is WRITTEN.
- A value carried over from a process built earlier in the session is a guess wearing the clothes of
  context, which is why a missing BUSINESS decision is asked for rather than reused.

== Modifying an existing process — safety rules: evidence and rare cases ==
- `removeFlow` with `kind` or `condition`: the refusal is version-dependent, so it is not a backstop. Below
  the floor `process-modeling` states, the extra fields are accepted and silently dropped rather than
  refused, and clio does no client-side check either way.
- Gateway ELEMENTS, DEFAULT flows and the Formula element used to be on the list of constructs the
  builder cannot create. All three are buildable now; `process-branch-conditions` and
  `process-element-catalog` own them.
- A designer retarget of a Sub-process element. The build path refuses a retarget while live dependents
  still read from the element; the designer has no such guard. When a retarget goes through it, the
  platform removes the orphaned parameters and flags every element that referenced them invalid, so the
  process refuses to START later, blamed on the process rather than on the edit. That chain is read from
  platform source, not measured through this tool, which refuses the retarget first.
- MULTI-INSTANCE Sub-process elements used to be refused outright; they are now de-converted, edited as
  a single-instance element with every guard, and re-converted (`process-sub-process`). Multi-instance
  is not an edge case: 61 of the 416 sub-process elements in the shipped package corpus are in that
  state (scanned 2026-09-12 over the local PackageStore, 1 099 package roots; recorded in the
  CrtProcessBuilder repository as `docs/sub-process-element-capture.md`).

== Stranded mapping row (Sub-process element) ==
- Read this when a caller reports a mapping that resolves to nothing. The hazard is UNOBSERVED rather
  than guarded: a mapping row stranded on a parameter the Sub-process element itself owns.
- The dependents check does scan the element's own parameters (from CrtProcessBuilder 1.6.3.26, the
  version that shipped this element), but it never reads `schema.Mappings` - it walks stored VALUES and
  EXPRESSIONS - so a stranded row is not what it looks for.
- Nor does the platform clean one up: its prune arm skips a parameter whose `CreatedInSchemaUId` is the
  CALLER's schema, and that is exactly the owner-created parameter this hazard is about.
- Nobody has yet observed the state (it is tracked as an open item in the CrtProcessBuilder repository),
  but treat that as unobserved, not as prevented.
- If a caller later reports a mapping that resolves to nothing, that is the state to look for, and the
  test is whether the `[Parameter:{…}]` UId inside the stored metapath still matches a `uid` that
  `describe-business-process` reports on that element.

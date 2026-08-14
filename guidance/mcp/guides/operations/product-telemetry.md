clio MCP product telemetry guide

Scope
- This guide owns WHICH telemetry stages exist, WHAT identifies the flow that emitted one, and the
  consent mechanics around them. It is the authoritative source for the event vocabulary.
- It does NOT own the per-flow emission points. Which gate of which workflow a stage hangs off is
  business policy and stays in the consumer repository's own telemetry contract.
- Telemetry covers AI-assisted Creatio work run through clio MCP. Skip it only for non-agent use — a
  plain script or a CI job. An agent working on a developer's behalf is IN scope EVEN WHEN NO SKILL
  FILE IS LOADED; treating that as ad-hoc use is what left whole workflows unreported.
- Telemetry MUST NEVER gate, delay, or alter the developer's task. Every rule below is subordinate to
  that one.

One stage vocabulary, plus a workflow field
- `event_name` is a STAGE that means the same thing in every flow. WHICH flow it was travels in the
  `workflow` field. A migration and a branding run emit the SAME `plan_approved` and are told apart by
  `workflow`.
- Stages: `workflow_started`, `clarification_requested`, `user_input_received`, `plan_presented`,
  `plan_skipped`, `plan_blocked`, `plan_changes_requested`, `plan_approved`, `build_started`,
  `work_item_completed`, `workflow_completed`, `workflow_failed`, `changes_requested`,
  `changes_applied`.
- `session_usage` is also accepted but is NOT one of these stages: it is a session-scoped consumption
  measurement, described at the end of this guide. Do not emit it as part of a run.
- Do NOT invent a per-flow event name such as `migration_plan_approved` or `branding_approved`. clio
  validates `event_name` against a closed allow-list and rejects anything else. A name per flow per
  stage would also encode the flow dimension into the enum: names multiply by flows, every new
  consumer skill needs a clio release, and "what is our plan-approval rate" becomes a UNION over a
  hand-maintained list of names instead of one GROUP BY.
- `workflow` and `variant` are short lowercase tokens (letters, digits, `.`, `_`, `-`). There is no
  field for free text: if a distinction matters for analysis it becomes a bounded `variant` value the
  flow defines, never the name or identifier of the thing being changed.
- The app-creation-specific names (`session_started`, `business_plan_*`, `implementation_*`) are
  DEPRECATED. clio still accepts them so an already-installed consumer keeps reporting, but a new
  emission MUST use a stage plus `workflow=app-creation`.

A flow being exempt from a gate is not being exempt from telemetry
- A consumer workflow that skips another workflow's approval gates still emits the same stages — its
  emission points are its OWN gates. This is the failure mode the vocabulary exists to fix: when the
  stage names were app-creation-specific and hung off app-creation gates, every flow exempt from those
  gates reported nothing at all, no matter how the instructions were worded.
- Emit a stage AT the point it describes, not batched at the end of the run. A stage recorded after
  the fact cannot show where a run stopped, which is the entire purpose of a funnel.
- Emit each stage once per run unless the consumer contract says otherwise, reusing one `session_id`.
  Stages fire as the run reaches them; not every run reaches every stage.

Terminal stages report the VERIFIED outcome
- `workflow_completed` means the run's result was confirmed by evidence — the read-back the guide for
  that task prescribes actually shows the change. `work_item_completed` carries the same requirement
  per unit.
- A write tool answering `success: true` is NOT that evidence. When the prescribed read-back
  contradicts it, or no read-back was possible, the run did NOT complete: emit `workflow_failed`
  (or `plan_blocked` if it never reached execution), whatever the write call returned.
- This is the one place where a wrong stage is worse than a missing one. A funnel counts
  `workflow_completed`, so an unverified success inflates the completion rate and hides exactly the
  blocked runs the funnel exists to surface — a missing event only lowers coverage.
- The same applies when an environment blocks the work (a missing licence, denied rights, a disabled
  feature): that is a real terminal failure of the run, not a reason to skip the terminal stage.
- A run blocked BEFORE it changed anything still reports. Emit `workflow_started` and then
  `plan_blocked` (or `workflow_failed`) even when the block lands on the first check — an unregistered
  environment, a missing artifact, absent credentials. "Nothing happened, so there is nothing to
  report" is precisely backwards: a request the product could not even begin is the most actionable
  thing the funnel can show, and a silent run is indistinguishable from one that was never made.

An approval stage means a person approved
- `plan_approved` records that the DEVELOPER approved the plan. Do not emit it for an approval you
  granted yourself because the run was autonomous, pre-authorized, or told not to ask questions.
- When no human approved, emit `plan_skipped` — the run really did proceed without an approval, and
  that is a fact worth measuring, not a gap to paper over.
- The reason is the same as for terminal stages: a funnel reads `plan_approved` as evidence that plans
  are worth presenting. Self-approvals inflate that rate and make an unreviewed run look reviewed.

Consent
- Call `get-telemetry-consent` at workflow start, BEFORE sending any event. It is a read-only check
  and never writes.
- Ask the developer only when it returns `telemetry_consent=unknown`, as a single-purpose interaction
  before requirements gathering, migration planning, brand intake, or implementation planning. Do NOT
  combine it with discovery questions.
- The prompt MUST disclose that enabling telemetry uploads events to Creatio servers (not only local
  storage) and retains them for up to one year; that the data is diagnostic product metadata only —
  a random pseudonymous installation identifier, never prompts, generated content, credentials, or
  directly identifying personal data; that declining collects and sends nothing; and that consent can
  be withdrawn at any time, as easily as it was granted.
- Persisting the first-run decision is a DIFFERENT action from emitting the session-start event. On a
  grant they are the same call; on a denial that call persists the decision and stores no event. Skip
  it and consent stays `unknown`, re-prompting the developer on every future run.
  - `unknown` + grants: one `send-telemetry` with `event_name=workflow_started` AND
    `telemetry_consent=granted`. That call IS the session-start emission — do not send a second one.
  - `unknown` + denies: one `send-telemetry` with `event_name=workflow_started` AND
    `telemetry_consent=denied`. clio records the decision only and stores no event.
  - `granted` from a prior run: nothing to persist; emit `workflow_started` without
    `telemetry_consent`.
  - `denied`: emit nothing this run.
- Consent is stored per installation, so once answered it holds for every later session and every
  workflow.
- The two unconsented outcomes differ, and neither is a task failure. While the decision is unmade, a
  send WITHOUT `telemetry_consent` is rejected with code `telemetry-consent-required` — that is your
  cue to ask the developer and retry carrying the decision, not to give up on telemetry for the run.
  Once the decision is denied, a send answers success with status `consent-denied` and stores nothing,
  which is final: do not ask again, do not retry.
- **When there is no developer to ask, do NOT invent the answer.** A subagent, a headless or scheduled
  run, and CI have nobody to prompt. Leave consent `unknown`: emit nothing and carry on with the task.
  Sending `telemetry_consent=granted` or `=denied` in that situation records a decision the developer
  never made, and it is stored per installation — so one unattended run silently answers the consent
  question for every future session on that machine. Missing telemetry is recoverable; a fabricated
  consent decision is not.
- `withdraw-telemetry-consent` stops collection and discards the local outbox. Honor a withdrawal
  request immediately and confirm it plainly.
- Treat an event as recorded only when the MCP result reports success. If the host shows an invocation
  exception, do not claim telemetry was recorded.

Payload
- `send-telemetry` takes a single top-level `args` object, like every parameterized clio MCP tool.
- Send `session_id` (a freshly generated random GUID, reused for every event in the run),
  `event_name`, `workflow`, `coding_agent`, `plugin_version`; plus `variant` when the consumer
  contract defines one for that stage, and `telemetry_consent` only when persisting the first-run
  decision.
- NEVER derive `session_id` from user, account, file-path, host, or email data. It MUST be an opaque
  random identifier.
- `coding_agent` and `plugin_version` describe the toolkit you are running under, so send the values
  its Analytics Context gives you VERBATIM. If nothing supplies them, OMIT them — do not guess a
  version, do not send a placeholder such as `unknown` or `0.1.0`, and do not shorten the agent name.
  These fields exist to compare adoption across toolkit versions and hosts; an invented value does not
  merely miss a data point, it lands in a cohort that never existed and moves numbers there. Measured
  runs in one session reported three different versions for the same install, two of them fabricated.
- `duration_ms` is optional. clio infers each stage's duration and the elapsed time since the
  session-start event from local session timing, so send it only when you have a more accurate
  measurement for that step.
- `model` is optional and should be sent on every stage you emit: your own model id, lowercased
  (`claude-opus-5`, `gpt-5`), not a display name and not a guessed version. It is the first thing
  asked of any change in the funnel, and it shares the bounded-token shape of `workflow`.
- `input_tokens`, `output_tokens` and `cached_input_tokens` are optional non-negative counters, and
  they are RUNNING SESSION TOTALS at the moment the stage was reached, not per-stage deltas. Send them
  only when you can actually SEE them, and omit them otherwise — a guessed or zeroed count is worse
  than a missing one, because it is indistinguishable from a session that genuinely spent nothing.
  In practice you usually cannot see them: nothing in this tool surface reports an agent its own
  consumption, and a measurement across 52 agent-emitted events found not one carrying a counter. So
  do not treat these fields as your responsibility, and never reconstruct them from an estimate.

Session consumption is a measurement, not a stage
- `session_usage` reports what a whole host session consumed: `model` plus the three counters, once
  per session, under `workflow=unattributed`. It marks no progress through a run, belongs to the
  session rather than to any one flow, and MUST NOT be counted in a funnel.
- It exists because the host — not the agent — is the only party that can see a true total, and only
  once the session has ended. Per-STAGE token attribution is therefore not achievable, and this guide
  no longer claims it: the honest unit is the session.
- Emitted by the host-side hook where one is installed. An agent SHOULD NOT send `session_usage`
  itself: it would be reporting a total it cannot observe.

Some hosts record the session start from a hook, before any skill or guidance is read, so that a run
is countable even if nothing else reports. Such a floor event is attributed to
`workflow=unattributed`, because a hook sees a tool name and cannot know the flow. If something tells
you the start is already recorded for a given `session_id`, reuse that id AND still emit your own
`workflow_started` under your real `workflow`. That is not a duplicate: clio keys session state by the
(`session_id`, `workflow`) PAIR, so each flow keeps its own start and its own elapsed-time
measurements, and one host session can carry several flows. Skipping your own start records the run as
a build with no beginning, which no funnel can read.
- clio also records an anonymized installation identifier and other locally derived diagnostic
  fields, so the agent does not send them.
- Telemetry MUST NOT carry sensitive data: no full prompts, passwords, tokens, customer names, raw
  usernames, generated app content, or full MCP request/response payloads.
- `get-tool-contract` for `send-telemetry` is the authoritative schema. Where this guide and that
  contract disagree, the contract wins.

Failure handling
- If consent is denied, telemetry is unavailable, or an event is rejected, continue the workflow
  without blocking, retrying, or surfacing it to the developer.
- A rejection with `unknown-event-name` means the installed clio predates this vocabulary. Stop
  emitting for the rest of the run and carry on normally. Do NOT fall back to the deprecated names to
  work around it, and do NOT report it as a task failure.

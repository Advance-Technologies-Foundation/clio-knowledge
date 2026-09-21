clio MCP sequences guide

Scope and compatibility
- This guide owns Sales Engagement sequence definition, execution and portability decisions. It does not define a new sequence engine.
- Verified on Creatio 10.1.752.0, .NET 8, PostgreSQL with Sales Engagement installed. Other versions require capability discovery and the same readback assertions. Email delivery, exhaustive timezone/DST combinations and two-user assignment were not verified.
- Resolve `get-sequence-context`, `execute-dataservice-batch` and `enroll-sequence-participants` with `get-tool-contract` first. These contracts are introduced by clio#1574; older released clients may not have them. If absent, report the missing capability; do not upgrade the client without user authorization. Do not pretend an unknown tool ran. The native package rich-text fix is clio#1579.
- Call these long-tail tools through `clio-run`, with `command` and `args`. Use the live contract for required arguments and limits. Read `core-rules` for transport safety, `esq` plus `esq-filters-frontend` for DataService reads, `data-bindings` for package files, and `package-dependencies` for package references.

Choose the operation
- Follow the DataService-first rule in `core-rules` for general record reads/writes. `execute-dataservice-batch` wraps native entity writes; it does not bypass entity validation or sequence listeners.
- Native `enroll-sequence-participants` owns audience enrollment. It creates the participant through the platform lifecycle, applies eligibility/rules/capacity and may create the first activity. MUST NOT insert a participant directly as Active or manually create its first activity.
- Activation and supported pause/resume belong to Creatio's sequence UI and lifecycle. A successful row write is not proof that an execution transition is valid.

Discover once, then keep the working set small
1. `clio-run {"command":"get-sequence-context","args":{"environment-name":"<env>"}}` reads merged schema metadata and relevant lookup choices. Add `sequence-id` when inspecting an existing definition.
2. Require complete sections for every decision you will make. `absent`, `missing`, `failed` and `truncated` are not empty complete results. Resolve missing dependencies or permissions before authoring. Never guess a lookup GUID from another environment.
3. Retain only needed schema fields and lookup IDs. Reuse them during the operation; re-read after mutations that can invoke listeners. Context is bounded and is not a full export: explicitly read step Description, Subject, Body, priority/action and email settings when copying or packaging them.

Definition model
- `Sequence` references a `SequenceRuleset` and `DeliverySchedule`. Ordered `SequenceStep` rows define actions. `SequenceParticipant` and generated `Activity` rows are runtime state.
- Create the definition as Draft. Use an existing appropriate ruleset/schedule or create package-owned custom ones; do not silently change shared rules or schedules.
- Resolve required fields and reference schemas from discovery. Rules include enrollment eligibility, initial/audience transition stages and per-user capacity. A cap can legitimately leave enrolled participants Pending.
- The ruleset initializes the participant stage. A stage supplied during ad hoc insertion can be replaced. There is no universal Stage × Status matrix; inspect the actual rules and native outcome.
- Sequence Owner is the definition owner; the participant Sequencer is the executing user used for generated activity assignment. Do not assume assigning Owner chooses every participant's Sequencer. Native enrollment uses the current execution context; inspect resulting records. The lab used one user, so cross-user assignment needs its own verification.

Steps and content
- Normal entity saves maintain one-based Index and generated Name. In the verified version, Index 0 appends; deletion renumbers the remaining steps. Read back order and names after a batch. Do not overwrite the generated names as an independent numbering scheme.
- Task and Call use Description for activity Notes. Setting only Body is not a substitute for their instructions. A task's Action selects its native action; the verified default was General task.
- Email uses Subject for activity Title and Body for its HTML body. Description remains notes. Select the intended EmailMode and EmailThreadingMode from discovery; the reference workflow uses Manual and New thread.
- Use the sequence designer's native sales email template and macro insertion controls. Do not invent macro syntax or treat a sales sequence template as a marketing BulkEmail template. `email-templates` owns its stated EmailTemplate/BulkEmail scope; it does not establish a sequence macro contract. Verify rendered output in the generated activity before any real send. Automatic sending and macro expansion were not acceptance-tested here.
- Postpone plus Measurement defines a delay followed by delivery-slot scheduling. This is not simply creation time plus a duration. The verified progression used completion of the preceding activity as its timing anchor. Read the resulting StartDate/DueDate; do not fabricate them to make a fixture look active.
- DeliveryScheduleSlot has a weekday and TimeFrom/TimeTo. They are Time columns despite date-bearing API representations. Preserve time-of-day semantics; the dummy/readback date is not a business date. Verify timezone policy and delivery windows on the target, especially weekends and DST.

Minimal executable workflow
Use a named isolated environment, synthetic contacts with example.invalid addresses, a Draft definition and a Manual email step. Resolve all placeholder GUIDs from discovery or create them once for package-owned records. Use stable IDs when binding definitions. Never run this workflow against real recipients as a test.

1. Discover schemas/choices. Resolve Task/Call/Email, Hours, Medium priority, General task, Manual, New thread, Draft and the intended rules/schedule IDs.
2. Create missing rules and schedule with one native batch, then their slots, then the Draft Sequence. Check each item's state and read back required references. Avoid mixing dependent writes until their prerequisites are confirmed.
3. Insert ordered steps with `execute-dataservice-batch`. For example, the following inserts one task; add a call and a manual email using the same shape and the appropriate content fields. Values use the live batch contract's typed scalar form, not OData lookup-column suffixes.

```json
{"command":"execute-dataservice-batch","args":{"environment-name":"<env>","operations":[{"operation":"insert","schema-name":"SequenceStep","record-id":"<new-step-guid>","values":{"Sequence":{"data-value-type":10,"value":"<sequence-guid>"},"Type":{"data-value-type":10,"value":"<task-type-guid>"},"Index":{"data-value-type":4,"value":0},"Postpone":{"data-value-type":4,"value":0},"Measurement":{"data-value-type":10,"value":"<hours-guid>"},"Priority":{"data-value-type":10,"value":"<medium-guid>"},"Action":{"data-value-type":10,"value":"<general-task-guid>"},"Description":{"data-value-type":1,"value":"<p>Review the synthetic prospect before calling.</p>"}}}]}}
```

4. Read back Index/Name/content. Follow core-rules' saved browser-verification preference: reopen the standard designer when automatic verification is selected; otherwise give the user the following checks. For the three-step reference, assert Task → Call → Manual Email and intended descriptions/subject/body. Confirm the rules, schedule and Draft state.
5. If package delivery is requested, package the definition as described below before target activation. On the target, configure local owner/execution context, activate explicitly in the standard UI using authorized browser interaction or ask the user to perform this step when browser interaction is unavailable. Read back Active before continuing with the active-execution test. Then enroll a synthetic contact:

```json
{"command":"enroll-sequence-participants","args":{"environment-name":"<env>","sequence-id":"<sequence-guid>","contact-ids":["<synthetic-contact-guid>"]}}
```

6. Interpret added-count as enrollment, not Active count. Inspect readback status. When capacity permits, assert exactly one first-step Activity for the new participant, matching SequenceStep, assigned execution context and Description→Notes. Do not create another activity when enrollment already did so.
7. In an isolated fixture only, complete the first task and then the call through supported activity behavior. Verify one new current activity at each transition, prior activities no longer current, and the manual email Title/Body. A synthetic completion proves orchestration, not a real call or email delivery.

Failures and safe recovery
- Native batch continuation is enabled; one failed item does not imply rollback of earlier or later items. Correlate item indices, inspect completed/failed/unknown and read affected records before resubmitting uncertain writes. `diagnostic` adds context, not proof of atomicity or a parsed HTTP status. Follow `core-rules` for general retry safety.
- Enrollment rules may reject duplicates, or permit another enrollment. Do not impose a generic deduplication policy different from the selected ruleset. A readback can include preexisting participants.
- Pause/resume a participant created by the native lifecycle using the standard audience actions; verify both participant and current activity. An artificially inserted Paused row without its current-step activity is not a valid resumable participant. Do not manufacture a missing activity or force Active to bypass the failure; discard that synthetic test setup or create a fresh native enrollment in a suitable lab.
- Pending under an active cap is not a failure to repair by direct status assignment. Investigate eligibility, active counts, execution user and schedule.
- Preserve useful bounded validation messages but never publish raw responses, stack traces, credentials or private investigation artifacts.

Package portability
- Use existing `create-data-binding` and `add-data-binding-row`, then normal package compression/install. Follow `data-bindings` for file semantics. Include Sequence, ordered SequenceStep rows, custom SequenceRuleset, DeliverySchedule and every DeliveryScheduleSlot; bind other custom referenced definitions or declare/verify their dependency.
- Include the complete intended field projection, including rich-text step content and email settings. Product lookup IDs and the supporting sequence/email/execution packages must exist on the target. Stable package-owned row IDs are portable identity; an unrelated same-name target row is not the same identity.
- Ship Draft status. Exclude participants, activities, execution history and mailbox credentials. Configure target-local owners, sequencers, mailboxes and audiences separately.
- Prove fresh installation and full readback; verify the reopened designer automatically or with the user according to core-rules' browser preference. Reinstall must preserve IDs/counts. Default non-key IsForceUpdate=false preserves target edits; unchanged-version reinstall is not proof of an upgrade overwrite policy. Test upgrades separately.

OData fallback
Only use OData when a specific required operation has no suitable DataService/native path and state that reason. Follow the discovered OData contract for lookup/filter names and temporal literals, including explicit offsets where required. Do not switch to OData merely because an ESQ request or permission check failed; fix the request or use the established privileged operation when appropriate.

Evidence and bounds
- clio#1575–#1579: discovery, native enrollment, mixed DataService batch/readback, bounded diagnostics and fresh package installation/reinstallation. Focused tests include SequenceContextCommandTests, SequenceEnrollmentCommandTests, DataServiceBatchCommandTests and DataBindingToolE2ETests.
- Clio implementation baseline: commit ee29c8866e5171d5b77811de613d0136ebb3ad93; rich-text portability repair 98b2a8bd4df755a1ac67f7a3a3a4d93fd7789eca (clio#1579).
- The package target showed three ordered step cards after reopen; explicit activation/enrollment produced one native first task. No real email was sent. These observations do not establish all platform versions, mailbox providers, automatic sending, macro expansion, DST cases or cross-user assignment.

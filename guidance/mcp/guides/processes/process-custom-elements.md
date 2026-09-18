# Custom business process elements

Create a package-owned BPMN user task, a Classic parameter page, icons, and installation-time toolbox registration. This guide owns the custom-element artifact contract and acceptance checks. Read `process-modeling` for process construction, `process-parameters` for process mappings, `integration-testing` for test scaffolding, and `deploy-lifecycle` for deployment. For one element selecting between separate tasks and pages, read `process-custom-element-families`.

## Applicability and reference

Tested with Clio 8.1.0.130 and Creatio 10.1.585.0, .NET 8, PostgreSQL, English captions, synchronous BPMN tasks. SQL Server scripts are provided but untested. DCM, asynchronous waiting, older designers, and multilingual toolbox captions require separate validation. This is a tested reference pattern, not a cross-version promise for Classic designer internals.

Use `list-knowledge-examples` or the catalog resource `atf.creatio.custom-process-element-reference` to obtain the immutable reference revision. The independent reference repository contains Format text, a four-operation Arithmetic family, source, package SQL, unit tests, typed process models, and a sanitized validation record. Clone the pinned revision; do not treat a mutable default branch as the verified artifact.

## Discover the available primitives

Read `core-rules`, discover the live contracts with `get-tool-contract`, and use `clio-run` for non-resident tools. Reuse `create-user-task` and `modify-user-task-parameters`. Both take an environment and absolute workspace path; preserve native schema and parameter identities when updating.

The original Clio 8.1.0.130 reference predates dedicated registration/page tools. Discover `register-process-element` and `create-user-task-page` separately. Use the following workflow only when the running server exposes the matching contracts; otherwise author those package artifacts from the pinned reference or native designers. Never use Freedom UI `create-page` for this panel or replace independent user-task creation with a new orchestrator.

The new contracts were verified against Clio source revisions for [registration](https://github.com/Advance-Technologies-Foundation/clio/commit/77a47d367c1e88876f7ccfd6afbb19d7c745114f), [Classic pages](https://github.com/Advance-Technologies-Foundation/clio/commit/031bb275d302bd715e44586bf3c149da6c818be5), and [parameter types/directions](https://github.com/Advance-Technologies-Foundation/clio/commit/f52b8cd65cae67642deb8c04ae9e9cb4e140b89c). A version with the older reference's tool contract need not expose these capabilities. Both new scaffold commands operate offline on an explicit workspace and package; deployment remains a separate step.

Dispatch each through `clio-run`, passing its command name and an `args` object:

```json
{
  "command": "create-user-task-page",
  "args": {
    "workspace-path": "<absolute workspace path>",
    "package-name": "UsrExample",
    "user-task-uid": "<existing task schema UId>",
    "page-name": "UsrTaskPropertiesPage",
    "caption": "Task parameters"
  }
}
```

This generates the native parent/FK11 association and editable input controls. Optional `culture` (default `en-US`) and `small-icon-path`, `large-icon-path`, `title-icon-path` arguments populate caption and task resource slots. Existing page associations/names/resources are refused; customize an existing page directly. The generated layout reserves input names `UserTaskContainer` and `EditorsContainer`. Icons must be static basic SVG shapes, at most 1 MiB each, without CSS, scripts, animation, external references, processing instructions or base-URL overrides. Omitted icon slots and unrelated resources are preserved.

```json
{
  "command": "register-process-element",
  "args": {
    "workspace-path": "<absolute workspace path>",
    "package-name": "UsrExample",
    "user-task-uid": "<same task schema UId>",
    "caption": "My process element"
  }
}
```

Registration generates separate package-owned PostgreSQL and SQL Server after-package scripts. Rerunning identical inputs preserves script identities/content; conflicting existing scripts are refused. The caption is the initial installed caption, not an update to an existing registration. PostgreSQL installation and forced script re-execution preserved one registration and its edited caption. SQL Server generation is covered by tests; its installation remains unverified.

## Artifact sequence

1. Create a custom package depending on `CrtProcessDesigner`; create the user task through the existing primitive or native designer.
2. Define inputs and outputs with explicit directions and correct data types. Keep schema UIds and parameter UIds stable.
3. Put business logic in a typed handler registered in the app DI container. The native task resolves that service from the app instance and maps the result into process outputs.
4. Create a Classic client view-model schema inheriting `ProcessFlowElementPropertiesPage`; add MAPPING editors for inputs using its inherited persistence behavior.
5. Associate the client schema with the task's **Parameters edit page** property; provide task icon resources.
6. Package database-specific after-package registration SQL, then deploy using the target's detected FSM mode and the owning deployment guidance.
7. Verify native designer behavior, persisted mappings, execution, errors, and clean installation on a separate non-FSM target.

### Directions and error contract

For these reference patterns, always expose `IsError` (Boolean, Out) and `ErrorMessage` (unlimited text, Out), plus the domain result (Out). Inputs are In. Set directions explicitly: leaving them unset behaved as Variable/bidirectional and offered inputs as outputs. Resulting is independent of direction; hiding an editor does not make a parameter output-only.

Use `modify-user-task-parameters` with `set-parameter-directions` entries containing `parameter-name` and `direction`, according to the current contract. Do not remove and recreate a parameter just to change its direction. Exported metadata uses L12 values 0 In, 1 Out, 2 Variable; prefer the supported operation over raw edits. Where the live parameter contract supports it, use `Unlimited text` (native MaxSizeText/type 29) for ErrorMessage; `Text`/`String` remain the short text type. The original reference used native MaxSizeText metadata because its tool version did not expose that type.

The direction-preserving implementation snapshots existing explicit workspace directions before saving, restores retained parameters, then applies explicit requested changes. This requires a linked FSM workspace even for unrelated edits when existing parameters have explicit directions. A failed import is not success: other parameter changes may already be saved. Follow the command's recovery instructions, import the corrected workspace, compile the affected task and verify native readback.

The pinned reference v0.2.0 explicitly sets Text/Prefix to In and FormattedText/IsError/ErrorMessage to Out. Its portable `scripts/Test-PackageContract.ps1` guards all five exported task contracts, including Boolean IsError and unlimited ErrorMessage. The earlier v0.1.0 export omitted L12 for Text, Prefix and FormattedText; upgrade or correct those directions while preserving schema/parameter UIds, then verify the installed task and saved process.

The handler returns `ErrorOr<T>`. Reset all outputs on each execution. Map expected errors to IsError=true, ErrorMessage, and a cleared domain result; complete the synchronous task normally so the process can branch. On success clear the error flag/message. Catch unexpected exceptions at the adapter boundary, log details server-side, and return a generic message rather than disclosing internals. Test subsequent success after failure. This is the reference's explicit error contract, not a claim that every Creatio user task must suppress exceptions.

Returning true completes this synchronous task; false leaves it running and needs a separate completion lifecycle. Let Creatio generate the partial companion and parameter properties. Do not fabricate generated declarations to fix a local test build.

### Classic parameter page

This is the designer's right-side configuration panel, not a Freedom UI execution page. Parent inheritance belongs in client schema metadata. A minimal input attribute is:

```javascript
"Text": {
    dataValueType: Terrasoft.DataValueType.MAPPING,
    type: Terrasoft.ViewModelColumnType.VIRTUAL_COLUMN,
    initMethod: "initPropertySilent",
    doAutoSave: true
}
```

Insert a GRID_LAYOUT into inherited `EditorsContainer`. Insert an editor with the same name as the task parameter into that layout; its controlConfig uses the matching autocomplete name, e.g. `TextMapping`. MAPPING carries a binding object, not just a string. The base page initializes and persists it; do not replace its save pipeline with a custom request. Show outputs as explanations or downstream mapping sources, not editable inputs. Use schema localizable strings for production labels; the reference is English-only.

Set **Parameters edit page** to the client schema UId. In this build its exported field is FK11 (`ParametersEditPageSchemaV2UId`); do not use SysSchema.Id, a caption, or the older FK3. Keep the page accessible through package dependencies.

Parameter `Group` is optional design-time organization. Nonempty localized groups cause generated DesignModeGroup declarations; an empty value does not establish General. Group does not set the toolbox category, direction, or the custom panel layout. Author custom layout through the page diff; do not edit generated C# attributes.

### Icons and registration

Set SmallSvgImage, LargeSvgImage, TitleSvgImage, and color in task properties. Export package resources with the actual image bytes; an unreferenced SVG file is insufficient. DCM has separate image/page properties outside this verification.

Register by user-task schema UId in `SysProcessUserTask`, resolving the installed `SysSchema.UId`. Include an INSERT ... SELECT guarded by NOT EXISTS so repeated execution does not duplicate registrations. Place it after schema installation: the verified descriptors use InstallType=1, PostgreSQL DBEngineType=2, and SQL Server DBEngineType=0. Keep dialect scripts separate. An insert-only script preserves existing captions; a rename needs a deliberate upgrade. Do not use a manual database INSERT as a hidden deployment prerequisite.

Name a single-package archive exactly `<package descriptor Name>.gz`. Put build labels in the containing directory, not the archive basename. Native package installation can report `Install information is empty` before running SQL when the name differs. Renaming identical bytes may reuse the same cached extraction, because the installer cache key is content-based. Regenerate a correctly named archive with a deliberately advanced package version/stamp instead of treating that error as a SQL failure.

## Acceptance and recovery

- Require the toolbox entry, icon, custom panel, constant input, mapped input, and correct In/Out choices. Save, close, and reopen before claiming persistence.
- Run the saved process and assert every output on success and expected failure. A task's parameters are not automatically process-level parameters: expose and map each required input/output, then regenerate the typed process model. Format text's model must have Text, Prefix, FormattedText, IsError, and ErrorMessage equivalents.
- Unit tests subclass the real task to expose protected InternalExecute, set inherited inputs, and assert inherited outputs. Use DI substitutes to exercise handler errors; do not invoke protected methods through reflection.
- Install the package on a fresh non-FSM target. Check that registration is absent beforehand, installer SQL runs, one row per task exists afterward, the UI loads, and live tests pass. Reference v0.1.0 passed all ten live cases after a fresh PostgreSQL installation; v0.2.0 passed them after upgrading that retained instance, with installed direction readback and browser output-selector checks. No manual registration writes or linked workspace were required. The upgrade is not new clean-install evidence.
- Missing toolbox entry: verify SQL execution, schema UId, task classification, and a fresh designer session. Missing panel: verify FK11, parent metadata, and dependencies. Lost values: verify exact attribute names, MAPPING type, inherited save behavior, and saved process mappings.
- Missing generated parameter properties or an invalid InternalExecute override: obtain native generation and matching platform dependencies. A LocalizableString copied from the email task is not the same CLR type as plain Text.

Do not publish local credentials, platform binaries, or raw business data in a reference. Keep the original lab diary separate from the portable instructions. Refer to the pinned repository's validation record for the exact evidence and remaining boundaries.

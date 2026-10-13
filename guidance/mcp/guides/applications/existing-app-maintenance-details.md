clio MCP existing-app-maintenance-details guide — column-default round trip, merged reads, section-title languages, and the evidence behind the maintenance rules

A details article of the application guides, reached through `existing-app-maintenance`, which points here and is read first.
This article owns the field-by-field round trip of a column default (`default-value-config`) through `get-app-info` and `sync-schemas update-entity`, why merged column reads return unknown values, how section-title languages are preserved and the evidence behind the translation rules, and the full schema-reuse example. Split out of `existing-app-maintenance`, which keeps every rule, refusal and required order of steps; read that first. Nothing here overrides it.

Discover the target app
- `find-app` replaces a `list-apps` + per-app `list-app-sections` scan: it searches application name/code/description and section captions and returns the matching app(s) WITH their sections in a single call.
- Why sections are created one at a time, and what each `create-app-section` `error-class` means: `app-modeling-details` owns them; the handling is in `existing-app-maintenance`.

Inspect pages before editing
- `get-page-hierarchy` returns the full replacing-schema chain ordered root-first (by hierarchy level), with each schema's raw body, in a single round-trip. Pass `metadata-only` for a lightweight listing, or `offset`/`limit` to page a very large chain.
- Datasource captions keyed by the view-model attribute name: `$Resources.Strings.PDS_UsrStatus` for a `$PDS_UsrStatus` control, or `$Resources.Strings.Name` for `$Name`. The platform auto-provides the caption from the bound column.
- The proxy-attribute anti-pattern, spelled out: binding a control to `$UsrStatus` while declaring a separate proxy attribute whose underlying view model path is `PDS.UsrStatus`. A plain attribute name like `$UsrName` is itself valid when it is directly bound to a DS column; the anti-pattern is duplicating an already-DS-bound attribute under a different name.

Inspect schema before editing
- Why the merged read is the reliable one: custom columns are frequently added in a package other than the one that defines the schema. Merged column mode (`get-entity-schema-column-properties` without `package-name`) reads the compiled runtime schema, so it can find a column contributed by any package without probing package layers.
- Why three values come back `null` in merged mode: `RuntimeEntitySchemaRequest` does not expose `track-changes`, `localizable-text` or `do-not-control-integrity`. They are unknown, not `false`.
- A supplied `package-name` preserves the package-scoped designer read, which is why it returns the full designer metadata for that layer.

Preferred minimal mutations
- `sync-schemas` retries transient network faults (DNS / reset / timeout / gateway) per operation. Re-submitting a schema-only batch is safe because the convergent schema operations replay as `already-satisfied` / `reconciled`. `resume-plan.operations` holds the failed operation plus the operations that did not run, already in re-submittable shape; the plan also converts a post-create seed failure to a standalone operation, which is why it is the thing to resubmit when the batch includes `seed-data`.
- Column round trip, full shape: a column read from `get-app-info` as `{name, type (or data-value-type), reference-schema-name (or reference-schema), required (or is-required), caption, default-value-config?}` can be sent back inside an `update-operations` entry — just add the `action` verb (`modify`/`remove`).
- `default-value-config` per source: the per-source payload fields `existing-app-maintenance` lists are what you SEND. A read may also carry read-only enrichment the write path does not accept: `Settings` and `SystemValue` come back with `resolved-value-source` alongside `value-source`. Treat the read shape as non-exhaustive; an extra field is not drift.
- Removing a default: replace the whole object with `{"source": "None"}`. Flipping `source` on the object you hold fails, because the validator rejects `None` sent together with any surviving `value` / `value-source` / `sequence-prefix` / `sequence-number-of-chars`: "cannot set value, value-source, or sequence fields when default-value-config source is None".
- When the field is absent: it is absent whenever the column has no default, including when its stored source is `None`. Absence is only meaningful on a clio that returns the field at all — an older clio omits it for every column, defaulted or not — which is why `get-tool-contract` for `get-app-info` must name `default-value-config` in its `entities` output before absence is read as "no default".

Translations (other cultures)
- How section-title languages are preserved: when the `update-app-section` contract lists `caption-culture`, clio reads the section title and description in the non-default languages first, writes them back after the update, and re-saves the app package's `SysModule_<SectionCode>` data binding; the result lists the kept languages.
- Evidence boundary: measured on Creatio 10.2.254 / .NET Framework under an `en-US` profile, September 2026. Section-title writes under a non-`en-US` profile were not measured.

Verification
- Full schema-reuse example: if `get-app-info` or refreshed schema metadata already exposes `Support Case Knowledge Link` / `UsrSupportCaseKbLink`, a request to add a Related Knowledge detail on the Support Case form is a page mutation that reuses that schema. Do not create `UsrSupportCaseKnowledgeBase`.

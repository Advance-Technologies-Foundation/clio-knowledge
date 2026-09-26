clio MCP page-schema resources guide

Scope: use when a Freedom UI page change adds, references, or modifies localizable strings (captions, labels, titles, validator messages), and when a page is translated into another culture (TRANSLATING A PAGE INTO ANOTHER CULTURE below).

For schema ownership, culture-file setup, backend strict/fallback lookup, and localization testing,
MUST also read `localizable-values`. This guide remains the owner of Freedom UI binding and registration rules.

─────────────────────────────────────────────────────────────
WHEN TO USE A LOCALIZABLE STRING
─────────────────────────────────────────────────────────────

Author user-visible string values as localizable-string bindings, not inline literals. The rule covers any string-like property the runtime renders to the user (e.g. `label`, `caption`, `title`, `tooltip`, `placeholder`, `description`, button/tab/group captions, validator and dialog messages — non-exhaustive). Inline literals are fine for non-displayed values: type/schema/attribute names, enum-like state values (`labelPosition`, `size`, `direction`, …), and binding/converter expressions. Applies equally to web and mobile.

─────────────────────────────────────────────────────────────
ENFORCEMENT — HARD REJECT (not advisory)
─────────────────────────────────────────────────────────────

`update-page`, `sync-pages`, and `validate-page` REJECT a body that sets `label`, `caption`, `title`, `tooltip`, or `placeholder` to an inline string literal anywhere in `viewConfigDiff` (including nested child components) — with ONE inverse carve-out: `crt.ImageInput.tooltip` is literal-only (see REFERENCE SYNTAX below). The save fails with a diagnostic naming the node, the property, and the literal. To pass, author the value as `$Resources.Strings.<Key>` (or `#ResourceString(<Key>)#` for data-grid column captions and validator messages) and register the key where the platform does not auto-provide it. `description` is NOT hard-rejected (it also names non-display metadata), but localize it too when it is user-visible. A value that begins with `$` (any binding expression) or that is a non-string (e.g. `placeholder: false`) is never treated as a literal.

DANGLING BINDING — ALSO HARD REJECT (inserted widget/metric titles). A `title`/`caption`/`tooltip`/`placeholder` on a freshly INSERTED widget/container (`operation:"insert"`) that is bound as `#ResourceString(<Key>)#` is rejected when `<Key>` will NOT resolve — i.e. it is not passed in `resources`, is not a DS-bound attribute the platform auto-provides. ALWAYS pair a widget title with `resources: '{"IndicatorWidget_<slug>_title": "<the title text>"}'`. (A `merge` is not checked — a parent schema may already provide the caption.)

CREATION RULE — POPULATE THE DEFAULT-LANGUAGE VALUE
When you introduce a NEW user-visible string (a placeholder, a custom title, a button caption, …) you must seed its default-language text yourself: choose a `<Key>`, point the property at `$Resources.Strings.<Key>`, and register `{"<Key>": "<the exact text you would have typed inline>"}` through the `resources` parameter. That registered value becomes the default-language entry in the page's `localizableStrings`; without it the binding resolves to an empty caption. Example: a placeholder you would have written as `"name@firm.com"` becomes `placeholder: "$Resources.Strings.EmailField_placeholder"` plus `resources: '{"EmailField_placeholder": "name@firm.com"}'`.

─────────────────────────────────────────────────────────────
THE DECISION ALGORITHM
─────────────────────────────────────────────────────────────

Two independent decisions per localizable string — do not conflate them:

1. REFERENCE SYNTAX in the page body
   - Default: `$Resources.Strings.<ResourceKey>` (reactive binding; resolved by the Freedom UI engine for any key registered in the schema's `localizableStrings`).
   - Exception: validator params — use `#ResourceString(<Key>)#` there (see VALIDATOR PARAMS).
   - Inside `viewConfigDiff` string values (any user-visible string-like property — `label`, `caption`, `title`, `tooltip`, `placeholder`, `description`, etc. are examples, not an exhaustive list) both forms are interchangeable; prefer the binding form except where convention already established the macro form — notably data grid column captions in list pages and embedded grids like AttachmentList (`"#ResourceString(PDS_UsrName)#"`, `"#ResourceString(AttachmentListDS_Name)#"`).
   - Narrow exception — a few controls do NOT read a localizable resource for a given property, so a `$Resources.Strings.*` binding renders EMPTY there and the value MUST be an inline literal: `crt.ImageInput`'s `tooltip` (the control shows the raw text; see its `get-component-info` pitfall). Validation allows an inline literal for these specific (component, property) pairs; the localizable-binding rule is unchanged everywhere else. The exemption is resolved from the node's component type: when you patch only the `tooltip` with a stand-alone `merge` entry that carries no `type` (and no sibling `insert` in the same body establishes it), repeat `"type":"crt.ImageInput"` in that `merge`'s `values` so validation can resolve the exemption — otherwise the type is unknown and the literal is rejected.

2. REGISTER VIA `resources` PARAMETER? — depends on the target page, not the key name. Call `get-page` and inspect the merged `bundle.viewModelConfig.attributes.<Key>` (this is the effective runtime view, regardless of whether the attribute lives in the parent schema, inline `viewModelConfig`, or is added via `viewModelConfigDiff`). Then apply:
   - Exists AND has a DS binding (`modelConfig.path` → data source column) → platform auto-provides the caption from the entity column → **DO NOT register** (unless overriding the caption with a custom value).
   - Does NOT exist, OR exists without a DS binding → **MUST register** with an explicit value, or the binding will not resolve.

Same key name can require registration on one page and not on another. Prefixes (`PDS_`, `PageParameters_`, `MyDs_`, `AttachmentListDS_`, or none) are NOT signals — only the underlying DS binding matters.

KEY NAMING for data-bound controls — two distinct cases:
- AUTO-PROVIDED caption (DS-bound attribute, default caption, register nothing): the label key must be the VIEW-MODEL ATTRIBUTE NAME — the same attribute the control binds to — and that attribute must have a DS-bound `modelConfig.path`. The platform resolves the caption from the column the attribute points to. The attribute name is whatever the page declares (`PDS_UsrStatus`, `UsrName`, `Name123`, a designer hash-suffixed `PDS_UsrColumn2_r2s859x`) — all auto-provide as long as the label key equals the attribute name. Examples: attribute `$PDS_UsrStatus` (path `PDS.UsrStatus`) → label `$Resources.Strings.PDS_UsrStatus`; attribute `$Name123` (path `PDS.Name`) → label `$Resources.Strings.Name123`. The bare entity column code is NOT auto-provided unless it equals the attribute name — e.g. `$Resources.Strings.UsrStatus` renders blank for a `PDS_UsrStatus` attribute.
- EXPLICITLY REGISTERED caption (you pass a `resources` entry): the label key and the `resources` key must be identical; you control both. You may use the binding attribute name or any other key. Example: `resources: '{"PDS_UsrStatus": "Status"}'` paired with label `$Resources.Strings.PDS_UsrStatus`.
For `operation:"insert"`, update-page rejects an inserted field whose label is neither auto-provided (label key equal to the DS-bound binding attribute) nor explicitly registered — the bare column-code key form only works when it equals the attribute name or you register it.

PAIRED EXAMPLES — same field, opposite handling depending on the page
- Page A has `PDS_UsrStatus` bound to DS column `PDS.UsrStatus`, default caption:
  ✅ Bind `$Resources.Strings.PDS_UsrStatus` (the DS-bound attribute name) and pass nothing — platform auto-provides the caption from the entity column.
  ❌ Bind `$Resources.Strings.UsrStatus` (the bare column code) and pass nothing — NOT auto-provided (the key is not a DS-bound attribute name); on `operation:"insert"` this is rejected, and the label renders blank otherwise.
  ✅ `resources: '{"PDS_UsrStatus": "Custom status caption"}'` paired with label `$Resources.Strings.PDS_UsrStatus` ONLY to deliberately override with a custom caption under that key.
- Page B has `UsrLocalFlag` declared in `viewModelConfigDiff` with no DS binding:
  ✅ `resources: '{"UsrLocalFlag": "Local flag"}'` — required, or `$Resources.Strings.UsrLocalFlag` will not resolve.
- Page C does not declare `PDS_UsrStatus` at all:
  ✅ `resources: '{"PDS_UsrStatus": "Status"}'` — required; platform has nothing to auto-provide.

─────────────────────────────────────────────────────────────
DECISION TABLE
─────────────────────────────────────────────────────────────

| Scenario | Reference syntax | Pass `resources` param? |
| --- | --- | --- |
| Key is the name of a DS-bound attribute on the page (the control's binding attribute), default caption acceptable | `$Resources.Strings.<attributeName>` (the binding attribute, any prefix/suffix — NOT the bare column code) | NO — platform auto-provides |
| DS-bound attribute, overriding the caption (any key form) | `$Resources.Strings.<Key>` | YES — register the same key with the override value |
| Key has NO matching DS-bound attribute (custom tab/group title, button caption, custom grid column, free-form `viewModelConfigDiff` attribute) | `$Resources.Strings.<Key>` (or `#ResourceString(<Key>)#` for grid column captions by convention) | YES — must register with an explicit value |
| Validator error message | `#ResourceString(<Key>)#` (macro form required) | YES — always register with an explicit value |
| Inherited caption from parent schema, simple non-localizable strings, converter display values | Inherited / inline string / N/A | NO |

─────────────────────────────────────────────────────────────
HOW TO PASS, AND PRESERVATION
─────────────────────────────────────────────────────────────

Pass the `resources` parameter as a JSON object string of `Key → display string` pairs; values must be plain strings (no nesting/arrays) and must be explicit:
  `resources: '{"UsrDetailsTab_caption": "Details", "UsrSave_caption": "Save record"}'`

`update-page` / `sync-pages` preserve omitted `localizableStrings` entries (platform entries like `SaveButton`, `CancelButton`, `GeneralInfoTab_caption` included). Check the installed tool contract with `get-tool-contract`: versions whose `resources` description says **updates supplied en-US values** can update an existing key's English value while preserving its declaration identity and other cultures. Earlier contracts say **Additions only** and silently ignore a supplied value for an existing key; use the native designer for those updates or upgrade to a build carrying the repair for [Clio #1614](https://github.com/Advance-Technologies-Foundation/clio/issues/1614). Never infer that a changed caption landed from `success:true` or `resourcesRegistered`: that count covers new keys only. Read back the value and verify the rendered page.

The `update-page` / `sync-pages` key/value input targets `en-US`, not the caller's current culture. Other cultures are written with `localize-page` (TRANSLATING A PAGE INTO ANOTHER CULTURE below), never by putting translated text under `en-US`. Preservation is not permission to skip the registration check: confirm no DS-bound view model attribute with that name already provides the caption before adding it.

**Capture before push.** A successful designer save does not synchronize an independent workspace. Stale `metadata.json` or culture resource XML can revert the declaration or text on the next `push-workspace`. Apply the create/capture/review/push rule in `app-modeling` to resource edits too: preserve local edits, capture the affected package with `restore-workspace`, and review both B2 declarations and culture XML before pushing. A body-only `get-page` file or a standalone `export-schema` bundle is not that workspace capture. In linked FSM workspaces the native designer can already write those files through the link; inspect the source diff and follow that workspace's FSM instructions instead of blindly pulling over local work. `localizable-values` owns schema metadata and culture-file details.

Evidence boundary: the Clio #1614 investigation reproduced ignored existing values on Creatio 10.1.585.0 / .NET 8 / PostgreSQL and confirmed native designer writes to linked FSM metadata/XML. The updated reference example remains schema-owned; no second B2 serializer is required in Clio.

─────────────────────────────────────────────────────────────
TRANSLATING A PAGE INTO ANOTHER CULTURE
─────────────────────────────────────────────────────────────

This guide owns the page-translation workflow and the `localize-page` tool. Availability: a clio whose `get-tool-contract` index lists `localize-page`. It is a long-tail tool, called through `clio-run` (`core-rules` owns that rule). On an earlier clio there is no per-culture page write: use the native localization workflow (page designer) for other cultures.

`localize-page` writes ONE culture of ONE page per call:
- `schema-name` — the page.
- `culture` — the target culture in canonical `ll-CC` form (`es-ES`); matched case-insensitively and stored in the environment's spelling. It is for ADDITIONAL cultures only: `culture: "en-US"` is REJECTED with an error pointing to `update-page` `resources`, which registers and changes the default-culture text.
- `resources` — JSON-object string `Key → text in that culture`, e.g. `'{"UsrName_caption":"Nombre"}'`. The valid keys are exactly the set `get-page` shows under `bundle.resources.strings`, including keys inherited from the template hierarchy (an inherited key gets a page-level override holding only that culture; its `en-US` text keeps coming from the parent).
- `caption` — the page title in that culture.
- `output-directory` — pass the same directory you gave `get-page` `output-directory`, so the tool finds and refreshes that `.clio-pages/<schema>/meta.json` conflict baseline. It does not change where the page is saved.
- With neither `resources` nor `caption` the call is REPORT-ONLY: nothing is saved and `coverage` returns `keys`, `translated`, `missing`, `sameAsDefault`, `captionSameAsDefault` over the same key set as `get-page`.

What the tool guarantees:
- Every other culture of every key, and the default culture, is left unchanged. A second culture is another call and keeps the first.
- Idempotent: a re-run with the same values stores nothing and reports `saved:false`.
- An unknown key fails the WHOLE call and nothing is saved; the error lists the candidate keys. `localize-page` never registers a key: register it with `update-page` (default culture first), then translate it.
- A culture that the environment's Languages section does not contain fails before any write; the error names the Languages section and lists the available cultures. An inactive culture is written and the result warns to activate it in the Languages section. `localizable-values` owns these culture rules.
- After a save the tool reads the page back and fails when a written value was not stored. The `get-page` baseline found through `output-directory` is refreshed, so the next `update-page` is not refused as an external modification. If that baseline is already stale (the page changed after your `get-page`), the write still succeeds, the baseline is left untouched, and a warning says the next `update-page` will report the conflict: run `get-page` again before editing the body.
- `sameAsDefault` lists keys whose value equals the `en-US` value. Right after `create-page` the page title holds the English text in every culture, so "present" does not mean "translated". Review these keys; a word can legitimately be the same in two languages, so it is never an error.

Not page resources — translate them elsewhere:
- A DS-bound field label (the auto-provided case in THE DECISION ALGORITHM) is the entity column caption, not a page key; `localize-page` fails on it. Translate the column with `title-localizations` on the entity tools — `existing-app-maintenance` owns that rule.
- The section title in the application is `update-app-section` `caption` + `caption-culture` — also owned by `existing-app-maintenance`.

Workflow for "translate this page / this app into Spanish":
1. `get-page` the page.
2. `localize-page` report-only with `culture: "es-ES"`; read `coverage.missing` and `coverage.sameAsDefault`.
3. Translate those keys and the page title.
4. `localize-page` with `resources` (and `caption`); expect `saved:true`. A report-only re-run must show `missing` empty.
5. Translate DS-bound field labels and list columns through the entity column `title-localizations`.
6. Translate the section title with `update-app-section` `caption-culture`.
7. If the result warned the culture is inactive, activate it in the Languages section, then run a FULL configuration compile (`clio compile-configuration --all`; MCP `compile-creatio` without `package-name`). Until that compile the UI does not load in that culture at all (its client resource files answer 404); no restart is needed. The compile confirmation rule in `core-rules` applies: warn the user and ask before compiling. Then switch the user profile language to the culture and verify the rendered page (verification-in-browser preference in `core-rules`).
Repeat steps 1-4 for every page and every culture: each call covers one page and one culture.

After changing an `en-US` value with `update-page`, the other cultures are NOT updated and nothing marks them stale: re-run `localize-page` for every translated culture of that key.

Capture before push (above) applies to `localize-page` too: a server save updates neither workspace metadata nor culture XML.

Evidence boundary: the storage behavior behind this workflow (inherited-key overrides, own-key full-list writes, inactive and absent cultures) was measured on Creatio 10.2.254 / .NET Framework for ENG-90576. Mobile page schemas were not measured; the readback makes an unsupported case fail instead of reporting success. The full-compile requirement after activating a culture was observed on the same stand (the `es-ES` resource file answered 404 until `compile-configuration --all`).

─────────────────────────────────────────────────────────────
VALIDATOR PARAMS — special rule
─────────────────────────────────────────────────────────────

Validator params (inside `viewModelConfigDiff` attribute validators) are not processed by the reactive binding engine. `$Resources.Strings.*` is rejected by clio validation here — use `#ResourceString(KeyName)#`.

✅ `"params": { "message": "#ResourceString(UsrMaxLength_Message)#" }`
❌ `"params": { "message": "$Resources.Strings.UsrMaxLength_Message" }`

See `page-schema-validators` for the full validator authoring guide.

─────────────────────────────────────────────────────────────
COMMON MISTAKES
─────────────────────────────────────────────────────────────

1. Passing `resources` for a key without checking the page first — if a DS-bound attribute with that exact name already exists, the platform auto-provides the caption and the entry is unnecessary. The key name alone cannot tell you; inspect `bundle.viewModelConfig.attributes.<Key>` from `get-page` (merged form — reflects parent, inline `viewModelConfig`, and `viewModelConfigDiff` additions in one view).
2. Treating a prefix (e.g. `PDS_`) as a signal of auto-provisioning — only the DS binding on the attribute matters. Manually authored attributes use plain names (`UsrName`); page-parameter attributes use `PageParameters_`; other data sources use their own prefixes (`MyDs_`, `AttachmentListDS_`).
3. Inventing data-source resource keys from column names — the key must match the view model attribute identifier from the binding, including any designer-generated hash suffix (e.g. `PDS_UsrColumn2_r2s859x`).
4. Using `$Resources.Strings.*` in validator params — rejected by clio validation; use `#ResourceString(KeyName)#`.
5. Re-registering inherited captions from a parent schema — already registered; the entry is unnecessary (though harmless).
6. Hardcoding a user-visible string as an inline literal — bind it via `$Resources.Strings.<Key>` (or `#ResourceString(<Key>)#` for validator params).

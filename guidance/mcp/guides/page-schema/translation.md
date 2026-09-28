clio MCP page translation guide

Scope: use when a Freedom UI page, or an app's pages, must get another culture (language). This guide owns the page-translation workflow and the `localize-page` tool. `page-schema-resources` owns registering keys and the default-culture (`en-US`) text; `localizable-values` owns the culture rules (absent / inactive culture, `ll-CC` names); `existing-app-maintenance` owns object, column and section titles in another culture.

Availability: a clio whose `get-tool-contract` index lists `localize-page`. It is a long-tail tool, called through `clio-run` (`core-rules` owns that rule). On an earlier clio there is no per-culture page write: use the native localization workflow (page designer) for other cultures.

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
- The culture is checked before any write as `localizable-values` describes (an absent culture fails and names the Languages section; an inactive one is written with a warning).
- After a save the tool reads the page back and fails when a written value was not stored. The `get-page` baseline found through `output-directory` is refreshed, so the next `update-page` is not refused as an external modification. If that baseline is already stale (the page changed after your `get-page`), the write still succeeds, the baseline is left untouched, and a warning says the next `update-page` will report the conflict: run `get-page` again before editing the body.
- `sameAsDefault` lists keys whose value equals the `en-US` value. Right after `create-page` the page title holds the English text in every culture, so "present" does not mean "translated". Review these keys; a word can legitimately be the same in two languages, so it is never an error.
- The tool does NOT reject an empty string: `""` is stored as the value. Omit a key you cannot translate; never send an empty string.

Not page resources — translate them elsewhere:
- A data-source-bound field label (the auto-provided case in `page-schema-resources`) is the entity column title, not a page key; `localize-page` fails on it. So is a list column WITHOUT its own caption resource. A list column with its own caption resource is a page key: steps 2-4 cover it.
- The section title in the application is `update-app-section` `caption-culture`, owned by `existing-app-maintenance`.

Workflow for "translate this page / this app into Spanish":
1. `get-page` the page.
2. `localize-page` report-only with `culture: "es-ES"`; read `coverage.missing` and `coverage.sameAsDefault`.
3. Translate those keys and the page title.
4. `localize-page` with `resources` (and `caption`); expect `saved:true`. A report-only re-run must show `missing` empty, except keys you deliberately omitted.
5. Titles outside the page change every page, list and app that shows them. Translate a column title only for a data-source-bound label with no page key, on an object created in the app's own package (not a replacing schema of another package's object), with the entity `title-localizations` (`existing-app-maintenance`). For a page-only request, and for a column of an object from another package (usually already translated by the platform's language pack), list those labels and ASK the user before editing them.
6. Translate the section title only when the request covers the app or the section, or the user agrees: `update-app-section` `caption-culture`, after the `get-tool-contract` check `existing-app-maintenance` makes mandatory.
7. A full compile is needed whenever the culture is inactive (the result warned) or was activated with no full configuration compile since — even without a warning; the observable sign is that `0/conf/content/resources/<culture>/ConfigurationConstantsResources.js` answers 404 and the shell does not load in that culture. Activate the culture in the Languages section if needed, then run a FULL configuration compile (`clio compile-configuration --all`; MCP `compile-creatio` without `package-name`). This is the compile case "a culture was just activated"; on a clio whose `compile-creatio` description does not list that case yet, it is an explicit exception to its "Call only when" list. Until that compile the UI does not load in that culture at all. On the measured stand the culture loaded after the full compile without a restart; if captions still render in the old culture, the DLL activation and restart rule in `core-rules` applies. The compile confirmation rule in `core-rules` applies: warn the user and ask before compiling. Then switch the user profile language to the culture and verify the rendered page (verification-in-browser preference in `core-rules`).
Repeat steps 1-4 for every page and every culture: each call covers one page and one culture.

After changing an `en-US` value with `update-page`, the other cultures are NOT updated and nothing marks them stale: re-run `localize-page` for every translated culture of that key.

The capture-before-push rule in `page-schema-resources` applies to `localize-page` too: a server save updates neither workspace metadata nor culture XML.

Evidence boundary: the storage behavior behind this workflow (inherited-key overrides, own-key full-list writes, inactive and absent cultures) was measured on Creatio 10.2.254 / .NET Framework for ENG-90576. Mobile page schemas were not measured; the readback makes an unsupported case fail instead of reporting success. The full-compile requirement after activating a culture was observed on the same stand (the `es-ES` resource file answered 404 until `compile-configuration --all`). Empty values: `localize-page` rejects only `null` (clio `LocalizePageCommand.TryParseResources`); how Creatio renders an empty or an omitted culture value was not measured.

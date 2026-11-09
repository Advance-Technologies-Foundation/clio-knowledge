clio MCP page modal size guide

Owns the size of a Freedom UI modal window: where it lives, how to read and change it, and what never changes it. Requires a clio that provides the `get-page-modal-size` and `set-page-modal-size` tools; if they are not available, say the modal window size cannot be changed and do not edit the page body.

WHERE THE SIZE LIVES
- A modal window's size is NOT in the page body. It is `modalSettings.size` in the page's `AppearanceSettings` add-on (`<Page>AppearanceSettings`). The Page Designer writes the same add-on (Page settings -> Appearance -> Modal window size).
- `layoutConfig.width` of the `Main` container is NOT the window size: it is inherited from `BaseMiniPageTemplate` and changing it leaves the window as it was. Inner container widths and CSS do not change the window size either.
- A modal page is a page that inherits `BaseMiniPageTemplate`. A form page, a list page or any other page has no modal window size: the tools refuse it. Tell the user the page is not a modal window and change nothing.

MUST
- Change a modal window's size ONLY with `set-page-modal-size`. MUST NOT edit `layoutConfig.width` of `Main`, inner container widths, or CSS to make a modal window bigger or smaller — `update-page` reports success and the window does not change.
- Read the current size with `get-page-modal-size` first and name it to the user (`size`, or `effectiveSize` when no size is set) together with `sourcePackageName`.
- An exact size ("make it large", "XL") is applied as given. A vague request ("wider", "bigger", "too small") MUST NOT be resolved by picking a size: name the current size, offer the available sizes, and write only the one the user chooses.

SIZES
| Size | Page Designer | Stored `size` |
| --- | --- | --- |
| `small` | S | 1 |
| `medium` | M | 2 |
| `large` | L | 3 |
| `extra-large` | XL | 4 |

`set-page-modal-size` accepts the size names and the designer captions (case-insensitive) and refuses any other value with `availableSizes`; name those to the user. A page with no size opens `small`.

FIND THE MODAL PAGE
- The user names the page: use it.
- The user names the window by its title or by the button that opens it: `get-page` the page that holds the button, find the button's `clicked` request `crt.OpenPageRequest`, and take its `schemaName` — that is the modal page.

PACKAGE
- `package-name` is the editable package the add-on is written to (the app's package), the same rule as for a page save. Both tools take it: call `get-page-modal-size` with the same `package-name` that `set-page-modal-size` will write to, otherwise the read can miss an override above that package. For a vendor page (its add-on in a locked package) write to the app's package: the tool creates the page's add-on layer there and keeps every other add-on field.
- `get-page-modal-size` returns `warning` when a package above `package-name` overrides the size; then a write to `package-name` would not change the window. Tell the user before writing. `set-page-modal-size` refuses that write before saving anything ("Nothing was changed."): name the overriding package to the user instead of retrying with the same `package-name`.
- `get-page-modal-size` also returns `warning` when the stored add-on settings are not a JSON object; `set-page-modal-size` refuses to write such a page ("Nothing was changed."). Report it to the user; do not repair the add-on by editing the page body.
- `set-page-modal-size` returns `warning` when the window still opens with another size after the write.

RESULT
`set-page-modal-size` returns `previousSize`, `previousSourcePackageName` and `size`: name the old size, the new size and the package to the user.

Evidence: measured on 2026-11-06 on Creatio 10.2.1027 (Sales Enterprise, Marketing, Service Enterprise; MSSQL) — a size written with `set-page-modal-size` is returned by the runtime `AppearanceSettingsService` at once, for a page without its own add-on (created in the app's package) and for a vendor page (`EmailMiniPage`, add-on in `CrtUIv2`). Runtime only: on that stand the Page Designer's size panel showed its default M after reopening, even for a size the designer itself had saved, so the designer showing the written size as selected is not confirmed.

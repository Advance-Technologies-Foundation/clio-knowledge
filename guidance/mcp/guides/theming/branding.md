clio MCP branding guide

Scope
Use this guide to brand a Creatio environment:
- Apply the product logos and the browser-tab favicon — see "Logos & favicon".
- Apply a shell background image — see "Background".
For brand colours, fonts, and custom themes read the theming guide (`get-guidance name=theming`); do not improvise theme changes from here.

Constraints
- Every branding asset here is an environment-wide (All-Users) setting, not per-user: applying one changes the look for every user after a page refresh. The favicon needs more: the user must sign out and back in, and an already-open browser tab may keep the old icon until it is closed and reopened. Tell them so whenever it changes.
- Branding writes require the `CanCustomizeBranding` license. Check up front with `check-theming-access` (`canCustomizeBranding` in the response; the check itself runs on any Creatio version, including ones below the 10.0.0 theming floor); when it is false, stop — do not upload or write anything — and tell the user something like: "Custom branding is not available for the Growth plan. Upgrade your subscription to Enterprise or Unlimited."
- A successful access check proves the branding licence/rights only; it does not probe every transport used by the asset tools. `set-logo` writes Binary system settings, while `upload-image` and the file form of `set-background-image` authenticate to the image API. One path may therefore work while the other is blocked by authentication, proxy, or CSRF configuration. Relay the failing tool's exact error and diagnose that path; do not reinterpret an image-API failure as a licensing failure or use a successful logo write as proof that background upload must work.
- Applying a logo cannot be automatically reverted by clio; warn the user before writing one.

Target package (data delivery)
- `set-logo` and `set-background-image` do not only change this environment: each also binds the applied branding into a package as Creatio data bindings (their `package` argument), so installing that package elsewhere reproduces the branding instead of leaving it behind here.
- Resolve the one target package for the whole branding operation BEFORE you present what will be done: call `get-target-package` with `package` when the user named one, and without it otherwise — then the package the environment's `CurrentPackageId` system setting names is resolved for you. It answers with `package-name`, and it also verifies the package can receive the data.
- `success: false` with `resolutionFailed: true` means the environment answered and there is no usable target: the name does not exist, the package is locked, or no current package is set. Relay that reason and ask the user which package to use — never pick one yourself. With `resolutionFailed: false` the environment could not be asked at all: retry, and do not tell the user there is no target package.
- When you finalize what will be done (theme, logos, favicon, background), tell the user which package the new data will be added to — name it, for example: "The theme, logos, favicon and background will be added to package <X>." Use the `package-name` the probe returned, never a raw id, and never guess it.
- Pass that same package to `create-theme` (its package argument) and to the `package` argument of `set-logo` / `set-background-image`, so the theme and every asset land in the one package you named instead of drifting apart.
- The apply comes first and the packaging second, deliberately: when the target package turns out to be unusable, the branding stays applied on this environment and the tool fails naming the package problem. That is not a rollback prompt — resolve the package (unlock it, or pick another) and re-run the same call to deliver the same branding into it. Resolving the target with `get-target-package` up front is what keeps you out of this state.
- Read each tool's result back to the user: it names the package and reports what was bound. The `warnings` entries are the only place a delivery gap is reported — relay them; a run with warnings still succeeded, but each entry means the package ships less than the user may expect.
- Re-running the same tool after any later branding change refreshes both the environment and the packaged snapshot; the bindings are created when missing and updated in place when present. Removing or replacing an asset drops the bindings whose source row is gone (reported in `warnings`).

Calling the tools
- Wrap tool arguments under the top-level `args` JSON object exactly as advertised by the tool schema (for example `{"args": {"environment-name": "...", "file": "..."}}`). Do not flatten or rename canonical fields.

Logos & favicon
One call: `set-logo`. Pass `logo` with one local image file to brand every slot at once; pass a slot argument to give that slot its own file (it overrides `logo` for that slot). At least one of these is required:
- `logo` — every slot below.
- `login-logo` — login page (light background).
- `menu-logo` — main menu / shell header (light background).
- `configuration-logo` — configuration section (light background).
- `dark-logo` — the Freedom UI top panel, which is a DARK surface. Pass the brand's light/white logo variant here. If the brand has no light variant, ask the user before reusing the main logo — a logo drawn for a light background is often unreadable on the dark panel.
- `favicon` — the browser tab. Whenever you apply logos, pass a square icon here in the same call: no separate question, and never render it in the conversation. Use the brand's own favicon when it has one; derive one from the logo only when it does not. It is never taken from `logo`, which is usually wider than it is tall, and clio uploads the file as it is. ICO, PNG and SVG are the safest formats.
Typical call: `logo` with the main file, `dark-logo` with the light variant, `favicon` with the derived icon. The tool reads the file rules, size cap, and file-security policy from `docs://mcp/guides/sys-settings`, suppresses the stock splash-screen logo automatically, turns on the favicon's `UseFaviconFromSysSettings` gate — without it the platform keeps the stock icon — and binds what it applied into the target package. Only what this run applied is bound, so a slot nobody branded stays out of the package.
Read the result before retrying. A refused image returns `success: false` naming it; the accepted images are already written and bound, so re-run only what was refused instead of the whole set. The run also fails when the favicon's gate can neither be turned on nor already reads as on, because the icon is inert without it.
The `CrtAppToolbarLogoUnderlayColor` system setting (text) paints a backing color under the top-panel logo — write it with `update-sys-setting` only when the user explicitly asks; it stays on this environment and does not travel with the package.

Background
Call `set-background-image` with the local image file path (`file`); it uploads the file and makes it the shell background, replacing the currently configured one. To re-apply an image that was already uploaded with `upload-image`, pass its `image-id` instead of `file` (exactly one of the two).
So the new background is actually visible, the tool also turns off the panel's own icon background for all users (the `UsePanelIconBackground` feature) — while it is on it can cover the shell background. Pass `keep-icon-background` = true only when the user explicitly wants the panel icon background kept. The turn-off is best-effort — a failure is reported as a warning, not a failure of the apply — and the off-state is bound with the background so the install target inherits it. The off-state is bound only when the All-Users state row is verified to read as off on the environment; when it was never turned off here, or it is still on (`keep-icon-background`, or the turn-off failed), the binding is left out instead — relay that `warnings` entry, because it means the package will not turn the feature off on the install target and the background can stay hidden there.

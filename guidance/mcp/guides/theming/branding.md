clio MCP branding guide

Scope
Use this guide to brand a Creatio environment:
- Apply the product logos and the browser-tab favicon — see "Logos & favicon".
- Set, change, or remove a background — an image or a color on a theme, or an image for the whole site — see "Background".
For brand colours, fonts, and custom themes read the theming guide (`get-guidance name=theming`); do not improvise theme changes from here.

Constraints
- The logos, the favicon and the site background are environment-wide (All-Users) settings, not per-user: applying one changes the look for every user after a page refresh. A theme background reaches only the accounts on that theme (see "Background"). The favicon needs more: the user must sign out and back in, and an already-open browser tab may keep the old icon until it is closed and reopened. Tell them so whenever it changes.
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
A background is an image or a color at one of two levels: a theme's own, or the site's. The first row that applies to an account is what it sees:

| Order | Background | Set with | Applies when |
|---|---|---|---|
| 1 | Accessibility color | the personal `UseAccessibleDesktopColor` system setting; no branding tool writes it | the setting is on |
| 2 | Theme background: an image or a color | `set-background-image` or `set-background-color` with `theme` | the account's current theme declares one, its image loads, and the `DisableThemeBackground` feature is off for the account |
| 3 | Site background | `set-background-image` without `theme` (the `CrtBackgroundConfig` system setting) | rows 1 and 2 do not apply |
| 4 | Bundled default image | — | no site background is set |

- The theme level needs Creatio 10.2.96 or later and a clio whose `get-tool-contract` with `tool-names: ["set-background-color", "remove-background"]` returns both tools; that clio also takes `theme` in `set-background-image` and returns `background` from `get-theme` / `list-themes`. When either is missing, see "Background: no theme level".
- A new or removed background shows after a page refresh.
- Each tool's arguments and errors: `get-tool-contract`.

Background: tools
- `set-background-image` takes the local image file path (`file`), which it uploads, or the `image-id` of an image already uploaded with `upload-image` — exactly one of the two. With `theme` the image becomes that theme's background; without it, the site background. It replaces the background already set at that level.
- `set-background-color` takes `color` (`#RGB`, `#RRGGBB` or `#RRGGBBAA`) and `theme`, and replaces that theme's background with the color. A site color is not supported.
- `remove-background` takes `theme`; the theme's accounts see the site background again. A repeat run succeeds.
- `theme` is the theme's `id` from `create-theme` or `list-themes`. With `file` the upload runs before the theme is checked, so a wrong id leaves the uploaded image behind.
- `create-theme` and `update-theme` take no background, and `update-theme` keeps the stored one; only the three tools above change it.

Background: panel icon
So the new background is actually visible, `set-background-image` and `set-background-color` also turn off the panel's own icon background for all users (the `UsePanelIconBackground` feature) — while it is on it can cover the background. The feature is environment-wide, so a theme background turns it off on every theme. Pass `keep-icon-background` = true only when the user explicitly wants the panel icon background kept. The turn-off is best-effort — a failure is reported as a warning, not a failure of the apply. Only `set-background-image` binds the off-state with the background, so the install target inherits it. The off-state is bound only when the All-Users state row is verified to read as off on the environment; when it was never turned off here, or it is still on (`keep-icon-background`, or the turn-off failed), the binding is left out instead — relay that `warnings` entry, because it means the package will not turn the feature off on the install target and the background can stay hidden there.

Background: in a branding run
- Put the background into the theme the run creates: after `create-theme`, call `set-background-image` with `theme` set to the `id` it returned.
- For the site background instead, see "Background: site".

Background: an existing theme
- Read its current background with `get-theme`: `background` is `{ "mode": "Image", "imageId": "<id>" }` or `{ "mode": "Color", "color": "<hex>" }`, and absent when the theme declares none, so its accounts see the site background. `list-themes` returns the same `background` for each theme.
- Set an image with `set-background-image`, a color with `set-background-color`, or remove it with `remove-background`, each with `theme` (see "Background: tools").

Background: site
Call `set-background-image` without `theme` only when the user asks for a background for the whole site: every account whose theme declares no background sees it.

Background: package
- `set-background-image` with `theme` binds the image and its gallery membership into `package` under that theme's own folders, so it never replaces the site background's bindings; the theme itself carries the `background` that names the image. Pass the package resolved under "Target package" — in a branding run, the one the theme was created in.
- `set-background-color` and `remove-background` bind nothing; a color travels inside the theme.
- `remove-background` and `delete-theme` leave the image bindings in the package, which keeps shipping the image. Tell the user so when you remove a theme's image or delete a theme that had one.

Background: no theme level
- No theme can carry a background when this clio lacks the theme-level tools (the user can update clio) or the environment is below the Creatio floor (the tool answers `version-too-old`).
- Tell the user, and set the site background (see "Background: site") instead only after the user agrees, even when the tool's error suggests it.

Background: not showing
When the user does not see the background they expect, find the row of the table that wins for their account and tell them which:
- Their current theme is another one, or declares no background: the site background shows.
- `UseAccessibleDesktopColor` is on: the accessibility color replaces every background.
- `DisableThemeBackground` is on for them: the site background shows. A change to the feature shows on the next application load.
- The theme's image no longer loads (its `SysImage` was deleted): the site background shows, and `get-theme` still reports the `imageId`. Set a new image.
- The panel icon background is on: it can cover the background (see "Background: panel icon").
- Never change `UseAccessibleDesktopColor` or `DisableThemeBackground` to make a background show.

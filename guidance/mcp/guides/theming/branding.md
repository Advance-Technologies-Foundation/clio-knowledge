clio MCP branding guide

Scope
Use this guide to brand a Creatio environment:
- Apply the product logos — see "Logos".
- Apply a shell background image — see "Background".
- Replace the browser-tab favicon — see "Favicon".
For brand colours, fonts, and custom themes read the theming guide (`get-guidance name=theming`); do not improvise theme changes from here.

Constraints
- Both branding assets are environment-wide (All-Users) settings, not per-user: applying them changes the look for every user after a page refresh.
- Branding writes require the `CanCustomizeBranding` license. Check up front with `check-theming-access` (`canCustomizeBranding` in the response); when it is false, stop — do not upload or write anything — and tell the user something like: "Custom branding is not available for the Growth plan. Upgrade your subscription to Enterprise or Unlimited."
- Applying a logo cannot be automatically reverted by clio; warn the user before writing one.

Calling the tools
- Wrap tool arguments under the top-level `args` JSON object exactly as advertised by the tool schema (for example `{"args": {"environment-name": "...", "file": "..."}}`). Do not flatten or rename canonical fields.

Logos
Four Binary system settings, one per product slot; write each from a local file with `update-sys-setting` + `value-file-path` (never inline the bytes — see `docs://mcp/guides/sys-settings` for the Binary rules, size cap, and file-security policy):
- `LogoImage` — login page (white background).
- `MenuLogoImage` — main menu / shell header (white background).
- `ConfigurationPageLogoImage` — configuration section (white background).
- `CrtAppToolbarLogo` — top panel (dark surface; use the white/light logo variant when one exists, otherwise the main logo).
After applying custom logos, set the `HideSplashScreenLogoImage` system setting (Boolean) to true with `update-sys-setting` so the stock splash logo does not flash during load; leave it untouched when no logos were applied. The `CrtAppToolbarLogoUnderlayColor` system setting (text) paints a backing color under the top-panel logo — write it with `update-sys-setting` only when the user explicitly asks.

Background
Call `set-background-image` with the local image file path (`file`); it uploads the file and makes it the shell background, replacing the currently configured one. To re-apply an image that was already uploaded with `upload-image`, pass its `image-id` instead of `file` (exactly one of the two).

Favicon
The browser-tab icon, driven by two system settings written with `update-sys-setting` (see `docs://mcp/guides/sys-settings` for the Binary rules, size cap, and file-security policy):
- `FaviconImage` (Binary) — the icon file (a small square SVG, PNG, or ICO); write it from a local file with `value-file-path`, never inline the bytes.
- `UseFaviconFromSysSettings` (Boolean) — set it to true, otherwise the platform ignores `FaviconImage` and keeps the stock Creatio icon.
Apply order: write `FaviconImage`, then set `UseFaviconFromSysSettings` to true. A favicon change is not visible on an open session — the user must sign out and back in, and an already-open browser tab may keep the old icon until it is closed and reopened, because browsers cache tab icons aggressively; tell the user this whenever the favicon changes.
# Creatio Freedom IFrame Section

## Overview

Use this skill when you need a Creatio section that works as a shell for external UI (for example React build under package `Files/ui`) and should contain only one `crt.IFrame`.

This skill is optimized for composable applications like Radar.

## Target Result

1. A section is visible in Creatio navigation.
2. Section page is based on `BlankPageTemplate`.
3. Page contains only one `crt.IFrame` with runtime-aware URL:
   - net8: `/rest/<UiService>/ui`
   - .NET Framework: `/0/rest/<UiService>/ui`
4. IFrame runs without sandbox restrictions required for same-origin assets:
   - `isSandbox: false`
   - `sandbox: null`
5. Section registration data is valid (`SysModule`, `SysModuleEntity`, `SysModuleInWorkplace`).

## Required Inputs

- `PACKAGE_NAME` (for example `Radar`)
- `SECTION_CODE` (for example `RADAR`)
- `SECTION_SCHEMA_NAME` (for example `RADAR_ListPage`)
- `UI_URL`
  - net8 example: `/rest/RadarUiService/ui`
  - .NET Framework example: `/0/rest/RadarUiService/ui`
- `WORKPLACE_ID` or workplace lookup key (usually Studio for dev)
- Operation code for access policy from module spec (do not invent)

If access mapping is missing in spec/doc, ask developer before implementing endpoint changes.

## File Locations

- Section schema JS:
  - `packages/<PACKAGE_NAME>/Schemas/<SECTION_SCHEMA_NAME>/<SECTION_SCHEMA_NAME>.js`
- Section registration data:
  - `packages/<PACKAGE_NAME>/Data/SysModule_<SECTION_CODE>/data.json`
  - `packages/<PACKAGE_NAME>/Data/SysModuleEntity_<SECTION_CODE>/data.json`
  - `packages/<PACKAGE_NAME>/Data/SysModuleInWorkplace_<SECTION_CODE>/data.json`

## Implementation Steps

1. Create/Update section page schema (`<SECTION_SCHEMA_NAME>.js`):
- Parent template must be `BlankPageTemplate`.
- Add single `crt.GridContainer` + single `crt.IFrame`.
- No `PDS`, no list datasets, no extra visual controls.

2. Configure iframe node:
- `type: "crt.IFrame"`
- `isSandbox: false`
- `sandbox: null`
- `contentType: "url"`
- `urlContent: "<UI_URL>"`

3. Register section in package data:
- `SysModule`: code/caption/section schema binding.
- `SysModuleEntity`: connect section module to target entity (or app-specific placeholder mapping).
- `SysModuleInWorkplace`: put section into target workplace.

4. Data integrity rules:
- Never store literal string `"null"` in image/icon lookup fields.
- If old `SysModuleInWorkplace` row has zero GUID links, create a new row with new `Id`.

5. Build/deploy loop (repo-specific):
- If UI changed: `cd src/ui/<UI_APP> && npm run build:creatio`
- C# build: `dotnet build MainSolution.slnx -c dev-n8`
- Install package: `clio push-pkg packages/<PACKAGE_NAME> -e "$CLIO_ENV" --Safe false`

## Mandatory Validation

### A. HTTP checks

1. Runtime-correct UI URL returns `200` for authenticated session:
   - net8: `GET /rest/<UiService>/ui`
   - .NET Framework: `GET /0/rest/<UiService>/ui`
2. Referenced JS/CSS assets from returned HTML return `200`.

### B. OData checks (section metadata)

Verify rows exist and links are non-empty:

- `SysModule` row for `<SECTION_CODE>`
- `SysModuleInWorkplace` row for module/workplace
- `SysModuleInWorkplace.SysWorkplaceId != 00000000-0000-0000-0000-000000000000`
- `SysModuleInWorkplace.SysModuleId != 00000000-0000-0000-0000-000000000000`

### C. Browser DOM checks

On section page:

- `#iframeRegular` is visible
- `#iframeSandboxed` is hidden
- Network has successful load for the runtime-correct `/rest` or `/0/rest` UI URL and main assets

## Troubleshooting Order

1. Reproduce with terminal (`AuthService.svc/Login` + `curl`) first.
2. Check latest `Error.log`.
3. Inspect browser console/network/DOM.
4. Then patch.

Do not start from manual refresh-only debugging.

## Output Requirements

When using this skill, provide:

1. Files changed.
2. Section registration summary (module/workplace/schema ids or keys).
3. Validation evidence:
- UI URL + asset status codes,
- OData presence checks,
- one log exception line or explicit "no blocking section errors found".

## References

- `references/creatio-iframe-section-template.md`
- Radar working example:
  - `packages/Radar/Schemas/RADAR_ListPage/RADAR_ListPage.js`
  - `packages/Radar/Data/SysModule_RADAR/data.json`
  - `packages/Radar/Data/SysModuleEntity_RADAR/data.json`
  - `packages/Radar/Data/SysModuleInWorkplace_RADAR/data.json`
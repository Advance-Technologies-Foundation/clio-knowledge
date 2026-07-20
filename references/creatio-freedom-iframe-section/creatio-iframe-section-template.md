---
interface:
  display_name: "Creatio Freedom IFrame Section"
  short_description: "Scaffold a Freedom UI section shell with a single iframe-bound external UI."
  default_prompt: "Create a Creatio Freedom UI section based on BlankPageTemplate with one crt.IFrame and validate registration + runtime checks."
---
# Creatio Freedom IFrame Section Template

Use this template when creating a new section that embeds an external UI (React/SPA) via runtime-aware UI route:
- net8: `/rest/<UiService>/ui`
- .NET Framework: `/0/rest/<UiService>/ui`

## Inputs

- `PACKAGE_NAME`
- `SECTION_CODE`
- `SECTION_SCHEMA_NAME`
- `SECTION_CAPTION`
- `UI_URL`
  - net8 example: `/rest/RadarUiService/ui`
  - .NET Framework example: `/0/rest/RadarUiService/ui`
- `WORKPLACE_ID` (or resolved Studio workplace id)

## 1) Section Page Schema

Path:
- `packages/<PACKAGE_NAME>/Schemas/<SECTION_SCHEMA_NAME>/<SECTION_SCHEMA_NAME>.js`

Skeleton:

```js
define("<SECTION_SCHEMA_NAME>", [], function() {
  return {
    viewConfigDiff: [
      {
        operation: "insert",
        name: "MainContainer",
        values: {
          type: "crt.GridContainer",
          rows: "minmax(0, 1fr)",
          columns: ["minmax(0, 1fr)"],
          gap: { columnGap: "none", rowGap: "none" },
          items: []
        },
        parentName: "CardContentContainer",
        propertyName: "items",
        index: 0
      },
      {
        operation: "insert",
        name: "RadarIFrame",
        values: {
          type: "crt.IFrame",
          contentType: "url",
          urlContent: "<UI_URL>",
          isSandbox: false,
          sandbox: null,
          fitContentToViewport: true,
          scrollType: "none"
        },
        parentName: "MainContainer",
        propertyName: "items",
        index: 0
      }
    ]
  };
});
```

Requirements:
- Parent template must be `BlankPageTemplate`.
- Keep only one iframe control.
- Do not add `PDS`/lists/data-source components.

## 2) Data Registration Files

- `packages/<PACKAGE_NAME>/Data/SysModule_<SECTION_CODE>/data.json`
- `packages/<PACKAGE_NAME>/Data/SysModuleEntity_<SECTION_CODE>/data.json`
- `packages/<PACKAGE_NAME>/Data/SysModuleInWorkplace_<SECTION_CODE>/data.json`

Rules:
- Avoid string literal `"null"` in icon/image fields.
- If `SysModuleInWorkplace` has zero-GUID links, add new valid row with a new `Id`.

## 3) Build and Deploy

```bash
source .env
cd src/ui/<UI_APP_NAME> && npm run build:creatio
cd /path/to/repo
dotnet build MainSolution.slnx -c dev-n8
clio push-pkg packages/<PACKAGE_NAME> -e "$CLIO_ENV" --Safe false
```

## 4) Runtime Validation

1. UI endpoint:
- net8: `GET <CREATIO_URL>/rest/<UiService>/ui` -> `200`
- .NET Framework: `GET <CREATIO_URL>/0/rest/<UiService>/ui` -> `200`

2. Assets:
- JS/CSS asset URLs from HTML -> `200`

3. OData registration checks:
- `SysModule` exists for `<SECTION_CODE>`
- `SysModuleInWorkplace` exists with non-zero `SysModuleId` and `SysWorkplaceId`

4. Browser DOM checks:
- `#iframeRegular` visible
- `#iframeSandboxed` hidden

5. Logs:
- Check latest `Error.log` and report first blocking backend error (if any).

## 5) Access Policy

- Enforce operation-level authorization for related backend endpoints.
- Use operation codes from module spec/doc.
- If mapping is missing, request it from developer before implementation.

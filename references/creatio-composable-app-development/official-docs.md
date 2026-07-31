# Official Docs And Local References

Use these references when you need confirmation for Creatio-specific concepts, especially when package boundaries, Freedom UI behavior, or CLIO delivery details matter.

## Official Creatio Docs

### Application architecture and package model

- Creating applications on Creatio platform:
  https://academy.creatio.com/docs/developer/architecture/development_in_creatio/creating_applications_on_creatio_platform/overview
- Use this reference for:
  - package boundaries
  - application structure
  - why delivery happens through packages instead of core edits

### Freedom UI page customization

- Freedom UI page customization basics:
  https://academy.creatio.com/docs/sites/academy_en/files/pdf/guide/1407/Freedom_UI_page_customization_basics_8.0.pdf
- Use this reference for:
  - handlers, validators, and converters
  - page load and save customization
  - deciding whether the behavior belongs in page source code

### Custom Freedom UI components

- Custom UI component based on a classic Creatio page element:
  https://academy.creatio.com/docs/sites/academy_en/files/pdf/guide/1403/Implement_a_custom_component_8.0.pdf
- Use this reference for:
  - `@creatio-devkit/common`
  - AMD module shape
  - custom component implementation boundaries

## Local CLIO References

These are machine-local references verified from the workspace on 2026-03-30.

- Command index:
  `C:\Projects\clio\clio\Commands.md`
- Package creation:
  `C:\Projects\clio\clio\docs\commands\new-pkg.md`
- Package pull:
  `C:\Projects\clio\clio\docs\commands\pull-pkg.md`
- Package push:
  `C:\Projects\clio\clio\docs\commands\push-pkg.md`

## Guidance

- Prefer the official Creatio docs for product concepts and framework behavior.
- Prefer the local CLIO docs for exact command names and examples in this environment.
- If the official docs mention an older version label, still use them for structure and terminology, but verify the current repository conventions before copying code verbatim.

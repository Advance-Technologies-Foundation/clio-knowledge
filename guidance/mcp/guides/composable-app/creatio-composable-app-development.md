# Creatio Composable App Development

Use this skill for source-level Creatio work, especially when the task involves packages, Freedom UI, page schemas, or CLIO-based delivery.

## Operating Rules

- Prefer package-safe customization. Extend through custom packages instead of assuming direct core edits are acceptable.
- Treat Creatio artifacts as package-scoped. Identify the package, schema, and object ownership before editing.
- Prefer existing workspace conventions over generic samples. If the repository already has naming, packaging, or schema patterns, follow them.
- Use CLIO command names exactly when you mention them. The local command set in this environment includes `new-pkg`, `push-pkg`, and `pull-pkg`.
- For niche Creatio APIs or Freedom UI patterns, consult [docs://knowledge/com.creatio.clio/reference.creatio-composable-app-development.official-docs](docs://knowledge/com.creatio.clio/reference.creatio-composable-app-development.official-docs) before inventing a structure.

## Workflow

1. Identify the artifact type.
   Package work usually starts in a package directory, package manifest, schema folder, or CLIO workflow.
   Freedom UI work usually starts in a page schema, client module schema, view model config, or handler chain.

2. Locate the owning package and schema.
   Determine which package should contain the change and whether the requested behavior belongs in an existing schema, a replacement schema, or a new custom schema.

3. Inspect the current implementation before designing the change.
   For Freedom UI pages, look for existing handlers, converters, validators, data source config, and request chains.
   For custom components, inspect the module declaration, package dependencies, and any existing usage of `@creatio-devkit/common`.

4. Choose the smallest extension surface that solves the task.
   Prefer page handlers, schema sections, and package-contained modules before introducing broader architectural changes.
   If no-code designer settings are sufficient, note that explicitly instead of forcing code.

5. Keep the delivery path explicit.
   If the task changes a package, mention the CLIO path that normally validates or ships that package.
   Use local docs or command help when command options are not obvious.

## Common Task Map

### Package Lifecycle

- Create a new package: `clio new-pkg <PACKAGE_NAME>`
- Pull a package from an environment: `clio pull-pkg <PACKAGE_NAME>`
- Push or install a package: `clio push-pkg <PACKAGE_NAME>`

Use package lifecycle commands when the user is creating a new customization package, synchronizing a local package, or deploying a finished change to an environment.

### Freedom UI Pages

- Start by finding the page schema and the surrounding client schema sections.
- Prefer handler-based business logic for page events, data load/save hooks, and request interception.
- Check whether the behavior belongs in page visibility, validation, filtering, data querying, navigation, or save/load flows before adding custom code.

### Custom Components

- Confirm whether a standard Freedom UI component plus page logic is enough before creating a custom component.
- If a custom component is required, keep it package-local and verify module dependencies first.
- When the component is based on a classic page element, inspect the AMD module declaration and dependency list carefully.

### Review and Refactor Work

- Validate package boundaries first.
- Look for hard-coded object names, duplicated request logic, and handler chains that mix unrelated responsibilities.
- Flag risky changes that affect shared schemas or out-of-the-box record pages across multiple apps.

## What to Inspect

- Package directory structure and manifest files
- Schema names and prefixes
- Existing client module handlers, converters, and validators
- Data source declarations and save/load request chains
- Existing CLIO docs or command help when deployment steps matter

## Reference Set

Read [docs://knowledge/com.creatio.clio/reference.creatio-composable-app-development.official-docs](docs://knowledge/com.creatio.clio/reference.creatio-composable-app-development.official-docs) when you need:

- Official Creatio documentation links for app architecture, Freedom UI pages, and custom components
- Local CLIO command references for package lifecycle commands
- Guardrails about package ownership, page customization boundaries, and component implementation choices
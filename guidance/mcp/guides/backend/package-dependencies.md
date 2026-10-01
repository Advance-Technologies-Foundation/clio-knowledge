clio MCP package-dependencies guide

PURPOSE
- Create a new package in an environment with create-package (see CREATING A NEW PACKAGE).
- Manage the dependency list of a workspace/custom package via PackageService.svc:
  add-package-dependency (extend) and remove-package-dependency (trim). Both round-trip
  the package's full properties and are idempotent (re-adding / removing an absent
  dependency is a no-op).

WHEN YOU NEED THIS (the canonical symptom)
- A schema mutation (modify-entity-schema-column / update-entity-schema / create-entity /
  sync-schemas) fails with:
    "GetSchemaDesignItem returned an HTML error page instead of JSON".
- The MOST COMMON cause is NOT a server bug: the package you are writing into does not
  depend on the package/app that OWNS the upper layer of the object you are extending.
  A replacing schema can only be created when the target package depends on the owner of
  the object's top layer.
  Classic example: extending the Opportunity layer from a custom package (for example
  `Custom`) that depends only on `CrtCore` → the designer fails until the package also
  depends on `CrtLeadOppMgmtApp` (the app that owns the Opportunity layer).

AUTOMATIC RESOLUTION
clio automatically detects when GetSchemaDesignItem fails because of a missing
dependency: it finds the package containing the target schema, adds it as a
dependency, and retries the operation — one transparent recovery cycle. This
applies ONLY to WRITE operations (modify-entity-schema-column, update-entity-schema,
create-entity) and ONLY when exactly one candidate package is found.
When multiple candidates exist, clio refuses to auto-resolve (ambiguous) and
instructs the user to add the correct dependency manually.
Read-only operations (get-entity-schema-properties, get-entity-schema-column-properties)
never trigger auto-resolution.

NOT THIS CASE: a transient network flap (DNS resolution failure, connection reset,
timeout, gateway 502/503/504) is a DIFFERENT failure class from a missing dependency.
`sync-schemas` retries transient network faults per operation on its own and, on a
mid-batch abort, returns a `resume-plan` — resubmit only `resume-plan.operations`. Do
NOT add a package dependency in response to a DNS/timeout error; add one only for the
"GetSchemaDesignItem returned an HTML error page" missing-dependency symptom above.

MANUAL RECOVERY (when auto-resolution is not available)
1) Identify the owning app/package of the object's upper layer (for Opportunity it is
   `CrtLeadOppMgmtApp`; use get-app-info / find-entity-schema to confirm the owner of
   other objects).
2) add-package-dependency
     --package-name <your package>  --dependencies <OwningPackage>
   (MCP: dependencies = [{ name: "<OwningPackage>" }]; version defaults to installed).
3) Retry the original schema mutation. It now succeeds.

DO NOT (anti-patterns that waste time and are unsafe)
- Do NOT write the column/schema directly into the owning (managed) package
  (for example into `CrtLeadOppMgmtApp`). Your changes belong in your own package.
- Do NOT fall back to raw SQL, direct OData writes to SysPackageDependency, or DataService
  to patch dependencies — those are blocked, permission-gated, or unsafe. Use
  add-package-dependency.
- Do NOT hand-edit a package descriptor and push-pkg to add a dependency — it conflicts.

REMOVING A DEPENDENCY (cleanup / rollback)
- remove-package-dependency
    --package-name <your package>  --dependencies <PackageToRemove>
  Removes matching entries by name (idempotent) and returns the resulting dependency list.
- Use it when fully rolling back an experiment that added a dependency only to unblock the
  designer. Note: if the extension you created still exists, the dependency is CORRECT and
  should stay — only remove it when nothing in your package relies on the owner anymore.

CREATING A NEW PACKAGE
- Use create-package ONLY when the user asks for a separate package (for example to hold a
  migration's output or to extend out-of-the-box functionality). Do NOT create one just because no
  package was named: page and schema writes already resolve their own target package
  (get-target-package, the application's current package).
- create-package is a destructive, non-idempotent long-tail tool: find it with get-tool-contract and
  call it through clio-run-destructive. When the running clio does not expose it, ask the user to create
  the package in the Configuration section; do NOT fall back to push-workspace, SQL, OData or
  DataService.
- Arguments: environment-name, package-name; optional description, dependencies and application-code
  (creates the package inside that installed application). dependencies here is a plain array of
  package names, e.g. dependencies: ["CrtBase"], NOT the [{ name: ... }] objects add-package-dependency
  takes.
- The environment's SchemaNamePrefix is prepended when package-name lacks it ("Calls" becomes
  "UsrCalls"). Use the RETURNED package-name for every later call, never the one you sent: pass it as
  package to get-target-package to confirm the new package as the write target, and as the package
  argument of every later write in the same run.
- Result: package-created=false means nothing changed (duplicate name, unknown dependency or
  application, rejected name). package-created=true with success=false means the package exists but a
  later step failed; error says which: dependencies not applied (add them with add-package-dependency)
  or the readback failed (check it with list-packages). Do not create it again. package-created=null
  means the create request failed in transport and the outcome is unknown: check list-packages for the
  name before retrying.
- Do NOT create a package with push-workspace: it installs the package as an archive, so it comes out
  locked (InstallType 1) and registered as an installed application.

NOTES
- A dependency change may report compilation-required; follow the package-scoped compilation and activation policy in `core-rules` for the affected package.
- add-package-dependency and remove-package-dependency need the same elevated package-management
  access as other package tools. create-package needs a user with administrator permission.

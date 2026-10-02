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

DIAGNOSTICS ARE READ-ONLY
Current Clio reports ranked candidate packages; it never automatically adds a dependency,
including when exactly one candidate remains. An HTML error page does not identify its cause:
it can also be a proxy or server failure. Confirm schema visibility before changing packages.
On a server advertising dependency contract v1, candidates already reachable through indirect
package dependencies are excluded. On older or incomplete servers the diagnostic explicitly
marks dependencies as unknown; candidates may already be reachable. Do not interpret them
as confirmed missing edges.

A transient network failure is a different failure class. sync-schemas retries transient network
faults and returns a resume-plan after a mid-batch abort: resubmit only resume-plan.operations.
Do not add package dependencies to repair DNS, timeout, authentication or gateway errors.

EXPERIMENTAL DEPENDENCY CONTRACT V1 (ENG-100222)
Use this workflow only when get-tool-contract advertises the named tools AND the target Creatio
server advertises dependency contract v1. The source-branch implementation was validated on a
10.2.344 seed, .NET 10 and PostgreSQL; the stock version number alone does not imply support.
Discover these long-tail tools with get-tool-contract and invoke them through clio-run.
Older Creatio servers are explicitly unsupported; do not substitute legacy endpoints silently.

1) find-pkg-by-schema with schema and manager-name uses exact, case-insensitive matching.
   With package-name and purpose=reference or extend, it asks the ENTITY designer for visibility.
   Use status=resolved and availableSchemaUIds as the answer. Candidates alone do not select a
   valid schema layer. ambiguous, notVisible, notFound and incomplete require investigation.
   Context resolution supports EntitySchemaManager only; do not extrapolate to client modules.
2) get-pkg-dependencies with package and transitive=true explains existing indirect visibility.
   pkg-dependency-path with from and to shows a shortest directed path. dependants=true reverses
   traversal for get-pkg-dependencies. export-pkg-graph returns JSON or DOT for larger analysis.
3) pkg-dependency-why with from and to summarizes registered metadata references; details=true
   includes individual reasons. check-removal=true previews removing that specific direct edge.
   check-pkg-dependency with action=add or remove is also a READ-ONLY preview. Mutations still
   use add-package-dependency or remove-package-dependency after the intended change is clear.
4) blocked identifies known problems. noKnownBlockers means only that the reported checkedKinds
   found no blocker; it is NOT a safe-to-delete guarantee or write authorization. Always read
   uncheckedKinds. Raw JavaScript/AMD body imports are not automatically registered as client
   metadata dependencies: source-only imports can be absent even when a real dependency exists.
   Review source and run the application's tests before removal. An empty reasons array is not
   proof that an edge is unused. A nonempty array is not proof it is essential: alternate paths
   can retain visibility after removing a redundant direct edge.

contains=true performs a literal substring search (% and _ are not wildcards). limit is 1..200.
hasMore=true means incomplete results: narrow the query; do not select an owner from a truncated
set. A graph revision covers packages/dependency topology only, not a schema snapshot or a
concurrency precondition. Rerun inspection after edits. Unknown/busy/timeout/incomplete-graph
errors never authorize mutation. Future optional fields may be ignored; a new contract major
requires explicit client support while existing servers can continue serving v1.

MANUAL RECOVERY (after confirming the missing visibility)
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

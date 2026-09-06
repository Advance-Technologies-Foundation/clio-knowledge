clio MCP package-dependencies guide

PURPOSE
- Manage the dependency list of a workspace/custom package via PackageService.svc:
  add-package-dependency (extend) and remove-package-dependency (trim). Both round-trip
  the package's full properties and are idempotent (re-adding / removing an absent
  dependency is a no-op).

WHEN YOU NEED THIS (the canonical symptom)
- A schema operation (modify-entity-schema-column / update-entity-schema / create-entity /
  sync-schemas, and the READ paths get-entity-schema-properties /
  get-entity-schema-column-properties / set-entity-schema-properties) fails with:
    "Schema '<Schema>' could not be opened in package '<Package>'. The usual cause is that
     '<Package>' has no dependency on the package that owns the layer of '<Schema>' it is
     trying to extend."
  followed by the packages that contribute the schema, installed applications first.
- The MOST COMMON cause is NOT a server bug: the package you are writing into does not
  depend on the package/app that OWNS the upper layer of the object you are extending.
  A replacing schema can only be created when the target package depends on the owner of
  the object's top layer. The platform raises SchemaIsNotAvailableException inside
  GetSchemaDesignItem and the web stack renders it as an HTML page, which is why older
  clio builds reported this as a server error or a stale database table. It is neither.
  Classic example: extending the Opportunity layer from a custom package (for example
  `Custom`) that depends only on `CrtCore` → the designer fails until the package also
  depends on `CrtLeadOppMgmtApp` (the app that owns the Opportunity layer).
- A READ scoped to a package shows the same failure while the SAME read without
  `--package` succeeds. That is not evidence that reads are unaffected — it means the
  unscoped read never had to resolve the dependency. Do not conclude "reads work" from it.

CLIO NEVER ADDS THE DEPENDENCY FOR YOU
clio does NOT auto-resolve. It reports the packages that contribute the schema and are
not already dependencies of your package, ranked with installed applications first, and
stops. Adding one changes the package for real, more than one of them can be a valid
dependency, and the failure alone does not prove a missing dependency is the cause — so
the choice is yours. Treat the ranked list as a hint, not an answer.
Two qualifications travel in the message itself and change what it is worth:
- If clio could not read the dependencies your package already declares, the list is
  UNFILTERED and may name packages that are already there. The message says so. Adding an
  already-declared dependency is a harmless no-op, but it also fixes nothing.
- If the lookup never completed, or found no contributing package, clio states that it has
  NO evidence about the cause and names none. Do not invent one; confirm the schema name
  and target package with find-entity-schema / list-packages and check the server log.

NOT THIS CASE: a successful write whose verification reload came back empty. After
modify-entity-schema-column saves and publishes, clio re-reads the schema to confirm.
Publishing refreshes the schema manager in two steps and the schema is briefly missing
while that runs (about nine seconds, measured). That failure names itself explicitly —
"was saved and published successfully ... but the verification reload could not read it
back yet" — and instructs you NOT to repeat the write and NOT to add a dependency. The
change is already applied. Re-read the schema to confirm.

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

NOTES
- A dependency change may report compilation-required; run compile-configuration to apply.
- Both operations need the same elevated package-management access as other package tools.
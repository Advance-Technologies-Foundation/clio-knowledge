# Shared Freedom UI client logic

Owns reusing a JavaScript helper across Freedom UI **web** pages. Use a client-unit schema for a small AMD helper; use `ui-project` when the requirement is an Angular remote module or custom component. Mobile page bodies are not AMD web pages; follow `mobile-page-modification` for those.

## Create once, consume from each page

1. Read `page-modification` before changing an existing page. Resolve its owning package and read its current body. Discover `create-client-unit-schema`, `update-client-unit-schema`, `get-client-unit-schema` and `update-page` with `get-tool-contract`; dispatch long-tail tools through `clio-run`.
2. Create the helper with `schema-name`, `package-name` and `environment-name`. This creates the schema; it does not write your implementation. Save its full body with `update-client-unit-schema` and read it back.
3. Put the helper in the consumers' package, or a package they explicitly depend on. A JavaScript import does not create a package dependency. Read `package-dependencies` for dependency changes.
4. Export a small API. Pass values or the current page context into functions when needed; do not store one page's context in a module-level variable shared by other pages.

```javascript
define("UsrSharedText", [], function() {
    return {
        label: function(name) { return "Shared: " + name; }
    };
});
```

5. In each consuming page, add the module name to `SCHEMA_DEPS` and its argument to `SCHEMA_ARGS` at the **same position**, preserving existing imports:

```javascript
define("UsrExamplePage", /**SCHEMA_DEPS*/["UsrSharedText"]/**SCHEMA_DEPS*/,
    function/**SCHEMA_ARGS*/(sharedText)/**SCHEMA_ARGS*/ {
        // Preserve the rest of the page body.
        return {
            handlers: /**SCHEMA_HANDLERS*/[{
                request: "crt.HandleViewModelInitRequest",
                handler: async (request, next) => {
                    await next?.handle(request);
                    request.$context.SharedLabel = sharedText.label("Example");
                }
            }]/**SCHEMA_HANDLERS*/
        };
    });
```

This excerpt is not a replacement for a complete page. Declare `SharedLabel` in the page's view-model attributes and bind a label's caption to `$SharedLabel`; retain its model, view, converters, validators and other handlers. `page-schema-handlers` owns handler composition and SDK request dispatch.

6. Save consumers with `update-page` and verify their readback. Keep expressions such as `sharedText.label(...)` inside executable handlers/converters. `SCHEMA_VIEW_CONFIG_DIFF` is JSON: inserting a function call there fails validation. Do not disable validation to force that pattern through.
7. Open both pages in the actual browser. Verify distinct expected outputs produced by the same helper. Change only the helper, refresh the browser, and verify both outputs change. An already loaded AMD module can remain cached in the current browser session; a successful server save is not proof that the open tab loaded its new body.

## Failure checks

- Missing module/load failure: verify the exact schema name, persisted helper body, package dependency, and browser load errors. Do not copy the helper into each page to hide an unresolved dependency.
- Undefined helper argument: check the order and count of AMD dependencies and factory arguments.
- Empty label: check that the attribute exists, the handler ran, and the control binds that attribute. Read `page-schema-handlers` for request lifecycle issues.
- Old result after saving: refresh the browser and repeat the behavioral check on both pages before changing backend deployment settings. Client-only JavaScript edits do not need C# compilation; `core-rules` owns compilation and activation policy.

## Evidence boundary

Validated on a disposable Creatio 10.1.585 / .NET 8 / PostgreSQL instance through Clio MCP and the real browser: one helper, two BlankPageTemplate-derived web pages, distinct v1 labels, then a helper-only edit and both v2 labels after reload. The rejected layout expression and successful handler-based alternative were exercised. This does not establish mobile support or cross-package dependency installation on every Creatio version. See [validation evidence](https://github.com/Advance-Technologies-Foundation/clio-knowledge/issues/208).

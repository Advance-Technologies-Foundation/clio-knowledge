clio MCP — mobile remote module (custom business logic for the Creatio Mobile app)

Scope
- Use this guide when a Freedom UI MOBILE page needs behaviour that lives in code: a handler for a
  built-in request (save, load, attribute change), a custom request behind a button, or a virtual data
  source. In the Mobile app such code is a REMOTE MODULE: a TypeScript project built on the npm package
  `@creatio/mobile-common` into one script that a Creatio package carries to the device.
- This guide owns the Creatio-side mechanics: what the script must be, where it sits in a package, how
  it is delivered with clio, and how a mobile page refers to what the module implements.
- It does NOT own the mobile page body rules (`get-guidance name=mobile-page-modification`), the web
  remote module (`get-guidance name=ui-project`, an Angular project under `Files/src/js`), or the SDK
  API itself — the type declarations shipped in the npm package are the API reference.
- A mobile page body MUST NOT contain handler code. `get-guidance name=mobile-page-modification` owns
  that rule; this guide is where the code goes instead.

What a module can and cannot do
- A module registers request handlers and virtual data sources and calls the SDK services.
- The npm package `@creatio/mobile-common` (https://www.npmjs.com/package/@creatio/mobile-common) is
  the reference for what a module can do. Install its newest release and read the package itself: its
  type declarations (`index.d.ts`) list the decorators, base classes, requests and services a module can
  use, and its `CHANGELOG.md` says which release introduced each of them. What the declarations do not
  export does not exist: do not invent an API for it; solve the task with what they export or with a
  page or entity business rule.
- The SDK a module runs against is the one inside the installed Mobile app, not the npm release the
  project compiled against. A feature newer than the user's app is absent at run time.

The script contract
- ONE self-running script named `main.js`, built as an IIFE. The SDK import MUST be an external mapped
  to the global `crtSdk` (webpack: `externals: { '@creatio/mobile-common': 'crtSdk' }`). The npm package
  ships type declarations only, so a build without that external either fails or carries no SDK.
- The script MUST NOT be minified with class-name mangling: the SDK keys a module by its class name.
- TypeScript needs `experimentalDecorators` and `emitDecoratorMetadata`; target `es2019`; no DOM library.
  The Mobile app evaluates the script in an embedded JavaScript engine: there is no `window` or
  `document`, and `console` has `log`, `warn` and `error` only.
- Module class: decorated with `@CrtModule({ requestHandlers, virtualDataSources })`, implements
  `DoBootstrap`, and calls `bootstrapCrtModule('<ModuleName>', <ModuleClass>)` in `bootstrap()`.
  `<ModuleName>` MUST equal the name of the folder the script is delivered in.
- Every handler `type`, request `type` and data source name MUST be unique across all modules on the
  environment and carry the customer prefix (`usr.` / `Usr`); a duplicate replaces the earlier one
  without an error.

Where the script sits
- `<Package>/Files/src/mobile/<ModuleName>/main.js`.
- The folder MUST hold exactly one `.js` file. The app evaluates every script it finds there, so a
  second build variant next to `main.js` registers the module twice.
- No manifest entry, schema or system setting registers a module: the server collects the scripts under
  `Files/src/mobile` of every package, and the device receives them during synchronization.
- The feature `EnableMobileSDK` MUST NOT be switched off; while it is off the app ignores every module.
  clio MCP has no tool that changes a feature. Read its state with `odata-read` (`Feature` by `Code`,
  then `AdminUnitFeatureState`); to change it, ask the user to run
  `clio set-feature EnableMobileSDK 1 -e <environment>`.

Delivering with clio
1. `create-workspace` (once), then `add-package` for the package that carries the module. Prefer a
   package that holds only module scripts. `add-package` scaffolds a C# project into it; for a
   script-only package delete `Files/src/cs/`, `Files/Libs/`, `Files/<Package>.csproj` and
   `Files/Directory.Build.targets`, in `descriptor.json` set the package's own `Descriptor.Type` to 0
   and remove `ProjectPath`, and drop the deleted project's line from the workspace's
   `MainSolution.slnx`, so nothing is compiled on the environment.
2. Keep the project sources in the workspace outside `packages/`, build, and copy the built `main.js`
   to `packages/<Package>/Files/src/mobile/<ModuleName>/main.js`.
3. `push-workspace`. It packs each package folder whole, `Files/` included, and installs it through the
   application installer — the same route `get-guidance name=ui-project` uses for `Files/src/js`.
   The installer regenerates the served file content itself; a script-only package needs no
   `compile-creatio`. A package that carries a C# project (`Type` 1 or a `ProjectPath` in its
   descriptor) is compiled by the push itself after the installation, and the push adds the project
   files `restore-workspace` generated to the package; a failure of that compilation ends the push
   with a non-zero exit code although the script is delivered, so confirm the script before calling
   the delivery failed. A workspace without an application descriptor is registered on the environment
   as an installed application named `CreatioPackages`. The `push-workspace` contract describes a new
   package as locked; a package whose descriptor `Maintainer` equals the environment's `Maintainer`
   setting arrived editable in both measured deliveries. Either state suits a code-only package, which
   changes only through the next push. The installer log also prints `Package "<name>" loaded` for
   packages the workspace does not hold; only a package with a `saved` line was written.
4. Confirm with `list-package-files` and `get-package-file` (cliogate 2.0.0.47 or later) that the
   script is on the environment and equals the local build. Both address it relative to the package's
   `Files` folder: `src/mobile/<ModuleName>/main.js`. `get-package-file` also returns a `project-error`
   about an unavailable `.csproj` for a script-only package; that is expected.
5. The user synchronizes the Mobile app (Settings, Synchronize). The app reloads with the new scripts;
   a restart is needed only if the previous behaviour persists.
- MUST NOT push a stale local copy of a package that people edit in the designers: `push-workspace`
  installs the workspace copy of every package it holds and replaces what changed on the environment
  since the copy was taken. When the module has to live in such a package, run `restore-workspace`
  before adding the script, and run it again when the push does not follow at once.
- A delivered script is NOT withdrawn by removing it. Measured in the run under Evidence: after
  `delete-app` (package and files gone from the environment), after a later push of the package without
  the script, and after a push that ran "File content generation" with the script absent
  (`list-package-files` no longer listed it), each synchronization still produced a device revision
  containing `<Package>/<ModuleName>/main.js`, and its handler ran. A push DOES replace the script's
  content. To switch a module off, MUST push an inert `main.js` at the same path (`(() => {})();`),
  confirm on a device that the inert script is delivered, and only then delete the package if wanted.
  After the package was deleted, the entry left the served content at a later installation of another
  package that carried file content; no earlier trigger was found.
- Nothing in clio runs the Mobile app. A delivered module is NOT a verified one: report it as delivered
  and give the user the device check (synchronize, perform the action; on failure, Settings, Send log).
  A handler error is written to that log as `ERROR Type: JSError` with its message and a script line
  number but without the module or handler name; `Logger.console` output is not in it and goes to the
  device system log (`adb logcat`).

How a mobile page refers to the module
- A custom request from a component event. `validate-page` and `update-page` do not check request
  names, so a misspelt name passes validation and does nothing on the device — copy it from the
  `@CrtRequest({ type })` in the module:
    "clicked": { "request": "usr.ApplyDiscountRequest", "params": { "percent": 10 } }
  A parameter value may be an attribute binding ("$UsrDiscountPercent"). Do not put `type` or `scopes`
  into `params`.
- A handler for a built-in request needs no page change. It is attached by `scopes` in
  `@CrtRequestHandler`: `['<PageSchemaName>']` for the page, omitted for every page. Use the schema name
  exactly as `get-page` returns it. A request also carries the names of the templates the page is built
  on, so a handler scoped to a template runs on every page derived from it. A row of a list or a detail
  carries no scopes: a request processed on a row reaches only handlers declared without `scopes`. A
  Freedom UI page reports no `_Root` scope, so a handler scoped to `<PageSchemaName>_Root` never runs
  on it. `get-request-info` with `schema-type: "mobile"` lists the built-in requests a handler can
  intercept.
- A virtual data source is declared in the page's data sources and bound like any other:
    "UsrOrderHintsDS": {
      "type": "crt.VirtualDataSource",
      "config": {
        "virtualDataSourceType": "UsrOrderHintsDataSource",
        "primaryAttributeName": "Id",
        "primaryDisplayAttributeName": "Name",
        "parameters": {
          "Id": { "caption": "Id", "dataValueTypeName": "Guid" },
          "Name": { "caption": "Hint", "dataValueTypeName": "Text" }
        }
      }
    }
  `virtualDataSourceType` is the name passed to `@CrtVirtualDataSource` (the data source's own name is
  used when it is omitted). `parameters` declares the attributes the source returns. The primary and
  display attribute names are required unless `parameters` contains `Id` and `Name`.
  A source that feeds a `crt.List` sits next to the page's own data source with
  `"scope": "viewElement"`; the list reads a collection attribute (`isCollection: true`,
  `modelConfig.path` = the data source name) whose row attributes are declared inside it, under
  `viewModelConfig.attributes`. This is the one case where a page declares a second data source.
- A button whose request the web-to-mobile conversion dropped as `drop-unknown-request` can be added
  back once a module implements that request.

Evidence
- Placement under `Files/src/mobile/<ModuleName>/`, the `EnableMobileSDK` feature, `scopes`, the
  `clicked` request binding and synchronization as the delivery step: Creatio Academy, "Custom request
  handler implemented using remote module in Creatio Mobile" (8.x), read 2026-11-16.
- The package as the API reference: the `@creatio/mobile-common` npm package holds no runtime code,
  only type declarations (`index.d.ts`), a README and a `CHANGELOG.md` with one section per release.
- Folder name as module name, evaluation of every script in the folder, the `crtSdk` global and the
  `crt.VirtualDataSource` configuration keys: Creatio Mobile app source read at commit `fde23346cf`
  (2026-11-16).
- Delivery end to end, measured 2026-11-16 on Creatio 10.1.796 (.NET Framework, PostgreSQL) with clio
  8.1.0.152, cliogate 2.0.0.53 and Creatio Mobile 10.1.0 on Android: a new package holding only
  `Files/src/mobile/<ModuleName>/main.js` was delivered with `push-workspace` (the installer log ends
  with "File content descriptors generated", no compilation was run), `get-package-file` returned the
  script byte for byte, and after Synchronize the device's content descriptor listed
  `<Package>/<ModuleName>/main.js` and an unscoped `crt.LoadDataRequest` handler ran, reporting scopes
  `["BaseMobileHomePage","FieldSalesMobileHomePage"]`. The environment had no `EnableMobileSDK`
  feature record and no manifest entry for the module. In the same run `window`, `document`,
  `console.info` and `console.debug` were undefined inside the script, a handler error reached the app
  log as `ERROR Type: JSError`, and `Logger.console` output appeared only in the device system log.
- The `add-package` scaffold contents and the `CreatioPackages` application: observed in the same run.
- Request scopes as the page's template chain followed by its schema name, rows of a list or a detail
  created without scopes, and no `_Root` scope on a Freedom UI page: Creatio Mobile app source read at
  commit `87053f2a69` (the view model takes its scopes from the schema's `tag`; a collection item is
  generated with none), matching the scopes the device reported in the first run above. The web
  Freedom UI source at commit `65359d398d` assigns scopes the same way.
- A third delivery the same day, of a module into an existing application package with a C# project
  (`Type` 1, `ProjectPath` set), with a `crt.List` on a record page fed by a `crt.VirtualDataSource`:
  the push log reached "Application installed successfully" and "Files saved in package", then ran a
  package build that failed with "Cannot run compilation because another compilation is in progress"
  and the call returned exit code 1; `get-package-file` returned the script byte for byte;
  `list-package-files` afterwards listed `<Package>.csproj`, `Directory.Build.props`,
  `Directory.Build.targets` and `obj/` files that were not there before the push; the package stayed
  in its own application. On the device the list showed the rows the source returned, with subtitle
  labels taken from the parameter captions.
- A second delivery the same day, of a module with a `crt.SaveRecordRequest` handler scoped to a record
  page: an invalid value showed the handler's dialog and the record stayed unsaved on the server; a
  value the handler rewrote with `setAttribute` before calling the next handler was saved in its
  rewritten form.

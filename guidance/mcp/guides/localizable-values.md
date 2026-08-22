clio MCP localizable-values guide

Scope: use when backend C# code needs user-visible text, when deciding which Creatio schema owns a resource, or when testing localization and fallback. For Freedom UI page authoring details, also read `page-schema-resources`; that guide owns page bindings and resource registration.

## Ownership

- Every localizable value MUST belong to a Creatio schema.
- Put a schema-specific value on the schema that renders or consumes it. Page captions belong to the page; process text belongs to the process; object captions belong to the object.
- A dedicated source-code schema MAY own package-level backend values only when no more specific schema is a natural owner.
- Do not use one source-code schema as a package-wide registry for unrelated page, process, object, and backend values.
- `clio add-package <PackageName> --as-app` (`clio ap <PackageName> -a`) creates a small package-level source-code owner and an injectable `ILocalizableStringResolver` adapter as starting points in localization-ready Clio releases. Keep the schema narrow; the resolver does not change resource ownership.

## Backend resource and lookup contract

Persist a string item under the owning schema's resource folder using this exact item-name form:

```text
LocalizableStrings.<Key>.Value
```

Application and transport code MUST depend on the generated `ILocalizableStringResolver`, not construct Creatio Core's concrete `LocalizableString` directly. Inject the abstraction into the domain service that needs localized text:

```csharp
using System.Globalization;

public sealed class GreetingService {
    private readonly ILocalizableStringResolver _strings;

    public GreetingService(ILocalizableStringResolver strings) {
        _strings = strings;
    }

    public string GetSpanishGreeting() {
        string greeting = _strings.GetCultureValueWithFallback(
            "<OwningSchemaName>",
            "LocalizableStrings.<Key>.Value",
            CultureInfo.GetCultureInfo("es-ES"));
        return greeting;
    }
}
```

`LocalizableStringResolver` is the infrastructure adapter. It is the only generated class that constructs `LocalizableString`, and the package application composition root registers it as `ILocalizableStringResolver`. In this small primitive, keep the interface and implementation together in `LocalizableStringResolver.cs`. Keep that boundary injectable so domain and web-service tests can replace it.

Choose the lookup intentionally:

| Requirement | API | Observable behavior when the requested translation is absent |
| --- | --- | --- |
| Render for the current execution culture | `ILocalizableStringResolver.GetValue` | Uses Creatio culture fallback |
| Prove that an exact translation exists | `ILocalizableStringResolver.GetCultureValue` | Returns `null` |
| Render for an explicit culture with fallback | `ILocalizableStringResolver.GetCultureValueWithFallback` | Returns the default-culture value |

The generated adapter passes `throwIfNoManager: false` to the underlying Creatio methods, making a missing resource manager return `null` instead of throwing. Do not treat that as proof that the key exists; assert the result.

Do not replace the resolver with an `I*Helper`, static accessor, or package-wide string registry. Add a separate domain-facing interface when an application use case adds policy or orchestration beyond one lookup.

## Thin web-service boundary

A configuration web service validates transport input, creates an application scope, resolves a domain service, delegates, and maps the concrete response DTO. It MUST NOT construct `LocalizableString` or perform localization logic. Read `configuration-webservice` and `configuration-webservice-tests` before implementing or testing that endpoint.

## Freedom UI boundary

Freedom UI pages bind visible text through `$Resources.Strings.<Key>`. The persisted resource still uses `LocalizableStrings.<Key>.Value`, and the page's schema metadata must declare the localizable value so Creatio can populate the runtime resource dictionary.

Read `page-schema-resources` before creating or changing a page resource. It owns the `resources` parameter, data-source caption auto-provisioning, validator macros, and binding syntax. Do not copy those rules here.

After deployment, use `get-page` and inspect `bundle.resources.strings.<Key>` as the platform oracle. A resource XML file on disk is not sufficient proof that the Freedom UI runtime can resolve it.

## Test the behavior

Use at least two cultures: the default culture and one active secondary culture. Include three deterministic cases:

1. A key translated in both cultures.
2. A key present only in the default culture.
3. A missing key.

Split stand-free tests into two explicit categories:

- `ResourceContent` inspects schema ownership, resource XML item names and values, Freedom UI bindings, and localizable-value metadata.
- `Implementation` executes the concrete `LocalizableStringResolver`, domain services, composition root, and transport validation/delegation.

Do not test only consumers with an `ILocalizableStringResolver` substitute. Unit-test the generated `LocalizableStringResolver` itself. Give a test `UserConnection` a substituted `IResourceStorage`, return a substituted `IResourceManager` for the owning schema, configure `GetString` and `GetStringWithCultureFallback`, then assert the returned value and the exact platform method call. This proves the adapter constructs `LocalizableString` with the request's workspace resource storage and preserves strict versus fallback behavior without a running Creatio instance.

Keep `ResourceContent` assertions for package structure and fast feedback:

- the resource belongs to the intended schema;
- persisted item names use `LocalizableStrings.<Key>.Value`;
- Freedom UI code binds `$Resources.Strings.<Key>`;
- the page metadata declares every custom page resource;
- the secondary resource deliberately omits the fallback test key.

Keep `Implementation` assertions behavioral:

- the web service delegates to a substitute domain service and rejects invalid input before delegation;
- the domain service calls the three resolution operations through `ILocalizableStringResolver`.

Run both stand-free categories with coverage enabled and enforce 100% line, branch, and method coverage for the production package assembly. Keep the threshold in the test project so a regression fails the coverage command.

Run Creatio-backed tests after synchronizing and compiling the package. Assert strict and fallback results independently. For a Freedom UI page, also assert `get-page` exposes the expected cultures under `bundle.resources.strings`, then render the page once in each culture. The default-only value must remain visible in the secondary culture to prove fallback.

## Failure signals and recovery

- Strict lookup is `null`, fallback returns the default value: expected when the requested translation is intentionally absent. Add the translation if strict coverage is required.
- Strict and fallback lookups are both `null`: verify the owning schema name, exact `LocalizableStrings.<Key>.Value` item name, active cultures, package synchronization, and configuration compilation.
- Backend lookup succeeds but a Freedom UI label is blank: run `get-page`. If `bundle.resources.strings` lacks the key, repair the page localizable-value metadata and persisted item name, then synchronize, compile, clear cache, and reload.
- The page shows the default value in every culture: verify the secondary culture is active, the user profile uses it, and that culture's resource file contains the translated key.
- A value cannot be found after moving it to a shared registry schema: restore ownership to the consuming schema or update every lookup deliberately. Do not use a global registry to hide ownership errors.

## Verified boundary and reference lab

The strict, fallback, missing-key, current-culture, Freedom UI metadata, and rendered page behaviors were live-verified on Creatio `10.1.585.0`, .NET 8, PostgreSQL. Use the independent reference repository at immutable commit `273eb7531a8284b6072730b097769b95df56a02e`: `https://github.com/Advance-Technologies-Foundation/creatio-localization-lab/tree/273eb7531a8284b6072730b097769b95df56a02e`.

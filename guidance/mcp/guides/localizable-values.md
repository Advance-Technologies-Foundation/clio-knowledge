clio MCP localizable-values guide

Scope: use when backend C# code needs user-visible text, when deciding which Creatio schema owns a resource, or when testing localization and fallback. For Freedom UI page authoring details, also read `page-schema-resources`; that guide owns page bindings and resource registration.

## Ownership

- Every localizable value MUST belong to a Creatio schema.
- Put a schema-specific value on the schema that renders or consumes it. Page captions belong to the page; process text belongs to the process; object captions belong to the object.
- A dedicated source-code schema MAY own package-level backend values only when no more specific schema is a natural owner.
- Do not use one source-code schema as a package-wide registry for unrelated page, process, object, and backend values.
- Starting with Clio 8.1.0.111, `clio add-package <PackageName> --as-app` (`clio ap <PackageName> -a`) creates a small package-level source-code owner and an injectable `ILocalizableStringResolver` adapter. On an older Clio version, add the interface and adapter using `packages/AtfLocalizationLab/Files/src/cs/LocalizableStrings/LocalizableStringResolver.cs` from the pinned reference lab, or upgrade. Keep the schema narrow; the resolver does not change resource ownership.

## Backend resource and lookup contract

Persist a string item under the owning schema's resource folder using this exact item-name form:

```text
LocalizableStrings.<Key>.Value
```

To add a secondary culture:

1. In the same owning schema resource folder, add `resource.<culture>.xml`, for example
   `resource.es-ES.xml` beside `resource.en-US.xml`.
2. Keep each translated item's `Name` identical across culture files and change only its `Value`.
   Deliberately omit a key from the secondary file only when testing default-culture fallback.
3. Add or activate that culture in Creatio's Languages section, then synchronize the package and compile
   the configuration so the resource manager sees the new culture file.
4. Set the test user's language to the secondary culture and sign in again before checking current-culture
   UI behavior. Use explicit-culture resolver calls for deterministic backend tests.

Domain and application services that perform localization MUST depend on the generated `ILocalizableStringResolver`, not construct Creatio Core's concrete `LocalizableString` directly. Inject the abstraction into the domain service that needs localized text:

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

The generated package targets C# 7.3, so the sample uses `string` rather than nullable-reference syntax.
The resolver can still return `null`; check the value before dereferencing it.

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

A configuration web service validates transport input, creates an application scope, resolves a domain service, delegates, and maps the concrete response DTO. The transport entry point depends on that domain service, not directly on `ILocalizableStringResolver`. It MUST NOT construct `LocalizableString` or perform localization logic. Read `configuration-webservice` and `configuration-webservice-tests` before implementing or testing that endpoint.

## Freedom UI boundary

Read `page-schema-resources` before creating or changing a Freedom UI page resource. It owns binding syntax, the `resources` parameter, data-source caption auto-provisioning, validator macros, and the decision whether a custom page resource must be registered. Do not infer one binding or registration rule for every page resource, and do not copy those rules here.

After deployment, use `get-page` and inspect `bundle.resources.strings.<Key>` as the platform oracle for an explicitly registered custom page resource. A resource XML file on disk is not sufficient proof that the Freedom UI runtime can resolve it. For data-source-bound captions and other resource types, follow the decision rules in `page-schema-resources`; absence from this node is not by itself proof of a defect.

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
- Freedom UI bindings and metadata follow `page-schema-resources` for the specific resource type;
- the secondary resource deliberately omits the fallback test key.

Keep `Implementation` assertions behavioral:

- the web service delegates to a substitute domain service and rejects invalid input before delegation;
- the domain service calls the three resolution operations through `ILocalizableStringResolver`.

Run both stand-free categories with coverage enabled and enforce 100% line, branch, and method coverage for the production package assembly. Keep the threshold in the test project so a regression fails the coverage command.

Run Creatio-backed tests after synchronizing and compiling the package. Assert strict and fallback results independently. For an explicitly registered custom page resource, also assert each `bundle.resources.strings.<Key>` object from `get-page` exposes the expected culture properties, then render the page once in each culture. For data-source-bound captions and other resource types, follow `page-schema-resources`. The default-only value must remain visible in the secondary culture to prove fallback.

## Failure signals and recovery

- Strict lookup is `null`, fallback returns the default value: expected when the requested translation is intentionally absent. Add the translation if strict coverage is required.
- Strict and fallback lookups are both `null`: verify the owning schema name, exact `LocalizableStrings.<Key>.Value` item name, active cultures, package synchronization, and configuration compilation.
- Backend lookup succeeds but a Freedom UI label is blank: read `page-schema-resources`, run `get-page`, and diagnose the binding key and registration decision according to that guide. Do not add or change page resource metadata based only on absence from `bundle.resources.strings`.
- The page shows the default value in every culture: verify the secondary culture is active, the user profile uses it, and that culture's resource file contains the translated key.
- A value cannot be found after moving it to a shared registry schema: restore ownership to the consuming schema or update every lookup deliberately. Do not use a global registry to hide ownership errors.

## Verified boundary and reference lab

The strict, fallback, missing-key, current-culture, Freedom UI metadata, and rendered page behaviors were live-verified on Creatio `10.1.585.0`, .NET 8, PostgreSQL. Use the independent reference repository at immutable commit `273eb7531a8284b6072730b097769b95df56a02e`: `https://github.com/Advance-Technologies-Foundation/creatio-localization-lab/tree/273eb7531a8284b6072730b097769b95df56a02e`.

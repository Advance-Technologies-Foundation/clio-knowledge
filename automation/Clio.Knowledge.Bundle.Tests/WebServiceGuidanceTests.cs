using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-web-service</c> guide: the <c>webService</c> block of the Call web service element. It is
/// registered as <c>process-send-email-attachments</c> is - banner-less, ungated, no legacy route - and reached from
/// the element catalog, its details and routing. Routing also has to tell it apart from the configuration web
/// service guides, because "call a web service" otherwise lands on the C# backend articles.
/// </summary>
/// <remarks>
/// TRIM ALARMS: every clause below is a rule CrtProcessBuilder enforces from <see cref="Floor"/>, a refusal or notice
/// an agent has to recognise, or a run-time fact an agent cannot see from a green build. A reword that keeps the rule
/// updates the clause here; a trim that drops it is the failure these tests exist for.
/// </remarks>
[TestFixture]
public sealed class WebServiceGuidanceTests
{
    /// <summary>
    /// The first CrtProcessBuilder that builds, retargets and describes the element. It is stated here, in the
    /// article, in the two catalog articles and in the manifest description, and moves in all five together.
    /// </summary>
    internal const string Floor = "1.6.6.150";

    /// <summary>
    /// The clio side of the gate. It is a capability, not a version: the release that adds <c>webService</c> to the
    /// pre-send key check also names it in the FULL contract, and no clio release carrying it exists yet, so a number
    /// would be a guess. An older clio refuses the block on create, <c>addElement</c> and <c>setElement</c> before
    /// anything is sent.
    /// </summary>
    private const string ClioGate =
        "Use it only when the FULL contract (`get-tool-contract` with `tool-names: [\"create-business-process\"]`, `detail: \"full\"`) names `webService`; a clio whose contract lacks it refuses the block before sending.";

    private const string ItemId = "process-web-service";

    private const string ArticlePath = "guidance/mcp/guides/processes/web-service.md";

    private const string CatalogPath = "guidance/mcp/guides/processes/element-catalog.md";

    private const string CatalogDetailsPath = "guidance/mcp/guides/processes/element-catalog-details.md";

    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";

    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    private const string VersionsPath = "guidance/mcp/guides/processes/versions.md";

    /// <summary>A released four-part package version: the only shape a published floor may take.</summary>
    private static readonly Regex FloorShape = new(@"^\d+(?:\.\d+){3}$", RegexOptions.Compiled);

    /// <summary>
    /// This guide's design target, in the response characters of <see cref="ProcessGuideResponseSizeTests"/>. The guide
    /// is about 21,300 characters, so a new clause pays for itself with a trim, and a larger addition is reviewed, or
    /// split, rather than absorbed. The 19,000-byte inline rule is asserted separately.
    /// </summary>
    private const int DesignTargetResponseCharacters = 22_000;

    [Test]
    [Description("The guide owns the block, states the FULL-contract gate and the floor, and the stored-name rule; it is registered banner-less, ungated and without a legacy route, required by the library, and the element catalog, its details and routing send the reader to it.")]
    public void Guide_ShouldOwnTheBlock_AndTheEntryArticlesShouldRouteToIt()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string[] requiredItems = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString()!)];
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string raw = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        string guide = Collapse(raw);
        string catalog = ReadCollapsed(CatalogPath);
        string details = ReadCollapsed(CatalogDetailsPath);
        string routing = ReadCollapsed(RoutingPath);
        string versions = ReadCollapsed(VersionsPath);

        // Assert
        using (new AssertionScope())
        {
            FloorShape.IsMatch(Floor).Should().BeTrue(
                because: "a published floor is a released package version; a placeholder cannot merge");
            resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("description").GetString().Should().Contain($"(CrtProcessBuilder {Floor} or newer; only when the FULL create-business-process contract names webService)",
                because: "the manifest description states the same package floor and clio gate as the article and the catalog; a client picking guides by description would otherwise build the block on a clio that refuses it");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("topicId").GetString().Should().Be($"creatio.{ItemId}",
                because: "the topic is the stable cross-library identity of the guide");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the block ships enabled; a gated guide would be hidden while the tool accepts the block");
            resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
                because: "the guide was born after the v0 migration and has no v0 route to preserve");
            requiredItems.Should().Contain(ItemId,
                because: "the library requires every item it publishes for the process tools");
            requiredUris.Should().Contain($"docs://knowledge/com.creatio.clio/{ItemId}",
                because: "the requirement lists the canonical route next to the item id");
            raw.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "a banner obliges process-modeling to index the guide, and that article has no room left under the inline rule");

            guide.Should().Contain("This guide owns the `webService` block of the Call web service element",
                because: "the article is the one owner of the block");
            guide.Should().Contain("`webService {service, method, timeoutSeconds?, ignoreErrors?}`, on `create-business-process`, `addElement` and `setElement`",
                because: "the contract anchor names every member and every route that takes the block");
            guide.Should().Contain($"Needs CrtProcessBuilder {Floor} or later on the environment.",
                because: "an older package refuses the type, so the floor gates every instruction below it");
            guide.Should().Contain(ClioGate,
                because: "an older clio refuses the block in its pre-send key check, so the package floor alone does not make the block usable, and only the FULL contract tells the two clio releases apart");
            guide.Should().Contain("request parameter `title` (body, header, query, path or cookie alike) -> input `title_In`; response parameter `status` -> output `status_Out`",
                because: "the stored names are what every mapping addresses");
            guide.Should().Contain("addressed dotted: `items_In.name_In`, at any depth",
                because: "a nested item is addressed by its dotted path");
            guide.Should().Contain("No aliases: the service's own name `title` is not an element parameter.",
                because: "a mapping onto the service's name instead of the stored one is refused");
            guide.Should().Contain("Both are REQUIRED on `create-business-process` and `addElement`; a sole method is not picked for you.",
                because: "the builder, unlike the designer, never selects a method by itself");

            catalog.Should().Contain($"- `webService` (Call web service), from CrtProcessBuilder **{Floor}** and only when the FULL contract names it:",
                because: "the catalog owns what builds today and from which package, and an older clio refuses the block, so the row carries the guide's contract gate");
            catalog.Should().Contain("`process-web-service` owns its `webService` block.",
                because: "the catalog names the owner of every block it lists");
            catalog.Should().Contain("`WebServiceUserTask` (\"Call web service\" in list-user-tasks) is a legacy SOAP task, REFUSED too: build `webService` (`process-web-service`).",
                because: "list-user-tasks shows the legacy task under the element's caption, and the data-id paragraph is where a task name is mapped to a build type");
            details.Should().Contain($"BUILDABLE from CrtProcessBuilder **{Floor}** via `type:\"webService\"`",
                because: "the element entry carries the floor, as every entry that changed from read-only to buildable does");
            details.Should().Contain("(`{service, method, timeoutSeconds?, ignoreErrors?}`), only when the FULL contract names it; `process-web-service` owns it.",
                because: "the element entry names the owner of its block");
            routing.Should().Contain("(`webService`; build it only when the FULL contract names it):",
                because: "routing carries the guide's contract gate, as it does for the other blocks a released clio may not know");
            versions.Should().Contain("a Call web service element describe reports unresolved -> fix it BEFORE any modify: `process-web-service`",
                because: "an unrelated modify of a process with a drifted element saves its service parameters away, and the edit path reaches process-versions, not the owner");
            routing.Should().Contain("-> name=process-web-service",
                because: "an article nothing routes to is never read");
        }
    }

    [Test]
    [Description("The guide states describe's webService member, the stored tree, the resolution flags and issues, the replay recipe, and that no service default is ever shown.")]
    public void Guide_ShouldStateTheReadBack()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "returns the element's `webService` block: `service` (the name; null when the stored service is not registered), `method`, `timeoutSeconds`",
            "the describe-only `serviceUId`, `serviceResolved`, `methodResolved`, `parameters` (every stored parameter, unbound inputs included, with `tag`, `direction`, `type`, nested `items` and the `required`, `bound` and `result` flags; no values) and `issues`",
            "A bound input's value comes back in the element's generic `parameters`.",
            "Nothing the SERVICE stores - its base URI, authentication, header or parameter defaults - comes back in any answer.",
            "To resubmit, send the block back unchanged - the describe-only members are ignored with a warning - except: a fractional `timeoutSeconds` is refused on write (leave it out on `setElement` to keep the stored timeout); `service: null` leaves a method-only block, which is refused; a stored method whose case differs from the service's makes the block a retarget, not a no-op."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "the read-back is how an agent learns the generated names and how it replays a block. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide states how a composite body is mapped, that a formula item needs its list mapped, when to use RequestBody, that it replaces the generated body, and that nothing checks or warns about it.")]
    public void Guide_ShouldStateTheRequestBodyRules()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "Set an input through the ordinary mapping surface (`process-parameters`): `mappings[]` on create, `addMapping` on modify, target `elementName` + `elementParameter`.",
            "the list is bound to that collection too, one request item per source row (rule P1, `process-files`)",
            "An item fed by a FORMULA that reads row items does NOT bind its list: map the list itself to the source collection as well, or the formula is evaluated outside any row. Nothing warns about this for a top-level list; an inner list draws the unsourced-inner-list notice.",
            "when non-null it REPLACES the whole body the element would build from its body parameters; headers, query, path and cookies are still built from theirs.",
            "An empty string from a formula or parameter replaces it too (an empty body is sent); a constant \"\" counts as unset.",
            "Use it for a method with no body parameter (as the shipped `ActionGeminiSearch` does with a formula)",
            "Nothing checks it as JSON, and nothing warns when it and body parameters are both bound."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "the builder neither checks a raw body nor warns about a formula item with an unbound list, so the article is the only place these rules live. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide states that an object-array response is read into a declared Collection of at most two levels, that an item mapping binds its collections, that a scalar list and a list of objects never feed each other, and what is not buildable yet.")]
    public void Guide_ShouldStateTheResponseRules()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "declare a process `Collection` with `itemProperties` (at most TWO collection levels, `process-parameters`), then map its items from the output's items",
            "Mapping an item binds the collections above it too (rule P1).",
            "A scalar list (`tags_In`) cannot take rows from a list of objects, nor the reverse: the types differ, refused.",
            "Not buildable yet: a scalar-array output into a process parameter, a third collection level",
            "A process parameter that feeds an input or takes an output: declare it with `typeFromElement` + `typeFromElementParameter` (`process-parameters`), NEVER a guessed `type`: a mapping across type groups is refused. A list is refused as a mirror: declare a Collection"
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "the declared Collection is the only response route that builds; the others are refused or shapeless. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide states resolution and its refusals, the retarget replace with its guard, the generic-route and multi-instance refusals, SOAP, the drift signals and the designer save that deletes the parameters, with the remedy first.")]
    public void Guide_ShouldStateResolutionReplaceAndRefusals()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "The service resolves by UId, then by name (exact, else ignoring case), then by caption (ignoring case); the first step that matches decides.",
            "The CANONICAL spelling is stored",
            "an unknown service (the refusal lists 30 services with caption and up to 8 methods, 70 more with caption, then a count - one not listed still resolves by exact name or UId; one shown as \"methods not readable\" IS registered, never register it again - and says to register a missing one in Studio > Web services)",
            "a name or caption matching several services (listed with methods; send the exact name or the UId)",
            "an unknown method (the refusal lists the service's methods)",
            "The generic `userTask` route REFUSES `WebServiceUserTask`: a legacy SOAP task, not this element.",
            "(REST, or SOAP: built with REST's parameter rules, with a notice)",
            "`timeoutSeconds`: whole seconds, an overall limit; `0`, or absent on create and `addElement`: none (absent on `setElement`: kept).",
            "EVERY parameter of the element is replaced by the new method's and every input binding on them is dropped, `RequestBody` included;",
            "the answer lists every dropped input binding WITH its value, as `addMapping` `mapping` objects, so one call re-applies those whose target name the new method still has; a binding no mapping can write (a system value or setting, a stored mapping) is listed apart, with its stored source: re-bind it in the Process Designer;",
            "a service change ALWAYS renames the caption to `<service caption, or its name> <N>`",
            "a registered service that cannot be read (open and save it in Studio > Web services)",
            "the Process Designer adds that dependency when it saves (while `AutoAddPackageDependenciesInProcesses` is on, the default), this tool does not.",
            "REFUSED, nothing written, while anything - another element, a process parameter, a flow condition, a formula - reads ANY other parameter of the element: an output, a nested item, an input, `RequestBody`.",
            "Remove or re-point those uses in a SEPARATE, earlier call",
            "A multi-instance Call web service element refuses the `webService` block; mappings onto it still apply, and describe shows it with an issue.",
            "UNLESS the stored method differs in case from the service's: then it is a retarget, guarded as below.",
            "`Success`, `Response` and `StatusCode` keep their identity here, so their readers stay valid (the designer re-creates those three under new ids, leaving their readers dangling)",
            "`describe-business-process` reports `serviceResolved: false` or `methodResolved: false` with a sentence in `issues`: the call fails at run time, `ignoreErrors` or not. An issue saying the service could not be read or checked means UNKNOWN, not failing.",
            "Every `modify-business-process` request warns for EVERY such element of the process, touched or not; a settings-only change does not resolve it.",
            "In the Process Designer, when the service exists but lacks the stored method, OPENING the element deletes EVERY parameter of that element, `Success`, `Response` and `StatusCode` included, with their mapping rows; closing its panel marks it invalid, and a designer save keeps that.",
            "When the service itself is gone, the load has already removed the service parameters, and the designer leaves those three.",
            "So FIX FIRST, before anyone opens the element in the designer or makes an unrelated modify",
            "A retarget is refused while anything reads a parameter of the element, a removed one too; a read of a removed one also makes Creatio's pre-save check refuse EVERY modify, an unrelated one too, nothing saved.",
            "A request's load has already dropped the service inputs and their bindings, so a retarget lists at most a `RequestBody` binding. To keep them (before any save), restore the method in Studio > Web services, or install the package that carries the service (one registered again gets another UId).",
            "Adding the parameter does not repair it, nor does re-pointing the element; the notice names the elements and the order: restore as above and repeat (unless the process was saved in the designer since); else remove or replace every read in an earlier request, then retarget and re-bind the reads in one request.",
            "`methodResolved: false` is the signal (an unreadable service: unknown, above).",
            "`setElement` with `elementName` and `elementUpdate.webService`, top-level elements only (a nested one: the designer):",
            "Omitted members keep their stored values (a service change resets the timeout, below). `method` alone: another method of the same service, refused while the stored service is not registered or readable.",
            "`service` naming ANOTHER service without `method`: refused with that service's methods; the SAME service and nothing else: refused, nothing to change.",
            "an element or parameter marked invalid (e.g. a required input not set)"
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "these are the refusals an agent must recognise and the one loss it must prevent before a designer save. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide states the result outputs, the timeout, ignoreErrors and its feature gate, an HTTP error answer versus no answer, and the inner-list caveat with its remedy.")]
    public void Guide_ShouldStateTheRunTimeFacts()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "`Success` (Boolean, out), `Response` (the response body, Text, out), `StatusCode` (the HTTP status code, Integer, out), `RequestBody` (raw request text, Text, in)",
            "An HTTP error answer (404, 500) never fails the element: it completes with `Success` false and that `StatusCode`, whatever `ignoreErrors` is",
            "with `ignoreErrors` off the element and the process FAIL; with it on the element completes with `Success` false and `Response` holding the error text, and `StatusCode` is NOT written.",
            "While feature `DisableIgnoreErrorsInWebServiceUserTask` is on, `ignoreErrors: true` is saved, with a notice, but a failing call still stops the process.",
            "It does NOT cover computing the inputs (an empty system setting behind `[#SysSettings.<Code>#]` fails the element), an unresolved service or method, or reading the response.",
            "Timeout: absent or `0`, only the method's own timeout applies, to each attempt, and retries repeat it. `timeoutSeconds` adds an overall limit over the whole call, retries included; it cannot lengthen an attempt beyond the method's timeout. When it expires, the call fails like a call with no answer, unless an earlier attempt had an HTTP error answer: that answer completes the element.",
            "Creatio builds without the platform fix for arrays inside arrays (10.1.37 among them), or with feature `DisableParametersWithNestedArrays` on, send NO value for that inner list. Bind the inner list itself to a collection, or build the body in `RequestBody`.",
            "While application setting `Feature-ProcessParameterCollections` is on, a name or tag repeated in one top-level list's tree: in a request list a repeated name stops the element once two of those items carry a value, a repeated tag sends one value; in a response list a repeated tag stops it when the result is stored, a repeated name keeps one value.",
            "A compiled (not interpretable) process applies neither `timeoutSeconds` nor `ignoreErrors`.",
            "Measured on Creatio 10.1.37 (.NET Framework) on 2026-11-03 (designer-built elements):",
            "Every other run-time rule is read from the Creatio core source and the designer client code, not measured."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "none of these is visible from a green build. Missing: " + string.Join(" | ", missing));
    }

    [Test]
    [Description("No process article and no manifest description still claims that webService is read-only or cannot be built.")]
    public void NoProcessArticleOrManifestDescription_ShouldClaimWebServiceIsUnbuildable()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string catalog = ReadCollapsed(CatalogPath);
        string details = ReadCollapsed(CatalogDetailsPath);
        string modeling = ReadCollapsed(ModelingPath);

        // Act
        string catalogDescription = Collapse(source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-element-catalog")
            .GetProperty("description").GetString()!);
        string readOnlyInDescription = Regex.Match(catalogDescription, @"while (?<list>[^.]+?) remain read-only").Groups["list"].Value;
        string notBuildable = Regex.Match(catalog, @"NOT yet buildable(?<list>.+?)== Building from a data-id").Groups["list"].Value;
        string detailsEntry = Regex.Match(details, @"- `webService` Call web service(?<entry>.+?)- `callActivity`").Groups["entry"].Value;
        string cannotCreate = Regex.Match(modeling, @"Constructs the builder cannot create \((?<list>[^)]+)\)").Groups["list"].Value;

        // Assert
        using (new AssertionScope())
        {
            notBuildable.Should().NotBeEmpty(because: "anti-vacuity: the catalog lists what does not build");
            notBuildable.Should().NotContain("webService", because: "the element is buildable from the floor");
            detailsEntry.Should().NotBeEmpty(because: "anti-vacuity: the details catalog carries the entry");
            detailsEntry.Should().NotContain("READ-ONLY", because: "the read-only marker would now be a lie");
            cannotCreate.Should().NotBeEmpty(because: "anti-vacuity: process-modeling lists what the builder cannot create");
            cannotCreate.Should().NotContain("webService", because: "the builder creates it now");
            readOnlyInDescription.Should().NotBeEmpty(because: "anti-vacuity: the description lists what remains read-only");
            readOnlyInDescription.Should().NotContain("webService",
                because: "a manifest description is what an MCP client shows an agent choosing a guide");
            catalogDescription.Should().Contain("Call web service (webService, only when the FULL contract names it; owner process-web-service)",
                because: "the description lists what builds today, with the guide's contract gate, and names the owner");
            detailsEntry.Should().Contain("outputs Success, Response + Http status code",
                because: "Response is one of the three outputs every Call web service element carries");
        }
    }

    [Test]
    [Description("Routing sends a process call of a registered web service to process-web-service and tells it apart from writing a C# configuration web service.")]
    public void Routing_ShouldSeparateTheProcessElementFromConfigurationServices()
    {
        // Arrange
        string routing = ReadCollapsed(RoutingPath);

        // Act
        string row = Regex.Match(routing, @"- CALL a REST or SOAP web service .+?name=portal-service-routing\)").Value;

        // Assert
        using (new AssertionScope())
        {
            row.Should().NotBeEmpty(because: "the row routing the Call web service element must exist in one piece");
            row.Should().Contain("-> name=process-web-service;",
                because: "the row that describes the element must route to its guide, not only mention it");
            row.Should().Contain("registered in Studio > Web services from a process",
                because: "the row is about calling a registered service from a process");
            row.Should().Contain("writing a C# configuration web service that Creatio exposes is not this -> name=configuration-webservice",
                because: "\"web service\" otherwise lands on the C# backend guides");
            row.Should().Contain("name=configuration-webservice-tests",
                because: "the configuration web service tests guide is the other half of that backend topic");
        }
    }

    [Test]
    [Description("The guide and its manifest description carry no internal task key, spec path, URL, host or test-stand name, no placeholder boundary, and no example fills a key-, token- or secret-looking parameter with a constant, whatever the member order.")]
    public void Guide_ShouldCarryNoInternalReferenceOrSecret()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string article = ProcessGuideSet.Read(repositoryRoot, ArticlePath);
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string description = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId)
            .GetProperty("description").GetString()!;
        string guide = article + "\n" + description;

        // Act
        bool jiraKey = Regex.IsMatch(guide, @"\b[A-Z][A-Z0-9]+-\d+\b");
        bool url = Regex.IsMatch(guide,
            @"https?://|\blocalhost\b|\b\d{1,3}(?:\.\d{1,3}){3}(?::\d+|/)|\b(?:[a-z0-9-]+\.)+(?:com|net|org|io|dev|local|cloud|ru|ua)\b",
            RegexOptions.IgnoreCase);
        bool specPath = Regex.IsMatch(guide, @"spec/|\.cs\b|docs/knowledge|packages/|\b[A-Z]:\\", RegexOptions.IgnoreCase);
        bool standName = Regex.IsMatch(guide, @"UsrWsc|Usr\w*Probe\b|\beng-?\d{4,}", RegexOptions.IgnoreCase);
        string[] placeholders = [.. Regex.Matches(guide, @"<[^<> ]*TBD[^<> ]*>").Select(match => match.Value).Distinct()];
        string[] mappingEntries = [.. Regex.Matches(article, @"\{[^{}]*""elementParameter""[^{}]*\}").Select(match => match.Value)];
        // A constant reaches a key-named input either as a `value` member or as a string literal inside an `expression`
        // (an escaped double quote or a single quote in the formula text).
        string[] constantSecrets = [.. mappingEntries
            .Where(entry => Regex.IsMatch(entry, @"""value""\s*:")
                || Regex.IsMatch(entry, @"""expression""\s*:\s*""(?:[^""\\]|\\.)*(?:\\""|')(?:[^""\\]|\\.)*"""))
            .Select(entry => Regex.Match(entry, @"""elementParameter""\s*:\s*""(?<name>[^""]+)""").Groups["name"].Value)
            .Where(name => Regex.IsMatch(name, "key|token|secret|password|authorization|signature", RegexOptions.IgnoreCase))];

        // Assert
        using (new AssertionScope())
        {
            mappingEntries.Should().NotBeEmpty(because: "anti-vacuity: the secret scan reads the article's mapping examples");
            jiraKey.Should().BeFalse(because: "published guidance references no internal task");
            url.Should().BeFalse(because: "the guide names no stand, host or issue link");
            specPath.Should().BeFalse(because: "the guide names no internal file path of the clio repository or the package");
            standName.Should().BeFalse(because: "the test services of the development stand are not product names");
            placeholders.Should().BeEmpty(
                because: "a placeholder boundary cannot merge: the release gate refuses it");
            constantSecrets.Should().BeEmpty(
                because: "a key or token comes from a system setting or a process parameter; an example with a constant is what an agent copies");
        }
    }

    [Test]
    [Description("The guide tells an agent what to do when the task names no service: find it through the refusal, ask when several fit, stop when none does and say what registration needs, build in two steps, and take keys from a setting or parameter.")]
    public void Guide_ShouldStateTheTaskWithoutDetails()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "Find the service and method through ONE refusal: a build naming an unknown service (and any `method`) saves nothing and lists the registered services (bounds above). Listed without methods? Probe it with an unknown `method`: that refusal lists them.",
            "when several fit, ASK the user which; never guess between them.",
            "When none fits, STOP: no process, no placeholder element, no Script task in its place.",
            "what that needs: the base URL, the method and its parameters, and the authentication. The user enters any credential there, not you.",
            "Build in two steps: the element first, then the mappings, from the names the answer and describe report.",
            "is filled from a system setting - an `expression` `[#SysSettings.<Code>#]` (`process-formulas`) - or from a process parameter, NEVER from a constant",
            "When the service itself defaults the key from a system setting (the shipped `GeminiApiSearch` does), leave that input unbound: an unbound input is skipped."
        ];
        string[] missing = [.. clauses.Where(clause => !guide.Contains(clause, StringComparison.Ordinal))];

        // Assert
        missing.Should().BeEmpty(
            because: "an agent with no service named either finds one through the refusal or stops; a placeholder process is the failure. Missing: "
                + string.Join(" | ", missing));
    }

    [Test]
    [Description("The guide stays inside its design target and the 19,000-byte inline reply rule.")]
    public void Guide_ShouldStayInsideItsDesignTarget()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        int responseCharacters = ProcessGuideResponseSizeTests.ResponseSize(repositoryRoot, ArticlePath);
        int replyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(ProcessGuideSet.Read(repositoryRoot, ArticlePath));
        TestContext.WriteLine($"{ItemId}: {responseCharacters:N0} response characters of {DesignTargetResponseCharacters:N0}, "
            + $"{replyBytes:N0} reply bytes of {GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes:N0}");

        // Assert
        responseCharacters.Should().BeLessThanOrEqualTo(DesignTargetResponseCharacters,
            because: "a larger addition should be reviewed, or split, rather than absorbed");
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline rule is spilled to a file and read back in pieces");
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

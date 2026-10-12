using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// <c>process-digest</c> is the ROUTING CARD of the process domain (ENG-102798): the build recipe in a few
/// lines, the D1/D2/D4 check and N1-N10 in short form, the buildable types, and a table naming the ONE article
/// per element. It replaces four owner articles for an ordinary build, and it owns no rule.
/// </summary>
/// <remarks>
/// <para>
/// Why a card and not a digest: the previous version (ENG-100155) restated the four owners block by block at
/// about 14 K, and CAADT runs read it ON TOP of the topic articles - guidance reads per test did not go down.
/// About 61% of a run's cost is the cache-read of its context, so every kilobyte read early is paid again on
/// every later request. The card earns its place only while it is small and sends the agent to the element
/// articles it needs, and nowhere else.
/// </para>
/// <para>
/// What this fixture proves: the card stays inside its budget; every article it routes to is one
/// <c>get-guidance</c> can serve; the few clauses it still restates are carried verbatim by their owner; its
/// buildable list and stop-word list agree with the owners; and every rule the 14 K digest used to restate is
/// still stated by its owner (<see cref="RulesHandedBackToTheirOwners"/>), so shrinking the card lost nothing.
/// Like every clause table in this suite these are TRIM ALARMS: they fail when a clause is reworded or dropped,
/// not when a contradiction is written around an intact one.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ProcessDigestTests
{
    private const string DigestItemId = "process-digest";

    /// <summary>
    /// The card budget: 3 KiB of article text as clio writes it (JSON-escaped, UTF-8, relaxed encoder -
    /// <see cref="GuidanceInlineReplyBudgetTests.ReplyBytes"/>), so about 4.3 KB with the reply envelope. The
    /// 14 K digest this replaced was read on top of the articles it summarised; at this size it costs less than
    /// a fifth of one element article. Shrink by routing, never by dropping a condition that changes what the
    /// agent does: the rule stays with its owner, and <see cref="RulesHandedBackToTheirOwners"/> proves it does.
    /// </summary>
    private const int MaxCardBytes = 3_072;

    /// <summary>
    /// The articles the routing table must name: one per element or need an ordinary build meets, plus the
    /// entry for an edit. Written down, so a row dropped from the card fails instead of shrinking the table.
    /// </summary>
    private static readonly string[] RequiredRoutes =
    [
        "process-data-elements", "process-read-data", "process-add-data", "process-delete-data",
        "process-data-source-filters", "process-parameters", "process-formulas", "process-branch-conditions",
        "process-activity-result-branches", "process-perform-task", "process-task-performer",
        "process-send-email", "process-approval", "process-open-edit-page", "process-access-rights",
        "process-sub-process", "process-activity-connections", "process-script-task", "process-versions"
    ];

    /// <summary>
    /// Clauses the card still restates, carried VERBATIM (whitespace-collapsed) by both the card and the owner.
    /// </summary>
    private static readonly (string Owner, string Clause)[] RestatedClauses =
    [
        ("process-sub-process-when", "A request produces ONE process"),
        ("process-modeling", "ONE START PER TRIGGER"),
        ("process-modeling", "descriptor as the JSON object itself"),
        ("process-modeling", "key on the schema NAME"),
        ("process-naming", "`<prefix><Object>_<Action>`"),
        ("process-naming", "PascalCase plus a `Parameter` suffix"),
        ("process-naming", "LABEL EVERY CONDITIONAL AND DEFAULT ARM"),
        ("process-naming", "`get-schema-name-prefix`"),
        ("process-naming", "<= 60"),
        ("process-modeling", "1.6.2.24"),
        ("process-modeling", "8.1.0.131"),
        ("process-modeling", "`CallUserTask`, `EmailUserTask`, `SendEmailUserTask`"),
        ("process-modeling", "`success: false`")
    ];

    /// <summary>
    /// Every rule the 14 K digest restated and the card no longer does, as its OWNER words it. When the card
    /// was cut, each of these was checked against the owner and found there (one, the `changeAccessRights`
    /// alias, was only on the card and was added to <c>process-element-catalog</c>). A row failing means an
    /// owner reworded or dropped a rule the card hands the reader to that owner for - update the row if the
    /// rule moved, never delete it to go green.
    /// </summary>
    private static readonly (string Owner, string Clause)[] RulesHandedBackToTheirOwners =
    [
        ("process-element-catalog", "`changeAccessRights`->ChangeAdminRightsUserTask"),
        ("process-sub-process-when", "D1 — the same work for EACH item of a set: multi-instance, do not ask"),
        ("process-sub-process-when", "D2 — the same fragment twice in the plan: one helper, do not ask"),
        ("process-sub-process-when", "D4 — two or more long phases: propose a split, and ask"),
        ("process-sub-process-when", "so an email to each contact of a set is D1 even when every contact gets the same text"),
        ("process-sub-process-when", "NOT D1 — work a set-based element does in one run, with no loop and no second process"),
        ("process-sub-process-when", "so a value computed from each record's own data is D1 again"),
        ("process-sub-process-when", "an Add data of Activity records is no substitute"),
        ("process-sub-process-when", "the same fragment of AT LEAST 3 elements at AT LEAST 2 places"),
        ("process-sub-process-when", "FIRST TRY A JOIN"),
        ("process-sub-process-when", "AND HOIST WHAT DOES NOT DEPEND ON THE BRANCH"),
        ("process-sub-process-when", "a lone task in an otherwise automatic flow is a step, not a phase"),
        ("process-sub-process-when", "SIZE WITHOUT PHASES: past about 30 elements"),
        ("process-sub-process-when", "is built as asked"),
        ("process-sub-process-when", "EXECUTION MODE — choose it, do not inherit it."),
        ("process-sub-process-when", "ORDER: create the helper FIRST, then the caller"),
        ("process-sub-process-when", "Never restructure an existing process without being asked"),
        ("process-sub-process-when", "D1 is the one exception, stated above: a requested change that is itself per-item."),
        ("process-sub-process-when", "An EXISTING process that repeats a fragment is left as it is"),
        ("process-sub-process-when", "EXISTING process: never propose restructuring one on your own, whatever its size."),
        ("process-element-catalog", "Events: `startEvent` (Simple start), `signalStart` (record signal: add/modify/delete), `endEvent`."),
        ("process-element-catalog", "(aliases `readData`->ReadDataUserTask, `changeData`->ChangeDataUserTask,"),
        ("process-access-rights", "(`changeAccessRights` / `ChangeAdminRightsUserTask`)"),
        ("process-element-catalog", "Send email: `sendEmail` (the Send email element / EmailTemplateUserTask) is BUILDABLE"),
        ("process-element-catalog", "Approval: `approval` (the Approval element / ApprovalUserTask) is BUILDABLE"),
        ("process-element-catalog", "Open edit page: `openEditPage` (the Open edit page element / OpenEditPageUserTask) is BUILDABLE"),
        ("process-element-catalog", "`preconfiguredPage` — Pre-configured page"),
        ("process-element-catalog", "`exclusiveGateway` (XOR) and `parallelGateway` (AND) ELEMENTS"),
        ("process-element-catalog", "`formulaTask` (Formula), from CrtProcessBuilder **1.6.3.16**. Below that version the type is refused outright, naming the ones it does build."),
        ("process-element-catalog", "`subProcess` (Sub-process), from CrtProcessBuilder **1.6.3.26**. Below that version the type is refused outright, naming the ones it does build."),
        ("process-element-catalog", "`scriptTask` (Script task), from CrtProcessBuilder **1.6.6.30**"),
        ("process-element-catalog", "FOUR user tasks have their own dedicated build type and must NOT be built as a generic `userTask`"),
        ("process-element-catalog", "`flows[]` takes `kind` (`sequence` | `conditional` | `default`) and, on a conditional flow, its `condition`."),
        ("process-element-catalog", "`flows[].label` on the build path"),
        ("process-activity-result-branches", "A conditional flow carries its predicate in ONE OF TWO disjoint slots, and the flow's SOURCE decides which"),
        ("process-activity-result-branches", "Among the elements clio can BUILD, four do: Perform task, Open edit page, Pre-configured page and Approval."),
        ("process-element-catalog", "Routing between the three page elements (Open edit page, Pre-configured page, Auto-generated page) is owned by `process-open-edit-page` — read its ROUTING section before choosing"),
        ("process-element-catalog-details", "`{body, and exactly ONE of resultProcessParameter | elementName + elementParameter}`"),
        ("process-element-catalog-details", "BOTH the body and a target are required when the element is created"),
        ("process-element-catalog-details", "`body` is the SAME dialect as a flow condition (`process-formulas` owns the vocabulary)"),
        ("process-element-catalog-details", "parameter may be referenced by NAME — `[#Amount#]`"),
        ("process-element-catalog", "DESTRUCTIVE — count the matching records, name the object and what the filter selects, and get an explicit yes BEFORE you build it"),
        ("process-access-rights", "and every grantee with its operations and level, and get an explicit yes before building"),
        ("process-element-catalog", "UNSUPPORTED through `create-business-process`"),
        ("process-element-catalog", "the INCLUSIVE and EVENT-BASED gateway elements, User dialog and Auto-generated page (REFUSED), timer/message start, intermediate events,"),
        ("process-element-catalog", "reading one COLUMN out of a read COLLECTION's items"),
        ("process-element-catalog", "NOT in a build descriptor: the \"Connected to\" links of an Activity a task creates."),
        ("process-element-catalog", "`useBackgroundMode` on any element that OFFERS it"),
        ("process-element-catalog", "Custom user-task compile rule"),
        ("process-naming", "Process `caption`: SENTENCE CASE"),
        ("process-naming", "so never hard-code `Usr`"),
        ("process-naming", "Add the package name only to break a real collision"),
        ("process-naming", "NO autonumber, NO random suffix, NO GUID fragment"),
        ("process-naming", "A process meant to be CALLED as a sub-process ends its code with `SubProcess`"),
        ("process-naming", "`elements[].caption`: ALWAYS set one explicitly on EVERY element"),
        ("process-naming", "Sentence case, <= 60 characters"),
        ("process-naming", "VERB FIRST"),
        ("process-naming", "the TRIGGER or the OUTCOME, a noun phrase"),
        ("process-naming", "Keep a gateway caption to TWO lines (~44 characters)"),
        ("process-naming", "DERIVE the code from the element's own `caption`"),
        ("process-naming", "`a`, `an`, `the`, `is`, `are`, `was`, `were`, `be`, `been`, `has`, `have`, `had`"),
        ("process-naming", "treat every punctuation mark as a WORD BOUNDARY"),
        ("process-naming", "Do not pad a code with the element's type name either"),
        ("process-naming", "a `signalStart` is `<Trigger>Signal` (`AccountAddedSignal`), a `startEvent` is `<Reason>Start` (`OnboardingRequestedStart`), an end event is `End<Reason>`"),
        ("process-naming", "\"Account is added\" -> `AccountAddedSignal`"),
        ("process-naming", "An element code MUST NOT contradict the element's RUNTIME type"),
        ("process-naming", "The `UserTask` postfix belongs to a user-task SCHEMA you author, NEVER to an element code"),
        ("process-naming", "never rename them"),
        ("process-naming", "Codes are STABLE"),
        ("process-naming", "Never derive a code from the clock, a GUID, a counter"),
        ("process-naming", "LABEL EVERY CONDITIONAL AND DEFAULT ARM, and leave an ordinary continuation bare"),
        ("process-naming", "Name the OUTCOME in the reader's language, never the condition"),
        ("process-naming", "A label of roughly 20 characters"),
        ("process-naming", "an EMPTY `label` CLEARS a designer's caption"),
        ("process-modeling", "ONE START PER TRIGGER the process must react to"),
        ("process-modeling", "the SIMPLE (manual) start only once"),
        ("process-modeling", "CrtProcessBuilder 1.6.2.24 or later on the environment AND clio 8.1.0.131 or later"),
        ("process-modeling", "(recommended) `validate-process-graph(graph)` -> fix every error-severity finding."),
        ("process-modeling", "RETIRED schemas (`CallUserTask`, `EmailUserTask`, `SendEmailUserTask`) as equal peers with no marker"),
        ("process-modeling", "always key on the schema NAME, never on a caption"),
        ("process-modeling", "resolved with ONE `get-target-package` call"),
        ("process-modeling", "Never guess one from `list-packages`"),
        ("process-modeling", "On `success: false` relay its error and ask; never substitute a package."),
        ("process-modeling", "Pass the descriptor as the JSON object itself"),
        ("process-modeling", "an output you can map FROM has `isOutput:true`"),
        ("process-modeling", "An OPTIONAL field the request did not mention stays OUT of the descriptor"),
        ("process-modeling", "BUSINESS decision the request did not make - who approves, who performs, whom to notify - ASK"),
        ("process-modeling", "Do NOT run `compile-creatio` to"),
        ("process-modeling", "unless its save WARNED it cannot run until compiled (a `scriptTask`; then pass `process-name`)"),
        ("process-modeling", "read status back with `describe-business-process`"),
        ("process-modeling", "On a file-design-mode (FSD) stand a built process is NOT runtime-active until it is loaded FS->DB and published"),
        ("process-modeling", "\"name\": \"UsrAccount_Onboard\", \"caption\": \"Account onboarding\""),
        ("process-modeling", "it is `name`, not `id`"),
        ("process-modeling", "Read it BEFORE editing or launching ANY existing process."),
        ("process-modeling", "ALWAYS `describe-business-process` first, and re-describe after the edit"),
        ("process-modeling", "You MUST read `isActiveVersion` from the describe output before ANY modify"),
        ("process-modeling", "offer `modify-business-process-as-new-version`"),
        ("process-modeling", "re-describe by `activeVersionSchemaUId`"),
        ("process-modeling", "The modify path runs NO structural validation"),
        ("process-modeling", "`removeElement` also CASCADES"),
        ("process-modeling", "Add the bridging `addFlow` in the same operations array"),
        ("process-modeling", "`removeFlow` takes `source` and `target` ONLY — MUST strip `kind` and `condition` first"),
        ("process-modeling", "the refusal ABORTS THE WHOLE atomic BATCH"),
        ("process-modeling", "Every modify re-draws the WHOLE diagram"),
        ("process-modeling", "prefer additive edits, do not remove or rewire"),
        ("process-modeling", "a target retarget clears the record filter"),
        ("process-modeling", "a supplied `add`/`remove` REPLACES that whole collection")
    ];

    [Test]
    [Description("The card stays a card: its text, as clio serves it, is under 3 KB.")]
    public void TheCard_ShouldStayWithinItsBudget()
    {
        // Arrange
        string text = ReadCard(ProcessGuideSet.FindRepositoryRoot());

        // Act
        int reply = GuidanceInlineReplyBudgetTests.ReplyBytes(text);
        int card = reply - GuidanceInlineReplyBudgetTests.EnvelopeAllowanceBytes;
        TestContext.WriteLine($"{DigestItemId}: {card:N0} bytes of card, about {reply:N0} bytes of reply");

        // Assert
        card.Should().BeLessThanOrEqualTo(MaxCardBytes,
            because: $"every build reads the card first and pays for it on every later request; at {card:N0} "
                + "bytes it is turning back into a digest. Route to the owner instead of restating it");
    }

    [Test]
    [Description("Every article the card names is one get-guidance can serve, and the routing table names every required route.")]
    public void EveryArticleTheCardRoutesTo_ShouldBeADeclaredGuide()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string text = ReadCard(repositoryRoot);
        HashSet<string> declared = ProcessGuideSet.DeclaredItemIds(repositoryRoot);

        // Act
        string table = Regex.Match(text, @"^== Element or need -> article ==\s*$(?<rows>.+)\z",
            RegexOptions.Multiline | RegexOptions.Singleline).Groups["rows"].Value;
        string[] missing = [.. RequiredRoutes.Where(id => !table.Contains($"`{id}`", StringComparison.Ordinal))];
        string[] undeclared = [.. Regex.Matches(text, @"`(?<id>process-[a-z-]+)`")
            .Select(match => match.Groups["id"].Value)
            .Distinct()
            .Where(id => !declared.Contains(id))];

        // Assert
        missing.Should().BeEmpty(
            because: "the routing table is what the card is for: an element it does not route sends the agent "
                + "back to the owners the card replaces. Missing: " + string.Join(", ", missing));
        undeclared.Should().BeEmpty(
            because: "the card sends the agent to these names with get-guidance, and an undeclared one answers "
                + "with availableGuides instead of the rule - another request paid for nothing. Found: "
                + string.Join(", ", undeclared));
    }

    [Test]
    [Description("Every clause the card restates is still carried verbatim by both the card and its owner.")]
    public void EveryRestatedClause_ShouldStillBeCarriedByTheCardAndItsOwner()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string card = Collapse(ReadCard(repositoryRoot));

        // Act
        string[] drifted = [.. RestatedClauses
            .Where(row => !card.Contains(Collapse(row.Clause), StringComparison.Ordinal)
                || !Collapse(Owner(repositoryRoot, row.Owner)).Contains(Collapse(row.Clause), StringComparison.Ordinal))
            .Select(row => $"{row.Owner}: \"{row.Clause}\"")];

        // Assert
        drifted.Should().BeEmpty(
            because: "the card restates these for an agent that will NOT open the owner; a clause reworded on "
                + "one side is a rule the card now states differently. Change BOTH in the same pull request "
                + "(CONTRIBUTING, 'The process digest'). Drifted: " + string.Join("; ", drifted));
    }

    [Test]
    [Description("Every rule the card stopped restating is still stated by the article that owns it.")]
    public void EveryRuleTheCardHandedBack_ShouldStillBeStatedByItsOwner()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string[] lost = [.. RulesHandedBackToTheirOwners
            .Where(row => !Collapse(Owner(repositoryRoot, row.Owner)).Contains(Collapse(row.Clause), StringComparison.Ordinal))
            .Select(row => $"{row.Owner}: \"{row.Clause}\"")];

        // Assert
        RulesHandedBackToTheirOwners.Should().HaveCountGreaterThan(100,
            because: "anti-vacuity: the table is the inventory of what the 14 K digest restated");
        lost.Should().BeEmpty(
            because: "the card now sends the agent to the owner for these rules, so an owner that drops or "
                + "rewords one loses it from the whole guide set. Lost: " + string.Join("; ", lost));
    }

    [Test]
    [Description("Every type the card lists as buildable is on the buildable list of process-element-catalog and not on its UNSUPPORTED list.")]
    public void EveryTypeTheCardListsAsBuildable_ShouldBeBuildableInTheCatalog()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string card = Collapse(ReadCard(repositoryRoot));
        string catalog = Collapse(Owner(repositoryRoot, "process-element-catalog"));
        string buildable = Regex.Match(catalog,
            @"== What you can build today \(create-business-process\) ==(?<list>.+?)NOT yet buildable").Groups["list"].Value;
        string unsupported = Regex.Match(catalog, @"NOT yet buildable(?<list>.+?)== Building from a data-id")
            .Groups["list"].Value;
        string typeList = Regex.Match(card, @"Types:(?<list>.+?)== Names").Groups["list"].Value;

        // Act
        string[] types = [.. Regex.Matches(typeList.Replace("(never as a userTask)", string.Empty), @"[a-z][A-Za-z]+")
            .Select(match => match.Value)
            .Where(type => type != "userTaskName")
            .Distinct()];
        string[] notBuildable = [.. types.Where(type => !buildable.Contains($"`{type}`", StringComparison.Ordinal))];
        string[] unsupportedToo = [.. types.Where(type => unsupported.Contains($"`{type}`", StringComparison.Ordinal))];

        // Assert
        buildable.Should().NotBeEmpty(because: "anti-vacuity: the catalog lists what builds today");
        types.Should().HaveCountGreaterThanOrEqualTo(15, because: "anti-vacuity: the card lists what builds today");
        unsupported.Should().NotBeEmpty(because: "anti-vacuity: the catalog lists what does not build");
        notBuildable.Should().BeEmpty(
            because: "the card stands in for the catalog's buildable list, so a type only the card lists is one "
                + "nobody owns. Not on the catalog's list: " + string.Join(", ", notBuildable));
        unsupportedToo.Should().BeEmpty(
            because: "an agent reading the card alone would build a type its owner refuses. Found: "
                + string.Join(", ", unsupportedToo));
    }

    [Test]
    [Description("The N5 drop-word list on the card is exactly the list process-naming owns.")]
    public void TheCardsDropWordList_ShouldEqualTheOwners()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string card = Collapse(ReadCard(repositoryRoot));
        string naming = Collapse(Owner(repositoryRoot, "process-naming"));

        // Act
        string[] onCard = Regex.Match(card, @"minus only (?<list>[a-z/]+);").Groups["list"].Value
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        string[] owned = [.. Regex.Matches(
                Regex.Match(naming, @"drop ONLY these words: (?<list>.+?)\. That list").Groups["list"].Value,
                @"`(?<word>[a-z]+)`")
            .Select(match => match.Groups["word"].Value)];

        // Assert
        owned.Should().NotBeEmpty(because: "anti-vacuity: process-naming N5 owns an exhaustive drop-word list");
        onCard.Should().Equal(owned,
            because: "N5 derives every element code from its caption, and the list is EXHAUSTIVE: a word the card "
                + "adds or omits changes the code an agent derives without opening process-naming");
    }

    [Test]
    [Description("The types the card says never to build as a userTask are exactly the FOUR the catalog gives a dedicated build type.")]
    public void TheCardsDedicatedTypes_ShouldEqualTheCatalogsFour()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string card = Collapse(ReadCard(repositoryRoot));
        string catalog = Collapse(Owner(repositoryRoot, "process-element-catalog"));

        // Act
        string[] onCard = [.. Regex.Match(card, @";(?<list>[^;]+)\(never as a userTask\)").Groups["list"].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal)];
        string[] owned = [.. Regex.Matches(
                Regex.Match(catalog, @"FOUR user tasks have their own dedicated build type(?<rest>.+?)\)\s*-")
                    .Groups["rest"].Value,
                "`type:\"(?<type>[A-Za-z]+)\"`")
            .Select(match => match.Groups["type"].Value)
            .Order(StringComparer.Ordinal)];

        // Assert
        owned.Should().HaveCount(4, because: "anti-vacuity: the catalog names FOUR dedicated build types");
        onCard.Should().Equal(owned,
            because: "a dedicated type built as a generic userTask is refused or builds an element that fails at "
                + "run time, and the card is the only place an ordinary build reads the list");
    }

    private static string ReadCard(string repositoryRoot) => Owner(repositoryRoot, DigestItemId);

    private static string Owner(string repositoryRoot, string itemId) =>
        ProcessGuideSet.Read(repositoryRoot, ProcessGuideSet.Declared(repositoryRoot)
            .Single(article => article.ItemId == itemId).SourcePath);

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

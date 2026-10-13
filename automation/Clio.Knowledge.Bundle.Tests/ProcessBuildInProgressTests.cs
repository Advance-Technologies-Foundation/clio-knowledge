using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the BUILD-IN-PROGRESS exemption (ENG-103019): a process family the agent created in this same task is
/// not an "existing process", so fixing, re-saving or launching it follows the build card and the element
/// articles, not the version path - within the limits the owner states (activation, live triggers, versions).
/// </summary>
/// <remarks>
/// <para>
/// Measured in CAADT (Copilot, claude-sonnet-5): process runs read process-versions and process-version-writes
/// (about 34 KB together) plus the owners the card says an ordinary build must not open - about 80 KB per run,
/// re-paid as cache-read on every later request - because every self-correcting modify of a process the agent
/// had just built read as an edit of an "existing process", and its <c>isActiveVersion</c> is TRUE, which
/// <c>process-modeling</c> answers with a confirmation.
/// </para>
/// <para>
/// The rule has ONE owner, <c>process-version-writes</c>: it owns the gates the exemption lifts (the session
/// questions and what counts as the explicit yes for an in-place edit of the active version), and
/// <c>process-versions</c> is already past the inline reply budget. Everyone else carries a pointer. These are
/// TRIM ALARMS like the rest of the suite: they fail when a clause is reworded or dropped.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ProcessBuildInProgressTests
{
    private const string OwnerItemId = "process-version-writes";
    private const string SectionHeading = "== A process you built in this task is a build in progress ==";
    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    /// <summary>The definition and its edge cases, as the owner words them.</summary>
    private static readonly string[] OwnerClauses =
    [
        "This section owns what \"existing process\" means for the version path and `isActiveVersion`.",
        "The unit is the FAMILY (V1, V2): a process is existing when its family was there before this task, or when another person has since edited, activated or launched it by hand.",
        "A family YOU created in this same task is a BUILD IN PROGRESS, within the limits below; if you cannot tell whether the family is yours or whether a limit applies, it is existing.",
        "Fixing, re-saving or launching it follows the build path, `process-digest` plus the element articles, NOT `process-versions` or this article",
        "do not ask the two questions below",
        "`process-modeling`'s `isActiveVersion` rule does not apply.",
        "Its other modify rules (describe first, `removeFlow`, `removeElement`), `core-rules` and the element articles' own confirmations still apply.",
        "A version you add to a family that existed before this task is NOT a build in progress: the family is existing, so the version articles and the `isActiveVersion` rule apply in full.",
        "The REQUEST asks for versions (save a revision, several revisions, one of them actual): a version task by request, so the version articles apply to those steps in full.",
        "ACTIVATING it is part of the build only when the request asks for a working or active process; otherwise the request has to say so.",
        "Once it is active AND any of its runs, from ANY start type (record signal, timer, message, signal, manual), can read or write data you did not create, an in-place modify of the active version is no longer exempt: save a new version, or get the `process-modeling` confirmation.",
        "A start that fires on no record still counts: a timer run of a changed graph updates other people's data on its own.",
        "An un-activated draft, or one activated moments ago whose runs can touch only data you created, stays exempt: a run you started does not make it existing.",
        "Built in an EARLIER task or session: existing"
    ];

    [Test]
    [Description("The owner defines the exemption and its edge cases in one section, before the session questions it lifts.")]
    public void TheOwner_ShouldDefineTheExemptionAndItsEdgeCases()
    {
        // Arrange
        string owner = Collapse(Article(OwnerItemId));

        // Act
        int section = owner.IndexOf(SectionHeading, StringComparison.Ordinal);
        int questions = owner.IndexOf("== Ask once, then behave predictably ==", StringComparison.Ordinal);
        string[] missing = [.. OwnerClauses.Where(clause => !owner.Contains(Collapse(clause), StringComparison.Ordinal))];

        // Assert
        section.Should().BeGreaterThan(0, because: "the exemption is a section of its owner, not a scattered aside");
        questions.Should().BeGreaterThan(section,
            because: "the exemption says 'do not ask the two questions below', so it has to sit above them");
        missing.Should().BeEmpty(
            because: "each clause decides whether a run reads ~34 KB of version guidance or asks a confirmation "
                + "nobody needs, or - the other way - whether a genuinely existing process loses its safety "
                + "rules. Missing: " + string.Join("; ", missing));
    }

    private const string DefinitionClause = "is a BUILD IN PROGRESS";

    /// <summary>
    /// The card's edit route, clause by clause: the exemption and its key limits (own family, created in this
    /// task, not live with runs of any start type that can touch data the agent did not create), the lifted confirmation, the owner
    /// ONLY for a limit, and the "unsure means existing" default.
    /// </summary>
    private static readonly string[] CardPointerClauses =
    [
        "edit/run YOUR family from THIS task: no `isActiveVersion` ask",
        "unless activating, or active with any run (any start) on others' data (`process-version-writes`).",
        "Else/unsure: `process-versions`, `process-modeling`."
    ];

    /// <summary>The same decision on the routing row that sends an edit to process-versions.</summary>
    private static readonly string[] RoutingPointerClauses =
    [
        "NOT existing: a family YOU created in THIS task.",
        "Fix, re-save or run it from name=process-digest, with no isActiveVersion confirmation",
        "unless you activate it, or it is active and any of its runs, from any start (record, timer, message, signal, manual), can read or write data you did not create (then name=process-version-writes).",
        "Unsure: existing"
    ];

    /// <summary>
    /// The record-only limit this clause replaced (PR #276 review): a timer, message or signal start fires on no
    /// record, so "a trigger that can fire on records" left a scheduled run of a changed graph exempt.
    /// </summary>
    private const string RecordOnlyLimit = "trigger that can fire on records";

    [Test]
    [Description("The live-process limit covers every start type, not only record triggers, in the owner and both pointers.")]
    public void TheLiveProcessLimit_ShouldCoverEveryStartType()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        (string Name, string Text)[] carriers =
        [
            (OwnerItemId, Collapse(Article(OwnerItemId))),
            ("process-digest", Collapse(Article("process-digest"))),
            (RoutingPath, Collapse(ProcessGuideSet.Read(repositoryRoot, RoutingPath)))
        ];

        // Act
        string[] recordOnly = [.. carriers
            .Where(carrier => carrier.Text.Contains(RecordOnlyLimit, StringComparison.Ordinal))
            .Select(carrier => carrier.Name)];

        // Assert
        carriers[0].Text.Should().Contain("from ANY start type (record signal, timer, message, signal, manual)",
            because: "anti-vacuity: the owner names every start type the limit covers");
        recordOnly.Should().BeEmpty(
            because: "a timer-, message- or signal-start process activated in this task runs against other "
                + "people's data without firing on any record; a record-only limit would let an in-place modify "
                + "of its active version skip the isActiveVersion confirmation");
    }

    [Test]
    [Description("Only the owner defines the exemption; the card, routing and process-modeling point at it.")]
    public void TheExemption_ShouldHaveOneOwnerAndPointers()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string card = Collapse(Article("process-digest"));
        string modeling = Collapse(Article("process-modeling"));
        string routing = Collapse(ProcessGuideSet.Read(repositoryRoot, RoutingPath));

        // Act
        string[] otherDefiners = [.. ProcessGuideSet.Declared(repositoryRoot)
            .Where(article => article.ItemId != OwnerItemId)
            .Where(article => Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath))
                .Contains(DefinitionClause, StringComparison.Ordinal))
            .Select(article => article.ItemId)];
        string cardEditRoute = Regex.Match(card, @"- edit/run .*\z").Value;
        string routingRow = Regex.Match(routing,
            @"- which VERSION of a process you are reading.*?(?= - save a change as a NEW version)").Value;

        // Assert
        Collapse(Article(OwnerItemId)).Should().Contain(DefinitionClause,
            because: "anti-vacuity: the single-owner check looks for the owner's own defining words");
        otherDefiners.Should().BeEmpty(
            because: "AGENTS.md gives every rule one owner; a second definition drifts from the first");
        foreach (string clause in CardPointerClauses)
        {
            cardEditRoute.Should().Contain(clause,
                because: "the card is what every build reads, so its edit route has to decide the common case - "
                    + "the agent's own fresh process - with NO further read: the exemption, its limits, that the "
                    + "isActiveVersion confirmation does not apply, and that 'unsure' means existing");
        }
        Regex.Matches(card, Regex.Escape("`process-version-writes`")).Should().ContainSingle(
            because: "the card sends the agent into the version owner only when a limit applies; a second mention "
                + "is an invitation to read ~17 KB for the case the card already decides");
        cardEditRoute.Should().Contain("others' data (`process-version-writes`)",
            because: "that single mention is attached to the limit, not to the exemption");
        routingRow.Should().Contain("ANY existing process -> name=process-versions first",
            because: "the version path stays the entry for a genuinely existing process");
        foreach (string clause in RoutingPointerClauses)
        {
            routingRow.Should().Contain(clause,
                because: "routing is read before any article, so the row that sends an edit to process-versions "
                    + "must decide the agent's own fresh process too, and send it to the owner only for a limit");
        }
        Regex.Matches(routingRow, Regex.Escape("name=process-version-writes")).Should().ContainSingle(
            because: "an unconditional owner pointer is what sent runs into the version articles; the row names "
                + "the owner once, for the limit");
        routingRow.Should().Contain("did not create (then name=process-version-writes)",
            because: "that single mention is attached to the limit, not to the exemption");
        modeling.Should().Contain(
            "You MUST read `isActiveVersion` from the describe output before ANY modify of a process that is not a build in progress (`process-version-writes`):",
            because: "the isActiveVersion rule is the line that otherwise overrides the exemption: a fresh build is "
                + "the active version, so TRUE would demand a confirmation, and on a file-design-mode stand FALSE would "
                + "forbid fixing it");
    }

    /// <summary>
    /// The owner's activation limit (PR #276 review): an agent asked for "a process that does X" builds it, and
    /// must not activate it unless the request asks for a working or active process. That limit lives only in
    /// the owner, so both pointers have to send ACTIVATING there - a route that names only the live-process
    /// limit lets the agent build, re-save and activate without ever reading it.
    /// </summary>
    private const string ActivationLimit =
        "ACTIVATING it is part of the build only when the request asks for a working or active process";

    [Test]
    [Description("The card and the routing row send an activation of the agent's own fresh process to the owner.")]
    public void ThePointers_ShouldSendActivationToTheOwner()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string card = Collapse(Article("process-digest"));
        string routing = Collapse(ProcessGuideSet.Read(repositoryRoot, RoutingPath));

        // Act
        string cardLimits = Regex.Match(card, @"- edit/run .*?\bunless (?<limits>.*?)\(`process-version-writes`\)")
            .Groups["limits"].Value;
        string routingLimits = Regex.Match(routing,
            @"NOT existing: a family YOU created in THIS task\..*?\bunless (?<limits>.*?)\(then name=process-version-writes\)")
            .Groups["limits"].Value;

        // Assert
        Collapse(Article(OwnerItemId)).Should().Contain(ActivationLimit,
            because: "anti-vacuity: the limit the pointers route to is stated by the owner");
        cardLimits.Should().NotBeEmpty(because: "anti-vacuity: the card's edit route names its limits");
        routingLimits.Should().NotBeEmpty(because: "anti-vacuity: the routing row names its limits");
        cardLimits.Should().Contain("activating",
            because: "the card is the only article a fresh build reads; without 'activating' among the cases it "
                + "routes to process-version-writes, an agent activates a process nobody asked to be active, and a "
                + "record trigger then fires on everyone's records");
        routingLimits.Should().Contain("activate",
            because: "routing is read before the card and decides the same case, so it routes activation too");
    }

    [Test]
    [Description("Every article this change touched stays inside the inline reply budget.")]
    public void TheArticlesCarryingTheExemption_ShouldFitTheInlineReplyBudget()
    {
        // Arrange
        string[] touched = [OwnerItemId, "process-modeling", "process-digest"];

        // Act
        (string ItemId, int Size)[] over = [.. touched
            .Select(itemId => (itemId, Size: GuidanceInlineReplyBudgetTests.ReplyBytes(Article(itemId))))
            .Where(article => article.Size > GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes)];

        // Assert
        over.Should().BeEmpty(
            because: "an article that spills out of the inline reply is read back in pieces, one request each - "
                + "the cost this exemption exists to remove");
    }

    private static string Article(string itemId)
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        return ProcessGuideSet.Read(repositoryRoot, ProcessGuideSet.Declared(repositoryRoot)
            .Single(article => article.ItemId == itemId).SourcePath);
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

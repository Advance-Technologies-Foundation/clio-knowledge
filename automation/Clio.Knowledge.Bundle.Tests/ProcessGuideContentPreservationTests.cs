using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the text against being lost by the move rather than by an edit.
///
/// ENG-96212 moved 100 KB of prose between files, and the extraction dropped the source's final line —
/// the base article ends WITHOUT a trailing newline, so a line count that trusts newline characters is
/// one short. The article that received the tail ended mid-sentence, and three connection constraints
/// (R1, R2, R10) existed nowhere under <c>guidance/</c>. The verification written alongside the split
/// used the same line bound, so it was blind to precisely the line it dropped and reported the move as
/// byte-for-byte complete. The whole suite was green.
///
/// Nothing here re-derives the source — it is gone at this head, and vendoring 100 KB to diff against
/// would be worse than the problem. Instead this pins the two things that failure actually cost: the
/// shape (an article that stops mid-sentence) and the payload (the rule set that vanished with the tail).
/// Both are cheap, and both would have failed on the defect as shipped.
///
/// ENG-96536 moved four more sections and inherited the shape check for free, but a payload check is per
/// move: for the new articles a later TRIM is invisible to everything else here. Size goes DOWN, the tail
/// still ends in a period, the section heading survives so no citation dangles, and the marker row for
/// that section skips its own owner. <see cref="MovedPayloads"/> is the pin for those four.
/// </summary>
[TestFixture]
public sealed class ProcessGuideContentPreservationTests
{
    private const string ConnectionRulesArticle = "guidance/mcp/guides/processes/activity-connections.md";

    [Test]
    [Description("No process article ends mid-sentence, which is what a truncated extraction leaves behind.")]
    public void NoProcessArticle_ShouldEndMidSentence()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        (string ItemId, string Tail)[] truncated = ProcessGuideSet.Declared(repositoryRoot)
            .Select(article => (article.ItemId, Tail: LastMeaningfulLine(repositoryRoot, article.SourcePath)))
            .Where(article => !EndsASentence(article.Tail))
            .ToArray();

        truncated.Should().BeEmpty(
            because: "a move that drops the last line of a range leaves the receiving article ending on a comma "
                + "or a half clause — which reads as prose, passes every size and reference check, and is only "
                + "visible if something looks at the tail. That is exactly how R1/R2/R10 were lost. Ending "
                + string.Join("; ", truncated.Select(a => $"{a.ItemId} with \"{Excerpt(a.Tail)}\"")));
    }

    [Test]
    [Description("The connection-rule catalog still carries every rule R1 through R17 and its closing summary.")]
    public void ConnectionRuleCatalog_ShouldCarryEveryRuleAndItsClosingSummary()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string article = ProcessGuideSet.Read(repositoryRoot, ConnectionRulesArticle);

        string[] missing = Enumerable.Range(1, 17)
            .Select(number => $"R{number}")
            .Where(rule => !Regex.IsMatch(article, $@"(?m)^{rule}\s"))
            .ToArray();

        missing.Should().BeEmpty(
            because: "R1-R17 are the connection contract validate-process-graph enforces a subset of; a rule "
                + $"that disappears takes a real constraint out of published guidance. Missing: {string.Join(", ", missing)}");

        // The closing cheat-sheet, and specifically the clause the split dropped. Pinned verbatim because
        // this is the text that was lost, and because summarising it here would let the summary drift from
        // the article while the test stayed green.
        article.Should().Contain("never ->start (R1)",
            because: "the closing quick-reference is the compressed form a reader scans instead of the catalog");
        article.Should().Contain("end is a sink, never a source (R2)",
            because: "this clause existed nowhere under guidance/ after the split and no test noticed");
        article.Should().Contain("event-based gateway out must hit a catch event (R10)",
            because: "it is the final clause of the article, and the final clause is what a line-bound "
                + "extraction drops");
    }

    private static string LastMeaningfulLine(string repositoryRoot, string sourcePath) =>
        ProcessGuideSet.Read(repositoryRoot, sourcePath)
            .Split('\n')
            .Select(line => line.TrimEnd())
            .LastOrDefault(line => line.Length > 0) ?? string.Empty;

    /// <summary>
    /// Sentence-final punctuation as these articles use it: a full stop, optionally closed by a bracket or
    /// a backtick. Deliberately narrow — a tail ending in a comma, a conjunction or an open bracket is the
    /// signature being looked for.
    /// </summary>
    private static bool EndsASentence(string line) =>
        Regex.IsMatch(line, @"[.!?][)`""']*$");

    private static string Excerpt(string line) =>
        line.Length <= 60 ? line : "…" + line[^60..];

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    /// <summary>
    /// One load-bearing clause per section ENG-96536 moved, each ANCHORED to the site it protects: the
    /// clause has to sit within <see cref="AnchorWindow"/> collapsed characters of the anchor phrase.
    ///
    /// An earlier version counted occurrences per article instead, and a count is not a location. Five
    /// mutations walked through it: move the second setFilter precondition out of its setElement bullet
    /// and re-add the phrase to the article's ownership preamble — count still 2, suite green; strip
    /// READ-ONLY here from two catalog rows and re-add it to the top-of-article caveat — count still 4,
    /// green; add a FIFTH read-only row carrying no marker — still 4, green, because the 4 was hard-coded
    /// rather than derived; delete the Activity.AllowedResult warning while the token survived in an
    /// ownership line — green. Each of those is the exact loss the row's own reason describes.
    ///
    /// Chosen the way the R1-R17 pins were: not a summary of the article, but the sentence whose loss a
    /// reader could not detect — a completeness claim, a refusal, or a default that reads as absent.
    /// Verbatim rather than paraphrased, so a reworded rule fails here instead of drifting quietly.
    /// </summary>
    private static readonly (string ItemId, string Anchor, string Clause, string Because)[] MovedPayloads =
    [
        ("process-data-source-filters", "`macro` vocabulary",
            "COMPLETE set — an unknown name is rejected at BUILD",
            "the macro vocabulary is only usable because it is stated to be exhaustive and validated at "
            + "build; trimmed to a sample, a reader has no way to tell a missing macro from an invalid one"),
        ("process-data-source-filters", "DayOfYearToday",
            "the ONLY DayOfYear macro that takes NO argument",
            "the one exception in that vocabulary, and the one an agent gets wrong by symmetry with its "
            + "six argument-taking neighbours"),
        ("process-data-source-filters", "may ONLY be a constant",
            "SIGNAL-START RESTRICTION",
            "the restriction that makes a signal filter different from every other filter; without it a "
            + "parameter reference is authored on a trigger evaluated before any instance exists"),
        ("process-data-source-filters", "ops `setFilter`",
            "REPLACES the element's whole filter",
            "the precondition on the one op this article owns that overwrites live configuration"),
        // Two rows, two anchors, because the rule is restated at two SITES. One row with a count of two
        // was satisfied by any two mentions anywhere, which is how deleting one of these bullets and
        // adding a mention to the preamble stayed green. CONTRIBUTING requires the precondition next to
        // the instruction, so the anchor is the instruction.
        ("process-data-elements", "clears the columns, sort AND record filter",
            "REPLACES the element's whole filter",
            "the readData retarget instructs a setFilter on a live process and carries the precondition "
            + "inline; losing it widens the records the element reads with nothing to warn the reader"),
        ("process-data-elements", "the stored record filter clears UNLESS",
            "REPLACES the element's whole filter",
            "the changeData retarget does the same, and there the widening is a bulk update on live "
            + "customer records"),
        ("process-data-elements", "include `entity` only to retarget",
            "WIDEN the trigger silently",
            "setSignal is the third clearing op in this article and the last to get a precondition; "
            + "without it a retargeted trigger fires on every record of the new object"),
        ("process-task-performer", "type \"role\" is THE way",
            "OwnerRole column and its Owner stays EMPTY",
            "the claim model. A reader who loses this reads the empty Owner back as an unassigned task and "
            + "\"fixes\" it by routing the team through OwnerId, which this article says is REFUSED"),
        ("process-task-performer", "Leaving both layers unset",
            "NOT an unassigned task",
            "leaving both layers unset silently assigns the process starter; there is no nobody state, and "
            + "the absence of a performer reads as a decision not yet made"),
        ("process-task-category", "GetResultParameterAllValues",
            "only for a ConstValue source",
            "the mechanism behind the whole article. Without it the rule is a style preference and the "
            + "expression form looks like a working alternative"),
        ("process-task-category", "Do NOT try to verify the degradation",
            "Activity.AllowedResult",
            "the trap: the column a reader reaches for to verify the degradation derives from conditional "
            + "flows, not from the category, so it confirms the wrong thing either way"),
        ("process-element-catalog", "Use the catalog below",
            "NOT yet buildable",
            "the article exists to say what cannot be built; silence here used to read as buildable, which "
            + "is the defect its own text records"),
        // The per-entry markers, one row per read-only entry, anchored on the entry itself. A count of
        // four was satisfied by four mentions anywhere and could not notice a fifth entry arriving
        // unmarked, which is what its own reason claimed to prevent.
        ("process-element-catalog", "`formulaTask`       Formula", "READ-ONLY here",
            "the per-entry marker on the entry a reader is most likely to reach for by mistake"),
        ("process-element-catalog", "`scriptTask`        Script task", "READ-ONLY here",
            "the per-entry marker on the entry whose C# pulls a compile in"),
        ("process-element-catalog", "`webService`        Call web service", "READ-ONLY here",
            "the per-entry marker on the entry with no other signal that it cannot be built"),
        ("process-element-catalog", "`callActivity`      Sub-process", "READ-ONLY here",
            "the per-entry marker on the entry whose children no read API shows"),
        ("process-element-catalog", "Add the element", "NOT in a build descriptor",
            "the OTHER cannot-build fact, and the one a short form of this list dropped: the Connected-to "
            + "links are not in a build descriptor at all, so the flagship \"a task connected to the "
            + "account\" shape has to bind them afterwards")
    ];

    /// <summary>
    /// The smallest unit of an article a reader takes in as one thing: a list item, or a paragraph when
    /// there is no marker. The articles delimit these themselves — a top-level <c>- </c> or a nested
    /// <c>* </c> starts a new one, a blank line ends one — and continuation lines are indented, so a
    /// wrapped item stays whole.
    ///
    /// This replaced a character window, which cannot work here: measured on the element catalog, the
    /// nearest OWN marker sits 95 collapsed characters from its anchor and the nearest FOREIGN one 117,
    /// so any window admitting the first admits the second with 22 characters to spare. The catalog rows
    /// are short and adjacent by design. Structure separates them; distance does not.
    /// </summary>
    private static readonly Regex ItemBoundary = new(@"(?m)^\s*(?:[-*]\s|\d+\.\s)|^\s*$",
        RegexOptions.Compiled);

    [Test]
    [Description("Each clause ENG-96536 moved is still next to the instruction it guards, not merely somewhere in the article.")]
    public void EachMovedSection_ShouldStillCarryItsLoadBearingClause()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        Dictionary<string, string> byItemId = ProcessGuideSet.Declared(repositoryRoot)
            .ToDictionary(article => article.ItemId,
                article => Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)));
        Dictionary<string, string[]> itemsByItemId = ProcessGuideSet.Declared(repositoryRoot)
            .ToDictionary(article => article.ItemId,
                article => Items(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)));

        string[] undeclared = [.. MovedPayloads
            .Select(payload => payload.ItemId)
            .Distinct(StringComparer.Ordinal)
            .Where(itemId => !byItemId.ContainsKey(itemId))];
        undeclared.Should().BeEmpty(
            because: "a pin against an article the manifest no longer declares checks nothing, and the "
                + "article being gone is the larger defect. Not declared: " + string.Join(", ", undeclared));

        // An anchor that stopped matching is its own defect: the site the clause guards has been reworded
        // or removed, and reporting only "the clause is missing" would send the reader looking for the
        // wrong thing. Both sides are collapsed, so a re-wrap that changes no words moves nothing.
        string[] lostAnchors = [.. MovedPayloads
            .Where(payload => !byItemId[payload.ItemId]
                .Contains(Collapse(payload.Anchor), StringComparison.Ordinal))
            .Select(payload => $"{payload.ItemId}: anchor \"{payload.Anchor}\" is gone, so nothing "
                + $"protects \"{payload.Clause}\" — {payload.Because}")];

        string[] adrift = [.. MovedPayloads
            .Where(payload => itemsByItemId[payload.ItemId]
                .Any(item => item.Contains(Collapse(payload.Anchor), StringComparison.Ordinal)))
            .Where(payload => !itemsByItemId[payload.ItemId].Any(item =>
                item.Contains(Collapse(payload.Anchor), StringComparison.Ordinal)
                && item.Contains(Collapse(payload.Clause), StringComparison.Ordinal)))
            .Select(payload => $"{payload.ItemId}: \"{payload.Clause}\" has left the item that says "
                + $"\"{payload.Anchor}\" — {payload.Because}")];

        lostAnchors.Should().BeEmpty(
            because: "each pin names the SITE it protects, so an anchor that no longer matches means the "
                + "instruction moved or was reworded and the pin is now guarding nothing. Re-anchor it to "
                + "where the instruction lives now. " + string.Join("; ", lostAnchors));
        adrift.Should().BeEmpty(
            because: "a clause that has left the instruction it guards is lost to the reader even when it "
                + "still appears elsewhere in the article — the reader fetches one article and reads it in "
                + "order, and a precondition in the preamble does not stop a bulk update four kilobytes "
                + "further down. This is what a per-article occurrence COUNT could not see. "
                + string.Join("; ", adrift));
    }

    /// <summary>
    /// The article split into the units a reader takes in as one thing, each collapsed. A unit runs from
    /// one list marker or blank line to the next, so a wrapped item stays whole and two adjacent catalog
    /// rows stay apart.
    /// </summary>
    private static string[] Items(string article) =>
        [.. ItemBoundary.Split(article).Select(Collapse).Where(item => item.Length > 0)];
}

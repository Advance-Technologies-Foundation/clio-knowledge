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

    /// <summary>
    /// One load-bearing clause per section ENG-96536 moved, with the reason it is the clause that matters.
    /// Chosen the way the R1-R17 pins were: not a summary of the article, but the sentence whose loss a
    /// reader could not detect — a completeness claim, a refusal, or a default that reads as absent.
    /// Verbatim rather than paraphrased, so a reworded rule fails here instead of drifting quietly.
    /// </summary>
    private static readonly (string ItemId, string Clause, string Because)[] MovedPayloads =
    [
        ("process-data-source-filters", "COMPLETE set — an unknown name is rejected at BUILD",
            "the macro vocabulary is only usable because it is stated to be exhaustive and validated at "
            + "build; trimmed to a sample, a reader has no way to tell a missing macro from an invalid one"),
        ("process-data-source-filters", "the ONLY DayOfYear macro that takes NO argument",
            "the one exception in that vocabulary, and the one an agent gets wrong by symmetry with its "
            + "six argument-taking neighbours"),
        ("process-data-source-filters", "SIGNAL-START RESTRICTION",
            "the restriction that makes a signal filter different from every other filter; without it a "
            + "parameter reference is authored on a trigger evaluated before any instance exists"),
        ("process-data-source-filters", "REPLACES the element's whole filter",
            "the precondition on the one op here that overwrites live configuration; process-data-elements "
            + "restates it inline as a MUST and cites this article for the detail"),
        ("process-task-performer", "OwnerRole column and its Owner stays EMPTY",
            "the claim model. A reader who loses this reads the empty Owner back as an unassigned task and "
            + "\"fixes\" it by routing the team through OwnerId, which this article forbids"),
        ("process-task-performer", "NOT an unassigned task",
            "leaving both layers unset silently assigns the process starter; there is no nobody state, and "
            + "the absence of a performer reads as a decision not yet made"),
        ("process-task-category", "only for a ConstValue source",
            "the mechanism behind the whole article. Without it the rule is a style preference and the "
            + "expression form looks like a working alternative"),
        ("process-task-category", "Activity.AllowedResult",
            "the trap: the column a reader reaches for to verify the degradation derives from conditional "
            + "flows, not from the category, so it confirms the wrong thing either way"),
        ("process-element-catalog", "NOT yet buildable",
            "the article exists to say what cannot be built; silence here used to read as buildable, which "
            + "is the defect its own text records"),
        ("process-element-catalog", "READ-ONLY here",
            "the per-entry marker that carries that same distinction into the catalog rows")
    ];

    [Test]
    [Description("Each section ENG-96536 moved still carries the clause whose loss a reader could not detect.")]
    public void EachMovedSection_ShouldStillCarryItsLoadBearingClause()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        Dictionary<string, string> byItemId = ProcessGuideSet.Declared(repositoryRoot)
            .ToDictionary(article => article.ItemId,
                article => ProcessGuideSet.Read(repositoryRoot, article.SourcePath));

        string[] undeclared = [.. MovedPayloads
            .Select(payload => payload.ItemId)
            .Distinct(StringComparer.Ordinal)
            .Where(itemId => !byItemId.ContainsKey(itemId))];
        undeclared.Should().BeEmpty(
            because: "a pin against an article the manifest no longer declares checks nothing, and the "
                + "article being gone is the larger defect. Not declared: " + string.Join(", ", undeclared));

        string[] missing = [.. MovedPayloads
            .Where(payload => !byItemId[payload.ItemId]
                .Contains(payload.Clause, StringComparison.Ordinal))
            .Select(payload => $"{payload.ItemId} lost \"{payload.Clause}\" — {payload.Because}")];

        missing.Should().BeEmpty(
            because: "these clauses moved into new articles, where a later trim is invisible to every other "
                + "check here: the size goes DOWN, the tail still ends in a sentence, the heading survives "
                + "so no citation dangles, and the section's marker row skips its own owner. " 
                + string.Join("; ", missing));
    }
}

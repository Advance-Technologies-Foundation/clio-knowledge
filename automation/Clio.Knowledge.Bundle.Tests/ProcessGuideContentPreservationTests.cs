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
}

using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps internal task references out of every body the bundle publishes. An agent reading guidance
/// cannot open a Jira key or an issue thread, so a rule that says "fixed in clio#1574" or "until
/// ENG-12345 ships" gives it a boundary it cannot check. A version boundary is a clio version; evidence
/// is stated inline (what was measured, on which Creatio and clio version, when).
///
/// The scanned set is <c>bundle-source.json</c> itself (its descriptions reach agents through the catalog)
/// plus every <c>sourcePath</c> it declares, so an article added to the manifest is scanned without an edit
/// here.
/// </summary>
[TestFixture]
public sealed class InternalTaskReferenceTests
{
    /// <summary>
    /// References allowed to stay, as (published path, matched text, reason). Empty on purpose: no task
    /// reference is needed by a published body today. An entry must match a reference that still exists,
    /// so it cannot outlive the text it excuses.
    /// </summary>
    private static readonly (string SourcePath, string Text, string Reason)[] AllowedReferences = [];

    [Test]
    [Description("No published body references an internal task: Jira keys, GitHub issue or pull request URLs, repo#N, bare #N, or 'issue N'.")]
    public void PublishedBodies_ShouldNotReferenceInternalTasks()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string[] sourcePaths = PublishedPaths(repositoryRoot);

        sourcePaths.Should().NotBeEmpty(because: "a scan over no bodies would prove nothing");

        string[] found = sourcePaths
            .SelectMany(path => TaskReferenceScanner.Scan(ProcessGuideSet.Read(repositoryRoot, path))
                .Where(hit => !IsAllowed(path, hit.Text))
                .Select(hit => $"{path}:{hit.Line}: {hit.Text}"))
            .ToArray();

        if (found.Length > 0)
        {
            Assert.Fail(
                "Published guidance must not reference internal tasks. Replace a compatibility boundary with "
                + "the clio version that shipped the change, and state evidence inline (what was measured, "
                + "on which version, when) instead of linking the task. "
                + $"{found.Length} reference(s):{Environment.NewLine}"
                + string.Join(Environment.NewLine, found));
        }
    }

    [Test]
    [Description("Every allow-list entry still matches a reference in its body, so an entry cannot outlive the text it excuses.")]
    public void AllowedReferences_ShouldEachMatchAnExistingReference()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        string[] stale = AllowedReferences
            .Where(entry => !TaskReferenceScanner.Scan(ProcessGuideSet.Read(repositoryRoot, entry.SourcePath))
                .Any(hit => hit.Text == entry.Text))
            .Select(entry => $"{entry.SourcePath}: {entry.Text}")
            .ToArray();

        stale.Should().BeEmpty(because: "an allow-list entry whose reference is gone would silently excuse the "
            + "next reference that happens to match it");
    }

    [TestCase("Tracked as ENG-92761 until it ships.", "ENG-92761", TestName = "Jira key")]
    [TestCase("see https://creatio.atlassian.net/browse/ENG-1 for details", "ENG-1", TestName = "Jira key in a browse URL")]
    [TestCase("branch feature/ENG-102333-compile-timeout", "ENG-102333", TestName = "Jira key in a branch name")]
    [TestCase("See https://github.com/Advance-Technologies-Foundation/clio/issues/1364 for evidence.",
        "https://github.com/Advance-Technologies-Foundation/clio/issues/1364", TestName = "GitHub issue URL")]
    [TestCase("[comment](https://github.com/o/r/issues/1619#issuecomment-5726093804)",
        "https://github.com/o/r/issues/1619", TestName = "GitHub issue comment URL")]
    [TestCase("Merged in https://creatio.ghe.com/engineering/creatio-ui/pull/42/files.",
        "https://creatio.ghe.com/engineering/creatio-ui/pull/42", TestName = "GHE pull request URL")]
    [TestCase("introduced by clio#1574;", "clio#1574", TestName = "repo#N")]
    [TestCase("the lab for clio-knowledge#161 used", "clio-knowledge#161", TestName = "hyphenated repo#N")]
    [TestCase("issue Advance-Technologies-Foundation/clio#1138 captured", "Advance-Technologies-Foundation/clio#1138",
        TestName = "owner/repo#N")]
    [TestCase("builds containing the #1138 fix", "#1138", TestName = "bare #N")]
    [TestCase("The Clio #968 investigation", "#968", TestName = "bare three-digit #N")]
    [TestCase("clio#1575–#1579: discovery", "#1579", TestName = "bare #N closing a range")]
    [TestCase("verified for clio issue 1249: both", "issue 1249", TestName = "issue N without hash")]
    [TestCase("FSM descriptor compatibility (Clio issue #1416)", "issue #1416", TestName = "issue #N")]
    [TestCase("as reported in pull request 57", "pull request 57", TestName = "pull request N")]
    public void Scan_ShouldReport_ATaskReference(string text, string expected)
    {
        TaskReferenceScanner.Scan(text).Select(hit => hit.Text)
            .Should().Contain(expected);
    }

    [TestCase("UTF-8 bytes", TestName = "UTF-8")]
    [TestCase("an independent SHA-256 checksum", TestName = "SHA-256")]
    [TestCase("pass ISO-8601, e.g. 2026-05-01", TestName = "ISO-8601")]
    [TestCase("\"value\": \"CRM-000042\"", TestName = "example data with a leading zero")]
    [TestCase("E52BD583-7825-E011-8165-00155D043204 is ActivityType Call", TestName = "GUID")]
    [TestCase("the id starts E52BD583-7825-E011-8165…", TestName = "GUID fragment")]
    [TestCase("color: #333;", TestName = "three-digit decimal colour")]
    [TestCase("\"backgroundColor\": \"#999\"", TestName = "quoted decimal colour")]
    [TestCase("border: 1px solid #000000", TestName = "colour with a leading zero")]
    [TestCase("text #808080 on white", TestName = "six-digit decimal colour")]
    [TestCase("one of #A6DE00, #20A959, #7848EE, #247EE5", TestName = "hex colours with letters")]
    [TestCase("## 2. Configure the page", TestName = "markdown heading")]
    [TestCase("is the #1 mistake", TestName = "ranking #1")]
    [TestCase("[#Read.ResultEntity.Column#] and #SysSettings.Code#", TestName = "process macro")]
    [TestCase("&#123; is an HTML entity", TestName = "HTML entity")]
    [TestCase("see docs/page.md#12", TestName = "numeric anchor")]
    [TestCase("the tool issues requests in batches", TestName = "issues as a verb")]
    [TestCase("source revision `e53009498` and commit 410b124f7", TestName = "commit hash")]
    public void Scan_ShouldIgnore_ATokenThatIsNotATaskReference(string text)
    {
        TaskReferenceScanner.Scan(text).Select(hit => hit.Text)
            .Should().BeEmpty();
    }

    [Test]
    [Description("A body with several references reports each one with its own line number.")]
    public void Scan_ShouldReportEveryReference_WithItsLine()
    {
        const string text = "first line\nENG-1 and clio#2\nplain\nsee issue\n1249";

        TaskReferenceScanner.Scan(text).Select(hit => $"{hit.Line}: {hit.Text}")
            .Should().Equal("2: ENG-1", "2: clio#2", "4: issue\n1249");
    }

    private static bool IsAllowed(string sourcePath, string text) =>
        AllowedReferences.Any(entry => entry.SourcePath == sourcePath && entry.Text == text);

    private static string[] PublishedPaths(string repositoryRoot)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return ["bundle-source.json", .. manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("sourcePath").GetString()!)];
    }
}

/// <summary>
/// Finds internal task references in a published body. Each pattern was checked against the whole
/// published corpus; the exclusions are the tokens of the same shape that the corpus really contains.
/// </summary>
internal static class TaskReferenceScanner
{
    internal sealed record Hit(int Line, string Text);

    /// <summary>
    /// A Jira key: an upper-case project of 2–10 characters, a hyphen, a number without a leading zero
    /// (example data such as <c>CRM-000042</c> has one). Not glued to a preceding word or hyphen, and not
    /// followed by a GUID's next group, so the groups of <c>E52BD583-7825-E011-…</c> do not match.
    /// </summary>
    private static readonly Regex JiraKey = new(
        @"(?<![\w-])(?<project>[A-Z][A-Z0-9]{1,9})-[1-9][0-9]*(?!\w)(?!-[0-9A-Fa-f]{4}-)",
        RegexOptions.Compiled);

    /// <summary>Standard names with the Jira-key shape. Add a prefix here only for a published standard.</summary>
    private static readonly HashSet<string> StandardNames = new(StringComparer.Ordinal) { "UTF", "SHA", "ISO" };

    /// <summary>An issue or pull request URL on any GitHub host (github.com, creatio.ghe.com).</summary>
    private static readonly Regex IssueUrl = new(
        @"https?://[^\s/()\[\]<>]+/[^\s/()\[\]<>]+/[^\s/()\[\]<>]+/(?:issues|pull)/[0-9]+",
        RegexOptions.Compiled);

    /// <summary>
    /// <c>clio#1574</c>, <c>clio-knowledge#161</c>, <c>owner/repo#12</c>. No dot in a name, so a numeric
    /// anchor on a file (<c>page.md#12</c>) is not read as a repository.
    /// </summary>
    private static readonly Regex RepoQualifiedReference = new(
        @"(?<![\w./-])[A-Za-z][\w-]*(?:/[A-Za-z][\w-]*)?#[1-9][0-9]*(?![\w-])",
        RegexOptions.Compiled);

    /// <summary>
    /// A bare <c>#1301</c>: two to five digits without a leading zero, not glued to a word, an entity
    /// (<c>&amp;#</c>), a path or an anchor. A single digit is a ranking (<c>the #1 mistake</c>); six or more
    /// digits, a leading zero or a hex letter make a colour.
    /// </summary>
    private static readonly Regex BareReference = new(
        @"(?<![\w&#/.-])#[1-9][0-9]{1,4}(?![\w-])",
        RegexOptions.Compiled);

    /// <summary>
    /// A decimal-only colour such as <c>#333</c> has the bare-reference shape; it is told apart by the line
    /// naming a colour property.
    /// </summary>
    private static readonly Regex ColourContext = new(
        @"colou?r|background|border|fill|stroke",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary><c>issue 1249</c>, <c>issue #1416</c>, <c>PR 12</c>, <c>pull request 57</c>.</summary>
    private static readonly Regex KeywordReference = new(
        @"\b(?:issue|PR|pull request)\s+#?[1-9][0-9]*\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static IReadOnlyList<Hit> Scan(string text)
    {
        List<(int Start, int End)> spans = [];

        foreach (Match match in JiraKey.Matches(text))
        {
            if (!StandardNames.Contains(match.Groups["project"].Value))
            {
                spans.Add((match.Index, match.Index + match.Length));
            }
        }

        foreach (Match match in BareReference.Matches(text))
        {
            if (!ColourContext.IsMatch(LineOf(text, match.Index)))
            {
                spans.Add((match.Index, match.Index + match.Length));
            }
        }

        foreach (Regex pattern in new[] { IssueUrl, RepoQualifiedReference, KeywordReference })
        {
            spans.AddRange(pattern.Matches(text).Select(match => (match.Index, match.Index + match.Length)));
        }

        // One reference can match two patterns ("issue #1416" and its "#1416"); report the widest span once.
        return spans
            .Where(span => !spans.Any(other => other != span
                && other.Start <= span.Start && span.End <= other.End
                && other.End - other.Start > span.End - span.Start))
            .Distinct()
            .OrderBy(span => span.Start)
            .Select(span => new Hit(LineNumber(text, span.Start), text[span.Start..span.End]))
            .ToArray();
    }

    private static int LineNumber(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;

    private static string LineOf(string text, int index)
    {
        int start = text.LastIndexOf('\n', Math.Max(index - 1, 0)) + 1;
        int end = text.IndexOf('\n', index);
        return text[start..(end < 0 ? text.Length : end)];
    }
}

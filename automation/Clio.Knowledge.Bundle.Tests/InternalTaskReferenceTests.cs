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
/// here. Every <c>guidance/**/*.md</c> file is declared there:
/// <see cref="GuidanceInventoryTests.PublishedGuidance_ShouldCorrespondExactlyToTheGuidanceSourceTree"/> fails
/// on an undeclared article (only the unpublished <c>guidance/README.md</c> is outside the manifest).
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
    [TestCase("see https://example.atlassian.net/browse/ENG-1 for details", "ENG-1", TestName = "Jira key in a browse URL")]
    [TestCase("branch feature/ENG-102333-compile-timeout", "ENG-102333", TestName = "Jira key in a branch name")]
    [TestCase("branch feature/ENG-1234-feed-post", "ENG-1234", TestName = "Jira key before a hex-looking word")]
    [TestCase("branch bugfix-ENG-123", "ENG-123", TestName = "Jira key after a hyphen")]
    [TestCase("See https://github.com/Advance-Technologies-Foundation/clio/issues/1364 for evidence.",
        "https://github.com/Advance-Technologies-Foundation/clio/issues/1364", TestName = "GitHub issue URL")]
    [TestCase("[comment](https://github.com/o/r/issues/1619#issuecomment-5726093804)",
        "https://github.com/o/r/issues/1619", TestName = "GitHub issue comment URL")]
    [TestCase("Merged in https://ghe.example.com/org/repo/pull/42/files.",
        "https://ghe.example.com/org/repo/pull/42", TestName = "GHE pull request URL")]
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
    [TestCase("tracked in _ENG-92761_ for now", "ENG-92761", TestName = "Jira key in underscore emphasis")]
    [TestCase("tracked in __ENG-92761__ for now", "ENG-92761", TestName = "Jira key in double underscore emphasis")]
    [TestCase("fixed in ENG\u2011123", "ENG\u2011123", TestName = "Jira key with a non-breaking hyphen")]
    [TestCase("fixed in ENG\u2013123", "ENG\u2013123", TestName = "Jira key with an en dash")]
    [TestCase("see _clio#1574_ and _#1138_", "clio#1574", TestName = "repo#N in underscore emphasis")]
    [TestCase("see _clio#1574_ and _#1138_", "#1138", TestName = "bare #N in underscore emphasis")]
    [TestCase("see github.com/o/r/issues/1364", "github.com/o/r/issues/1364", TestName = "issue URL without a scheme")]
    [TestCase("https://bitbucket.org/ws/repo/pull-requests/12/overview",
        "https://bitbucket.org/ws/repo/pull-requests/12", TestName = "Bitbucket pull request URL")]
    [TestCase("https://bb.example.com/projects/P/repos/r/pull-requests/7",
        "https://bb.example.com/projects/P/repos/r/pull-requests/7", TestName = "Bitbucket Server pull request URL")]
    [TestCase("The #1138 fix prevents filling an empty slot.", "#1138", TestName = "bare #N on a line with a colour word")]
    [TestCase("fixed the colour #1416 regression", "#1416", TestName = "bare #N after the word colour")]
    [TestCase("Autofill: #1416", "#1416", TestName = "bare #N after a word ending in fill")]
    [TestCase("Backfill: #1234", "#1234", TestName = "bare #N after another word ending in fill")]
    [TestCase("discovery in #1575-#1579", "#1575", TestName = "bare #N opening an ASCII-hyphen range")]
    [TestCase("discovery in #1575-#1579", "#1579", TestName = "bare #N closing an ASCII-hyphen range")]
    [TestCase("clio#1575-#1579: discovery", "clio#1575", TestName = "repo#N opening an ASCII-hyphen range")]
    [TestCase("builds with the #1138-fix", "#1138", TestName = "bare #N with a hyphen suffix")]
    [TestCase("merged in PR 12", "PR 12", TestName = "PR N")]
    [TestCase("merged in pr 7", "pr 7", TestName = "lower-case pr N")]
    [TestCase("see https://github.com/o/r/pull/88 for details", "https://github.com/o/r/pull/88",
        TestName = "GitHub pull request URL")]
    [Description("Each reference form an author writes is reported, so the gate cannot pass it silently.")]
    public void Scan_ShouldReport_ATaskReference(string text, string expected)
    {
        TaskReferenceScanner.Scan(text).Select(hit => hit.Text)
            .Should().Contain(expected, because: "this text names an internal task an agent cannot open");
    }

    [TestCase("UTF-8 bytes", TestName = "UTF-8")]
    [TestCase("an independent SHA-256 checksum", TestName = "SHA-256")]
    [TestCase("pass ISO-8601, e.g. 2026-05-01", TestName = "ISO-8601")]
    [TestCase("HTTP-2 server push", TestName = "HTTP-2")]
    [TestCase("meets WCAG-2 contrast", TestName = "WCAG-2")]
    [TestCase("RFC-7231 semantics", TestName = "RFC-7231")]
    [TestCase("patched for CVE-2024-12345", TestName = "CVE id")]
    [TestCase("an ECMA-262 regular expression", TestName = "ECMA-262")]
    [TestCase("\"value\": \"CRM-000042\"", TestName = "example data with a leading zero")]
    [TestCase("E52BD583-7825-E011-8165-00155D043204 is ActivityType Call", TestName = "GUID")]
    [TestCase("the id starts E52BD583-7825-E011-8165…", TestName = "GUID fragment")]
    [TestCase("color: #333;", TestName = "three-digit decimal colour")]
    [TestCase("\"backgroundColor\": \"#999\"", TestName = "quoted decimal colour")]
    [TestCase("border: 1px solid #000000", TestName = "colour with a leading zero")]
    [TestCase("border: 1px solid #333", TestName = "decimal colour in a border shorthand")]
    [TestCase("--text-color: #8080;", TestName = "four-digit decimal colour in a custom property")]
    [TestCase("text #808080 on white", TestName = "six-digit decimal colour")]
    [TestCase("one of #A6DE00, #20A959, #7848EE, #247EE5", TestName = "hex colours with letters")]
    [TestCase("## 2. Configure the page", TestName = "markdown heading")]
    [TestCase("is the #1 mistake", TestName = "ranking #1")]
    [TestCase("[#Read.ResultEntity.Column#] and #SysSettings.Code#", TestName = "process macro")]
    [TestCase("&#123; is an HTML entity", TestName = "HTML entity")]
    [TestCase("see docs/page.md#12", TestName = "numeric anchor")]
    [TestCase("see [step 12](#12-configure-the-page)", TestName = "in-page heading anchor")]
    [TestCase("the tool issues requests in batches", TestName = "issues as a verb")]
    [TestCase("source revision `e53009498` and commit 410b124f7", TestName = "commit hash")]
    [Description("Tokens of the same shape that the corpus really contains are not reported.")]
    public void Scan_ShouldIgnore_ATokenThatIsNotATaskReference(string text)
    {
        TaskReferenceScanner.Scan(text).Select(hit => hit.Text)
            .Should().BeEmpty(because: "a false positive would push authors to the allow-list for ordinary text");
    }

    [TestCase("Background colour: #333; fixed in #1138.", TestName = "colour and bare #N on one line")]
    [TestCase("Background is fixed in #1138.", TestName = "colour word, no colour value")]
    [Description("A colour value on the same line neither hides the reference nor is reported itself.")]
    public void Scan_ShouldReportTheTaskReference_NotTheColour(string text)
    {
        TaskReferenceScanner.Scan(text).Select(hit => hit.Text)
            .Should().Equal(["#1138"], because: "only the task reference is a hit, the colour value is not");
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
/// Boundaries are letters and digits only, so Markdown emphasis (<c>_ENG-1_</c>, <c>__clio#2__</c>) does
/// not hide a reference.
/// </summary>
internal static class TaskReferenceScanner
{
    internal sealed record Hit(int Line, string Text);

    /// <summary>
    /// A Jira key: an upper-case project of 2–10 characters, a hyphen (ASCII or a Unicode hyphen or dash),
    /// a number without a leading zero (example data such as <c>CRM-000042</c> has one). Not glued to a
    /// preceding letter or digit; a hyphen before it is a boundary, so <c>bugfix-ENG-123</c> is reported. A
    /// match inside a GUID (<see cref="GuidShape"/>) is dropped in <see cref="Scan"/>.
    /// </summary>
    private static readonly Regex JiraKey = new(
        @"(?<![\p{L}\p{N}])(?<project>[A-Z][A-Z0-9]{1,9})[\-‐-―][1-9][0-9]*(?![\p{L}\p{N}])",
        RegexOptions.Compiled);

    /// <summary>
    /// A GUID or a GUID fragment of at least three groups (<c>E52BD583-7825-E011-…</c>). Its groups have the
    /// Jira-key shape, so a key that falls inside one is not reported; a key merely followed by a
    /// hex-looking word (<c>ENG-1234-feed-post</c>) still is.
    /// </summary>
    private static readonly Regex GuidShape = new(
        @"(?<![\p{L}\p{N}])[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}(?:-[0-9A-Fa-f]{4}(?:-[0-9A-Fa-f]{12})?)?",
        RegexOptions.Compiled);

    /// <summary>Standard names with the Jira-key shape. Add a prefix here only for a published standard.</summary>
    private static readonly HashSet<string> StandardNames = new(StringComparer.Ordinal)
    {
        "CVE", "ECMA", "HTTP", "ISO", "RFC", "SHA", "UTF", "WCAG"
    };

    /// <summary>
    /// An issue or pull request URL, with or without a scheme: GitHub and GitHub Enterprise
    /// (<c>/issues/N</c>, <c>/pull/N</c>) and Bitbucket (<c>/pull-requests/N</c>).
    /// </summary>
    private static readonly Regex IssueUrl = new(
        @"(?:https?://|(?<![^\s(\[<""'`*_]))(?:[^\s/()\[\]<>""'`]+/){3,7}(?:issues|pull|pull-requests)/[0-9]+",
        RegexOptions.Compiled);

    /// <summary>
    /// <c>clio#1574</c>, <c>clio-knowledge#161</c>, <c>owner/repo#12</c>. No dot in a name, so a numeric
    /// anchor on a file (<c>page.md#12</c>) is not read as a repository.
    /// </summary>
    private static readonly Regex RepoQualifiedReference = new(
        @"(?<![\p{L}\p{N}./-])[A-Za-z][\w-]*(?:/[A-Za-z][\w-]*)?#[1-9][0-9]*(?![\p{L}\p{N}])",
        RegexOptions.Compiled);

    /// <summary>
    /// A bare <c>#1301</c>: two to five digits without a leading zero, not glued to a letter, digit, entity
    /// (<c>&amp;#</c>), path or anchor (<c>page.md#12</c>, <c>](#12-configure)</c>). A hyphen is no boundary,
    /// so <c>#1575-#1579</c> and <c>#1138-fix</c> are reported. A single digit is a ranking (<c>the #1 mistake</c>); six or more
    /// digits, a leading zero or a hex letter make a colour.
    /// </summary>
    private static readonly Regex BareReference = new(
        @"(?<![\p{L}\p{N}&#/.])(?<!\]\()#[1-9][0-9]{1,4}(?![\p{L}\p{N}])",
        RegexOptions.Compiled);

    /// <summary>
    /// What may stand directly before a three- or four-digit decimal colour such as <c>#333</c>, which has
    /// the bare-reference shape: a colour property and its value position (<c>color: </c>,
    /// <c>"backgroundColor": "</c>, <c>border: 1px solid </c>). The bare word colour does not count, so
    /// "the colour #1416 regression" is still reported, and the property name must start a word, so
    /// <c>Autofill: #1416</c> is reported too. Only the text right before the token counts.
    /// </summary>
    private static readonly Regex ColourValuePrefix = new(
        @"(?<![\p{L}\p{N}])(?:(?:colou?r|background|border|outline|fill|stroke|shadow)[\w-]*[""']?\s*[:=]\s*[""']?"
            + @"(?:(?:[0-9.]+(?:px|em|rem|%)?|solid|dashed|dotted|double|inset|none)\s+)*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary><c>issue 1249</c>, <c>issue #1416</c>, <c>PR 12</c>, <c>pull request 57</c>.</summary>
    private static readonly Regex KeywordReference = new(
        @"(?<![\p{L}\p{N}])(?:issue|PR|pull\s+request)\s+#?[1-9][0-9]*(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static IReadOnlyList<Hit> Scan(string text)
    {
        List<(int Start, int End)> spans = [];
        (int Start, int End)[] guids = GuidShape.Matches(text)
            .Select(match => (match.Index, match.Index + match.Length))
            .ToArray();

        foreach (Match match in JiraKey.Matches(text))
        {
            if (!StandardNames.Contains(match.Groups["project"].Value)
                && !guids.Any(guid => guid.Start <= match.Index && match.Index + match.Length <= guid.End))
            {
                spans.Add((match.Index, match.Index + match.Length));
            }
        }

        foreach (Match match in BareReference.Matches(text))
        {
            if (!IsColourValue(text, match))
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

    /// <summary>A three- or four-digit bare match standing in a colour-value position.</summary>
    private static bool IsColourValue(string text, Match match)
    {
        int digits = match.Length - 1;
        if (digits is not (3 or 4))
        {
            return false;
        }
        int lineStart = text.LastIndexOf('\n', Math.Max(match.Index - 1, 0)) + 1;
        return ColourValuePrefix.IsMatch(text[lineStart..match.Index]);
    }

    private static int LineNumber(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;
}

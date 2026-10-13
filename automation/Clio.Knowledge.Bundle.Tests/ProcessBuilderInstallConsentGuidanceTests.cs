using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// ENG-102116: <c>install-process-builder</c> installs CrtProcessBuilder INTO the environment, which builds its
/// configuration and restarts the site for every connected user. <c>core-rules</c> owns the rule that the user
/// approves EVERY such call and that a process tool's refusal (or the hint it carries) is not that approval.
/// </summary>
/// <remarks>
/// Every process article that tells an agent to run the install must defer to that rule where it does so: an
/// agent reads the element article in the middle of a build, and an unconditional "run it" there is the reading
/// that turns a refusal into permission. So EVERY mention in EVERY process article is checked, a new article
/// included, against consent WORDS (not the <c>approv</c> stem, which an "approval block" or
/// <c>ApprovalUserTask</c> also matches). Articles that only report what a refusal names are exempt by name.
/// </remarks>
[TestFixture]
public sealed class ProcessBuilderInstallConsentGuidanceTests
{
    private const string CoreRulesPath = "guidance/mcp/guides/core-rules.md";

    private const string ProcessArticlesFolder = "guidance/mcp/guides/processes";

    /// <summary>
    /// Articles that name install-process-builder only as what a refusal or a version boundary names, without
    /// telling the agent to run it. A new exemption is a decision, made here by name.
    /// </summary>
    private static readonly string[] ReportOnlyArticles = ["access-rights-details.md", "data-elements.md", "version-writes.md"];

    /// <summary>Consent wording, as the articles state it. Deliberately not the bare <c>approv</c> stem.</summary>
    private const string ConsentWords = @"ask the user|user approv|user approves|user to approve|after user approval";

    /// <summary>How far either side of a mention the consent wording may sit.</summary>
    private const int ConsentWindow = 80;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    [Test]
    [Description("core-rules owns the consent rule for install-process-builder: ask before EVERY call, and a refusal or its hint is never the approval.")]
    public void CoreRules_ShouldRequireApprovalBeforeEveryInstallProcessBuilderCall()
    {
        // Arrange
        string text = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), CoreRulesPath));

        // Act
        string rule = Regex.Match(text, @"- install-process-builder and install-dashboards-migrator each install a bundled package INTO the environment.+?themselves\.",
            RegexOptions.None, RegexTimeout).Value;

        // Assert
        rule.Should().NotBeEmpty(
            because: "core-rules is read before every operation, so it is the one place the consent rule cannot be missed");
        rule.Should().Contain("never install on the strength of a refusal or its hint",
            because: "the refusal names the install as the remedy, and an agent on a shared stand read that as permission");
        rule.Should().Contain("Before EVERY call",
            because: "an earlier answer is not standing consent - the same rule compile-creatio follows");
        rule.Should().Contain("clio install-process-builder -e <ENV>",
            because: "a user who postpones needs the command to run it themselves later");
    }

    [Test]
    [Description("Every mention of install-process-builder in every process article, a future one included, sits next to consent wording, unless the article is a named report-only exemption.")]
    public void EveryInstallMentionInAProcessArticle_ShouldSitNextToConsentWording()
    {
        // Arrange
        string folder = Path.Combine(ProcessGuideSet.FindRepositoryRoot(), ProcessArticlesFolder);
        string[] articles = [.. Directory.GetFiles(folder, "*.md")
            .Where(path => !ReportOnlyArticles.Contains(Path.GetFileName(path), StringComparer.Ordinal))];

        // Act
        int mentions = 0;
        List<string> unconditional = [];
        foreach (string path in articles)
        {
            string text = Collapse(File.ReadAllText(path));
            foreach (Match mention in Regex.Matches(text, "install-process-builder", RegexOptions.None, RegexTimeout))
            {
                mentions++;
                int start = Math.Max(0, mention.Index - ConsentWindow);
                int end = Math.Min(text.Length, mention.Index + mention.Length + ConsentWindow);
                if (!Regex.IsMatch(text[start..end], ConsentWords, RegexOptions.IgnoreCase, RegexTimeout))
                {
                    unconditional.Add($"{Path.GetFileName(path)}: ...{text[start..end]}...");
                }
            }
        }

        // Assert
        mentions.Should().BePositive(because: "anti-vacuity: the process articles prescribe the install today");
        unconditional.Should().BeEmpty(
            because: "an element article read mid-build that says only 'run install-process-builder' undoes the "
                + "core-rules consent rule for the agent that reads it, and one approved mention does not cover a "
                + "second bare one; found: " + string.Join(" | ", unconditional));
    }

    [Test]
    [Description("The report-only exemptions still exist and still mention the install: a renamed or emptied exemption is a list entry that no longer says anything.")]
    public void ReportOnlyExemptions_ShouldStillNameTheInstall()
    {
        // Arrange
        string folder = Path.Combine(ProcessGuideSet.FindRepositoryRoot(), ProcessArticlesFolder);

        // Act
        string[] stale = [.. ReportOnlyArticles
            .Where(name => !File.Exists(Path.Combine(folder, name))
                || !File.ReadAllText(Path.Combine(folder, name)).Contains("install-process-builder", StringComparison.Ordinal))];

        // Assert
        stale.Should().BeEmpty(
            because: "an exemption is a decision about one article's wording; when the article no longer names the "
                + "install, drop it from the list. Stale: " + string.Join(", ", stale));
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ", RegexOptions.None, RegexTimeout);
}

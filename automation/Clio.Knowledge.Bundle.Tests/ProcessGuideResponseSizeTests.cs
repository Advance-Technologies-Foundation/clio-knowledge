using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the reason ENG-96212 exists. The process guidance was ONE article of 102 KB, and
/// <c>get-guidance name=process-modeling</c> answered with a single JSON line the calling harness could
/// not accept: it exceeded the response limit, spilled to a file, and that file has ONE line, so line-based
/// paging cannot read it either. The tool takes only <c>name</c> — there is no <c>section</c> and no
/// <c>offset</c> — so every agent fell back to grepping a fragment, and which fragment it got decided the
/// outcome. The rule about the mandatory schema-code prefix was already written, verbatim, in the article
/// that none of ten measured runs could read to the end.
///
/// So "the article is too long" was never the defect; "correct text does not reach the reader" was. That
/// makes the size a CONTRACT, not a style preference, and this is the test that keeps it one. A future edit
/// that grows an article back over the limit reintroduces the original defect in full, and it would
/// otherwise ship green — the content is still correct, still reviewed, and still unreachable.
///
/// The budget is measured, not assumed. Probing the live tool through the same harness (2026-08-31,
/// generation 1.13.25):
///
///   esq-filter-parsing        32,698 chars  returned WHOLE
///   page-schema-handlers      50,351 chars  spilled
///   mobile-page-modification  52,655 chars  spilled
///   process-modeling          71,177 chars  spilled
///
/// The cliff is therefore somewhere in (32,698, 50,351]; the exact token limit is the harness's and is not
/// published. <see cref="MaxResponseCharacters"/> sits BELOW the largest size observed to pass, so every
/// article is held to a size that has actually been round-tripped through the real tool, with room left
/// for the two moving parts this repository does not own — the harness limit and clio's envelope. Two
/// articles outside this suite's scope — page-schema-handlers and mobile-page-modification — are over the
/// cliff today and have the same defect; they are not ENG-96212's to fix.
/// </summary>
[TestFixture]
public sealed class ProcessGuideResponseSizeTests
{
    /// <summary>
    /// The budget, set DELIBERATELY BELOW the largest response observed to survive the round trip
    /// (32,698 characters — see the class summary). That observation is a single data point at a single
    /// moment, and two of the things it depends on are not this repository's to hold still: the harness's
    /// token limit, and the size of the envelope clio wraps the article in. Sitting exactly on a measured
    /// ceiling means the first change to either one turns a passing article into a spilling one with no
    /// warning, so the margin is part of the contract rather than slack in it.
    /// </summary>
    private const int MaxResponseCharacters = 30_000;

    /// <summary>
    /// The response carries the article inside a JSON envelope — feedback policy, name, uri, itemId,
    /// topicId, libraryVersion, digest and the resolved local path. Measured at 1,218 and 1,258 characters
    /// on two articles; 1,400 is the allowance so a longer itemId cannot quietly eat the margin. The
    /// envelope is built by the clio repository, so treat this as an estimate that repository can
    /// invalidate, not as a contract this one enforces — which is the other reason the budget above keeps
    /// room under the measured ceiling.
    /// </summary>
    private const int EnvelopeAllowance = 1_400;

    /// <summary>
    /// Every article in the processes folder. The size limit is a property of the TRANSPORT, so it binds
    /// the two guides the split did not touch exactly as much as the seven it produced.
    /// </summary>
    private static readonly string[] ProcessArticles = ProcessGuideSet.AllPaths;

    [Test]
    [Description("Every process article fits in one get-guidance response, so an agent can read it whole.")]
    public void EveryProcessArticle_ShouldFitInOneGetGuidanceResponse()
    {
        string repositoryRoot = FindRepositoryRoot();

        (string Article, int Size)[] tooLarge = ProcessArticles
            .Select(article => (Article: article, Size: ResponseSize(repositoryRoot, article)))
            .Where(measured => measured.Size > MaxResponseCharacters)
            .ToArray();

        tooLarge.Should().BeEmpty(
            because: "an article over the response limit spills to a single-line file that Read cannot page, so "
                + "agents grep a fragment instead of reading it — which is the ENG-96212 defect verbatim. Split "
                + "the article at a section boundary rather than raising this budget; the budget is the largest "
                + $"response measured to survive the round trip ({MaxResponseCharacters:N0} characters). "
                + $"Over budget: {string.Join(", ", tooLarge.Select(m => $"{m.Article} at {m.Size:N0}"))}");
    }

    [Test]
    [Description("The split guides are declared in bundle-source.json, so get-guidance can actually serve them.")]
    public void EverySplitArticle_ShouldBeADeclaredGuidanceTopic()
    {
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(repositoryRoot, "bundle-source.json")));
        string[] declared = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("sourcePath").GetString()!)
            .ToArray();

        string[] undeclared = ProcessArticles.Where(article => !declared.Contains(article)).ToArray();

        undeclared.Should().BeEmpty(
            because: "an article on disk that no resource points at is unreachable through get-guidance; the "
                + "split only helps if each piece is a topic a reader can ask for by name");
    }

    /// <summary>
    /// Serialises the article the way the MCP server does — <see cref="JsonSerializer"/>'s default encoder
    /// is the same JavaScriptEncoder the server uses, so backticks, angle brackets and every non-ASCII
    /// character expand to their six-character escapes here exactly as they do on the wire. Measuring the
    /// raw file instead would understate a dense article by more than a third.
    /// </summary>
    private static int ResponseSize(string repositoryRoot, string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(
            repositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Serialize(text).Length + EnvelopeAllowance;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

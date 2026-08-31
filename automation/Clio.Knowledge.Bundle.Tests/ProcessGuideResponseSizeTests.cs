using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the reason ENG-96212 exists. The process guidance was ONE article of 102 KB, and
/// <c>get-guidance name=process-modeling</c> answered with a single JSON line the calling harness could
/// not accept: it exceeded the response limit, spilled to a file, and that file has ONE line, so
/// line-based paging cannot read it either. The tool takes only <c>name</c> — there is no
/// <c>section</c> and no <c>offset</c> — so every agent fell back to grepping a fragment, and which
/// fragment it got decided the outcome. The rule about the mandatory schema-code prefix was already
/// written, verbatim, in the article that none of ten measured runs could read to the end.
///
/// So "the article is too long" was never the defect; "correct text does not reach the reader" was. That
/// makes the size a CONTRACT, not a style preference, and this is the test that keeps it one. A future
/// edit that grows an article back over the limit reintroduces the original defect in full, and it would
/// otherwise ship green — the content is still correct, still reviewed, and still unreachable.
///
/// The article set is DERIVED from the manifest rather than listed here: the stated remedy for outgrowing
/// the budget is to split, so new articles are expected, and a hand-maintained list would leave each new
/// one unmeasured while staying green.
/// </summary>
[TestFixture]
public sealed class ProcessGuideResponseSizeTests
{
    /// <summary>
    /// The largest response OBSERVED to return whole rather than spill. Kept as a documented probe
    /// constant, not as the budget — see <see cref="MaxResponseCharacters"/>.
    ///
    /// Measured against the live tool on 2026-08-31, knowledge generation 1.13.25:
    ///
    ///   esq-filter-parsing        32,698 chars  returned WHOLE
    ///   page-schema-handlers      50,351 chars  spilled
    ///   mobile-page-modification  52,655 chars  spilled
    ///   process-modeling          71,177 chars  spilled
    ///
    /// Those are that generation's article sizes, used only to bracket the cliff — none of them describes
    /// the article this split produced. The cliff is somewhere in (32,698, 50,351]; the exact limit is the
    /// harness's, is token-based, and is not published.
    /// </summary>
    private const int LargestObservedPass = 32_698;

    /// <summary>
    /// The budget: 85% of the largest observed pass. The margin is part of the contract rather than slack
    /// in it, for three reasons. The observation is a single data point at a single moment; the real limit
    /// counts TOKENS, and a table- or backtick-dense article tokenizes worse per character than the prose
    /// article the figure was measured on; and two of its inputs — the harness limit and the response
    /// envelope — belong to other repositories, which can move them without anything here going red.
    /// </summary>
    private const int MaxResponseCharacters = (int)(LargestObservedPass * 0.85);

    // Where the set stands against that budget (2026-08-31): process-data-elements is the largest at
    // roughly 27,000 characters, which is about 97% of it. That is deliberate rather than accidental —
    // it is the article to split FIRST when this test goes red, at the seam between the record trigger
    // and Read/Modify data on one side and the shared data-source filter on the other. Raising the budget
    // instead would be trading a measured delivery guarantee for the convenience of not splitting.

    /// <summary>
    /// The get-guidance response wraps the article in a JSON envelope — feedback policy, name, uri,
    /// itemId, topicId, libraryVersion, digest and the resolved local path. Measured at 1,218 and 1,258
    /// characters on two articles (2026-08-31); 1,400 is the allowance.
    ///
    /// PROVENANCE: this envelope is built by the **clio** repository, not this one. If its shape or its
    /// feedback-policy text grows, every real response grows while this constant stays put. Re-measure by
    /// calling get-guidance on a known article and subtracting the JSON-escaped length of its body.
    /// </summary>
    private const int EnvelopeAllowance = 1_400;

    // No exception list. Every article in scope is inside the budget, so an empty allow-list would be
    // machinery for a case that does not exist — and an empty one is the easiest place to quietly add a
    // first entry. The two known over-budget articles, page-schema-handlers and mobile-page-modification,
    // are not in the processes folder: they are outside this fixture's scope rather than excused by it,
    // carry the same defect, and are tracked as a follow-up on PR #110. Generalising this fixture to all
    // guidance is the right end state, and that is when an explicit, ticketed exception list earns its
    // place.

    [Test]
    [Description("Every process article fits in one get-guidance response, so an agent can read it whole.")]
    public void EveryProcessArticle_ShouldFitInOneGetGuidanceResponse()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] declared = ProcessGuideSet.Declared(repositoryRoot);

        declared.Should().HaveCountGreaterThanOrEqualTo(ProcessGuideSet.SplitItemIds.Length,
            because: "the set is derived from the manifest, and a derivation that selected nothing would "
                + "report every article as within budget while measuring none of them");

        (string ItemId, int Size)[] measured = declared
            .Select(article => (article.ItemId, Size: ResponseSize(repositoryRoot, article.SourcePath)))
            .OrderByDescending(article => article.Size)
            .ToArray();

        // Headroom is reported on a GREEN run, not only when it is gone. Without this the slide from 87%
        // to 97% to red is invisible, and the first person to see the number is whoever hits the failure —
        // at the moment when raising the budget looks cheapest.
        foreach ((string itemId, int size) in measured)
        {
            TestContext.WriteLine(
                $"{itemId,-32} {size,7:N0}  {(double)size / MaxResponseCharacters,6:P1} of budget"
                + (size > MaxResponseCharacters * 0.9 ? "   <-- approaching the limit; split it" : string.Empty));
        }

        (string ItemId, int Size)[] tooLarge = measured
            .Where(article => article.Size > MaxResponseCharacters)
            .ToArray();

        tooLarge.Should().BeEmpty(
            because: "an article over the response limit spills to a single-line file that Read cannot page, so "
                + "agents grep a fragment instead of reading it — which is the ENG-96212 defect verbatim. Split "
                + "the article at a section boundary rather than raising this budget, which is 85% of the "
                + $"largest response measured to survive the round trip ({LargestObservedPass:N0} characters). "
                + $"Over budget: {string.Join(", ", tooLarge.Select(m => $"{m.ItemId} at {m.Size:N0}"))}");
    }

    [Test]
    [Description("Every process article on disk is declared in the manifest, so none escapes the size contract.")]
    public void EveryProcessArticleOnDisk_ShouldBeDeclaredInTheManifest()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string folder = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "processes");

        string[] onDisk = Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
        string[] declared = ProcessGuideSet.Declared(repositoryRoot)
            .Select(article => article.SourcePath)
            .ToArray();

        onDisk.Should().NotBeEmpty(because: "a scan that found no files would prove nothing");
        onDisk.Except(declared).Should().BeEmpty(
            because: "the size contract is derived from the manifest, so an article on disk that no resource "
                + "declares is both unreachable through get-guidance and unmeasured by the test above — it "
                + "would carry the ENG-96212 defect with nothing watching");
    }

    /// <summary>
    /// Serialises the article the way the MCP server does — <see cref="JsonSerializer"/>'s default encoder
    /// is the same JavaScriptEncoder the server uses, so backticks, angle brackets and every non-ASCII
    /// character expand to their six-character escapes here exactly as they do on the wire. Measuring the
    /// raw file instead would understate a dense article by more than a third.
    /// </summary>
    private static int ResponseSize(string repositoryRoot, string sourcePath) =>
        JsonSerializer.Serialize(ProcessGuideSet.Read(repositoryRoot, sourcePath)).Length + EnvelopeAllowance;
}

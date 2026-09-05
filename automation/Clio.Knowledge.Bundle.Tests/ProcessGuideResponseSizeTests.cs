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
    /// The article that observation was taken on, and its size AT that observation. It lives in this
    /// repository, outside the folder this fixture measures, and it has already drifted: it is 32,996
    /// characters today, 298 past the figure the budget is derived from. So the anchor is recorded here
    /// and checked, because "the largest response observed to return whole" stops meaning anything once
    /// the article it was measured on is a different article.
    ///
    /// This does NOT hold the article at a size. It fails when the drift grows large enough that the
    /// observation no longer describes anything real, and the fix then is to re-probe and re-date
    /// <see cref="LargestObservedPass"/> — not to widen this tolerance.
    /// </summary>
    private const string ProbeItemId = "esq-filter-parsing";

    private const int ProbeDriftTolerance = 2_000;

    [Test]
    [Description("The article the response budget was measured on has not drifted far enough to make the measurement meaningless.")]
    public void TheArticleTheBudgetWasMeasuredOn_ShouldStillResembleThatMeasurement()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string? sourcePath = manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("itemId").GetString() == ProbeItemId)
            .Select(resource => resource.GetProperty("sourcePath").GetString())
            .FirstOrDefault();

        sourcePath.Should().NotBeNull(
            because: $"the budget is derived from one observation taken on '{ProbeItemId}'; if the "
                + "manifest no longer declares that article, the observation describes nothing and "
                + "LargestObservedPass has to be re-probed on an article that exists");

        int size = ResponseSize(repositoryRoot, sourcePath!);
        TestContext.WriteLine(
            $"budget probe {ProbeItemId,-24} {size,7:N0}  observed at {LargestObservedPass,7:N0}"
            + $"  drift {size - LargestObservedPass,+7:N0}");

        Math.Abs(size - LargestObservedPass).Should().BeLessThanOrEqualTo(ProbeDriftTolerance,
            because: $"every number in this fixture is {LargestObservedPass:N0} multiplied twice — the "
                + "budget by 0.85, the headroom gate by 0.9 again — and that figure is one observation of "
                + $"this one article on 2026-08-31. The article has since been edited freely, so once it "
                + "is far from the size that was probed, nobody can say what was measured. Re-probe "
                + "get-guidance against the published library and re-date LargestObservedPass; widening "
                + "this tolerance instead keeps the number and discards its meaning");
    }

    /// <summary>
    /// The budget: 85% of the largest observed pass. The margin is part of the contract rather than slack
    /// in it, for three reasons. The observation is a single data point at a single moment; the real limit
    /// counts TOKENS, and a table- or backtick-dense article tokenizes worse per character than the prose
    /// article the figure was measured on; and two of its inputs — the harness limit and the response
    /// envelope — belong to other repositories, which can move them without anything here going red.
    /// </summary>
    private const int MaxResponseCharacters = (int)(LargestObservedPass * 0.85);

    /// <summary>
    /// The share of the budget an article may hold and still be considered to have room to work in.
    ///
    /// ENG-96536: for most of this fixture's life 90% only PRINTED a warning, and three articles walked
    /// from 87% to 99.4% underneath it — 174 characters, less than one sentence, on the largest. At that
    /// point splitting is forced on whoever happens to arrive next, and the cheapest-looking way out of
    /// their red build is to raise the budget, which trades a measured delivery guarantee for the
    /// convenience of not splitting. So the warning is a gate now, and
    /// <see cref="EveryProcessArticle_ShouldKeepHeadroomForTheNextEdit"/> is where it fires.
    ///
    /// It needs no diff to be attributable: an article's size changes only in a change that TOUCHES it,
    /// so the run that goes red is the run that grew it, and it goes red with roughly 2,700 characters
    /// still in hand — which is the one thing the 100% gate cannot give the person who has to split.
    /// </summary>
    private const double HeadroomThreshold = 0.9;

    /// <summary>
    /// Where the reported line starts saying "watch this", BELOW the gate. It has to be a separate number:
    /// printed at <see cref="HeadroomThreshold"/> the marker can only appear on a run that is already red,
    /// which is the "first person to see the number is whoever hits the failure" outcome the report exists
    /// to prevent — the same defect as the old print, one tier up.
    /// </summary>
    private const double ReportThreshold = 0.8;

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

    // Scope: the processes folder. No exception list, because every article in scope is inside the
    // budget — an empty allow-list would be machinery for a case that does not exist, and an empty one is
    // the easiest place to quietly add a first entry.
    //
    // Articles elsewhere in guidance/ are not measured here. Several are over this budget, and two were
    // observed to spill outright when probed on 2026-08-31: page-schema-handlers at 50,351 response
    // characters and mobile-page-modification at 52,655. That is recorded as measurement, not as a
    // commitment — nothing here owns their remediation.

    [Test]
    [Description("Every process article fits in one get-guidance response, so an agent can read it whole.")]
    public void EveryProcessArticle_ShouldFitInOneGetGuidanceResponse()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] declared = ProcessGuideSet.Declared(repositoryRoot);

        // A superset check, not a count. A count floor accepted a derivation that
        // had lost several articles — 11 named, 15 declared, so four could leave the measured set and
        // still clear the floor. Naming them means the article that left is the one the failure reports.
        declared.Select(article => article.ItemId)
            .Should().Contain(ProcessGuideSet.GoLiveItemIds(repositoryRoot),
            because: "the set is derived from the manifest, so an article that stops matching the "
                + "derivation is one this contract silently stops measuring — and the go-live articles are "
                + "the ones whose delivery was decided, so they are the floor");

        (string ItemId, int Size)[] measured = declared
            .Select(article => (article.ItemId, Size: ResponseSize(repositoryRoot, article.SourcePath)))
            .OrderByDescending(article => article.Size)
            .ToArray();

        // Headroom is reported on a GREEN run, not only when it is gone. Without this the slide from 87%
        // to 97% to red is invisible, and the first person to see the number is whoever hits the failure —
        // at the moment when raising the budget looks cheapest. Two tiers, because one is not a warning:
        // ReportThreshold is a heads-up on a green run, HeadroomThreshold is where the run stops being one.
        foreach ((string itemId, int size) in measured)
        {
            TestContext.WriteLine(
                $"{itemId,-32} {size,7:N0}  {(double)size / MaxResponseCharacters,6:P1} of budget"
                + (size > MaxResponseCharacters * HeadroomThreshold
                    ? "   <-- over the headroom gate; split it"
                    : size > MaxResponseCharacters * ReportThreshold
                        ? "   <-- approaching the headroom gate; plan the seam"
                        : string.Empty));
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
    [Description("Every process article still has room for the next edit, so the split lands on the change "
        + "that grew the article rather than on whoever arrives once there is no room left.")]
    public void EveryProcessArticle_ShouldKeepHeadroomForTheNextEdit()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] declared = ProcessGuideSet.Declared(repositoryRoot);

        declared.Select(article => article.ItemId)
            .Should().Contain(ProcessGuideSet.GoLiveItemIds(repositoryRoot),
            because: "the set is derived from the manifest, so an article that stops matching the "
                + "derivation is one this contract silently stops measuring — see the same floor on the "
                + "delivery gate above");

        int threshold = (int)(MaxResponseCharacters * HeadroomThreshold);
        (string ItemId, int Size)[] crowded = declared
            .Select(article => (article.ItemId, Size: ResponseSize(repositoryRoot, article.SourcePath)))
            .Where(article => article.Size > threshold)
            .OrderByDescending(article => article.Size)
            .ToArray();

        // Deliberately a SECOND test rather than a lower budget on the first. The two say different
        // things and a reader has to be able to tell which one failed: over 100% is a DELIVERY failure —
        // correct text does not reach the reader at all — while over 90% is housekeeping, and the article
        // still answers correctly the whole time. Collapsing them into one number would report the
        // delivery failure and the housekeeping failure in the same words.
        //
        // Known tightest, measured at e8e3790: process-formulas at 88.1% of budget, so the next
        // substantial edit to it has to split it first. That is the gate working, not a defect in it —
        // ENG-96536 left process-formulas alone deliberately (its boundary with
        // process-branch-conditions was drawn by ENG-95891 and is pinned by ProcessFormulaGuidanceTests),
        // and someone splitting it at 88% has room to do it properly.
        crowded.Should().BeEmpty(
            because: "an article this close to the response limit forces the split onto whoever edits it "
                + "next, with nothing left to work with — which is how three articles reached 95-99% of "
                + "budget while this threshold only printed a warning nobody reads. Split at a section "
                + $"boundary now, while there is room. Over {threshold:N0} characters "
                + $"({HeadroomThreshold:P0} of the {MaxResponseCharacters:N0} budget): "
                + string.Join(", ", crowded.Select(a => $"{a.ItemId} at {a.Size:N0}")));
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

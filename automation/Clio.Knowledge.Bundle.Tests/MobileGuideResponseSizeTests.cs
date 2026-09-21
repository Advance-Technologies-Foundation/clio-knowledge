using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Measures the mobile conversion articles the way <see cref="ProcessGuideResponseSizeTests"/> measures the
/// process set, because nothing did — and one of them is marked MANDATORY by a tool description.
/// </summary>
/// <remarks>
/// <para>
/// <c>ProcessGuideResponseSizeTests</c> scopes itself to the processes folder plus <c>routing</c>, and says
/// so: "Articles elsewhere in guidance/ are not measured here. Several are over this budget." That was
/// fine while nothing outside depended on the number. It stopped being fine when
/// <c>web-to-mobile-conversion</c> became the article <c>get-mobile-page-conversion-guide</c> names as
/// mandatory reading: a spilled article writes to a single-line file that line-based paging cannot read,
/// so its rules reach nobody — which is the exact argument this branch used to split the reason codes out.
/// </para>
/// <para>
/// <b>This fixture does NOT assert the articles are safe, and must not be read as saying so.</b> The
/// conversion article is roughly twice <c>MaxResponseCharacters</c> and above both sizes this repository
/// has recorded as spilling (<c>page-schema-handlers</c> 50,351 and <c>mobile-page-modification</c>
/// 52,655). Whether it actually spills is unknown: the limit is the harness's, token-based, and
/// unpublished. What this does is make the number VISIBLE on every run and stop it growing unnoticed,
/// which is the state it was in — a real risk that nothing measured.
/// </para>
/// <para>
/// The ceilings below are a RATCHET, not a budget. Lower them when an article shrinks; do not raise one to
/// make a commit pass. The real remedies are a further split at a real seam, a trim, or re-probing the
/// cliff — all of them decisions about the article rather than about this file.
/// </para>
/// </remarks>
[TestFixture]
public sealed class MobileGuideResponseSizeTests
{
    /// <summary>
    /// The EXACT size of each article at the head of the ENG-95827 branch. Deliberately exact rather than
    /// rounded up: the defect this guards is that the conversion article grew ~14.5 KB unnoticed, and a
    /// ceiling with headroom would let the next few KB through the same way. An edit that adds a line must
    /// therefore either shrink something else or move the number knowingly.
    /// <para>
    /// MOVED ONCE, deliberately: 61,244 -> 61,689. The article claimed the grid row was prebuilt for an
    /// INSERT only and was the caller's job whenever the mobile template already provided List/ListItem.
    /// clio now builds it on both paths, so the claim had become false in the direction that fails
    /// silently — a converted section list shipped with no title and no body, and nothing reported it.
    /// The correction cost ~800 chars; roughly half was paid back by deleting the duplicate statements of
    /// the same rule that the edit itself had created (it was stated in three places). The rest is this
    /// move. Recorded here rather than absorbed, because a ratchet whose moves are not written down is
    /// just a number that keeps going up. Lowered again to 61,636 when the list-row rule was restated
    /// type-agnostically (one source element, several operations) and the per-type prose it replaced
    /// came out, then to 61,607 when the sub-element clause was restated as DERIVED from the rule's one
    /// template rather than declared beside it; a ratchet goes DOWN as soon as the article does.
    /// </para>
    /// <para>
    /// MOVED AGAIN, deliberately (ENG-96589): 61,607 -> 62,764 and 17,776 -> 18,912. The converter now
    /// PRUNES properties the target mobile component does not declare, which adds one reason code and two
    /// response fields. None can go unwritten: an undecoded code reaches the caller through this article's
    /// own fallback as unexplained loss, and a response field the conversion article does not list is one
    /// the agent never reads — which is exactly what happened to `propertyPruneApplied` in the first draft
    /// of this change, leaving the article teaching the very ambiguity that field was added to resolve.
    /// Every entry was cut to roughly half its first draft before this move, so what is left is the floor,
    /// not the draft.
    /// </para>
    /// <para>
    /// LOWERED, 62,764 -> 62,636. The `mobileRuntimeVersion` field was removed from the conversion response
    /// before release: it is provenance the producer has not published since 2026-09-17, so it was null in
    /// every real conversion, and the two lines this article spent warning readers not to read anything into
    /// its absence were the whole cost of carrying it. `propertyPruneApplied` already answers the only
    /// question a caller asks, and `resolvedFrom` already reports which catalog was served. A ratchet goes
    /// DOWN when a field stops shipping, not just when prose is tightened. Lowered again to 62,632 when the
    /// gate learned to refuse an explicit version=latest: the article had to name that third refusal case,
    /// and the clause was paid for by tightening the two entries around it rather than by moving the number.
    /// </para>
    /// <para>
    /// STANDING PROBLEM, not caused by any move above: freedom-page-web-to-mobile-conversion is 62,632 against
    /// a SmallestObservedSpill of 50,351. It has been past that threshold for every move recorded above,
    /// which means the article this repository marks MANDATORY is in the band where the response has been
    /// observed to spill. Trimming at the margin no longer changes that; it needs a real split, and each
    /// further move should be read as evidence for one rather than as headroom.
    /// </para>
    /// </summary>
    private static readonly (string ItemId, int Ceiling)[] MeasuredArticles =
    [
        ("freedom-page-web-to-mobile-conversion", 62_632),
        ("freedom-page-mobile-reason-codes", 18_912)
    ];

    /// <summary>
    /// The largest size this repository has ever RECORDED returning whole, and the two it has recorded
    /// spilling. Copied from <see cref="ProcessGuideResponseSizeTests"/> rather than referenced, because
    /// they are that fixture's private probe constants; they are quoted here only to report where an
    /// article sits relative to observed evidence.
    /// </summary>
    private const int LargestObservedPass = 32_698;
    private const int SmallestObservedSpill = 50_351;

    /// <summary>Matches ProcessGuideResponseSizeTests: the wrapper the article is returned inside.</summary>
    private const int EnvelopeAllowance = 1_400;

    [Test]
    [Description("Every measured mobile article is reported with its size and where it sits against the sizes this repository has observed passing and spilling, and none has grown past the ratchet recorded for it. The point is visibility: this number was previously measured by nothing, on the article a tool marks MANDATORY.")]
    public void MobileArticles_ShouldNotGrowPastTheirRecordedSize()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        List<string> grown = [];

        foreach ((string itemId, int ceiling) in MeasuredArticles)
        {
            string? sourcePath = ManifestSourcePath(repositoryRoot, itemId);
            sourcePath.Should().NotBeNull(
                because: $"'{itemId}' must be declared in the manifest for its size to mean anything");

            int size = ResponseSize(repositoryRoot, sourcePath!);
            string verdict = size >= SmallestObservedSpill
                ? "   <-- ABOVE the smallest size observed to SPILL"
                : size > LargestObservedPass
                    ? "   <-- above the largest size observed to pass"
                    : string.Empty;
            TestContext.WriteLine($"{itemId,-40} {size,7:N0}  ceiling {ceiling,7:N0}{verdict}");

            if (size > ceiling)
            {
                grown.Add($"{itemId} is {size:N0}, past its recorded {ceiling:N0}");
            }
        }

        grown.Should().BeEmpty(
            because: "these articles are already at or past the sizes this repository has recorded "
                + "SPILLING, and a spilled article writes to a single-line file that line-based paging "
                + "cannot read — so its rules reach nobody. The ratchet exists so that state cannot get "
                + "quietly worse; raising a ceiling to make a commit pass is the one response it forbids. "
                + "Split at a real seam or trim instead. " + string.Join("; ", grown));
    }

    private static string? ManifestSourcePath(string repositoryRoot, string itemId)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("itemId").GetString() == itemId)
            .Select(resource => resource.GetProperty("sourcePath").GetString())
            .FirstOrDefault();
    }

    private static int ResponseSize(string repositoryRoot, string sourcePath) =>
        JsonSerializer.Serialize(ProcessGuideSet.Read(repositoryRoot, sourcePath)).Length + EnvelopeAllowance;
}

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
    /// </summary>
    private static readonly (string ItemId, int Ceiling)[] MeasuredArticles =
    [
        ("freedom-page-web-to-mobile-conversion", 61_244),
        ("freedom-page-mobile-reason-codes", 17_776)
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

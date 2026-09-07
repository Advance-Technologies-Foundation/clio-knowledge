using System.Text.Json;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// The set of process guidance articles, DERIVED from <c>bundle-source.json</c> rather than listed by
/// hand.
///
/// Three fixtures assert over this set — the response-size contract, the naming-rule scans, and the
/// feature-gate decision. A hand-maintained list is the wrong shape for all three: the stated remedy for
/// an article outgrowing the size budget is to split it, so new articles under this folder are expected,
/// and one added to a literal array in one fixture but not the others ships green while going unmeasured
/// by the rest. Deriving from the manifest also makes the right thing the published thing — the manifest
/// is the agent-facing contract, so an article declared there is one <c>get-guidance</c> can serve, and a
/// file on disk that nobody declared is not in scope at all.
/// </summary>
internal static class ProcessGuideSet
{
    private const string ProcessFolder = "guidance/mcp/guides/processes/";

    /// <summary>
    /// The line every member of the process guide set opens with. It is how an article declares that
    /// <c>process-modeling</c> is its entry point, which is what obliges the entry to index it — and what
    /// makes it mandatory reading reached through that entry.
    /// </summary>
    internal const string SetBanner = "Part of the process guide set.";

    /// <summary>The entry article. Stable: it keeps the original itemId, uri and legacyUris.</summary>
    internal const string EntryItemId = "process-modeling";

    /// <summary>
    /// The ids the go-live decision covers, written out. This is the EXTERNAL anchor for
    /// <see cref="GoLiveItemIds"/>, which is derived and therefore cannot police itself: an assertion
    /// that the derived set contains the derived set holds by construction, and every consumer of it
    /// was written that way. Measured against that: moving an article out of the processes folder — the
    /// file and its `sourcePath` together — dropped it from the size contract, the citation scan, the
    /// marker scan and the routing/index requirement with the whole suite green; and adding
    /// `requiredFeatures` to an article while deleting its banner in the same commit did the same to the
    /// re-gating gate. Both were found in review, by mutation, not by this list.
    ///
    /// So this list is only ever asserted to be a SUBSET of something derived, never equal to it -- and
    /// the something differs by fixture, deliberately: the size gates assert it against
    /// <see cref="Declared"/>, so an article that leaves the folder or the manifest is reported there,
    /// while the re-gating gate asserts it against the banner-carrying resources, so an article that
    /// leaves the derivation is reported there. A split adds its pieces to both without an edit here,
    /// which is the property the derivation exists for;
    /// what this adds is that nothing already decided can leave quietly. When a split lands, add its
    /// pieces here too — until then they are in scope but not anchored, and that gap is the honest
    /// remainder rather than a claim.
    /// </summary>
    internal static readonly string[] GoLiveFloor =
    [
        "process-modeling",
        "process-element-catalog",
        "process-naming",
        "process-data-elements",
        "process-data-source-filters",
        "process-parameters",
        "process-formulas",
        "process-branch-conditions",
        "process-perform-task",
        "process-task-performer",
        "process-task-category",
        "process-send-email",
        "process-activity-connections",
        "process-preconfigured-page"
    ];

    /// <summary>
    /// The articles the ENG-96132 go-live decision covers: the entry, plus every declared process article
    /// that carries <see cref="SetBanner"/>. DERIVED, because the hand-written list this replaced covered
    /// 11 of the 13 and the two it missed were re-gatable with the whole suite green — <c>process-formulas</c>
    /// and <c>process-branch-conditions</c>, both banner-carrying, both indexed by the entry, both named by
    /// routing as mandatory reading, and one of them itself the product of a split.
    ///
    /// The criterion the hand list stated was "extracted from a listed article", which is a fact about
    /// history that nothing in the tree records. The banner is a fact about the tree, it means the same
    /// thing — this article is reached through the entry, so gating it hides guidance the entry still
    /// points at — and it maintains itself. A future process guide that legitimately documents a
    /// restricted capability simply does not carry the banner, and is not in the set.
    /// </summary>
    internal static string[] GoLiveItemIds(string repositoryRoot) =>
        [EntryItemId, .. Declared(repositoryRoot)
            .Where(article => article.ItemId != EntryItemId)
            .Where(article => Read(repositoryRoot, article.SourcePath)
                .Contains(SetBanner, StringComparison.Ordinal))
            .Select(article => article.ItemId)];

    internal sealed record Article(string ItemId, string SourcePath);

    /// <summary>
    /// Every guidance resource the manifest declares under the processes folder, in manifest order.
    /// </summary>
    internal static Article[] Declared(string repositoryRoot)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return [.. manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "guidance")
            .Where(resource => resource.GetProperty("sourcePath").GetString()!
                .StartsWith(ProcessFolder, StringComparison.Ordinal))
            .Select(resource => new Article(
                resource.GetProperty("itemId").GetString()!,
                resource.GetProperty("sourcePath").GetString()!))];
    }

    /// <summary>Every itemId the manifest declares, for resolving a pointer to a servable topic.</summary>
    internal static HashSet<string> DeclaredItemIds(string repositoryRoot)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("itemId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    internal static string Read(string repositoryRoot, string sourcePath) =>
        File.ReadAllText(Path.Combine(repositoryRoot, sourcePath.Replace('/', Path.DirectorySeparatorChar)));

    internal static string FindRepositoryRoot()
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

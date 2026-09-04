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

    /// <summary>Those articles' source paths, derived, in <see cref="GoLiveItemIds"/> order.</summary>
    internal static string[] SplitPaths(string repositoryRoot)
    {
        Article[] declared = Declared(repositoryRoot);
        return [.. GoLiveItemIds(repositoryRoot).Select(itemId =>
            declared.SingleOrDefault(article => article.ItemId == itemId)?.SourcePath
                ?? throw new InvalidOperationException(
                    $"bundle-source.json declares no guidance resource '{itemId}'; every article the "
                    + "go-live decision covers must stay a declared get-guidance topic."))];
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

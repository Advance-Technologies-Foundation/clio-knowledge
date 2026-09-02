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
    /// Articles ENG-96212 split <c>process-modeling</c> into, entry article first. Named explicitly
    /// because the go-live decision (ENG-96132) and the naming-rule scans are about THESE seven, not
    /// about whatever else the folder later holds — see <see cref="Declared"/> for the derived set that
    /// the size contract uses.
    /// </summary>
    internal static readonly string[] SplitItemIds =
    [
        "process-modeling",
        "process-naming",
        "process-data-elements",
        "process-parameters",
        "process-perform-task",
        "process-send-email",
        "process-activity-connections",
        "process-preconfigured-page"
    ];

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

    /// <summary>The seven split articles' source paths, derived, in <see cref="SplitItemIds"/> order.</summary>
    internal static string[] SplitPaths(string repositoryRoot)
    {
        Article[] declared = Declared(repositoryRoot);
        return [.. SplitItemIds.Select(itemId =>
            declared.SingleOrDefault(article => article.ItemId == itemId)?.SourcePath
                ?? throw new InvalidOperationException(
                    $"bundle-source.json declares no guidance resource '{itemId}'; the ENG-96212 split "
                    + "articles must each stay a declared get-guidance topic."))];
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

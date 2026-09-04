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
    /// The articles the ENG-96132 go-live decision covers, entry article first: the seven ENG-96212 split
    /// <c>process-modeling</c> into, plus <c>process-element-catalog</c>, which ENG-96536 extracted from the
    /// entry article's OWN body. That eighth is on the list for the same reason the other seven are — the
    /// text in it shipped un-gated as part of process-modeling, so gating it would hide guidance the GA
    /// business-process tools name as mandatory reading. Named explicitly rather than derived, because it
    /// records a DECISION about specific articles: a future process guide that legitimately documents a
    /// restricted capability must be able to carry `requiredFeatures` without turning this red. See
    /// <see cref="Declared"/> for the derived set the size contract uses.
    /// </summary>
    internal static readonly string[] SplitItemIds =
    [
        "process-modeling",
        "process-element-catalog",
        "process-naming",
        "process-data-elements",
        "process-parameters",
        "process-perform-task",
        "process-send-email",
        "process-activity-connections"
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

    /// <summary>Those articles' source paths, derived, in <see cref="SplitItemIds"/> order.</summary>
    internal static string[] SplitPaths(string repositoryRoot)
    {
        Article[] declared = Declared(repositoryRoot);
        return [.. SplitItemIds.Select(itemId =>
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

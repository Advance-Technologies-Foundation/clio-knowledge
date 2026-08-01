using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Byte-parity coverage that stays complete on its own.
/// </summary>
/// <remarks>
/// The per-partition MIG fixtures each hard-code the articles they were written for, and
/// <c>migration/guidance-partitions.json</c> is pinned to the pre-extraction baseline commit, so an
/// article Clio added afterwards can be published here without any fixture noticing. This fixture
/// derives its subject list from <c>bundle-source.json</c> instead: every resource published with
/// role <c>guidance</c> is compared against the current Clio oracle unless
/// <see cref="KnowledgeOracle.MirrorsClio"/> records that this repository has taken the article
/// over. A newly published article is therefore covered the moment it appears in the manifest, with
/// no map to remember to update.
/// </remarks>
[TestFixture]
public sealed class PublishedGuidanceOracleParityTests
{
    [Test]
    [Description("Verifies that every published guidance article mirroring Clio is byte-identical to the current Clio oracle, whatever partition it belongs to.")]
    public void EveryMirroringPublishedArticle_ShouldMatchCurrentOracleByteForByte()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        PublishedArticle[] mirroring = ReadPublishedGuidance(repositoryRoot)
            .Where(article => KnowledgeOracle.MirrorsClio(article.ItemId))
            .ToArray();

        // Act
        string[] missingOracleBytes = mirroring
            .Where(article => !File.Exists(ToFullPath(
                repositoryRoot,
                KnowledgeOracle.CurrentResourcePath(article.ItemId))))
            .Select(article => article.ItemId)
            .ToArray();
        string[] differences = mirroring
            .Where(article => !missingOracleBytes.Contains(article.ItemId, StringComparer.Ordinal))
            .Where(article => !CanonicalBytes(
                    ReferenceLinkMigration.NormalizeToFrozenLinkText(
                        ReadText(repositoryRoot, article.SourcePath)))
                .SequenceEqual(CanonicalBytes(ReadText(
                    repositoryRoot,
                    KnowledgeOracle.CurrentResourcePath(article.ItemId)))))
            .Select(article => article.ItemId)
            .ToArray();

        // Assert
        mirroring.Should().NotBeEmpty(
            because: "a manifest that published no mirroring guidance would make this assertion vacuous");
        missingOracleBytes.Should().BeEmpty(
            because: "an article expected to mirror Clio must have captured Clio bytes to be compared against");
        differences.Should().BeEmpty(
            because: "a published article this repository has not taken ownership of may differ from Clio "
                + "only by links that now target independently published reference articles");
    }

    [Test]
    [Description("Verifies that the ownership sets partition the published guidance inventory exactly, so no article escapes byte-parity coverage unnoticed.")]
    public void OwnershipSets_ShouldPartitionThePublishedGuidanceInventory()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        PublishedArticle[] published = ReadPublishedGuidance(repositoryRoot);
        HashSet<string> publishedIds = published
            .Select(article => article.ItemId)
            .ToHashSet(StringComparer.Ordinal);

        // Act
        string[] mirroringIds = published
            .Where(article => KnowledgeOracle.MirrorsClio(article.ItemId))
            .Select(article => article.ItemId)
            .ToArray();
        string[] excludedIds = published
            .Where(article => !KnowledgeOracle.MirrorsClio(article.ItemId))
            .Select(article => article.ItemId)
            .ToArray();
        string[] staleExclusions = KnowledgeOracle.IndependentlyEditedArticles
            .Concat(KnowledgeOracle.ArticlesRetiredInClio)
            .Where(id => !publishedIds.Contains(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        // Assert
        published.Should().NotBeEmpty(
            because: "the manifest must publish guidance for the ownership sets to describe");
        published.Select(article => article.ItemId).Should().OnlyHaveUniqueItems(
            because: "one stable guidance ID must resolve to exactly one published article");
        mirroringIds.Concat(excludedIds).Should().BeEquivalentTo(publishedIds,
            because: "every published guidance article is either compared against Clio or recorded as owned here");
        mirroringIds.Should().NotIntersectWith(excludedIds,
            because: "an article cannot both mirror Clio and be owned by this repository");
        staleExclusions.Should().BeEmpty(
            because: "an ownership exemption for an article this repository no longer publishes hides the next drift");
    }

    [Test]
    [Description("Verifies that a published article absent from the current Clio oracle is declared retired rather than silently uncovered.")]
    public void ArticlesAbsentFromTheOracle_ShouldBeDeclaredRetiredInClio()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        PublishedArticle[] published = ReadPublishedGuidance(repositoryRoot);
        using JsonDocument provenance = ReadJson(
            repositoryRoot,
            $"{KnowledgeOracle.CurrentDirectory}/provenance.json");
        HashSet<string> oracleIds = provenance.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        // Act
        string[] undeclaredNewArticles = published
            .Where(article => !oracleIds.Contains(article.ItemId))
            .Where(article => !KnowledgeOracle.ArticlesRetiredInClio.Contains(article.ItemId))
            .Select(article => article.ItemId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] retiredArticlesStillServedByClio = KnowledgeOracle.ArticlesRetiredInClio
            .Where(id => oracleIds.Contains(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        // Assert
        oracleIds.Should().NotBeEmpty(
            because: "the current oracle must carry the Clio bytes the parity assertions read");
        undeclaredNewArticles.Should().BeEmpty(
            because: "an article Clio does not serve must be recorded as retired, so the parity assertion "
                + "reports a deliberate exemption instead of a missing oracle file");
        retiredArticlesStillServedByClio.Should().BeEmpty(
            because: "an article Clio serves again must return to byte-parity coverage rather than stay exempt");
    }

    private static PublishedArticle[] ReadPublishedGuidance(string repositoryRoot)
    {
        using JsonDocument source = ReadJson(repositoryRoot, "bundle-source.json");
        return source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => string.Equals(
                resource.GetProperty("role").GetString(),
                "guidance",
                StringComparison.Ordinal))
            .Select(resource => new PublishedArticle(
                resource.GetProperty("itemId").GetString()!,
                resource.GetProperty("sourcePath").GetString()!))
            .ToArray();
    }

    private static byte[] CanonicalBytes(string text) =>
        Encoding.UTF8.GetBytes(BundleBuilder.CanonicalizeText(text.TrimStart('\uFEFF')));

    private static string ReadText(string repositoryRoot, string relativePath) =>
        File.ReadAllText(ToFullPath(repositoryRoot, relativePath), new UTF8Encoding(false, true));

    private static JsonDocument ReadJson(string repositoryRoot, string relativePath) =>
        JsonDocument.Parse(File.ReadAllBytes(ToFullPath(repositoryRoot, relativePath)));

    private static string ToFullPath(string repositoryRoot, string relativePath) =>
        Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

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

    private sealed record PublishedArticle(string ItemId, string SourcePath);
}

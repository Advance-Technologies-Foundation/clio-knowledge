using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the seams ENG-96212 created. Splitting one article into seven turned every
/// "see the section below" into a reference that either names another article or dangles, and a
/// dangling one is invisible at read time: the sentence still reads as a complete instruction, and
/// the reader simply never gets the rule it points at.
///
/// This is a GENERIC scan on purpose. The split originally shipped with two literal string
/// assertions in place of a general one, and a real dangler walked straight through them — the
/// citation <c>the "Modify data\nelement" section</c>, which a line-based check cannot see because
/// the quoted name straddles a line break. The scan below is multiline for exactly that reason: a
/// check whose shape cannot express the defect it is for is not a check.
/// </summary>
[TestFixture]
public sealed class ProcessGuideCrossReferenceTests
{
    /// <summary>A quoted section name followed by a locator word, across line breaks.</summary>
    private static readonly Regex Citation = new(
        "\"([A-Z][^\"]{3,60})\"(\\s+)(section|sections|below|above)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Heading = new(@"^== (.+?) ==", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex ArticleName = new(@"`process-[a-z-]+`", RegexOptions.Compiled);

    [Test]
    [Description("Every quoted section citation resolves in its own article or names the article that owns it.")]
    public void ProcessGuides_ShouldNotCiteASectionTheyDoNotOwn_WithoutNamingItsArticle()
    {
        string repositoryRoot = FindRepositoryRoot();
        List<string> dangling = [];
        int scanned = 0;

        foreach (string relativePath in ProcessGuideSet.SplitPaths)
        {
            string text = ReadArticle(repositoryRoot, relativePath);
            string headings = string.Join(" | ", Heading.Matches(text).Select(m => m.Groups[1].Value));

            foreach (Match citation in Citation.Matches(text))
            {
                scanned++;
                string cited = Collapse(citation.Groups[1].Value);
                if (headings.Contains(cited, StringComparison.OrdinalIgnoreCase))
                {
                    continue;   // the article defines the section it is pointing at
                }

                int from = Math.Max(0, citation.Index - 90);
                int to = Math.Min(text.Length, citation.Index + citation.Length + 90);
                if (ArticleName.IsMatch(text[from..to]))
                {
                    continue;   // an owning article is named right beside the citation
                }

                dangling.Add($"{relativePath}: {Collapse(citation.Value)}");
            }
        }

        // A floor, not a census. The point is to fail if the pattern stops matching ALTOGETHER — a regex
        // edit, or a house style that stops quoting section names — because an empty scan would report
        // "no dangling citations" while reading nothing. Pinning the exact count instead would fail every
        // time a citation is legitimately rewritten to name its article, which is the very fix this test
        // asks for.
        scanned.Should().BeGreaterThanOrEqualTo(3,
            because: "a scan that matches nothing would pass this test while guarding nothing; the articles "
                + "still quote section names by hand, so the pattern has to keep reaching them");
        dangling.Should().BeEmpty(
            because: "after the split a 'see the section below' either resolves inside its own article or has "
                + "to name the article that owns it; a citation that does neither reads as a complete "
                + "instruction while silently withholding the rule. Found: " + string.Join("; ", dangling));
    }

    /// <summary>
    /// Section markers distinctive enough to name one owner, paired with the article that owns them.
    /// The quoted-citation scan above cannot see these: the reference that survived it read "the same
    /// way the R1-R17 header separates the full catalog from the buildable slice" — no quotes, no
    /// locator word, and the header it compares itself to had moved to another article. A reader cannot
    /// check that comparison without being told where to look.
    /// </summary>
    private static readonly (string Marker, string Owner)[] MovedSectionMarkers =
    [
        ("R1-R17", "process-activity-connections"),
        ("R1–R17", "process-activity-connections"),      // en dash, as the articles write it
        ("N1-N10", "process-naming"),
        ("Naming and codes", "process-naming"),
        ("Data source filters", "process-data-elements")
    ];

    [Test]
    [Description("An article naming a section another article owns points at that article.")]
    public void ProcessGuides_ShouldNameTheOwningArticle_WhenReferencingAMovedSection()
    {
        string repositoryRoot = FindRepositoryRoot();
        Dictionary<string, string> ownerByPath = ProcessGuideSet.SplitItemIds
            .Zip(ProcessGuideSet.SplitPaths)
            .ToDictionary(pair => pair.Second, pair => pair.First);

        List<string> unattributed = [];
        foreach (string relativePath in ProcessGuideSet.SplitPaths)
        {
            string text = ReadArticle(repositoryRoot, relativePath);
            foreach ((string marker, string owner) in MovedSectionMarkers)
            {
                if (ownerByPath[relativePath] == owner)
                {
                    continue;   // the owner may name its own section however it likes
                }
                foreach (int index in Occurrences(text, marker))
                {
                    // Wide enough to span the entry article's index, where the article name heads the
                    // entry and the section it owns is named on the continuation line below it.
                    int from = Math.Max(0, index - 220);
                    int to = Math.Min(text.Length, index + marker.Length + 220);
                    if (text[from..to].Contains($"`{owner}`", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    unattributed.Add($"{relativePath}: '{marker}' without naming {owner}");
                }
            }
        }

        unattributed.Should().BeEmpty(
            because: "after the split these sections live in one article each, and a bare mention leaves the "
                + "reader with a claim they cannot check — the reference is not broken enough to look broken, "
                + "which is why it survives review. Found: " + string.Join("; ", unattributed));
    }

    private static IEnumerable<int> Occurrences(string text, string value)
    {
        for (int index = text.IndexOf(value, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            yield return index;
        }
    }

    [Test]
    [Description("Every article the split produced is reachable from the routing map and from the entry article's index.")]
    public void Routing_ShouldNameEverySplitArticle_AndTheEntryArticleShouldIndexThem()
    {
        string repositoryRoot = FindRepositoryRoot();
        string routing = ReadArticle(repositoryRoot, "guidance/mcp/guides/routing.md");
        string entry = ReadArticle(repositoryRoot, ProcessGuideSet.SplitPaths[0]);

        string[] missingFromRouting = ProcessGuideSet.SplitItemIds
            .Where(itemId => !routing.Contains($"name={itemId}", StringComparison.Ordinal))
            .ToArray();
        string[] missingFromIndex = ProcessGuideSet.SplitItemIds
            .Skip(1)    // the entry article does not index itself
            .Where(itemId => !entry.Contains($"`{itemId}`", StringComparison.Ordinal))
            .ToArray();

        missingFromRouting.Should().BeEmpty(
            because: "routing is the only guidance pointer clio's MCP instructions carry, so an article it does "
                + "not name is one an agent reaches only by already knowing it exists");
        missingFromIndex.Should().BeEmpty(
            because: "process-modeling keeps the legacy uri and is where a reader following an old pointer "
                + "lands; if it does not index its siblings, the split turns one reachable article into one "
                + "reachable article and six orphans");
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static string ReadArticle(string repositoryRoot, string relativePath) =>
        File.ReadAllText(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

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

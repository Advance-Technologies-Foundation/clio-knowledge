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
/// Every scan here is driven from the MANIFEST, not from the seven ids the split happened to produce.
/// The size fixture prescribes splitting an article that outgrows its budget, so an eighth article is
/// expected — and one that is size-measured but not routed, not indexed and not citation-scanned is the
/// "one reachable article and six orphans" failure deferred to whoever adds it.
///
/// The scans are also deliberately shaped to see what a naive one cannot. The split shipped with two
/// literal string assertions in place of a general scan, and a real dangler walked through them: the
/// citation <c>the "Modify data\nelement" section</c>, invisible to a line-based check because the quoted
/// name straddles a line break. Headings wrap the same way — <c>activity-connections.md</c> opens its
/// largest section with a <c>== ... ==</c> heading spread over four physical lines — so both patterns
/// cross newlines and both collapse whitespace before comparing.
/// </summary>
[TestFixture]
public sealed class ProcessGuideCrossReferenceTests
{
    /// <summary>A quoted section name followed by a locator word, across line breaks.</summary>
    private static readonly Regex Citation = new(
        "\"([A-Z][^\"]{3,60})\"(\\s+)(section|sections|below|above)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// A <c>== ... ==</c> heading, which may wrap over several physical lines. Singleline is what lets
    /// the dot cross the newline; without it the four-line R1-R17 heading is absent from the index and a
    /// legitimate self-citation of that section reads as dangling.
    /// </summary>
    private static readonly Regex Heading = new(
        @"^== (.+?) ==\s*$",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ArticleName = new(@"`(process-[a-z-]+)`", RegexOptions.Compiled);

    /// <summary>
    /// The line every sub-article of the split opens with. It is how an article declares that
    /// <c>process-modeling</c> is its entry point, which is what makes the entry obliged to index it.
    /// </summary>
    private const string SetBanner = "Part of the process guide set.";

    /// <summary>
    /// The token routing uses to name a guide. The dot is INSIDE the class: the manifest declares dotted
    /// ids (<c>atf.creatio.kafka-reference</c>), and a pattern that stopped at the dot would capture a
    /// truncated name the manifest does not declare — failing with "routing names an undeclared topic"
    /// and pointing the next contributor at a typo that does not exist.
    /// </summary>
    private static readonly Regex RoutingName = new(@"\bname=([a-z0-9][a-z0-9.-]*)", RegexOptions.Compiled);

    /// <summary>
    /// Section markers distinctive enough to name one owner. The quoted-citation scan cannot see these:
    /// the reference that survived it read "the same way the R1-R17 header separates the full catalog from
    /// the buildable slice" — no quotes, no locator word, and the header had moved to another article.
    ///
    /// The destructive-operation rows are the other end of a safety citation: two articles instruct
    /// removals and point at the guardrails in <c>process-modeling</c>, so the marker keeps that pointer
    /// honest, and <see cref="DestructiveRemovalClauses"/> keeps the guardrails themselves alive.
    /// </summary>
    private static readonly (string Marker, string Owner)[] MovedSectionMarkers =
    [
        ("R1-R17", "process-activity-connections"),
        ("R1–R17", "process-activity-connections"),      // en dash, as the articles write it
        ("N1-N10", "process-naming"),
        ("Naming and codes", "process-naming"),
        ("Data source filters", "process-data-elements")
    ];

    // Deliberately NOT here: removeElement / removeParameter. Adding them was the literal reading of
    // "add marker rows for the destructive ops", and it produced false positives against a decision that
    // had already been reasoned through — send-email.md mentions removeParameter while describing
    // CrtProcessDesigner internals, not while instructing anything, and a marker keyed on an API
    // identifier cannot tell an instruction from a description. The destination is guarded instead, by
    // DestructiveRemovalRules_ShouldSurviveInTheArticleThatOwnsThem, which is what the finding was
    // actually about: the citations must not point at rules that have been trimmed away.

    /// <summary>
    /// The preconditions that make a removal safe. They live in the entry article; two sub-articles
    /// instruct removals and cite it. A later trim of that section would leave those articles pointing at
    /// guardrails that no longer exist, with every other test green.
    /// </summary>
    private static readonly string[] DestructiveRemovalClauses =
    [
        "runs NO structural validation",
        "CASCADES",
        "validate-process-graph",
        "confirm destructive removals with the user"
    ];

    [Test]
    [Description("Every quoted section citation resolves in its own article or names the article that owns it.")]
    public void ProcessGuides_ShouldNotCiteASectionTheyDoNotOwn_WithoutNamingItsArticle()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] articles = ProcessGuideSet.Declared(repositoryRoot);
        Dictionary<string, string[]> headingsByItemId = articles.ToDictionary(
            article => article.ItemId,
            article => Headings(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)));

        List<string> dangling = [];
        int scanned = 0;

        foreach (ProcessGuideSet.Article article in articles)
        {
            // Only articles that use the `== ... ==` heading convention can have a citation resolved
            // against their own sections. run-process-button and process-script-task predate it and head
            // their blocks with plain text, so a citation like `see "Behavior flags" above` is perfectly
            // valid there and merely invisible to this scan. Imposing the convention on articles this
            // change does not touch would be scope creep dressed as a guard.
            if (headingsByItemId[article.ItemId].Length == 0)
            {
                continue;
            }

            string text = ProcessGuideSet.Read(repositoryRoot, article.SourcePath);
            foreach (Match citation in Citation.Matches(text))
            {
                scanned++;
                string cited = Collapse(citation.Groups[1].Value);
                if (Owns(headingsByItemId[article.ItemId], cited))
                {
                    continue;   // the article defines the section it is pointing at
                }

                int from = Math.Max(0, citation.Index - 90);
                int to = Math.Min(text.Length, citation.Index + citation.Length + 90);
                // The named article must actually OWN the cited section. Accepting any nearby
                // `process-*` token would clear a citation misrouted to the wrong sibling, which is the
                // failure this test claims to prevent rather than one it may wave through.
                bool named = ArticleName.Matches(text[from..to])
                    .Select(match => match.Groups[1].Value)
                    .Any(name => headingsByItemId.TryGetValue(name, out string[]? headings)
                        && Owns(headings, cited));
                if (named)
                {
                    continue;
                }

                dangling.Add($"{article.ItemId}: {Collapse(citation.Value)}");
            }
        }

        // A floor, not a census: fail if the pattern stops matching ALTOGETHER, because an empty scan
        // would report "no dangling citations" while reading nothing. Pinning the exact count would fail
        // every time a citation is legitimately rewritten to name its article — the fix this test asks for.
        scanned.Should().BeGreaterThanOrEqualTo(3,
            because: "a scan that matches nothing would pass this test while guarding nothing; the articles "
                + "still quote section names by hand, so the pattern has to keep reaching them");
        dangling.Should().BeEmpty(
            because: "after the split a 'see the section below' either resolves inside its own article or has "
                + "to name the article that OWNS it; a citation that does neither, or that names a sibling "
                + "which does not define the section, reads as a complete instruction while silently "
                + "withholding the rule. Found: " + string.Join("; ", dangling));
    }

    [Test]
    [Description("An article naming a section another article owns points at that article.")]
    public void ProcessGuides_ShouldNameTheOwningArticle_WhenReferencingAMovedSection()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        List<string> unattributed = [];

        foreach (ProcessGuideSet.Article article in ProcessGuideSet.Declared(repositoryRoot))
        {
            string text = ProcessGuideSet.Read(repositoryRoot, article.SourcePath);
            foreach ((string marker, string owner) in MovedSectionMarkers)
            {
                if (article.ItemId == owner)
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
                    unattributed.Add($"{article.ItemId}: '{marker}' without naming {owner}");
                }
            }
        }

        unattributed.Should().BeEmpty(
            because: "after the split these sections live in one article each, and a bare mention leaves the "
                + "reader with a claim they cannot check — the reference is not broken enough to look broken, "
                + "which is why it survives review. Found: " + string.Join("; ", unattributed));
    }

    [Test]
    [Description("The destructive-removal preconditions still exist in the article the sub-articles cite for them.")]
    public void DestructiveRemovalRules_ShouldSurviveInTheArticleThatOwnsThem()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string owner = ProcessGuideSet.Read(repositoryRoot, ProcessGuideSet.SplitPaths(repositoryRoot)[0]);

        string[] missing = DestructiveRemovalClauses
            .Where(clause => !owner.Contains(clause, StringComparison.Ordinal))
            .ToArray();

        missing.Should().BeEmpty(
            because: "two articles instruct removals on a live customer process and cite process-modeling for "
                + "the rules that make them safe. Trimming those rules leaves both pointing at guardrails "
                + "that no longer exist, and every other test stays green because the pointer still resolves. "
                + "Missing: " + string.Join(", ", missing));
    }

    [Test]
    [Description("Every declared process article is reachable from the routing map and from the entry article's index.")]
    public void Routing_ShouldNameEveryProcessArticle_AndTheEntryArticleShouldIndexThem()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string routing = ReadRouting(repositoryRoot);
        ProcessGuideSet.Article[] articles = ProcessGuideSet.Declared(repositoryRoot);
        string entryItemId = ProcessGuideSet.SplitItemIds[0];
        string entry = ProcessGuideSet.Read(repositoryRoot,
            articles.Single(article => article.ItemId == entryItemId).SourcePath);

        string[] missingFromRouting = articles
            .Where(article => !routing.Contains($"name={article.ItemId}", StringComparison.Ordinal))
            .Select(article => article.ItemId)
            .ToArray();
        // Which articles the ENTRY must index is decided by the articles themselves: a sub-article
        // carries the set banner naming process-modeling as its entry point, so it has to be indexed
        // there. run-process-button and process-script-task carry no banner — they are process-folder
        // articles with their own routing rows, not sub-articles of the entry — so requiring the index to
        // list them would confuse "lives in the folder" with "is reached through this entry". An eighth
        // SPLIT article gets the banner and is therefore caught.
        string[] missingFromIndex = articles
            .Where(article => article.ItemId != entryItemId)
            .Where(article => ProcessGuideSet.Read(repositoryRoot, article.SourcePath)
                .Contains(SetBanner, StringComparison.Ordinal))
            .Where(article => !entry.Contains($"`{article.ItemId}`", StringComparison.Ordinal))
            .Select(article => article.ItemId)
            .ToArray();

        missingFromIndex.Should().NotBeNull();
        articles.Count(article => ProcessGuideSet.Read(repositoryRoot, article.SourcePath)
                .Contains(SetBanner, StringComparison.Ordinal))
            .Should().BeGreaterThan(1,
                because: "the index requirement is keyed on the set banner, so a banner text change would "
                    + "otherwise silently reduce this to asserting nothing");

        missingFromRouting.Should().BeEmpty(
            because: "routing is the only guidance pointer clio's MCP instructions carry, so an article it does "
                + "not name is one an agent reaches only by already knowing it exists");
        missingFromIndex.Should().BeEmpty(
            because: "process-modeling keeps the legacy uri and is where a reader following an old pointer "
                + "lands; if it does not index its siblings, the split turns one reachable article into one "
                + "reachable article and a set of orphans");
    }

    [Test]
    [Description("Every name= token in the routing map resolves to a topic get-guidance can actually serve.")]
    public void Routing_ShouldOnlyNameDeclaredGuidanceTopics()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        HashSet<string> declared = ProcessGuideSet.DeclaredItemIds(repositoryRoot);

        string[] routed = RoutingName.Matches(ReadRouting(repositoryRoot))
            .Select(match => match.Groups[1].Value.TrimEnd('.', '-'))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        routed.Should().HaveCountGreaterThan(20,
            because: "routing names most of the library; a token scan that found only a handful would be "
                + "matching something other than the routing rows and would prove nothing");
        routed.Where(name => !declared.Contains(name)).Should().BeEmpty(
            because: "routing is the map an agent reads before choosing a guide, so a row naming a topic the "
                + "manifest does not declare sends the reader to a get-guidance call that cannot succeed — "
                + "a misspelling here is indistinguishable from the article not existing");
    }

    private static string[] Headings(string article) =>
        [.. Heading.Matches(article).Select(match => Collapse(match.Groups[1].Value))];

    private static bool Owns(string[] headings, string cited) =>
        headings.Any(heading => heading.Contains(cited, StringComparison.OrdinalIgnoreCase));

    private static string ReadRouting(string repositoryRoot) =>
        ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/routing.md");

    private static IEnumerable<int> Occurrences(string text, string value)
    {
        for (int index = text.IndexOf(value, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            yield return index;
        }
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

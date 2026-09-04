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
        // ENG-96536 moved this section into its own article; the row follows the section, not the file
        // it used to live in. Left on process-data-elements, the owner-skip would have exempted the
        // article that no longer defines it while the one that does went unwatched.
        ("Data source filters", "process-data-source-filters"),
        // ENG-95891 split process-branch-conditions out of process-formulas and added no marker row, so
        // this guard stayed silent about the newest split for the whole of that work. Both phrases were
        // checked against the folder before being added: each occurs in exactly one non-owner article.
        // "parallel split" was the obvious third and is deliberately NOT here - activity-connections
        // carries it inside BPMN rule R12 ("multiple outgoing sequence flows = implicit parallel split"),
        // a different subject, and a marker cannot tell the two apart.
        // Was ("CONDITION on a conditional flow", ...), which matched NOTHING in the folder — it was
        // keyed on the manifest description's wording rather than on anything an article says, so the
        // row read as a guard while guarding nothing.
        //
        // Its replacement had to survive a second question: not "does it match" but "can it FAIL". The
        // folder writes this phrase three ways — the owner heads its section BRANCH PRECEDENCE IS FLOW
        // ORDER, the entry article's index writes "branch precedence", process-formulas writes "branch
        // PRECEDENCE" — and a case-sensitive row on the index spelling matched only the index, whose
        // bullet label sits ~60 characters above it by construction, so the window could never be
        // missing the owner. It matched, and it could not fail. Occurrences is OrdinalIgnoreCase now, so
        // this one row reaches all three; the process-formulas mention, genuinely unattributed at ~230
        // lines from that article's own pointer, was what it caught first.
        ("branch precedence", "process-branch-conditions"),
        ("the last conditional flow", "process-branch-conditions"),
        // ENG-96536 moved "What you can build today" and the element catalog out of the entry article,
        // which had no budget headroom left and grew by both of those sections on every new element. Each
        // phrase was checked against the folder the way the rows above were: "What you can build today"
        // occurs in one non-owner (activity-connections, where the validation-pass-is-not-buildable caveat
        // cites the slice), and lower-case "element catalog" in two (the entry's own index and recipe, and
        // N6 in process-naming). The owner's capitalised heading "Element catalog" matches this row too —
        // Occurrences is OrdinalIgnoreCase — and is harmless only because the owner is skipped. A
        // non-owner writing the capitalised form WILL be required to name the owner, which is correct.
        ("What you can build today", "process-element-catalog"),
        ("element catalog", "process-element-catalog"),
        // The perform-task split shipped with no rows, and that is precisely how a stale pointer got
        // through it: process-formulas kept sending readers to `process-perform-task` for the
        // allowed-results degradation after the rule moved to `process-task-category`, and no scan in
        // this file could see an unquoted pointer with no locator word. These two rows are what makes
        // that class of miss red rather than reviewable.
        ("allowed-results", "process-task-category"),
        // Keyed on "element-level `performer`" and NOT on "Who performs the task", which was the first
        // choice and is a tripwire: its one non-owner occurrence sits 185 characters from the owner name
        // in a 220-character window, so inserting 46 characters of unrelated prose into the same
        // parameter row reported it as unattributed when the article names the owner right below. Slack
        // is 144 characters on this phrase; every other row has 60 or more.
        ("element-level `performer`", "process-task-performer")
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

    /// <summary>
    /// What makes a backticked sibling name readable as a FETCH rather than as a heading. Every article in
    /// this set used to carry that sentence itself, which meant any single article could lose it and the
    /// other ten still told the reader; ENG-96536 deduplicated it into <c>routing</c>, which every agent
    /// reads before anything else, and that trade removed the redundancy along with the repetition.
    ///
    /// So it is pinned. Nothing else in the suite reads routing's prose — the routing assertions match
    /// `name=` tokens only — and routing is itself a get-guidance article under the same size pressure as
    /// any other, so a trim for length would take the convention out of the library entirely with the
    /// whole suite green. Every other rule this change moved got a marker row, a payload pin or a
    /// survival test; this one had none.
    /// </summary>
    private static readonly string[] ReadingConventionClauses =
    [
        "READING CONVENTION",
        "get-guidance topic to fetch",
        "not a heading to scroll to"
    ];

    [Test]
    [Description("The reading convention survives in routing, which is the only copy of it left.")]
    public void TheReadingConvention_ShouldSurviveInRouting()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string routing = Collapse(ReadRouting(repositoryRoot));

        string[] missing = [.. ReadingConventionClauses
            .Where(clause => !routing.Contains(clause, StringComparison.Ordinal))];

        missing.Should().BeEmpty(
            because: "eleven articles gave up their own copy of this sentence for this one, so it is now "
                + "the only place the library says that a backticked sibling name is a topic to fetch. "
                + "Without it every cross-article pointer in the set reads as a heading the reader cannot "
                + "find. If routing has to lose it, put it back in the articles rather than nowhere. "
                + "Missing: " + string.Join(", ", missing));
    }

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
            // Collapsed, because these markers are PHRASES and the articles are hard-wrapped: a marker
            // that a line break can split is a marker an ordinary reflow disarms silently. Found the way
            // everything here is found - a reflow while fixing ENG-95891's stale pointer moved "the last
            // conditional flow" across a wrap, the occurrence count went to zero, and the test passed
            // because it was no longer scanning anything.
            string text = Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath));
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
    [Description("Every moved-section marker still matches in an article that does NOT own it, which is "
        + "the only place the scan it feeds can report anything - so no row can be reworded into a guard "
        + "that cannot fail while the suite stays green.")]
    public void EveryMovedSectionMarker_ShouldStillMatchOutsideItsOwner()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] declared = ProcessGuideSet.Declared(repositoryRoot);
        string[] articles = [.. declared
            .Select(article => Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)))];
        // The PROCESS-folder ids, not DeclaredItemIds. The owner-skip this protects
        // (ProcessGuides_ShouldNameTheOwningArticle_WhenReferencingAMovedSection) iterates only the
        // articles Declared() yields, so an owner that is a real manifest id from somewhere else in the
        // library — a renamed row, or a copy-paste from another folder — is never skipped, and every
        // legitimate mention of that phrase gets reported instead. Checking against all 140 ids would
        // clear exactly that case and catch only a misspelling.
        HashSet<string> processItemIds = [.. declared.Select(article => article.ItemId)];

        // Not a vacuous-pass guard — an empty scan CONDEMNS every row here rather than clearing it,
        // because a marker matches nowhere in an empty set. It is here so that failure reports the
        // derivation as the cause instead of listing every marker as dead.
        articles.Should().NotBeEmpty(because: "a scan over no articles would report every row as dead");

        // OUTSIDE THE OWNER, deliberately. A first version of this test asked only whether the phrase
        // occurred anywhere in the set, and that is not the invariant: the scan this list feeds skips the
        // owner, so a row whose OWNER writes the phrase looks alive here while guarding nothing. Measured
        // on the row this change added: rewording the single non-owner occurrence of "What you can build
        // today" left every test in this fixture green, because the owner's own heading still matched.
        //
        // It does forbid a row aimed at a phrase nobody has written yet. That is the trade taken on
        // purpose: an unfalsifiable row is indistinguishable from a broken one, and this fixture exists
        // because a check that reassures is worse than no check. ("Naming and codes", process-naming) was
        // dropped for exactly this reason — zero non-owner occurrences, and references to that article
        // are already watched by ("N1-N10", process-naming), which has seven.
        string[] dead = [.. MovedSectionMarkers
            .Where(row => !declared
                .Where(article => article.ItemId != row.Owner)
                .Any(article => Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath))
                    .Contains(row.Marker, StringComparison.OrdinalIgnoreCase)))
            .Select(row => $"'{row.Marker}' (owner {row.Owner})")];
        string[] unownable = [.. MovedSectionMarkers
            .Where(row => !processItemIds.Contains(row.Owner))
            .Select(row => $"'{row.Marker}' names owner {row.Owner}")];

        dead.Should().BeEmpty(
            because: "the scan below skips a marker's OWNER, so a row that matches only inside its owner "
                + "has no article left to report and cannot fail — it reads as coverage and is none. That "
                + "has happened here twice already: a row keyed on manifest wording no article uses, and a "
                + "row whose one non-owner match was the entry index's own continuation line, which sits "
                + "sixty characters under the bullet that names the owner by construction. Re-key the row "
                + "to what a sibling now writes, or drop it. Unfalsifiable: "
                + string.Join("; ", dead));
        unownable.Should().BeEmpty(
            because: "an owner outside the scanned set cannot be skipped as the owner, so the row scans the "
                + "article that DEFINES the section as if it were borrowing it — and every legitimate "
                + "mention of the phrase is then reported. Found: "
                + string.Join("; ", unownable));
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
        // OrdinalIgnoreCase, matching Owns() above: a marker is a PHRASE, and which case a sibling writes
        // it in is not a fact about whether the reference is attributed. Case-sensitive, this scan needed
        // one row per spelling — and the spelling that actually dangled (process-formulas writing "branch
        // PRECEDENCE" where the index writes "branch precedence") was the one no row had.
        for (int index = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.OrdinalIgnoreCase))
        {
            yield return index;
        }
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    /// <summary>
    /// The manifest description a catalog listing shows for one guidance topic. Read from
    /// <c>bundle-source.json</c> rather than retyped, so a rewritten description is measured, not assumed.
    /// </summary>
    private static string ManifestDescription(string repositoryRoot, string itemId)
    {
        using System.Text.Json.JsonDocument manifest = System.Text.Json.JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(resource => resource.GetProperty("itemId").GetString() == itemId)
            .GetProperty("description").GetString()!;
    }

    [Test]
    [Description("The manifest's entry-article description indexes the same articles the entry article itself does. Both are indexes an agent picks a guide from - one it reads inside get-guidance, one it scans in the catalog beforehand - and only the article's copy was checked by anything. ENG-95891 split an eighth article out, updated the article's index and the counts in this sentence once for process-formulas, then split again and left the sentence naming seven; nothing went red, and an agent reading the catalog never learns process-branch-conditions exists.")]
    public void ManifestEntryDescription_ShouldIndexEveryArticleTheEntryArticleIndexes()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] articles = ProcessGuideSet.Declared(repositoryRoot);
        string entryItemId = ProcessGuideSet.SplitItemIds[0];
        string entry = ProcessGuideSet.Read(repositoryRoot,
            articles.Single(article => article.ItemId == entryItemId).SourcePath);
        string description = ManifestDescription(repositoryRoot, entryItemId);

        string[] indexedByArticle = [.. articles
            .Where(article => article.ItemId != entryItemId)
            .Where(article => entry.Contains($"`{article.ItemId}`", StringComparison.Ordinal))
            .Select(article => article.ItemId)];

        indexedByArticle.Length.Should().BeGreaterThan(1,
            because: "the requirement is derived from what the entry article names, so an index that stopped "
                + "naming its siblings would otherwise reduce this test to asserting nothing");
        indexedByArticle
            .Where(itemId => !description.Contains(itemId, StringComparison.Ordinal))
            .Should().BeEmpty(
                because: "the entry article indexes these and the manifest description does not, so the two "
                    + "indexes disagree about what the set contains - and the catalog is the one an agent "
                    + "reads BEFORE deciding which guide to fetch, so an article missing from it is one that "
                    + "has to be already known to be found");
    }
}

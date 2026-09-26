using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// ENG-100155: <c>process-digest</c> is the build CARD for the process domain. It owns no rule; it restates,
/// in short form and naming each owner, what every build and edit needs from four articles, so an agent can
/// read it instead of all four. A digest drifts from what it summarises unless something ties the two
/// together, and this fixture is that tie - the synchronisation rule CONTRIBUTING states in prose.
/// </summary>
/// <remarks>
/// <para>
/// Why the card exists at all: the four articles it stands in for were MANDATORY reading for every new
/// process - roughly 91 KB served - before the agent read the article of a single element it actually
/// used. In the CAADT gate the dearest prompt pulled 19-20 process articles, the context compacted, and 18
/// of 33 guidance reads in one run were re-reads of an article lost to the compaction (ENG-99970).
/// </para>
/// <para>
/// What the restated-clause table does and does not prove, stated the way
/// <c>ARuleRestatedOutsideItsOwner_ShouldStillAgreeWithTheOwner</c> states it for its own pairs: each row is
/// a TRIM ALARM on one clause both copies carry verbatim. It fails when either side rewords or drops the
/// clause - which is when the card would start telling an agent something its owner no longer says. It does
/// NOT prove the two agree: a contradiction written around an intact clause passes. Agreement stays a
/// reviewer's job; the table makes the obvious drift impossible to merge.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ProcessDigestTests
{
    private const string DigestItemId = "process-digest";

    /// <summary>
    /// A CARD budget, well under the article budget: the digest earns its place only while it is a fraction
    /// of what it replaces - about a sixth of the ~91 KB the four owners serve - and while it stays inline in
    /// an agent CLI (the smallest spill measured in the CAADT transcripts was 21.3 KB). Past this it has
    /// become a fifth long article that every build reads. Measured JSON-escaped, like every size in this
    /// suite; the descriptor skeleton is quote-dense and the rules are backtick-dense.
    /// <para>
    /// Why it is not smaller: a first cut was 12.4 K and a refute-first check against the owners found that
    /// the short form had dropped conditions an agent acts on - the email-to-each-item D1 case, the result
    /// SELECTION rule for branches off Approval or Perform task, the three user tasks with their own build
    /// type, the version floor for a second signal start, the existing-process restructure ban. Restoring
    /// them is what the extra 3 K is. Shrink the card by moving a DETAIL to its owner, never by dropping a
    /// condition that changes what the agent does.
    /// </para>
    /// </summary>
    private const int MaxDigestResponseCharacters = 16_500;

    /// <summary>The four articles the card restates. A block that names another owner is a new decision.</summary>
    private static readonly string[] Owners =
    [
        "process-modeling",
        "process-element-catalog",
        "process-naming",
        "process-sub-process-when"
    ];

    /// <summary>
    /// Clauses the card and their owner both carry VERBATIM (whitespace-collapsed). Chosen from the rules an
    /// agent acts on without opening the owner: the trigger names that decide how many processes, the naming
    /// rules the code derivation depends on, the start and edit rules that are refused or irreversible, and
    /// the list of what does not build.
    /// </summary>
    private static readonly (string Owner, string Clause)[] RestatedClauses =
    [
        ("process-sub-process-when", "A request produces ONE process."),
        ("process-sub-process-when", "D1 — the same work for EACH item of a set: multi-instance, do not ask"),
        ("process-sub-process-when", "D2 — the same fragment twice in the plan: one helper, do not ask"),
        ("process-sub-process-when", "D4 — two or more long phases: propose a split, and ask"),
        ("process-element-catalog", "UNSUPPORTED through `create-business-process`"),
        ("process-naming", "`<prefix><Object>_<Action>`"),
        ("process-naming", "`a`, `an`, `the`, `is`, `are`, `was`, `were`, `be`, `been`, `has`, `have`, `had`"),
        ("process-naming", "\"Account is added\" -> `AccountAddedSignal`"),
        ("process-naming", "PascalCase plus a `Parameter` suffix"),
        ("process-naming", "LABEL EVERY CONDITIONAL AND DEFAULT ARM"),
        ("process-modeling", "ONE START PER TRIGGER"),
        ("process-modeling", "`removeFlow` takes `source` and `target` ONLY — MUST strip `kind` and `condition` first"),
        ("process-modeling", "You MUST read `isActiveVersion` from the describe output before ANY modify"),
        ("process-modeling", "The modify path runs NO structural validation"),
        ("process-modeling", "Do NOT run `compile-creatio` to"),
        ("process-modeling", "Pass the descriptor as the JSON object itself"),
        ("process-modeling", "CrtProcessBuilder 1.6.2.24 or later on the environment AND clio 8.1.0.131 or later"),
        ("process-modeling", "prefer additive edits, do not remove or rewire"),
        ("process-element-catalog", "THREE user tasks have their own dedicated build type and must NOT be built as a generic `userTask`"),
        ("process-sub-process-when", "Never restructure an existing process without being asked")
    ];

    [Test]
    [Description("The digest stays a CARD: small enough that reading it in place of the four articles it restates is the saving it exists for.")]
    public void TheDigest_ShouldStayWithinItsCardBudget()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string text = ReadDigest(repositoryRoot);

        // Act
        int size = JsonSerializer.Serialize(text).Length;

        // Assert
        size.Should().BeLessThanOrEqualTo(MaxDigestResponseCharacters,
            because: $"the card replaces roughly 91 KB of mandatory reading; at {size:N0} characters it is "
                + "turning into another long article every build pays for - move detail back to its owner");
    }

    [Test]
    [Description("Every clause the digest restates is still carried verbatim by BOTH the digest and the article that owns it, so neither can reword a rule the other still states.")]
    public void EveryRestatedClause_ShouldStillBeCarriedByTheDigestAndItsOwner()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        ProcessGuideSet.Article[] declared = ProcessGuideSet.Declared(repositoryRoot);
        string digest = Collapse(ReadDigest(repositoryRoot));
        string Owner(string itemId) => Collapse(ProcessGuideSet.Read(repositoryRoot,
            declared.Single(article => article.ItemId == itemId).SourcePath));

        // Act
        string[] drifted = [.. RestatedClauses
            .Where(row => !digest.Contains(Collapse(row.Clause), StringComparison.Ordinal)
                || !Owner(row.Owner).Contains(Collapse(row.Clause), StringComparison.Ordinal))
            .Select(row => $"{row.Owner}: \"{row.Clause}\"")];

        // Assert
        drifted.Should().BeEmpty(
            because: "the digest restates these rules for an agent that will NOT open the owner, so a clause "
                + "reworded on one side is a rule the card now states differently from its owner. Change BOTH "
                + "in the same pull request (CONTRIBUTING, 'The process digest'). Drifted: "
                + string.Join("; ", drifted));
    }

    [Test]
    [Description("The digest names an owner on every block, and every owner it names is one of the four articles it restates.")]
    public void EveryDigestBlock_ShouldNameAnOwnerTheDigestRestates()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string text = ReadDigest(repositoryRoot);

        // Act
        string[] headings = [.. Regex.Matches(text, @"^== (?<title>.+?) ==$", RegexOptions.Multiline)
            .Select(match => match.Groups["title"].Value)];
        string[] withoutOwner = [.. headings.Where(title => !title.Contains("(owner", StringComparison.Ordinal))];
        string[] foreignOwners = [.. headings
            .SelectMany(title => Regex.Matches(title, @"`(?<id>process-[a-z-]+)`").Select(match => match.Groups["id"].Value))
            .Where(id => !Owners.Contains(id) && id != "process-versions")
            .Distinct()];

        // Assert
        headings.Should().NotBeEmpty(because: "anti-vacuity: the card is organised in owner-named blocks");
        withoutOwner.Should().BeEmpty(
            because: "a block with no owner is a rule the card now owns, and the card owns none - "
                + string.Join("; ", withoutOwner));
        foreignOwners.Should().BeEmpty(
            because: "restating a fifth article is a decision to make on purpose, with rows in the clause table "
                + "above - not a heading edit. Found: " + string.Join(", ", foreignOwners));
    }

    private static string ReadDigest(string repositoryRoot) =>
        ProcessGuideSet.Read(repositoryRoot, ProcessGuideSet.Declared(repositoryRoot)
            .Single(article => article.ItemId == DigestItemId).SourcePath);

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

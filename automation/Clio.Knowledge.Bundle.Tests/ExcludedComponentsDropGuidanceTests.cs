using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-95081 payload: the guidance that teaches an agent to read an
/// <c>excludedComponents</c> drop as a POSITIONAL exclusion rather than conversion loss. The content
/// digest cannot guard a rule's wording — it is re-recorded on every edit — so a dropped clause, a
/// softened directive, or a reason string that stops matching what the converter emits would
/// otherwise ship green.
/// <para>
/// The clause this exists for is "do NOT re-insert the component". Losing it silently undoes the
/// ticket: the converter still removes the component, the agent puts it straight back, and the page
/// looks exactly as it did before the fix.
/// </para>
/// </summary>
[TestFixture]
public sealed class ExcludedComponentsDropGuidanceTests
{
    private const string OwnerGuide = "guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md";

    /// <summary>
    /// The two reason CODES clio emits for one positional exclusion. Both come from
    /// <c>ExcludedComponentsPass</c> in the clio repository — the first from <c>BuildDropReason</c>
    /// (the component the rule names), the second from <c>DropOrphanedSubtrees</c> (everything that
    /// hung below it). An agent branches on these codes, so a divergence between the converter and this
    /// guide is a silent failure: the unmatched drop reads as conversion loss and the natural response
    /// to conversion loss is to re-insert.
    /// </summary>
    /// <remarks>
    /// These were English SUBSTRINGS until ENG-95827 replaced <c>reason</c> with a list of
    /// {code, params}. Pinning the codes is strictly stronger: a code is a closed-vocabulary token the
    /// converter cannot reword by accident, whereas the old fragments could drift out of a format string
    /// with nothing failing until an agent misclassified a drop.
    /// </remarks>
    private static readonly (string Fragment, string Because)[] ReasonShapes =
    [
        ("drop-excluded-by-rule",
            "the direct-removal code ExcludedComponentsPass.BuildDropReason emits; the guide keys on this code"),
        ("drop-parent-excluded",
            "the orphan-cascade code DropOrphanedSubtrees emits — a rule targeting a CONTAINER type produces "
                + "mostly this shape, and it names the very elements a user asks about")
    ];

    /// <summary>
    /// Clauses that each carry weight: dropping any one leaves the paragraph readable but wrong.
    /// </summary>
    private static readonly (string Fragment, string Because)[] LoadBearingClauses =
    [
        ("POSITIONAL",
            "the whole point is that the exclusion is about WHERE the component sat, not about the type being unsupported"),
        ("NOT conversion loss",
            "an agent that reads the drop as loss will try to recover it"),
        ("do NOT re-insert the component",
            "the single directive whose loss silently undoes ENG-95081"),
        ("not into that host, not",
            "re-inserting somewhere else is the obvious workaround and must be closed explicitly"),
        ("do NOT ask whether to keep it",
            "asking re-opens a decision the converter configuration already made"),
        ("converter configuration",
            "which types are banned from which hosts must stay data the agent READS, never a list it memorizes"),
        ("codes rather than assuming one",
            "the agent must be routed to the codes rather than to an assumed type list"),
        ("converts normally",
            "dropped-here / kept-there on one page is correct and must not be reported as an inconsistency")
    ];

    [Test]
    [Description("Both reason shapes clio emits for a positional exclusion are taught by the guide, so neither kind of drop reaches an agent unexplained.")]
    public void Guide_ShouldTeachBothExcludedComponentsReasonShapes()
    {
        string guide = Normalize(ReadGuide(OwnerGuide));

        string[] missing = ReasonShapes
            .Where(shape => !guide.Contains(shape.Fragment))
            .Select(shape => $"{shape.Fragment} ({shape.Because})")
            .ToArray();

        missing.Should().BeEmpty(
            because: "a reason string the converter emits but the guide never names is a drop the agent cannot "
                + "classify, and an unclassified drop reads as conversion loss");
    }

    [Test]
    [Description("Every load-bearing clause of the positional-exclusion guidance is intact.")]
    public void Guide_ShouldKeepEveryLoadBearingClauseOfTheExclusionRule()
    {
        string guide = Normalize(ReadGuide(OwnerGuide));

        string[] dropped = LoadBearingClauses
            .Where(clause => !guide.Contains(clause.Fragment))
            .Select(clause => $"{clause.Fragment} ({clause.Because})")
            .ToArray();

        dropped.Should().BeEmpty(
            because: "each clause was added to make the guidance correct; dropping one re-records the digest silently");
    }

    private static string ReadGuide(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull(because: "the tests must run from inside the knowledge repository");
        return directory!.FullName;
    }
}

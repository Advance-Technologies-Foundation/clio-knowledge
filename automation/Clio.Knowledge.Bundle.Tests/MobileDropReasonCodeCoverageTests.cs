using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the CLOSED VOCABULARY of <c>guide.droppedElements[].reason</c> against the article that decodes
/// it: every code clio can emit has an entry, and the article documents no code clio never emits.
/// <para>
/// This fixture exists because ENG-95827 shipped a gap that nothing could have caught. The converter's
/// last commit folded <c>relocate-children</c> into <c>droppedElements</c> as
/// <c>drop-container-no-mobile-equivalent</c>, the article update missed it, and every test stayed green
/// — the code fires on ZERO of the 12 drops of the OOTB Leads_FormPage the whole conversion effort was
/// measured on, so no sample, no regression fixture and no eyeball would ever have shown it absent.
/// </para>
/// <para>
/// An undocumented code is not a cosmetic gap. The article's own fallback ("an UNKNOWN code means your
/// clio is newer than this article — report it verbatim and do not guess") then turns the most benign
/// outcome the converter has into its most alarming report: a FLATTENED branch, where the layout wrapper
/// is gone and every child was kept and re-parented, reaches the user as an unexplained drop.
/// </para>
/// </summary>
[TestFixture]
public sealed class MobileDropReasonCodeCoverageTests
{
    /// <summary>The article that owns the decode table; the routing guide sends a caller here by name.</summary>
    private const string CodeArticle = "guidance/mcp/guides/platform/mobile/web-to-mobile-reason-codes.md";

    /// <summary>
    /// Every code <c>ReasonCodes</c> declares in the clio repository
    /// (<c>clio/Command/McpServer/Tools/MobilePageConverter/MobilePageConversionGuideModels.cs</c>), paired
    /// with what makes it worth documenting separately.
    /// </summary>
    /// <remarks>
    /// Held as LITERALS deliberately: this repository does not reference clio, so the two halves of the
    /// contract cannot be compared by reflection from here. The other half of the guard lives in clio —
    /// <c>MobileDropReasonCodeVocabularyTests</c> pins this same set against the real constants, so a code
    /// ADDED there fails THERE, with a message naming this article. Together the two fixtures close both
    /// directions: a code added in clio cannot ship undocumented, and an entry deleted here cannot ship
    /// either.
    /// </remarks>
    private static readonly (string Code, string Because)[] ConverterCodes =
    [
        ("drop-inherited-chrome",
            "the mobile template provides the element natively; re-adding it duplicates a native control"),
        ("drop-excluded-by-rule",
            "a POSITIONAL exclusion — the same type converts normally outside that host, so the drop must not read as loss"),
        ("drop-parent-excluded",
            "the orphan cascade; it names the very elements a user asks about, so an exclusion must be matched on BOTH codes"),
        ("drop-empty-container",
            "housekeeping after every child was dropped; the shell must not be re-created"),
        ("drop-container-no-mobile-equivalent",
            "the branch is FLATTENED, not lost — the children are in viewConfigDiff under a new parent; "
                + "without an entry this benign case is reported as loss"),
        ("drop-unsupported-request",
            "genuine loss the user must be told about: the request is KNOWN-unsupported on mobile"),
        ("drop-unknown-request",
            "clio cannot claim the request is unavailable, only that it does not know it — the caller can re-add it by hand"),
        ("drop-type-not-in-mobile-registry",
            "the LEAF counterpart of drop-container-no-mobile-equivalent, and the one drop that IS loss for the same cause"),
        ("drop-target-missing",
            "a conversion-RULES defect (a rule retargets into a container the target template lacks), not a page problem"),
        ("drop-no-rule-in-scope",
            "inside a non-converting scope: nothing to do, and nothing to report as broken"),
        ("drop-not-an-action-in-scope",
            "its nested actions were still flattened and appear on their own, so the element's own absence is expected")
    ];

    [Test]
    [Description("Every droppedElements reason code the converter can emit has an entry in the decode article, so no drop reaches a caller unexplained.")]
    public void Article_ShouldDocumentEveryCodeTheConverterEmits()
    {
        // Arrange
        string article = ReadGuide(CodeArticle);

        // Act
        string[] undocumented = ConverterCodes
            .Where(code => !article.Contains(code.Code))
            .Select(code => $"{code.Code} ({code.Because})")
            .ToArray();

        // Assert
        undocumented.Should().BeEmpty(
            because: "a code the converter emits but this article never names is a drop the caller cannot "
                + "classify, and the article's own fallback then has it reported as unexplained loss");
    }

    [Test]
    [Description("The decode article documents no drop code the converter cannot emit, so a caller never writes a branch for a case that never arrives.")]
    public void Article_ShouldNotDocumentACodeTheConverterNeverEmits()
    {
        // Arrange
        string article = ReadGuide(CodeArticle);
        HashSet<string> emitted = ConverterCodes.Select(code => code.Code).ToHashSet(StringComparer.Ordinal);

        // Act
        string[] invented = Regex.Matches(article, @"drop-[a-z]+(?:-[a-z]+)*")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .Where(code => !emitted.Contains(code))
            .ToArray();

        // Assert
        invented.Should().BeEmpty(
            because: "a documented code the converter never emits is dead guidance: the caller writes a "
                + "branch that never runs, and a REAL code renamed in clio hides behind the stale entry");
    }

    private static string ReadGuide(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

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

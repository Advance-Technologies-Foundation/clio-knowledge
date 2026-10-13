using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins behavioural rules in the CORE of the ENG-103030 process splits.
/// </summary>
/// <remarks>
/// <para>
/// Each core's banner tells an ordinary build to read the core only and open its <c>-details</c> article
/// for evidence and reasoning. A rule that moves to <c>-details</c> is therefore a rule an ordinary build
/// no longer reads. The split first moved three such rules out, and the review caught them: the caption
/// as the better identity on a versioned process, the gateway-default re-kind trap, and the level naming
/// difference between <c>record-rights</c> and this element. These pins keep them in the core when the
/// next budget squeeze moves text to <c>-details</c>.
/// </para>
/// <para>
/// Every pin reads WHITESPACE-COLLAPSED text, so a reflow of the hard-wrapped article does not break it.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ProcessSplitCoreRuleTests
{
    private const string ProcessGuides = "guidance/mcp/guides/processes/";

    /// <summary>
    /// A label the split once generated in front of a moved sentence ("On `allRolesAndUsers`: The shipped
    /// approval processes are built that way", "How `restrict` writes rows: ..."), which strips the sentence
    /// of what it refers to. Matched at a line start, where the generator put them.
    /// </summary>
    private static readonly Regex GeneratedLabelPrefix =
        new(@"^\s*(- )?(On|Why|How) [^.:\n]{1,80}:\s+[A-Za-z`]", RegexOptions.Multiline | RegexOptions.Compiled);

    [Test]
    [Description("process-script-task keeps, in the core, that on a versioned process the caption is the better identity because it resolves to the ACTIVE version.")]
    public void ScriptTaskCore_ShouldPreferTheCaptionOnAVersionedProcess()
    {
        // Arrange
        string core = Collapsed("process-script-task.md");

        // Act + Assert
        core.Should().Contain(
            "on a versioned process the caption is the better identity, because it resolves to the ACTIVE version while a code names one member",
            because: "an agent reading only the core would otherwise hand get-process-signature or run-process a pinned "
                + "version code and target a version that is not active");
    }

    [Test]
    [Description("process-branch-conditions keeps, next to 're-kind the existing default first', the trap that re-kinding the gateway's own default to sequence silently does nothing.")]
    public void BranchConditionsCore_ShouldWarnThatRekindingTheGatewayDefaultToSequenceDoesNothing()
    {
        // Arrange
        string core = Collapsed("branch-conditions.md");
        const string instruction = "re-kind the existing default first";
        const string trap = "re-kinding the gateway's OWN default to `sequence` is a TRAP that silently does nothing";

        // Act
        int instructionAt = core.IndexOf(instruction, StringComparison.Ordinal);
        int trapAt = core.IndexOf(trap, StringComparison.Ordinal);

        // Assert
        instructionAt.Should().BeGreaterThanOrEqualTo(0, because: "anti-vacuity: the core gives the instruction the trap qualifies");
        trapAt.Should().BeGreaterThan(instructionAt,
            because: "the flow stays the default and the next setFlow is refused again, so an agent that follows the "
                + "instruction without the warning loops or reports the branch as fixed");
        (trapAt - instructionAt).Should().BeLessThan(200, because: "the warning belongs next to the instruction it qualifies");
    }

    [Test]
    [Description("process-access-rights keeps, at the top of the core, that record-rights spells levels granted/delegated while this block takes permit/delegate/restrict.")]
    public void AccessRightsCore_ShouldWarnAboutTheLevelNamingDifferenceAtTheTop()
    {
        // Arrange
        string core = Collapsed("access-rights.md");
        const string warning = "`record-rights` exposes `granted`/`delegated`, this block takes `permit`/`delegate`/`restrict`";

        // Act
        int warningAt = core.IndexOf(warning, StringComparison.Ordinal);
        int firstSectionAt = core.IndexOf("== Read this first", StringComparison.Ordinal);

        // Assert
        firstSectionAt.Should().BeGreaterThanOrEqualTo(0, because: "anti-vacuity: the core still has the section the warning must precede");
        warningAt.Should().BeGreaterThanOrEqualTo(0,
            because: "an agent fresh from record-rights writes \"level\": \"granted\", which this block does not take");
        firstSectionAt.Should().BeGreaterThan(warningAt, because: "the warning sits at the top of the core, before the first section");
    }

    [Test]
    [Description("No process -details article carries a generated 'On X:' / 'Why X:' / 'How X:' label in front of a moved sentence.")]
    public void DetailsArticles_ShouldNotCarryGeneratedOnLabelPrefixes()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        string[] detailsArticles = [.. Directory
            .GetFiles(Path.Combine(repositoryRoot, ProcessGuides), "*-details.md")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];

        // Act
        string[] labelled = [.. detailsArticles
            .SelectMany(file => GeneratedLabelPrefix
                .Matches(ProcessGuideSet.Read(repositoryRoot, ProcessGuides + file))
                .Select(match => $"{file}: {match.Value.Trim()}"))];

        // Assert
        detailsArticles.Should().Contain("access-rights-details.md",
            because: "anti-vacuity: the scan reads the process -details articles");
        labelled.Should().BeEmpty(
            because: "a moved sentence must stand on its own; a label like \"On `allRolesAndUsers`:\" leaves \"that way\" "
                + "referring to nothing");
    }

    private static string Collapsed(string file) =>
        Regex.Replace(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ProcessGuides + file), @"\s+", " ");
}

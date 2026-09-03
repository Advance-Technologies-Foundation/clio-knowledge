using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Content pins for the <c>process-versions</c> article (ENG-94374).
///
/// The article exists because an agent that assumes the ordinary "one schema, many revisions" shape
/// explains and edits a graph nobody runs. Each fact pinned below is one an agent has to be told and
/// cannot derive, so losing it in an edit is the failure this fixture exists to catch. Assertions are
/// deliberately SHORT contiguous phrases: the article wraps at a fixed width, and a longer quote would
/// break on a line wrap rather than on a content change.
/// </summary>
[TestFixture]
public sealed class ProcessVersionsGuidanceTests
{
    private const string GuidePath = "guidance/mcp/guides/processes/versions.md";

    [Test]
    [Description("Pins the five platform facts about the version model that an agent cannot derive: flat family, one active version, instances pinned to their version, rollback affecting later runs only, and no delete.")]
    public void Guide_ShouldStateTheVersionModel()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("The family is FLAT.",
            because: "an agent that expects a chain looks for 'the previous version', which the platform does not store");
        guide.Should().Contain("Exactly ONE member of a family is the ACTIVE version",
            because: "every other member is readable and startable, so 'it exists' is not 'it runs'");
        guide.Should().Contain("A running INSTANCE stays on the version it started on.",
            because: "an agent that believes an edit or a rollback reaches work in flight promises a repair that never happens");
        guide.Should().Contain("affects only runs that start afterwards",
            because: "that is the whole meaning of a rollback here, and the half a builder most often assumes wrongly");
        guide.Should().Contain("DELETING a version does not exist.",
            because: "the platform's own removal path would cancel every logged run of that schema, so the absence is permanent rather than a gap to work around");
    }

    [Test]
    [Description("Pins the rule that versionhood is never inferable from a schema Name, and that absent version fields mean unknown rather than unversioned.")]
    public void Guide_ShouldRefuseNameBasedInferenceAndZeroDefaulting()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("A schema Name tells you NOTHING about whether a process is a version",
            because: "a version's name tail is a package name plus a number, so a regex over names is a wrong answer that looks right");
        guide.Should().Contain("ABSENT is not zero, and zero is not \"unversioned\" either.",
            because: "reporting an unestablished standing as 'unversioned' reproduces the defect the version fields were added to stop");

        guide.Should().Contain("only THIS IS THE FAMILY ROOT",
            because: "the root of a versioned family reports 0 with no warning, so 0 alone can never settle whether a process has versions - the review found this sentence claiming the opposite");
        guide.Should().Contain("LENGTH of `versions[]`",
            because: "a rule that only forbids an inference leaves the agent stuck; the article has to name the field that actually discriminates");
        guide.Should().Contain("a capped list is not a count",
            because: "versions[] can be truncated, so the discriminator is unusable without that caveat attached to it");
    }

    [Test]
    [Description("Pins the tool names the article instructs an agent to call, since clio owns those constants and a rename there must not leave this article pointing at a tool that does not exist.")]
    public void Guide_ShouldNameOnlyShippedToolNames()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string[] instructedTools =
        [
            "describe-business-process",
            "get-process-signature",
            "generate-process-model",
            "modify-business-process",
            "run-process"
        ];
        // Near-misses of the names above rather than arbitrary wrong strings, because a drifting edit
        // reaches for the plausible shape, not an implausible one.
        string[] wrongSpellings =
        [
            "describe-process",
            "describe_business_process",
            "run_process",
            "get-process-signatures",
            "generate-process-models"
        ];

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, GuidePath);

        // Assert
        foreach (string tool in instructedTools)
        {
            guide.Should().Contain(tool,
                because: $"the article instructs the agent to call {tool}, so the exact shipped name has to appear");
        }
        foreach (string wrong in wrongSpellings)
        {
            guide.Should().NotContain(wrong,
                because: $"'{wrong}' is not a tool clio ships, and an agent that copies it from guidance gets an unknown-tool error");
        }
    }

    [Test]
    [Description("Keeps the article a declared, routed get-guidance topic and keeps the entry article's own index and counts in step with it.")]
    public void Resource_ShouldBeDeclaredRoutedAndCounted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-versions");
        string routing = ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/routing.md");
        string entryArticle = ProcessGuideSet.Read(repositoryRoot,
            "guidance/mcp/guides/processes/process-modeling.md");
        string entryDescription = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-modeling")
            .GetProperty("description").GetString()!;

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(GuidePath,
            because: "the manifest is the agent-facing contract, and a body it does not declare is unreachable");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "the version model applies to every existing process, gated tools or not");
        routing.Should().Contain("name=process-versions",
            because: "routing is the only guidance pointer clio's MCP instructions carry, so an unrouted article is reachable only by already knowing it exists");
        entryArticle.Should().Contain("`process-versions`",
            because: "the entry article indexes the set, and an article missing from the index is one a reader never learns to fetch");
        entryDescription.Should().Contain("and process-versions.",
            because: "the entry item's description enumerates what it routes to, so a new article has to join that list");
        entryDescription.Should().Contain("Each of the eight",
            because: "the description states how many articles the set holds, and a stale count is a claim the reader can check and find wrong");
    }
    [Test]
    [Description("Pins the version boundary and the modify precondition: guidance publishes ahead of the clio release that carries the fields, and an in-place edit of the running version is irreversible.")]
    public void Guide_ShouldDeclareItsClioBoundaryAndTheModifyPrecondition()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, GuidePath);
        string entryArticle = ProcessGuideSet.Read(repositoryRoot,
            "guidance/mcp/guides/processes/process-modeling.md");

        // Assert
        // AGENTS.md requires an explicit compatible clio version, and the version that first carries
        // these fields is unknown until the read half merges and is tagged: 8.1.0.118 is already
        // released WITHOUT them, so naming it would tell a user on that build they have the feature.
        // The placeholder therefore warns on every run rather than failing the suite, and the strict
        // assertion takes over the moment a real version is written in.
        Match placeholder = Regex.Match(guide, @"from clio\s+<CLIO-READBACK-VERSION-TBD>\s+onward");
        Match declared = Regex.Match(guide, @"from clio\s+(\d+\.\d+\.\d+\.\d+)\s+onward");
        if (placeholder.Success)
        {
            Assert.Warn("process-versions still carries <CLIO-READBACK-VERSION-TBD>. Before opening the "
                + "pull request: fetch and merge master, find the clio release that first CONTAINS the "
                + "version read-back (the next tag after it lands - verify against the real tag, do not "
                + "assume), and replace the placeholder with it.");
        }
        else
        {
            declared.Success.Should().BeTrue(
                because: "with the placeholder gone the boundary must name a four-part clio version, or the article declares no compatibility at all");
            declared.Groups[1].Value.Should().NotBe("8.1.0.118",
                because: "that release is tagged and does NOT contain the read half, so it would tell every user on it that the fields are available");
        }
        guide.Should().Contain("Check by BEHAVIOUR rather than by number",
            because: "the number is advisory and can be wrong across forks and pre-releases; the absent-fields-and-no-warning test is performable and always true");
        guide.Should().Contain("does not report version standing at all",
            because: "on an older clio every version field is absent WITHOUT a warning, a state the read-failure rule does not cover and which the agent would otherwise read as unversioned");
        entryArticle.Should().Contain("You MUST read `isActiveVersion` from the describe output before ANY modify",
            because: "CONTRIBUTING.md forbids separating an irreversible operation from its preconditions, and modify-business-process is owned by this article, not by process-versions");
        entryArticle.Should().Contain("irreversible",
            because: "an edit to the active version has no version boundary and nothing to roll back to, which is what makes explicit confirmation mandatory");
        entryArticle.Should().Contain("launching ANY existing process",
            because: "a pointer conditional on 'has versions' is unusable: that fact is only knowable from the article the pointer gates");
    }

    [Test]
    [Description("Pins the clio-owned field names the article is now the library's only description of, plus the two numbered rules it cites by ID and the boundary of what this build cannot do.")]
    public void Guide_ShouldPinTheFieldContractTheRulesItCitesAndItsBoundary()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string[] fields =
        [
            "`version`", "`isActiveVersion`", "`activeVersionName`", "`activeVersionSchemaUId`",
            "`versionRootSchemaUId`", "`versions[]`", "`activeVersionSource`", "`versionsTruncatedAt`",
            "`versionReadWarning`"
        ];

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, GuidePath);

        // Assert
        foreach (string field in fields)
        {
            guide.Should().Contain(field,
                because: $"clio owns {field} and this article is the library's only description of it, so a rename there has to break here rather than ship a contract nothing serves");
        }
        // The article cites V1 and V4 by number three times, so trimming or renumbering them silently
        // breaks its own cross-references.
        guide.Should().Contain("V1  A version is a SEPARATE SCHEMA",
            because: "process-name and run-process both justify themselves by citing V1");
        guide.Should().Contain("V4  A version's Name is",
            because: "V7's first bullet cites V4 for why a name tail proves nothing");
        guide.Should().Contain("the implication runs one way",
            because: "a stock stand carries parentless schemas numbered 1 and 2, so 'the root is version 0' as a two-way rule is measurably false (ENG-94374 story 8)");
        guide.Should().Contain("Treat this as the default,",
            because: "stock versions named ...V2 / ...Extended / ...WithTracking exist, so V4 must not read as a test a caller can apply");
        guide.Should().Contain("There is no operation that CREATES a version.",
            because: "the boundary is what stops an agent promising a version or a rollback this build cannot perform");
        guide.Should().Contain("There is no operation that SETS the active version",
            because: "a rollback request has to be refused explicitly, not answered with a process copy");
    }

    [Test]
    [Description("Pins the three-outcome isActiveVersion rule and the ABSENCE of the unconditional redirect that a round-one splice left behind, which 125 green tests did not notice.")]
    public void Guide_ShouldDefineAllThreeOutcomesAndNotRestateTheUnconditionalRedirect()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = ProcessGuideSet.Read(repositoryRoot, GuidePath);

        // Assert
        guide.Should().Contain("Three outcomes",
            because: "this is the only instruction the article makes mandatory, so an undefined branch propagates into a wrong edit");
        guide.Should().Contain("FALSE WITH an `activeVersionSchemaUId`",
            because: "the redirect is only performable when the response carries the UId to redirect by");
        guide.Should().Contain("FALSE with NO `activeVersionSchemaUId`",
            because: "the branch where there is nothing to redirect to has to be stated, not left to the reader");
        guide.Should().NotContain("describe again by `activeVersionSchemaUId` and work from that.",
            because: "that sentence prescribes the redirect UNCONDITIONALLY and contradicts the branch above it - it survived a round-one edit and shipped green, so its absence is what has to be guarded");
        guide.Should().Contain("WHEN `versions[]` is present",
            because: "the remedy named a field that is absent in exactly the state that reaches it, which made a mandatory instruction unexecutable");
    }

}

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the csharp-platform-checks guide: the eight Creatio-specific checks for C# an agent writes or reviews, in
/// the form read in platform source, and the way agents reach it. It is reached only through a one-sentence
/// pointer in the guides where C# is written, never through routing or another guide every task reads, so an
/// agent fetches it when it is about to write or review C# and not just in case.
/// </summary>
[TestFixture]
public sealed class CSharpPlatformChecksGuidanceTests
{
    private const string ItemId = "csharp-platform-checks";

    /// <summary>The guides where C# is or may be written, each of which carries the pointer.</summary>
    private static readonly string[] PointerGuides =
    [
        "guidance/mcp/guides/processes/process-script-task.md",
        "guidance/mcp/guides/backend/configuration-webservice.md",
        "guidance/mcp/guides/backend/portal-service-routing.md",
        "guidance/mcp/guides/composable-app/configuration-entity-event-listener.md",
        "guidance/mcp/guides/composable-app/application-listener.md",
        "guidance/mcp/guides/backend/assembly-package.md",
        "guidance/mcp/guides/integration/websocket-messaging.md",
        "guidance/mcp/guides/operations/record-permission-extensions.md",
        "guidance/mcp/guides/backend/virtual-entities.md",
        "guidance/mcp/guides/processes/process-custom-elements.md",
        "guidance/mcp/guides/integration/custom-mcp-tools.md",
        "guidance/mcp/guides/esq-filters/backend.md",
        "guidance/mcp/guides/composable-app/sys-setting.md",
        "guidance/mcp/guides/composable-app/atf-repository-dev.md"
    ];

    /// <summary>The longest pointer line accepted: one sentence, not a second copy of the guide.</summary>
    private const int MaxPointerLineBytes = 160;

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Guide() =>
        Regex.Replace(Read("guidance/mcp/guides/backend/csharp-platform-checks.md"), @"\s+", " ");

    [Test]
    [Description("Every check states its correct form as read in platform source, and none of the refuted forms appears.")]
    public void Guide_ShouldStateEveryCheckWithItsCorrectForm()
    {
        // Arrange
        string[] requiredFacts =
        [
            "`AddAllSchemaColumns()`",
            "`AddColumn(\"<column path>\")` for each column the code reads",
            "`.WithHints(Hints.NoLock)`",
            "A `CustomQuery` cannot take a hint",
            "`using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())`",
            "`RollbackTransaction()` in the catch",
            "Disposing the executor rolls back what is still open",
            "`MsgChannelUtilities.PostMessageToAll(`",
            "`MsgChannelUtilities.PostMessage(UserConnection, sender, text)`",
            "inherits `BaseService` and uses its `UserConnection`",
            "uses the `UserConnection` passed to `Execute`",
            "There is no DI registration for `UserConnection`",
            "a `SecureText` system setting read through `SysSettings`",
            "`Supervisor` / `Supervisor`",
            "`UseAdminRights = false`",
            "keep the default `UseAdminRights = true`"
        ];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "each correct form was read in the platform source and is what an agent must write instead of the defect");
        guide.Should().NotContain("WithHints(\"NOLOCK\")",
            because: "the hint is Hints.NoLock; a string hint does not exist in the platform API");
        guide.Should().NotContain("resolved through DI",
            because: "UserConnection has no DI registration; a web service takes it from BaseService");
    }

    [Test]
    [Description("The NoLock check is advice for MS SQL only, not a defect on every database.")]
    public void Guide_ShouldReportNoLockAsAdviceForMsSqlOnly()
    {
        // Arrange
        string[] requiredFacts =
        [
            "Check 2 is advice, not a defect",
            "only for code that runs on MS SQL",
            "MS SQL only: PostgreSQL and Oracle render no hint"
        ];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "only the MS SQL engine renders the hint, and it trades locks for dirty reads, so a missing hint is not a defect");
    }

    [Test]
    [Description("Each guide where C# is written points at the checks in one short sentence that names when to read them.")]
    public void EveryCSharpGuide_ShouldPointAtTheChecksInOneShortSentence()
    {
        // Arrange
        string token = $"`{ItemId}`";
        List<string> problems = [];

        // Act
        foreach (string path in PointerGuides)
        {
            string[] pointerLines = [.. Read(path).Split('\n').Where(line => line.Contains(token, StringComparison.Ordinal))];
            if (pointerLines.Length != 1)
            {
                problems.Add($"{path}: {pointerLines.Length} pointer lines");
                continue;
            }
            string line = pointerLines[0].TrimEnd('\r');
            if (!line.Contains("C#", StringComparison.Ordinal))
            {
                problems.Add($"{path}: the pointer does not say it is for C# work");
            }
            int bytes = Encoding.UTF8.GetByteCount(line);
            if (bytes > MaxPointerLineBytes)
            {
                problems.Add($"{path}: the pointer line is {bytes} bytes");
            }
        }

        // Assert
        problems.Should().BeEmpty(
            because: "the guide is reached only through these pointers, and each must be a single short sentence that "
                + "makes the agent fetch the checks when it writes or reviews C#, not on every read of the guide");
    }

    [Test]
    [Description("No guide that every task reads names the checks, so they are not fetched just in case.")]
    public void TheChecks_ShouldNotBeNamedByAGuideEveryTaskReads()
    {
        // Arrange
        string[] everyTaskGuides =
        [
            "guidance/mcp/guides/routing.md",
            "guidance/mcp/guides/core-rules.md",
            "guidance/mcp/guides/core-rules-details.md"
        ];

        // Act
        string[] naming = [.. everyTaskGuides.Where(path => Read(path).Contains(ItemId, StringComparison.Ordinal))];

        // Assert
        naming.Should().BeEmpty(
            because: "the checks are read on need, through the C# guides; a routing or core-rules pointer would make "
                + "every agent fetch them on every task");
    }

    [Test]
    [Description("The owners the guide cites instead of restating their rules are declared guidance articles.")]
    public void Guide_ShouldCiteOnlyDeclaredOwners()
    {
        // Arrange
        string[] owners =
        [
            "websocket-messaging",
            "configuration-webservice",
            "sys-settings",
            "sys-setting",
            "record-permission-extensions",
            "assembly-package",
            "server-to-server-oauth",
            "esq-filters-backend"
        ];
        using JsonDocument manifest = JsonDocument.Parse(Read("bundle-source.json"));
        HashSet<string> guidance = [.. manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Where(item => item.GetProperty("role").GetString() == "guidance")
            .Select(item => item.GetProperty("itemId").GetString()!)];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(owners.Select(owner => $"`{owner}`"),
            because: "the guide hands the surrounding patterns to their owners instead of restating them");
        owners.Should().OnlyContain(owner => guidance.Contains(owner),
            because: "a backticked guide name is a get-guidance topic to fetch, so every cited owner must be declared");
    }

    [Test]
    [Description("Pins the csharp-platform-checks resource identity in the manifest and the catalog line agents see.")]
    public void Guide_ShouldKeepItsPublishedResourceIdentity()
    {
        // Arrange
        using JsonDocument manifest = JsonDocument.Parse(Read("bundle-source.json"));

        // Act
        JsonElement resource = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId);
        JsonElement requirements = manifest.RootElement.GetProperty("requirements");
        string[] requiredItemIds = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString()!)];
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(item => item.GetString()!)];
        string description = resource.GetProperty("description").GetString()!;

        // Assert
        resource.GetProperty("uri").GetString().Should().Be($"docs://knowledge/com.creatio.clio/{ItemId}",
            because: "clio resolves the guide by this URI");
        resource.GetProperty("sourcePath").GetString().Should().Be("guidance/mcp/guides/backend/csharp-platform-checks.md",
            because: "the manifest must publish the reviewed file, outside the processes folder that routing must index");
        resource.GetProperty("role").GetString().Should().Be("guidance",
            because: "get-guidance resolves a bare name only for guidance items");
        requiredItemIds.Should().Contain(ItemId,
            because: "a required item is part of every published generation");
        requiredUris.Should().Contain($"docs://knowledge/com.creatio.clio/{ItemId}",
            because: "the required URI list must match the required items");
        description[..40].Should().Contain("C#",
            because: "the get-guidance name=list catalog shows about the first 40 characters of the first sentence, "
                + "and they must tell the agent the guide is for C# work");
    }
}

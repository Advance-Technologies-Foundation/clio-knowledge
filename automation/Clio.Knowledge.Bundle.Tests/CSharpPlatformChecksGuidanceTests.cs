using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the csharp-platform-checks guide: the seven Creatio-specific checks for C# an agent writes or reviews, in
/// the form read in platform source, and the way agents reach it. It is reached only through a one-sentence
/// pointer in the guides listed in <see cref="PointerGuides"/>, never through routing or another guide every task
/// reads, so an agent fetches it when it is about to write or review C# and not just in case.
/// </summary>
[TestFixture]
public sealed class CSharpPlatformChecksGuidanceTests
{
    private const string ItemId = "csharp-platform-checks";

    /// <summary>The guides chosen to carry the pointer, among those where C# is or may be written.</summary>
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
            "joins the table of every lookup for its display value",
            "`AddColumn(\"<column path>\")` for each column the code reads",
            "`Save()` throws `RequiredColumnsEmptyValuesException` for a required column that was not loaded",
            "`StartTransaction()` with no `RollbackTransaction()` in a catch",
            "`using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())`",
            "`RollbackTransaction()` in the catch, then rethrow",
            "inside a transaction an outer level started, both calls do nothing",
            "Only the dispose that closes the outermost level rolls back a transaction still open",
            "`MsgChannelUtilities.PostMessageToAll(`",
            "`PostToAll(` on a `MsgChannelManager` or `IMsgChannelManager`",
            "`MsgChannelUtilities.PostMessage(UserConnection, ...)` posts to the current user only and is not a broadcast",
            "behind an operation-permission check",
            "inherits `BaseService` and uses its `UserConnection`",
            "uses the `UserConnection` passed to `Execute`",
            "inject a `Func<UserConnection>` accessor, never the connection itself",
            "a `SecureText` system setting (`sys-settings`) read through `SysSettings` (`sys-setting`)",
            "`Supervisor` / `Supervisor`",
            "`localhost` is no exception",
            "`UseAdminRights = false`",
            "a raw `Select`, `Update` or `Delete` that does not apply the caller's rights itself",
            "`SystemUserConnection` in place of the caller's connection",
            "the default `UseAdminRights = true`, which applies the user's record rights",
            "A raw query that enforces the caller's rights itself and fails closed (`virtual-entities`)",
            "a `Select` inside the condition a record-permission extension returns (`record-permission-extensions`) are not hits"
        ];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "each fact was read in the platform source, the named package or an owner guide, and each correct "
                + "form is what an agent must write instead of the defect");
        guide.Should().NotContain("resolved through DI",
            because: "a UserConnection is never resolved from the container; DI code takes a Func<UserConnection> accessor");
        guide.Should().NotContain("There is no DI registration",
            because: "configuration-webservice registers a Func<UserConnection> accessor, so the guide must not deny it");
        guide.Should().NotContain("PostMessage(UserConnection, sender, text)",
            because: "a Freedom UI page drops a message body that is not JSON, so plain text is not a correct form");
        guide.Should().NotContain("every host beyond",
            because: "a default credential is a defect on localhost too: shipped code runs on the customer's server");
    }

    [Test]
    [Description("Read locks are not a check: the platform already reads with NOLOCK on MS SQL, so the guide asks for no hint.")]
    public void Guide_ShouldNotAskForANoLockHint()
    {
        // Arrange
        string[] requiredFacts =
        [
            "Read locks are not a check",
            "already read with `NOLOCK` by default",
            "Do not add `Hints.NoLock` and do not report its absence"
        ];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "Select and EntitySchemaQuery default CanReadUncommitedData to true, and Select renders NOLOCK on MS SQL from it");
        guide.Should().NotContain("WithHints(",
            because: "a hint the platform already renders changes nothing, so the guide must not ask for one");
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
            "virtual-entities",
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

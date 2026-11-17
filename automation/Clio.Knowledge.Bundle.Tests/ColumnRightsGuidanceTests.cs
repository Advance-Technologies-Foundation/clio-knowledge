using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the column-rights rules in ONE place. They are safety-relevant: a save of column permissions replaces the
/// rows of every column of the object, the service read hides rows, a disable lifts every column restriction at once,
/// removing a row can widen access, and the rows do not ship with a package. An edit that drops one of these rules
/// would let an agent erase or widen column access without the developer's decision.
/// </summary>
[TestFixture]
public sealed class ColumnRightsGuidanceTests
{
    private const string GuidePath = "guidance/mcp/guides/operations/column-rights.md";

    private static readonly Regex VersionBoundary = new(
        @"Version boundary: get-column-rights and set-column-rights require (clio \d+(\.\d+){2,3} or later|a clio release newer than \d+(\.\d+){2,3})[;.]",
        RegexOptions.Compiled);

    private static readonly Regex CitedGuide = new(@"get-guidance name=([a-z0-9][a-z0-9.-]*[a-z0-9])",
        RegexOptions.Compiled);

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    // Line breaks and indentation are layout, not content: a rule wrapped across lines is still the rule.
    private static string Normalized(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(Root(), relativePath));

    [TestCase("A save there REPLACES the rows of EVERY column of the object",
        TestName = "ColumnRights_ShouldStateThatASaveReplacesEveryColumn")]
    [TestCase("You MUST NOT write column rights with execute-esq, DataService or a hand-built call to RightManagementService",
        TestName = "ColumnRights_ShouldForbidWritingByHand")]
    [TestCase("The service read also FILTERS",
        TestName = "ColumnRights_ShouldStateThatTheServiceReadFilters")]
    [TestCase("A column with no rows is NOT locked",
        TestName = "ColumnRights_ShouldStateThatNoRowsIsNotALock")]
    [TestCase("the higher the row, the higher its priority, and a user in several roles gets the access of the HIGHEST of their rows",
        TestName = "ColumnRights_ShouldStateThePriorityRule")]
    [TestCase("Object operation and record permissions outrank column permissions",
        TestName = "ColumnRights_ShouldStateThePrecedenceOfObjectAndRecordPermissions")]
    [TestCase("External / portal users are NOT, so an All employees row does not restrict them",
        TestName = "ColumnRights_ShouldStateThatAllEmployeesDoesNotCoverPortalUsers")]
    [TestCase("Documented, not measured",
        TestName = "ColumnRights_ShouldLabelTheDocumentedRulesAsNotMeasured")]
    [TestCase("You MUST ask the developer in chat before every write",
        TestName = "ColumnRights_ShouldRequireAskingBeforeEveryWrite")]
    [TestCase("So you MUST first call it with preview=true",
        TestName = "ColumnRights_ShouldPreviewBeforeEveryWrite")]
    [TestCase("You MUST name the object by schema name AND title",
        TestName = "ColumnRights_ShouldNameTheObjectBySchemaNameAndTitle")]
    [TestCase("When several roles or users match a name, you MUST NOT pick one",
        TestName = "ColumnRights_ShouldAskWhenSeveralGranteesMatch")]
    [TestCase("Calls for one object MUST NOT run in parallel",
        TestName = "ColumnRights_ShouldForbidParallelWritesOfOneObject")]
    [TestCase("You MUST NOT remove a row when the developer asked to take access away",
        TestName = "ColumnRights_ShouldNotRemoveARowToTakeAccessAway")]
    [TestCase("which can WIDEN their access",
        TestName = "ColumnRights_ShouldStateThatARemovalCanWiden")]
    [TestCase("When an All employees row ends ABOVE a row with another level, the preview and the result warn",
        TestName = "ColumnRights_ShouldStateTheShadowingWarning")]
    [TestCase("Before an enable you MUST name to the developer the rows that start to apply",
        TestName = "ColumnRights_ShouldRequireStatingTheEnableEffect")]
    [TestCase("You MUST NOT disable unless that widening is the developer's intent",
        TestName = "ColumnRights_ShouldGuardTheDisable")]
    [TestCase("read them with get-column-rights first (the disable preview gives only their number)",
        TestName = "ColumnRights_ShouldReadTheColumnsBeforeADisable")]
    [TestCase("never take that for OFF",
        TestName = "ColumnRights_ShouldNotTakeAnUnknownSwitchForOff")]
    [TestCase("do not plan a write from it",
        TestName = "ColumnRights_ShouldNotPlanFromTheFilteredView")]
    [TestCase("Role membership is NOT resolved",
        TestName = "ColumnRights_ShouldStateThatTheGranteeFilterIsExact")]
    [TestCase("re-read with get-column-rights before retrying or reporting",
        TestName = "ColumnRights_ShouldReReadAfterAFailedOrUnverifiedSave")]
    [TestCase("You MUST tell the developer that the column rights do not ship with the app",
        TestName = "ColumnRights_ShouldStateThatTheRowsAreNotPackaged")]
    [TestCase("test AS that user — a non-administrator in a session opened after the write",
        TestName = "ColumnRights_ShouldVerifyAsTheUser")]
    [Description("column-rights keeps each rule an agent must weigh before changing column permissions; losing one in an edit would let a call erase, widen or narrow column access without the developer's decision.")]
    public void ColumnRights_ShouldStateTheContractRule(string rule)
    {
        // Act
        string normalized = Normalized(Read(GuidePath));

        // Assert
        normalized.Should().Contain(rule, because: "the owning article must keep every rule of the tool contract");
    }

    [Test]
    [Description("column-rights declares its clio version boundary as a plain-text line naming both tools before its body, and holds no HTML comment: it is served as text/plain.")]
    public void ColumnRights_ShouldDeclareItsClioVersionBoundaryFirst()
    {
        // Arrange
        string guide = Normalized(Read(GuidePath));

        // Act
        Match boundary = VersionBoundary.Match(guide);

        // Assert
        boundary.Success.Should().BeTrue(because: "an agent on an older clio must learn that the tools do not exist");
        boundary.Index.Should().BeLessThan(guide.IndexOf("## How the platform decides", StringComparison.Ordinal),
            because: "the boundary gates the whole article, so it comes before the rules");
        guide.Should().NotContain("<!--", because: "a text/plain article delivers an HTML comment verbatim");
    }

    [Test]
    [Description("The catalog description of column-rights starts with its clio boundary, so get-guidance name=list shows it before the article is opened.")]
    public void ColumnRights_CatalogDescriptionShouldStartWithTheBoundary()
    {
        // Arrange
        using JsonDocument manifest = JsonDocument.Parse(Read("bundle-source.json"));

        // Act
        string description = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Single(resource => resource.GetProperty("itemId").GetString() == "column-rights")
            .GetProperty("description").GetString()!;

        // Assert
        description.Should().StartWith("Needs a clio release newer than 8.1.0.155.",
            because: "a knowledge library is updated apart from the clio that serves it");
    }

    [Test]
    [Description("The article is published, routed, reached from object-rights and default-record-rights, and every guide it cites is declared.")]
    public void ColumnRights_ShouldBePublishedRoutedAndCiteOnlyDeclaredGuides()
    {
        // Arrange
        using JsonDocument manifest = JsonDocument.Parse(Read("bundle-source.json"));
        HashSet<string> declared = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Select(resource => resource.GetProperty("itemId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        string guide = Read(GuidePath);

        // Act
        string[] undeclared = CitedGuide.Matches(guide).Select(match => match.Groups[1].Value)
            .Where(name => !declared.Contains(name)).Distinct().ToArray();

        // Assert
        declared.Should().Contain("column-rights", because: "the article must be deliverable");
        undeclared.Should().BeEmpty(because: "a guide the bundle does not declare is not published");
        Read("guidance/mcp/guides/routing.md").Should().Contain("name=column-rights",
            because: "a request to restrict a field must reach the owner");
        Normalized(Read("guidance/mcp/guides/operations/object-rights.md")).Should().Contain(
            "NOT column permissions (`get-guidance name=column-rights`)",
            because: "object operation permissions and column permissions are different tools");
        Normalized(Read("guidance/mcp/guides/operations/default-record-rights.md")).Should().Contain(
            "NOT column permissions (`get-guidance name=column-rights`)",
            because: "the record layer and the column layer are different tools");
    }

    [TestCase("guidance/mcp/guides/operations/object-rights.md",
        TestName = "ObjectRights_ShouldStateTheSharedWriteLease")]
    [TestCase("guidance/mcp/guides/operations/default-record-rights.md",
        TestName = "DefaultRecordRights_ShouldStateTheSharedWriteLease")]
    [Description("From a clio release newer than 8.1.0.155 the three rights writers share one lease per object and refuse a save when the object changed after the plan; each article tells the agent not to run them in parallel.")]
    public void RightsWriters_ShouldStateTheSharedWriteLease(string path)
    {
        // Act
        string normalized = Normalized(Read(path));

        // Assert
        normalized.Should().Contain("a clio release newer than 8.1.0.155",
            because: "the lease exists only from that clio on");
        normalized.Should().Contain("Calls for one object MUST NOT run in parallel",
            because: "a second write of the object is refused while the first holds the lease");
    }
}

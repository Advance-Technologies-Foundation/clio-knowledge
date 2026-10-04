using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the default-record-rights rules in ONE place. They are safety-relevant: the record-permissions switch changes
/// who reaches every record, ON with no rule leaves every user with only their own records, existing records get no
/// rights until the rules are applied, and applying them is a heavy run the agent must never start on its own. An edit
/// that drops one of these rules would let an agent widen or narrow access silently.
/// </summary>
[TestFixture]
public sealed class DefaultRecordRightsGuidanceTests
{
    private const string GuidePath = "guidance/mcp/guides/operations/default-record-rights.md";

    private static readonly Regex VersionBoundary = new(
        @"Version boundary: set-default-record-rights and apply-default-record-rights require clio (<[A-Z][A-Z0-9-]*TBD>|\d+(\.\d+){2,3}) or later",
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

    [TestCase("You MUST NOT run apply-default-record-rights on your own initiative",
        TestName = "DefaultRecordRights_ShouldForbidApplyingOnTheAgentsOwnInitiative")]
    [TestCase("ASK whether and when to run it", TestName = "DefaultRecordRights_ShouldRequireAskingBeforeApply")]
    [TestCase("every user sees only the records they create",
        TestName = "DefaultRecordRights_ShouldStateTheOwnRecordsDefault")]
    [TestCase("Turning the switch on or changing a rule does NOT touch existing records",
        TestName = "DefaultRecordRights_ShouldStateThatExistingRecordsAreNotTouched")]
    [TestCase("Before an enable you MUST tell the developer what the object becomes",
        TestName = "DefaultRecordRights_ShouldRequireStatingTheEnableEffect")]
    [TestCase("You MUST ask the developer in chat before every write",
        TestName = "DefaultRecordRights_ShouldRequireAskingBeforeEveryWrite")]
    [TestCase("The platform replaces the whole rule list on every save and checks nothing",
        TestName = "DefaultRecordRights_ShouldStateTheFullListSave")]
    [TestCase("A revoke is allowed while the switch is OFF",
        TestName = "DefaultRecordRights_ShouldAllowTheCleanUpRevokeWhileOff")]
    [TestCase("A call is EITHER a rule change",
        TestName = "DefaultRecordRights_ShouldStateTheTwoCallShapes")]
    [TestCase("do NOT start it again", TestName = "DefaultRecordRights_ShouldForbidRestartingARunStillGoing")]
    [TestCase("rights granted by hand with set-record-rights stay",
        TestName = "DefaultRecordRights_ShouldStateThatManualGrantsSurviveTheUpdate")]
    [TestCase("You MUST NOT pass it unless that widening is the developer's intent",
        TestName = "DefaultRecordRights_ShouldGuardTheDisable")]
    [Description("default-record-rights keeps each rule an agent must weigh before changing the record layer; losing one in an edit would let a call widen or narrow access, or start a heavy run, without the developer's decision.")]
    public void DefaultRecordRights_ShouldStateTheContractRule(string rule)
    {
        // Act
        string normalized = Normalized(Read(GuidePath));

        // Assert
        normalized.Should().Contain(rule, because: "the owning article must keep every rule of the tool contract");
    }

    [Test]
    [Description("default-record-rights declares its clio version boundary as a plain-text line naming both tools, and holds no HTML comment: it is served as text/plain.")]
    public void DefaultRecordRights_ShouldDeclareItsClioVersionBoundary()
    {
        // Arrange
        string guide = Read(GuidePath);

        // Act
        bool declared = VersionBoundary.IsMatch(Normalized(guide));

        // Assert
        declared.Should().BeTrue(because: "an agent on an older clio must learn that the tools do not exist");
        guide.Should().NotContain("<!--", because: "a text/plain article delivers an HTML comment verbatim");
    }

    [Test]
    [Description("The article is published, routed, and reached from the record-permissions entry point, record-rights and object-rights, and every guide it cites is declared.")]
    public void DefaultRecordRights_ShouldBePublishedRoutedAndCiteOnlyDeclaredGuides()
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
        declared.Should().Contain("default-record-rights", because: "the article must be deliverable");
        undeclared.Should().BeEmpty(because: "a guide the bundle does not declare is not published");
        Read("guidance/mcp/guides/routing.md").Should().Contain("name=default-record-rights",
            because: "a request to turn record permissions on must reach the owner");
        Read("guidance/mcp/guides/operations/record-permissions.md").Should().Contain("`default-record-rights`",
            because: "the decision entry point routes the object's default rules to their owner");
        Read("guidance/mcp/guides/operations/record-rights.md").Should().Contain("name=default-record-rights",
            because: "per-record grants and default rules are different tools");
        Read("guidance/mcp/guides/operations/object-rights.md").Should().Contain("name=default-record-rights",
            because: "get-object-rights reports the record layer, owned there");
    }
}

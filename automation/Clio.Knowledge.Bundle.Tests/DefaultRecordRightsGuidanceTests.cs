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
    [TestCase("ON with NO rule: every user sees only the records they create",
        TestName = "DefaultRecordRights_ShouldStateTheOwnRecordsDefault")]
    [TestCase("with no rule, every user sees only the records they create",
        TestName = "DefaultRecordRights_ShouldStateTheOwnRecordsDefaultBeforeAnEnable")]
    [TestCase("after a re-enable they keep the rights they had, and records created while the switch was OFF have none until applied",
        TestName = "DefaultRecordRights_ShouldTellAFirstEnableFromAReEnable")]
    [TestCase("The author/owner of a record always reaches it",
        TestName = "DefaultRecordRights_ShouldStateThatTheAuthorAlwaysReachesTheRecord")]
    [TestCase("Turning the switch OFF keeps the rules and every record's rights",
        TestName = "DefaultRecordRights_ShouldStateThatTheDisableKeepsRulesAndRights")]
    [TestCase("A record created while it is OFF gets no record rights",
        TestName = "DefaultRecordRights_ShouldStateThatRecordsCreatedWhileOffGetNoRights")]
    [TestCase("While the switch is OFF the rules are listed as stored, not in effect",
        TestName = "DefaultRecordRights_ShouldListTheRulesAsStoredWhileOff")]
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
    [TestCase("optionally with enable-record-permissions, never with disable-record-permissions",
        TestName = "DefaultRecordRights_ShouldAllowTheEnableWithAGrant")]
    [TestCase("The first enable and its first rule SHOULD go in one call",
        TestName = "DefaultRecordRights_ShouldKeepTheFirstEnableAndRuleTogether")]
    [TestCase("you MUST name the object by schema name AND title",
        TestName = "DefaultRecordRights_ShouldNameTheObjectBySchemaNameAndTitle")]
    [TestCase("you MUST ask which object is meant before reading or writing",
        TestName = "DefaultRecordRights_ShouldAskWhenTheWordIsNotTheTitle")]
    [TestCase("leaves the other two operations unchanged (as stored)",
        TestName = "DefaultRecordRights_ShouldKeepTheOtherOperationsOfAGrant")]
    [TestCase("you MUST say in the confirmation who gains or loses what, by direction",
        TestName = "DefaultRecordRights_ShouldStateTheExternalExposure")]
    [TestCase("a portal GRANTEE — portal / external users get (a grant) or stop getting (a revoke) the rule's rights on NEW records",
        TestName = "DefaultRecordRights_ShouldStateThePortalGranteeEffect")]
    [TestCase("a portal AUTHOR — the grantee gets or stops getting them on NEW records created by portal users",
        TestName = "DefaultRecordRights_ShouldStateThePortalAuthorEffect")]
    [TestCase("after a revoke they KEEP the right until apply — say so, never \"they lose access\"",
        TestName = "DefaultRecordRights_ShouldNotClaimARevokeClosesExistingRecords")]
    [TestCase("as the AUTHOR it gives the grantee rights on records created by portal users, and portal users get nothing from it",
        TestName = "DefaultRecordRights_ShouldStateTheRuleDirectionForPortalRoles")]
    [TestCase("`All external users` (`720b771c-e7a7-4f31-9cfb-52cd21c3739f`) or a portal role",
        TestName = "DefaultRecordRights_ShouldStateHowPortalUsersReachARecord")]
    [TestCase("the refusal names the stored rules an enable would bring into effect",
        TestName = "DefaultRecordRights_ShouldShowTheStoredRulesAfterAGrantOnOffRefusal")]
    [TestCase("Refused, writing nothing: a grant on an object whose switch is OFF without enable-record-permissions",
        TestName = "DefaultRecordRights_ShouldRefuseAGrantWhileOffWithoutTheEnable")]
    [TestCase("revoke together with either switch flag",
        TestName = "DefaultRecordRights_ShouldRefuseARevokeWithASwitchFlag")]
    [TestCase("so is a level other than granted / delegated",
        TestName = "DefaultRecordRights_ShouldRefuseAnUnsupportedLevelBeforeAnyRead")]
    [TestCase("refuses a stored list with duplicate pairs or invalid levels",
        TestName = "DefaultRecordRights_ShouldRefuseABrokenStoredList")]
    [TestCase("A rule left with no right is REMOVED",
        TestName = "DefaultRecordRights_ShouldStateThatAnEmptyRuleIsRemoved")]
    [TestCase("\"saved, but NOT verified\"",
        TestName = "DefaultRecordRights_ShouldFailAnUnverifiedSave")]
    [TestCase("call it with preview=true first",
        TestName = "DefaultRecordRights_ShouldPreviewBeforeEveryWrite")]
    [TestCase("re-read before retrying", TestName = "DefaultRecordRights_ShouldReReadAfterAnUnansweredSave")]
    [TestCase("After an enable or a rule change that leaves the switch ON",
        TestName = "DefaultRecordRights_ShouldOfferApplyOnlyWhenTheSwitchIsOn")]
    [TestCase("While the switch is OFF there is nothing to apply: apply is refused then",
        TestName = "DefaultRecordRights_ShouldStateThatApplyIsRefusedWhileOff")]
    [TestCase("Make all the rule changes first, then run it once",
        TestName = "DefaultRecordRights_ShouldRunApplyOnce")]
    [TestCase("A new launch is safe only after \"not started\" or a run that ended",
        TestName = "DefaultRecordRights_ShouldNameWhenANewLaunchIsSafe")]
    [TestCase("you MUST NOT start it again", TestName = "DefaultRecordRights_ShouldForbidRestartingARunStillGoing")]
    [TestCase("the launch got no usable answer (success=false, \"got no usable answer\") — the run MAY already be going",
        TestName = "DefaultRecordRights_ShouldTreatAnUnansweredLaunchAsMaybeGoing")]
    [TestCase("rights granted by hand with set-record-rights stay",
        TestName = "DefaultRecordRights_ShouldStateThatManualGrantsSurviveTheUpdate")]
    [TestCase("You MUST NOT pass it unless that widening is the developer's intent",
        TestName = "DefaultRecordRights_ShouldGuardTheDisable")]
    [TestCase("before it you MUST tell the developer that every user with read on the object — portal / external users who hold it included — will reach every record",
        TestName = "DefaultRecordRights_ShouldRequireStatingTheDisableEffect")]
    [TestCase("name each portal / external rule that will now reach existing records and each access that goes away",
        TestName = "DefaultRecordRights_ShouldStateWhatApplyChangesBeforeAsking")]
    [TestCase("ended as Error or Canceled",
        TestName = "DefaultRecordRights_ShouldTreatACanceledRunAsEnded")]
    [TestCase("A success=false result that matches none of these texts counts as \"MAY already be going\"",
        TestName = "DefaultRecordRights_ShouldTreatAnUnrecognizedFailureAsMaybeGoing")]
    [TestCase("On MCP the wait defaults to 60 s (timeout-seconds)",
        TestName = "DefaultRecordRights_ShouldStateTheMcpWaitDefault")]
    [TestCase("disable-record-permissions is a call of its own",
        TestName = "DefaultRecordRights_ShouldKeepTheDisableACallOfItsOwn")]
    [TestCase("a revoke never changes the switch",
        TestName = "DefaultRecordRights_ShouldKeepTheSwitchOutOfARevoke")]
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
    [Description("The article is published, routed, and reached from the record-permissions entry point (its decision-table row), record-rights, object-rights and processes/access-rights, and every guide it cites is declared.")]
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
        Normalized(Read("guidance/mcp/guides/operations/record-permissions.md")).Should().Contain(
            "| Records created by role X are available to role Y by default; turn record permissions on/off for an "
            + "object; apply the rules to existing records | Default record rules of the object | `default-record-rights` |",
            because: "the decision table routes the object's default rules to their owner");
        Read("guidance/mcp/guides/operations/record-rights.md").Should().Contain("name=default-record-rights",
            because: "per-record grants and default rules are different tools");
        Normalized(Read("guidance/mcp/guides/operations/object-rights.md")).Should().Contain(
            "`get-guidance name=default-record-rights`; from the clio version named there, get-object-rights reports them",
            because: "get-object-rights reports the record layer, owned there, only from that clio version");
        Normalized(Read("guidance/mcp/guides/processes/access-rights.md")).Should().Contain(
            "turning them on is `get-guidance name=default-record-rights`: ask the developer first",
            because: "a process build that needs record permissions must reach the owner with its guard");
    }
}

using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps portal-sections an orchestration card: it routes each step to its owner (object-rights,
/// default-record-rights) and states only what the portal case adds, plus the preview-and-ask MUST that guards
/// the destructive set-object-rights write. The owners' mechanics are pinned in the owners' own tests; restating
/// them here is how the old object step drifted onto arguments the tool now refuses.
/// </summary>
[TestFixture]
public sealed class PortalSectionsGuidanceTests
{
    private const string PortalSectionsPath = "guidance/mcp/guides/applications/portal-sections.md";

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

    [TestCase("GRANT each approved object in its own call",
        TestName = "PortalSections_ShouldGrantEachApprovedObjectInItsOwnCall")]
    [TestCase("--operations read", TestName = "PortalSections_ShouldNameTheOperations")]
    [TestCase("--enable-operation-permissions", TestName = "PortalSections_ShouldNameTheEnableFlag")]
    [TestCase("ASK THE USER before any write", TestName = "PortalSections_ShouldAskBeforeEachWrite")]
    [TestCase("EVERY external user gets that access, on EVERY record of the object",
        TestName = "PortalSections_ShouldStateWhatTheWholeAudienceGets")]
    [TestCase("the object of each detail on the page, and the lookup object of each column shown",
        TestName = "PortalSections_ShouldListEveryObjectThePortalPagesShow")]
    [TestCase("not only what `--include-connected` reports",
        TestName = "PortalSections_ShouldNotLimitTheListToTheConnectedListing")]
    [TestCase("you MUST preview each call and ASK THE USER before any write",
        TestName = "PortalSections_ShouldPreviewBeforeAsking")]
    [TestCase("owner: `object-rights`. Follow its \"Give a role access to an object and its lookups\" flow",
        TestName = "PortalSections_ShouldRouteObjectAccessToTheObjectRightsFlow")]
    [Description("The object-access step lists, asks, then grants each approved object in its own set-object-rights call, naming the operations and the enable flag.")]
    public void PortalSections_ShouldFollowTheObjectRightsContract(string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), PortalSectionsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "the portal grant follows the set-object-rights contract");
    }

    [TestCase("ALWAYS design a dedicated portal record page", TestName = "PortalSections_ShouldDesignASeparatePortalPage")]
    [TestCase("never reuse the employees' page", TestName = "PortalSections_ShouldNotReuseTheEmployeesPage")]
    [TestCase("ASK THE USER before designing it", TestName = "PortalSections_ShouldAskThePortalPageScope")]
    [TestCase("owner: `default-record-rights`", TestName = "PortalSections_ShouldRouteRecordAccessToDefaultRecordRights")]
    [TestCase("AUTHOR `All external users` (or the portal role) and an internal GRANTEE",
        TestName = "PortalSections_ShouldRequireARuleAuthoredByThePortalAudience")]
    [TestCase("This rule is REQUIRED on every object where portal users create records",
        TestName = "PortalSections_ShouldMakeThePortalToEmployeesRuleRequired")]
    [TestCase("whether portal users see records they did not create is a business decision",
        TestName = "PortalSections_ShouldLeaveThePortalVisibilityOfOthersRecordsToTheUser")]
    [TestCase("employees do not see the records created on the portal",
        TestName = "PortalSections_ShouldNameTheSymptomOfAMissingAuthorRule")]
    [TestCase("A rule reaches NEW records only", TestName = "PortalSections_ShouldStateThatRulesReachNewRecordsOnly")]
    [TestCase("`apply-default-record-rights` runs — the user's decision, never yours",
        TestName = "PortalSections_ShouldLeaveTheApplyToTheUser")]
    [TestCase("need the clio releases named in `object-rights` and `default-record-rights`",
        TestName = "PortalSections_ShouldRouteTheVersionBoundaryToTheOwners")]
    [Description("The record step requires a rule authored by the portal audience so employees reach portal-created records, leaves what portal users see of others' records to the user, and leaves the apply to the user.")]
    public void PortalSections_ShouldCheckRecordAccess(string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), PortalSectionsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "record permissions decide which records portal users reach after the object grant");
    }

    [Test]
    [Description("The object-rights section the object step routes to still exists under the name the step cites.")]
    public void PortalSections_ShouldRouteToAnExistingObjectRightsSection()
    {
        // Arrange
        string owner = File.ReadAllText(Path.Combine(Root(), "guidance/mcp/guides/operations/object-rights.md"));

        // Act
        bool hasSection = owner.Contains("## Give a role access to an object and its lookups", StringComparison.Ordinal);

        // Assert
        hasSection.Should().BeTrue(because: "portal-sections step 4 sends the reader to that flow by name");
    }

    [Test]
    [Description("The '…any data' bypass is cited to object-rights, and object-rights states it.")]
    public void PortalSections_ShouldCiteTheAnyDataBypassToAnOwnerThatStatesIt()
    {
        // Arrange
        string guide = Normalized(File.ReadAllText(Path.Combine(Root(), PortalSectionsPath)));
        string owner = Normalized(File.ReadAllText(Path.Combine(Root(), "guidance/mcp/guides/operations/object-rights.md")));

        // Act
        bool cites = guide.Contains("let internal roles bypass object permissions — see `object-rights`", StringComparison.Ordinal);
        bool states = owner.Contains("Internal roles holding the \"…any data\" system operations reach records whatever the object rows say",
            StringComparison.Ordinal);

        // Assert
        cites.Should().BeTrue(because: "the bypass claim must name the guide that owns it");
        states.Should().BeTrue(because: "the cited owner must state the bypass rule");
    }

    [Test]
    [Description("No line of the article puts include-connected on a set-object-rights call: set changes one object per call and refuses the argument.")]
    public void PortalSections_ShouldNotPassIncludeConnectedToSetObjectRights()
    {
        // Arrange
        string[] lines = File.ReadAllLines(Path.Combine(Root(), PortalSectionsPath));

        // Act
        string[] offending = lines
            .Where(line =>
            {
                int set = line.IndexOf("set-object-rights", StringComparison.OrdinalIgnoreCase);
                return set >= 0 && line.IndexOf("include-connected", set, StringComparison.OrdinalIgnoreCase) > set;
            })
            .Select(line => line.Trim())
            .ToArray();

        // Assert
        offending.Should().BeEmpty(because: "include-connected is an argument of get-object-rights, never of set-object-rights");
    }

    [TestCase("confirmation-code")]
    [TestCase("connected-operations")]
    [TestCase("allow-security-object")]
    [Description("The object-access step names no set-object-rights argument the tool has retired: an unknown argument is refused before any read or write.")]
    public void PortalSections_ShouldNotNameARetiredSetObjectRightsArgument(string retired)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), PortalSectionsPath));

        // Act
        bool names = guide.Contains(retired, StringComparison.OrdinalIgnoreCase);

        // Assert
        names.Should().BeFalse(because: $"'{retired}' is no longer a set-object-rights argument");
    }

    [TestCase(PortalSectionsPath, "`workplace-type=portal`, clio 8.1.0.142 and later",
        TestName = "PortalSections_ShouldPassThePortalWorkplaceType")]
    [TestCase(PortalSectionsPath, "An existing workplace of another type is refused, never retyped",
        TestName = "PortalSections_ShouldStateThatAnotherTypeIsRefused")]
    [TestCase("guidance/mcp/guides/applications/workplaces.md", "pass `workplace-type=portal` to `place-section-in-workplace` (clio 8.1.0.142 and later)",
        TestName = "Workplaces_ShouldNameTheWorkplaceTypeArgumentAndItsVersion")]
    [TestCase("guidance/mcp/guides/applications/workplaces.md", "the tool never retypes a workplace",
        TestName = "Workplaces_ShouldStateThatTheToolNeverRetypes")]
    [Description("The portal placement names the workplace-type argument with its first clio version, and states that an existing workplace of another type is refused, never retyped.")]
    public void PortalPlacement_ShouldNameTheWorkplaceType(string path, string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), path));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "a portal section needs a Portal workplace, set by the tool, not by hand");
    }
}

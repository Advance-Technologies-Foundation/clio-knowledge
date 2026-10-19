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
    [TestCase("operations=read preview=true", TestName = "PortalSections_ShouldPreviewTheGrantWithItsOperations")]
    [TestCase("The preview MUST carry exactly the arguments of the intended write, and the write is the previewed call minus `preview`, nothing else",
        TestName = "PortalSections_ShouldWriteExactlyThePreviewedCall")]
    [TestCase("operations=read enable-operation-permissions preview=true",
        TestName = "PortalSections_ShouldPreviewTheEnablingGrantWithItsFlag")]
    [TestCase("If a write is refused, or any argument changes after the user's yes, preview again and ask again",
        TestName = "PortalSections_ShouldPreviewAgainWhenTheWriteChanges")]
    [TestCase("grantee=720b771c-e7a7-4f31-9cfb-52cd21c3739f operations=read",
        TestName = "PortalSections_ShouldGrantTheAllExternalUsersRole")]
    [TestCase("needs `enable-operation-permissions`, which turns operation permissions ON for EVERY role",
        TestName = "PortalSections_ShouldStateWhatTheEnableFlagChanges")]
    [TestCase("Before that write you MUST name to the user, from the preview, each stored row of an external role that starts to decide",
        TestName = "PortalSections_ShouldNameStoredExternalRowsBeforeAnEnable")]
    [TestCase("internal users then keep only what the rows give them",
        TestName = "PortalSections_ShouldStateThatAnEnableNarrowsInternalAccess")]
    [TestCase("ASK THE USER before any write", TestName = "PortalSections_ShouldAskBeforeEachWrite")]
    [TestCase("EVERY external user gets that access, on EVERY record and EVERY column of the object, whatever the portal page shows",
        TestName = "PortalSections_ShouldStateWhatTheWholeAudienceGets")]
    [TestCase("the object of each detail on the page, and the lookup object of each column shown",
        TestName = "PortalSections_ShouldListEveryObjectThePortalPagesShow")]
    [TestCase("not only what `include-connected` reports",
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
    [TestCase("Keeping it OFF is the user's decision, never yours: ASK THE USER, naming that consequence",
        TestName = "PortalSections_ShouldLeaveRecordPermissionsOffToTheUser")]
    [TestCase("the record tools need a clio newer than 8.1.0.140",
        TestName = "PortalSections_ShouldNameTheRecordStepVersionBoundary")]
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

    [TestCase("The page is a LAYOUT, not an access boundary", TestName = "PortalSections_ShouldNotPresentThePageAsAnAccessBoundary")]
    [TestCase("Hiding a field from them needs column permissions, which this flow does not set",
        TestName = "PortalSections_ShouldStateThatColumnsNeedColumnPermissions")]
    [TestCase("The binding is per OBJECT and audience, not per section",
        TestName = "PortalSections_ShouldStateThatThePortalBindingIsPerObject")]
    [TestCase("reuse it, or ASK THE USER before replacing it", TestName = "PortalSections_ShouldNotReplaceAnExistingPortalPageSilently")]
    [TestCase("must NOT be written for a Freedom portal section", TestName = "PortalSections_ShouldNotWriteTheClassicSspTables")]
    [TestCase("`place-section-in-workplace` needs clio 8.1.0.141 and later",
        TestName = "PortalSections_ShouldNameTheWorkplaceStepVersionBoundary")]
    [TestCase("`set-object-rights` / `get-object-rights` need a clio newer than 8.1.0.137",
        TestName = "PortalSections_ShouldNameTheObjectStepVersionBoundary")]
    [Description("The page, binding and placement steps state what the portal case adds: the page is no access boundary, the binding is per object, the Classic SSP tables stay untouched, and each step names its clio version.")]
    public void PortalSections_ShouldStateThePortalScopeOfEachStep(string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), PortalSectionsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "the portal case adds this rule to the step its owner describes");
    }

    [TestCase("guidance/mcp/guides/related-data/page-binding.md", "--portal-default-page",
        TestName = "PortalSections_OwnerShouldStillNameThePortalDefaultPageFlag")]
    [TestCase("guidance/mcp/guides/related-data/page-binding.md", "--portal-add-page",
        TestName = "PortalSections_OwnerShouldStillNameThePortalAddPageFlag")]
    [TestCase("guidance/mcp/guides/applications/workplaces.md", "`place-section-in-workplace` through `clio-run` (after `get-tool-contract`; clio 8.1.0.141 and later)",
        TestName = "PortalSections_OwnerShouldStillStateTheWorkplaceVersion")]
    [TestCase("guidance/mcp/guides/operations/object-rights.md", "get-object-rights and set-object-rights require a clio release newer than 8.1.0.137",
        TestName = "PortalSections_OwnerShouldStillStateTheObjectRightsVersion")]
    [TestCase("guidance/mcp/guides/operations/default-record-rights.md", "require a clio release newer than 8.1.0.140",
        TestName = "PortalSections_OwnerShouldStillStateTheRecordRightsVersion")]
    [Description("Every owner fact portal-sections restates (flags, tool names, versions) is still stated by its owner, so a change there fails the build instead of leaving portal-sections stale.")]
    public void PortalSections_RestatedOwnerFactShouldStillHoldInItsOwner(string ownerPath, string fact)
    {
        // Arrange
        string owner = File.ReadAllText(Path.Combine(Root(), ownerPath));

        // Act
        string normalized = Normalized(owner);

        // Assert
        normalized.Should().Contain(fact, because: "portal-sections restates this fact from its owner");
    }

    [Test]
    [Description("The object-rights section the object step routes to still exists, and the owner still states the enable precondition and the call form the step restates.")]
    public void PortalSections_ShouldRouteToAnExistingObjectRightsSection()
    {
        // Arrange
        string owner = File.ReadAllText(Path.Combine(Root(), "guidance/mcp/guides/operations/object-rights.md"));

        // Act
        string normalizedOwner = Normalized(owner);
        bool hasSection = owner.Contains("## Give a role access to an object and its lookups", StringComparison.Ordinal);
        bool hasEnableRule = normalizedOwner.Contains("Before an enable you MUST name each such row to the developer", StringComparison.Ordinal);
        bool hasCallSyntax = normalizedOwner.Contains("entity-schema-name=<Object>", StringComparison.Ordinal)
            && normalizedOwner.Contains("preview=true", StringComparison.Ordinal)
            && normalizedOwner.Contains("operations=read", StringComparison.Ordinal);

        // Assert
        hasSection.Should().BeTrue(because: "portal-sections step 4 sends the reader to that flow by name");
        hasEnableRule.Should().BeTrue(because: "portal-sections step 4 restates the owner's enable precondition");
        hasCallSyntax.Should().BeTrue(because: "the step 4 example call uses the owner's argument form");
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
    [Description("No set-object-rights call in the article carries include-connected: set changes one object per call and refuses the argument.")]
    public void PortalSections_ShouldNotPassIncludeConnectedToSetObjectRights()
    {
        // Arrange
        string guide = Normalized(File.ReadAllText(Path.Combine(Root(), PortalSectionsPath)));

        // Act
        // A call can wrap across lines, so each code span is checked whole, not each physical line.
        string[] offending = System.Text.RegularExpressions.Regex.Matches(guide, "`[^`]*`")
            .Select(match => match.Value)
            .Where(span => span.Contains("set-object-rights", StringComparison.OrdinalIgnoreCase)
                && span.Contains("include-connected", StringComparison.OrdinalIgnoreCase))
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
}

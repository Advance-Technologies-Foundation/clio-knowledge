using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the set-object-rights rules in ONE place and keeps them there. The rules are safety-relevant: the
/// object's rows are a priority list, so a revoke keeps the row as a deny, a grant below the All employees row
/// changes nothing for internal users, and turning operation permissions on or off is always an explicit flag.
/// An article that restated the old contract (a default grant, a fan-out to lookups, a two-step confirmation
/// code, a security-object opt-in) would lead an agent to call arguments the tool refuses, or to expect effects
/// it no longer has.
/// </summary>
[TestFixture]
public sealed class ObjectRightsGuidanceTests
{
    private const string ObjectRightsPath = "guidance/mcp/guides/operations/object-rights.md";

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

    [Test]
    [Description("entity-operation-access defers the set-object-rights enable, revoke and disable rules to object-rights instead of restating them, and does not claim that a first grant turns operation permissions on by itself.")]
    public void EntityOperationAccess_ShouldDeferTheRulesToObjectRights()
    {
        // Arrange
        string guide = Normalized(File.ReadAllText(
            Path.Combine(Root(), "guidance/mcp/guides/operations/entity-operation-access.md")));

        // Act
        bool restatesLifecycle = guide.Contains("turns them back OFF", StringComparison.OrdinalIgnoreCase)
            || guide.Contains("revoking the last", StringComparison.OrdinalIgnoreCase);
        bool claimsImplicitEnable = guide.Contains("granting the first role turns it on",
            StringComparison.OrdinalIgnoreCase);

        // Assert
        guide.Should().Contain("name=object-rights",
            because: "the enable, revoke and disable rules are owned by the object-rights article");
        guide.Should().Contain("a grant turns it on only with `enable-operation-permissions`",
            because: "a grant turns operation permissions on only with that flag");
        restatesLifecycle.Should().BeFalse(
            because: "restating the revoke lifecycle here drifts from the owner and contradicts its refusals");
        claimsImplicitEnable.Should().BeFalse(
            because: "a grant without enable-operation-permissions is refused, so the first grant enables nothing");
    }

    [Test]
    [Description("object-rights states the refusal of a revoke that would leave no granting row, and that disable-operation-permissions is the only way to turn operation permissions off.")]
    public void ObjectRights_ShouldOwnTheNoGrantingRowRefusal()
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), ObjectRightsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain("no row that grants any operation is REFUSED",
            because: "the owning article must keep the refusal an agent relies on");
        normalized.Should().Contain("Passing disable-operation-permissions instead turns operation permissions OFF",
            because: "the explicit opt-in is the only path to turning operation permissions off");
    }

    [TestCase("HIGHEST MATCHING ROW", TestName = "ObjectRights_ShouldStateThePriorityRule")]
    [TestCase("a row with no operations is an explicit DENY",
        TestName = "ObjectRights_ShouldStateThatAClearedRowDenies")]
    [TestCase("clears the named operations on the grantee's row and KEEPS the row",
        TestName = "ObjectRights_ShouldStateThatARevokeKeepsTheRow")]
    [TestCase("the grant changes nothing for internal users",
        TestName = "ObjectRights_ShouldStateThatTheAllEmployeesRowShadowsAnInternalGrant")]
    [TestCase("ONE object per call", TestName = "ObjectRights_ShouldStateOneObjectPerCall")]
    [TestCase("nothing is granted by default", TestName = "ObjectRights_ShouldStateThatOperationsAreRequired")]
    [TestCase("REFUSED (nothing is written) unless enable-operation-permissions is passed",
        TestName = "ObjectRights_ShouldStateThatEnablingIsExplicit")]
    [TestCase("ASK THE DEVELOPER IN CHAT BEFORE EVERY WRITE",
        TestName = "ObjectRights_ShouldRequireAskingInChatBeforeEveryWrite")]
    [TestCase("an auto-approve mode skips that approval",
        TestName = "ObjectRights_ShouldStateThatTheHostApprovalIsNotTheDevelopersYes")]
    [TestCase("first call it with preview=true", TestName = "ObjectRights_ShouldPreviewBeforeAsking")]
    [TestCase("When the role has NO row it lists every row",
        TestName = "ObjectRights_ShouldStateWhatTheReadShowsForARoleWithoutARow")]
    [TestCase("a stored one is kept AS STORED", TestName = "ObjectRights_ShouldStateThatAStoredAllEmployeesRowIsKeptAsStored")]
    [TestCase("For an INTERNAL grantee, do not turn operation permissions on just to grant",
        TestName = "ObjectRights_ShouldNotEnableForAnInternalGranteeByDefault")]
    [TestCase("It does NOT list lookups the object inherits",
        TestName = "ObjectRights_ShouldStateWhatTheConnectedListingLeavesOut")]
    [TestCase("`All employees` = `a29a3ba5-4b0d-de11-9a51-005056c00008`",
        TestName = "ObjectRights_ShouldOwnTheAllEmployeesId")]
    [TestCase("returns success with ZERO rows", TestName = "ObjectRights_ShouldStateThatADeniedReadIsNotAnError")]
    [Description("object-rights keeps each rule of the one-object, explicit-flag contract that an agent must weigh before a write; losing one of them in an edit turns the article back into the old contract.")]
    public void ObjectRights_ShouldStateTheContractRule(string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), ObjectRightsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "the owning article must keep every rule of the tool contract");
    }

    // Every published body plus the bundle manifest, whose item descriptions agents read as well.
    private static IEnumerable<string> PublishedTexts() =>
        Directory.EnumerateFiles(Path.Combine(Root(), "guidance"), "*.md", SearchOption.AllDirectories)
            .Append(Path.Combine(Root(), "bundle-source.json"));

    [TestCase("confirmation-code")]
    [TestCase("connected-operations")]
    [TestCase("allow-security-object")]
    [Description("No guidance article and no bundle description names a set-object-rights argument the tool has retired: the tool refuses an unknown argument before any read or write, so an agent that follows such text fails every call.")]
    public void Guidance_ShouldNotNameARetiredSetObjectRightsArgument(string retired)
    {
        // Arrange
        IEnumerable<string> texts = PublishedTexts();

        // Act
        string[] naming = texts
            .Where(path => File.ReadAllText(path).Contains(retired, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(Root(), path))
            .ToArray();

        // Assert
        naming.Should().BeEmpty(because: $"'{retired}' is no longer a set-object-rights argument");
    }

    [Test]
    [Description("No guidance line puts include-connected on a set-object-rights call: set changes one object per call and refuses the argument; include-connected belongs to get-object-rights only.")]
    public void Guidance_ShouldNotPassIncludeConnectedToSetObjectRights()
    {
        // Arrange
        IEnumerable<string> texts = PublishedTexts();

        // Act
        string[] offending = texts
            .SelectMany(path => File.ReadAllLines(path).Select(line => (path, line)))
            .Where(entry =>
            {
                int set = entry.line.IndexOf("set-object-rights", StringComparison.OrdinalIgnoreCase);
                return set >= 0 && entry.line.IndexOf("include-connected", set, StringComparison.OrdinalIgnoreCase) > set;
            })
            .Select(entry => $"{Path.GetRelativePath(Root(), entry.path)}: {entry.line.Trim()}")
            .ToArray();

        // Assert
        offending.Should().BeEmpty(because: "include-connected is an argument of get-object-rights, never of set-object-rights");
    }
}

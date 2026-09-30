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
        guide.Should().Contain("enable-operation-permissions",
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
        normalized.Should().Contain("disable-operation-permissions",
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
    [TestCase("Ask the developer BEFORE any write", TestName = "ObjectRights_ShouldRequireAskingBeforeEachWrite")]
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

    [TestCase("confirmation-code")]
    [TestCase("connected-operations")]
    [TestCase("allow-security-object")]
    [Description("No guidance article names a set-object-rights argument the tool has retired: the tool refuses an unknown argument before any read or write, so an agent that follows such an article fails every call.")]
    public void Guidance_ShouldNotNameARetiredSetObjectRightsArgument(string retired)
    {
        // Arrange
        string guidance = Path.Combine(Root(), "guidance");

        // Act
        string[] naming = Directory.EnumerateFiles(guidance, "*.md", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(retired, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(Root(), path))
            .ToArray();

        // Assert
        naming.Should().BeEmpty(because: $"'{retired}' is no longer a set-object-rights argument");
    }
}

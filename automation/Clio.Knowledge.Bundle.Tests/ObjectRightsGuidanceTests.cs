using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the set-object-rights revoke/disable rule in ONE place. The rule is safety-relevant: a revoke that
/// would remove an object's last rights row is refused because turning operation permissions off widens
/// access to every internal user. An article that restated the old lifecycle ("revoking the last grant
/// turns them back OFF") would lead an agent to pass disable-operation-permissions without that intent.
/// </summary>
[TestFixture]
public sealed class ObjectRightsGuidanceTests
{
    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    [Test]
    [Description("entity-operation-access defers the set-object-rights revoke/disable rule to object-rights instead of restating it.")]
    public void EntityOperationAccess_ShouldDeferRevokeRuleToObjectRights()
    {
        // Arrange
        string guide = File.ReadAllText(
            Path.Combine(Root(), "guidance/mcp/guides/operations/entity-operation-access.md"));

        // Act
        bool restatesLifecycle = guide.Contains("turns them back OFF", StringComparison.OrdinalIgnoreCase)
            || guide.Contains("revoking the last", StringComparison.OrdinalIgnoreCase);

        // Assert
        guide.Should().Contain("name=object-rights",
            because: "the revoke and disable rules are owned by the object-rights article");
        restatesLifecycle.Should().BeFalse(
            because: "restating the revoke lifecycle here drifts from the owner and contradicts the last-row refusal");
    }

    [Test]
    [Description("object-rights states the last-row refusal and that disable-operation-permissions is the only way to turn operation permissions off.")]
    public void ObjectRights_ShouldOwnTheLastRowRefusal()
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), "guidance/mcp/guides/operations/object-rights.md"));

        // Act
        string normalized = string.Join(" ", guide.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        // Assert
        normalized.Should().Contain("LAST rights row is REFUSED",
            because: "the owning article must keep the refusal an agent relies on");
        normalized.Should().Contain("disable-operation-permissions",
            because: "the explicit opt-in is the only path to turning operation permissions off");
    }
}

using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
namespace Clio.Knowledge.Bundle.Tests;
[TestFixture]
public sealed class RecordPermissionGuidanceTests {
    [Test]
    [Description("The unified entry point routes to distinct owners and preserves the tested failure and verification boundaries.")]
    public void PermissionFamily_ShouldKeepOwnersAndBoundariesDiscoverable() {
        // Arrange
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json"))) directory = directory.Parent;
        string root = directory!.FullName;
        string Read(string name) => File.ReadAllText(Path.Combine(root, "guidance/mcp/guides/operations", name + ".md"));
        string entry = Read("record-permissions");
        string extension = Read("record-permission-extensions");
        string stored = Read("record-rights");
        string routing = File.ReadAllText(Path.Combine(root, "guidance/mcp/guides/routing.md"));
        using JsonDocument bundle = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "bundle-source.json")));
        // Act
        string[] names = bundle.RootElement.GetProperty("resources").EnumerateArray().Select(item => item.GetProperty("itemId").GetString()!).ToArray();
        // Assert
        names.Should().Contain(new[] { "record-permissions", "record-permission-extensions", "record-rights" }, "each canonical owner must be deliverable");
        entry.Should().ContainAll(new[] { "`record-rights`", "`record-permission-extensions`", "`process-access-rights`", "NOT a complete effective-access decision", "own authenticated session" }, "the entry must route each responsibility and require real identity verification");
        stored.Should().Contain("MUST first read `record-permissions`", "stored-grant callers must discover the broader model");
        routing.Should().Contain("name=record-permissions", "dynamic permission requests must discover the entry point");
        extension.Should().ContainAll(new[] { "IReadRecordPermissionExtension", "IEditRecordPermissionExtension", "IDeleteRecordPermissionExtension",
            "10.1.585.0", "context.SchemaName", "And (1)", "Or (2)", "InsteadOf (3)", "`null` is NOT denial",
            "Entity.UseAdminRights=true", "CanManageSolution", "CanManageAdministration", "delegation", "DataService" },
            "the contract must retain all three interfaces, activation prerequisites and verified security limits");
    }
}

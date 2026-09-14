using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>Protects the discovery and safety contract for entity-backed list authoring.</summary>
[TestFixture]
public sealed class ListWidgetGuidanceTests
{
    [Test]
    [Description("Resolves every list-authoring entry point to the same published guide and preserves binding and runtime-verification boundaries.")]
    public void Guidance_ShouldResolveTheBindingOwner_WhenListAuthoringIsRouted()
    {
        // Arrange
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "bundle-source.json")));

        // Act
        JsonElement resource = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "list-widget");
        string guide = File.ReadAllText(Path.Combine(root, resource.GetProperty("sourcePath").GetString()!));
        string[] entryPoints =
        [
            "routing.md", "pages/modification/index.md", "pages/modification/containers.md",
            "dashboards/index.md", "dashboards/layout.md"
        ];

        // Assert
        resource.GetProperty("uri").GetString().Should().Be("docs://knowledge/com.creatio.clio/list-widget",
            because: "all entry points need one stable binding-contract identity");
        foreach (string path in entryPoints)
        {
            File.ReadAllText(Path.Combine(root, "guidance/mcp/guides", path)).Should().Contain("list-widget",
                because: $"{path} must route list authors before they copy an incomplete visual node");
        }
        guide.Should().ContainAll([
            "remove ONLY the leading `$`", "modelConfig.path", "isCollection: true",
            "columns[].code", "primaryColumnName", "filterAttributes[].name",
            "required WHEN referenced", "An empty local diff does not establish",
            "Do not reject a list solely", "Navigate away and reopen", "not a claim that clio now hard-rejects"
        ], because: "the correction must preserve complete bindings without false claims about inheritance or offline validation");
    }
}

using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class UiProjectGuidanceTests
{
    [Test]
    [Description("Keeps the generated Angular project's executable test workflow and zero-spec boundary explicit.")]
    public void UiProjectGuidance_ShouldRequireARealSpec_WhenRunningAngularTests()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guidePath = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "applications",
            "ui-project.md");

        // Act
        string guidance = File.ReadAllText(guidePath);

        // Assert
        guidance.Should().Contain("`npm test` or `ng test`",
            because: "agents should expose the generated project's supported Angular test commands")
            .And.Contain("When `creatioVersion` is omitted, the current default Angular 19 template uses Jest",
                because: "the Jest-specific rule applies only to the current default template")
            .And.Contain("no `*.spec.ts` files",
                because: "an untouched scaffold should not be mistaken for a passing empty test suite")
            .And.Contain("Do not enable `passWithNoTests`",
                because: "a zero-test success would hide broken test discovery instead of proving the scaffold works")
            .And.Contain("a real spec proves the generated Angular test environment works",
                because: "the success criterion must execute Angular's test environment rather than only inspect config")
            .And.Contain("A version-specific template can use a different test runner, including Karma",
                because: "agents must not apply Jest-only settings to older supported templates");
    }

    [Test]
    [Description("Keeps generated UI-project testing requests discoverable through routing and resource metadata.")]
    public void UiProjectGuidance_ShouldBeDiscoverable_WhenTestingGeneratedProject()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string routingPath = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "routing.md");
        string sourcePath = Path.Combine(repositoryRoot, "bundle-source.json");

        // Act
        string routing = File.ReadAllText(routingPath);
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(sourcePath));
        string description = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "ui-project")
            .GetProperty("description")
            .GetString()!;

        // Assert
        routing.Should().Contain(
            "create, build, or test a Freedom UI Angular remote-module project with new-ui-project -> name=ui-project",
            because: "testing requests should route to the guide that owns the generated project's runner boundary");
        description.Should().Contain("scaffolding, building, and testing",
            because: "resource discovery metadata should advertise the article's test-workflow scope")
            .And.Contain("runner selection",
                because: "versioned templates can use a different test runner than the current default");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

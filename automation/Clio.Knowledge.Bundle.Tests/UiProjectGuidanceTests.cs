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
            .And.Contain("no `*.spec.ts` files",
                because: "an untouched scaffold should not be mistaken for a passing empty test suite")
            .And.Contain("Do not enable `passWithNoTests`",
                because: "a zero-test success would hide broken test discovery instead of proving the scaffold works")
            .And.Contain("a real spec proves the generated Angular test environment works",
                because: "the success criterion must execute Angular's test environment rather than only inspect config");
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

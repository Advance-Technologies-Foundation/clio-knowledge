using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ProcessPreconfiguredPageCatalogGuidanceTests
{
    [Test]
    [Description("ENG-102112: the element catalog lists the Pre-configured page among the user tasks with a dedicated build type. It was missing from that list, so an agent could legitimately pick the generic userTask route, which carries no page and is refused from CrtProcessBuilder 1.6.6.86.")]
    public void ElementCatalog_ShouldRoutePreconfiguredPageToItsDedicatedType()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string catalog = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/element-catalog.md"));

        // Assert
        catalog.Should().Contain("`preconfiguredPageUserTask` -> `type:\"preconfiguredPage\"`",
            because: "the dedicated-build-type list is where an agent mapping a task name to a build type looks");
        catalog.Should().NotContain("THREE user tasks have their own dedicated build type",
            because: "the old count excluded the Pre-configured page and contradicts the list that now includes it");
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

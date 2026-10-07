using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ProcessPreconfiguredPageCatalogGuidanceTests
{
    [Test]
    [Description("ENG-102112: the element catalog lists the Pre-configured page among the user tasks with a dedicated build type. It was missing from that list, so an agent could legitimately pick the generic userTask route, which carries no page and is refused from CrtProcessBuilder 1.6.6.87.")]
    public void ElementCatalog_ShouldRoutePreconfiguredPageToItsDedicatedType()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string catalog = ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/processes/element-catalog.md");

        // Assert
        catalog.Should().Contain("FOUR user tasks have their own dedicated build type",
            because: "the count is read before the list, and it must match the four entries that follow");
        catalog.Should().Contain("`preconfiguredPageUserTask` -> `type:\"preconfiguredPage\"`",
            because: "the dedicated-build-type list is where an agent mapping a task name to a build type looks");
    }

    [Test]
    [Description("ENG-102112: the article that owns the Pre-configured page states the build-type rule itself, so an agent that fetches only process-preconfigured-page still learns the generic userTask route is refused.")]
    public void PreconfiguredPageArticle_ShouldStateTheGenericRouteIsRefused()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string article = ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/processes/preconfigured-page.md");

        // Assert
        article.Should().Contain("Build it ONLY as `type:\"preconfiguredPage\"`",
            because: "the owning article is the authoritative place for the element's build rule");
        article.Should().Contain("`PreconfiguredPageUserTask` cannot carry the block",
            because: "the article must say why the generic route fails, not only that it is discouraged");
    }
}

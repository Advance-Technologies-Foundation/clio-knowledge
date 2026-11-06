using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class MultiStartGuidanceTests
{
    private const string ItemId = "process-multi-start";
    private const string ArticlePath = "guidance/mcp/guides/processes/multi-start.md";

    [Test]
    [Description("process-multi-start is registered ungated, required by the library and routed: a build that needs it starts from the digest, whose rule 1 points at it, and a request that does not start there finds it through the routing guide (the process-modeling description has no room under its 1,000-character limit).")]
    public void Resource_ShouldBeRegisteredAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string[] requiredItems = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString()!)];
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string routing = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/routing.md"));
        string digest = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/processes/digest.md"));

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
            because: "the manifest must point at the article this fixture pins");
        resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
            because: "the flat resources folder names each article by its itemId");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "the article carries its own contract gate, so a feature gate would only hide it");
        requiredItems.Should().Contain(ItemId, because: "the library requires every item it publishes for the process tools");
        requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
            because: "the requirement lists the canonical route next to the item id");
        routing.Should().Contain("name=process-multi-start", because: "an article nothing routes to is never read");
        digest.Should().Contain("shared chain: `process-multi-start`",
            because: "rule 1 is where a build learns that two starts share one chain");
    }

    [Test]
    [Description("The article gates mergeFrom on the full create-business-process contract, so a clio that does not carry it is told to update rather than sent a field its server drops, and it names the merge rules a caller acts on: the shape, one object, the type, the direction, the stable order and the refusals that keep a merge from being lost.")]
    public void Guide_ShouldGateAndStateTheMerge()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath)).ReplaceLineEndings("\n");

        // Assert
        guide.Should().Contain("use `mergeFrom` only when the FULL `create-business-process` contract",
            because: "an older clio has no mergeFrom and its server would drop it while answering success");
        guide.Should().Contain("\"mergeFrom\": [\"OpportunityProductAddedSignal.RecordId\", \"OpportunityProductChangedSignal.RecordId\"]",
            because: "the example is the shape a caller copies");
        guide.Should().Contain("All starts on ONE object",
            because: "the server refuses starts on different objects, and the guide must say why before it does");
        guide.Should().Contain("Direction is always `Internal`", because: "any other direction is refused");
        guide.Should().Contain("the same request always builds the same process",
            because: "the stored order is the process's, not the list's");
        guide.Should().Contain("`removeElement` of a listed start is refused",
            because: "removing a source is the change that would otherwise lose the merge");
        guide.Should().Contain("the multi-start example in `process-parameters-details`",
            because: "a clio without mergeFrom still has a working shape, and the gate must point at it rather than "
                + "stop the build (review of clio-knowledge#314)");
        guide.Should().NotContain("formulaTask",
            because: "the Formula-per-start recipe lives in process-parameters-details; restating it here would "
                + "offer two ways where mergeFrom is available");
    }
}

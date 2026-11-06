using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class MultiStartGuidanceTests
{
    private const string ItemId = "process-multi-start";
    private const string ArticlePath = "guidance/mcp/guides/processes/multi-start.md";
    private const string ParametersPath = "guidance/mcp/guides/processes/parameters.md";
    private const string ParametersDetailsPath = "guidance/mcp/guides/processes/parameters-details.md";

    [Test]
    [Description("process-multi-start is registered ungated, required by the library and routed: a build that needs it starts from the digest, whose rule 1 points at it, and a request that does not start there finds it through the process-modeling description and the routing guide.")]
    public void Resource_ShouldBeRegisteredAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");
        JsonElement[] resources = [.. source.RootElement.GetProperty("resources").EnumerateArray()];

        // Act
        JsonElement resource = resources.Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string processModelingDescription = resources
            .Single(item => item.GetProperty("itemId").GetString() == "process-modeling")
            .GetProperty("description").GetString()!;
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
        processModelingDescription.Should().Contain(ItemId,
            because: "process-modeling's description lists the articles it routes to, and it sits two characters "
                + "under its 1,000-character limit: a later trim must not drop this route silently");
        digest.Should().Contain("shared chain: `process-multi-start`",
            because: "rule 1 is where a build learns that two starts share one chain");
    }

    [Test]
    [Description("The article gates mergeFrom on the package floor and the full create-business-process contract, so a clio that does not carry it builds the Formula-per-branch fallback instead of a field it cannot send, states the boundary of what mergeFrom merges, and names the merge rules a caller acts on: the shape, one object, the type, the direction, the stable order and the refusals that keep a merge from being lost.")]
    public void Guide_ShouldGateAndStateTheMerge()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath)).ReplaceLineEndings("\n");

        // Assert
        guide.Should().Contain("use `mergeFrom` only when the FULL `create-business-process` contract",
            because: "an older clio has no mergeFrom and its typed descriptor drops the member before the server "
                + "sees it; a server since CrtProcessBuilder 1.6.6.136 refuses an undeclared key instead");
        guide.Should().Contain("`mergeFrom` needs CrtProcessBuilder 1.6.6.141 or later on the environment",
            because: "a current clio refuses an older package up front, and the article must say why, as the "
                + "sibling articles state their package floors");
        guide.Should().Contain("the Formula-per-branch shape in `process-parameters-details`",
            because: "a clio without mergeFrom still has a working shape, and the gate must point at it rather than "
                + "stop the build");
        guide.Should().Contain("\"mergeFrom\": [\"OpportunityProductAddedSignal.RecordId\", \"OpportunityProductChangedSignal.RecordId\"]",
            because: "the example is the shape a caller copies");
        guide.Should().Contain("All starts on ONE object",
            because: "the server refuses starts on different objects, and the guide must say why before it does");
        guide.Should().Contain("Direction is always `Internal`", because: "any other direction is refused");
        guide.Should().Contain("the same request always builds the same process",
            because: "the stored order is the process's, not the list's");
        guide.Should().Contain("`removeElement` of a listed start is refused",
            because: "removing a source is the change that would otherwise lose the merge");
    }

    [Test]
    [Description("The article states where mergeFrom stops and hands everything else to process-parameters, which owns the per-branch Formula; it names that route in one sentence and never restates the recipe as the multi-start shape.")]
    public void Guide_ShouldStateTheBoundaryAndPointAtTheOwnerOfTheFallback()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapse(File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath)));

        // Assert
        guide.Should().Contain("`mergeFrom` merges signal starts on ONE object, nothing else",
            because: "clio's binding warnings send starts mergeFrom cannot merge to the same answer, and the article "
                + "must not read as the remedy for them");
        guide.Should().Contain("with a `formulaTask` instead; `process-parameters` owns that rule",
            because: "the per-branch Formula has one owner, and this article only points at it");
        guide.Should().NotContain("resultProcessParameter",
            because: "the Formula recipe lives in process-parameters and process-parameters-details; restating it "
                + "here would offer two shapes where mergeFrom is available");
        guide.Should().NotContain("\"formulaTask\"",
            because: "no example in this article may build a Formula per start");
    }

    [Test]
    [Description("The modify rules match what CrtProcessBuilder 1.6.6.141 accepts: a one-entry list is refused, so leaving a two-start merge goes through mergeFrom: [] and re-pointed readers; an unbound parameter reads empty with no warning; sources resolve at once; a Lookup cannot follow its starts to another object; a hand-edited formula is restored only while it is still a pure merge; and the start-not-in-the-list warning also fires for a lone start.")]
    public void Guide_ShouldStateTheModifyRulesTheServerEnforces()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Collapse(File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath)));

        // Assert
        guide.Should().Contain("Add a start to a merge: `setParameter` with the extended list",
            because: "extending the list is how a start joins an existing merge, and the remedy for the warning");
        guide.Should().Contain("In one request either goes AFTER the `addElement` of every new start it names",
            because: "the server resolves the list at once and refuses a start that is added later in the batch");
        guide.Should().Contain("the chain then reads an empty Id on every run, with no warning",
            because: "neither warning fires after mergeFrom: [], so the article is the only thing that says so");
        guide.Should().Contain("from two (a one-entry list is refused), `mergeFrom: []` and point the chain's readers at the remaining start's `RecordId`",
            because: "taking one start out of a two-start merge by a shorter list is refused");
        guide.Should().Contain("and for a `Lookup` parameter even then",
            because: "moving every listed start together is allowed for a Guid only");
        guide.Should().Contain("restores the merge only while that formula references nothing but signal starts' `RecordId`s",
            because: "a formula with a designer-added fallback is refused as already taking a value");
        guide.Should().Contain("neither the refusals above nor the start-not-in-the-list warning guard it",
            because: "a formula the server does not recognise as a merge is reported raw and unguarded");
        guide.Should().Contain("also when it is the only start that reaches that element",
            because: "since 1.6.6.141 the warning fires for a lone unlisted start too");
    }

    [Test]
    [Description("process-parameters keeps the general rule for SETTING a parameter in the flow, scoped to what mergeFrom does not cover, and process-parameters-details keeps the wiring that makes its Formula-per-branch shape buildable.")]
    public void ParameterOwners_ShouldKeepTheFormulaFallbackBuildable()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string parameters = Collapse(File.ReadAllText(Path.Combine(repositoryRoot, ParametersPath)));
        string details = Collapse(File.ReadAllText(Path.Combine(repositoryRoot, ParametersDetailsPath)));

        // Assert
        parameters.Should().Contain("Signal starts on ONE object that share a chain merge their record Ids into one parameter with `mergeFrom` (`process-multi-start`)",
            because: "the owner routes the one case mergeFrom covers to its article");
        parameters.Should().Contain("give each branch a `formulaTask` whose `formula.resultProcessParameter` names it (it wins over a stored binding)",
            because: "starts on different objects, timer and message starts and gateway branches have no other remedy, "
                + "and clio's binding warnings send them here");
        details.Should().Contain("Flow each start into its own read, each read into its own Formula and both Formulas into the shared next step",
            because: "the example lists no flows, and without this sentence an agent guesses the wiring");
        details.Should().Contain("`mappings[]` carries no `targetProcessParameter` entry for `DaysLeftParameter`",
            because: "a mapping per branch next to the Formulas brings back the binding the section warns about");
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}

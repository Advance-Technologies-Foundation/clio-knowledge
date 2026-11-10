using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-dynamic-case</c> guide: <c>create-dynamic-case</c>, a minimal dynamic case (DCM) from a lookup
/// column plus the record-page stage progress bar. Registered banner-less and ungated with no legacy route, routed from
/// <c>routing</c> on its own row and from its Pages block.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each clause below is a rule the tool enforces or a run-time fact an agent cannot see from a successful
/// call - the order rule, the absent compile, the undo order and what is not supported. A reword that keeps the rule
/// updates the clause here; a trim that drops it is the failure these tests exist for.
/// </remarks>
[TestFixture]
public sealed class ProcessDynamicCaseGuidanceTests
{
    private const string ItemId = "process-dynamic-case";
    private const string ArticlePath = "guidance/mcp/guides/processes/dynamic-case.md";
    private const string ModelingPath = "guidance/mcp/guides/processes/process-modeling.md";
    private const string RoutingPath = "guidance/mcp/guides/routing.md";

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath), @"\s+", " ");

    [Test]
    [Description("The guide is registered at its itemId path, required by the library, and routed from routing on its own row and from the Pages block.")]
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
        string routing = ReadCollapsed(RoutingPath);

        // Assert
        using (new AssertionScope())
        {
            resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString())
                .Should().Contain(ItemId, because: "the library requires every guide it ships");
            Regex.Matches(routing, $"name={ItemId}\\b").Count.Should().BeGreaterThanOrEqualTo(2,
                because: "a stage lifecycle and a progress bar on a page both lead an agent here");
        }
    }

    [Test]
    [Description("The guide carries the rules an agent cannot see from a successful call: pass stages, the order source is reported, no compile or restart, the reused settings row is not removed on undo, and what is not supported yet.")]
    public void Article_ShouldKeepTheRulesASuccessfulCallHides()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("create-dynamic-case", because: "the guide owns this tool");
            guide.Should().Contain("pass `stages`", because: "only the caller knows the business order");
            guide.Should().Contain("orderSource", because: "the order used is always answered and must be relayed");
            guide.Should().Contain("NO compile and NO restart", because: "a case is interpreted; a compile only costs time");
            guide.Should().Contain("ONLY when the call answered `settings.status: created`",
                because: "deleting a reused settings row cascades away other cases' link rows");
            guide.Should().Contain("== Not supported yet ==", because: "an agent must not promise steps or transitions");
            guide.Should().NotContain("Part of the process guide set",
                because: "a banner would make process-modeling index the guide, which has no room");
        }
    }

    [Test]
    [Description("process-modeling no longer lists DCM among the schemas that carry their own compile obligations: a case is interpreted.")]
    public void ProcessModeling_ShouldNotClaimACaseNeedsACompile()
    {
        // Arrange & Act
        string modeling = ReadCollapsed(ModelingPath);

        // Assert
        modeling.Should().NotContain("business objects, DCM,",
            because: "the sentence sent agents to compile after creating a case");
    }
}

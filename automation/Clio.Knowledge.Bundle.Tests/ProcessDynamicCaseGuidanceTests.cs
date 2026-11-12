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
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string guide = ProcessGuideSet.Read(repositoryRoot, ArticlePath);

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
            requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
                because: "the requirement lists the canonical route next to the item id");
            resource.GetProperty("topicId").GetString().Should().Be("creatio.process-dynamic-case",
                because: "the topic is the stable cross-library identity of the guide");
            resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
                because: "the tool ships ungated; a gated guide would be hidden while the tool answers");
            guide.Should().NotContain(ProcessGuideSet.SetBanner,
                because: "a banner obliges process-modeling to index the guide, and that article has no room left");
            ProcessGuideSet.GoLiveFloor.Should().NotContain(ItemId,
                because: "the floor holds only banner-carrying articles split out of process-modeling");
            Regex.Matches(routing, $"name={ItemId}\\b").Count.Should().BeGreaterThanOrEqualTo(2,
                because: "a stage lifecycle and a progress bar on a page both lead an agent here");
        }
    }

    [Test]
    [Description("The guide carries the rules an agent cannot see from a successful call: the clio version boundary, pass stages, the order source is reported, no compile or restart, undo only what the call created and in order, and what is not supported yet.")]
    public void Article_ShouldKeepTheRulesASuccessfulCallHides()
    {
        // Arrange & Act
        string guide = ReadCollapsed(ArticlePath);

        // Assert
        using (new AssertionScope())
        {
            guide.Should().Contain("create-dynamic-case", because: "the guide owns this tool");
            guide.Should().MatchRegex(
                @"Version boundary: `create-dynamic-case` needs clio (<[A-Z][A-Z0-9-]*TBD>|\d+(\.\d+){3}) or later; on an older clio, stop here",
                because: "a published guide reaches every clio, and an older one has no such tool to call");
            guide.Should().Contain("pass `stages`", because: "only the caller knows the business order");
            guide.Should().Contain("orderSource", because: "the order used is always answered and must be relayed");
            guide.Should().Contain("NO compile and NO restart", because: "a case is interpreted; a compile only costs time");
            guide.Should().Contain("ONLY when the call answered `settings.status: created`",
                because: "deleting a reused settings row cascades away other cases' link rows");
            guide.Should().Contain("Steps 1-3 ONLY for a call whose `case.status` is `created`",
                because: "after already-present the case and its link row were never written by this call");
            guide.Should().Contain("Steps 4 and 5 follow `page.status` and `dependencies.status` alone",
                because: "a call that found the case can still have inserted the bar or added dependencies");
            guide.Should().Contain("ONLY when `dependencies.status` is `added` or `left-after-failure`; for `kept-for-leftovers` only after the leftovers are gone",
                because: "removed-after-failure dependencies are already gone, and kept ones still hold the leftovers");
            guide.Should().Contain("stop at the first failure",
                because: "a later step assumes the earlier ones took");
            guide.Should().Contain("Re-run the call with `dry-run: true` before anything else",
                because: "after a lost reply the case may exist; the tool's own message says the same, and a real re-run would create it unasked");
            guide.Should().Contain("KEEP this result's `dependencies.added`: no later result reports them",
                because: "a re-run finds the dependencies present and reports none added, so step 5 would never remove them");
            guide.Should().Contain("With `outcome: refused` nothing was written",
                because: "request-failed and page-append-rejected also follow a write, under unknown and created-page-failed");
            guide.Should().Contain("or one without its settings row or link row",
                because: "already-present needs the settings row and the link row too, not only the same stages");
            guide.Should().Contain("AUTO-SAVE mode (`saveOnChange: true`, `askUserToChangeSchema: false`)",
                because: "a stage click saves the whole record, which the user must be told; the designer's default is manual");
            guide.Should().Contain("MUST have the user's explicit agreement",
                because: "the undo tools delete live data and ask for no confirmation");
            guide.Should().Contain("ONLY when `page.status` is `inserted`",
                because: "after already-present the bar on the page is not this call's to remove");
            guide.Should().MatchRegex(
                @"1\. `delete-schema \{ schema-name: case\.schemaName, environment-name, remote: true \}` .* 2\. `remove-data-binding-row-db` on the `SysDcmSchemaInSettings` entry of `settings\.bindings`, package-name `case\.packageName`, key .* 3\. `remove-data-binding-row-db` on the `SysDcmSettings` entry of `settings\.bindings`, package-name `case\.packageName`, key .* 4\. ONLY when `page\.status` is `inserted`: remove the two inserted items .* `update-page` with `mode: replace` .* 5\. `remove-package-dependency` for each of `dependencies\.added`",
                because: "the case goes first as the server's own rollback does (an enabled case without its settings rows keeps starting and cannot be opened), delete-schema reaches an environment only with remote: true, remove-data-binding-row-db requires the package the server bound the rows to, append cannot remove the bar, and the dependencies go last");
            guide.Should().Contain("== Not supported yet ==", because: "an agent must not promise steps or transitions");
            guide.Should().Contain("Steps inside stages", because: "the case is minimal: its stages are empty");
            guide.Should().Contain("several cases per object", because: "the platform starts every enabled case");
            guide.Should().Contain("changing, disabling or deleting an existing case",
                because: "the tool only creates; the case designer changes a case");
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
        modeling.Should().NotContain("DCM",
            because: "the compile-obligation list sent agents to compile after creating a case, wherever DCM stands in it");
    }
}

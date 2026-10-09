using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ProcessTracingGuidanceTests
{
    private const string ArticlePath = "guidance/mcp/guides/processes/tracing.md";

    [Test]
    [Description("process-tracing is declared ungated and the routing guide selects it: tracing is asked for while diagnosing a run, which is not a build, so the article is reachable only through routing (ENG-102111).")]
    public void Resource_ShouldBeUngatedAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-tracing");
        string routing = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/routing.md"));

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
            because: "the manifest must point at the article this test pins");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "process tracing is a shipped capability, not an experimental one");
        routing.Should().Contain("tracing on or off -> name=process-tracing",
            because: "an article nothing routes to is never read");
    }

    [Test]
    [Description("The article states the contract the clio tools and CrtProcessBuilder 1.6.6.92 implement: the field and operation names, that enabled is required, the root-of-the-family scope, the automatic switch-off and the describe block - each one a fact a caller acts on, measured on a stand on 2026-10-09.")]
    public void Guide_ShouldStateTheSwitchContract()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath));

        // Assert
        guide.Should().Contain("top-level `isTracing: true`",
            because: "the create route is a descriptor field");
        guide.Should().Contain("{\"op\":\"setTracing\",\"enabled\":true}",
            because: "the modify route is one operation with one argument");
        guide.Should().Contain("`enabled` is REQUIRED",
            because: "the server refuses a setTracing without it");
        guide.Should().Contain("The switch lives on the version family's ROOT",
            because: "naming one version switches every version, which a caller has to expect");
        guide.Should().Contain("`ProcessParameterTracingDisableTimeoutDays`",
            because: "the automatic switch-off is controlled by this setting");
        guide.Should().Contain("already traced writes nothing and does NOT",
            because: "re-enabling is not a way to extend the countdown");
        guide.Should().Contain("tracing: {enabled: true, turnOffDate: \"yyyy-MM-dd\"}",
            because: "describe reports the switch only while it is on");
        guide.Should().Contain("Switching it needs the `CanManageSolution` right on top of process design",
            because: "the package refuses a caller without it, and the agent should know why before it tries");
        guide.Should().Contain("when it is the ONLY change, send it to `modify-business-process`",
            because: "sent alone to the version tool, setTracing is refused");
        guide.Should().Contain("put `setTracing` in\n  the SAME batch as those edits",
            because: "agents read the former 'send it ALONE' as 'always a separate call' and split one request into two writes (stand run 2026-10-09, TC-06 and TC-09)");
        guide.Should().NotContain("ALONE",
            because: "the word made agents split a graph edit and the switch into two writes");
        guide.Should().Contain("REFUSES a batch made only of `setTracing`",
            because: "the package refuses it, and an agent told otherwise would retry the version tool");
        guide.Should().Contain("the `tracing` block is the proof, never the success of the write",
            because: "an older clio and package pair drops isTracing in silence, so the write's success proves nothing");
    }

    [Test]
    [Description("The article tells the agent to TELL THE USER about the data and volume costs before switching tracing on, and to switch it off afterwards: a trace stores record data, and nothing in a success response says so.")]
    public void Guide_ShouldMakeTheAgentAskAndSwitchItOff()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath));

        // Assert
        guide.Should().Contain("tell the user before you switch it on",
            because: "the costs are invisible in the response");
        guide.Should().Contain("personal data", because: "a trace stores record data");
        guide.Should().Contain("never as a default", because: "tracing is a diagnostic, not a setting");
        guide.Should().Contain("`SysPrcElementTraceLog`", because: "the agent has to know where to read the trace");
    }

    [Test]
    [Description("The trace query in the article is a literal, parseable clio-run call whose SelectQuery sits under 'query': a prose recipe made agents send rootSchemaName/columns/filters as top-level execute-esq arguments, which is refused (8 of 11 failed execute-esq calls in the 2026-10-09 stand run).")]
    public void Guide_ShouldCarryARunnableTraceQuery()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string guide = File.ReadAllText(Path.Combine(repositoryRoot, ArticlePath));
        const string Opening = "{\"command\":\"execute-esq\"";
        int start = guide.IndexOf(Opening, StringComparison.Ordinal);
        int end = guide.IndexOf("\n   Keep the `element` filter", start, StringComparison.Ordinal);

        // Act
        string call = string.Concat(guide[start..end].Split('\n').Select(line => line.Trim()));
        using JsonDocument document = JsonDocument.Parse(call);
        JsonElement args = document.RootElement.GetProperty("args");
        JsonElement query = args.GetProperty("query");

        // Assert
        start.Should().BeGreaterThan(0, because: "the article must carry the call, not describe it");
        args.TryGetProperty("rootSchemaName", out _).Should().BeFalse(
            because: "execute-esq refuses a top-level rootSchemaName; it belongs inside 'query'");
        query.GetProperty("rootSchemaName").GetString().Should().Be("SysPrcElementTraceLog",
            because: "the trace rows live in SysPrcElementTraceLog");
        query.GetProperty("columns").GetProperty("items").EnumerateObject().Select(column => column.Name)
            .Should().Contain(new[] { "TraceEvent", "ElementData", "ProcessData" },
                because: "those are the columns the comparison in step 3 reads");
        query.GetProperty("filters").GetProperty("items").GetProperty("run").GetProperty("leftExpression")
            .GetProperty("columnPath").GetString().Should().Be("SysProcessElementLog.SysProcess",
                because: "a trace row reaches its run through its element-log row");
    }
}

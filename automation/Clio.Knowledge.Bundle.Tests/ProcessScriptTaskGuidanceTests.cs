using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ProcessScriptTaskGuidanceTests
{
    [Test]
    [Description("Keeps ScriptTask guidance independently discoverable without inheriting the experimental process-designer feature gate.")]
    public void Resource_ShouldBeUngatedAndRouted()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "process-script-task");
        string routing = File.ReadAllText(Path.Combine(repositoryRoot, "guidance/mcp/guides/routing.md"));
        string runProcessButton = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/run-process-button.md"));

        // Assert
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "ScriptTask C# guidance applies to existing processes even when experimental process-designer tools are disabled");
        routing.Should().Contain("name=process-script-task",
            because: "an ungated article is useful only when the mandatory routing guide can select it");
        runProcessButton.Should().Contain("copy exactly what `get-process-signature` echoes back",
            because: "the button guide must remain self-contained when process-modeling is feature-gated");
        runProcessButton.Should().NotContain("process-modeling",
            because: "an ungated button workflow must not require an unavailable experimental article");
    }

    [Test]
    [Description("Pins the compatibility details that prevent common ScriptTask compile failures across Creatio runtimes.")]
    public void Guide_ShouldKeepPortableParameterLoggingAndJsonRules()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/process-script-task.md"));

        // Assert
        guide.Should().Contain("Get<Guid>(\"UsrAccountId\")",
            because: "ScriptTask inputs must be read through the generated process parameter API by exact code");
        guide.Should().Contain("get-process-signature process-name=<code-or-caption> environment-name=<env>",
            because: "the ungated guide must name the executable tool path that discovers exact parameter codes");
        guide.Should().Contain("Set<string>(\"UsrResult\"",
            because: "ScriptTask outputs must be written through the same generated parameter API");
        guide.Should().Contain("global::Common.Logging.LogManager",
            because: "global qualification avoids the Terrasoft.Common namespace collision in generated process code");
        guide.Should().Contain("JsonConvert.SerializeObject(value)",
            because: "the one-argument overload remains portable to older Newtonsoft.Json assemblies");
        guide.Should().Contain("`AggregationTypeStrict` and `LogicalOperationStrict` belong to `Terrasoft.Common`",
            because: "AggregationTypeStrict belongs to Terrasoft.Common rather than Terrasoft.Core.DB");
        guide.Should().NotContain("AggregationTypeStrict` and `LogicalOperationStrict` belong to `Terrasoft.Core.DB`",
            because: "the concrete compiler failure came from assigning the enums to the wrong namespace");
        guide.Should().Contain("Creatio 10.0.0.858 assemblies",
            because: "a compatibility rule must carry its verified runtime boundary rather than read as universal folklore");
        guide.Should().Contain("Treat every process parameter as untrusted input",
            because: "matching the signature establishes type compatibility, not record or business authorization");
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

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

    [Test]
    [Description("Pins the rules a ScriptTask author cannot recover from anywhere else: that the element costs a compile and comes last in the decision order, that the compile is compile-creatio process-name, the namespaces that are NOT imported, why an alias exists and when one is refused, and that Get/Set names are case-sensitive. Each was measured on a stand (ENG-92711); losing one sends an author into a compile failure or a silently stale process.")]
    public void Guide_ShouldKeepTheAuthoringDecisionCompileAndNamespaceRules()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance/mcp/guides/processes/process-script-task.md"));

        // Assert
        guide.Should().Contain("Decide first: a ScriptTask costs a compile",
            because: "the element is the one that makes a clio-built process need a compile, so choosing it is a decision");
        guide.Should().Contain("call it from a custom user task",
            because: "reusable C# belongs in a compiled user task, whose callers need no compile of their own");
        guide.Should().Contain("run `compile-creatio` with `process-name` set to the process",
            because: "on Creatio 10.x a plain and a package-name compile were both measured leaving an edited body uncompiled");
        guide.Should().Contain("with the user's confirmation that tool requires",
            because: "a compile reloads the runtime for every user, so the process compile is asked for like any other");
        guide.Should().Contain("keeps running its PREVIOUS body",
            because: "an edited, uncompiled script runs the old code silently, which no save reports");
        guide.Should().Contain("There is NO `System.Linq`, and no `Terrasoft.Configuration`",
            because: "the two namespaces a script most often needs are exactly the two it does not get");
        guide.Should().Contain("An ALIAS on a default namespace is refused",
            because: "the generator drops such an entry with its alias, so the alias would not exist at compile time");
        guide.Should().Contain("makes `SysSettings` ambiguous (CS0104)",
            because: "this is the measured collision an alias exists to break");
        guide.Should().Contain("the run-time lookup is case-sensitive",
            because: "Get/Set resolve names ordinally, unlike every name lookup in the builder");
        guide.Should().Contain("The text is C# CLASS MEMBERS, not statements",
            because: "the methods text is pasted into the generated class, so statements there do not compile");
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

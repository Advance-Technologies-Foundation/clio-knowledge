using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ParameterDirectionGuidanceTests
{
    private const string ParametersPath = "guidance/mcp/guides/processes/parameters.md";
    private const string ParametersDetailsPath = "guidance/mcp/guides/processes/parameters-details.md";
    private const string FilesPath = "guidance/mcp/guides/processes/files.md";

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    [Test]
    [Description("process-parameters tells the agent how to choose a parameter's direction (Internal for a helper, In / Out / Variable only for what a caller passes or reads), states the builder's default switch with its clio boundary, and says that an older package stays silent so the agent states Internal itself.")]
    public void Parameters_ShouldStateTheDirectionRuleAndTheDefaultSwitch()
    {
        // Arrange
        string parameters = Read(ParametersPath);

        // Act
        string normalized = string.Join(' ', parameters.Split((char[])['\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries));

        // Assert
        normalized.Should().Contain("CHOOSE THE DIRECTION BY WHO USES THE VALUE",
            because: "the rule is the point of the article section, and an agent scanning the guide keys on it");
        normalized.Should().Contain("`Internal` — the value only passes between elements of THIS process",
            because: "a helper parameter is the common case and must be named as Internal");
        normalized.Should().Contain("a `mergeFrom` parameter is always `Internal`",
            because: "the multi-start merge is the other way a shared helper is built, and it is never Variable");
        normalized.Should().Contain("is saved as `Internal`, and the response carries a warning naming it",
            because: "an agent that left the direction out must recognise the builder's switch and its notice");
        normalized.Should().Contain("from clio 8.1.0.150",
            because: "a version boundary is stated as the clio version that ships the change");
        normalized.Should().Contain("An older CrtProcessBuilder neither switches nor warns, so silence proves nothing",
            because: "silence after a build on an older package must not read as 'no helper is Variable'");
        normalized.Should().Contain("A direction the request states is never switched, but a `Variable` stated for a parameter the request creates and an element fills gets the advice below once",
            because: "a stated direction is the author's choice and is never switched, yet a helper declared Variable must not land in the signature with no word");
        normalized.Should().Contain("A `FileCollection` keeps `Variable` and is not advised otherwise",
            because: "the FileCollection default is the builder's own choice and gets no advice");
        normalized.Should().Contain("a parameter that already existed before the request is never changed",
            because: "an existing parameter is only advised about, never switched");
        normalized.Should().NotContain("CrtProcessBuilder 1.6.6.143",
            because: "that package number was overtaken by other cuts before this change shipped");
    }

    [Test]
    [Description("The examples that declare a helper parameter declare it Internal, so an agent copying them builds no Variable helper: the Formula-per-branch shape in process-parameters-details and recipe 1 of process-files.")]
    public void Examples_ShouldDeclareHelpersInternal()
    {
        // Arrange
        string details = Read(ParametersDetailsPath);
        string files = Read(FilesPath);

        // Act
        string normalizedFiles = string.Join(' ', files.Split((char[])['\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries));

        // Assert
        details.Should().Contain("{ \"name\": \"DaysLeftParameter\", \"type\": \"Integer\", \"direction\": \"Internal\"",
            because: "the shared parameter each branch sets is a helper no caller passes or reads");
        normalizedFiles.Should().Contain(
            "{\"name\": \"DocumentsParameter\", \"type\": \"FileCollection\", \"direction\": \"Internal\"}",
            because: "recipe 1 keeps files an element produced for later elements, and the builder leaves a FileCollection Variable");
        normalizedFiles.Should().Contain("A helper: `Internal`.",
            because: "the Direction bullet of process-files must not contradict process-parameters");
    }
}

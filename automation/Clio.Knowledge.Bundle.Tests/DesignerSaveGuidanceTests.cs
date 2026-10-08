using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class DesignerSaveGuidanceTests
{
    private static readonly Regex PageFactoryOpening = new(
        @"define\(\s*""[^""]*""\s*,\s*/\*\*SCHEMA_DEPS\*/.*?/\*\*SCHEMA_DEPS\*/\s*,\s*function\s*/\*\*SCHEMA_ARGS\*/\s*\([^)]*\)\s*/\*\*SCHEMA_ARGS\*/\s*\{",
        RegexOptions.Singleline);
    private static readonly Regex FactoryReturn = new(@"\breturn\b");
    private static readonly Regex Comments = new(@"/\*.*?\*/|//[^\n]*", RegexOptions.Singleline);

    [Test]
    [Description("Keeps the Designer-save preservation rule in the page save-lifecycle guide.")]
    public void OverviewGuidance_ShouldStateWhatSurvives_WhenTheDesignerSavesAPage()
    {
        // Arrange
        string guidePath = Path.Combine(FindRepositoryRoot(), "guidance", "mcp", "guides", "pages",
            "modification", "overview.md");

        // Act
        string guidance = File.ReadAllText(guidePath);

        // Assert
        guidance.Should().Contain("What survives an Interface Designer save (web pages)",
                because: "routing and shared-client-logic point at this section by name")
            .And.Contain("Kept verbatim: the contents of `SCHEMA_DEPS`, `SCHEMA_ARGS`, `SCHEMA_HANDLERS`, `SCHEMA_CONVERTERS` and `SCHEMA_VALIDATORS`.",
                because: "agents must know which regions a Designer save keeps")
            .And.Contain("Deleted: everything else in the `define` factory body",
                because: "code placed in the factory body is lost on the next Designer save")
            .And.Contain("Do not move a removed helper back above the `return` statement",
                because: "re-adding the helper there only loses it again on the next save")
            .And.Contain("https://github.com/Advance-Technologies-Foundation/clio/issues/1697",
                because: "the deletion claim must keep its live-stand evidence");
    }

    [Test]
    [Description("Routes every page helper, not only a shared one, to the client-unit module guide.")]
    public void SharedClientLogic_ShouldOwnSinglePageHelpers_WhenRoutingPageCode()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guidePath = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "page-schema",
            "shared-client-logic.md");
        string routingPath = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "routing.md");
        string sourcePath = Path.Combine(repositoryRoot, "bundle-source.json");

        // Act
        string guidance = File.ReadAllText(guidePath);
        string routing = File.ReadAllText(routingPath);
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(sourcePath));
        string description = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "shared-client-logic")
            .GetProperty("description")
            .GetString()!;

        // Assert
        guidance.Should().Contain("whether one page uses them or several",
            because: "a helper used by one page is deleted by a Designer save just like a shared one");
        routing.Should().Contain("for one page or several), or a page broke after an Interface Designer save",
            because: "every page edit that adds code outside a handler must reach shared-client-logic");
        description.Should().Contain("for one page or many",
            because: "resource discovery metadata must advertise the widened scope");
    }

    [Test]
    [Description("Fails when a published page example puts code in the factory body that a Designer save deletes.")]
    public void PageExamples_ShouldKeepTheFactoryBodyEmpty_WhenTheyDefineAPage()
    {
        // Arrange
        string guidanceRoot = Path.Combine(FindRepositoryRoot(), "guidance");
        List<string> offenders = [];
        int checkedFactories = 0;

        // Act
        foreach (string path in Directory.EnumerateFiles(guidanceRoot, "*.md", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(path);
            foreach (Match opening in PageFactoryOpening.Matches(text))
            {
                string afterOpening = text[(opening.Index + opening.Length)..];
                string withoutComments = Comments.Replace(afterOpening, comment => new string(' ', comment.Length));
                Match factoryReturn = FactoryReturn.Match(withoutComments);
                if (!factoryReturn.Success)
                {
                    continue;
                }
                if (afterOpening[..factoryReturn.Index].Contains("/**SCHEMA_", StringComparison.Ordinal))
                {
                    // An excerpt that shows only a marker region, not the factory body.
                    continue;
                }
                checkedFactories++;
                string factoryBody = withoutComments[..factoryReturn.Index].Trim();
                if (factoryBody.Length > 0)
                {
                    offenders.Add($"{Path.GetRelativePath(guidanceRoot, path)}: {factoryBody.Split('\n')[0]}");
                }
            }
        }

        // Assert
        checkedFactories.Should().BeGreaterThan(5,
            because: "the scan must reach the published page examples, or an empty offender list proves nothing");
        offenders.Should().BeEmpty(
            because: "an Interface Designer save deletes factory-body code, so an example that teaches it breaks the page");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}

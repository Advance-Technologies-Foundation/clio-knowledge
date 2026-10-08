using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the long-tail executor guidance (ENG-101352): destructive long-tail tools run through
/// clio-run, and clio-run-destructive is mentioned only as its deprecated alias.
/// </summary>
[TestFixture]
public sealed class ClioRunExecutorGuidanceTests
{
    [Test]
    [Description("The DataForge guide runs the destructive DataForge tools through clio-run and keeps the note for older clio builds.")]
    public void DataForgeGuide_ShouldRunDestructiveToolsThroughClioRun()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Normalize(File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "integration", "dataforge-orchestration.md")));

        // Assert
        guide.Should().Contain(
            "Invoke the destructive DataForge tools `dataforge-initialize` and `dataforge-update` through the same `clio-run` executor",
            because: "clio-run dispatches destructive long-tail tools; clio-run-destructive is only a deprecated alias");
        guide.Should().Contain("clio builds older than 8.1.0.72 reject destructive tools in `clio-run`",
            because: "the bundle still declares clio 8.1.0.x compatible, and clio-run accepts destructive tools only from 8.1.0.72");
    }

    [Test]
    [Description("The agent-execution guide sends long-tail tools to clio-run only.")]
    public void AgentExecutionGuide_ShouldInvokeLongTailToolsViaClioRun()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Normalize(File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "operations", "agent-execution.md")));

        // Assert
        guide.Should().Contain("invoke them via `clio-run` — do not treat their absence as missing",
            because: "long-tail tools, destructive ones included, are invoked through clio-run");
        guide.Should().NotContain("clio-run-destructive",
            because: "the deprecated alias must not come back as a recommended executor");
    }

    [Test]
    [Description("Outside the oracle fixtures, guidance names clio-run-destructive only as the deprecated alias of clio-run.")]
    public void Guidance_ShouldMentionClioRunDestructiveOnlyAsDeprecatedAlias()
    {
        // Arrange
        string guidanceRoot = Path.Combine(ProcessGuideSet.FindRepositoryRoot(), "guidance");

        // Act
        string[] offending = Directory.EnumerateFiles(guidanceRoot, "*.md", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadAllLines(path)
                .Where(line => line.Contains("clio-run-destructive", StringComparison.Ordinal)
                    && !line.Contains("deprecated alias", StringComparison.Ordinal))
                .Select(line => $"{Path.GetRelativePath(guidanceRoot, path)}: {line.Trim()}"))
            .ToArray();

        // Assert
        offending.Should().BeEmpty(
            because: "clio-run-destructive is a deprecated alias of clio-run and must not be recommended on its own");
    }

    // Collapses every whitespace run (line breaks of either style, wrap indentation) to one space,
    // so the pins survive a CRLF checkout or a meaning-preserving rewrap of the guide.
    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");
}

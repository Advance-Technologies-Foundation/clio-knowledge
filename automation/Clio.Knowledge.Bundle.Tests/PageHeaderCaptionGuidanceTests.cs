using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the rule that a home page and a dashboard are created with a human-readable <c>caption</c>.
/// </summary>
/// <remarks>
/// ENG-107757: the caption of a <c>BaseHomePage</c> or <c>BaseDashboardTemplate</c> page is the title in its
/// header. The creation steps used to list only schema-name, template and package-name, and agents following
/// them omitted <c>caption</c> in 3 of 5 ADAC runs, so the header showed the schema name; clio now refuses such
/// a call on the MCP path. The tests assert key tokens inside the creation step, so the steps can be reworded
/// while the caption cannot drop out of them again.
/// </remarks>
[TestFixture]
public sealed class PageHeaderCaptionGuidanceTests
{
    [TestCase("guidance/mcp/guides/dashboards/home-page.md", "`template` = `BaseHomePage`")]
    [TestCase("guidance/mcp/guides/dashboards/creation.md", "`template` = `BaseDashboardTemplate`")]
    [Description("The create-page step of the home-page and dashboard-creation guides tells the agent to pass a human-readable caption, not the schema name.")]
    public void CreationStep_ShouldPassHumanReadableCaption(string relativePath, string stepAnchor)
    {
        // Arrange
        string guide = Regex.Replace(
            ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath), @"\s+", " ");
        int stepStart = guide.IndexOf(stepAnchor, StringComparison.Ordinal);

        // Act
        string step = stepStart < 0 ? string.Empty : guide.Substring(stepStart, Math.Min(500, guide.Length - stepStart));

        // Assert
        stepStart.Should().BeGreaterThanOrEqualTo(0,
            because: $"{relativePath} must keep its create-page step for this template");
        step.Should().Contain("a `caption`",
            because: "the step must list caption among the create-page arguments, or the header shows the schema name");
        step.Should().Contain("never the schema name",
            because: "an agent that copies the schema name into caption reproduces the defect");
        step.Should().Contain("refuses",
            because: "the agent must expect current clio to refuse a call without caption");
    }

    [Test]
    [Description("The page-creation guide marks caption as required for the two header templates and says OData on SysSchema.Caption does not change the header.")]
    public void PageCreationInputs_ShouldMarkCaptionRequiredForHeaderTemplates()
    {
        // Arrange
        string guide = Regex.Replace(
            ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), "guidance/mcp/guides/pages/creation.md"),
            @"\s+", " ");

        // Act
        bool required = guide.Contains("REQUIRED for `BaseHomePage` and `BaseDashboardTemplate`", StringComparison.Ordinal);
        bool odataWarning = guide.Contains("`SysSchema.Caption` through OData afterwards does not change the page header",
            StringComparison.Ordinal);

        // Assert
        required.Should().BeTrue(because: "the inputs list must say which templates need a caption");
        odataWarning.Should().BeTrue(
            because: "agents tried odata-update on SysSchema.Caption and reported a header they had not changed");
    }
}

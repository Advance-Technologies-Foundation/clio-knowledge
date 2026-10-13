using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// ENG-102116: <c>install-process-builder</c> installs CrtProcessBuilder INTO the environment, which builds its
/// configuration and restarts the site for every connected user. <c>core-rules</c> owns the rule that the user
/// approves EVERY such call and that a process tool's refusal (or the hint it carries) is not that approval.
/// </summary>
/// <remarks>
/// Every process article that tells an agent to run the install must defer to that rule where it does so: an
/// agent reads the element article in the middle of a build, and an unconditional "run it" there is the reading
/// that turns a refusal into permission. The articles below are the ones that prescribed the install as a
/// remedy; articles that only report what a refusal names are not listed.
/// </remarks>
[TestFixture]
public sealed class ProcessBuilderInstallConsentGuidanceTests
{
    private const string CoreRulesPath = "guidance/mcp/guides/core-rules.md";

    private static readonly string[] ArticlesPrescribingTheInstall =
    [
        "guidance/mcp/guides/processes/branch-conditions-details.md",
        "guidance/mcp/guides/processes/formulas-details.md",
        "guidance/mcp/guides/processes/perform-task.md",
        "guidance/mcp/guides/processes/send-email-template.md",
        "guidance/mcp/guides/processes/sub-process.md",
        "guidance/mcp/guides/processes/task-category.md"
    ];

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    [Test]
    [Description("core-rules owns the consent rule for install-process-builder: ask before EVERY call, and a refusal or its hint is never the approval.")]
    public void CoreRules_ShouldRequireApprovalBeforeEveryInstallProcessBuilderCall()
    {
        // Arrange
        string text = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), CoreRulesPath));

        // Act
        string rule = Regex.Match(text, @"- install-process-builder installs the CrtProcessBuilder package INTO the environment.+?themselves\.",
            RegexOptions.None, RegexTimeout).Value;

        // Assert
        rule.Should().NotBeEmpty(
            because: "core-rules is read before every operation, so it is the one place the consent rule cannot be missed");
        rule.Should().Contain("never install on the strength of a refusal or its hint",
            because: "the refusal names the install as the remedy, and an agent on a shared stand read that as permission");
        rule.Should().Contain("Before EVERY call",
            because: "an earlier answer is not standing consent - the same rule compile-creatio follows");
        rule.Should().Contain("clio install-process-builder -e <ENV>",
            because: "a user who postpones needs the command to run it themselves later");
    }

    [Test]
    [Description("Every process article that prescribes install-process-builder as a remedy ties it to the user's approval in the same place.")]
    public void ArticlesPrescribingTheInstall_ShouldTieItToTheUsersApproval()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string[] unconditional = [.. ArticlesPrescribingTheInstall
            .Where(path => !Regex.IsMatch(Collapse(ProcessGuideSet.Read(repositoryRoot, path)),
                @"install-process-builder.{0,40}approv|approv.{0,40}install-process-builder",
                RegexOptions.IgnoreCase, RegexTimeout))];

        // Assert
        unconditional.Should().BeEmpty(
            because: "an element article read mid-build that says only 'run install-process-builder' undoes the "
                + "core-rules consent rule for the agent that reads it; found: " + string.Join(", ", unconditional));
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ", RegexOptions.None, RegexTimeout);
}

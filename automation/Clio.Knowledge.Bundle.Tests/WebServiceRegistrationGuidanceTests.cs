using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the safety rules of the <c>web-service-registration</c> guide: credentials only through system settings or an
/// OAuth application, the usage guard acknowledged only on the user's yes, and setMethod as a full replacement. An
/// edit that drops one of them lets an agent store a key as a constant, break a calling process, or erase a
/// setting-bound key without a refusal it would notice.
/// </summary>
[TestFixture]
public sealed class WebServiceRegistrationGuidanceTests
{
    private const string ArticlePath = "guidance/mcp/guides/integration/web-service-registration.md";

    [Test]
    [Description("The guide states the credential rule: no credential value passes through the agent, keys are system-setting references, and missing settings are created empty for the user to fill.")]
    public void Guide_ShouldStateTheCredentialRule()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "Credential rule (MUST)",
            "No credential value ever passes through the agent.",
            "MUST NOT get `default-value`. Give it `default-sys-setting` with a system-setting code instead.",
            "Create the setting first with `create-sys-setting` and no value",
            "Do not try to read them another way."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "an agent that has not read the rule stores an API key as a constant every reader of the schema sees");
        }
    }

    [Test]
    [Description("The guide keeps acknowledge-usages for the user's explicit yes and says setMethod replaces kept parameters.")]
    public void Guide_ShouldStateTheUsageGuardAndTheReplacement()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "only on their explicit yes, repeat the call with `acknowledge-usages`",
            "Never fill `acknowledge-usages` on your own.",
            "`setMethod` REPLACES the method. Every parameter it keeps must be sent complete, as `describe-web-service` returned it"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "without it an agent breaks a calling process or erases a setting-bound key with no refusal");
        }
    }

    [Test]
    [Description("The tools are non-resident: the guide sends the agent through get-tool-contract and clio-run, and routing reaches the guide from the process web-service route.")]
    public void Guide_ShouldBeReachableAndCallable()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string routing = ReadCollapsed("guidance/mcp/guides/routing.md");

        // Assert
        guide.Should().Contain("They are NOT in `tools/list`",
            because: "an agent that calls a non-resident tool directly finds nothing and wrongly stops");
        routing.Should().Contain("register it first by name=web-service-registration",
            because: "a process task with no registered service must reach the registration tools, not stop at Studio");
    }

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath), @"\s+", " ");
}

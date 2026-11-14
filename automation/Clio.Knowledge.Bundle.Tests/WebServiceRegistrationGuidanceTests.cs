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

    [Test]
    [Description("The SOAP guide keeps the SOAP action from the WSDL or the user, keeps soap-header parameters on system settings, guards setNamespaces and setType, and routing reaches it.")]
    public void SoapGuide_ShouldStateItsSafetyRules()
    {
        // Arrange
        string soap = ReadCollapsed("guidance/mcp/guides/integration/web-service-soap.md");
        string guide = ReadCollapsed(ArticlePath);
        string routing = ReadCollapsed("guidance/mcp/guides/routing.md");

        // Act
        string[] clauses =
        [
            "take it from the WSDL (the binding operation's `soapAction`) or ask the user; NEVER make one up.",
            "Pass the whole notice on, risk sentence included",
            "A soap-header parameter takes no `default-value`",
            "NEVER a JSONPath",
            "`removeMethod`, with the processes listed"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            soap.Should().Contain(clause,
                because: "without it an agent invents a SOAP action, stores a WS-Security secret as a constant, or switches a called service unasked");
        }
        guide.Should().NotContain("a SOAP service stays in Studio",
            because: "SOAP services are registered and changed by the tools now");
        guide.Should().Contain("get-guidance name=web-service-soap",
            because: "the owner guide must send a SOAP task to the SOAP article");
        routing.Should().Contain("for a SOAP service (SOAP 1.1 / 1.2, a WSDL to read with read-wsdl, namespaces, message element, SOAP action, WS-Security header) also name=web-service-soap",
            because: "a SOAP registration task, and a WSDL the user gives, must reach the SOAP article from routing");
    }

    [Test]
    [Description("The SOAP guide sends a WSDL to read-wsdl, registers its output as it is, leaves the code choice and the test call to the user, and keeps the fallback for a clio without the tool.")]
    public void SoapGuide_ShouldSendAWsdlToReadWsdl()
    {
        // Arrange
        string soap = ReadCollapsed("guidance/mcp/guides/integration/web-service-soap.md");

        // Act
        string[] clauses =
        [
            "call `read-wsdl` first and register what it returns",
            "never hand-build what it returned",
            "does not list `read-wsdl` has no WSDL reading",
            "Register a port's `arguments` with `register-web-service` as they are, adding only `package-name`",
            "ask the user which way to go and do not pick a code yourself",
            "the confirmation rule of `web-service-registration` applies to every SOAP method"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            soap.Should().Contain(clause,
                because: "without it an agent builds a SOAP service by hand from a WSDL, rewrites what read-wsdl returned, picks a code over a taken one, or calls a SOAP method unasked");
        }
        soap.Should().NotContain("There is no WSDL import",
            because: "read-wsdl reads a WSDL now");
    }

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath), @"\s+", " ");
}

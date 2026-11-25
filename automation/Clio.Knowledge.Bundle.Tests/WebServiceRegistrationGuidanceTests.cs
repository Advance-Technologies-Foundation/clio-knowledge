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
    private const string MaintenancePath = "guidance/mcp/guides/integration/web-service-maintenance.md";
    private const string SoapDetailsPath = "guidance/mcp/guides/integration/web-service-soap-details.md";

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
            "Do not try to read them another way.",
            "a constant whose value looks like a key is refused too, and describe hides one Studio stored.",
            "the tool accepting a value does not make it public: a key, password or PIN the user gives, short or not, still goes into a system setting."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "an agent that has not read the rule stores an API key as a constant every reader of the schema sees");
        }
    }

    [Test]
    [Description("The guide lets a plain header constant stay a constant and tells an old clio's blanket refusal apart from the key-like refusal.")]
    public void Guide_ShouldAllowPlainHeaderConstants()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "`Accept-Language: en-US`, `X-Api-Version: 2`, `Content-Type: application/xml`",
            "A refusal that says \"refused on a header, cookie or credential-like parameter\" comes from a clio released before this rule"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "without it an agent pushes a harmless value such as a language into a system setting the user must fill");
        }
    }

    [Test]
    [Description("The maintenance guide keeps acknowledge-usages for the user's explicit yes and says setMethod replaces kept parameters.")]
    public void Guide_ShouldStateTheUsageGuardAndTheReplacement()
    {
        // Arrange
        string guide = ReadCollapsed(MaintenancePath);

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
    [Description("The SOAP guide keeps the SOAP action from the WSDL or the user, keeps credential-like or key-like soap-header values on system settings, guards setNamespaces and setType, and routing reaches it.")]
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
            "or with a value that looks like a key it takes no `default-value`: give it `default-sys-setting`",
            "every element of the path counts, so `wsse:Security/wsse:UsernameToken/wsse:Username` is one",
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
            "the confirmation rule of `web-service-maintenance` applies to every SOAP method"
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

    [Test]
    [Description("The maintenance guide guards a real change of the base URI or the authentication like a changed method, leaves setRetryCount unguarded and says a method without use-auth sends no authentication; the registration guide says Digest goes out as Basic.")]
    public void Guide_ShouldGuardEveryCallChangesAndStatePerMethodAuthentication()
    {
        // Arrange
        string guide = ReadCollapsed(MaintenancePath);
        string registration = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "`setAuth` that really changes the authentication changes every call of the methods with `use-auth: true`",
            "The tool compares the service before and after the whole call",
            "never set `use-auth` on your own, not even in the same call as `setAuth`",
            "`setRetryCount` is never guarded.",
            "a method with `use-auth: false` sends no authentication at all"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "without it an agent repoints or re-authenticates a service a process calls unasked, or expects a new authentication to reach every method");
        }
        registration.Should().Contain("The platform sends Digest as a preemptive `Authorization: Basic` header",
            because: "the registration guide owns the authentication types");
    }

    [Test]
    [Description("The SOAP guide sends the agent to the SOAP 1.1 port a read-wsdl warning names, explains why WCF refuses the platform's SOAP 1.2 and WS-Addressing calls, and gives the measured workarounds only with the user's agreement.")]
    public void SoapGuide_ShouldSendTheAgentToACallablePort()
    {
        // Arrange
        string soap = ReadCollapsed("guidance/mcp/guides/integration/web-service-soap.md");

        // Act
        string[] clauses =
        [
            "Register the port a warning names to register instead (\"Register port ... instead\"), else the `studio-pick` port",
            "Studio's choice, not a recommendation",
            "`application/soap+xml;charset=utf-8;action=\"<the soap-action>\"`",
            "two `soap-header` body parameters `wsa:Action` (the soap-action) and `wsa:To` (the base URI)",
            "Do this only with the user's agreement, and test a call.",
            "A port that requires WS-Security message security cannot be called at all; ask the API owner for a plain endpoint.",
            "never register first and warn afterwards.",
            "keep the SOAP 1.2 port and switch only if a test call fails because the action or operation is not found",
            "Switch the registered service with `modify-web-service` `setType` soap11"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            soap.Should().Contain(clause,
                because: "without it an agent registers Studio's SOAP 1.2 pick of a WCF service and every call fails with a SOAP fault");
        }
        ReadCollapsed(SoapDetailsPath).Should().Contain("never as the `action` parameter of `Content-Type`",
            because: "the details article says why the platform cannot call a WCF SOAP 1.2 port");
    }

    [Test]
    [Description("The SOAP guide has the agent pass the code the user named for a new service to read-wsdl as code, keeps service-code for an existing service, and reads again with code when the code is taken.")]
    public void SoapGuide_ShouldPassANewServiceCodeAsCode()
    {
        // Arrange
        string soap = ReadCollapsed("guidance/mcp/guides/integration/web-service-soap.md");

        // Act
        string[] clauses =
        [
            "When the user named the code of a NEW service, pass it as `code`",
            "`service-code` is ONLY for an existing SOAP service the operations go into",
            "(read again with that `code`)"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            soap.Should().Contain(clause,
                because: "without it an agent passes a new service's code as service-code, fails, and edits the arguments it should register as they are");
        }
    }

    [Test]
    [Description("The maintenance guide says run-process withholds the text results of a process that calls a web service or reads a SecureText setting, and that the agent never copies a response or a key elsewhere to read it.")]
    public void Guide_ShouldStateThatRunProcessWithholdsCredentialResults()
    {
        // Arrange
        string guide = ReadCollapsed(MaintenancePath);

        // Act
        string[] clauses =
        [
            "in a process that calls a web service or reads a SecureText setting",
            "`run-process` returns every text, Guid or collection result as `[withheld]`, named in `withheldResultParameters`",
            "never copy a response or a key elsewhere to read it"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "an agent that has not read the rule takes [withheld] for a broken run, or works around it and puts the key into its context");
        }
    }

    [Test]
    [Description("The registration guide sends test calls and changes to web-service-maintenance, routing reaches it, and the maintenance guide restates the credential MUST where its changes are made.")]
    public void Guide_ShouldSendTestCallsAndChangesToTheMaintenanceGuide()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string maintenance = ReadCollapsed(MaintenancePath);
        string routing = ReadCollapsed("guidance/mcp/guides/routing.md");

        // Assert
        guide.Should().Contain("are owned by `get-guidance name=web-service-maintenance`: read it before either.",
            because: "a test call or a change needs the rules that moved to the maintenance guide");
        routing.Should().Contain("test-call or change it -> name=web-service-maintenance",
            because: "a change task must reach the maintenance guide from routing");
        maintenance.Should().Contain("MUST: a credential is a system-setting reference, never a value, and every setting a call references anew must exist first",
            because: "a split never separates a write from its preconditions");
        maintenance.Should().Contain("ALWAYS tell the user it sends a real request that can change data in that API and wait for their explicit yes",
            because: "the test call's confirmation rule travels with the test call");
    }

    [Test]
    [Description("The guides describe the narrowed replies: methods-not-shown read with methods, the list-web-services filter, and read-wsdl's one port in full.")]
    public void Guides_ShouldDescribeTheNarrowedReplies()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);
        string soap = ReadCollapsed("guidance/mcp/guides/integration/web-service-soap.md");

        // Assert
        guide.Should().Contain("in `methods-not-shown`: read those with `methods` before you use or send one back",
            because: "a method listed by code only was not shown, and sending a guess back would replace it");
        guide.Should().Contain("`filter` keeps the services whose code, caption or package contains a text",
            because: "a long service list is narrowed with filter");
        soap.Should().Contain("only the port to register (below) comes with `arguments`; the others list their `operations` names",
            because: "the agent must read another port with port instead of registering names");
    }

    private static string ReadCollapsed(string relativePath) =>
        Regex.Replace(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath), @"\s+", " ");
}

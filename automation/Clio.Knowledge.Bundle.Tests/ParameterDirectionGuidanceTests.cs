using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the parameter-direction guidance: the rule in <c>process-parameters</c> (choose the direction by who uses the
/// value, state it, a helper is Internal), the builder's default switch with its conditions and the caller rule for
/// an existing parameter in <c>process-parameters-details</c>, the examples that declare a parameter, and the
/// routing card's pointer. TRIM ALARMS: each clause is a condition the package applies or a step that protects a
/// caller, so a reworded or dropped clause fails here.
/// </summary>
[TestFixture]
public sealed class ParameterDirectionGuidanceTests
{
    private const string ParametersPath = "guidance/mcp/guides/processes/parameters.md";
    private const string ParametersDetailsPath = "guidance/mcp/guides/processes/parameters-details.md";
    private const string FilesPath = "guidance/mcp/guides/processes/files.md";
    private const string FilesReportPath = "guidance/mcp/guides/processes/files-report.md";
    private const string DigestPath = "guidance/mcp/guides/processes/digest.md";

    /// <summary>The clio release that ships the switch and the package it bundles, as both articles state them.</summary>
    private static readonly Regex SwitchBoundary = new(
        @"clio (?<clio>\d+\.\d+\.\d+\.\d+)(?: and later)? \(its bundled CrtProcessBuilder (?<package>\d+\.\d+\.\d+\.\d+)\)");

    [Test]
    [Description("process-parameters states the direction rule: choose by who uses the value, state it on every parameter but a mergeFrom one, Internal for every helper, and the four directions as the process signature.")]
    public void Parameters_ShouldStateTheDirectionRule()
    {
        // Arrange
        string parameters = ReadCollapsed(ParametersPath);

        // Act
        string[] clauses =
        [
            "CHOOSE THE DIRECTION BY WHO USES THE VALUE, and state it on every parameter you declare except a `mergeFrom` one, where you leave it out (`process-multi-start`).",
            "a Sub-process element that calls the process maps its In and Variable parameters and reads its Out and Variable ones, and a Creatio AI skill built on the process exposes every parameter but an Internal one.",
            "`run-process` does not check direction - it accepts a value for an Internal parameter too",
            "`Internal` — the value only passes between elements of THIS process",
            "a `mergeFrom` parameter is always `Internal`",
            "This is the right choice for every helper parameter.",
            "`Variable` — a caller passes it in AND reads it back. Use it only when both are true."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            parameters.Should().Contain(clause,
                because: "the direction is the process signature, and an agent that does not read this rule declares a helper a caller is offered");
        }
    }

    [Test]
    [Description("process-parameters states the default and its switch: Out for a generic Collection is the builder's choice, not the designer's; from the clio boundary a new, element-filled, non-collection parameter left without a direction is saved Internal with a warning whose remedy is Out or Variable; a stated direction, a FileCollection and an existing parameter are never switched; and an older package stays silent.")]
    public void Parameters_ShouldStateTheDefaultAndTheSwitch()
    {
        // Arrange
        string parameters = ReadCollapsed(ParametersPath);

        // Act
        Match boundary = SwitchBoundary.Match(parameters);

        // Assert
        parameters.Should().Contain("Omitting the direction gives Variable, and Out for a generic Collection: that Out is this builder's choice, as the designer's plain Add gives every type Variable, a collection included.",
            because: "the designer's plain Add makes every type Variable; reading Out as the designer's choice would be wrong");
        boundary.Success.Should().BeTrue(
            because: "a version boundary is stated as the clio release that ships the switch and the package it bundles");
        parameters.Should().Contain("a top-level parameter, not a collection, that the request creates with no direction and an element of the process fills is saved as Internal instead, with a warning naming it",
            because: "the agent must recognise the switch and its notice, and know a collection is never switched");
        parameters.Should().Contain("a report, not an error: if the process returns the value to a caller, set Out (or Variable when a caller also passes it in).",
            because: "the warning names the way back, and an agent must not treat it as a failure");
        parameters.Should().Contain("A stated direction, a FileCollection and a parameter that existed before the request are never switched.",
            because: "the switch applies to an unstated direction on a new parameter only");
        parameters.Should().Contain("The exact conditions, what the response says in each case, and the measurements: `process-parameters-details`.",
            because: "the conditions live in the details article, and the owner of the rule must say where");
        parameters.Should().Contain("An older CrtProcessBuilder neither switches nor warns, so silence proves nothing: state Internal yourself.",
            because: "silence after a build on an older package must not read as 'no helper is Variable'");
    }

    [Test]
    [Description("process-parameters scopes 'every reference survives' to this process and, before the direction of an existing parameter changes, requires its callers to be found or the user asked, and every caller re-synchronized afterwards.")]
    public void Parameters_ShouldGuardTheDirectionOfAnExistingParameter()
    {
        // Arrange
        string parameters = ReadCollapsed(ParametersPath);

        // Act
        string[] clauses =
        [
            "the UId and every reference INSIDE this process survive, but once it is no longer In or Variable a caller's Sub-process mapping onto it is cleared on that caller's next synchronization, with no error (`process-sub-process`).",
            "So before you change the direction of a parameter that existed before this request you MUST find its callers (execute-esq over VwProcessLib, then describe each: `process-parameters-details`) or ask the user, and afterwards re-synchronize every caller."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            parameters.Should().Contain(clause,
                because: "the change clears a caller's mapping silently, so the precondition must sit next to the instruction that changes the direction");
        }
    }

    [Test]
    [Description("process-parameters-details carries the switch in full: the three tools, the later-setParameter exception, what fills a parameter, the collection exclusion, the remedy, the advised-once stated Variable, the FileCollection that is never advised, the report on an existing parameter with the caller warning, the steps before a change, and the measurement.")]
    public void Details_ShouldStateTheSwitchInFullAndTheCallerSteps()
    {
        // Arrange
        string details = ReadCollapsed(ParametersDetailsPath);

        // Act
        (string Clause, string Because)[] rules =
        [
            ("A top-level parameter that a create-business-process, modify-business-process or modify-business-process-as-new-version request creates with no direction",
                "the new-version path runs the same pipeline and switches too"),
            ("(and that no later setParameter in that request gives one)",
                "a direction stated by a later operation of the same request is a stated direction"),
            ("which an element of the process fills (an element output mapped onto it, an expression that reads an element, or a Formula element's result), and which is not a collection, is saved as Internal.",
                "what counts as filled and the collection exclusion are the switch's conditions"),
            ("if the process returns the value to a caller, set Out (or Variable when a caller also passes it in).",
                "the warning names the way back"),
            ("a stated Variable on a parameter the request creates and an element fills is kept and advised once, in that request",
                "a stated direction is the author's choice and is never switched"),
            ("a FileCollection, which keeps its Variable default and is never advised",
                "the FileCollection default is the builder's own choice"),
            ("(an edit that keeps the Formula's target does not count)",
                "only a Formula whose result the request points at the parameter makes the request judge it"),
            ("that changing its direction clears a calling Sub-process element's mapping for it on that caller's next synchronization with no error, and that its callers must be found (or the user asked) first. It is a report, never an order to switch.",
                "the advice on an existing parameter must not lead an agent to switch one other processes call"),
            ("find its callers - `execute-esq` over `VwProcessLib` for the candidates, then `describe` each and look for a `subProcess.process` naming this process",
                "nothing lists a process's callers, so the steps must be spelled out"),
            ("After the change, re-synchronize every caller with `subProcess: {resync: true}` and read its notices.",
                "a caller left unsynchronized keeps a contract the callee no longer has"),
            ("ONLY WHAT A REQUEST TOUCHES IS JUDGED, so no warning after a modify does not mean the process has no Variable helpers.",
                "silence after a modify proves nothing about parameters the request did not reach")
        ];

        // Assert
        foreach ((string clause, string because) in rules)
        {
            details.Should().Contain(clause, because: because);
        }
        details.Should().MatchRegex(
            @"Measured on Creatio 10\.2\.522 \(\.NET Framework, MSSQL\) with CrtProcessBuilder \d+\.\d+\.\d+\.\d+, \d{4}-\d{2}-\d{2}:",
            because: "the switch is a behavioral claim, and its evidence names the stand, the package and the date");
    }

    [Test]
    [Description("process-parameters and process-parameters-details state the same clio release and bundled package for the switch, so correcting the boundary in one article and not the other fails.")]
    public void SwitchBoundary_ShouldBeTheSameInBothArticles()
    {
        // Arrange
        string parameters = ReadCollapsed(ParametersPath);
        string details = ReadCollapsed(ParametersDetailsPath);

        // Act
        Match inParameters = SwitchBoundary.Match(parameters);
        Match inDetails = SwitchBoundary.Match(details);

        // Assert
        inParameters.Success.Should().BeTrue(because: "anti-vacuity: process-parameters states the boundary");
        inDetails.Success.Should().BeTrue(because: "anti-vacuity: process-parameters-details states the boundary");
        inDetails.Groups["clio"].Value.Should().Be(inParameters.Groups["clio"].Value,
            because: "the two articles describe one switch, shipped by one clio release");
        inDetails.Groups["package"].Value.Should().Be(inParameters.Groups["package"].Value,
            because: "the two articles describe one switch, shipped by one package version");
    }

    [Test]
    [Description("Every example in the process articles that declares a parameter states its direction, except a mergeFrom parameter, which leaves it out as process-parameters says: an agent copies the examples, and one without a direction teaches the default.")]
    public void EveryParameterExample_ShouldStateItsDirection()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        (string Path, string Declaration)[] declarations = [.. ProcessGuideSet.Declared(repositoryRoot)
            .SelectMany(article => Regex.Matches(Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)),
                    @"\{ ?""name"": ?""[A-Za-z]+Parameter""[^{}]*\}")
                .Select(match => (article.SourcePath, match.Value)))];
        string[] withoutDirection = [.. declarations
            .Where(item => !item.Declaration.Contains("\"direction\"", StringComparison.Ordinal)
                && !item.Declaration.Contains("\"mergeFrom\"", StringComparison.Ordinal))
            .Select(item => $"{item.Path}: {item.Declaration}")];

        // Assert
        declarations.Should().HaveCountGreaterThanOrEqualTo(6,
            because: "anti-vacuity: the process articles declare parameters in their examples");
        withoutDirection.Should().BeEmpty(
            because: "the rule is to state a direction on every parameter but a mergeFrom one. Found: "
                + string.Join("; ", withoutDirection));
    }

    [Test]
    [Description("The examples that declare a helper declare it Internal and the ones that take a caller's value declare it In: the Formula-per-branch shape in process-parameters-details, recipe 1 and the type example of process-files, and the Add data descriptor of process-files-report.")]
    public void Examples_ShouldDeclareTheRightDirection()
    {
        // Arrange
        string details = ReadCollapsed(ParametersDetailsPath);
        string files = ReadCollapsed(FilesPath);
        string filesReport = ReadCollapsed(FilesReportPath);

        // Act
        string helperFileCollection = "{\"name\": \"DocumentsParameter\", \"type\": \"FileCollection\", \"direction\": \"Internal\"}";

        // Assert
        details.Should().Contain("{ \"name\": \"DaysLeftParameter\", \"type\": \"Integer\", \"direction\": \"Internal\"",
            because: "the shared parameter each branch sets is a helper no caller passes or reads");
        files.Should().Contain(helperFileCollection,
            because: "recipe 1 keeps files an element produced for later elements, and the builder leaves a FileCollection Variable");
        files.Should().Contain("{\"name\": \"ContractFileParameter\", \"type\": \"File\", \"direction\": \"In\"}",
            because: "recipe 3's callee takes the file from its caller");
        files.Should().Contain("An explicit direction always wins. State one; a helper is Internal.",
            because: "the Direction bullet of process-files must not contradict process-parameters");
        filesReport.Should().Contain("{\"name\": \"SenderDomainParameter\", \"type\": \"Lookup\", \"direction\": \"In\"",
            because: "the caller passes the sender domain in");
        filesReport.Should().Contain("{\"name\": \"SiteUrlParameter\", \"type\": \"Text\", \"direction\": \"In\"}",
            because: "the caller passes the site URL in");
    }

    [Test]
    [Description("The process routing card tells a build that does not open process-parameters to state each direction and to make a helper Internal.")]
    public void Digest_ShouldRouteTheDirectionRule()
    {
        // Arrange
        string digest = ReadCollapsed(DigestPath);

        // Act
        bool routed = digest.Contains("`process-parameters` (state directions; helper `Internal`)", StringComparison.Ordinal);

        // Assert
        routed.Should().BeTrue(
            because: "an ordinary build reads the card instead of the owners, and a helper left without a direction lands in the signature on an older package");
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

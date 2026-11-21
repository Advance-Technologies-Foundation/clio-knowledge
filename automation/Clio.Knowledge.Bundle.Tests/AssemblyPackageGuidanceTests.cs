using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the assembly-package guide: the rules that were measured on a stand and that an agent would
/// otherwise get wrong by following the platform's folklore (re-set the type after every push, a
/// <c>.svc</c> per service, <c>async void Execute</c>, remove-before-create scheduling).
/// </summary>
[TestFixture]
public sealed class AssemblyPackageGuidanceTests
{
    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([ProcessGuideSet.FindRepositoryRoot(), .. path]));

    [Test]
    [Description("The guide keeps the measured registration, compile and delivery rules of an assembly package.")]
    public void AssemblyPackageGuide_ShouldKeepTheMeasuredRegistrationAndCompileRules()
    {
        // Arrange
        string[] requiredFacts =
        [
            "Keep `\"Type\": 1` and `\"ProjectPath\"` in it",
            "After every install MUST read both back",
            "so run no package compile; run only ONE full compile",
            "MUST NOT set `Type` or `ProjectPath` with SQL, OData or DataService writes",
            "builds an assembly package ONLY when it has compilable content",
            "`skipped (no compilable schemas)",
            "clio add-schema <Package>Marker -t source-code",
            "also needs ONE full compile",
            "The shipped DLL is then the running code",
            "Use one of the two, never both",
            "`Assembly.Location` is an empty string",
            "read `process-timer-start`",
            "MUST NOT be `async void`",
            "Make `Execute` total",
            "`[DefaultBinding(typeof(IJobExecutor))]` is not required",
            "replace the job with the same key",
            "Make `Execute` idempotent",
            "MUST NOT carry `Files/Bin/<Package>.dll`",
            "names every call's job with a new GUID",
            "6 fields with seconds first",
            "`Execute` MUST check rights and validate its parameters itself"
        ];

        // Act
        string guide = System.Text.RegularExpressions.Regex.Replace(
            Read("guidance", "mcp", "guides", "backend", "assembly-package.md"), @"\s+", " ");

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "each of these was measured on a stand or read in platform source and corrects a claim agents meet in the field");
        guide.Should().NotContain("after every push",
            because: "re-setting the type after every push is the refuted recipe; the descriptor carries the type");
    }

    [Test]
    [Description("Routing and the owning guides point to the assembly-package guide instead of restating it.")]
    public void AssemblyPackageGuide_ShouldBeReachableFromRoutingAndItsNeighbours()
    {
        // Arrange
        string routing = Read("guidance", "mcp", "guides", "routing.md");
        string coreRules = Read("guidance", "mcp", "guides", "core-rules.md");
        string webService = Read("guidance", "mcp", "guides", "backend", "configuration-webservice.md");
        string troubleshooting = Read("guidance", "mcp", "guides", "operations", "backend-deployment-troubleshooting.md");

        // Act
        string routingRow = routing.Split('\n').SingleOrDefault(line => line.Contains("name=assembly-package")) ?? string.Empty;

        // Assert
        routingRow.Should().Contain("assembly package",
            because: "agents find a guide only through its routing row");
        routingRow.Should().NotContain("review",
            because: "generic C# code review is owned elsewhere; this row must not compete for it");
        coreRules.Should().Contain("read `assembly-package` before compiling or installing one",
            because: "core-rules is read on every operation and must hand assembly packages to their owner");
        webService.Should().Contain("No `.svc` file and no web.config entry for an authenticated service",
            because: "the .svc claim was refuted on a stand and the web-service owner states it");
        webService.Should().Contain("`CRT_CSRF`",
            because: "the CSRF header name depends on the cookie the login set");
        troubleshooting.Should().Contain("`skipped (no compilable schemas)`",
            because: "the skipped package is the first boundary to check when no DLL appears");
    }

    [Test]
    [Description("Pins the assembly-package resource identity in the manifest.")]
    public void AssemblyPackageGuide_ShouldKeepItsPublishedResourceIdentity()
    {
        // Arrange
        using JsonDocument manifest = JsonDocument.Parse(Read("bundle-source.json"));

        // Act
        JsonElement resource = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "assembly-package");
        string[] requiredItemIds = [.. manifest.RootElement.GetProperty("requirements").GetProperty("itemIds")
            .EnumerateArray().Select(item => item.GetString()!)];

        // Assert
        resource.GetProperty("uri").GetString().Should().Be("docs://knowledge/com.creatio.clio/assembly-package",
            because: "clio resolves the guide by this URI");
        resource.GetProperty("sourcePath").GetString().Should().Be("guidance/mcp/guides/backend/assembly-package.md",
            because: "the manifest must publish the reviewed file");
        resource.GetProperty("role").GetString().Should().Be("guidance",
            because: "get-guidance resolves a bare name only for guidance items");
        requiredItemIds.Should().Contain("assembly-package",
            because: "a required item is part of every published generation");
    }
}

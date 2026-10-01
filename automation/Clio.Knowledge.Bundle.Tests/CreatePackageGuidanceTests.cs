using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the create-package guidance (ENG-101352): the call path, the fallbacks an agent must not
/// take, the returned-name rule, the three package-created outcomes, and the routing entry. Field
/// names follow the clio create-package contract (CreatePackageTool / CreatePackageCommand).
/// </summary>
[TestFixture]
public sealed class CreatePackageGuidanceTests
{
    [Test]
    [Description("The package-dependencies guide owns the create-package contract an agent needs to create a package through MCP alone.")]
    public void PackageDependenciesGuide_ShouldOwnTheCreatePackageContract()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string guide = Normalize(File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "backend", "package-dependencies.md")));

        // Assert
        guide.Should().Contain("CREATING A NEW PACKAGE",
            because: "the create-package rules need one section an agent can find");
        guide.Should().Contain("Use create-package ONLY when the user asks for a separate package",
            because: "page and schema writes resolve their own target package; an unasked-for package is clutter");
        guide.Should().Contain("find it with get-tool-contract and call it through clio-run-destructive",
            because: "create-package is a destructive long-tail tool and is not called directly");
        guide.Should().Contain("do NOT fall back to push-workspace, SQL, OData or DataService",
            because: "those paths create a locked or half-registered package instead of an editable one");
        guide.Should().Contain("Use the RETURNED package-name for every later call, never the one you sent",
            because: "the environment's SchemaNamePrefix is prepended, so the sent name may not exist");
        guide.Should().Contain("pass it as package to get-target-package to confirm the new package as the write target",
            because: "the created package must be usable as the write target of the same run (AC-2)");
        guide.Should().Contain("package-created=false means nothing changed",
            because: "a duplicate name or unknown dependency must read as a clean refusal (AC-4)");
        guide.Should().Contain("package-created=true with success=false means the package exists",
            because: "a partial failure must not be retried as a fresh create (AC-3)");
        guide.Should().Contain("dependencies here is a plain array of package names, e.g. dependencies: [\"CrtBase\"]",
            because: "create-package takes package names, not the [{ name }] objects add-package-dependency takes");
        guide.Should().Contain("add them with add-package-dependency",
            because: "dependencies that failed to apply are fixed on the existing package");
        guide.Should().Contain("package-created=null",
            because: "an unknown outcome must be checked with list-packages before retrying");
        guide.Should().Contain("create-package needs a user with administrator permission",
            because: "the access note must cover the create operation, not only the two dependency operations");
        guide.Should().NotContain("Both operations need",
            because: "the guide now covers three operations, so a sentence about 'both' is ambiguous");
    }

    [Test]
    [Description("Routing sends a request for a new package to the package-dependencies guide.")]
    public void Routing_ShouldMapANewPackageRequestToPackageDependencies()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string routing = Normalize(File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "routing.md")));

        // Assert
        routing.Should().Contain(
            "the user asks for a new, separate package in the environment -> name=package-dependencies (create-package)",
            because: "an agent asked for a new package must find the create-package rules before acting");
    }

    // Collapses every whitespace run (line breaks of either style, wrap indentation) to one space,
    // so the pins survive a CRLF checkout or a meaning-preserving rewrap of the guide.
    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");
}

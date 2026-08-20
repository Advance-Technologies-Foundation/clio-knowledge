using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class PageModificationComponentsGuidanceTests
{
    [Test]
    [Description("Keeps custom remote-module output payloads mapped through event expressions instead of deprecated raw request events.")]
    public void PageModificationComponentsGuidance_ShouldMapCustomOutputPayload_WhenComponentIsAWebComponent()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guidePath = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "pages",
            "modification", "components.md");

        // Act
        string guidance = File.ReadAllText(guidePath);

        // Assert
        guidance.Should().Contain("\"recordId\": \"@event.detail\"",
                because: "Angular Elements exposes a remote-module output value through CustomEvent.detail")
            .And.Contain("const recordId = request.recordId;",
                because: "the handler should consume the named request parameter produced by the event expression")
            .And.Contain("Use `\"@event.detail.<field>\"` when the component emits an object",
                because: "object payloads need an explicit nested-field mapping rather than raw event inspection")
            .And.Contain("One configured output creates one request binding",
                because: "duplicate dispatch is not a supported platform contract")
            .And.Contain("e8c0882bea89923e493c8476f1ecc177c7c22bd1",
                because: "the prescriptive payload rule needs an immutable Creatio source evidence boundary");
        guidance.Should().NotContain("request.$initialEvent.detail",
            because: "Freedom UI deprecates direct handler access to the raw initial event");
        guidance.Should().NotContain("if (!recordId)",
            because: "the source evidence does not justify a generic missing-payload guard or dispatch workaround");
    }

    [Test]
    [Description("Keeps UI-project guidance linked to the canonical page-output owner without duplicating its payload rules.")]
    public void UiProjectGuidance_ShouldRouteToPageModificationComponents_WhenConsumingCustomOutput()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guidePath = Path.Combine(repositoryRoot, "guidance", "mcp", "guides", "applications",
            "ui-project.md");

        // Act
        string guidance = File.ReadAllText(guidePath);

        // Assert
        guidance.Should().Contain("read `page-modification-components`",
                because: "remote-module consumers must reach the one guide that owns page output wiring")
            .And.Contain("that guide owns the `@event.detail` request-parameter mapping",
                because: "the adjacent guide should link to the owner rather than duplicate its contract");
        guidance.Should().NotContain("\"recordId\": \"@event.detail\"",
            because: "the executable payload example belongs only to page-modification-components");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

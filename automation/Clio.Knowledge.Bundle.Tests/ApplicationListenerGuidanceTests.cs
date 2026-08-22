using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class ApplicationListenerGuidanceTests
{
    [Test]
    [Description("Keeps all four application-listener hooks, their stateless activation contract, and routing in one canonical guide.")]
    public void ApplicationListenerGuidance_ShouldOwnTheFullLifecycleContract()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "composable-app", "application-listener.md"));
        string routing = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "routing.md"));
        string composableApp = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "composable-app", "creatio-composable-app-development.md"));
        string entityListener = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "composable-app", "configuration-entity-event-listener.md"));
        string kafkaCatalog = File.ReadAllText(Path.Combine(repositoryRoot,
            "catalog", "reference-examples", "creatio-kafka.yaml"));

        // Act
        string[] requiredGuideFacts =
        [
            "OnAppStart(AppEventContext)",
            "OnAppEnd(AppEventContext)",
            "OnSessionStart(AppEventContext)",
            "OnSessionEnd(AppEventContext)",
            "public parameterless constructor",
            "Construction and static initialization MUST be side-effect-free and non-fallible",
            "MUST NOT own cross-hook state in instance fields",
            "AppEventContext` carries application state only",
            "Session hooks MUST NOT be used as authentication, authorization, revocation, mandatory-audit, impersonation, or privileged-action boundaries",
            "OnSessionExpired` is not an `IAppEventListener` hook",
            "their ordering relative to unrelated platform services",
            "not guaranteed to run on the same node or form an exactly-once pair",
            "Session callbacks can overlap across sessions",
            "Session hooks MUST NOT wait for synchronous network or database I/O",
            "Shutdown MUST be bounded",
            "consumes the same host shutdown deadline",
            "atomically clear that same owner exactly once",
            "creates a fresh listener instance for each dispatched event",
            "constructor or static-initializer failure is outside that hook exception boundary",
            "Construct separate listener instances for start and end calls",
            "atf.creatio.kafka-reference"
        ];

        // Assert
        guide.Should().ContainAll(requiredGuideFacts,
            because: "the canonical guide must preserve every verified lifecycle decision and safety boundary");
        routing.Should().Contain("implement application or session lifecycle hooks with IAppEventListener / AppEventListenerBase -> name=application-listener",
            because: "agents must discover the lifecycle owner before implementing any of the four hooks");
        composableApp.Should().Contain("MUST also read `application-listener`",
            because: "the package workflow must route lifecycle work to its canonical owner");
        entityListener.Should().Contain("MUST NOT apply it to `AppEventListenerBase`; read `application-listener`",
            because: "entity-listener instance state is the opposite of the application-listener contract");
        kafkaCatalog.Should().Contain("id: atf.creatio.kafka-reference",
            because: "the published Kafka example is the pinned evidence for the application-to-singleton boundary");
    }

    [Test]
    [Description("Pins the application-listener resource identity used by guidance routing and compatibility consumers.")]
    public void ApplicationListenerGuidance_ShouldKeepItsPublishedResourceIdentity()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(repositoryRoot, "bundle-source.json")));

        // Act
        JsonElement[] resources = manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("itemId").GetString() == "application-listener")
            .ToArray();

        // Assert
        resources.Should().ContainSingle(
            because: "the routed application-listener name must resolve to one stable resource");
        JsonElement resource = resources.Single();
        resource.GetProperty("topicId").GetString().Should().Be("creatio.application-listener",
            because: "the lifecycle guide topic is part of the published discovery contract");
        resource.GetProperty("uri").GetString().Should().Be("docs://knowledge/com.creatio.clio/application-listener",
            because: "the canonical resource URI must remain stable for clients");
        resource.GetProperty("legacyUris").EnumerateArray().Select(value => value.GetString())
            .Should().ContainSingle().Which.Should().Be("docs://mcp/guides/application-listener",
                because: "the legacy route must keep resolving for compatibility");
        resource.GetProperty("sourcePath").GetString().Should().Be(
            "guidance/mcp/guides/composable-app/application-listener.md",
            because: "the manifest must publish the canonical source guide");
        resource.GetProperty("bundlePath").GetString().Should().Be("resources/application-listener.md",
            because: "the bundle path must remain aligned with the routed item name");
        resource.GetProperty("role").GetString().Should().Be("guidance",
            because: "the resource must remain discoverable as guidance");
        resource.GetProperty("mediaType").GetString().Should().Be("text/markdown",
            because: "the published resource contains Markdown guidance");
        resource.GetProperty("description").GetString().Should().Contain("all four IAppEventListener",
            because: "discovery must advertise the guide's complete lifecycle scope");
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

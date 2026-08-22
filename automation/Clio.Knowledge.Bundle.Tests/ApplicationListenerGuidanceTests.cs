using FluentAssertions;
using NUnit.Framework;

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
            "fresh listener instance for each dispatched event",
            "public parameterless constructor",
            "MUST NOT own cross-hook state in instance fields",
            "AppEventContext` carries application state only",
            "OnSessionExpired` is not an `IAppEventListener` hook",
            "their ordering relative to unrelated platform services",
            "Session callbacks can overlap across sessions",
            "Shutdown MUST be bounded",
            "creates a fresh listener instance for each dispatched event",
            "continues dispatching other listeners",
            "Construct separate listener instances for start and end calls",
            "atf.creatio.kafka-reference"
        ];

        // Assert
        guide.Should().ContainAll(requiredGuideFacts,
            because: "the canonical guide must preserve every verified lifecycle decision and safety boundary");
        routing.Should().Contain("-> name=application-listener",
            because: "agents must discover the lifecycle owner before implementing any of the four hooks");
        composableApp.Should().Contain("MUST also read `application-listener`",
            because: "the package workflow must route lifecycle work to its canonical owner");
        entityListener.Should().Contain("MUST NOT apply that rule to `AppEventListenerBase`",
            because: "entity-listener instance state is the opposite of the application-listener contract");
        kafkaCatalog.Should().Contain("creatio-application-lifecycle",
            because: "the published Kafka example is the pinned evidence for the application-to-singleton boundary");
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

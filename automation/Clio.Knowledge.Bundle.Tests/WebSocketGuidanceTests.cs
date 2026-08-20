using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class WebSocketGuidanceTests
{
    [Test]
    [Description("Keeps backend-to-frontend WebSocket rules in one routed canonical guide with an adjacent mandatory cross-link.")]
    public void WebSocketGuidance_ShouldOwnTheExecutableContract()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "integration", "websocket-messaging.md"));
        string routing = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "routing.md"));
        string sdkCommon = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "page-schema", "creatio-devkit-common.md"));

        // Act
        string[] requiredGuideFacts =
        [
            "MsgChannelManager.Instance.FindItemByUId(userId)",
            "SimpleMessage.Header.Sender",
            "new sdk.MessageChannelService()",
            "crt.HandleViewModelResumeRequest",
            "crt.HandleViewModelPauseRequest",
            "websocketSubscriptionPending",
            "PostMessage` exception as a transient disconnect race",
            "transient notification path, not a durable queue",
            "ClassFactory.Get<IMsgChannelManager>()"
        ];

        // Assert
        guide.Should().ContainAll(requiredGuideFacts,
            because: "the canonical article must preserve every verified decision needed for safe implementation");
        routing.Should().Contain("-> name=websocket-messaging",
            because: "an agent asking for backend-to-frontend WebSockets must discover the owner before planning");
        sdkCommon.Should().Contain("MUST also read `websocket-messaging`",
            because: "the adjacent SDK guide must route detailed channel semantics to their canonical owner");
        sdkCommon.Should().NotContain("MessageChannelService lifecycle:",
            because: "duplicated lifecycle templates would drift independently from the canonical WebSocket guide");
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

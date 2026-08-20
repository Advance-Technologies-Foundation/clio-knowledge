using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class WebSocketGuidanceTests
{
    [Test]
    [Description("Keeps supported WebSocket routes and the internal SERVER boundary in one routed canonical guide with an adjacent mandatory cross-link.")]
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
        string handlers = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "page-schema", "handlers.md"));
        string catalog = File.ReadAllText(Path.Combine(repositoryRoot,
            "catalog", "reference-examples", "creatio-websocket.yaml"));

        // Act
        string[] requiredGuideFacts =
        [
            "MsgChannelManager.Instance.FindItemByUId(userId)",
            "SimpleMessage.Header.Sender",
            "new sdk.MessageChannelService()",
            "crt.HandleViewModelResumeRequest",
            "crt.HandleViewModelPauseRequest",
            "websocketSubscriptionPending",
            "manager-resolution or `PostMessage` exception as non-delivery",
            "could not be posted to user channel",
            "channel.sendMessage(",
            "sdk.MessageChannelType.PTP",
            "sdk.MessageChannelType.BROADCAST",
            "same-user frontend bridge",
            "INTERNAL/UNSUPPORTED",
            "ClassFactory.Get<IMsgServiceLayer>()",
            "configuration web service",
            "low-trust",
            "Do not trust a browser-supplied target user ID",
            "never drive a privileged action",
            "sender string routes messages; it does not authenticate a trusted publisher",
            "The WebSocket subscription could not be established.",
            "A rejected subscription has no handle to release.",
            "Common.Logging.ILog",
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
        handlers.Should().Contain("MUST read `websocket-messaging`",
            because: "the handler guide must route subscription lifecycle decisions to their canonical owner");
        handlers.Should().NotContain(".subscribe(\"<Channel>\"",
            because: "a competing lifecycle template would teach a weaker resolved-handle-only guard");
        catalog.Should().Contain("Frontend SERVER messages are internal-only",
            because: "the published reference metadata must preserve the verified package boundary");
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

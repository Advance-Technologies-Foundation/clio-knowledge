using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class PageModalSizeGuidanceTests
{
    [Test]
    [Description("Keeps the modal window size rules, the size table and the capability boundary in page-modal-size, and routes size requests to it.")]
    public void PageModalSizeGuidance_ShouldOwnTheModalSizeContract()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string guide = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "pages", "modal-size.md"));
        string modification = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "pages", "modification", "index.md"));
        string routing = File.ReadAllText(Path.Combine(repositoryRoot,
            "guidance", "mcp", "guides", "routing.md"));

        // Act
        string[] requiredGuideFacts =
        [
            "`modalSettings.size` in the page's `AppearanceSettings` add-on",
            "`layoutConfig.width` of the `Main` container is NOT the window size",
            "Change a modal window's size ONLY with `set-page-modal-size`",
            "MUST NOT be resolved by picking a size",
            "| `small` | S | 1 |",
            "| `medium` | M | 2 |",
            "| `large` | L | 3 |",
            "| `extra-large` | XL | 4 |",
            "`crt.OpenPageRequest`",
            "call `get-page-modal-size` with the same `package-name` that `set-page-modal-size` will write to",
            "Requires a clio that provides the `get-page-modal-size` and `set-page-modal-size` tools"
        ];

        // Assert
        guide.Should().ContainAll(requiredGuideFacts,
            because: "the guide must keep every rule that stops an agent from editing the page body instead of the add-on");
        guide.Should().NotContain("8.1.0.147",
            because: "the boundary is the capability, not a release number nobody can know before the clio change ships");
        modification.Should().Contain("| `page-modal-size` |",
            because: "the page-modification GATE must send a modal size request to its owner");
        routing.Should().Contain("-> name=page-modal-size",
            because: "an agent asked to resize a modal window must discover the owner before editing the page");
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

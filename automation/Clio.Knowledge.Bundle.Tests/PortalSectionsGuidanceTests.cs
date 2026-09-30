using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the object-access step of portal-sections on the set-object-rights contract: one call per approved
/// object, with its operations and the enable flag named, after the developer has seen what the whole external
/// audience would get. The old step (a fan-out grant, a two-step confirmation code, a default grant of
/// read/create/edit) calls arguments the tool now refuses before any read or write.
/// </summary>
[TestFixture]
public sealed class PortalSectionsGuidanceTests
{
    private const string PortalSectionsPath = "guidance/mcp/guides/applications/portal-sections.md";

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    // Line breaks and indentation are layout, not content: a rule wrapped across lines is still the rule.
    private static string Normalized(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [TestCase("GRANT each approved object in its own call",
        TestName = "PortalSections_ShouldGrantEachApprovedObjectInItsOwnCall")]
    [TestCase("--operations read", TestName = "PortalSections_ShouldNameTheOperations")]
    [TestCase("--enable-operation-permissions", TestName = "PortalSections_ShouldNameTheEnableFlag")]
    [TestCase("ASK THE USER before any write", TestName = "PortalSections_ShouldAskBeforeEachWrite")]
    [TestCase("EVERY external user gets that access, on EVERY record of the object",
        TestName = "PortalSections_ShouldStateWhatTheWholeAudienceGets")]
    [TestCase("the object of each detail on the page, and the lookup object of each column shown",
        TestName = "PortalSections_ShouldListEveryObjectThePortalPagesShow")]
    [TestCase("it does not list lookups the object inherits",
        TestName = "PortalSections_ShouldStateWhatTheConnectedListingLeavesOut")]
    [TestCase("with the facts of a preview of the planned call",
        TestName = "PortalSections_ShouldPreviewBeforeAsking")]
    [Description("The object-access step lists, asks, then grants each approved object in its own set-object-rights call, naming the operations and the enable flag.")]
    public void PortalSections_ShouldFollowTheObjectRightsContract(string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), PortalSectionsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "the portal grant follows the set-object-rights contract");
    }

    [Test]
    [Description("No line of the article puts include-connected on a set-object-rights call: set changes one object per call and refuses the argument.")]
    public void PortalSections_ShouldNotPassIncludeConnectedToSetObjectRights()
    {
        // Arrange
        string[] lines = File.ReadAllLines(Path.Combine(Root(), PortalSectionsPath));

        // Act
        string[] offending = lines
            .Where(line =>
            {
                int set = line.IndexOf("set-object-rights", StringComparison.OrdinalIgnoreCase);
                return set >= 0 && line.IndexOf("include-connected", set, StringComparison.OrdinalIgnoreCase) > set;
            })
            .Select(line => line.Trim())
            .ToArray();

        // Assert
        offending.Should().BeEmpty(because: "include-connected is an argument of get-object-rights, never of set-object-rights");
    }

    [TestCase("confirmation-code")]
    [TestCase("connected-operations")]
    [TestCase("allow-security-object")]
    [Description("The object-access step names no set-object-rights argument the tool has retired: an unknown argument is refused before any read or write.")]
    public void PortalSections_ShouldNotNameARetiredSetObjectRightsArgument(string retired)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), PortalSectionsPath));

        // Act
        bool names = guide.Contains(retired, StringComparison.OrdinalIgnoreCase);

        // Assert
        names.Should().BeFalse(because: $"'{retired}' is no longer a set-object-rights argument");
    }
}

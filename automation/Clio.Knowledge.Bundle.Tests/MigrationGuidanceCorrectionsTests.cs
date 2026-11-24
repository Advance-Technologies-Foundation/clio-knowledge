using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class MigrationGuidanceCorrectionsTests
{
    [Test]
    [Description("Custom requests, converters and validators use the package's namespace; usr. is only the default.")]
    public void PageSchemaGuidance_ShouldNotRequireUsrNamespace_WhenNamingCustomEntries()
    {
        // Arrange
        string handlers = ReadGuide("page-schema", "handlers.md");
        string converters = ReadGuide("page-schema", "converters.md");
        string validators = ReadGuide("page-schema", "validators.md");

        // Act
        string[] pageSchemaGuides = [handlers, converters, validators];

        // Assert
        pageSchemaGuides.Should().NotContain(guide => guide.Contains("MUST use the `usr.` prefix"),
            because: "a team package with its own namespace must not be pushed to usr.");
        pageSchemaGuides.Should().NotContain(guide => guide.Contains("must use the `usr.` prefix"),
            because: "the lower-case form of the same rule is just as binding for an agent");
        handlers.Should().Contain("Custom namespace (this rule owns it for requests, converters and validators)",
                because: "one article owns the namespace rule and the others cite it")
            .And.Contain("Use `usr.` only when the package has no custom namespace yet.",
                because: "usr. stays the default for a package without a namespace")
            .And.Contain("Never use `crt.`",
                because: "crt. is the built-in namespace")
            .And.Contain("creatio-ui commit `cc78bd8be9b`",
                because: "the claim that the runtime ignores the prefix needs its source evidence");
        converters.Should().Contain("`page-schema-handlers` (NON-NEGOTIABLES, \"Custom namespace\")",
            because: "the converter guide cites the owner instead of restating the rule");
        validators.Should().Contain("`page-schema-handlers` (NON-NEGOTIABLES, \"Custom namespace\")",
            because: "the validator guide cites the owner instead of restating the rule");
    }

    private static string ReadGuide(params string[] relativePath)
    {
        string[] parts = [FindRepositoryRoot(), "guidance", "mcp", "guides", .. relativePath];
        return File.ReadAllText(Path.Combine(parts));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}

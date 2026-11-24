using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the lookup-colors guide: the palette and its pick order, the write path that works for a
/// <c>Color</c> column, the confirmation before a schema change and the whole-article clio boundary.
/// </summary>
[TestFixture]
public sealed class LookupColorsGuidanceTests
{
    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([ProcessGuideSet.FindRepositoryRoot(), .. path]));

    private static string Guide() =>
        System.Text.RegularExpressions.Regex.Replace(
            Read("guidance", "mcp", "guides", "applications", "lookup-colors.md"), @"\s+", " ");

    [Test]
    [Description("The guide keeps both Academy palette sequences in their published order.")]
    public void LookupColorsGuide_ShouldKeepThePaletteInOrder()
    {
        // Arrange
        const string recommended = "Recommended, in order: `#EDF9CC`, `#CCF4DC`, `#D2EED0`, `#FFEECD`, `#FFE7CC`, "
            + "`#FED6E5`, `#FFE0D6`, `#FFD9D0`, `#F1E5F5`, `#E4DAFC`, `#CCE9FB`, `#CCDEFC`, `#CDEEFD`, `#DCD9F3`, "
            + "`#CCEAE7`, `#CCF2ED`.";
        const string auxiliary = "Auxiliary, in order: `#A6DE00`, `#00C853`, `#22AC14`, `#FFAC07`, `#FF8800`, "
            + "`#F9307F`, `#FF6534`, `#FF4013`, `#B87CCF`, `#7848EE`, `#0091EA`, `#0058EF`, `#03A9F4`, `#4F43C2`, "
            + "`#009688`, `#00BFA5`.";

        // Act
        string guide = Guide();

        // Assert
        guide.Should().Contain(recommended,
            because: "the recommended colors are picked in this order, copied verbatim from Creatio Academy");
        guide.Should().Contain(auxiliary,
            because: "the auxiliary colors continue the pick order after the recommended ones");
    }

    [Test]
    [Description("The guide keeps the rules that make the color choice deterministic and collision-free.")]
    public void LookupColorsGuide_ShouldKeepThePickRules()
    {
        // Arrange
        string[] requiredFacts =
        [
            "Read EVERY record of the lookup with `execute-esq`",
            "otherwise by `Name` ascending",
            "the first palette color, recommended in their order and then auxiliary in their order, that no record of the lookup uses yet",
            "All 32 palette colors are in use: stop and ask the user",
            "Keep a value's existing color unless the user asks to recolor it",
            "Each MUST match `#RRGGBB`"
        ];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "without the full value set and the first-unused rule two values get the same color");
        guide.Should().NotContain("Find the record `Id`s with `dataforge-find-lookups`",
            because: "dataforge-find-lookups is a scored search and cannot list every value of a lookup");
    }

    [Test]
    [Description("The guide keeps the write path, the confirmation and the version gate.")]
    public void LookupColorsGuide_ShouldKeepTheWritePathAndGates()
    {
        // Arrange
        string[] requiredFacts =
        [
            "Version boundary: writing the color needs clio 8.1.0.118 or later",
            "stop before any schema change and tell the user to update clio",
            "EXPERIMENTAL:",
            "MUST ask the user for the target package and get explicit confirmation before any schema change",
            "add the column to that schema with `modify-entity-schema-column`",
            "Write each value with `upsert-data-binding-row-db`",
            "MUST NOT use `odata-update` or `odata-create`",
            "MUST NOT use `execute-dataservice-batch`",
            "does NOT write the color into a record that already exists on the target"
        ];

        // Act
        string guide = Guide();

        // Assert
        guide.Should().ContainAll(requiredFacts,
            because: "each rule keeps the agent on the only write path that stores a Color value and away from a half-done schema change");
    }

    [Test]
    [Description("Routing, the sibling guides and the manifest point to the lookup-colors guide.")]
    public void LookupColorsGuide_ShouldBeReachableAndPublished()
    {
        // Arrange
        string routing = Read("guidance", "mcp", "guides", "routing.md");
        string modeling = Read("guidance", "mcp", "guides", "applications", "app-modeling-details.md");
        string bindings = Read("guidance", "mcp", "guides", "pages", "data-bindings.md");
        using JsonDocument manifest = JsonDocument.Parse(Read("bundle-source.json"));

        // Act
        JsonElement resource = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == "lookup-colors");

        // Assert
        routing.Should().Contain("-> name=lookup-colors",
            because: "agents find a guide only through its routing row");
        modeling.Should().Contain("`get-guidance name=lookup-colors`",
            because: "the owner of the Color column type points to the coloring rules");
        bindings.Should().Contain("`get-guidance name=lookup-colors`",
            because: "the owner of the binding rules points to the coloring rules");
        resource.GetProperty("uri").GetString().Should().Be("docs://knowledge/com.creatio.clio/lookup-colors",
            because: "clio resolves the guide by this URI");
        resource.GetProperty("sourcePath").GetString().Should().Be("guidance/mcp/guides/applications/lookup-colors.md",
            because: "the manifest must publish the reviewed file");
        resource.GetProperty("description").GetString().Should().StartWith("Needs clio 8.1.0.118 or later.",
            because: "a whole-article boundary leads the catalog description");
    }
}

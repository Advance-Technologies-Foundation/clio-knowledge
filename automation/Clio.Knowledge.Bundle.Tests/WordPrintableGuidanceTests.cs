using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the MS Word printable guides: <c>word-printable</c> (rows, designer, display, shipping),
/// <c>word-printable-template</c> (fields, macros, converters) and <c>word-printable-troubleshooting</c>.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each pinned clause is a fact measured on a 10.3.1152 stand or read in the 10.2.444 / 10.3.1152
/// configuration source. Shortening one into a weaker or stronger claim changes what an agent builds: a field
/// without a switch prints nothing, a converter outside <c>Terrasoft.Configuration</c> or returning null fails the
/// whole document, and an existing printable on a target takes only force-updated binding columns.
/// </remarks>
[TestFixture]
public sealed class WordPrintableGuidanceTests
{
    private const string EntryPath = "guidance/mcp/guides/backend/word-printable.md";

    private const string TemplatePath = "guidance/mcp/guides/backend/word-printable-template.md";

    private const string TroubleshootingPath = "guidance/mcp/guides/backend/word-printable-troubleshooting.md";

    private static readonly (string ItemId, string Path)[] Articles =
    [
        ("word-printable", EntryPath),
        ("word-printable-template", TemplatePath),
        ("word-printable-troubleshooting", TroubleshootingPath)
    ];

    [Test]
    [Description("The three guides are registered as banner-less guidance with no legacy route, required by the library, and reached from routing and from process-files-report.")]
    public void Resources_ShouldBeRegisteredAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");
        string[] requiredItems = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString()!)];
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];

        // Act
        string filesReport = ReadCollapsed("guidance/mcp/guides/processes/files-report.md");

        // Assert
        foreach ((string itemId, string path) in Articles)
        {
            JsonElement resource = source.RootElement.GetProperty("resources")
                .EnumerateArray()
                .Single(item => item.GetProperty("itemId").GetString() == itemId);
            resource.GetProperty("sourcePath").GetString().Should().Be(path,
                because: "the manifest must point at the article this fixture pins");
            resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{itemId}.md",
                because: "the flat resources folder names each article by its itemId");
            resource.GetProperty("topicId").GetString().Should().Be($"creatio.{itemId}",
                because: "the topic is the stable cross-library identity of the guide");
            resource.GetProperty("role").GetString().Should().Be("guidance",
                because: "get-guidance serves only guidance resources");
            resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
                because: "the guides were born after the v0 migration and have no v0 route to preserve");
            requiredItems.Should().Contain(itemId,
                because: "the library requires every item it publishes");
            requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
                because: "the requirement lists the canonical route next to the item id");
        }

        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), "guidance/mcp/guides/routing.md").Replace("\r\n", "\n")
            .Should().Contain("\n- MS Word printables (SysModuleReport): build, template, ship, fix -> name=word-printable\n",
                because: "a printable task that is not about a page button must still find the entry guide, so the row is a "
                    + "top-level domain row and not nested under Pages");
        filesReport.Should().Contain(
            "Building or changing a printable (its columns, template, converter, package): `get-guidance name=word-printable`.",
            because: "the process element refuses a missing printable; the agent must learn where building one is owned");
    }

    [Test]
    [Description("Template rules measured on a stand: the switch, case-sensitive binding, the argument grammar, the literal pipe in examples, and the stated-token rule before a converter.")]
    public void Template_ShouldStateTheMeasuredFieldAndMacroRules()
    {
        // Arrange
        string guide = ReadCollapsed(TemplatePath);

        // Act
        string[] clauses =
        [
            "MUST write `MERGEFIELD` in upper case and follow the name with a switch",
            "Without a switch the field is replaced by an empty `w:fldSimple` and prints NOTHING",
            "No space before `[#`: `Name [#Upper#]` matches nothing and prints empty (measured).",
            "When a bare `Name` field comes EARLIER in the template, it is worse: the whole document fails with HTTP 400",
            "The argument is the second non-empty `|`-separated segment",
            "With an argument: <Caption>[#Token|arg#]. A chain: <Caption>[#A#][#B|x#]. The pipe is written as is.",
            "BirthDate[#Date|yyyy-MM-dd#] printed 1986-01-20 (measured).",
            "MUST NOT add a converter before checking them: before writing one, tell the user which built-in token came closest and why it does not do the job."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "each of these fails silently in a generated document when an agent gets it wrong");
        }

        guide.Should().NotContain("\\|",
            because: "the article is served as plain text, so a Markdown escape reaches the agent and breaks a copied token");
    }

    [Test]
    [Description("The converter contract measured on a stand: the namespace and the null return each fail the whole document.")]
    public void Template_ShouldStateTheConverterContract()
    {
        // Arrange
        string guide = ReadCollapsed(TemplatePath);

        // Act
        string[] clauses =
        [
            "Namespace: MUST be `Terrasoft.Configuration`, the class top-level.",
            "fails the WHOLE document with HTTP 400 and a `NullReferenceException` in `ExpressionConverterHelper.CreateExpressionConverter` (measured)",
            "Output: MUST NOT return `null`.",
            "`arguments` is `null` when there is no argument, never `\"\"` (measured)."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "the contract is checked only when the first report runs, never at compile time");
        }
    }

    [Test]
    [Description("Shipping measured with push-pkg: clio's DB-first binding is incomplete, an existing row takes only forced columns, and the binding carries the template as of its last save.")]
    public void Entry_ShouldStateTheMeasuredShippingRules()
    {
        // Arrange
        string guide = ReadCollapsed(EntryPath);

        // Act
        string[] clauses =
        [
            "on clio 8.1.0.130 `Type` and `SysEntitySchema` were left out of the binding and no column was marked force-update (measured).",
            "a row that exists takes ONLY the force-update columns",
            "an archive whose binding has the same `ModifiedOnUtc` as the installed one applies nothing, forced or not;",
            "The package export carries the binding's copy as of its last save, not the current `File` (measured).",
            "MUST NOT download it with `clio call-service`",
            "FastReport (`SysModuleReport.Type` `8def0f6d-…`) and DevExpress (`32f72c9c-…`) are different engines. Stop and say so; nothing here applies to them.",
            "run the login from a script that reads the settings at run time, never as a command line that carries the password."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "an agent that misses one of these delivers a printable without its type, or an old template");
        }
    }

    [Test]
    [Description("Troubleshooting: errors are in Error.log, the trace counts table part records, and rights are never widened without a yes.")]
    public void Troubleshooting_ShouldStateWhereErrorsAreAndTheRightsRule()
    {
        // Arrange
        string guide = ReadCollapsed(TroubleshootingPath);

        // Act
        string[] clauses =
        [
            "Generator errors do NOT go to `MSWordLog.log` (measured).",
            "for each table part `Count of records for table part with Entity <TableCaption> is <n>` (all measured)",
            "| `ArgumentException` \"An item with the same key has already been added\" in `WordReportGenerator.AddColumnToEntitySchemaQuery` |",
            "MUST reproduce with one record, AS the user who reported it",
            "MUST NOT ask for, reset or use another user's password.",
            "Create feature `EnableMSWordLog` (it is not shipped) and turn it on",
            "Turn the feature off again afterwards.",
            "an 8.2.3 boundary from another source was not verified: an empty or missing file there proves nothing",
            "MUST NOT widen object, column or record rights to make a printable fill"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "a wrong log or a silent rights change is the costliest mistake in this diagnosis");
        }
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

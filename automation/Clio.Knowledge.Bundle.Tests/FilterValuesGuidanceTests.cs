using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class FilterValuesGuidanceTests
{
    private const string ArticlePath = "guidance/mcp/guides/processes/data-source-filters.md";

    private static string Article() =>
        File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), ArticlePath)).ReplaceLineEndings("\n");

    [Test]
    [Description("ENG-105557: the filter article states that several values of one column are ONE condition and never equal conditions joined by AND, why the AND form is wrong (it matches no record and only warns), and the values shape with its worked example, so an agent asked for 'status is To do or In progress' builds one condition.")]
    public void Guide_ShouldStateThatSeveralValuesOfOneColumnAreOneCondition()
    {
        // Arrange
        string guide = Article();

        // Act
        string[] required = [
            "SEVERAL VALUES OF ONE COLUMN are ONE condition, never separate `equal` conditions joined by AND",
            "such a group matches NO record",
            "`{ \"column\": \"Status\", \"comparison\": \"equal\", \"values\": [\"To do\", \"In progress\"] }`",
            "with `equal` (the record is any of them) or `notEqual` (it is",
        ];

        // Assert
        guide.Should().ContainAll(required,
            because: "the rule, its reason and the shape agents copy must all be in the article that owns the filter");
    }

    [Test]
    [Description("ENG-105557: every place that enumerates the right-hand sources names values - the source list, the signal-start restriction (a signal start takes values like a constant) and the catalog description agents discover the guide by - so no statement of the article contradicts the rule or the write contracts.")]
    public void Guide_ShouldNameValuesWhereverItListsTheRightHandSources()
    {
        // Arrange
        string guide = Article();
        string catalog = File.ReadAllText(Path.Combine(ProcessGuideSet.FindRepositoryRoot(), "bundle-source.json"));

        // Act
        bool sourceList = guide.Contains("`values` (several records of one LOOKUP column, see the next bullet)");
        bool signalStart = guide.Contains("may ONLY be a constant\n  (`value`, or `values` for several lookup records)");
        bool description = catalog.Contains("the right-hand value sources (value, values for several lookup records,")
            && catalog.Contains("allows only constants (value or values), a macro or isNull");

        // Assert
        sourceList.Should().BeTrue(because: "the list of right-hand sources says exactly ONE of them, so it must name values");
        signalStart.Should().BeTrue(
            because: "the restriction marked important must not read as a ban on values, which a signal start takes");
        description.Should().BeTrue(because: "guide discovery reads the catalog description, not the article");
    }

    [Test]
    [Description("ENG-105557: values is gated on the package floor and on the FULL create-business-process contract, and the gate names both fallbacks - 'any of' as an or group of equal conditions, 'none of' as notEqual conditions in the AND group - so a clio or package that does not carry values never builds a field it cannot send, never falls back to the AND form, and never sends an or group of exclusions, which matches every record.")]
    public void Guide_ShouldGateValuesAndNameTheFallback()
    {
        // Arrange
        string guide = Article();

        // Act
        bool floor = guide.Contains("`values` needs CrtProcessBuilder 1.6.6.157 or later");
        bool contractGate = guide.Contains("Use it only when the FULL\n  `create-business-process` contract");
        bool fallback = guide.Contains("otherwise build \"any of\" as that `or` group of `equal` conditions, and \"none of\"\n"
            + "  as `notEqual` conditions in the AND group");
        bool noneOfNeverOr = guide.Contains("\"None of\" them is one `notEqual` condition per value in the AND group itself, never an `or`\n"
            + "  group: an `or` of `notEqual` conditions matches every record");

        // Assert
        floor.Should().BeTrue(because: "a current clio refuses an older package up front, and the article must say why");
        contractGate.Should().BeTrue(
            because: "an older clio does not carry values, and its typed descriptor would drop or refuse the member");
        fallback.Should().BeTrue(because: "without values the correct form is an or group for any of, never the AND form");
        noneOfNeverOr.Should().BeTrue(
            because: "an or group of exclusions is always true, so a deleteData element built that way deletes every record");
    }

    [Test]
    [Description("ENG-105557: the read-back of a condition over several records is stated both ways - whole as values when CrtProcessBuilder 1.6.6.157 and clio both carry them, and as its FIRST record only, marked, on an older package or with a clio that predates values, where re-sending it narrows an equal and WIDENS a notEqual - so the describe -> setFilter loop never silently drops records.")]
    public void Guide_ShouldStateTheReadBackOfSeveralRecords()
    {
        // Arrange
        string guide = Article();

        // Act
        string[] required = [
            "reads back as `values`",
            "MUST, on CrtProcessBuilder earlier than 1.6.6.157 or with a clio that predates `values`",
            "`<multi-value filter: N values; describe shows only the first>`",
            "an `equal` then selects fewer records, a `notEqual` MORE",
        ];

        // Assert
        guide.Should().ContainAll(required,
            because: "the read-back on both sides of the floor decides whether a re-sent filter keeps its records");
    }
}

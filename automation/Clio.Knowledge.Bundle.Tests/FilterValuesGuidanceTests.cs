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
    [Description("ENG-105557: values is gated on the package floor and on the FULL create-business-process contract, and the gate names the fallback (an or group of equal conditions), so a clio or package that does not carry values never builds a field it cannot send and never falls back to the AND form.")]
    public void Guide_ShouldGateValuesAndNameTheFallback()
    {
        // Arrange
        string guide = Article();

        // Act
        bool floor = guide.Contains("`values` needs CrtProcessBuilder 1.6.6.154 or later");
        bool contractGate = guide.Contains("Use it only when the FULL\n  `create-business-process` contract");
        bool fallback = guide.Contains("otherwise build \"any of\" as that `or` group of `equal` conditions");

        // Assert
        floor.Should().BeTrue(because: "a current clio refuses an older package up front, and the article must say why");
        contractGate.Should().BeTrue(
            because: "an older clio does not carry values, and its typed descriptor would drop or refuse the member");
        fallback.Should().BeTrue(because: "without values the correct form is an or group, never the AND form");
    }

    [Test]
    [Description("ENG-105557: the read-back of a condition over several records is stated both ways - whole as values from CrtProcessBuilder 1.6.6.154, and as its FIRST record only, marked, on an older package, where re-sending it narrows an equal and WIDENS a notEqual - so the describe -> setFilter loop never silently drops records.")]
    public void Guide_ShouldStateTheReadBackOfSeveralRecords()
    {
        // Arrange
        string guide = Article();

        // Act
        string[] required = [
            "reads back as `values`",
            "MUST, on CrtProcessBuilder earlier than 1.6.6.154",
            "an `equal` then selects fewer records, a `notEqual` MORE",
        ];

        // Assert
        guide.Should().ContainAll(required,
            because: "the read-back on both sides of the floor decides whether a re-sent filter keeps its records");
    }
}

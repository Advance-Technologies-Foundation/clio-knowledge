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
    [Description("The filter article states that several values of one column are ONE condition and never equal conditions joined by AND, why the AND form is wrong (it matches no record and only warns), and the values shape with its worked example, so an agent asked for 'status is To do or In progress' builds one condition.")]
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
    [Description("Every place that enumerates the right-hand sources names values - the source list, the signal-start restriction (a signal start takes values like a constant) and the catalog description agents discover the guide by - so no statement of the article contradicts the rule or the write contracts.")]
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
    [Description("The values source is gated on the create-business-process contract, quoted verbatim. The short form a default lookup returns, which is what agents read, counts when it says the sentence, and its silence sends the agent to the full form before any fallback. The article gives no last release whose short form is silent, since a release without the short-form sentence could overtake any number it named. The gate names the clio release that ships the sentence, never a CrtProcessBuilder number, which a cut from main without values can overtake (1.6.6.158 and 1.6.6.159 did), and both fallbacks: 'any of' as an or group of equal conditions, 'none of' as notEqual conditions in an and group. So a clio or package that does not carry values never builds a field it cannot send, never falls back to the AND form, and never sends an or group of exclusions, which matches every record.")]
    public void Guide_ShouldGateValuesAndNameTheFallback()
    {
        // Arrange
        string guide = Article();

        // Act
        bool contractGate = guide.Contains("Gate: use `values` only when the `create-business-process` contract says\n"
            + "  \"any of several lookup records is ONE condition with values\"");
        bool shortFormCounts = guide.Contains("The short form a default `get-tool-contract` lookup returns\n"
            + "  counts, but its silence proves nothing");
        bool silentShortForm = guide.Contains("the full form (`detail=full`) says it from clio 8.1.0.150, the short\n"
            + "  form only from a later release, so read the full form before you fall back");
        bool fullFormOnly = guide.Contains("only when the FULL `create-business-process` contract");
        bool shippingClio = guide.Contains("as clio 8.1.0.150 and later does; that clio\n"
            + "  refuses an older CrtProcessBuilder up front");
        bool packageNumber = guide.Contains("`values` needs CrtProcessBuilder");
        bool fallback = guide.Contains("If neither form says it, build\n"
            + "  \"any of\" as that `or` group of `equal` conditions, and \"none of\" as `notEqual` conditions in an `and` group");
        bool noneOfNeverOr = guide.Contains("\"None of\" them is one `notEqual` condition per value in an `and` group (the root when it is\n"
            + "  `and`, otherwise a nested `and` group), never in an `or` group: an `or` of `notEqual` conditions matches every\n"
            + "  record");

        // Assert
        contractGate.Should().BeTrue(
            because: "an older clio does not carry values, and the agent checks the gate against the contract text, "
                + "so the article quotes the contract's own words on one line");
        shortFormCounts.Should().BeTrue(
            because: "agents read the short form a default lookup returns, and a clio that says the sentence there "
                + "needs no detail=full read");
        silentShortForm.Should().BeTrue(
            because: "a clio from 8.1.0.150 carries values and may say so only in the full form, and an agent that took "
                + "a silent short form for a no built an or group although its clio carried values");
        fullFormOnly.Should().BeFalse(
            because: "a gate on the full form alone would make every agent pay a detail=full read for a sentence "
                + "the short form already says");
        shippingClio.Should().BeTrue(
            because: "a version boundary is the clio release that shipped the change, and its package floor is why the gate needs no package number");
        packageNumber.Should().BeFalse(
            because: "a CrtProcessBuilder number can be overtaken by a cut from main that lacks values, as 1.6.6.158 and 1.6.6.159 were");
        fallback.Should().BeTrue(because: "without values the correct form is an or group for any of, never the AND form");
        noneOfNeverOr.Should().BeTrue(
            because: "an or group of exclusions is always true, so a deleteData element built that way deletes every record");
    }

    [Test]
    [Description("On a lookup column notEqual, with one value or with values, also selects records whose column is empty. The article says so next to the 'none of' rule, with its evidence and the isNotNull condition that leaves those records out, so a deleteData element built for 'none of them' does not silently delete every record with no value.")]
    public void Guide_ShouldSayThatNotEqualOnALookupAlsoSelectsEmptyValues()
    {
        // Arrange
        string guide = Article();

        // Act
        string[] required = [
            "`notEqual`, with one value or with `values`, also selects records whose column is EMPTY",
            "a `deleteData` element deletes those too",
            "`isNotNull` condition on the same column beside it in an `and` group",
            "Measured on Creatio 10.2.522",
        ];

        // Assert
        guide.Should().ContainAll(required,
            because: "an agent that reads 'none of them' as leaving out records with no value changes or deletes those records without being told");
    }

    [Test]
    [Description("The read-back of a condition over several records is stated both ways. It reads back whole as values when clio 8.1.0.150 and its CrtProcessBuilder both carry them. The MUST keys on the marker, never on a version, because every package without values returns the FIRST record only (1.6.6.158 and 1.6.6.159 included), and re-sending it narrows an equal and WIDENS a notEqual. So the describe -> setFilter loop never silently drops records.")]
    public void Guide_ShouldStateTheReadBackOfSeveralRecords()
    {
        // Arrange
        string guide = Article();

        // Act
        string[] required = [
            "reads back as `values`",
            "MUST, whatever the versions: a condition that describe returns marked",
            "`<multi-value filter: N values; describe shows only the first>`",
            "an `equal` then selects fewer records, a `notEqual` MORE",
        ];

        // Assert
        guide.Should().ContainAll(required,
            because: "the read-back on both sides of the floor decides whether a re-sent filter keeps its records");
    }
}

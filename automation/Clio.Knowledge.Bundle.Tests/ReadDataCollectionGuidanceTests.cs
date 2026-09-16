using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the ENG-96504 collection-mode guidance in the data-elements article and its catalog entry: the two rules
/// the platform makes non-negotiable (an explicit column selection, and a column the collection cannot carry),
/// the top-N that belongs to this mode alone, the shape that exists from the moment the mode is entered, and the
/// state that leaves with the mode. Each claim below is a platform behaviour an agent cannot observe from a
/// refusal message alone, so a copy-edit that drops one sends it back to a call the server refuses — or worse, to
/// a call that succeeds and reads the wrong columns.
/// </summary>
[TestFixture]
public sealed class ReadDataCollectionGuidanceTests
{
    private const string Article = "guidance/mcp/guides/processes/read-data.md";
    private const string Catalog = "guidance/mcp/guides/processes/element-catalog.md";

    [Test]
    [Description("Collection is taught as a buildable mode with both of its outputs, and the shaped one is named as what a Collection process parameter mirrors.")]
    public void Guide_ShouldTeachCollectionAsBuildable_WithBothOutputs()
    {
        string guide = ReadGuide();

        guide.Should().Contain("first / collection / count / aggregation modes",
            because: "the heading is what an agent scans for the mode set; leaving collection out of it hides a "
                + "buildable mode behind a paragraph");
        guide.Should().Contain("`ResultEntityCollection` (the raw list) and",
            because: "a collection element has TWO outputs and a mapping has to name the right one");
        guide.Should().Contain("per-column shape is the only\n    thing a consumer can bind to",
            because: "the raw list carries no shape, so an agent that mirrors it instead of ResultCompositeObjectList "
                + "produces a parameter nothing can read");
        ReadCatalog().Should().Contain("\"collection\" | \"count\" | \"aggregation\"",
            because: "the catalog restates the describe tokens and is where an agent checks what a mode reads back as");
    }

    [Test]
    [Description("Both platform silences are stated as refusals: an omitted column selection reads every column, and an uncarryable column is dropped from the query.")]
    public void Guide_ShouldNameBothCollectionRefusals()
    {
        string guide = ReadGuide();

        guide.Should().Contain("an omitted selection is not \"no columns\"",
            because: "this is the trap: the runtime falls back to every column of the object, so an agent that "
                + "omits the selection gets a shape it never asked for rather than an error");
        guide.Should().Contain("drops it from the query without a word",
            because: "an uncarryable column is stripped silently, so the refusal has to explain what the platform "
                + "would otherwise do behind the agent's back");
    }

    [Test]
    [Description("The top-N is scoped to collection and paired with the sort warning; the shape lifecycle says when it appears, how it re-shapes, and what leaving the mode clears.")]
    public void Guide_ShouldScopeTheTopN_AndTeachTheShapeLifecycle()
    {
        string guide = ReadGuide();

        guide.Should().Contain("is the top-N: positive, refused in every other",
            because: "`first` already reads one record and a function mode reads none, so a top-N there would be "
                + "stored and ignored");
        guide.Should().Contain("Omitting it KEEPS the stored one",
            because: "an omitted top-N is 'keep', not 'clear' — a stand run caught the opposite, where re-selecting "
                + "columns silently turned a top-25 read into a read-everything one");
        guide.Should().Contain("a top-N without a sort takes an arbitrary slice",
            because: "the pair is only meaningful together, and nothing at run time reports an unsorted top-N");
        guide.Should().Contain("`create-business-process` call find a shape rather than an empty list",
            because: "the write-time shaping is the whole reason this story exists — without it a same-call mirror "
                + "copies an empty list and the build fails");
        guide.Should().Contain("LEAVING collection additionally clears its top-N pair",
            because: "a stale top-N is not inert: under FeatureReadDataUserTaskEntityReadOldMode it still decides "
                + "how many rows a first-record read takes");
    }

    private static string ReadGuide() =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), Article);

    private static string ReadCatalog() =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), Catalog);
}

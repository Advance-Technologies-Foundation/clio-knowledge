using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the ENG-96230 collection-parameter guidance in the process-parameters article: the mirror is the way a
/// collection gets a shape, the two refusals that keep a shapeless one from being created, and the re-mirror rule
/// that the package enforces against the parameter's own tag and its live binding. Each claim below was wrong at
/// some point in the story's review rounds, and a copy-edit that drops one puts an agent back on the path the
/// server refuses.
/// </summary>
[TestFixture]
public sealed class CollectionParameterGuidanceTests
{
    private const string Article = "guidance/mcp/guides/processes/parameters.md";

    [Test]
    [Description("The Collection type is listed with its Out default, and the mirror is taught as the way it gets a shape.")]
    public void Guide_ShouldTeachTheCollectionTypeAndItsMirror()
    {
        string guide = ReadGuide();

        guide.Should().Contain("Collection (a record collection, `CompositeObjectList`",
            because: "an agent picks the type by name, and the platform type it maps to is what describe reports back");
        guide.Should().Contain("whose direction defaults to `Out` unless you set one",
            because: "the Out default belongs to the TYPE, not to the mirror - a bare declared collection gets it too, "
                + "and an agent that expects the Variable default writes a process parameter it did not mean to expose");
        guide.Should().Contain("the per-column\n  `itemProperties` are copied",
            because: "the shape is the contract a consumer binds to; a caller that thinks the mirror copies only the "
                + "type ships a collection nothing can read");
    }

    [Test]
    [Description("Both mirror-source refusals are stated: no item properties at all, and items without a column UId.")]
    public void Guide_ShouldNameBothMirrorSourceRefusals()
    {
        string guide = ReadGuide();

        guide.Should().Contain("mirror of a collection output that carries NO `itemProperties`",
            because: "the refusal exists so a bound, tagged, unbindable empty shape is never created, and the guide "
                + "must say it before an agent meets it as a build failure");
        guide.Should().Contain("items carry no column UId in their `tag`",
            because: "only a shape the platform stamped can be reproduced, and this second refusal was missing from "
                + "the first version of this guidance (ENG-96230 review round 3)");
        guide.Should().Contain("not `ResultEntityCollection`",
            because: "the shape-bearing output is ResultCompositeObjectList; naming the wrong one is the mistake the "
                + "original acceptance criteria made");
    }

    [Test]
    [Description("Re-mirror is scoped to the parameter's own source AND its live binding, with remove + add as the retarget path.")]
    public void Guide_ShouldScopeTheReMirrorToItsOwnSourceAndBinding()
    {
        string guide = ReadGuide();

        guide.Should().Contain("refresh its `itemProperties` from the output it was CREATED",
            because: "a re-mirror is a shape refresh, not a retarget - the guide said 'from the named collection "
                + "output' until the package proved otherwise");
        guide.Should().Contain("must still be\n  bound to that output",
            because: "the tag alone is not proof: addMapping accepts collection to collection, so a moved binding "
                + "would leave the shape following one output while the data came from another");
        guide.Should().Contain("retargeting is `removeParameter` + `addParameter`",
            because: "an agent refused on a retarget needs the path that works, or it retries the same call");
        guide.Should().Contain("The `tag` and the binding are left untouched",
            because: "the package stopped re-stamping the tag once the gate admitted only the parameter's own source");
    }

    private static string ReadGuide() =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), Article);
}

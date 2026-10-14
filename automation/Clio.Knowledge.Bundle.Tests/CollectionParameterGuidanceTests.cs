using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the ENG-96230 collection-parameter guidance in the process-parameters article: the mirror is the way a
/// collection gets a shape, the two refusals that keep a shapeless one from being created, and the re-mirror rule
/// that the package enforces against the parameter's own tag and its live binding. Each claim below was wrong at
/// some point in the story's review rounds, and a copy-edit that drops one puts an agent back on the path the
/// server refuses. ENG-95984 added the File and FileCollection types to the same list, and FileCollection is the one
/// collection describe reports by an alias rather than by its platform type, so it is the documented exception to
/// re-submitting a described collection only through its mirror.
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
            because: "an agent picks the type by name, and the platform type it maps to is what describe reports back - "
                + "for every collection but a process parameter of the FileCollection shape, which describe reports by "
                + "that alias so the described type rebuilds the shape");
        guide.Should().Contain("whose direction defaults to `Out` unless you set one",
            because: "the Out default belongs to the TYPE, not to the mirror - a bare declared collection gets it too, "
                + "and an agent that expects the Variable default writes a process parameter it did not mean to expose");
        guide.Should().Contain("the per-column\n  `itemProperties` are copied",
            because: "the shape is the contract a consumer binds to; a caller that thinks the mirror copies only the "
                + "type ships a collection nothing can read");
    }

    [Test]
    [Description("File and FileCollection are on the type list from the clio release, binary (no longer file) is on the not-supported list, and a described FileCollection is the stated exception to re-submitting a collection only through typeFromElement.")]
    public void Guide_ShouldListTheFileTypesAndTheFileCollectionException()
    {
        // Arrange
        string guide = Regex.Replace(ReadGuide(), @"\s+", " ");
        string boundary = FileParameterGuidanceTests.ClioBoundary;

        // Act
        string[] supportedTypes = [.. guide.Split("Supported types:", 2)[^1].Split(" — ", 2)[0].Split(", ")];

        // Assert
        guide.Should().Contain("Guid/Lookup/Collection/File/FileCollection),",
            because: "the parameter shape at the top of the article is the list an agent reads first");
        supportedTypes.Should().Contain(type => type.Contains("File and FileCollection", StringComparison.Ordinal),
            because: "the two types belong in the supported-types sentence itself, not in a note after it");
        guide.Should().Contain($"and, from clio {boundary}, File and FileCollection (one file and several, carried by "
                + "reference: `process-files`)",
            because: "the supported-types sentence is where an agent looks for a file type, and it must reach the guide "
                + "that owns the two types");
        guide.Should().Contain("other types (composite object / entity / binary / ...) are not supported yet, and "
                + "binary never will be: a process parameter carries a file by reference (File).",
            because: "a file is supported now, so the not-supported list names binary, which stays refused for good");
        guide.Should().NotContain("entity / file /",
            because: "the old list told agents a file could not be a process parameter");
        guide.Should().Contain("never by re-typing the shape. The one exception, from clio "
                + $"{boundary}: a PROCESS parameter described as type `FileCollection` is re-sent as that type, which "
                + "rebuilds its shape (`process-files`).",
            because: "describe reports a process-level collection of the FileCollection shape by that alias, and the "
                + "mirror refuses a file output, so re-typing is the only way to rebuild it");
        guide.Should().Contain($"from clio {boundary} `Name.Item` names an item of a collection parameter, as does "
                + "`targetProcessParameter`",
            because: "the dotted process-parameter path is part of the mapping grammar this article owns");
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

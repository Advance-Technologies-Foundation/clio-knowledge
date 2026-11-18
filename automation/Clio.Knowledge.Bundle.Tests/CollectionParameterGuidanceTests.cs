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
/// collection describe reports by an alias rather than by its platform type. The declared item shape then gave a
/// collection a second way to get a shape - its own `itemProperties` - so a described collection is replayed by how it
/// was made: a mirrored one through its mirror, a declared one as described, a FileCollection by its alias.
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
    [Description("File and FileCollection are on the type list for a File-aware clio, binary (no longer file) is on the not-supported list, and a described FileCollection is re-sent by its alias.")]
    public void Guide_ShouldListTheFileTypesAndReplayAFileCollectionByItsAlias()
    {
        // Arrange
        string guide = Regex.Replace(ReadGuide(), @"\s+", " ");

        // Act
        string[] supportedTypes = [.. guide.Split("Supported types:", 2)[^1].Split(" — ", 2)[0].Split(", ")];

        // Assert
        guide.Should().Contain("Guid/Lookup/Collection/File/FileCollection),",
            because: "the parameter shape at the top of the article is the list an agent reads first");
        supportedTypes.Should().Contain(type => type.Contains("File and FileCollection", StringComparison.Ordinal),
            because: "the two types belong in the supported-types sentence itself, not in a note after it");
        guide.Should().Contain("and, with a File-aware clio, File and FileCollection (one file and several, carried by "
                + "reference: `process-files`)",
            because: "the supported-types sentence is where an agent looks for a file type, and it must reach the guide "
                + "that owns the two types");
        guide.Should().Contain("other types (composite object / entity / binary / ...) are not supported yet, and "
                + "binary never will be: a process parameter carries a file by reference (File).",
            because: "a file is supported now, so the not-supported list names binary, which stays refused for good");
        guide.Should().NotContain("entity / file /",
            because: "the old list told agents a file could not be a process parameter");
        guide.Should().Contain("With a File-aware clio, a PROCESS parameter described as type `FileCollection` is "
                + "re-sent as that type, which rebuilds its shape (`process-files`).",
            because: "describe reports a process-level collection of the FileCollection shape by that alias, and the "
                + "mirror refuses a file output, so re-sending the alias is how it is rebuilt");
        guide.Should().Contain("with a File-aware clio `Name.Item` names an item of a collection parameter, as does "
                + "`targetProcessParameter`",
            because: "the dotted process-parameter path is part of the mapping grammar this article owns");
    }

    [Test]
    [Description("The declared item shape is taught where the bare Collection is: gated on the create contract's call shape or wording, with its member list, the Variable item default, the two-level limit, the refusals and FileCollection as the one-File form; and a described collection is replayed by how it was made - a mirrored one through typeFromElement, a declared one as described.")]
    public void Guide_ShouldTeachTheDeclaredItemShapeAndHowToReplayIt()
    {
        // Arrange
        string guide = Regex.Replace(ReadGuide(), @"\s+", " ");

        // Act
        // (nothing to act on - the guide text is the contract)

        // Assert
        guide.Should().Contain("with a clio whose create-business-process contract (`get-tool-contract`) shows "
                + "`itemProperties?` in its `parameter {...}` shape or says \"declare the columns of a Collection\"",
            because: "an older clio carries no floor for the member and an older server drops it silently, so the gate "
                + "is what clio ships with the floor - the call shape the default, shortened reply keeps, or the "
                + "sentence the full reply carries - and a default read must be able to answer it without a 44 KB full read");
        guide.Should().Contain("`itemProperties: [{name, type, direction?, caption?, description?, referenceSchema?, "
                + "itemProperties?}]` creates exactly those items, in order, each Variable unless set, with no tag.",
            because: "the member list and the item default are the write shape");
        guide.Should().Contain("an item of a top-level collection may be a Collection whose items are not collections. "
                + "Binary, a type outside the list (a described MaxSizeText or MediumText item: re-declare it as Long "
                + "text) and anything deeper are refused, naming the item",
            because: "the depth limit and the refusals are where an agent stops");
        guide.Should().Contain("`setParameter` does not reshape (remove + add)",
            because: "a reshape sent as an update is refused, and the route that works is named");
        guide.Should().Contain("`FileCollection` is this form with one item `File: File`.",
            because: "the alias and the general form store the same metadata");
        guide.Should().Contain("feed a collection back by its OWN `tag`: a tagged (MIRRORED) one through "
                + "`typeFromElement` naming its source, without its `itemProperties`; re-send an untagged (DECLARED) one "
                + "as described, `type` + `itemProperties`",
            because: "a declared collection has no source to mirror, so the old 'only through typeFromElement' rule "
                + "would leave it unrebuildable; and the parameter's own tag decides, since designer items carry tags too");
        guide.Should().Contain("a bound one's `value` is its binding: drop it and replay the binding as a mapping",
            because: "the write side refuses a constant on a collection, so a bound collection re-sent with its value "
                + "would be refused");
        guide.Should().Contain("or declare the shape yourself, `Collection` + `itemProperties`, below)",
            because: "the mirror refusal now points at itemProperties, and the guide paraphrases that refusal");
        guide.Should().NotContain("never by re-typing the shape",
            because: "re-sending a declared shape is now the supported way back");
    }

    [Test]
    [Description("A declared Lookup needs its referenceSchema, as a parameter and as an item: without one it is refused, the guide says why an older describe can read one back that way, and that a bare record id is a Guid.")]
    public void Guide_ShouldRequireTheObjectOfADeclaredLookup()
    {
        string guide = Regex.Replace(ReadGuide(), @"\s+", " ");

        guide.Should().Contain("`type: Lookup` without it is refused (older packages saved a lookup to no object); a bare record id is a Guid",
            because: "the package refuses the shape, and an agent replaying an older describe meets the refusal");
        guide.Should().Contain("Lookup only with `referenceSchema`",
            because: "a collection item is refused the same way");
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

    [Test]
    [Description("A script that fills a declared collection is shown the pattern that compiles - build a CompositeObjectList of CompositeObject rows keyed by the item names and Set it once - and why adding to the read-back interface does not; the ScriptTask core points there.")]
    public void ScriptTaskGuides_ShouldShowHowToWriteACollectionParameter()
    {
        // Arrange
        string root = ProcessGuideSet.FindRepositoryRoot();

        // Act
        string details = Regex.Replace(ProcessGuideSet.Read(root, "guidance/mcp/guides/processes/process-script-task-details.md"), @"\s+", " ");
        string core = ProcessGuideSet.Read(root, "guidance/mcp/guides/processes/process-script-task.md");

        // Assert
        details.Should().Contain("var rows = new CompositeObjectList<CompositeObject>();",
            because: "the concrete list type is the one that has Add, and an agent that guesses writes the interface");
        details.Should().Contain("Set<ICompositeObjectList<ICompositeObject>>(\"OverdueTasks\", rows);",
            because: "the list is assigned once, through the interface type the parameter carries");
        details.Should().Contain("Key each row by the collection's item names (its `itemProperties`)",
            because: "a row is read back by the target's item names, so another key is an empty column");
        details.Should().Contain("has no `Add`, so adding to `Get<...>(\"OverdueTasks\")` does not compile (CS1061)",
            because: "this exact mistake cost a compile cycle in the stand run");
        core.Should().Contain("(to write one: `process-script-task-details`)",
            because: "the core is at its reply budget, so it points at the details article instead of repeating it");
    }

    private static string ReadGuide() =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), Article);
}

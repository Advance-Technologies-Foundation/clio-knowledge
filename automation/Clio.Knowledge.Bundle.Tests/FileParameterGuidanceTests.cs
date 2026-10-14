using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the ENG-95984 guidance: the <c>process-files</c> guide (the File and FileCollection process parameter types
/// and the two-level collection rules P1, P2, P2-MI, P3, R-M1 and R-M2 that CrtProcessBuilder applies to every
/// structured mapping) and the version-gated multi-instance passages in <c>process-sub-process</c> and
/// <c>process-sub-process-when</c>. The type list and the FileCollection exception in <c>process-parameters</c> are
/// pinned beside the other collection rules, in <see cref="CollectionParameterGuidanceTests"/>.
/// </summary>
/// <remarks>
/// <see cref="ClioBoundary"/> is the clio release that ships the behaviour. It was not known when the text was
/// written, so every article states it as this one literal: correcting it at merge is one edit here and the same
/// replacement in the articles, and a missed article fails <see cref="EveryVersionGate_ShouldNameTheSameClioRelease"/>.
/// </remarks>
[TestFixture]
public sealed class FileParameterGuidanceTests
{
    /// <summary>The clio release from which the server applies the File types and the two-level rules.</summary>
    internal const string ClioBoundary = "8.1.0.142";

    private const string ItemId = "process-files";

    private const string ArticlePath = "guidance/mcp/guides/processes/files.md";

    private const string ParametersPath = "guidance/mcp/guides/processes/parameters.md";

    private const string SubProcessPath = "guidance/mcp/guides/processes/sub-process.md";

    private const string SubProcessWhenPath = "guidance/mcp/guides/processes/sub-process-when.md";

    /// <summary>
    /// The size the whole guide is designed to stay within, in the response characters of
    /// <see cref="ProcessGuideResponseSizeTests"/>: the element sections that later work adds go into the same guide,
    /// and 21,000 characters keeps its reply under the 19,000-byte inline rule (22,000 was about 19,500 bytes at the
    /// ratio measured on the process articles).
    /// </summary>
    private const int DesignTargetResponseCharacters = 21_000;

    [Test]
    [Description("process-files is registered banner-less and ungated, as process-tracing is, with no legacy route because it was born after the migration: published at its itemId path, required by the library, routed, and outside the go-live floor that only banner-carrying articles may join.")]
    public void Resource_ShouldBeRegisteredBannerLessAndRouted()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        JsonElement requirements = source.RootElement.GetProperty("requirements");

        // Act
        JsonElement resource = source.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Single(item => item.GetProperty("itemId").GetString() == ItemId);
        string[] requiredItems = [.. requirements.GetProperty("itemIds").EnumerateArray().Select(item => item.GetString()!)];
        string[] requiredUris = [.. requirements.GetProperty("resourceUris").EnumerateArray().Select(uri => uri.GetString()!)];
        string routing = Collapse(ProcessGuideSet.Read(repositoryRoot, "guidance/mcp/guides/routing.md"));
        string guide = ProcessGuideSet.Read(repositoryRoot, ArticlePath);

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
            because: "the manifest must point at the article this fixture pins");
        resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
            because: "the flat resources folder names each article by its itemId");
        resource.GetProperty("topicId").GetString().Should().Be("creatio.process-files",
            because: "the topic is the stable cross-library identity of the guide");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "the File types ship enabled; a gated guide would be hidden while its tool accepts the types");
        resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
            because: "the guide was born after the v0 migration and has no v0 route to preserve");
        requiredItems.Should().Contain(ItemId,
            because: "the library requires every item it publishes for the process tools");
        requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
            because: "the requirement lists the canonical route next to the item id");
        routing.Should().Contain("-> name=process-files (after name=process-parameters)",
            because: "an article nothing routes to is never read, and it extends the parameters article rather than replacing it");
        guide.Should().NotContain(ProcessGuideSet.SetBanner,
            because: "a banner obliges process-modeling to index the guide, and that article has no room left under the inline rule");
        ProcessGuideSet.GoLiveFloor.Should().NotContain(ItemId,
            because: "the floor may hold only banner-carrying articles split out of process-modeling, which this one is not");
    }

    [Test]
    [Description("The guide declares File as FileLocator by reference, FileCollection as a collection with one File item, their aliases and their Variable default against a generic Collection's Out, and refuses Binary, a constant and a referenceSchema.")]
    public void Guide_ShouldStateTheTwoTypes()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "`File` — ONE file, carried by reference: the platform's FileLocator type.",
            "Aliases `file`, `filelocator` and `file locator`",
            "The platform's own type NAME \"File\" is a different, byte-carrying type the flow cannot store",
            "`Binary` and `Blob` are refused: a process parameter carries a file by reference, not its bytes.",
            "`FileCollection` — SEVERAL files: a record collection (`CompositeObjectList`) with exactly ONE item, `File` (FileLocator, Variable, no tag).",
            "Aliases `filecollection`, `file collection` and `collection of files`.",
            "a FileCollection defaults to Variable; a generic Collection to Out.",
            "An explicit direction always wins.",
            "Neither takes a `value` (a file has no constant form) nor a `referenceSchema` (it is not a lookup): both are refused."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "each clause is a rule the package enforces, and an agent that has not read it declares a type the server refuses or a direction it did not mean");
        }
    }

    [Test]
    [Description("The guide teaches the read-back: File as FileLocator, a process-level collection of the FileCollection shape as FileCollection, both re-submittable, and the item-name caveat of a designer-made collection.")]
    public void Guide_ShouldTeachTheReadBackAndTheItemNameCaveat()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "A `File` reads back as type `FileLocator`.",
            "A PROCESS parameter that is a collection with no tag and exactly one FileLocator item reads back as type `FileCollection`, with its `itemProperties`.",
            "Both types re-submit as written",
            "An element's file collection keeps the platform name `CompositeObjectList`.",
            "It still reads back as `FileCollection`, with its real item name (measured on CrtProcessBuilder 1.6.6.98 on 2026-10-14).",
            "A process REBUILT from the description names the item `File`",
            "(\"has no item 'Attachment'\", followed by the items it has): rename that segment to `File`."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "describe is how an agent learns a process, and a read-back that does not rebuild, or a replayed item name the rebuild renamed, fails without this explanation");
        }
    }

    [Test]
    [Description("The guide states every two-level rule the package applies to a structured mapping (P1, P2, P2-MI, P3, R-M1, R-M2), the targets that take files, that an expression bypasses them, and the two consequences the implementation made necessary: the last mapping decides, and R-M1 also covers a single-instance sub-process's File input.")]
    public void Guide_ShouldStateEveryBindingRule()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        (string Clause, string Because)[] rules =
        [
            ($"From clio {ClioBoundary} every structured mapping (`sourceElement` + `sourceElementParameter`, or `processParameter`, without `sourceColumn`) applies the rules below",
                "the rules run on structured sources only, and a record column is not a parameter-to-parameter binding"),
            ("A collection TAKES FILES when it is (a) the `Files` input of the Process parameter variant (`ProcessFileProcessingUserTask`), or (b) a process parameter of the FileCollection shape. P2 and R-M2 apply to these targets only.",
                "P2 and R-M2 are scoped to these two targets; a reader who applies them to any collection expects a pairing that never happens"),
            ("P1 — an ITEM from an ITEM",
                "P1 is the rule that turns an item-only mapping into a two-level binding"),
            ("It applies to every collection, a multi-instance `InputRecordCollection` included",
                "P1 is type-agnostic, which is what changes the multi-instance passages"),
            ("one bound to anything else refuses the mapping",
                "P1 refuses rather than rebinds a parent, because rebinding would change what every other item reads"),
            ("A source nested deeper than the target is refused: outside its row it reads empty.",
                "the second P1 refusal"),
            ("P2 — a COLLECTION onto a collection that takes files: each file item of the target is bound to the source item of the same name (case-insensitive) or, when each side has exactly one file item, to that one (`Files.File` from `ObjectFiles.ObjectFile`).",
                "the pairing policy is what makes an outer-only mapping deliver files"),
            ("An item it cannot pair is reported (\"at run time it will be empty\"), and cleared first when it was bound elsewhere.",
                "an unpairable item is a notice, not a refusal (owner decision), and the agent must act on it"),
            ("R-M2 — a collection with NO file item onto a collection that takes files is refused (\"has no File item\")",
                "the refusal keeps a saved-green process from failing in the element at run time"),
            ("P2-MI — a mapping that binds a multi-instance `InputRecordCollection` (as its target, or through P1)",
                "iterating over records without files is legitimate, so it is a notice"),
            ("\"will be empty on every iteration\" unless you map `InputRecordCollection.<F>`",
                "the notice text is how the agent recognises the gap"),
            ("P3 — a SINGLE file (not an item) onto a file item of a single-instance owner (any element but a multi-instance sub-process, or the process itself: `DocumentsParameter.File`) clears every bound collection above that item, one notice per level",
                "P3 resets the parents (owner decision), which a designer user would not expect"),
            ("R-M1 — a single File (a process parameter, or a flat File input of an element, a single-instance sub-process's included) from an ITEM of a collection is refused (\"use a FileCollection, or a multi-instance sub-process\")",
                "R-M1 also refuses a single-instance sub-process's flat File input, and the refusal names the two alternatives"),
            ("Order matters. Onto a collection that takes files, the LAST accepted mapping decides (an item taken from another collection is refused by P1).",
                "an item mapping and an outer mapping onto the same file collection undo each other in either order"),
            ("the outer one re-pairs an item bound outside its source (P2), and its notice names the old binding.",
                "item, then outer: P2 rebinds the item"),
            ("followed by a single-file mapping onto its item: the collection is cleared (P3).",
                "outer, then item: P3 resets the parent"),
            ("An `expression` goes through as given; only its meta-path tokens are checked, and none of the rules above runs on it (the stored text has its GUIDs lower-cased).",
                "an expression source is the one route the rules do not cover, and the agent must know it bypasses them"),
            ("A misspelled token, a token from another process and a column of a collection item are refused; a token that names an item (`Files.File`) is accepted.",
                "what the meta-path check refuses and accepts")
        ];

        // Assert
        foreach ((string clause, string because) in rules)
        {
            guide.Should().Contain(clause, because: because);
        }
    }

    [Test]
    [Description("The guide is gated on the clio release and on the tool contract, and names a CrtProcessBuilder version only as inline evidence of a measurement, never as the boundary.")]
    public void Guide_ShouldGateOnTheClioReleaseAndCiteThePackageOnlyAsEvidence()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] packageVersions = [.. Regex.Matches(guide, @"CrtProcessBuilder \d+\.\d+\.\d+\.\d+")
            .Select(match => guide[Math.Max(0, match.Index - "measured on ".Length)..(match.Index + match.Length)])];

        // Assert
        guide.Should().Contain($"From clio {ClioBoundary}: use only when the FULL create-business-process contract (`get-tool-contract` with `tool-names: [\"create-business-process\"]`, `detail: \"full\"`) names type File in `parameters[]`; the default reply is shortened and lists no types, so its silence proves nothing.",
            because: "the boundary is the clio release, and the tool contract is the check an agent can make before it declares a type an older pair refuses");
        guide.Should().Contain("does NOT apply the rules below either",
            because: "an older package refuses File loudly but stores an outer-only mapping and binds an item alone in silence");
        guide.Should().NotContain("TBD",
            because: "a placeholder boundary cannot merge: the release gate refuses it");
        packageVersions.Should().NotBeEmpty(
            because: "anti-vacuity: the guide cites the measured package version as evidence");
        packageVersions.Should().OnlyContain(text => text.StartsWith("measured on ", StringComparison.OrdinalIgnoreCase),
            because: "a CrtProcessBuilder version is evidence of what was measured, not the version boundary, which is the clio release");
    }

    [Test]
    [Description("Every describe-business-process the guide names asks for mode=full or element=<name>: a plain describe of a large process is a summary without the parameters and sources the guide teaches to read.")]
    public void Guide_ShouldReadBackWithTheFullGraphOrOneElement()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] plain = [.. Regex.Matches(guide, @"describe-business-process`?(?!\s*`?\s*(?:mode=|element=))")
            .Select(match => guide[Math.Max(0, match.Index - 60)..(match.Index + match.Length)])];

        // Assert
        guide.Should().Contain("describe-business-process mode=full",
            because: "anti-vacuity: the guide teaches a read-back");
        plain.Should().BeEmpty(
            because: "the summary carries no itemProperties and no sources, so a plain read-back reads nothing this guide is about");
    }

    [Test]
    [Description("The guide stays inside the design target the whole guide must meet once the element sections join it, and inside the 19,000-byte inline reply rule.")]
    public void Guide_ShouldStayInsideItsDesignTarget()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();

        // Act
        int responseCharacters = ProcessGuideResponseSizeTests.ResponseSize(repositoryRoot, ArticlePath);
        int replyBytes = GuidanceInlineReplyBudgetTests.ReplyBytes(ProcessGuideSet.Read(repositoryRoot, ArticlePath));
        TestContext.WriteLine($"{ItemId}: {responseCharacters:N0} response characters of {DesignTargetResponseCharacters:N0}, "
            + $"{replyBytes:N0} reply bytes of {GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes:N0}");

        // Assert
        responseCharacters.Should().BeLessThanOrEqualTo(DesignTargetResponseCharacters,
            because: "the element sections still to come share this guide, so this part has to leave them room");
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline rule is spilled to a file and read back in pieces");
    }

    [Test]
    [Description("The multi-instance passages state, under the clio release gate, that a per-item value taken from an item of a collection binds InputRecordCollection too, that a different collection is refused, and that sending both mappings stays the advice; the read-back asks for one element.")]
    public void MultiInstancePassages_ShouldGateTheItemBindingOnTheClioRelease()
    {
        // Arrange
        string subProcess = ReadCollapsed(SubProcessPath);
        string subProcessWhen = ReadCollapsed(SubProcessWhenPath);

        // Act
        string[] subProcessClauses =
        [
            "MUST: send THIS mapping as well as the per-item ones below",
            $"From clio {ClioBoundary} a per-item mapping from an ITEM of a collection",
            "binds `InputRecordCollection` to that collection too, and one from a DIFFERENT collection than the bound one is refused (rule P1, `process-files`)",
            "any other per-item source still needs this mapping, and an older clio may bind only what you send, so send both.",
            "the build succeeds, `describe-business-process element=<name>` shows `InputRecordCollection` with `source: \"None\"`"
        ];
        string[] subProcessWhenClauses =
        [
            $"From clio {ClioBoundary} a per-item value taken from an item of a collection binds that collection too (`process-files`)",
            "any other per-item value still needs the first mapping, and an older clio may need it in every case, so send both."
        ];

        // Assert
        foreach (string clause in subProcessClauses)
        {
            subProcess.Should().Contain(clause,
                because: "process-sub-process owns the multi-instance mappings, and the item-only behaviour changed at the clio release: one iteration became one per item");
        }
        foreach (string clause in subProcessWhenClauses)
        {
            subProcessWhen.Should().Contain(clause,
                because: "the decision article restates the two-mapping rule, so it must carry the same gate");
        }
    }

    [Test]
    [Description("Every version gate this change wrote names the same clio release, so correcting the literal at merge cannot leave one article behind.")]
    public void EveryVersionGate_ShouldNameTheSameClioRelease()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string manifest = File.ReadAllText(Path.Combine(repositoryRoot, "bundle-source.json"));

        // Act
        (string Path, int Gates)[] gates = [.. new[] { ArticlePath, ParametersPath, SubProcessPath, SubProcessWhenPath }
            .Select(path => (path, Regex.Matches(ReadCollapsed(path), $@"clio {Regex.Escape(ClioBoundary)}\b").Count))];

        // Assert
        gates.Should().OnlyContain(article => article.Gates > 0,
            because: "each of these articles states a behaviour that starts at the clio release");
        manifest.Should().Contain($"From clio {ClioBoundary}.",
            because: "the catalog description of process-files names the release too");
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

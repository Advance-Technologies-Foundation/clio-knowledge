using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the guidance for the Process file element (<c>fileProcessing</c>, CrtProcessBuilder 1.6.6.103, with the
/// <c>report</c> and <c>files</c> groups from 1.6.6.109, the first released cut that carries them): its sections of <c>process-files</c>, which owns the block
/// (the Object attachments and Process parameter groups), and the buildable row and dedicated-type entry of
/// <c>process-element-catalog</c>. The Generated report group lives in <c>process-files-report</c>, pinned by
/// <see cref="ProcessFileReportGuidanceTests"/>. The File and FileCollection parameter rules of the same guide are pinned by
/// <see cref="FileParameterGuidanceTests"/>, which also holds the guide's size target.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each clause is a rule the package enforces or a fact measured on a stand, and the tests fail when one is
/// reworded or dropped. The measured facts carry their version (and the date, where one is given) in the clause
/// itself, so the evidence cannot be trimmed away from the claim.
/// </remarks>
[TestFixture]
public sealed class ProcessFileGuidanceTests
{
    private const string ArticlePath = "guidance/mcp/guides/processes/files.md";

    private const string CatalogPath = "guidance/mcp/guides/processes/element-catalog.md";

    private const string SectionHeading = "== The Process file element: `fileProcessing` ==";

    private const string ProcessParameterHeading = "== The Process parameter variant: the `files` group ==";

    [Test]
    [Description("The element section gates the type on the contract, the 1.6.6.103 floor and the 1.6.6.109 floor a clio that also offers report and files applies to every group, says the group sent is the variant among the three groups, that an older package refuses report and files, and that the generic userTask route is refused for all three Process file tasks.")]
    public void Section_ShouldGateTheElementAndMakeTheGroupTheVariant()
    {
        // Arrange
        string section = ReadSection();

        // Act
        string[] clauses =
        [
            "Build it only when the FULL contract (as above) names `fileProcessing` (server alias `processFile`)",
            "such a clio refuses a CrtProcessBuilder older than 1.6.6.103 up front (1.6.6.109, for every group, if it also offers report and files), as above.",
            "The block carries ONE group, and the group IS the variant",
            "`attachments` (Object attachments), `report` (Generated report: `process-files-report` owns it) or `files` (Process parameter, last section)",
            "1.6.6.103 and 1.6.6.104 write attachments only and refuse the other two by name.",
            "Omit `userTaskName`.",
            "The generic userTask route naming any of the three Process file tasks is refused: build them only here."
        ];

        // Assert
        section.Should().NotBeEmpty(because: "anti-vacuity: process-files carries the element section");
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "a block with no group or two groups is refused, a server older than the floor refuses the type, and the generic route builds no Process file element at all");
        }
        section.Should().NotContain("(1.6.6.106 if it offers report and files)",
            because: "the floor is per command, not per group: a clio that also offers report and files refuses an older package for an attachments element too");
    }

    [Test]
    [Description("The attachments members: the object and its storage, the record scope and its sources, setFilter replacing the other conditions around the scope, the 50-file create default that does NOT read all and a kept count on setElement, and the one-object sort.")]
    public void Section_ShouldStateTheAttachmentsMembers()
    {
        // Arrange
        string section = ReadSection();

        // Act
        string[] clauses =
        [
            "`attachments.object`: the record object or its attachment object (`AccountFile`).",
            "SysFile storage is refused in this version, on either side and by `setFilter`.",
            "`value` (an existing record's id), `processParameter`, `sourceElement` + `sourceElementParameter` (+ `sourceColumn`), or the `expression` `[#SysVariable.CurrentUserContact#]` for Contact.",
            "A structured source naming an item is refused here and in `saveTo.recordId`: the element reads its record once per run.",
            "It becomes the filter's record scope: `setFilter` REPLACES the other conditions and keeps the scope - send them all in one call, never the scope column.",
            "`attachments.numberOfRecords`: 1..5000; omitted on create, it defaults to 50 and does NOT read all; omitted on `setElement`, the stored count stays.",
            "`attachments.sort`: ONE object `{column, direction}`, a direct column of the attachment object. Never an array."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "unlike readData an omitted count is not every file, and a sort sent as an array is lost without a refusal that names it");
        }
    }

    [Test]
    [Description("The action per variant, the measured fact that ObjectFiles keeps the source files after a save, and the empty-filter refusal outside the operation loop.")]
    public void Section_ShouldStateTheActionAndWhereTheCopiesAre()
    {
        // Arrange
        string section = ReadSection();

        // Act
        string[] clauses =
        [
            "`action`: `saveToAttachments` copies the files to the `saveTo` record",
            "`useInProcess` hands them on. Omitted on create, it follows from `saveTo`.",
            "A constant `saveTo.recordId` must be an existing record; a mapped one gets a notice.",
            "After saving, `ObjectFiles` points at the SOURCE files; the copies are only in `CreatedObjectFileIds` (measured on 1.6.6.100, 2026-10-15)",
            "switching to `useInProcess`, or moving `attachments.object` or `saveTo.object` to another object, is refused while an element reads it.",
            "A saving element with neither `recordId` nor a filter condition would copy arbitrary files: refused at the end of the request (`failedOperationIndex: null`)."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "an agent that iterates ObjectFiles after a save touches the originals, and a saving element with no scope copies arbitrary files");
        }
    }

    [Test]
    [Description("setElement merges member by member, and the read-back asks for one element or the full graph, separates identity checks from describe-only members, and tells the agent to remove the describe-only ones.")]
    public void Section_ShouldTeachTheMergeAndTheReadBack()
    {
        // Arrange
        string section = ReadSection();

        // Act
        string[] clauses =
        [
            "`setElement` merges member by member: an omitted member keeps its value; `{}` clears `attachments.recordId` or `attachments.sort`; `saveTo.linkColumn: \"\"` derives the column again, never clears it.",
            "Another `attachments.object` clears the stored filter and sort: resend `recordId`, `sort` and setFilter",
            "Read it back with `describe-business-process element=<name>` or `mode=full`.",
            "`source`, `storage`, `fileObject` and `attachments.linkColumn` are identity checks, never settings: drop them (and `saveTo.linkColumn`) when changing the object.",
            "Remove the describe-only members before resubmitting: `actionStored`, `outputs`, `issues` and `display` on every value source."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "a resubmitted description must rebuild the element, and a partial setElement must not lose what it did not send");
        }
    }

    [Test]
    [Description("The endpoint sentence is the shipped pattern and the retired 'rarely an endpoint' appears nowhere; the consumers this tool cannot build are named as not buildable YET; the copies are re-selected with Read data, never Modify data, and the re-find carries its hazards.")]
    public void Section_ShouldStateTheEndpointConsumersAndTheReFind()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        string section = ReadSection();
        string[] everyProcessArticle = [.. ProcessGuideSet.Declared(repositoryRoot)
            .Select(article => Collapse(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)))];

        // Act
        string[] clauses =
        [
            "Shipped product processes end in a record's attachments (Generated report); when a following element needs the files, map its output collection",
            "Send email attachments and Creatio.ai file inputs are not buildable here yet.",
            "iterate `CreatedObjectFileIds` with a multi-instance sub-process",
            "re-select them with Read data (not Modify data) on the target's attachment object, filtered on its link column = the target record and `CreatedOn` `CurrentHour`.",
            "Hazards: it also matches others' files from that hour, and misses a copy made before the hour turned."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "the shipped processes end in attachments, the consumers need slots this tool cannot create, and the re-find pattern is only safe with its hazards stated");
        }
        everyProcessArticle.Should().NotContain(text => text.Contains("rarely an endpoint", StringComparison.OrdinalIgnoreCase),
            because: "the shipped corpus contradicts it: most uses end in a record's attachments");
        section.Should().NotContain("Creatio cannot",
            because: "the platform can; only this tool cannot build it yet");
        section.Should().NotContain("(`report` and `files` are refused)",
            because: "the retired sentence told agents the two variants could not be built, which this cut changed");
    }

    [Test]
    [Description("The catalog lists fileProcessing as buildable from 1.6.6.103 with report and files from 1.6.6.109, refuses the generic route for all three Process file tasks, names the guide of each group, and maps all three data-ids to the one dedicated type.")]
    public void Catalog_ShouldListTheElementAndItsDedicatedType()
    {
        // Arrange
        string catalog = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), CatalogPath));

        // Act
        string[] clauses =
        [
            "- `fileProcessing` (Process file, alias `processFile`), from CrtProcessBuilder **1.6.6.103**:",
            "`attachments` (Object attachments), and from **1.6.6.109** also `report` (Generated report) and `files` (Process parameter) - `process-files` owns the block, `process-files-report` its `report` group.",
            "On the generic `userTask` route all three Process file tasks (`ObjectFileProcessingUserTask`, `ReportFileProcessingUserTask`, `ProcessFileProcessingUserTask`) are REFUSED: that route carries no file source, record or target.",
            "SEVEN user tasks have their own dedicated build type",
            "`objectFileProcessingUserTask`, `reportFileProcessingUserTask` and `processFileProcessingUserTask` -> `type:\"fileProcessing\"` with the matching `attachments`, `report` or `files` group, see `process-files`"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            catalog.Should().Contain(clause,
                because: "the catalog owns what builds today; the generic route builds a Process file element that saves green and fails at run time");
        }
        catalog.Should().NotContain("are not buildable yet",
            because: "the Generated report and Process parameter elements build through their groups now");
        catalog.Should().NotContain("the server does not refuse that route",
            because: "the generic route is refused for every Process file task now");
    }

    [Test]
    [Description("The Process parameter section: saving only, exactly one source and the refused value, both levels for a collection, one file for a single File, an unset single File stops the process, an empty collection copies nothing, an expression goes through as given, the outputs, and the block-level describe-only bindingLevels with its issues: none means the platform refuses to start the process, a stored useInProcess or an invalid action fails at run time, and a mapped action is decided at run time.")]
    public void Section_ShouldStateTheProcessParameterGroup()
    {
        // Arrange
        string section = ReadSection(ProcessParameterHeading);

        // Act
        string[] clauses =
        [
            "It copies files the process holds to a record's attachments, and only that: saveTo is required on create and useInProcess is refused.",
            "`files` takes exactly ONE of processParameter, sourceElement + sourceElementParameter, or expression. A value is refused (a file has no constant form)",
            "a source with no file item or with several, a parameter of the element itself, and any mapping FROM its Files.",
            "A collection (a FileCollection, or an element's ObjectFiles or ReportFiles) binds BOTH levels, Files and Files.File: one copy per row.",
            "A single File binds Files.File only (P3) and copies that one file.",
            "A single File must be set before the element runs: an unset one stops the process with an error.",
            "An empty collection copies nothing, silently.",
            "An expression goes onto Files.File as given, with a notice: nothing checks that it yields one file.",
            "Outputs: ObjectFiles (item ObjectFile) holds the COPIES, and CreatedObjectFileIds their ids.",
            "Describe adds the describe-only fileProcessing.bindingLevels (both, itemOnly, outerOnly or none), with an issue for a stored shape that would not resubmit as stored (none: nothing bound, the platform refuses to start the process) and for an action other than save: a stored useInProcess or an invalid one fails at run time, a mapped one is decided at run time.",
            "Remove it before resubmitting."
        ];

        // Assert
        section.Should().NotBeEmpty(because: "anti-vacuity: process-files carries the Process parameter section");
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "the variant only saves, an unset single File fails the run with an error, and an empty collection succeeds while copying nothing");
        }
        section.Should().NotContain("files.bindingLevels",
            because: "describe writes bindingLevels on the fileProcessing block, not inside the files group");
        section.Should().NotContain("a stored action other than save (it fails at run time)",
            because: "a mapped action is decided at run time and does not necessarily fail; only a stored useInProcess or an invalid action does");
    }

    [Test]
    [Description("P3 carries its run-time evidence: a nested-only binding of the Process parameter element's Files to one File was measured to copy one file.")]
    public void P3_ShouldCarryTheNestedOnlyMeasurement()
    {
        // Arrange
        string guide = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath));

        // Act
        bool measured = guide.Contains(
            "and reads as one row at run time (measured on 2026-10-19 with the Process parameter element: one copy).",
            StringComparison.Ordinal);

        // Assert
        measured.Should().BeTrue(
            because: "the single-File arm of the files group rests on this measurement, and the claim must keep its evidence");
        guide.Should().NotContain("not measured on a stand yet).",
            because: "the P3 one-row reading has been measured, so the old caveat would understate the evidence");
    }

    [Test]
    [Description("Recipe 2 keeps its evidence: the copy through a designer-made Process parameter element, names and bytes equal, was measured on a stand on CrtProcessBuilder 1.6.6.98.")]
    public void Recipe2_ShouldCarryTheMeasuredPackageVersion()
    {
        // Arrange
        string guide = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath));

        // Act
        bool measured = guide.Contains(
            "Measured on CrtProcessBuilder 1.6.6.98 on a stand, on designer-made elements: one run of recipes 1 and 2 copied the three source files to the target record, names and bytes equal.",
            StringComparison.Ordinal);

        // Assert
        measured.Should().BeTrue(
            because: "a measurement names the package version it was made on, so a reader can tell whether a later package is covered");
    }

    [Test]
    [Description("The element section names no internal task: no Jira key, pull request link or issue number.")]
    public void Section_ShouldNotCarryInternalReferences()
    {
        // Arrange
        string section = ReadSection();

        // Act
        bool namesATask = Regex.IsMatch(section, @"\b[A-Z][A-Z0-9]+-\d+\b|/pull/\d+|#\d+\b");

        // Assert
        namesATask.Should().BeFalse(because: "published guidance names no Jira key, pull request or issue number");
    }

    private static string ReadSection(string heading = SectionHeading)
    {
        string guide = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath));
        int start = guide.IndexOf(heading, StringComparison.Ordinal);
        return start < 0 ? string.Empty : guide[start..];
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

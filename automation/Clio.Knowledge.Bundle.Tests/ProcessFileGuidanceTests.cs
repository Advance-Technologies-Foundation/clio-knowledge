using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the guidance for the Process file element (<c>fileProcessing</c>, CrtProcessBuilder 1.6.6.102): its section of
/// <c>process-files</c>, which owns the block, and the buildable row and dedicated-type entry of
/// <c>process-element-catalog</c>. The File and FileCollection parameter rules of the same guide are pinned by
/// <see cref="FileParameterGuidanceTests"/>, which also holds the guide's size target.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each clause is a rule the package enforces or a fact measured on a stand, and the tests fail when one is
/// reworded or dropped. The two measured facts carry their version and date in the clause itself, so the evidence
/// cannot be trimmed away from the claim.
/// </remarks>
[TestFixture]
public sealed class ProcessFileGuidanceTests
{
    private const string ArticlePath = "guidance/mcp/guides/processes/files.md";

    private const string CatalogPath = "guidance/mcp/guides/processes/element-catalog.md";

    private const string SectionHeading = "== The Process file element: `fileProcessing` ==";

    [Test]
    [Description("The element section gates the type on the contract and the 1.6.6.102 floor, and says the group sent is the variant, with only the Object attachments group written in this version.")]
    public void Section_ShouldGateTheElementAndMakeTheGroupTheVariant()
    {
        // Arrange
        string section = ReadSection();

        // Act
        string[] clauses =
        [
            "Build it only when the FULL contract (as above) names `fileProcessing` (server alias `processFile`)",
            "such a clio refuses a CrtProcessBuilder older than 1.6.6.102 up front",
            "The block carries ONE group, and the group IS the variant",
            "`attachments` (Object attachments) is the only one this version writes (`report` and `files` are refused).",
            "Omit `userTaskName`."
        ];

        // Assert
        section.Should().NotBeEmpty(because: "anti-vacuity: process-files carries the element section");
        foreach (string clause in clauses)
        {
            section.Should().Contain(clause,
                because: "a block with no group, two groups or a group this version does not write is refused, and a server older than the floor refuses the type");
        }
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
            "A constant `saveTo.recordId` must be an existing record; a mapped one gets a notice (it may be empty at run time).",
            "After saving, `ObjectFiles` points at the SOURCE files; the copies are only in `CreatedObjectFileIds` (measured on CrtProcessBuilder 1.6.6.100 on 2026-10-15)",
            "switching to `useInProcess` is refused while an element reads it.",
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
            "Shipped product processes end in a record's attachments (via Generated report); when a following element needs the files, map the element's output collection",
            "Send email attachments and Creatio.ai file inputs are not buildable through this tool yet.",
            "iterate `CreatedObjectFileIds` with a multi-instance sub-process",
            "re-select them with Read data (not Modify data) on the target's attachment object, filtered on its link column = the target record and `CreatedOn` `CurrentHour`.",
            "Hazards: that also matches files anyone added to the record within the hour, and misses a copy made before the hour turned."
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
    }

    [Test]
    [Description("The catalog lists fileProcessing as buildable from 1.6.6.102, refuses only the Object schema on the generic route, keeps the Report and Process-parameter elements out of the generic route, and maps the data-id to the dedicated type among FIVE.")]
    public void Catalog_ShouldListTheElementAndItsDedicatedType()
    {
        // Arrange
        string catalog = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), CatalogPath));

        // Act
        string[] clauses =
        [
            "- `fileProcessing` (Process file, alias `processFile`), from CrtProcessBuilder **1.6.6.102**:",
            "this version builds the Object attachments variant (`attachments`) only - `process-files` owns the block.",
            "On the generic `userTask` route only `ObjectFileProcessingUserTask` is refused; the Generated report and Process parameter elements (`ReportFileProcessingUserTask`, `ProcessFileProcessingUserTask`) are not buildable yet; do not build them as a generic `userTask` - the server does not refuse that route, and the element it saves is unconfigured.",
            "FIVE user tasks have their own dedicated build type",
            "`objectFileProcessingUserTask` -> `type:\"fileProcessing\"` with its `attachments` group, see `process-files`"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            catalog.Should().Contain(clause,
                because: "the catalog owns what builds today; the generic route builds a Process file element that saves green and fails at run time");
        }
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

    private static string ReadSection()
    {
        string guide = Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath));
        int start = guide.IndexOf(SectionHeading, StringComparison.Ordinal);
        return start < 0 ? string.Empty : guide[start..];
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

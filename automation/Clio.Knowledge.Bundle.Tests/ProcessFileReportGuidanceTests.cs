using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the <c>process-files-report</c> guide: the Generated report variant (the <c>report</c> group) of the Process
/// file element, split out of <c>process-files</c> when that guide reached its design target. The rest of the element
/// and the File types stay in <c>process-files</c>, pinned by <see cref="ProcessFileGuidanceTests"/> and
/// <see cref="FileParameterGuidanceTests"/>.
/// </summary>
/// <remarks>
/// TRIM ALARMS: each clause is a rule the package enforces or a fact measured on a stand. The measured lifetime of the
/// temporary report files says it was measured on a stand, and the sub-process and error cases say they are read from
/// source, so neither can be trimmed into a stronger claim than the evidence.
/// </remarks>
[TestFixture]
public sealed class ProcessFileReportGuidanceTests
{
    private const string ItemId = "process-files-report";

    private const string ArticlePath = "guidance/mcp/guides/processes/files-report.md";

    private const string ParentPath = "guidance/mcp/guides/processes/files.md";

    /// <summary>
    /// The same design target <see cref="FileParameterGuidanceTests"/> holds <c>process-files</c> to, in the response
    /// characters of <see cref="ProcessGuideResponseSizeTests"/>: it keeps the reply under the 19,000-byte inline rule
    /// with room for the report notices a later cut adds.
    /// </summary>
    private const int DesignTargetResponseCharacters = 21_000;

    [Test]
    [Description("process-files-report is registered banner-less and ungated with no legacy route, as process-files is: published at its itemId path, required by the library, routed after process-files, and pointed to by process-files.")]
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
        string parent = Collapse(ProcessGuideSet.Read(repositoryRoot, ParentPath));

        // Assert
        resource.GetProperty("sourcePath").GetString().Should().Be(ArticlePath,
            because: "the manifest must point at the article this fixture pins");
        resource.GetProperty("bundlePath").GetString().Should().Be($"resources/{ItemId}.md",
            because: "the flat resources folder names each article by its itemId");
        resource.GetProperty("topicId").GetString().Should().Be("creatio.process-files-report",
            because: "the topic is the stable cross-library identity of the guide");
        resource.GetProperty("role").GetString().Should().Be("guidance",
            because: "get-guidance serves only guidance resources");
        resource.TryGetProperty("requiredFeatures", out _).Should().BeFalse(
            because: "the report group ships enabled; a gated guide would be hidden while its tool accepts the group");
        resource.TryGetProperty("legacyUris", out _).Should().BeFalse(
            because: "the guide was born after the v0 migration and has no v0 route to preserve");
        requiredItems.Should().Contain(ItemId,
            because: "the library requires every item it publishes for the process tools");
        requiredUris.Should().Contain(resource.GetProperty("uri").GetString(),
            because: "the requirement lists the canonical route next to the item id");
        routing.Should().Contain("-> name=process-files-report (after name=process-files)",
            because: "an article nothing routes to is never read, and it extends process-files rather than replacing it");
        parent.Should().Contain("its report group: `process-files-report`",
            because: "an agent that opened process-files for the element must learn where the report group lives");
        guide.Should().NotContain(ProcessGuideSet.SetBanner,
            because: "a banner obliges process-modeling to index the guide, and that article has no room left under the inline rule");
    }

    [Test]
    [Description("The printable: a list-printables templateId preferred, a macro or a caption; an ambiguous caption narrowed by the record's object or refused with candidates; MS Word printables of an object only; FastReport and DevExpress refused; an unknown one refused with the Report setup hint and candidates; type and object are identity checks.")]
    public void Guide_ShouldStateThePrintableRules()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "Pass the `templateId` that `list-printables` returns (preferred), a `[#Lookup...#]` macro, or a caption (letter case ignored; matched in the culture of the user clio connects as).",
            "A caption several printables carry is narrowed to the one of the object of `report.recordId` (a Lookup process parameter or element output, without `sourceColumn`) when exactly one matches; otherwise it is refused, listing the candidates (up to 10) with their id, type and object.",
            "Only MS Word printables of an object are accepted. FastReport and DevExpress printables are refused, and so is a printable with no object (neither its own nor its section's).",
            "A printable that does not exist on this environment is refused with \"create it in System Designer -> Report setup\": the element cannot create one, so ask the user to, then retry.",
            "The refusal lists up to 10 MS Word printables by `templateId`, of the record's object when it can be told. Nothing is created or picked for you.",
            "`report.type` and `report.object` are identity checks derived from the printable, never settings"
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "a printable the package refuses must not be retried unchanged, and a missing one is the user's to create");
        }
    }

    [Test]
    [Description("The rights sentence says the printable lookup and the record selection apply NO record permissions (UseAdminRights = false), never that they run with the rights of the user the process runs as, and that only the rights cases and a printable missing on another run-time environment escape the design-time checks.")]
    public void Guide_ShouldStateThatNoRecordPermissionsApply()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        bool statesNoRights = guide.Contains(
            "the printable lookup and the record selection apply NO record permissions (`UseAdminRights = false` in the query means that no rights are checked at all), so every user sees the same printables.",
            StringComparison.Ordinal);

        // Assert
        statesNoRights.Should().BeTrue(
            because: "UseAdminRights = false on an EntitySchemaQuery turns rights checking off, so the list does not depend on who configures or runs the process");
        guide.Should().Contain("Generating the Word file is different: it reads the printable and the record's columns WITH rights checks",
            because: "the Word generator reads the printable and the merge fields with rights on, so the run can fail or fill blanks even though selection ignores rights");
        guide.Should().Contain("a record whose columns that user cannot read is generated with those fields blank, reporting success.",
            because: "a blank-field report that reports success is the silent half of the rights split");
        guide.Should().Contain("A printable missing on the environment you build on is refused (above) and reported by describe; no design-time check sees the rights cases, or a printable missing only on another environment the process runs on.",
            because: "the missing printable is caught where the agent builds; only the rights cases and another run-time environment go unseen");
        guide.Should().NotContain("No design-time check sees either.",
            because: "that sentence denied the design-time refusal and the describe issue a missing printable gets");
        guide.Should().NotContainEquivalentOf("rights of the user the process runs as",
            because: "that reading of UseAdminRights = false is the opposite of what the platform does");
        guide.Should().NotContainEquivalentOf("the caller's rights",
            because: "no rights are applied, so there is no caller whose rights decide the list");
    }

    [Test]
    [Description("The records: report.recordId is the record scope stored as Id = value, setFilter keeps it, the same scope twice is refused, a Word report with no scope and no filter is refused at the end of the request, the record order is unspecified, a printable swap keeps or clears the filter by object, and clearFilter keeps the scope and is refused with the printable missing.")]
    public void Guide_ShouldStateTheRecordScopeAndTheSwap()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "`report.recordId` is the record scope: it is stored as `Id = value` on the report's object",
            "`setFilter` writes the other conditions on that object and keeps the scope; another `filter.object` is refused, and so is an `Id` equality condition beside `report.recordId` (the same scope set twice).",
            "A Word report with neither a record scope nor a filter condition would generate one for EVERY record of its object: refused at the end of the request (`failedOperationIndex: null`), whatever the action.",
            "The records are not sorted: their order, and so the order of the files and their `(n)` numbers, is unspecified.",
            "Another printable of the SAME object keeps the filter, the scope and the suffix column, and checks the stored scope again. One of ANOTHER object clears those of the three that are stored, with a notice: a `report.recordId` sent with the change becomes the new scope",
            "That move is refused while another element reads the element's `CreatedObjectFileIds`.",
            "`clearFilter` keeps the scope too. Both are refused while the stored printable is missing on this environment: set `report.printable` first."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "a report without a scope generates a file for every record, and a printable swap silently changes which records are selected");
        }
    }

    [Test]
    [Description("The file names: separateReports written true when omitted on create or with a new printable and kept otherwise, an explicit false kept with a notice and clearing the suffix column, the caption read in the runner's culture, the constant suffix stored as a localizable value with its culture not measured, and the text-column suffix with its preconditions.")]
    public void Guide_ShouldStateTheFileNameRules()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "Omitted, it is written `true` on create or when the printable changes, as the designer forces, and the stored value is kept otherwise; an explicit `false` is kept, with a notice",
            "`false` also clears a stored `fileNameSuffixColumn`, with a notice.",
            "A file is named after the printable's caption, read in the culture of the user the process runs as",
            "a constant is stored as a localizable value, as the designer stores it (how it reads under another culture is not measured).",
            "`fileNameSuffixColumn`: the code of a TEXT column of the report's object, with separate reports on and without `fileNameSuffix`.",
            "Only the text types the designer offers are accepted (localizable text too); a hashed, secure or color column is refused: the designer does not offer them, and the run time reads the column with no rights checks and writes its value into every file name."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "the file name is what a following element or a user finds the report by, and an unmeasured culture case must not read as verified");
        }
    }

    [Test]
    [Description("Where the files go: several records into one target raise a notice, use-in-process files are temporary with their measured lifetime, a sub-process handing them out through an Out or a Variable parameter hands over deleted files, the Out-or-Variable, filter-without-scope, PDF and designer-feature notices.")]
    public void Guide_ShouldStateWhereTheFilesGo()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "A filter that can select several records attaches every report to the ONE `saveTo` record, with a notice",
            "Use in process: the files are temporary and go on as `ReportFiles`",
            "They live while the process instance runs and are deleted when it completes (measured on a stand:",
            "Read from source, not measured: a sub-process deletes the files it generated when IT completes, so one that hands `ReportFiles` to its caller through an Out or a Variable parameter (Variable is the default, for a FileCollection too) hands over deleted files: save them inside the sub-process.",
            "an instance ending in an error keeps its temporary files (only completion and cancel delete them).",
            "A notice warns when they flow into an Out or a Variable process parameter",
            "Another warns when the element has a filter but no `report.recordId`: it generates one temporary file per matching record.",
            "a printable that asks for PDF (`ConvertInPDF`) gets it only with a Word-to-PDF converter package installed; otherwise the files are .docx",
            "The group has no `sort`: do not send one."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "a temporary file handed out of the process is gone when the instance completes, and the evidence for that has a known gap");
        }
        guide.Should().NotContain("measured on 2026-",
            because: "the lifetime is attributed to a stand measurement without a date");
        guide.Should().NotContain("flow into an Out process parameter",
            because: "a Variable parameter, the default direction, hands the files to a caller as well");
    }

    [Test]
    [Description("The Add data pattern: the row the report is generated for is added first with the columns the printable prints and its RecordId feeds report.recordId; the example declares its process parameters, says how the shipped process links its files instead, and says that a designer save of the element clears the explicit link column, leaving the files linked to no record.")]
    public void Guide_ShouldTeachAddDataFirst()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] clauses =
        [
            "== Add data creates the report's data row first ==",
            "the report takes its id from the Add data output `RecordId`.",
            "{\"sourceElement\": \"AddDNSGuideReport\", \"sourceElementParameter\": \"RecordId\"}",
            "\"values\": [{\"column\": \"SenderDomain\", \"processParameter\": \"SenderDomainParameter\"}, {\"column\": \"SiteUrl\", \"processParameter\": \"SiteUrlParameter\"}]",
            "\"saveTo\": {\"object\": \"DNSGuideFile\", \"linkColumn\": \"SenderDomain\", \"recordId\": {\"processParameter\": \"SenderDomainParameter\"}}",
            "\"parameters\": [{\"name\": \"SenderDomainParameter\", \"type\": \"Lookup\", \"referenceSchema\": \"SenderDomain\"}, {\"name\": \"SiteUrlParameter\", \"type\": \"Text\"}]",
            "a column left unset prints blank, with no error.",
            "The shipped process that generates it saves to DNSGuideFile too, naming the sender domain as the record but no link column, and links the files to it afterwards with a Modify data on the file name.",
            "`saveTo.recordId` takes a Lookup of SenderDomain, as declared, or a Guid.",
            "the save is accepted with a notice: opening and saving the element in the process designer clears that column, and the files are then linked to no record."
        ];

        // Assert
        foreach (string clause in clauses)
        {
            guide.Should().Contain(clause,
                because: "a report on a record the process creates needs that record first, and RecordId is the only output Add data has");
        }
        guide.Should().NotContain("where the shipped process that generates it saves it",
            because: "the shipped process saves to the same object but writes no link column; it links its files with a Modify data afterwards");
    }

    [Test]
    [Description("The Add data then report example is one whole create-business-process descriptor: it parses as JSON, and every parameter, element and flow end it names is declared in it.")]
    public void AddDataExample_ShouldBeACompleteDescriptor()
    {
        // Arrange
        string guide = ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), ArticlePath);
        int start = guide.IndexOf("{\"name\": \"UsrSenderDomain_GenerateDNSGuide\"", StringComparison.Ordinal);

        // Act
        using JsonDocument descriptor = JsonDocument.Parse(start < 0 ? "{}" : BalancedObject(guide, start));
        JsonElement root = descriptor.RootElement;
        string[] parameters = root.TryGetProperty("parameters", out JsonElement declared)
            ? [.. declared.EnumerateArray().Select(parameter => parameter.GetProperty("name").GetString()!)]
            : [];
        string[] elements = root.TryGetProperty("elements", out JsonElement nodes)
            ? [.. nodes.EnumerateArray().Select(element => element.GetProperty("name").GetString()!)]
            : [];
        string[] flowEnds = root.TryGetProperty("flows", out JsonElement flows)
            ? [.. flows.EnumerateArray().SelectMany(flow => new[] { flow.GetProperty("source").GetString()!, flow.GetProperty("target").GetString()! })]
            : [];
        string[] parameterReferences = [.. Regex.Matches(root.GetRawText(), @"""processParameter"":\s*""(\w+)""").Select(match => match.Groups[1].Value)];
        string[] elementReferences = [.. Regex.Matches(root.GetRawText(), @"""sourceElement"":\s*""(\w+)""").Select(match => match.Groups[1].Value)];
        string[] types = root.TryGetProperty("elements", out JsonElement typed)
            ? [.. typed.EnumerateArray().Select(element => element.GetProperty("type").GetString()!)]
            : [];

        // Assert
        start.Should().BeGreaterThanOrEqualTo(0, because: "anti-vacuity: the guide carries the whole descriptor");
        root.TryGetProperty("packageName", out _).Should().BeTrue(
            because: "create-business-process refuses a descriptor without the package it is created in");
        types.Should().Equal(["startEvent", "addData", "fileProcessing", "endEvent"],
            because: "the minimal process is a start, the Add data, the report and an end");
        parameterReferences.Should().NotBeEmpty(because: "anti-vacuity: the example maps process parameters");
        parameterReferences.Should().OnlyContain(name => parameters.Contains(name),
            because: "a mapping from an undeclared process parameter is refused, so the example would not build as written");
        elementReferences.Should().OnlyContain(name => elements.Contains(name),
            because: "a source element the descriptor does not declare is refused");
        flowEnds.Should().NotBeEmpty(because: "anti-vacuity: the example connects its elements");
        flowEnds.Should().OnlyContain(name => elements.Contains(name),
            because: "a flow end that names no element is refused");
        elements.Should().OnlyContain(name => flowEnds.Contains(name),
            because: "an element no flow reaches is left unconnected");
    }

    [Test]
    [Description("The read-back lists the report members, the identity checks and the describe-only caption and convertToPdf, which are removed before resubmitting; every describe it names asks for one element or the full graph.")]
    public void Guide_ShouldTeachTheReadBack()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        string[] plain = [.. Regex.Matches(guide, @"describe-business-process`?(?!\s*`?\s*(?:mode=|element=))")
            .Select(match => guide[Math.Max(0, match.Index - 60)..(match.Index + match.Length)])];

        // Assert
        guide.Should().Contain("the identity checks `type` and `object`, and the describe-only `caption` and `convertToPdf`",
            because: "describe-only members are dropped by the write contract, and a resubmitted description must rebuild the element");
        guide.Should().Contain("Remove `caption` and `convertToPdf`, with the describe-only members `process-files` lists, before resubmitting.",
            because: "the agent resubmits what it read, so it must know which members to strip");
        plain.Should().BeEmpty(
            because: "the summary carries no element blocks, so a plain read-back reads nothing this guide is about");
    }

    [Test]
    [Description("The guide names no internal task and no clio release, and cites a CrtProcessBuilder version nowhere: the version boundary is owned by process-files and the catalog.")]
    public void Guide_ShouldNotCarryInternalReferencesOrReleaseNumbers()
    {
        // Arrange
        string guide = ReadCollapsed(ArticlePath);

        // Act
        bool namesATask = Regex.IsMatch(guide, @"\b[A-Z][A-Z0-9]+-\d+\b|/pull/\d+|#\d+\b");
        bool namesARelease = Regex.IsMatch(guide, @"(?:clio|CrtProcessBuilder) \d+\.\d+\.\d+");

        // Assert
        namesATask.Should().BeFalse(because: "published guidance names no Jira key, pull request or issue number");
        namesARelease.Should().BeFalse(
            because: "the gate and the package floor are stated once, in process-files and the element catalog");
        guide.Should().NotContain("TBD", because: "a placeholder boundary cannot merge: the release gate refuses it");
    }

    [Test]
    [Description("The guide stays inside the design target process-files is held to and inside the 19,000-byte inline reply rule.")]
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
            because: "the guide was split out to give the report group room; it must not grow back into a spill");
        replyBytes.Should().BeLessThanOrEqualTo(GuidanceInlineReplyBudgetTests.InlineReplyBudgetBytes,
            because: "a reply over the inline rule is spilled to a file and read back in pieces");
    }

    private static string BalancedObject(string text, int start)
    {
        int depth = 0;
        bool inString = false;
        for (int index = start; index < text.Length; index++)
        {
            char character = text[index];
            if (inString)
            {
                if (character == '\\')
                {
                    index++;
                }
                else if (character == '"')
                {
                    inString = false;
                }
                continue;
            }
            if (character == '"')
            {
                inString = true;
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}' && --depth == 0)
            {
                return text[start..(index + 1)];
            }
        }
        return text[start..];
    }

    private static string ReadCollapsed(string relativePath) =>
        Collapse(ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), relativePath));

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}

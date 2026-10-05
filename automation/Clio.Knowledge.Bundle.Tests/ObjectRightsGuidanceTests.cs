using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the set-object-rights rules in ONE place and keeps them there. The rules are safety-relevant: the
/// object's rows are a priority list, so a revoke keeps the row as a deny, a grant below the All employees row
/// changes nothing for internal users, and turning operation permissions on or off is always an explicit flag.
/// An article that restated the old contract (a default grant, a fan-out to lookups, a two-step confirmation
/// code, a security-object opt-in) would lead an agent to call arguments the tool refuses, or to expect effects
/// it no longer has.
/// </summary>
[TestFixture]
public sealed class ObjectRightsGuidanceTests
{
    private const string ObjectRightsPath = "guidance/mcp/guides/operations/object-rights.md";

    // How far from a retired name a set-object-rights mention still makes the text read as that tool's argument.
    private const int SetObjectRightsContext = 300;

    // The boundary line. Until the clio release it carries the placeholder the publish gate in
    // validate-pull-request.yml refuses; the released version replaces it.
    private static readonly Regex VersionBoundary = new(
        @"Version boundary: get-object-rights and set-object-rights require clio (<[A-Z][A-Z0-9-]*TBD>|\d+(\.\d+){2,3}) or later\.",
        RegexOptions.Compiled);

    // A set-object-rights call written out with include-connected among its arguments, in MCP form (name=value)
    // or CLI form (--name, optionally with a value). Prose that merely names both is not a call.
    private static readonly Regex SetCallWithIncludeConnected = new(
        @"set-object-rights(?:\s+(?:[a-z-]+=\S+|--[a-z-]+(?:\s+[^\s-]\S*)?))*\s+(?:--)?include-connected\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // A guide named the way articles cite one another; the last character excludes trailing punctuation.
    private static readonly Regex CitedGuide = new(@"get-guidance name=([a-z0-9][a-z0-9.-]*[a-z0-9])",
        RegexOptions.Compiled);

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    // Line breaks and indentation are layout, not content: a rule wrapped across lines is still the rule.
    private static string Normalized(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool IsObjectRightsArticle(string path) =>
        string.Equals(Path.GetFullPath(path), Path.GetFullPath(Path.Combine(Root(), ObjectRightsPath)),
            StringComparison.OrdinalIgnoreCase);

    // In the owning article any mention counts; elsewhere only one within reach of a set-object-rights mention,
    // so the same word in another tool's text is not taken for this tool's argument.
    private static bool NamesInSetObjectRightsContext(string text, string retired, bool ownerArticle) =>
        Regex.Matches(text, Regex.Escape(retired), RegexOptions.IgnoreCase).Any(match =>
        {
            if (ownerArticle)
            {
                return true;
            }
            int start = Math.Max(0, match.Index - SetObjectRightsContext);
            int end = Math.Min(text.Length, match.Index + match.Length + SetObjectRightsContext);
            return text[start..end].Contains("set-object-rights", StringComparison.OrdinalIgnoreCase);
        });

    [Test]
    [Description("object-rights states the refusal of a revoke that would leave no granting row, that a revoke never turns operation permissions off, and that the disable alone turns them off while keeping every row.")]
    public void ObjectRights_ShouldOwnTheNoGrantingRowRefusal()
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), ObjectRightsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain("no row that grants any operation is REFUSED",
            because: "the owning article must keep the refusal an agent relies on");
        normalized.Should().Contain("A revoke never turns operation permissions off",
            because: "the switch is never a side effect of a revoke");
        normalized.Should().Contain(
            "disable-operation-permissions alone turns operation permissions OFF and keeps every row exactly as it is",
            because: "the disable is its own call, and it keeps the rows' operations like the designer's switch");
    }

    [TestCase("Owns: the row priority rule of object operation permissions",
        TestName = "ObjectRights_ShouldStateWhichRulesItOwns")]
    [TestCase("HIGHEST MATCHING ROW", TestName = "ObjectRights_ShouldStateThePriorityRule")]
    [TestCase("a row with no operations is an explicit DENY",
        TestName = "ObjectRights_ShouldStateThatAClearedRowDenies")]
    [TestCase("External / portal users are therefore deny-by-default",
        TestName = "ObjectRights_ShouldStateThatExternalUsersAreDenyByDefault")]
    [TestCase("For EXTERNAL users it can WIDEN access",
        TestName = "ObjectRights_ShouldStateThatAnEnableCanWidenExternalAccess")]
    [TestCase("Before an enable you MUST name each such row to the developer",
        TestName = "ObjectRights_ShouldRequireNamingTheExternalRowsAnEnableRevives")]
    [TestCase("Internal roles holding the \"…any data\" system operations reach records whatever the object rows say.",
        TestName = "ObjectRights_ShouldStateThatTheAnyDataOperationsOverrideTheRows")]
    [TestCase("When several rows match, you MUST NOT pick one",
        TestName = "ObjectRights_ShouldForbidPickingAmongSeveralGranteeMatches")]
    [TestCase("clears the named operations on the grantee's row and KEEPS the row",
        TestName = "ObjectRights_ShouldStateThatARevokeKeepsTheRow")]
    [TestCase("the grant changes nothing for internal users",
        TestName = "ObjectRights_ShouldStateThatTheAllEmployeesRowShadowsAnInternalGrant")]
    [TestCase("The tool never reorders rows", TestName = "ObjectRights_ShouldStateThatTheToolNeverReordersRows")]
    [TestCase("ONE object per call", TestName = "ObjectRights_ShouldStateOneObjectPerCall")]
    [TestCase("nothing is granted by default", TestName = "ObjectRights_ShouldStateThatOperationsAreRequired")]
    [TestCase("a grant or revoke without it (or with a value that names no operation) is refused before any read or write",
        TestName = "ObjectRights_ShouldStateThatACallWithoutOperationsIsRefusedFirst")]
    [TestCase("REFUSED (nothing is written) unless enable-operation-permissions is passed",
        TestName = "ObjectRights_ShouldStateThatEnablingIsExplicit")]
    [TestCase("enable-operation-permissions goes with a grant or alone; disable-operation-permissions always goes alone",
        TestName = "ObjectRights_ShouldStateWhichCallEachTransitionFlagBelongsTo")]
    [TestCase("enable-operation-permissions alone turns operation permissions ON with the stored rows as they are",
        TestName = "ObjectRights_ShouldStateWhatAnEnableAloneDoes")]
    [TestCase("A switch call on a switch already in place reports no change",
        TestName = "ObjectRights_ShouldStateThatARepeatedSwitchCallChangesNothing")]
    [TestCase("You MUST name the object by schema name AND title",
        TestName = "ObjectRights_ShouldRequireNamingTheObjectBySchemaNameAndTitle")]
    [TestCase("you MUST NOT decide which object is meant: ask before reading or writing",
        TestName = "ObjectRights_ShouldForbidPickingAnObjectByAWordThatIsNotItsTitle")]
    [TestCase("Both tools show each object by its title next to its code",
        TestName = "ObjectRights_ShouldStateThatTheToolsShowTheTitleNextToTheCode")]
    [TestCase("set-object-rights refuses a title, naming the code it belongs to",
        TestName = "ObjectRights_ShouldStateThatAWriteRefusesATitle")]
    [TestCase("You MUST ask the developer in chat before every write",
        TestName = "ObjectRights_ShouldRequireAskingInChatBeforeEveryWrite")]
    [TestCase("an auto-approve mode skips that approval",
        TestName = "ObjectRights_ShouldStateThatTheHostApprovalIsNotTheDevelopersYes")]
    [TestCase("you MUST first call it with preview=true", TestName = "ObjectRights_ShouldPreviewBeforeAsking")]
    [TestCase("On the CLI the same rules hold: run it with --preview first, ask in chat",
        TestName = "ObjectRights_ShouldHoldTheCliToThePreviewAndAskRule")]
    [TestCase("When the role has NO row it lists every row",
        TestName = "ObjectRights_ShouldStateWhatTheReadShowsForARoleWithoutARow")]
    [TestCase("kept AS STORED", TestName = "ObjectRights_ShouldStateThatAStoredAllEmployeesRowIsKeptAsStored")]
    [TestCase("| stored rows but none for All employees | added below them with read/create/edit/delete",
        TestName = "ObjectRights_ShouldStateWhenAnEnableAddsAnAllEmployeesRow")]
    [TestCase("its new row gets exactly the named operations — no second row is added",
        TestName = "ObjectRights_ShouldStateWhatAGrantToAllEmployeesItselfDoes")]
    [TestCase("For an INTERNAL grantee, do not turn operation permissions on just to grant",
        TestName = "ObjectRights_ShouldNotEnableForAnInternalGranteeByDefault")]
    [TestCase("A revoke on an object that is NOT administered is REFUSED",
        TestName = "ObjectRights_ShouldStateThatARevokeOnANonAdministeredObjectIsRefused")]
    [TestCase("an access WIDENING", TestName = "ObjectRights_ShouldStateThatADisableWidensAccess")]
    [TestCase("Before a disable you MUST name to the developer each row of an external role that stops applying",
        TestName = "ObjectRights_ShouldRequireNamingTheExternalRowsADisableCloses")]
    [TestCase("Refused as well: a grantee with more than one row on the object",
        TestName = "ObjectRights_ShouldStateThatDuplicateRowsAreRefused")]
    [TestCase("saved, but NOT verified", TestName = "ObjectRights_ShouldStateThatAnUnverifiedSaveFails")]
    [TestCase("The save is sent once, with no automatic retry",
        TestName = "ObjectRights_ShouldStateThatTheSaveIsNotRetried")]
    [TestCase("It does NOT list lookups the object inherits",
        TestName = "ObjectRights_ShouldStateWhatTheConnectedListingLeavesOut")]
    [TestCase("You MUST NOT propose a security or system object",
        TestName = "ObjectRights_ShouldForbidProposingASecurityObject")]
    [TestCase("opens its WHOLE table to every external user",
        TestName = "ObjectRights_ShouldWarnThatAPortalLookupGrantOpensTheWholeTable")]
    [TestCase("`All employees` = `a29a3ba5-4b0d-de11-9a51-005056c00008`",
        TestName = "ObjectRights_ShouldOwnTheAllEmployeesId")]
    [TestCase("returns success with ZERO rows", TestName = "ObjectRights_ShouldStateThatADeniedReadIsNotAnError")]
    [Description("object-rights keeps each rule of the one-object, explicit-flag contract that an agent must weigh before a write; losing one of them in an edit turns the article back into the old contract.")]
    public void ObjectRights_ShouldStateTheContractRule(string rule)
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), ObjectRightsPath));

        // Act
        string normalized = Normalized(guide);

        // Assert
        normalized.Should().Contain(rule, because: "the owning article must keep every rule of the tool contract");
    }

    [Test]
    [Description("object-rights declares its clio version boundary as a plain-text line that names both tools and carries either the placeholder the publish gate refuses or a released version, and the article holds no HTML comment: it is served as text/plain, so a comment reaches the agent verbatim and a boundary inside one is easy to miss.")]
    public void ObjectRights_ShouldDeclareItsClioVersionBoundary()
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), ObjectRightsPath));

        // Act
        bool declared = VersionBoundary.IsMatch(Normalized(guide));

        // Assert
        declared.Should().BeTrue(
            because: "AGENTS.md requires guidance that names a clio tool to declare the clio version it needs, and an agent on an older clio must learn that neither tool exists");
        guide.Should().NotContain("<!--",
            because: "a text/plain article delivers an HTML comment verbatim, maintainer notes included");
    }

    [Test]
    [Description("Every get-guidance name the object-rights article cites is a guide the bundle declares, so the article never sends an agent to a topic that is not published: an unknown name returns only availableGuides.")]
    public void ObjectRights_ShouldCiteOnlyDeclaredGuides()
    {
        // Arrange
        string guide = File.ReadAllText(Path.Combine(Root(), ObjectRightsPath));
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "bundle-source.json")));
        HashSet<string> declared = manifest.RootElement.GetProperty("resources").EnumerateArray()
            .Select(resource => resource.GetProperty("itemId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        // Act
        string[] cited = CitedGuide.Matches(guide).Select(match => match.Groups[1].Value).Distinct().ToArray();
        string[] undeclared = cited.Where(name => !declared.Contains(name)).ToArray();

        // Assert
        cited.Should().NotBeEmpty(because: "the article cites its sibling guides by name");
        undeclared.Should().BeEmpty(because: "a guide the bundle does not declare is not published");
    }

    // Every published body plus the bundle manifest, whose item descriptions agents read as well.
    private static IEnumerable<string> PublishedTexts() =>
        Directory.EnumerateFiles(Path.Combine(Root(), "guidance"), "*.md", SearchOption.AllDirectories)
            .Append(Path.Combine(Root(), "bundle-source.json"));

    [TestCase("confirmation-code")]
    [TestCase("connected-operations")]
    [TestCase("allow-security-object")]
    [Description("No guidance article and no bundle description names a retired set-object-rights argument where it reads as one: anywhere in object-rights, or elsewhere within reach of a set-object-rights mention. The tool refuses an unknown argument before any read or write, so an agent that follows such text fails every call; the same word in another tool's text is not this tool's argument.")]
    public void Guidance_ShouldNotNameARetiredSetObjectRightsArgument(string retired)
    {
        // Arrange
        IEnumerable<string> texts = PublishedTexts();

        // Act
        string[] naming = texts
            .Where(path => NamesInSetObjectRightsContext(Normalized(File.ReadAllText(path)), retired,
                ownerArticle: IsObjectRightsArticle(path)))
            .Select(path => Path.GetRelativePath(Root(), path))
            .ToArray();

        // Assert
        naming.Should().BeEmpty(because: $"'{retired}' is no longer a set-object-rights argument");
    }

    [Test]
    [Description("No guidance text writes a set-object-rights call with include-connected among its arguments, even when the call wraps across lines: set changes one object per call and refuses the argument; include-connected belongs to get-object-rights only. Prose that names both, such as a negation, is not a call.")]
    public void Guidance_ShouldNotPassIncludeConnectedToSetObjectRights()
    {
        // Arrange
        IEnumerable<string> texts = PublishedTexts();

        // Act
        string[] offending = texts
            .SelectMany(path => SetCallWithIncludeConnected.Matches(Normalized(File.ReadAllText(path)))
                .Select(match => $"{Path.GetRelativePath(Root(), path)}: {match.Value}"))
            .ToArray();

        // Assert
        offending.Should().BeEmpty(because: "include-connected is an argument of get-object-rights, never of set-object-rights");
    }
}

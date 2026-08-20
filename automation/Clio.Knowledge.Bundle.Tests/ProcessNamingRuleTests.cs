using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-94378 payload: the <c>Naming and codes</c> section of the process-modeling guide, the
/// load-bearing clauses that make its rules correct, the guide's own examples (which a generating model
/// copies in preference to the prose), and the two intra-guide citations that point at the section. The
/// content digest cannot guard a rule's wording — it is re-recorded on every edit — so a renumbered rule,
/// a dropped carve-out, an example drifted back to <c>Start1</c>, or a citation left pointing at a section
/// that no longer exists would otherwise ship green.
///
/// Three conflicts were resolved while writing the section; the resolutions are recorded here rather than
/// in the shipped guide, which states each rule without re-litigating it:
///
/// 1. CASING. The BP best-practices page illustrates element names in Title Case ("Check Budget
///    Threshold"); the naming-convention document (NCD) says capitalize the first word only. NCD wins —
///    it is the naming standard, and the ENG-94378 description itself writes "Check budget threshold".
///    That is why N1 and N4 say SENTENCE CASE.
/// 2. PREFIX OWNERSHIP. The research findings proposed adopting the toolkit's rule ("codes carry no
///    prefix; clio applies the environment's SchemaNamePrefix"). That is false at this layer: the
///    app-modeling guide already owns the prefix rule for ALL custom schema codes, and the platform
///    REFUSES an unprefixed process code on an environment that declares a prefix — probed 2026-08-19 on
///    a 7.8.0 stand whose SchemaNamePrefix is Usr, descriptor name "Account_Onboard" came back as
///    'The "Account_Onboard" code of the "ENG-94378 probe A" object must start with the "Usr" prefix'.
///    So N2 is &lt;prefix&gt;&lt;Object&gt;_&lt;Action&gt; and CITES app-modeling for which prefix rather
///    than restating it (AGENTS.md gives every rule exactly one owner). That evidence covers a
///    prefix-declaring environment only, so N2 states the empty-prefix branch separately and marks it
///    unprobed; app-modeling's enumeration was extended to name business-process codes, so a reader
///    following the pointer can confirm the ownership claim instead of having to infer it.
/// 3. SEGMENT COUNT. The findings' acceptance regex allowed exactly one '_', but the NCD example
///    Application_Disburse_Loan carries two. A third segment is ACCEPTED — UsrProbe_Check_Naming saved on
///    a 7.8.0 stand, probed 2026-08-20 — so N2 permits one while still preferring two, because of 427
///    process schemas on that stand 90 carry exactly one '_' and ZERO carry two or more. The house shape
///    is the reason for the preference; a platform refusal is not, and N2 no longer implies one.
///
/// Two review outcomes also shape what is pinned here. N4 covers every element on probed ground: all five
/// buildable types — startEvent, signalStart, endEvent, userTask (performTask / readData) and sendEmail —
/// were built WITH a caption and read it back verbatim, and the same graph built WITHOUT captions read back
/// the element CODE as each one. Omitting a caption therefore puts a raw Start1 on the diagram rather than
/// producing a friendly default, which is why the omission consequence is pinned and not just the MUST.
/// And the scratch-cleanup rule that first shipped as N10 governs no name or code; it moved to core-rules,
/// which is why this suite asserts it lives there and NOT in the naming section.
/// </summary>
[TestFixture]
public sealed class ProcessNamingRuleTests
{
    private const string OwnerGuide = "guidance/mcp/guides/processes/process-modeling.md";
    private const string CoreRulesGuide = "guidance/mcp/guides/core-rules.md";
    private const string PrefixOwnerGuide = "guidance/mcp/guides/applications/app-modeling.md";
    private const string NextSectionHeading = "== Trigger a process on a record event";

    /// <summary>
    /// Every rule the section ships. A dropped or renumbered rule fails here rather than silently
    /// re-recording the digest — the four E2E stories that depend on ENG-94378 (ENG-94823, ENG-94834,
    /// ENG-94854, ENG-94866) all cite "the naming guidelines" as one indivisible bar.
    /// </summary>
    private static readonly string[] RuleNumbers =
        ["N1", "N2", "N3", "N4", "N5", "N6", "N7", "N8", "N9", "N10"];

    /// <summary>
    /// The heading and the cited range are DERIVED, so adding an N11 moves both together instead of
    /// failing an assertion whose message describes a different defect (a dangling citation).
    /// </summary>
    private static string RuleRange => $"{RuleNumbers[0]}-{RuleNumbers[^1]}";

    private static string SectionHeading => $"== Naming and codes ({RuleRange}) ==";

    /// <summary>
    /// Clauses added to make individual rules correct. Dropping any one leaves the section green but
    /// wrong, so each is pinned with the reason it protects.
    /// </summary>
    private static readonly (string Fragment, string Because)[] LoadBearingClauses =
    [
        ("belongs to a user-task SCHEMA you author, NEVER to an element code",
            "N7's carve-out: at runtime Read data / Perform task / Send email / Modify data are ALL user tasks, so "
            + "without it the postfix rule reads as demanding UserTask on nearly every element code"),
        ("CallTaskUserTask` is wrong",
            "N7 needs the concrete wrong-side example; the rule is unfalsifiable to a reader without it"),
        ("the platform auto-creates on an ELEMENT",
            "N8's exclusion: Duration / ShowInScheduler / RemindBefore / ResultEntity are the task's parameters, "
            + "and a reader who applies the Parameter suffix to them would rename platform-owned state"),
        ("MUST NOT contradict the element's RUNTIME type",
            "N6 is the rule the baseline broke — EndNormal on a Terminate event — and the clause is the rule"),
        ("ProcessSchemaTerminateEvent",
            "N6 is only actionable if it names the runtime type endEvent actually builds today"),
        ("It does NOT ban the `End<Reason>` shape N5 prescribes",
            "without it a strict reading of N6 bans the very shape N5 hands the reader, and the guide answers "
            + "the same question two ways"),
        ("ENG-94378",
            "AGENTS.md requires an evidence pointer for a prescriptive claim; N2's prefix refusal is one"),
        ("must start with the \"Usr\" prefix",
            "N2 asserts the server REFUSES an unprefixed code — the observed error text is that claim's evidence"),
        ("The environment declares an EMPTY prefix -> add none",
            "N2's applicability boundary: the refusal was observed on a prefix-DECLARING stand, so the rule must "
            + "state the empty-prefix branch instead of reading as 'the platform always demands a prefix'"),
        ("app-modeling",
            "AGENTS.md gives every rule one owner: N2 must CITE the prefix authority, never restate it"),
        ("falls back to THE ELEMENT CODE as the caption",
            "N4 is a hard MUST-set, and this is the probed consequence of ignoring it: the omitted caption is not "
            + "a friendly default but the raw code on the diagram, which is how Start1 reaches a reviewer"),
        ("NOT YET BUILDABLE",
            $"{RuleNumbers[^1]} documents flow labels ahead of the buildable slice; without the marker it reads "
            + "as available now"),
        ("ENG-91853",
            $"{RuleNumbers[^1]}'s not-yet-buildable marker must name the dependency that would make it buildable"),
        ("SEQUENCE FLOWS",
            "the disambiguation: \"connections\" in the E2E stories' \"names, codes and connections\" means "
            + "sequence flows, NOT the Activity \"Connected to\" links, which are a different feature")
    ];

    /// <summary>
    /// The codes the guide's own examples carried before ENG-94378. They modelled exactly the output the
    /// rules exist to prevent — the baseline run's <c>StartSignal1</c> is the <c>Start1</c> signalStart
    /// example, and its unsegmented process code is <c>UsrSchemaCode</c> — so an example that drifts back
    /// to one of them cancels the prose out, whatever the prose says.
    /// </summary>
    private static readonly string[] RetiredExampleCodes =
        ["Start1", "task1", "End1", "MyText", "UsrSchemaCode", "SendEmail1", "ReadContact1"];

    /// <summary>
    /// One code appears under four different keys — an element declares it as <c>name</c>, a flow repeats
    /// it as <c>source</c>/<c>target</c>, an operation as <c>elementName</c> — so scanning <c>name</c>
    /// alone would pass an example whose flows still point at a retired code.
    /// </summary>
    private const string CodeValuedKeys = @"""(?:name|elementName|source|target|processParameter)"":\s*""([^""]+)""";

    [Test]
    [Description("The Naming and codes section exists in the process-modeling guide and still carries every rule.")]
    public void NamingSection_ShouldExistWithEveryRule()
    {
        string section = NamingSection(ReadGuide(OwnerGuide));

        string[] missingRules = RuleNumbers
            .Where(rule => !Regex.IsMatch(section, $@"(?m)^{Regex.Escape(rule)}\s"))
            .ToArray();

        missingRules.Should().BeEmpty(
            because: $"{RuleRange} are the whole payload of ENG-94378 and the bar four E2E stories are scored "
                + "against; dropping or renumbering one must fail a test, not re-record the digest");
    }

    [Test]
    [Description("Every load-bearing clause of the naming rules is still inside the section.")]
    public void NamingSection_ShouldKeepLoadBearingClauses()
    {
        string sectionNormalized = Normalize(NamingSection(ReadGuide(OwnerGuide)));

        string[] droppedClauses = LoadBearingClauses
            .Where(clause => !sectionNormalized.Contains(clause.Fragment))
            .Select(clause => $"{clause.Fragment} ({clause.Because})")
            .ToArray();

        droppedClauses.Should().BeEmpty(
            because: "each clause was added to make a rule correct; dropping one re-records the digest silently");
    }

    [Test]
    [Description("The rules are numbered N-, not R-, so they are never mistaken for the validate-process-graph-enforced connection rules.")]
    public void NamingSection_ShouldNotReuseTheEnforcedRSeries()
    {
        string section = NamingSection(ReadGuide(OwnerGuide));

        Regex.IsMatch(section, @"(?m)^R\d+\s").Should().BeFalse(
            because: "R1-R17 are pre-checked by validate-process-graph and the N rules are not; reusing an R "
                + "number would advertise enforcement that does not exist");

        Normalize(section).Should().Contain("validate-process-graph",
            because: "the section header must state the enforcement boundary explicitly, the way the R1-R17 header "
                + "states which subset of its own rules is enforced");
    }

    [Test]
    [Description("The Build recipe and Parameters citations resolve to the Naming and codes section rather than dangling.")]
    public void NamingCitations_ShouldResolveToTheSection()
    {
        string guideNormalized = Normalize(ReadGuide(OwnerGuide));

        guideNormalized.Should().Contain($"name them per {RuleRange} in \"Naming and codes\"",
            because: "Build recipe step 1 is where the model plans the graph — the naming pointer has to be there, "
                + "and a pointer no test guards is a pointer a later edit drops");
        guideNormalized.Should().Contain("Name a process parameter per N8 in \"Naming and codes\"",
            because: "the Parameters section introduces parameters[].name; N8 governs it and must be cited there");

        string[] citedSections = Regex.Matches(guideNormalized, @"in \x22([^\x22]+)\x22")
            .Select(match => match.Groups[1].Value.Trim())
            .Where(cited => cited.Contains("Naming and codes"))
            .Distinct()
            .ToArray();

        citedSections.Should().NotBeEmpty(because: "the citations above must be discoverable by the same scan that validates them");
        citedSections.Should().OnlyContain(cited => guideNormalized.Contains($"== {cited} ({RuleRange}) =="),
            because: "a citation naming a section the guide does not define is a dangling pointer");
    }

    [Test]
    [Description("The guide's own JSON examples obey the naming rules, so copying an example applies them.")]
    public void GuideExamples_ShouldObeyTheNamingRules()
    {
        string guide = ReadGuide(OwnerGuide);

        string[] offenders = Regex.Matches(guide, CodeValuedKeys)
            .Select(match => match.Groups[1].Value)
            .Where(code => Regex.IsMatch(code, @"\d$") || char.IsLower(code[0]))
            .Distinct()
            .ToArray();

        offenders.Should().BeEmpty(
            because: "an autonumbered or camelCase code in an example is what a generating model reproduces — the "
                + "baseline run's StartSignal1 came from the Start1 example, not from ignoring the prose (N5)");

        string[] resurrected = RetiredExampleCodes
            .Where(code => Regex.IsMatch(guide, $@"\x22{Regex.Escape(code)}\x22"))
            .ToArray();

        resurrected.Should().BeEmpty(
            because: "these are the pre-ENG-94378 example codes; each one breaks a rule stated 40 lines below it");
    }

    [Test]
    [Description("The canonical descriptor example carries an N2-shaped process code.")]
    public void CanonicalDescriptorExample_ShouldCarryAnN2ShapedProcessCode()
    {
        string descriptor = DescriptorExample(ReadGuide(OwnerGuide));

        Regex.Match(descriptor, @"""name"":\s*""([^""]+)""").Groups[1].Value.Should()
            .MatchRegex(@"^[A-Z][A-Za-z]*[A-Z][A-Za-z]*(_[A-Z][A-Za-z]*)+$",
                because: "the process code an example hands the reader must itself be <prefix><Object>_<Action> (N2); "
                    + "the baseline run's unsegmented UsrAccountOnboarding is the shape UsrSchemaCode modelled");
    }

    [Test]
    [Description("Every element declared in a guide example carries a caption, events included.")]
    public void GuideExamples_ShouldCaptionEveryElement()
    {
        string guide = ReadGuide(OwnerGuide);

        string[] elementsWithoutCaption = Regex
            .Matches(guide, @"""type"":\s*""(?:startEvent|signalStart|endEvent|userTask|sendEmail|readData|performTask)""")
            .Select(match => EnclosingObject(guide, match.Index))
            .Where(element => !element.Contains("\"caption\"", StringComparison.Ordinal))
            .Select(Normalize)
            .Distinct()
            .ToArray();

        elementsWithoutCaption.Should().BeEmpty(
            because: "N4 requires a caption on EVERY element, and the probe showed an omitted one falls back to the "
                + "element CODE — so an example that omits one teaches putting a raw code on the diagram. Events are "
                + "where this bites hardest, and the sendEmail example shipped without a caption until it was caught");
    }

    [Test]
    [Description("N2's prefix citation is confirmable in app-modeling: the quoted bullet exists and its enumeration names process codes.")]
    public void PrefixOwnership_ShouldBeConfirmableInAppModeling()
    {
        string prefixOwner = Normalize(ReadGuide(PrefixOwnerGuide));

        prefixOwner.Should().Contain("as the prefix for ALL custom schema codes",
            because: "N2 quotes this bullet verbatim as the owner of the prefix rule, and a quotation whose text "
                + "no longer exists at the destination is a dangling pointer with no error to show for it");
        prefixOwner.Should().Contain("business-process codes",
            because: "the bullet's enumeration is what lets a reader following N2's pointer CONFIRM that process "
                + "codes are covered; while it omitted them, the ownership claim could only be inferred");
    }

    [Test]
    [Description("The scratch-cleanup invariant lives in core-rules, and only there.")]
    public void ScratchCleanupInvariant_ShouldLiveInCoreRules()
    {
        Normalize(ReadGuide(CoreRulesGuide)).Should().Contain("Leave NO scratch behind",
            because: "the rule governs no name or code, so it is session hygiene and core-rules owns it — but it "
                + "must survive the move out of the naming section rather than be lost in it");

        Normalize(NamingSection(ReadGuide(OwnerGuide))).Should().NotContain("Leave NO scratch behind",
            because: "AGENTS.md gives every rule one owner, and a cleanup rule counted among the naming rules "
                + "inflates the catalog the four E2E stories are scored against");
    }

    private static string NamingSection(string guide) =>
        Section(guide, SectionHeading, NextSectionHeading);

    private static string DescriptorExample(string guide) =>
        Section(guide, "== Descriptor (create-business-process) ==", "- `name` is the local element handle");

    /// <summary>
    /// An element declaration is a JSON object inside a prose snippet, so it has no line discipline to key
    /// off — a <c>sendEmail</c> element spans eleven lines while three <c>startEvent</c>s share one. Walking
    /// the braces is what lets the caption assertion apply to every element rather than to whichever ones
    /// happen to be formatted like the descriptor.
    /// </summary>
    private static string EnclosingObject(string text, int indexInside)
    {
        int start = indexInside;
        for (int depth = 0; start > 0; start--)
        {
            if (text[start] == '}') { depth++; }
            else if (text[start] == '{' && depth-- == 0) { break; }
        }

        int end = start;
        for (int depth = 0; end < text.Length - 1; end++)
        {
            if (text[end] == '{') { depth++; }
            else if (text[end] == '}' && --depth == 0) { break; }
        }

        return text[start..(end + 1)];
    }

    private static string Section(string guide, string heading, string terminator)
    {
        int start = guide.IndexOf(heading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0,
            because: $"the owner guide must carry the '{heading}' section the citations point at");
        int end = guide.IndexOf(terminator, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start,
            because: "the section must be bounded by the next one so its content is asserted INSIDE it");
        return guide[start..end];
    }

    private static string ReadGuide(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

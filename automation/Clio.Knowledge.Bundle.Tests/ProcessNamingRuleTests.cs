using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-94378 payload: the <c>Naming and codes (N1-N11)</c> section of the process-modeling
/// guide, the load-bearing clauses that make its rules correct, and the two intra-guide citations that
/// point at it. The content digest cannot guard a rule's wording — it is re-recorded on every edit — so a
/// renumbered rule, a dropped carve-out, or a citation left pointing at a section that no longer exists
/// would otherwise ship green.
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
///    REFUSES an unprefixed process code outright — probed 2026-08-19 on a 7.8.0 stand, descriptor
///    name "Account_Onboard" came back as
///    'The "Account_Onboard" code of the "ENG-94378 probe A" object must start with the "Usr" prefix'.
///    So N2 is <prefix><Object>_<Action> and CITES app-modeling for which prefix, rather than restating
///    it (AGENTS.md gives every rule exactly one owner).
/// 3. SEGMENT COUNT. The findings' acceptance regex allowed exactly one '_', but the NCD example
///    Application_Disburse_Loan carries two. N2 therefore permits a further qualifier segment. NOT
///    VERIFIED against the platform: the build probe for a two-underscore code was blocked before it
///    ran, and of 427 process schemas on the probe stand, 90 carry exactly one '_' and ZERO carry two or
///    more — observational evidence that the two-segment shape is the norm, not proof that a third
///    segment is refused. N2 is worded to prefer two segments accordingly.
/// </summary>
[TestFixture]
public sealed class ProcessNamingRuleTests
{
    private const string OwnerGuide = "guidance/mcp/guides/processes/process-modeling.md";
    private const string SectionHeading = "== Naming and codes (N1-N11) ==";
    private const string NextSectionHeading = "== Trigger a process on a record event";

    /// <summary>
    /// Every rule the section ships. A dropped or renumbered rule fails here rather than silently
    /// re-recording the digest — the four E2E stories that depend on ENG-94378 (ENG-94823, ENG-94834,
    /// ENG-94854, ENG-94866) all cite "the naming guidelines" as one indivisible bar.
    /// </summary>
    private static readonly string[] RuleNumbers =
        ["N1", "N2", "N3", "N4", "N5", "N6", "N7", "N8", "N9", "N10", "N11"];

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
        ("ENG-94378",
            "AGENTS.md requires an evidence pointer for a prescriptive claim; N2's prefix refusal is one"),
        ("must start with the \"Usr\" prefix",
            "N2 asserts the server REFUSES an unprefixed code — the observed error text is that claim's evidence"),
        ("app-modeling",
            "AGENTS.md gives every rule one owner: N2 must CITE the prefix authority, never restate it"),
        ("NOT YET BUILDABLE",
            "N11 documents flow labels ahead of the buildable slice; without the marker it reads as available now"),
        ("ENG-91853",
            "N11's not-yet-buildable marker must name the dependency that would make it buildable"),
        ("SEQUENCE FLOWS",
            "N11's disambiguation: \"connections\" in the E2E stories' \"names, codes and connections\" means "
            + "sequence flows, NOT the Activity \"Connected to\" links, which are a different feature")
    ];

    [Test]
    [Description("The Naming and codes section exists in the process-modeling guide and still carries every rule N1-N11.")]
    public void NamingSection_ShouldExistWithEveryRule()
    {
        string section = NamingSection(ReadGuide(OwnerGuide));

        string[] missingRules = RuleNumbers
            .Where(rule => !Regex.IsMatch(section, $@"(?m)^{Regex.Escape(rule)}\s"))
            .ToArray();

        missingRules.Should().BeEmpty(
            because: "N1-N11 are the whole payload of ENG-94378 and the bar four E2E stories are scored against; "
                + "dropping or renumbering one must fail a test, not re-record the digest");
    }

    [Test]
    [Description("Every load-bearing clause of N1-N11 is still inside the section.")]
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
            because: "R1-R17 are pre-checked by validate-process-graph and N1-N11 are not; reusing an R number "
                + "would advertise enforcement that does not exist");

        Normalize(section).Should().Contain("validate-process-graph",
            because: "the section header must state the enforcement boundary explicitly, the way the R1-R17 header "
                + "states which subset of its own rules is enforced");
    }

    [Test]
    [Description("The Build recipe and Parameters citations resolve to the Naming and codes section rather than dangling.")]
    public void NamingCitations_ShouldResolveToTheSection()
    {
        string guide = ReadGuide(OwnerGuide);
        string guideNormalized = Normalize(guide);

        guideNormalized.Should().Contain("name them per N1-N11 in \"Naming and codes\"",
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
        citedSections.Should().OnlyContain(cited => guideNormalized.Contains("== " + cited + " (N1-N11) =="),
            because: "a citation naming a section the guide does not define is a dangling pointer");
    }

    private static string NamingSection(string guide)
    {
        int start = guide.IndexOf(SectionHeading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0,
            because: $"the owner guide must carry the '{SectionHeading}' section the citations point at");
        int end = guide.IndexOf(NextSectionHeading, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start,
            because: "the section must be bounded by the next one so the rules are asserted INSIDE it");
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

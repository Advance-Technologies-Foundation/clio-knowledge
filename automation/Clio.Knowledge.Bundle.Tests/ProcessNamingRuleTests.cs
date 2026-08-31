using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-94378 payload: the <c>Naming and codes</c> section — since ENG-96212 the whole of the
/// <c>process-naming</c> article — the load-bearing clauses that make its rules correct, the process
/// guides' own examples (which a generating model copies in preference to the prose), and the two
/// citations that point at the section. The content digest cannot guard a rule's wording — it is
/// re-recorded on every edit — so a renumbered rule, a dropped carve-out, an example drifted back to
/// <c>Start1</c>, or a citation left pointing at a section that no longer exists would otherwise ship
/// green. Since N5 makes an element code a FUNCTION of its caption, the examples are also checked against
/// the derivation itself rather than against a shape predicate: five of them contradicted the rule while
/// the handle scan, which sees only a trailing digit or a camelCase head, reported green (d-krestov, PR
/// #82).
///
/// ENG-96212 changed WHERE this content lives, not what it says. The naming rules are one article now, and
/// the two citations that used to read "see the section below" cross an article boundary — so
/// <see cref="NamingCitations_ShouldResolveToTheNamingArticle"/> additionally checks that the cited
/// article is a declared get-guidance topic. A pointer to an article a reader cannot fetch is the same
/// dangling pointer the old test guarded against, one layer down.
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
    private const string NamingArticle = "process-naming";
    private const string NamingGuide = "guidance/mcp/guides/processes/naming.md";
    private const string DescriptorGuide = "guidance/mcp/guides/processes/process-modeling.md";
    private const string ParametersGuide = "guidance/mcp/guides/processes/parameters.md";
    private const string BundleSource = "bundle-source.json";
    private const string CoreRulesGuide = "guidance/mcp/guides/core-rules.md";
    private const string PrefixOwnerGuide = "guidance/mcp/guides/applications/app-modeling.md";

    /// <summary>
    /// ENG-96212 split the monolithic process-modeling guide into seven articles, because the single
    /// article no longer fit in one get-guidance response: the payload spilled to a file that Read cannot
    /// page (it is one line), so every agent grepped a fragment and no agent read the whole thing. The
    /// naming rules moved to their own article, but the EXAMPLES a generating model copies are now spread
    /// across all seven — the worked Perform task example, the Send email element, the data elements. So
    /// every example scan below reads the SET rather than one file; scanning only the entry article would
    /// still pass while silently guarding nothing but the descriptor.
    /// </summary>
    private static string[] ProcessGuides => ProcessGuideSet.SplitPaths(FindRepositoryRoot());

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
            + "sequence flows, NOT the Activity \"Connected to\" links, which are a different feature"),
        ("DERIVE the code from the element's own `caption`, do not compose it separately",
            $"{RuleNumbers[4]}'s derivation formula: the event shapes above it fix a prefix and leave the wording "
            + "free, and a code composed independently of the caption is what drifted — two clean-room runs wrote "
            + "the same caption \"Follow-up task created\" and produced EndFollowUpTaskCreated and EndFollowUpCreated"),
        ("Do NOT paraphrase, abbreviate, or drop a content word",
            $"without the prohibition {RuleNumbers[4]}'s formula reads as advisory; shortening on the way from "
            + "caption to code is the exact mechanism of the observed drift, so it has to be named as forbidden"),
        ("the caption wording is part of what this rule constrains",
            $"{RuleNumbers[8]} has to reach past codes: a code that is a function of the caption drifts when the "
            + "caption does, so a stability rule governing codes alone would leave the drift's own source free"),
        ($"{RuleNumbers[8]} governs the codes of elements and parameters PRESENT IN BOTH runs",
            $"{RuleNumbers[8]}'s scope carve-out: two runs may legitimately model one request with different "
            + "process parameters, and without the boundary that modelling choice reads as naming drift"),
        ("treat every punctuation mark as a WORD BOUNDARY",
            "the first cut said \"drop all punctuation\", which yields EndFollowUpTaskCreated's rival "
            + "EndFollowupTaskCreated on the incident's own caption — a rule that contradicts its own worked "
            + "example opens a second drift axis instead of closing the first"),
        ("That list is EXHAUSTIVE — KEEP every other word",
            $"a derivation is only a formula if it is total: without the closure, \"Contact has been created\" "
            + $"admits two conforming codes and {RuleNumbers[4]} stops deciding anything on an ordinary caption"),
        ("Prefer the plainest statement of the action or the outcome",
            $"{RuleNumbers[3]} owns caption wording ({RuleNumbers[8]} cites it), and this is the sentence that "
            + "makes a derived code stable — a reader looking up how to word a caption must find it there"),
        ($"nothing in {RuleRange} constrains that structural choice",
            $"{RuleNumbers[8]}'s carve-out lifts the naming constraint from a structural difference, and no other "
            + "rule picks it up; saying so keeps the exclusion from reading as approval")
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
    private const string ElementHandleKeys = @"""(?:name|elementName|source|target)"":\s*""([^""]+)""";

    /// <summary>
    /// Parameter codes need their own scan because N8 asks for something the element-handle predicate
    /// cannot see: <c>AccountName</c> is PascalCase with no trailing digit, so it passes as a handle while
    /// breaking N8's suffix. A parameter code is also the field a model is likeliest to invent from the
    /// request text rather than copy, which makes it the rule most exposed to a bad example.
    /// </summary>
    private const string ParameterCodeKeys = @"""processParameter"":\s*""([^""]+)""";

    /// <summary>
    /// The verbs the guide's activity captions open with. A curated list is the only way to check
    /// "verb first" statically; it doubles as the check that an EVENT caption is NOT verb-led. A new
    /// example needing a verb outside this list should add it here — the failure says so.
    /// </summary>
    private static readonly string[] ActivityVerbs =
    [
        "Add", "Approve", "Ask", "Assign", "Call", "Check", "Compute", "Create", "Delete", "Modify",
        "Notify", "Open", "Publish", "Read", "Send", "Set", "Show", "Start", "Update", "Wait"
    ];

    private const string ActivityTypes = "userTask|sendEmail|readData|performTask";
    private const string EventTypes = "startEvent|signalStart|endEvent";

    [Test]
    [Description("The Naming and codes section exists in the process-modeling guide and still carries every rule.")]
    public void NamingSection_ShouldExistWithEveryRule()
    {
        string section = NamingSection();

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
        string sectionNormalized = Normalize(NamingSection());

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
        string section = NamingSection();

        Regex.IsMatch(section, @"(?m)^R\d+\s").Should().BeFalse(
            because: "R1-R17 are pre-checked by validate-process-graph and the N rules are not; reusing an R "
                + "number would advertise enforcement that does not exist");

        Normalize(section).Should().Contain("validate-process-graph",
            because: "the section header must state the enforcement boundary explicitly, the way the R1-R17 header "
                + "states which subset of its own rules is enforced");
    }

    [Test]
    [Description("The Build recipe and Parameters citations resolve to the process-naming article rather than dangling.")]
    public void NamingCitations_ShouldResolveToTheNamingArticle()
    {
        // Each pointer is asserted against the file that must CARRY it, not against the concatenation of
        // all seven. Read from the concatenation these assertions still pass once a pointer has been moved
        // into an unrelated article — which is the opposite of what the because-clauses claim, and after a
        // split "which article states this" is the whole question.
        string buildRecipe = Normalize(ReadGuide(DescriptorGuide));
        string parameters = Normalize(ReadGuide(ParametersGuide));

        buildRecipe.Should().Contain($"name them per {RuleRange} in `{NamingArticle}`",
            because: "Build recipe step 1 is where the model plans the graph, and the recipe lives in the entry "
                + "article — the naming pointer has to be there, and a pointer no test guards is a pointer a "
                + "later edit drops");
        parameters.Should().Contain($"Name a process parameter per N8 in `{NamingArticle}`",
            because: "the parameters article is where parameters[].name is introduced; N8 governs it and must "
                + "be cited at that introduction rather than somewhere in the set");

        // Since ENG-96212 the citation crosses an ARTICLE boundary, so resolving it takes two things that
        // a same-file "see below" never needed: the section must exist at the destination, and the
        // destination must be a get-guidance topic. A reader who cannot fetch `process-naming` cannot
        // reach N1-N10 at all, which is the exact failure the split was done to remove.
        ReadGuide(NamingGuide).Should().Contain(SectionHeading,
            because: $"the citations name {NamingArticle} as the owner of {RuleRange}, so it must define the section");

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(FindRepositoryRoot(), BundleSource)));
        string[] declaredTopics = manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("itemId").GetString()!)
            .ToArray();
        declaredTopics.Should().Contain(NamingArticle,
            because: "a cross-article citation only resolves if the cited article is a declared get-guidance topic; "
                + "an article that exists on disk but is absent from bundle-source.json is unreachable at runtime");
    }

    [Test]
    [Description("The guide's own JSON examples obey the naming rules, so copying an example applies them.")]
    public void GuideExamples_ShouldObeyTheNamingRules()
    {
        string guide = ReadProcessGuides();

        string[] offenders = Regex.Matches(guide, ElementHandleKeys)
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
    [Description("Every element code in a guide example is what N5's derivation produces from that element's own caption.")]
    public void GuideExamples_ShouldDeriveEveryElementCodeFromItsCaption()
    {
        (string Type, string Name, string Caption)[] elements = CaptionedElements(ReadProcessGuides());

        elements.Should().HaveCountGreaterThanOrEqualTo(8,
            because: "the scan has to reach every caption-bearing example — a predicate that matches nothing, or "
                + "only the descriptor, would pass this test while guarding the drift it exists to catch");

        string[] undeliverable = elements
            .Where(element => element.Name != DeriveCode(element.Caption, element.Type))
            .Select(element => $"{element.Type} \"{element.Caption}\" declares {element.Name}, "
                + $"derivation yields {DeriveCode(element.Caption, element.Type)}")
            .ToArray();

        undeliverable.Should().BeEmpty(
            because: "N5 makes the code a FUNCTION of the caption, and the prose alone cannot hold the examples to "
                + "it: the handle predicate above only rejects a trailing digit or a camelCase head, so EndOnboarded "
                + "on \"Onboarding handed off\" and OrderChangedSignal on \"Order amount or status changed\" both "
                + "passed while contradicting the rule 80 lines below them. An example is what a generating model "
                + "copies in preference to the prose, so the examples are where the derivation has to be enforced");
    }

    [Test]
    [Description("Every parameter code in a guide example carries N8's Parameter suffix.")]
    public void GuideExamples_ShouldSuffixEveryParameterCode()
    {
        string guide = ReadProcessGuides();

        string[] parameterCodes = Regex.Matches(guide, ParameterCodeKeys)
            .Select(match => match.Groups[1].Value)
            .Concat(ParameterBlocks(guide)
                .SelectMany(block => Regex.Matches(block, @"""name"":\s*""([^""]+)""")
                    .Select(match => match.Groups[1].Value)))
            .Where(code => !code.StartsWith('<'))
            .Distinct()
            .ToArray();

        parameterCodes.Should().NotBeEmpty(
            because: "a scan that matches nothing would pass this test while guarding nothing");

        parameterCodes.Should().OnlyContain(code => code.EndsWith("Parameter", StringComparison.Ordinal),
            because: "N8 is the rule an example is likeliest to undercut: a code like AccountName is PascalCase "
                + "with no trailing digit, so the N5 predicate passes it, and MyText only ever failed because it "
                + "was blocklisted by name rather than because the suffix was checked");
    }

    [Test]
    [Description("Activity captions in the examples open with a verb; event captions name a trigger or an outcome instead.")]
    public void GuideExamples_ShouldShapeCaptionsByElementKind()
    {
        string guide = ReadProcessGuides();

        string[] activitiesNotVerbFirst = CaptionsOf(guide, ActivityTypes)
            .Where(caption => !ActivityVerbs.Contains(FirstWord(caption), StringComparer.Ordinal))
            .ToArray();

        activitiesNotVerbFirst.Should().BeEmpty(
            because: "N4 asks an activity caption to name the action the process performs. If the verb is right and "
                + "simply missing from ActivityVerbs, add it there — the list is the static stand-in for 'is a verb'");

        string[] eventsVerbFirst = CaptionsOf(guide, EventTypes)
            .Where(caption => ActivityVerbs.Contains(FirstWord(caption), StringComparer.Ordinal))
            .ToArray();

        eventsVerbFirst.Should().BeEmpty(
            because: "N4 scopes verb-first to activities: an imperative on an event misdescribes it — 'Modify record' "
                + "reads as an action the process performs rather than the condition that starts it, and a Terminate "
                + "end performs no action at all. With the clause scoped, the examples are the only statement of the "
                + "event shape, so both halves need pinning");
    }

    [Test]
    [Description("The canonical descriptor example carries an N2-shaped process code.")]
    public void CanonicalDescriptorExample_ShouldCarryAnN2ShapedProcessCode()
    {
        string descriptor = DescriptorExample(ReadGuide(DescriptorGuide));

        Regex.Match(descriptor, @"""name"":\s*""([^""]+)""").Groups[1].Value.Should()
            .MatchRegex(@"^[A-Z][A-Za-z]*[A-Z][A-Za-z]*(_[A-Z][A-Za-z]*)+$",
                because: "the process code an example hands the reader must itself be <prefix><Object>_<Action> (N2); "
                    + "the baseline run's unsegmented UsrAccountOnboarding is the shape UsrSchemaCode modelled");
    }

    [Test]
    [Description("Every element declared in a guide example carries a caption, events included.")]
    public void GuideExamples_ShouldCaptionEveryElement()
    {
        string guide = ReadProcessGuides();

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

        Normalize(NamingSection()).Should().NotContain("Leave NO scratch behind",
            because: "AGENTS.md gives every rule one owner, and a cleanup rule counted among the naming rules "
                + "inflates the catalog the four E2E stories are scored against");
    }

    /// <summary>
    /// Since ENG-96212 the naming rules ARE their own article, so the section runs from its heading to the
    /// end of the file rather than to the next heading. Reading it from the naming article specifically —
    /// not from the concatenated set — is what keeps "this rule lives in the naming article" assertable.
    /// </summary>
    private static string NamingSection()
    {
        string guide = ReadGuide(NamingGuide);
        int start = guide.IndexOf(SectionHeading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0,
            because: $"{NamingGuide} must carry the '{SectionHeading}' section the citations point at");
        return guide[start..];
    }

    private static string ReadProcessGuides() =>
        string.Join("\n", ProcessGuides.Select(ReadGuide));

    private static string DescriptorExample(string guide) =>
        Section(guide, "== Descriptor (create-business-process) ==", "- `name` is the local element handle");

    /// <summary>
    /// N5's stop list, verbatim and EXHAUSTIVE by the rule's own words: every other word of the caption
    /// survives into the code. Keeping it closed is the point — an open-ended "drop the small words" leaves
    /// two runs free to disagree on <c>has been</c>, which is how a formula stops being one.
    /// </summary>
    private static readonly string[] DerivationStopWords =
        ["a", "an", "the", "is", "are", "was", "were", "be", "been", "has", "have", "had"];

    /// <summary>
    /// An activity's shape is empty on purpose: for the elements a model names most often the derivation IS
    /// the whole code, so nothing about the type can excuse a divergence from the caption.
    /// </summary>
    private static readonly (string Type, string Prefix, string Suffix)[] ElementShapes =
    [
        ("signalStart", "", "Signal"),
        ("startEvent", "", "Start"),
        ("endEvent", "End", ""),
        ("userTask", "", ""),
        ("performTask", "", ""),
        ("readData", "", ""),
        ("sendEmail", "", "")
    ];

    /// <summary>
    /// Two details of N5 that a looser implementation would get wrong in opposite directions: punctuation is
    /// a WORD BOUNDARY, so "Follow-up" must reach `FollowUp` and never `Followup` (the contradiction the
    /// first cut of the rule shipped with); and only the first letter of a surviving word is forced, so an
    /// acronym a caption carries is not flattened into title case by the check itself.
    /// </summary>
    private static string DeriveCode(string caption, string elementType)
    {
        string stem = string.Concat(Regex.Split(caption, "[^A-Za-z0-9]+")
            .Where(word => word.Length > 0)
            .Where(word => !DerivationStopWords.Contains(word.ToLowerInvariant(), StringComparer.Ordinal))
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

        (string Type, string Prefix, string Suffix) shape = ElementShapes
            .FirstOrDefault(candidate => candidate.Type == elementType,
                (Type: elementType, Prefix: string.Empty, Suffix: string.Empty));
        return shape.Prefix + stem + shape.Suffix;
    }

    /// <summary>
    /// Element declarations paired with their own caption. The pairing is what the older scans lack: they
    /// read codes and captions through separate predicates, so neither can see that a code contradicts the
    /// caption sitting three characters away from it.
    /// </summary>
    private static (string Type, string Name, string Caption)[] CaptionedElements(string guide) =>
        [.. Regex.Matches(guide, $@"""type"":\s*""(?:{ActivityTypes}|{EventTypes})""")
            .Select(match => (Type: match.Groups[0].Value, Element: EnclosingObject(guide, match.Index)))
            .Select(found => (
                Type: Regex.Match(found.Type, @"""type"":\s*""([^""]+)""").Groups[1].Value,
                Name: Regex.Match(found.Element, @"""name"":\s*""([^""]+)""").Groups[1].Value,
                Caption: Regex.Match(found.Element, @"""caption"":\s*""([^""]+)""").Groups[1].Value))
            .Where(element => element.Name.Length > 0 && element.Caption.Length > 0)
            .Distinct()];

    private static string[] CaptionsOf(string guide, string elementTypes) =>
        Regex.Matches(guide, $@"""type"":\s*""(?:{elementTypes})""")
            .Select(match => EnclosingObject(guide, match.Index))
            .Select(element => Regex.Match(element, @"""caption"":\s*""([^""]+)""").Groups[1].Value)
            .Where(caption => caption.Length > 0)
            .Distinct()
            .ToArray();

    private static string FirstWord(string caption) => caption.Split(' ')[0];

    /// <summary>
    /// A parameter code hides under the same <c>name</c> key an element uses, so only its position inside a
    /// <c>parameters</c> array distinguishes the two. Walking the brackets is what keeps N8's check off
    /// element handles and N5's off parameter codes.
    /// </summary>
    private static string[] ParameterBlocks(string guide)
    {
        List<string> blocks = [];
        foreach (Match match in Regex.Matches(guide, @"""parameters""\s*:\s*\["))
        {
            int index = match.Index + match.Length - 1;
            int end = index;
            for (int depth = 0; end < guide.Length; end++)
            {
                if (guide[end] == '[') { depth++; }
                else if (guide[end] == ']' && --depth == 0) { break; }
            }
            blocks.Add(guide[index..Math.Min(end + 1, guide.Length)]);
        }
        return [.. blocks];
    }

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

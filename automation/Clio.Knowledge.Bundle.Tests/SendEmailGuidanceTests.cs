using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the ENG-95979 send-email guidance: the auto-mode mandatory-field checklist, the missing-body
/// runtime trap, the build-time sender resolution contract kept apart from the run-time missing-sender
/// failure, and the ranked recipient sources with the advise-against-literals rule.
/// </summary>
[TestFixture]
public sealed class SendEmailGuidanceTests
{
    private const string Article = "guidance/mcp/guides/processes/send-email.md";
    private const string TemplateArticle = "guidance/mcp/guides/processes/send-email-template.md";

    [Test]
    [Description("Auto mode teaches all four mandatory fields and the missing-body trap that build and describe both miss.")]
    public void Guide_ShouldKeepAutoModeChecklistAndMissingBodyTrap()
    {
        string guide = ReadGuide();

        guide.Should().Contain("treat `sender`, `to`, `subject` and `body` as MANDATORY",
            because: "an agent that omits any of the four ships an element that builds green and dies at run "
                + "time, so the authoring rule must name all four explicitly (ENG-95979 AC)");
        guide.Should().Contain("Localizable template not found for record",
            because: "the missing-body run failure is only recognizable by its error text — build and describe "
                + "both report success, so the string is the agent's one anchor (reported on ENG-95979)");
        guide.Should().Contain("`hasBody:false` is the ONLY trace",
            because: "describe does not fail on a missing body; reading hasBody back is the only pre-run check "
                + "an agent can make");
        guide.Should().Contain("only an OMITTED one slips through",
            because: "an explicitly empty body IS rejected at build (SendEmailApplier), and stating the "
                + "asymmetry stops an agent from concluding the build validates body presence");
    }

    [Test]
    [Description("Sender's build-time resolution failure stays separate from the evidenced missing-sender run-time failure.")]
    public void Guide_ShouldKeepMalformedSenderApartFromMissingSender()
    {
        string guide = ReadGuide();

        guide.Should().Contain("no MailboxSyncSettings record has sender email",
            because: "a [#SysSettings.<Code>#] sender falls into address resolution and fails the build with "
                + "this text (SendEmailApplier.ApplySender, CrtProcessBuilder sources, read 2026-09-01) — the "
                + "guide must quote what the agent will actually see");
        guide.Should().Contain("a MISSING sender saves and fails the RUN",
            because: "the section also carries the RND-T26743/T26744-evidenced claim that a missing sender is "
                + "not a save-time error; without the missing-vs-malformed split the two claims read as a "
                + "contradiction");
        guide.Should().Contain("missing one as a save-time error",
            because: "the pre-existing evidenced rule (do NOT report a missing sender as a save-time error) "
                + "must survive the ENG-95979 edit rather than be overwritten by the build-time claim");
        guide.Should().Contain("NOT usable for `sender`",
            because: "the recipient ranking prefers a system setting, and without the explicit sender "
                + "exemption an agent would apply rung one to the one field whose build rejects it");
    }

    [Test]
    [Description("Recipient sources are ranked, and a user-supplied literal address gets a push-back with the ranked alternatives.")]
    public void Guide_ShouldRankRecipientSourcesAndAdviseAgainstLiterals()
    {
        string guide = ReadGuide();

        guide.Should().Contain("RANK RECIPIENT SOURCES for `to`/`cc`/`bcc`",
            because: "the ENG-95979 AC asks for an explicit ranking, not the previous prose-only preference");
        guide.Should().Contain("a CONSTANT address LAST",
            because: "the constant is the one source that silently keeps mailing an old destination, so the "
                + "ranking must place it last by name");
        guide.Should().Contain("ADVISE AGAINST storing it and offer the ranked alternatives",
            because: "when the user supplies a literal like hr@company.com the agent must push back with the "
                + "alternatives rather than silently storing the literal (ENG-95979 AC)");
    }

    [Test]
    [Description("Sender gets the same ranked-source discipline as recipients: discover configured mailboxes, reuse before creating, state the reason.")]
    public void Guide_ShouldApplyRankingDisciplineToSender()
    {
        string guide = ReadGuide();

        guide.Should().Contain("SENDER DISCIPLINE",
            because: "the ENG-95979 AC ranks sources for sender as well as to/cc/bcc; without a sender-specific "
                + "rule an agent lands on a mailbox record as a build side-effect and never reasons about WHICH");
        guide.Should().Contain("is NOT an instruction to create one",
            because: "the manual test on ENG-95979 (2026-09-04) saw an agent create a second mailbox for a literal "
                + "sender without checking the one it had configured minutes earlier — discovery and reuse must "
                + "come before creation");
        guide.Should().Contain("ONLY as the LAST rung",
            because: "creating a MailboxSyncSettings record is the sender-side twin of a hard-coded recipient and "
                + "must be the ranked last resort, taken only after the user confirms a distinct identity");
        guide.Should().Contain("the build checks only `SenderEmailAddress`",
            because: "a bare record satisfies the build without guaranteeing delivery, so an agent that creates "
                + "one must say what the tool did and did not set up (SendEmailApplier.ApplySender)");
    }

    private static string ReadGuide() =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), Article);

    private static string ReadTemplateGuide() =>
        ProcessGuideSet.Read(ProcessGuideSet.FindRepositoryRoot(), TemplateArticle);

    /// <summary>
    /// Whitespace-normalised, case-insensitive claim matching (the ChangeAccessRightsGuidanceTests technique): a
    /// hard wrap or a copy-edit must not turn a negative guard green, and the match is reported when it fails.
    /// </summary>
    private static void ShouldNotClaim(string text, string pattern, string subject, string because)
    {
        Match match = Regex.Match(Regex.Replace(text, @"\s+", " "), pattern, RegexOptions.IgnoreCase);
        match.Success.Should().BeFalse(
            because: $"{subject}: {because}" + (match.Success ? $" - found: '{match.Value}'" : string.Empty));
    }

	[Test]
	[Description("Template mode (ENG-95986) has its own owner article: the contract anchor with its CrtProcessBuilder floor, the messageSource switch and the templateEntity shape, the refusals, the object requirement, the subject override and the choose-or-ask rule are pinned there; the entry article routes to it by name.")]
	public void TemplateGuide_ShouldOwnTemplateMode_AndTheEntryShouldRouteToIt()
	{
		string template = ReadTemplateGuide();
		string entry = ReadGuide();

		template.Should().Contain("Part of the process guide set.",
			because: "set membership is declared by this literal token, and the entry-index guard binds only to declared articles carrying it");
		template.Should().Contain("== Template message (messageSource \"template\") ==",
			because: "the mode has its own owner SECTION heading, not a run-in paragraph — the split seam the size budget asked for");
		Match floor = Regex.Match(template, @"TEMPLATE MODE \(CrtProcessBuilder ([0-9]+[.][0-9]+[.][0-9]+[.][0-9]+) or newer\)");
		floor.Success.Should().BeTrue(because: "the template contract declares the four-part CrtProcessBuilder version it is gated on (AGENTS.md compatibility rule)");
		new System.Version(floor.Groups[1].Value).Should().BeGreaterThanOrEqualTo(new System.Version("1.6.2.1"),
			because: "1.6.2.1 is the first archive that carries the mode; an older number would tell an agent on it that the tool serves templates");
		template.Should().Contain("`template` and `body`/`bodyFormat` in one block\n  are REFUSED",
			because: "one element carries one message; an agent that sends both gets a build refusal it must be able to predict");
		template.Should().Contain("more than one email template is named",
			because: "an ambiguous template name is refused rather than resolved by precedence, and the agent must recognise the text");
		template.Should().Contain("`templateEntity` is REQUIRED",
			because: "an object-bound template without a macro source renders every macro empty; the refusal is the agent's cue to bind a record");
		template.Should().Contain("33 of the 38 templates have NO object",
			because: "on a stock environment most templates cannot be personalized, and an agent must check before promising it");
		template.Should().Contain("ASK the user which template to use",
			because: "the stored value is an id, so a guessed template is undetectable afterwards - asking is the rule (ENG-96034 AC 4 mirrored)");
		template.Should().Contain("`subject` in\n  template mode is an OVERRIDE",
			because: "omitting the subject sends the template's own; an agent that always sends one silently overrides every template");
		template.Should().Contain("A `subject` sent ALONE never\n  changes the mode",
			because: "the pre-fix server flipped a template element to custom on a subject-only update; the guide must state the fixed behaviour");
		template.Should().Contain("`templateObject` (OMITTED when none",
			because: "clio's describe serializer drops null fields, so the read-back must be described in the shape the agent actually sees");
		entry.Should().Contain("\"messageSource\"?: \"custom\"|\"template\"",
			because: "the entry article keeps the whole email-block contract, including the mode switch (AC-1)");
		entry.Should().Contain("TEMPLATE MODE is owned by `process-send-email-template`",
			because: "the entry routes to the owner by NAME (CONTRIBUTING: cite sibling articles by name across a split)");
		entry.Should().Contain("`useBackgroundMode` is an element-level field outside the `email`",
			because: "AC-7 asks for the mode-independence of the other fields to be stated, and useBackgroundMode is not part of the email contract at all");
	}

	[Test]
	[Description("The superseded 'templates are not supported' claim is gone from EVERY declared process article and every manifest description, in any of the three wordings that shipped — matched whitespace-insensitively so a reflow cannot hide a regression (AC-8 / refinement R-5).")]
	public void NoProcessArticleOrManifestDescription_ShouldClaimTemplatesAreUnsupported()
	{
		string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
		const string pattern = @"(email\s+)?templates\s+are\s+not\s+supported|no\s+email\s+templates|custom[- ]message\s+(mode\s+)?only";

		foreach (ProcessGuideSet.Article article in ProcessGuideSet.Declared(repositoryRoot))
		{
			ShouldNotClaim(ProcessGuideSet.Read(repositoryRoot, article.SourcePath), pattern, article.ItemId,
				because: "ENG-95986 ships the template message mode; the old claim in any article would make an agent refuse a request the tool now serves");
		}
		using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
		foreach (JsonElement resource in manifest.RootElement.GetProperty("resources").EnumerateArray())
		{
			string description = resource.TryGetProperty("description", out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;
			ShouldNotClaim(description, pattern, resource.GetProperty("itemId").GetString() ?? "?",
				because: "a manifest description is what an MCP client shows an agent choosing which guide to read");
		}
	}

	[Test]
	[Description("The missing-message trap keeps its error text, is explained from the platform's mode dispatch, and describes the read-back in the WIRE shape clio emits: the mode and template keys are omitted, not null.")]
	public void Guide_ShouldExplainTheMissingMessageTrapFromTheModeDispatch()
	{
		string guide = ReadGuide();

		guide.Should().Contain("RUNS in TEMPLATE mode with no template",
			because: "an unset BodyTemplateType reads as 0, which is the template provider - the mechanism behind the run-time text (CrtProcessDesigner 7.8.0 sources, 2026-09-09)");
		guide.Should().Contain("`messageSource` and `template` are OMITTED from\n  the read-back (clio drops null fields)",
			because: "clio's describe serializer uses WhenWritingNull, so an agent never sees messageSource:null; the check must name the absence");
		ShouldNotClaim(guide, @"messageSource\s*:\s*null", "process-send-email",
			because: "describing a null key the agent will never see sends it looking for the wrong signal");
		ShouldNotClaim(guide, @"subject-only element points at a body template that does not exist", "process-send-email",
			because: "that explanation attributed the template provider's text to a custom-message element and was wrong");
		ShouldNotClaim(guide, @"which cannot produce it", "process-send-email",
			because: "only the no-mode run was observed; the custom-provider corollary is stated as unsupported by the sources, not as a verified fact");
	}

}

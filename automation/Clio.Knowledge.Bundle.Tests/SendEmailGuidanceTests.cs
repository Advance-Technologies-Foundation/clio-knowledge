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
}

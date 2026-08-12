using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the guarantees the `product-telemetry` article is the sole owner of.
/// </summary>
/// <remarks>
/// These assertions used to live in the consumer toolkit, which restated the vocabulary and the
/// consent rules in its own contract file. That duplication is what this article replaced, so the
/// regression guard has to move with the ownership — otherwise the privacy wording is published from
/// here while only a downstream repository checks it, and deleting a sentence here fails nothing.
/// </remarks>
[TestFixture]
public sealed class ProductTelemetryGuidanceTests
{
    private const string ArticlePath = "guidance/mcp/guides/operations/product-telemetry.md";

    // clio validates event_name against a closed allow-list, so an article that documents an extra,
    // missing, or misspelled stage sends every reader into a rejected call.
    private static readonly string[] Stages =
    [
        "workflow_started", "clarification_requested", "user_input_received", "plan_presented",
        "plan_skipped", "plan_blocked", "plan_changes_requested", "plan_approved", "build_started",
        "work_item_completed", "workflow_completed", "workflow_failed", "changes_requested",
        "changes_applied"
    ];

    [Test]
    [Description("Verifies the article documents the whole stage vocabulary and the workflow field that keeps those stage names flow-agnostic.")]
    public void Article_ShouldDocumentTheStageVocabularyAndTheWorkflowField()
    {
        // Arrange
        string article = ReadArticle();

        // Assert
        foreach (string stage in Stages)
        {
            article.Should().Contain(stage,
                because: "a stage missing from the article is a stage no consumer emits");
        }
        // The set of `workflow` VALUES is deliberately not asserted here: clio validates the token
        // shape, not a closed list, precisely so a new consumer flow does not need a clio release.
        // Each consumer owns its own values and guards them in its own suite.
        article.Should().Contain("`workflow` field",
            because: "the flow dimension has to be named as a field for the stages to stay generic");
        article.Should().Contain("short lowercase tokens",
            because: "the bounded token shape is what keeps free text and customer data out");
        article.Should().Contain("Do NOT invent a per-flow event name",
            because: "per-flow names are the failure this vocabulary replaced, and clio rejects them");
        article.Should().Contain("EVEN WHEN NO SKILL FILE IS LOADED",
            because: "treating a skill-less agent session as ad-hoc use is what left whole workflows unreported");
    }

    [Test]
    [Description("Verifies the consent disclosure keeps every element an informed decision needs, including the withdrawal right.")]
    public void Article_ShouldDocumentTheConsentDisclosureAndWithdrawal()
    {
        // Arrange
        string article = ReadArticle();

        // Assert
        article.Should().Contain("get-telemetry-consent");
        article.Should().Contain("read-only check",
            because: "the consent probe must never be mistaken for a write");
        article.Should().Contain("telemetry_consent=unknown",
            because: "the developer is asked in exactly one state");
        article.Should().Contain("single-purpose interaction",
            because: "bundling consent into discovery questions makes the answer uninformed");
        article.Should().Contain("uploads events to Creatio servers");
        article.Should().Contain("up to one year",
            because: "retention is part of what makes the disclosure informed");
        article.Should().Contain("withdraw-telemetry-consent");
        article.Should().Contain("withdrawn at any time",
            because: "withdrawal must be as easy as granting (GDPR Art. 7(3))");
    }

    [Test]
    [Description("Verifies the data-minimization prohibition and every forbidden category survive, and that the article does not claim the dataset is free of personal data.")]
    public void Article_ShouldForbidSensitiveDataAndNotOverclaimAnonymity()
    {
        // Arrange
        string article = ReadArticle();

        // Assert
        article.Should().Contain("MUST NOT carry sensitive data",
            because: "data minimization is the load-bearing guarantee for an anonymous ingest endpoint");
        foreach (string forbidden in new[]
        {
            "full prompts", "passwords", "tokens", "customer names", "raw usernames",
            "generated app content", "full MCP request/response payloads"
        })
        {
            article.Should().Contain(forbidden,
                because: "each forbidden category is an explicit ENG-89424 acceptance item");
        }
        // The dataset carries a random installation id, which is pseudonymous personal data under
        // GDPR Recital 30 — so the article may only deny DIRECTLY IDENTIFYING data.
        article.Should().Contain("pseudonymous installation identifier");
        article.Should().Contain("directly identifying personal data");
        article.Should().Contain("NEVER derive `session_id`",
            because: "a session id derived from user or host data would re-identify the run");
    }

    [Test]
    [Description("Verifies a terminal stage must report the verified outcome, so an unverified write cannot be counted as a completed run.")]
    public void Article_ShouldRequireTerminalStagesToReportVerifiedOutcomes()
    {
        // Arrange
        string article = ReadArticle();

        // Assert
        // Measured failure, not a hypothetical: a probe run emitted workflow_completed for a theme
        // whose prescribed read-back never showed it, because create-theme had answered success.
        article.Should().Contain("is NOT that evidence",
            because: "a write tool's success response is the exact signal that produced a false completion");
        article.Should().Contain("emit `workflow_failed`",
            because: "an unverifiable result is a failed run, whatever the write call returned");
        article.Should().Contain("inflates the completion rate",
            because: "the asymmetry is the reason this rule exists — a wrong terminal stage corrupts the "
                + "funnel while a missing one only lowers coverage");
    }

    [Test]
    [Description("Verifies the article keeps telemetry subordinate to the developer's task, including on a rejected event.")]
    public void Article_ShouldKeepTelemetryNonBlocking()
    {
        // Arrange
        string article = ReadArticle();

        // Assert
        article.Should().Contain("MUST NEVER gate, delay, or alter");
        article.Should().Contain("unknown-event-name",
            because: "a clio too old to accept a stage must degrade to silence, never to a blocker");
        article.Should().Contain("Stop emitting for the rest of the run",
            because: "the recovery is to stop emitting and carry on, not to retry or fall back");
    }

    private static string ReadArticle()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        string root = current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
        string article = File.ReadAllText(
            Path.Combine(root, ArticlePath.Replace('/', Path.DirectorySeparatorChar)));
        // Collapse whitespace so the assertions pin WORDING, not line wrapping. Prose guidance gets
        // rewrapped constantly; a test that fails on a reflowed paragraph trains people to edit the
        // test instead of reading it.
        return System.Text.RegularExpressions.Regex.Replace(article, @"\s+", " ");
    }
}

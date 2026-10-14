using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the articles of the ENG-102795 split inside what a Copilot CLI agent receives INLINE from
/// <c>get-guidance</c>.
/// </summary>
/// <remarks>
/// <para>
/// The CAADT release gate runs its agent through Copilot CLI, which hands a tool result to the model whole
/// up to about 19.8 KB and, from about 20.7 KB, writes it to a file instead. The agent then reads that
/// file back in pieces - "Output too large", a view, a grep - and every piece is another model request.
/// Splitting process-modeling, app-modeling, workplaces and existing-app-maintenance into a core and a
/// <c>-details</c> article removed the read-backs those articles caused on every run; this fixture is what
/// keeps the split from eroding, because articles grow back: process-branch-conditions went from 19.8 to
/// 20.3 KB in two days. core-rules, read on every operation, was split into core-rules and core-rules-details the
/// same way and is held to the same budget.
/// </para>
/// <para>
/// Scoped to the pieces of that split, deliberately. A library-wide ratchet would freeze the articles other
/// teams are editing in open pull requests, and the check runs on the merge result, so it would turn them
/// red without a rebase. Measuring the rest of the library this way is a decision for its owners, not a
/// side effect of one split. <c>routing</c> is out of scope for the same reason: it is already over this
/// budget and edited by most guidance pull requests.
/// </para>
/// <para>
/// It complements <see cref="ProcessGuideResponseSizeTests"/> and <see cref="MobileGuideResponseSizeTests"/>.
/// Those measure characters under the default JSON encoder against a cliff probed in another harness; this
/// measures the UTF-8 BYTES clio writes today (see <see cref="ReplyBytes"/>) against the inline limit of the
/// harness the release gate uses. An article here that outgrows the budget is split again at a section
/// boundary (CONTRIBUTING, "An article must fit in one get-guidance response"); the budget is not raised.
/// </para>
/// </remarks>
[TestFixture]
public sealed class GuidanceInlineReplyBudgetTests
{
    /// <summary>
    /// The reply size each article of the split must stay within: 19,000 bytes, about 800 under the largest
    /// reply the release gate's Copilot CLI was seen to pass inline.
    /// </summary>
    internal const int InlineReplyBudgetBytes = 19_000;

    /// <summary>
    /// Everything in a <c>get-guidance</c> reply except the article text: the feedback policy, name, uri,
    /// itemId, topicId, libraryVersion, sequence, digest and the article's resolved local path.
    /// <para>
    /// Measured against clio 8.1.0.139 on 2026-10-12, default settings (feedback mode <c>ask</c>), with
    /// <c>CLIO_HOME</c> at <c>C:\dev\ab\hP3</c>: routing 19,964 bytes = 18,827 of article + 1,137;
    /// process-modeling 24,836 = 23,654 + 1,182; app-modeling 29,207 = 28,045 + 1,162. The local path is the
    /// part that varies: it grows with <c>CLIO_HOME</c>, and the default <c>%LOCALAPPDATA%\creatio\clio</c>
    /// adds about 40 bytes to the home measured. 1,300 covers a home path about 120 bytes longer than the
    /// measured one, so the estimate here is at most about 1% above a real reply.
    /// </para>
    /// <para>
    /// PROVENANCE: the envelope is built by the clio repository (<c>GuidanceGetTool</c>). If its fields or
    /// the feedback-policy text grow, every real reply grows while this constant stays put. Re-measure by
    /// calling <c>get-guidance</c> and subtracting the JSON-escaped UTF-8 length of the article text.
    /// </para>
    /// </summary>
    private const int EnvelopeAllowanceBytes = 1_300;

    /// <summary>
    /// The pieces of the split, written down rather than derived: an edit that drops one from the manifest
    /// must fail here instead of silently leaving the set.
    /// </summary>
    private static readonly string[] SplitArticles =
    [
        "process-modeling",
        "process-modeling-details",
        "app-modeling",
        "app-modeling-details",
        "workplaces",
        "workplaces-details",
        "existing-app-maintenance",
        "existing-app-maintenance-details",
        "process-access-rights",
        "process-access-rights-details",
        "process-script-task",
        "process-script-task-details",
        "process-open-edit-page",
        "process-open-edit-page-details",
        "process-branch-conditions",
        "process-branch-conditions-details",
        "process-versions",
        "process-versions-details",
        "process-data-elements",
        "process-data-elements-details",
        "process-formulas",
        "process-formulas-details",
        "process-perform-task",
        "process-perform-task-details",
        "core-rules",
        "core-rules-details"
    ];

    [Test]
    [Description("Every article of the split answers get-guidance within the inline reply budget.")]
    public void SplitArticles_ShouldFitTheInlineReplyBudget()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        Dictionary<string, string> declared = GuidanceArticles(repositoryRoot);

        SplitArticles.Except(declared.Keys).Should().BeEmpty(
            because: "the manifest has to declare every piece of the split as guidance; an article this fixture "
                + "cannot find is one it has stopped measuring");

        (string ItemId, int Size)[] measured = [.. SplitArticles
            .Where(declared.ContainsKey)
            .Select(itemId => (itemId, Size: ReplyBytes(ProcessGuideSet.Read(repositoryRoot, declared[itemId]))))
            .OrderByDescending(article => article.Size)];
        foreach ((string itemId, int size) in measured)
        {
            TestContext.WriteLine(
                $"{itemId,-36} {size,7:N0}  {(double)size / InlineReplyBudgetBytes,6:P1} of budget");
        }

        measured.Where(article => article.Size > InlineReplyBudgetBytes)
            .Select(article => $"{article.ItemId} at {article.Size:N0}")
            .Should().BeEmpty(
                because: $"a get-guidance reply over {InlineReplyBudgetBytes:N0} bytes is spilled to a file that the "
                    + "agent reads back in pieces, one model request per piece. Move evidence, history or rare "
                    + "cases to the -details article, or split again at a section boundary, rather than raising "
                    + "the budget");
    }

    /// <summary>
    /// The size of a <c>get-guidance</c> reply for an article text, in UTF-8 bytes: the article as clio
    /// writes it (<see cref="ArticleBytes"/>) plus the envelope around it.
    /// </summary>
    internal static int ReplyBytes(string articleText) => ArticleBytes(articleText) + EnvelopeAllowanceBytes;

    /// <summary>
    /// The size of an article text as clio writes it into a <c>get-guidance</c> reply, in UTF-8 bytes,
    /// without the envelope. clio writes tool results with <c>McpResultJsonEncoder</c>:
    /// <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/> plus a six-character <c>\uXXXX</c> escape for
    /// every BMP character of category Format. Under the relaxed encoder backticks, dashes and non-ASCII
    /// letters go out as themselves, so measuring with the default encoder would overstate a backtick-dense
    /// article by a fifth or more.
    /// </summary>
    internal static int ArticleBytes(string articleText)
    {
        int escapedFormatBytes = articleText
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format
                && !char.IsSurrogate(character))
            .Sum(character => 6 - Encoding.UTF8.GetByteCount(character.ToString()));
        return Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(articleText, RelaxedJson))
            + escapedFormatBytes;
    }

    private static readonly JsonSerializerOptions RelaxedJson =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Every resource the manifest serves through get-guidance (guidance role), by itemId.</summary>
    private static Dictionary<string, string> GuidanceArticles(string repositoryRoot)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "guidance")
            .ToDictionary(
                resource => resource.GetProperty("itemId").GetString()!,
                resource => resource.GetProperty("sourcePath").GetString()!,
                StringComparer.Ordinal);
    }
}

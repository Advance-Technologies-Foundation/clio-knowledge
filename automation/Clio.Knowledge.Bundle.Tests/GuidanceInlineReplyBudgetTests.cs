using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps every guidance article inside what a Copilot CLI agent receives INLINE from <c>get-guidance</c>.
/// </summary>
/// <remarks>
/// <para>
/// The CAADT release gate and customers on Copilot run the agent through Copilot CLI, which hands a tool
/// result to the model whole up to 20,480 bytes and writes anything larger to a file instead. The agent then
/// reads that file back in pieces - "Output too large", a view, a grep - and every piece is another model
/// request that re-reads the whole context. Articles grow back after a split, and new articles start large,
/// so a budget held only for a hand-written list misses exactly the articles that need it.
/// </para>
/// <para>
/// So the set is DERIVED from the manifest: every resource with the guidance role is measured, a new article
/// from its first pull request. The only way out is the explicit list <see cref="ExemptArticles"/>.
/// An article that outgrows the budget is split at a
/// section boundary (CONTRIBUTING, "An article must fit in one get-guidance response"); the budget is not
/// raised.
/// </para>
/// <para>
/// It complements <see cref="ProcessGuideResponseSizeTests"/> and <see cref="MobileGuideResponseSizeTests"/>.
/// Those measure characters under the default JSON encoder against a cliff probed in another harness; this
/// measures the UTF-8 BYTES clio writes today (see <see cref="ReplyBytes"/>) against the inline limit of
/// Copilot CLI.
/// </para>
/// </remarks>
[TestFixture]
public sealed class GuidanceInlineReplyBudgetTests
{
    /// <summary>
    /// The reply size every article must stay within: 19,000 bytes, about 800 under the largest reply the
    /// release gate's Copilot CLI was seen to pass inline and 1,480 under its 20,480-byte spill threshold.
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
    /// Articles allowed to exceed <see cref="InlineReplyBudgetBytes"/>, by itemId. Every other guidance article
    /// in the manifest is measured. These are the articles that were already over the budget when the check
    /// started to cover the whole library; remove an entry once its article fits.
    /// </summary>
    private static readonly string[] ExemptArticles =
    [
        "mobile-page-modification",
        "freedom-page-web-to-mobile-conversion",
        "page-schema-handlers",
        "page-modification-overview",
        "esq-filter-parsing",
        "object-rights",
        "web-service-registration",
        "related-list",
        "esq-filters-frontend",
        "esq-filters-backend",
        "routing",
        "theming",
        "page-modification-standard-components",
        "page-schema-validators",
        "process-parameters",
        "page-modification",
        "page-schema-creatio-devkit-common",
        "page-modification-template-supplied-parts",
        "process-send-email",
        "web-service-soap",
        "page-schema-translation",
        "business-rules",
        "page-schema-resources",
        "page-modification-components",
        "custom-mcp-tools",
        "process-parameters-details",
        "process-activity-connections"
    ];

    [Test]
    [Description("Every guidance article that is not exempt answers get-guidance within the inline reply budget.")]
    public void EveryGuidanceArticle_ShouldFitTheInlineReplyBudget()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        Dictionary<string, string> declared = GuidanceArticles(repositoryRoot);

        (string ItemId, int Size)[] measured = [.. declared
            .Select(article => (article.Key, Size: ReplyBytes(ProcessGuideSet.Read(repositoryRoot, article.Value))))
            .OrderByDescending(article => article.Size)
            .ThenBy(article => article.Key, StringComparer.Ordinal)];
        foreach ((string itemId, int size) in measured)
        {
            TestContext.WriteLine(
                $"{itemId,-44} {size,7:N0}  {(double)size / InlineReplyBudgetBytes,6:P1} of budget"
                + (ExemptArticles.Contains(itemId) ? "   exempt" : string.Empty));
        }

        string[] overBudget = [.. measured
            .Where(article => article.Size > InlineReplyBudgetBytes && !ExemptArticles.Contains(article.ItemId))
            .Select(article => $"{article.ItemId} at {article.Size:N0}")];
        overBudget.Should().BeEmpty(
            because: $"a get-guidance reply over {InlineReplyBudgetBytes:N0} bytes is spilled to a file that the "
                + "agent reads back in pieces, one model request per piece. Move evidence, history or rare "
                + "cases to a -details article, or split again at a section boundary, rather than raising "
                + $"the budget. Over the budget ({overBudget.Length}): {string.Join("; ", overBudget)}");
    }

    [Test]
    [Description("Every exemption names a guidance article the manifest declares, so the list cannot rot.")]
    public void ExemptArticles_ShouldBeDeclaredGuidance()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        Dictionary<string, string> declared = GuidanceArticles(repositoryRoot);

        ExemptArticles.Except(declared.Keys).Should().BeEmpty(
            because: "an exemption for an article the manifest no longer declares is a stale entry; remove it");
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

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps every guidance article's <c>get-guidance</c> reply inside what a Copilot CLI agent receives
/// INLINE, and stops the articles already past that line from growing.
/// </summary>
/// <remarks>
/// <para>
/// The CAADT release gate runs its agent through Copilot CLI, which hands a tool result to the model whole
/// up to about 19.8 KB and, from about 20.7 KB, writes it to a file instead. The agent then reads that
/// file back in pieces - "Output too large", a view, a grep - and every piece is another model request.
/// Splitting process-modeling, app-modeling, workplaces and existing-app-maintenance under the line
/// (ENG-102795) removed the read-backs those articles caused on every run; this fixture is what keeps the
/// split from eroding, because articles grow back: process-branch-conditions went from 19.8 to 20.3 KB in
/// two days.
/// </para>
/// <para>
/// It complements <see cref="ProcessGuideResponseSizeTests"/> and <see cref="MobileGuideResponseSizeTests"/>
/// rather than replacing them. Those measure characters under the default JSON encoder against a cliff
/// probed in another harness; this measures the UTF-8 BYTES clio writes today (see <see cref="ReplyBytes"/>)
/// against the inline limit of the harness the release gate uses, and it covers every guidance article,
/// not one folder.
/// </para>
/// <para>
/// <see cref="OverBudgetCeilings"/> is a RATCHET over the articles that were already past the budget when
/// this fixture was added, each held at its exact size then. An article on the list may shrink and never
/// grow; lower its ceiling when it shrinks, and delete the entry once it fits. An article NOT on the list
/// must fit the budget - the remedy for one that does not is a split at a section boundary (see
/// CONTRIBUTING, "An article must fit in one get-guidance response"), not a new entry.
/// </para>
/// </remarks>
[TestFixture]
public sealed class GuidanceInlineReplyBudgetTests
{
    /// <summary>
    /// The reply size every guidance article must stay within: 19,000 bytes, about 800 under the largest
    /// reply the release gate's Copilot CLI was seen to pass inline.
    /// </summary>
    private const int InlineReplyBudgetBytes = 19_000;

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
    /// The articles already over <see cref="InlineReplyBudgetBytes"/> when this fixture was added, each at its
    /// exact size then. See the class remarks: lower a ceiling when the article shrinks, never raise one.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> OverBudgetCeilings =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["mobile-page-modification"] = 59_475,
            ["freedom-page-web-to-mobile-conversion"] = 57_179,
            ["page-schema-handlers"] = 42_771,
            ["esq-filter-parsing"] = 30_883,
            ["related-list"] = 29_881,
            ["esq-filters-frontend"] = 28_272,
            ["page-modification-overview"] = 28_156,
            ["esq-filters-backend"] = 25_817,
            ["page-modification-standard-components"] = 24_809,
            ["page-schema-validators"] = 23_718,
            ["process-access-rights"] = 22_753,
            ["theming"] = 21_929,
            ["page-schema-creatio-devkit-common"] = 21_233,
            ["core-rules"] = 21_021,
            ["process-open-edit-page"] = 20_993,
            ["page-modification-template-supplied-parts"] = 20_889,
            ["process-script-task"] = 20_656,
            ["process-branch-conditions"] = 20_421,
            ["page-modification"] = 20_209,
            ["object-rights"] = 19_925,
            ["custom-mcp-tools"] = 19_904,
            ["process-data-elements"] = 19_673,
            ["process-formulas"] = 19_480,
            ["routing"] = 19_411,
            ["process-versions"] = 19_326,
            ["page-schema-resources"] = 19_173,
            ["process-perform-task"] = 19_056,
        };

    [Test]
    [Description("Every guidance article answers get-guidance inline, or - for the articles recorded over the budget - does not grow.")]
    public void EveryGuidanceArticle_ShouldFitTheInlineReplyBudgetOrItsRecordedCeiling()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        (string ItemId, int Size)[] measured = [.. GuidanceArticles(repositoryRoot)
            .Select(article => (article.ItemId, Size: ReplyBytes(ProcessGuideSet.Read(repositoryRoot, article.SourcePath))))
            .OrderByDescending(article => article.Size)];

        measured.Should().NotBeEmpty(because: "a scan that measured nothing would prove nothing");
        foreach ((string itemId, int size) in measured.Where(article => article.Size > InlineReplyBudgetBytes * 0.9))
        {
            TestContext.WriteLine(
                $"{itemId,-44} {size,7:N0}  {(double)size / InlineReplyBudgetBytes,6:P1} of budget"
                + (OverBudgetCeilings.TryGetValue(itemId, out int ceiling) ? $"   ceiling {ceiling:N0}" : string.Empty));
        }

        string[] overBudget = [.. measured
            .Where(article => !OverBudgetCeilings.ContainsKey(article.ItemId))
            .Where(article => article.Size > InlineReplyBudgetBytes)
            .Select(article => $"{article.ItemId} at {article.Size:N0}")];
        string[] grown = [.. measured
            .Where(article => OverBudgetCeilings.TryGetValue(article.ItemId, out int ceiling) && article.Size > ceiling)
            .Select(article => $"{article.ItemId} at {article.Size:N0} (ceiling {OverBudgetCeilings[article.ItemId]:N0})")];

        overBudget.Should().BeEmpty(
            because: $"a get-guidance reply over {InlineReplyBudgetBytes:N0} bytes is spilled to a file that the "
                + "agent reads back in pieces, one model request per piece. Split the article at a section "
                + "boundary rather than adding it to OverBudgetCeilings");
        grown.Should().BeEmpty(
            because: "an article already over the inline budget may shrink but not grow: pay for an addition "
                + "by trimming or splitting the same article, then lower its ceiling to the new size");
    }

    [Test]
    [Description("Every recorded ceiling names a declared guidance article that is still over the budget, so the list only ever shrinks.")]
    public void OverBudgetCeilings_ShouldNameDeclaredArticlesStillOverTheBudget()
    {
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        Dictionary<string, int> sizes = GuidanceArticles(repositoryRoot).ToDictionary(
            article => article.ItemId,
            article => ReplyBytes(ProcessGuideSet.Read(repositoryRoot, article.SourcePath)),
            StringComparer.Ordinal);

        OverBudgetCeilings.Keys.Except(sizes.Keys).Should().BeEmpty(
            because: "a ceiling for an article the manifest no longer declares guards nothing; when an article "
                + "is split or retired, its pieces have to fit the budget on their own");
        OverBudgetCeilings.Keys.Where(sizes.ContainsKey).Where(itemId => sizes[itemId] <= InlineReplyBudgetBytes)
            .Should().BeEmpty(
                because: "an article that now fits the budget leaves the list, so the budget, not a stale "
                    + "ceiling, holds it from then on");
    }

    /// <summary>
    /// The size of a <c>get-guidance</c> reply for an article text, in UTF-8 bytes. clio writes tool results
    /// with <c>McpResultJsonEncoder</c>: <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/> plus a
    /// six-character <c>\uXXXX</c> escape for every BMP character of category Format. Under the relaxed
    /// encoder backticks, dashes and non-ASCII letters go out as themselves, so measuring with the default
    /// encoder would overstate a backtick-dense article by a fifth or more.
    /// </summary>
    internal static int ReplyBytes(string articleText)
    {
        int escapedFormatBytes = articleText
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format
                && !char.IsSurrogate(character))
            .Sum(character => 6 - Encoding.UTF8.GetByteCount(character.ToString()));
        return Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(articleText, RelaxedJson))
            + escapedFormatBytes
            + EnvelopeAllowanceBytes;
    }

    private static readonly JsonSerializerOptions RelaxedJson =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Every resource the manifest serves through get-guidance, which answers only the guidance role.</summary>
    private static IEnumerable<ProcessGuideSet.Article> GuidanceArticles(string repositoryRoot)
    {
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        return [.. manifest.RootElement.GetProperty("resources")
            .EnumerateArray()
            .Where(resource => resource.GetProperty("role").GetString() == "guidance")
            .Select(resource => new ProcessGuideSet.Article(
                resource.GetProperty("itemId").GetString()!,
                resource.GetProperty("sourcePath").GetString()!))];
    }
}

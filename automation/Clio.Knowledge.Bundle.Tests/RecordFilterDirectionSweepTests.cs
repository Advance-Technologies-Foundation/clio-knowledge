using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Sweeps every guidance article AND the manifest for the "Change access rights" record-filter inversion.
///
/// <para>The named pins in <see cref="ChangeAccessRightsGuidanceTests"/> all read a <c>.md</c> through
/// <c>ProcessGuideSet.Read</c>. The manifest's <c>description</c> fields were the one surface none of them
/// covered — and that is exactly where the inversion survived, stating BOTH filter states backwards on the
/// text an MCP client shows an agent choosing which guide to open. A pin per surface is always one surface
/// short; this sweeps whatever is there.</para>
///
/// <para>The fact: <c>ChangeAdminRightsUserTask.InternalExecute</c> gates on a non-empty
/// <c>DataSourceFilters</c>, so an ABSENT record filter never enters that branch — the query runs UNFILTERED
/// with record permissions disabled and the change lands on EVERY row. A filter that is PRESENT but carries
/// NO CONDITIONS takes the "filters empty" exit and changes nothing. The element has no output parameters,
/// so prose is the entire contract.</para>
/// </summary>
[TestFixture]
public sealed class RecordFilterDirectionSweepTests
{
    private const string InertWords =
        @"match(es)?\s+no\s+records|changes?\s+nothing|changes?\s+no\s+permissions|cannot\s+act|"
        + @"\binert\b|silent\s+no-?op|(?:does|do|did)\s+nothing|is\s+an?\s+no-?op";

    private const string WideWords =
        @"EVERY\s+record|every\s+row|widest|unbounded|runs?\s+UNFILTERED";

    // Absence is described as a noun ("no record filter") AND as the action that produces it ("clearing the
    // record filter"). The action phrasing is what the write paths actually use; omitting it let a live
    // inversion through the clio sweep while it reported green.
    private const string AbsentSubject =
        @"no\s+record\s+filter|NO\s+filter|without\s+a\s+filter|absent\s+filter|filter\s+is\s+absent|"
        + @"no\s+filter\s+at\s+all|clear(?:s|ed|ing)?\s+(?:\w+\s+){0,3}?record\s+filter|"
        + @"record\s+filter\s+(?:was|is|were)\s+cleared|filter\s+(?:was|is)\s+CLEARED";

    private const string ConditionlessSubject =
        @"no\s+conditions|conditionless|carries\s+no\s+condition";

    // Gap stop-tokens, broader than the subjects: a contrastive sentence names the other state in shorthand
    // ("while an ABSENT one acts on every record"), and the gap must stop there so each claim is attributed
    // to the subject it belongs to rather than needing an exemption per correct sentence.
    private const string AbsentMention = AbsentSubject + @"|\bABSENT\b";
    private const string ConditionlessMention = ConditionlessSubject + @"|\bPRESENT\b";

    // A line may quote a forbidden phrasing in order to REJECT it. Exact phrases only — the bare word
    // "would" would exempt a real inversion phrased as a prediction.
    private static readonly string[] ExemptionMarkers =
    [
        "is a FAIL", "must not come back", "CORRECTED", "is the WIDEST", "is the WIDE", "not the inert",
        "rather than wide", "NOT to none", "would be false", "must never", "rather than inert", "NotContain",
        "gets told", "is how a caller", "would tell the caller"
    ];

    private const int ExemptionRadius = 130;

    // Prose wraps - hard-wrapped Markdown especially - so subject and claim routinely sit on different
    // lines. Matching one line at a time cannot see those at all.
    private const int WindowLines = 3;

    [Test]
    [Description("No shipped guidance file or manifest description may call an ABSENT record filter inert. That claim presents the widest permission change this element can produce - every row of the object, with record permissions disabled - as harmless, on an element with no output parameters to contradict it at run time.")]
    public void NoShippedText_MayDescribeAnAbsentRecordFilter_AsInert()
    {
        // Arrange
        Regex inversion = Inversion(AbsentSubject, InertWords, ConditionlessMention);

        // Act
        IReadOnlyList<string> offenders = Sweep(inversion);

        // Assert
        offenders.Should().BeEmpty(
            because: "an ABSENT record filter makes the element apply the change to EVERY record of its "
                + "object; calling it inert is the sentence this guidance shipped and had to remove:"
                + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Test]
    [Description("No shipped guidance file or manifest description may call a PRESENT-but-conditionless record filter wide. It is the mirror error, and both halves were always swapped together - a conditionless filter takes the runtime's 'filters empty' exit and changes nothing.")]
    public void NoShippedText_MayDescribeAConditionlessRecordFilter_AsWide()
    {
        // Arrange
        Regex inversion = Inversion(ConditionlessSubject, WideWords, AbsentMention);

        // Act
        IReadOnlyList<string> offenders = Sweep(inversion);

        // Assert
        offenders.Should().BeEmpty(
            because: "a conditionless record filter is the INERT state; describing it as acting on every "
                + "record swaps it with the absent-filter state, which is how both halves went wrong at once:"
                + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    // One logical line: this line plus the next few, with the punctuation that exists only because the text
    // wraps flattened away, so a sentence split across lines reads as the sentence it is.
    private static string Window(string[] lines, int index)
    {
        StringBuilder joined = new();
        for (int offset = 0; offset < WindowLines && index + offset < lines.Length; offset++)
        {
            joined.Append(lines[index + offset].Replace("\"", " ").TrimStart(' ', '+', '#', '-', '*')).Append(' ');
        }

        return joined.ToString();
    }

    private static Regex Inversion(string subject, string claim, string otherSubject) =>
        new($@"(?<subject>{subject})(?:(?!{otherSubject})[^.;]){{0,160}}?(?<claim>{claim})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static IReadOnlyList<string> Sweep(Regex inversion)
    {
        string root = ProcessGuideSet.FindRepositoryRoot();
        // Every DECLARED resource body plus the manifest. Reading only guidance/**/*.md skipped 48 shipped
        // bodies under catalog/ and references/ - and "the surface nobody swept" is precisely how this
        // inversion kept surviving.
        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(root, "bundle-source.json")));
        string[] files =
        [
            .. manifest.RootElement.GetProperty("resources").EnumerateArray()
                .Select(resource => resource.TryGetProperty("sourcePath", out JsonElement path)
                    ? path.GetString()
                    : null)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.Combine(root, path!.Replace('/', Path.DirectorySeparatorChar)))
                .Distinct(StringComparer.OrdinalIgnoreCase),
            Path.Combine(root, "bundle-source.json")
        ];

        // A sweep that reads nothing reports green forever, which is worse than no sweep: it turns
        // "nobody checked" into "checked and clean".
        files.Length.Should().BeGreaterThan(20,
            because: "the library publishes dozens of guidance articles, so a near-empty list means the "
                + "enumeration failed rather than that there is nothing to sweep");

        List<string> offenders = [];
        foreach (string file in files.Where(File.Exists))
        {
            // This fixture necessarily contains both halves of every pattern it forbids.
            if (file.EndsWith($"{nameof(RecordFilterDirectionSweepTests)}.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string window = Window(lines, i);

                // EVERY match, not just the first: an exempted contrastive sentence must not hide a real
                // inversion further along the same window.
                for (Match match = inversion.Match(window); match.Success; match = match.NextMatch())
                {
                    int from = Math.Max(0, match.Index - ExemptionRadius);
                    int to = Math.Min(window.Length, match.Index + match.Length + ExemptionRadius);
                    string vicinity = window[from..to];
                    if (ExemptionMarkers.Any(m => vicinity.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    offenders.Add($"  {Path.GetRelativePath(root, file)}:{i + 1}: ...{vicinity.Trim()}...");
                    break;
                }
            }
        }

        return offenders;
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// One repository-wide scan of the references a published body makes to other published items
/// (clio-knowledge issue #8). Before it, only <c>routing.md</c> and the object-rights article had their
/// <c>name=</c> citations checked, and <c>docs://</c> routes in article bodies only for supporting references -
/// so <c>core-rules.md</c>, which every operation reads, pointed at a route no item declares and nothing
/// went red. A dangling pointer reads as a complete instruction; the reader simply never gets the rule.
///
/// Every published body is walked, driven from <c>bundle-source.json</c> rather than from folders, and
/// each failure is reported as <c>file:line: reference -> what is missing</c>, all of them at once.
///
/// Each rule mirrors how Clio resolves that form (clio master 7ab76575b):
/// <list type="bullet">
/// <item>a <c>get-guidance</c> name matches the <c>itemId</c> or <c>topicId</c> of a <c>guidance</c>-role item
/// only, ordinally (<c>KnowledgeResolver.FindTopic</c>), so naming a supporting reference or a reference example
/// is dangling even though the id exists, and so is a near miss in case or punctuation;</item>
/// <item>a <c>docs://knowledge/...</c> or <c>docs://mcp/...</c> route must equal a declared <c>uri</c> or
/// <c>legacyUris</c> entry exactly - the <c>docs://mcp/guides/{family}/{guide}</c> template forwards to that
/// exact lookup, it does not search by the last segment;</item>
/// <item>a relative markdown link never resolves: Git delivery reads bodies at their <c>sourcePath</c>, but a
/// release bundle stores every body flat as <c>resources/&lt;itemId&gt;.md</c>
/// (<c>KnowledgeBundleRuntime.CreateArticle</c>), so a link that works in the repository is dead in the
/// shipped bundle. Only a <c>docs://</c> route survives every transport.</item>
/// </list>
/// </summary>
[TestFixture]
public sealed class ReferenceIntegrityTests
{
    internal const string GuideNameKind = "guide name";
    internal const string RouteKind = "docs route";
    internal const string LinkKind = "relative link";
    internal const string CatalogIdKind = "catalog id";

    /// <summary>Sentence punctuation that may follow a reference without being part of it.</summary>
    private static readonly char[] SentencePunctuation = ['.', ',', ';', ':', ')'];

    /// <summary>
    /// <c>name=&lt;id&gt;</c> as routing and articles write it. The lookbehind skips other tools' arguments
    /// (<c>schema-name=</c>, <c>event_name=</c>). The WHOLE argument is captured, so a trailing <c>-</c> or
    /// <c>_</c>, or a capital letter, is compared and reported rather than trimmed away or skipped: Clio compares
    /// the name exactly and answers a near miss with <c>availableGuides</c> only. Placeholders such as
    /// <c>name=&lt;env&gt;</c> start with a character the argument cannot contain.
    /// </summary>
    private static readonly Regex NameToken = new(
        @"(?<![\w-])name=([A-Za-z0-9_.-]+)",
        RegexOptions.Compiled);

    /// <summary>
    /// The prose form: <c>get-guidance</c> followed by a backtick-quoted name, optionally through "with",
    /// "name" and "set to" (<c>call `get-guidance` with `name` set to `workplaces`</c>). The quoted value is
    /// taken exactly. Whitespace may cross a line break, because a wrapped citation is still a citation.
    /// A quoted <c>name=&lt;id&gt;</c> is left to <see cref="NameToken"/>, so it is not checked twice.
    /// </summary>
    private static readonly Regex ProseGuideName = new(
        @"get-guidance`?(?:\s+with)?(?:\s+`?name`?)?(?:\s+set\s+to)?\s+`([^`\s=]+)`",
        RegexOptions.Compiled);

    /// <summary>
    /// A knowledge route, taken whole up to whitespace, a quote, a bracket, a backtick or a markdown <c>*</c>
    /// (which no stable id contains), so a suffix such as <c>?x</c> or <c>#x</c> stays part of what is compared. One that continues into <c>&lt;</c> or holds
    /// <c>{</c> is a template (<c>docs://knowledge/&lt;library-id&gt;/&lt;item-id&gt;</c>) and names nothing.
    /// <c>docs://help/...</c> is a Clio-owned resource and out of scope.
    /// </summary>
    private static readonly Regex Route = new(
        @"docs://(?:knowledge|mcp)/[^\s`'""()<>\[\]*]*",
        RegexOptions.Compiled);

    /// <summary>A markdown link whose target has no URI scheme and is not a bare in-page anchor.</summary>
    private static readonly Regex RelativeLink = new(
        @"\]\((?![a-zA-Z][a-zA-Z0-9+.-]*:|#)([^)\s]+)\)",
        RegexOptions.Compiled);

    /// <summary>
    /// The top-level <c>id:</c> of a catalog entry, which must be the item's manifest identity. One pair of
    /// surrounding quotes and a trailing <c>#</c> comment are YAML spelling, not part of the id.
    /// </summary>
    private static readonly Regex CatalogId = new(
        @"^id:[ \t]*[""']?([^\s""'#]+)[""']?[ \t]*(?:#.*)?\r?$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Test]
    [Description("Every reference a published body makes - a get-guidance name, a docs://knowledge or docs://mcp route, a catalog entry's id - resolves to something bundle-source.json publishes, exactly the way Clio resolves it, and no body carries a relative link, which the flat release-bundle layout breaks. Failures name the source file, the line and the missing target, all at once.")]
    public void PublishedBodies_ShouldReferenceOnlyPublishedTargets()
    {
        // Arrange
        string repositoryRoot = ProcessGuideSet.FindRepositoryRoot();
        PublishedLibrary library = PublishedLibrary.Read(repositoryRoot);

        // Act
        ScannedReference[] references = [.. library.Bodies
            .SelectMany(body => Scan(body, ProcessGuideSet.Read(repositoryRoot, body.SourcePath), library))];
        string[] dangling = [.. references
            .Where(reference => reference.Missing is not null)
            .Select(reference => reference.ToString())];

        // Assert
        references.Count(reference => reference.Kind == GuideNameKind).Should().BeGreaterThan(100,
            because: "routing and the articles cite well over a hundred guides by name; a scan that found a "
                + "handful would be matching something other than citations and would prove nothing");
        references.Count(reference => reference.Kind == RouteKind).Should().BeGreaterThan(30,
            because: "articles link their supporting references by route; a scan that found none is broken");
        if (dangling.Length > 0)
        {
            Assert.Fail($"{dangling.Length} dangling reference(s) in published bodies:{Environment.NewLine}"
                + string.Join(Environment.NewLine, dangling) + Environment.NewLine
                + "A dangling reference reads as a complete instruction while withholding what it points at: "
                + "get-guidance answers an unknown name with availableGuides only, and an undeclared route "
                + "returns nothing. Point it at a declared itemId/topicId, uri or legacyUris entry.");
        }
    }

    [Test]
    [Description("The scanner itself, on a synthetic body: each reference form is compared exactly the way Clio resolves it, near misses in case, punctuation or suffix are reported, markdown emphasis around a route and YAML quotes or comments around a catalog id are not, every relative link is reported, each with file and line, and shapes that only look like references (other tools' name arguments, placeholders, templates, web links, sentence punctuation) are ignored. The real corpus has no relative link and no broken catalog id today, so without this those rules would be untested.")]
    public void Scan_ShouldReportEachDanglingReferenceWithFileAndLine()
    {
        // Arrange
        PublishedLibrary library = new(
            [
                new PublishedBody("core-rules", "creatio.core-rules", "guidance",
                    "docs://knowledge/lib/core-rules", ["docs://mcp/guides/core-rules"], "guidance/core-rules.md"),
                new PublishedBody("reference.only", "creatio.reference.only", "reference",
                    "docs://knowledge/lib/reference.only", [], "references/only.md"),
                new PublishedBody("example", "example", "reference-example",
                    "docs://knowledge/lib/example", [], "catalog/example.yaml")
            ]);
        string text = string.Join('\n',
            "get-guidance name=core-rules and -> name=creatio.core-rules. (name=core-rules), name=core-rules;",
            "-> name=missing-guide; name=reference.only",
            "call `get-guidance` with `name` set to",
            "`also-missing` before acting",
            "schema-name=Foo event_name=bar name=<env> name=... package-name=core",
            "see docs://knowledge/lib/core-rules and `docs://mcp/guides/core-rules`.",
            "not `docs://mcp/guides/operations/core-rules` nor docs://knowledge/<library-id>/<item-id>",
            "[sibling](core-rules.md) [bad](../missing.md#anchor) [web](https://example.com) [here](#top)",
            "name=core-rules- name=core-rules_ name=Core-rules get-guidance `Core-rules`",
            "docs://knowledge/lib/core-rules?bad and docs://mcp/guides/{family}/{guide}",
            "**docs://knowledge/lib/core-rules** _docs://mcp/guides/core-rules_ docs://mcp/guides/core-rules_ get-guidance `name=core-rules`");
        const string catalog = "schemaVersion: 0\nid: example-renamed\ntitle: x\n";
        const string quotedCatalog = "schemaVersion: 0\nid: \"example\"\n";
        const string commentedCatalog = "schemaVersion: 0\nid: 'example'  # stable\r\ntitle: x\n";
        const string nestedIdOnlyCatalog = "schemaVersion: 0\nprimaryUseCase:\n  id: example\n";

        // Act
        string[] reported = [.. Scan(library.Bodies[0], text, library)
            .Concat(Scan(library.Bodies[2], catalog, library))
            .Concat(Scan(library.Bodies[2], quotedCatalog, library))
            .Concat(Scan(library.Bodies[2], commentedCatalog, library))
            .Concat(Scan(library.Bodies[2], nestedIdOnlyCatalog, library))
            .Where(reference => reference.Missing is not null)
            .Select(reference => reference.ToString())];

        // Assert
        const string NoGuide = "no guidance-role itemId or topicId";
        const string NoRoute = "no declared uri or legacyUris entry";
        const string FlatLayout = "a release bundle stores bodies flat as resources/<itemId>.md; "
            + "use docs://knowledge/<library>/<itemId>";
        reported.Should().Equal(
            $"guidance/core-rules.md:2: name=missing-guide -> {NoGuide}",
            $"guidance/core-rules.md:2: name=reference.only -> {NoGuide}",
            $"guidance/core-rules.md:9: name=core-rules- -> {NoGuide}",
            $"guidance/core-rules.md:9: name=core-rules_ -> {NoGuide}",
            $"guidance/core-rules.md:9: name=Core-rules -> {NoGuide}",
            $"guidance/core-rules.md:4: get-guidance `also-missing` -> {NoGuide}",
            $"guidance/core-rules.md:9: get-guidance `Core-rules` -> {NoGuide}",
            $"guidance/core-rules.md:7: docs://mcp/guides/operations/core-rules -> {NoRoute}",
            $"guidance/core-rules.md:10: docs://knowledge/lib/core-rules?bad -> {NoRoute}",
            $"guidance/core-rules.md:11: docs://mcp/guides/core-rules_ -> {NoRoute}",
            $"guidance/core-rules.md:8: ](core-rules.md) -> {FlatLayout}",
            $"guidance/core-rules.md:8: ](../missing.md#anchor) -> {FlatLayout}",
            "catalog/example.yaml:2: id: example-renamed -> manifest itemId is example",
            "catalog/example.yaml:1: id: -> no top-level id; manifest itemId is example");
    }

    /// <summary>Every reference one body makes, each with what is missing for it, or null when it resolves.</summary>
    internal static IEnumerable<ScannedReference> Scan(PublishedBody body, string text, PublishedLibrary library)
    {
        foreach (Match match in NameToken.Matches(text))
        {
            string name = match.Groups[1].Value.TrimEnd(SentencePunctuation);
            if (name.Length == 0)
            {
                continue;
            }
            yield return new(GuideNameKind, body.SourcePath, LineOf(text, match.Groups[1].Index), $"name={name}",
                library.GuideNames.Contains(name) ? null : "no guidance-role itemId or topicId");
        }
        foreach (Match match in ProseGuideName.Matches(text))
        {
            string name = match.Groups[1].Value;
            yield return new(GuideNameKind, body.SourcePath, LineOf(text, match.Groups[1].Index),
                $"get-guidance `{name}`",
                library.GuideNames.Contains(name) ? null : "no guidance-role itemId or topicId");
        }
        foreach (Match match in Route.Matches(text))
        {
            int end = match.Index + match.Length;
            if ((end < text.Length && text[end] == '<') || match.Value.Contains('{'))
            {
                continue;
            }
            string route = match.Value.TrimEnd(SentencePunctuation);
            if (match.Index > 0 && text[match.Index - 1] == '_' && route.EndsWith('_'))
            {
                // _docs://.../x_ is markdown emphasis; a lone trailing _ is still a near miss and is reported.
                route = route[..^1];
            }
            yield return new(RouteKind, body.SourcePath, LineOf(text, match.Index), route,
                library.Routes.Contains(route) ? null : "no declared uri or legacyUris entry");
        }
        foreach (Match match in RelativeLink.Matches(text))
        {
            yield return new(LinkKind, body.SourcePath, LineOf(text, match.Index), $"]({match.Groups[1].Value})",
                "a release bundle stores bodies flat as resources/<itemId>.md; use docs://knowledge/<library>/<itemId>");
        }
        if (body.Role == "reference-example")
        {
            Match id = CatalogId.Match(text);
            yield return id.Success
                ? new(CatalogIdKind, body.SourcePath, LineOf(text, id.Index), $"id: {id.Groups[1].Value}",
                    id.Groups[1].Value == body.ItemId ? null : $"manifest itemId is {body.ItemId}")
                : new(CatalogIdKind, body.SourcePath, 1, "id:", $"no top-level id; manifest itemId is {body.ItemId}");
        }
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    internal sealed record ScannedReference(string Kind, string SourcePath, int Line, string Text, string? Missing)
    {
        public override string ToString() => $"{SourcePath}:{Line}: {Text} -> {Missing ?? "resolved"}";
    }

    internal sealed record PublishedBody(
        string ItemId,
        string TopicId,
        string Role,
        string Uri,
        string[] LegacyUris,
        string SourcePath);

    /// <summary>The targets a reference can resolve to, all derived from one manifest.</summary>
    internal sealed class PublishedLibrary(PublishedBody[] bodies)
    {
        internal PublishedBody[] Bodies { get; } = bodies;

        /// <summary>What get-guidance accepts as a name: the itemId or topicId of a guidance-role item.</summary>
        internal HashSet<string> GuideNames { get; } = [.. bodies
            .Where(body => body.Role == "guidance")
            .SelectMany(body => new[] { body.ItemId, body.TopicId })];

        internal HashSet<string> Routes { get; } = [.. bodies.SelectMany(body => body.LegacyUris.Prepend(body.Uri))];

        internal static PublishedLibrary Read(string repositoryRoot)
        {
            using JsonDocument manifest = JsonDocument.Parse(
                File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
            return new([.. manifest.RootElement.GetProperty("resources").EnumerateArray()
                .Select(resource => new PublishedBody(
                    resource.GetProperty("itemId").GetString()!,
                    resource.GetProperty("topicId").GetString()!,
                    resource.GetProperty("role").GetString()!,
                    resource.GetProperty("uri").GetString()!,
                    resource.TryGetProperty("legacyUris", out JsonElement legacy)
                        ? [.. legacy.EnumerateArray().Select(uri => uri.GetString()!)]
                        : [],
                    resource.GetProperty("sourcePath").GetString()!))]);
        }
    }
}

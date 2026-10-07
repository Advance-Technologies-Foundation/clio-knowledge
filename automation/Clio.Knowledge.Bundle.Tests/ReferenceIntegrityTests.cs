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
/// Each target set mirrors how Clio resolves that form (clio master 7ab76575b,
/// <c>KnowledgeResolver</c> and <c>MultiSourceKnowledgeResource</c>):
/// <list type="bullet">
/// <item>a <c>get-guidance</c> name matches the <c>itemId</c> or <c>topicId</c> of a <c>guidance</c>-role item
/// only, so naming a supporting reference or a reference example is dangling even though the id exists;</item>
/// <item>a <c>docs://knowledge/...</c> or <c>docs://mcp/...</c> route must equal a declared <c>uri</c> or
/// <c>legacyUris</c> entry exactly - the <c>docs://mcp/guides/{family}/{guide}</c> template forwards to that
/// exact lookup, it does not search by the last segment.</item>
/// </list>
/// </summary>
[TestFixture]
public sealed class ReferenceIntegrityTests
{
    internal const string GuideNameKind = "guide name";
    internal const string RouteKind = "docs route";
    internal const string LinkKind = "relative link";
    internal const string CatalogIdKind = "catalog id";

    /// <summary>
    /// <c>name=&lt;id&gt;</c> as routing and articles write it. The lookbehind skips other tools' arguments
    /// (<c>schema-name=</c>, <c>event_name=</c>); the lookahead refuses a token the pattern would otherwise
    /// cut short, so <c>name=crt.Button</c> is not read as a citation of <c>crt</c>. Placeholders such as
    /// <c>name=&lt;env&gt;</c> never start with the allowed first character.
    /// </summary>
    private static readonly Regex NameToken = new(
        @"(?<![\w-])name=([a-z0-9][a-z0-9.-]*[a-z0-9])(?![\w.-]*\w)",
        RegexOptions.Compiled);

    /// <summary>
    /// The prose form: <c>get-guidance</c> followed by a backtick-quoted name, optionally through "with",
    /// "name" and "set to" (<c>call `get-guidance` with `name` set to `workplaces`</c>). Whitespace may
    /// cross a line break, because a wrapped citation is still a citation.
    /// </summary>
    private static readonly Regex ProseGuideName = new(
        @"get-guidance`?(?:\s+with)?(?:\s+`?name`?)?(?:\s+set\s+to)?\s+`([a-z0-9][a-z0-9.-]*[a-z0-9])`",
        RegexOptions.Compiled);

    /// <summary>
    /// A knowledge route. One followed by <c>&lt;</c> or <c>{</c> is a template
    /// (<c>docs://knowledge/&lt;library-id&gt;/&lt;item-id&gt;</c>) and names nothing; the lookahead makes the
    /// whole match fail rather than stop early. <c>docs://help/...</c> is a Clio-owned resource and out of scope.
    /// </summary>
    private static readonly Regex Route = new(
        @"docs://(?:knowledge|mcp)/[A-Za-z0-9._~%/-]*(?![<{A-Za-z0-9._~%/-])",
        RegexOptions.Compiled);

    /// <summary>A markdown link whose target has no URI scheme and is not a bare in-page anchor.</summary>
    private static readonly Regex RelativeLink = new(
        @"\]\((?![a-zA-Z][a-zA-Z0-9+.-]*:|#)([^)\s#]+)(#[^)\s]*)?\)",
        RegexOptions.Compiled);

    /// <summary>The top-level <c>id:</c> of a catalog entry, which must be the item's manifest identity.</summary>
    private static readonly Regex CatalogId = new(
        @"^id:[ \t]*(\S+)[ \t\r]*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    [Test]
    [Description("Every reference a published body makes - a get-guidance name, a docs://knowledge or docs://mcp route, a relative markdown link, a catalog entry's id - resolves to something bundle-source.json publishes, the way Clio resolves it. Failures name the source file, the line and the missing target, all at once.")]
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
                + "returns nothing. Point it at a declared itemId/topicId, uri or legacyUris entry, or a "
                + "published file.");
        }
    }

    [Test]
    [Description("The scanner itself, on a synthetic body: each reference form is resolved the way Clio resolves it, every dangling one is reported with file and line, and shapes that only look like references (other tools' name arguments, placeholders, templates, web links) are ignored. The real corpus has no relative link and no broken catalog id today, so without this the two rules would be untested.")]
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
            "get-guidance name=core-rules and -> name=creatio.core-rules.",
            "-> name=missing-guide; name=reference.only",
            "call `get-guidance` with `name` set to",
            "`also-missing` before acting",
            "schema-name=Foo event_name=bar name=<env> name=crt.Button package-name=core",
            "see docs://knowledge/lib/core-rules and `docs://mcp/guides/core-rules`.",
            "not `docs://mcp/guides/operations/core-rules` nor docs://knowledge/<library-id>/<item-id>",
            "[ok](core-rules.md) [bad](../missing.md#anchor) [web](https://example.com) [here](#top)");
        const string catalog = "schemaVersion: 0\nid: example-renamed\ntitle: x\n";

        // Act
        string[] reported = [.. Scan(library.Bodies[0], text, library)
            .Concat(Scan(library.Bodies[2], catalog, library))
            .Where(reference => reference.Missing is not null)
            .Select(reference => reference.ToString())];

        // Assert
        reported.Should().Equal(
            "guidance/core-rules.md:2: name=missing-guide -> no guidance-role itemId or topicId",
            "guidance/core-rules.md:2: name=reference.only -> no guidance-role itemId or topicId",
            "guidance/core-rules.md:4: get-guidance `also-missing` -> no guidance-role itemId or topicId",
            "guidance/core-rules.md:7: docs://mcp/guides/operations/core-rules -> no declared uri or legacyUris entry",
            "guidance/core-rules.md:8: ](../missing.md#anchor) -> no published file at missing.md",
            "catalog/example.yaml:2: id: example-renamed -> manifest itemId is example");
    }

    /// <summary>Every reference one body makes, each with what is missing for it, or null when it resolves.</summary>
    internal static IEnumerable<ScannedReference> Scan(PublishedBody body, string text, PublishedLibrary library)
    {
        foreach (Match match in NameToken.Matches(text))
        {
            string name = match.Groups[1].Value;
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
            // A sentence may end on a route; the full stop is punctuation, not part of the identity.
            string route = match.Value.TrimEnd('.');
            yield return new(RouteKind, body.SourcePath, LineOf(text, match.Index), route,
                library.Routes.Contains(route) ? null : "no declared uri or legacyUris entry");
        }
        foreach (Match match in RelativeLink.Matches(text))
        {
            string? target = ResolveRelative(body.SourcePath, match.Groups[1].Value);
            yield return new(LinkKind, body.SourcePath, LineOf(text, match.Index),
                $"]({match.Groups[1].Value}{match.Groups[2].Value})",
                target is not null && library.SourcePaths.Contains(target)
                    ? null
                    : $"no published file at {target ?? "a path outside the repository"}");
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

    /// <summary>
    /// A link target resolved against the linking file's folder, in repository-relative form, or null when
    /// it climbs out of the repository. Pure string work, so the rule is testable without a file system.
    /// </summary>
    private static string? ResolveRelative(string sourcePath, string target)
    {
        List<string> segments = target.StartsWith('/') ? [] : [.. sourcePath.Split('/')[..^1]];
        foreach (string segment in target.Split('/'))
        {
            if (segment is "" or ".")
            {
                continue;
            }
            if (segment != "..")
            {
                segments.Add(segment);
                continue;
            }
            if (segments.Count == 0)
            {
                return null;
            }
            segments.RemoveAt(segments.Count - 1);
        }
        return string.Join('/', segments);
    }

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

        internal HashSet<string> SourcePaths { get; } = [.. bodies.Select(body => body.SourcePath)];

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

using System.Text.RegularExpressions;

namespace Clio.Knowledge.Bundle.Tests;

internal static partial class ReferenceLinkMigration
{
    private const string CanonicalPrefix = "docs://knowledge/com.creatio.clio/reference.";

    internal static string NormalizeToFrozenLinkText(string text) => CanonicalReferencePattern().Replace(
        text,
        match => FrozenLink(match.Groups["family"].Value, match.Groups["name"].Value));

    private static string FrozenLink(string family, string name) => family is
        "configuration-webservice" or "configuration-webservice-tests"
            ? $"docs://mcp/references/{family}/{name}"
            : $"references/{name}.md";

    [GeneratedRegex(
        CanonicalPrefix + "(?<family>[a-z0-9-]+)\\.(?<name>[a-z0-9-]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalReferencePattern();
}

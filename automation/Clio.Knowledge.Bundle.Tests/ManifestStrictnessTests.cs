using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// The manifest is parsed by clio with a reader that REJECTS duplicate JSON properties; every reader this test
/// project uses accepts them (the last one wins). So a manifest that no test here could fault was refused by
/// <c>install-knowledge</c> with "duplicate JSON property 'itemId'" — a merge had glued two resource entries into
/// one object, and 141 green tests said nothing. This guard reads the manifest the way clio does.
/// </summary>
[TestFixture]
public sealed class ManifestStrictnessTests
{
    [Test]
    [Description("bundle-source.json carries no object with a duplicate property. System.Text.Json's JsonDocument tolerates duplicates and every other test parses through it, so without this guard a glued object reaches clio, whose manifest reader refuses it — and the published bundle installs for nobody.")]
    public void BundleSource_ShouldContainNoDuplicateProperties()
    {
        // Arrange
        byte[] manifest = File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "bundle-source.json"));

        // Act
        List<string> duplicates = FindDuplicateProperties(manifest);

        // Assert
        duplicates.Should().BeEmpty(
            because: "clio's manifest reader rejects a duplicate property outright, so one glued object makes the "
                + "whole bundle uninstallable while every JsonDocument-based test here still passes");
    }

    /// <summary>
    /// Walks the tokens with <see cref="Utf8JsonReader"/>, keeping the property names seen in each open object,
    /// and reports every repeat as "&lt;path&gt;: &lt;name&gt;" so the offending object is nameable from the failure.
    /// </summary>
    private static List<string> FindDuplicateProperties(byte[] json)
    {
        var duplicates = new List<string>();
        var seen = new Stack<HashSet<string>>();
        var path = new Stack<string>();
        var reader = new Utf8JsonReader(json, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
        string? pendingName = null;
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    seen.Push(new HashSet<string>(StringComparer.Ordinal));
                    path.Push(pendingName ?? "[]");
                    pendingName = null;
                    break;
                case JsonTokenType.EndObject:
                    seen.Pop();
                    path.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    string name = reader.GetString()!;
                    if (!seen.Peek().Add(name))
                    {
                        duplicates.Add($"{string.Join("/", path.Reverse())}: '{name}'");
                    }
                    pendingName = name;
                    break;
                default:
                    pendingName = null;
                    break;
            }
        }
        return duplicates;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new InvalidOperationException("bundle-source.json not found above the test output");
    }
}

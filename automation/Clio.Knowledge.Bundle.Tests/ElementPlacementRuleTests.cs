using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the ENG-94937 payload: the <c>ELEMENT PLACEMENT IS AUTHORITATIVE</c> rule and the
/// intra-guide citations that point at it. The content digest cannot guard a rule's wording — it is
/// re-recorded on every edit — so a rename or deletion of the rule, or a dangling
/// "see … in HARD MOBILE RULES" citation, would otherwise ship green.
/// </summary>
[TestFixture]
public sealed class ElementPlacementRuleTests
{
    private const string RuleName = "ELEMENT PLACEMENT IS AUTHORITATIVE";
    private const string OwnerGuide = "guidance/mcp/guides/platform/mobile/web-to-mobile-conversion.md";

    // Entry-point guides that must carry a reciprocal pointer to the rule (AGENTS.md: one authoritative
    // owner per rule, referenced from elsewhere — get-component-info stays authoritative for SHAPE only).
    private static readonly string[] ReciprocalPointerGuides =
    [
        "guidance/mcp/guides/pages/modification/containers.md",
        "guidance/mcp/guides/platform/mobile/page-modification.md"
    ];

    [Test]
    [Description("The ELEMENT PLACEMENT IS AUTHORITATIVE rule still exists as a HARD MOBILE RULES bullet in its owner guide.")]
    public void Rule_ShouldExistAsBulletInOwnerGuide()
    {
        string owner = ReadGuide(OwnerGuide);

        owner.Should().MatchRegex($@"(?m)^- {Regex.Escape(RuleName)}\b",
            because: "the rule is the whole payload of ENG-94937; renaming or deleting it must fail a test "
                + "rather than silently re-record the content digest");
        owner.Should().Contain("HARD MOBILE RULES",
            because: "the rule lives in the HARD MOBILE RULES section its citations point at");
    }

    [Test]
    [Description("Every 'see <RULE NAME> in HARD MOBILE RULES' citation resolves to a bullet with that exact name in the owner guide.")]
    public void RuleCitations_ShouldResolveToAnExistingBullet()
    {
        string ownerNormalized = Normalize(ReadGuide(OwnerGuide));

        string[] unresolved = Regex.Matches(ownerNormalized, @"see ([A-Z][A-Z0-9 ]+?) in HARD MOBILE RULES")
            .Select(match => match.Groups[1].Value.Trim())
            .Where(citedRule => !ownerNormalized.Contains("- " + citedRule))
            .Distinct()
            .ToArray();

        unresolved.Should().BeEmpty(
            because: "a citation naming a rule that no bullet defines is a dangling pointer");
    }

    [Test]
    [Description("The reciprocal pointers in the container and mobile page-modification guides still name the rule, so catalog-vs-placement authority does not silently split.")]
    public void ReciprocalPointers_ShouldNameTheRule()
    {
        string[] missing = ReciprocalPointerGuides
            .Where(guide => !ReadGuide(guide).Contains(RuleName))
            .ToArray();

        missing.Should().BeEmpty(
            because: "each entry-point guide must point per-page placement back at the one owner rule "
                + "(AGENTS.md: absence of contradictory sources of truth)");
    }

    private static string ReadGuide(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Guards the invariant a consumer relies on to accept a candidate: one generation sequence, one set
/// of bytes.
/// </summary>
/// <remarks>
/// Clio refuses a candidate whose sequence equals an already accepted one while its content digest
/// differs, and the refusal keeps the previously installed generation active — so the edit never
/// reaches anyone who already synced the earlier bytes, on that machine, until the sequence moves
/// forward. The high-water mark holding that decision is on disk and survives a restart.
///
/// The sequence is therefore derived from <c>libraryVersion</c> rather than authored: the release tag
/// must equal <c>libraryVersion</c>, and the release workflow refuses to overwrite a published tag.
/// Different content under an already accepted sequence has no path to a consumer. These tests cover
/// the derivation and the repository's own conformance to it; nothing here records a content digest,
/// so ordinary guidance edits do not touch this file.
/// </remarks>
[TestFixture]
public sealed class GenerationSequenceTests
{
    // The last generation published from a hand-maintained `sequence` field, shipped as 1.13.8. It is
    // a closed historical fact, not a value to keep current: every derived sequence is far above it,
    // and the assertion below only proves the switch to derivation moved the sequence forward rather
    // than backwards for consumers that already accepted it.
    private const ulong LastAuthoredSequence = 23;

    [Test]
    [Description("Verifies that the derived sequence increases strictly with the publisher version label, which is what lets a consumer order generations.")]
    public void DeriveSequence_ShouldIncreaseStrictly_WithVersionOrder()
    {
        // Arrange
        string[] ascending =
        [
            "0.0.1",
            "0.9.9",
            "1",
            "1.13",
            "1.13.8",
            "1.13.9",
            "1.13.9.1",
            "1.13.10",
            "1.14",
            "2.0.0",
            "2026.07.19.1",
            "2026.07.19.2",
            "2026.08.10"
        ];

        // Act
        ulong[] sequences = ascending.Select(BundleBuilder.DeriveSequence).ToArray();

        // Assert
        sequences.Should().BeInAscendingOrder(
            because: "a consumer refuses a sequence that moves backwards, so the derivation must "
                + "preserve the version order for every label shape this repository accepts")
            .And.OnlyHaveUniqueItems(
                because: "two versions sharing a sequence would make the second publication look like "
                    + "edited content under an accepted generation, which Clio rejects");
    }

    [TestCase("1.13.9", 1_013_009_000UL)]
    [TestCase("1.13.9.1", 1_013_009_001UL)]
    [TestCase("1.13", 1_013_000_000UL)]
    [TestCase("1", 1_000_000_000UL)]
    [TestCase("2026.07.19.1", 2_026_007_019_001UL)]
    [Description("Verifies that each version component occupies its own fixed decimal slot, so an omitted trailing component reads as zero.")]
    public void DeriveSequence_ShouldPlaceEachComponent_InItsOwnSlot(string libraryVersion, ulong expected)
    {
        // Act
        ulong sequence = BundleBuilder.DeriveSequence(libraryVersion);

        // Assert
        sequence.Should().Be(expected,
            because: "the slot layout is the published contract behind the ordering, so a change to it "
                + "is a change to how every future generation is numbered");
    }

    [TestCase("1.13.9-beta", TestName = "DeriveSequence_ShouldReject_PrereleaseSuffix")]
    [TestCase("1.13.9+build.4", TestName = "DeriveSequence_ShouldReject_BuildMetadata")]
    [TestCase("1.13.9.1.2", TestName = "DeriveSequence_ShouldReject_FifthComponent")]
    [TestCase("1.1000.0", TestName = "DeriveSequence_ShouldReject_TrailingComponentWiderThanItsSlot")]
    [TestCase("10000000.1.1", TestName = "DeriveSequence_ShouldReject_LeadingComponentWiderThanItsSlot")]
    [TestCase("v1.13.9", TestName = "DeriveSequence_ShouldReject_TagPrefix")]
    [TestCase("1..9", TestName = "DeriveSequence_ShouldReject_EmptyComponent")]
    [TestCase("0", TestName = "DeriveSequence_ShouldReject_LabelDerivingZero")]
    [TestCase("0.0.0.0", TestName = "DeriveSequence_ShouldReject_PaddedLabelDerivingZero")]
    [Description("Verifies that a version label the derivation cannot order is refused at build time instead of shipping an unorderable generation.")]
    public void DeriveSequence_ShouldReject_UnorderableVersionLabel(string libraryVersion)
    {
        // Act
        Action act = () => BundleBuilder.DeriveSequence(libraryVersion);

        // Assert
        act.Should().Throw<InvalidDataException>(
            because: "an unorderable or zero sequence is rejected by the consumer as an invalid "
                + "generation, so the build must fail while the mistake is still cheap. A component "
                + "wider than its slot is refused for the same reason: its digits would carry into the "
                + "neighbouring slot and the derived sequence would stop rising with the version");
    }

    [Test]
    [Description("Verifies that the repository declares no sequence field, so the number cannot drift back to being hand-maintained.")]
    public void RepositoryManifest_ShouldNotDeclare_AnAuthoredSequence()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();

        // Act
        using JsonDocument source = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));

        // Assert
        source.RootElement.TryGetProperty("sequence", out _).Should().BeFalse(
            because: "a hand-maintained sequence is the mistake this design removes: it would win over "
                + "the derivation for anyone reading the manifest, and the repository schema rejects "
                + "the unknown property anyway");
    }

    [Test]
    [Description("Verifies that the version the repository publishes derives a sequence above the last hand-maintained one, so the switch to derivation moves consumers forward.")]
    public void RepositoryVersion_ShouldDeriveSequence_AboveTheLastAuthoredGeneration()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        using JsonDocument source = JsonDocument.Parse(
            File.ReadAllBytes(Path.Combine(repositoryRoot, "bundle-source.json")));
        string libraryVersion = source.RootElement.GetProperty("libraryVersion").GetString()!;

        // Act
        ulong sequence = BundleBuilder.DeriveSequence(libraryVersion);

        // Assert
        sequence.Should().BeGreaterThan(LastAuthoredSequence,
            because: "a consumer that already accepted sequence 23 refuses anything below it, and would "
                + "keep serving the older generation instead of this one");
    }

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

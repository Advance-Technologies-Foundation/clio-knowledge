using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class BundleBuilderTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "clio-knowledge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Test]
    [Description("Builds a signed v1 generation with canonical library/item routes and deterministic resource bytes.")]
    public void Build_ShouldProduceCanonicalSignedBundle_WhenSourceIsValid()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "b.md"), "second\r\nline\r\n", new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(_directory, "a.md"), "first\nline\n", Encoding.UTF8);
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-b", "topicId": "creatio.guide-b", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-b", "legacyUris": ["docs://mcp/guides/b"], "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" },
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "legacyUris": ["docs://mcp/guides/a"], "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""",
            "[\"guide-b\", \"guide-a\"]",
            "[\"docs://knowledge/com.example.knowledge/guide-b\", \"docs://knowledge/com.example.knowledge/guide-a\"]");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        BundleBuildResult result = new BundleBuilder().Build(sourcePath, outputPath, key, Publication());

        // Assert
        result.Manifest.Resources.Select(resource => resource.Path).Should().Equal(
            ["resources/a.md", "resources/b.md"],
            because: "resource ordering is part of the canonical bundle contract");
        result.Manifest.LibraryId.Should().Be("com.example.knowledge",
            because: "the signed generation identity must be scoped to its stable publisher library");
        result.Manifest.Resources.Select(resource => resource.Uri).Should().Equal(
            [
                "docs://knowledge/com.example.knowledge/guide-a",
                "docs://knowledge/com.example.knowledge/guide-b"
            ], because: "every exact route must be derived from library and item identity");
        result.Manifest.Resources.Single(resource => resource.ItemId == "guide-a").LegacyUris.Should()
            .ContainSingle(because: "migrated content keeps one transitional v0 route as signed metadata")
            .Which.Should().Be("docs://mcp/guides/a",
                because: "migrated content keeps its transitional v0 route as signed metadata");
        key.VerifyData(result.ManifestBytes, result.SignatureBytes, HashAlgorithmName.SHA256).Should().BeTrue(
            because: "the detached signature must cover the exact canonical manifest bytes");
        using ZipArchive archive = ZipFile.OpenRead(outputPath);
        ReadEntry(archive, "resources/b.md").Should().Equal(
            Encoding.UTF8.GetBytes("second\nline\n"),
            because: "text payloads must be UTF-8 without BOM and use LF newlines");
    }

    [Test]
    [Description("Produces identical canonical manifest bytes when source arrays and aliases are reordered.")]
    public void Build_ShouldProduceSameManifestBytes_WhenDeclarationsAreReordered()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a\n");
        File.WriteAllText(Path.Combine(_directory, "b.md"), "b\n");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-b", "topicId": "creatio.guide-b", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-b", "legacyUris": ["docs://legacy/b-two", "docs://legacy/b-one"], "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" },
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""", "[\"guide-b\", \"guide-a\"]",
            "[\"docs://knowledge/com.example.knowledge/guide-b\", \"docs://knowledge/com.example.knowledge/guide-a\"]");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        BundleBuilder builder = new();

        // Act
        BundleBuildResult first = builder.Build(sourcePath, Path.Combine(_directory, "first.zip"), key, Publication());
        sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" },
			  { "itemId": "guide-b", "topicId": "creatio.guide-b", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-b", "legacyUris": ["docs://legacy/b-one", "docs://legacy/b-two"], "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" }
			]
			""", "[\"guide-a\", \"guide-b\"]",
            "[\"docs://knowledge/com.example.knowledge/guide-a\", \"docs://knowledge/com.example.knowledge/guide-b\"]");
        BundleBuildResult second = builder.Build(sourcePath, Path.Combine(_directory, "second.zip"), key, Publication());

        // Assert
        second.ManifestBytes.Should().Equal(first.ManifestBytes,
            because: "source declaration order must not change the signed generation manifest");
    }

    [Test]
    [Description("Rejects a legacy alias claimed by more than one item in the same library.")]
    public void Build_ShouldRejectDuplicateLegacyUris_WhenSourceIsAmbiguous()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        File.WriteAllText(Path.Combine(_directory, "b.md"), "b");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "legacyUris": ["docs://mcp/guides/same"], "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" },
			  { "itemId": "guide-b", "topicId": "creatio.guide-b", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-b", "legacyUris": ["docs://mcp/guides/same"], "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" }
			]
			""", "[\"guide-a\", \"guide-b\"]",
            "[\"docs://knowledge/com.example.knowledge/guide-a\", \"docs://knowledge/com.example.knowledge/guide-b\"]");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*resource route*",
                because: "ambiguous stable resource identities must fail the whole bundle build");
    }

    [Test]
    [Description("Rejects an explicit item URI that is not exactly derived from its library and item identity.")]
    public void Build_ShouldRejectUri_WhenItDoesNotMatchCanonicalIdentity()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.other.library/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""", resourceUrisJson: "[\"docs://knowledge/com.other.library/guide-a\"]");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*must be exactly 'docs://knowledge/com.example.knowledge/guide-a'*",
                because: "exact lookup must never fall through or resolve through publisher-controlled aliases");
    }

    [Test]
    [Description("Rejects two items from one library that compete for the same logical topic and role.")]
    public void Build_ShouldRejectDuplicateTopicRole_WhenLibraryResolutionWouldBeAmbiguous()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        File.WriteAllText(Path.Combine(_directory, "b.md"), "b");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.shared", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" },
			  { "itemId": "guide-b", "topicId": "creatio.shared", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-b", "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" }
			]
			""", "[\"guide-a\", \"guide-b\"]",
            "[\"docs://knowledge/com.example.knowledge/guide-a\", \"docs://knowledge/com.example.knowledge/guide-b\"]");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*topic and role pair*",
                because: "logical selection must yield at most one eligible item per role from a library");
    }

    [TestCase("guidance", "catalog/sample.md")]
    [TestCase("advisory", "guidance/sample.md")]
    [TestCase("capability", "advisories/sample.md")]
    [TestCase("reference-example", "capabilities/sample.md")]
    [Description("Rejects a resource whose source directory does not match its semantic role.")]
    public void Build_ShouldRejectSourcePath_WhenRoleUsesAnotherCanonicalRoot(string role, string sourcePath)
    {
        // Arrange
        string manifestPath = WriteSource($$"""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "{{role}}", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "{{sourcePath}}", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(
            manifestPath,
            Path.Combine(_directory, "bundle.zip"),
            key,
            Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*must use a safe source path under*",
                because: "a role must only publish content from its canonical repository directory");
    }

    [Test]
    [Description("Rejects a source path longer than the public repository schema allows.")]
    public void Build_ShouldRejectSourcePath_WhenPathExceedsSchemaLengthLimit()
    {
        // Arrange
        string overlongPath = "guidance/" + new string('a', 504);
        string manifestPath = WriteSource($$"""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "{{overlongPath}}", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(
            manifestPath,
            Path.Combine(_directory, "bundle.zip"),
            key,
            Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*must use a safe source path under*",
                because: "the producer must enforce the same 512-character bound as its public schema");
    }

    [Test]
    [Description("Rejects legacy v0 source descriptors from the canonical v1 builder.")]
    public void Build_ShouldRejectLegacyContract_WhenCanonicalIdentityIsMissing()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        string legacyJson = File.ReadAllText(sourcePath)
            .Replace("\"contractVersion\": \"1.0.0\"", "\"contractVersion\": \"0.1.0\"", StringComparison.Ordinal);
        File.WriteAllText(sourcePath, legacyJson);
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*requires contract and schema version 1.0.0*",
                because: "new publications must carry explicit multi-source identity instead of an implicit library");
    }

    [Test]
    [Description("Rejects archive traversal paths before writing bundle entries.")]
    public void Build_ShouldRejectTraversal_WhenBundlePathEscapesResources()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/../a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*safe resources/ path*",
                because: "published archives must not contain traversal paths");
    }

    [Test]
    [Description("Rejects source text that cannot be decoded as strict UTF-8.")]
    public void Build_ShouldRejectMalformedUtf8_WhenResourceCannotBeDecodedLosslessly()
    {
        // Arrange
        File.WriteAllBytes(Path.Combine(_directory, "a.md"), [0xC3, 0x28]);
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*valid UTF-8*",
                because: "malformed source bytes must never be normalized and signed as replacement characters");
    }

    [Test]
    [Description("Rejects wildcard compatibility ranges so producer and consumer selection remains deterministic.")]
    public void Build_ShouldRejectWildcardCompatibilityVersion_WhenSelectionWouldBeAmbiguous()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""", clioMax: "8.1.x");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*exact MAJOR.MINOR.PATCH*",
                because: "producer and consumer must share one deterministic compatibility comparison grammar");
    }

    [Test]
    [Description("Accepts a zero-length item payload while retaining a non-empty resource inventory.")]
    public void Build_ShouldAcceptZeroLengthResource_WhenBundleIsOtherwiseValid()
    {
        // Arrange
        File.WriteAllBytes(Path.Combine(_directory, "a.md"), []);
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        BundleBuildResult result = new BundleBuilder().Build(sourcePath, outputPath, key, Publication());

        // Assert
        result.Manifest.Resources.Single().Length.Should().Be(0,
            because: "the valid-empty fixture means one zero-length resource, not a manifest with no resources");
    }

    [Test]
    [Description("Rejects abbreviated source revisions because signed provenance must identify one complete Git object.")]
    public void Build_ShouldRejectAbbreviatedCommit_WhenProvenanceIsNotImmutable()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(
            sourcePath,
            Path.Combine(_directory, "bundle.zip"),
            key,
            Publication("0123456789abcdef"));

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*complete SHA-1 or SHA-256*",
                because: "abbreviated revisions can become ambiguous as a repository grows");
    }

    [Test]
    [Description("Accepts a complete SHA-256 source revision for repositories that use the Git SHA-256 object format.")]
    public void Build_ShouldAcceptSha256Commit_WhenProvenanceUsesCompleteObjectId()
    {
        // Arrange
        const string commit = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        BundleBuildResult result = new BundleBuilder().Build(
            sourcePath,
            Path.Combine(_directory, "bundle.zip"),
            key,
            Publication(commit));

        // Assert
        result.Manifest.Source.Commit.Should().Be(commit,
            because: "the producer and consumer both permit complete SHA-256 Git object identities");
    }

    [Test]
    [Description("Rejects one source item larger than the consumer limit without truncating an existing bundle.")]
    public void Build_ShouldPreserveExistingOutput_WhenResourceExceedsItemLimit()
    {
        // Arrange
        File.WriteAllBytes(Path.Combine(_directory, "a.md"), new byte[4 * 1024 * 1024 + 1]);
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        byte[] existing = "existing-signed-bundle"u8.ToArray();
        File.WriteAllBytes(outputPath, existing);
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, outputPath, key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*item size limit*",
                because: "producer output must remain inside the consumer's four-MiB item bound");
        File.ReadAllBytes(outputPath).Should().Equal(existing,
            because: "a rejected publication must preserve the last known-good destination bytes");
        Directory.EnumerateFiles(_directory, ".bundle.zip.*.tmp").Should().BeEmpty(
            because: "failed publication must not leave sibling temporary artifacts");
    }

    [Test]
    [Description("Rejects aggregate resource bytes beyond the consumer limit even when every individual item is valid.")]
    public void Build_ShouldRejectResources_WhenAggregateSizeExceedsConsumerLimit()
    {
        // Arrange
        const int resourceCount = 9;
        File.WriteAllBytes(Path.Combine(_directory, "shared.md"), new byte[4 * 1024 * 1024]);
        (string resourcesJson, string itemIdsJson, string resourceUrisJson) = CreateResourceDeclarations(
            resourceCount,
            "shared.md");
        string sourcePath = WriteSource(resourcesJson, itemIdsJson, resourceUrisJson);
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*total size limit*",
                because: "nine valid four-MiB items exceed the consumer's 32-MiB aggregate bound");
    }

    [Test]
    [Description("Rejects a resource inventory that would exceed the consumer's total archive-entry bound.")]
    public void Build_ShouldRejectResources_WhenArchiveEntryCountExceedsConsumerLimit()
    {
        // Arrange
        const int resourceCount = 1023;
        (string resourcesJson, string itemIdsJson, string resourceUrisJson) = CreateResourceDeclarations(
            resourceCount,
            "shared.md");
        string sourcePath = WriteSource(resourcesJson, itemIdsJson, resourceUrisJson);
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key, Publication());

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*1024 entries*",
                because: "manifest and signature consume two entries before resource payloads are added");
    }

    [Test]
    [Description("Removes the sibling temporary archive when atomic destination publication cannot complete.")]
    public void Build_ShouldCleanTemporaryArchive_WhenDestinationCannotBePublished()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        Directory.CreateDirectory(outputPath);
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, outputPath, key, Publication());

        // Assert
        act.Should().Throw<IOException>(because: "a file cannot atomically replace an existing directory");
        Directory.Exists(outputPath).Should().BeTrue(
            because: "failed publication must not alter the existing destination object");
        Directory.EnumerateFiles(_directory, ".bundle.zip.*.tmp").Should().BeEmpty(
            because: "the builder owns and must clean every sibling temporary archive it creates");
    }

    [Test]
    [Description("Atomically replaces an existing destination only after the complete candidate archive is ready.")]
    public void Build_ShouldReplaceExistingOutput_WhenPublicationSucceeds()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "replacement");
        string sourcePath = WriteSource("""
			[
			  { "itemId": "guide-a", "topicId": "creatio.guide-a", "role": "guidance", "uri": "docs://knowledge/com.example.knowledge/guide-a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        byte[] previous = "previous-signed-bundle"u8.ToArray();
        File.WriteAllBytes(outputPath, previous);
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        BundleBuildResult result = new BundleBuilder().Build(sourcePath, outputPath, key, Publication());

        // Assert
        File.ReadAllBytes(outputPath).Should().NotEqual(previous,
            because: "successful atomic publication must replace the previous destination generation");
        using FileStream artifact = File.OpenRead(outputPath);
        Convert.ToHexStringLower(SHA256.HashData(artifact)).Should().Be(result.ArtifactSha256,
            because: "the reported digest must describe the bytes atomically published at the destination");
        Directory.EnumerateFiles(_directory, ".bundle.zip.*.tmp").Should().BeEmpty(
            because: "successful publication moves rather than retains the sibling temporary archive");
    }

    private string WriteSource(
        string resourcesJson,
        string itemIdsJson = "[\"guide-a\"]",
        string resourceUrisJson = "[\"docs://knowledge/com.example.knowledge/guide-a\"]",
        string clioMax = "8.1.999")
    {
        string guidanceDirectory = Path.Combine(_directory, "guidance");
        Directory.CreateDirectory(guidanceDirectory);
        foreach (string file in Directory.EnumerateFiles(_directory))
        {
            File.Copy(file, Path.Combine(guidanceDirectory, Path.GetFileName(file)), overwrite: true);
        }
        resourcesJson = resourcesJson
            .Replace("\"sourcePath\": \"a.md\"", "\"sourcePath\": \"guidance/a.md\"", StringComparison.Ordinal)
            .Replace("\"sourcePath\": \"b.md\"", "\"sourcePath\": \"guidance/b.md\"", StringComparison.Ordinal);
        string sourcePath = Path.Combine(_directory, "bundle-source.json");
        File.WriteAllText(sourcePath, $$"""
			{
			  "$schema": "./schemas/v1/knowledge-repository.schema.json",
			  "contractVersion": "1.0.0",
			  "bundleSchemaVersion": "1.0.0",
			  "libraryId": "com.example.knowledge",
			  "libraryVersion": "2026.07.19.1",
			  "sequence": 1,
			  "compatibility": {
			    "clio": { "min": "8.1.0", "max": "{{clioMax}}" },
			    "mcpToolContract": { "min": "1.0.0", "max": "1.999.999" }
			  },
			  "requirements": {
			    "tools": ["get-guidance"],
			    "itemIds": {{itemIdsJson}},
			    "resourceUris": {{resourceUrisJson}}
			  },
			  "resources": {{resourcesJson}}
			}
			""");
        return sourcePath;
    }

    private static BundlePublicationMetadata Publication(
        string commit = "0123456789abcdef0123456789abcdef01234567") => new(
        new SourceProvenance("example/repo", commit),
        new SignatureDescriptor("ECDSA-P256-SHA256", "p1-test"));

    private static (string ResourcesJson, string ItemIdsJson, string ResourceUrisJson)
        CreateResourceDeclarations(int count, string sourcePath)
    {
        sourcePath = sourcePath.Contains('/')
            ? sourcePath
            : $"guidance/{sourcePath}";
        var resources = Enumerable.Range(0, count).Select(index => new
        {
            itemId = $"guide-{index}",
            topicId = $"creatio.guide-{index}",
            role = "guidance",
            uri = $"docs://knowledge/com.example.knowledge/guide-{index}",
            sourcePath,
            bundlePath = $"resources/guide-{index}.md",
            mediaType = "text/markdown"
        }).ToArray();
        return (
            JsonSerializer.Serialize(resources),
            JsonSerializer.Serialize(resources.Select(resource => resource.itemId)),
            JsonSerializer.Serialize(resources.Select(resource => resource.uri)));
    }

    private static byte[] ReadEntry(ZipArchive archive, string path)
    {
        using Stream stream = archive.GetEntry(path)!.Open();
        using MemoryStream result = new();
        stream.CopyTo(result);
        return result.ToArray();
    }
}

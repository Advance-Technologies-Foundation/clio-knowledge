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
    public void Build_ShouldProduceCanonicalSignedBundle_WhenSourceIsValid()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "b.md"), "second\r\nline\r\n", new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(_directory, "a.md"), "first\nline\n", Encoding.UTF8);
        string sourcePath = WriteSource("""
			[
			  { "id": "guide-b", "uri": "docs://mcp/guides/b", "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" },
			  { "id": "guide-a", "uri": "docs://mcp/guides/a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""",
            "[\"guide-b\", \"guide-a\"]",
            "[\"docs://mcp/guides/b\", \"docs://mcp/guides/a\"]");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        BundleBuildResult result = new BundleBuilder().Build(sourcePath, outputPath, key);

        // Assert
        result.Manifest.Resources.Select(resource => resource.Path).Should().Equal(
            ["resources/a.md", "resources/b.md"],
            because: "resource ordering is part of the canonical bundle contract");
        key.VerifyData(result.ManifestBytes, result.SignatureBytes, HashAlgorithmName.SHA256).Should().BeTrue(
            because: "the detached signature must cover the exact canonical manifest bytes");
        using ZipArchive archive = ZipFile.OpenRead(outputPath);
        ReadEntry(archive, "resources/b.md").Should().Equal(
            Encoding.UTF8.GetBytes("second\nline\n"),
            because: "text payloads must be UTF-8 without BOM and use LF newlines");
    }

    [Test]
    public void Build_ShouldRejectDuplicateUris_WhenSourceIsAmbiguous()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        File.WriteAllText(Path.Combine(_directory, "b.md"), "b");
        string sourcePath = WriteSource("""
			[
			  { "id": "guide-a", "uri": "docs://mcp/guides/same", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" },
			  { "id": "guide-b", "uri": "docs://mcp/guides/same", "sourcePath": "b.md", "bundlePath": "resources/b.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*resource URI*",
                because: "ambiguous stable resource identities must fail the whole bundle build");
    }

    [Test]
    public void Build_ShouldRejectTraversal_WhenBundlePathEscapesResources()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "id": "guide-a", "uri": "docs://mcp/guides/a", "sourcePath": "a.md", "bundlePath": "resources/../a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*safe resources/ path*",
                because: "published archives must not contain traversal paths");
    }

    [Test]
    public void Build_ShouldRejectMalformedUtf8_WhenResourceCannotBeDecodedLosslessly()
    {
        // Arrange
        File.WriteAllBytes(Path.Combine(_directory, "a.md"), [0xC3, 0x28]);
        string sourcePath = WriteSource("""
			[
			  { "id": "guide-a", "uri": "docs://mcp/guides/a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*valid UTF-8*",
                because: "malformed source bytes must never be normalized and signed as replacement characters");
    }

    [Test]
    public void Build_ShouldRejectWildcardCompatibilityVersion_WhenSelectionWouldBeAmbiguous()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directory, "a.md"), "a");
        string sourcePath = WriteSource("""
			[
			  { "id": "guide-a", "uri": "docs://mcp/guides/a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""", clioMax: "8.1.x");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        Action act = () => new BundleBuilder().Build(sourcePath, Path.Combine(_directory, "bundle.zip"), key);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*exact MAJOR.MINOR.PATCH*",
                because: "producer and consumer must share one deterministic compatibility comparison grammar");
    }

    [Test]
    public void Build_ShouldAcceptZeroLengthResource_WhenBundleIsOtherwiseValid()
    {
        // Arrange
        File.WriteAllBytes(Path.Combine(_directory, "a.md"), []);
        string sourcePath = WriteSource("""
			[
			  { "id": "guide-a", "uri": "docs://mcp/guides/a", "sourcePath": "a.md", "bundlePath": "resources/a.md", "mediaType": "text/markdown" }
			]
			""");
        string outputPath = Path.Combine(_directory, "bundle.zip");
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        BundleBuildResult result = new BundleBuilder().Build(sourcePath, outputPath, key);

        // Assert
        result.Manifest.Resources.Single().Length.Should().Be(0,
            because: "the valid-empty fixture means one zero-length resource, not a manifest with no resources");
    }

    private string WriteSource(
        string resourcesJson,
        string guidanceIdsJson = "[\"guide-a\"]",
        string resourceUrisJson = "[\"docs://mcp/guides/a\"]",
        string clioMax = "8.1.999")
    {
        string sourcePath = Path.Combine(_directory, "bundle-source.json");
        File.WriteAllText(sourcePath, $$"""
			{
			  "contractVersion": "0.1.0",
			  "bundleSchemaVersion": "0.1.0",
			  "sequence": 1,
			  "bundleVersion": "2026.07.18.1",
			  "issuedAt": "2026-07-18T00:00:00Z",
			  "source": { "repository": "example/repo", "commit": "0123456789abcdef" },
			  "compatibility": {
			    "clio": { "min": "8.1.0", "max": "{{clioMax}}" },
			    "mcpToolContract": { "min": "1.0.0", "max": "1.999.999" }
			  },
			  "requirements": {
			    "tools": ["get-guidance"],
			    "guidanceIds": {{guidanceIdsJson}},
			    "resourceUris": {{resourceUrisJson}}
			  },
			  "signature": { "algorithm": "ECDSA-P256-SHA256", "keyId": "p1-test" },
			  "resources": {{resourcesJson}}
			}
			""");
        return sourcePath;
    }

    private static byte[] ReadEntry(ZipArchive archive, string path)
    {
        using Stream stream = archive.GetEntry(path)!.Open();
        using MemoryStream result = new();
        stream.CopyTo(result);
        return result.ToArray();
    }
}

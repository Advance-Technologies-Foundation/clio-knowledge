using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Clio.Knowledge.Bundle;

/// <summary>
/// Verifies a built bundle the way the Clio consumer will, before it is ever published.
/// </summary>
/// <remarks>
/// A release that fails verification must never reach a consumer, and finding that out from user
/// reports is far too late. The release pipeline runs this against the exact artifact it is about to
/// upload, and against the artifact after upload, so both the build and the transfer are proven
/// before the release stops being a draft.
/// </remarks>
public sealed class BundleVerifier
{
    private const int MaxArchiveBytes = 40 * 1024 * 1024;
    private const int MaxArchiveEntries = 1024;
    private const int MaxManifestBytes = 4 * 1024 * 1024;
    private const int MaxSignatureBytes = 4 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    /// Verifies one archive against a trusted public key.
    /// </summary>
    /// <param name="bundlePath">The archive to inspect.</param>
    /// <param name="publicKeyPem">The trusted P-256 public key in PEM form.</param>
    /// <param name="expectedKeyId">The key identifier the manifest must name.</param>
    /// <param name="expectedLibraryVersion">
    /// The library version the manifest must declare, or <see langword="null"/> to accept any. The
    /// release pipeline passes the tag here, which is what binds a tag to the content it ships.
    /// </param>
    /// <returns>The verified identity of the artifact.</returns>
    /// <exception cref="InvalidDataException">The archive is not a bundle this key vouches for.</exception>
    public BundleVerificationResult Verify(
        string bundlePath,
        string publicKeyPem,
        string expectedKeyId,
        string? expectedLibraryVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundlePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedKeyId);

        string fullPath = Path.GetFullPath(bundlePath);
        long length = new FileInfo(fullPath).Length;
        if (length <= 0 || length > MaxArchiveBytes)
        {
            throw new InvalidDataException($"Bundle archive must be between 1 and {MaxArchiveBytes} bytes.");
        }
        string artifactDigest;
        using (FileStream artifact = File.OpenRead(fullPath))
        {
            artifactDigest = Convert.ToHexStringLower(SHA256.HashData(artifact));
        }

        using ZipArchive archive = ZipFile.OpenRead(fullPath);
        if (archive.Entries.Count > MaxArchiveEntries)
        {
            throw new InvalidDataException($"Bundle archive must contain at most {MaxArchiveEntries} entries.");
        }
        byte[] manifestBytes = ReadEntry(archive, "manifest.json", MaxManifestBytes);
        byte[] signatureBytes = ReadEntry(archive, "manifest.sig", MaxSignatureBytes);
        KnowledgeBundleManifest manifest = JsonSerializer.Deserialize(
                manifestBytes,
                BundleJsonContext.Default.KnowledgeBundleManifest)
            ?? throw new InvalidDataException("Bundle manifest is empty.");

        if (!string.Equals(manifest.Signature.KeyId, expectedKeyId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Bundle manifest is signed by key '{manifest.Signature.KeyId}' rather than '{expectedKeyId}'.");
        }
        if (expectedLibraryVersion is not null
            && !string.Equals(manifest.LibraryVersion, expectedLibraryVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Bundle declares library version '{manifest.LibraryVersion}' rather than '{expectedLibraryVersion}'.");
        }
        VerifySignature(manifestBytes, signatureBytes, publicKeyPem);
        VerifyInventory(archive, manifest);
        return new BundleVerificationResult(
            manifest.LibraryId,
            manifest.LibraryVersion,
            manifest.Sequence,
            artifactDigest,
            manifest.Resources.Count);
    }

    private static void VerifySignature(byte[] manifestBytes, byte[] signatureBytes, string publicKeyPem)
    {
        using ECDsa verifier = ECDsa.Create();
        verifier.ImportFromPem(publicKeyPem);
        ECParameters parameters = verifier.ExportParameters(includePrivateParameters: false);
        if (!string.Equals(
                parameters.Curve.Oid.Value,
                ECCurve.NamedCurves.nistP256.Oid.Value,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("Bundle verification requires a P-256 public key.");
        }
        if (!verifier.VerifyData(manifestBytes, signatureBytes, HashAlgorithmName.SHA256))
        {
            throw new InvalidDataException("Bundle manifest signature does not verify against the trusted key.");
        }
    }

    /// <summary>
    /// Confirms the archive carries exactly the declared runtime content and nothing else.
    /// </summary>
    /// <remarks>
    /// Both directions matter. A missing resource breaks a consumer at activation; an undeclared
    /// extra entry means something outside the publication contract — repository automation, test
    /// fixtures, or key material — was swept into a public artifact.
    /// </remarks>
    private static void VerifyInventory(ZipArchive archive, KnowledgeBundleManifest manifest)
    {
        HashSet<string> expected = new(StringComparer.Ordinal) { "manifest.json", "manifest.sig" };
        foreach (BundleResource resource in manifest.Resources)
        {
            if (!expected.Add(resource.Path))
            {
                throw new InvalidDataException($"Bundle declares path '{resource.Path}' more than once.");
            }
        }
        string[] actual = archive.Entries.Select(entry => entry.FullName).ToArray();
        string[] duplicates = actual.GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException(
                $"Bundle archive contains duplicate entries: {string.Join(", ", duplicates)}.");
        }
        string[] unexpected = actual.Where(name => !expected.Contains(name)).Order(StringComparer.Ordinal).ToArray();
        if (unexpected.Length > 0)
        {
            throw new InvalidDataException(
                $"Bundle archive contains entries the manifest does not declare: {string.Join(", ", unexpected)}.");
        }
        string[] missing = expected.Where(name => !actual.Contains(name, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"Bundle archive is missing declared entries: {string.Join(", ", missing)}.");
        }
        foreach (BundleResource resource in manifest.Resources)
        {
            byte[] bytes = ReadEntry(archive, resource.Path, MaxManifestBytes);
            if (bytes.LongLength != resource.Length
                || !string.Equals(
                    Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    resource.Digest,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Bundle resource '{resource.Path}' does not match its declared length and digest.");
            }
            _ = StrictUtf8.GetString(bytes);
        }
    }

    private static byte[] ReadEntry(ZipArchive archive, string path, int maximumBytes)
    {
        ZipArchiveEntry entry = archive.GetEntry(path)
            ?? throw new InvalidDataException($"Bundle archive is missing '{path}'.");
        if (entry.Length <= 0 && !path.StartsWith("resources/", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Bundle entry '{path}' is empty.");
        }
        if (entry.Length > maximumBytes)
        {
            throw new InvalidDataException($"Bundle entry '{path}' exceeds the {maximumBytes}-byte limit.");
        }
        byte[] bytes = new byte[entry.Length];
        using Stream stream = entry.Open();
        stream.ReadExactly(bytes);
        return bytes;
    }
}

/// <summary>The verified identity of one bundle artifact.</summary>
public sealed record BundleVerificationResult(
    string LibraryId,
    string LibraryVersion,
    ulong Sequence,
    string ArtifactSha256,
    int ResourceCount);

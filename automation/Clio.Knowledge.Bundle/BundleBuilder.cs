using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Clio.Knowledge.Bundle;

public sealed class BundleBuilder
{
    private const string DigestAlgorithm = "SHA-256";
    private static readonly DateTimeOffset ZipEpoch = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Regex CompatibilityVersionPattern = new(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$",
        RegexOptions.CultureInvariant);

    public BundleBuildResult Build(string sourceFilePath, string outputPath, ECDsa signingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(signingKey);

        string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath))
            ?? throw new InvalidOperationException("Bundle source file must have a parent directory.");
        BundleSource source = ReadSource(sourceFilePath);
        ValidateSource(source, signingKey);

        List<PreparedResource> resources = source.Resources
            .OrderBy(resource => resource.BundlePath, StringComparer.Ordinal)
            .Select(resource => PrepareResource(sourceDirectory, resource))
            .ToList();
        KnowledgeBundleManifest manifest = CreateManifest(source, resources);
        byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, BundleJsonContext.Default.KnowledgeBundleManifest);
        byte[] signatureBytes = signingKey.SignData(manifestBytes, HashAlgorithmName.SHA256);

        string fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)
            ?? throw new InvalidOperationException("Bundle output path must have a parent directory."));
        using (FileStream output = File.Create(fullOutputPath))
        {
            using ZipArchive archive = new(output, ZipArchiveMode.Create, leaveOpen: true);
            WriteEntry(archive, "manifest.json", manifestBytes);
            WriteEntry(archive, "manifest.sig", signatureBytes);
            foreach (PreparedResource resource in resources)
            {
                WriteEntry(archive, resource.Descriptor.BundlePath, resource.Bytes);
            }
        }

        string artifactDigest = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(fullOutputPath)));
        return new BundleBuildResult(manifest, manifestBytes, signatureBytes, artifactDigest);
    }

    public static string CanonicalizeText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static BundleSource ReadSource(string sourceFilePath)
    {
        using FileStream stream = File.OpenRead(sourceFilePath);
        return JsonSerializer.Deserialize(stream, BundleJsonContext.Default.BundleSource)
            ?? throw new InvalidDataException("Bundle source JSON is empty.");
    }

    private static void ValidateSource(BundleSource source, ECDsa signingKey)
    {
        if (source.Sequence == 0)
        {
            throw new InvalidDataException("Bundle sequence must be greater than zero.");
        }
        if (!string.Equals(source.Signature.Algorithm, "ECDSA-P256-SHA256", StringComparison.Ordinal))
        {
            throw new InvalidDataException("P1 supports only ECDSA-P256-SHA256 detached signatures.");
        }
        ECParameters keyParameters = signingKey.ExportParameters(includePrivateParameters: false);
        if (!string.Equals(keyParameters.Curve.Oid.Value, ECCurve.NamedCurves.nistP256.Oid.Value, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The ECDSA-P256-SHA256 signature requires a P-256 signing key.");
        }
        EnsureUnique(source.Resources.Select(resource => resource.Id), "resource id");
        EnsureUnique(source.Resources.Select(resource => resource.Uri), "resource URI");
        EnsureUnique(source.Resources.Select(resource => resource.BundlePath), "bundle path");
        EnsureUnique(source.Requirements.Tools, "required tool");
        EnsureUnique(source.Requirements.GuidanceIds, "required guidance id");
        EnsureUnique(source.Requirements.ResourceUris, "required resource URI");
        EnsureSameValues(
            source.Resources.Select(resource => resource.Id),
            source.Requirements.GuidanceIds,
            "resource ids",
            "required guidance ids");
        EnsureSameValues(
            source.Resources.Select(resource => resource.Uri),
            source.Requirements.ResourceUris,
            "resource URIs",
            "required resource URIs");
        ValidateVersionRange(source.Compatibility.Clio, "Clio");
        ValidateVersionRange(source.Compatibility.McpToolContract, "MCP tool contract");
        foreach (SourceResource resource in source.Resources)
        {
            ValidateBundlePath(resource.BundlePath);
            if (!resource.MediaType.StartsWith("text/", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"P1 resource '{resource.Id}' must use a text/* media type.");
            }
        }
    }

    private static void ValidateVersionRange(VersionRange range, string label)
    {
        if (!CompatibilityVersionPattern.IsMatch(range.Min)
            || !CompatibilityVersionPattern.IsMatch(range.Max)
            || !Version.TryParse(range.Min, out Version? min)
            || !Version.TryParse(range.Max, out Version? max))
        {
            throw new InvalidDataException(
                $"{label} compatibility bounds must use exact MAJOR.MINOR.PATCH versions.");
        }
        if (min > max)
        {
            throw new InvalidDataException($"{label} compatibility minimum must not exceed its maximum.");
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string label)
    {
        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value) || !unique.Add(value))
            {
                throw new InvalidDataException($"Every {label} must be non-empty and unique; invalid value '{value}'.");
            }
        }
    }

    private static void EnsureSameValues(
        IEnumerable<string> actual,
        IEnumerable<string> declared,
        string actualLabel,
        string declaredLabel)
    {
        HashSet<string> actualValues = actual.ToHashSet(StringComparer.Ordinal);
        HashSet<string> declaredValues = declared.ToHashSet(StringComparer.Ordinal);
        if (!actualValues.SetEquals(declaredValues))
        {
            throw new InvalidDataException($"Declared {declaredLabel} must exactly match {actualLabel}.");
        }
    }

    private static void ValidateBundlePath(string path)
    {
        if (Path.IsPathRooted(path)
            || path.Contains("..", StringComparison.Ordinal)
            || path.Contains('\\')
            || !path.StartsWith("resources/", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Bundle resource path '{path}' must be a safe resources/ path.");
        }
    }

    private static PreparedResource PrepareResource(string sourceDirectory, SourceResource descriptor)
    {
        string fullPath = Path.GetFullPath(Path.Combine(sourceDirectory, descriptor.SourcePath));
        string relativePath = Path.GetRelativePath(sourceDirectory, fullPath);
        if (Path.IsPathRooted(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Source path '{descriptor.SourcePath}' escapes the bundle source directory.");
        }
        RejectReparsePoints(sourceDirectory, fullPath, descriptor.SourcePath);
        string decodedText;
        try
        {
            decodedText = StrictUtf8.GetString(File.ReadAllBytes(fullPath));
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                $"Source resource '{descriptor.SourcePath}' must contain valid UTF-8 text.",
                exception);
        }
        string canonicalText = CanonicalizeText(decodedText.TrimStart('\uFEFF'));
        byte[] bytes = StrictUtf8.GetBytes(canonicalText);
        return new PreparedResource(descriptor, bytes, Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    private static void RejectReparsePoints(string sourceDirectory, string fullPath, string sourcePath)
    {
        string currentPath = sourceDirectory;
        if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The bundle source directory must not be a symbolic link or junction.");
        }
        foreach (string segment in Path.GetRelativePath(sourceDirectory, fullPath)
                     .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            currentPath = Path.Combine(currentPath, segment);
            if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    $"Source resource '{sourcePath}' must not traverse a symbolic link or junction.");
            }
        }
    }

    private static KnowledgeBundleManifest CreateManifest(BundleSource source, IReadOnlyList<PreparedResource> resources) => new(
        source.ContractVersion,
        source.BundleSchemaVersion,
        source.Sequence,
        source.BundleVersion,
        source.IssuedAt.ToUniversalTime(),
        source.Source,
        source.Compatibility,
        new BundleRequirements(
            source.Requirements.Tools.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            source.Requirements.GuidanceIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            source.Requirements.ResourceUris.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
        DigestAlgorithm,
        source.Signature,
        resources.Select(resource => new BundleResource(
            resource.Descriptor.Id,
            resource.Descriptor.Uri,
            resource.Descriptor.BundlePath,
            resource.Descriptor.MediaType,
            resource.Bytes.LongLength,
            resource.Digest)).ToArray());

    private static void WriteEntry(ZipArchive archive, string path, byte[] bytes)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        entry.LastWriteTime = ZipEpoch;
        entry.ExternalAttributes = 0;
        using Stream stream = entry.Open();
        stream.Write(bytes);
    }

    private sealed record PreparedResource(SourceResource Descriptor, byte[] Bytes, string Digest);
}

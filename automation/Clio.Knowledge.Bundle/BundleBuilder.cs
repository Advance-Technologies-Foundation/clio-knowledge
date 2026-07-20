using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Clio.Knowledge.Bundle;

public sealed class BundleBuilder
{
    private const string ContractVersion = "1.0.0";
    private const string BundleSchemaVersion = "1.0.0";
    private const string RepositorySchemaPath = "./schemas/v1/knowledge-repository.schema.json";
    private const string DigestAlgorithm = "SHA-256";
    private const int MaxArchiveBytes = 40 * 1024 * 1024;
    private const int MaxArchiveEntries = 1024;
    private const int MaxResourceBytes = 4 * 1024 * 1024;
    private const int MaxBundleResourceBytes = 32 * 1024 * 1024;
    private static readonly DateTimeOffset ZipEpoch = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Regex CompatibilityVersionPattern = new(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$",
        RegexOptions.CultureInvariant);
    private static readonly Regex LibraryIdPattern = new(
        "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*(?:\\.[a-z][a-z0-9]*(?:-[a-z0-9]+)*)+$",
        RegexOptions.CultureInvariant);
    private static readonly Regex StableIdPattern = new(
        "^[a-z0-9]+(?:[.-][a-z0-9]+)*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex RolePattern = new(
        "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex CompleteCommitPattern = new(
        "^(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})$",
        RegexOptions.CultureInvariant);

    public BundleBuildResult Build(
        string sourceFilePath,
        string outputPath,
        ECDsa signingKey,
        BundlePublicationMetadata publication)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentNullException.ThrowIfNull(publication);

        string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath))
            ?? throw new InvalidOperationException("Bundle source file must have a parent directory.");
        BundleSource source = ReadSource(sourceFilePath);
        ValidateSource(source);
        ValidatePublication(publication, signingKey);

        List<PreparedResource> resources = [];
        long totalResourceBytes = 0;
        foreach (SourceResource descriptor in source.Resources.OrderBy(
                     resource => resource.ItemId,
                     StringComparer.Ordinal))
        {
            PreparedResource resource = PrepareResource(sourceDirectory, descriptor);
            totalResourceBytes = checked(totalResourceBytes + resource.Bytes.LongLength);
            if (totalResourceBytes > MaxBundleResourceBytes)
            {
                throw new InvalidDataException(
                    $"Bundle resources exceed the {MaxBundleResourceBytes}-byte total size limit.");
            }
            resources.Add(resource);
        }
        KnowledgeBundleManifest manifest = CreateManifest(source, publication, resources);
        byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, BundleJsonContext.Default.KnowledgeBundleManifest);
        byte[] signatureBytes = signingKey.SignData(manifestBytes, HashAlgorithmName.SHA256);

        string fullOutputPath = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(fullOutputPath)
            ?? throw new InvalidOperationException("Bundle output path must have a parent directory.");
        Directory.CreateDirectory(outputDirectory);
        string temporaryPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(fullOutputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (FileStream output = new(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                using ZipArchive archive = new(output, ZipArchiveMode.Create, leaveOpen: true);
                WriteEntry(archive, "manifest.json", manifestBytes);
                WriteEntry(archive, "manifest.sig", signatureBytes);
                foreach (PreparedResource resource in resources)
                {
                    WriteEntry(archive, resource.Descriptor.BundlePath, resource.Bytes);
                }
            }
            long archiveLength = new FileInfo(temporaryPath).Length;
            if (archiveLength > MaxArchiveBytes)
            {
                throw new InvalidDataException(
                    $"Bundle archive exceeds the {MaxArchiveBytes}-byte compressed size limit.");
            }
            string artifactDigest;
            using (FileStream artifact = File.OpenRead(temporaryPath))
            {
                artifactDigest = Convert.ToHexStringLower(SHA256.HashData(artifact));
            }
            PublishAtomically(temporaryPath, fullOutputPath);
            return new BundleBuildResult(manifest, manifestBytes, signatureBytes, artifactDigest);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
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

    private static void ValidateSource(BundleSource source)
    {
        if (!string.Equals(source.Schema, RepositorySchemaPath, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"The repository manifest must reference '{RepositorySchemaPath}'.");
        }
        if (!string.Equals(source.ContractVersion, ContractVersion, StringComparison.Ordinal)
            || !string.Equals(source.BundleSchemaVersion, BundleSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"The canonical multi-source builder requires contract and schema version {ContractVersion}.");
        }
        if (string.IsNullOrWhiteSpace(source.LibraryId) || !LibraryIdPattern.IsMatch(source.LibraryId))
        {
            throw new InvalidDataException(
                "Library ID must be a lowercase reverse-DNS identifier such as 'com.creatio.clio'.");
        }
        if (string.IsNullOrWhiteSpace(source.LibraryVersion) || source.LibraryVersion.Length > 128)
        {
            throw new InvalidDataException("Library version must be a non-empty publisher generation label.");
        }
        if (source.Sequence == 0)
        {
            throw new InvalidDataException("Bundle sequence must be greater than zero.");
        }
        if (source.Compatibility is null
            || source.Compatibility.Clio is null
            || source.Compatibility.McpToolContract is null
            || source.Requirements is null
            || source.Requirements.Tools is null
            || source.Requirements.ItemIds is null
            || source.Requirements.ResourceUris is null
            || source.Resources is null
            || source.Resources.Count == 0
            || source.Resources.Any(resource => resource is null))
        {
            throw new InvalidDataException("Bundle source is missing required v1 values.");
        }
        if (source.Resources.Count > MaxArchiveEntries - 2)
        {
            throw new InvalidDataException(
                $"Bundle archive cannot contain more than {MaxArchiveEntries} entries including its manifest and signature.");
        }
        EnsureUnique(source.Resources.Select(resource => resource.ItemId), "resource item ID");
        EnsureUnique(source.Resources.Select(resource => resource.Uri), "canonical resource URI");
        EnsureUnique(source.Resources.Select(resource => resource.BundlePath), "bundle path");
        EnsureUnique(
            source.Resources.Select(resource => $"{resource.TopicId}\u001f{resource.Role}"),
            "resource topic and role pair");
        EnsureUnique(source.Requirements.Tools, "required tool");
        EnsureUnique(source.Requirements.ItemIds, "required item ID");
        EnsureUnique(source.Requirements.ResourceUris, "required resource URI");
        EnsureSameValues(
            source.Resources.Select(resource => resource.ItemId),
            source.Requirements.ItemIds,
            "resource item IDs",
            "required item IDs");
        EnsureSameValues(
            source.Resources.Select(resource => resource.Uri),
            source.Requirements.ResourceUris,
            "resource URIs",
            "required resource URIs");
        ValidateVersionRange(source.Compatibility.Clio, "Clio");
        ValidateVersionRange(source.Compatibility.McpToolContract, "MCP tool contract");
        List<string> allRoutes = [];
        foreach (SourceResource resource in source.Resources)
        {
            ValidateStableId(resource.ItemId, "item ID");
            ValidateStableId(resource.TopicId, "topic ID");
            ValidateDiscoveryText(resource.Title, "title", resource.ItemId, 160);
            ValidateDiscoveryText(resource.Description, "description", resource.ItemId, 1000);
            IReadOnlyList<string> requiredFeatures = resource.RequiredFeatures ?? [];
            EnsureUnique(requiredFeatures, $"required feature for resource '{resource.ItemId}'");
            foreach (string requiredFeature in requiredFeatures)
            {
                ValidateStableId(requiredFeature, $"required feature for resource '{resource.ItemId}'");
            }
            if (string.IsNullOrWhiteSpace(resource.SourcePath))
            {
                throw new InvalidDataException($"Resource '{resource.ItemId}' must declare a source path.");
            }
            if (string.IsNullOrWhiteSpace(resource.Role) || !RolePattern.IsMatch(resource.Role))
            {
                throw new InvalidDataException(
                    $"Resource '{resource.ItemId}' role must be a lowercase stable role identifier.");
            }
            string expectedUri = CreateCanonicalUri(source.LibraryId, resource.ItemId);
            if (!string.Equals(resource.Uri, expectedUri, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Resource '{resource.ItemId}' URI must be exactly '{expectedUri}'.");
            }
            allRoutes.Add(resource.Uri);
            foreach (string legacyUri in resource.LegacyUris ?? [])
            {
                ValidateLegacyUri(resource, legacyUri, expectedUri);
                allRoutes.Add(legacyUri);
            }
            ValidateBundlePath(resource.BundlePath);
            if (string.IsNullOrWhiteSpace(resource.MediaType)
                || !resource.MediaType.StartsWith("text/", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"P1 resource '{resource.ItemId}' must use a text/* media type.");
            }
        }
        EnsureUnique(allRoutes, "canonical or legacy resource route");
    }

    private static void ValidatePublication(BundlePublicationMetadata publication, ECDsa signingKey)
    {
        if (publication.Source is null
            || string.IsNullOrWhiteSpace(publication.Source.Repository)
            || string.IsNullOrWhiteSpace(publication.Source.Commit))
        {
            throw new InvalidDataException("Bundle publication provenance must identify a repository and immutable commit.");
        }
        if (!CompleteCommitPattern.IsMatch(publication.Source.Commit))
        {
            throw new InvalidDataException(
                "Bundle publication commit must be a complete SHA-1 or SHA-256 hexadecimal object ID.");
        }
        if (publication.Signature is null || string.IsNullOrWhiteSpace(publication.Signature.KeyId)
            || !string.Equals(publication.Signature.Algorithm, "ECDSA-P256-SHA256", StringComparison.Ordinal))
        {
            throw new InvalidDataException("P1 supports only ECDSA-P256-SHA256 detached signatures.");
        }
        ECParameters keyParameters = signingKey.ExportParameters(includePrivateParameters: false);
        if (!string.Equals(keyParameters.Curve.Oid.Value, ECCurve.NamedCurves.nistP256.Oid.Value, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The ECDSA-P256-SHA256 signature requires a P-256 signing key.");
        }
    }

    public static string CreateCanonicalUri(string libraryId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return $"docs://knowledge/{libraryId}/{itemId}";
    }

    private static void ValidateStableId(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 160 || !StableIdPattern.IsMatch(value))
        {
            throw new InvalidDataException(
                $"Every resource {label} must be a lowercase dot-or-hyphen separated stable identifier.");
        }
    }

    private static void ValidateDiscoveryText(
        string value,
        string label,
        string itemId,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maxLength
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Any(char.IsControl))
        {
            throw new InvalidDataException(
                $"Resource '{itemId}' {label} must be non-empty, trimmed, free of control characters, "
                + $"and at most {maxLength} characters.");
        }
    }

    private static void ValidateLegacyUri(SourceResource resource, string legacyUri, string canonicalUri)
    {
        if (string.IsNullOrWhiteSpace(legacyUri)
            || !legacyUri.StartsWith("docs://", StringComparison.Ordinal)
            || !Uri.TryCreate(legacyUri, UriKind.Absolute, out _)
            || string.Equals(legacyUri, canonicalUri, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Resource '{resource.ItemId}' contains an invalid legacy URI alias '{legacyUri}'.");
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
        if (string.IsNullOrWhiteSpace(path)
            || Path.IsPathRooted(path)
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
        long sourceLength = new FileInfo(fullPath).Length;
        if (sourceLength > MaxResourceBytes)
        {
            throw new InvalidDataException(
                $"Source resource '{descriptor.SourcePath}' exceeds the {MaxResourceBytes}-byte item size limit.");
        }
        string decodedText;
        try
        {
            byte[] sourceBytes = File.ReadAllBytes(fullPath);
            if (sourceBytes.Length > MaxResourceBytes)
            {
                throw new InvalidDataException(
                    $"Source resource '{descriptor.SourcePath}' exceeds the {MaxResourceBytes}-byte item size limit.");
            }
            decodedText = StrictUtf8.GetString(sourceBytes);
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

    private static KnowledgeBundleManifest CreateManifest(
        BundleSource source,
        BundlePublicationMetadata publication,
        IReadOnlyList<PreparedResource> resources) => new(
        source.ContractVersion,
        source.BundleSchemaVersion,
        source.LibraryId,
        source.LibraryVersion,
        source.Sequence,
        publication.Source,
        source.Compatibility,
        new BundleRequirements(
            source.Requirements.Tools.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            source.Requirements.ItemIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            source.Requirements.ResourceUris.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
        DigestAlgorithm,
        publication.Signature,
        resources.Select(resource => new BundleResource(
            resource.Descriptor.ItemId,
            resource.Descriptor.Title,
            resource.Descriptor.Description,
            resource.Descriptor.TopicId,
            resource.Descriptor.Role,
            resource.Descriptor.RequiredFeatures is { Count: > 0 }
                ? resource.Descriptor.RequiredFeatures.OrderBy(value => value, StringComparer.Ordinal).ToArray()
                : null,
            resource.Descriptor.Uri,
            resource.Descriptor.LegacyUris is { Count: > 0 }
                ? resource.Descriptor.LegacyUris.OrderBy(value => value, StringComparer.Ordinal).ToArray()
                : null,
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

    private static void PublishAtomically(string temporaryPath, string outputPath)
    {
        if (File.Exists(outputPath))
        {
            File.Replace(temporaryPath, outputPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return;
        }
        File.Move(temporaryPath, outputPath);
    }

    private sealed record PreparedResource(SourceResource Descriptor, byte[] Bytes, string Digest);
}

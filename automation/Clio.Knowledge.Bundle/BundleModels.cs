using System.Text.Json.Serialization;

namespace Clio.Knowledge.Bundle;

public sealed record BundleSource(
    string ContractVersion,
    string BundleSchemaVersion,
    ulong Sequence,
    string BundleVersion,
    DateTimeOffset IssuedAt,
    SourceProvenance Source,
    CompatibilityRange Compatibility,
    BundleRequirements Requirements,
    SignatureDescriptor Signature,
    IReadOnlyList<SourceResource> Resources);

public sealed record SourceProvenance(string Repository, string Commit);

public sealed record CompatibilityRange(VersionRange Clio, VersionRange McpToolContract);

public sealed record VersionRange(string Min, string Max);

public sealed record BundleRequirements(
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> GuidanceIds,
    IReadOnlyList<string> ResourceUris);

public sealed record SignatureDescriptor(string Algorithm, string KeyId);

public sealed record SourceResource(
    string Id,
    string Uri,
    string SourcePath,
    string BundlePath,
    string MediaType);

public sealed record KnowledgeBundleManifest(
    string ContractVersion,
    string BundleSchemaVersion,
    ulong Sequence,
    string BundleVersion,
    DateTimeOffset IssuedAt,
    SourceProvenance Source,
    CompatibilityRange Compatibility,
    BundleRequirements Requirements,
    string DigestAlg,
    SignatureDescriptor Signature,
    IReadOnlyList<BundleResource> Resources);

public sealed record BundleResource(
    string Id,
    string Uri,
    string Path,
    string MediaType,
    long Length,
    string Digest);

public sealed record BundleBuildResult(
    KnowledgeBundleManifest Manifest,
    byte[] ManifestBytes,
    byte[] SignatureBytes,
    string ArtifactSha256);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BundleSource))]
[JsonSerializable(typeof(KnowledgeBundleManifest))]
internal sealed partial class BundleJsonContext : JsonSerializerContext;

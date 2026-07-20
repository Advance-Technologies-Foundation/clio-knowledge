using System.Text.Json.Serialization;

namespace Clio.Knowledge.Bundle;

public sealed record BundleSource(
    [property: JsonPropertyName("$schema")]
    string Schema,
    string ContractVersion,
    string BundleSchemaVersion,
    string LibraryId,
    string LibraryVersion,
    ulong Sequence,
    CompatibilityRange Compatibility,
    BundleRequirements Requirements,
    IReadOnlyList<SourceResource> Resources);

public sealed record BundlePublicationMetadata(
    SourceProvenance Source,
    SignatureDescriptor Signature);

public sealed record SourceProvenance(string Repository, string Commit);

public sealed record CompatibilityRange(VersionRange Clio, VersionRange McpToolContract);

public sealed record VersionRange(string Min, string Max);

public sealed record BundleRequirements(
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> ItemIds,
    IReadOnlyList<string> ResourceUris);

public sealed record SignatureDescriptor(string Algorithm, string KeyId);

public sealed record SourceResource(
    string ItemId,
    string Title,
    string Description,
    string TopicId,
    string Role,
    IReadOnlyList<string>? RequiredFeatures,
    string Uri,
    IReadOnlyList<string>? LegacyUris,
    string SourcePath,
    string BundlePath,
    string MediaType);

public sealed record KnowledgeBundleManifest(
    string ContractVersion,
    string BundleSchemaVersion,
    string LibraryId,
    string LibraryVersion,
    ulong Sequence,
    SourceProvenance Source,
    CompatibilityRange Compatibility,
    BundleRequirements Requirements,
    string DigestAlg,
    SignatureDescriptor Signature,
    IReadOnlyList<BundleResource> Resources);

public sealed record BundleResource(
    string ItemId,
    string Title,
    string Description,
    string TopicId,
    string Role,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? RequiredFeatures,
    string Uri,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? LegacyUris,
    string Path,
    string MediaType,
    long Length,
    string Digest);

public sealed record BundleBuildResult(
    KnowledgeBundleManifest Manifest,
    byte[] ManifestBytes,
    byte[] SignatureBytes,
    string ArtifactSha256);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(BundleSource))]
[JsonSerializable(typeof(KnowledgeBundleManifest))]
internal sealed partial class BundleJsonContext : JsonSerializerContext;

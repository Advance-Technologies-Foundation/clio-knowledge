using System.Security.Cryptography;
using System.Text.Json;
using Clio.Knowledge.Bundle;

const string buildUsage =
    "Usage: Clio.Knowledge.Bundle build <bundle-source.json> <signing-key.pem> <output.zip> <key-id> <repository> <commit>";
const string verifyUsage =
    "Usage: Clio.Knowledge.Bundle verify <bundle.zip> <public-key.pem> <key-id> [expected-library-version]";

// The original verb-less form is still accepted so existing docs and local scripts keep working.
string[] arguments = args is ["build" or "verify", ..] ? args : ["build", .. args];

try
{
    return arguments[0] switch
    {
        "build" => Build(arguments),
        "verify" => Verify(arguments),
        _ => Usage()
    };
}
catch (Exception exception) when (exception is IOException
    or UnauthorizedAccessException
    or CryptographicException
    or InvalidDataException
    or JsonException)
{
    Console.Error.WriteLine($"Bundle operation failed: {exception.Message}");
    return 1;
}

int Build(string[] parts)
{
    if (parts.Length != 7)
    {
        Console.Error.WriteLine(buildUsage);
        return 2;
    }
    using ECDsa signingKey = ECDsa.Create();
    signingKey.ImportFromPem(File.ReadAllText(parts[2]));
    BundlePublicationMetadata publication = new(
        new SourceProvenance(parts[5], parts[6]),
        new SignatureDescriptor("ECDSA-P256-SHA256", parts[4]));
    BundleBuildResult result = new BundleBuilder().Build(parts[1], parts[3], signingKey, publication);
    Console.WriteLine($"Built {parts[3]}");
    Console.WriteLine(
        $"Library: {result.Manifest.LibraryId} {result.Manifest.LibraryVersion} sequence {result.Manifest.Sequence}");
    Console.WriteLine($"Artifact SHA-256: {result.ArtifactSha256}");
    return 0;
}

int Verify(string[] parts)
{
    if (parts.Length is not (4 or 5))
    {
        Console.Error.WriteLine(verifyUsage);
        return 2;
    }
    BundleVerificationResult result = new BundleVerifier().Verify(
        parts[1],
        File.ReadAllText(parts[2]),
        parts[3],
        parts.Length == 5 ? parts[4] : null);
    Console.WriteLine($"Verified {parts[1]}");
    Console.WriteLine(
        $"Library: {result.LibraryId} {result.LibraryVersion} sequence {result.Sequence} "
        + $"({result.ResourceCount} resources)");
    Console.WriteLine($"Artifact SHA-256: {result.ArtifactSha256}");
    return 0;
}

int Usage()
{
    Console.Error.WriteLine(buildUsage);
    Console.Error.WriteLine(verifyUsage);
    return 2;
}

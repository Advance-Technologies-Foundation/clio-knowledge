using System.Security.Cryptography;
using System.Text.Json;
using Clio.Knowledge.Bundle;

if (args.Length != 6)
{
    Console.Error.WriteLine("Usage: Clio.Knowledge.Bundle <bundle-source.json> <signing-key.pem> <output.zip> <key-id> <repository> <commit>");
    return 2;
}

try
{
    using ECDsa signingKey = ECDsa.Create();
    signingKey.ImportFromPem(File.ReadAllText(args[1]));
    BundlePublicationMetadata publication = new(
        new SourceProvenance(args[4], args[5]),
        new SignatureDescriptor("ECDSA-P256-SHA256", args[3]));
    BundleBuildResult result = new BundleBuilder().Build(args[0], args[2], signingKey, publication);
    Console.WriteLine($"Built {args[2]}");
    Console.WriteLine(
        $"Library: {result.Manifest.LibraryId} {result.Manifest.LibraryVersion} sequence {result.Manifest.Sequence}");
    Console.WriteLine($"Artifact SHA-256: {result.ArtifactSha256}");
    return 0;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or InvalidDataException or JsonException)
{
    Console.Error.WriteLine($"Bundle build failed: {exception.Message}");
    return 1;
}

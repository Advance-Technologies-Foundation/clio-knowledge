using System.Security.Cryptography;
using System.Text.Json;
using Clio.Knowledge.Bundle;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: Clio.Knowledge.Bundle <bundle-source.json> <signing-key.pem> <output.zip>");
    return 2;
}

try
{
    using ECDsa signingKey = ECDsa.Create();
    signingKey.ImportFromPem(File.ReadAllText(args[1]));
    BundleBuildResult result = new BundleBuilder().Build(args[0], args[2], signingKey);
    Console.WriteLine($"Built {args[2]}");
    Console.WriteLine($"Bundle: {result.Manifest.BundleVersion} sequence {result.Manifest.Sequence}");
    Console.WriteLine($"Artifact SHA-256: {result.ArtifactSha256}");
    return 0;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or InvalidDataException or JsonException)
{
    Console.Error.WriteLine($"Bundle build failed: {exception.Message}");
    return 1;
}

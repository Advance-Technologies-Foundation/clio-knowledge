using System.IO.Compression;
using System.Security.Cryptography;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

if (args.Length != 3)
{
    Console.Error.WriteLine(
        "Usage: Clio.Knowledge.NuGetSpike <v3-feed-or-folder> <package-id> <output-bundle.zip>");
    return 2;
}

string source = args[0];
string packageId = args[1];
string outputPath = Path.GetFullPath(args[2]);
SourceRepository repository = Repository.Factory.GetCoreV3(source);
FindPackageByIdResource packages = await repository.GetResourceAsync<FindPackageByIdResource>();
using SourceCacheContext cache = new();
IEnumerable<NuGetVersion> available = await packages.GetAllVersionsAsync(
    packageId,
    cache,
    NullLogger.Instance,
    CancellationToken.None);
NuGetVersion? selected = available
    .OrderByDescending(version => version)
    .FirstOrDefault();
if (selected is null)
{
    Console.Error.WriteLine($"Package '{packageId}' was not found at '{source}'.");
    return 1;
}

using MemoryStream packageBytes = new();
bool copied = await packages.CopyNupkgToStreamAsync(
    packageId,
    selected,
    packageBytes,
    cache,
    NullLogger.Instance,
    CancellationToken.None);
if (!copied)
{
    Console.Error.WriteLine($"Package '{packageId}' version '{selected}' could not be downloaded.");
    return 1;
}

packageBytes.Position = 0;
using ZipArchive package = new(packageBytes, ZipArchiveMode.Read, leaveOpen: true);
ZipArchiveEntry payload = package.GetEntry("knowledge/knowledge.bundle.zip")
    ?? throw new InvalidDataException("NuGet package does not contain knowledge/knowledge.bundle.zip.");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)
    ?? throw new InvalidOperationException("Output bundle path must have a parent directory."));
await using (Stream input = payload.Open())
await using (FileStream output = File.Create(outputPath))
{
    await input.CopyToAsync(output);
}

string packageHash = Convert.ToHexStringLower(SHA256.HashData(packageBytes.ToArray()));
string bundleHash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(outputPath)));
Console.WriteLine($"Package: {packageId} {selected}");
Console.WriteLine($"Package SHA-256: {packageHash}");
Console.WriteLine($"Bundle SHA-256: {bundleHash}");
Console.WriteLine($"Extracted: {outputPath}");
return 0;

using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length != 4)
{
    Console.Error.WriteLine(
        "Usage: Clio.Knowledge.OracleCapture <clio.dll> <clio-commit> <GuidanceGetTool.cs> <output-directory>");
    return 2;
}

string assemblyPath = Path.GetFullPath(args[0]);
string clioCommit = args[1];
string toolContractPath = Path.GetFullPath(args[2]);
string outputDirectory = Path.GetFullPath(args[3]);
if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Clio assembly was not found: {assemblyPath}");
    return 2;
}
if (!File.Exists(toolContractPath))
{
    Console.Error.WriteLine($"Guidance tool contract source was not found: {toolContractPath}");
    return 2;
}

Directory.CreateDirectory(outputDirectory);
using ClioLoadContext context = new(assemblyPath);
Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
CaptureTarget[] targets =
[
    new("esq", "Clio.Command.McpServer.Resources.EsqGuidanceResource", "Guide"),
    new("esq-filters", "Clio.Command.McpServer.Resources.EsqFiltersGuidanceResource", "Guide"),
    new("esq-filters-frontend", "Clio.Command.McpServer.Resources.EsqFiltersGuidanceResource", "FrontendGuide"),
    new("esq-filters-backend", "Clio.Command.McpServer.Resources.EsqFiltersBackendGuidanceResource", "Guide"),
    new("esq-filter-parsing", "Clio.Command.McpServer.Resources.EsqFilterParsingGuidanceResource", "Guide")
];

List<CapturedResource> resources = [];
foreach (CaptureTarget target in targets.OrderBy(target => target.Id, StringComparer.Ordinal))
{
    object value = ReadStaticField(assembly, target.TypeName, target.FieldName);
    Type valueType = value.GetType();
    string uri = ReadStringProperty(value, valueType, "Uri");
    string mediaType = ReadStringProperty(value, valueType, "MimeType");
    string text = CanonicalizeText(ReadStringProperty(value, valueType, "Text"));
    byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
    string relativePath = $"resources/{target.Id}.md";
    string outputPath = Path.Combine(outputDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, bytes);
    resources.Add(new CapturedResource(
        target.Id,
        uri,
        mediaType,
        relativePath,
        bytes.LongLength,
        Convert.ToHexStringLower(SHA256.HashData(bytes)),
        target.TypeName,
        target.FieldName));
}

string assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? assembly.GetName().Version?.ToString()
    ?? "unknown";
CaptureProvenance provenance = new(
    clioCommit,
    assemblyVersion,
    "get-guidance",
    Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(toolContractPath))),
    $"dotnet run --project automation/Clio.Knowledge.OracleCapture -- \"{assemblyPath}\" {clioCommit} \"{toolContractPath}\" \"{outputDirectory}\"",
    resources);
byte[] provenanceBytes = JsonSerializer.SerializeToUtf8Bytes(provenance, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
});
File.WriteAllBytes(Path.Combine(outputDirectory, "provenance.json"), provenanceBytes);
Console.WriteLine($"Captured {resources.Count} guidance resources from Clio {clioCommit} ({assemblyVersion}).");
return 0;

static object ReadStaticField(Assembly assembly, string typeName, string fieldName)
{
    Type type = assembly.GetType(typeName, throwOnError: true)!;
    FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeName, fieldName);
    return field.GetValue(null) ?? throw new InvalidDataException($"{typeName}.{fieldName} returned null.");
}

static string ReadStringProperty(object value, Type valueType, string propertyName) =>
    valueType.GetProperty(propertyName)?.GetValue(value) as string
    ?? throw new InvalidDataException($"{valueType.FullName}.{propertyName} was missing or null.");

static string CanonicalizeText(string value) =>
    value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimStart('\uFEFF');

internal sealed record CaptureTarget(string Id, string TypeName, string FieldName);

internal sealed record CapturedResource(
    string Id,
    string Uri,
    string MediaType,
    string Path,
    long Length,
    string Sha256,
    string SourceType,
    string SourceField);

internal sealed record CaptureProvenance(
    string ClioCommit,
    string ClioAssemblyVersion,
    string McpTool,
    string McpToolContractSourceSha256,
    string CaptureCommand,
    IReadOnlyList<CapturedResource> Resources);

internal sealed class ClioLoadContext(string assemblyPath) : AssemblyLoadContext(isCollectible: true), IDisposable
{
    private readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    public void Dispose() => Unload();
}

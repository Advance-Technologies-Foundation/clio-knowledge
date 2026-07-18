using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length != 4)
{
    Console.Error.WriteLine(
        "Usage: Clio.Knowledge.OracleCapture <clio.dll> <clio-commit> <clio-repository-root> <output-directory>");
    return 2;
}

string assemblyPath = Path.GetFullPath(args[0]);
string clioCommit = args[1];
string clioRepositoryRoot = Path.GetFullPath(args[2]);
string outputDirectory = Path.GetFullPath(args[3]);
string toolContractPath = Path.Combine(
    clioRepositoryRoot,
    "clio",
    "Command",
    "McpServer",
    "Tools",
    "GuidanceGetTool.cs");
if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Clio assembly was not found: {assemblyPath}");
    return 2;
}
if (!Directory.Exists(clioRepositoryRoot))
{
    Console.Error.WriteLine($"Clio repository root was not found: {clioRepositoryRoot}");
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
IReadOnlyList<CatalogEntry> catalogEntries = ReadCatalog(assembly);
HashSet<string> routingReferences = ReadRoutingReferences(catalogEntries);

List<CapturedResource> resources = [];
foreach (CatalogEntry entry in catalogEntries.OrderBy(entry => entry.Id, StringComparer.Ordinal))
{
    string text = CanonicalizeText(ReadStringProperty(entry.Article, "Text"));
    byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
    string relativePath = $"resources/{entry.Id}.md";
    string outputPath = Path.Combine(outputDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, bytes);

    SourceLocation source = FindSourceLocation(assembly, entry.Article, clioRepositoryRoot);
    resources.Add(new CapturedResource(
        entry.Id,
        entry.Description,
        ReadStringProperty(entry.Article, "Uri"),
        ReadStringProperty(entry.Article, "MimeType"),
        relativePath,
        bytes.LongLength,
        Convert.ToHexStringLower(SHA256.HashData(bytes)),
        source.TypeName,
        source.MemberName,
        source.SourcePath,
        entry.FeatureGate,
        routingReferences.Contains(entry.Id)));
}

string assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? assembly.GetName().Version?.ToString()
    ?? "unknown";
if (!assemblyVersion.EndsWith($"+{clioCommit}", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidDataException(
        $"Clio assembly version '{assemblyVersion}' does not identify requested commit '{clioCommit}'.");
}
CaptureProvenance provenance = new(
    "Advance-Technologies-Foundation/clio",
    clioCommit,
    assemblyVersion,
    "get-guidance",
    Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(toolContractPath))),
    $"dotnet run --project automation/Clio.Knowledge.OracleCapture -- \"{assemblyPath}\" {clioCommit} \"{clioRepositoryRoot}\" \"{outputDirectory}\"",
    routingReferences.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
    resources);
string provenanceJson = JsonSerializer.Serialize(provenance, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
});
byte[] provenanceBytes = Encoding.UTF8.GetBytes(CanonicalizeText(provenanceJson));
File.WriteAllBytes(Path.Combine(outputDirectory, "provenance.json"), provenanceBytes);
Console.WriteLine($"Captured {resources.Count} guidance resources from Clio {clioCommit} ({assemblyVersion}).");
return 0;

static IReadOnlyList<CatalogEntry> ReadCatalog(Assembly assembly)
{
    Type catalogType = assembly.GetType(
        "Clio.Command.McpServer.Resources.GuidanceCatalog",
        throwOnError: true)!;
    MethodInfo getNames = catalogType.GetMethod(
        "GetNames",
        BindingFlags.Static | BindingFlags.NonPublic,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null)
        ?? throw new MissingMethodException(catalogType.FullName, "GetNames()");
    MethodInfo tryGet = catalogType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(method => method.Name == "TryGet" && method.GetParameters().Length == 2);
    IEnumerable names = (IEnumerable)(getNames.Invoke(null, null)
        ?? throw new InvalidDataException("GuidanceCatalog.GetNames() returned null."));

    List<CatalogEntry> entries = [];
    foreach (object? value in names)
    {
        string id = value as string
            ?? throw new InvalidDataException("GuidanceCatalog.GetNames() returned a non-string value.");
        object?[] parameters = [id, null];
        bool found = (bool)(tryGet.Invoke(null, parameters)
            ?? throw new InvalidDataException($"GuidanceCatalog.TryGet({id}) returned null."));
        object entry = found && parameters[1] is not null
            ? parameters[1]!
            : throw new InvalidDataException($"GuidanceCatalog.TryGet({id}) did not resolve its registered entry.");
        Type entryType = entry.GetType();
        object article = ReadProperty(entry, entryType, "Article");
        Type? featureGateType = entryType.GetProperty("FeatureGateType")?.GetValue(entry) as Type;
        entries.Add(new CatalogEntry(
            id,
            ReadStringProperty(entry, "Description"),
            article,
            ReadFeatureGate(featureGateType)));
    }
    return entries;
}

static HashSet<string> ReadRoutingReferences(IEnumerable<CatalogEntry> entries)
{
    CatalogEntry routing = entries.Single(entry => entry.Id == "routing");
    string text = ReadStringProperty(routing.Article, "Text");
    return Regex.Matches(text, @"\bname=([a-z0-9][a-z0-9-]*)", RegexOptions.CultureInvariant)
        .Select(match => match.Groups[1].Value)
        .ToHashSet(StringComparer.Ordinal);
}

static SourceLocation FindSourceLocation(Assembly assembly, object article, string clioRepositoryRoot)
{
    Type articleType = article.GetType();
    foreach (Type type in assembly.GetTypes()
        .Where(type => type.Namespace?.StartsWith("Clio.Command.McpServer.Resources", StringComparison.Ordinal) == true)
        .OrderBy(type => type.FullName, StringComparer.Ordinal))
    {
        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!articleType.IsAssignableFrom(field.FieldType))
            {
                continue;
            }
            object? candidate = field.GetValue(null);
            if (ReferenceEquals(candidate, article))
            {
                return new SourceLocation(
                    type.FullName!,
                    field.Name,
                    FindSourcePath(clioRepositoryRoot, type.Name));
            }
        }
    }

    const string generatedCatalogType = "Clio.Command.McpServer.Resources.ComposableAppSkillResourceCatalog";
    return new SourceLocation(
        generatedCatalogType,
        "GetGuides",
        FindSourcePath(clioRepositoryRoot, "ComposableAppSkillResourceCatalog"));
}

static string FindSourcePath(string clioRepositoryRoot, string typeName)
{
    string resourcesRoot = Path.Combine(clioRepositoryRoot, "clio", "Command", "McpServer", "Resources");
    Regex declaration = new(
        $@"\b(?:class|record|struct)\s+{Regex.Escape(typeName)}\b",
        RegexOptions.CultureInvariant);
    string sourcePath = Directory.EnumerateFiles(resourcesRoot, "*.cs", SearchOption.AllDirectories)
        .FirstOrDefault(path => declaration.IsMatch(File.ReadAllText(path)))
        ?? throw new FileNotFoundException($"Could not locate source for {typeName} under {resourcesRoot}.");
    return Path.GetRelativePath(clioRepositoryRoot, sourcePath).Replace('\\', '/');
}

static string? ReadFeatureGate(Type? featureGateType)
{
    if (featureGateType is null)
    {
        return null;
    }
    CustomAttributeData attribute = featureGateType.CustomAttributes.Single(candidate =>
        candidate.AttributeType.FullName == "Clio.Command.FeatureToggleAttribute");
    return attribute.ConstructorArguments.Single().Value as string
        ?? throw new InvalidDataException($"{featureGateType.FullName} has an invalid feature toggle.");
}

static object ReadProperty(object value, Type valueType, string propertyName) =>
    valueType.GetProperty(propertyName)?.GetValue(value)
    ?? throw new InvalidDataException($"{valueType.FullName}.{propertyName} was missing or null.");

static string ReadStringProperty(object value, string propertyName) =>
    value.GetType().GetProperty(propertyName)?.GetValue(value) as string
    ?? throw new InvalidDataException($"{value.GetType().FullName}.{propertyName} was missing or null.");

static string CanonicalizeText(string value) =>
    value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimStart('\uFEFF');

internal sealed record CatalogEntry(
    string Id,
    string Description,
    object Article,
    string? FeatureGate);

internal sealed record SourceLocation(
    string TypeName,
    string MemberName,
    string SourcePath);

internal sealed record CapturedResource(
    string Id,
    string Description,
    string Uri,
    string MediaType,
    string Path,
    long Length,
    string Sha256,
    string SourceType,
    string SourceMember,
    string SourcePath,
    string? FeatureGate,
    bool Routed);

internal sealed record CaptureProvenance(
    string ClioRepository,
    string ClioCommit,
    string ClioAssemblyVersion,
    string McpTool,
    string McpToolContractSourceSha256,
    string CaptureCommand,
    IReadOnlyList<string> RoutingReferences,
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

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

[TestFixture]
public sealed class DistributionPackageCompatibilityTests
{
    [Test]
    [NonParallelizable]
    [Description("Rejects an ordinary package build because the POC artifact uses a publicly committed disposable signing key.")]
    public async Task DistributionPackage_ShouldRejectPack_WhenTestSigningOptInIsMissing()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(
            repositoryRoot,
            "distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj");
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--verbosity");
        startInfo.ArgumentList.Add("quiet");

        // Act
        using Process process = new() { StartInfo = startInfo };
        process.Start().Should().BeTrue(because: "the installed dotnet SDK must start the package build");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(1));
        await process.WaitForExitAsync(timeout.Token);
        string buildOutput = (await standardOutput) + Environment.NewLine + await standardError;

        // Assert
        process.ExitCode.Should().NotBe(0,
            because: "ordinary package publication must fail closed while the bundle uses a public test key");
        buildOutput.Should().Contain("AllowTestSignedPackage=true",
            because: "the failure must explain the explicit POC-only opt-in");
    }

    [Test]
    [NonParallelizable]
    [Description("Packs the real distribution project and verifies Clio can discover its stable version and exact inner bundle path.")]
    public async Task DistributionPackage_ShouldMatchClioConsumerContract_WhenActuallyPacked()
    {
        // Arrange
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(
            repositoryRoot,
            "distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj");
        string outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "clio-knowledge-tests",
            $"package-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--verbosity");
        startInfo.ArgumentList.Add("quiet");
        startInfo.ArgumentList.Add("-p:AllowTestSignedPackage=true");

        try
        {
            // Act
            using Process process = new() { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("The installed dotnet SDK did not start the real package build.");
            }
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(1));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("The real Clio knowledge package build exceeded one minute.");
            }
            string buildOutput = (await standardOutput) + Environment.NewLine + await standardError;

            // Assert
            process.ExitCode.Should().Be(0,
                because: $"the real distribution project must pack successfully: {buildOutput}");
            string packagePath = Directory.EnumerateFiles(outputDirectory, "*.nupkg").Should()
                .ContainSingle(because: "one distribution project must produce exactly one transport package")
                .Which;
            using ZipArchive package = ZipFile.OpenRead(packagePath);
            ZipArchiveEntry payload = package.Entries.Should()
                .ContainSingle(
                    entry => string.Equals(
                        entry.FullName,
                        "content/knowledge-bundle.zip",
                        StringComparison.Ordinal),
                    because: "Clio extracts only the exact content/knowledge-bundle.zip transport path")
                .Which;
            byte[] payloadBytes = ReadEntry(payload);
            ZipArchiveEntry nuspec = package.Entries.Should()
                .ContainSingle(
                    entry => entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal),
                    because: "every valid NuGet package must carry exactly one metadata document")
                .Which;
            using Stream nuspecStream = nuspec.Open();
            XDocument metadata = XDocument.Load(nuspecStream);
            XNamespace ns = metadata.Root!.Name.Namespace;
            string packageVersion = metadata.Root.Element(ns + "metadata")!.Element(ns + "version")!.Value;
            packageVersion.Should().Be("1.3.0",
                because: "Clio deliberately ignores prerelease knowledge transport versions");
            using MemoryStream payloadStream = new(payloadBytes);
            using ZipArchive bundle = new(payloadStream, ZipArchiveMode.Read);
            ZipArchiveEntry? manifest = bundle.GetEntry("manifest.json");
            manifest.Should().NotBeNull(
                because: "the signed bundle must contain the manifest Clio validates");
            using JsonDocument manifestDocument = JsonDocument.Parse(ReadEntry(manifest!));
            manifestDocument.RootElement.GetProperty("libraryVersion").GetString().Should().Be(packageVersion,
                because: "Clio binds a NuGet transport version to the signed library version");
            manifestDocument.RootElement.TryGetProperty("issuedAt", out _).Should().BeFalse(
                because: "the immutable source commit already carries the publication timestamp");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using MemoryStream output = new();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "bundle-source.json")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the clio-knowledge repository root.");
    }
}

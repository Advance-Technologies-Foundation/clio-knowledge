# Distribution experiments

The first experiment uses NuGet only as the delivery envelope. The signed knowledge bundle remains
the feed-portable trust and compatibility boundary.

Build the package:

```powershell
dotnet pack distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj -c Release -o artifacts
```

Then use `Clio.Knowledge.NuGetSpike` to discover the newest package through the NuGet Client SDK,
download it in-process, and extract the signed inner bundle without requiring the `dotnet` or
`nuget` CLI in the consuming process.

## 2026-07-18 local-feed result

The spike packed `ATF.Clio.Knowledge 0.1.0-alpha.1`, discovered it from a local NuGet folder feed
through `FindPackageByIdResource`, downloaded it through `CopyNupkgToStreamAsync`, and extracted
`knowledge/knowledge.bundle.zip` in-process.

- NuGet package SHA-256: `4d48f1e712b574cca26d4432401c39590e618bf5ae8ffe742e28b862235ae077`
- Source bundle SHA-256: `71fd0bc810a1c46dc522db1b5dceb30338c2d0697604a0936774d06cfdfd2e4a`
- Extracted bundle SHA-256: `71fd0bc810a1c46dc522db1b5dceb30338c2d0697604a0936774d06cfdfd2e4a`

This proves the native .NET discovery/download/extraction path and exact payload preservation. It
does not yet prove remote-feed authentication, feed-specific signature enforcement, or offline
global-package-cache behavior; those remain explicit follow-up experiments.

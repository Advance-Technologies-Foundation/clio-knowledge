# Distribution experiments

The first experiment uses NuGet only as one delivery envelope. The signed, library-scoped knowledge
bundle remains the transport-independent trust, identity, and compatibility boundary. A transport
package version is retrieval state; it does not replace `(libraryId, sequence, bundleDigest)`.

Build the package:

```powershell
dotnet pack distribution/Clio.Knowledge.Package/Clio.Knowledge.Package.csproj -c Release -o artifacts -p:AllowTestSignedPackage=true
```

The explicit property acknowledges that this experimental package uses the repository's public
test signing key. It is for approved internal POC publication only; a production pipeline must use
an externally protected production signing key and must not rely on this override.

Then use `Clio.Knowledge.NuGetSpike` to discover the newest package through the NuGet Client SDK,
download it in-process, and extract the signed inner bundle without requiring the `dotnet` or
`nuget` CLI in the consuming process.

## Historical 2026-07-18 v0 local-feed result

The spike packed `ATF.Clio.Knowledge 0.1.0-alpha.1`, discovered it from a local NuGet folder feed
through `FindPackageByIdResource`, downloaded it through `CopyNupkgToStreamAsync`, and extracted
`knowledge/knowledge.bundle.zip` in-process.

- NuGet package SHA-256: `4d48f1e712b574cca26d4432401c39590e618bf5ae8ffe742e28b862235ae077`
- Source bundle SHA-256: `71fd0bc810a1c46dc522db1b5dceb30338c2d0697604a0936774d06cfdfd2e4a`
- Extracted bundle SHA-256: `71fd0bc810a1c46dc522db1b5dceb30338c2d0697604a0936774d06cfdfd2e4a`

This proves the native .NET discovery/download/extraction path and exact payload preservation. It
does not yet prove remote-feed authentication, feed-specific signature enforcement, or offline
global-package-cache behavior; those remain explicit follow-up experiments.

The package build generates the v1 `com.creatio.clio` bundle into its intermediate output directory
and packages it at `content/knowledge-bundle.zip` under transport version `1.3.0`. Git does not use
that archive; it reads the checked-out repository directly. Generated ZIP files are never committed.

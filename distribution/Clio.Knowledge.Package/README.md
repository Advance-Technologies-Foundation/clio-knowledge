# ATF.Clio.Knowledge

Experimental transport package for the Clio external-knowledge proof of concept.

The NuGet package is only a delivery envelope. `content/knowledge-bundle.zip` declares the
`com.creatio.clio` library, its readable library version, forward-only library-scoped sequence,
exact item routes, compatibility metadata, and resource digests. Clio must verify that inner bundle
before activation. The package version is bound to the signed `libraryVersion` for NuGet delivery,
while immutable knowledge identity remains `(libraryId, sequence, bundleDigest)`. The public trust
key is deliberately not shipped in this package.

The packed payload is generated into the package project's intermediate output directory. Git
transport reads repository source files directly and does not consume this archive. The stable
`1.2.0` transport version and exact inner path match Clio's NuGet discovery and extraction contract.

This POC artifact is signed by a publicly committed disposable test key. Packing is blocked by
default to prevent accidental publication. An explicitly approved internal experiment must opt in
with `-p:AllowTestSignedPackage=true`; production publication must use an external production key
and remove this test-only override.

# ATF.Clio.Knowledge

Experimental transport package for the Clio external-knowledge proof of concept.

The NuGet package is only a delivery envelope. `knowledge/knowledge.bundle.zip` retains its own
signed manifest, compatibility metadata, forward-only sequence, and resource digests. Clio must
verify that inner bundle before activation. The public trust key is deliberately not shipped in
this package.

# Clio guidance migration inventory

MIG0 freezes the complete guidance surface served by Clio commit
`baa34546589413aa898429051d1702442bbd2dd2`. This commit is the immutable source baseline for
the first migration wave and is also the parent of Clio's external-knowledge delivery prototype.

## Authoritative inventory

[`fixtures/oracles/clio-guidance-v0/provenance.json`](../fixtures/oracles/clio-guidance-v0/provenance.json)
is the machine-readable inventory. It records every `GuidanceCatalog` entry exactly once, including:

- stable guide ID and `docs://` URI;
- description, media type, byte length, and SHA-256 digest;
- Clio source type, member, and repository-relative source path;
- feature-toggle key when present;
- whether the routing article references the guide.

The corresponding canonical UTF-8/LF bytes are under
`fixtures/oracles/clio-guidance-v0/resources/`. These files are immutable migration evidence, not
the authoring location for future guidance.

[`guidance-partitions.json`](guidance-partitions.json) assigns every frozen guide to exactly one
migration slice. The five ESQ guides are already present under `guidance/`; `core-rules` and
`routing` remain deliberately deferred for a separate safety and routing-contract review.

[`clio-content-test-disposition.md`](clio-content-test-disposition.md) classifies the Clio tests
that assert guidance wording or per-article content. MIG6 uses this inventory to remove or replace
those assertions while retaining synthetic delivery-mechanics coverage in Clio.

## Reproducing the oracle

1. Create a detached worktree for the baseline commit under the Clio repository root:

   ```powershell
   git -C C:\Projects\clio worktree add --detach `
     C:\Projects\clio\.worktrees\knowledge-mig0-oracle `
     baa34546589413aa898429051d1702442bbd2dd2
   ```

2. Build the exact Clio source:

   ```powershell
   dotnet build C:\Projects\clio\.worktrees\knowledge-mig0-oracle\clio\clio.csproj `
     -c Release -f net10.0 -p:NuGetAudit=false
   ```

3. From the clio-knowledge repository, capture the catalog through reflection:

   ```powershell
   dotnet run --project automation\Clio.Knowledge.OracleCapture\Clio.Knowledge.OracleCapture.csproj `
     -c Release -- `
     C:\Projects\clio\.worktrees\knowledge-mig0-oracle\clio\bin\Release\net10.0\clio.dll `
     baa34546589413aa898429051d1702442bbd2dd2 `
     C:\Projects\clio\.worktrees\knowledge-mig0-oracle `
     fixtures\oracles\clio-guidance-v0
   ```

The capture utility calls the compiled internal `GuidanceCatalog`, so generated composable-app
guides and feature-gated entries are included without maintaining a second hard-coded guide list.
It then maps each returned article back to its Clio source member and extracts routing references
from the frozen routing article. The baseline contains 48 `*GuidanceResource.cs` source files plus
the generated `ComposableAppSkillGuidanceResources.cs` catalog; every one is represented in the
oracle.

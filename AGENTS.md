# Agent instructions

`AGENTS.md` is the authoritative instruction file for coding agents working in this repository.

## Repository purpose

This repository is the knowledge control plane for Clio. It owns guidance content, advisories, capability identifiers, reference-example metadata, validation contracts, and publication automation. It does not own Clio executable behavior or complete reference workspaces.

## Working rules

- Read `README.md` and `CONTRIBUTING.md` before making a change.
- Keep content human-readable in source control. Markdown, YAML, and JSON are preferred source formats.
- Do not add complete reference workspaces or copied application source. Reference immutable releases or commits in the catalog.
- Do not duplicate a rule across guidance, routing, and advisories. Give each rule one authoritative owner and reference its stable ID elsewhere.
- Treat guidance as agent instruction supply-chain content. Never introduce instructions copied from an untrusted repository without review and provenance.
- Never commit credentials, connection strings, tokens, customer information, or environment-specific secrets.
- Do not claim behavior is verified without identifying its evidence and applicable versions.
- Distinguish canonical guidance, tested reference patterns, observed implementations, experiments, limitations, and hazards.
- Preserve stable IDs. Renaming a title must not silently change an article, capability, advisory, or reference-example identity.
- Use immutable repository tags or commit hashes for published reference metadata. Never publish a mutable default branch as verified evidence.
- Keep compatibility declarations explicit. Guidance that references a Clio tool must declare a compatible tool contract or Clio version.
- Keep examples independent. Catalog changes must not require cloning every reference repository to consume ordinary guidance.
- Prefer small, reviewable content changes. Do not mix a schema redesign with unrelated guidance edits.
- Create linked worktrees only under this repository's `.worktrees/` directory. Never create a
  sibling worktree elsewhere on disk.

## Validation expectations

Until automated validation is implemented, review changes manually for:

- technical correctness and unambiguous language;
- stable and unique identifiers;
- valid internal links and catalog references;
- explicit applicability and version boundaries;
- evidence for prescriptive or safety-critical claims;
- absence of contradictory sources of truth;
- clear separation between guidance and example-specific decisions.

When automation is added, run the narrowest relevant validation before committing and record the result in the change description.

## Delivery status

This is the real authoring repository, but the v0 bundle delivery path is not production-ready until
its publication and signing decisions are ratified. Do not present a proposed schema as stable or
production-ready unless the discussion records that decision. Do not modify the Clio repository
from this repository without an explicitly scoped coordinated task.

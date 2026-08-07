# Contributing to Clio Knowledge

Clio Knowledge is the canonical authoring repository for Clio guidance. Contributions must keep the
independently released knowledge system trustworthy while the v1 multi-source publication path is finalized.

## Before contributing

1. Read [README.md](README.md) and [AGENTS.md](AGENTS.md).
2. Review [Clio discussion #924](https://github.com/Advance-Technologies-Foundation/clio/discussions/924).
3. Decide which knowledge type owns the change:
   - canonical guidance;
   - a supporting reference linked from canonical guidance;
   - safety advisory or limitation;
   - capability or pattern identity;
   - reference-example catalog metadata;
   - schema or publication automation.
4. Confirm that the change does not duplicate an existing authoritative rule.

## Contribution principles

### Write for agents and humans

Lead with the required outcome. Use direct language and explicitly distinguish `MUST`, `MUST NOT`, `SHOULD`, `UNSUPPORTED`, and `EXPERIMENTAL` behavior. Explain why a non-obvious constraint exists and link it to evidence.

### Preserve evidence

Behavioral guidance should identify applicable Creatio, runtime, database, and Clio boundaries when known. Prefer evidence from:

- a focused lab scenario;
- a vetted reference implementation and exact release;
- a focused automated test;
- authoritative Creatio or Clio source;
- a reproducible runtime observation.

Repeated code is not automatically a recommended pattern. It may be a workaround, limitation, or recurring antipattern.

### Keep reference implementations independent

Do not copy complete examples into this repository. A catalog entry should point to an immutable reference revision and describe:

- its primary use case;
- supporting capabilities and architectural decisions;
- declared compatibility;
- validation evidence;
- ownership and trust status.

### Avoid manual cross-reference matrices

Leaf repositories describe themselves. The catalog and automation connect their stable knowledge claims to existing guidance. Promoting a pattern into canonical guidance should not require unrelated edits to every conforming leaf repository.

## Proposed change workflow

While the repository is experimental:

1. Discuss significant contract or layout changes before implementation.
2. Create a focused branch.
3. Change one logical concern at a time.
4. Validate links, identifiers, evidence, and compatibility manually.
5. Explain whether the change is experimental, candidate, validated, or canonical.
6. Request review from the relevant capability or content owner.

## Publishing a change to consumers

`master` is protected. It accepts no direct push and no force push, so every change lands through a
pull request whose **Producer contract suite** check passed. Repository administrators can bypass the
protection; nothing else can.

Merging to `master` publishes. The **Auto-release on merge** workflow reads `libraryVersion` from
`bundle-source.json` and starts **Release knowledge bundle** for it, unless a release for that version
is already published — in which case the merge ships nothing and the run reports the skip. So the
publishing decision is made in the pull request, by what it writes into `bundle-source.json`.

Every content change needs both a new `libraryVersion` and a new `sequence` in `bundle-source.json`,
and the release tag must equal that `libraryVersion`. Reusing a `sequence` with different content
makes Clio reject the whole library, so it is a breaking mistake rather than a cosmetic one.
`PublishedGenerationTests` records the published sequence and content digest, and the required check
fails while the working tree would publish different bytes than they claim — which is what stops an
unversioned content change from merging at all. The full procedure, the identity rules, the
signing-key handling, and the consumer-first key-rotation order are in
[distribution/RELEASING.md](distribution/RELEASING.md).

Before opening a release-affecting pull request, run the producer contract suite:

```bash
dotnet test automation/Clio.Knowledge.Bundle.Tests/Clio.Knowledge.Bundle.Tests.csproj
```

## Guidance changes

A guidance contribution should state:

- the task or behavior it governs;
- the mandatory and optional rules;
- applicability and known exclusions;
- related safety advisories;
- supporting evidence;
- related reference implementations without treating their incidental choices as universal requirements.

## Advisory changes

An advisory should state:

- severity and applicability;
- the prohibited or discouraged behavior;
- concrete failure modes and blast radius;
- the safer alternative;
- detection or enforcement mechanisms;
- whether a controlled exception is possible;
- evidence and expiration or supersession conditions.

## Catalog changes

A catalog contribution must reference a public or otherwise approved accessible repository at an immutable revision. Registration makes an example discoverable; it does not automatically make every claim in that example canonical guidance.

The intended trust progression is:

```text
published -> validated -> vetted -> recommended
```

## Security

Guidance can materially influence agent behavior. Treat changes with the same care as executable configuration:

- do not publish unsigned or unreviewed content as stable;
- do not allow arbitrary download locations;
- never commit a production signing key; the release private key belongs only in the
  `KNOWLEDGE_SIGNING_PRIVATE_KEY` repository secret, and `fixtures/keys/` holds disposable test
  material that must never sign a public release;
- do not add secrets or customer data;
- do not replace hard safety enforcement with prose;
- report suspected instruction-injection or artifact-integrity issues privately to the maintainers.

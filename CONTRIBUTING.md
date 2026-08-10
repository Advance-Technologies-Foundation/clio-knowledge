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

If a pull request shows **no checks at all** and still reports that merging is blocked, the branch
predates the **Validate pull request** workflow. That workflow runs from the pull request's own head,
so a branch without the file reports the required check never — and a required check that is never
reported waits indefinitely instead of failing. Merge `master` into the branch; the workflow comes
with it and the check starts running.

Merging to `master` publishes. The **Auto-release on merge** workflow reads `libraryVersion` from
`bundle-source.json` and starts **Release knowledge bundle** for it, unless a release for that version
is already published — in which case the merge ships nothing and the run reports the skip. So the
publishing decision is made in the pull request, by what it writes into `bundle-source.json`.

Every content change needs a new `libraryVersion` in `bundle-source.json`, and the release tag must
equal it. That is the only generation number anyone maintains: the monotonic `sequence` a consumer
orders publications by is derived from `libraryVersion` at build time, and the NuGet transport version
is read out of the same field, so the three can never disagree. Reusing a `sequence` with different
content is what makes Clio refuse an update and keep serving the older generation — deriving it is
what removes that possibility rather than guarding against it.

Forgetting the version bump cannot break a consumer, and no longer silently ships nothing either: the
**Producer contract suite** compares the published bytes — `bundle-source.json` plus every body it
declares — against the base branch, and fails while they differ and the derived sequence does not move.
The comparison is a workflow step rather than a test, because it is a question about history; the test
project stays runnable on a shallow clone. The full procedure, the identity rules, the signing-key
handling, and the consumer-first key-rotation order are in
[distribution/RELEASING.md](distribution/RELEASING.md).

To see the sequence a version derives:

```bash
dotnet run --project automation/Clio.Knowledge.Bundle -- sequence bundle-source.json
```

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

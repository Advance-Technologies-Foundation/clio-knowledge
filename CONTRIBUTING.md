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

## Reviewing a change under a stable ID

A published item keeps its `itemId`, `topicId`, `uri` and `legacyUris` while its content changes, so
its identity never tells a reader that the instructions behind it moved. Review classifies every change
to a published body or its `bundle-source.json` entry. These rules govern the v1 candidate contract and
change with it; they do not declare that contract stable.

### Editorial or behavioral

Decision test: would an agent following the old text and one following the new text call different
tools, pass different parameters or values, act in a different order, proceed where the other stops or
asks, or apply the rule to different Creatio or Clio versions? If yes, or if you cannot tell, the
change is **behavioral**; otherwise it is **editorial**.

| Editorial | Behavioral |
|---|---|
| Fixing a typo, grammar, or Markdown formatting. | Adding, removing, or reversing a `MUST`, `MUST NOT`, `SHOULD`, `UNSUPPORTED`, or `EXPERIMENTAL`. |
| Reordering sections within one article without changing any rule. | Renaming a tool or parameter, or changing a value, an order of operations, or a stop condition. |
| Replacing an example with one that leads to the same calls. | Moving a version boundary, an applicability limit, a safety precondition, or a routing target. |

Borderline cases are behavioral: rewording `MUST` as `SHOULD` lets an agent skip a step it could not
skip before; deleting the reason for a constraint lets an agent decide it does not apply; changing the
only example that shows a parameter value changes the value agents copy.

The pull request description states the kind; a pull request mixing both is reviewed as behavioral.
Both kinds change published bytes and need the bump in
[Publishing a change to consumers](#publishing-a-change-to-consumers): the kind decides the review
evidence, not whether the change ships.

### What a behavioral change must state

The pull request description states, for each behavioral change:

- **Applicability**: the Creatio versions, runtime, database, and product boundaries it holds for,
  and the known exclusions; a boundary nobody has established is stated as `unknown`, not omitted.
- **Compatibility**: the Clio version or MCP tool contract it needs. When that is newer than
  `compatibility.clio.min` in `bundle-source.json`, the article states the boundary inline next to the
  instruction it gates (for example `clio 8.1.0.136 or later`).
- **Evidence**: a source of a kind listed in [Preserve evidence](#preserve-evidence), identified
  precisely enough to re-check, for example a test name, a source path at a commit, or a reference
  implementation at its exact release.

A reviewer MUST NOT approve a behavioral change whose evidence is missing or does not cover the stated
applicability. The reviewer asks for the evidence, for the change to be narrowed to what the evidence
covers, or for the unproven part to be labeled `EXPERIMENTAL`. An editorial change needs none of these;
the reviewer confirms only that the decision test holds.

### Identity

[AGENTS.md](AGENTS.md#working-rules) owns the stable-ID rule; review applies it as follows. Renaming a
`title`, a heading, or a source file changes the same item, so `itemId`, `topicId`, `uri`, and
`legacyUris` stay as they were. A new `itemId` is only for a genuinely new item: a subject no published
item owns, or a piece split out of an existing article; an end-to-end rewrite under a new title keeps
its identity. A split or move follows
[An article must fit in one `get-guidance` response](#an-article-must-fit-in-one-get-guidance-response).
The same test applies to capability, advisory, and reference-example IDs: renaming their title or
file keeps the ID, and a new ID is only for a new subject.

### Supersession and references

The v1 contract has no `supersedes`, `replaces`, or `affects` field: the `schemas/v1/` resource
contract and the builder both reject unknown properties. Adding one is a schema change in its own pull
request; until then, record the relationship in the pull request description and the article text.
Supersession and retirement are behavioral.

- **A rule moves to another item**: the item that loses it drops the rule and cites the new owner as
  described under Citations below; the pull request names both items.
- **An item is retired**: the pull request names it and its successor, removes it from `resources`,
  `requirements.itemIds`, and `requirements.resourceUris`, rewrites every `name=` or
  `docs://knowledge/` citation of it to the successor, and moves its `legacyUris` to the successor so
  pre-v1 `docs://mcp/...` routes keep resolving. The retired `itemId` and `uri` cannot be
  aliased: Clio resolves a `docs://knowledge/` URI by exact `itemId` and a bare name by `itemId` or
  `topicId`, never through `legacyUris` (`clio/Command/McpServer/Knowledge/KnowledgeResolution.cs` in
  the Clio repository, read at commit `7ab76575`; the behavior is unchanged from Clio 8.1.0.97 to
  8.1.0.139), so listing them in `legacyUris` passes the builder and does nothing. The pull request
  states that retirement breaks callers of the old ID. Clio's own source names the required inventory in
  [README.md](README.md#status) (`core-rules`, `routing`, `when-to-use-requests`), so those items are
  never retired or renamed without a coordinated Clio change.
- **Citations**: cite a `guidance` item by `get-guidance name=<itemId>` and an item of any other role
  by its canonical `docs://knowledge/...` `uri`, never by title. Clio resolves a bare name only for the
  `guidance` role, so `name=` finds no `reference` or `reference-example` item. An advisory names each
  item it affects the same way, next to the supersession condition that
  [Advisory changes](#advisory-changes) asks for.

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
`bundle-source.json` and starts **Release knowledge bundle** for it. What happens when that version is
already published — a clean skip when no published body moved, a failing run until a bump-only pull
request lands when one did — is stated once in
[`distribution/RELEASING.md`](distribution/RELEASING.md#who-can-publish-and-from-where), which owns
that contract. So the publishing decision is made in the pull request, by what it writes into
`bundle-source.json`.

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
project stays runnable on a shallow clone.

The same check also refuses a published body that still carries an unresolved version boundary — a
`<...TBD>` placeholder standing in for a release that does not exist yet. Writing one while an article
is being drafted is fine and keeps `dotnet test` green; merging it is not, because merging publishes,
and an agent reading the placeholder cannot tell whether the tool it gates is available on the
environment it is about to write to. Replace it with the released version before asking for a merge.

The full procedure, the identity rules, the signing-key
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

That reports pass or fail. It does NOT print the per-article size table, and that table is the only
early warning there is: an article between 80% and 100% of the response budget is green, by design,
because gating that band blocked open pull requests that were doing nothing wrong. The table is
written to the test output and needs a logger the default command does not attach, so an article
sliding toward the limit is invisible under the line above. When you touch a process guide, run it
this way and read the `of budget` lines -- an article marked `approaching the budget` should be split
before it is grown further:

```bash
dotnet test automation/Clio.Knowledge.Bundle.Tests/Clio.Knowledge.Bundle.Tests.csproj --logger "console;verbosity=detailed"
```

## Guidance changes

A guidance contribution should state:

- the task or behavior it governs;
- the mandatory and optional rules;
- applicability and known exclusions;
- related safety advisories;
- supporting evidence;
- related reference implementations without treating their incidental choices as universal requirements.

### An article must fit in one `get-guidance` response

An article is delivered as a single JSON line. Past a size limit the caller cannot accept it, the
payload is spilled to a file, and because that file has one line it cannot be paged either — so the
agent greps a fragment and which fragment it gets decides the answer. `get-guidance` takes only
`name`; there is no `section` and no `offset`. Correct, reviewed, evidence-backed text in an
oversized article simply does not reach its reader, and nothing about the failure looks like a
failure.

So size is a delivery contract, not a matter of taste:

- Keep every article inside the budget in
  `automation/Clio.Knowledge.Bundle.Tests/ProcessGuideResponseSizeTests.cs`, which is the enforcing
  check. Measure the JSON-escaped payload, not the file on disk — escaping inflates a
  backtick-dense article by 20–35%.
- When an article outgrows the budget, split it at a section boundary rather than trimming
  evidence. Give each piece its own `itemId` in `bundle-source.json`, keep the original `itemId`,
  `uri` and `legacyUris` on the piece that stays the entry point, and have that entry index the
  others.
- State in each new article which rules it is the authoritative owner of, and cite sibling articles
  by NAME rather than repeating their rules — the one-owner rule in `AGENTS.md` applies across the
  split exactly as it does within one article.
- Rewrite every "see the section below" that now crosses an article boundary. A citation that names
  no article still reads as a complete instruction while withholding the rule it points at, which is
  invisible at review time; `ProcessGuideCrossReferenceTests` scans for it.
- Never let a split separate a destructive or irreversible operation from its preconditions. Routing
  sends an agent to ONE article and the premise is that it reads that article whole, so an instruction
  to remove, clear or overwrite something in a live customer environment must carry its preconditions
  where the instruction is — restate them inline as a MUST and cite the owning article for the detail.
  Keeping the rule in one place is right; leaving the reader to discover that it exists is not.

### The process digest

`process-digest` (`guidance/mcp/guides/processes/digest.md`) is the build CARD for the process domain.
It owns no rule: it restates, in short form and naming the owner on every block, what every process
build and edit needs from `process-modeling`, `process-element-catalog`, `process-naming` and
`process-sub-process-when`, so that an agent reads it INSTEAD of those four for an ordinary build.
Those four were mandatory reading before any element article, roughly 91 KB per build, and the
context that consumed was what pushed process runs into compaction (ENG-99970).

A restatement drifts unless something ties it to its owner, so three rules apply:

- A pull request that changes a rule the card restates changes the card in the SAME pull request.
  `ProcessDigestTests.RestatedClauses` pins one verbatim clause per restated rule in both files; a
  reword on one side fails the suite. Add a row when you restate a new rule.
- The card stays a card. `ProcessDigestTests` holds it to a 16 500-character budget (JSON-escaped, as the bundle ships it), far under the
  article budget: detail belongs to the owner, and the card says when to open it.
- The card names only those four owners (plus `process-versions` for the edit block). Restating a
  fifth article is a decision, not an edit.

Like any restatement, a destructive or irreversible instruction on the card carries its preconditions
inline, as a MUST, next to the instruction (see "An article must fit in one `get-guidance` response").

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

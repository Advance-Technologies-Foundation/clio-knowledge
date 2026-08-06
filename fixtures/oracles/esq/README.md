# ESQ guidance oracle

The earliest capture in this repository: the exact bytes compiled Clio returned for the five ESQ
guidance articles, frozen before any content moved. It is a strict subset of
[`clio-guidance-v0`](../clio-guidance-v0/README.md), which froze the complete pre-extraction
catalog, and it exists because the ESQ family was migrated first, on its own.

- `provenance.json` — stable IDs, sources, lengths and digests for the captured articles.
- `resources/` — one immutable Markdown file per captured article.

## Retired as an active guard (2026-08-06)

Nothing compares published content against these bytes any more, and nothing should. The two suites
that did — a byte-parity check over the five migrated ESQ articles, and a subset check against
`clio-guidance-v0` — were removed under ENG-94882 once Clio stopped compiling guidance at all: after
clio `aa8760da` (PR #927) `clio/Command/McpServer/Resources` holds only the resource adapter, so this
capture can never change and no future Clio revision can drift from it.

The guard had also inverted. Content ownership now sits in this repository, so every legitimate
article edit had to be added to an exemption list to keep the byte comparison green — seven entries
for a single new article in #36. Continued, that list would have covered every article, guarding
nothing while taxing every content change.

These files stay as the migration's evidence: they are what makes the claim "the extraction was
byte-preserving" checkable after the fact. Do not re-capture them, and do not build new assertions
on them.

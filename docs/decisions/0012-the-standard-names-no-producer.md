# Decision 0012: The standard names no producer

- **Decision:** 0012
- **Date:** 2026-11-06
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0012: The standard names no producer: every producer-specific file, default and promotion leaves CAI](../proposals/0012-the-standard-names-no-producer.md)
  ([issue #41](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/41))
- **Outcome:** Accepted as written, its two questions answered below.
- **Supersedes:** [decision 0008](0008-a-scanner-mapping-covers-every-rule-its-scanner-emits.md). Its diagnosis stands
  (a producer's rename made the benchmark blind to that producer's own rules); its remedy, correcting the producer's
  mapping inside CAI and making mapping coverage a CAI rule, is replaced, because it kept CAI changing whenever that
  producer changed its product. Decision 0008's mapping change never took effect in the benchmark repository.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA and the Assure, Assert and Assay products of
Canine Development, today the only producer CAI publishes. **The conflict runs against the decider's products here**:
the decision removes the advantages that producer has inside CAI (its mapping and generator in the benchmark, CAI's
vocabulary generated from its engine, defaults that trust it, a site that promotes it). Nobody else can decide today,
so nobody recused.

## Comment

None before adoption. This change moves no number (no CAI score, rubric, key or published benchmark result changes),
so under the interim rule the **14-day** comment period runs after adoption, to **2026-11-20**. An unresolved objection
from a commenter not affiliated with the proposer reopens the decision.

## Reasons

- A standard that must change when one producer renames its rules is coupled to that producer, whatever its governance
  says. The evidence is decision 0008's own: a rename by the producer blinded the benchmark to 219 of its 223 rules,
  and the only remedy available was a change inside CAI.
- The coupling was structural, not incidental: the producer's mapping and generator lived in the benchmark repository
  (scanner-benchmark `257f61f`), the generator wrote CAI's own `taxonomy.json` and `coverage/matrix.json` from that
  producer's engine, and the contract cited the producer's files as where concept scopes are defined.
- Producer defaults and promotion in CAI's code (a default producer, a built-in issuer, a cross-origin allowlist of the
  producer's sites, a main call to action leading to its product) are the same coupling in commercial form, and an
  independent standard cannot carry them.
- The neutral path already exists: the harness takes a mapping as input (`cai_bench score --mapping <file>`), so a
  producer can maintain its mapping beside its own engine and name it in what it publishes.

## Answers to the proposal's questions

1. **The steward disclosure stays in the site footer.** It is a disclosure the independence rules require, it names the
   steward rather than promoting a product, and moving it out of sight would weaken the very thing this decision is
   about. It links to Governance, not to a product.
2. **Published results stay under the name they were published with.** `results/watchdog/` is history and keeps its
   provenance; new results are filed under the producer's product name (decision 0007), as data.

## What took effect

- Proposal 0012's status, proposal 0008's status ("Superseded by 0012") and the proposal and decision indexes.
- In this repository: `Cai.Cli` has no default producer; the badge issuer catalogue has no built-in issuer; the
  cross-origin allowlist names no producer site; producer marks come from the producer listing; the site's call to
  action leads to the producer listing; the glossary, the llms text and the API reference use neutral examples.
- In the benchmark repository (code-assurance-initiative/scanner-benchmark): `mappings/watchdog.json`,
  `mappings/watchdog-build/` and `mappings/watchdog_scores.py` removed; `taxonomy.json` is CAI's own source with its
  concepts' scopes as fields (and decision 0009's 34 concepts written into it directly); `coverage/matrix.json` built by
  CAI's own tool from the taxonomy; the harness takes `--mapping` with no default and reports, per scored run, the rule
  ids that reach no concept; the contract, README, harness and tests name no producer.
- On the producer's side (not CAI's): CCA maintains its mapping, generator and score adapter in its own repository
  (`tools/bench/mapping/`) and scores with `--mapping`.

## What did not yet

- Nothing. The three changes above are released together.

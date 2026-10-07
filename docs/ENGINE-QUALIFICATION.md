# Engine qualification — how an engine earns the right to publish a CAI

**Status: draft.** This document describes how engine qualification is intended to work. It is not yet in
operation: the public benchmark sets and their scoring harness exist
([scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark)); the sealed holdout windows,
the qualification record and the thresholds described below do not. Every number marked *to be set* is decided by
the steering group ([GOVERNANCE.md](GOVERNANCE.md)) before the first window opens, and published here.

## Why qualification exists

A CAI is computed from evidence. The arithmetic is open and deterministic: the same evidence under the same rubric
version always folds to the same score. The evidence is not. It is produced by an **engine** — a scanner, an analyzer,
a pipeline of tools — that reads a codebase and reports what it found. Two engines reading the same code will never
report exactly the same findings, and the standard does not ask them to.

What a buyer of a CAI needs to know is narrower and answerable:

1. **Does the engine find enough of what is there?** (recall)
2. **Does it stay quiet about what is not?** (trap resistance and noise)
3. **Does it turn its findings into the right score?** (scoring conformance)

Qualification answers those three questions, per dimension and per language, with a dated, public record. It
qualifies **engines**. It does not certify **codebases**: a CAI remains a measurement of one codebase at one commit,
and a dispute about a finding in that codebase still goes to the issuer of that survey ([CHALLENGE.md](CHALLENGE.md)).

## Two tests, deliberately separate

| | Part A — Scoring conformance | Part B — Detection qualification |
|---|---|---|
| Question | Does the engine compute CAI correctly from evidence? | Does the engine find real defects and leave look-alikes alone? |
| Nature | Deterministic | Statistical |
| Test material | Public conformance vectors | Sealed holdout, revealed after the window |
| Pass rule | Exact reproduction | Thresholds per dimension, with confidence bounds |
| Attempts | Unlimited | One per engine version per window |
| Result | Pass / fail per rubric version | A grade and a measured noise level per dimension |

They are separate because they fail for different reasons. An engine with excellent detection can still roll its
findings up wrongly; an engine with flawless arithmetic can still miss half of what is there. Neither result may hide
the other.

## Part A — Scoring conformance

The Initiative publishes **conformance vectors** for each rubric version: evidence bundles together with the dimension
scores, lens scores, headline CAI and band that the published algorithm produces from them. They cover the ordinary
path and the edges — absent dimensions, critical gates, band boundaries, rounding.

An engine passes Part A for a rubric version when, for every vector, its own output path reproduces every dimension
score, every lens score, the headline and the band **exactly**, at the precision the specification defines. There is
no tolerance beyond that precision. An engine that uses the [reference scorer](../src/Cai.Scoring) unmodified must still
pass: the test is of the engine's output, not of the library it links.

Part A is open and repeatable because it cannot be gamed. Computing the right number is the whole of what it asks.

## Part B — Detection qualification

### The test material

Each window uses a fresh **holdout**: a set of small synthetic repositories, each with an answer key that labels
exactly what is in it, in the format of the scanner-benchmark contract:

- `must-fire` — a planted defect;
- `must-not-fire` — a trap: code that looks like a defect and is not;
- `clean` — code certified free of the listed concepts;
- `not-applicable` — a concept with nothing to measure in that repository;
- `score-band` — an expected score range for a property that is judged rather than located.

Synthetic repositories are used because only then is the truth known exactly — both what should be found and what
must not be. A real codebase cannot tell you its own false negatives.

### What is measured

For every dimension, in every language the holdout covers:

- **Recall** — planted defects found ÷ planted defects.
- **Trap resistance** — traps left alone ÷ traps.
- **Noise** — findings on traps, on certified-clean code, on not-applicable concepts, or matching nothing the key
  expects. Noise is reported both as a share of findings and as the **number of repositories affected**, so a single
  noisy repository cannot dominate the figure.
- **Band agreement** — the share of `score-band` entries where the score the engine's evidence produces falls inside
  the expected range. This is the figure that ties detection to the number a buyer reads.

Every rate is published with its **95 % confidence interval** (Wilson). A rate without its interval invites a
precision the sample does not have.

### Passing and grading

A dimension **passes** when the lower bound of its recall interval is at or above the recall threshold **and** its
noise is at or below the noise ceiling. Both conditions are required: a pass rule on recall alone rewards an engine
that reports everything.

- Recall threshold per dimension: *to be set* (working assumption: 50 %).
- Noise ceiling per dimension: *to be set*.
- Grades **A+, A, B, C, D, E** per dimension: boundaries *to be set*, defined on recall with the noise ceiling applied.

Where an interval straddles the threshold the result is recorded as **undecided**, not as a pass or a fail.

The **grade of record** for a dimension is computed over the engine's last four windows pooled, so that a grade rests
on enough planted defects to mean something. Each single window is published as it happens, as a provisional result.

### Coverage

Qualification is per dimension and per language. An engine's record lists exactly which dimensions it is qualified
for, in which languages. A CAI published from an engine's evidence names that record; a dimension the engine is not
currently qualified for is reported as **not measured**, never as a score.

## The holdout lifecycle

Four windows a year. Each holdout moves through the same steps, in this order, and each step leaves a public trace.

1. **Protocol.** Before authoring starts, the authoring protocol for the window is published: which concepts and
   dimensions, the quota of planted defects per dimension and language, the ratio of traps to plants, the acceptance
   criteria, the authoring and review models, and the dispute rules.
2. **Authoring.** The repositories and answer keys are written to the protocol (see *Authoring* below).
3. **Validation.** Every answer-key entry is reviewed before sealing (see *Validation* below).
4. **Seal and commit.** The holdout is sealed. The SHA-256 of every repository archive and every answer key is
   published, with the time of sealing. Nothing published after this point can change what the holdout contains
   without the change being visible.
5. **Window.** Participating engines are run (see *Running an engine*).
6. **Reveal.** The repositories, the answer keys, the authoring log (every candidate, every rejection and its reason)
   and every result are published together. Anyone can check them against the hashes from step 4.
7. **Disputes.** For a fixed period after reveal (*to be set*), any participant may dispute an answer-key entry. An
   entry found wrong is corrected in a new key version, the correction is published with its reason, and **every**
   result in that window is rescored against it. A correction never applies to one participant alone.
8. **Release.** The revealed holdout joins the public benchmark set. What was a test becomes material everyone may
   train and tune against, equally.

## Authoring

Holdouts are written by AI models working to the published protocol, with a human operator. The controls are
mechanical, so that they hold regardless of who operates them:

- **Isolation.** The authoring environment has no access to any participating engine: not its source, its
  documentation, its findings or its results. It receives the concept taxonomy, the answer-key schema and the
  protocol, and nothing else.
- **No preview.** No participating engine is run against a holdout, or any part of it, before reveal.
- **More than one model family.** Each holdout is authored by models from at least two different model families, and
  recall is published per authoring family. A systematic advantage for engines built with the same family as an
  author then shows up as a measured difference instead of hiding in the average.
- **Acceptance by rule, not by taste.** Candidates are accepted or rejected only against the acceptance criteria
  published in step 1. Every candidate and every rejection, with its reason, is logged and published at reveal.
- **Quotas.** The protocol fixes the number of planted defects per dimension and language, sized so that the pooled
  grade of record has a usable confidence interval (on the order of 40 or more per dimension per year).
- **Disclosure of the operator.** The operator of the authoring pipeline is named on each holdout. Where an operator
  is affiliated with a participating engine, that is stated on the holdout and on every result in that window.

## Validation

An answer key can be wrong: a plant may not contain the defect it claims, and a trap may in fact be a defect.
Validation must not use any participating engine, because that would bend the key toward it. Instead:

- every entry is reviewed by a model from a **different** family than the one that authored it;
- where a defect can be demonstrated, it is — a test that exercises the injection, a secret that is shown to be
  present in history, a dependency resolved to the vulnerable version;
- a random sample of entries is reviewed by a human, and the sample size and outcome are published at reveal;
- any remaining doubt is resolved by removing the entry before sealing, never after.

## Running an engine

- **What is submitted, before the window opens:** the engine as a container image (by digest), its configuration, and
  its **mapping** from its own rule identifiers to the benchmark's concepts. All three are hashed and recorded. Nothing
  submitted may change once the window opens; a mapping written after seeing a holdout would be a way to buy recall.
- **Where it runs:** offline, operated by the Initiative, with no network access. Data an engine legitimately needs,
  such as a vulnerability advisory database, is supplied as a fixed snapshot identical for every participant, so that
  results do not depend on the day a run happened.
- **Hosted-only engines** that cannot be submitted as an image may be given time-boxed access to the holdout under
  embargo. Their record states *hosted run*, because the holdout was disclosed to the operator of the engine.
- **One attempt.** Each engine version gets one run per window. Engines that are not deterministic are run once, with
  their configuration recorded; there is no best-of-several.
- **Every attempt is published,** including failures. A record that shows only passes would hide the retries.

## The qualification record

Published on codeassuranceindex.info for every attempt:

- engine name, version, image digest, configuration label and mapping hash;
- window and holdout identifiers, with the holdout's published hashes;
- Part A result per rubric version;
- per dimension and language: recall, trap resistance, noise (share and repositories affected), band agreement, each
  with its confidence interval; pass / fail / undecided; the grade of record;
- coverage: the dimensions and languages the engine is qualified for;
- *hosted run*, operator affiliation and any dispute corrections that changed the result.

A qualification applies to the engine version it was earned with. A new version carries no grade until it has been
through a window of its own.

## Checking that the synthetic test tracks reality

Synthetic repositories written to a protocol can drift into a recognisable style that engines learn to spot. As a
check, qualified engines are also run on a sample of real public repositories. These runs are not graded; the
Initiative publishes how closely the engines' results on real code follow their results on the holdout. If they drift
apart, the protocol is changed.

## What qualification is not

- It is not a certification of any codebase, and it does not make any finding in any survey correct.
- It is not an endorsement of an engine, and grades are not a ranking of products beyond what the numbers state.
- It does not change the standard. Rubric changes follow their own versioned process
  ([ADR-0004](adr/0004-versioned-frozen-rubrics.md)); qualification thresholds are published per rubric version.

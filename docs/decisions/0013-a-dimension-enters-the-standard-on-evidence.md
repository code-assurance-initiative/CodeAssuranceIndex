# Decision 0013: A dimension enters the standard on evidence, and the standard holds only what must be comparable

- **Decision:** 0013
- **Date:** 2026-11-06
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0013: A dimension enters the standard on evidence, and the standard holds only what must be comparable](../proposals/0013-a-dimension-enters-the-standard-on-evidence.md)
  ([pull request #48](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/48))
- **Outcome:** Accepted as amended: the principle as written; the evidence reported from today, its bars binding only
  once calibrated; every scored dimension reviewed against it by a date; evidence from any producer counts.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA and the Assure, Assert and Assay products of
Canine Development, today the only producer CAI publishes, and who can today supply the corpus measurement the
evidence asks for. **That is the conflict**: a bar only one producer can measure would favour it. The amendment below
(evidence from any producer counts, and must be reproducible by others) is the answer to it. Nobody else can decide
today, so nobody recused.

## Comment

None before adoption. This change moves no number, so under the interim rule the **14-day** comment period runs after
adoption, to **2026-11-20**. An unresolved objection from a commenter not affiliated with the proposer reopens it.

## Reasons

- The catalog said what a dimension is, never what a dimension must show to be one, so a definition could enter on
  argument and turn out, once measured, to read what kind of code a repository is (PF2, proposal 0014).
- The line between the standard and a producer's own advice keeps the standard about what must be comparable, which
  is the only thing a standard is for, and keeps it free of any producer's product (decision 0012).
- The proposed bars (precision, upheld disputes, saturation) are first guesses, not calibrated values. Binding them
  today could fail dimensions that are sound, or pass ones that are not; measuring the existing scored dimensions
  first is what calibrates them.

## Answers to the proposal's questions

1. **The bars.** All six measures are **reported from today** with every proposal that adds, re-defines or retires a
   dimension. The numeric bars (repositories and languages, the 50-result blind sample, 75 % precision, 25 % upheld
   disputes, the 50 % saturation argument) become **binding with the first rubric version published after the review
   below is complete**, at values the review calibrates; until then a proposal that misses one says so and argues why.
2. **The dimensions already in the catalog.** Every **scored** dimension publishes the six measures by **2027-03-01**.
   A dimension that misses a bar is **reviewed** — a proposal to re-define, demote to advisory or retire it, or a
   recorded reason to keep it — never removed automatically.

## Amendment

Evidence is accepted **from any producer**, measured over a corpus whose repositories and commits are named, so that
another producer can reproduce it. A measurement only one producer can make, or that names no repositories, is not
evidence under this decision.

## What took effect

- Proposal 0013's status, and the proposal and decision indexes.
- [GOVERNANCE.md](../GOVERNANCE.md): a new section, "What the catalog holds, and the evidence a dimension needs",
  with the text of the proposal as amended here.

## What did not yet take effect

- The numeric bars, until the review of the scored dimensions completes (answer 1).

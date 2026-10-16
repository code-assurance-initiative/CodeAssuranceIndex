# 0012 — Changes to CAI go through proposals and decisions

- Status: Accepted
- Date: 2026-10-16

Every change to the standard, to the benchmark process, or to the process by which the benchmark's answer keys are
made and corrected starts as a numbered proposal in [`docs/proposals/`](../proposals/README.md) and takes effect only
through a numbered decision record in [`docs/decisions/`](../decisions/README.md) that names who decided, when, and
why.

## Context

[GOVERNANCE.md](../GOVERNANCE.md) holds the Initiative to four bars before it may call itself independent. The second
is: *how a change is proposed — and how one is refused — is published, with who decided and why.* Until now the
published part was a description of a process (issue, RFC, comment period, decision written into the RFC file) with
nothing behind it: `docs/rfcs/` never held an RFC, so not one change had been proposed or refused through it, and
the changes that did happen reached the repository as commits. A commit records who typed a change; it does not
record who decided it, what was weighed, or that anyone could have said no.

That gap matters most where the decider has a stake. The same governance page names the conflict it exists to be
honest about: an implementation that writes the rules it is measured by. Watchdog is such an implementation. It is
measured by the scanner benchmark, and its maintainer is today also the person who maintains the benchmark and holds
the standard. If a change can arrive by commit, then a change that suits Watchdog can arrive the same way, and the
record cannot tell a reasoned decision from a convenient one. The first change that forced the question was
Watchdog's own: a proposal for how an answer key may be disputed, from the party a disputed key would most help.

There is also a scope gap. The RFC process covered "what the standard says". The benchmark's process (how a window
runs, how a key is frozen, how a key is disputed and corrected) lives in a second repository,
[scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark), and had no decision process at
all.

## Decision

- **One path for every change** to the standard (a dimension, a weight, a formula, a band, the qualification scheme,
  the governance process), to the benchmark process, and to the process for its answer keys. It starts as a
  **numbered proposal** — `docs/proposals/NNNN-short-title.md`, filed by pull request from the template in
  [the proposals README](../proposals/README.md) — and takes effect only when a **numbered decision record** —
  `docs/decisions/NNNN-short-title.md` — accepts it (whole or amended). A decision may equally reject it; a proposer
  may withdraw it.
- **A decision names who decided, when, and why**: the decider (person, and the body they decide for), the date, the
  outcome (accepted, rejected or amended), the reasons, any recusal, and what took effect with it and what did not.
  A rejection without written reasons is not a decision.
- **Both are immutable once recorded.** The text of a filed proposal and of a recorded decision is never edited
  afterwards; only the status line of a proposal's registration block and the index tables move. A later change of
  mind is a new proposal and a new decision that supersedes the earlier one, so what was previously decided stays
  readable, as ADRs already work ([ADR-0001](0001-record-architecture-decisions.md)).
- **A proposer with a stake declares it** in the proposal: which engine they build or work for, or which code
  measured by CAI they own. A proposal without a declaration is not filed.
- **The RFC directory is folded into this.** No RFC was ever filed, so nothing moves; the RFC template's sections
  (problem, change, effect on published scores, alternatives) become the proposal template, and `docs/rfcs/` is
  removed rather than kept as a second path to the same place. The comment periods and the rule on who may accept a
  score-moving change are unchanged and now apply to proposals ([GOVERNANCE.md](../GOVERNANCE.md)).
- **Process changes for scanner-benchmark are decided here**, beside the standard they serve. The benchmark
  repository implements a decision and links to it; it does not hold decisions of its own.

## Consequences

- Bar 2 has something behind it: a reader can list every change proposed, see which were refused, and read who
  decided each and why. The first proposal and decision are recorded with this ADR.
- A change that arrives as a commit without a decision record is now visibly out of process, which is the point: a
  reviewer has a rule to point to, not a feeling.
- This does not remove the conflict of interest; it makes it legible. Until the steering group exists the decider is
  still one person, and today that person also maintains Watchdog. Each decision says so where it applies. Only bar
  1 (a steering group with no single company in control) removes the conflict itself.
- There is a cost: even a small process change now needs two files. A correction that moves no number and changes no
  process (a typo, a broken link, a reference-scorer defect against the published text) does not; it stays an
  ordinary pull request, as [GOVERNANCE.md](../GOVERNANCE.md) already allows during the standstill.
- Nothing here is enforced by a test. The proposal and decision indexes are kept by hand, and nothing yet checks that
  a decision exists for a given change; that check would need a definition of "a change that needs one" precise
  enough to automate, which this ADR does not attempt.

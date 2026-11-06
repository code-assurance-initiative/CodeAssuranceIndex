# Decisions — what was decided about a proposal, by whom, and why

A proposal in [`../proposals/`](../proposals/README.md) changes nothing on its own. It takes effect only through a
decision recorded here ([ADR-0012](../adr/0012-changes-go-through-proposals-and-decisions.md)), and a refusal is
recorded here the same way. This directory is the published answer to bar 2 of
[GOVERNANCE.md](../GOVERNANCE.md#the-bar-this-page-will-be-held-to): how a change is proposed and how one is refused,
with who decided and why.

## What a decision record holds

One file per decision, `NNNN-short-title.md`, numbered in its own sequence (a decision's number need not match its
proposal's), with:

- **Decision**, **Date**, **Decided by** (the person, and the body they decide for), **Proposal** (a link), and
  **Outcome**: *Accepted*, *Accepted as amended*, or *Rejected*.
- **Conflicts and recusals**: whether the decider, or anyone who took part, has a stake in the proposal, and who
  stood aside. "None" is a statement, not a blank.
- **Comment**: where comment was taken, for how long, and the objections raised and how each was answered.
- **Reasons** for the outcome. A rejection without written reasons is not a decision.
- **Answers** to every question the proposal put to the decider, and every amendment, stated as the rule that now
  holds.
- **What took effect** with the decision (the files and repositories changed to implement it), and **what did not
  yet**, named, so a reader can tell the adopted rule from the parts that are still only promised.

A decision is immutable once recorded. Overruling it, or changing what it adopted, is a new proposal and a new
decision that says which earlier decision it supersedes; the earlier one stays here unchanged, and its index row
links to its successor.

## Index

| Decision | Proposal | Outcome | Date | Decided by |
|---|---|---|---|---|
| [0001](0001-adopt-the-answer-key-dispute-path.md) — Adopt the answer-key dispute path | [0001](../proposals/0001-answer-key-dispute-path.md) | Accepted, with the proposal's four questions answered | 2026-10-16 | Jimmy Borch, for the CAI team |
| [0002](0002-rubric-versions-are-published-by-the-initiative.md): Rubric versions are published by the Initiative | [0002](../proposals/0002-rubric-versions-are-published-by-the-initiative.md) | Accepted, the proposal's three questions answered; comment period not taken | 2026-10-16 | Jimmy Borch, for the CAI team |
| [0003](0003-survey-pages-link-the-producers-public-report.md): Survey pages link the producers' public reports | [0003](../proposals/0003-survey-pages-link-the-producers-public-report.md) | Accepted as amended (a list of reports per delivery, the listing as a file with a host rule, no probe); comment period not taken | 2026-10-22 | Jimmy Borch, for the CAI team |
| [0005](0005-migrated-contributors-are-read-by-tool.md): Contributors no longer read by a language model are catalogued as tools | [0005](../proposals/0005-migrated-contributors-are-read-by-tool.md) | Accepted, M4 declares `advisory: false`; comment period not taken | 2026-11-01 | Jimmy Borch, for the CAI team |
| [0006](0006-vulnerability-database-findings-are-advisory.md): Correct a defect against the reproducibility promise: vulnerability-database findings are advisory | [0006](../proposals/0006-vulnerability-database-findings-are-advisory.md) | Accepted as an extraordinary defect correction (D30, D43 advisory; §2.1 states the class); score-moving; 30-day comment period runs after adoption, to 2026-12-03, and an unaffiliated objection reopens it | 2026-11-03 | Jimmy Borch, for the CAI team |
| [0007](0007-producers-are-named-by-their-products.md): Producers are named by their products | [0007](../proposals/0007-producers-are-named-by-their-products.md) | Accepted as written; comment period not taken | 2026-11-04 | Jimmy Borch, for the CAI team |
| [0008](0008-a-scanner-mapping-covers-every-rule-its-scanner-emits.md): Correct the mapping for the rule-id rename, and require a mapping to cover every rule its scanner emits | [0008](../proposals/0008-a-scanner-mapping-covers-every-rule-its-scanner-emits.md) | Accepted as written, two questions answered; moves benchmark scores (CCA recall/noise); 30-day comment period runs after adoption, to 2026-12-06; **superseded by [decision 0012](0012-the-standard-names-no-producer.md)** (its mapping change never took effect) | 2026-11-06 | Jimmy Borch, for the CAI team (interim rule, decision 0011) |
| [0009](0009-concepts-a-known-defect-corpus-needs.md): Add the 34 concepts a known-defect corpus needs | [0009](../proposals/0009-concepts-a-known-defect-corpus-needs.md) | Accepted as written, two questions answered; no CAI score moves; 14-day comment period runs after adoption, to 2026-11-20 | 2026-11-06 | Jimmy Borch, for the CAI team (interim rule, decision 0011) |
| [0011](0011-interim-pragmatic-governance.md): Adopt an interim pragmatic rule in place of the standstill | [0011](../proposals/0011-interim-pragmatic-governance.md) | Accepted as written, two questions answered; moves no number; 14-day comment period runs after adoption, to 2026-11-20 | 2026-11-06 | Jimmy Borch, for the CAI team |
| [0012](0012-the-standard-names-no-producer.md): The standard names no producer | [0012](../proposals/0012-the-standard-names-no-producer.md) | Accepted as written, two questions answered; supersedes decision 0008; moves no number; 14-day comment period runs after adoption, to 2026-11-20 | 2026-11-06 | Jimmy Borch, for the CAI team (interim rule, decision 0011) |
| [0013](0013-a-dimension-enters-the-standard-on-evidence.md): A dimension enters the standard on evidence, and the standard holds only what must be comparable | [0013](../proposals/0013-a-dimension-enters-the-standard-on-evidence.md) | Accepted as amended (evidence reported now, bars binding after calibration; scored dimensions reviewed by 2027-03-01; evidence from any producer); moves no number; 14-day comment period runs after adoption, to 2026-11-20 | 2026-11-06 | Jimmy Borch, for the CAI team (interim rule, decision 0011) |

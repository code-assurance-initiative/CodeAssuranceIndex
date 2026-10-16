# Proposal 0002: Rubric versions are published by the Initiative, not by an implementation

- **Number:** 0002
- **Filed:** 2026-10-16
- **Proposer:** Jimmy Borch, for the CAI team
- **Declared interests:** the proposer maintains Watchdog, a commercial engine that produces CAI evidence and whose
  continuous-integration pipeline has published every rubric version since June 2026. This proposal takes that
  pipeline's write access away and makes Watchdog's own rubric changes wait for a decision here. The proposer
  benefits from the credibility that gives the standard and pays for it in Watchdog's release cadence.
- **Moves scores:** no. It changes no published catalog and no rule a scorer applies. It changes who may publish a
  catalog and when a new version may be minted.
- **Status:** Accepted by [decision 0002](../decisions/0002-rubric-versions-are-published-by-the-initiative.md), 2026-10-16

## Problem

Discussed in [issue #20](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/20).

[GOVERNANCE.md](../GOVERNANCE.md#the-bar-this-page-will-be-held-to) holds the Initiative to four bars before it may
call itself independent. The fourth is that rubric versions are published by the Initiative, not by an
implementation, and the page calls it the most important and the furthest away. This is what it looks like in the
repository today.

**An implementation writes the standard, by direct push.** The 34 commits that have ever touched `rubrics/` were
all pushed straight to `main`; none came through a pull request. 15 are authored `kennel-ci`, the Watchdog
pipeline, which emits the catalog from the Watchdog engine and pushes it on the commit that bumps the engine's
version constant. The other 19 were pushed by hand. `main` has a ruleset that forbids deletion and
force-push and nothing else, so no review step exists to skip.

**ADR-0012 already requires a proposal for this, and the pipeline cannot obey it.** Since 2026-10-16 a change to
"a dimension, a weight, a formula, a band" is a proposal and a decision. The pipeline publishes the moment a bump
lands, so the next Watchdog bump would publish a new rubric with no proposal behind it. Nothing has gone through
since ADR-0012; the pipe is still connected.

**Rubric versions are being used as Watchdog's release numbers.** Comparing each of the 48 published versions with
the one before it, by what the reference scorer actually reads
([`tools/rubric/classify-rubric-change.py --history`](../../tools/rubric/classify-rubric-change.py)):

| Class | Versions | What changed |
|---|---:|---|
| Score-relevant | 27 | a dimension added, removed or re-filed, the lenses, or the scoring block |
| Descriptive | 8 | only prose: names, descriptions, evaluator labels |
| Name only | 13 | **nothing.** Byte-identical apart from `rubricVersion` |

Thirteen versions of the standard, more than one in four, record no change to the standard. They were minted to mark
a change in how Watchdog *measures*: `rubric-2026.10.5`, for example, records that one Watchdog dimension's model
verdict became advisory, which is a change to Watchdog's evidence, not to the rules evidence is scored by. A reader
who sees thirteen versions of a standard in four months concludes it is unstable or that one company is steering
it. The first is false. The second is what this proposal ends.

Measurement belongs to the producer. [ADR-0002](../adr/0002-deterministic-reproducible-scoring.md) says producing the
bundle is the analyzer's job and scoring it is open, and
[ADR-0010](../adr/0010-signed-cai-delivery-package-and-registry.md) already records the producer's identity in every
delivery. An engine that changes how it measures has a place to say so that is not the rubric's version number.

## Change

1. **Only the Initiative publishes.** A rubric version enters `rubrics/` only through a pull request merged by a CAI
   maintainer. No implementation's pipeline, credential or bot writes to this repository. Anyone may *open* the pull
   request, including an implementation's pipeline: opening it is a request, merging it is the publication.

2. **`main` takes changes only through pull requests.** The `main` ruleset gains "require a pull request before
   merging". While the Initiative has one maintainer it requires no approving review, because GitHub does not let an
   author approve their own pull request and there is nobody else; the pull request is still the public record of
   what changed, when, and on whose word. It requires one approval as soon as there is a second maintainer.

3. **A new version is minted only for a change to the standard.** Every pull request that adds a version under
   `rubrics/` is classified against the newest published version by the rule in
   `tools/rubric/classify-rubric-change.py`, and CI posts the result:

   | Class | What it needs before it is merged |
   |---|---|
   | Score-relevant, moves numbers | an accepted proposal, linked from the pull request. Comment period 30 days, as GOVERNANCE.md sets. |
   | Score-relevant, moves no number (advisory dimensions only) | an accepted proposal. Comment period 14 days. |
   | Descriptive | review by a maintainer. It is a correction that moves no number, which GOVERNANCE.md already allows as an ordinary pull request. |
   | Name only | **refused.** A catalog identical to its predecessor is not a new version of the standard. |

   A proposal may cover several dimensions, and the version that implements it may carry all of them, so an
   implementation that batches its changes waits once, not once per dimension.

4. **Published versions stay immutable**, as [ADR-0004](../adr/0004-versioned-frozen-rubrics.md) requires. CI fails a
   pull request that modifies or deletes any file under an existing `rubrics/<version>/`. The thirteen name-only
   versions already published stay where they are, because a delivery may name them.

## Effect on published scores

None. No catalog changes and no scorer rule changes. Every published version keeps its content and its URL.

The effect falls on implementations, and today that means Watchdog. A Watchdog change that adds or re-files a scored
dimension cannot ship until a proposal for it is accepted, which is at least 30 days after it is filed. Of Watchdog's
48 version bumps so far, 27 would have needed a proposal, 8 a maintainer review, and 13 would have been refused and
handled inside Watchdog instead. That cost is deliberate: an implementation that can change the standard on its own
schedule is the conflict this proposal is about.

## Alternatives considered

- **Keep the automated push and review afterwards.** Rejected: a published version is immutable and may be named by
  a delivery minutes later, so review after publication can comment but cannot refuse.
- **Let implementations publish under their own namespace** (for example `rubrics/watchdog/...`). Rejected for the
  standard itself: a score would then be taken under rules one company wrote, which is the present problem with a
  prefix on it. It may still be right for *advisory extensions* that can never move a CAI number; see question 3.
- **A separate rubric repository with its own maintainers.** Possibly later, once there are maintainers to give it.
  Today it would move the same single maintainer to a second repository and change nothing.
- **Do nothing until the steering group exists.** Rejected: the steering group is more likely to form around a
  standard whose publication is already separated from any one implementation than around one where it is not.

## Questions for the decider

1. **Should the pull-request rule cover all of `main`, or only `rubrics/`, `schemas/` and the other governed paths in
   `.github/CODEOWNERS`?** The proposer's view: all of `main`. A rule with a documented exception for "everything
   else" invites the exception, and a reader should not need to know which paths are governed to trust the record.
2. **Is a descriptive change a new version at all?** It must be, because published versions are immutable, but it
   could be marked as a revision (for example `rubric-2026.10.5+1`) so a reader sees that no rule moved. The proposer
   has no strong view.
3. **Should advisory extensions have their own route?** An implementation could declare dimensions under its own
   prefix that a conforming scorer ignores and no CAI number reads, so it can ship them without a decision here.
   That is a change to the standard (it touches `rejectUnknownContributors`) and the proposer suggests it is
   proposed separately, not decided here.

## What Watchdog will do on its own side (noted, not part of the proposal)

This binds Watchdog, not CAI, and is listed so the reader can check it happens:

- Watchdog's pipeline stops pushing to this repository on the day this proposal is filed. A Watchdog rubric bump
  opens a pull request here instead, and Watchdog's own build refuses to merge the bump until the version is served,
  which it already does.
- Watchdog stops minting name-only versions. A change to how Watchdog measures is recorded in Watchdog's producer
  identity, not in a rubric version.

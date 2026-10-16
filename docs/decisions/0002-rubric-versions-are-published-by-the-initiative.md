# Decision 0002: Rubric versions are published by the Initiative, not by an implementation

- **Decision:** 0002
- **Date:** 2026-10-16
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, and today the whole team, until the steering
  group exists: [GOVERNANCE.md](../GOVERNANCE.md))
- **Proposal:** [0002: Rubric versions are published by the Initiative, not by an implementation](../proposals/0002-rubric-versions-are-published-by-the-initiative.md)
  ([pull request #21](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/21),
  [issue #20](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/20))
- **Outcome:** Accepted. The proposal's four changes are adopted as written. Its three questions are answered below,
  and those answers are the only amendments.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who also maintains Watchdog, the implementation whose pipeline
published the rubric versions this proposal is about. Nobody else could decide, so nobody recused.

The conflict runs against the decider's own engine, not in its favour: the decision takes away Watchdog's write
access, refuses the kind of version Watchdog minted most often after score-relevant ones, and makes every Watchdog
change to a scored dimension wait for a proposal here. The steering group, once it exists, can confirm or replace
this decision, and this decision asks for that.

## Comment

None was taken. The proposal was filed and accepted on the same day, without the 14-day comment period that
[GOVERNANCE.md](../GOVERNANCE.md#how-a-change-to-the-standard-is-decided) sets for a change that moves no number. That
is a departure from the published process, and it is recorded as one, as decision 0001's was.

Why it was accepted anyway: the proposal moves no number, restrains only the proposer's own engine, and leaving the
pipeline connected during a comment period would have let it publish the next Watchdog rubric with no proposal behind
it, which ADR-0012 already forbids. Objections remain welcome: anyone may file a proposal to amend or replace this
decision, and it will be decided under the full comment period.

## Reasons

- Bar 4 of GOVERNANCE.md is the one the page calls the most important. The evidence in the proposal is reproducible
  from the published catalogs (`tools/rubric/classify-rubric-change.py --history`): 34 of 34 rubric commits were
  direct pushes, 15 by an implementation's pipeline, and 13 of 48 versions changed nothing the scorer reads.
- A version is a promise that something about the rules changed. A name-only version breaks that promise and turns
  the standard's version number into one engine's release number.
- Review after publication cannot refuse anything, because a published version is immutable. So the review has to
  come before, which means a pull request merged by a maintainer.

## Answers to the proposal's questions

1. **The pull-request rule covers all of `main`.** The `Protect main` ruleset requires a pull request for every change,
   with no bypass, including for the maintainer. It requires no approving review while there is one maintainer and
   one approval as soon as there are two.
2. **A descriptive change is a plain new version** (`rubric-YYYY.MM.N`), with no revision suffix. Every consumer,
   including `RubricVersionOrder.IsWellFormed` in the reference scorer, parses that format, and a suffix would change
   the format of the standard for a label the rubric gate already prints on the pull request. Its class is recorded
   on the pull request and in the CHANGELOG.
3. **Advisory extensions are not decided here.** A route for implementation-prefixed dimensions that no CAI number
   reads would change `rejectUnknownContributors` and is a change to the standard. It needs its own proposal.

## What took effect

- Proposal 0002's status and the proposal and decision indexes.
- `tools/rubric/classify-rubric-change.py`: the classification rule (score-relevant, descriptive, name-only).
- `tools/rubric/check-rubric-pr.py` and `.github/workflows/rubrics.yml`: the rubric gate on every pull request. It
  refuses a change under an already published version, a catalog that does not declare its directory, a name-only
  version, and a score-relevant version whose pull request links no accepted decision. Checked against seven cases
  before it was merged: four refusals, three passes.
- The `Protect main` ruleset gains "require a pull request before merging" (0 approvals, no bypass actors).
- `.github/workflows/deploy.yml`: the step that commits a rotated corpus signing key cannot push to `main` any more;
  its warning now says to open a pull request.
- [GOVERNANCE.md](../GOVERNANCE.md): bar 4 says what is now true and what is not.
- Watchdog, on its own side (noted, not adopted): its pipeline stopped pushing to this repository
  (`kennel.canine.dev` `acabed5cdd`). A Watchdog rubric bump opens a pull request here with
  `publish-rubric-catalog.sh --pr`, and that script refuses to request a name-only version.

## What did not yet

- **Bar 4 is not met in substance.** The Initiative now publishes, but every catalog so far was still *written* by one
  engine's emitter, and the one maintainer who merges also maintains that engine. A second maintainer with no tie to
  Watchdog is what turns the merge into a review.
- **Watchdog cannot yet ship a measurement-only change.** It has been recording such changes as rubric versions, which
  this decision refuses; until Watchdog records them in its producer identity instead, those changes wait.
- The 13 name-only versions already published stay, because deliveries may name them.
- The standstill in GOVERNANCE.md ("CAI is not materially changed while the body that ought to decide such changes is
  still being put together") was not true of the rubric while an implementation could publish it. From today a
  score-relevant version needs an accepted proposal, which brings the rubric under the rule the standstill describes.

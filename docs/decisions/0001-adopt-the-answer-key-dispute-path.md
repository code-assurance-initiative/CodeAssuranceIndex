# Decision 0001: Adopt the answer-key dispute path

- **Decision:** 0001
- **Date:** 2026-10-16
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, and today the whole team, until the steering
  group exists: [GOVERNANCE.md](../GOVERNANCE.md))
- **Proposal:** [0001 — A dispute path for CAI answer keys](../proposals/0001-answer-key-dispute-path.md)
- **Outcome:** Accepted. The process in the proposal's §2 is adopted as written; its four questions (§4) are answered
  below, and those answers are the only amendments. The proposal's §3 describes what Watchdog will do on its own side
  and is noted, not adopted: it binds Watchdog, not CAI.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer is the Watchdog maintainers. The decider maintains Watchdog. This is the conflict the proposal itself
names, and here it is complete: the party who would benefit from a loose dispute path wrote the proposal and decided
it, and there was nobody else to stand aside for. No one recused, because no one else could decide.

Three things make the decision defensible despite that, and none of them removes the conflict:

- The proposal moves **no** key and **no** recorded number. It sets the procedure by which a key may later move.
- What it adopts narrows the proposer's latitude rather than widening it: a scanner's output is never evidence that
  a key is wrong; the adjudicator may be neither the claimant, nor anyone affiliated with the claimant's scanner, nor
  the entry's labeller; a correction re-scores every scanner, so it cannot help the claimant alone; and every
  decision, whatever its outcome, is counted per scanner in a public ledger.
- It is reversible on the record: the steering group, once it exists, can confirm or replace it by a new decision.
  It should be put to the steering group for that reason, and this decision asks for it.

## Comment

None was taken. The proposal was filed and decided on the same day, 2026-10-16, without the 14-day public comment
period that [GOVERNANCE.md](../GOVERNANCE.md#how-a-change-to-the-standard-is-decided) sets for a change that moves no
number. That is a departure from the published process, and it is recorded as one. The reason it was accepted
anyway: until today there was no written way to dispute a key at all — `CHALLENGE-RUN.md` promised a dispute window
with no procedure behind it — and a written procedure decided without comment is still more contestable than an
unwritten one. Objections remain welcome: anyone may file a proposal to amend or replace this decision, and it will
be decided under the full comment period.

## Reasons

- The integrity rules of the benchmark already said what must be true (a key change after freeze is a new version;
  results never feed back into keys except by the authoring loop; never select on the outcome) but not who may ask
  for a change, with what evidence, who decides, or how a re-scored result is published beside one that cannot be
  withdrawn. The proposal fills exactly that gap and contradicts none of the existing rules.
- Its central rule — *a scanner's output is the reason a dispute is filed, never the reason it is upheld* — is what
  keeps a dispute path from becoming a way for a scanner to be graded on its own opinion.
- Re-scoring beside the original, rather than in place of it, keeps the pre-registered measurement of a holdout
  verifiable after a correction, which the holdout's content commitment exists for.

## Answers to the proposal's questions

1. **A separate post-verdict window for a scanner owner** is adopted as proposed: 30 days from the publication of
   that scanner's own verdict, even if the reveal window has closed. A scanner owner often first sees a wrong entry in
   its own verdict; a window that has already closed by then is a window it never had. The rule against selecting on
   the outcome, and the per-scanner count of disputes, keep this from being a second bite at every miss.
2. **A training-set correction is a new per-unit key version, released when the dispute is decided**: the existing
   integrity rule — a new tag (`v1.1.0` or later), a new registry entry with its own `keySha256`, and a journal entry
   with the reason and the evidence — re-bundled into the set as `docs/AUTHORING.md` already describes. Corrections are
   not held back to be batched into a periodic set release: a key known to be wrong would keep scoring every scanner
   until the batch shipped, and a batch would blur which correction moved which number.
3. **The dispute ledger lives in scanner-benchmark, at `disputes/ledger.json`**, beside the keys, the registry and the
   recorded results it annotates. The Index records publications, not keys; a ledger there would describe entries the
   Index does not hold.
4. **An erratum record is the instrument on the Index.** A publication on the Index can be neither withdrawn nor
   replaced, so a re-scored figure is published as a new record that references the original publication and carries
   both figures and the key versions behind each. The original is never edited. The erratum record type is not built
   by this decision (below).

The ledger entry shape is the one in the proposal's §2.4, with what a reader needs to count disputes per scanner and
per outcome and to tie each entry to the exact key it was about: `outcome` (`upheld`, `corrected`, `not-evidenced`,
`withdrawn`), `claimantScanner` (the scanner the claimant owns or works for, or `null`), `keySha256` of the disputed
key, `filed` (the filing date, beside `date`, the decision date) and, for a post-verdict dispute, `publication`. The
authoritative description is `disputes/README.md` in scanner-benchmark.

## Consequences

- From today a dispute about an answer key has a route, a form, a decider, a deadline (14 days to a decision) and a
  record. A dispute filed through any other channel is pointed to the form.
- **Today, most disputes cannot be decided by the coordinator.** The coordinator maintains Watchdog, and the current
  keys were authored under the coordinator (their journals name no other labeller), so under the recusal rule a
  dispute filed by Watchdog, or about any entry the coordinator labelled, needs a second maintainer or an external
  reviewer. There is no second maintainer yet. Such a
  dispute waits, open and visible, past the 14-day target if it must; the target is not met by relaxing the recusal.
  The steering group, or the first second maintainer, is what fixes this.
- Headline figures cite the key version they were scored against once any key has a second version. Until a
  correction happens, every key is at its first frozen version and the citation changes nothing.

## What took effect with this decision

In this repository (CodeAssuranceIndex):

- [ADR-0012](../adr/0012-changes-go-through-proposals-and-decisions.md), this directory and
  [`docs/proposals/`](../proposals/README.md), which replace `docs/rfcs/`.
- [GOVERNANCE.md](../GOVERNANCE.md) (the change process now runs through proposals and decisions; bar 2 says what is
  still missing) and [CHALLENGE.md](../CHALLENGE.md) (the route "an answer key is wrong").

In [scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark):

- `disputes/README.md` — the process of the proposal's §2, as adopted here.
- `disputes/ledger.json` — the ledger, empty and append-only.
- `.github/ISSUE_TEMPLATE/dispute-key.yml` — the `dispute-key` issue form, carrying the fields of §2.2.
- `tools/disputes.py` and `tests/test_disputes.py` — the check that every ledger entry has the required fields, that
  a correction names a newer key version, and that the ledger has only ever been appended to (compared against every
  committed version of the file), plus a per-scanner count; a test workflow runs the harness's tests on every push.
- `docs/CHALLENGE-RUN.md` step 4 points to the process and to this decision.

## What did not take effect yet

- **The Index's erratum record type is not built.** Nothing on the Index can yet publish a re-scored figure beside an
  original. The follow-up would touch: `src/Cai.Web.Noise/Infrastructure/NoiseSchema.cs` (an append-only errata
  table, or a typed second row of `noise_publications`, which is already append-only and already treats a correction
  as a second row but does not reference the record it corrects or carry key versions),
  `src/Cai.Web.Noise/Infrastructure/NoiseStore.cs` and `NoiseStoreContracts.cs` (writing and reading it),
  `src/Cai.Web.Noise/Endpoints/NoisePublicationEndpoints.cs` (publishing it and serving it with the original),
  `src/Cai.Web.Noise/Pages/NoiseRecord.razor` (rendering both figures side by side, with their key versions), a
  normative description under `docs/spec/`, and tests in `tests/Cai.Tests`. It is needed before the first correction
  touches a figure already published on the Index, and not before.
- **No format is fixed for a re-scored record under `results/<scanner>/`.** The rule (a new record beside the
  original, naming the key version and the record it was re-scored from) holds now; its file shape is set by the
  first correction that needs one.
- **No page renders the per-scanner dispute counts.** `python3 -m tools.disputes stats` computes them from the public
  ledger; with an empty ledger there is nothing to render yet.
- **The steering group does not exist**, so this decision has not been confirmed by more than one party.

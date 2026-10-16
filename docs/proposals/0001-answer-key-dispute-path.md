<!--
  Registration block, prepended by the CAI team on 2026-10-16. Everything below the horizontal rule is the
  proposal exactly as filed, byte for byte; it is not edited, including where it is addressed to "the cai-bench
  coordinator" or calls itself a PROPOSAL. Paths it names under docs/plans/ and tools/bench/ are in the
  Watchdog repository, not this one.
-->

# Proposal 0001: A dispute path for CAI answer keys

- **Number:** 0001
- **Filed:** 2026-10-16
- **Proposer:** the Watchdog maintainers, as a scanner owner
- **Declared interests:** the proposer builds Watchdog, a scanner measured by the scanner benchmark against the very
  answer keys this proposal is about. The proposal declares that conflict itself, in its opening lines.
- **Moves scores:** no. It changes no key and no recorded result; it sets the procedure by which a key may later be
  corrected and results re-scored.
- **Status:** Accepted by [decision 0001](../decisions/0001-adopt-the-answer-key-dispute-path.md), 2026-10-16
- **As filed:** sha256 `c12a2e8de942825f1d016b99467e3a3220d0783330cd118b614072cd91ebec15` of the proposal as filed: every byte after the
  `---` line below and the blank line that follows it

---

# Proposal: a dispute path for CAI answer keys

**For:** the cai-bench coordinator (owner of `code-assurance-initiative/scanner-benchmark`, the training sets and the
quarterly holdouts).
**From:** the Watchdog maintainers, as a scanner owner. That is a conflict of interest, and it is the reason for most of
what follows: a scanner owner is exactly the party who must not be able to move a key it is measured against.
**Status:** PROPOSAL, 2026-10-16. Nothing here changes the cai-bench repository, any key or any published result. It
asks the coordinator to adopt (or reject) a process; adopting it is the coordinator's change to make.
**Context:** `docs/plans/watchdog-golden-corpus.md` §4 (phase G5). The same process already runs on Watchdog's own gate
(the bench over `training-set-2026`): cases in the maintainer service, a key ledger in `tools/bench/keys/ledger.json`,
and re-scoring of history in `tools/bench/bench.py rescore`.

## 1 What exists today, and the gap

The harness already states the principles:

- `README.md`, *Integrity rules*: a key change after freeze is a new version (new tag, new registry entry, a journal
  entry saying why); recorded results never feed back into the keys except through the authoring loop, before freeze;
  never select on the outcome.
- `docs/CHALLENGE-RUN.md`, step 4: for 30 days after reveal anyone may dispute an answer-key entry; a corrected entry
  gets a new key version and every engine's result in the window is rescored against it.

What is not written down is the procedure: who may file and how, what counts as evidence, who decides, what the
possible outcomes are, where the decision is recorded, and how a re-scored result is published when the Code
Assurance Index allows no withdrawal and no replacement of a publication. There is also no path for a training-set
entry (no window applies), and no path for a scanner owner who sees a wrong entry only when their own verdict is
published.

## 2 Proposed process

### 2.1 Who may file, and when

| Set | Who | Window |
|---|---|---|
| Holdout, after reveal | anyone | 30 days from reveal (as `CHALLENGE-RUN.md` already says) |
| Holdout, a scanner's own published verdict | the scanner's owner | 30 days from the publication of that verdict, even if the reveal window has closed |
| Training set | anyone | any time while the set is current |

A dispute is about **one entry of one key version**. A scanner owner disputing after their verdict names the verdict
(publication id) the entry was scored in.

### 2.2 What a dispute carries (inline, never a link alone)

A `dispute-key` issue form, with every field stated in the issue text itself:

1. set, unit, tag, key version and `keySha256` of the key disputed, and the entry id;
2. direction: `must-fire-not-a-defect` (a plant that is not a defect, or not where the key says) or
   `must-not-fire-is-a-defect` (a trap that is in fact a defect);
3. the scanner evidence, if any: rule id, location and message of the result (or the statement that no result was
   emitted within the key's line tolerance), with the engine version and configuration label;
4. the argument, and the **independent evidence** it rests on: the code at the frozen tag (quoted, with lines), the
   concept's definition in `taxonomy.json` applied to that code, the repository's own history, the authoring journal;
5. the claimant, their affiliation, and a conflict declaration (which scanner they own or work for, if any).

**A scanner's output is never evidence that a key is wrong.** It is the reason a dispute is filed, not a reason it is
upheld. A dispute whose only support is "our scanner disagrees" is closed as not evidenced.

### 2.3 Who decides

The adjudicator is **neither the claimant, nor anyone affiliated with the claimant's scanner, nor the entry's
author** (the labeller named in the unit's journal). The coordinator assigns the adjudicator and records the
assignment and any recusal. Where the coordinator authored the entry, the adjudicator is a second maintainer or an
external reviewer. The adjudicator is named in the decision.

### 2.4 Outcomes

- **Key upheld (the scanner is wrong).** The entry stands. A journal entry in the unit's `benchmark/journal.md` records
  the dispute and the reason. No number changes.
- **Key corrected.** The unit gets a new key version (`v1.0.0` to `v1.1.0`): a new tag, a new registry entry with its
  own `keySha256`, and a journal entry with the reason and the evidence. The frozen tag and hash of the old version are
  never edited. The change is one of: relabel (a plant is a trap, or the reverse), relocate (right defect, wrong
  site), remove (not a defect and not a useful trap).
- **Not evidenced / withdrawn.** Closed with the reason.

A decision is due within 14 days of filing. Every decision, whatever its outcome, is appended to a machine-readable
ledger in the harness repository (proposal: `disputes/ledger.json`, append-only), so a reader can count disputes per
scanner and per outcome. The shape we use on our side, which we would align with whatever the coordinator adopts:

```json
{"set": "training-set-2026", "unit": "bench-csharp-tests", "entryId": "TQ-001", "keyVersion": "1.0.0",
 "newKeyVersion": "1.1.0", "op": "relabel", "to": "must-not-fire", "case": "<dispute id>",
 "claimant": "...", "labeller": "...", "adjudicator": "...", "evidence": "...", "journal": "...", "date": "2026-10-16"}
```

### 2.5 Re-scoring, without rewriting what was published

- Every recorded result under `results/<scanner>/` that was scored against the old key version is re-scored against
  the new one, for every scanner, not only the claimant's. The harness already scores from the stored SARIF, so this
  is a re-score, not a re-run.
- The re-scored result is published as a **new record beside the original**, stating the key version it was scored
  against and the record it was re-scored from. The original is never edited or removed.
- On the Code Assurance Index, where a publication can be neither withdrawn nor replaced, the re-score is published
  as an **erratum record** that references the original publication and carries both numbers and the key versions
  behind them. Headline figures thereafter cite their key version.
- A holdout's pre-registered content commitment covers the units and keys as sealed. The figure measured at reveal
  remains the pre-registered measurement. A post-dispute figure is labelled as such ("re-scored against key v1.1.0
  after dispute <id>"), so the pre-registered number and the corrected one are both visible and both verifiable.

### 2.6 Guarding against the obvious abuse

- **Never select on the outcome** applies to disputes too: an entry is never relabelled, moved or removed because
  scanners miss it or fire on it; only because independent evidence shows the label is wrong.
- Dispute statistics are published per scanner (filed, upheld, corrected), so a scanner owner who disputes every
  entry it misses is visible as one.
- A correction re-scores every scanner, so a correction cannot help the claimant alone.

## 3 How Watchdog will use it

- Watchdog's gate already runs this process on its own copy of the scores (`docs/plans/watchdog-golden-corpus.md` §4):
  a disagreement between our scanner and a confirmed entry is filed as a case (maintainer tool
  `corpus_key_case_file`), decided by someone who is neither the claimant nor the labeller
  (`corpus_key_case_adjudicate`), and a "key wrong" verdict becomes an amendment in `tools/bench/keys/ledger.json`
  with a key version of the form `1.0.0+wd.1`.
- Such a version is **ours, not CAI's**: every bench result records the key version it was scored against, and a
  figure at `+wd.N` is never quoted as a CAI number.
- Every "key wrong" verdict on a training-set entry is forwarded to the coordinator through the path above, with the
  evidence the case already carries. When the coordinator publishes the corrected version, we move our pin to it; our
  bench refuses an amendment whose base key has moved, which forces the amendment to be re-examined against the
  published correction rather than silently applied twice.
- If the coordinator upholds the key, we record that against our case and retire the amendment in a new ledger version.
  The coordinator's decision on a CAI key outranks our adjudication of it.

## 4 Questions for the coordinator

1. Is a separate post-verdict window for scanner owners acceptable, or should every dispute use the reveal window?
2. Should training-set disputes be batched into a periodic set release (`training-set-2026` v1.1) rather than a new
   unit tag each?
3. Where should the dispute ledger live: in `scanner-benchmark`, or in the Code Assurance Index repository beside the
   publications it annotates?
4. Is an erratum record the right instrument on the Index, given that a publication allows no withdrawal and no
   replacement?

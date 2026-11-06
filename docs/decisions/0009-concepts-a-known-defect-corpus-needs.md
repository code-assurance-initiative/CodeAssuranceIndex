# Decision 0009: Add the 34 concepts a known-defect corpus needs to the taxonomy

- **Decision:** 0009
- **Date:** 2026-11-06
- **Decided by:** Jimmy Borch, for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0009: Add the concepts a known-defect corpus needs to the taxonomy](../proposals/0009-concepts-a-known-defect-corpus-needs.md)
  ([pull request #42](https://github.com/code-assurance-initiative/CodeAssuranceIndex/pull/42),
  [issue #41](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/41))
- **Outcome:** Accepted as written, its two questions answered below.
- **Supersedes:** nothing.

## Conflicts and recusals

The proposer and the decider are the same person, who maintains CCA, whose known-defect regression floor needs these
concepts to be scorable. The conflict is mild: adding a concept names a defect class in the shared taxonomy, which any
scanner can then be measured against; a scanner with no rule for a concept is mapped to it `unmapped` (contract 1.4),
which records a real miss rather than hiding one. So the change helps every producer be *measured* on these defects,
not only CCA. Nobody else can decide today, so nobody recused.

## Comment

None before adoption. **No CAI score moves** (adding a concept changes no codebase's score and no existing key); on
the benchmark, recall moves only for keys that use the new concepts, and no published key does today. It is therefore
treated as a number-neutral change: under the interim rule the **14-day** comment period runs after adoption, to
**2026-11-20**, and an unaffiliated objection reopens it.

## Reasons

- The benchmark could not have caught the failure that prompted this. Its units plant defects of the taxonomy's
  concepts, and several of the most common web-application defects — an identity taken from an unsigned cookie, a
  record anyone signed in could edit — have **no concept at all**, so no key or mapping can name them. On 2026-11-05 a
  CCA product found none of eight such planted defects and called the application exemplary.
- A taxonomy exists to name what a scanner should find. 112 of 291 must-fire entries in the new known-defect layer
  (eight documented vulnerable applications across seven languages) use one of 34 concepts the taxonomy lacks. Naming
  them is the taxonomy doing its job; leaving them out keeps a 38 % blind spot that only one producer can even record.
- The definitions are authored from each application's own documentation and the CWE text, with boundaries against the
  near concepts stated (for example `missing-ownership-check` vs `missing-authorization` vs `client-controlled-identity`),
  so a scanner is credited for the right finding, not a neighbouring one.

## Answers to the proposal's questions

1. **The 19 rare concepts enter now, not later.** A defect class is no less real for being rare in one corpus, and the
   definitions are already written from documented applications. Waiting would re-create the blind spot the proposal
   closes, and under the interim rule a concept that later proves ill-drawn can be amended or withdrawn by a follow-up
   decision. Entering them now lets keys and mappings name them immediately.
2. **`cleartext-storage-of-sensitive-data` stays a sibling of `insufficient-password-hashing`, not its parent.**
   Passwords have their own concept, and this one is "one named field the documentation says is left unprotected";
   making it a parent would risk double-crediting a password finding under both. The annex boundary (passwords are
   `insufficient-password-hashing`; the repository-wide posture is `data-encryption-controls`) is adopted as written.

## What took effect

- Proposal 0009's status and the proposal and decision indexes in this repository.
- The 34 definitions in the proposal's annex are adopted as the authoritative text of these concepts (CWE, OWASP Top
  10 2025 category and boundary each as the annex states).

## What did not yet

- **The taxonomy file lives in the benchmark repository, not here.** The 34 concepts are added to it there, from the
  adopted annex text, via that repository's own pull request; this decision is the authority for that edit. No
  published answer key uses the new concepts until a key is written against them.
- Each scanner mapping gains an `unmapped` entry per concept it has no rule for when its maintainer next builds it;
  that is the producer's step, not this decision's.

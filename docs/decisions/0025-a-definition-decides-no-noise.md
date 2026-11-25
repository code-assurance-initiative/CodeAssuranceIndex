# Decision 0025: A definition decides no noise

- **Decision:** 0025
- **Date:** 2026-11-25
- **Decided by:** Jimmy Borch (owner), for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0025: A definition decides no noise](../proposals/0025-a-definition-decides-no-noise.md)
- **Outcome:** Accepted as written.
- **Supersedes:** nothing; it states explicitly what [decision 0024](0024-a-definition-says-what-not-how.md) implied.

## Conflicts and recusals

The proposer and the decider are the same person. He maintains CCA and the Assure, Assert and Assay products built on
it, and owns code measured by CAI. **The decision runs against his engine's convenience**: the D4 exemption it rules
out would have raised D4 on his own vertical-slice code base through the standard; under this decision his engine may
still judge that noise, but CAI measures the judgement as the engine's precision, as for any other engine. Nobody else
can decide today, so nobody recused.

## Comment

None. The comment period is paused until the steering group exists (decision 0019).

## Reasons

- Noise is a judgement about whether a correct report is worth acting on in a given context. A standard that writes
  such judgements down as rules makes every engine apply one engine's judgement, and makes judging better a matter of
  asking CAI instead of being measured better.
- CAI already measures noise: blind judges and the holdout give each engine's precision, published beside its results.
  That is where an engine's judgement of noise is held to account.
- Bounds and thresholds that apply to every code base alike stay in definitions: they say what the property is.

## What takes effect

The paragraph in GOVERNANCE.md and the check in the proposal template, as the proposal states, from today. It moves no
number.

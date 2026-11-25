# Proposal 0025: A definition decides no noise

- **Number:** 0025
- **Filed:** 2026-11-25
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA, an engine that produces CAI evidence, and the Assure, Assert and
  Assay products of Canine Development built on it; the proposer also owns code measured by CAI. The change applies to
  every engine alike. The proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** no.
- **Status:** Accepted by [decision 0025](../decisions/0025-a-definition-decides-no-noise.md)

## Problem

[Decision 0024](../decisions/0024-a-definition-says-what-not-how.md) says a definition says what is measured, never
how an engine finds it. It does not say, in so many words, that deciding which findings are **noise** is part of the
"how". A draft written under 0024 (not filed) showed the gap: to stop D4 (*Code Duplication*) counting wiring repeated
across the slices of a vertical-slice code base, it first put the engine's method into the definition, and once that
was cut, it still asked for one sentence: *"Not counted: a copy of wiring … whose copies all sit in different slices of
a code base built in vertical slices or as a modular monolith."*

That sentence states no method, yet it does not belong in a definition either. It says that reporting those copies
would be noise in such a code base. Whether a correct report is worth acting on, in this architecture, this style or
this context, is a judgement, not a rule. Written into the standard, every such judgement becomes a rule that every
engine must apply alike, that an engine which judges better must ask CAI to change, and that grows by one exemption per
architecture and convention. CAI measures noise (by blind judges, and the holdout); it does not legislate it.

## Change

1. In [GOVERNANCE.md](../GOVERNANCE.md), after *How a definition is written*, add:

   > **A definition decides no noise** ([decision 0025](decisions/0025-a-definition-decides-no-noise.md)). Whether a
   > finding is noise (a report a maintainer should not act on, in that code base, its architecture, its conventions or
   > its context) is a judgement, and an engine makes it. A definition never exempts something because reporting it
   > would be noise somewhere: no exemption by architecture, style, framework, intent or kind of code base ("in a
   > vertical-slice code base, repeated wiring is not counted"). Such a sentence fails 0024's test, since an engine
   > that judges noise better would have to ask CAI first. A definition may still bound its population and set a
   > threshold at which something counts (tests, generated and vendored code excluded; a block of at least so many
   > lines), as long as they apply to every code base alike and say what the property is, not when reporting it is
   > useful. CAI measures each engine's judgement of noise as its precision ([ENGINE-QUALIFICATION.md](ENGINE-QUALIFICATION.md)),
   > and publishes it beside the engine's results. A change motivated by findings being noise is an engine change, and
   > is not a proposal.

2. In the proposal template's *Change* section ([proposals/README.md](README.md#template)), add: *"Check every sentence
   of a changed definition against GOVERNANCE.md, How a definition is written: it states no method and decides no
   noise. If the problem is that correct reports are noise somewhere, fix the engine instead; it is not a proposal."*

## Effect on published scores

None. No definition in the catalog today carries such an exemption; the D4 draft that would have added one is not filed.

## Alternatives considered

- **Leave it to 0024's test.** The test covers it, but the D4 draft passed a careful reading of 0024 twice; a rule that
  is only implied did not stop it.
- **Allow exemptions backed by evidence of precision.** Rejected: the evidence measures one engine's judgement, which
  is exactly what CAI measures afterwards; the standard would then freeze that engine's judgement for all.

## Questions for the decider

None.

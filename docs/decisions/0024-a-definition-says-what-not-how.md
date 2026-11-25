# Decision 0024: A definition says what is measured and how it is scored, never how an engine finds it

- **Decision:** 0024
- **Date:** 2026-11-24
- **Decided by:** Jimmy Borch (owner), for the CAI team (the acting maintainer, under the interim pragmatic rule of
  [decision 0011](0011-interim-pragmatic-governance.md))
- **Proposal:** [0024: A definition says what is measured and how it is scored, never how an engine finds it](../proposals/0024-a-definition-says-what-not-how.md)
- **Outcome:** Accepted as written, its request about decision 0013 answered below. Two rulings beyond the proposal
  are added and marked as such: an interim registration channel for engines, and a rule on how a product may refer to
  CAI's measurement of it.
- **Supersedes:** in [ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md) Part B, the pass rule, the *undecided*
  result, the grades, the recall threshold and noise ceiling left *to be set*, and the grade of record pooled over four
  windows. Part B's test material, its measures and their intervals, the holdout lifecycle and the publication of each
  window stay.

In this decision: a **dimension** is one property of code the rubric catalog defines (for example PF3, *Async &
latency hygiene*); a **scored** dimension is one the catalog does not declare advisory; a **rubric version** is a
numbered, frozen version of that catalog. An **engine** is software that scans a repository and produces the
**evidence bundle** a CAI is computed from, by CAI's **reference scorer**; a **reading** is how one engine measures
one dimension, and today the only reading of each dimension is CCA's. A **dimension page** is a page of CAI's
specification (`docs/spec/dimensions/<id>.md`) stating one dimension's unit, denominator, thresholds, curve and the
languages it applies to. **CCA** is Canine Code Analyzer, today the only engine that publishes CAI. The **holdout** is CAI's
sealed set of synthetic test repositories with an exact answer key, renewed each quarter (a **window**). A **cell** is
one scored dimension in one language. The **web front** is JavaScript, TypeScript, HTML and CSS. A **platform family** is a
set of languages of one platform the proposal's table lists (.NET: C#, F#, Visual Basic .NET; JVM: Java, Kotlin, Scala;
BEAM: Erlang, Elixir; Apple: Swift, Objective-C; every other language a family of one). The **95 % rule**: an engine
publishes a repository's CAI only where the languages it reads hold at least 95 % of the repository's production lines
(non-blank, non-comment lines of the languages on CAI's production-lines list, tests, generated and vendored code
excluded), and the CAI states that share. The **listing rule**: an engine that does not read a language's whole family and the web front may not list
that language as supported. Section numbers (§1-§7) are the proposal's.

## Conflicts and recusals

The proposer and the decider are the same person. He maintains CCA and the Assure, Assert and Assay products of
Canine Development built on it, and owns code measured by CAI. **The decision runs against his engine**: it takes CCA's method out of the standard's
definitions; it publishes CCA's measured noise and recall beside any other engine's; it reports CCA as *method not
published* unless CCA publishes its method; it withholds a CAI from repositories CCA does not read to 95 % (about
6.6 % of the 11,739 scanned corpus repositories today); and, from the window §6 names, it requires CCA to read
Objective-C or stop listing Swift (about 14.1 % without a CAI if it stops). Nobody else can decide today, so nobody
recused.

## Comment

None. The comment period is paused until the steering group exists
([decision 0019](0019-the-comment-period-is-paused-until-the-steering-group-exists.md)).

## Reasons

- A standard that states one engine's method must change whenever that engine, or any other, finds the same property
  better; every engine maker would have to ask CAI before improving, and CAI would protect one way of measuring
  instead of measuring code. Decision 0012 removed one engine's files and defaults from CAI; this removes it from the
  words of the definitions.
- Comparability between engines comes from agreeing on what is counted and how the count becomes a score, and from
  measuring how good each engine's findings are, not from every engine copying one recipe.
- A bar an engine must pass hides everything above and below it. A measurement CAI makes itself, on the same holdout
  for every engine, published with its interval and its trend, tells a buyer which engine is good at what, and gives
  every engine maker a reason to lower its noise.
- Precision is weighed above recall because a finding that cannot be trusted cannot be acted on, and noise a user
  must configure away is a cost no score shows; full recall is easy to reach by reporting everything.
- An engine that reads one language of a polyglot repository describes part of it as the whole; 28.6 % of the 11,161
  corpus repositories with a counted language hold two or more.

## Answers to the proposal's request

**Decision 0013 and the rewrites: yes**, a rewrite that states the same property in other words is not a re-definition
under decision 0013. Decision 0013's measurement shows how a new or changed property behaves on real code; a property
that does not change behaves as it did. What a same-property rewrite must show instead is that the property is the
same. The proposal shows it for each of the 48 rewrites: all 48 passed five blind judges (at least four of five with no
blocking issue under each criterion); X10 and X20 each keep one judge's blocking point, deliberately not followed
because following it would write the existing reading's method into the definition, and the decider accepts those two
reasons ([0024-judging.tsv](../proposals/0024-judging.tsv)). SC1's removed sentence described the scanning engine's
own environment, not a property of any repository, and its removal is a correction of that kind.

## Rulings beyond the proposal

1. **Registration.** Until a window's protocol names another channel, an engine registers to be measured, and files
   its record, by an issue titled *Engine registration* in this repository, by the date the window's protocol sets.
   Reason: §3 requires engines to register and submit, and names no channel; one is needed before the first window.
2. **How a product refers to CAI's measurement** ([IPR.md](../IPR.md)): a product may quote CAI's measurement of it only
   as its engine report states it, and may not call itself *qualified* or *certified* by CAI, nor present its own
   figures as CAI's. Reason: qualification no longer exists, and §3 makes CAI's measurement the only one that counts.

## What took effect

- Proposal 0024's status, and the proposal and decision indexes.
- [ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md), Part B changed as §3 and §5 state, with *When each part
  applies* repeating §6: no pass, grade or ceiling; CAI's measurement on the holdout and on real code; the engine
  report and score; the languages an engine must read; the production-line share.
- [GOVERNANCE.md](../GOVERNANCE.md): how a definition is written (§1, §2, §4), stated as applying from the rubric
  version that carries the rewrites. [README.md](../../README.md): engines are measured, not qualified, from the window
  §6 names.
- The two rulings above.

## What did not yet take effect

- **The dimension pages and the production-lines list** (§1, §5.4): written, judged and published in
  `docs/spec/dimensions/` and `docs/spec/production-code.md` (whose last section is the production-lines list), every scored dimension first. Each page states the unit,
  denominator, thresholds and curve that produce today's published scores; a parameter of the one reading's model that
  a page must state is marked *pending re-definition*, and for each such page a re-definition under decision 0013
  follows; the score does not change until it is decided.
- **With the first rubric version published after the pages of every scored dimension and the production-lines list**
  (the pages of advisory dimensions follow later): §1, §2 and §4; the 48 rewritten definitions
  ([0024-rewrites.tsv](../proposals/0024-rewrites.tsv)); the 95 % rule (§5.4); the evidence bundle's
  `productionLineShare` field, which the scoring specification adds; the reference scorer's refusal of a bundle
  without it or under 0.95; and CAI's check of the share in each window's run on the holdout (a provisional run until
  the covering window below). A CAI published below 95 %, or with a share that does not hold, is not a valid CAI. Until §5.1-5.3 apply, the languages an engine reads under §5.4 are those it
  parses and states, with the share, in `productionLineShare`. Until this rubric version, every published score stands
  under the rubric version its delivery names.
- **From the first window whose holdout covers the languages §3 names** (every language in the platform family of
  every language supported by an engine that publishes CAI or is registered to be measured in the next window, the
  web front, and C, C++, Objective-C and R; planned for the first window of 2027; if none is sealed by then, the first
  window that has one): CAI's quarterly measurement on the holdout and on real code, the engine report and score (§3),
  and the family, web-front and listing rules (§5.1-5.3, §5.5); and from then only the languages an
  engine lists as supported count as read under the 95 % rule, a language it parses but may not list not counting.
  Until then, the current holdout's results are published as provisional figures, with no pass, grade or ceiling.
- **Engines' part**, each from the date above that governs it. With the rubric version: emit `productionLineShare`,
  state the share in every CAI it publishes, and publish no CAI below 95 %. From the covering window: declare the languages and cells it measures, a dimension it does not declare reading
  *not measured*, in its report and in every CAI it publishes, never as a score; submit its
  current release to every window (image by digest, its default shipped configuration and its mapping, frozen when the
  window opens); publish CAI only from a release CAI has measured, and no new CAI after two missed windows in a row
  until measured again; read the whole family and the web front of every language it lists, only listed languages then counting towards
  its 95 %. Its method is described
  in its own public documentation, or its report reads *method not published*.

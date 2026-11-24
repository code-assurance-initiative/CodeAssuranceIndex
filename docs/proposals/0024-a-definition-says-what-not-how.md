# Proposal 0024: A definition says what is measured and how it is scored, never how an engine finds it

- **Number:** 0024
- **Filed:** 2026-11-24
- **Proposer:** Jimmy Borch, for the CAI team.
- **Declared interests:** the proposer maintains CCA (Canine Code Analyzer), today the only engine that publishes CAI
  results, and the commercial products Canine Development ApS builds on it (Assure, Assert and Assay, which report
  CCA's findings to their customers); the proposer also owns code measured by CAI. **The change runs against the
  proposer's engine**: it takes CCA's way of finding things out of the standard, so CCA no longer defines what every
  other engine must do, and it publishes CCA's own measured noise and recall per dimension beside every other
  engine's. The proposer is also the acting maintainer, i.e. the decider
  ([decision 0011](../decisions/0011-interim-pragmatic-governance.md)).
- **Moves scores:** no score changes: each rewritten definition states the same property, and every reading CCA uses
  today remains a valid reading of it. Three parts change what is published: from the rubric version §6 names, a
  repository whose production code an engine reads less than 95 % of gets no CAI from that engine (§5.4); from the
  first measurement window §6 names, each engine's measured noise and recall are public (§3), an engine publishes CAI
  only once measured, and an engine that does not read a language's whole family and the web front may not list that
  language as supported (§5.5).
- **Status:** Accepted by [decision 0024](../decisions/0024-a-definition-says-what-not-how.md)

## Terms

- **CAI** (the Code Assurance Index) is a score of a repository's code, computed by a public, fixed arithmetic from
  the evidence an engine produces. Its **rubric catalog** lists the dimensions; each version has a number
  (rubric-2026.11.5 is the current one).
- A **dimension** is one property of code the catalog defines, named by an id (for example PF3, *Async & latency
  hygiene*) and described by its definition, the catalog's `whatItMeasures` text. In this proposal a **scored**
  dimension is one the catalog does not declare advisory, whatever an evidence bundle marks: 146 of the 162 in
  rubric-2026.11.5 (32 of family *dimension* and 114 of family *meta*). An advisory one is reported only.
- An **engine** is software that scans a repository and produces that evidence (CAI's other documents also say
  *producer*; this proposal says engine throughout). A **reading** is how one engine measures one dimension.
- The **holdout** is CAI's sealed set of small synthetic repositories whose answer key labels exactly what is in them:
  planted defects, traps (code that looks like a defect and is not), clean code, and code a concept does not apply
  to. It is renewed every quarter (a **window**) and revealed after it
  ([ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md), Part B).
- **Recall** is planted defects found ÷ planted defects. **Noise** is the share of an engine's findings that fall on
  traps, on clean or not-applicable code, or match nothing the key expects. **Trap resistance** is traps left alone ÷
  traps. Some properties are judged rather than located; for them the key gives a **score band** (an expected score
  range), and **band agreement** is the share of those entries where the engine's score falls inside the range.

## Problem

A standard that describes one engine's method has to change every time that engine, or any other, finds the same
property better. Then every engine maker improving its engine must ask CAI first, and CAI ends up protecting one way
of measuring rather than measuring code. [Decision 0012](../decisions/0012-the-standard-names-no-producer.md) removed
one engine's files, defaults and promotion from CAI; the same coupling is still present in the words of the catalog.

In rubric-2026.11.5, 48 of the 162 definitions say more than what is measured:

| What the definition carries | Definitions | Example |
|---|---|---|
| One language's syntax used to define a property found in several languages, or more words than the property needs | 20 | PF3 (*Async & latency hygiene*): *".Wait()/.GetAwaiter().GetResult() ... block_on ... runBlocking"* |
| A named tool or framework as the means, not as an example | 9 | D43 (*Malicious Dependencies*): *"in any ecosystem osv-scanner reads"* |
| An engine's internal design-decision number | 7 | R4 (*Test Coverage*): *"Static test reachability (D-386)"*, where D-386 is a CCA design decision |
| An engine's own voice or components | 7 | AXH1 (*Runtime security headers*): *"booting it in our no-coupling sandbox"*; SC1 (*Supply-chain hygiene*) describes CCA's build sandbox, which SC1 does not even read |
| An algorithm, a parser or a graph | 3 | R10 (*Code Duplication*): *"the D4 clone algorithm over JS/TS tokens"* |
| One engine's coverage | 2 | AC6 (*Visual & motion safety*): *"PARTIAL: inline styles, in-repo `<style>` blocks ... are read"* |

The same pull shows in proposals: a re-definition is written as one engine's recipe (half-lives, statement counts,
which methods are nodes of a graph, git flags) *"stated so another producer can reproduce it"*. Reproducing a recipe
is not what makes two engines comparable. Agreeing on what is counted and how the count becomes a score, and
measuring how good each engine's findings are, does.

## Change

### 1. What a definition holds

A dimension's definition states, and every engine applies alike:

1. **The property**: what in the code is found or measured, in words a developer can check against the code.
2. **The unit and the denominator**: what is counted, over what (methods, files, production lines, repositories).
3. **The threshold at which something counts**, where one decides the result (*six responsibilities* in a class for
   D6, *Cohesion*; *90 % of a file's knowledge* for D16, *Bus Factor*; *30 lines* of a copy for R10). A threshold on
   the property belongs to the standard; a parameter of an engine's model does not (a half-life, a decay floor, a
   weight per changed file, a statement count that decides what a node of a graph is).
4. **The score curve**, and what is outside the population (for example: production code only, generated code
   excluded).
5. **What the result is not**, where readers would otherwise over-read it (*"presence of committed policy, not a
   runtime guarantee"*).

The catalog's one-paragraph definition (`whatItMeasures`) states 1, the population of 4 and 5, and any threshold of 3
that decides what is found. The unit, the denominator, every threshold, the score curve, and the languages the
dimension **applies to**, are stated in full on a **dimension page** in CAI's specification
(`docs/spec/dimensions/<id>.md`), one page per dimension. Today CAI's
specification holds the fold above the dimensions (the catalog's scoring constants for categories, lenses, the
critical gate and the bands), and takes each dimension's 0-10 score as given in the evidence bundle; how that score is
computed from what the code shows (the unit, the denominator, the thresholds and the curve) is stated per dimension
only in the one existing engine's own method document, which is not public. So the maintainers write the pages of every scored dimension,
each stating the unit, denominator, thresholds and curve that produce today's published scores (described from the
one existing reading, without its method), each judged as the rewrites were (§ Evidence), and publish them in CAI
first; the rubric version that carries this proposal's rewrites (§7) is published only after them, and the pages of
advisory dimensions follow. A page states no method. A page that would change any published score is not a page of
this proposal: it is a re-definition, decided under decision 0013. Where today's score of a dimension cannot be stated
without a parameter of the one reading's model (a half-life, a decay floor, a weight), the page states that parameter
as part of today's curve, marked *pending re-definition*, and a re-definition under decision 0013 follows; the score
does not change until it is decided.

### 2. What a definition never holds

- how an engine finds the property: algorithms, parsers, graphs, models, and windows and weights internal to the
  method;
- a tool as the means. A tool or framework may appear only as a non-exhaustive example, marked *for example*;
- one language's syntax as the definition. A dimension that applies to one language may name that language's
  construct; a dimension over several languages states the construct in general words, with syntax at most as an
  example;
- an engine's name, voice (*our*, *we*), components or decision numbers (extending to the definitions what decision
  0012 did for CAI's files, defaults and promotion);
- an engine's coverage (which languages, ecosystems or file kinds it reads). Coverage is per engine and is stated in
  its record (§3);
- more words than the property and §1 need. Why the property matters is said in one clause at most.

**The test for any sentence:** would an engine that finds the same property better, or the same way faster, have to
ask CAI first? If yes, the sentence is a method and does not belong in the definition.

### 3. How a reading is judged: by CAI's own measurement, published per engine

CAI sets no bar a reading must pass. It measures every engine itself, the same way, every quarter, and publishes the
result, so a reader can see which engine is good at what.

**The engine's part.** An engine that publishes CAI declares, in its record, the languages it supports and the
(dimension, language) cells it measures (a cell is one scored dimension in one language), and submits the release it
publishes from before each window as [ENGINE-QUALIFICATION.md](../ENGINE-QUALIFICATION.md)'s *Running an engine*
says, unchanged: the image by digest, its configuration and its mapping from rule ids to the benchmark's concepts,
hashed and frozen when the window opens (or a hosted run, recorded as such). The configuration measured is the one
the engine ships to its users by default. It submits its current release to every window, and publishes CAI only from a release CAI has measured. A
window it misses is shown in its report as *not measured this quarter*; after two missed windows in a row it publishes
no new CAI until CAI has measured its current release again.

**CAI's measurement.** CAI runs that release over the window's holdout once and computes, per dimension and language,
recall, noise (as a share of findings), trap resistance and band agreement as Part B defines them, each with its
Wilson 95 % interval. This measurement is the one that counts: an engine's own figures, higher or lower, are never
published as CAI's.

**On real code too.** For every engine that publishes CAI, CAI also measures noise on real code each window, by the
method of the noise standard ([codeassuranceindex.info/noise](https://codeassuranceindex.info/noise/)): a sample of
public repositories drawn before any engine runs, CAI running the engine at the pinned revisions, and blind judges
from more than one model family giving each sampled finding one of the standard's verdicts. It shows whether an
engine's holdout figures hold on real code, and is shown in the engine's report beside them. A tool that does not
publish CAI may still submit its own noise run under the noise standard; such a result is labelled *self-measured*
and never appears in an engine report.

**The holdout's languages.** The holdout covers every language in the platform family (§5) of every language an
engine publishing CAI, or registered to be measured in the next window, supports, the web front, and C, C++, Objective-C and R, which no engine publishing CAI supports
today (C and C++ hold 5 % or more of the production code in 3.0 % and 2.6 % of 11,739 scanned repositories;
Objective-C is in the Apple family; R is included by the decider). A further language is added by a proposal. Every
engine is measured on all of them, so its report also says how it does on languages it does not support.

**The report, per engine**: per dimension and language, this quarter's figures and a **trend graph** of noise and
recall over every quarter measured; the supported languages marked apart from the ones only measured; and a link to
the engine's own description of its method (§4). A dimension the engine does not declare reads *not measured*, in its
report and in every CAI it publishes. A declared dimension is never withheld for a high noise figure: its figure, its
interval and its trend are shown.

**The engine score**, so a buyer can compare engines without reading every row:

- per (scored dimension, supported language) cell, the F-score with β = 0.5:
  `F0.5 = 100 × 1.25 · P · R / (0.25 · P + R)`, where `R` is recall and `P = 1 − noise`, from this quarter's
  measurement. β = 0.5 counts recall half as much as precision, on purpose. CAI exists to give an overview that is
  correct and can be acted on; a finding that cannot be trusted cannot be acted on, and noise an engine leaves for the
  user to configure away is cost moved onto the user that no CAI shows. Reporting everything earns full recall but
  little precision, and reporting nothing earns no recall: F0.5 rewards neither, and leans towards precision. Recall
  100 % at 80 % noise scores 24; recall 60 % at 25 % noise scores 71. A declared cell where the engine reports nothing,
  or reports only noise (P = R = 0), scores 0. A cell in which the window's holdout plants no defect (including a
  dimension measured only by score bands) has no F0.5: it is listed apart with its band agreement or trap resistance,
  and counts neither in the mean nor in coverage.
- The engine score is the unweighted mean of the F0.5 of its declared cells, shown beside its **coverage**: declared
  cells ÷ the cells of scored dimensions that apply (by their dimension pages) to its supported languages and that the
  holdout plants defects in. Its 95 % interval is a percentile bootstrap that resamples the holdout's repositories
  within each language, 10,000 times, with the seed published. So an engine covering few cells well and one covering
  many are both visible, and neither hides the other. Results on languages the engine does not support are shown
  beside the score, not in it.

**Part B of ENGINE-QUALIFICATION.md** changes accordingly: the pass rule, the *undecided* result, the grades, the recall
threshold and noise ceiling left *to be set*, and the grade of record pooled over four windows are retired; the
engine's *record* becomes its declared languages and dimensions plus CAI's reports; the test material, the measures,
their intervals, the holdout lifecycle and the publication of each window stay.

### 4. Where an engine's method lives

An engine describes its method (how it finds each property: models, windows, weights, graphs, tools) in its own
documentation, publicly readable. CAI's report for the engine links to it and does not hold it, so an engine improving
its method changes nothing in CAI. An engine with no public description of its method is reported as *method not
published*.

A proposal to re-define a dimension changes the definition (§1) and brings the evidence decision 0013 requires. The
engine's method used for that evidence is stated in the proposal as evidence, for information, and never becomes the
definition.

### 5. The languages an engine publishing CAI must read

Repositories are often polyglot. In the corpus CCA measures (open-source repositories sampled per language, and a
few customer repositories), 11,161 repositories have at least one language with 300 production lines and 5 % of the
production code; about 29 % of them (3,195, 28.6 %) hold two or more such languages; counting smaller code would raise that
number. JavaScript or TypeScript is the most common second language: it is in 21-31 % of the Python, Rust, PHP, Go,
Java and Ruby repositories and 17 % of C# ([0024-languages-together.md](0024-languages-together.md)). An engine that reads only one
of them reads the others as nothing, and a CAI of such a repository describes part of it as the whole. So:

1. **Every language an engine supports comes with its platform family.** The families are exactly those in this
   table (languages that run on one platform and are written side by side in one code base); a language not listed is
   a family of one until a proposal adds it to a family:

   | family | languages |
   |---|---|
   | .NET | C#, F#, Visual Basic .NET |
   | JVM (the Java virtual machine) | Java, Kotlin, Scala |
   | BEAM (the Erlang virtual machine) | Erlang, Elixir |
   | Apple | Swift, Objective-C |
   | a family of one | Go, Rust, Python, Ruby, PHP, Dart, and any language in no row above |

   A language called from another through a native interface (C beside Swift, C++ beside Dart, Rust beside Python) is
   not in that language's family: it is its own family, and counts in the 95 % rule below like any other code.
2. **And always the web front**: JavaScript, TypeScript, HTML and CSS.

   An engine **reads** a language when it parses that language's production code, so that code is measured and not
   passed over; which dimensions it measures in it is what it declares as cells (§3), and its coverage shows that.
3. **A specialised engine is welcome.** One family plus the web front is a complete engine: .NET plus the web front,
   or Go plus the web front. An engine for one language without the rest of its family or without the web front is
   not: C# alone is not.
4. **A CAI only where the engine reads the repository.** An engine publishes a repository's CAI only where the
   languages it reads hold at least 95 % of the repository's production lines, and the CAI states that share.
   *Production lines* are the non-blank, non-comment lines of the repository's files in the languages on CAI's
   production-lines list (`docs/spec/production-lines.md`, published with the dimension pages): general-purpose
   programming languages, HTML and CSS. SQL, shell, build scripts (for example Dockerfiles, Makefiles) and data and
   configuration files (for example JSON, YAML) are not on it. The same page states the one rule by which test,
   generated and vendored files are excluded. The engine computes the share and records it in a new
   field of the evidence bundle, `productionLineShare` (the share, the production lines per language, and the version
   of the production-lines list used), which CAI's scoring specification adds in the same rubric version; CAI's
   reference scorer refuses a bundle without the field or with a share under 0.95, and CAI's quarterly run checks the
   share on the holdout. A CAI published below 95 %, or with a share that does not hold, is not a valid CAI.
5. **An engine that does not meet 5.1-5.2 may not list a language as supported** until it reads that language's
   family and the web front. From §6's date, the languages it reads in §5.4 are only the languages it lists as
   supported: a language it parses but may not list does not count towards the 95 %.

The families and the 95 % are reviewed by the maintainers once a year, against the corpus's co-occurrence figures, by
a proposal; a proposal that adds a language an engine supports names its family.

### 6. When each part takes effect

- §1, §2, §4, the rewrites (§7), the 95 % rule (§5.4) with the `productionLineShare` field and the reference
  scorer's refusal: with the first rubric version published after the decision and after the pages of every scored
  dimension and the production-lines list (§1, §5.4). Until §5.1-5.3 apply, *the languages it reads* in §5.4 are the languages the engine parses in the sense of §5.2 and
  states, with the share, in `productionLineShare`.
- §3, §5.1-5.3 and §5.5: from the first window whose holdout covers the languages §3 names, planned for the first
  window of 2027; if none is sealed by then, from the first window that has one. Until then, the current holdout's
  results are published as provisional figures, with no pass, grade or ceiling.

### 7. The rewrites

The rubric version after the decision carries the rewritten definitions in
[0024-rewrites.tsv](0024-rewrites.tsv): each row its current text, its proposed text and the rule of §2 it applies.
Score constants, lenses, families and polarity are untouched.

## Evidence

Decision 0013 requires a corpus measurement for a proposal that adds, re-defines or retires a dimension. **This
proposal asks the decider to rule that a rewrite stating the same property in other words is not a re-definition under
decision 0013**: the measurement 0013 asks for shows how a new or changed property behaves on real code, and a
property that does not change behaves as it did; what must be shown instead is that each property is the same, which
the review below does. All 162 definitions were screened
for decision numbers, engine voice, named tools, method words, code syntax and length (55 flagged), and each of the
162 was then read by hand: 36 of the 55 flagged are rewritten, 19 are kept because their length states the property
and what counts (D35, *Change Coupling*, keeps the time window decision 0021 set: the last 20 changes of the less
often changed file, within the year, is a threshold on the property, not a method), and 12 the screen did not flag are
rewritten too. For each rewrite, the text that states what CCA's reading inspects was compared with the proposed
definition, so that no rewrite claims more than the current definition does; SC1 (*Supply-chain hygiene*) is
the one case where a sentence of the current text is removed rather than reworded: *"the deny-by-default egress sandbox
the build runs in"* describes the scanning engine's own environment, which no repository has or lacks, so it is engine
voice under §2 and states no property of the code; SC1's property, what the repository itself declares, was and stays
whether it pins its dependencies. Each rewrite was then judged by
five blind judges for being correct, complete and understandable, against its current text and that reading, each
judge in a fresh session that saw neither the proposer nor the other judges. A rewrite passed when at least four of the
five found no blocking issue (a factual error, a wrong scope, or something the reader needs and cannot do without)
under each criterion; every blocking issue any judge raised was then fixed and judged again, except one each in X10
and X20 that was deliberately not followed (below). All 48 passed, after
two to nine rounds; in their final round 46 carry no blocking issue from any judge, and two (X10, X20) each carry one
that was deliberately not followed, because following it would write the one existing reading's method into the
definition; the reasons are recorded with each ([0024-judging.tsv](0024-judging.tsv)). The judges' findings changed most rewrites: the first drafts often narrowed a
definition to what the one existing reading inspects, which this proposal forbids, and those were put back to the
current scope. Where the current definition claims more than that reading inspects, the definition keeps it and the
difference is recorded as that engine's gap ([0024-cca-gaps.tsv](0024-cca-gaps.tsv)).

For §5.4, measured on 2026-11-24 over 11,739 scanned repositories (the newest scan of each; the languages CCA has
frontends for counted as read, HTML and CSS not counted): those languages hold under 95 % of the production code in 771
(6.6 %). Among the languages CCA does not read, C holds 5 % or more of the code in 356 repositories, C++ in 309 and
Objective-C in 86. The exact set under §5.4 is known once the production-lines list is published.

## Consequences the decision should see

1. **Every engine's weak dimensions become public.** The quarterly report shows each engine's noise and recall per
   dimension, CCA's included, and the engine score falls with a noisy reading. CCA's reading of D6 (*Cohesion*) in
   draft proposal 0023, for example, was judged a false positive by five blind judges in 4 of 5 sampled findings on
   its held-out set of 272 repositories (80 %, 95 % interval 38-96 %); CAI's own measurement on its holdout is what
   CCA's report will show, whether higher or lower.
2. **CCA does not read Objective-C**, so from §5.5's date it must read it, or stop listing Swift as supported; then
   Swift code no longer counts as read, and a repository gets no CAI from CCA where its Swift code takes the languages
   CCA reads under 95 %. It has frontends for every other family's languages and for JavaScript and
   TypeScript, and reads HTML and CSS for the dimensions that apply to markup (accessibility). Under §5.4 it publishes
   no CAI for about 6.6 % of repositories by today's measurement (above), counting Swift as read; if it stopped
   listing Swift, the same measurement gives 14.1 % (1,656 of the 11,739).
3. **CCA's method must be public.** CCA's method document is today in a private repository. It holds two things: the
   unit, denominator, thresholds and curve of each dimension, which move to CAI's dimension pages (§1); and how CCA
   finds each property (its models, windows, weights and graphs), its method, which under §4 CCA publishes in its own
   documentation, or its report reads *method not published*.
4. **Twenty dimensions are a single bug pattern** (X13-X32, each detecting one specific mistake: a negated *or*-guard,
   a lock released on a flag, a loop that applies the first element's answer to all of them). Their definitions cannot
   be made broader without becoming other dimensions. A separate proposal, after this one, folds them into a few
   broader dimensions (contradictory logic, unsafe concurrency, state that is not restored, unbounded input, a
   contract that misleads its caller), with the corpus measurement decision 0013 requires; it moves scores, because it
   changes the categories and their weights.

## Decided in drafting (the acting maintainer, 2026-11-24)

1. No noise ceiling: CAI itself measures every engine on its holdout every quarter and its measurement is the one that
   counts; the holdout covers every supported language and family plus C, C++ and R; a public report per engine shows
   noise and recall per dimension and language with their trend; the engine score weighs precision over recall
   (F0.5) (§3).
2. An engine's method lives with the engine, linked from CAI (§4).
3. X13-X32 are folded into broader dimensions by a separate proposal (consequence 4).
4. An engine publishing CAI reads the platform family of every language it supports and the web front; a CAI is
   published only where the engine reads at least 95 % of the production code, and states the share (§5); each part
   takes effect as §6 says.
5. The noise standard's real-code measurement is run by CAI itself for every engine that publishes CAI; a tool outside
   CAI may still submit its own run, labelled *self-measured*, never in an engine report (§3).

# Engine measurement — how CAI measures every engine that publishes a CAI

**Status: draft.** This document describes how CAI measures engines, as
[decision 0024](decisions/0024-a-definition-says-what-not-how.md) set it. It is not yet in operation: the public
benchmark sets and their scoring harness exist ([scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark)),
and a first holdout is sealed (window 2026-q4: 40 synthetic repositories in C# and TypeScript). It does not yet cover
the languages *The holdout's languages* names, so until the parts below take effect its results, and those of any
holdout before them, are published at reveal as provisional figures, with no pass, grade or ceiling. Numbers marked *to be set* are fixed by the maintainers in each window's protocol (step 1 of
the holdout lifecycle) and published before the window opens.

**When each part applies** (decision 0024 §6):

- The 95 % rule, the `productionLineShare` field of the evidence bundle and the reference scorer's refusal: with the
  first rubric version published after decision 0024 and after the dimension pages of every scored dimension and the
  production-lines list. Until the family rules apply, *the languages it reads* are the languages the engine parses and
  states, with the share, in `productionLineShare`.
- CAI's quarterly measurement on the holdout and on real code, the engine report and score, the engine's
  declaration and submission, the rule that an engine publishes CAI only from a release CAI has measured, the
  missed-window rule, the family and web-front rules and the listing rule: from
  the first window whose holdout covers the languages *The holdout's languages* names, planned for the first window of
  2027; if none is sealed by then, from the first window that has one.

## Terms

- **CAI** (the Code Assurance Index) is a score of a repository's code, computed by a public, fixed arithmetic from
  the evidence an engine produces. Its **rubric catalog** lists the dimensions; each version has a number
  (rubric-2026.11.5 is the current one).
- A **dimension** is one property of code the catalog defines, named by an id (for example PF3, *Async & latency
  hygiene*) and described by its definition, the catalog's `whatItMeasures` text. In this document a **scored**
  dimension is one the catalog does not declare advisory, whatever an evidence bundle marks: 146 of the 162 in
  rubric-2026.11.5 (32 of family *dimension* and 114 of family *meta*). An advisory one is reported only.
- An **engine** is software that scans a repository and produces that evidence (CAI's other documents, and the section *Publishing your reports* below, also say *producer*; it means the same
  as engine). A **reading** is how one engine measures one dimension.
- The **holdout** is CAI's sealed set of small synthetic repositories whose answer key labels exactly what is in them:
  planted defects, traps (code that looks like a defect and is not), clean code, and code a concept does not apply
  to. It is renewed every quarter (a **window**) and revealed after it (see Part B and *The holdout lifecycle* below).
- **Recall** is planted defects found ÷ planted defects. **Noise** is the share of an engine's findings that fall on
  traps, on clean or not-applicable code, or match nothing the key expects. **Trap resistance** is traps left alone ÷
  traps. Some properties are judged rather than located; for them the key gives a **score band** (an expected score
  range), and **band agreement** is the share of those entries where the engine's score falls inside the range.


## Why CAI measures engines

A CAI is computed from evidence. The arithmetic is open and deterministic: the same evidence under the same rubric
version always folds to the same score. The evidence is not. It is produced by an **engine** — a scanner, an analyzer,
a pipeline of tools — that reads a codebase and reports what it found. Two engines reading the same code will never
report exactly the same findings, and the standard does not ask them to.

What a buyer of a CAI needs to know is narrower and answerable:

1. **Does the engine find enough of what is there?** (recall)
2. **Does it stay quiet about what is not?** (trap resistance and noise)
3. **Does it turn its findings into the right score?** (scoring conformance)

CAI answers those three questions, per dimension and per language, with a dated, public report per engine. It
measures **engines**. It does not certify **codebases**: a CAI remains a measurement of one codebase at one commit,
and a dispute about a finding in that codebase still goes to the issuer of that survey ([CHALLENGE.md](CHALLENGE.md)).

## Two tests, deliberately separate

| | Part A — Scoring conformance | Part B — Detection measurement |
|---|---|---|
| Question | Does the engine compute CAI correctly from evidence? | Does the engine find real defects and leave look-alikes alone? |
| Nature | Deterministic | Statistical |
| Test material | Public conformance vectors | Sealed holdout, revealed after the window |
| Pass rule | Exact reproduction | None: CAI's measurement is published, with its intervals and its trend |
| Attempts | Unlimited | One per engine version per window |
| Result | Pass / fail per rubric version | Recall, noise, trap resistance and band agreement per dimension and language, and an engine score |

They are separate because they fail for different reasons. An engine with excellent detection can still roll its
findings up wrongly; an engine with flawless arithmetic can still miss half of what is there. Neither result may hide
the other.

## Part A — Scoring conformance

The Initiative publishes **conformance vectors** for each rubric version: evidence bundles together with the dimension
scores, lens scores, headline CAI and band that the published algorithm produces from them. They cover the ordinary
path and the edges — absent dimensions, critical gates, band boundaries, rounding.

An engine passes Part A for a rubric version when, for every vector, its own output path reproduces every dimension
score, every lens score, the headline and the band **exactly**, at the precision the specification defines. There is
no tolerance beyond that precision. An engine that uses the [reference scorer](../src/Cai.Scoring) unmodified must still
pass: the test is of the engine's output, not of the library it links.

Part A is open and repeatable because it cannot be gamed. Computing the right number is the whole of what it asks.

## Part B — Detection measurement

### The test material

Each window uses a fresh **holdout**: a set of small synthetic repositories, each with an answer key that labels
exactly what is in it, in the format of the scanner-benchmark contract:

- `must-fire` — a planted defect;
- `must-not-fire` — a trap: code that looks like a defect and is not;
- `clean` — code certified free of the listed concepts;
- `not-applicable` — a concept with nothing to measure in that repository;
- `score-band` — an expected score range for a property that is judged rather than located.

Synthetic repositories are used because only then is the truth known exactly — both what should be found and what
must not be. A real codebase cannot tell you its own false negatives.

### What is measured

For every dimension, in every language the holdout covers:

- **Recall** — planted defects found ÷ planted defects.
- **Trap resistance** — traps left alone ÷ traps.
- **Noise** — findings on traps, on certified-clean code, on not-applicable concepts, or matching nothing the key
  expects. Noise is reported both as a share of findings and as the **number of repositories affected**, so a single
  noisy repository cannot dominate the figure.
- **Band agreement** — the share of `score-band` entries where the score the engine's evidence produces falls inside
  the expected range. This is the figure that ties detection to the number a buyer reads.

Every rate is published with its **95 % confidence interval** (Wilson). A rate without its interval invites a
precision the sample does not have.

### No bar: the measurement is the result

CAI sets no recall threshold and no noise ceiling. It runs every engine that publishes CAI over each window's holdout
itself and computes, per dimension and language, recall, noise (as a share of findings), trap resistance and band
agreement, each with its Wilson 95 % interval. That measurement is the one that counts: an engine's own figures,
higher or lower, are never published as CAI's. A dimension the engine declares is never withheld for a high noise
figure; its figure, its interval and its trend are shown, and the engine score falls with it.

### The holdout's languages

The holdout covers every language in the platform family of every language an engine publishing CAI, or registered
to be measured in the next window, supports; the web front (JavaScript, TypeScript, HTML and CSS); and C, C++,
Objective-C and R, which no engine publishing CAI supports today. A further language is added by a proposal. Every
engine is measured on all of them, so its report also says how it does on languages it does not support. The
families are exactly those of decision 0024 §5 (a language not listed is a family of one until a proposal adds it to
a family):

| family | languages |
|---|---|
| .NET | C#, F#, Visual Basic .NET |
| JVM (the Java virtual machine) | Java, Kotlin, Scala |
| BEAM (the Erlang virtual machine) | Erlang, Elixir |
| Apple | Swift, Objective-C |
| a family of one | Go, Rust, Python, Ruby, PHP, Dart, and any language in no row above |

The families and the 95 % are reviewed by the maintainers once a year, against the corpus's co-occurrence figures, by
a proposal; a proposal that adds a language an engine supports names its family.

### The engine score

So that a buyer can compare engines without reading every row:

- per (scored dimension, supported language) cell, the F-score with β = 0.5:
  `F0.5 = 100 × 1.25 · P · R / (0.25 · P + R)`, where `R` is recall and `P = 1 − noise` (noise as a share of the
  engine's findings), from that window's measurement. β = 0.5 counts recall half as much as precision: a finding that
  cannot be trusted cannot be acted on, and noise left for the user to configure away is cost no CAI shows. Recall
  100 % at 80 % noise scores 24; recall 60 % at 25 % noise scores 71. A declared cell where the engine reports nothing,
  or reports only noise (P = R = 0), scores 0. A cell in which the window's holdout plants no defect (including a
  dimension measured only by score bands) has no F0.5: it is listed apart with its band agreement or trap resistance,
  and counts neither in the mean nor in coverage.
- The engine score is the unweighted mean of the F0.5 of its declared cells, shown beside its **coverage**: declared
  cells ÷ the cells of scored dimensions that apply (by their dimension pages, `docs/spec/dimensions/<id>.md`) to its
  supported languages and that the holdout plants defects in. Its 95 % interval is a percentile bootstrap that
  resamples the holdout's repositories within each language, 10,000 times, with the seed published. Results on
  languages the engine does not support are shown beside the score, not in it.

A **scored** dimension here is one the catalog does not declare advisory, whatever an evidence bundle marks.

### Coverage and the languages an engine must read

An engine declares, in its record, the languages it supports and the (dimension, language) cells it measures (a cell
is one scored dimension in one language). A dimension it does not declare reads **not measured**, in its report and in every CAI it publishes, never as a
score. An engine **reads** a language when it parses that language's production
code, so that code is measured and not passed over. A language called from another through a native interface (C
beside Swift, C++ beside Dart, Rust beside Python) is not in that language's family: it is its own family, and counts
in the 95 % rule like any other code.

- Every language an engine supports comes with its platform family (the table above), and always with the web front.
  One family plus the web front is a complete engine (.NET plus the web front; Go plus the web front); one language
  without the rest of its family or the web front is not. An engine that does not read a language's whole family and
  the web front may not list that language as supported; once this rule applies, only the languages an engine lists as
  supported count as read for the 95 % rule, and a language it parses but does not list does not count.
- An engine publishes a repository's CAI only where the languages it reads hold at least 95 % of the repository's
  production lines, and the CAI states that share. Production lines are the non-blank, non-comment lines of files in
  the languages on CAI's production-lines list (`docs/spec/production-lines.md`): general-purpose programming
  languages, HTML and CSS; SQL, shell, build scripts and data and configuration files are not on it, and the same page
  states the one rule by which test, generated and vendored files are excluded. The engine records the share in the
  evidence bundle's `productionLineShare` field (the share, the production lines per language, and the version of the
  list used); CAI's scoring specification adds that field in the same rubric version; the reference scorer refuses a
  bundle without it or with a share under 0.95; CAI's quarterly run checks the share on the holdout; and a CAI
  published below 95 %, or with a share that does not hold, is not a valid CAI.

## The holdout lifecycle

Four windows a year. Each holdout moves through the same steps, in this order, and each step leaves a public trace.

1. **Protocol.** Before authoring starts, the authoring protocol for the window is published: which concepts and
   dimensions, the quota of planted defects per dimension and language, the ratio of traps to plants, the acceptance
   criteria, the authoring and review models, the dispute rules, the date by which an engine registers, and where and
   how submissions (the image, configuration and mapping, or a hosted run) are delivered (*to be set*).
2. **Authoring.** The repositories and answer keys are written to the protocol (see *Authoring* below).
3. **Validation.** Every answer-key entry is reviewed before sealing (see *Validation* below).
4. **Seal and commit.** The holdout is sealed. The SHA-256 of every repository archive and every answer key is
   published, with the time of sealing. Nothing published after this point can change what the holdout contains
   without the change being visible.
5. **Window.** Every engine that publishes CAI, and every engine registered for this window, is run (see *Running an
   engine*).
6. **Reveal.** The repositories, the answer keys, the authoring log (every candidate, every rejection and its reason)
   and every result are published together. Anyone can check them against the hashes from step 4.
7. **Disputes.** For a fixed period after reveal (*to be set* in the window's protocol), any participant may dispute an answer-key entry. An
   entry found wrong is corrected in a new key version, the correction is published with its reason, and **every**
   result in that window is rescored against it. A correction never applies to one participant alone.
8. **Release.** The revealed holdout joins the public benchmark set. What was a test becomes material everyone may
   train and tune against, equally.

## Authoring

Holdouts are written by AI models working to the published protocol, with a human operator. The controls are
mechanical, so that they hold regardless of who operates them:

- **Isolation.** The authoring environment has no access to any participating engine: not its source, its
  documentation, its findings or its results. It receives the concept taxonomy, the answer-key schema and the
  protocol, and nothing else.
- **No preview.** No participating engine is run against a holdout, or any part of it, before reveal.
- **More than one model family.** Each holdout is authored by models from at least two different model families, and
  recall is published per authoring family. A systematic advantage for engines built with the same family as an
  author then shows up as a measured difference instead of hiding in the average.
- **Acceptance by rule, not by taste.** Candidates are accepted or rejected only against the acceptance criteria
  published in step 1. Every candidate and every rejection, with its reason, is logged and published at reveal.
- **Quotas.** The protocol fixes the number of planted defects per dimension and language (*to be set*), sized so that
  each window's figure per cell has a usable Wilson interval.
- **Disclosure of the operator.** The operator of the authoring pipeline is named on each holdout. Where an operator
  is affiliated with a participating engine, that is stated on the holdout and on every result in that window.

## Validation

An answer key can be wrong: a plant may not contain the defect it claims, and a trap may in fact be a defect.
Validation must not use any participating engine, because that would bend the key toward it. Instead:

- every entry is reviewed by a model from a **different** family than the one that authored it;
- where a defect can be demonstrated, it is — a test that exercises the injection, a secret that is shown to be
  present in history, a dependency resolved to the vulnerable version;
- a random sample of entries is reviewed by a human, and the sample size and outcome are published at reveal;
- any remaining doubt is resolved by removing the entry before sealing, never after.

## Running an engine

- **Registering:** an engine registers to be measured, and files its record (the languages it supports and the cells
  it measures), by an issue titled *Engine registration* in the
  [CAI repository](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues) (decision 0024), no later
  than the date the window's protocol (step 1 of the holdout lifecycle) sets; the protocol says where the submission
  itself is delivered.
- **Who is run:** every engine that publishes CAI, and every engine registered to be measured in the next window. An
  engine submits its current release to every window and publishes CAI only from a release CAI has measured. A window
  it misses is shown in its report as *not measured this quarter*; after two missed windows in a row it publishes no
  new CAI until CAI has measured its current release again. The configuration measured is the one the engine ships
  to its users by default.
- **What is submitted, before the window opens:** the engine as a container image (by digest), its configuration, and
  its **mapping** from its own rule identifiers to the benchmark's concepts. All three are hashed and recorded. Nothing
  submitted may change once the window opens; a mapping written after seeing a holdout would be a way to buy recall.
- **Where it runs:** offline, operated by the Initiative, with no network access. Data an engine legitimately needs,
  such as a vulnerability advisory database, is supplied as a fixed snapshot identical for every participant, so that
  results do not depend on the day a run happened.
- **Hosted-only engines** that cannot be submitted as an image may be given time-boxed access to the holdout under
  embargo. Their record states *hosted run*, because the holdout was disclosed to the operator of the engine.
- **One attempt.** Each engine version gets one run per window. Engines that are not deterministic are run once, with
  their configuration recorded; there is no best-of-several.
- **Every run is published,** whatever its figures. A record that showed only good runs would hide the retries.

## The engine report

Published on codeassuranceindex.info for every engine, updated every window:

- engine name, version, image digest, configuration label and mapping hash;
- window and holdout identifiers, with the holdout's published hashes;
- Part A result per rubric version;
- per dimension and language: recall, trap resistance, noise (share and repositories affected) and band agreement,
  each with its confidence interval, and a **trend graph** of noise and recall over every window measured;
- the engine score with its interval, and its coverage;
- the languages and dimensions the engine declares, its supported languages marked apart from the ones only measured;
- a link to the engine's own, publicly readable description of its method: CAI does not hold it, so an engine
  improving its method changes nothing in CAI; an engine with no public description reads *method not published*;
- *hosted run*, operator affiliation and any dispute corrections that changed the result.

A result applies to the engine version it was measured with. A new version is shown as such from its first window.

## Checking that the synthetic test tracks reality: noise on real code

Synthetic repositories written to a protocol can drift into a recognisable style that engines learn to spot. So, each
window, CAI also measures the noise of every engine that publishes CAI on real code, by the method of the
[noise standard](https://codeassuranceindex.info/noise/): a sample of public repositories drawn before any engine runs,
CAI running the engine at the pinned revisions, and blind judges from more than one model family giving each sampled
finding one of the standard's verdicts. The result is shown in the engine's report beside its holdout figures, so a
reader can see whether they hold on real code; if they drift apart, the holdout protocol is changed.

A tool that does not publish CAI may still submit its own noise run under the noise standard. Such a result is
labelled *self-measured* and never appears in an engine report.

## Publishing your reports

*Adopted by [decision 0003](decisions/0003-survey-pages-link-the-producers-public-report.md). This is guidance, not a
condition of publishing a CAI.*

A producer **should** publish a public report for every survey of open-source code it delivers. The report is how
readers check the evidence behind a CAI, how the owners of measured code find what to dispute and what to fix, and how
readers compare producers. A producer that publishes reports helps all three.

A producer **may** publish a report for code that is not open source **only with the recorded consent of the
organisation that owns the code.**

A public report **must not**:

- disclose a security finding that is not yet fixed in a form that helps someone exploit it. Report it as a count, a
  category or a severity, and disclose the detail through the project's own security policy;
- identify individual contributors beyond what the project's own public history already shows. For code that is not
  open source, it identifies no individual at all.

Publishing or withholding a report never changes a score, and a producer must not offer a different score, or a
different measurement, in exchange for publication.

A delivery names its reports in `reports` ([package MINOR 1.3](spec/cai-delivery-package.md)). The survey page links a
report, under the mark of the system that wrote it, when the producer is in the
[producer listing](../producers/README.md), the report names one of that producer's systems, and its URL is `https` on
that system's host. The report URL must describe the delivery's own commit and stay readable while the survey is
published: the standard does not probe it, so keeping it answering is the producer's obligation.

## What the measurement is not

- It is not a certification of any codebase, and it does not make any finding in any survey correct.
- It is not an endorsement of an engine, and the engine score is not a ranking of products beyond what the numbers
  state.
- It does not change the standard. Rubric changes follow their own versioned process
  ([ADR-0004](adr/0004-versioned-frozen-rubrics.md)).

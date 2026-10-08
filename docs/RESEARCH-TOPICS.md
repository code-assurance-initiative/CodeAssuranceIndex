# Research and thesis topics

CAI is an open, reproducible measure of codebase condition, with an open scorer, a public corpus of measured
codebases, a benchmark whose answer keys say exactly which defects are planted, and a sealed holdout. That makes it a
good subject for theses and research: the data exists, the method is published, and the questions matter to
everyone who buys or maintains software — including public procurement.

Each topic below is scoped for a master's thesis (one or two students, one semester) unless marked otherwise. All of
them can be published, and a negative result is as welcome as a positive one: a measurement nobody can falsify is not
worth much. To take one up, open an issue or a discussion in this repository.

## 1. Does a low CAI predict cost? *(PhD-sized, or a thesis on one slice)*

Relate CAI scores of open-source projects to observable outcomes over the following 12–24 months: defect-fix rate,
time to merge, security advisories, contributor turnover. Which lenses carry predictive signal, and which do not?
**Data:** the public corpus of signed surveys, plus the projects' own git and issue history.

## 2. A second, independent implementation

Implement the CAI fold from [the specification](spec/cai-scoring.md) alone, in a language other than C#, and record
every place the text did not decide a question. Success is reproducing published scores at the stated precision;
the list of ambiguities is the research contribution.

## 3. A new language for the open reference engine

Add TypeScript/JavaScript (or Java, Python) to the
[community reference engine](https://github.com/open-assurance/reference-implementation), and measure the result on
the benchmark. How much of the standard is language-neutral, and where does each language need its own rule?

## 4. How stable are the measurements?

Scan the same commit with different engines and configurations; scan successive commits of the same project. How much
of a score's movement is the code, and how much the instrument? What is a meaningful difference between two scores?

## 5. Noise in AI-assisted code review

CAI keeps language-model readings out of the number. Measure how often such readings are right, using the
benchmark's traps and certified-clean regions as ground truth, and compare model families. When, if ever, would a
model's verdict be reliable enough to count?

## 6. Synthetic benchmarks versus real code

The benchmark plants defects in synthetic repositories. Do engines that score well on it also find real defects in
real code? Design and run the comparison.

## 7. CAI in procurement *(public administration, law or IT management)*

How would a quality requirement such as "CAI of at least 70, verified independently" work in a Danish or EU public
tender under the procurement rules? What does a contracting authority gain, and what does it risk? Interview buyers
and suppliers.

## 8. Writing the rubric for a new lens

Propose and validate a lens the standard does not yet cover well — for example operational resilience or
energy efficiency — through the [RFC process](rfcs/).

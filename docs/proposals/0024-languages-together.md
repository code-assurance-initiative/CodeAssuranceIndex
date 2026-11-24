# Which languages appear together in the same repository

Measured 2026-11-24 for proposal 0024 §5.

**Source.** The newest analysis of each repository in the analysis database of CCA (Canine Code Analyzer, today the
only engine that publishes the Code Assurance Index, CAI, a public score of a repository's code computed from an
engine's evidence): its production lines (lines of code that are not tests and not generated) per language. The repositories are
the open-source corpus CCA measures, about 13,600 public repositories, and a small number of customer repositories. A
language counts in a repository when it has at least 300 production lines and at least 5 % of the repository's
production code; 11,161 repositories have at least one language that passes.

**Limits.**

1. **The corpus was sampled per language.** Repositories were picked to a quota for each of the languages CCA reads.
   The shares below therefore describe this corpus, not open-source code in general, and the direction of that bias is
   not measured. Languages CCA does not read (C, C++) were not sampled for at all: they are in the corpus almost only
   beside a sampled language; the few repositories where one stands alone (1 % and 2 %) are probably customer
   repositories or ones whose sampled language fell under the threshold of limit 2, which was not checked.
2. **Small companion code is not counted.** A language under 300 lines or 5 % of the code is left out, so a small C
   shim or a few script files do not show; combinations are undercounted.
3. **HTML, CSS, SQL and shell are not in the language map**, so for example "C# + HTML" cannot be read from it.
   JavaScript and TypeScript are the nearest stand-in for a web front end, but they also cover Node back ends, desktop
   shells and build tooling.
   Because of limit 2, the number of repositories with two or more mapped languages (3,195), and the number pairing a
   language with JavaScript or TypeScript, are lower bounds; adding the unmapped languages of limit 3 could move them
   either way. The percentages, the single-language count and each
   exact-set row of the combinations table can be off in either direction (counting small code changes the
   denominators too, and moves repositories between sets).
4. Percentages in the per-language table are of that language's own repositories. The columns overlap, so a row does
   not add up to 100 %.

## How many languages

| languages in a repository | repositories |
|---|---|
| 1 | 7,966 (71 %) |
| 2 | 2,475 |
| 3 | 557 |
| 4 or more | 163 |

About 29 % of the repositories (3,195 of 11,161, 28.6 %) hold two or more languages that each pass the threshold.

## The most common combinations

Each row counts repositories whose counted languages are exactly that set; a repository with Java, Kotlin and
JavaScript is not in the Java + Kotlin row. The 14 most common:

| combination | repositories |
|---|---|
| JavaScript + TypeScript | 260 |
| JavaScript + PHP | 145 |
| Rust + TypeScript | 134 |
| Java + Kotlin | 112 |
| JavaScript + Ruby | 109 |
| JavaScript + Python | 109 |
| Python + TypeScript | 106 |
| C++ + Dart | 92 |
| Go + TypeScript | 90 |
| C# + JavaScript | 65 |
| Java + JavaScript | 63 |
| Python + Rust | 58 |
| Java + Scala | 58 |
| C# + TypeScript | 52 |

Further combinations cited below: C + Swift 25, C# + F# 24 (more common in the same output are JavaScript + Python +
TypeScript 42, JavaScript + Rust + TypeScript 30, JavaScript + Scala 29, JavaScript + Swift 29, PHP + TypeScript 28).

## Per language

| language | repositories | alone | with JavaScript or TypeScript | 3+ languages |
|---|---|---|---|---|
| JavaScript | 1,874 | 29 % | (itself) | 18 % |
| TypeScript | 1,778 | 35 % | (itself) | 19 % |
| Python | 1,257 | 45 % | 31 % | 20 % |
| Rust | 1,057 | 54 % | 27 % | 14 % |
| Java | 977 | 49 % | 21 % | 17 % |
| Swift | 961 | 64 % | 14 % | 17 % |
| C# | 927 | 68 % | 17 % | 7 % |
| Kotlin | 898 | 58 % | 13 % | 17 % |
| Go | 799 | 66 % | 25 % | 11 % |
| PHP | 759 | 70 % | 27 % | 5 % |
| Dart | 753 | 58 % | 10 % | 15 % |
| Ruby | 660 | 73 % | 21 % | 4 % |
| Scala | 567 | 69 % | 11 % | 7 % |
| Elixir | 381 | 75 % | 14 % | 6 % |
| Erlang | 327 | 81 % | 6 % | 3 % |
| F# | 315 | 82 % | 5 % | 3 % |
| C | 287 | 1 % | 15 % | 56 % |
| C++ | 285 | 2 % | 13 % | 47 % |
| Visual Basic .NET | 238 | 84 % | 3 % | 3 % |

## What it says

1. **JavaScript or TypeScript is the most common second language overall.** It is in 21-31 % of the Python, Rust,
   PHP, Go, Java and Ruby repositories, 17 % of C#, 10-14 % of Swift, Kotlin, Dart, Scala and Elixir, and 3-6 % of
   Erlang, F# and Visual Basic .NET. For some languages a platform partner is as common or more: Dart pairs more often
   with C++, F# with C#, and Kotlin and Scala about as often with Java.
2. **Languages of one platform appear together**: Java with Kotlin (112) or Scala (58), C# with F# (24). So do a
   language and native code beside it: Dart with C++ (92; the data does not show whether this is plugin code or the
   desktop runner code Flutter projects contain), Python with Rust (58), Swift with C (25).
3. **C and C++, which no engine publishing CAI reads, sit beside the languages that are read.** In this corpus that
   follows from the sampling (limit 1), so it says nothing about C and C++ code in general. What it shows is that C
   and C++ code, at 5 % or more of a repository's code, is found in repositories that also contain read languages,
   often several of them: 56 % of the C repositories and 47 % of the C++ repositories hold three or more counted
   languages. Where C or C++ is above 5 % of the code (or together with other unread code), an engine that does not
   read it cannot publish a CAI for that repository under proposal 0024 §5.

## Consequence for the language rules of proposal 0024

An engine that reads only one language of a repository like these reads the rest as nothing. Proposal 0024 §5, which
this file is evidence for, asks an engine to read the platform family of each language it supports (the languages of one platform, for
example Java, Kotlin and Scala on the Java virtual machine) and the web front (JavaScript, TypeScript, HTML and CSS),
and to publish a CAI only where the languages it reads hold at least 95 % of the production code. Separately, its §3
has CAI measure every engine each quarter on its holdout (a sealed set of synthetic test repositories with a known
answer key), which covers every supported family plus C, C++, Objective-C and R. This data
supports the platform families, JavaScript or TypeScript beside most languages, and C and C++ beside the read
languages; it does not measure HTML or CSS (limit 3), and the 95 % share is the decider's choice, not derived here.

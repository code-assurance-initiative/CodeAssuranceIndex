# PF1: Benchmark discipline

**Lens:** performance · **Scored:** yes

## Property
Whether the code protects its performance with benchmarks — a benchmark suite, allocation/memory measurement, and (ideally) a CI gate. Presence is credited as a bonus, never a deduction.

## Applies to
Any language that has benchmark frameworks or benchmark harnesses. Examples are a .NET benchmark library, Go's built-in benchmark functions, a JVM microbenchmark harness, a Rust benchmark crate, a Python benchmark plugin, a JavaScript bench library, a Swift benchmark package, and the benchmark tools of Erlang, Elixir, Ruby, PHP and Dart. The dimension also reads the repository's continuous-integration definitions.

## Population
- **Benchmark code is read wherever its authors put it.** That includes benchmark projects, benchmark folders and test projects. [production-code.md](../production-code.md) excludes benchmark projects from production code, but this dimension reads them, because they are what it measures. A suite inside a test project counts.
- **Declarations count.** These are the build and package manifests that declare a benchmark framework as a dependency or set up how it runs.
- **Continuous integration counts.** These are the pipeline definitions the repository commits, together with the task runners those pipelines call.
- **Not read:** vendored third-party code, generated code and build output, as [production-code.md](../production-code.md) defines them. Text that only mentions or quotes benchmark code is not a benchmark. Examples are comments, string literals, documentation and test fixtures that contain benchmark source. A benchmark tool's own implementation is not a suite of that tool's benchmarks.
- **Size floor:** the repository's production lines, as [production-code.md](../production-code.md) defines them, decide whether the dimension is measured (see Not measured when).

## Unit and denominator
- **Benchmark suite:** benchmark code the repository's authors wrote to measure the speed or memory use of their own product. It is any one of the following:
  - benchmark cases written for a benchmark framework;
  - a benchmark framework the repository declares as a dependency;
  - a script that runs the product in order to measure it (a benchmark harness).
- **Benchmark case:** one named measurement that a framework or harness runs. Examples are a method marked as a benchmark, a Go `BenchmarkX` function, one job in a benchmark group, or one entry in a benchmark definition file. A case belongs to the framework that runs it, so a name that only looks like a benchmark outside that framework is not a case.
- **Count:** the number of benchmark cases across all of the repository's suites, in every language, added together.
- **Allocation measurement:** at least one suite measures memory allocations or memory use alongside time. This counts whether the suite turns it on (for example a memory diagnoser, a GC profiler, or an allocation report call) or the framework records it by default.
- **Run in CI:** the repository's continuous integration runs at least one suite.
- **Denominator:** none. The dimension reads presence and one count, not a share.

## Thresholds
- **Suite present:** at least one benchmark suite. Without one, the dimension is not measured.
- **Benchmark cases:** 5 or more cases earn the full count credit. 1 to 4 cases earn half of it. 0 cases earn none.
- **Allocation measurement:** present or absent.
- **Run in CI:** present or absent. CI only has to run the benchmarks. A gate that fails the build on a regression is not required.

## Score
The score adds bonus terms to a base:

| Term | Points |
|---|---|
| Base | 6.0 |
| A benchmark suite is present | +1.5 |
| 5 or more benchmark cases | +1.0 |
| 1 to 4 benchmark cases | +0.5 |
| Allocation or memory measurement | +1.0 |
| The suite runs in CI | +0.5 |

The result is clamped to 0-10. A scored repository always has a suite, so scores run from 7.5 (a suite with no counted cases, no memory measurement, not in CI) to 10.0 (5 or more cases, memory measured, run in CI). A low score is not reachable, because absence is not scored.

## Not measured when
- The repository has no benchmark suite. The result is not applicable: absence is neither scored nor deducted.
- The repository has fewer than 400 production lines.
- The engine cannot read the benchmark frameworks of the repository's main language and finds no suite. The result is then a gap in the engine's reach, not a finding about the code.

## What the result is not
- It does not measure performance. A fast product with no benchmarks is not measured, and a slow one with a full suite scores 10.
- It does not judge benchmark quality. It does not check whether the benchmarks cover the hot paths, whether their numbers are sound, or whether anyone reads the results.
- It does not check for a regression gate. Running in CI is credited even when nothing fails on a slowdown.
- It cannot see benchmarks kept in another repository.
- It is not a deduction. A repository without benchmarks gets no score, not a low one.

## Pending re-definition
These are parameters of the existing reading's model. Today's scores depend on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Size floor:** 400 production lines. Below this, the repository is treated as too small for performance engineering to be assessed.
- **File size cap:** a source file larger than 512 KiB is not read for benchmark cases.
- **File count cap:** at most 20,000 files are read for benchmark cases in one repository.

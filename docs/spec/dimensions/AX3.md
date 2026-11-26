# AX3: Project dependency cycles

**Lens:** architecture · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

Whether the project-reference graph is acyclic (cycles block independent build/deploy and signal eroding boundaries).

## Applies to

This dimension applies to any language whose build declares projects or modules and the references between them. Examples: .NET projects, Gradle, Maven, sbt or Mill modules, Cargo crates, npm, Deno or pub packages and workspaces, Go modules, Python distributions, SwiftPM, Xcode, Tuist or XcodeGen targets, Ruby gems, Composer packages, Mix or rebar3 applications, Unity assembly definitions, and CMake or Bazel targets. In a JavaScript, TypeScript or Python tree whose manifests declare no production module, the projects are the components its own source declares, and the references are the imports between them.

## Population

- **Projects:** every project (module, package, crate, target) that the repository's own build declares. A repository that uses more than one build system has one graph that holds the projects of all of them. When two build systems each declare a project with the same name, those are two projects.
- **Test projects stay in.** This differs from the shared rule in [production-code.md](../production-code.md): test projects, benchmark projects, and sample or demonstration projects are nodes of the graph. A cycle through any of them still blocks the build.
- **Out of scope:** projects in build output, projects in trees the repository marks as generated or vendored, and project files that are only fixtures or material for an analysis tool. Examples are a deliberately circular sample solution planted for a cycle detector, or project files shipped as embedded resources. None of these is the repository's own architecture.
- **References:** project A references project B when any of these holds:
  - A's build declares a dependency on B.
  - A uses B's built output directly by file.
  - A loads B's types by name at run time.

  References to projects outside the population are ignored.

## Unit and denominator

- **Unit:** a cycle. A cycle is a group of two or more projects in which every project can reach every other by following references, taken as large as possible. A project that references itself is also a cycle. One group counts once, however many separate loops run through it.
- **Denominator:** the number of projects in the population, with a minimum of 1.

Cycles between namespaces inside one project are reported next to this dimension but are not counted.

## Thresholds

- A group counts as a cycle when it has 2 or more mutually reachable projects, or when it is 1 project that references itself.
- 0 cycles scores 10.

## Score

Let C be the number of cycles and P the number of projects (at least 1).

- If C = 0, the score is 10.
- Otherwise:
  - density d = min(C / P, 1)
  - isolation i = max(0, 1 − d / 0.10)
  - headroom h = min(max(log10(P) / log10(200), 0), 1)
  - ceiling c = 6.0 + 2.0 · i · h
  - score = 1.0 + (1 − d) · (c − 1.0), held between 1.0 and c, rounded to 2 decimals.

Any cycle keeps the score below 8.0.

- **Above 6.0:** the score rises above 6.0 only when cycles are under 10 % of the projects.
- **Toward 8.0:** the ceiling gets closer to 8.0 as the project count grows toward 200 and cycles become rarer. The score itself always stays below the ceiling. For example, 1 cycle in 200 projects gives a ceiling of 7.9 and a score of 7.87.
- **Toward 1.0:** the score falls toward 1.0 as cycles become pervasive.

## Not measured when

The repository declares no projects, so there is no project-reference graph.

## What the result is not

- It is not a type-level or namespace-level cycle measure. It reads only references between projects.
- It does not grade how tangled a cycle is. A group of 8 projects counts as one cycle, the same as a group of 2.
- It does not say whether the repository builds.
- No published score includes it today because the engine reports it advisory.

## Pending re-definition

These are parameters of the existing reading's model. Today's score depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Ceiling with any cycle:** 6.0, raised by at most 2.0, toward 8.0.
- **Isolation density:** the raise shrinks to nothing once cycles reach 10 % of the projects.
- **Population saturation:** the raise grows with log10 of the project count and reaches its full size at 200 projects.
- **Floor:** the score falls toward 1.0 as density approaches 1.

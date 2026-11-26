# SC1: Supply-chain hygiene

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property
Whether the repository pins its dependencies: for each ecosystem it uses (one with a package manifest in the repository), whether the dependency versions are fixed in a committed file, either a lockfile or one shared file fixing the versions for every project of that ecosystem (for example .NET's central package management), so that a build resolves the same versions every time. Evidence toward practice PW.4 of the NIST SSDF (Secure Software Development Framework) and toward SLSA (Supply-chain Levels for Software Artifacts). Pinning committed in the repository does not prove that the build uses it, that the build is reproducible bit for bit, or that it reaches any SLSA level. Advisory: reported, never scored.

## Applies to
Three package ecosystems. The language the code is written in does not decide which ones apply:

- **.NET (NuGet):** any project that restores NuGet packages, for example C#, F#, Visual Basic .NET or C++ projects.
- **JavaScript (the npm family of package managers):** JavaScript and TypeScript packages, for example those installed with npm, Yarn, pnpm or Bun.
- **Go modules.**

A repository that uses none of these ecosystems is not read by this dimension.

## Population
This dimension reads dependency manifests and the committed files that pin them. It does not read source files, so the production code and production lines rules in [production-code.md](../production-code.md) do not decide its population.

In scope: every manifest that the repository's own build or install resolves and that declares at least one third-party dependency.

- **.NET:** a project that declares at least one NuGet package. Examples are a package reference, a legacy reference to an assembly restored from a package, a classic package list file (`packages.config`), or a shared build file that adds package references to the projects below it. A solution file only lists projects, so it is not a manifest.
- **JavaScript:** an install root. An install root is a package manifest (`package.json`) that is not a member of a workspace above it. It covers its own dependencies, the dependencies of every workspace member it names, and any sub-package it installs through a local-path dependency. The install root is in scope when at least one of these manifests declares a dependency in any dependency block (runtime, development, peer or optional).
- **Go:** a module whose manifest requires at least one other module.

Out of scope:

- A manifest that declares no dependencies, because there is nothing to lock. For example: a package manifest that only reserves a package name, a .NET project that references only framework assemblies, or a Go module that uses only the standard library.
- Vendored code, installed dependency folders, and build output and tool state, as [production-code.md](../production-code.md) defines them.
- Manifests the repository's own build never installs, in any of the three ecosystems. For example: test fixtures and test data, examples and samples a reader copies into their own project, benchmark harnesses, documentation sites, and editor extensions packaged by their own tool.
- A JavaScript package that only holds type declarations. Nothing it names ever runs or builds.
- A published JavaScript library (it has a name and is not marked private) whose repository leaves its lockfile uncommitted on purpose. For example, the package manager's settings turn the lockfile off, or the version-control ignore file ignores it. The lockfiles of the library's consumers pin its ranges. A private application that does the same stays in scope.

## Unit and denominator
The unit is an ecosystem in scope. Each one gets a yes or no answer: are its dependencies pinned?

- **.NET:** pinned when every in-scope project is covered by one of these:
  - a committed NuGet lockfile for that project;
  - central package management that applies to that project: a shared file fixes the package versions for the projects below it, and the repository has not explicitly turned central management off.
- **JavaScript:** pinned when every in-scope install root has a committed lockfile in its own directory. A lockfile in a parent directory does not lock an install root that the parent's workspace does not name. A monorepo tool that installs every project from one committed lockfile in a fixed shared location (for example Rush) pins the whole repository.
- **Go:** pinned when every in-scope module has its own committed checksum file (`go.sum`).

The denominator is the set of ecosystems in scope. Each unpinned .NET project, JavaScript install root or Go module is one finding.

## Thresholds
- A manifest is in scope as soon as it declares one third-party dependency.
- An ecosystem is pinned only when all its in-scope units are pinned. One unpinned unit makes the whole ecosystem unpinned.

## Score
No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when
- The repository has no in-scope manifest in any of the three ecosystems. There is no result, and this is not a finding.
- No in-scope manifest was found in the part of the repository that could be read, and some of the repository could not be read. The result says so, because it is not known whether there are dependencies to lock.

## What the result is not
- It does not show that the build actually installs from the committed pins, that the fetched artifacts match them, or that the build is reproducible bit for bit.
- It says nothing about the contents, safety, licence or age of any pinned dependency.
- It is not a SLSA level and not a full SSDF PW.4 assessment.
- It does not cover ecosystems other than the three listed, for example Python, Java, Rust or Ruby package managers.

## Pending re-definition
- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve. The closing sentence of the property ("Advisory: reported, never scored") is catalog wording. Until that decision, the standard counts the dimension as scored, with no curve yet.
- **Files read:** at most 50,000 files of the repository are read. Manifests beyond that limit are not seen. This is a parameter of the existing reading's model, and a later decision under decision 0013 will replace it with a threshold on the property.

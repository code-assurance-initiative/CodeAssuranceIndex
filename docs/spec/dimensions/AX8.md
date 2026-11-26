# AX8: Test isolation

**Lens:** architecture · **Scored:** yes

## Property
Whether production projects stay free of references to test projects — tests may depend on production, never the reverse.

## Applies to
This dimension applies to any language where the repository is split into modules that reference one another. A module is the unit that the language's build or package system declares and builds, for example a project, package, crate, target, assembly definition or application.

Sometimes no build declaration in the repository declares a production module. Then the modules are the components that the source's own layout and imports make, and a reference is an import of one component by another.

## Population
The population is the repository's modules and the references each module declares to other modules in the repository and to external packages.

A reference counts only when it is part of what the module builds or ships. A dependency that a module declares only for its own tests or development is not a reference, for example npm devDependencies, a Maven test scope or a Gradle testImplementation dependency. Such a dependency points the correct way and does not ship.

- **Test module:** a module that is a test project as [production-code.md](../production-code.md) defines it. The project's own declaration decides.
- **Non-product modules:** benchmark projects, examples, samples, demonstrations, sandboxes and project templates. This page leaves them out as referrers, even though [production-code.md](../production-code.md) counts the authors' own examples and samples as production code. They are not part of the product, so a reference they make into test code is not a defect in the product.
- **Production module:** a module that is neither a test module nor a non-product module. A production module does not have to ship.
- **Shipped module:** a production module that ships by its own declaration, for example an executable, a web application, a service or worker host, or a published package with an entry point or command. A production module also ships when a shipped module reaches it through references to production modules, either directly or through other production modules.

## Unit and denominator
The unit is a **violation**. There are two kinds:

1. **Production-to-test reference:** a production module that directly references a test module. Each distinct (production module, test module) pair counts once.
2. **Shipped test package:** a shipped module that references an external package whose only job is testing, for example a test framework, a test-double (mocking) library or an assertion library. Each such package reference counts once. These rules limit this kind:
   - Only a reference that ships with the module counts. A package declared only for development or tests does not count, for example an npm devDependency or a Maven test-scope dependency.
   - A package that the module says the consumer must supply, rather than shipping it, does not count. For example, a test-framework plugin that lists the framework as an npm peer dependency.
   - A production library that no shipped module carries is not judged for this kind.

There is no denominator. The count is absolute and is not normalised by repository size, because every violation is a real defect whatever the size of the repository.

## Thresholds
- A single violation counts. There is no minimum.
- Only direct references count for kind 1. A production module that reaches a test module only through another module does not count.

## Score
`score = clamp(10 − 2.5 × N, 0, 10)`, where N is the number of violations (kind 1 plus kind 2).

| N | Score |
|---|-------|
| 0 | 10 |
| 1 | 7.5 |
| 2 | 5 |
| 3 | 2.5 |
| 4 or more | 0 |

When a score is reported for one service inside the repository, the same curve applies to the violations that the service's own production modules declare.

## Not measured when
- The repository has no production module.
- The repository has no test module, and no shipped module references a test-only package. In that case tests and production have no dependency direction that could be wrong.
- No module structure can be read for the repository. For example, the repository declares no modules in any build or package system, and its source does not form components with imports between them. This is a gap in what was read, not a finding about the repository.

## What the result is not
- It does not judge whether tests exist or are any good. A repository with no tests and no shipped test-only package is not measured. It does not score 10.
- It does not find indirect inversions. These cases are not counted:
  - a production module that reaches test code only through another module;
  - test code compiled into a production module by something other than a module reference, for example a shared source file that both modules compile.
- It does not judge test-only packages that a module declares for development or tests only. Those packages do not ship.
- It does not judge test-only packages in production libraries that no shipped module carries.
- It is not a layering check. Production modules that reference each other in the wrong direction belong to a different dimension.
- It is not scaled by size. One violation costs a small repository exactly as much as a large one.

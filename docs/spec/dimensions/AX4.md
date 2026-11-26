# AX4: Dependency direction

**Lens:** architecture · **Scored:** yes

## Property
Whether dependencies point inward (Domain ← Application ← Infrastructure/Web) — the clean-architecture dependency rule, checked across the project graph.

## Applies to
- **Between modules:** any language.
- **Between folder layers inside one module:** any language whose code is organised in namespaces or packages (for example C#, Java, Kotlin).
- **Against declared architecture rules:** any language in which a repository's tests can declare architecture rules (for example C# or Java).

## Population
- **Modules:** these rules hold in every language.
  - **Declared module:** a project, package or target that a build or package manifest in the repository declares.
  - **No declared production module:** some repositories declare no production module, meaning no module that is neither a test nor a sample, benchmark or other non-product module. In that case the modules are read from the source tree.
  - **Undeclared source:** production source that lies outside every declared module forms further modules, read from the source tree. These are added to the declared modules.
- **Modules in the source tree:**
  - Each top-level source folder is one module.
  - A folder that only holds components (for example `src/`, `lib/` or `packages/`) is not a module itself. Instead, each folder directly below it is one module (for example `src/core`).
  - The loose source files at the repository root together form one module.
  - A tree whose source all sits in one component is one module.
- **Links between modules:** two declared modules are linked when the manifest of one declares a dependency on the other, whether or not its code uses it. Two modules read from the source tree are linked when a production source file in one imports code from the other.
- **Which modules count:** every module counts, including test modules and modules that are not the product (for example samples or benchmarks).
- **Referencing side:** a wrong-way edge always starts in production code, as [production-code.md](../production-code.md) defines it. These can never be the referencing side:
  - a test module;
  - a module that is not the product;
  - the application's executable host, for module edges only. The host is where the layers are wired together. Its own folder-layer edges and declared-rule edges still count. Only a manifest declares a host, so a module read from the source tree is never the host.
- **Layers:** each module belongs to one layer. The module's own name declares its layer through the usual clean-architecture names. For a module read from the source tree, that name is the folder's own name.
  - **Shared:** a shared kernel, building blocks, contracts, abstractions or common primitives (for example `Shop.SharedKernel`, `Shop.Contracts`).
  - **Domain:** for example `Shop.Domain` or `Shop.Core`.
  - **Application:** for example `Shop.Application` or `Shop.UseCases`.
  - **Infrastructure:** for example `Shop.Infrastructure`, `Shop.Persistence` or `Shop.Adapters`.
  - **Web:** for example `Shop.Web`, `Shop.Api`, `Shop.Ui` or `Shop.Host`.
  - **Unknown:** the name declares no layer.

  When the executable host is the module being referenced, it counts as Web, whatever its name says.
- **Folder layers:** one module can hold several layers.
  - The first namespace or package segment under the module's root namespace or package declares the layer. It uses the same names as module layers.
  - `Core` and `Models` do not declare a folder layer.
  - Folder layers are checked inside every production module, whatever the module's own layer, including Shared and Unknown modules.
- **Declared rules:** a rule in the repository's architecture tests saying that code in a module, namespace or package must not depend on a named namespace, package or type (for example a NetArchTest, ArchUnitNET or ArchUnit rule). The rule is read as written. It is never run.

## Unit and denominator
- **Unit: one wrong-way edge.** Three kinds of edge are added together:
  1. **Module edge:** a production module is linked to another module, and the link points outward.
     - Domain points outward when it references Application, Infrastructure or Web.
     - Application points outward when it references Infrastructure or Web.
     - Infrastructure and Web form one outer ring, so references between them do not point outward.

     Each link counts once, however many files use it. Shared and Unknown modules are on neither side of a module edge.
  2. **Folder-layer edge:** inside one production module, a file in an inner folder layer names a type in an outer folder layer, or imports that layer's namespace or package. Inner and outer follow the same direction as for module edges.
     - Each distinct combination of module, inner layer folder and outer layer folder counts once.
     - Two folders that declare the same layer count as two layers here. For example, a Domain folder that references both `Infrastructure` and `Persistence` gives two edges.
     - Folders that declare no layer, or declare Shared, are on neither side of a folder-layer edge.
  3. **Declared-rule edge:** a production file names a type, or imports a namespace or package, that a declared rule forbids. Each distinct pair of rule and module counts once, in any layer.

  A namespace or package that appears only in a comment or a string is not a reference.
- **Denominator:** P, the number of modules counted under Population, with every kind of module included. When there are no modules, P is 1.

## Thresholds
- A module edge or folder-layer edge counts only when it points outward.
- A declared-rule edge counts when the rule forbids what the file references.
- The curve terms and the source-tree cap are listed under Pending re-definition.

## Score
Let n be the number of wrong-way edges and P the number of modules (at least 1).

- If n = 0, the score is **10**.
- Otherwise:
  - d = min(n / P, 1)
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, max(0, log10(P) / log10(200)))
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1). The score is clamped between 1 and the ceiling, then rounded to 2 decimals.

Any violation means a score of at most 8. A score above 6 needs a few violations spread across many modules. When there are at least as many violations as modules, the score is 1.

## Not measured when
- The codebase is organised in a recognised style that is not layered or clean (for example vertical slices, transaction scripts, or CQRS alone). The inward rule does not apply there.
- No module exists. This happens when no manifest declares a module and the source tree gives none. Either no module can be read from the tree, or the tree holds more than 50,000 source files.

## What the result is not
- It does not check whether the code is well layered. It checks only two things: whether references between named layers point outward, and whether declared rules hold. A codebase with no layer names and no broken declared rule scores 10.
- It does not judge module edges to or from Shared or Unknown modules. A misnamed module is judged by its name, not by its role.
- It does not count how many files use a wrong-way reference.
- For declared modules, it does not check whether a declared dependency is used in code.
- It is not a verdict on the architecture style itself.
- It does not run the repository's architecture tests.

## Pending re-definition
These are parameters of the existing reading's model, and today's score depends on them. A later decision under decision 0013 will replace each with a threshold on the property.

- **Base ceiling 6, headroom 2:** any non-zero count scores at most 6. When violations are few and spread across many modules, the ceiling rises by up to 2, to 8.
- **Isolation fades at a density of 10 %:** at 0.10 or more wrong-way edges per module, the ceiling is 6.
- **Headroom saturates at 200 modules:** headroom grows with log10 of P and is full at 200.
- **Floor 1:** the score falls toward 1 as density rises.
- **Denominator includes modules that cannot break the rule:** test, non-product, Shared and Unknown modules count toward P.
- **Source-tree cap:** a source tree with more than 50,000 source files is not read for modules.
- **Module grain from the source tree:** a module is a top-level source folder, or a folder directly below a folder that only holds components. The loose root files form one more module.

# AX7: Slice cohesion

**Lens:** architecture · **Scored:** yes

## Property
Whether feature slices stay independent (no direct cross-slice references) — the discipline that makes vertical-slice architecture pay off.

## Applies to
Any language. It applies only to a repository organised as vertical slices (see Not measured when). What counts as a referencing unit depends on the language (see Unit and denominator).

## Population
Production code as [production-code.md](../production-code.md) defines it, placed into feature slices:

- **Slice container:** a folder, namespace or package that holds the code base's features side by side. Examples are `Features/`, `Slices/` and `UseCases/` in any language and spelling. A language's own conventions also count, for example Ruby packs, components or engines, or a Django project's `apps/`.
- **Slice:** each direct child of a slice container is one slice, with everything below it. This holds whether the child is a folder inside one project or a project of its own.
- **Project slices:** some repositories have no slice container but give each feature or bounded context its own project. In that case each project is a slice. One context's layered projects form one slice, for example `Ordering.Domain` and `Ordering.Api`.
- **Not a slice, and never either side of a cross-slice reference:**
  - a container child that holds shared code (a shared kernel, common or core code);
  - a container child that is a technical layer (for example `domain`, `data`, `infrastructure`);
  - a container child that is the app shell composing the features (for example `app`, `host`);
  - a file directly inside a container (for example a registration file or barrel);
  - code outside every slice (the composition root, top-level statements);
  - a shared project (a shared kernel, shared contracts, persistence or infrastructure);
  - with project slices only: a foundation slice that other slices depend on and that depends on no other slice (thresholds under Pending re-definition).
- **Published contract:** a slice's own contract place is its sanctioned entry point, and naming it is not coupling. Examples are:
  - a `Contracts` folder, a `Contracts` namespace or a `*.Contracts` project directly under the slice;
  - a Ruby or Python `public/` or `contracts` place;
  - a Ruby component's root file;
  - what a Python slice package exports at its root.

## Unit and denominator
**Unit: a cross-slice coupling.** A coupling is one pair: a referencing unit in one slice, and a symbol that another slice declares outside its published contract. A pair counts once, however many times the unit names that symbol. Naming a different foreign symbol is a separate coupling.

- **Referencing unit:**
  - In TypeScript, JavaScript, Python and Ruby, a source file (module).
  - In every other language, a top-level type. A nested type belongs to the top-level type that declares it.
  - Where a language lets code be declared outside any type (for example Go, C or Rust functions, or Kotlin top-level functions and properties), all of a source file's code outside types is one more referencing unit, belonging to that file.
- **Foreign symbol:** a type, or a function, constant or variable declared outside any type, that another slice declares. A foreign nested type counts as its outer type. A member of a foreign type counts as that type.
- **A reference:** any use of the symbol in code. That includes:
  - a member's declared type or a signature;
  - a base type or mixin;
  - an attribute;
  - a construction, call, cast or type test;
  - a JSX element;
  - a constant read.

  A string is also a reference when a framework resolves it to a class, for example an Active Record `class_name: "Billing::Invoice"` or a Django relation `"payments.Payment"`.
- **Not a reference:**
  - an import or `using` statement on its own;
  - a name in a comment or an ordinary string;
  - a member name (`x.Invoice`) or an object key;
  - a reference between two units of the same slice.

**Denominator:** the number of distinct slices.

## Thresholds
- A repository needs at least 2 slices to be judged.
- Each coupling counts as 1. There is no severity weighting and no minimum count.

## Score
Let *v* be the number of couplings and *n* the number of slices.

- If *v* = 0, the score is 10.
- Otherwise:
  - density *d* = min(*v* / *n*, 1)
  - isolation = max(0, 1 − *d* / 0.10)
  - headroom = min(max(log10 *n* / log10 200, 0), 1)
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − *d*) × (ceiling − 1), clamped to [1, ceiling] and rounded to 2 decimals.

What this means:

- With one or more couplings the score is always below 8. A single coupling scores close to 8 only when there are many slices. For example, it scores 7.87 with 200 slices and 3.5 with 2 slices.
- At 10 % density or more, the ceiling is 6.
- At one coupling per slice or more, the score is 1.

**Part of a repository** (for example one service): the same curve applies. *n* is the number of slices with code in that part, and *v* is the number of couplings whose referencing site lies in that part. A part with no slice code is not measured. The 2-slice minimum applies only to the whole repository: a part with at least 1 slice is scored with *n* set to its slice count.

## Not measured when
- The repository is not organised as vertical slices (rule under Pending re-definition).
- Fewer than 2 slices remain after the population rules above.
- Not one reference from slice code could be followed to anything the repository declares. The result is then not measured rather than 10.
- For a part of a repository: the part holds no slice code.

## What the result is not
- It does not show whether the slices are well chosen, sized or named. It only shows whether they reach into each other.
- It is not a measure of layering or of dependencies on shared code. Depending on the shared kernel, a layer or a published contract is the intended way to share.
- It does not apply to layered, hexagonal or other non-sliced code bases. A missing score there says nothing about coupling.
- It does not weigh how deep a coupling is. A unit that names a foreign symbol once counts the same as one built around it.

## Pending re-definition
These values are parameters of the existing reading's model, and today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Vertical-slice organisation, slices inside projects:** at least 2 types, and at least 5 % of all types, lie inside slice containers. In addition, one of these holds: the repository is not also layered into a domain layer and an infrastructure layer outside its feature projects, or it uses an in-process mediator library (for example MediatR, Mediator or Wolverine).
- **Vertical-slice organisation, a project per slice:** at least 8 production projects sit under a feature or context root folder (for example `Features/`, `Slices/`, `Modules/`, `BoundedContexts/`, `Contexts/`). Alternatively, there are at least 4 production projects and at least 50 % of them sit under such a root.
- **Foundation slice (project slices only):** with at least 3 slices, a slice that depends on no other slice and that at least 2 other slices depend on is not a slice.

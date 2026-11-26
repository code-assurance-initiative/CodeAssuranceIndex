# GD1: Unfinished & placeholder code

**Lens:** codeHealth · **Scored:** yes, advisory (the 0-10 score is published but does not change the Code Health lens score or the CAI)

## Property
Leftovers of unfinished code (for example scaffolding, or AI coding-assistant output never completed) in production code, excluding tests and code emitted by a code generator (for example files marked as auto-generated): code that still throws an error marking it as not yet implemented (for example NotImplementedException in .NET), and placeholder string literals, that is, literals whose text stands in for real content still to be written (for example "TODO", "lorem ipsum", "changeme" or "your-api-key-here"). Scored on what the code shows, as a quality signal, never as a claim about who or what wrote it.

## Applies to
Any programming language that has callables with bodies and string literals. Web markup (HTML) and styles (CSS) are not covered.

## Population
Production code as [production-code.md](../production-code.md) defines it, with these further exclusions:

- **Web markup (HTML) and styles (CSS) files:** these are not in the population and do not count in the denominator.
- **Code the product does not ship:** examples, samples, demonstrations, playgrounds and sandboxes, and the developer tools and scripts the repository runs on itself. This covers whole projects or folders. It also covers single files that exist to demonstrate or exercise the product rather than to be part of it. A file whose subject is only related to samples or examples is still product code.
- **Generated parts of hand-written files:** a region or member that a tool wrote and marks as generated, inside a file people otherwise edit (for example a form designer's generated region, or a member marked as generated code).
- **Declaration-only stub files:** files that only declare signatures for other code (for example Python `.pyi` stubs).

## Unit and denominator
**Unit:** a file in the population that holds at least one leftover. A file counts once, however many leftovers it holds. A leftover is either of the following.

1. **An unfinished stub:** a named callable (a method, function, constructor, operator, property or accessor, or a local or nested named function, as the language defines it) whose whole body does nothing but throw or raise an error that says the work is not done.
   - **Error type:** an error whose type says the work is not done counts whatever its message says. Examples are `NotImplementedException`, Python's or Kotlin's `NotImplementedError` (including Kotlin's `TODO()`), Rust's `todo!()` or `unimplemented!()`, and Scala's `???`.
   - **Error message:** an error of any other type counts when its message says the work is not done yet or is put off until later. Examples: "Not implemented yet", "Implement me", "TODO: ...", "Stub: returns nothing for now", "will be implemented in a later release". A message that refuses the work for good is a refusal and does not count. Examples: "won't be implemented", or "not supported on this platform". A refusal that also puts the work off until later counts as putting it off (for example "won't be implemented until v19"). A message that tells someone else to supply an implementation does not count either (for example "a handler must be registered").

   These do **not** count:
   - a throw that is only one branch, one operand or a guard inside a real body;
   - an anonymous function on its own, which is not judged as a callable;
   - an error whose type does not say the work is not done and whose message does not say it either (for example an "unsupported operation" error thrown by a read-only type, or by a member that answers an interface or base contract);
   - a callable the code declares as having no body of its own: an abstract member, an interface or protocol member, or an overload signature;
   - an overridable base member that some subclass in the repository overrides, which makes it abstract by convention;
   - a callable that the contract it answers can never call (for example the disabled direction of a one-way serializer converter);
   - a stub the code explicitly marks as intended, for example by switching off, for that member, the warning a code analyser gives about not-implemented throws.
2. **A placeholder string literal:** a string literal, or literal text in markup inside code (for example JSX text), whose text stands in for content still to be written. Examples: "TODO", "lorem ipsum", "changeme", "your-api-key-here", "https://yourdomain.com". An ordinary word that only contains a placeholder's letters (for example "replacement") is not a placeholder. These do **not** count:
   - a literal the code compares, matches or substitutes against (for example `== "changeme"`, a regular-expression pattern, or a list of placeholder values that the code only checks input against);
   - the hint text shown in an empty input field (for example an HTML `placeholder` attribute);
   - documentation text: docstrings, usage and help text, and values of attributes, parameters or properties whose name says they hold documentation (for example `description` or `help`);
   - text in a file whose purpose is that text (for example a lorem ipsum generator);
   - a reserved example domain (for example `example.com`) used as an example address, not as a value still to be filled in;
   - comments, which are not literals.

**Denominator:** the number of files in the population, across all its languages together.

## Thresholds
- A file is unclean at 1 leftover. There is no minimum count and no size weighting.

## Score
Let *clean* = 1 − (unclean files ÷ files in the population).

Score = 4 + 6 × *clean*, rounded to 2 decimals.

A repository with no leftovers scores 10. A repository where every file holds a leftover scores 4, the floor. The score never goes below 4.

The score is advisory. It is published with the other dimensions but carries no weight in the Code Health lens or the CAI.

## Not measured when
- The population is empty, for example when every source file is a test, generated, or code the product does not ship. The result is reported as not scored.

## What the result is not
- It is not part of the lens or the CAI. The score is advisory and changes neither.
- It is not a claim about authorship. A leftover is a defect whether a person or a tool wrote it.
- It is not a measure of how much is unfinished. A file with one stub and a file with fifty stubs count the same, and the score reads only the share of files.
- It is not incompleteness in general. Empty bodies, hollow implementations, TODO comments and disabled tests are outside this property unless they throw a not-done error or are placeholder literals.
- It is not a secret scan. A placeholder credential counts as an unfinished value, not as a leaked secret.

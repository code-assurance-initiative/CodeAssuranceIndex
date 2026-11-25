# Production code and production lines

Every dimension page that reads "production code" means the files this page defines. The last section defines
"production lines" for decision 0024's 95 % rule: an engine publishes a repository's CAI only where the languages it
reads hold at least 95 % of the repository's production lines. A dimension page states only where it differs.

## Production code

A repository's production code is its source files (code in a programming language, web page markup in HTML, and
styles in CSS) that its own authors wrote, including their examples, samples, demonstrations and their own scripts and tools. Every such file not
excluded below is production code. Other files
are not source files and are not production code: for example documentation in prose markup (Markdown,
reStructuredText), data and configuration markup (XML, YAML, JSON), images, and licence and readme files.

These are **not** production code:

- **Tests and test support:** test files, test projects, and code that exists only to help tests run (test helpers,
  fixtures, test data). A project's own declaration decides, whatever its folder: the nearest project that declares
  itself a test project is one, and one that declares itself not a test project is production code. A product that
  ships base classes for its users' own tests is production code.
- **Evaluation inputs:** material an evaluation suite feeds to what it evaluates (for example the sample programs or
  files in an evaluation suite's fixtures), which is neither the product nor its tests.
- **Benchmark projects:** a project that exists only to measure the product's performance and is not shipped (for
  example a project of benchmark runs).
- **Generated code:** files a tool writes and people do not edit, for example code generated from a schema, a protocol
  definition or a designer, or a file that says it is generated.
- **Vendored code:** third-party code copied into the repository rather than declared as a dependency, for example an
  installed dependency folder, a copied-in library, or a single-file library copied in whole. Code another person
  contributed to the project itself is production code.
- **Build output and tool state:** compiled output, packaged, minified or compiler-emitted bundles, caches, editor and
  tool folders, and files the version control ignores rather than tracks.
- **Documentation sites and showcases:** generated documentation-site output, component showcase files (for example UI
  component stories).
- **Specimen collections:** a folder of code samples or examples that holds code in 10 or more languages, or that has
  5 or more direct subfolders each named after a language and holding code in that language (typically samples a
  product reads, for example a classifier's training samples).
- **Server-side templates:** view templates a web framework fills in on the server (for example Blade or Razor
  views), even when their file name ends in a programming language's extension.
- **Data and lockfiles:** committed data files (for example tables, records, serialised datasets), source files whose only content is one literal data value, with no logic (for example a
  translation or settings catalogue written as one literal table), and the lockfiles a package
  manager writes. A source file that declares named constants is code.
- **Tool configuration files:** a file whose only job is to configure a development tool and that the tool loads as its
  settings, usually named as that tool's configuration file (for example `vite.config.ts` or `.eslintrc.js`), even
  when written in a programming language.
- **Build scripts:** a script that runs the repository's build or install steps, in any language (for example a
  Dockerfile, a Makefile, a Gradle build script, a `setup.py` or a `gulpfile.js`).
- **Linked copies:** a folder that is a link to another folder; its files count once, under their real location.
- **Hidden folders:** everything in a folder whose name starts with a dot (for example `.github` or `.venv`), except a
  folder that a documentation-site or component-showcase tool uses as the authors' own source folder (for example
  `.vuepress` or `.storybook`).
- **TypeScript declaration files:** files that only declare TypeScript types for other code (`.d.ts`). Declaration-only
  files in other languages (for example C headers) are production code.
- **Source the product's own build leaves out:** a source file inside a build project that lists what it compiles,
  where that project leaves the file out; and a file in a folder the product's build tool skips by its own convention
  (for example a Unity package's hidden folders whose names end in `~`, such as `Samples~`). A file that no such
  project or convention covers is not excluded by this rule.
- **Files the repository itself declares not to be the product:** files its own settings mark as generated, vendored
  or documentation.

A file the repository's own build or package manifest declares as part of the product (for example a declared
command-line entry point) is production code even inside a folder the build-output or hidden-folder rule would
exclude; it is still excluded when it is a test, generated, vendored, or ignored by version control.

A dimension page that leaves examples, samples or the authors' own tools out says so. How an engine recognises each category is the engine's method (decision 0024
§2).

## Production lines

The production lines of a repository are the lines of its production code files that are neither blank nor only a
comment, in the languages on this list. The list is closed: a proposal adds a language by name, and each change raises
the list's version. This is **list version 1**; an evidence bundle names the version it counted under
([cai-scoring.md](cai-scoring.md) §1.3).

- **General-purpose programming languages:** C#, F#, Visual Basic .NET, Java, Kotlin, Scala, Groovy, Clojure, Python, Cython, PHP,
  Ruby, Elixir, Erlang, Go, Swift, Objective-C, C, C++, Dart, Rust, JavaScript, TypeScript, Lua, Perl, Prolog, Haskell,
  OCaml, Julia, Zig, Nim, Crystal, R, Fortran, Pascal, Ada, COBOL, Solidity, Haxe, GDScript, Tcl, Zephir, VBA and
  VBScript, and single-file components (for example Vue or Svelte), counted whole.
- **Web markup and styles:** HTML and CSS.

Not counted: SQL; shell, PowerShell and batch scripts; build scripts in any language (above); and data
and configuration files (for example JSON, YAML, XML used as data). Files that are not production code (above) are not
counted either.

## Pending re-definition

These are parameters of the existing reading's model. Today's population depends on them, and a later decision under
decision 0013 will replace each with a rule on the property.

- **Specimen collections:** 10 or more languages, or 5 or more direct subfolders each named after a language and
  holding code in that language, in one sample folder.

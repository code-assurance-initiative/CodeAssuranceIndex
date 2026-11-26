# AXB1: Runtime evidence locked — no reproducible boot

**Lens:** maturity · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

Whether the repository declares a way to boot the application: a compose file (with one service or several, for example a docker-compose file), an orchestration project that starts the application and its services (for example a .NET Aspire AppHost), or a container build file (for example a Dockerfile). The finding is raised when none is declared: then no evidence is gathered from the running application for the dimensions observed on it (rendered accessibility, outbound requests and security headers), though those dimensions may still have evidence from reading the code. A declared way to boot is not proof that the application builds or boots. Advisory: it explains missing runtime evidence and is never counted in any CAI score.

## Applies to

Any language. The dimension reads boot declarations, not source code. A boot declaration is a file of one of three kinds:

- a compose file, which declares one or more services to start together;
- an orchestration project, which starts the application and the services it depends on;
- a container build file, which builds an image of the application.

## Population

This dimension does not use the production-code population in [production-code.md](../production-code.md). It reads boot declarations of any of the three kinds wherever they sit in the repository, with its own exclusions. A boot declaration does not count when it is:

- **Test material:** a boot declaration that exists for tests or as a test input, for example a sample container build file in a test corpus.
- **Examples, samples and demonstrations:** a boot declaration that belongs to an example, sample or demo, not to the product. Production code includes the authors' examples, but this dimension leaves them out.
- **Templates and scaffolds:** a boot declaration that a generator copies into other repositories, including one that still holds the generator's unfilled placeholders.
- **Development environments:** a container that sets up an editor's or a cloud IDE's environment and starts none of the product, for example a dev-container definition.
- **Vendored code and build output:** as [production-code.md](../production-code.md) defines them.
- **Boots of other people's code only:** a compose file where every service runs an image built elsewhere and nothing is built or mounted from the repository, for example a file that only starts the database that integration tests talk to.

## Unit and denominator

- **Unit:** a declared way to boot, meaning one boot declaration of the three kinds above that is not excluded.
- **Denominator:** the repository. The result is yes or no: does the repository declare at least one way to boot?

## Thresholds

- One declared way to boot is enough. The finding is then not raised.
- With no declared way to boot, the finding is raised.
- The kind of declaration, the number of declarations and where they sit in the repository do not matter. A container build file that publishes no network port still counts.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- The repository has no application that a person or a client reaches while it runs: no product user interface, no web service and no declared API surface. Typical examples are a library, a computation kernel or a command-line tool. Pages and apps that belong to demonstrations, documentation or developer tools do not make an application. The dimension is then not applicable, and that is not a finding.
- The repository cannot be read.

## What the result is not

- It is not proof that the application builds, boots or serves anything. A compose file that cannot start still counts as a declared way to boot.
- It is not a judgement of code quality. Many well-run applications start some other way, for example through a package script or the language's own run command. Those ways are not among the three kinds this dimension names.
- It does not say whether the runtime-observed dimensions have evidence. They may still have evidence from reading the code.
- It does not say which declaration is the product's main boot when there are several.

## Pending re-definition

- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.
- **Files read:** at most 50,000 of the repository's files are examined for a boot declaration. A declaration beyond that limit is not seen. This is a parameter of the existing reading's model, and a later decision under decision 0013 will replace it with a threshold on the property.

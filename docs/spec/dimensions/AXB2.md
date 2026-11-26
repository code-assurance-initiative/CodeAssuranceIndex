# AXB2: Runtime readiness

**Lens:** maturity · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

How reproducibly this app can be booted for runtime evidence, plus the test frameworks detected. Advisory — never affects the score.

The word "advisory" in this definition describes how the one existing engine reports the dimension. It is not the standard's status. That status is still open: see Status under Pending re-definition.

## Applies to

Any language. The dimension reads two things the repository declares: how to start the application, and which test frameworks it uses. The application can be written in any language.

## Population

- **The application, once.** The unit is the repository's application as a whole, not its files. A repository may declare more than one way to start it. The result then reports one declared boot and names it: the file, script or command it starts, and its folder.
- **Declared boot.** A declared boot is a way to start the application that the repository itself states. It must run without the reader writing new steps. Examples: a container definition, a multi-container composition, an application-host project, a development-server script in a package manifest, a language toolchain's run command for a server, or a mobile application project that builds and installs.
- **What the boot starts.** The boot may start any code it declares. That code need not be production code as [production-code.md](../production-code.md) defines it.
- **Test frameworks.** These are the test frameworks the repository declares it uses, in any ecosystem. A repository can declare a framework in three ways:
  - as a dependency in a package or build manifest, including the manifests of test projects (for example xUnit, NUnit, JUnit, pytest, Jest, RSpec);
  - through the framework's own configuration file (for example a Jest or Playwright configuration file, a PHPUnit configuration file, a pytest settings file, or an RSpec options file);
  - through the test setup its language toolchain requires (for example ExUnit, which ships with Elixir and is declared by the project's test helper that starts it).

  A framework counts once, however many projects or files declare it.
- **Environment.** The boot runs in an isolated environment. No secrets are supplied there. The only external services are those the repository's own declared boot starts.

## Unit and denominator

The unit is one boot attempt of the declared boot, with its outcome, plus the list of test frameworks. There is no denominator. The result has four parts:

- **Boot mechanism:** the kind of declared boot and what it starts (the file, script or command, and its folder), or "none".
- **Outcome:** exactly one of these:
  - **booted:** the application came up. It did at least one of these things: published a network port, served a page or screen that could be reached, or answered a web request without serving a page (for example an API). Any published ports are listed.
  - **failed:** the boot was attempted and the application did not come up. The reason the boot reported is carried word for word.
  - **not attempted:** no boot was tried. The reason is stated.
- **Test frameworks:** the list, or "none detected".
- **Cause of a failure, where known:** the repository (with the change that would fix it), the environment, or unknown.

A declared mechanism is never reported as an outcome.

## Thresholds

- A boot step that produces no output for 120 seconds has not come up.
- A boot step that has run for 30 minutes in total has not come up.
- A development-server script gets 2 minutes to serve a page. An application-host project gets 15 minutes. When that time runs out, the outcome is booted if the application published a port or answered a request. Otherwise the boot did not come up.
- An application-host project stops waiting for a page once its open ports have stayed the same for 2 minutes. The outcome is then booted if it published a port or answered a request, and failed otherwise.
- A server started by a toolchain's run command has not come up if it is not listening within 5 minutes.
- A mobile application has not come up if its emulator has not finished booting within 5 minutes.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- The repository declares no boot, no test framework is detected, and no boot was attempted. Nothing is reported.
- Runtime evidence was not gathered for the run. The outcome is "not attempted", with the reason.

## What the result is not

- It does not say whether the application boots in its owners' own environment, with their secrets and external services. It says only whether it booted in an isolated environment that has neither.
- One boot does not prove reproducibility. A single "booted" or "failed" is one observation.
- "booted" does not mean the application works. It means the application came up: it published a port, served a page or screen, or answered requests.
- A "failed" outcome caused by the environment, or by something unknown, is not a defect of the repository.
- A detected test framework does not mean tests exist, run or pass.
- A declared boot mechanism is not a result.

## Pending re-definition

These values are parameters of the existing reading's model, and today's outcome depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Idle limit:** 120 seconds with no output from a boot step.
- **Total limit:** 30 minutes for one boot step.
- **Development-server wait:** 2 minutes to serve a first page.
- **Application-host waits:** 15 minutes to serve a page. The wait also ends after 2 minutes in which the open ports do not change.
- **Start limit:** 5 minutes for a server started by a toolchain's run command to start listening, or for a mobile emulator to finish booting.
- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.

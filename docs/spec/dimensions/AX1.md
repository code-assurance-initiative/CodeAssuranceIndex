# AX1: Captive dependencies

**Lens:** architecture · **Scored:** yes

## Property
Whether any singleton service captures a scoped/transient dependency — a silent lifetime/threading bug.

## Applies to
Any language whose code wires services through a dependency-injection container that gives services different lifetimes. Some examples: the .NET service container in C#; Spring, Guice or Koin on the JVM; Riverpod, get_it, Provider or GetX in Dart; InversifyJS, tsyringe, NestJS or Awilix in TypeScript and JavaScript; dependency-injector in Python; Laravel in PHP; dry-container in Ruby; Swinject or Factory in Swift; MacWire in Scala. A container the repository writes for itself also counts.

## Population
Production code as [production-code.md](../production-code.md) defines it, plus one addition. A runnable application is in scope even when it sits among the tests, as long as it contains no tests and does not declare itself a test project. For example, a demo web app kept in a test folder is in scope because its container really runs.

What is read is every container registration in that code, together with the constructors and resolution calls of the services those registrations create.

## Unit and denominator
**Lifetimes.** A container creates each service with one of these lifetimes:

- **Long-lived:** created once and shared for as long as the container lives. For example: a singleton, a Spring singleton bean, a keep-alive Riverpod provider, a get_it singleton, a GetX permanent or app-lifetime instance, or the value a widget-tree provider creates once.
- **Shorter-lived:** one of two kinds:
  - **Scoped:** one instance per request, scope, session or route, or an auto-dispose provider.
  - **Transient:** a new instance for every resolution, for example a prototype bean, a get_it factory, or a value rebuilt by a proxy provider.

**A dependency's lifetime** is the shortest lifetime it is registered under. That covers registrations by its service type or by its implementation type, and registrations under its key when it is requested by key. A registration the framework makes itself also counts; for example, the .NET options snapshot is scoped. A constructed generic type takes the lifetime of its open generic registration. A dependency that no registration covers has no lifetime and cannot be captured.

**The unit is a capture.** A capture is one place where a long-lived service ends up holding an instance of a shorter-lived dependency:

- **Kept dependency:** the service receives the dependency by injection (for example as a constructor parameter) and keeps it after construction. This still applies when the dependency comes wrapped in a lazy wrapper or in a collection.
- **Root resolution:** the service resolves the dependency from the root container, outside any scope. This can happen while the service is being built or later through a container it kept. A scoped dependency resolved this way counts whether or not it is kept. A transient one counts only if it is kept.

These are **not** captures:

- **Per-use access:** taking a factory, a provider, a scope factory or the container, and on each use either creating a scope and resolving the dependency in it, or resolving a transient dependency and not keeping it. A scoped dependency resolved from the root container counts as a capture (see Root resolution) even when it is resolved per use.
- **Construction-only use:** using the dependency during construction only to read a plain value (a number, string, boolean, date or enum), then letting it go.
- **Stateless transients:** a transient dependency whose implementations all hold no instance state and are not disposable. One kept instance then behaves like a fresh one. Scoped dependencies never get this exception.
- **Proxied scopes:** a shorter scope declared with a scoped proxy, so the long-lived service holds the proxy rather than the instance.

If one place is reached through two registrations of the same long-lived service, it counts as one capture.

**Denominator:** the number of long-lived registrations across every container read, with a minimum of 1.

## Thresholds
- **Any capture counts.** A score of 10 requires zero captures.
- **Density:** at a capture density of 10 % or more, the highest possible score is 6.0. Below 10 %, that highest possible score rises toward 8.0.
- **Population saturation:** the headroom above 6.0 grows with the number of long-lived registrations and is full at 200 registrations.

## Score
Let *v* be the number of captures and *n* = max(long-lived registrations, 1).

- If *v* = 0, the score is **10**.
- Otherwise:
  - density *d* = min(*v* / *n*, 1)
  - isolation = max(0, 1 − *d* / 0.10)
  - headroom = min(1, log₁₀(*n*) / log₁₀(200))
  - ceiling = 6.0 + 2.0 × isolation × headroom
  - score = 1.0 + (1 − *d*) × (ceiling − 1.0), rounded to 2 decimals

Worked values:

- A single capture among 200 or more long-lived registrations scores close to 8.
- Any capture among a handful of registrations scores 6 or less.
- When there are at least as many captures as long-lived registrations, the score is 1.0.
- A repository that registers services but none as long-lived scores 10.

## Not measured when
- **Not applicable, no container:** the production code uses no container and defines none of its own, so it cannot hold a captive dependency. Two examples: a language with no such containers, or code that imports no container and has nothing that registers services under two lifetimes.
- **Not applicable, capture impossible:** the code's only containers cannot, by their own rules, give a long-lived service a shorter-lived instance. For example:
  - a container that rejects such wiring when it compiles
  - one that always injects shorter-lived services through a proxy
  - one where a long-lived service cannot see a shorter-lived provider and fails instead
  - one that makes a service asking for a shorter-lived dependency shorter-lived itself
  - one that creates every service once for the life of the application
- **Not measured:** no container's lifetimes could be read at all. This covers two cases:
  - The code uses a container but none of its registrations can be read.
  - Production files that could hold a container could not be read, and no container was found.

  No score is given in either case, and the result is never 10.
- **Partial reading:** where at least one container's lifetimes are read, the score covers only the containers read. The result names each container or file that was left out.

## What the result is not
- It does not prove a bug at runtime. A kept shorter-lived instance may be harmless in a given deployment, and some containers reject some captures at startup.
- It does not show that the whole container is wired correctly. This dimension does not count:
  - a missing registration
  - a wrong lifetime that causes no capture
  - a service resolved per use but never disposed
- It does not count services wired by hand outside a container.
- A 10 does not mean the code uses a container well. It only means no long-lived service holds a shorter-lived one.

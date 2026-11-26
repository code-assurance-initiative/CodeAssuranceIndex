# AXS1: Runtime exposed surface

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

The catalog paragraph below calls the result advisory evidence, but the catalog has not formally declared the dimension advisory. Until a decision settles it, no CAI score includes AXS1.

## Property

Every network binding the application actually opens when the stack the repository declares is started as declared, each with its service, container port, host port, protocol and bind scope (the addresses the port listens on, for example all interfaces or the local machine only), with any port the repository's own files never declare called out, for example one a base image opens. A port counts as declared when a file of the repository names it, for example a compose file's port mappings or a container build file's exposed ports. The bindings themselves are observed on the running application, never taken from those files, which serve only to tell declared ports from undeclared ones. An inventory, not a risk verdict: it does not say which networks can reach a port. Advisory: reported as evidence, never counted in any CAI score.

## Applies to

Any language. The dimension reads a running application, not source files. It applies to a repository that declares a stack that can be started, for example a multi-container compose file or a container build file.

## Population

- **Bindings:** every network binding that any service of the declared stack opens after the stack is started as the repository declares it, with no added profiles, overrides or extra services.
- **Declaring files:** any file of the repository that names a container port. This includes files that are not production code under [production-code.md](../production-code.md), for example compose files and container build files, which that page treats as build scripts or configuration. Here they count only as declarations. [production-code.md](../production-code.md) does not decide which bindings are in scope.
- **Excluded:** ports that only a different start-up of the stack would open (for example a production-only profile), and ports the application opens only after a later action.

## Unit and denominator

- **Unit:** one binding. A binding is one service, one container port (the port the application listens on inside its container), one host port (or none), and one protocol (for example tcp or udp). It also has a bind scope.
- **One binding, several address families:** when one port is published on both IPv4 and IPv6, it counts as one binding. Its bind scope is the more exposed of the two addresses.
- **Published or internal:** a binding is published when it has a host port. A binding with no host port is internal: only other services on the same stack's network can reach it.
- **Bind scope:** a published binding is loopback only when its host address is the local machine (`127.0.0.1`, `::1` or `localhost`). Any other host address, including one left unspecified, is all interfaces.
- **Declared port:** a container port that a file of the repository names as a port of the stack. Examples are the container side of a compose port mapping (in the short or the long form), a compose exposed port, or a container build file's exposed-port instruction. A port range declares every port in it. Host-side numbers do not declare anything. A port named for any protocol counts as declared.
- **Denominator:** none. The result is an inventory that reports every binding. The summary counts are: published bindings, published bindings on all interfaces, internal bindings, distinct services, and undeclared bindings.

## Thresholds

- A binding is **undeclared** when it is published and its container port is not a declared port.
- An internal binding is listed but never called undeclared, even when no file names its port.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- The repository declares no stack that can be started.
- The declared stack did not start.
- The stack started but opened no binding.

## What the result is not

- It is not a risk verdict. A binding is a fact, but whether publishing it is dangerous depends on the network it is published onto, and that is not assessed.
- It is not every binding the application could ever open. It comes from one start of the stack as declared, so a port that only another profile or a later action opens is not seen.
- It is not a check of the declarations. A declared port that the stack never opens is not reported.
- An undeclared binding does not prove that a base image opened the port. A base image is only the most common reason a published port goes unnamed.

## Pending re-definition

These are parameters of the existing reading's model. Today's result depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Wide port ranges:** a declared range of more than 1,024 ports declares only its first and last port.
- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve. The catalog paragraph calls the result advisory evidence, but the catalog has not formally declared the dimension advisory. Until a decision settles it, no CAI score includes AXS1.

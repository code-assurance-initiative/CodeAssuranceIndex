# AXI1: Runtime container hardening

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

The confinement each container was actually started with — effective user, privileged mode, added and dropped capabilities, read-only root filesystem, no-new-privileges — read from the running stack rather than from a Dockerfile. Advisory; never scored.

The definition ends with "Advisory; never scored." Those words describe how today's engine reports the dimension. The catalog does not declare it advisory, so in the standard it is scored. No curve exists yet, so its status is pending (see Pending re-definition).

## Applies to

Any language. The dimension reads the containers that the repository's own container configuration starts, for example a compose file or a Dockerfile.

The property belongs to the running containers, not to source lines. Container configuration files are build scripts, and [production-code.md](../production-code.md) leaves build scripts out of production code. That does not matter here, because this dimension reads no production code.

## Population

- **In scope:** every container that the repository's own stack starts when it boots from the repository's own configuration. Each container is read as it actually started. Settings it inherits from a base image count as they resolved, and so do compose settings that override an image.
- **Out of scope:**
  - containers that the repository's configuration does not start;
  - any confinement an outside platform adds at deployment, for example an orchestrator's default security policy, an admission controller that rewrites containers, or a separate set of deployment manifests.
- **Settings the runtime adds:** a setting the container runtime adds by itself counts only as the runtime's setting, not as a choice the repository made. One example is the label setting a runtime adds to every privileged container.

## Unit and denominator

- **Unit:** one started container and the confinement it actually runs with. Six facts are read for each container:
  - **Effective user:** the user the main process runs as. A container that was given no user runs as root.
  - **Privileged mode:** whether the container runs privileged.
  - **Capabilities added:** the Linux capabilities added on top of the runtime's default set.
  - **Capabilities dropped:** the Linux capabilities removed from the runtime's default set.
  - **Read-only root filesystem:** whether the root filesystem is read-only.
  - **No-new-privileges:** whether the repository's own configuration turns on no-new-privileges.
- **Denominator:** the containers in the population.
- **Reported counts:** the number of containers seen, how many run privileged, how many run as root, and how many are confined (see Thresholds).

## Thresholds

- **Runs as root:** the effective user is user id 0. This covers three cases:
  - no user was given;
  - the user is `root` or `0`;
  - the user is `root` or `0` followed by a group.
- **Exposed:** a container is exposed if it does at least one of these:
  - it runs privileged;
  - it runs as root;
  - it has one or more capabilities added.

  Each of these is a separate finding for that container.
- **Confined:** a container is confined if it meets both conditions:
  - it is not exposed;
  - it has at least one confinement beyond the defaults: one or more capabilities dropped, a read-only root filesystem, or no-new-privileges turned on.
- **Not exposed but not confined:** a container that is not exposed and has none of those extra confinements is reported as not exposed. It does not count as confined.
- **Minimum population:** at least one container in the population.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- the repository has no container configuration that starts a container;
- the stack does not build or boot;
- the stack boots but starts no container;
- the stack boots, but the configuration of its running containers cannot be read.

## What the result is not

- It describes only this boot, started from the repository's own configuration. It is not the confinement found in production, where a different orchestrator, policy or manifest set can tighten or loosen it.
- It does not judge whether a workload really needs an added capability or privileged mode. It records only that these were granted.
- A healthcheck is not a confinement. Whether a container has one is not part of this property.
- It is not a reading of what a Dockerfile declares. If a Dockerfile declares a user and that user is overridden when the container starts, the user it resolved to is what counts.

## Pending re-definition

- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.

# AXP1: Runtime third-party data flows

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

Third-party hosts the rendered UI tried to reach (analytics/trackers/CDNs), observed at render time — advisory evidence for GDPR data-protection-by-default. Never scored; jurisdiction/role never inferred.

## Applies to

User interfaces that render as web pages. This covers the HTML and CSS of those pages and the scripts and resources they load, whatever language produces them on the server or at build time.

## Population

- **What is read:** the repository's rendered user interface, not its files. A surface is one rendered view of the product. For example, a surface can be:
  - a page the product's build prerenders;
  - a page the running application serves;
  - a page reached with a login session the repository itself provides;
  - a page the repository's own end-to-end tests reach;
  - a component showcase story, where the product's UI exists only as components.
- **Differs from [production-code.md](../production-code.md):** a component showcase is not production code, but its stories count as rendered surfaces here when they are how the product's UI can be rendered. The repository's own end-to-end tests are not production code either, but the pages they reach are surfaces. Production code as [production-code.md](../production-code.md) defines it does not decide which surfaces exist.
- **Out of scope:** requests to the origin that serves the rendered surface itself, and requests that never leave the page (for example inline data, blank pages and in-memory objects).
- **How a surface is produced:** the repository's own build or start-up produces it.

## Unit and denominator

- **Unit:** one third-party host that at least one rendered surface tried to contact while it was being rendered. A third-party host is any host name other than the one serving the rendered surface. The attempt is the observation. Whether the request would have succeeded does not matter.
- **Counted once:** a host counts once for the repository, however many surfaces, renders or requests reached for it. Host names are compared without regard to case. For each host, the result lists every kind of resource the UI asked it for (for example a script, a stylesheet, a font, an image or a beacon).
- **Marked:** a host is marked with its kind when it, or a domain it belongs to, is a well-known service of one of these kinds:
  - analytics;
  - advertising, including social-network advertising and tracking (for example a social network's pixel or plugin);
  - tag management;
  - session replay;
  - error and performance monitoring;
  - customer support or messaging widgets that track visitors (for example a hosted chat widget);
  - font delivery that receives the visitor's address (for example a hosted font service).

  A host that is not marked is still reported as contacted. Not being marked does not mean the host is safe.
- **Denominator:** none. The result is the set of hosts across all rendered surfaces of the repository, plus two figures: the number of hosts and the number of marked hosts.

## Thresholds

- One attempt to contact a third-party host is enough for that host to count.
- Each surface is rendered 2 times (see Pending re-definition).

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

The best case is a rendered UI that tried to contact no third-party host. Any attempt is evidence for a data-protection-by-default review. Neither case is a number on the 0-10 scale.

## Not measured when

- The repository has no user interface that renders as a web page.
- The repository's UI could not be built, started or rendered, so no surface was rendered.

## What the result is not

- It is not a verdict on lawfulness. It does not judge a host's jurisdiction, its role as controller or processor, or the lawful basis for the transfer. Those are for the repository's owner to decide.
- It is not a full list of the product's data flows. It covers only what the rendered surfaces tried to reach while they were rendered. Where the repository provides a login session, those surfaces can include pages behind a login. It does not cover requests made by server-side code, requests from native mobile screens, or requests from pages that no rendered surface reaches.
- Marking is conservative: only services that clearly collect data are marked. A host that is not marked can still collect data.

## Pending re-definition

These are parameters of the existing reading's model. Today's result depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.
- **Renders per surface:** each surface is rendered 2 times, and the hosts from all renders are combined.
- **Surface cap:** at most 200 surfaces are rendered per repository. Surfaces beyond the cap are not read. A host that only those surfaces contact is not counted.

# AXA1: Runtime unauthenticated reachability

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

Of the screens reached by crawling with the application's own logged-in session, which ones also answer a GET carrying no credentials. Observed on the live app; GET only, and only URLs the crawl already fetched. Advisory; never scored.

## Applies to

Any language. The dimension reads the running application, not its files: it applies to a repository whose application can be started and serves screens over HTTP (for example a web application or a server that renders pages). It applies to no file kind directly.

## Population

- **In scope:** the screens (URLs) the application served while it was crawled with its own logged-in session. A screen counts once, however often the crawl reached it.
- **Out of scope:**
  - any URL the crawl did not fetch. No URL is invented, guessed or derived from another.
  - every request other than GET. Nothing that changes the application's state is sent.
  - screens reached only by a crawl without a session.
- Production code as [production-code.md](../production-code.md) defines it does not decide this population. The unit is a served screen, not a source file.

## Unit and denominator

- **Unit:** one screen from the population, requested twice by GET with no credentials at all (no cookie, no session, no authorisation header, no stored login state). The response body is not read. Only the HTTP status of each request counts.
- **Settled screen:** both requests returned the same HTTP status. A screen whose two requests disagree, or where a request got no answer, is unsettled. The page makes no claim about it in either direction and reports how many screens were unsettled.
- **Each settled screen is one of three kinds:**
  - **Answered anonymously:** the status is 2xx (200 to 299).
  - **Declined:** the status is 401 or 403, or a redirect (300 to 399), which is typically a redirect to a sign-in page.
  - **Neither:** any other status (for example 404, or a 5xx server failure). It is evidence neither of access nor of refusal.
- **Reported figure:** the number of screens that answered anonymously, out of all screens requested (unsettled ones included).
- **Findings:** one per settled screen. A screen that answered anonymously is a warning. A screen that declined is positive evidence of access control.

## Thresholds

- Anonymous access counts only when both requests return a 2xx status.
- Refusal counts only when both requests return 401, 403 or a 3xx status.
- Two requests per screen. One request is not evidence.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- The crawl had no logged-in session. Without one, every crawled screen is public by construction, so nothing is measured. This is reported as not measured, never as a clean result: it says nothing about whether access control is present or missing.
- The application could not be started and observed live, for example when only a static render was possible or the application failed to boot.
- The crawl with a session reached no screen.

## What the result is not

- **Not authorisation.** A screen may answer 2xx with an empty, redacted or public body. Reachability does not mean that protected data was served.
- **Not a verdict on intent.** Many screens are meant to be public, for example a landing page that the logged-in crawl also passed through. Whether a screen should answer anonymously is a question about the product.
- **Not a scan.** Only screens the crawl found are checked. A protected URL the crawl never reached is not tested, and an unprotected one is not found.
- **Not about other methods.** Only GET is sent. Access control on requests that change state is not observed.
- **Not read from source.** The result describes the running application in the configuration it was started in, not its code.

## Pending re-definition

These are parameters of the existing reading's model. Today's result depends on them, and a later decision under decision 0013 will replace each with a threshold on the property.

- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.
- **Screens requested:** the first 100 screens of the population. Screens beyond the first 100 are not requested.
- **Answer time:** a request that gets no answer within 5 seconds counts as no answer, so its screen is unsettled.

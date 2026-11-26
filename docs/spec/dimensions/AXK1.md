# AXK1: Runtime cookie security

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property
The HttpOnly, Secure and SameSite flags on the cookies the live application actually issued, read from its own responses. Advisory; never scored.

## Applies to
Any language. The dimension reads a running web application's HTTP responses, not its source files. It applies to any repository whose product is a server that answers HTTP requests and can be started for the reading.

## Population
- **In scope:** every cookie the running application sets through a `Set-Cookie` response header on the responses read. Each `Set-Cookie` header sets one cookie and is read on its own.
- **Responses read:** the responses the running application returns for the pages visited during the reading. A cookie the application sets only on a page that was not visited is not in the population.
- **Out of scope:** cookies set by script in the page rather than by a response header, cookies set by third-party origins, and `Set-Cookie` headers that carry no cookie name (no `name=value` pair before the first `;`, or an empty name).
- **Files:** this dimension does not read production code as [production-code.md](../production-code.md) defines it, and has no file population. It does not read history, so [people.md](../people.md) does not apply.

## Unit and denominator
- **Unit:** a cookie, identified by its name. The name is the text before the first `=` in the header's first part. A cookie re-issued on many responses still counts once.
- **Flags read on each cookie:**
  - **HttpOnly:** the header carries the `HttpOnly` attribute (case does not matter).
  - **Secure:** the header carries the `Secure` attribute (case does not matter).
  - **SameSite:** the header carries a `SameSite` attribute with an explicit value: `Strict`, `Lax` or `None`. A missing attribute, or one with no value, means no policy was stated and the browser's default decides.
- A cookie name carries a flag only when every issuance of it that was read carries that flag.
- **Denominator:** the number of distinct cookies set. Today's result reports four counts: cookies set, cookies with HttpOnly, cookies with Secure, and cookies with an explicit SameSite policy.

## Thresholds
- **Fully flagged:** a cookie carries HttpOnly, Secure and an explicit SameSite policy, all three. Each such cookie is recorded as positive evidence.
- **Missing a flag:** a cookie that lacks any of the three is a finding that names the cookie and each missing flag.
- **Secure over plain http:** when the application was served over plain http for the reading, a missing Secure flag is reported with a caveat and not as a defect. An application may set Secure only when it is served over TLS.
- **10 means:** every cookie carries HttpOnly, Secure and an explicit SameSite policy.
- **0 means:** cookies were issued without the flags that keep them out of script and off plain transport.

## Score
No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when
- The application cannot be started and serve responses, or the product is a static site with no server.
- The running application set no cookie on the responses read. With nothing observed, nothing is claimed.

## What the result is not
- It is not a verdict on the application in production. A proxy, an environment setting or TLS in front of the application can each change a cookie's flags, and the reading sees only what the application issued under the reading's conditions.
- A missing Secure flag seen over plain http does not show that the application omits it behind TLS.
- It is not a census of every cookie the application can set. Cookies set only on pages that were not visited, after a login that was not performed, or by script are not seen.
- It does not judge whether a flag is right for a cookie. For example, a cookie that script must read cannot be HttpOnly, and `SameSite=None` counts as an explicit policy.
- It does not read the cookie's other attributes (for example lifetime, domain, path or name prefixes).

## Pending re-definition
- Status: the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.

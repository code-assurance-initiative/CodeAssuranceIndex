# AXH1: Runtime security headers

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

Which HTTP security headers the running application actually sets in its responses (for example Content-Security-Policy, Strict-Transport-Security, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy or Cross-Origin-Opener-Policy), observed while the application runs, never inferred from source. A header counts as set when a response carries it: the result records presence, not whether a value is strong (a policy that allows inline scripts still counts as set), and a deployment configured otherwise may send other headers. Advisory evidence for application security, for the secure-by-default requirements of the EU Cyber Resilience Act (CRA) and for the cybersecurity risk-management measures of the NIS2 Directive; never counted in any CAI score.

## Applies to

Any language. The dimension applies to a repository whose product serves pages over HTTP once it is started, for example a web application or a web server. It reads responses from the running application, not files.

## Population

- **In scope:** the pages the running application serves. For each page, only the response that delivers the page document itself is read.
- **Cap:** at most the first 200 pages served are read. Pages after that are not read.
- **Out of scope:** responses for a page's sub-resources (for example scripts, styles, images or API calls the page makes). Also out of scope:
  - files shown without a running server (for example static markup opened directly), because there is no server response;
  - headers that infrastructure in front of the application adds or removes (for example a reverse proxy, a load balancer or a content delivery network) when that infrastructure is not part of the run.
- The file population in [production-code.md](../production-code.md) does not apply: this dimension counts no files or lines.

## Unit and denominator

- **Unit:** a baseline security header the application sets. A baseline header is set when at least one page response in scope carries that header name, whatever its value. An empty value still counts. Header names are compared without regard to letter case, as HTTP defines them.
- **Union over pages:** the headers are counted across all pages read. A header that only one page sends still counts as set for the application.
- **Baseline (11 headers):** Content-Security-Policy, Cross-Origin-Embedder-Policy, Cross-Origin-Opener-Policy, Cross-Origin-Resource-Policy, Permissions-Policy, Referrer-Policy, Strict-Transport-Security, X-Content-Type-Options, X-DNS-Prefetch-Control, X-Frame-Options, X-Permitted-Cross-Domain-Policies.
- **Denominator:** the 11 baseline headers.
- **Reported figure:** the number of baseline headers set, out of 11 (for example "7/11").
- **Findings:** one finding for each baseline header that no page response in scope carried.

## Thresholds

- A baseline header counts as set when at least 1 page response in scope carries it.
- The header's value is not judged. Any value, weak or strong, counts as set.
- Headers outside the baseline do not count toward the figure.
- At most 200 pages are read.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- The application was not started, or it served no page over HTTP during the run.
- No response in scope carried any header at all.

In these cases nothing is reported: there is no figure and there are no findings. An absent result does not mean that headers are missing.

## What the result is not

- It is not a judgement of header values. A permissive Content-Security-Policy or a short Strict-Transport-Security lifetime counts as set.
- It is not a reading of the source or the configuration. A header that the source appears to configure counts as not set if no response carried it.
- It is not the production deployment. A production edge, proxy or content delivery network may add or strip headers that this run cannot see.
- It is not per page. A header set on one page and missing on others counts as set.
- It does not cover headers that should be absent (for example headers that disclose the server or framework), cookies, or sub-resource responses.

## Pending re-definition

- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.
- **Baseline list:** the 11 headers above are the existing reading's selection. They are a published list of recommended response headers, minus its two per-response caching and sign-out headers (Cache-Control, Clear-Site-Data). This is a parameter of the existing reading's model, and a later decision under decision 0013 will replace it with a threshold on the property.
- **Pages read:** at most 200 pages per run. This is a parameter of the existing reading's model, and a later decision under decision 0013 will replace it with a threshold on the property.

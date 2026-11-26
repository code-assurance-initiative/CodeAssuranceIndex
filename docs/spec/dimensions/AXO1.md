# AXO1: Runtime API surface

**Lens:** securityCompliance · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property
The API description the running application serves about itself, diffed against the one the repository commits, plus anonymous GET probes of its parameter-free operations. Templated paths and non-GET methods are listed as surface and never requested. Advisory; never scored.

The words "Advisory; never scored" describe how the one existing engine reports this dimension today. The catalog does not set the dimension as advisory, so in the standard it is scored. No score curve is defined yet (see Score and Pending re-definition, Status).

## Applies to
This applies to any language, where the repository's application runs as an HTTP service. The file kind read from the repository is a machine-readable HTTP API description, for example an OpenAPI 3.x or Swagger 2.0 document in JSON or YAML. The other side is the same kind of document, served by the running application about itself.

## Population
- **The running application:** the application started from the repository's own declaration of how to start it, in an isolated environment. It is observed only if it starts and answers HTTP.
- **The served description:** the API description the running application serves at one of the default locations its framework publishes (for example `/openapi.json` or `/swagger/v1/swagger.json`).
- **The committed description:** the API description of this application that the repository commits as one of its own files. A description inside vendored code or build output is not the repository's description (production-code.md defines both categories).
- **Operations:** every operation either description declares. Non-GET operations and operations with templated paths are surface. They are listed and counted, and never requested.

## Unit and denominator
- **Operation:** an HTTP method together with a path, as an API description declares it (for example `GET /users` or `DELETE /users/{id}`). The methods are GET, PUT, POST, DELETE, OPTIONS, HEAD, PATCH and TRACE. Other entries on a path are not operations, for example shared parameters, a summary or a list of servers. Two operations are the same when they have the same method and the same path.
- **Served but not documented:** an operation the served description declares and the committed description does not.
- **Documented but not served:** an operation the committed description declares and the served description does not. This is documentation drift, not a security finding.
- **Probeable operation:** a served GET operation whose path starts with `/` and has no templated segment.
- **Answered anonymously:** a probeable operation that gave the same 2xx status to two GET requests sent with no credentials, cookies or body. A redirect (3xx), a refusal (401, 403) or a server error (5xx) does not count as an answer. If the two answers disagree, the operation is not cited.
- **Denominators reported:**
  - The number of served operations. The served-but-not-documented count is read against it.
  - The number of probed operations. The answered-anonymously count is read against it.
  - Beside them, the number of served operations not requested, meaning those with templated paths or non-GET methods.

## Thresholds
- A served operation counts as served but not documented when it is missing from the committed description. No margin applies.
- A probe counts as answered anonymously only when both requests get the same 2xx status.

## Score
No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when
- The application does not start, or does not answer HTTP.
- The running application serves no API description and the repository commits none.
- If only one side exists, no comparison is made. The served count or the committed count is still reported.
- No probes run when the application serves no description, or when it serves no probeable operation.

## What the result is not
- It does not show what production exposes. The application is started from the repository's own declaration, which often selects a development configuration. An operation or description served here may not be served in a deployed environment.
- It does not enumerate hidden endpoints. It finds only what the application itself declares. An endpoint the served description leaves out is invisible to this dimension.
- It does not test the whole API. Templated paths and every method other than GET are never requested, so the probe covers only part of the surface.
- An anonymous 2xx answer does not mean the operation should be private. This dimension does not decide whether an operation is meant to be public.
- A documented-but-not-served operation is drift, not a vulnerability.

## Pending re-definition
- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.

The items below are parameters of the existing reading's model. Today's findings depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Probe cap:** only the first 50 probeable operations are probed.
- **Answer time:** a request that gets no HTTP status within 5 seconds has no answer. Such a probe is not answered anonymously and counts toward giving up. The same 5-second limit applies to fetching the served description from each default location. A location that does not answer in time serves no description.
- **Giving up:** probing stops after 5 probed operations in a row that got no answer to either request.
- **One served description:** the application's description is taken from the first default location that answers with a description declaring operations. Other locations are not read.

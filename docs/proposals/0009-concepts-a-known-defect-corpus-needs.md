# Proposal 0009: Add the concepts a known-defect corpus needs to the taxonomy

- **Number:** 0009
- **Filed:** 2026-11-05
- **Proposer:** Jimmy Borch, for the CAI team (drafted with Claude Code)
- **Declared interests:** the proposer maintains CCA (formerly Watchdog), whose known-defect regression floor (below)
  needs these concepts to be scored. Adding a concept changes no score of any codebase and no existing key; a scanner
  with no rule for a concept is mapped to it with no rule (contract 1.4 `unmapped`), which records a real miss rather
  than hiding it.
- **Moves scores:** no CAI score moves. Benchmark recall moves only for keys that use the new concepts, which no
  published key does today.
- **Status:** Accepted by [decision 0009](../decisions/0009-concepts-a-known-defect-corpus-needs.md)

## Problem

Issue: [#41](https://github.com/code-assurance-initiative/CodeAssuranceIndex/issues/41).

On 2026-11-05 an external tester scanned a deliberately vulnerable web application with eight planted defects
through a CCA product. None was found, and the product reported the application's security as exemplary. The
benchmark had no way to have caught this: its units are clean applications with planted defects of the taxonomy's
concepts, and several of the most common web-application defects have no concept at all, so neither a key nor a
scanner mapping can name them. The planted defects included an identity taken from an unsigned cookie and a review
anyone signed in could edit: no concept covers either.

CCA has since added a known-defect layer to its private training corpus: eight documented vulnerable applications in
seven languages (OWASP Juice Shop, NodeGoat, WebGoat, RailsGoat, DVWA, pygoat, dvcsharp-api and the tester's
application), keyed from each application's own documentation of its defects, never from scanner output: 291
must-fire entries. **112 of them (38 %) use one of 34 concepts the taxonomy does not have.** They can be
recorded, but no scanner can be credited for them.

## Change

Add the 34 concepts below to the taxonomy, each with the definition, CWE and OWASP Top 10 2025 category given
in the annex (written by the corpus curator from each application's documentation and the CWE text). None of them is
a child of an existing umbrella; where the annex names a near concept (for example `missing-authorization`, whose
definition excludes per-record ownership), the boundary is stated.

| Concept | Known-defect must-fires | Applications |
|---|---|---|
| `client-controlled-identity` | 12 | 7 |
| `missing-ownership-check` | 11 | 7 |
| `missing-function-level-authorization` | 5 | 4 |
| `cross-site-request-forgery` | 7 | 5 |
| `unrestricted-file-upload` | 4 | 2 |
| `weak-password-requirements` | 4 | 4 |
| `weak-password-recovery` | 4 | 4 |
| `session-fixation` | 1 | 1 |
| `account-enumeration` | 4 | 3 |
| `missing-authentication-rate-limit` | 6 | 2 |
| `insufficient-session-expiration` | 1 | 1 |
| `token-issuer-or-audience-not-validated` | 1 | 1 |
| `excessive-data-exposure` | 7 | 5 |
| `unsafe-reflection` | 1 | 1 |
| `cryptographic-key-reuse` | 1 | 1 |
| `cleartext-storage-of-sensitive-data` | 2 | 2 |
| `output-encoding-disabled` | 2 | 2 |
| `file-inclusion` | 1 | 1 |
| `client-side-enforced-security-check` | 2 | 1 |
| `leftover-debug-bypass` | 1 | 1 |
| `debug-mode-enabled` | 3 | 1 |
| `weak-content-security-policy` | 2 | 1 |
| `spoofable-client-ip-header` | 6 | 2 |
| `missing-security-event-logging` | 3 | 1 |
| `missing-input-validation` | 8 | 3 |
| `exposed-directory-listing` | 4 | 1 |
| `encoded-token-without-integrity` | 2 | 1 |
| `unverified-password-change` | 1 | 1 |
| `validation-before-canonicalization` | 1 | 1 |
| `jsonp-cross-origin-data-exposure` | 1 | 1 |
| `predictable-derived-credential` | 1 | 1 |
| `non-constant-time-secret-comparison` | 1 | 1 |
| `fail-open-verification-check` | 1 | 1 |
| `host-header-in-generated-link` | 1 | 1 |

## Effect on published scores

None on CAI scores. On the benchmark: no published key uses these concepts. Each scanner mapping gains an `unmapped`
entry per concept it has no rule for (contract 1.4), so a key that uses one records the miss instead of being
unscorable.

## Alternatives considered

- **Key the defects under the nearest existing concept** (an ownership defect as `missing-authorization`). Rejected:
  that concept's definition excludes it, so a scanner would be credited for the wrong finding and a correct one could
  score as noise.
- **Keep them out of the standard and score them privately.** Rejected: a known-defect floor that only one producer
  can read is not a benchmark measurement, and the taxonomy is meant to name what a scanner should find.

## Questions for the decider

1. Should the 19 annex concepts that are rare in the corpus (one or two must-fires) wait for more
   evidence, or enter now so keys can use them?
2. Should `cleartext-storage-of-sensitive-data` become a parent of `insufficient-password-hashing`'s plaintext case,
   or stay a sibling?

## Annex: the definitions (verbatim from the curator's NEW-CONCEPTS.md, 2026-11-05)

## `client-controlled-identity`
- Title: Identity or role taken from an unverified client value
- CWE: CWE-565
- OWASP Top 10 2025: A08:2025 Software or Data Integrity Failures
- CAI taxonomy: no

The server decides WHO the caller is, or WHAT role it has, from a value the client sends and the server never verifies:
an unsigned cookie holding a user id or an admin flag, a header, a hidden form field, a query parameter or a body
field naming the acting user. Anyone can set the value, so anyone can be anyone. It differs from
`missing-authorization` (there IS an identity check, but its input is forgeable) and from `missing-ownership-check`
(there the identity is genuine and the RECORD is not scoped to it). A signed or server-side session value is not this
concept.

## `missing-ownership-check`
- Title: Missing object-level authorization (insecure direct object reference)
- CWE: CWE-639
- OWASP Top 10 2025: A01:2025 Broken Access Control
- CAI taxonomy: no

An authenticated caller reads, changes or deletes a record named by an identifier the caller supplies (a path id, a
query or body field), and nothing scopes the record to the caller: the lookup is by id alone and no comparison of the
record's owner with the caller's genuine identity decides the response. The caller is signed in — that is what
separates it from `missing-authorization` — but may act on everyone's records. The site is the lookup or write that
uses the caller's id unscoped.

## `missing-function-level-authorization`
- Title: Role check missing on a privileged function
- CWE: CWE-285
- OWASP Top 10 2025: A01:2025 Broken Access Control
- CAI taxonomy: no

An endpoint that performs a privileged function (administration, other users' management, configuration, reporting
over all accounts) checks that the caller is signed in, or nothing beyond a hidden link, but not that the caller holds
the role the function needs, so any ordinary user who calls it directly succeeds. Hiding the link in the UI is not a
check.

## `cross-site-request-forgery`
- Title: State-changing request without anti-forgery protection
- CWE: CWE-352
- OWASP Top 10 2025: A01:2025 Broken Access Control
- CAI taxonomy: no

A request that changes state is authenticated only by credentials the browser attaches on its own (a session cookie,
basic auth) and the server checks no anti-forgery token, origin or equivalent, so another site can make the victim's
browser send it. The site is the handler, or the framework set-up that disables or omits the protection
(`site: file` where there is no single line).

## `unrestricted-file-upload`
- Title: Unrestricted upload of a dangerous file
- CWE: CWE-434
- OWASP Top 10 2025: A06:2025 Insecure Design
- CAI taxonomy: no

An upload handler stores (and usually serves) a caller-supplied file without the checks its purpose needs — of type
by content, of extension against an allowlist, of size — or with a check the caller controls (a client-sent
content type, a check only in the browser), so executable or active content lands where it runs or is served. The site
is the handler's accept/store statement.

## `weak-password-requirements`
- Title: Weak password requirements
- CWE: CWE-521
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

Registration or password change accepts passwords with no or trivial strength rules (any length, empty, a single
character class), so accounts end up with guessable passwords. The site is the validation that is missing or too
weak.

## `weak-password-recovery`
- Title: Weak password recovery mechanism
- CWE: CWE-640
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

A forgotten-password mechanism lets someone who is not the account holder reset the password: a security answer that
is public or guessable, a reset token derived from data the attacker knows (the e-mail, a timestamp), a token not bound
to the account or never expiring, or a reset that trusts a caller-supplied account name without proof.

## `session-fixation`
- Title: Session identifier not renewed at login
- CWE: CWE-384
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

After a successful login the application keeps the session identifier the browser arrived with, instead of issuing a
new one, so an attacker who planted that identifier before login holds the authenticated session afterwards.

## `account-enumeration`
- Title: Response reveals whether an account exists
- CWE: CWE-204
- OWASP Top 10 2025: A07:2025 Authentication Failures (CWE-204 is not in the 2025 mapped lists; it is the authentication-response face of CWE-203/CWE-200)
- CAI taxonomy: no

A login, registration or password-recovery endpoint answers differently (a different message, status code or
observable behaviour) depending on whether the supplied user name or e-mail exists, so a caller can list valid
accounts before guessing passwords.

## `missing-authentication-rate-limit`
- Title: No limit on authentication attempts
- CWE: CWE-307
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

A login, password-reset, one-time-code or similar credential check can be tried without bound: no attempt counter,
lockout, delay that grows, or throttle keyed to the account or the client, so passwords and codes can be guessed
automatically. A fixed short sleep or a CSRF token is not a limit. A missing limit has no line of its own; the site is
the credential check that runs unthrottled.

## `insufficient-session-expiration`
- Title: Session without expiry
- CWE: CWE-613
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

A session or authentication token is issued with no idle or absolute lifetime (no maxAge / expires, no server-side
expiry), so it stays valid until an explicit logout, and one captured once, or left open on a shared machine, stays
usable indefinitely. The site is the session or token configuration that omits it.

## `token-issuer-or-audience-not-validated`
- Title: Security token issuer or audience not validated
- CWE: CWE-345
- OWASP Top 10 2025: A08:2025 Software or Data Integrity Failures
- CAI taxonomy: no

Bearer-token validation accepts a correctly signed token without checking that its issuer is the expected authority
or that its audience is this service (for example `ValidateIssuer = false`, `ValidateAudience = false`), so a token
minted for another relying party, or by another issuer sharing the key, is accepted. The signature and the lifetime
are `token-signature-or-expiry-not-validated`.

## `excessive-data-exposure`
- Title: Response returns properties the caller must not receive
- CWE: CWE-213
- OWASP Top 10 2025: A01:2025 Broken Access Control (CWE-213 is not in the 2025 mapped lists; its parent CWE-200 is)
- CAI taxonomy: no

A response serialises a stored object, or a field of it, without a server-side step that restricts it to what this
caller may receive, so credentials, password hashes, tokens, role flags, full identity or payment numbers, or other
users' private attributes leave the server. The endpoint may correctly decide that the caller may see the object;
the defect is that nobody chose its properties (OWASP API3:2023, read side). Hiding the fields later in client-side
code does not restrict them.

## `unsafe-reflection`
- Title: Request value selects the class or method the server runs
- CWE: CWE-470
- OWASP Top 10 2025: A05:2025 Injection
- CAI taxonomy: no

A request value is resolved into a class, constant or method (`constantize`, `const_get`, `send`, `Class.forName`,
`Type.GetType`, `getattr`) and then instantiated or invoked without an allowlist, so the caller chooses which code the
server constructs or runs. Deserialisation that picks a type from the payload is `insecure-deserialization`.

## `cryptographic-key-reuse`
- Title: One key and IV for security tokens and user-chosen plaintexts
- CWE: CWE-323
- OWASP Top 10 2025: A04:2025 Cryptographic Failures
- CAI taxonomy: no

The same symmetric key, with a fixed or repeated IV or nonce, is used both to mint a security value (an
authentication token, a signature) and to encrypt values a user can choose and read back, so the user obtains the
ciphertext of any plaintext — including a valid token for another identity.

## `cleartext-storage-of-sensitive-data`
- Title: Sensitive field stored in plaintext
- CWE: CWE-312
- OWASP Top 10 2025: A06:2025 Insecure Design
- CAI taxonomy: no

A field holding high-sensitivity data (a national identity number, payment card, bank account) or a secret that
proves identity on its own (a TOTP seed, a recovery code, an API secret) is written to the store in plaintext, not
encrypted under a key held outside that store, so read access to the store or its backups is enough to read it or
impersonate the account. The repository-wide posture "no encryption of stored personal data anywhere" is the suite
concept `data-encryption-controls`; this concept is one named field the documentation says is left unprotected.
Passwords are `insufficient-password-hashing`.

## `output-encoding-disabled`
- Title: Framework output encoding switched off
- CWE: CWE-116
- OWASP Top 10 2025: A05:2025 Injection
- CAI taxonomy: no

A template engine's or serializer's automatic output encoding is turned off for the whole application (template
autoescape set to false, HTML-entity escaping in JSON disabled), so every output the framework would have encoded is
written raw. The site is the setting. Each sink that then writes untrusted text into markup is
`cross-site-scripting` on its own.

## `file-inclusion`
- Title: Request-controlled file included as code
- CWE: CWE-98
- OWASP Top 10 2025: A05:2025 Injection
- CAI taxonomy: no

A path or URL the request controls is passed to a construct that loads and EXECUTES a file as program code (PHP
`include`/`require` and their `_once` forms, or equivalents) without restricting it to a fixed set of known files. A
local name executes or discloses any file the process can read, including uploads and logs the attacker wrote; a URL,
where the runtime allows it, executes remote code. Unlike `path-traversal`, the consequence is execution, and a name
without traversal sequences can be enough.

## `client-side-enforced-security-check`
- Title: Server trusts the client that a security step was passed
- CWE: CWE-602
- OWASP Top 10 2025: A06:2025 Insecure Design
- CAI taxonomy: no

A server-side handler decides that a prerequisite security step (a CAPTCHA, a re-authentication, a confirmation, an
earlier wizard step) has been completed from a value the client sends — a step number, a "passed" flag — instead of
from state the server recorded when the step actually succeeded, so the client sends that value and skips the step.

## `leftover-debug-bypass`
- Title: Development bypass of a security check left in the shipped path
- CWE: CWE-489
- OWASP Top 10 2025: A02:2025 Security Misconfiguration
- CAI taxonomy: no

Code written for development or testing remains in the shipped path and lets a request skip a security check when it
carries fixed literal values (a magic parameter value, a specific User-Agent or header), so anyone who learns the
literals passes the check.

## `debug-mode-enabled`
- Title: Framework debug mode on in the served application
- CWE: CWE-489
- OWASP Top 10 2025: A02:2025 Security Misconfiguration
- CAI taxonomy: no

The application turns on its framework's debug mode (Flask/Werkzeug `debug=True`, `app.debug`, Django `DEBUG = True`,
a developer exception page) in code or configuration the shipped entry point uses, so error responses carry stack
traces, settings and locals and, with Werkzeug, an interactive console that runs code. The site is the statement
that enables it; a value read from the environment with a false default is not this concept.

## `weak-content-security-policy`
- Title: Content-Security-Policy that does not restrict scripts
- CWE: CWE-693
- OWASP Top 10 2025: A06:2025 Insecure Design
- CAI taxonomy: no

The application sends a Content-Security-Policy whose script directives do not restrict which scripts run: they
allowlist hosts that serve arbitrary user-supplied content (public CDNs for npm/GitHub files, paste sites), allow
`'unsafe-inline'` or `'unsafe-eval'`, or use a nonce that is constant rather than fresh per response, so an injected
script runs although a policy is present. A missing policy is `security-response-headers`.

## `spoofable-client-ip-header`
- Title: Client IP taken from a header the client sets
- CWE: CWE-348
- OWASP Top 10 2025: A09:2025 Security Logging & Alerting Failures (CWE-348 is not in the 2025 mapped lists; chosen for the documented log-forgery use)
- CAI taxonomy: no

The program takes the client's IP address — for logging, rate limiting or an access decision — from a request header
the client can set (`X-Forwarded-For`, `X-Real-IP`, `Client-IP`) without first establishing that the request came
through a trusted proxy that sets it, so the client chooses the address recorded or checked.

## `missing-security-event-logging`
- Title: Security-relevant action with no audit record
- CWE: CWE-778
- OWASP Top 10 2025: A09:2025 Security Logging & Alerting Failures
- CAI taxonomy: no

A handler performs a security-relevant action — a successful or failed sign-in, a password or role change, a lockout
— and completes it without writing any audit record, so the event can be neither detected nor reconstructed. The site
is the branch with no logging call. Logging that exists but is incomplete is this concept only where the
documentation names the missing event.

## `missing-input-validation`
- Title: Business value accepted without the check its rule needs
- CWE: CWE-20
- OWASP Top 10 2025: A05:2025 Injection
- CAI taxonomy: no

A business value from the request — a quantity, price, rating, coupon, date, balance or step — is used without the
range or consistency check the business rule needs (negative quantities, a zero-star rating the UI forbids, an expired
coupon), so the caller obtains an outcome the rule excludes. Used in K only where the application's documentation
names the missing check; it is never a catch-all for "input not validated".

## `exposed-directory-listing`
- Title: Browsable listing of a non-public directory
- CWE: CWE-548
- OWASP Top 10 2025: A01:2025 Broken Access Control
- CAI taxonomy: no

The web server publishes a browsable index — and usually per-file download — of a directory whose contents are not
meant for the public (backups, logs, keys, deployment or configuration files), so anyone can enumerate and fetch them
without guessing names. The site is the route or static-serving registration that turns the listing on.

## `encoded-token-without-integrity`
- Title: Server-issued token trusted by decoding, without a MAC or signature
- CWE: CWE-345
- OWASP Top 10 2025: A08:2025 Software or Data Integrity Failures
- CAI taxonomy: no

The server decides that a token it issued (a coupon, a progress or continue code, a ticket) is genuine by decoding a
reversible encoding (Base64, Base85, hashids with a public salt) and checking its format, with no MAC or signature, so
anyone who learns the encoding mints valid tokens with chosen contents. A JWT is `token-signature-or-expiry-not-validated`.

## `unverified-password-change`
- Title: Password change without the current password
- CWE: CWE-620
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

An endpoint that sets a new password for the signed-in account accepts the request without verifying the current
password (or verifies it only when the caller chooses to send it), so a stolen session or a forged request takes the
account over permanently.

## `validation-before-canonicalization`
- Title: Security check on the raw value, use of a transformed one
- CWE: CWE-180
- OWASP Top 10 2025: A05:2025 Injection (CWE-180 is not in the 2025 mapped lists; placed by its parent CWE-20)
- CAI taxonomy: no

A security check (an extension allowlist, a path or name filter) runs on the raw input, and the program then
transforms the value (cuts it at a null byte, decodes, normalises) before using it, so a value that passes the check
is used in a different, forbidden form. The site is the check together with the later transformation.

## `jsonp-cross-origin-data-exposure`
- Title: Caller's data served as JSONP
- CWE: CWE-942
- OWASP Top 10 2025: A02:2025 Security Misconfiguration
- CAI taxonomy: no

An endpoint that answers with the data of the caller identified by ambient credentials (a cookie) wraps the response in
a caller-named callback (JSONP), so any third-party page can load it as a script and read the data across origins.

## `predictable-derived-credential`
- Title: Credential derived from public account attributes
- CWE: CWE-1391
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

The program sets or presents an account's password (or another credential) as a deterministic function of public
attributes such as the e-mail address or user name (reversed, encoded, hashed without a secret), so anyone who knows
the attribute and the function authenticates as the account.

## `non-constant-time-secret-comparison`
- Title: Secret compared in time that depends on the match
- CWE: CWE-208
- OWASP Top 10 2025: A04:2025 Cryptographic Failures (CWE-208 is not in the 2025 mapped lists; chosen for the MAC and token verification it breaks)
- CAI taxonomy: no

A secret value (a MAC tag, signature, token, password verifier) is compared with caller-supplied input by a routine
whose running time depends on how much of the input matches — a byte loop that returns at the first difference, a
plain string equality — so repeated timing measurements reveal the secret piece by piece. A comparison made for
authentication values (`MessageDigest.isEqual`, `CryptographicOperations.FixedTimeEquals`, `crypto.timingSafeEqual`,
`hmac.compare_digest`) is not this concept.

## `fail-open-verification-check`
- Title: Verification that succeeds when its inputs are absent
- CWE: CWE-287
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

An authentication or account-verification routine checks each expected credential or answer only if the request
carries it, and reports success when none of the checks ran, so a caller who removes or renames the fields is
verified without knowing any secret. The defect is the default-allow shape of the check, not the strength of the
secret.

## `host-header-in-generated-link`
- Title: Secret-bearing link built from the request's Host header
- CWE: CWE-640
- OWASP Top 10 2025: A07:2025 Authentication Failures
- CAI taxonomy: no

An absolute URL that carries a secret — a password-reset or account-confirmation link sent by e-mail — is composed
from the incoming request's `Host` (or `X-Forwarded-Host`) header instead of a configured origin, so an attacker who
requests the link for a victim with a forged host receives the secret when the victim clicks. It is one specific
`weak-password-recovery` mechanism, kept apart because its site and its repair (a configured origin) are its own.

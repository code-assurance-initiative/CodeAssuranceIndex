# AXR1: Runtime accessibility (rendered)

**Lens:** accessibility · **Scored:** yes in the standard (the catalog does not declare it advisory); the one existing engine reports it advisory, so no published score includes it

## Property

Web Content Accessibility Guidelines (WCAG) success criteria checked on the page as a browser renders it, including those only a rendered page can show (for example contrast as displayed, focus order, and state that exists only once the page runs). A result counts only when it is reproducible: the same page rendered again gives the same result. Not a WCAG conformance claim: automated checks on a rendered page cover only part of the success criteria. Advisory: failures are reported as findings, never counted in any CAI score, and never change the static accessibility dimensions (AC1-AC6, which read the source without running it).

The catalog does not declare this dimension advisory, but its definition above calls it advisory, and no score curve exists. The question is open; see Status under Pending re-definition. Until a decision settles it, no CAI score includes this dimension.

## Applies to

The repository's web user interface as a browser renders and runs it: HTML pages with their styles (CSS) and scripts. Any language or framework can produce these pages, on the server or in the browser.

## Population

- **In scope:** pages that the repository's own product serves to its users once it runs, whatever files produce them, including server-side templates and built or bundled scripts. A page is one address that returns an HTML document. A page includes any state it reaches by itself once its scripts run.
- **Out of scope:** pages that exist only for tests, documentation sites, component showcases, and the authors' examples, samples and demonstrations. [production-code.md](../production-code.md) defines what these categories mean. This dimension does not use that page's file-level exclusions to leave out pages.
- **Not pages:** responses that are not pages (for example an API answering with JSON), and user interfaces that are not web pages (for example native desktop or mobile apps).
- No more than 200 pages are rendered (see Pending re-definition).

## Unit and denominator

- **Check:** one automated accessibility check applied to one element of a rendered page. Each check belongs to one or more WCAG success criteria. Its outcome is pass, violation, or needs human review.
- **Reproducible:** a check on an element is reproducible when it appears with the same outcome in every render of the page. Each page is rendered 2 times. A check that appears in only one render, or whose outcome changes between renders, is not reproducible. With fewer than 2 renders, nothing is reproducible.
- **Verdict for one page and one criterion:** the first of these that holds:
  1. **Fail:** at least one reproducible violation.
  2. **Unstable:** at least one check that is not reproducible.
  3. **Needs review:** at least one check that needs human review.
  4. **Pass:** at least one reproducible pass.
  5. **Not assessed:** none of the above.
- **Verdict for one criterion across the repository:** the first of these that holds: Fail if any page fails; Unstable if any page is unstable; Needs review if any page needs review; Pass if any page passes; otherwise Not assessed.
- **Counted:** the success criteria whose repository verdict is Fail or Pass. Only these count as evidence. A criterion that is Unstable or Needs review is never cited as evidence.
- **Denominator:** none. No ratio is formed. The result reports the number of success criteria whose repository verdict is Fail or Pass, the number of pages rendered, and the unstable share.
- **Unstable share:** the (page, criterion) pairs with an Unstable verdict, divided by the (page, criterion) pairs with any verdict other than Not assessed. It is reported, not scored.
- **Finding:** one finding for each success criterion whose verdict is Fail. It names the criterion, the checks that failed and the elements they failed on.

## Thresholds

- A check counts only when it is reproducible across both renders of the page.
- One reproducible violation on any rendered page makes that criterion Fail.
- A criterion is Pass only when no page that assessed it has a violation, an unstable check or a check that needs review.

## Score

No score curve is defined: the one existing engine reports findings only, with no 0-10 reading.

## Not measured when

- **No user interface:** the repository has no web user interface of its own product to render (for example a library, a command-line tool or a service that serves only data). The dimension does not apply.
- **The interface could not be rendered:** the product did not build or start, it started but served no page, or no page could be rendered. The result is marked not measured, with the reason, and is never reported as clean.

Rendered pages where no criterion reached Fail or Pass still count as measured. That result reports 0 criteria with evidence, the number of pages rendered and the unstable share.

## What the result is not

- It is not a WCAG conformance claim. Automated checks cover only part of the success criteria. A Pass means only that the automated checks found no violation.
- It says nothing about pages that were not reached or rendered, or about pages beyond the cap.
- An Unstable verdict is not a finding about the product. It means the result could not be reproduced.
- It does not change the static accessibility dimensions (AC1-AC6), and they do not change it.

## Pending re-definition

These are parameters of the existing reading's model, and today's results depend on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Renders per page:** 2. A check is reproducible only when it is the same in both.
- **Pages rendered:** no more than 200 per repository. Pages beyond the cap are not rendered.
- **Status:** the catalog does not declare it advisory, yet no score curve exists; a decision either declares it advisory or sets its curve.

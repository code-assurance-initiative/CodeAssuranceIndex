#!/usr/bin/env python3
"""The rubric gate on a pull request (decision 0002). Run from the head checkout:

    tools/rubric/check-rubric-pr.py <base-sha> <head-sha>      # PR body in the PR_BODY environment variable

Refuses, in order:
  1. a change to any file under an ALREADY PUBLISHED rubrics/<version>/ (published versions are immutable, ADR-0004);
  2. a new version whose catalog does not declare the directory it sits in;
  3. a new version identical to the newest published one apart from its name (name-only, decision 0002);
  4. a score-relevant new version whose pull request does not link the accepted decision that allows it
     (a link to docs/decisions/NNNN-*.md that exists in this checkout).
A descriptive new version passes, and says it needs a maintainer's review.

Writes a markdown report to GITHUB_STEP_SUMMARY when set. Exit 0 = pass, 1 = refused.
"""
import json
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
classify_mod = __import__("classify-rubric-change")


def git(*args):
    return subprocess.run(["git", *args], capture_output=True, text=True, check=True).stdout


def version_key(name):
    return [int(x) for x in name.removeprefix("rubric-").split(".")]


def main(base, head):
    report, failures = [], []
    changed = [l.split("\t") for l in git("diff", "--name-status", "--no-renames", base, head, "--", "rubrics").splitlines() if l]
    base_versions = {p.split("/")[1] for p in git("ls-tree", "-r", "--name-only", base, "rubrics").splitlines()
                     if p.count("/") >= 2 and p.split("/")[1].startswith("rubric-")}

    new_versions = set()
    for status, path in changed:
        parts = path.split("/")
        if len(parts) < 3 or not parts[1].startswith("rubric-"):
            continue
        version = parts[1]
        if version in base_versions:
            failures.append(f"`{path}` is under the published version `{version}` ({status}). Published versions "
                            "are immutable (ADR-0004); a change is a new version.")
        else:
            new_versions.add(version)

    published = sorted(base_versions, key=version_key)
    body = os.environ.get("PR_BODY", "")
    linked = [m for m in re.findall(r"docs/decisions/(\d{4}-[\w-]+\.md)", body)
              if os.path.isfile(f"docs/decisions/{m}")]

    for version in sorted(new_versions, key=version_key):
        path = f"rubrics/{version}/rubric-catalog.json"
        if not os.path.isfile(path):
            failures.append(f"`{version}` adds files but no `rubric-catalog.json`.")
            continue
        new = json.load(open(path, encoding="utf-8"))
        if new.get("rubricVersion") != version:
            failures.append(f"`{path}` declares `{new.get('rubricVersion')}`; a catalog must declare the directory it is in.")
            continue
        older = [v for v in published if version_key(v) < version_key(version)]
        if not older:
            report.append(f"`{version}`: no earlier published version to compare with.")
            continue
        prev = older[-1]
        prev_doc = json.loads(git("show", f"{base}:rubrics/{prev}/rubric-catalog.json"))
        cls, sub, reasons = classify_mod.classify(prev_doc, new)
        label = f"{cls}/{sub}" if sub else cls
        report.append(f"`{version}` against `{prev}`: **{label}**. " + "; ".join(reasons))
        if cls == "name-only":
            failures.append(f"`{version}` is identical to `{prev}` apart from its name. That is not a new version of "
                            "the standard (decision 0002); a change in how an engine measures belongs in the "
                            "engine's producer identity.")
        elif cls == "score-relevant" and not linked:
            failures.append(f"`{version}` is score-relevant and the pull request links no accepted decision. Link "
                            "the decision record (`docs/decisions/NNNN-*.md`) that allows it (decision 0002).")
        elif cls == "score-relevant":
            report.append(f"Allowed by: {', '.join('docs/decisions/' + l for l in linked)}. A maintainer confirms "
                          "the decision covers exactly this change.")
        else:
            report.append("Descriptive: needs a maintainer's review, no proposal.")

    if not changed:
        report.append("No change under `rubrics/`.")
    out = ["## Rubric gate (decision 0002)", ""] + [f"- {r}" for r in report]
    if failures:
        out += ["", "### Refused", ""] + [f"- {f}" for f in failures]
    text = "\n".join(out) + "\n"
    print(text)
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as f:
            f.write(text)
    return 1 if failures else 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    sys.exit(main(sys.argv[1], sys.argv[2]))

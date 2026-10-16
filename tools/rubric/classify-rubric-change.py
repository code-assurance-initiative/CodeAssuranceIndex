#!/usr/bin/env python3
"""Classify a rubric catalog against the version before it, by what a conforming scorer READS.

    tools/rubric/classify-rubric-change.py rubrics/rubric-2026.10.4 rubrics/rubric-2026.10.5
    tools/rubric/classify-rubric-change.py --history [rubrics]

Three classes, decided only from the two documents:

  score-relevant  something Cai.Scoring reads differs: the `scoring` block, the `lenses`,
                  `rejectUnknownContributors`, the set of dimension ids, or a dimension's
                  lens / category / advisory / family. Further split into
                    moves-numbers    a scored (non-advisory) dimension is added, removed or re-filed,
                                     or the scoring block or lenses change
                    moves-no-number  only advisory dimensions are added or removed
  descriptive     only prose differs (name, whatItMeasures, evaluator, ceilingRung, scoringPolarity, ...)
  name-only       the documents are identical apart from `rubricVersion`

The field list mirrors what src/Cai.Scoring reads today (CaiScorer, RubricCatalog). If the scorer starts
reading another field, add it to SCORED_DIMENSION_FIELDS or this tool under-reports.

Exit code: 0 always for --history; for a pair, 0 = descriptive, 10 = score-relevant, 20 = name-only.
"""
import json
import os
import sys

SCORED_DIMENSION_FIELDS = ("lens", "category", "advisory", "family")
SCORED_TOP_LEVEL = ("scoring", "lenses", "rejectUnknownContributors")


def load(path):
    if os.path.isdir(path):
        path = os.path.join(path, "rubric-catalog.json")
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def classify(old, new):
    a = {d["id"]: d for d in old.get("dimensions", [])}
    b = {d["id"]: d for d in new.get("dimensions", [])}
    reasons, moves = [], False

    for key in SCORED_TOP_LEVEL:
        if old.get(key) != new.get(key):
            reasons.append(f"{key} changed")
            moves = moves or key != "rejectUnknownContributors"

    added = sorted(set(b) - set(a))
    removed = sorted(set(a) - set(b))
    added_scored = [i for i in added if not b[i].get("advisory")]
    removed_scored = [i for i in removed if not a[i].get("advisory")]
    refiled = [i for i in sorted(set(a) & set(b))
               if any(a[i].get(f) != b[i].get(f) for f in SCORED_DIMENSION_FIELDS)]
    if added:
        reasons.append(f"+{len(added)} dimension(s): {', '.join(added)}")
    if removed:
        reasons.append(f"-{len(removed)} dimension(s): {', '.join(removed)}")
    if refiled:
        reasons.append(f"{len(refiled)} dimension(s) re-filed (lens/category/advisory/family)")
    moves = moves or bool(added_scored or removed_scored or refiled)

    if reasons:
        return ("score-relevant", "moves-numbers" if moves else "moves-no-number", reasons)

    prose = [i for i in sorted(set(a) & set(b)) if a[i] != b[i]]
    rest_old = {k: v for k, v in old.items() if k not in ("rubricVersion", "dimensions")}
    rest_new = {k: v for k, v in new.items() if k not in ("rubricVersion", "dimensions")}
    if prose or rest_old != rest_new:
        return ("descriptive", None, [f"prose changed on {len(prose)} dimension(s)"] if prose else ["top-level prose changed"])
    return ("name-only", None, ["identical apart from rubricVersion"])


def version_key(name):
    return [int(x) for x in name.removeprefix("rubric-").split(".")]


def history(root):
    versions = sorted(
        (d for d in os.listdir(root)
         if d.startswith("rubric-") and os.path.isfile(os.path.join(root, d, "rubric-catalog.json"))),
        key=version_key)
    counts = {}
    for prev, cur in zip(versions, versions[1:]):
        cls, sub, reasons = classify(load(os.path.join(root, prev)), load(os.path.join(root, cur)))
        label = f"{cls}/{sub}" if sub else cls
        counts[label] = counts.get(label, 0) + 1
        print(f"{cur:20} {label:32} {'; '.join(reasons)}")
    print()
    for label, n in sorted(counts.items()):
        print(f"{n:4}  {label}")
    print(f"{len(versions) - 1:4}  versions compared with their predecessor ({versions[1]} .. {versions[-1]})")


def main(argv):
    if argv and argv[0] == "--history":
        root = argv[1] if len(argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..", "rubrics")
        history(root)
        return 0
    if len(argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    cls, sub, reasons = classify(load(argv[0]), load(argv[1]))
    print(f"{cls}{'/' + sub if sub else ''}")
    for r in reasons:
        print(f"  {r}")
    return {"descriptive": 0, "score-relevant": 10, "name-only": 20}[cls]


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

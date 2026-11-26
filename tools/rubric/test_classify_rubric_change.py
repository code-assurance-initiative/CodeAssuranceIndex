#!/usr/bin/env python3
"""Tests for classify-rubric-change.py: what a conforming scorer reads must be classified as score-relevant."""
import importlib.util
import os
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("classify", os.path.join(HERE, "classify-rubric-change.py"))
classify = importlib.util.module_from_spec(spec)
spec.loader.exec_module(classify)


def catalog(**top):
    doc = {"rubricVersion": "rubric-x", "lenses": [], "dimensions": [{"id": "D1", "lens": "codeHealth", "whatItMeasures": "a"}]}
    doc.update(top)
    return doc


class ClassifyTests(unittest.TestCase):
    def test_a_production_line_minimum_moves_numbers(self):
        # Decision 0024 §5.4: a repository under the minimum gets no CAI at all.
        cls, sub, reasons = classify.classify(catalog(), catalog(minProductionLineShare=0.95))
        self.assertEqual(("score-relevant", "moves-numbers"), (cls, sub))
        self.assertIn("minProductionLineShare changed", reasons)

    def test_prose_only_is_descriptive(self):
        new = catalog()
        new["dimensions"][0]["whatItMeasures"] = "b"
        cls, _, _ = classify.classify(catalog(), new)
        self.assertEqual("descriptive", cls)

    def test_the_published_11_6_is_score_relevant(self):
        root = os.path.join(HERE, "..", "..", "rubrics")
        old, new = os.path.join(root, "rubric-2026.11.5"), os.path.join(root, "rubric-2026.11.6")
        if not (os.path.isdir(old) and os.path.isdir(new)):
            self.skipTest("archive not present")
        cls, sub, _ = classify.classify(classify.load(old), classify.load(new))
        self.assertEqual(("score-relevant", "moves-numbers"), (cls, sub))


if __name__ == "__main__":
    unittest.main()

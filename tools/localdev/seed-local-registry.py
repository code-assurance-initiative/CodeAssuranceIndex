#!/usr/bin/env python3
"""Seed a LOCAL CAI registry with published subjects, for the end-to-end render.

★★ THIS EXISTS BECAUSE "THE TESTS ARE GREEN" SAYS NOTHING ABOUT WHAT A READER GETS. The standard's
pages are composed here and rendered by imprint; only a real browser over really-published files
shows whether the node trees the builders emit become pages.

★★ THE DELIVERIES ARE FIXTURES, NOT ARTIFACTS. They carry no real signature — the sweep reads
`package_json` and never verifies it, and the schema/ingest gate is covered by its own test
(`DeliveryCarriesThePageFactsTests`). Writing them straight into the store keeps this harness to the
one question it is for: does what the standard composes RENDER.

★ MINOR 1.1 THROUGHOUT, deliberately: the whole point is to render the pages that only exist when a
delivery carries languages, an origin and a security reading.
"""

import argparse
import json
import random
import sqlite3
import sys
from datetime import datetime, timedelta, timezone

LANGUAGES = [
    ("csharp", ["typescript"]), ("csharp", []), ("go", []), ("go", ["shell"]),
    ("typescript", ["css"]), ("python", []), ("rust", []), ("java", ["kotlin"]),
]
COUNTRIES = ["Denmark", "Denmark", "Denmark", "Germany", "Germany", "United States", None]
ADVISORIES = ["GHSA-aaaa-bbbb-cccc", "CVE-2026-0001", "CVE-2026-0002"]
PACKAGES = ["left-pad", "Acme.Widgets", "google.golang.org/grpc"]


# The two subjects whose delivery count is deliberately short of the rest.
ONCE_OWNER = "acme-once"
TWICE_OWNER = "acme-twice"


def readings_for(ordinal, requested):
    """How many deliveries this subject gets — one, two, or the full run."""
    if ordinal == 0:
        return 1
    if ordinal == 1:
        return min(2, requested)
    return requested


def payload(ordinal, at, rng):
    language, secondary = LANGUAGES[ordinal % len(LANGUAGES)]
    country = COUNTRIES[ordinal % len(COUNTRIES)]
    # ★★ THE FIRST TWO SUBJECTS ARE NAMED, BECAUSE THEY ARE THE STATES NOTHING RENDERED. Every
    #    seeded subject used to get the same three deliveries, so the harness only ever drew a
    #    three-point trend — while the most common portrait in production is a repository measured
    #    ONCE (its first survey) and the next most common is one measured twice. The one-reading
    #    page is where "1 measurements over time" was published for months. Named owners so the
    #    screenshot runner can ask for them by path rather than guess which ordinal is which.
    owner = ONCE_OWNER if ordinal == 0 else TWICE_OWNER if ordinal == 1 else f"acme-{ordinal % 9}"
    repository = f"{owner}/service-{ordinal}"
    cai = round(35 + rng.random() * 55, 1)
    band = "Exemplary" if cai >= 90 else "Strong" if cai >= 70 else "Adequate" if cai >= 50 else "Weak"
    affected = ordinal % 3 == 0
    scanned = at.isoformat().replace("+00:00", "Z")

    subject = {"repository": repository, "commit": "3f9a1c2", "host": "github.com",
               "languages": {"primary": language, "secondary": secondary}}
    if country is not None:
        subject["origin"] = {"country": country, "declared": True, "resolvedBy": "countryName"}
    else:
        subject["origin"] = {"declared": False, "unresolvedReason": "blank"}

    reading = {
        "vulnMeasurable": ordinal % 7 != 0,
        "vulnScanFailed": ordinal % 11 == 0,
        "vulnAffected": affected,
        "vulnHighOrCritical": affected and ordinal % 6 == 0,
        "vulnCritical": affected and ordinal % 12 == 0,
        "findings": 3 if affected else 0,
        "findingsCritical": 1 if affected and ordinal % 12 == 0 else 0,
        "findingsHigh": 1 if affected and ordinal % 6 == 0 else 0,
        "findingsMedium": 1 if affected else 0,
        "findingsLow": 1 if affected else 0,
        "disclosureMeasured": True,
        "disclosurePolicy": ordinal % 4 == 0,
        "disclosureContact": ordinal % 8 == 0,
        "secretsHistoryMeasured": True,
        "secretsHistoryAffected": ordinal % 9 == 0,
        "secretsHistoryFindings": 2 if ordinal % 9 == 0 else 0,
        "secretsCurrentMeasured": True,
        "secretsCurrentAffected": ordinal % 17 == 0,
        "supplyChainMeasurable": ordinal % 5 != 0,
        "sbom": ordinal % 6 == 0,
        "provenance": ordinal % 10 == 0,
        "signing": ordinal % 4 == 0,
        "pinnedActions": ordinal % 3 == 0,
        "advisoriesRead": True,
        "advisoryListComplete": ordinal % 13 != 0,
        "advisories": ([{"advisoryId": ADVISORIES[ordinal % len(ADVISORIES)],
                         "package": PACKAGES[ordinal % len(PACKAGES)],
                         "packageVersion": "1.3.0",
                         "inherited": ordinal % 2 == 0}] if affected else []),
    }

    return {
        "schemaVersion": "1.1",
        "deliveryId": f"cd_local_{ordinal}_{int(at.timestamp())}",
        "issuedAt": scanned,
        "issuer": {"name": "codeassuranceindex.info", "keyId": "local"},
        "producer": {"name": "watchdog.canine.dev", "scanner": "watchdog-surveyor",
                     "scannerVersion": "local"},
        "subject": subject,
        "rubricVersion": "rubric-2026.08.15",
        "measurement": {"measuredLoc": 12000, "productionLoc": 9000,
                        "analyzableProjects": 4, "scannedAt": scanned},
        "verdict": {"cai": cai, "band": band, "aggregate": cai, "coherenceNote": "",
                    "lenses": [{"lens": "codeHealth", "score": cai, "band": band, "weight": 0.5,
                                "contribution": cai / 2, "criticalGated": False,
                                "criticalContributors": [], "itemCount": 3},
                               {"lens": "maturity", "score": max(0.0, cai - 8), "band": band,
                                "weight": 0.5, "contribution": cai / 2, "criticalGated": False,
                                "criticalContributors": [], "itemCount": 2}],
                    "categories": []},
        "evidence": {"rubricVersion": "rubric-2026.08.15", "commit": "3f9a1c2",
                     "analyzableProjects": 4, "productionLoc": 9000, "headlineScore": cai,
                     "dimensions": [{"id": "D1", "category": "code-quality",
                                     "score": round(cai / 10, 2), "confidence": 0.95}],
                     "securityReading": reading},
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--db", required=True)
    parser.add_argument("--subjects", type=int, default=40)
    parser.add_argument("--readings", type=int, default=3,
                        help="deliveries per subject, so a portrait can draw a trend")
    parser.add_argument("--weeks", type=int, default=8,
                        help="backdated corpus readings, so the sheet's series has more than one day")
    args = parser.parse_args()

    rng = random.Random(20260923)
    now = datetime.now(timezone.utc).replace(microsecond=0)

    connection = sqlite3.connect(args.db)
    try:
        tables = {r[0] for r in connection.execute(
            "SELECT name FROM sqlite_master WHERE type='table'")}
        missing = {"deliveries", "publications", "corpus_readings"} - tables
        if missing:
            # ★ FAIL LOUDLY RATHER THAN CREATE. The schema belongs to the registry, and a table
            #   invented here would drift from it silently — then the harness would prove a render
            #   over a store the real one does not have.
            print(f"error: {args.db} has no {', '.join(sorted(missing))} table — boot Cai.Web "
                  f"against it once so the registry creates its own schema, then seed.",
                  file=sys.stderr)
            return 2

        for ordinal in range(args.subjects):
            readings = readings_for(ordinal, args.readings)
            for reading in range(readings):
                at = now - timedelta(days=(readings - reading) * 30)
                p = payload(ordinal, at, rng)
                connection.execute(
                    "INSERT OR REPLACE INTO deliveries (delivery_id, owner_org_id, repository, "
                    "commit_sha, host, producer, rubric_version, cai, band, issued_at, key_id, "
                    "canonical_sha256, signature_value, package_json, published_at) "
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    (p["deliveryId"], "org-local", p["subject"]["repository"], "3f9a1c2",
                     "github.com", "watchdog.canine.dev", p["rubricVersion"], p["verdict"]["cai"],
                     p["verdict"]["band"], p["issuedAt"], "local", "sha", "sig",
                     json.dumps({"payload": p, "signature": {"alg": "ed25519", "keyId": "local",
                                                             "canon": "RFC8785-json", "value": "local"}}),
                     p["issuedAt"]))

            connection.execute(
                "INSERT OR REPLACE INTO publications (owner_org_id, repository, status, granted_at, "
                "withdrawn_at) VALUES (?,?,?,?,NULL)",
                ("org-local", payload(ordinal, now, rng)["subject"]["repository"], "granted",
                 now.isoformat().replace("+00:00", "Z")))

        # ★★ BACKDATED CORPUS READINGS, BECAUSE §5 CANNOT BE RENDERED WITHOUT THEM. The series takes
        #    the latest reading of each DAY — so a re-run cannot put two marks on one date — which
        #    means every sweep a harness performs in one sitting collapses to a single point, and a
        #    trend over one point is correctly refused. Without these the newest code on the sheet
        #    would go unrendered while the run reported success, which is exactly the shape of gap
        #    this whole harness exists to close.
        #
        # ★ They are DATED READINGS, not a decoration: each is a real row of the append-only record
        #   the line is drawn through, and the medians drift so the chart has a shape to show.
        for week in range(args.weeks, 0, -1):
            at = (now - timedelta(days=7 * week)).isoformat().replace("+00:00", "Z")
            median = round(52 + (args.weeks - week) * 1.1, 1)
            codebases = max(1, args.subjects - week * 2)
            connection.execute(
                "INSERT OR REPLACE INTO corpus_readings (taken_at, codebases, median_cai, reading_json) "
                "VALUES (?,?,?,?)",
                (at, codebases, median, json.dumps({"codebases": codebases})))

        connection.commit()
        print(f"seeded {args.subjects} published subjects × up to {args.readings} readings "
              f"(one measured once, one twice), "
              f"and {args.weeks} backdated corpus readings, into {args.db}")
        return 0
    finally:
        connection.close()


if __name__ == "__main__":
    sys.exit(main())

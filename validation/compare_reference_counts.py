#!/usr/bin/env python3
"""Validate all 11 PreEI stage counts + update/full-rescan decisions.

Usage: python validation/compare_reference_counts.py ACTUAL_CSV REFERENCE_CSV
Values of runtime and memory are intentionally not compared.
"""
import csv
import sys
from pathlib import Path

REQUIRED = ("stage", "type", "erasable_itemsets")
EXPECTED_STAGES = 11

def load_rows(path):
    with Path(path).open("r", newline="", encoding="utf-8-sig") as stream:
        reader = csv.DictReader(stream)
        if not reader.fieldnames or not set(REQUIRED).issubset(reader.fieldnames):
            raise ValueError(f"{path}: missing required CSV columns {REQUIRED}")
        rows = list(reader)
    total_rows = [row for row in rows if row["stage"].strip().upper() == "TOTAL"]
    stage_rows = [row for row in rows if row["stage"].strip().upper() != "TOTAL"]
    if len(total_rows) > 1:
        raise ValueError(f"{path}: multiple TOTAL rows")
    if len(stage_rows) != EXPECTED_STAGES:
        raise ValueError(f"{path}: expected {EXPECTED_STAGES} stages, found {len(stage_rows)}")
    # The stage order must match the historical sequence; no silent truncation.
    for i, row in enumerate(stage_rows):
        if row["stage"].strip() != str(i):
            raise ValueError(f"{path}: expected stage {i}, got {row['stage']!r}")
    return stage_rows

def main():
    if len(sys.argv) != 3:
        print("Usage: compare_reference_counts.py ACTUAL_CSV REFERENCE_CSV", file=sys.stderr)
        return 2
    try:
        actual = load_rows(sys.argv[1])
        reference = load_rows(sys.argv[2])
    except (OSError, ValueError) as exc:
        print(f"FAIL: {exc}")
        return 1
    problems = []
    for stage, (observed, expected) in enumerate(zip(actual, reference)):
        for column in REQUIRED:
            if observed[column].strip() != expected[column].strip():
                problems.append(
                    f"stage {stage} {column}: expected {expected[column]!r}, "
                    f"observed {observed[column]!r}"
                )
    if problems:
        print("FAIL: historical reference mismatch (counts and/or re-scan decisions):")
        for problem in problems:
            print(" - " + problem)
        print("Ensure that the dataset has the ORIGINAL profit values and transaction order,")
        print("and the matching thresholds defined in validation/reference_profiles.json.")
        return 1
    print("PASS: all 11 erasable-itemset counts and update/full-rescan decisions match the reference.")
    print("Timing and working-set memory are not compared because environments differ.")
    return 0

if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Compare the itemset counts and re-scan decisions from a saved PreEI CSV."""
import csv
import sys
from pathlib import Path

if len(sys.argv) != 2:
    sys.exit('Usage: python compare_mushroom_counts.py path/to/PreEI_mushroom_results.csv')

reference_path = Path(__file__).with_name('mushroom_expected_itemset_counts.csv')
with reference_path.open(newline='', encoding='utf-8') as file:
    expected = list(csv.DictReader(file))
with Path(sys.argv[1]).open(newline='', encoding='utf-8-sig') as file:
    actual = list(csv.DictReader(file))[:len(expected)]

if len(actual) != len(expected):
    sys.exit(f'FAIL: expected {len(expected)} stages, found {len(actual)}')

problems = []
for i, (ref, observed) in enumerate(zip(expected, actual)):
    for field in ('stage', 'type', 'erasable_itemsets'):
        if ref[field] != observed.get(field):
            problems.append(f'stage {i}: {field} expected {ref[field]}, got {observed.get(field)}')
if problems:
    print('FAIL:')
    print('\n'.join(problems))
    sys.exit(1)
print('PASS: All 11 Mushroom itemset counts and re-scan decisions match the historical run.')

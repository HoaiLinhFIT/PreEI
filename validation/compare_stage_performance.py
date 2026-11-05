#!/usr/bin/env python3
"""Compare a PreEI Release CSV with a reference (no third-party packages).

The reference can be either the author's original Mushroom log extraction or
an earlier Release run. Counts/types are correctness checks. Elapsed seconds and
working-set MB are comparisons, not pass/fail assertions because they vary by run.
"""

import argparse
import csv
from pathlib import Path
import sys


def load_stages(path: Path):
    with path.open('r', encoding='utf-8-sig', newline='') as handle:
        rows = list(csv.DictReader(handle))
    required = {'stage', 'type', 'erasable_itemsets', 'runtime_seconds', 'working_set_mb'}
    if not rows or not required.issubset(rows[0]):
        raise ValueError(f'{path}: expected CSV headers: {", ".join(sorted(required))}')
    result = []
    for row in rows:
        if row['stage'].strip().upper() == 'TOTAL':
            continue
        if not row['stage'].strip():
            continue
        result.append({
            'stage': int(row['stage']),
            'type': row['type'].strip(),
            'erasable_itemsets': int(row['erasable_itemsets']),
            'runtime_seconds': float(row['runtime_seconds']),
            'working_set_mb': float(row['working_set_mb']),
        })
    if len(result) != 11 or sorted(r['stage'] for r in result) != list(range(11)):
        raise ValueError(f'{path}: expected stages 0 through 10 exactly once')
    return sorted(result, key=lambda x: x['stage'])


def pct(new: float, old: float) -> str:
    if old == 0:
        return 'N/A'
    return f'{((new / old) - 1) * 100:+.2f}%'


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('candidate', type=Path, help='New Release CSV exported from PreEI R9')
    parser.add_argument('--reference', type=Path, default=Path(__file__).with_name('mushroom_original_stage_reference.csv'),
                        help='Reference CSV (default: original Mushroom benchmark)')
    args = parser.parse_args()
    try:
        candidate = load_stages(args.candidate)
        reference = load_stages(args.reference)
    except (ValueError, OSError) as exc:
        print('ERROR:', exc, file=sys.stderr)
        return 2

    print(f'Candidate: {args.candidate}')
    print(f'Reference: {args.reference}')
    print('Stage | Operation          | Itemsets     | Reference(s) | Candidate(s) | Runtime delta | MB delta')
    print('-' * 104)
    mismatches = []
    for current, old in zip(candidate, reference):
        if current['type'] != old['type']:
            mismatches.append(f"stage {current['stage']}: type {current['type']} != {old['type']}")
        if current['erasable_itemsets'] != old['erasable_itemsets']:
            mismatches.append(f"stage {current['stage']}: itemsets {current['erasable_itemsets']} != {old['erasable_itemsets']}")
        mb_delta = current['working_set_mb'] - old['working_set_mb']
        print(f"{current['stage']:5} | {current['type']:<18} | {current['erasable_itemsets']:>12,} | "
              f"{old['runtime_seconds']:>12.6f} | {current['runtime_seconds']:>12.6f} | "
              f"{pct(current['runtime_seconds'], old['runtime_seconds']):>13} | {mb_delta:>+9.2f}")

    old_sum = sum(r['runtime_seconds'] for r in reference)
    new_sum = sum(r['runtime_seconds'] for r in candidate)
    print('-' * 104)
    print(f'TOTAL: reference {old_sum:.7f} s; candidate {new_sum:.7f} s; runtime change {pct(new_sum, old_sum)}')
    print('Memory delta is per-stage working-set difference (MB), NOT the overall peak.')
    if mismatches:
        print('FAIL: itemset counts or stage operations changed:')
        for mismatch in mismatches:
            print(' -', mismatch)
        return 1
    print('PASS: all 11 itemset counts and re-scan decisions match the reference.')
    print('Note: runtime and memory variation is expected across runs and machines.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

# R9 - concrete-list access in PreEI hot paths

## Requested changes

1. Keep descriptive academic names introduced in previous refactor (`PreEIMiningEngine`, `CountErasableItemsets`, `BuildTransactionDifference`, etc.).
2. Replace `IReadOnlyList<T>` by concrete `List<T>` **only** where the existing parameters/locals are already lists: enumeration of candidate itemsets, SaveResult prefix, difference-list counting/building, disjoint-range testing, and the two singleton-list construction/update loops.
4. **Keep** the original `result_EIs` `List<int>` and `SaveResult` representation; clear and trim after each stage has been measured. Preserve fixed 80% + 10 x 2% sequential splitting, all threshold formulas and re-scan decisions.
5. Prepare a Release benchmark, compare per-stage runtimes/counts/types against the original run and the prior R5 Release run.

## Explicitly excluded

- Do **not** reinstate separate `Mine()` and `MineSub()`. The single generalized recursive method `CountErasableItemsets()` remains.
- No change to candidate ordering, pruning, difference-list arithmetic, threshold/re-scan rules, memory sampling or Stopwatch boundaries.
- No change to the flat TreeView or CSV/TXT export.

## Source-change inventory

Only `PreEI/Mining/PreEIMiningEngine.cs` has functional source changes, limited to concrete-list declarations/parameter types and comments. `DataSplitter.cs`, `Program.cs`, the WinForms source/designer and the other mining model classes remain byte-for-byte identical to R8.

## Reference performance and validation

- Historical Mushroom original: 11 stages, 3.4005246 s total, overall process peak reported as 1177.56 MB.
- R5 Release Mushroom: 11 stages, 4.2030894 s total, total process peak from CSV 1473.171456 MB.
- Their itemset counts and re-scan decisions match 11/11 (stage 6 is full re-scan).
- `validation/compare_stage_performance.py` prints per-stage runtime change and memory differences and rejects changed counts or stage operations.
- **R9 runtime and correctness remain unmeasured** until the author builds and runs R9 with Visual Studio 2022, Release, Ctrl+F5, 1.05% and 1.2%.

We cannot attribute any measured speedup to the concrete-list changes without the R9 CSV. Wall-clock timing and process peak memory are sensitive to Windows/.NET/process state; the original paper values should not be rewritten based on this draft.

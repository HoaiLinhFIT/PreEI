# PreEI refactoring notes (draft)

## Scope

The target is code cleanliness while retaining the original `result_EIs` materialization, as well as the PreEI gain-threshold and re-scan decisions. The new implementation is not yet a validated reproduction of the original benchmark executable.

### Name changes

| Original | Refactored | Meaning |
| --- | --- | --- |
| `Product` / `ProductDb` | `TransactionRecord` and an explicit `List<TransactionRecord>` | Profit-augmented transactions |
| `Item` | `ItemPostingList` | Candidate itemset posting/difference list |
| `Items` / `Val` / `TIDs` | `ItemId` / `Gain` / `TransactionIds` | Item ID, covered gain, and transaction-ID list |
| `Global_IPList` | `_singletonPostingLists` | Current one-item candidate list |
| `Non_EI1` | `_prunedSingletonItemIds` | Filtered non-erasable singletons |
| `OriginalVal` / `TotalInsertVal` | `_profitAtLastFullScan` / `_profitSinceLastFullScan` | Profit used for the re-scan bound |
| `threshold_lw` / `threshold_up` | `_lowerGainThreshold` / `_upperGainThreshold` | Gain thresholds in profit units |
| `minVal` | `_minimumTransactionProfit` | Lower bound on a transaction's profit |
| `kq` | `_erasableItemsetCount` | Number of mined erasable itemsets |
| `processOriginal` | `PruneAndMineInitialCandidates` | Initial mining after upper-threshold filtering |
| `Mine` / `MineSub` | `CountErasableItemsets` | Unified recursive candidate enumeration |
| `IsPRI` | `HaveDisjointTransactionRanges` | Early check for disjoint sorted TID ranges |
| `CountdPID` | `CountTransactionDifference` | Count of `candidate \ excluded` TIDs |
| `dipset` | `BuildTransactionDifference` | Difference-list construction and profit sum |
| `ReadFile0` / `ReadFile1` | `ReadTransactions(..., storeProfits)` | Read new transactions or reread on a full scan |

### Removed code

- Unused `PatternGroup` (but **NOT** `result_EIs`, `SaveResult`, or stage-boundary `TrimExcess`: those are preserved).
- Unused `dipsetkhonggain` method and `GetMemoryUsedMB(forceGC)` argument.
- Empty TreeView `AfterSelect` handler and unused designer `OpenFileDialog` component.
- Commented-out code and obsolete `IME` / `P2` namespaces.
- Redundant duplicate `Mine` and `MineSub` recursion bodies (shared candidate enumerator instead).

### Behavioral safeguards

- Same source transaction order, same profit values, one-based TIDs.
- Same floor-plus-0.6 split rounding, **fixed at 80% + ten 2% increments** (no selectable profile).
- Same strict update-versus-re-scan comparison and gain threshold formulas.
- Same TID difference-list bound and candidate enumeration logic.
- Same historical incremental list-sort/index boundaries, left unchanged pending separate experimental verification.
- Input parsing now fails visibly on invalid lines instead of continuing after catching exceptions.
- Stage results can be saved in CSV to compare the counts across runs.

### Before using this package in the manuscript

1. Build the project in Visual Studio 2022; fix any compilation or Designer errors.
2. Verify the generated Mushroom part files using the SHA-256 script.
3. Compare the 11 itemset counts and update/re-scan decisions at lower=1.05%, upper=1.2% with the historic log.
4. Validate at least one sparse dataset and one re-scan-heavy dataset; note this release always uses 80/2, which may differ from the historical split of some datasets.
5. Rerun all performance experiments before replacing the original benchmark code with this optimized version; otherwise publish the original source snapshot separately for runtime and memory reproducibility.

## R5 constraints requested by the author

- `result_EIs` remains a `List<int>` in the mining engine, with original prefix/suffix serialization via `SaveResult`. The list is cleared and trimmed **after** a stage's runtime and working-set measurement, as in the source implementation.
- The dataset split is hardcoded to 80% initial + ten 2% insertions. The tenth insertion receives any rounding remainder. The GUI selector and command-line profile parameter have been removed.
- Source project still requires Visual Studio 2022/.NET on Windows for actual build and result validation.

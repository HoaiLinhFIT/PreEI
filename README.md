# PreEI - R9 candidate enumeration hot-path changes (validation draft)

This archive contains a **refactored C# implementation of PreEI**, the proposed method for incremental gain-based erasable-itemset mining. It does not include any third-party baseline implementation. **Do not publish this draft before compiling it on Windows and verifying the itemset counts against the author's historical runs.**

## Requirements

- Windows with Visual Studio 2022, including the .NET desktop development workload.
- .NET 6 Windows Desktop (`net6.0-windows`), matching the project target. **.NET 6 is out of support**; the target was not changed in order to minimize experimental-environment differences.
- Profit-augmented input files from the original experiments. **Profits are NOT randomly regenerated.**

Open `PreEI.sln` and select **Build > Build Solution**. Run the `PreEI` project.

## Input format and workflow

Each text file represents one complete dataset. **Each line contains integer item IDs separated by spaces, followed by the integer transaction profit as the final value.** Transaction IDs are assigned sequentially from 1 in input order.

1. The data split is fixed: **80% initial database + ten incremental batches of 2% each**. There is no split-percentage selector.
2. Click **Load data** and select ONE complete, profit-augmented `.txt` dataset.
3. The program starts a separate `--split` child process and creates `<dataset>_PreEI_batches/` beside the original data. It writes `*_part1.txt` through `*_part11.txt` in chronological order, plus `split_manifest.csv`.
4. Enter the **lower** and **upper** gain ratios as percentages, using a dot as the decimal separator (for example, **1.05** and **1.2** for the Mushroom demonstration).
5. Click **Run PreEI** and inspect the 11 stages as simple lines in the TreeView; click **Save results** to export a machine-readable CSV or text report.

**Important:** The author confirms that **all experimental datasets use 80% initial + ten 2% incremental batches**. The splitter implements that same division for every dataset. Matching the historical itemsets additionally requires the exact profit-augmented file and unchanged transaction order.

## Exact splitting and rounding

The splitter preserves the author's rule `Math.Floor(N * percentage / 100 + 0.6)` and the **float arithmetic** used in the original C# code. Part 1 receives the rounded 80%; parts 2 through 10 each receive the rounded 2%; part 11 receives **all remaining lines**. No transactions are reordered, shuffled, or assigned new profit values. The source file is read as a stream rather than loaded into memory during splitting.

For instance, the author's Mushroom dataset has **8,124 transactions**. It is split into 6,499 transactions in part 1, 163 in each of parts 2 through 10, and 158 in part 11. These historical source data files have been verified byte-for-byte; their SHA-256 hashes appear in `validation/mushroom_split_validation.csv`.

Manual splitting (Windows command prompt, without opening the GUI):

```text
PreEI.exe --split "C:\data\mushroom.txt" "C:\data\mushroom_PreEI_batches"
```

No split-percentage argument is accepted. The split is fixed to 80% + ten 2% batches.

## Verifying the original Mushroom experiment

After building in Visual Studio 2022:

1. Generate 11 batches from the original `mushroom.txt` using the **80%** initial profile.
2. Validate their hashes in PowerShell (from the repository root):

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File ".\validation\verify_mushroom_parts.ps1" -PartsFolder "C:\data\mushroom_PreEI_batches"
   ```

3. Run PreEI with lower gain threshold **1.05%** and upper threshold **1.2%**, and save the CSV results.
4. Compare all 11 itemset counts and the update/re-scan sequence with the original published-run log:

   ```text
   python validation/compare_mushroom_counts.py PreEI_mushroom_results.csv
   ```

The reference counts are stored in `validation/mushroom_expected_itemset_counts.csv`. This file contains **itemset counts**, not itemsets themselves. Python is needed only for this optional comparison script; it is not used by the PreEI application.

## Algorithm organization

- `Mining/PreEIMiningEngine.cs` - initialization, gain thresholds, incremental updates, full re-scans, and candidate enumeration.
- `Mining/TransactionRecord.cs` - one transaction (ID, item IDs, profit).
- `Mining/ItemPostingList.cs` - item ID, associated gain and sorted transaction IDs.
- `Mining/MiningResult.cs` - the 11 per-stage measurements and aggregate result.
- `PreEIForm.cs` and `PreEIForm.Designer.cs` - the Windows Forms UI only. R7 writes simple, flat TreeView lines using `Nodes.Add`, as in the original UI; no parent/child nodes or DataGridView are used.
- `DataSplitter.cs` - deterministic data splitting (executed as a separate process).
- `Program.cs` - GUI entry point and command-line splitting entry point.

## Measurement scope: exporting CSV/TXT is excluded

The timing and memory measurements are taken **inside the mining engine**, before any results are displayed or saved:

- Each stage's `Stopwatch` stops immediately after the mining operations and before result display or file export. `TotalRuntimeSeconds` is the sum of these per-stage measurements.
- `WorkingSet64` is sampled for each stage after its mining stopwatch has stopped. The overall `PeakWorkingSet64` is captured before `MineIncrementalDataset` returns to the GUI.
- The `Save results` button writes CSV or TXT using the already captured `MiningRunResult`. **Neither the time spent creating the report nor the memory allocations of CSV/TXT export are included in the returned measurements.** TreeView rendering is also outside the measurement interval.
- **Process-wide peak-memory caveat:** Windows `PeakWorkingSet64` is a process-lifetime high-water mark. If an experiment is run again in the *same application process*, the next recorded peak may still reflect the prior experiment, its TreeView display, or its CSV/TXT export. For independent peak-memory results, **close and restart PreEI before each experimental run**.

This section documents existing measurement boundaries; R8 does not change measured code paths, the CSV schema, the mining algorithm, or the 80% + ten 2% data split.

## What changed and benchmarking caveats

The original `result_EIs` collection is **retained** in this version. The mining engine serializes each prefix/suffix group using the original `SaveResult` layout and clears/trims the collection **after** recording the stage runtime and working-set memory. This storage is part of the historical implementation, although the encoded itemsets are not exported by the current UI. The refactor also removes an unused difference-list method, consolidates duplicate mining recursion, eliminates an unused dialog and empty UI event handler, renames domain objects and methods, and validates input rather than suppressing parsing exceptions.

**Restoring result_EIs preserves one significant memory-allocation behavior, but other refactoring changes can still affect runtime and memory.** The historical runtime and peak-memory numbers in the manuscript refer to the **original benchmark implementation** and cannot be claimed to have been reproduced by this refactor without running new experiments. The numerical itemset counts and re-scan decisions should be compared with the historical logs before release. If the manuscript retains the older benchmarks, archive and identify the exact original version used for those benchmarks separately from this optimized version, or rerun and update the benchmarks.

The process working-set measurements use `WorkingSet64` and `PeakWorkingSet64`, divided by 1,000,000 to report decimal **MB**. The split process exits before mining starts so it does not inflate the mining process's peak. **Restart the application before measuring an independent threshold configuration**, because Windows peak working-set counters do not reset on successive runs within the same process.

The reference implementation's incremental index rebuild and partial posting-list sorting rules have been preserved intentionally; modifications to those rules should be treated as algorithmic changes and evaluated separately.

## Current validation status

- Original Mushroom dataset and 11 historical files: **byte-for-byte match**, verified using the supplied input files.
- Mushroom expected itemset counts and update/re-scan decisions: the author's **R5 and R9 Release CSV files both match all 11 stages** in the historical Mushroom log. The R9 Release CSV is included under `validation/`.
- **Windows/.NET 6 compilation and GUI execution: NOT YET TESTED** in the current environment.

See `REFACTORING_NOTES.md` for a summary of renamed identifiers, removed code and required Windows tests.

## R7 flat TreeView display (no algorithm changes)

The original author preference is preserved: every result is written with a plain `resultsTreeView.Nodes.Add(...)`. For each of the 11 stages, the UI shows the stage/operation, runtime, erasable-itemset count, working-set memory and a separator line; it then appends thresholds, total mining runtime and overall process peak working set. There are **no nested TreeView nodes** and **no DataGridView**.

The CSV and TXT exports are generated directly from the measured `MiningRunResult`, never reconstructed from the displayed TreeView. CSV headers and result rows are unchanged from R5/R6, so historical validation scripts remain compatible.

Only the WinForms display files changed in R7. `Mining/PreEIMiningEngine.cs`, `DataSplitter.cs`, `Program.cs`, and the `result_EIs` behavior are byte-for-byte identical to R5/R6. The fixed 80% initial + ten 2% incremental batches remain in place.

**Validation:** File-level code comparison and static UI control reference checks passed in this environment. Build and run the R7 WinForms application in Visual Studio 2022 before public release.


## R9: concrete lists in performance-sensitive loops

The author requested **only** the following changes, retaining the R8 structure:

- Keep the clear, research-oriented method/variable/class names from R8.
- Use concrete `List<ItemPostingList>` in the recursive candidate enumerator and `List<int>` in the transaction difference / range-disjointness / result-serialization hot paths. Also use `List<TransactionRecord>` for the initial and incremental transaction processing loops. These are **type-only changes**, without changing the order, boundaries, or formulas of the loops.
- Keep the original `result_EIs` collection, `SaveResult`, post-measurement `Clear()`/`TrimExcess()`, fixed 80% + ten 2% split, flat `TreeView`, and CSV/TXT export after mining measurements.
- **Do not split** `CountErasableItemsets()` back into `Mine()` and `MineSub()`; the shared recursive implementation is unchanged.

These changes are intended to reduce interface-dispatch overhead where possible, but do not guarantee improved performance. A **Windows Release** benchmark is needed.

### R9 Release benchmarking (Mushroom)

1. In Visual Studio 2022, choose **Release**, preferably **x64**, and **Build > Rebuild Solution**.
2. Close prior PreEI instances. Run with **Ctrl+F5** (without debugger), load the original profit-augmented `mushroom.txt`, and enter **lower=1.05**, **upper=1.2** (both percentages).
3. Save the CSV after the full 11-stage run. CSV/TXT export itself is outside the reported mining runtime and memory. Restart the program between independent runs. Three to five runs on the same machine can help assess measurement variability.
4. Validate itemset counts and update/re-scan decisions:

   ```text
   python validation/compare_mushroom_counts.py "C:\data\PreEI_mushroom_results_R9.csv"
   ```

5. Compare every stage and total runtime with the original log measurements:

   ```text
   python validation/compare_stage_performance.py "C:\data\PreEI_mushroom_results_R9.csv" --reference validation/mushroom_original_stage_reference.csv
   ```

   For a more comparable **R5 Release → R9 Release** comparison, run:

   ```text
   python validation/compare_stage_performance.py "C:\data\PreEI_mushroom_results_R9.csv" --reference validation/mushroom_R5_release_reference.csv
   ```

The reference CSVs contain **historical measurements** supplied by the author: the original manuscript run (3.4005246 s total) and the R5 Release run (4.2030894 s total). Reference working-set values are measured *per stage* and are **not** the overall peak. Times and memory depend on environment and run conditions. The comparator treats matching itemset counts and stage types as correctness checks, but never imposes an exact runtime match.

**R9 validation:** The author supplied a Windows Release run of R9 (`validation/mushroom_R9_release_reference.csv`) that matches 11/11 historical Mushroom itemset counts and stage types. This environment has not compiled the application; the separate .NET 10 cloud runner must still pass a GitHub Actions build/run before publication.

## Run R9 directly on GitHub Actions (Connect by default; other datasets selectable)

This package includes a .NET 10 console runner in `PreEI.Console/` and the manual workflow `.github/workflows/preei-run.yml`. The console runner **links the unchanged R9 mining and split C# sources**; the GUI and simple TreeView remain available in the Windows project.

The workflow defaults to `datasets/connect.txt`, lower threshold **2.5%** and upper threshold **2.8%**, with 11-stage historical count/re-scan verification. **The actual profit-augmented Connect dataset is NOT included**, so it must be added under that path before running the default job. The exact log is included under `validation/connect_2_5_2_8_original_log.txt`, and its reference CSV is `validation/connect_expected_itemset_counts.csv`.

To run Mushroom, select `datasets/mushroom.txt`, lower **1.05%**, upper **1.2%**. It is bundled and has a separate 11-stage historical check. For any other profit-augmented dataset, upload it and enter its repository-relative path and thresholds in the workflow form. Without a corresponding reference, the program runs and exports results but reports **SKIP** rather than claiming correctness validation.

**All datasets:** fixed 80% initial plus ten 2% increments (author-confirmed). CSV and TXT writing are outside mining runtime and working-set measurement. Time/peak memory vary with environment and are never required to match original benchmark values. The .NET 10 GitHub runner must still be built and executed on GitHub to confirm the deployment works.

See **[GITHUB_ACTIONS_QUICKSTART.md](GITHUB_ACTIONS_QUICKSTART.md)** for Vietnamese instructions.

# PreEI Version 1

**PreEI** is a C# implementation for incremental gain-based erasable-itemset mining. This repository provides two ways to run the **same mining engine**:

- **GitHub Actions:** a headless console runner on a GitHub-hosted Windows runner. It can be run manually from the **Actions** tab.
- **Windows desktop:** the WinForms application, with a simple TreeView for displaying stage results.

The repository contains the PreEI implementation, not third-party baseline implementations. The research input datasets are profit-augmented; these are distinct from unmodified public datasets.

## Run PreEI Version 1 on GitHub

1. Place your complete profit-augmented dataset at `datasets/connect.txt`, or upload another `.txt` file to `datasets/`. The actual dataset file is **not included** in this source archive.
2. Open **Actions → Run PreEI Version 1 → Run workflow** on the repository's default branch.
3. Connect is the default configuration: `datasets/connect.txt`, lower threshold `2.5`, upper threshold `2.8`. To run another dataset, change the repository-relative path and thresholds.
4. Leave **Verify reference** checked to compare all 11 stages whenever a matching reference profile exists.
5. After the job finishes, open its run summary and download the artifact named `preei-version-1-run-<number>`. It contains CSV, TXT, run metadata, validation status and data batches.

Data preparation runs in a separate process before mining. The split is fixed for **every dataset**: **80% initial database + ten incremental batches of 2%**, in original transaction order. The last batch receives any rounding remainder. No profit values are generated or changed.

### Input format

Each input line contains space-separated **integer item IDs** followed by one **integer transaction profit** as the last field. File order is retained; transaction IDs are assigned sequentially. To reproduce published counts, use the **same profit-augmented file, record order and thresholds** as the reference run.

### Reference verification

`validation/reference_profiles.json` maps dataset filenames and threshold pairs to reference CSV files. The supplied Connect profile uses `validation/connect_expected_itemset_counts.csv` (`2.5%` lower, `2.8%` upper). When a profile matches, `validation/compare_reference_counts.py` checks all 11 stage counts and the update/re-scan sequence; a mismatch produces a failing run. If no profile matches, the workflow records **SKIP** rather than claiming the result was verified. You may add reference profiles for additional datasets.

### Runtime and peak memory

The mining engine measures stage runtime via `Stopwatch` and process working-set/peak-working-set memory. **CSV/TXT export and UI rendering occur after mining measurements are frozen.** The data-splitting step is excluded from mining runtime and peak-memory measurement by running it in a separate process.

The GitHub runner uses **.NET 10**, while the Windows desktop project targets **.NET 6 Windows Desktop**. Runtime and peak memory depend on operating environment, CPU, JIT, GC and machine state. GitHub Actions results are intended primarily for reproducibility checks, and should **not** be substituted directly for the performance benchmarks reported for a different environment.

## Build the Windows application

Open `PreEI.sln` in **Visual Studio 2022** with the .NET desktop development workload. Build the `PreEI` project in **Release** mode, then choose one complete profit-augmented `.txt` dataset. The application generates 11 batches automatically and displays flat TreeView results. It can export CSV and TXT after mining has completed.

The `PreEI.Console` project uses the exact same mining and data-splitting `.cs` source files; it does not contain a duplicate algorithm implementation.

## Main files

| File or folder | Purpose |
| --- | --- |
| `PreEI/Mining/PreEIMiningEngine.cs` | Mining, incremental updates and re-scans |
| `PreEI/DataSplitter.cs` | Fixed 80% + 10 × 2% data splitting |
| `PreEI/PreEIForm.cs` | Windows UI |
| `PreEI.Console/Program.cs` | Console entry point, result exports and metadata |
| `.github/workflows/preei-run.yml` | On-demand GitHub Actions workflow |
| `validation/` | Reference counts and result verification |
| `datasets/` | Place profit-augmented input files here |

**Release name:** PreEI Version 1 (`1.0.0`). This is the release naming of the refactored code, not a claim that measurements from different execution environments are numerically identical.

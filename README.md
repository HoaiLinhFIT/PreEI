# PreEI Version 1

**Version:** `1.0.0`  
**Repository:** https://github.com/HoaiLinhFIT/PreEI  
**Language:** C#  
**Run options:** GitHub Actions (console) or Windows desktop (WinForms)

PreEI implements **incremental gain-based erasable-itemset mining** for transaction databases in which each transaction is associated with a profit. The implementation uses selective maintenance of singleton posting lists, a gain-based re-scanning condition, and prefix-based depth-first candidate exploration.

The same `PreEIMiningEngine.cs` and `DataSplitter.cs` source files are used by the WinForms application and the console runner. This repository provides the **PreEI implementation**, not the source code of third-party baseline algorithms.

> **Scope of reproducibility:** GitHub Actions is primarily for checking the number of erasable itemsets and the incremental-update/full-re-scan sequence. Runtime and peak memory are environment-dependent and are not required to equal the measurements reported in the manuscript.

**Vietnamese step-by-step guide:** [GITHUB_ACTIONS_QUICKSTART.md](GITHUB_ACTIONS_QUICKSTART.md)

## 1. Run on GitHub Actions

1. Upload a complete **profit-augmented** input file to the `datasets/` folder. For the default configuration, use `datasets/connect.txt`. The dataset itself is **not included in this source archive**.
2. Commit the dataset file to your repository (only if you have permission to publish it).
3. Open **Actions → Run PreEI Version 1 → Run workflow**.
4. Select the dataset and thresholds. The default values are:

   | Workflow input | Default | Meaning |
   | --- | --- | --- |
   | `dataset` | `datasets/connect.txt` | Repository-relative input path |
   | `lower_threshold` | `2.5` | Lower gain threshold, in percent |
   | `upper_threshold` | `2.8` | Upper gain threshold, in percent |
   | `verify_reference` | `true` | Compare with a matching reference, if available |

5. Click **Run workflow**. After completion, open the run summary and download the **Artifacts** package named `preei-version-1-run-<run_number>`.

The workflow runs on a GitHub-hosted Windows runner, builds the console project with **.NET 10**, splits the dataset in a separate process, mines 11 stages, and optionally verifies the stage results. The input thresholds must satisfy **0 < lower < upper**.

**Important:** The GitHub Actions runner does not display the WinForms TreeView. CSV and TXT files contain the stage summaries. The workflow does not require or invoke the desktop UI.

## 2. Input dataset format

Provide one transaction per line. Fields must be **space-separated integers**:

```text
<item_id_1> <item_id_2> ... <item_id_n> <transaction_profit>
```

For example, `3 7 15 120` means item IDs `3`, `7`, `15` and transaction profit `120`. The **last integer is always the transaction profit**, not an item ID. The original transaction order is preserved and transaction IDs are assigned sequentially.

For a valid reproduction, use the **same profit-augmented data file, transaction order, thresholds, and software version** as the reference run. The program does **not** generate profits or randomize transaction order. Supplying an unmodified source dataset instead of the profit-augmented research input may produce different results or fail to parse.

### Fixed incremental-data partition

For **every dataset**, `DataSplitter.cs` divides the input sequentially into:

- **Stage 0:** the initial 80% of transactions.
- **Stages 1–10:** ten additional batches of approximately 2% each.

The splitter preserves its original floating-point rounding convention (`floor(value + 0.6)`); the **last batch receives the remaining transactions** so that every input transaction is assigned exactly once. Consequently, small rounding differences are expected in batch sizes. All 11 batches must be non-empty.

The splitting process is run separately from mining to exclude dataset preparation from mining measurements. You do **not** need to upload 11 pre-split files.

## 3. Results and automatic verification

A completed workflow uploads the `outputs/` directory as a GitHub Actions artifact. Key files include:

| Output | Contents |
| --- | --- |
| `results/PreEI_<dataset>_results.csv` | One row per stage, plus a `TOTAL` row |
| `results/PreEI_<dataset>_results.txt` | Human-readable mining summary |
| `results/run_metadata.json` | Dataset SHA-256, thresholds, batch sizes, software/environment metadata, timing and memory |
| `results/validation_status.txt` | Reference check: `PASS`, `FAIL`, or `SKIP` (when the verification step reaches this file) |
| `batches/` | The 11 generated input batches and `split_manifest.csv` |

The CSV columns are `stage`, `type`, `erasable_itemsets`, `runtime_seconds`, and `working_set_mb`. Stage types are `InitialMining`, `IncrementalUpdate`, and `FullRescan` as applicable. The `TOTAL` row's runtime field is the **sum of per-stage runtimes**; its `working_set_mb` field records the **overall process peak working set**, rather than a sum of stage memory values. Memory is expressed as **decimal MB (1 MB = 1,000,000 bytes)**.

### What the verifier checks

`validation/reference_profiles.json` identifies the reference CSV applicable to a dataset filename and exact pair of thresholds. The included Connect profile is:

- Dataset: `connect.txt`
- Lower threshold: `2.5%`
- Upper threshold: `2.8%`
- Reference: `validation/connect_expected_itemset_counts.csv`

When a profile matches, `validation/compare_reference_counts.py` compares all **11 stage indices, erasable-itemset counts, and processing types**. A mismatch is reported as **FAIL** and causes the verification step to fail. When no matching profile exists, the step reports **SKIP** rather than claiming successful validation. Verification can also be disabled in the workflow form.

**Limitations:** The reference check verifies stage counts and update/full-re-scan decisions; it does **not** compare the complete membership of all itemsets, assert bit-for-bit equality of memory/runtime values, or automatically compare the input file's SHA-256 against a published reference hash. The input SHA-256 is recorded in `run_metadata.json` to support independent checks.

To verify another dataset, add a matching profile to `validation/reference_profiles.json` and a reference CSV with columns `stage,type,erasable_itemsets` for stages `0` through `10`. A trustworthy reference must use the **same original profit-augmented file and fixed split**.

## 4. Runtime and peak memory

- **Runtime:** `Stopwatch` starts before reading each mining stage's input and stops after mining and result recording for that stage.
- **Stage memory:** process `WorkingSet64` is sampled per stage.
- **Overall peak memory:** process `PeakWorkingSet64` is captured in the mining result; this is the process peak working set, not the maximum managed-heap allocation.
- **Excluded:** initial dataset splitting, display on TreeView, and CSV/TXT/metadata export. The stored `result_EIs` groups are cleared after stage measurements have been recorded.

The **GitHub Actions console runner** targets .NET 10; the **WinForms desktop application** targets .NET 6 Windows. Processor, operating system, runtime/JIT, garbage collection, other processes, and measurement conditions can change runtime and peak memory. Do **not** use GitHub-runner performance measurements as direct replacements for benchmarks performed under a different environment.

## 5. Build and run locally

### Windows desktop application (WinForms)

1. Open `PreEI.sln` in Visual Studio 2022 with the .NET desktop development workload and the necessary .NET 6 targeting components installed.
2. Select **Release** and build the `PreEI` project.
3. Launch PreEI, select the complete profit-augmented dataset and enter the lower/upper thresholds.
4. Run the algorithm. The flat TreeView displays stage summaries; use the application's export controls to save CSV/TXT.

### Console runner (without the desktop UI)

Install the **.NET 10 SDK** and run from the repository root:

```powershell
dotnet build PreEI.Console/PreEI.Console.csproj --configuration Release

dotnet PreEI.Console/bin/Release/net10.0/PreEI.Console.dll split `
  --dataset datasets/connect.txt --parts outputs/batches

dotnet PreEI.Console/bin/Release/net10.0/PreEI.Console.dll mine `
  --dataset datasets/connect.txt --parts outputs/batches `
  --lower 2.5 --upper 2.8 --output outputs/results

python validation/compare_reference_counts.py `
  outputs/results/PreEI_connect_results.csv `
  validation/connect_expected_itemset_counts.csv
```

The final command is an **optional manual reference comparison** and requires Python 3 and the appropriate reference CSV. To mine another dataset, replace the dataset path and thresholds; the verification command must also use that dataset's matching reference (if available).

## 6. Repository structure

```text
.github/workflows/preei-run.yml       GitHub Actions workflow
PreEI/Mining/PreEIMiningEngine.cs     Shared mining engine
PreEI/Mining/ItemPostingList.cs       Posting-list representation
PreEI/Mining/TransactionRecord.cs     Transaction representation
PreEI/DataSplitter.cs                Deterministic 80% + 10 x 2% splitter
PreEI/PreEIForm.cs                    WinForms interface
PreEI.Console/Program.cs             Headless console runner and exports
datasets/                           Place profit-augmented data files here
validation/reference_profiles.json    Mapping from inputs to references
validation/compare_reference_counts.py Reference checker
README.md                           Main documentation
LICENSE                             Software license
CITATION.cff                        Software citation metadata
VERSION                             Version identifier (1.0.0)
```

## 7. Data availability and reproducibility

This source archive contains the implementation and a **Connect reference count file**, but **not the original profit-augmented Connect dataset**. To run the default workflow, you must supply that input at `datasets/connect.txt`. The reference CSV alone is not a substitute for the research dataset.

If you make datasets available publicly, document their source, preprocessing/profit assignment, transaction ordering, and checksums. **Only redistribute datasets when their terms and rights allow it.** Large or separately licensed datasets may be archived outside GitHub with stable download instructions and identifiers in this README.

## 8. Citation and license

If you use the PreEI software, cite this repository using [CITATION.cff](CITATION.cff). GitHub will display a **Cite this repository** option when that file is present on the default branch. A versioned Zenodo DOI and an accepted journal article citation can be added **when they actually exist**; no DOI is claimed here.

The original PreEI source code and its documentation are distributed under the **MIT License**, subject to the rights of their copyright holders; see [LICENSE](LICENSE). The software license does **not** automatically grant rights to redistribute third-party datasets, third-party baseline implementations or their documentation.

**Maintainers:** repository owner [HoaiLinhFIT](https://github.com/HoaiLinhFIT). For questions or reproducibility issues, open a GitHub issue. The listed software citation authors follow the associated manuscript's author order and should be confirmed by the contributors before public release.

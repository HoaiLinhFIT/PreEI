# R7: simple flat TreeView, as in the original PreEI program

## Rationale

The original program wrote results directly using `treeView1.Nodes.Add(...)`. The R5 nested TreeView and R6 DataGridView were unnecessary. This revision restores a flat TreeView with top-level lines only.

## Changed files

- `PreEI/PreEIForm.cs`: replace R6 table rendering with simple `resultsTreeView.Nodes.Add(...)` calls; keep TXT/CSV export independent from the UI.
- `PreEI/PreEIForm.Designer.cs`: restore the TreeView from R5, remove DataGridView and summary label.
- `README.md`: document the layout and preserved functionality.

## Preserved without modification

- `PreEI/Mining/PreEIMiningEngine.cs` (including `result_EIs` and `SaveResult()`)
- `PreEI/DataSplitter.cs` (fixed 80% initial and ten incremental batches of 2%)
- `PreEI/Program.cs`
- All model classes and CSV export schema.

No changes to gain thresholds, incremental update, full re-scan or itemset enumeration.

## Test requirements

Open `PreEI.sln` in Visual Studio 2022, build in Release mode and run `mushroom.txt` with lower=1.05 and upper=1.2. Verify itemset counts for all 11 stages and the re-scan at stage 6 against the reference CSV. The code has not been compiled under Windows in the authoring environment.

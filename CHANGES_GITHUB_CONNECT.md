# GitHub Actions: Connect as default, with reference checks

- Default dataset: `datasets/connect.txt`; lower=2.5%, upper=2.8%.
- Other datasets remain selectable via `workflow_dispatch.inputs.dataset` and their own thresholds.
- All datasets use 80% initial + ten 2% increments, as confirmed by the author.
- Added historical Connect reference (`validation/connect_expected_itemset_counts.csv`), sourced from the original PreEI log (`validation/connect_2_5_2_8_original_log.txt`). Full re-scan is expected at stage 5.
- Historical validation runs for Connect 2.5/2.8 and Mushroom 1.05/1.2 when `verify_reference` is true; otherwise status is SKIP, not PASS.
- Connect profit-augmented dataset is not provided and must be added by the author. Reference counts are not proof that any arbitrary Connect file has matching assigned profits.
- **No changes** to `PreEI/Mining/*.cs`, `PreEI/DataSplitter.cs`, the WinForms GUI, `result_EIs`, `SaveResult`, the incremental re-scan conditions, or the measurement scope.
- This package has NOT been compiled or run using GitHub-hosted Actions in the present environment.

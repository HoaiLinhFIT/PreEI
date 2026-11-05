# Built-in demonstration dataset

`mushroom.txt` is the **profit-augmented** transaction file supplied with the PreEI research project. It has **8,124 transactions**. The last integer on each line is the assigned transaction profit. It is used in the historical PreEI validation with thresholds **lower = 1.05%**, **upper = 1.2%**, and the fixed **80% initial + 10 x 2%** split.

**Source attribution for underlying data:**

- *Mushroom*, UCI Machine Learning Repository.
- Dataset DOI: https://doi.org/10.24432/C5959T
- Original dataset license: Creative Commons Attribution 4.0 International (CC BY 4.0): https://creativecommons.org/licenses/by/4.0/
- Modifications: transaction encoding into integer item IDs and addition of transaction profit values as supplied for this research project. These values are **not part of UCI's original Mushroom features**.

If publishing this derived dataset, retain the attribution and clearly distinguish the derived values from the original UCI data.

## Connect and additional datasets

The default GitHub Actions input is `datasets/connect.txt`, lower threshold **2.5%** and upper threshold **2.8%**. **Connect is NOT bundled with this archive:** copy the exact profit-augmented Connect transaction file from the author's experiment into this directory and commit it before running the default workflow. Do not replace it with raw Connect data or regenerate the profits. Historical counts and re-scan decisions are recorded in `validation/connect_expected_itemset_counts.csv`, transcribed from the author's `validation/connect_2_5_2_8_original_log.txt`.

The author confirms the standard data division is **80% initial + ten increments of 2%** for all datasets. Other profit-augmented `.txt` datasets can be uploaded under `datasets/` and selected via the GitHub Actions `dataset` input. For datasets with no historical reference, the mining run works but reference validation is skipped.

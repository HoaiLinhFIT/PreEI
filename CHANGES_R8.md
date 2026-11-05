# R8: explicit measurement boundaries for CSV/TXT export

## Requirement

Saving CSV or TXT reports must not be included in the reported mining runtime or working-set memory.

## Verified in the R7 implementation

- `PreEIMiningEngine` stops the per-stage `Stopwatch` and records the stage measurements before it returns a `MiningRunResult`.
- The overall process peak is sampled before returning from `MineIncrementalDataset`.
- `PreEIForm.OnSaveResultsClick` is a separate user-triggered callback, executed only after `_lastRun` has been populated.
- CSV and TXT are written from the cached result object, not from TreeView.

## R8 change scope

- Added explanatory comments to `PreEIForm.cs` and `PreEIMiningEngine.cs`.
- Documented the measurement scope and process-wide peak limitation in `README.md`.
- No executable logic was altered. R7 and R8 perform identical calculations and follow the same code paths.
- `result_EIs`, the plain flat TreeView, the fixed 80% + ten 2% split, thresholds, and CSV/TXT output format are unchanged.

## Important limitation

`PeakWorkingSet64` cannot be assumed to reset between multiple experiments in the same running Windows process. Restart PreEI before each independent experiment. This restriction applies even though the save operations occur after the first run's measurements.

## Validation

Static source-level checks only; no Windows/.NET compilation was performed in this environment.

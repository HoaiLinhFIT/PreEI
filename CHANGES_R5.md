# R5 changes (author-requested)

This draft modifies the R4 refactor in only two functional areas.

1. **Retained `result_EIs`:** The mining engine stores prefix/suffix groups in the same integer-list layout as the original `SaveResult` method. The list is cleared and trimmed after each stage's runtime/working-set measurement, matching the original stage boundaries. The GUI still exports stage counts and resource measurements, not the full list of itemsets.
2. **Fixed split profile:** The program always creates 11 sequential parts: initial 80%, nine rounded 2% parts, and one final part containing the remaining 2%-approximately transactions. The former 80/90/95/99 dropdown and command-line split-percentage option were removed. The split still runs in a child process and writes `split_manifest.csv`.

Unchanged from R4: class/function renaming, the PreEI threshold and re-scan logic, and the other organizational refactors.

## Verification status

- Compared the fixed split counts and ordered transaction text against the supplied historical Mushroom parts (8,124 transactions; 6,499 / 163 x 9 / 158).
- Performed static structure/reference checks on the C# project.
- Windows/.NET 6 build and actual PreEI itemset counts have **not** been independently executed for this draft. Check them in Visual Studio 2022 before publication.
- Even with `result_EIs` restored, the refactored project is not the identical benchmark binary used in the manuscript; runtime and peak-memory results should be checked before making reproducibility claims.

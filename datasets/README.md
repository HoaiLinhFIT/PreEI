# Profit-augmented datasets

Upload the complete profit-augmented transaction files used in your research here, for example `connect.txt`. Dataset files are not bundled with this source release.

Each line contains space-separated integer item IDs followed by the transaction profit as the final integer. The exact input file and record order are necessary when comparing with historical reference counts.

GitHub Actions defaults to `datasets/connect.txt`, with lower=2.5% and upper=2.8%. You can select another dataset and threshold pair from the manual workflow form. All datasets use **80% initial + 10 incremental batches of 2% each**.

When publishing datasets, check redistribution rights and document the source, preprocessing, profit assignment and checksums.

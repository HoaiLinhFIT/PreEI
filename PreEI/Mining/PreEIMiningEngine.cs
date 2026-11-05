using System.Diagnostics;
using System.Globalization;

namespace PreEIApplication.Mining;

/// <summary>
/// Mining engine for the proposed PreEI algorithm. This class contains no GUI
/// code, so dataset handling, mining, and reporting can be examined separately.
/// The original gain-threshold and re-scan conditions are retained.
/// </summary>
internal sealed class PreEIMiningEngine
{
    private static readonly IComparer<ItemPostingList> PostingListSizeComparer =
        Comparer<ItemPostingList>.Create((left, right) =>
            right.TransactionIds.Count.CompareTo(left.TransactionIds.Count));

    // Transaction ID is one-based: profit for TID t is at index t - 1.
    private readonly List<int> _transactionProfits = new();
    private readonly Dictionary<int, int> _postingPositionByItemId = new();
    private readonly HashSet<int> _prunedSingletonItemIds = new();
    private List<ItemPostingList> _singletonPostingLists = new();

    private int _nextTransactionId = 1;
    private int _minimumTransactionProfit = int.MaxValue;
    private int _profitAtLastFullScan;
    private int _profitSinceLastFullScan;
    private int _newSingletonCountSinceLastFullScan;
    private double _lowerGainThreshold;
    private double _upperGainThreshold;
    private long _erasableItemsetCount;

    // Preserve the original result_EIs encoding used in the historical runs:
    // [prefixLength, prefixItems..., suffixLength, suffixItems...] per group.
    // Retaining these itemsets affects runtime and working-set measurements.
    private readonly List<int> result_EIs = new();

    internal MiningRunResult MineIncrementalDataset(
        IReadOnlyList<string> orderedBatchPaths,
        double lowerGainRatioPercent,
        double upperGainRatioPercent)
    {
        if (orderedBatchPaths.Count != DataSplitter.PartCount)
            throw new ArgumentException("Exactly 11 chronologically ordered dataset parts are required.",
                nameof(orderedBatchPaths));
        if (!double.IsFinite(lowerGainRatioPercent) ||
            !double.IsFinite(upperGainRatioPercent) ||
            lowerGainRatioPercent <= 0 || upperGainRatioPercent <= lowerGainRatioPercent)
            throw new ArgumentException("Threshold percentages must satisfy 0 < lower < upper.");

        // A fresh engine is instantiated for each run. Do not reuse it across
        // threshold configurations: its posting lists are stateful.
        var stages = new List<MiningStageResult>(DataSplitter.PartCount);
        var stopwatch = Stopwatch.StartNew();

        // Stage 0: original dataset.
        var initialTransactions = new List<TransactionRecord>();
        int initialProfit = ReadTransactions(orderedBatchPaths[0], initialTransactions, storeProfits: true);
        BuildSingletonPostingLists(initialTransactions);
        initialTransactions.Clear(); // release parsed rows before candidate enumeration
        _postingPositionByItemId.Clear();
        _profitAtLastFullScan = initialProfit;
        SetGainThresholds(lowerGainRatioPercent, upperGainRatioPercent);
        PruneAndMineInitialCandidates();
        stopwatch.Stop();
        stages.Add(CaptureStage(0, MiningStageType.InitialMining, stopwatch));
        ClearStoredItemsets();

        // Preserve the author's original re-scan bound, including the
        // strict comparison (profitSinceLastFullScan < reScanBound).
        for (int stageIndex = 1; stageIndex < orderedBatchPaths.Count; stageIndex++)
        {
            _erasableItemsetCount = 0;
            stopwatch.Restart();
            var insertedTransactions = new List<TransactionRecord>();
            int insertedProfit = ReadTransactions(
                orderedBatchPaths[stageIndex], insertedTransactions, storeProfits: true);
            _profitSinceLastFullScan += insertedProfit;

            double lowerRatio = lowerGainRatioPercent / 100.0;
            double upperRatio = upperGainRatioPercent / 100.0;
            double reScanBound = ((upperRatio - lowerRatio) * _profitAtLastFullScan) / lowerRatio;

            MiningStageType stageType;
            if (_profitSinceLastFullScan < reScanBound)
            {
                _lowerGainThreshold =
                    ((_profitAtLastFullScan + _profitSinceLastFullScan) * lowerGainRatioPercent) / 100.0;
                UpdateSingletonPostingLists(insertedTransactions);
                insertedTransactions.Clear();
                CountErasableItemsets(
                    _singletonPostingLists, new List<int>(), filterByLowerGainThreshold: true);
                stageType = MiningStageType.IncrementalUpdate;
            }
            else
            {
                _singletonPostingLists = new List<ItemPostingList>();
                _prunedSingletonItemIds.Clear();
                _postingPositionByItemId.Clear();
                _profitAtLastFullScan += _profitSinceLastFullScan;
                _profitSinceLastFullScan = 0;
                _nextTransactionId = 1;
                _newSingletonCountSinceLastFullScan = 0;

                // Exactly as in the original ReadFile1 path: read all parts
                // again without appending duplicate entries to the profit index.
                var transactionsToRescan = new List<TransactionRecord>();
                for (int priorStage = 0; priorStage <= stageIndex; priorStage++)
                    ReadTransactions(orderedBatchPaths[priorStage], transactionsToRescan,
                        storeProfits: false);

                BuildSingletonPostingLists(transactionsToRescan);
                transactionsToRescan.Clear();
                _postingPositionByItemId.Clear();
                SetGainThresholds(lowerGainRatioPercent, upperGainRatioPercent);
                PruneAndMineInitialCandidates();
                stageType = MiningStageType.FullRescan;
            }

            stopwatch.Stop();
            stages.Add(CaptureStage(stageIndex, stageType, stopwatch));
            ClearStoredItemsets();
        }

        // Freeze the process peak before returning to the WinForms layer.
        // TreeView rendering and CSV/TXT export occur only after this point.
        return new MiningRunResult(
            stages,
            stages.Sum(stage => stage.RuntimeSeconds),
            MeasureProcessMemoryMegabytes(peak: true));
    }

    private int ReadTransactions(
        string path, List<TransactionRecord> destination, bool storeProfits)
    {
        int batchProfit = 0;
        using var reader = new StreamReader(path);
        string? line;
        int lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2)
                throw new InvalidDataException($"Invalid transaction in {path} at line {lineNumber}.");

            var transaction = new TransactionRecord
            {
                TransactionId = _nextTransactionId++,
                Profit = int.Parse(fields[^1], NumberStyles.Integer, CultureInfo.InvariantCulture)
            };
            for (int itemIndex = 0; itemIndex < fields.Length - 1; itemIndex++)
                transaction.ItemIds.Add(
                    int.Parse(fields[itemIndex], NumberStyles.Integer, CultureInfo.InvariantCulture));
            destination.Add(transaction);

            if (!storeProfits)
                continue;

            batchProfit += transaction.Profit;
            _transactionProfits.Add(transaction.Profit);
            if (transaction.Profit < _minimumTransactionProfit)
                _minimumTransactionProfit = transaction.Profit;
        }
        return batchProfit;
    }

    private void BuildSingletonPostingLists(List<TransactionRecord> transactions)
    {
        int nextPostingPosition = _singletonPostingLists.Count;
        foreach (TransactionRecord transaction in transactions)
        {
            foreach (int itemId in transaction.ItemIds)
            {
                if (_postingPositionByItemId.TryGetValue(itemId, out int position))
                {
                    ItemPostingList postingList = _singletonPostingLists[position];
                    postingList.TransactionIds.Add(transaction.TransactionId);
                    postingList.Gain += transaction.Profit;
                }
                else
                {
                    var postingList = new ItemPostingList
                    {
                        ItemId = itemId,
                        Gain = transaction.Profit
                    };
                    postingList.TransactionIds.Add(transaction.TransactionId);
                    _singletonPostingLists.Add(postingList);
                    _postingPositionByItemId[itemId] = nextPostingPosition++;
                }
            }
        }
    }

    private void SetGainThresholds(double lowerRatioPercent, double upperRatioPercent)
    {
        _lowerGainThreshold = (_profitAtLastFullScan * lowerRatioPercent) / 100.0;
        _upperGainThreshold = (_profitAtLastFullScan * upperRatioPercent) / 100.0;
    }

    private void PruneAndMineInitialCandidates()
    {
        int lastCandidateIndex = _singletonPostingLists.Count - 1;
        for (int candidateIndex = lastCandidateIndex; candidateIndex >= 0; candidateIndex--)
        {
            if (_singletonPostingLists[candidateIndex].Gain <= _upperGainThreshold)
                continue;

            _prunedSingletonItemIds.Add(_singletonPostingLists[candidateIndex].ItemId);
            if (lastCandidateIndex > candidateIndex)
                _singletonPostingLists[candidateIndex] = _singletonPostingLists[lastCandidateIndex];
            _singletonPostingLists.RemoveAt(lastCandidateIndex--);
        }
        _singletonPostingLists.Sort(PostingListSizeComparer);
        CountErasableItemsets(
                    _singletonPostingLists, new List<int>(), filterByLowerGainThreshold: true);
    }

    private void UpdateSingletonPostingLists(List<TransactionRecord> insertedTransactions)
    {
        // Preserve the historical incremental index/sort boundaries.
        // Changing them is a separate algorithmic correction requiring new
        // experiments, so this refactor deliberately retains the same behavior.
        int totalCandidates = _singletonPostingLists.Count;
        for (int index = _newSingletonCountSinceLastFullScan; index < totalCandidates; index++)
            _postingPositionByItemId[_singletonPostingLists[index].ItemId] = index;

        foreach (TransactionRecord transaction in insertedTransactions)
        {
            for (int itemPosition = transaction.ItemIds.Count - 1; itemPosition >= 0; itemPosition--)
            {
                int itemId = transaction.ItemIds[itemPosition];
                if (_prunedSingletonItemIds.Contains(itemId))
                    continue;

                if (_postingPositionByItemId.TryGetValue(itemId, out int position))
                {
                    ItemPostingList postingList = _singletonPostingLists[position];
                    postingList.Gain += transaction.Profit;
                    postingList.TransactionIds.Add(transaction.TransactionId);
                }
                else
                {
                    var postingList = new ItemPostingList
                    {
                        ItemId = itemId,
                        Gain = transaction.Profit
                    };
                    postingList.TransactionIds.Add(transaction.TransactionId);
                    _singletonPostingLists.Add(postingList);
                    _postingPositionByItemId[itemId] = totalCandidates++;
                    _newSingletonCountSinceLastFullScan++;
                }
            }
        }

        _singletonPostingLists.Sort(
            _newSingletonCountSinceLastFullScan,
            totalCandidates - _newSingletonCountSinceLastFullScan,
            PostingListSizeComparer);
    }

    /// <summary>
    /// Uses concrete List instances in the candidate-enumeration hot path,
    /// matching the original implementation for direct indexed access.
    /// Enumerates erasable itemsets, preserving the original prefix/suffix
    /// grouping in result_EIs while counting each itemset once.
    /// The initial candidate level applies the lower threshold; recursively
    /// constructed candidates already satisfy that threshold.
    /// </summary>
    private void CountErasableItemsets(
        List<ItemPostingList> candidates,
        List<int> prefix,
        bool filterByLowerGainThreshold)
    {
        int eligibleCount = 0;
        if (filterByLowerGainThreshold)
        {
            for (int index = 0; index < candidates.Count; index++)
                if (candidates[index].Gain <= _lowerGainThreshold)
                    eligibleCount++;
        }
        else
        {
            eligibleCount = candidates.Count;
        }

        int[] suffix = new int[eligibleCount];
        int suffixCount = 0;
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            ItemPostingList current = candidates[candidateIndex];
            if (filterByLowerGainThreshold && current.Gain > _lowerGainThreshold)
                continue;

            suffix[suffixCount++] = current.ItemId;
            _erasableItemsetCount++;
            var conditionalCandidates = new List<ItemPostingList>();
            for (int nextIndex = candidateIndex + 1; nextIndex < candidates.Count; nextIndex++)
            {
                ItemPostingList next = candidates[nextIndex];
                if (filterByLowerGainThreshold && next.Gain > _lowerGainThreshold)
                    continue;

                ItemPostingList? extension = TryExtendCandidate(current, next);
                if (extension is not null)
                    conditionalCandidates.Add(extension);
            }
            if (conditionalCandidates.Count > 0)
            {
                prefix.Add(current.ItemId);
                CountErasableItemsets(
                    conditionalCandidates, prefix, filterByLowerGainThreshold: false);
                prefix.RemoveAt(prefix.Count - 1);
            }
        }

        SaveResult(prefix, suffix, suffixCount);
    }

    // Original SaveResult serialization, retained for experiment fidelity.
    private void SaveResult(List<int> prefix, int[] suffix, int count)
    {
        if (count == 0)
            return;

        result_EIs.Add(prefix.Count);
        for (int index = 0; index < prefix.Count; index++)
            result_EIs.Add(prefix[index]);

        result_EIs.Add(count);
        for (int index = 0; index < count; index++)
            result_EIs.Add(suffix[index]);
    }

    // Match the original stage boundary: measure runtime/working set first,
    // then discard stored groups before starting the next stage.
    private void ClearStoredItemsets()
    {
        result_EIs.Clear();
        result_EIs.TrimExcess();
    }

    private ItemPostingList? TryExtendCandidate(ItemPostingList current, ItemPostingList next)
    {
        if (HaveDisjointTransactionRanges(current.TransactionIds, next.TransactionIds))
        {
            int combinedGain = current.Gain;
            foreach (int transactionId in next.TransactionIds)
                combinedGain += _transactionProfits[transactionId - 1];

            if (combinedGain > _lowerGainThreshold)
                return null;
            return new ItemPostingList
            {
                ItemId = next.ItemId,
                Gain = combinedGain,
                TransactionIds = next.TransactionIds // preserve original shared-list optimization
            };
        }

        int differenceCount = CountTransactionDifference(current.TransactionIds, next.TransactionIds);
        if ((long)differenceCount * _minimumTransactionProfit + current.Gain > _lowerGainThreshold)
            return null;

        ItemPostingList difference = BuildTransactionDifference(next, current);
        difference.Gain += current.Gain;
        if (difference.Gain > _lowerGainThreshold)
            return null;
        difference.ItemId = next.ItemId;
        return difference;
    }

    private static bool HaveDisjointTransactionRanges(
        List<int> first, List<int> second)
    {
        return first.Count == 0 || second.Count == 0 ||
               first[^1] < second[0] || second[^1] < first[0];
    }

    // Hot-path inputs are concrete List<int> values; no data copies are made.
    private static int CountTransactionDifference(
        List<int> excludedIds, List<int> candidateIds)
    {
        int differenceCount = 0;
        int excludedIndex = 0;
        int candidateIndex = 0;
        while (candidateIndex < candidateIds.Count && excludedIndex < excludedIds.Count)
        {
            if (excludedIds[excludedIndex] < candidateIds[candidateIndex])
                excludedIndex++;
            else if (excludedIds[excludedIndex] == candidateIds[candidateIndex])
            {
                excludedIndex++;
                candidateIndex++;
            }
            else
            {
                differenceCount++;
                candidateIndex++;
            }
        }
        return differenceCount + (candidateIds.Count - candidateIndex);
    }

    // Computes next.TransactionIds minus current.TransactionIds, including
    // the sum of profits of transactions in that difference.
    private ItemPostingList BuildTransactionDifference(
        ItemPostingList next, ItemPostingList current)
    {
        var difference = new ItemPostingList();
        int excludedIndex = 0;
        int candidateIndex = 0;
        List<int> excluded = current.TransactionIds;
        List<int> candidate = next.TransactionIds;

        while (candidateIndex < candidate.Count && excludedIndex < excluded.Count)
        {
            if (excluded[excludedIndex] < candidate[candidateIndex])
                excludedIndex++;
            else if (excluded[excludedIndex] == candidate[candidateIndex])
            {
                excludedIndex++;
                candidateIndex++;
            }
            else
            {
                int transactionId = candidate[candidateIndex++];
                difference.Gain += _transactionProfits[transactionId - 1];
                difference.TransactionIds.Add(transactionId);
            }
        }
        while (candidateIndex < candidate.Count)
        {
            int transactionId = candidate[candidateIndex++];
            difference.Gain += _transactionProfits[transactionId - 1];
            difference.TransactionIds.Add(transactionId);
        }
        return difference;
    }

    private MiningStageResult CaptureStage(
        int stageIndex, MiningStageType stageType, Stopwatch stopwatch)
    {
        return new MiningStageResult(
            stageIndex,
            stageType,
            _erasableItemsetCount,
            stopwatch.Elapsed.TotalSeconds,
            MeasureProcessMemoryMegabytes(peak: false));
    }

    private static double MeasureProcessMemoryMegabytes(bool peak)
    {
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        return (peak ? process.PeakWorkingSet64 : process.WorkingSet64) / 1_000_000.0;
    }
}

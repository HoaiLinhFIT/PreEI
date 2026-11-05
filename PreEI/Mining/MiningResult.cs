namespace PreEIApplication.Mining;

internal enum MiningStageType
{
    InitialMining,
    IncrementalUpdate,
    FullRescan
}

internal sealed record MiningStageResult(
    int StageIndex,
    MiningStageType StageType,
    long ErasableItemsetCount,
    double RuntimeSeconds,
    double WorkingSetMegabytes);

internal sealed record MiningRunResult(
    IReadOnlyList<MiningStageResult> Stages,
    double TotalRuntimeSeconds,
    double PeakWorkingSetMegabytes);

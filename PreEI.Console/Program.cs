using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using PreEIApplication.Mining;

namespace PreEIApplication;

/// <summary>
/// Noninteractive entry point for GitHub Actions. The R9 mining engine and
/// DataSplitter are linked as original source files, without code changes.
/// Run "split" and "mine" in two separate processes so data preparation does
/// not inflate the mining process's lifetime peak-working-set measurement.
/// </summary>
internal static class Program
{
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length == 0 || arguments[0] is "--help" or "-h")
            {
                PrintUsage();
                return arguments.Length == 0 ? 2 : 0;
            }

            string command = arguments[0].ToLowerInvariant();
            Dictionary<string, string> options = ReadOptions(arguments);
            switch (command)
            {
                case "split":
                    SplitDataset(options);
                    return 0;
                case "mine":
                    MineDataset(options);
                    return 0;
                default:
                    throw new ArgumentException($"Unknown command: {arguments[0]}");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"PreEI failed: {exception}");
            return 1;
        }
    }

    private static Dictionary<string, string> ReadOptions(string[] arguments)
    {
        if ((arguments.Length - 1) % 2 != 0)
            throw new ArgumentException("Every option must have a value. Use --help for syntax.");

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 1; index < arguments.Length; index += 2)
        {
            string option = arguments[index];
            if (!option.StartsWith("--", StringComparison.Ordinal) || option.Length <= 2)
                throw new ArgumentException($"Invalid option: {option}");
            if (!options.TryAdd(option[2..], arguments[index + 1]))
                throw new ArgumentException($"Duplicate option: {option}");
        }
        return options;
    }

    private static string Required(Dictionary<string, string> options, string name)
    {
        if (!options.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Missing --{name}. Use --help for syntax.");
        return value;
    }

    private static double ParsePercentage(Dictionary<string, string> options, string name)
    {
        if (!double.TryParse(Required(options, name), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double percentage) || !double.IsFinite(percentage))
            throw new ArgumentException($"--{name} must be a finite decimal percentage (use a dot).");
        return percentage;
    }

    private static void SplitDataset(Dictionary<string, string> options)
    {
        string dataset = Path.GetFullPath(Required(options, "dataset"));
        string partsDirectory = Path.GetFullPath(Required(options, "parts"));
        DataSplitter.SplitIntoIncrementalBatches(dataset, partsDirectory);
        Console.WriteLine($"Split finished: {Path.GetFileName(dataset)} -> {partsDirectory}");
        Console.WriteLine("Fixed split: 80% initial + ten increments of 2% each (11 parts).");
    }

    private static void MineDataset(Dictionary<string, string> options)
    {
        string dataset = Path.GetFullPath(Required(options, "dataset"));
        string partsDirectory = Path.GetFullPath(Required(options, "parts"));
        string resultDirectory = Path.GetFullPath(Required(options, "output"));
        double lowerPercentage = ParsePercentage(options, "lower");
        double upperPercentage = ParsePercentage(options, "upper");
        if (!File.Exists(dataset))
            throw new FileNotFoundException("Dataset not found", dataset);
        if (!(lowerPercentage > 0 && lowerPercentage < upperPercentage))
            throw new ArgumentException("Percentages must satisfy 0 < lower < upper.");

        IReadOnlyList<string> batchPaths = DataSplitter.GetBatchFilePaths(dataset, partsDirectory);
        if (batchPaths.Count != DataSplitter.PartCount || batchPaths.Any(path => !File.Exists(path)))
            throw new FileNotFoundException("Missing one or more of the 11 split files. Run the split command first.");

        // Do not create output files, enumerate results or produce metadata until
        // MineIncrementalDataset has frozen all runtime and memory measurements.
        MiningRunResult result = new PreEIMiningEngine().MineIncrementalDataset(
            batchPaths, lowerPercentage, upperPercentage);

        Directory.CreateDirectory(resultDirectory);
        string stem = "PreEI_" + Path.GetFileNameWithoutExtension(dataset) + "_results";
        string csvPath = Path.Combine(resultDirectory, stem + ".csv");
        string textPath = Path.Combine(resultDirectory, stem + ".txt");
        string metadataPath = Path.Combine(resultDirectory, "run_metadata.json");

        WriteCsv(result, csvPath);
        WriteText(result, textPath, dataset, lowerPercentage, upperPercentage);
        WriteMetadata(result, metadataPath, dataset, lowerPercentage, upperPercentage, batchPaths);

        Console.WriteLine("Mining complete (R9 engine; output export excluded from measurements).");
        foreach (MiningStageResult stage in result.Stages)
            Console.WriteLine($"Stage {stage.StageIndex,2}: {stage.StageType,-17} " +
                $"itemsets={stage.ErasableItemsetCount,10:N0} " +
                $"time={stage.RuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture)} s");
        Console.WriteLine("Total mining runtime: " +
            result.TotalRuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture) + " s");
        Console.WriteLine("Process peak working set: " +
            result.PeakWorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture) + " MB");
        Console.WriteLine($"CSV: {csvPath}");
        Console.WriteLine($"TXT: {textPath}");
        Console.WriteLine($"Metadata: {metadataPath}");
    }

    private static void WriteCsv(MiningRunResult result, string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("stage,type,erasable_itemsets,runtime_seconds,working_set_mb");
        foreach (MiningStageResult stage in result.Stages)
            writer.WriteLine(string.Join(",",
                stage.StageIndex.ToString(CultureInfo.InvariantCulture),
                stage.StageType.ToString(),
                stage.ErasableItemsetCount.ToString(CultureInfo.InvariantCulture),
                stage.RuntimeSeconds.ToString("R", CultureInfo.InvariantCulture),
                stage.WorkingSetMegabytes.ToString("R", CultureInfo.InvariantCulture)));
        writer.WriteLine("TOTAL,,," +
            result.TotalRuntimeSeconds.ToString("R", CultureInfo.InvariantCulture) + "," +
            result.PeakWorkingSetMegabytes.ToString("R", CultureInfo.InvariantCulture));
    }

    private static void WriteText(
        MiningRunResult result, string path, string dataset,
        double lowerPercentage, double upperPercentage)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("PreEI R9 - mining result");
        writer.WriteLine("Dataset: " + Path.GetFileName(dataset));
        writer.WriteLine($"Gain threshold (%): lower={lowerPercentage.ToString(CultureInfo.InvariantCulture)}, " +
            $"upper={upperPercentage.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine("Split: 80% initial + 10 x 2% increments");
        foreach (MiningStageResult stage in result.Stages)
            writer.WriteLine($"Stage {stage.StageIndex} ({stage.StageType}): " +
                $"itemsets={stage.ErasableItemsetCount.ToString(CultureInfo.InvariantCulture)}, " +
                $"runtime={stage.RuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture)} s, " +
                $"working_set={stage.WorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture)} MB");
        writer.WriteLine("Total mining runtime (s): " +
            result.TotalRuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture));
        writer.WriteLine("Overall peak working set (MB): " +
            result.PeakWorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture));
    }

    private static void WriteMetadata(
        MiningRunResult result, string path, string dataset,
        double lowerPercentage, double upperPercentage, IReadOnlyList<string> batches)
    {
        using var stream = File.OpenRead(dataset);
        string datasetSha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        var metadata = new
        {
            algorithm = "PreEI",
            source_version = "R9 (original mining source linked unchanged)",
            dataset = Path.GetFileName(dataset),
            dataset_sha256 = datasetSha256,
            threshold_unit = "percent",
            lower_threshold = lowerPercentage,
            upper_threshold = upperPercentage,
            initial_percentage = DataSplitter.InitialDatabasePercentage,
            incremental_percentage = DataSplitter.IncrementalBatchPercentage,
            incremental_batch_count = DataSplitter.IncrementalBatchCount,
            stages = result.Stages.Count,
            batch_transaction_counts = batches.Select(path => File.ReadLines(path).Count()).ToArray(),
            total_mining_runtime_seconds = result.TotalRuntimeSeconds,
            overall_peak_working_set_mb = result.PeakWorkingSetMegabytes,
            memory_unit = "decimal MB (1,000,000 bytes)",
            dotnet_runtime = RuntimeInformation.FrameworkDescription,
            operating_system = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            processor_count = Environment.ProcessorCount,
            github_commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
        };
        File.WriteAllText(path, JsonSerializer.Serialize(metadata,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void PrintUsage()
    {
        Console.WriteLine("PreEI R9 CLI (shares R9 engine sources; .NET 10)");
        Console.WriteLine("  split --dataset <profit_dataset.txt> --parts <parts_dir>");
        Console.WriteLine("  mine  --dataset <profit_dataset.txt> --parts <parts_dir> " +
            "--lower <percent> --upper <percent> --output <results_dir>");
        Console.WriteLine("Example: lower=1.05 upper=1.2 (values expressed as percentages).");
    }
}

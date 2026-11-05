using System.Globalization;

namespace PreEIApplication;

/// <summary>
/// Deterministic sequential split with the author's fixed 80% initial
/// database and ten 2% increments. Runs in a separate child process.
/// </summary>
internal static class DataSplitter
{
    internal const int InitialDatabasePercentage = 80;
    internal const int IncrementalBatchPercentage = 2;
    internal const int IncrementalBatchCount = 10;
    internal const int PartCount = IncrementalBatchCount + 1;

    internal static int[] CalculateBatchLineCounts(int totalLines)
    {
        if (totalLines <= 0)
            throw new InvalidDataException("The source dataset contains no transactions.");

        // Preserve the original C# float arithmetic and floor-plus-0.6 rule.
        float initialShare = InitialDatabasePercentage;
        float incrementalShare = IncrementalBatchPercentage;
        int[] batchLineCounts = new int[PartCount];

        batchLineCounts[0] = (int)Math.Floor(totalLines * initialShare / 100 + 0.6);
        int assignedLines = batchLineCounts[0];
        for (int partIndex = 1; partIndex < PartCount - 1; partIndex++)
        {
            batchLineCounts[partIndex] =
                (int)Math.Floor(totalLines * incrementalShare / 100 + 0.6);
            assignedLines += batchLineCounts[partIndex];
        }
        batchLineCounts[^1] = totalLines - assignedLines;
        if (batchLineCounts.Any(lineCount => lineCount <= 0))
            throw new InvalidDataException(
                "Dataset is too small for the fixed 80% + ten 2% split.");
        return batchLineCounts;
    }

    internal static IReadOnlyList<string> GetBatchFilePaths(
        string datasetPath, string outputDirectory)
    {
        string datasetName = Path.GetFileNameWithoutExtension(datasetPath);
        return Enumerable.Range(1, PartCount)
            .Select(partIndex => Path.Combine(outputDirectory, $"{datasetName}_part{partIndex}.txt"))
            .ToArray();
    }

    internal static void SplitIntoIncrementalBatches(
        string datasetPath, string outputDirectory)
    {
        if (!File.Exists(datasetPath))
            throw new FileNotFoundException("Input dataset does not exist.", datasetPath);
        Directory.CreateDirectory(outputDirectory);

        int totalLines = File.ReadLines(datasetPath).Count();
        int[] batchLineCounts = CalculateBatchLineCounts(totalLines);
        IReadOnlyList<string> batchPaths = GetBatchFilePaths(datasetPath, outputDirectory);

        using (var reader = new StreamReader(datasetPath))
        {
            for (int batchIndex = 0; batchIndex < PartCount; batchIndex++)
            {
                using var writer = new StreamWriter(batchPaths[batchIndex]);
                for (int lineIndex = 0; lineIndex < batchLineCounts[batchIndex]; lineIndex++)
                {
                    string? line = reader.ReadLine();
                    if (line is null)
                        throw new InvalidDataException("Dataset ended before all batches were written.");
                    writer.WriteLine(line);
                }
            }
            if (reader.ReadLine() is not null)
                throw new InvalidDataException("One or more transactions were omitted from the batches.");
        }

        // This manifest is not loaded by the mining algorithm.
        string manifestPath = Path.Combine(outputDirectory, "split_manifest.csv");
        using var manifestWriter = new StreamWriter(manifestPath);
        manifestWriter.WriteLine("part,checkpoint_percent,file_name,start_line,end_line,line_count");
        int firstLine = 1;
        for (int batchIndex = 0; batchIndex < batchLineCounts.Length; batchIndex++)
        {
            int lastLine = firstLine + batchLineCounts[batchIndex] - 1;
            int checkpoint = InitialDatabasePercentage +
                batchIndex * IncrementalBatchPercentage;
            manifestWriter.WriteLine(string.Join(",",
                batchIndex + 1,
                checkpoint.ToString(CultureInfo.InvariantCulture),
                Path.GetFileName(batchPaths[batchIndex]),
                firstLine,
                lastLine,
                batchLineCounts[batchIndex]));
            firstLine = lastLine + 1;
        }
    }
}

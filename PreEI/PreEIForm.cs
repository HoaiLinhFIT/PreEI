using System.Diagnostics;
using System.Globalization;
using PreEIApplication.Mining;

namespace PreEIApplication;

/// <summary>
/// UI only: dataset selection, parameter validation, and result presentation.
/// PreEI mining and re-scan rules are implemented in PreEIMiningEngine.
/// </summary>
public partial class PreEIForm : Form
{
    private IReadOnlyList<string> _orderedBatchPaths = Array.Empty<string>();
    private string? _selectedDatasetPath;
    private MiningRunResult? _lastRun;
    private double _lastLowerThresholdPercent;
    private double _lastUpperThresholdPercent;

    public PreEIForm()
    {
        InitializeComponent();
    }

    private void OnBrowseDatasetClick(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select a complete profit-augmented dataset",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string sourcePath = dialog.FileName;
        string outputDirectory = Path.Combine(
            Path.GetDirectoryName(sourcePath)!,
            Path.GetFileNameWithoutExtension(sourcePath) + "_PreEI_batches");
        try
        {
            Cursor = Cursors.WaitCursor;
            RunSplitterInSeparateProcess(sourcePath, outputDirectory);
            IReadOnlyList<string> batchPaths = DataSplitter.GetBatchFilePaths(sourcePath, outputDirectory);
            if (batchPaths.Count != DataSplitter.PartCount || batchPaths.Any(path => !File.Exists(path)))
                throw new IOException("The expected 11 ordered data files were not generated.");

            _orderedBatchPaths = batchPaths;
            _selectedDatasetPath = sourcePath;
            _lastRun = null;
            datasetPartsListBox.Items.Clear();
            foreach (string batchPath in batchPaths)
                datasetPartsListBox.Items.Add(Path.GetFileName(batchPath));
            resultsTreeView.Nodes.Clear();
            resultsTreeView.Nodes.Add($"Dataset: {Path.GetFileName(sourcePath)} (ready to run)");
            MessageBox.Show(this,
                "Generated 11 ordered batches: 80% initial + ten 2% increments." +
                Environment.NewLine + "Manifest: " + Path.Combine(outputDirectory, "split_manifest.csv"),
                "Dataset prepared", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            _orderedBatchPaths = Array.Empty<string>();
            _selectedDatasetPath = null;
            _lastRun = null;
            datasetPartsListBox.Items.Clear();
            resultsTreeView.Nodes.Clear();
            MessageBox.Show(this, exception.Message, "Data preparation failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void OnRunMiningClick(object sender, EventArgs e)
    {
        if (_orderedBatchPaths.Count != DataSplitter.PartCount)
        {
            MessageBox.Show(this, "Select one complete dataset before running PreEI.",
                "Dataset required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!double.TryParse(lowerThresholdTextBox.Text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double lowerThresholdPercent) ||
            !double.TryParse(upperThresholdTextBox.Text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double upperThresholdPercent) ||
            lowerThresholdPercent <= 0 || upperThresholdPercent <= lowerThresholdPercent)
        {
            MessageBox.Show(this,
                "Enter decimal percentages with 0 < lower < upper (e.g., 1.05 and 1.2).",
                "Invalid gain thresholds", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            runMiningButton.Enabled = false;
            // Mining completes and freezes runtime/memory measurements before
            // either TreeView rendering or CSV/TXT export can take place.
            _lastRun = new PreEIMiningEngine().MineIncrementalDataset(
                _orderedBatchPaths, lowerThresholdPercent, upperThresholdPercent);
            _lastLowerThresholdPercent = lowerThresholdPercent;
            _lastUpperThresholdPercent = upperThresholdPercent;
            RenderMiningResults(_lastRun, lowerThresholdPercent, upperThresholdPercent);
        }
        catch (Exception exception)
        {
            _lastRun = null;
            resultsTreeView.Nodes.Clear();
            MessageBox.Show(this, exception.ToString(), "PreEI mining failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            runMiningButton.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    // Like the original implementation: write plain top-level TreeView lines.
    // Do not create parent/child nodes or parse results back from the UI.
    private void RenderMiningResults(MiningRunResult result, double lowerPercent, double upperPercent)
    {
        resultsTreeView.BeginUpdate();
        try
        {
            resultsTreeView.Nodes.Clear();
            resultsTreeView.Nodes.Add("Dataset: " + Path.GetFileName(_selectedDatasetPath));

            foreach (MiningStageResult stage in result.Stages)
            {
                resultsTreeView.Nodes.Add($"Stage {stage.StageIndex}: {FormatStageType(stage.StageType)}");
                resultsTreeView.Nodes.Add("Runtime (s): " +
                    stage.RuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture));
                resultsTreeView.Nodes.Add("Erasable itemsets: " +
                    stage.ErasableItemsetCount.ToString("N0", CultureInfo.InvariantCulture));
                resultsTreeView.Nodes.Add("Working-set memory (MB): " +
                    stage.WorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture));
                resultsTreeView.Nodes.Add("----------------------------------");
            }

            resultsTreeView.Nodes.Add($"Gain thresholds (%): lower={lowerPercent.ToString(CultureInfo.InvariantCulture)}; " +
                $"upper={upperPercent.ToString(CultureInfo.InvariantCulture)}");
            resultsTreeView.Nodes.Add("Total mining runtime (s): " +
                result.TotalRuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture));
            resultsTreeView.Nodes.Add("Overall peak working set (MB): " +
                result.PeakWorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture));
        }
        finally
        {
            resultsTreeView.EndUpdate();
        }
    }

    private static string FormatStageType(MiningStageType stageType) => stageType switch
    {
        MiningStageType.InitialMining => "Initial mining",
        MiningStageType.IncrementalUpdate => "Incremental update",
        MiningStageType.FullRescan => "Full re-scan",
        _ => stageType.ToString()
    };

    // Export is intentionally outside MineIncrementalDataset and its Stopwatch.
    // CSV/TXT serialization uses only the previously captured MiningRunResult;
    // no process memory or elapsed time is measured while saving these files.
    private void OnSaveResultsClick(object sender, EventArgs e)
    {
        if (_lastRun is null)
        {
            MessageBox.Show(this, "Run PreEI before saving results.",
                "No results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string datasetName = Path.GetFileNameWithoutExtension(_selectedDatasetPath) ?? "dataset";
        using var dialog = new SaveFileDialog
        {
            Title = "Save PreEI results",
            Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt",
            FileName = $"PreEI_{datasetName}_results.csv",
            DefaultExt = "csv",
            AddExtension = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (Path.GetExtension(dialog.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllLines(dialog.FileName, CreateTextReport(
                _lastRun, _selectedDatasetPath,
                _lastLowerThresholdPercent, _lastUpperThresholdPercent));
        }
        else
        {
            using var writer = new StreamWriter(dialog.FileName);
            writer.WriteLine("stage,type,erasable_itemsets,runtime_seconds,working_set_mb");
            foreach (MiningStageResult stage in _lastRun.Stages)
            {
                writer.WriteLine(string.Join(",",
                    stage.StageIndex,
                    stage.StageType,
                    stage.ErasableItemsetCount,
                    stage.RuntimeSeconds.ToString("R", CultureInfo.InvariantCulture),
                    stage.WorkingSetMegabytes.ToString("R", CultureInfo.InvariantCulture)));
            }
            writer.WriteLine("TOTAL,,," +
                _lastRun.TotalRuntimeSeconds.ToString("R", CultureInfo.InvariantCulture) + "," +
                _lastRun.PeakWorkingSetMegabytes.ToString("R", CultureInfo.InvariantCulture));
        }
        MessageBox.Show(this, "Results saved successfully.");
    }

    // Generate the text report from measured data, not from screen widgets.
    private static IEnumerable<string> CreateTextReport(
        MiningRunResult result, string? datasetPath,
        double lowerThresholdPercent, double upperThresholdPercent)
    {
        yield return "Dataset: " + Path.GetFileName(datasetPath);
        yield return $"Gain thresholds (%): lower={lowerThresholdPercent.ToString(CultureInfo.InvariantCulture)}, " +
            $"upper={upperThresholdPercent.ToString(CultureInfo.InvariantCulture)}";
        foreach (MiningStageResult stage in result.Stages)
        {
            yield return $"Stage {stage.StageIndex} ({FormatStageType(stage.StageType)}): " +
                $"itemsets={stage.ErasableItemsetCount.ToString(CultureInfo.InvariantCulture)}, " +
                $"runtime={stage.RuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture)} s, " +
                $"working_set={stage.WorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture)} MB";
        }
        yield return "Total mining runtime (s): " +
            result.TotalRuntimeSeconds.ToString("F6", CultureInfo.InvariantCulture);
        yield return "Overall peak working set (MB): " +
            result.PeakWorkingSetMegabytes.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static void RunSplitterInSeparateProcess(
        string sourcePath, string outputDirectory)
    {
        string executablePath = Environment.ProcessPath ??
            throw new InvalidOperationException("Unable to locate the PreEI executable.");
        var processStart = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        // When started as 'dotnet PreEI.dll', Environment.ProcessPath is dotnet.exe.
        if (Path.GetFileNameWithoutExtension(executablePath)
            .Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            processStart.ArgumentList.Add(typeof(Program).Assembly.Location);

        processStart.ArgumentList.Add("--split");
        processStart.ArgumentList.Add(sourcePath);
        processStart.ArgumentList.Add(outputDirectory);

        using Process process = Process.Start(processStart) ??
            throw new IOException("Failed to start the dataset splitter.");
        string errorDetails = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new IOException("Dataset splitter failed: " + errorDetails);
    }

    private void OnCloseClick(object sender, EventArgs e) => Close();
}

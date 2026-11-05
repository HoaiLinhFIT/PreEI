namespace PreEIApplication;

internal static class Program
{
    [STAThread]
    private static void Main(string[] arguments)
    {
        if (arguments.Length > 0 && arguments[0] == "--split")
        {
            try
            {
                if (arguments.Length != 3)
                    throw new ArgumentException(
                        "Usage: PreEI.exe --split <dataset.txt> <output_dir> " +
                        "(fixed 80% + ten 2% batches)");
                DataSplitter.SplitIntoIncrementalBatches(arguments[1], arguments[2]);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                Environment.ExitCode = 1;
            }
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new PreEIForm());
    }
}

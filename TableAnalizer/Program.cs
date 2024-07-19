// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить Autofac

// /Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles
// /Users/dmitrijsamarcev/Library/Mobile Documents/com~apple~CloudDocs/MPBoost/Клиенты/Юсупов 79164372997 13465

#nullable enable

using System.Reflection;
using OfficeOpenXml;
using TableAnalizer.Enums;
using TableAnalizer.Objects.Managers;
using static Tensorflow.Binding;

internal class Program
{
    #region Variables and constants
    private static readonly Version? AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
    private static string goodByMessage = "\nСпасибо за использование программы. Хорошего дня!";
    private static readonly string failureMessage = "Некорретные данные, попробуйте еще раз.";
    #endregion

    #region Properties 
    private static readonly FileManager fileManager = new();
    private static readonly ExcelManager excelManager = new();
    private static readonly SKUManager skuManager = new(excelManager);

    #endregion

    #region Functionality
    private static async Task Main(string[] args)
    {
        //await Task.Delay(100);

        // var hello = tf.constant("Hello, TensorFlow!");
        // Console.WriteLine(hello);
        // Console.ReadKey();

        await AnalyzeСlientReports();
    }

    private static async Task AnalyzeСlientReports()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        fileManager.ManagerReached += Report;
        excelManager.ManagerReached += Report;
        skuManager.ManagerReached += Report;

        var helloString = $"" +
            $"--------------------------------------------\n" +
            $"Отчета по взаиморасчетам с клиентом логистики\n" +
            $"Версия: {AppVersion}\n" +
            $"---------------------------------------------";

        Console.WriteLine(helloString);

        // Set deadline days
        string deadLineDaysTitleMessage = "Введите срок дедлайна для клиента и нажмите 'Enter'\n(положительное целое число, отличное от 0): ";
        string deadLineDaysSuccessMessage = "Срок дедлайна для текущего клиента, дней";
        int deadLineDays = await GetUserIntInfo(deadLineDaysTitleMessage, deadLineDaysSuccessMessage, failureMessage);
        skuManager.SetDeadlineDays(deadLineDays);

        await Task.Delay(500);
        Console.WriteLine();

        // Set source reports path
        string reportsPathTitleMessage = "Укажите путь к директории c исходными отчетами клиента и нажмите 'Enter':\n ";
        string reportsPathSuccessMessage = "Директория c отчетами клиента:\n";
        string reportsPath = await GetUserStringInfo(reportsPathTitleMessage, reportsPathSuccessMessage, failureMessage);

        // Availability necessary reports
        Task<List<SourceSKUReportFileKey>?> availabilityNecessaryTask = fileManager.ChecksAvailabilityNecessaryFiles(reportsPath);
        await Task.Run(async () => await availabilityNecessaryTask);
        availabilityNecessaryTask.Wait();

        // Report analysis tasks
        var existingKeys = availabilityNecessaryTask.Result;
        if (existingKeys is not null && existingKeys.Count > 0)
        {
            // Ransoms tasks
            List<Task> ransomsTasksList = new();
            Task[] ransomsTasks = Array.Empty<Task>();
            if (existingKeys.Contains(SourceSKUReportFileKey.RansomsWB))
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsWB));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RansomsOZ))
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsOZ));
            }

            if (ransomsTasksList.Any())
            {
                ransomsTasks = ransomsTasksList.ToArray();

                for (int i = 0; i < ransomsTasks.Length; i++)
                {
                    var currentTask = ransomsTasks[i];
                    if (currentTask is not null)
                    {
                        await Task.Run(async () => await currentTask);
                    }
                }
            }

            if (ransomsTasks.Any())
            {
                Task.WaitAll(ransomsTasks);
            }

            // Remaining tasks
            List<Task> remainingTasksList = new();
            Task[] remainingTasks = Array.Empty<Task>();
            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingWB))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingWB));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingOZ))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingOZ));
            }

            if (remainingTasksList.Any())
            {
                remainingTasks = remainingTasksList.ToArray();
                for (int i = 0; i < remainingTasks.Length; i++)
                {
                    var currentTask = remainingTasks[i];
                    if (currentTask is not null)
                    {
                        await Task.Run(async () => await currentTask);
                    }
                }

                Task.WaitAll(remainingTasks);
            }

            if (remainingTasks.Any())
            {
                Task.WaitAll(remainingTasks);
            }

            // WriteOff task 
            if (existingKeys.Contains(SourceSKUReportFileKey.WriteOffShortages))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.WriteOffShortages);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Shipped tasks
            if (existingKeys.Contains(SourceSKUReportFileKey.Shipped))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Shipped);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Transfer task
             if (existingKeys.Contains(SourceSKUReportFileKey.Transfer))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Transfer);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Additional tasks
            if (existingKeys.Contains(SourceSKUReportFileKey.Additional))
            {
                var additionalTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Additional);
                await Task.Run(async () => await additionalTask);
                additionalTask.Wait();
            }

            // Delete old logistic report 
            FileManager.DeleteExistingFile(FileManager.OutputClientReportFilePath);
            var skusForTargetReport = skuManager.MakeSkusForTargetReport();
            excelManager.MakeReports(skusForTargetReport);

            // Report 
            var completedMessage = $"Отчет логистики сохранен по пути: {FileManager.OutputClientReportFilePath}\n";
            goodByMessage = string.Concat(completedMessage, goodByMessage);
            await Report(goodByMessage, 2000, true);
        }
        else
        {
            await Report(string.Concat("Нет отчетов, доступных для анализа\n", goodByMessage), 2000, true);
        }

        await Exit();
    }

    /// <summary>
    /// Receiving and processing data from seller reports.
    /// </summary>
    /// <param name="reportFileKey">Key of source file with report.</param>
    /// <returns></returns>
    /// <remarks><i>New version</i></remarks>
    private static async Task<double> ReceivingAndProcessingData(SourceSKUReportFileKey reportFileKey)
    {
        var getReportFileTask = fileManager.GetReportFile(reportFileKey);
        await Task.Run(async () => await getReportFileTask);
        getReportFileTask.Wait();

        var reportFile = getReportFileTask.Result;

        if (reportFile is not null)
        {
            // Make ExcelPackage
            ExcelManager.CreateExcelPackage(reportFileKey, reportFile.FileInfo);

            // ReceivingAndProcessing
            Task<double>? processingRansomsDataTask = skuManager.ReceivingAndProcessingData(reportFileKey);
            if(processingRansomsDataTask is not null) 
            {
                await Task.Run(async () => await processingRansomsDataTask);
                processingRansomsDataTask.Wait();
            }
  
            // Dispose package
            if(ExcelManager.GetExcelPackageByKey(reportFileKey, out ExcelPackage? existingPackage)) 
            {
                existingPackage?.Dispose();
            }

            // Return time
            return processingRansomsDataTask?.Result ?? 0.00;
        }
        else
        {
            Console.WriteLine($"Нет отчета по ключу {reportFileKey}");
            return 0;
        }
    }

    private static async Task Exit()
    {
        skuManager.ManagerReached -= Report;
        excelManager.ManagerReached -= Report;
        fileManager.ManagerReached -= Report;

        await Task.Delay(500);

        Environment.Exit(0);
    }

    /// <summary>
    /// Receives data from user from console as a string.
    /// </summary>
    /// <param name="titleMessage">Title message.</param>
    /// <param name="successMessage">Message to user if successful.</param>
    /// <param name="failureMessage">Message to user in case of failure.</param>
    /// <returns>Received data as a string.</returns>
    /// <remarks><i>New version ok</i></remarks>
    private static async Task<string> GetUserStringInfo(string titleMessage, string successMessage, string failureMessage)
    {
        string? resultString = string.Empty;
        while (resultString == string.Empty)
        {
            Console.WriteLine(titleMessage);

            resultString = Console.ReadLine();

            if (!string.Equals(string.Empty, resultString))
            {
                await Report($"{successMessage}: {resultString}");
                break;
            }
            else
            {
                await Report(failureMessage);
                resultString = default;
                continue;
            }
        }

        return resultString!;
    }

    private static async Task<int> GetUserIntInfo(string titleMessage, string successMessage, string failureMessage)
    {
        int resultValue = default;
        while (resultValue == default)
        {
            Console.WriteLine(titleMessage);

            if (int.TryParse(Console.ReadLine(), out resultValue))
            {
                if (resultValue > 0)
                {
                    await Report($"{successMessage}: {resultValue}");
                    break;
                }
                else
                {
                    await Report(failureMessage);
                    resultValue = default;
                    continue;
                }
            }
        }

        return resultValue;
    }

    private static void Report(object? sender, ManagerReachedEventArgs e)
    {
        Console.WriteLine();
        Console.WriteLine(e.Message);
        Console.WriteLine();

        Task.Delay(e.Delay);

        if (e.Exit)
            Task.Run(Exit);
    }

    private static async Task Report(string message, int delay = 0, bool exit = false)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine();

        await Task.Delay(delay);

        if (exit)
            await Exit();
    }

    #endregion
}
// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить DI

// /Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles
// /Users/dmitrijsamarcev/Library/Mobile Documents/com~apple~CloudDocs/MPBoost/Клиенты/Юсупов 79164372997 13465

using System.Diagnostics;
using System.Reflection;
using System.Text;
using CSharpHelpers.Services;
using OfficeOpenXml;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.SKUItems;

internal class Program
{
    #region Variables and constants
    private static readonly Version? AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
    private static string goodByMessage = "\n Спасибо за использование программы. Хорошего дня!";
    private static bool? isRawReport = null;
    private static bool isMulty;
    private static readonly bool isTracing = true;
    private static readonly int tracingDelay = 1500;

    private const string TRACE_INFO_FLAG = "[INFO]: "; 
    private const string TRACE_ERROR_FLAG = "[ERROR]: "; 
    #endregion

    #region Properties 
    private static readonly ReportFileManager fileManager = new();
    private static readonly ExcelManager excelManager = new();
    private static readonly SKUManager skuManager = new();
    #endregion

    #region Functionality
    private static async Task Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        Trace.Listeners.Add(new ConsoleTraceListener());

        await Analyze(1000);

        Trace.Flush();
    }
    
    private static async Task Analyze(int delay)
    {
        ConsoleKey[] allowedConsoleKeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];

        fileManager.ManagerReached += Report;
        excelManager.ManagerReached += Report;
        skuManager.ManagerReached += Report;

        var helloString = $"------------------------------------------------------------------\n" +
                          $"Отчет по взаиморасчетам с клиентами логистики\n\n" +
                          $"Версия программы: {AppVersion}\n" +
                          $"------------------------------------------------------------------\n" +
                          "Выберите вариант отчета:\n" +
                          "1: Персональный отчет по взаиморасчетам с клиентом логистики\n" +
                          "2: Сводный отчет по взаиморасчетам c клиентами логистики\n" +
                          "ESC: Выход из программы \n" +
                          $"------------------------------------------------------------------";

        Console.Clear();

        Task? reportTask = null;
        ConsoleKey? selectKey = ConsoleAppService.GetUserKeyFromConsole(helloString);
        if(selectKey is null || !allowedConsoleKeys.Contains((ConsoleKey)selectKey)) 
        {
            Console.WriteLine("Выберите допустимую клавишу.");
            await Task.Delay(delay);
            await Analyze(delay);
        } 
        else 
        {
            switch (selectKey)
            {
                case ConsoleKey.D1:
                    isMulty = false;
                    break;
                case ConsoleKey.D2:
                    isMulty = true;
                    break;
                case ConsoleKey.Escape:
                    ConsoleAppService.ConsoleAppExit(goodByMessage);
                    break;
                default:
                    break;
            }

            Task<List<SourceSKUReportFileKey>?>? getDataTask = GetDataFromConsole(isMulty);
            await Task.Run(() => getDataTask);
            getDataTask.Wait();
            var keys = getDataTask.Result;

            switch (isMulty)
            {
                case true:
                    reportTask = AnalyzeMultiСlientReports(delay);
                    break;
                case false:
                    reportTask = AnalyzeSingleСlientReports(keys, delay);
                    break;
            }
        } 

        if(isRawReport is null) 
        {
            throw new Exception(nameof(isRawReport));
        } 
        else 
        {
            await TracingProgramExecution(TRACE_INFO_FLAG, "Report analysis task has started.");
            await Task.Run(() => reportTask);
        }
            
        reportTask?.Wait();
        fileManager.ManagerReached -= Report;
        excelManager.ManagerReached -= Report;
        skuManager.ManagerReached -= Report;
    }

    private static async Task AnalyzeSingleСlientReports(List<SourceSKUReportFileKey>? existingKeys, int delay) 
    {
        // Report analysis tasks
        if (existingKeys is not null && existingKeys.Count > 0)
        {
            // Ransoms tasks
            List<Task> ransomsTasksList = [];
            Task[] ransomsTasks = [];
            if (existingKeys.Contains(SourceSKUReportFileKey.RansomsWB))
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsWB, delay));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RansomsOZ))
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsOZ, delay));
            }

            if (ransomsTasksList.Count != 0)
            {
                ransomsTasks = [.. ransomsTasksList];

                for (int i = 0; i < ransomsTasks.Length; i++)
                {
                    var currentTask = ransomsTasks[i];
                    if (currentTask is not null)
                    {
                        await Task.Run(async () => await currentTask);
                    }
                }
            }

            if (ransomsTasks.Length != 0)
            {
                Task.WaitAll(ransomsTasks);
            }

            // Remaining tasks
            List<Task> remainingTasksList = [];
            Task[] remainingTasks = [];
            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingWB))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingWB, delay));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingOZ))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingOZ, delay));
            }

            if (remainingTasksList.Count != 0)
            {
                remainingTasks = [.. remainingTasksList];
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

            if (remainingTasks.Length != 0)
            {
                Task.WaitAll(remainingTasks);
            }

            // WriteOff task 
            if (existingKeys.Contains(SourceSKUReportFileKey.WriteOffShortages))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.WriteOffShortages, delay);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Shipped tasks
            if (existingKeys.Contains(SourceSKUReportFileKey.Shipped))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Shipped, delay);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Transfer task
            if (existingKeys.Contains(SourceSKUReportFileKey.Transfer))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Transfer, delay);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Additional tasks
            if (existingKeys.Contains(SourceSKUReportFileKey.Additional))
            {
                var additionalTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Additional, delay);
                await Task.Run(async () => await additionalTask);
                additionalTask.Wait();
            }

            // Delete old logistic report 
            if(isTracing) 
            {
                Trace.WriteLine($"Удаляю старый файлы по пути {ReportFileManager.OutputClientReportFilePath}...");
            }
            if(FileService.DeleteFileAtPath(ReportFileManager.OutputClientReportFilePath)) 
            {
                Trace.WriteLine("Старые файлы удалены.");
            } 
            else 
            {
                Trace.WriteLine("Ошибка в удалении старых файлов.");
                await Task.Delay(delay);
            }
            
            Task<List<SKUForTargetReports>>? skusForTargetReportTask = skuManager.MakeSkusForTargetReport((bool)isRawReport!, isMulty, delay);
            await Task.Run(() => skusForTargetReportTask);
            skusForTargetReportTask.Wait();

            if(isTracing) 
            {
                Trace.WriteLine($"Артикулов для итогового отчета: {skusForTargetReportTask.Result.Count}.");
            }

            // Save work 
            if(isTracing) 
            {
                Trace.WriteLine("Сохраняю текущую работу по отчету...");
            }
            Task<bool>? makeReportTask = excelManager.MakeReports(skusForTargetReportTask.Result, (bool)isRawReport,isTracing);
            await Task.Run(() => makeReportTask);
            makeReportTask.Wait();

            // Report 
            var completedMessage = $"Отчет логистики сохранен по пути: {ReportFileManager.OutputClientReportFilePath}\n";
            goodByMessage = string.Concat(completedMessage, goodByMessage);
            await Report(goodByMessage, 2000, true);
        }
        else
        {
            await Report(string.Concat("Нет отчетов, доступных для анализа\n", goodByMessage), 2000, true);
        }

            
        if(isTracing) 
        {
            Trace.WriteLine("Exiting the program");
        }
        await Exit();
    }

    private static async Task AnalyzeMultiСlientReports(int delay) 
    {   
        // Create and run analize task
        List<Task> summaryTasksList = [];
        foreach(var (key, value) in fileManager.ReportsPathsMultyReport) 
        {
            summaryTasksList.Add(ReceivingAndProcessingData(value, (bool)isRawReport!, isMulty, delay));
        }

        Console.Clear();

        Task[] summaryTasksArray = [.. summaryTasksList];
        for (int i = 0; i < summaryTasksArray.Length; i++)
        {
            var currentTask = summaryTasksArray[i];
            if (currentTask is not null)
            {
                await Task.Run(async () => await currentTask);
            }
        }

        Task.WaitAll(summaryTasksArray);

        // Additional tasks
        Task<double>? additionalTask = null;
        if (fileManager.AdditionalPathsMultyReport is not null && !(bool)isRawReport!)
        {
            Trace.WriteLine($"isMulty {isMulty}");
            additionalTask = ReceivingAndProcessingData(fileManager.AdditionalPathsMultyReport, (bool)isRawReport!, isMulty, delay);
            await Task.Run(async () => await additionalTask);
        }

        additionalTask?.Wait();

        // Delete old report 
        FileService.DeleteFileAtPath(ReportFileManager.OutputClientReportFilePath);

        // Make new report 
        Task<List<SKUForTargetReports>>? skusForTargetReportTask = skuManager.MakeSkusForTargetReport((bool)isRawReport!, isMulty, delay);
        await Task.Run(() => skusForTargetReportTask);
        skusForTargetReportTask.Wait();
        
        var saveWorkTask = excelManager.MakeReports(skusForTargetReportTask.Result, (bool)isRawReport, isTracing);
        await Task.Run(() => saveWorkTask);
        saveWorkTask.Wait();

        // Report 
        var completedMessage = $"Отчет логистики сохранен по пути: {ReportFileManager.OutputClientReportFilePath}\n";
        goodByMessage = string.Concat(completedMessage, goodByMessage);
        await Report(goodByMessage, 2000, true);

        await Exit();
    }

    private static async Task<double> ReceivingAndProcessingData(SourceReportFileForSKU reportFile, bool isRawReport, bool isMulty, int delay) 
    {
        if(reportFile.ReportKey is null) return 0.00;

        // Make ExcelPackage
        var package = new ExcelPackage(reportFile.FileInfo);
 
        // Receiving and processing data
        Task<double>? receivingAndProcessingDataTask = skuManager.ReceivingAndProcessingData((SourceSKUReportFileKey)reportFile.ReportKey, isRawReport, delay, package, isMulty);
        if(receivingAndProcessingDataTask is not null) 
        {
            await Task.Run(async () => await receivingAndProcessingDataTask);
        }
        receivingAndProcessingDataTask?.Wait();
        package.Dispose();

        return receivingAndProcessingDataTask?.Result ?? 0.00;
    }

    /// <summary>
    /// Receiving and processing data from seller reports.
    /// </summary>
    /// <param name="reportFileKey">Key of source file with report.</param>
    /// <returns></returns>
    /// <remarks><i>New version</i></remarks>
    private static async Task<double> ReceivingAndProcessingData(SourceSKUReportFileKey reportFileKey, int delay, SourceReportFileForSKU? existingReportFile = null, bool isMulty = false)
    {
        SourceReportFileForSKU? reportFile;
        if(existingReportFile == null) 
        {
            var getReportFileTask = fileManager.GetReportFile(reportFileKey);
            await Task.Run(async () => await getReportFileTask);
            getReportFileTask.Wait();

            reportFile = getReportFileTask.Result;
        } 
        else 
        {
            reportFile = existingReportFile;
        }

        if (reportFile is not null)
        {
            // Make ExcelPackage
            ExcelManager.CreateExcelPackage(reportFileKey, reportFile.FileInfo);

            // Receiving and processing data
            Task<double>? receivingAndProcessingDataTask = skuManager.ReceivingAndProcessingData(reportFileKey, (bool)isRawReport!, delay, null, isMulty);
            if(receivingAndProcessingDataTask is not null) 
            {
                await Task.Run(async () => await receivingAndProcessingDataTask);
                receivingAndProcessingDataTask.Wait();
            }
  
            // Dispose package
            if(ExcelManager.GetExcelPackageByKey(reportFileKey, out ExcelPackage? existingPackage)) 
            {
                existingPackage?.Dispose();
            }

            // Return time
            return receivingAndProcessingDataTask?.Result ?? 0.00;
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

        await Task.Delay(delay);

        if (exit)
            await Exit();
    }

    private static async Task<int> GetDeadlineDaysFromConsole()  
    {
        var deadlineRange = Enumerable.Range(14, 32);

        string deadLineDaysTitleMessage = $"Введите срок дедлайна для клиента и нажмите 'Enter'\n(положительное целое число, отличное от {deadlineRange.First()} до {deadlineRange.Last()}): ";
        
        string? numberString = ConsoleAppService.GetUserStringFromConsole(deadLineDaysTitleMessage);
        
        if(int.TryParse(numberString, out int numberResult) && deadlineRange.Contains(numberResult)) 
        {
            return numberResult;
        } 
        else 
        {
            await TracingProgramExecution(TRACE_ERROR_FLAG, "Invalid value, please try again.");
            return await GetDeadlineDaysFromConsole();
        }
    }

    private static async Task<DirectoryInfo?> GetSourceReportsPathFromConsole(int delay) 
    {
        string reportsPathTitleMessage = "Укажите путь к директории c исходными отчетами клиента и нажмите 'Enter':\n ";
        string reportsPathErrorMessage = "Путь к файлам отчетов некорректен.\nПовторите попытку.";    

        DirectoryInfo? reportsPathDirectoryInfo;
        string reportsPathString = ConsoleAppService.GetUserStringFromConsole(reportsPathTitleMessage);

        switch (string.IsNullOrEmpty(reportsPathString))
        {
            case true:
                await Report(reportsPathErrorMessage, delay, false);
                return await GetSourceReportsPathFromConsole(delay);
            case false:
                var isDirInfoSuccess = FileService.GetDirectoryInfo(reportsPathString, out reportsPathDirectoryInfo);
                switch(isDirInfoSuccess && reportsPathDirectoryInfo is not null) 
                {
                    case true:
                        return reportsPathDirectoryInfo;
                    case false:
                        await Report(reportsPathErrorMessage, delay, false);
                        return await GetSourceReportsPathFromConsole(delay); 
                }
        }
    }
    
    private static async Task<bool> GetCreateRawReportFromConsole(int delay)
    {
        StringBuilder rawReportStringBuilder = new("Do you want to create a \"raw\" report?\n");
        rawReportStringBuilder.Append($"1: Yes\n");
        rawReportStringBuilder.Append($"2: No\n");
        rawReportStringBuilder.Append($"ESC: Exit\n");
        var createRawReportString = rawReportStringBuilder.ToString();

        ConsoleKey[] validkeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];
        ConsoleKey? selectKey = ConsoleAppService.GetUserKeyFromConsole(createRawReportString);
        
        switch (selectKey is null || !validkeys.Contains((ConsoleKey)selectKey)) 
        {
            case true:
                await Report("Неверное значание, повторите попытку.");
                await Task.Delay(delay);
                return await GetCreateRawReportFromConsole(delay);
            case false: 
                switch((ConsoleKey)selectKey) 
                {
                    case ConsoleKey.D1:
                        return true;
                    case ConsoleKey.Escape:
                        ConsoleAppService.ConsoleAppExit(goodByMessage);
                        return false;
                    default:
                        return false;
                }
        }
    }
    
    private static async Task<List<SourceSKUReportFileKey>?> GetDataFromConsole(bool isMulty) 
    {
        // Get deadline days
        while(skuManager.DeadlineDays <= 0) 
        {
            Task<int> deadLineDaysTask = GetDeadlineDaysFromConsole();
            await Task.Run(() => deadLineDaysTask);
            deadLineDaysTask.Wait();
            skuManager.SetDeadlineDays(deadLineDaysTask.Result);
        }
        await TracingProgramExecution(TRACE_INFO_FLAG, $"Deadline set, days: {skuManager.DeadlineDays}");

        // Get source reports path
        DirectoryInfo? getSourceReportDirInfo = null;
        while(getSourceReportDirInfo is null) 
        {
            Task<DirectoryInfo?> getSourceReportsPathTask = GetSourceReportsPathFromConsole(tracingDelay);
            await Task.Run(() => getSourceReportsPathTask);
            getSourceReportsPathTask.Wait();
            getSourceReportDirInfo = getSourceReportsPathTask.Result;
        } 
        Console.Clear();
        await TracingProgramExecution(TRACE_INFO_FLAG, $"Reports path: {getSourceReportDirInfo.Name}");

        // Availability necessary reports
        List<SourceSKUReportFileKey>? existingKeys = null;
        while(existingKeys is null) 
        {
            Task<(List<SourceSKUReportFileKey>? keys, string message, bool isError)>?  availabilityNecessaryTask = 
                fileManager.ChecksAvailabilityNecessaryFiles(getSourceReportDirInfo.FullName, isMulty, isTracing, tracingDelay);
            await Task.Run(() => availabilityNecessaryTask);
            availabilityNecessaryTask.Wait();
            existingKeys = availabilityNecessaryTask.Result.keys;

            if(availabilityNecessaryTask.Result.isError) 
            {
                ConsoleAppService.ConsoleAppExit(availabilityNecessaryTask.Result.message);
            } 
            else 
            {
                await TracingProgramExecution(TRACE_INFO_FLAG, availabilityNecessaryTask.Result.message);
            }
        }

        Console.WriteLine("!!!");
        Console.ReadLine();

        // Get a "raw" report or not
        while(isRawReport is null) 
        {
            Task<bool> getCreateRawReportTask = GetCreateRawReportFromConsole(tracingDelay);
            await Task.Run(() => getCreateRawReportTask);
            getCreateRawReportTask.Wait();
            isRawReport = getCreateRawReportTask.Result;
        }
        Console.Clear();

        await TracingProgramExecution(TRACE_INFO_FLAG, $"This is raw report: {isRawReport}");

        return existingKeys; 
    }

    private static async Task TracingProgramExecution(string flag, string message) 
    {
        if(isTracing) 
        {
            switch (flag) 
            {
                case TRACE_INFO_FLAG:
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    break;
                case TRACE_ERROR_FLAG:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                default:
                    break;
            }

            Trace.WriteLine($"{flag}{message}");
            await Task.Delay(tracingDelay);
            Console.ResetColor();
        }
    }
    #endregion
}
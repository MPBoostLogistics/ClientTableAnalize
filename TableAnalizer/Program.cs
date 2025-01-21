// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить Autofac

// /Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles
// /Users/dmitrijsamarcev/Library/Mobile Documents/com~apple~CloudDocs/MPBoost/Клиенты/Юсупов 79164372997 13465

#nullable enable

using System.Reflection;
using System.Text;
using CSharpHelpers.Services;
using OfficeOpenXml;
using TableAnalizer.Objects.Enums;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.SKUItems;

internal class Program
{
    #region Variables and constants
    private static readonly Version? AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
    private static string goodByMessage = "\nСпасибо за использование программы. Хорошего дня!";
    private static bool isManualResorting;
    private static bool isAnalyzeResortsForWarehouse;
    #endregion

    #region Properties 
    private static readonly FileManager fileManager = new();
    private static readonly ExcelManager excelManager = new();
    private static readonly SKUManager skuManager = new(excelManager);
    #endregion

    #region Functionality
    private static async Task Main(string[] args)
    {
        await AnalyzeСlientReports();
    }

    private static async Task GetDeadlineDays() 
    {
        string deadLineDaysTitleMessage = "Введите срок дедлайна для клиента и нажмите 'Enter'\n(положительное целое число, отличное от 0): ";
       
        var deadLineDays = ConsoleAppService.GetUserStringFromConsole(deadLineDaysTitleMessage);
        
        if(int.TryParse(deadLineDays, out int deadLineDaysNumber) && deadLineDaysNumber > 0) 
        {
            skuManager.SetDeadlineDays(deadLineDaysNumber);
            await Report($"Срок дедлайна для взаиморасчетов, дней: {deadLineDaysNumber}");
        } 
        else 
        {
            await Report($"Неверное число, введите заново.");
            await Task.Delay(1500);
            await GetDeadlineDays(); 
        } 
    }

    private static async Task<DirectoryInfo> GetSourceReportsPath() 
    {
        string reportsPathTitleMessage = "Укажите путь к директории c исходными отчетами клиента и нажмите 'Enter':\n ";
        string reportsPathErrorMessage = "Путь к файлам отчетов некорректен.\nПовторите попытку.";    
        string reportsPathSuccessMessage = "Путь к файлам отчетов: \n";

        var reportsPath = ConsoleAppService.GetUserStringFromConsole(reportsPathTitleMessage);

        switch (string.IsNullOrEmpty(reportsPath))
        {
            case true:
                await Report(reportsPathErrorMessage);
                break;
            case false:
                var isDirInfoSuccess = FileService.GetDirectoryInfo(reportsPath, out DirectoryInfo? directoryInfo);
                switch(isDirInfoSuccess && directoryInfo is not null) 
                {
                    case true:
                        await Report(reportsPathSuccessMessage + $"{directoryInfo.FullName}");
                        return directoryInfo;
                    case false:
                        await Report(reportsPathErrorMessage);
                        break;
                }
                break;
        }

        await Task.Delay(1500);
        return await GetSourceReportsPath();
    }

    private static async Task GetUseManualRegranding()
    {
        ConsoleKey[] validkeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];

        StringBuilder manualResortsStringBuilder = new("Take into account resorts specified manually?\n");
        manualResortsStringBuilder.Append($"1: Yes\n");
        manualResortsStringBuilder.Append($"2: No\n");
        manualResortsStringBuilder.Append($"ESC: Exit\n");
        var manualResortsString = manualResortsStringBuilder.ToString();

        ConsoleKey? selectKey;
        do
        {
            selectKey = ConsoleAppService.GetUserKeyFromConsole(manualResortsString);
        } while (selectKey is null || !validkeys.Contains((ConsoleKey)selectKey));

        switch (selectKey)
        {
            case ConsoleKey.D1:
                isManualResorting = true;
                break;
            case ConsoleKey.D2:
                isManualResorting = false;
                break;
            case ConsoleKey.Escape:
                ConsoleAppService.ConsoleAppExit("Thank you for using app. Have a nice day!");
                break;
        }

        await Task.Delay(1500);
    }

    private static void GetUseInfoForWarehouse()
    {
        ConsoleKey[] validkeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];

        StringBuilder analyzeResortsForWarehouseBuilder = new("Analyze re-sorts for warehouse?\n");
        analyzeResortsForWarehouseBuilder.Append($"1: Yes\n");
        analyzeResortsForWarehouseBuilder.Append($"2: No\n");
        analyzeResortsForWarehouseBuilder.Append($"ESC: Exit\n");
        var analyzeResortsForWarehouseString = analyzeResortsForWarehouseBuilder.ToString();

        ConsoleKey? selectKey;
        do
        {
            selectKey = ConsoleAppService.GetUserKeyFromConsole(analyzeResortsForWarehouseString);
        } while (selectKey is null || !validkeys.Contains((ConsoleKey)selectKey));

        switch (selectKey)
        {
            case ConsoleKey.D1:
                isAnalyzeResortsForWarehouse = true;
                break;
            case ConsoleKey.D2:
                isAnalyzeResortsForWarehouse = false;
                break;
            case ConsoleKey.Escape:
                ConsoleAppService.ConsoleAppExit("Thank you for using app. Have a nice day!");
                break;
        }
    }
    
    private static async Task<List<SourceSKUReportFileKey>?> GetDataFromUserInConsole(bool isMulty) 
    {
        // Get deadline days
        Task deadLineDaysTask = Task.Run(GetDeadlineDays);
        deadLineDaysTask.Wait();
        Console.WriteLine();

        // Get source reports path
        Task<DirectoryInfo> getSourceReportsPathTask = Task.Run(GetSourceReportsPath);
        getSourceReportsPathTask.Wait();
        var getSourceReportDirInfo = getSourceReportsPathTask.Result;
        Console.WriteLine();

        // Availability necessary reports
        Task<List<SourceSKUReportFileKey>?> availabilityNecessaryTask = fileManager.ChecksAvailabilityNecessaryFiles(getSourceReportDirInfo.FullName, isMulty);
        await Task.Run(async () => await availabilityNecessaryTask);
        availabilityNecessaryTask.Wait();
        var existingKeys = availabilityNecessaryTask.Result;
        Console.WriteLine();

        // Get analyze resorts for warehouse
        Task analizeResortsTask = Task.Run(() => GetUseInfoForWarehouse());
        analizeResortsTask.Wait();
        Console.WriteLine();

        // Get use manual regranding
        if(existingKeys is not null && existingKeys.Contains(SourceSKUReportFileKey.Additional))
        {
            Task useManualRegrandingTask = Task.Run(() => GetUseManualRegranding());
            useManualRegrandingTask.Wait();
        } 

        Console.Clear();

        isManualResorting = true;

        return existingKeys;
    }
    
    private static async Task AnalyzeСlientReports()
    {
        await Task.Delay(1);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        fileManager.ManagerReached += Report;
        excelManager.ManagerReached += Report;
        skuManager.ManagerReached += Report;

        var helloString = $"--------------------------------------------\n" +
                          $"Отчет по взаиморасчетам с клиентами логистики\n" +
                          $"Версия: {AppVersion}\n" +
                          $"---------------------------------------------\n" +
                          "Выберите вариант отчета:\n" +
                          "1: Отчет по взаиморасчетам с клиентом логистики\n" +
                          "2: Сводный отчет по нескольким клиентам\n" +
                          "Другая клавиша: Выход из программы \n" +
                          $"---------------------------------------------";

        ConsoleKey? selectKey = ConsoleAppService.GetUserKeyFromConsole(helloString);
        Task choiseTask = selectKey switch
        {
            ConsoleKey.D1 => Task.Run(() => AnalyzeSingleСlientReports()),
            ConsoleKey.D2 => Task.Run(() => AnalyzeMultiСlientReports()),
            _ => Task.Run(() => Report(goodByMessage, 200, true)),
        };

        choiseTask.Wait();
    }

    private static async Task AnalyzeSingleСlientReports() 
    {
        // Get data from console
        var getExistingKeysTask = GetDataFromUserInConsole(false);
        await Task.Run(() => getExistingKeysTask);
        getExistingKeysTask.Wait();
        var existingKeys = getExistingKeysTask.Result;

        // Report analysis tasks
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

            if (ransomsTasks.Length != 0)
            {
                Task.WaitAll(ransomsTasks);
            }

            // Remaining tasks
            List<Task> remainingTasksList = [];
            Task[] remainingTasks = [];
            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingWB))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingWB));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingOZ))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingOZ));
            }

            if (remainingTasksList.Count != 0)
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

            if (remainingTasks.Length != 0)
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
            var skusForTargetReport = skuManager.MakeSkusForTargetReport(isAnalyzeResortsForWarehouse);
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

    private static async Task AnalyzeMultiСlientReports() 
    {
        // Get data from console
        var getExistingKeysTask = GetDataFromUserInConsole(true);
        await Task.Run(() => getExistingKeysTask);
        getExistingKeysTask.Wait();
        
        // Create and run analize task
        List<Task> summaryTasksList = [];
        foreach(var (key, value) in fileManager.ReportsPathsMultyReport) 
        {
            summaryTasksList.Add(ReceivingAndProcessingData(value, true));
        }

       //Console.Clear();

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
        if (fileManager.AdditionalPathsMultyReport is not null)
        {
            var additionalTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Additional, fileManager.AdditionalPathsMultyReport);
            await Task.Run(async () => await additionalTask);
            additionalTask.Wait();
        }

        // Delete old report 
        FileManager.DeleteExistingFile(FileManager.OutputClientReportFilePath);

        // Make new report 
        var skusForTargetReport = skuManager.MakeSkusForTargetReport(isAnalyzeResortsForWarehouse);
        excelManager.MakeReports(skusForTargetReport);

        // Report 
        var completedMessage = $"Отчет логистики сохранен по пути: {FileManager.OutputClientReportFilePath}\n";
        goodByMessage = string.Concat(completedMessage, goodByMessage);
        await Report(goodByMessage, 2000, true);

        await Exit();
    }

    private static async Task<double> ReceivingAndProcessingData(SourceReportFileForSKU reportFile, bool isMulty = true) 
    {
        if(reportFile.ReportKey is null)
            return 0.00;

        // Make ExcelPackage
        var package = new ExcelPackage(reportFile.FileInfo);
 
        // Receiving and processing data
        Task<double>? receivingAndProcessingDataTask = skuManager.ReceivingAndProcessingData((SourceSKUReportFileKey)reportFile.ReportKey, isManualResorting, package, isMulty);
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
    private static async Task<double> ReceivingAndProcessingData(SourceSKUReportFileKey reportFileKey, SourceReportFileForSKU? existingReportFile = null, bool isMulty = false)
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
            Task<double>? receivingAndProcessingDataTask = skuManager.ReceivingAndProcessingData(reportFileKey, isManualResorting, null, isMulty);
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

    #endregion
}
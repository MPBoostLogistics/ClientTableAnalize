// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить DI

using System.Diagnostics;
using System.Reflection;
using System.Text;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.SKUItems;

internal class Program
{
    #region Variables and constants
    private static readonly Version? AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
    
    private static int DeadlineDays { get; set; }
    private static string goodByMessage = string.Empty;
    private static bool isMulty;
    private static bool isTracing;
    private static int TracingDelay { get; set; }

    private static string TraceInfoFlag { get; set; } = string.Empty;
    private static string TraceErrorFlag { get; set; } = string.Empty;

    private static TargetReportRecipientType? ReportRecipientType { get; set; }

    private static IConfiguration? config;
    #endregion

    #region Properties 
    private static ReportFileManager? reportFileManager;
    private static ExcelManager? excelManager;
    private static SKUManager? skuManager;
    #endregion

    #region Functionality
    private static async Task Main(string[] args)
    {
        SetAppConfig();

        if(config is not null) 
        {
            reportFileManager = new(config);
            excelManager = new(config);
            skuManager = new(config, DeadlineDays, excelManager);
        }
        else 
        {
            ManagerReachedEventArgs a = new("Error in assembling application configuration", true, true);
            await TracingProgramExecution(a);
        }

        //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    
        Trace.Listeners.Add(new ConsoleTraceListener());

        var analizeTask = Analyze(TracingDelay);
        await Task.Run(() => analizeTask);
        analizeTask.Wait();

        var exitTask = TracingProgramExecution(new(goodByMessage,false,true));
        await Task.Run(() => exitTask);
        exitTask.Wait();
    }

    private static void SetAppConfig()
    {
        if (FileService.GetProjectDirectory(out DirectoryInfo? projectDirectory)
                    && projectDirectory != null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(projectDirectory.FullName)
                .AddJsonFile("config.json", optional: false);

            config = builder.Build();

            IConfigurationSection? flagsSection = config.GetSection("Flags");
            isTracing = flagsSection.GetValue<bool>("isTracing");

            IConfigurationSection? messagesSection = config.GetSection("Messages");
            goodByMessage = messagesSection.GetValue<string>("GoodbyMessage");
            TraceInfoFlag = messagesSection.GetValue<string>("TraceInfoFlag");
            TraceErrorFlag = messagesSection.GetValue<string>("TraceErrorFlag");

            IConfigurationSection? numbersSection = config.GetSection("Numbers");
            TracingDelay = numbersSection.GetValue<int>("TracingDelay");
            DeadlineDays = numbersSection.GetValue<int>("DeadlineDays");
        }
    }

    private static async Task Analyze(int delay)
    {
        ConsoleKey[] allowedConsoleKeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];

        reportFileManager!.ManagerReached += Report;
        excelManager!.ManagerReached += Report;
        skuManager!.ManagerReached += Report;

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

        await TracingProgramExecution(new("Report analysis task has started...", false, false));
        await Task.Run(() => reportTask);
        reportTask?.Wait();

        reportFileManager.ManagerReached -= Report;
        excelManager.ManagerReached -= Report;
        skuManager.ManagerReached -= Report;
    }

    private static async Task AnalyzeSingleСlientReports(List<SourceSKUReportFileKey>? existingKeys, int tracingDelay) 
    {
        bool isInternalReport = ReportRecipientType.HasValue && ReportRecipientType.Value == TargetReportRecipientType.Internal;


        // Report analysis tasks
        if (existingKeys is not null && existingKeys.Count > 0)
        {
            // Ransoms tasks
            List<Task> ransomsTasksList = [];
            Task[] ransomsTasks = [];
            if (existingKeys.Contains(SourceSKUReportFileKey.RansomsWB))
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsWB, tracingDelay, isInternalReport));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RansomsOZ))
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsOZ, tracingDelay, isInternalReport));
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
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingWB, tracingDelay, isInternalReport));
            }

            if (existingKeys.Contains(SourceSKUReportFileKey.RemainingOZ))
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingOZ, tracingDelay, isInternalReport));
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
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.WriteOffShortages, tracingDelay, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Shipped tasks
            if (existingKeys.Contains(SourceSKUReportFileKey.Shipped))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Shipped, tracingDelay, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Transfer task
            if (existingKeys.Contains(SourceSKUReportFileKey.Transfered))
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Transfered, tracingDelay, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Additional tasks
            if (existingKeys.Contains(SourceSKUReportFileKey.Additional))
            {
                var additionalTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Additional, tracingDelay, isInternalReport);
                await Task.Run(async () => await additionalTask);
                additionalTask.Wait();
            }

            // Delete old logistic report 
            if(isTracing) 
            {
                await TracingProgramExecution(new($"Deleting old files at path {ReportFileManager.OutputClientReportFilePath}...", false,false));
            }

            if(File.Exists(ReportFileManager.OutputClientReportFilePath)) 
            {
                File.Delete(ReportFileManager.OutputClientReportFilePath);
                await TracingProgramExecution(new("Old files have been deleted.", false,false));
            }
            
            Task<List<SKUForTargetReports>>? skusForTargetReportTask = skuManager!.MakeSkusForTargetReport(isInternalReport, isMulty, tracingDelay);
            await Task.Run(() => skusForTargetReportTask);
            skusForTargetReportTask.Wait();

            if(isTracing) 
            {
                await TracingProgramExecution(new($"Articles for final report: {skusForTargetReportTask.Result.Count}.", false, false));
            }

            // Save work 
            List<SKUForTargetReports> skuForTargetReports = skusForTargetReportTask.Result;
            var makeReportTask = excelManager!.MakeReports(skuForTargetReports, isTracing, isInternalReport);
            await Task.Run(() => makeReportTask);
            makeReportTask.Wait();
        }
        else
        {
            // Error exit 
            await TracingProgramExecution(new(string.Concat("There are no reports available for analysis.\n", goodByMessage), true, true));
        }
    }

    private static async Task AnalyzeMultiСlientReports(int tracingDelay) 
    {   
        bool isInternalReport = ReportRecipientType.HasValue && ReportRecipientType.Value == TargetReportRecipientType.Internal;

        // Create and run analize task
        List<Task> summaryTasksList = [];
        foreach(var (key, value) in reportFileManager!.ReportsPathsMultyReport) 
        {
            summaryTasksList.Add(ReceivingAndProcessingData(value, tracingDelay, isInternalReport, isMulty));
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
        if (reportFileManager.AdditionalPathsMultyReport is not null && ReportRecipientType.HasValue)
        {
            additionalTask = ReceivingAndProcessingData(reportFileManager.AdditionalPathsMultyReport, tracingDelay, isInternalReport, isMulty);
            await Task.Run(async () => await additionalTask);
        }

        additionalTask?.Wait();

        // Delete old report 
        FileService.DeleteFileAtPath(ReportFileManager.OutputClientReportFilePath);

        // Make new report 
        Task<List<SKUForTargetReports>>? skusForTargetReportTask = skuManager!.MakeSkusForTargetReport(isInternalReport, isMulty, tracingDelay);
        await Task.Run(() => skusForTargetReportTask);
        skusForTargetReportTask.Wait();
        
        var saveWorkTask = excelManager!.MakeReports(skusForTargetReportTask.Result, isTracing, isInternalReport);
        await Task.Run(() => saveWorkTask);
        saveWorkTask.Wait();

        // Report 
        var completedMessage = $"Отчет логистики сохранен по пути: {ReportFileManager.OutputClientReportFilePath}\n";
        goodByMessage = string.Concat(completedMessage, goodByMessage);
        
        await TracingProgramExecution(new(goodByMessage, false, true));
    }

    private static async Task<double> ReceivingAndProcessingData(SourceReportFileForSKU reportFile, int delay, bool isInternalReport, bool isMulty) 
    {
        if(reportFile.ReportKey is null) return 0.00;

        // Make ExcelPackage
        var package = new ExcelPackage(reportFile.FileInfo);
 
        // Receiving and processing data
        Task<double>? receivingAndProcessingDataTask = skuManager!.ReceivingAndProcessingData((SourceSKUReportFileKey)reportFile.ReportKey, isInternalReport, delay, package, isMulty);
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
    private static async Task<double> ReceivingAndProcessingData(SourceSKUReportFileKey reportFileKey, int delay, bool isInternalReport, SourceReportFileForSKU? existingReportFile = null, bool isMulty = false)
    {
        SourceReportFileForSKU? reportFile;
        if(existingReportFile == null) 
        {
            var getReportFileTask = reportFileManager!.GetSourceReportForSingleClient(reportFileKey);
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
            excelManager!.CreateExcelPackage(reportFileKey, reportFile.FileInfo);

            // Receiving and processing data
            Task<double>? receivingAndProcessingDataTask = skuManager!.ReceivingAndProcessingData(reportFileKey, isInternalReport, delay, null, isMulty);
            if(receivingAndProcessingDataTask is not null) 
            {
                await Task.Run(async () => await receivingAndProcessingDataTask);
                receivingAndProcessingDataTask.Wait();
            }
  
            // Dispose package
            if(excelManager.GetExcelPackageByKey(reportFileKey, out ExcelPackage? existingPackage)) 
            {
                existingPackage?.Dispose();
            }

            // Return time
            return receivingAndProcessingDataTask?.Result ?? 0.00;
        }
        else
        {
            await TracingProgramExecution(new($"No report for key: {reportFileKey}", true, true));
            return 0;
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
                await TracingProgramExecution(new(reportsPathErrorMessage, true, false));
                return await GetSourceReportsPathFromConsole(delay);
            case false:
                var isDirInfoSuccess = FileService.GetDirectoryInfo(reportsPathString, out reportsPathDirectoryInfo);
                switch(isDirInfoSuccess && reportsPathDirectoryInfo is not null) 
                {
                    case true:
                        return reportsPathDirectoryInfo;
                    case false:
                        await TracingProgramExecution(new(reportsPathErrorMessage, true, false));
                        return await GetSourceReportsPathFromConsole(delay); 
                }
        }
    }
    
    private static async Task<TargetReportRecipientType?> GetTargetReportRecipientTypeFromConsole(int delay)
    {
        StringBuilder rawReportStringBuilder = new("Select the report direction:\n");
        rawReportStringBuilder.Append($"1: Internal (for internal work)\n");
        rawReportStringBuilder.Append($"2: External (for client)\n");
        rawReportStringBuilder.Append($"ESC: Exit\n");
        var createRawReportString = rawReportStringBuilder.ToString();

        ConsoleKey[] validkeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];
        ConsoleKey? selectKey = ConsoleAppService.GetUserKeyFromConsole(createRawReportString);
        
        switch (selectKey is null || !validkeys.Contains((ConsoleKey)selectKey)) 
        {
            case true:
                await TracingProgramExecution(new("Invalid value, please try again.", true, false));
                return await GetTargetReportRecipientTypeFromConsole(delay);
            case false: 
                switch((ConsoleKey)selectKey) 
                {
                    case ConsoleKey.D1:
                        return TargetReportRecipientType.Internal;
                    case ConsoleKey.D2:
                        return TargetReportRecipientType.External;
                    case ConsoleKey.Escape:
                        ConsoleAppService.ConsoleAppExit(goodByMessage);
                        return null;
                    default:
                        return null;
                }
        }
    }

     private static async Task<bool> GetCreateInternalReportFromConsole(int delay)
    {
        StringBuilder rawReportStringBuilder = new("Do you want to create a \"internal\" report?\n");
        rawReportStringBuilder.Append($"1: Yes\n");
        rawReportStringBuilder.Append($"2: No\n");
        rawReportStringBuilder.Append($"ESC: Exit\n");
        var createRawReportString = rawReportStringBuilder.ToString();

        ConsoleKey[] validkeys = [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape];
        ConsoleKey? selectKey = ConsoleAppService.GetUserKeyFromConsole(createRawReportString);
        
        switch (selectKey is null || !validkeys.Contains((ConsoleKey)selectKey)) 
        {
            case true:
                await TracingProgramExecution(new("Invalid value, please try again.", true, false));
                return await GetCreateInternalReportFromConsole(delay);
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
        
       


        //await TracingProgramExecution(new($"Deadline set, days: {skuManager.DeadlineDays}", false, false));

        // Get source reports path
        DirectoryInfo? getSourceReportDirInfo = null;
        while(getSourceReportDirInfo is null) 
        {
            Task<DirectoryInfo?> getSourceReportsPathTask = GetSourceReportsPathFromConsole(TracingDelay);
            await Task.Run(() => getSourceReportsPathTask);
            getSourceReportsPathTask.Wait();
            getSourceReportDirInfo = getSourceReportsPathTask.Result;
        } 
        Console.Clear();
        await TracingProgramExecution(new($"Reports path: {getSourceReportDirInfo.Name}", false, false));

        // Availability necessary reports
        List<SourceSKUReportFileKey>? existingKeys = null;
        while(existingKeys is null) 
        {
            Task<List<SourceSKUReportFileKey>?>  availabilityNecessaryTask = 
                reportFileManager!.ChecksAvailabilityNecessaryFiles(getSourceReportDirInfo.FullName, isMulty, isTracing, TracingDelay);
            await Task.Run(() => availabilityNecessaryTask);
            availabilityNecessaryTask.Wait();
            existingKeys = availabilityNecessaryTask.Result;
        }
        
        // Select report direction
        while(ReportRecipientType is null) 
        {
            Task<TargetReportRecipientType?> getTargetReportRecipientTypeTask = GetTargetReportRecipientTypeFromConsole(TracingDelay);
            await Task.Run(() => getTargetReportRecipientTypeTask);
            getTargetReportRecipientTypeTask.Wait();
            ReportRecipientType = getTargetReportRecipientTypeTask.Result;
        }
        Console.Clear();

        return existingKeys; 
    }


    private static void Report(object? sender, ManagerReachedEventArgs e)
    {
        Task.Run(async () => await TracingProgramExecution(e));
    }
    private static async Task TracingProgramExecution(ManagerReachedEventArgs args) 
    {
        string flag;
        if(isTracing) 
        {
            switch (args.IsError) 
            {
                case false:
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    flag = TraceInfoFlag;
                    break;
                case true:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    flag = TraceErrorFlag;
                    break;
            }

            Trace.WriteLine($"{flag}{args.Message}");
            await Task.Delay(TracingDelay);
            Console.ResetColor();
        }

        if (args.IsExit) 
        {
            skuManager!.ManagerReached -= Report;
            excelManager!.ManagerReached -= Report;
            reportFileManager!.ManagerReached -= Report;

            Trace.Flush();
        }
    }
    #endregion
}
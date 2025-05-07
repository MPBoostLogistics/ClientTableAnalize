// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить DI

using System.Diagnostics;
using System.Text;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.ReportItems;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

internal class Program
{
    #region Variables and constants   
    private static int DeadlineDays { get; set; }
    private static string goodByMessage = string.Empty;
    private static bool isTracing;
    private static bool isAutoResorting;
    private static int TracingDelay { get; set; }

    private static string TraceInfoFlag { get; set; } = string.Empty;
    private static string TraceErrorFlag { get; set; } = string.Empty;
    private static IConfiguration? config;

    #endregion

    #region Properties 
    private static ReportFileManager? reportFileManager;
    private static ExcelManager? excelManager;
    private static SKUManager? skuManager;
    private static AnalyticalManager? anManager;
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
            anManager = new(config);
        }
        else 
        {
            ManagerReachedEventArgs a = new("Error in assembling application configuration", true, true);
            await TracingProgramExecution(a);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    
        Trace.Listeners.Add(new ConsoleTraceListener());

        var analizeTask = Analyze();
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
            isAutoResorting = flagsSection.GetValue<bool>("isAutoResorting");

            IConfigurationSection? messagesSection = config.GetSection("Messages");
            goodByMessage = messagesSection.GetValue<string>("GoodbyMessage");
            TraceInfoFlag = messagesSection.GetValue<string>("TraceInfoFlag");
            TraceErrorFlag = messagesSection.GetValue<string>("TraceErrorFlag");

            IConfigurationSection? numbersSection = config.GetSection("Numbers");
            TracingDelay = numbersSection.GetValue<int>("TracingDelay");
            DeadlineDays = numbersSection.GetValue<int>("DeadlineDays");
        }
    }
    private static async Task Analyze()
    {
        reportFileManager!.ManagerReached += Report;
        excelManager!.ManagerReached += Report;
        skuManager!.ManagerReached += Report;
        anManager!.ManagerReached += Report;

        Console.Clear();

        // Get data
        Task<List<SourceReportItemKey>?>? getDataFromConsoleTask = GetDataFromConsole();
        await Task.Run(() => getDataFromConsoleTask);
        getDataFromConsoleTask.Wait();
        List<SourceReportItemKey>? existingKeys = getDataFromConsoleTask.Result;

        //Start analize
        await TracingProgramExecution(new("Report analysis task has started...", false, false));
        var analyzeСlientReportTask = AnalyzeСlientReport(existingKeys);
        await Task.Run(() => analyzeСlientReportTask);
        analyzeСlientReportTask?.Wait();

        reportFileManager.ManagerReached -= Report;
        excelManager.ManagerReached -= Report;
        skuManager.ManagerReached -= Report;
    }

    private static async Task<List<SourceReportItemKey>?> GetDataFromConsole() 
    {
        // Get source reports path
        DirectoryInfo? getSourceReportDirInfo = null;
        while(getSourceReportDirInfo is null) 
        {
            Task<DirectoryInfo?> getSourceReportsPathTask = GetSourceReportsPathFromConsole(TracingDelay);
            await Task.Run(() => getSourceReportsPathTask);
            getSourceReportsPathTask.Wait();
            getSourceReportDirInfo = getSourceReportsPathTask.Result;
        } 

        await TracingProgramExecution(new($"Reports path: {getSourceReportDirInfo.Name}", false, false));

        // Availability necessary reports
        Task<List<SourceReportItemKey>?>? availabilityNecessaryTask = null;
        List<SourceReportItemKey>? existingKeys = null;

        try
        {
            availabilityNecessaryTask = reportFileManager!.ChecksAvailabilityNecessaryFiles(getSourceReportDirInfo.FullName);
            await Task.Run(() => availabilityNecessaryTask);
            availabilityNecessaryTask.Wait();
            existingKeys = availabilityNecessaryTask.Result;
        }
        catch (System.Exception ex)
        {
            existingKeys = null;
            await TracingProgramExecution(new(ex.Message, true, true));
            //break;
        }

        if(!availabilityNecessaryTask!.IsFaulted && existingKeys != null && existingKeys.Count > 0)  
        {
            // Select report direction
            while(ReportFileManager.ReportRecipientType is null) 
            {
                Task<TargetReportRecipientType?> getTargetReportRecipientTypeTask = GetTargetReportRecipientTypeFromConsole(TracingDelay);
                await Task.Run(() => getTargetReportRecipientTypeTask);
                getTargetReportRecipientTypeTask.Wait();
                ReportFileManager.ReportRecipientType = getTargetReportRecipientTypeTask.Result;
            }
            Console.Clear();
        }

        return existingKeys; 
    }
    private static async Task AnalyzeСlientReport(List<SourceReportItemKey>? existingKeys) 
    {
        bool isInternalReport = ReportFileManager.ReportRecipientType.HasValue && ReportFileManager.ReportRecipientType.Value == TargetReportRecipientType.Internal;

        // Report analysis tasks
        if (existingKeys is not null && existingKeys.Count > 0) 
        {
            // Ransoms task
            if(existingKeys.Contains(SourceReportItemKey.Ransoms))
            {
                Task ransomsTask = ReceivingAndProcessingData(SourceReportItemKey.Ransoms, isInternalReport);
                await Task.Run(async () => await ransomsTask);
                ransomsTask.Wait();
            }

            // Balance task
            if (existingKeys.Contains(SourceReportItemKey.Balance))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.Balance, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // WriteOff points via warehouse task (shortage)
            if (existingKeys.Contains(SourceReportItemKey.WriteOffPointsViaWarehouseShortage))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.WriteOffPointsViaWarehouseShortage, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

             // WriteOff points via warehouse task (adjustment)
            if (existingKeys.Contains(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // WriteOff points task 
            if (existingKeys.Contains(SourceReportItemKey.WriteOffPoints))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.WriteOffPoints, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Outgoing products tasks
            if (existingKeys.Contains(SourceReportItemKey.OutgoingProducts))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.OutgoingProducts, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Transfer task
            if (existingKeys.Contains(SourceReportItemKey.Transfered))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.Transfered, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Additional task
            if (existingKeys.Contains(SourceReportItemKey.Additional))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.Additional, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Paid shortages task
            if (existingKeys.Contains(SourceReportItemKey.PaidShortages))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.PaidShortages, isInternalReport);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
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

            // Make skus for target report
            Task<List<SKUForTargetReport>>? skusForTargetReportTask = skuManager!.MakeSkusForTargetReport();
            await Task.Run(() => skusForTargetReportTask);
            skusForTargetReportTask.Wait();

            if(isTracing) 
            {
                await TracingProgramExecution(new($"Articles for final report: {skusForTargetReportTask.Result.Count}.", false, false));
            }

            List<SKUForTargetReport> skuForTargetReports = skusForTargetReportTask.Result;

            // Article analysis and automatic re-sorting
            if(isAutoResorting) 
            {
                Task[] fixAndAutoResortTasks =
                [
                    AnalyticalManager.FixReceiptBySKUsOfParallelClients(ref skuForTargetReports),
                    AnalyticalManager.FixWriteOffByPointsThroughWarehouse(ref skuForTargetReports),
                    AnalyticalManager.SetAutoResorts(ref skuForTargetReports, ResortConsumptionTarget.ResortingConsumptionByOutgoing, true),
                    AnalyticalManager.SetAutoResorts(ref skuForTargetReports, ResortConsumptionTarget.ResortingConsumptionByTransfer, true),
                    AnalyticalManager.SetAutoResorts(ref skuForTargetReports, ResortConsumptionTarget.ResortingReceipt, true),
                    AnalyticalManager.FixExtraReceipts(ref skuForTargetReports, true),
                    AnalyticalManager.FinalAnalizeSkus(ref skuForTargetReports)
                ];

                for (int i = 0; i < fixAndAutoResortTasks.Length; i++)
                {
                    await Task.Run(() => fixAndAutoResortTasks[i]);
                }
                
                Task.WaitAll(fixAndAutoResortTasks);
            }
            
            // Save work 
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

    /// <summary>
    /// Receiving and processing data from seller reports.
    /// </summary>
    /// <param name="reportFileKey">Key of source file with report.</param>
    private static async Task<double> ReceivingAndProcessingData(SourceReportItemKey reportFileKey,  bool isInternalReport)
    {
        var getReportFileTask = reportFileManager!.GetSourceReport(reportFileKey);
        await Task.Run(async () => await getReportFileTask);
        getReportFileTask.Wait();
        SourceReportFileItem? reportFile = getReportFileTask.Result;
       
        if (reportFile is not null)
        {
            // Make ExcelPackage
            using var package = excelManager!.CreateExcelPackage(reportFileKey, reportFile.Info);

            // Receiving and processing data
            Task<double>? receivingAndProcessingDataTask = skuManager!.ReceivingAndProcessingData(reportFileKey, isInternalReport, package);
            if(receivingAndProcessingDataTask is not null) 
            {
                await Task.Run(async () => await receivingAndProcessingDataTask);
                receivingAndProcessingDataTask.Wait();
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
        string reportsPathTitleMessage = "Specify path to dir with the client's source reports and press 'Enter':\n ";
        string reportsPathErrorMessage = "Path to report files is incorrect.\nPlease try again.";    

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
// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить DI

using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using Org.BouncyCastle.Asn1.Cms;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.ReportItems;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;


public class DataFromConsole(string questionMessage, string errorMessage, ParsingTypeCode typeCode, bool isRequred)
{
    #region Properties
    public string QuestionMessage { get; } = questionMessage;
    public string ErrorMessage { get; } = errorMessage;
    public ParsingTypeCode TypeCode { get; } = typeCode;
    public bool IsRequred { get; } = isRequred;
    #endregion
}


internal class Program
{
    #region Variables and constants   
    private static string appVersion = string.Empty;
    private static string goodByMessage = string.Empty;
    private static bool isTracing;
    private static bool isAutoResorting;

    private static readonly DateOnly orderPeriodStartDateDefault = DateOnly.Parse("2022-12-26");

    private static IConfiguration? appConfig;

    #endregion

    #region Properties 
    private static int DeadlineDays { get; set; }
    private static int TraceDelay { get; set; }
    private static string TraceInfoFlag { get; set; } = string.Empty;
    private static string TraceErrorFlag { get; set; } = string.Empty;

    private static DateOnly OrderPeriodStartDate { get; set; } = orderPeriodStartDateDefault;

    private static ReportFileManager? reportFileManager;
    private static ExcelManager? excelManager;
    private static SKUManager? skuManager;
    private static AnalyticalManager? anManager;
    #endregion

    #region Functionality
    private static async Task Main(string[] args)
    {
        SetAppConfig();

        if (appConfig is not null)
        {
            reportFileManager = new(appConfig);
            excelManager = new(appConfig);
            skuManager = new(appConfig, DeadlineDays, excelManager);
            anManager = new(appConfig);
        }
        else
        {
            ManagerReachedEventArgs a = new("Error in assembling application configuration", true, true);
            await TracingProgramExecution(a);
        }

        //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        // https://epplussoftware.com/en/Home/GettingStartedCommunityLicense

        Trace.Listeners.Add(new ConsoleTraceListener());

        var analizeTask = Analyze();
        await Task.Run(() => analizeTask);
        analizeTask.Wait();

        var exitTask = TracingProgramExecution(new(goodByMessage, false, true));
        await Task.Run(() => exitTask);
        exitTask.Wait();
    }

    /// <summary>
    /// Setting up the app configuration
    /// </summary>
    private static void SetAppConfig()
    {
        if (FileService.GetProjectDirectory(out DirectoryInfo? projectDirectory)
                    && projectDirectory != null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(projectDirectory.FullName)
                .AddJsonFile("config.json", optional: false);

            appConfig = builder.Build();

            IConfigurationSection? stringsSection = appConfig.GetSection("Strings");
            appVersion = stringsSection.GetValue<string>("AppVersion");

            IConfigurationSection? flagsSection = appConfig.GetSection("Flags");
            isTracing = flagsSection.GetValue<bool>("isTracing");
            isAutoResorting = flagsSection.GetValue<bool>("isAutoResorting");

            IConfigurationSection? messagesSection = appConfig.GetSection("Messages");
            goodByMessage = messagesSection.GetValue<string>("GoodbyMessage");
            TraceInfoFlag = messagesSection.GetValue<string>("TraceInfoFlag");
            TraceErrorFlag = messagesSection.GetValue<string>("TraceErrorFlag");

            IConfigurationSection? numbersSection = appConfig.GetSection("Numbers");
            TraceDelay = numbersSection.GetValue<int>("TraceDelay");
            DeadlineDays = numbersSection.GetValue<int>("DeadlineDays");
        }
    }

    private static async Task Analyze()
    {
        await Task.Delay(10); // dummy

        reportFileManager!.ManagerReached += Report;
        excelManager!.ManagerReached += Report;
        skuManager!.ManagerReached += Report;
        anManager!.ManagerReached += Report;

        Console.Clear();

        // Get client report path, type of target report from console.
        var getDataFromConsoleTask = GetDataFromConsole();
        await Task.Run(async () => getDataFromConsoleTask);
        getDataFromConsoleTask.Wait();



        // // Checking for the presence of data (permanent and new).
        // var checkingForPresenceDataTask = CheckingForPresenceData();
        // await Task.Run(async () => checkingForPresenceDataTask);
        // checkingForPresenceDataTask.Wait();

        // // // Start analize
        // // await TracingProgramExecution(new("Report analysis task has started...", false, false));
        // // var analyzeСlientReportTask = AnalyzeСlientReport(existingKeys);
        // // await Task.Run(() => analyzeСlientReportTask);
        // // analyzeСlientReportTask?.Wait();

        reportFileManager.ManagerReached -= Report;
        excelManager.ManagerReached -= Report;
        skuManager.ManagerReached -= Report;
    }

    private static async Task CheckingForPresenceData()
    {
        // Checking for the presence of permanent data
        var constantDataSourceTask = reportFileManager!.CheckOfAvailailityData(null, PrimaryDataType.Permanent);
        await Task.Run(() => constantDataSourceTask);
        constantDataSourceTask.Wait();

        if (!constantDataSourceTask.Result.isError)
        {
            await TracingProgramExecution(new("Primary data is available", constantDataSourceTask.Result.isError, false));
        }
        else
        {
            await TracingProgramExecution(new(constantDataSourceTask.Result.message, constantDataSourceTask.Result.isError, true));
        }

        //Checking for the presence of new data
        // TODO: Add new data check
    }

    // Use order period start:





    private static async Task GetDataFromConsole()
    {
        // Get client reports path
        DataFromConsole cletientReportPathData = new(
            questionMessage: "Укажите путь к директории с клинетскими данными: ",
            errorMessage: "Путь к директории с клиентскими данными не указан или указан неверно.\nПроверьте правильность ввода и повторите попытку.",
            typeCode: ParsingTypeCode.DirectoryTypeCode,
            isRequred: true);

        var getReportsTask = GetFromConsole<DirectoryInfo?>(cletientReportPathData);
        await Task.Run(() => getReportsTask);
        getReportsTask.Wait();

        DirectoryInfo clientReportsDirectory = getReportsTask.Result!;
        await TracingProgramExecution(new($"Директория с клиентскими данными: {clientReportsDirectory.Name}", false, false));

        // Get order period start date  
        DataFromConsole orderPeriodStartDateData = new(
          questionMessage: "Укажите дату начала заказов клиента: ",
          errorMessage: "Дата начала заказов не указана или указана неверно.\nПроверьте правильность ввода и повторите попытку.",
          typeCode: ParsingTypeCode.DataOnlyTypeCode,
          isRequred: true);

        var getStartDateTask = GetFromConsole<DateOnly?>(orderPeriodStartDateData);
        await Task.Run(() => getStartDateTask);
        getStartDateTask.Wait();

        DateOnly? orderPeriodStartDateTask = getStartDateTask.Result;
        await TracingProgramExecution(new($"Дата начала заказов: {orderPeriodStartDateTask}", false, false));

        // // Select report direction
        // while (ReportFileManager.ReportRecipientType is null)
        // {
        //     Task<TargetReportRecipientType?> getTargetReportRecipientTypeTask = GetTargetReportRecipientTypeFromConsole(TracingDelay);
        //     await Task.Run(() => getTargetReportRecipientTypeTask);
        //     getTargetReportRecipientTypeTask.Wait();
        //     ReportFileManager.ReportRecipientType = getTargetReportRecipientTypeTask.Result;
        // }

        Console.WriteLine("!!!");
        Console.ReadKey();

        Console.Clear();
    }

    //     return existingKeys;
    // }

    private static async Task AnalyzeСlientReport(List<SourceReportItemKey>? existingKeys)
    {
        bool isInternalReport = ReportFileManager.ReportRecipientType.HasValue && ReportFileManager.ReportRecipientType.Value == TargetReportRecipientType.Internal;

        // Report analysis tasks
        if (existingKeys is not null && existingKeys.Count > 0)
        {
            // Ransoms task
            if (existingKeys.Contains(SourceReportItemKey.Ransoms))
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

            // // Paid shortages task
            // if (existingKeys.Contains(SourceReportItemKey.ShortagePaid))
            // {
            //     var currentTask = ReceivingAndProcessingData(SourceReportItemKey.ShortagePaid, isInternalReport);
            //     await Task.Run(async () => await currentTask);
            //     currentTask.Wait();
            // }

            // Delete old logistic report 
            if (isTracing)
            {
                await TracingProgramExecution(new($"Deleting old files at path {ReportFileManager.OutputClientReportFilePath}...", false, false));
            }

            if (File.Exists(ReportFileManager.OutputClientReportFilePath))
            {
                File.Delete(ReportFileManager.OutputClientReportFilePath);
                await TracingProgramExecution(new("Old files have been deleted.", false, false));
            }

            // Make skus for target report
            Task<List<SKUForTargetReport>>? skusForTargetReportTask = skuManager!.MakeSkusForTargetReport();
            await Task.Run(() => skusForTargetReportTask);
            skusForTargetReportTask.Wait();

            if (isTracing)
            {
                await TracingProgramExecution(new($"Articles for final report: {skusForTargetReportTask.Result.Count}.", false, false));
            }

            List<SKUForTargetReport> skuForTargetReports = skusForTargetReportTask.Result;

            // Article analysis and automatic re-sorting
            if (isAutoResorting)
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
    private static async Task<double> ReceivingAndProcessingData(SourceReportItemKey reportFileKey, bool isInternalReport)
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
            if (receivingAndProcessingDataTask is not null)
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
        string titleMessage = "Specify path to dir with the client's source reports and press 'Enter':\n ";
        string errorMessage = "Path to report files is incorrect.\nPlease try again.";

        DirectoryInfo? reportsPathDirectoryInfo;
        string reportsPathString = ConsoleAppService.GetUserStringFromConsole(titleMessage);

        switch (string.IsNullOrEmpty(reportsPathString))
        {
            case true:
                await TracingProgramExecution(new(errorMessage, true, false));
                return await GetSourceReportsPathFromConsole(delay);
            case false:
                var isDirInfoSuccess = FileService.GetDirectoryInfo(reportsPathString, out reportsPathDirectoryInfo);
                switch (isDirInfoSuccess && reportsPathDirectoryInfo is not null)
                {
                    case true:
                        return reportsPathDirectoryInfo;
                    case false:
                        await TracingProgramExecution(new(errorMessage, true, false));
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
                switch ((ConsoleKey)selectKey)
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

    private static async Task<DateOnly?> GetOrderPeriodStartDate(int delay)
    {
        StringBuilder orderPeriodStartDateStringBuilder = new("Select order period start (ENTER: Default, Write + ENTER: Date, ESC: Exit): \n");

        ConsoleKey? selectKey = ConsoleAppService.GetUserKeyFromConsole(orderPeriodStartDateStringBuilder.ToString());
        var dateString = Console.ReadLine();

        switch (selectKey is null)
        {
            case true:
                await TracingProgramExecution(new("Invalid value, please try again.", true, false));
                return await GetOrderPeriodStartDate(delay);
            case false:
                switch ((ConsoleKey)selectKey)
                {
                    case ConsoleKey.Enter:
                        return orderPeriodStartDateDefault;

                    case ConsoleKey.Escape:
                        ConsoleAppService.ConsoleAppExit(goodByMessage);
                        return null;
                    default:
                        if (string.IsNullOrEmpty(dateString))
                        {
                            return null;
                        }
                        else
                        {
                            Console.WriteLine("HELLOOO!");
                            Console.WriteLine(dateString);
                            await Task.Delay(3000);

                            if (DateOnly.TryParseExact(dateString.Trim(), [.. IParsingRowEntity.DateOnlyFormats], out DateOnly dateOnlyValue))
                            {
                                return dateOnlyValue;
                            }
                            else
                            {
                                await TracingProgramExecution(new("Invalid value, please try again.", true, false));
                                return await GetOrderPeriodStartDate(delay);
                            }
                        }
                }
        }
    }

    private static async Task<T?> GetFromConsole<T>(DataFromConsole data)
    {
        T? type = default;
        while (type is null)
        {
            var getValueTask = GetStringFromConsole(data);
            await Task.Run(() => getValueTask);
            getValueTask.Wait();

            if (!string.IsNullOrEmpty(getValueTask.Result) &&
                ParsingManager.Parse(getValueTask.Result!.Trim('\''),
                data.TypeCode,
                data.IsRequred,
                out object? resultObj) &&
                resultObj is T info)
            {
                type = info;
                break;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Значение не указано или указано неверно.\nПроверьте правильность ввода и повторите попытку.");
                await Task.Delay(TraceDelay + 1000);
                continue;
            }
        }
        return type;
    }

    private static async Task<string?> GetStringFromConsole(DataFromConsole dataFromConsole)
    {
        string? value = null;
        while (value is null)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(dataFromConsole.QuestionMessage);

            value = Console.ReadLine();

            if (!string.IsNullOrEmpty(value))
            {
                break;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(dataFromConsole.ErrorMessage);
                await Task.Delay(TraceDelay + 1000);
                await GetStringFromConsole(dataFromConsole);
            }
        }

        return value;
    }

    private static void Report(object? sender, ManagerReachedEventArgs e)
    {
        Task.Run(async () => await TracingProgramExecution(e));
    }
    private static async Task TracingProgramExecution(ManagerReachedEventArgs args)
    {
        string flag;
        if (isTracing)
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
            await Task.Delay(TraceDelay);
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
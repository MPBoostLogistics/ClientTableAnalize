// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

using System.Diagnostics;
using System.Text;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.ReportItems;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

internal class Program
{
    #region Variables and constants   
    private static string appVersion = string.Empty;
    private static string goodByMessage = string.Empty;
    private static bool isTracing;
    /// <summary>
    /// Флаг учета (проверки) выкупов
    /// </summary>
    private static bool isCheckRansoms;
    /// <summary>
    /// Флаг учета (проверки) баланса (прихода и остатков) товаров на складе
    /// </summary>
    private static bool isCheckBalance;
    /// <summary>
    /// Флаг учета (проверки) баланса расхода товаров со склада
    /// </summary>
    private static bool isCheckOutgoingProducts;
    /// <summary>
    /// Флаг учета (проверки) данных дополнительного отчета
    /// </summary>
    private static bool isCheckAdditionalData;

    private static bool isAutoResorting;

    /// <summary>
    /// Флаг учета статуса "В работе" при автопересортах.
    /// </summary>
    private static bool isConsiderInProgressStatusInAutoResorting;
    private static bool isCombineAliases;

    private static readonly DateOnly orderPeriodStartDateDefault = DateOnly.Parse("2022-12-26");

    private static IConfiguration? appConfig;

    #endregion

    #region Properties 
    private static int DeadlineDays { get; set; }
    private static int TraceDelay { get; set; }
    private static string TraceInfoFlag { get; set; } = string.Empty;
    private static string TraceErrorFlag { get; set; } = string.Empty;
    private static DirectoryInfo? ClientReportsDirectoryPath { get; set; } = null;
    private static DateOnly OrderPeriodStartDate { get; set; } = orderPeriodStartDateDefault;

    private static ReportFileManager? reportFileManager;
    private static ExcelManager? excelManager;
    private static ExcelSheetParsingManager? excelSheetsManager;
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
            excelSheetsManager = new(appConfig);
            skuManager = new(appConfig, DeadlineDays, excelManager, excelSheetsManager);
            anManager = new(appConfig);
        }
        else
        {
            ManagerReachedEventArgs a = new("Error in assembling application configuration", true, true);
            await TracingProgramExecution(a);
        }

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
        var pd = Directory.GetCurrentDirectory(); // GetProjectDirectory(out DirectoryInfo? projectDirectory)
        Console.WriteLine(pd);

        if (pd is not null && File.Exists(Path.Combine(pd, "config.json")))
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(pd)
                .AddJsonFile("config.json", optional: false) ?? throw new Exception("Configuration builder is null");

            appConfig = builder.Build();

            IConfigurationSection? stringsSection = appConfig.GetSection("Strings");
            appVersion = stringsSection.GetValue<string>("AppVersion");

            // Получение булевых значений
            IConfigurationSection? flagsSection = appConfig.GetSection("Flags");
            isTracing = flagsSection.GetValue<bool>("isTracing");
            isCheckRansoms = flagsSection.GetValue<bool>(nameof(isCheckRansoms));
            isCheckBalance = flagsSection.GetValue<bool>(nameof(isCheckBalance));
            isCheckOutgoingProducts = flagsSection.GetValue<bool>(nameof(isCheckOutgoingProducts));
            isCheckAdditionalData = flagsSection.GetValue<bool>(nameof(isCheckAdditionalData));
            isAutoResorting = flagsSection.GetValue<bool>("isAutoResorting");
            isConsiderInProgressStatusInAutoResorting = flagsSection.GetValue<bool>("isConsiderInProgressStatusInAutoResorting");
            isCombineAliases = flagsSection.GetValue<bool>("isCombineAliases");

            IConfigurationSection? messagesSection = appConfig.GetSection("Messages");
            goodByMessage = messagesSection.GetValue<string>("GoodbyMessage");
            TraceInfoFlag = messagesSection.GetValue<string>("TraceInfoFlag");
            TraceErrorFlag = messagesSection.GetValue<string>("TraceErrorFlag");

            IConfigurationSection? numbersSection = appConfig.GetSection("Numbers");
            TraceDelay = numbersSection.GetValue<int>("TraceDelay");
            DeadlineDays = numbersSection.GetValue<int>("DeadlineDays");
        }
    }

    /// <remarks>oct 2025 ver.</remarks>
    private static async Task Analyze()
    {
        await Task.Delay(10); // dummy
        SubscribeManagersToEvents();
        Console.Clear();

        // Get client report path, type of target report from console.
        var getDataFromConsoleTask = GetPrimaryDataFromConsole();
        await Task.Run(async () => getDataFromConsoleTask);
        getDataFromConsoleTask.Wait();

        // Checking for the presence of data (permanent and new).
        var checkingForPresenceDataTask = CheckingForPresenceData();
        await Task.Run(async () => checkingForPresenceDataTask);
        checkingForPresenceDataTask.Wait();

        // Start analize
        await TracingProgramExecution(new("Анализ первичных данных начат...", false, false));
        var analyzeСlientReportTask = AnalyzePresenceData();
        await Task.Run(() => analyzeСlientReportTask);
        analyzeСlientReportTask?.Wait();

        UnsubscribeManagersFromEvents();
    }

    /// <summary>
    /// Get primary data from manager in console.
    /// </summary>
    /// <returns>Async Task</returns>
    /// <remarks>oct 2025 ver.</remarks>
    private static async Task GetPrimaryDataFromConsole()
    {
        // Get client reports path
        DataFromConsole cletientReportPathData = new(
            questionMessage: "Укажите путь к директории с клиентскими данными: ",
            errorMessage: "Путь к директории с клиентскими данными не указан или указан неверно.\nПроверьте правильность ввода и повторите попытку.",
            typeCode: ParsingTypeCode.DirectoryTypeCode,
            isRequred: true);

        var getReportsTask = GetValueFromConsole<DirectoryInfo?>(cletientReportPathData);
        await Task.Run(() => getReportsTask);
        getReportsTask.Wait();

        ClientReportsDirectoryPath = getReportsTask.Result!;

        await TracingProgramExecution(new($"Директория с клиентскими данными: {ClientReportsDirectoryPath.Name}", false, false));

        // Get order period start date  
        // DataFromConsole orderPeriodStartDateData = new(
        //   questionMessage: "Укажите дату начала заказов клиента: ",
        //   errorMessage: "Дата начала заказов не указана или указана неверно.\nПроверьте правильность ввода и повторите попытку.",
        //   typeCode: ParsingTypeCode.DataOnlyTypeCode,
        //   isRequred: true);

        // var getStartDateTask = GetValueFromConsole<DateOnly?>(orderPeriodStartDateData);
        // await Task.Run(() => getStartDateTask);
        // getStartDateTask.Wait();

        //OrderPeriodStartDate = getStartDateTask.Result ?? orderPeriodStartDateDefault;
        OrderPeriodStartDate = new DateOnly(2022, 12, 26);
        await TracingProgramExecution(new($"Дата начала заказов: {OrderPeriodStartDate}", false, false));

        // // Select report direction
        // DataFromConsole reportDirectionData = new(
        //     questionMessage: "Выберите целевой тип отчета:\n1. Внутренний (для работы отдела логистики)\n2. Внешний (для предоставления клиенту)\nESC: Выход из программы.",
        //     errorMessage: "Клавиша нажата неверно.\nПроверьте правильность ввода и повторите попытку.",
        //     typeCode: ParsingTypeCode.StringTypeCode, // Dummy
        //     isRequred: true);

        // var reportDirectionTask = GetKeyFromConsole(reportDirectionData, [ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.Escape]);
        // await Task.Run(() => reportDirectionTask);
        // reportDirectionTask.Wait();

        // switch (reportDirectionTask.Result)
        // {
        //     case ConsoleKey.D1:
        //         ReportFileManager.ReportRecipientType = TargetReportRecipientType.Internal;
        //         break;
        //     case ConsoleKey.D2:
        //         ReportFileManager.ReportRecipientType = TargetReportRecipientType.External;
        //         break;
        //     case ConsoleKey.Escape:
        //         ReportFileManager.ReportRecipientType = null;
        //         break;
        // }
    }

    /// <summary>
    /// Checking for the presence of permanent and variable data.
    /// </summary>
    /// <returns>Async task as void</returns>
    /// <remarks>oct 2025 ver.</remarks>
    private static async Task CheckingForPresenceData()
    {
        // Checking for the presence of permanent data
        var getPermanentDataTask = reportFileManager!.CheckOfAvailailityData(null, PrimaryDataType.Permanent);
        await Task.Run(() => getPermanentDataTask);
        getPermanentDataTask.Wait();

        await TracingProgramExecution(
            new(!getPermanentDataTask.Result.isError ?
                "Первичнные постоянные данные валидны." :
                getPermanentDataTask.Result.message,
                getPermanentDataTask.Result.isError, getPermanentDataTask.Result.isError));

        // Checking for the presence of variable data
        var getVariableDataTask = reportFileManager!.CheckOfAvailailityData(ClientReportsDirectoryPath!.FullName, PrimaryDataType.Variable);
        await Task.Run(() => getVariableDataTask);
        getVariableDataTask.Wait();

        await TracingProgramExecution(new(!getVariableDataTask.Result.isError ?
             "Первичнные переменные данные валидны." :
             getVariableDataTask.Result.message,
         getVariableDataTask.Result.isError,
         getVariableDataTask.Result.isError));
    }

    private static async Task AnalyzePresenceData()
    {
        List<SourceReportItemKey> existingKeys = ReportFileManager.PrimaryReportFiles.Select(el => el.Key).ToList();

        // Report analysis tasks
        if (existingKeys is not null && existingKeys.Count > 0)
        {
            // Ransoms task
            if (isCheckRansoms && existingKeys.Contains(SourceReportItemKey.Ransoms))
            {
                Task ransomsTask = ReceivingAndProcessingData(SourceReportItemKey.Ransoms);
                await Task.Run(async () => await ransomsTask);
                ransomsTask.Wait();
            }

            // Balance task
            if (isCheckBalance && existingKeys.Contains(SourceReportItemKey.Balance))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.Balance);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // WriteOff points via warehouse task (shortage)
            if (isCheckBalance && existingKeys.Contains(SourceReportItemKey.WriteOffPointsViaWarehouseShortage))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.WriteOffPointsViaWarehouseShortage);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // WriteOff points via warehouse task (adjustment)
            if (isCheckBalance && existingKeys.Contains(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // WriteOff points task 
            if (isCheckBalance && existingKeys.Contains(SourceReportItemKey.WriteOffPoints))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.WriteOffPoints);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Outgoing products task
            if (isCheckOutgoingProducts && existingKeys.Contains(SourceReportItemKey.OutgoingProducts))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.OutgoingProducts);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Transfer task
            if (isCheckOutgoingProducts && existingKeys.Contains(SourceReportItemKey.Transfered))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.Transfered);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Additional task
            if (isCheckAdditionalData && existingKeys.Contains(SourceReportItemKey.Additional))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.Additional);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Paid shortages task
            if (isCheckBalance && isCheckOutgoingProducts && existingKeys.Contains(SourceReportItemKey.ShortagesPaid))
            {
                var currentTask = ReceivingAndProcessingData(SourceReportItemKey.ShortagesPaid);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Delete old files
            Task deletOldReportsTask = reportFileManager!.DeletOldReports(isTracing);
            await Task.Run(() => deletOldReportsTask);
            deletOldReportsTask.Wait();

            // Make and combine skus for target report
            Task<List<SKUForFinalReport>>? skusForTargetReportTask;
            List<SKUForFinalReport>? skuForTargetReports = [];

            skusForTargetReportTask = !isCombineAliases || skuManager!.SkuAliasesBalanceUnits.Count() == 0 ?
                skuManager!.MakeSkusForTargetReport() :
                skuManager!.MakeAndCombineSkusForTargetReport();

            await Task.Run(() => skusForTargetReportTask);
            skusForTargetReportTask!.Wait();
            skuForTargetReports = skusForTargetReportTask.Result;

            if (isTracing)
                await TracingProgramExecution(new($"Артикулов для финального отчета: {skusForTargetReportTask.Result.Count}.", false, false));

            // Article analysis and automatic re-sorting
            if (isAutoResorting)
            {
                Task[] fixAndAutoResortTasks =
                [
                    AnalyticalManager.FixReceiptBySKUsOfParallelClients(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting),
                    AnalyticalManager.FixWriteOffByPointsThroughWarehouse(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting),
                    AnalyticalManager.SetAutoResorts(ref skuForTargetReports, ResortConsumptionTarget.ResortingConsumptionByOutgoing, isConsiderInProgressStatusInAutoResorting),
                    AnalyticalManager.SetAutoResorts(ref skuForTargetReports, ResortConsumptionTarget.ResortingConsumptionByTransfer, isConsiderInProgressStatusInAutoResorting),
                    AnalyticalManager.SetAutoResorts(ref skuForTargetReports, ResortConsumptionTarget.ResortingReceipt, isConsiderInProgressStatusInAutoResorting),
                    AnalyticalManager.FixExtraReceipts(ref skuForTargetReports, true)
                ];

                for (int i = 0; i < fixAndAutoResortTasks.Length; i++)
                {
                    await Task.Run(() => fixAndAutoResortTasks[i]);
                }

                Task.WaitAll(fixAndAutoResortTasks);
            }

            // Final analize
            Task skusForTargetFinaAnalizelTask = AnalyticalManager.FinalAnalizeSkus(ref skuForTargetReports);
            await Task.Run(() => skusForTargetFinaAnalizelTask);
            skusForTargetFinaAnalizelTask!.Wait();

            // Создение и сохранение итоговых отчетов 
            var createAndSaveFinalReportsTask = excelManager!.CreateAndSaveFinalReports(skuForTargetReports, isTracing);
            await Task.Run(() => createAndSaveFinalReportsTask);
            createAndSaveFinalReportsTask.Wait();
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
    private static async Task<double> ReceivingAndProcessingData(SourceReportItemKey reportFileKey)
    {
        var getReportFileTask = ReportFileManager.GetSourceReport(reportFileKey);
        await Task.Run(async () => await getReportFileTask);
        getReportFileTask.Wait();

        PrimaryReportFileItem? reportFile = getReportFileTask.Result;

        if (reportFile is not null && reportFile.Info is not null)
        {
            // Make ExcelPackage
            using var package = excelManager!.CreateExcelPackage(reportFileKey, reportFile.Info);

            // Receiving and processing data
            Task<double>? receivingAndProcessingDataTask = skuManager!.ReceivingAndProcessingData(reportFileKey, package!);
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
            await TracingProgramExecution(new($"No report for key: {reportFileKey}", false, false));
            return 0.0;
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

    /// <summary>
    /// Get value from manager in console.
    /// </summary>
    /// <typeparam name="T">Type of returned value.</typeparam>
    /// <param name="data">DataFromConsole object with parsing data.</param>
    /// <returns>Value for generic type or null.</returns>
    /// <remarks>oct 2025 ver.</remarks>
    private static async Task<T?> GetValueFromConsole<T>(DataFromConsole data)
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

    /// <summary>
    /// Get string value from manager in console.
    /// </summary>
    /// <param name="data">DataFromConsole object with parsing data.</param>
    /// <returns>String value or null.</returns>
    private static async Task<string?> GetStringFromConsole(DataFromConsole data)
    {
        string? value = null;
        while (value is null)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(data.QuestionMessage);

            value = Console.ReadLine();
            if (!string.IsNullOrEmpty(value))
            {
                break;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(data.ErrorMessage);
                await Task.Delay(TraceDelay + 1000);
                await GetStringFromConsole(data);
            }
        }

        return value;
    }

    private static async Task<ConsoleKey> GetKeyFromConsole(DataFromConsole data, List<ConsoleKey> validKeys)
    {
        ConsoleKey? key = null;
        while (!key.HasValue)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(data.QuestionMessage);

            key = Console.ReadKey().Key;

            if (validKeys.Contains(key.Value))
            {
                Console.Clear();
                break;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"\n{data.ErrorMessage}");
                await Task.Delay(TraceDelay + 1000);
                await GetKeyFromConsole(data, validKeys);
            }
        }
        return key.Value;
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
            UnsubscribeManagersFromEvents();
            Trace.Flush();
        }
    }

    private static void SubscribeManagersToEvents()
    {
        List<BaseManager?> managers = [reportFileManager, excelManager, excelSheetsManager, skuManager, anManager];
        foreach (var item in managers)
            if (item != null) item.ManagerReached += Report;
    }

    private static void UnsubscribeManagersFromEvents()
    {
        List<BaseManager?> managers = [reportFileManager, excelManager, excelSheetsManager, skuManager, anManager];
        foreach (var item in managers)
            if (item != null) item.ManagerReached -= Report;
    }
    #endregion
}
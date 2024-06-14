// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

// Unicode:  https://www.compart.com/en/unicode/

// Внедрить Autofac

// /Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles
// /Users/dmitrijsamarcev/Library/Mobile Documents/com~apple~CloudDocs/MPBoost/Клиенты/Юсупов 79164372997 13465

#nullable enable

using System.Diagnostics;
using System.Reflection;
using OfficeOpenXml;
using TableAnalizer.Enums;
using TableAnalizer.Objects.Managers;

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
            if(existingKeys.Contains(SourceSKUReportFileKey.RansomsWB)) 
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsWB));
            }

            if(existingKeys.Contains(SourceSKUReportFileKey.RansomsOZ)) 
            {
                ransomsTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RansomsOZ));
            }

            if(ransomsTasksList.Any()) 
            {
                ransomsTasks = ransomsTasksList.ToArray();
           
                for (int i = 0; i < ransomsTasks.Length; i++)
                {
                    var currentTask = ransomsTasks[i];
                    if(currentTask is not null) 
                    {
                        await Task.Run(async () => await currentTask);
                    }
                }
            }
            if(ransomsTasks.Any()) 
            {
                Task.WaitAll(ransomsTasks);
            }


            // Remaining tasks
            List<Task> remainingTasksList = new();
            Task[] remainingTasks = Array.Empty<Task>();
            if(existingKeys.Contains(SourceSKUReportFileKey.RemainingWB)) 
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingWB));
            }

            if(existingKeys.Contains(SourceSKUReportFileKey.RemainingOZ)) 
            {
                remainingTasksList.Add(ReceivingAndProcessingData(SourceSKUReportFileKey.RemainingOZ));
            }

            if(remainingTasksList.Any()) 
            {
                remainingTasks = remainingTasksList.ToArray();
                for (int i = 0; i < remainingTasks.Length; i++)
                {
                    var currentTask = remainingTasks[i];
                    if(currentTask is not null) 
                    {
                        await Task.Run(async () => await currentTask);
                    }
                }

                Task.WaitAll(remainingTasks);
            }

            if(remainingTasks.Any()) 
            {
                Task.WaitAll(remainingTasks);
            }

            // WriteOff task 
            if(existingKeys.Contains(SourceSKUReportFileKey.WriteOffShortages)) 
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.WriteOffShortages);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            } 

            // Shipped tasks
            if(existingKeys.Contains(SourceSKUReportFileKey.Shipped)) 
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Shipped);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            }

            // Additional tasks
            if(existingKeys.Contains(SourceSKUReportFileKey.Additional)) 
            {
                var currentTask = ReceivingAndProcessingData(SourceSKUReportFileKey.Additional);
                await Task.Run(async () => await currentTask);
                currentTask.Wait();
            } 

            // Delete old logistic report 
            FileManager.DeleteExistingFile(FileManager.OutputClientReportFilePath);
            var skusForTargetReport = skuManager.MakeSkusForTargetReport();
            excelManager.MakeReports(skusForTargetReport);

            // Report 
            var completedMessage = $"Отчет логистики сохранен по пути: {FileManager.OutputClientReportFilePath}\n"; 
            goodByMessage = string.Concat(completedMessage,goodByMessage);
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
            var processingRansomsDataTask = skuManager.ReceivingAndProcessingRansomsData(reportFileKey);
            
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

    // private static async Task ReadRedemptionReportFile(Marketplace marketplace)
    // {
    //     await Report($"Анализирую отчет клиента {login} по выкупам {marketplace}...");

    //     var filePath = marketplace == Marketplace.WB ?
    //         redemtionFilePathWB :
    //         redemtionFilePathOZ;

    //     if (filePath is null) await Report(goodByMessage, 2000, true);

    //     (double time, List<SKURowInReport> rowInReports) = ExcelReportManager.AnalyzeRedemptionReport(filePath!, "Выкупы", skuManager.RedemptionTableIndexes);
    //     var saveTime = skuManager.SaveSKURowsInReport(rowInReports, marketplace);
    //     var redemtionReport = $"Артикулов {marketplace} с логистикой, шт.: {skuManager.SKUsCount}\n";
    //     redemtionReport += $"Анализ отчета занял {time + saveTime} секунд.";

    //     await Report(redemtionReport, 1500);
    // }

    // private static async Task ReadStorageReportFile(Marketplace marketplace)
    // {
    //     bool isFileExists = default;

    //     // string? filePath = marketplace == Marketplace.WB ?
    //     //     storageFilePathWB :
    //     //     storageFilePathOZ;

    //     // try
    //     // {
    //     //     isFileExists = filePath != null && CheckFileExistence(filePath);
    //     // }
    //     // catch (Exception ex)
    //     // {
    //     //     goodByMessage = string.Concat(goodByMessage,"\n",ex.Message);   
    //     //     await Report(goodByMessage, 2000, true);
    //     // }

    //     if (!isFileExists) return;

    //     var existingFile = new FileInfo(filePath!);

    //     using ExcelPackage package = new(existingFile);

    //     // if (CheckSheets(in package) == null) await Report(goodByMessage, 2000, true);

    //     var stopwatch = new Stopwatch();
    //     stopwatch.Start();

    //     await Report($"Анализирую отчет клиента {login} по приходу и остаткам {marketplace}...");

    //     int rowFirstRoomAddress = default;
    //     int rowSecondRoomAddress = default;
    //     int rowFinalRoomAddress = default;

    //     ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

    //     int firstRowIndex = 1, firstColIndex = 1;

    //     // Find addresses of special rows
    //     for (int row = firstRowIndex; row <= worksheet.Dimension.End.Row; row++)
    //     {
    //         for (int col = firstColIndex; col <= worksheet.Dimension.End.Column; col++)
    //         {
    //             var cellValue = worksheet.Cells[row, col].Value;
    //             if (cellValue is not null && !string.IsNullOrEmpty(cellValue.ToString()))
    //             {
    //                 switch (cellValue!.ToString()!.Trim())
    //                 {
    //                     case NAME_FIRST_ROOM: rowFirstRoomAddress = row; break;
    //                     case NAME_SECOND_ROOM: rowSecondRoomAddress = row; break;
    //                     case NAME_FINAL_ROW: rowFinalRoomAddress = row; break;
    //                     default: break;
    //                 }
    //             }
    //         }

    //         if (rowFirstRoomAddress != default &&
    //             rowSecondRoomAddress != default &&
    //             rowFinalRoomAddress != default) { break; }
    //     }

    //     // make row ranges
    //     var firstRoomRowRange = Enumerable.Range(rowFirstRoomAddress + 1, rowSecondRoomAddress - 1).ToList();
    //     var secondRoomRowRange = Enumerable.Range(rowSecondRoomAddress + 1, rowFinalRoomAddress).ToList();

    //     string? goodstName;         // название товара
    //     int? goodsReceivedCount;    // приход на склад
    //     int? goodsRemainingCount;   // остаток на складе 

    //     // Get coming && finalBalance++ (firstRoom)
    //     for (int row = firstRoomRowRange.First(); row <= firstRoomRowRange.Count; row++)
    //     {
    //         // article
    //         if (skuManager.GetSKUByObjectValue(worksheet.Cells[row, skuManager.StorageTableIndexes.skuIndex].Value, out SKU? existingArticle) &&
    //             existingArticle is not null)
    //         {
    //             // name
    //             goodstName = ParseValueToString(worksheet.Cells[row, skuManager.StorageTableIndexes.nameIndex].Value);
    //             if (!string.IsNullOrEmpty(goodstName))
    //                 existingArticle.SetName(goodstName!.Split(",")[0].Trim());

    //             // goods received
    //             goodsReceivedCount = ParseValueToInt(worksheet.Cells[row, skuManager.StorageTableIndexes.goodsReceivedIndex].Value); ;
    //             if (goodsReceivedCount.HasValue)
    //                 existingArticle.ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsReceived, goodsReceivedCount.Value);

    //             // goods remaining
    //             goodsRemainingCount = ParseValueToInt(worksheet.Cells[row, skuManager.StorageTableIndexes.finBalanceIndex].Value);
    //             if (goodsRemainingCount.HasValue)
    //                 existingArticle.ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsRemaining, goodsRemainingCount.Value);
    //         }
    //     }

    //     // Get finalBalance++ (secondRoom)
    //     for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
    //     {
    //         // article
    //         if (skuManager.GetSKUByObjectValue(worksheet.Cells[row, skuManager.StorageTableIndexes.skuIndex].Value, out SKU? existingArticle) &&
    //             existingArticle is not null)
    //         {
    //             // finalBalance
    //             goodsRemainingCount = ParseValueToInt(worksheet.Cells[row, skuManager.StorageTableIndexes.finBalanceIndex].Value);

    //             if (goodsRemainingCount.HasValue)
    //                 existingArticle.ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsRemaining, goodsRemainingCount.Value);
    //         }
    //     }

    //     package.Dispose();
    //     stopwatch.Stop();

    //     // Analize goods shipped && goods remaining, report
    //     // if(ArticleManager.GetArticleGroupsStorageIndex(out IOrderedEnumerable<IGrouping<ArticeStorageIndex, ArticleStorageBalanceUnit>>? storageBalanceGroups) && 
    //     //     storageBalanceGroups is not null) 
    //     // {
    //     //     var goodsRemainingCount = ArticleManager.GetArticleStorgeBalanceCountByIndex(ArticeStorageIndex.GoodsRemaining);

    //     //     var goodsRemainingSum = storageBalanceGroups!
    //     //         .First(g => g.Key == ArticeStorageIndex.GoodsRemaining)
    //     //         .Select(g => g.Count)
    //     //         .Sum();

    //     //     var balanceReport = $"Общий приход товаров: {goodsShippedSum}, общий остаток: {goodsRemainingSum}\n";
    //     //     balanceReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

    //     //     await Report(balanceReport, 1500);
    //     // }
    // }

    private static async Task ReadRegrandingReport(string filePath)
    {
        const int ARTICLE_INDEX = 1;
        const int REGRANDING_INDEX = 2;
        const int COUNT_INDEX = 3;
        const int COMMENT_INDEX = 4;

        // if (filePath is null || CheckFileExistence(filePath) == null) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using ExcelPackage package = new(new FileInfo(filePath));

        // if (CheckSheets(in package) == null) return;

        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

        int regrandingCount = default;

        for (int row = 1; row <= rowCount; row++)
        {
            var articleAddress = worksheet.Cells[row, ARTICLE_INDEX];
            var regrandingAddress = worksheet.Cells[row, REGRANDING_INDEX];
            var countAddress = worksheet.Cells[row, COUNT_INDEX];
            var commentAddress = worksheet.Cells[row, COMMENT_INDEX];

            string? commentForArticle = default;
            string? commentForRegranding = default;

            // Set article regranding 
            // if (articleAddress.Value is not null &&
            //     int.TryParse(articleAddress.Value.ToString(), out int article) &&
            //     articleDict.ContainsKey(article) &&
            //     regrandingAddress.Value is not null &&
            //     int.TryParse(regrandingAddress.Value.ToString(), out int regranding) &&
            //     articleDict.ContainsKey(regranding) &&
            //     countAddress.Value is not null &&
            //     int.TryParse(countAddress.Value.ToString(), out int count)
            //     )
            // {
            //     // Для текущего арта указываем артикул пересорта (regranding)
            //     articleDict[article].StoreBalance.SetRegrading(regranding, count);

            //     commentForArticle = $"пересорт на арт {regranding} ({count} шт) ";
            //     commentForRegranding = $"пересорт от арт {article} ({count} шт) ";

            //     if (commentAddress.Value is not null &&
            //         !string.IsNullOrEmpty(commentAddress.Value.ToString()))
            //     {
            //         commentForArticle = string.Concat(commentForArticle, commentAddress.Value!.ToString()!.Trim());
            //     }

            //     articleDict[article].SetComment(commentForArticle);
            //     articleDict[regranding].SetComment(commentForRegranding);

            //     regrandingCount += count;
            // }
        }

        package.Dispose();
        stopwatch.Stop();

        var regReport = $"Количество товаров в пересорте: {regrandingCount}.\n";
        regReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(regReport, 1500);
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

    private static (int col, int row) FindNumberColsAndRowsIn(in ExcelWorksheet sheet)
    {
        return (sheet.Dimension.End.Column, sheet.Dimension.End.Row);
    }

    #endregion
}
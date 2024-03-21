// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

#nullable enable

using System.Diagnostics;
using System.Reflection;
using OfficeOpenXml;
using ClientTableAnalizer.Objects;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.ArticleItems;

internal class Program
{
    #region Variables and constants
    private static readonly Version? AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
    private const string NAME_FIRST_ROOM = "1 - Помещение приемки, сортировки и упаковки";
    private const string NAME_SECOND_ROOM = "2 - Помещение длительного хранения и отгрузки";
    private const string NAME_FINAL_ROW = "Итого товаров в единицах хранения";
    private static string? login = default;
    private static string? redemtionFilePathWB = default;
    private static string? redemtionFilePathOZ = default;
    private static string? storageFilePathWB = default;
    private static string? storageFilePathOZ = default;
    private static string? consumptionPath, payShortagesPath, regradingPath, dummyPath = default;
    private static string? writeoffShortagesPath = default;
    private static DirectoryInfo? clientDirectoryPath;
    private static List<ArticleShortage> ArticleShortages { get; set; } = new();
    private static readonly List<ArticleItemForReport> ShortagesInWarehouse = new();
    private static readonly List<ArticleItemForReport> ShortagesInPickupPoints = new();
    private static readonly List<ArticleItemForReport> Overruns = new();
    private static string goodByMessage = "\nСпасибо за использование программы. Хорошего дня!";

    #endregion

    #region Properties 
    private static ArticleManager articleManager = new ();
    private static ExcelReportManager ExcelReportManager { get; } = new ();
    #endregion

    #region Functionality
    
    private static async Task Main(string[] args)
    {

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        Console.Clear();

        var helloString = $"" +
            $"--------------------------------------------\n" +
            $"Программа формирования отчета по клиенту логистики\n" +
            $"Версия: {AppVersion}\n" +
            $"---------------------------------------------";

        Console.WriteLine(helloString);

        // Set deadline days
        int deadLineDays = await GetDeadLineDays();
        articleManager.SetDeadlineDays(deadLineDays);

        await GetUserRedemtionDataFile(Marketplace.WB, false);

        await GetUserRedemtionDataFile(Marketplace.OZ, false);

        if(string.IsNullOrEmpty(redemtionFilePathWB) && string.IsNullOrEmpty(redemtionFilePathOZ))
        {
            var goodByMessage02 = string.Concat($"Данные по заказам клиента не предоставлены, к сожалению невозможно сформировать отчет.\n", goodByMessage);
            await Report(goodByMessage02, 2000, true);
        }

        Task[] readRedemptionTasksArray = new Task[2];

        if(!string.IsNullOrEmpty(redemtionFilePathWB))
            readRedemptionTasksArray[0] = ReadRedemptionReportFile(Marketplace.WB);

        if (!string.IsNullOrEmpty(redemtionFilePathOZ))
            readRedemptionTasksArray[1] = ReadRedemptionReportFile(Marketplace.OZ);

        if(readRedemptionTasksArray.Length > 0) 
        {
            foreach (var task in readRedemptionTasksArray)
            {
                _ = Task.Run (async () => await task) ;
            }
        } 
        else 
        {
            // TODO: Ругаемся и выходим
        }

        Task.WaitAll(readRedemptionTasksArray);
        
        //await GetUserStorageData(Marketplace.WB);

        //await GetUserStorageData(Marketplace.OZ);

       // if (!string.IsNullOrEmpty(storageFilePathWB))
         //   await ReadStorageReportFile(Marketplace.WB);

       // if (!string.IsNullOrEmpty(storageFilePathOZ))
         //   await ReadStorageReportFile(Marketplace.OZ);

       // await GetUserConsumingData();

        // if (!string.IsNullOrEmpty(consumptionPath))
        //     await ReadConsumptionReport(consumptionPath, ArticeStorageIndex.GoodsShipped, 4, 28);

        // await GetWriteoffShortagesData();

        // if (!string.IsNullOrEmpty(writeoffShortagesPath))
        //     await ReadConsumptionReport(writeoffShortagesPath, ArticeStorageIndex.WriteoffShortages, 5, 39);

        // await GetUserPayShortagesData();

        // if (!string.IsNullOrEmpty(consumptionPath))
        //     await ReadPayShortagesReport(payShortagesPath);

        // await GetRegrandingData();

        // if (!string.IsNullOrEmpty(regradingPath))
        //     await ReadRegrandingReport(regradingPath);

        // await GetDummyData();
        // if (!string.IsNullOrEmpty(dummyPath))
        //     await ReadDummyReport(dummyPath);

        Task analizeAndReportTask = readRedemptionTasksArray.Last().ContinueWith(_ => 
              AnalyzeDataAndSaveReport()
        );

       // Task analizeAndReportTask = new(async () => await AnalyzeDataAndSaveReport());
        
        analizeAndReportTask.Wait();

       
    }
    
    private static string? ParseValueToString(object? obj) =>
        obj is not null && !string.IsNullOrEmpty(obj.ToString()) ?
        obj.ToString()!.Trim() :
        null;
    
    private static int? ParseValueToInt(object? obj) =>
        obj is not null && !string.IsNullOrEmpty(obj.ToString()) ?
        int.Parse(obj.ToString()!.Trim()) :
        null;
    
    private static async Task<int> GetDeadLineDays()
    {
        int deadline = default;

        while(deadline == default)
        {
            Console.WriteLine("Введите срок дедлайна для клиента\n(положительное целое число, отличное от 0): ");

            if(int.TryParse(Console.ReadLine(), out deadline))
            {
                if (deadline > 0)
                {
                    await Report($"Срок дедлайна для клиента, дней: {deadline}");
                    break;
                }
                else
                {
                    await Report("Введено неправильное значение, повторите попытку.");
                    deadline = default;
                    continue;
                }
            }
        }

        return deadline;
    }
    
    private static async Task GetUserRedemtionDataFile(Marketplace marketplace, bool necessarily)
    {
        var clDirInfo = FileManager.GetDirectoryInfo(PathKey.MPBoostClients);
        if (clDirInfo is null || !clDirInfo.Exists)
            FileManager.CreateDirectoryInfo(PathKey.MPBoostClients);

        var necessarilyMessage = necessarily ? "обязательно" : "не обязательно";
        var messageInstruction = necessarily ? "ESC - выход" : "1 - пропустить, ESC - выход";
        var redemtionMessage01 = $"Перетащите путь к отчету по выкупам клиента на {marketplace} ({necessarilyMessage})\n({messageInstruction}):\n";

        if(marketplace == Marketplace.WB)
        {
            redemtionFilePathWB = await GetDirectoryPath(redemtionMessage01, necessarily, login is null);
        }
        else if(marketplace == Marketplace.OZ)
        {
            redemtionFilePathOZ = await GetDirectoryPath(redemtionMessage01, necessarily, login is null);
        }

        try
        {
            var clientDirectoryPath = await ExcelReportManager.GetClientOutputPath();
            if(!string.IsNullOrEmpty(clientDirectoryPath)) 
            {
                var reportMessage = $"Директория для работы с клтентом: {clientDirectoryPath}";
                await Report(reportMessage, 1000, false);
            }
        }
        catch (System.Exception ex) 
        { 
            Console.WriteLine(ex.Message);    
            throw;
        }


        
    }

    private static async Task GetUserStorageData(Marketplace marketplace)
    {
        var necessarilyMessage = marketplace == Marketplace.WB ? 
            "обязательно" : 
            "не обязательно";

        var messageInstruction = marketplace == Marketplace.WB ? 
            "ESC - выход" : 
            "1 - пропустить, ESC - выход";
            
        var storagePathMessage = $"Перетащите путь к отчету по приходу и остаткам товаров {marketplace} ({necessarilyMessage})\n({messageInstruction}):";

        switch (marketplace)
        {
            case Marketplace.WB:
                storageFilePathWB = await GetDirectoryPath(storagePathMessage, false, false);
                break;
            case Marketplace.OZ:
                storageFilePathOZ = await GetDirectoryPath(storagePathMessage, false, false);
                break;
        }

        Console.WriteLine();
    }

    private static async Task GetWriteoffShortagesData()
    {
        var writeoffShortagesMessage = "Перетащите путь к отчету по оприходованию и списанию недостач (обязательно)\n" +
           "(или нажмите ESC для выхода из программы):";
        writeoffShortagesPath = await GetDirectoryPath(writeoffShortagesMessage, true, false);
        Console.WriteLine();
    }

    private static async Task GetUserConsumingData()
    {
        var consumingDataMessage = "Перетащите путь к отчету по расходу товаров клиента со склада (обязательно)\n" +
                "(или нажмите ESC для выхода из программы):";
        consumptionPath = await GetDirectoryPath(consumingDataMessage, true, false);
        Console.WriteLine();
    }

    private static async Task GetUserPayShortagesData()
    {
        var payShortagesDataMessage = "Перетащите путь к отчету по недостачам (не обязательно)\n" +
           "(1 - пропустить, ESC - выход из программы):";
        payShortagesPath = await GetDirectoryPath(payShortagesDataMessage, false, false);
        Console.WriteLine();
    }

    private static async Task GetRegrandingData()
    {
        var regrandingDataMessage = "Перетащите путь к отчету по пересортам (не обязательно)\n" +
          "(1 - пропустить, ESC - выход):";
        regradingPath = await GetDirectoryPath(regrandingDataMessage, false, false);
        Console.WriteLine();
    }

    private static async Task GetDummyData()
    {
        var dummyDataMessage = "Перетащите путь к отчету по пустышкам (не обязательно)\n" +
            "(1 - пропустить, ESC - выход):";
        dummyPath = await GetDirectoryPath(dummyDataMessage, false, false);
        Console.WriteLine();
    }

    private static async Task ReadRedemptionReportFile(Marketplace marketplace)
    {
        await Report($"Анализирую отчет клиента {login} по выкупам {marketplace}...");

        var filePath = marketplace == Marketplace.WB ? 
            redemtionFilePathWB : 
            redemtionFilePathOZ;

        if(filePath is null) await Report(goodByMessage, 2000, true);

        (double time, List<ArticleRowInReport> rowInReports) = ExcelReportManager.AnalyzeRedemptionReport(filePath!, "Выкупы", articleManager.RedemptionTableIndexes);
        var saveTime = articleManager.SaveArticleRowsInReport(rowInReports, marketplace);
        var redemtionReport = $"Артикулов {marketplace} с логистикой, шт.: {articleManager.ArticlesCount}\n";
        redemtionReport += $"Анализ отчета занял {time + saveTime} секунд.";

        await Report(redemtionReport, 1500);
    }

    private static async Task ReadStorageReportFile(Marketplace marketplace)
    {
        bool isFileExists = default;

        string? filePath = marketplace == Marketplace.WB ? 
            storageFilePathWB : 
            storageFilePathOZ;

        // try
        // {
        //     isFileExists = filePath != null && CheckFileExistence(filePath);
        // }
        // catch (Exception ex)
        // {
        //     goodByMessage = string.Concat(goodByMessage,"\n",ex.Message);   
        //     await Report(goodByMessage, 2000, true);
        // }

        if(!isFileExists) return;

        var existingFile = new FileInfo(filePath!);

        using ExcelPackage package = new(existingFile);

        // if (CheckSheets(in package) == null) await Report(goodByMessage, 2000, true);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Report($"Анализирую отчет клиента {login} по приходу и остаткам {marketplace}...");

        int rowFirstRoomAddress = default;
        int rowSecondRoomAddress = default;
        int rowFinalRoomAddress = default;
  
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        int firstRowIndex = 1, firstColIndex = 1;

        // Find addresses of special rows
        for (int row = firstRowIndex; row <=  worksheet.Dimension.End.Row; row++)
        {
            for (int col = firstColIndex; col <=  worksheet.Dimension.End.Column; col++)
            {
                var cellValue = worksheet.Cells[row, col].Value;
                if (cellValue is not null && !string.IsNullOrEmpty(cellValue.ToString()))
                {
                    switch (cellValue!.ToString()!.Trim())
                    {
                        case NAME_FIRST_ROOM: rowFirstRoomAddress = row; break;
                        case NAME_SECOND_ROOM: rowSecondRoomAddress = row; break;
                        case NAME_FINAL_ROW: rowFinalRoomAddress = row; break;
                        default: break;
                    }
                }
            }

            if (rowFirstRoomAddress != default &&
                rowSecondRoomAddress != default &&
                rowFinalRoomAddress != default) { break; }
        }

        // make row ranges
        var firstRoomRowRange = Enumerable.Range(rowFirstRoomAddress + 1, rowSecondRoomAddress - 1).ToList();
        var secondRoomRowRange = Enumerable.Range(rowSecondRoomAddress + 1, rowFinalRoomAddress).ToList();

        string? goodstName;         // название товара
        int? goodsReceivedCount;    // приход на склад
        int? goodsRemainingCount;   // остаток на складе 

        // Get coming && finalBalance++ (firstRoom)
        for (int row = firstRoomRowRange.First(); row <= firstRoomRowRange.Count; row++)
        {
            // article
            if(articleManager.GetArticleByObjectValue(worksheet.Cells[row, articleManager.StorageTableIndexes.articleIndex].Value, out Article? existingArticle) && 
                existingArticle is not null) 
            {
                // name
                goodstName = ParseValueToString(worksheet.Cells[row, articleManager.StorageTableIndexes.nameIndex].Value);
                if (!string.IsNullOrEmpty(goodstName)) 
                    existingArticle.SetName(goodstName!.Split(",")[0].Trim());
                
                // goods received
                goodsReceivedCount = ParseValueToInt(worksheet.Cells[row, articleManager.StorageTableIndexes.goodsReceivedIndex].Value); ;
                if (goodsReceivedCount.HasValue)
                    existingArticle.ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsReceived, goodsReceivedCount.Value);

                // goods remaining
                goodsRemainingCount = ParseValueToInt(worksheet.Cells[row, articleManager.StorageTableIndexes.finBalanceIndex].Value);
                if (goodsRemainingCount.HasValue)
                    existingArticle.ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsRemaining, goodsRemainingCount.Value);
            }
        }

        // Get finalBalance++ (secondRoom)
        for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
        {
            // article
            if(articleManager.GetArticleByObjectValue(worksheet.Cells[row, articleManager.StorageTableIndexes.articleIndex].Value, out Article? existingArticle) && 
                existingArticle is not null) 
            {
                // finalBalance
                goodsRemainingCount = ParseValueToInt(worksheet.Cells[row, articleManager.StorageTableIndexes.finBalanceIndex].Value);

                if (goodsRemainingCount.HasValue)
                    existingArticle.ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsRemaining, goodsRemainingCount.Value);
            }
        }

        package.Dispose();
        stopwatch.Stop();

        // Analize goods shipped && goods remaining, report
        // if(ArticleManager.GetArticleGroupsStorageIndex(out IOrderedEnumerable<IGrouping<ArticeStorageIndex, ArticleStorageBalanceUnit>>? storageBalanceGroups) && 
        //     storageBalanceGroups is not null) 
        // {
        //     var goodsRemainingCount = ArticleManager.GetArticleStorgeBalanceCountByIndex(ArticeStorageIndex.GoodsRemaining);
  
        //     var goodsRemainingSum = storageBalanceGroups!
        //         .First(g => g.Key == ArticeStorageIndex.GoodsRemaining)
        //         .Select(g => g.Count)
        //         .Sum();

        //     var balanceReport = $"Общий приход товаров: {goodsShippedSum}, общий остаток: {goodsRemainingSum}\n";
        //     balanceReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        //     await Report(balanceReport, 1500);
        // }
    }

    private static async Task ReadConsumptionReport(string? filePath, ArticeStorageIndex storeUnit, int artIndex, int countIndex)
    {
        // if (filePath is null && CheckFileExistence(filePath!) == null)
        //     await Report(goodByMessage, 2000, true);
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using ExcelPackage package = new(new FileInfo(filePath!));

        // if (CheckSheets(in package) == null) await Report(goodByMessage, 2000, true);

        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

        for (int row = 1; row <= rowCount; row++)
        {
            var articleAddress = worksheet.Cells[row, artIndex];
            var countAddress = worksheet.Cells[row, countIndex];

            if (articleAddress.Value != null && int.TryParse(articleAddress.Value.ToString(), out int article))
            {
                if (countAddress.Value != null && int.TryParse(countAddress.Value.ToString(), out int count))
                {
                    // if (articleDict.ContainsKey(article))
                    // {
                    //     articleDict[article].SetStoreBalance(storeUnit, count);
                    // }
                }
            }
        }

        package.Dispose();
        stopwatch.Stop();

        int result = 0;

        // switch (storeUnit)
        // {
        //     case ArticeStorageIndex.GoodsShipped:
        //         result = articleDict.Sum(art => art.Value.StoreBalance.ShippedQuantity);
        //         break;
 
        //     case ArticeStorageIndex.WriteoffShortages:
        //         result = articleDict.Sum(art => art.Value.StoreBalance.WriteoffShortages);
        //         break;
        //     default:
        //         break;
        // }

        var targetMessage = storeUnit == ArticeStorageIndex.GoodsShipped ? "Общий расход по товарам клиента" : "Оприходовано и списано по недостачам от ПВЗ";
        var reportMessage = $"{targetMessage}: {result}.\n";
        reportMessage += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(reportMessage, 1500);
    }

    private static async Task ReadPayShortagesReport(string? filePath)
    {
        const int ARTICLE_INDEX = 1;
        const int COUNT_INDEX = 2;
        const int PRICE_INDEX = 3;

  
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using ExcelPackage package = new(new FileInfo(filePath));

            // if (CheckSheets(in package) == null) return;

            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

            for (int row = 1; row <= rowCount; row++)
            {
                var articleAddress = worksheet.Cells[row, ARTICLE_INDEX];
                var countAddress = worksheet.Cells[row, COUNT_INDEX];
                var priceAddress = worksheet.Cells[row, PRICE_INDEX];

                if (articleAddress.Value != null && int.TryParse(articleAddress.Value.ToString(), out int article) &&
                    countAddress.Value != null && int.TryParse(countAddress.Value.ToString(), out int count) &&
                    priceAddress.Value != null && double.TryParse(priceAddress.Value.ToString(), out double price))
                {
                    // if (articleDict.ContainsKey(article))
                    // {
                    //     articleDict[article].SetStoreBalance(count, price);
                    // }
                }
            }

            package.Dispose();
            stopwatch.Stop();

            // var paySum = articleDict
            //     .Aggregate(0.00, (acc, el) => acc + el.Value.StoreBalance.PayShortagesSum);

            string payReport;

            // if (paySum > 0.00)
            // {
            //     payReport = string.Format("Сумма компенсации за недостачи товаров: {0:C}.\n", paySum);
            // }
            // else
            // {
            //     payReport = "Нет погашений недостач по указанным артикулам";
            // }

            //payReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

            //await Report(payReport, 1500);
        
    }

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

    private static async Task ReadDummyReport(string filePath)
    {
        const int ARTICLE_INDEX = 1; const int COUNT_INDEX = 2;

        // if (filePath is null || CheckFileExistence(filePath) == null) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using ExcelPackage package = new(new FileInfo(filePath));

        // if (CheckSheets(in package) == null) return;

        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

        int dummyCount = default;

        for (int row = 1; row <= rowCount; row++)
        {
            var articleAddress = worksheet.Cells[row, ARTICLE_INDEX];
            var countAddress = worksheet.Cells[row, COUNT_INDEX];

            // Set article dummy 
            // if (articleAddress.Value is not null &&
            //     int.TryParse(articleAddress.Value.ToString(), out int article) &&
            //     articleDict.ContainsKey(article) &&
            //     countAddress.Value is not null &&
            //     int.TryParse(countAddress.Value.ToString(), out int count)
            //     )
            // {
            //     articleDict[article].StoreBalance.SetDummy(count);
            //     dummyCount += count;
            // }
        }

        package.Dispose();
        stopwatch.Stop();

        var regReport = $"Количество товаров в пустышках: {dummyCount}.\n";
        regReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(regReport, 1500);
    }

    private static async Task AnalyzeDataAndSaveReport()
    {
        try
        {
            articleManager.AnalyzeArticles();

            var outputPath = ExcelReportManager.CreateMainReportSheet("Отчет", articleManager.Articles);
            //ExcelReportManager.SaveReportFiles();

            Console.WriteLine($"Новый отчет по клиенту создан по пути: {outputPath}");
            Console.WriteLine();
            Console.WriteLine("Для завершения работы нажмите любую клавишу");

            await Report(goodByMessage, 1500, true);

            _ = Console.ReadKey();
        }
        catch (Exception ex)
        {
            goodByMessage = string.Concat(ex.Message, "\n", goodByMessage);
            await Report(goodByMessage, 2000, true);
        }
        
       
 

      
      
     
        // ResultCell[] resultCellsForAddReports = new ResultCell[]
        // {
        //     new (1, "Артикул", 13, "0", false, 0),                         
        //     new (2, "Название товара", 60, "0", true, 0),                 
        //     new (3, "Кол-во, шт.",11, "0", true, 0),                 
        //     new (4, "Средняя цена, руб.",11, "0.00", true, 0),                 
        //     new (5, "Сумма, руб.", 11, "0.00", true, 0),                        
        // };

        // Analize ArticleShortages
        // if (ArticleShortages.Count > 0)
        // {
        //     foreach (var articleShortage in ArticleShortages)
        //     {
        //         var articleShortagesInWarehouse = articleShortage.GetShortagesCountByPlace(ShortagesPlace.Warehouse);
        //         if (articleShortagesInWarehouse > 0)
        //         {
        //             var shortageInWarehouse = new ArticleItemForReport(articleShortage.ArticleNumber, articleShortage.ArtileName, articleShortagesInWarehouse, articleShortage.AveragePrice, articleShortage.AveragePrice * articleShortagesInWarehouse);
        //             ShortagesInWarehouse.Add(shortageInWarehouse);
        //         }

        //         var articleShortagesInPickupPoint = articleShortage.GetShortagesCountByPlace(ShortagesPlace.PickupPoint);
        //         if (articleShortagesInPickupPoint > 0)
        //         {
        //             var shortageInPickupPoints = new ArticleItemForReport(articleShortage.ArticleNumber, articleShortage.ArtileName, articleShortagesInPickupPoint, articleShortage.AveragePrice, articleShortage.AveragePrice * articleShortagesInPickupPoint);
        //             ShortagesInPickupPoints.Add(shortageInPickupPoints);
        //         }
        //     }

        //     // Shortages in warehouse report
        //     if (ShortagesInWarehouse.Count > 0)
        //     {
        //         // Create new worksheet
        //         CreateWorkSheet(in excelPackage, "Недовозы (склад)", out ExcelWorksheet shortagesInWarehouseWorksheet, System.Drawing.Color.Orange);

        //         // Make report in sheet
        //         MakeAdditionalReport(resultCellsForAddReports, ref shortagesInWarehouseWorksheet, ShortagesInWarehouse);
        //     }

        //     // Shortages in pickup report
        //     if (ShortagesInPickupPoints.Count > 0)  
        //     {
        //         // Create new worksheet
        //         CreateWorkSheet(in excelPackage, "Недовозы (ПВЗ)", out ExcelWorksheet shortagesInPickupPointsWorksheet, System.Drawing.Color.Orange);

        //         // Make report in sheet
        //         MakeAdditionalReport(resultCellsForAddReports, ref shortagesInPickupPointsWorksheet, ShortagesInPickupPoints);
        //     }
        // }

        // HACK: Плохо, переделать логику, двойной проход
        // foreach (var (key, value) in articleDict)
        // {
        //     var regradingArt = articleDict
        //         .Any(art => art.Value.StoreBalance.Regradings.ContainsKey(key));

        //     if (articleDict[key].Overrun != null && !regradingArt)
        //         Overruns.Add(articleDict[key].Overrun!);
        // }

        // // Analize ArticleShortages
        // if (Overruns.Count > 0)
        // {
        //     // Create new worksheet
        //     CreateWorkSheet(in excelPackage, "Перерасходы (склад)", out ExcelWorksheet overrunsWorksheet, System.Drawing.Color.ForestGreen);

        //     // Fill header data 
        //     SetupTableHeader(in resultCellsForAddReports, ref overrunsWorksheet, 55);

        //     // Make report in sheet
        //     MakeAdditionalReport(resultCellsForAddReports, ref overrunsWorksheet, Overruns);
        //}
    }

    private static async Task Report(string message, int delay = 0, bool exit = false)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine();

        await Task.Delay(delay);

        if (exit)
        {
            Environment.Exit(0);
        }
    }

    private static string FormСorrectPath(string path)
    {
        var copy = path;

        // Clear "\"
        var charArray = copy.Trim().ToList();
        for (int i = 0; i < charArray.Count; i++)
        {
            if (charArray[i] == '\u005c') charArray.RemoveAt(i);
        }

        copy = new string(charArray.ToArray());

        // Add "/"
        return $"/{copy}";
    }

    // private static bool CheckFileExistence(string path)
    // {
    //     if (File.Exists(path))return true;

    //     throw new FileNotFoundException($"Файл по пути\n{path}\nне найден. Проверьте правильность указания данных.");
    // }

    private static async Task<string?> GetDirectoryPath(string message, bool necessarily, bool giveMeLogin)
    {
        string? path = default;

        while(string.IsNullOrEmpty(path))
        {
            Console.Clear();
            Console.WriteLine(message);

            var key = Console.ReadKey();

            List<ConsoleKey> exitKeys = new(2) { ConsoleKey.D1 , ConsoleKey.Escape };

            if(exitKeys.Contains(key.Key))
            {
                if (key.Key == ConsoleKey.D1 && !necessarily) break;
                if (key.Key == ConsoleKey.Escape) await Report(goodByMessage, 2000, true);
            }
            else
            {
                path = Console.ReadLine();
            }
        }

        if (path is null) return null;

        if (path is not null && giveMeLogin) 
            ExcelReportManager.DeterminingСlientLogin(path);

        return FormСorrectPath(path!);
    }

    private static (int col, int row) FindNumberColsAndRowsIn(in ExcelWorksheet sheet)
    {
        return (sheet.Dimension.End.Column, sheet.Dimension.End.Row);
    }

    // public static void CreateWorkSheet(in ExcelPackage excelPackage, string workSheetName, out ExcelWorksheet worksheet, System.Drawing.Color tabColor)
    // {
    //     // Remove existing worksheet
    //     var existingWorksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(x => x.Name == workSheetName);
    //     if (existingWorksheet is not null)
    //         excelPackage.Workbook.Worksheets.Delete(existingWorksheet);

    //     // Create net worksheet
    //     worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);

    //     // Stilized workSheet
    //     worksheet.TabColor = tabColor;
    //     worksheet.DefaultRowHeight = DEFAULT_TABLE_ROW_HEIGHT;
    //     worksheet.DefaultColWidth = TABLE_COL_WIDTH;
    // }

    private static void MakeAdditionalReport(ResultCell[] cells, ref ExcelWorksheet worksheet, List<ArticleItemForReport> items)
    {
        // // Fill header data 
        // SetupTableHeader(cells, ref worksheet, 55);

        // // Fill table data 
        // int recordIndex = FIRST_ROW_INDEX + 1;
        // foreach (var item in items)
        // {
        //     worksheet.Cells[recordIndex, 1].Value = item.ArticleNumber;
        //     worksheet.Cells[recordIndex, 2].Value = item.Name;
        //     worksheet.Cells[recordIndex, 3].Value = item.Count;
        //     worksheet.Cells[recordIndex, 4].Value = item.Cost;
        //     worksheet.Cells[recordIndex, 5].Value = item.Sum;

        //     recordIndex++;
        // }

        // // Fill total sum
        // worksheet.Cells[recordIndex, 5].Value = items.Aggregate(0.00, (acc, i) => acc + i.Sum);

        // // Stilized table - fit text
        // worksheet.Column(2).Width = 60;
        // for (int i = 1; i < cells.Length + 1; i++)
        // {
        //     worksheet.Column(i).Style.WrapText = true;
        // }

        // AddFilterInWorkSheet(ref worksheet);
    }
    #endregion
}
// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

#nullable enable

using System.Diagnostics;
using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ClientTableAnalizer.Objects;
using static System.Runtime.InteropServices.JavaScript.JSType;

internal enum Marketplace { WB, OZ }

internal class Program
{
    #region Variables and constants

    private static int deadLineDays;
    private const string LOGISTIC_FLAG = "Да";
    private const string NAME_FIRST_ROOM = "1 - Помещение приемки, сортировки и упаковки";
    private const string NAME_SECOND_ROOM = "2 - Помещение длительного хранения и отгрузки";
    private const string NAME_FINAL_ROW = "Итого товаров в единицах хранения";

    private static string? login = default;
    private static string? redemtionFilePathWB = default;
    private static string? redemtionFilePathOZ = default;
    private static string? storageFilePathWB = default;
    private static string? storageFilePathOZ = default;

    private static string? consumptionPath = default, payShortagesPath = default, regradingPath = default, outputPath = default;

    private static string? writeoffShortagesPath = default;

    private static DirectoryInfo? clientDirectoryPath;

    private static readonly Dictionary<int, Article> articleDict = new();

    private static List<ArticleShortage> ArticleShortages { get; set; } = new();

    private static readonly List<(int article, string name, int count, double cost, double sum)> ShortagesInWarehouse = new();

    private static readonly List<(int article, string name, int count, double cost, double sum)> ShortagesInPickupPoints = new();

    private static readonly string goodByMessage = "\nСпасибо за использование программы. Хорошего дня!";

    #endregion

    private static async Task Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

        Console.Clear();

        var helloString = $"" +
            $"--------------------------------------------\n" +
            $"Программа анализа работы с клиентом логистики\n" +
            $"Версия: {appVersion}\n" +
            $"---------------------------------------------";

        Console.WriteLine(helloString);

        deadLineDays = await GetDeadLineDays();

        await GetUserRedemtionDataFile(Marketplace.WB, false);

        await GetUserRedemtionDataFile(Marketplace.OZ, false);

        if(string.IsNullOrEmpty(redemtionFilePathWB) && string.IsNullOrEmpty(redemtionFilePathOZ))
        {
            var goodByMessage02 = string.Concat($"Данные по выкупам не предоставлены, невозможно сформировать отчет\n", goodByMessage);
            await Report(goodByMessage02, 2000, true);
        }

        if(!string.IsNullOrEmpty(redemtionFilePathWB))
            await ReadRedemptionReportFile(Marketplace.WB);

        if (!string.IsNullOrEmpty(redemtionFilePathOZ))
            await ReadRedemptionReportFile(Marketplace.OZ);

        await GetUserStorageData(Marketplace.WB);

        await GetUserStorageData(Marketplace.OZ);

        if (!string.IsNullOrEmpty(storageFilePathWB))
            await ReadStorageReportFile(Marketplace.WB);

        if (!string.IsNullOrEmpty(storageFilePathOZ))
            await ReadStorageReportFile(Marketplace.OZ);

        await GetUserConsumingData();

        if (!string.IsNullOrEmpty(consumptionPath))
            await ReadConsumptionReport(consumptionPath, ItemBalanceUnit.Consumption, 4, 28);

        await GetWriteoffShortagesData();

        if (!string.IsNullOrEmpty(writeoffShortagesPath))
            await ReadConsumptionReport(writeoffShortagesPath, ItemBalanceUnit.WriteoffShortages, 5, 39);

        await GetUserPayShortagesData();

        if (!string.IsNullOrEmpty(consumptionPath))
            await ReadPayShortagesReport(payShortagesPath);

        await GetRegrandingData();

        if (!string.IsNullOrEmpty(regradingPath))
            await ReadRegrandingReport(regradingPath);

        await WriteXls(outputPath);

        Console.ReadLine();
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

        while(deadline == default || deadline == 0)
        {
            Console.WriteLine("Введите срок дедлайна для клиента\n(положительное целое число, отличное от 0): ");

            bool success = int.TryParse(Console.ReadLine(), out deadline);

            if(success && deadline > 0)
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
            redemtionFilePathWB = await GiveMePath(redemtionMessage01, necessarily, login is null);
        }
        else if(marketplace == Marketplace.OZ)
        {
            redemtionFilePathOZ = await GiveMePath(redemtionMessage01, necessarily, login is null);
        }

        await GetClientOutputPath();
    }

    private static async Task GetUserStorageData(Marketplace marketplace)
    {
        var necessarilyMessage = marketplace == Marketplace.WB ? "обязательно" : "не обязательно";
        var messageInstruction = marketplace == Marketplace.WB ? "ESC - выход" : "1 - пропустить, ESC - выход";
        var storagePathMessage = $"Перетащите путь к отчету по приходу и остаткам товаров {marketplace} ({necessarilyMessage})\n({messageInstruction}):";

        switch (marketplace)
        {
            case Marketplace.WB:
                storageFilePathWB = await GiveMePath(storagePathMessage, false, false);
                break;
            case Marketplace.OZ:
                storageFilePathOZ = await GiveMePath(storagePathMessage, false, false);
                break;
        }

        Console.WriteLine();
    }

    private static async Task GetWriteoffShortagesData()
    {
        var writeoffShortagesMessage = "Перетащите путь к отчету по оприходованию и списанию недостач (обязательно)\n" +
           "(или нажмите ESC для выхода из программы):";
        writeoffShortagesPath = await GiveMePath(writeoffShortagesMessage, true, false);
        Console.WriteLine();
    }

    private static async Task GetUserConsumingData()
    {
        var consumingDataMessage = "Перетащите путь к отчету по расходу товаров клиента со склада (обязательно)\n" +
                "(или нажмите ESC для выхода из программы):";
        consumptionPath = await GiveMePath(consumingDataMessage, true, false);
        Console.WriteLine();
    }

    private static async Task GetUserPayShortagesData()
    {
        var payShortagesDataMessage = "Перетащите путь к отчету по недостачам (не обязательно)\n" +
           "(1 - пропустить, ESC - выход из программы):";
        payShortagesPath = await GiveMePath(payShortagesDataMessage, false, false);
        Console.WriteLine();
    }

    private static async Task GetRegrandingData()
    {
        var regrandingDataMessage = "Перетащите путь к отчету по пересортам (не обязательно)\n" +
          "(1 - пропустить, ESC - выход):";
        regradingPath = await GiveMePath(regrandingDataMessage, false, false);
        Console.WriteLine();
    }

    private static async Task ReadRedemptionReportFile(Marketplace marketplace)
    {
        const int ARTICLE_COL_INDEX = 2;
        const int DATE_FLAG_COL_INDEX = 7;
        const int STATUS_COL_INDEX = 9;
        const int LOG_FLAG_COL_INDEX = 10;
        const int PRICE_COL_INDEX = 14;

        switch (marketplace)
        {
            case Marketplace.WB:
                if (redemtionFilePathWB is null || CheckFileExistence(redemtionFilePathWB) == null) await Report(goodByMessage, 2000, true);
                break;
            case Marketplace.OZ:
                if (redemtionFilePathOZ is null || CheckFileExistence(redemtionFilePathOZ) == null) await Report(goodByMessage, 2000, true);
                break;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Report($"Анализирую отчет клиента {login} по выкупам {marketplace}...");

        var filePath = marketplace == Marketplace.WB ? redemtionFilePathWB : redemtionFilePathOZ;

        using ExcelPackage package = new(new FileInfo(filePath!));

        if (CheckSheets(in package) == null) Environment.Exit(0);

        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

        for (int row = 1; row <= rowCount; row++)
        {
            var logisticFlagValue = worksheet.Cells[row, LOG_FLAG_COL_INDEX].Value.ToString();

            if (!string.IsNullOrEmpty(logisticFlagValue))
            {
                var logisticFlag = logisticFlagValue.Replace(" ", "");

                if (logisticFlag == LOGISTIC_FLAG)
                {
                    var article = ParseValueToInt(worksheet.Cells[row, ARTICLE_COL_INDEX].Value);

                    if (!article.HasValue) continue;
      
                    var redemptionDateString = worksheet.Cells[row, DATE_FLAG_COL_INDEX].Value.ToString();
                    var itemStatusValue = worksheet.Cells[row, STATUS_COL_INDEX].Value.ToString();
                    var priceSting = worksheet.Cells[row, PRICE_COL_INDEX].Value.ToString();

                    if (!string.IsNullOrEmpty(redemptionDateString) &&
                        !string.IsNullOrEmpty(itemStatusValue) &&
                        !string.IsNullOrEmpty(priceSting))
                    {
                        var item = new ArticleRowItem(redemptionDateString, itemStatusValue.Trim(), priceSting.Trim());

                            if (articleDict.ContainsKey(article.Value))
                            {
                                articleDict[article.Value].Items.Add(item);
                            }
                            else
                            {
                                articleDict[article.Value] = new Article(marketplace, item, deadLineDays);
                            }
                        }
                        else
                        {
                            await Report("Невозможно создать Item по выкупу.", 2000, true);
                        }
                    }
                }
            }

        package.Dispose();
        stopwatch.Stop();

        var redemtionReport = $"Артикулов с логистикой {articleDict.Values.Count}\n";

        var groups =
            from article in articleDict
            from item in article.Value.Items
            group item by item.ItemStatus into itemGroup
            orderby itemGroup.Key
            select itemGroup;

        int redemptonCount = default;

        foreach (var group in groups)
        {
            var valuesCount = group.Count();
            redemtionReport += $"- {group.Key} {valuesCount}\n";
            redemptonCount += valuesCount;
        }

        redemtionReport += $"\nВыкупов с логистикой {redemptonCount}\n";
        redemtionReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(redemtionReport);
    }

    private static async Task ReadStorageReportFile(Marketplace marketplace)
    {
        string? filePath = marketplace == Marketplace.WB ? storageFilePathWB : storageFilePathOZ;

        if (filePath is null || CheckFileExistence(filePath) == null) await Report(goodByMessage, 2000, true);

        var existingFile = new FileInfo(filePath!);

        using ExcelPackage package = new(existingFile);

        if (CheckSheets(in package) == null) await Report(goodByMessage, 2000, true);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Report($"Анализирую отчет клиента {login} по приходу и остаткам {marketplace}...");

        const int FIRST_INDEX = 1;
        const int COL_STORE_COUNT_INDEX = 17;

        (int articleIndex, int nameIndex, int initBalanceIndex, int comingIndex, int finBalanceIndex) columnIndexes = (1, 6, 14, 15, 17);

        int rowFirstRoomAddress = default;
        int rowSecondRoomAddress = default;
        int rowFinalRoomAddress = default;
  
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        (_, int rowStoreCount) = FindNumberColsAndRowsIn(in worksheet);

        // Find addresses of special rows
        for (int row = FIRST_INDEX; row <= rowStoreCount; row++)
        {
            for (int col = FIRST_INDEX; col <= COL_STORE_COUNT_INDEX; col++)
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

        int? article;
        string? name;
        int? coming;
        int? finalBalance;

        // Get coming && finalBalance++ (firstRoom)
        for (int row = firstRoomRowRange.First(); row <= firstRoomRowRange.Count; row++)
        {
            // article
            article = ParseValueToInt(worksheet.Cells[row, columnIndexes.articleIndex].Value);
            if (article.HasValue && articleDict.ContainsKey(article.Value))
            {
                // name
                name = ParseValueToString(worksheet.Cells[row, columnIndexes.nameIndex].Value);
                if (name is not null)
                   articleDict[article.Value]!.SetName(name!.Split(",")[0].Trim());
             
                // coming
                coming = ParseValueToInt(worksheet.Cells[row, columnIndexes.comingIndex].Value); ;
                if (coming.HasValue)
                    articleDict[article.Value].StoreBalance.ChangeBalance(ItemBalanceUnit.Coming, coming.Value);

                // finalBalance
                finalBalance = ParseValueToInt(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                if (finalBalance.HasValue)
                    articleDict[article.Value].StoreBalance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance.Value);
            }
        }

        // Get finalBalance++ (secondRoom)
        for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
        {
            // article
            article = ParseValueToInt(worksheet.Cells[row, columnIndexes.articleIndex].Value);

            if (article.HasValue && articleDict.ContainsKey(article.Value))
            {
                // finalBalance
                finalBalance = ParseValueToInt(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);

                if (finalBalance.HasValue)
                    articleDict[article.Value].StoreBalance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance.Value);
            }
        }

        package.Dispose();
        stopwatch.Stop();

        var comingSum = articleDict.Sum(art => art.Value.StoreBalance.ReceivedQuantity);
        var finBalance = articleDict.Sum(art => art.Value.StoreBalance.FinBalance);

        var comBalanceReport = $"Общий приход товаров: {comingSum}, общий остаток: {finBalance}\n";
        comBalanceReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(comBalanceReport);
    }

    private static async Task ReadConsumptionReport(string? filePath, ItemBalanceUnit storeUnit, int artIndex, int countIndex)
    {
        if (filePath is null && CheckFileExistence(filePath!) == null)
            await Report(goodByMessage, 2000, true);
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using ExcelPackage package = new(new FileInfo(filePath!));

        if (CheckSheets(in package) == null) await Report(goodByMessage, 2000, true);

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
                    if (articleDict.ContainsKey(article))
                    {
                        articleDict[article].SetStoreBalance(storeUnit, count);
                    }
                }
            }
        }

        package.Dispose();
        stopwatch.Stop();

        int result = 0;

        switch (storeUnit)
        {
            case ItemBalanceUnit.Consumption:
                result = articleDict.Sum(art => art.Value.StoreBalance.ShippedQuantity);
                break;
 
            case ItemBalanceUnit.WriteoffShortages:
                result = articleDict.Sum(art => art.Value.StoreBalance.WriteoffShortages);
                break;
            default:
                break;
        }

        var targetMessage = storeUnit == ItemBalanceUnit.Consumption ? "Общий расход по товарам клиента" : "Оприходовано и списано по недостачам от ПВЗ";
        var reportMessage = $"{targetMessage}: {result}.\n";
        reportMessage += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(reportMessage);
    }

    private static async Task ReadPayShortagesReport(string? filePath)
    {
        const int ARTICLE_INDEX = 1;
        const int COUNT_INDEX = 2;
        const int PRICE_INDEX = 3;

        if (filePath is null && CheckFileExistence(filePath) == null)
        {
            return;
        }
        else
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using ExcelPackage package = new(new FileInfo(filePath));

            if (CheckSheets(in package) == null) return;

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
                    if (articleDict.ContainsKey(article))
                    {
                        articleDict[article].SetStoreBalance(count, price);
                    }
                }
            }

            package.Dispose();
            stopwatch.Stop();

            var paySum = articleDict
                .Aggregate(0.00, (acc, el) => acc + el.Value.StoreBalance.PayShortagesSum);

            string payReport;

            if (paySum > 0.00)
            {
                payReport = string.Format("Сумма компенсации за недостачи товаров: {0:C}.\n", paySum);
            }
            else
            {
                payReport = "Нет погашений недостач по указанным артикулам";
            }

            payReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

            await Report(payReport);
        }
    }

    private static async Task ReadRegrandingReport(string filePath)
    {
        const int ARTICLE_INDEX = 1;
        const int REGRANDING_INDEX = 2;
        const int COUNT_INDEX = 3;
        const int COMMENT_INDEX = 4;

        if (filePath is null || CheckFileExistence(filePath) == null) return;
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using ExcelPackage package = new(new FileInfo(filePath));

        if (CheckSheets(in package) == null) return;

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
            if (articleAddress.Value is not null &&
                int.TryParse(articleAddress.Value.ToString(), out int article) &&
                articleDict.ContainsKey(article) &&
                regrandingAddress.Value is not null &&
                int.TryParse(regrandingAddress.Value.ToString(), out int regranding) &&
                articleDict.ContainsKey(regranding) &&
                countAddress.Value is not null &&
                int.TryParse(countAddress.Value.ToString(), out int count)
                )
            {
                articleDict[article].StoreBalance.SetRegrading(regranding, count);
               
                commentForArticle = $"пересорт на арт {regranding} ({count} шт) ";
                commentForRegranding = $"пересорт от арт {article} ({count} шт) ";

                if (commentAddress.Value is not null &&
                    !string.IsNullOrEmpty(commentAddress.Value.ToString()))
                {
                    commentForArticle = string.Concat(commentForArticle, commentAddress.Value!.ToString()!.Trim());
                }

                articleDict[article].SetComment(commentForArticle);
                articleDict[regranding].SetComment(commentForRegranding);

                regrandingCount += count;
            }
        }

        package.Dispose();
        stopwatch.Stop();

        var regReport = $"Количество товаров в пересорте: {regrandingCount}.\n";
        regReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(regReport);
    }

    private static async Task WriteXls(string? ouptutFilePath, string workSheetName = "Отчет")
    {
        const int HEADER_TABLE_ROW_HEIGHT = 40;
        const int DEFAULT_TABLE_ROW_HEIGHT = 16;
        const int TABLE_COL_WIDTH = 17;
        const int FIRST_ROW_INDEX = 1;

        using var excel = new ExcelPackage(ouptutFilePath);

        // Create new worksheet
        CreateWorkSheet(excel, workSheetName, out ExcelWorksheet newWorkSheet, System.Drawing.Color.GreenYellow, FIRST_ROW_INDEX, HEADER_TABLE_ROW_HEIGHT, DEFAULT_TABLE_ROW_HEIGHT, TABLE_COL_WIDTH);

        // Create cells 
        ResultCell[] resultCells = new ResultCell[]
        {
            new ResultCell("МП", "0"),                              // 1
            new ResultCell("Артикул", "0"),                         // 2
            new ResultCell("Название", "0"),                        // 3
            new ResultCell("Средняя цена, руб.", "0.00"),           // 4
            new ResultCell("Дата последнего выкупа", "dd.MM.yyyy"), // 5
            new ResultCell("Статус", "0"),                          // 6

            new ResultCell("Всего", "0"),                           // 7
            new ResultCell("Отменено", "0"),                        // 8
            new ResultCell("В пути", "0"),                          // 9
            new ResultCell("Доставлен на ПВЗ", "0"),                // 10                
            new ResultCell("Забран с ПВЗ", "0"),                    // 11

            new ResultCell("Склад: Ожидание", "0"),                 // 12
            new ResultCell("Склад: Приход", "0"),                   // 13
            new ResultCell("Склад: Недовоз от ПВЗ", "0"),           // 14

            new ResultCell("Склад: Расход всего", "0"),             // 15
            new ResultCell("Склад: Расход товаром", "0"),           // 16
            new ResultCell("Склад: Расход пересортом", "0"),        // 17
            new ResultCell("Склад: Расход компенсацией", "0"),      // 18
            new ResultCell("Склад: Остаток", "0"),                  // 19

            new ResultCell("Комментарий", "0"),                     // 19
        };

        // Fill header data 
        FillTableHeaderData(resultCells, ref newWorkSheet);

        newWorkSheet.Column(1).Width = 8;   // МП
        newWorkSheet.Column(2).Width = 12;  // Артикул 
        newWorkSheet.Column(3).Width = 60;  // Название 
        newWorkSheet.Column(20).Width = 60; // Комментарий

        // Populating with data
        int recordIndex = FIRST_ROW_INDEX + 1;

        foreach (var (key, value) in articleDict)
        {
            articleDict[key].AnalyzeArticle(key);

            if (articleDict[key].ArticleShortage.HasValue)
            {
                ArticleShortages.Add(articleDict[key].ArticleShortage!.Value);
            }

            // Fill Redemptions data
            newWorkSheet.Cells[recordIndex, 1].Value = articleDict[key].Marketplace;
            newWorkSheet.Cells[recordIndex, 2].Value = key;
            newWorkSheet.Cells[recordIndex, 3].Value = articleDict[key].Name;
            newWorkSheet.Cells[recordIndex, 4].Value = articleDict[key].AveragePrice;

            // RedemptionDate
            newWorkSheet.Cells[recordIndex, 5].Value = articleDict[key].MaxRedemptionDate;
            newWorkSheet.Cells[recordIndex, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            newWorkSheet.Cells[recordIndex, 5].Style.Fill.BackgroundColor.SetColor(articleDict[key].StatusValue.color);

            newWorkSheet.Cells[recordIndex, 6].Value = articleDict[key].StatusValue.statusComment;

            newWorkSheet.Cells[recordIndex, 7].Value = articleDict[key].Items.Count;
            newWorkSheet.Cells[recordIndex, 8].Value = articleDict[key].RedempionBalance.CanceledCount;
            newWorkSheet.Cells[recordIndex, 9].Value = articleDict[key].RedempionBalance.OnWayCount;
            newWorkSheet.Cells[recordIndex, 10].Value = articleDict[key].RedempionBalance.DeliveredCount;
            newWorkSheet.Cells[recordIndex, 11].Value = articleDict[key].RedempionBalance.WithDrawnCount;

            // Balance data
            // - WaitBalance
            newWorkSheet.Cells[recordIndex, 12].Value = articleDict[key].RedempionBalance.WaitBalance;
            newWorkSheet.Cells[recordIndex, 12].Style.Font.Bold = true;

            newWorkSheet.Cells[recordIndex, 13].Value = articleDict[key].StoreBalance.ReceivedQuantity;

            // - Undelivery
            newWorkSheet.Cells[recordIndex, 14].Value = articleDict[key].StoreBalance.Undelivery;
            if(articleDict[key].StoreBalance.Undelivery > 0 && articleDict[key].StatusValue.status != ArticleStatus.InWork)
            {
                newWorkSheet.Cells[recordIndex, 14].Style.Font.Color.SetColor(System.Drawing.Color.DarkRed); 
            }

            // Consumption
            newWorkSheet.Cells[recordIndex, 15].Value = articleDict[key].StoreBalance.TotalConsumption;
            newWorkSheet.Cells[recordIndex, 16].Value = articleDict[key].StoreBalance.ShippedQuantity;
            newWorkSheet.Cells[recordIndex, 17].Value = articleDict[key].StoreBalance.Regradings.Sum(re => re.Value);
            newWorkSheet.Cells[recordIndex, 18].Value = articleDict[key].StoreBalance.PayShortagesCount;

            newWorkSheet.Cells[recordIndex, 19].Value = articleDict[key].StoreBalance.FinBalance;

            // Comment
            newWorkSheet.Cells[recordIndex, 20].Value = articleDict[key].Comment;

            recordIndex++;
        }

        // Stilized table - fit && data format
        for (int i = 1; i < resultCells.Length + 1; i++)
        {
            //newWorkSheet.Column(i).AutoFit();
            newWorkSheet.Column(i).Style.Numberformat.Format = i != 5 ? "0" : "dd.MM.yyyy";
            newWorkSheet.Column(i).Style.WrapText = true;
        }

        // Add filter
        AddFilterInWorkSheet(ref newWorkSheet);

        //Freeze row && columns
        newWorkSheet.View.FreezePanes(2, 4);

        // Group column
        for (var i = 8; i <= 11; i++)
        {
            newWorkSheet.Column(i).OutlineLevel = 1;
            newWorkSheet.Column(i).Collapsed = true;
        }

        for (var i = 16; i <= 18; i++)
        {
            newWorkSheet.Column(i).OutlineLevel = 1;
            newWorkSheet.Column(i).Collapsed = true;
        }

        // Analize ArticleShortages
        if (ArticleShortages.Count > 0)
        {
            foreach (var articleShortage in ArticleShortages)
            {
                var articleShortagesInWarehouse = articleShortage.GetShortagesCountByPlace(ShortagesPlace.Warehouse);
                if (articleShortagesInWarehouse > 0)
                {
                    ShortagesInWarehouse.Add((articleShortage.ArticleNumber, articleShortage.ArtileName, articleShortagesInWarehouse, articleShortage.AveragePrice, articleShortage.AveragePrice * articleShortagesInWarehouse));
                }

                var articleShortagesInPickupPoint = articleShortage.GetShortagesCountByPlace(ShortagesPlace.PickupPoint);
                if (articleShortagesInPickupPoint > 0)
                {
                    ShortagesInPickupPoints.Add((articleShortage.ArticleNumber, articleShortage.ArtileName, articleShortagesInPickupPoint, articleShortage.AveragePrice, articleShortage.AveragePrice * articleShortagesInPickupPoint));
                }
            }

            // Shortages in warehouse report
            if (ShortagesInWarehouse.Count > 0)
            {
                // Create new worksheet
                CreateWorkSheet(in excel, "Недовозы (склад)", out ExcelWorksheet worksheet02, System.Drawing.Color.Orange, FIRST_ROW_INDEX, HEADER_TABLE_ROW_HEIGHT, DEFAULT_TABLE_ROW_HEIGHT, TABLE_COL_WIDTH);

                // Create info for cells 
                ResultCell[] resultCellsNewWorksheet02 = new ResultCell[]
                {
                    new ResultCell("Артикул", "0"),                         // 1
                    new ResultCell("Название товара", "0"),                 // 2
                    new ResultCell("Количество, шт.", "0"),                 // 3
                    new ResultCell("Средняя цена", "0.00"),                 // 4
                    new ResultCell("Сумма", "0.00"),                        // 5
                };

                // Fill header data 
                FillTableHeaderData(resultCellsNewWorksheet02, ref worksheet02);

                // Fill data 
                int itemRecordIndex01 = FIRST_ROW_INDEX + 1;

                foreach (var (article, name, count, cost, sum) in ShortagesInWarehouse)
                {
                    worksheet02.Cells[itemRecordIndex01, 1].Value = article;
                    worksheet02.Cells[itemRecordIndex01, 2].Value = name;
                    worksheet02.Cells[itemRecordIndex01, 3].Value = count;
                    worksheet02.Cells[itemRecordIndex01, 4].Value = cost;
                    worksheet02.Cells[itemRecordIndex01, 5].Value = sum;

                    itemRecordIndex01++;
                }

                // Fill total sum
                worksheet02.Cells[itemRecordIndex01, 5].Value = ShortagesInWarehouse.Aggregate(0.00, (acc, i) => acc + i.sum);

                // Stilized table - fit text
                worksheet02.Column(2).Width = 60;  
                for (int i = 1; i < resultCellsNewWorksheet02.Length + 1; i++)
                {
                    worksheet02.Column(i).Style.WrapText = true;
                }
            }

            // Shortages in pickup report
            if (ShortagesInPickupPoints.Count > 0)
            {
                // Create new worksheet
                CreateWorkSheet(in excel, "Недовозы (ПВЗ)", out ExcelWorksheet worksheet03, System.Drawing.Color.Orange, FIRST_ROW_INDEX, HEADER_TABLE_ROW_HEIGHT, DEFAULT_TABLE_ROW_HEIGHT, TABLE_COL_WIDTH);

                // Create info for cells 
                ResultCell[] resultCellsNewWorksheet03 = new ResultCell[]
                {
                    new ResultCell("Артикул", "0"),                         // 1
                    new ResultCell("Название товара", "0"),                 // 2
                    new ResultCell("Количество, шт.", "0"),                 // 3
                    new ResultCell("Средняя цена", "0.00"),                 // 4
                    new ResultCell("Сумма", "0.00"),                        // 5
                };

                // Fill header data 
                FillTableHeaderData(resultCellsNewWorksheet03, ref worksheet03);

                // Fill data 
                int itemRecordIndex02 = FIRST_ROW_INDEX + 1;

                foreach (var (article, name, count, cost, sum) in ShortagesInPickupPoints)
                {
                    worksheet03.Cells[itemRecordIndex02, 1].Value = article;
                    worksheet03.Cells[itemRecordIndex02, 2].Value = name;
                    worksheet03.Cells[itemRecordIndex02, 3].Value = count;
                    worksheet03.Cells[itemRecordIndex02, 4].Value = cost;
                    worksheet03.Cells[itemRecordIndex02, 5].Value = sum;

                    itemRecordIndex02++;
                }

                // Fill total sum
                worksheet03.Cells[itemRecordIndex02, 5].Value = ShortagesInPickupPoints.Aggregate(0.00, (acc, i) => acc + i.sum);

                // Stilized table - fit text
                worksheet03.Column(2).Width = 60;
                for (int i = 1; i < resultCellsNewWorksheet03.Length + 1; i++)
                {
                    worksheet03.Column(i).Style.WrapText = true;
                }
            }
        }

        // Save work
        if (File.Exists(outputPath)) File.Delete(outputPath);

        // Create excel file on physical disk 
        var objFileStrm = File.Create(outputPath);
        objFileStrm.Close();

        // Write content to excel file 
        File.WriteAllBytes(outputPath, excel.GetAsByteArray());

        //Close Excel package
        excel.Dispose();

        Console.WriteLine($"Новый отчет по клиенту создан по пути: {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Для завершения работы нажмите любую клавишу");

        _ = Console.ReadKey();

        await Report(goodByMessage, 2000, true);
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

    private static bool? CheckFileExistence(string path)
    {
        if (File.Exists(path))
        {
            return true;
        }
        else
        {
            Console.WriteLine($"Файл по пути\n{path}\nне найден. Проверьте правильность указания данных.");
            return null;
        }
    }

    private static bool? CheckSheets(in ExcelPackage package)
    {
        if(package.Workbook.Worksheets.Count != 0)
        {
            return true;
        }
        else
        {
            Console.WriteLine($"Нет активных листов для анализа. Проверьте книгу.");
            Console.ReadLine();
            return null;
        }
    }

    private static async Task<string?> GiveMePath(string message, bool necessarily, bool giveMeLogin)
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
            login = GiveMeLogin(path!);

        return FormСorrectPath(path!);
    }

    private static string? GiveMeLogin(string path)
    {
        var substrings01 = path.Split("/");

        if (substrings01.Length <= 1) return null;

        var loginChars = substrings01.Last().Split(" ")[3].Trim().ToList();

        if (loginChars is null || loginChars.Count == 0) return null;

        // Clear non-digits in login
        for (int i = 0; i < loginChars.Count; i++)
        {
            if (!int.TryParse(loginChars[i].ToString(), out _)) loginChars.RemoveAt(i);
        }

        return new string(loginChars.ToArray());
    }

    private static async Task GetClientOutputPath()
    {
        if (login is null)
            throw new Exception("Логин клинта не определен");

        clientDirectoryPath = FileManager.GetDirectoryInfo(PathKey.MPBoostClients, login);

        if (clientDirectoryPath is not null)
        {
            await Report($"Директория для работы с клиентом:\n{clientDirectoryPath.FullName}");

            var dateTime = DateTime.Now.ToString("dd-MM-yyyy");
            outputPath = $"{clientDirectoryPath.FullName}/ClientReport_{login}_{dateTime}.xlsx";
        }
        else
        {
            await Report($"Невозможно найти или создать директорию для работы с клиентом.");
        }
    }

    private static (int col, int row) FindNumberColsAndRowsIn(in ExcelWorksheet sheet)
    {
        return (sheet.Dimension.End.Column, sheet.Dimension.End.Row);
    }

    public static void CreateWorkSheet(in ExcelPackage excelPackage, string workSheetName, out ExcelWorksheet worksheet, System.Drawing.Color tabColor, int firstRowIndex, double headerRowHeight, double rowHeight, double rowWidth)
    {
        // Remove existing worksheet
        var existingWorksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(x => x.Name == workSheetName);
        if (existingWorksheet is not null)
            excelPackage.Workbook.Worksheets.Delete(existingWorksheet);

        // Create net worksheet
        worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);

        // Stilized workSheet
        worksheet.TabColor = tabColor;
        worksheet.DefaultRowHeight = rowHeight;
        worksheet.DefaultColWidth = rowWidth;

        // Stilized workSheet header
        worksheet.Row(firstRowIndex).Height = headerRowHeight;
        worksheet.Row(firstRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Row(firstRowIndex).Style.Font.Bold = true;
        worksheet.Row(firstRowIndex).Style.Font.Size = 14;
        worksheet.Row(firstRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    private static void FillTableHeaderData(ResultCell[] resultCells, ref ExcelWorksheet worksheet)
    {
        for (int i = 0; i < resultCells.Length; i++)
        {
            var colIndex = i + 1;

            // Add data
            worksheet.Cells[1, colIndex].Value = resultCells[i].Name;
            worksheet.Cells[1, colIndex].AutoFitColumns();

            // Stilized
            worksheet.Column(colIndex).AutoFit();
            worksheet.Column(colIndex).Style.Numberformat.Format = resultCells[i].Format;
        }
    }

    private static void AddFilterInWorkSheet(ref ExcelWorksheet excelWorksheet)
    {
        int firstRow = excelWorksheet.Dimension.Start.Row;
        int endRow = excelWorksheet.Dimension.End.Row;

        int firstCol = excelWorksheet.Dimension.Start.Column;
        int endCol = excelWorksheet.Dimension.End.Column;

        string firstColLet = ExcelCellAddress.GetColumnLetter(firstCol);
        string endColLet = ExcelCellAddress.GetColumnLetter(endCol);

        excelWorksheet.Cells[$"{firstColLet}{firstRow}:{endColLet}{endRow}"].AutoFilter = true;
    }
}
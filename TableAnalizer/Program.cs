// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

#nullable enable

using System.Diagnostics;
using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ClientTableAnalizer.Objects;

internal enum Marketplace { WB, OZ }

/// <summary>
/// “Lightweight” item of article (product).
/// </summary>
/// <remark>
/// Used for reports (shortages, overruns).
/// </remark>
internal class ArticleItemForReport
{
    public int ArticleNumber { get; private set; }
    public string Name { get; private set; }
    public int Count { get; private set; }
    public double Cost { get; private set; }
    public double Sum { get; private set; }

    public ArticleItemForReport(int articleNumber, string name, int count, double cost, double sum)
    {
        ArticleNumber = articleNumber; Name = name; Count = count; Cost = cost; Sum = sum;
    }
}

internal class Program
{
    #region Variables and constants

    private static int deadLineDays;
    private const string LOGISTIC_FLAG = "Да";
    private const string NAME_FIRST_ROOM = "1 - Помещение приемки, сортировки и упаковки";
    private const string NAME_SECOND_ROOM = "2 - Помещение длительного хранения и отгрузки";
    private const string NAME_FINAL_ROW = "Итого товаров в единицах хранения";
    private const int FIRST_ROW_INDEX = 1;
    private const int HEADER_TABLE_ROW_HEIGHT = 40;
    private const int DEFAULT_TABLE_ROW_HEIGHT = 16;
    private const int TABLE_COL_WIDTH = 17;

    private static string? login = default;
    private static string? redemtionFilePathWB = default;
    private static string? redemtionFilePathOZ = default;
    private static string? storageFilePathWB = default;
    private static string? storageFilePathOZ = default;

    private static string? consumptionPath, payShortagesPath, regradingPath, dummyPath, outputPath = default;

    private static string? writeoffShortagesPath = default;

    private static DirectoryInfo? clientDirectoryPath;

    private static readonly Dictionary<int, Article> articleDict = new();

    private static List<ArticleShortage> ArticleShortages { get; set; } = new();

    private static readonly List<ArticleItemForReport> ShortagesInWarehouse = new();

    private static readonly List<ArticleItemForReport> ShortagesInPickupPoints = new();

    private static readonly List<ArticleItemForReport> Overruns = new();

    private static readonly string goodByMessage = "\nСпасибо за использование программы. Хорошего дня!";

    #endregion

    #region Functionality
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
            await ReadConsumptionReport(consumptionPath, ItemBalanceIndex.Consumption, 4, 28);

        await GetWriteoffShortagesData();

        if (!string.IsNullOrEmpty(writeoffShortagesPath))
            await ReadConsumptionReport(writeoffShortagesPath, ItemBalanceIndex.WriteoffShortages, 5, 39);

        await GetUserPayShortagesData();

        if (!string.IsNullOrEmpty(consumptionPath))
            await ReadPayShortagesReport(payShortagesPath);

        await GetRegrandingData();

        if (!string.IsNullOrEmpty(regradingPath))
            await ReadRegrandingReport(regradingPath);

        await GetDummyData();
        if (!string.IsNullOrEmpty(dummyPath))
            await ReadDummyReport(dummyPath);

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

    private static async Task GetDummyData()
    {
        var dummyDataMessage = "Перетащите путь к отчету по пустышкам (не обязательно)\n" +
            "(1 - пропустить, ESC - выход):";
        dummyPath = await GiveMePath(dummyDataMessage, false, false);
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
                if (redemtionFilePathWB is null || CheckFileExistence(redemtionFilePathWB) == null)
                    await Report(goodByMessage, 2000, true);
                break;
            case Marketplace.OZ:
                if (redemtionFilePathOZ is null || CheckFileExistence(redemtionFilePathOZ) == null)
                    await Report(goodByMessage, 2000, true);
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

        await Report(redemtionReport, 1500);
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

        (int articleIndex, int nameIndex, int initBalanceIndex, int comingIndex, int finBalanceIndex) = (1, 6, 14, 15, 17);

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
            article = ParseValueToInt(worksheet.Cells[row, articleIndex].Value);
            if (!article.HasValue || !articleDict.ContainsKey(article.Value)) continue;

            // name
            name = ParseValueToString(worksheet.Cells[row, nameIndex].Value);
            if (!string.IsNullOrEmpty(name))
                articleDict[article.Value]!.SetName(name!.Split(",")[0].Trim());
             
            // coming
            coming = ParseValueToInt(worksheet.Cells[row, comingIndex].Value); ;
            if (coming.HasValue)
                articleDict[article.Value].StoreBalance.ChangesStorageBalance(ItemBalanceIndex.Coming, coming.Value);

            // finalBalance
            finalBalance = ParseValueToInt(worksheet.Cells[row, finBalanceIndex].Value);
            if (finalBalance.HasValue)
                articleDict[article.Value].StoreBalance.ChangesStorageBalance(ItemBalanceIndex.FinBalance, finalBalance.Value);
          
        }

        // Get finalBalance++ (secondRoom)
        for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
        {
            // article
            article = ParseValueToInt(worksheet.Cells[row, articleIndex].Value);
            if (!article.HasValue || !articleDict.ContainsKey(article.Value)) continue;

            // finalBalance
            finalBalance = ParseValueToInt(worksheet.Cells[row, finBalanceIndex].Value);

            if (finalBalance.HasValue)
                articleDict[article.Value].StoreBalance.ChangesStorageBalance(ItemBalanceIndex.FinBalance, finalBalance.Value);
        }

        package.Dispose();
        stopwatch.Stop();

        var comingSum = articleDict.Sum(art => art.Value.StoreBalance.ReceivedQuantity);
        var finBalance = articleDict.Sum(art => art.Value.StoreBalance.FinBalance);

        var comBalanceReport = $"Общий приход товаров: {comingSum}, общий остаток: {finBalance}\n";
        comBalanceReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(comBalanceReport, 1500);
    }

    private static async Task ReadConsumptionReport(string? filePath, ItemBalanceIndex storeUnit, int artIndex, int countIndex)
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
            case ItemBalanceIndex.Consumption:
                result = articleDict.Sum(art => art.Value.StoreBalance.ShippedQuantity);
                break;
 
            case ItemBalanceIndex.WriteoffShortages:
                result = articleDict.Sum(art => art.Value.StoreBalance.WriteoffShortages);
                break;
            default:
                break;
        }

        var targetMessage = storeUnit == ItemBalanceIndex.Consumption ? "Общий расход по товарам клиента" : "Оприходовано и списано по недостачам от ПВЗ";
        var reportMessage = $"{targetMessage}: {result}.\n";
        reportMessage += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(reportMessage, 1500);
    }

    private static async Task ReadPayShortagesReport(string? filePath)
    {
        const int ARTICLE_INDEX = 1;
        const int COUNT_INDEX = 2;
        const int PRICE_INDEX = 3;

        if (string.IsNullOrEmpty(filePath) || CheckFileExistence(filePath!) == null)
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

            await Report(payReport, 1500);
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
                // Для текущего арта указываем артикул пересорта (regranding)
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

        await Report(regReport, 1500);
    }

    private static async Task ReadDummyReport(string filePath)
    {
        const int ARTICLE_INDEX = 1; const int COUNT_INDEX = 2;

        if (filePath is null || CheckFileExistence(filePath) == null) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using ExcelPackage package = new(new FileInfo(filePath));

        if (CheckSheets(in package) == null) return;

        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

        int dummyCount = default;

        for (int row = 1; row <= rowCount; row++)
        {
            var articleAddress = worksheet.Cells[row, ARTICLE_INDEX];
            var countAddress = worksheet.Cells[row, COUNT_INDEX];

            // Set article dummy 
            if (articleAddress.Value is not null &&
                int.TryParse(articleAddress.Value.ToString(), out int article) &&
                articleDict.ContainsKey(article) &&
                countAddress.Value is not null &&
                int.TryParse(countAddress.Value.ToString(), out int count)
                )
            {
                articleDict[article].StoreBalance.SetDummy(count);
                dummyCount += count;
            }
        }

        package.Dispose();
        stopwatch.Stop();

        var regReport = $"Количество товаров в пустышках: {dummyCount}.\n";
        regReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

        await Report(regReport, 1500);
    }

    private static async Task WriteXls(string? ouptutFilePath, string workSheetName = "Отчет")
    {
        using var excel = new ExcelPackage(ouptutFilePath);

        // Create cells 
        ResultCell[] resultCells = new ResultCell[]
        {
            // Article info
            new (1, "МП", 8, "0", false, 0),                               
            new (2, "Артикул", 12, "0", false, 0),                        
            new (3, "Название", 60, "0", false, 0),                      
            new (4, "Средняя цена, руб.", 12, "0.00", true, 0),           
            new (5, "Дата последнего выкупа", 12, "dd.MM.yyyy", true, 0), 
            new (6, "Статус", 12, "0", false, 0),                          

            // Buyout info
            new (7, "Всего", 12, "0", false, 0),                           
            new (8, "Отменено", 7, "0", false, -90),                        
            new (9, "В пути", 7, "0", false, -90),                          
            new (10, "Доставлен на ПВЗ", 7, "0", true, -90),                              
            new (11, "Забран с ПВЗ", 7, "0", true, -90),                   

            // Info on arrival of goods at warehouse
            new (12, "Склад: Ожидание", 9,"0", false, 0),                
            new (13, "Склад: Приход", 9 ,"0",false, 0),                   
            new (14, "Склад: Недовоз от ПВЗ", 9, "0", true, 0),          

            // Info on shipment of goods from warehouse
            new (15, "Склад: Расход всего",12, "0", true, 0),            
            new (16, "Склад: Расход товаром",7, "0", true, -90),          
            new (17, "Склад: Расход пересортом",7, "0", true, -90),       
            new (18, "Склад: Расход компенсацией",7, "0", true, -90),     
            new (19, "Склад: Учтено 'пустышками'",7, "0", true, -90),     
            new (20, "Склад: Остаток", 9, "0", true, 0),        

            new (21, "Комментарий", 60, "0", true, 0),              
        };

        // Create new worksheet
        CreateWorkSheet(in excel, workSheetName, out ExcelWorksheet newWorkSheet, System.Drawing.Color.GreenYellow);

        // Fill header data 
        SetupTableHeader(in resultCells, ref newWorkSheet);

        // Populating with data
        int recordIndex = FIRST_ROW_INDEX + 1;

        foreach (var (key, value) in articleDict)
        {
            articleDict[key].AnalyzeArticle(key);

            if (articleDict[key].ArticleShortage.HasValue)
                ArticleShortages.Add(articleDict[key].ArticleShortage!.Value);

            var regradingArt = articleDict
                .Any(art => art.Value.StoreBalance.Regradings.ContainsKey(key));

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
            newWorkSheet.Cells[recordIndex, 19].Value = articleDict[key].StoreBalance.DummiesCount;
            newWorkSheet.Cells[recordIndex, 20].Value = articleDict[key].StoreBalance.FinBalance;

            // Comment
            newWorkSheet.Cells[recordIndex, 21].Value = articleDict[key].Comment;

            recordIndex++;
        }

        // Add filter
        AddFilterInWorkSheet(ref newWorkSheet);

        //Freeze row && columns
        newWorkSheet.View.FreezePanes(2, 4);

        // HACK: Переделать !!!
        // Group column
        for (var i = 8; i <= 11; i++)
        {
            newWorkSheet.Column(i).OutlineLevel = 1;
            newWorkSheet.Column(i).Collapsed = true;
        }

        for (var i = 16; i <= 19; i++)
        {
            newWorkSheet.Column(i).OutlineLevel = 1;
            newWorkSheet.Column(i).Collapsed = true;
        }

        ResultCell[] resultCellsForAddReports = new ResultCell[]
        {
            new (1, "Артикул", 13, "0", false, 0),                         
            new (2, "Название товара", 60, "0", true, 0),                 
            new (3, "Кол-во, шт.",11, "0", true, 0),                 
            new (4, "Средняя цена, руб.",11, "0.00", true, 0),                 
            new (5, "Сумма, руб.", 11, "0.00", true, 0),                        
        };

        // Analize ArticleShortages
        if (ArticleShortages.Count > 0)
        {
            foreach (var articleShortage in ArticleShortages)
            {
                var articleShortagesInWarehouse = articleShortage.GetShortagesCountByPlace(ShortagesPlace.Warehouse);
                if (articleShortagesInWarehouse > 0)
                {
                    var shortageInWarehouse = new ArticleItemForReport(articleShortage.ArticleNumber, articleShortage.ArtileName, articleShortagesInWarehouse, articleShortage.AveragePrice, articleShortage.AveragePrice * articleShortagesInWarehouse);
                    ShortagesInWarehouse.Add(shortageInWarehouse);
                }

                var articleShortagesInPickupPoint = articleShortage.GetShortagesCountByPlace(ShortagesPlace.PickupPoint);
                if (articleShortagesInPickupPoint > 0)
                {
                    var shortageInPickupPoints = new ArticleItemForReport(articleShortage.ArticleNumber, articleShortage.ArtileName, articleShortagesInPickupPoint, articleShortage.AveragePrice, articleShortage.AveragePrice * articleShortagesInPickupPoint);
                    ShortagesInPickupPoints.Add(shortageInPickupPoints);
                }
            }

            // Shortages in warehouse report
            if (ShortagesInWarehouse.Count > 0)
            {
                // Create new worksheet
                CreateWorkSheet(in excel, "Недовозы (склад)", out ExcelWorksheet worksheet02, System.Drawing.Color.Orange);

                // Fill header data 
                SetupTableHeader(in resultCells, ref worksheet02);

                // Make report in sheet
                MakeAdditionalReport(resultCellsForAddReports, ref worksheet02, ShortagesInWarehouse);
            }

            // Shortages in pickup report
            if (ShortagesInPickupPoints.Count > 0)
            {
                // Create new worksheet
                CreateWorkSheet(in excel, "Недовозы (ПВЗ)", out ExcelWorksheet worksheet03, System.Drawing.Color.Orange);

                // Fill header data 
                SetupTableHeader(in resultCells, ref worksheet03);

                // Make report in sheet
                MakeAdditionalReport(resultCellsForAddReports, ref worksheet03, ShortagesInPickupPoints);
            }
        }

        // HACK: Плохо, переделать логику, двойной проход
        foreach (var (key, value) in articleDict)
        {
            var regradingArt = articleDict
                .Any(art => art.Value.StoreBalance.Regradings.ContainsKey(key));

            if (articleDict[key].Overrun != null && !regradingArt)
                Overruns.Add(articleDict[key].Overrun!);
        }

        // Analize ArticleShortages
        if (Overruns.Count > 0)
        {
            // Create new worksheet
            CreateWorkSheet(in excel, "Перерасходы (склад)", out ExcelWorksheet overrunsWorksheet, System.Drawing.Color.ForestGreen);

            // Fill header data 
            SetupTableHeader(in resultCells, ref overrunsWorksheet);

            // Make report in sheet
            MakeAdditionalReport(resultCellsForAddReports, ref overrunsWorksheet, Overruns);
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

        await Report(goodByMessage, 1500, true);
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
        if (File.Exists(path)) return true;
  
        Console.WriteLine($"Файл по пути\n{path}\nне найден. Проверьте правильность указания данных.");
        return null;
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

    public static void CreateWorkSheet(in ExcelPackage excelPackage, string workSheetName, out ExcelWorksheet worksheet, System.Drawing.Color tabColor)
    {
        // Remove existing worksheet
        var existingWorksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(x => x.Name == workSheetName);
        if (existingWorksheet is not null)
            excelPackage.Workbook.Worksheets.Delete(existingWorksheet);

        // Create net worksheet
        worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);

        // Stilized workSheet
        worksheet.TabColor = tabColor;
        worksheet.DefaultRowHeight = DEFAULT_TABLE_ROW_HEIGHT;
        worksheet.DefaultColWidth = TABLE_COL_WIDTH;
    }

    private static void SetupTableHeader(in ResultCell[] resultCells, ref ExcelWorksheet worksheet)
    {
        worksheet.Row(FIRST_ROW_INDEX).Height = HEADER_TABLE_ROW_HEIGHT;
        worksheet.Row(FIRST_ROW_INDEX).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Row(FIRST_ROW_INDEX).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Row(FIRST_ROW_INDEX).Style.Font.Bold = true;
        worksheet.Row(FIRST_ROW_INDEX).Style.Font.Size = 14;

        foreach (var currentColumn in resultCells)
        {
            int i = currentColumn.ColumnNumber;
            ExcelColumn? columnInWorkSheet = worksheet.Column(i);

            if(columnInWorkSheet is null) continue;

            columnInWorkSheet.Width = currentColumn.ColumnWidth;
            columnInWorkSheet.Style.Numberformat.Format = currentColumn.Format;
            columnInWorkSheet.Style.WrapText = currentColumn.WrapTextInHeader;
            
            // Add name of column
            worksheet.Cells[FIRST_ROW_INDEX, i].Value = resultCells[i].Name;
           
            // Rotation text in header 
            worksheet.Cells[FIRST_ROW_INDEX, i].Style.TextRotation = currentColumn.TextRotationInHeader;

             worksheet.Cells[FIRST_ROW_INDEX, i].AutoFitColumns();
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

    private static void MakeAdditionalReport(ResultCell[] cells, ref ExcelWorksheet worksheet, List<ArticleItemForReport> items)
    {
        // Fill header data 
        SetupTableHeader(cells, ref worksheet);

        // Fill table data 
        int recordIndex = FIRST_ROW_INDEX + 1;
        foreach (var item in items)
        {
            worksheet.Cells[recordIndex, 1].Value = item.ArticleNumber;
            worksheet.Cells[recordIndex, 2].Value = item.Name;
            worksheet.Cells[recordIndex, 3].Value = item.Count;
            worksheet.Cells[recordIndex, 4].Value = item.Cost;
            worksheet.Cells[recordIndex, 5].Value = item.Sum;

            recordIndex++;
        }

        // Fill total sum
        worksheet.Cells[recordIndex, 5].Value = items.Aggregate(0.00, (acc, i) => acc + i.Sum);

        // Stilized table - fit text
        worksheet.Column(2).Width = 60;
        for (int i = 1; i < cells.Length + 1; i++)
        {
            worksheet.Column(i).Style.WrapText = true;
        }

        AddFilterInWorkSheet(ref worksheet);
    }
    #endregion
}
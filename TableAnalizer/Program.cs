// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

#nullable enable

using System.Diagnostics;
using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ClientTableAnalizer.Objects;

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
    private static DirectoryInfo? clientDirectoryPath;

    private static readonly Dictionary<int, Article> articleDict = new();

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

        await GetUserRedemtionDataFile(Marketplace.WB);

        await GetUserRedemtionDataFile(Marketplace.OZ);

        await ReadRedemptionReportFile(Marketplace.WB);

        await ReadRedemptionReportFile(Marketplace.OZ);

        await GetUserStorageData(Marketplace.WB);

        await GetUserStorageData(Marketplace.OZ);

        await ReadStorageReportFile(Marketplace.WB);

        await ReadStorageReportFile(Marketplace.OZ);

        await GetUserConsumingData();

        await ReadConsumptionReport(consumptionPath);

        await GetUserPayShortagesData();

        if (payShortagesPath != null)
        {
            await ReadPayShortagesReport(payShortagesPath);
        }

        await GetRegrandingData();

        if (regradingPath != null)
        {
            await ReadRegrandingReport(regradingPath);
        }

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

    private static async Task GetUserRedemtionDataFile(Marketplace marketplace)
    {
        var clDirInfo = FileManager.GetDirectoryInfo(PathKey.MPBoostClients);
        if (clDirInfo is null || !clDirInfo.Exists)
            FileManager.CreateDirectoryInfo(PathKey.MPBoostClients);

        var necessarilyMessage = marketplace == Marketplace.WB ? "обязательно" : "не обязательно";
        var messageInstruction = marketplace == Marketplace.WB ? "ESC - выход из программы" : "Пробел - пропустить, ESC - выход из программы";

        var redemtionMessage01 = $"Перетащите путь к отчету по выкупам клиента на {marketplace} ({necessarilyMessage})\n({messageInstruction}):\n";

        if(marketplace == Marketplace.WB)
        {
            redemtionFilePathWB = await GiveMePath(redemtionMessage01, true, true);

            await Report($"Логин клиента: {login}");

            clientDirectoryPath = FileManager.GetDirectoryInfo(PathKey.MPBoostClients, login);

            if (clientDirectoryPath is not null)
            {
                //await Report($"Директория для работы с клиентом:\n{clientDirectoryPath.FullName}");

                var dateTime = DateTime.Now.ToString("dd-MM-yyyy");
                outputPath = $"{clientDirectoryPath.FullName}/ClientReport_{login}_{dateTime}.xlsx";
            }
            else
            {
                await Report($"Невозможно найти или создать директорию для работы с клиентом.");
            }
        }
        else if(marketplace == Marketplace.OZ)
        {
            redemtionFilePathOZ = await GiveMePath(redemtionMessage01, false, false);
        }

        Console.WriteLine();
    }

    private static async Task GetUserStorageData(Marketplace marketplace)
    {
        var necessarilyMessage = marketplace == Marketplace.WB ? "обязательно" : "не обязательно";
        var messageInstruction = marketplace == Marketplace.WB ? "ESC - выход из программы" : "Пробел - пропустить, ESC - выход из программы";
        var storagePathMessage = $"Перетащите путь к отчету по приходу и остаткам товаров {marketplace} ({necessarilyMessage})\n(и{messageInstruction}):";

        switch (marketplace)
        {
            case Marketplace.WB:
                storageFilePathWB = await GiveMePath(storagePathMessage, marketplace == Marketplace.WB, false);
                break;
            case Marketplace.OZ:
                storageFilePathOZ = await GiveMePath(storagePathMessage, marketplace == Marketplace.WB, false);
                break;
        }

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
           "(Пробел - пропустить, ESC - выход из программы):";
        payShortagesPath = await GiveMePath(payShortagesDataMessage, false, false);
        Console.WriteLine();
    }

    private static async Task GetRegrandingData()
    {
        var regrandingDataMessage = "Перетащите путь к отчету по пересортам (не обязательно)\n" +
          "(Пробел - пропустить, ESC - выход из программы):";
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
                if (redemtionFilePathWB is null || CheckFileExistence(redemtionFilePathWB) == null) await Report(goodByMessage, true);
                break;
            case Marketplace.OZ:
                if (redemtionFilePathOZ is null || CheckFileExistence(redemtionFilePathOZ) == null) await Report(goodByMessage, true);
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
                            await Report("Невозможно создать Item по выкупу.", true);
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

        if (filePath is null || CheckFileExistence(filePath) == null) await Report(goodByMessage, true);

        var existingFile = new FileInfo(filePath!);

        using ExcelPackage package = new(existingFile);

        if (CheckSheets(in package) == null) await Report(goodByMessage, true);

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

    private static async Task ReadConsumptionReport(string? filePath)
    {
        const int ARTICLE_INDEX = 4;
        const int COUNT_INDEX = 28;

        if (filePath is null && CheckFileExistence(filePath!) == null)
        {
            await Report(goodByMessage, true);
        }
        else
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using ExcelPackage package = new(new FileInfo(filePath!));

            if (CheckSheets(in package) == null) await Report(goodByMessage, true);

            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

            for (int row = 1; row <= rowCount; row++)
            {
                var articleAddress = worksheet.Cells[row, ARTICLE_INDEX];
                var countAddress = worksheet.Cells[row, COUNT_INDEX];

                if (articleAddress.Value != null && int.TryParse(articleAddress.Value.ToString(), out int article))
                {
                    if (countAddress.Value != null && int.TryParse(countAddress.Value.ToString(), out int count))
                    {
                        if (articleDict.ContainsKey(article))
                        {
                            articleDict[article].SetStoreBalance(ItemBalanceUnit.Consumption, count);
                        }
                    }
                }
            }

            package.Dispose();
            stopwatch.Stop();

            var con = articleDict.Sum(art => art.Value.StoreBalance.ShippedQuantity);

            var conReport = $"Общий расход по товарам клиента: {con}.\n";
            conReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

            await Report(conReport);
        }
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
                    priceAddress.Value != null && int.TryParse(priceAddress.Value.ToString(), out int price))
                {
                    if (articleDict.ContainsKey(article))
                    {
                        articleDict[article].SetStoreBalance(count, price);
                    }
                }
            }

            package.Dispose();
            stopwatch.Stop();

            var paySum =
                from art in articleDict
                select art.Value.StoreBalance.PayShortagesSum;

            string payReport;

            if (paySum is not null && paySum.Count() > 0)
            {
                var aggregatePaySum = paySum.Aggregate((acc, sum) => acc + sum);
                payReport = string.Format("Сумма компенсации за недостачти товаров: {0:C}.\n", aggregatePaySum);
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

    private static async Task WriteXls(string? ouptutFilePath, string workSheetName = $"Report")
    {
        const int HEADER_TABLE_ROW_HEIGHT = 40;
        const int DEFAULT_TABLE_ROW_HEIGHT = 16;
        const int TABLE_COL_WIDTH = 17;
        const int FIRST_ROW_INDEX = 1;

        using var excel = new ExcelPackage(ouptutFilePath);

        // Remove existing worksheet
        var existingWorksheet = excel.Workbook.Worksheets.SingleOrDefault(x => x.Name == workSheetName);

        if(existingWorksheet is not null)
        {
            excel.Workbook.Worksheets.Delete(existingWorksheet);
        }

        var newWorkSheet = excel.Workbook.Worksheets.Add(workSheetName);

        // Stilized workSheet
        newWorkSheet.TabColor = System.Drawing.Color.YellowGreen;
        newWorkSheet.DefaultRowHeight = DEFAULT_TABLE_ROW_HEIGHT;
        newWorkSheet.DefaultColWidth = TABLE_COL_WIDTH;

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
        for (int i = 0; i < resultCells.Length; i++)
        {
            var colIndex = i + 1;

            // Add data
            newWorkSheet.Cells[1, colIndex].Value = resultCells[i].Name;
            newWorkSheet.Cells[1, colIndex].AutoFitColumns();

            // Stilized
            newWorkSheet.Column(colIndex).AutoFit();
            newWorkSheet.Column(colIndex).Style.Numberformat.Format = resultCells[i].Format;
        }

        // Stilized workSheet header
        newWorkSheet.Row(FIRST_ROW_INDEX).Height = HEADER_TABLE_ROW_HEIGHT;
        newWorkSheet.Row(FIRST_ROW_INDEX).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        newWorkSheet.Row(FIRST_ROW_INDEX).Style.Font.Bold = true;
        newWorkSheet.Row(FIRST_ROW_INDEX).Style.Font.Size = 14;
        newWorkSheet.Row(FIRST_ROW_INDEX).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        newWorkSheet.Column(1).Width = 8;   // МП
        newWorkSheet.Column(2).Width = 12;  // Артикул 
        newWorkSheet.Column(3).Width = 60;  // Название 
        newWorkSheet.Column(20).Width = 60; // Комментарий

        // Populating with data

        int recordIndex = FIRST_ROW_INDEX + 1;

        foreach (var art in articleDict)
        {
            art.Value.AnalyzeArticle();

            // Fill Redemptions data
            newWorkSheet.Cells[recordIndex, 1].Value = art.Value.Marketplace;
            newWorkSheet.Cells[recordIndex, 2].Value = art.Key;
            newWorkSheet.Cells[recordIndex, 3].Value = art.Value.Name;
            newWorkSheet.Cells[recordIndex, 4].Value = art.Value.AveragePrice;

            // RedemptionDate
            newWorkSheet.Cells[recordIndex, 5].Value = art.Value.MaxRedemptionDate;
            newWorkSheet.Cells[recordIndex, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            newWorkSheet.Cells[recordIndex, 5].Style.Fill.BackgroundColor.SetColor(art.Value.StatusValue.color);

            newWorkSheet.Cells[recordIndex, 6].Value = art.Value.StatusValue.statusComment;

            newWorkSheet.Cells[recordIndex, 7].Value = art.Value.Items.Count;
            newWorkSheet.Cells[recordIndex, 8].Value = art.Value.RedempionBalance.CanceledCount;
            newWorkSheet.Cells[recordIndex, 9].Value = art.Value.RedempionBalance.OnWayCount;
            newWorkSheet.Cells[recordIndex, 10].Value = art.Value.RedempionBalance.DeliveredCount;
            newWorkSheet.Cells[recordIndex, 11].Value = art.Value.RedempionBalance.WithDrawnCount;

            // Balance data
            // - WaitBalance
            newWorkSheet.Cells[recordIndex, 12].Value = art.Value.RedempionBalance.WaitBalance;
            newWorkSheet.Cells[recordIndex, 12].Style.Font.Bold = true;

            newWorkSheet.Cells[recordIndex, 13].Value = art.Value.StoreBalance.ReceivedQuantity;

            // - Undelivery
            newWorkSheet.Cells[recordIndex, 14].Value = art.Value.StoreBalance.Undelivery;
            if(art.Value.StoreBalance.Undelivery > 0 && art.Value.StatusValue.status != ArticleStatus.InWork)
            {
                newWorkSheet.Cells[recordIndex, 14].Style.Font.Color.SetColor(System.Drawing.Color.DarkRed); 
            }

            // Consumption
            newWorkSheet.Cells[recordIndex, 15].Value = art.Value.StoreBalance.TotalConsumption;
            newWorkSheet.Cells[recordIndex, 16].Value = art.Value.StoreBalance.ShippedQuantity;
            newWorkSheet.Cells[recordIndex, 17].Value = art.Value.StoreBalance.Regradings.Sum(re => re.Value);
            newWorkSheet.Cells[recordIndex, 18].Value = art.Value.StoreBalance.PayShortagesCount;

            newWorkSheet.Cells[recordIndex, 19].Value = art.Value.StoreBalance.FinBalance;

            // Comment
            newWorkSheet.Cells[recordIndex, 20].Value = art.Value.Comment;

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
        int firstRow = newWorkSheet.Dimension.Start.Row;
        int endRow = newWorkSheet.Dimension.End.Row;

        int firstCol = newWorkSheet.Dimension.Start.Column;
        int endCol = newWorkSheet.Dimension.End.Column;

        string firstColLet = ExcelCellAddress.GetColumnLetter(firstCol);
        string endColLet = ExcelCellAddress.GetColumnLetter(endCol);

        newWorkSheet.Cells[$"{firstColLet}{firstRow}:{endColLet}{endRow}"].AutoFilter = true;

        //Freeze row && columns
        newWorkSheet.View.FreezePanes(3, 4);

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

        await Report(goodByMessage, true);
    }

    private static async Task Report(string message, bool exit = false)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine();

        if (exit)
        {
            await Task.Delay(3000);
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

    private static async Task<string> GiveMePath(string message, bool necessarily, bool giveMeLogin)
    {
        string? path = default;

        while(string.IsNullOrEmpty(path))
        {
            Console.Clear();
            Console.WriteLine(message);

            var key = Console.ReadKey();
            path = Console.ReadLine();

            if (key.Key == ConsoleKey.Spacebar && !necessarily) break;
            if (key.Key == ConsoleKey.Escape) await Report(goodByMessage, true);
        }

        if (giveMeLogin)
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

    private static (int col, int row) FindNumberColsAndRowsIn(in ExcelWorksheet sheet)
    {
        return (sheet.Dimension.End.Column, sheet.Dimension.End.Row);
    }
}
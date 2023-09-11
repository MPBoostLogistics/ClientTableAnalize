// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

#nullable enable

using System.Diagnostics;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TableAnalizer.Objects;

internal struct PayShortages
{
    public int Count { get; }
    public double Price { get; }
    public double Sum { get; }

    public PayShortages(int count, double price)
    {
        Count = count;
        Price = price;
        Sum = Count * Price;
    }
}

internal class Program
{
    #region Variables and constants

    private const string LOGISTIC_FLAG = "Да";
    private const string NAME_FIRST_ROOM = "1 - Помещение приемки, сортировки и упаковки";
    private const string NAME_SECOND_ROOM = "2 - Помещение длительного хранения и отгрузки";
    private const string NAME_FINAL_ROW = "Итого товаров в единицах хранения";

    private static string? login = default, redemtionFilePath = default, storageFilePath = default,
        consumptionPath = default, payShortagesPath = default, regradingPath = default, outputPath = default; 
    private static DirectoryInfo? clientDirectoryPath;

    private static readonly Dictionary<int, Article> articleDict = new();

    private static readonly string goodByMessage = "\nСпасибо за использование программы. Хорошего дня!";

    #endregion

    private static async Task Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        Console.Clear();
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine("Программа анализа работы с клиентом логистики");
        Console.WriteLine("---------------------------------------------");

        await GetUserRedemtionData(goodByMessage);

        await ReadRedemptionReportFile(redemtionFilePath);

        await GetUserStorageData(goodByMessage);

        await ReadStorageReportFile(storageFilePath);

        await GetUserConsumingData(goodByMessage);

        await ReadConsumptionReport(consumptionPath);

        await GetUserPayShortagesData();

        if(payShortagesPath != null)
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


    private static async Task GetUserRedemtionData(string goodByMessage)
    {
        // Get 'MPBoostClients' directory
        var clDirInfo = FileManager.GetDirectoryInfo(PathKey.MPBoostClients);

        if (clDirInfo is null || !clDirInfo.Exists)
        {
            clDirInfo = FileManager.CreateDirectoryInfo(PathKey.MPBoostClients);
            if (clDirInfo is not null && clDirInfo!.Exists)
            {
                await Report("Директория '/Documents/MPBoostClients' создана.");
            }
        }
        else
        {
            await Report("Директория '/Documents/MPBoostClients' существует.");
        }

        while (string.IsNullOrEmpty(redemtionFilePath) && string.IsNullOrEmpty(login))
        {
            Console.WriteLine("1. Перетащите путь к отчету по выкупам клиента (обязательно)\n" +
                "(или нажмите ESC для выхода из программы):");

            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.Escape)
            {
                await Report(goodByMessage, true);
            }

            redemtionFilePath = Console.ReadLine();

            if (string.IsNullOrEmpty(redemtionFilePath)) continue;

            var substrings01 = redemtionFilePath.Split("/");

            if (substrings01.Length <= 1) continue;

            var loginChars = substrings01.Last().Split(" ")[3].Trim().ToList();

            if (loginChars is null || loginChars.Count == 0) continue;

            // Clear non-digits in login
            for (int i = 0; i < loginChars.Count; i++)
            {
                if (!int.TryParse(loginChars[i].ToString(), out _)) loginChars.RemoveAt(i);
            }

            login = new string(loginChars.ToArray());

            await Report($"Логин клиента: {login}");

            clientDirectoryPath = FileManager.GetDirectoryInfo(PathKey.MPBoostClients, login);

            if (clientDirectoryPath is not null)
            {
                await Report($"Директория для работы с клиентом:\n{clientDirectoryPath.FullName}");

                var dateTime = DateTime.Now.ToShortDateString();
                outputPath = $"{clientDirectoryPath.FullName}/ClientReport_{login}_{dateTime}.xlsx";
            }
            else
            {
                await Report($"Невозможно найти или создать директорию для работы с клиентом.");
            }

            redemtionFilePath = FormСorrectPath(redemtionFilePath);

            Console.WriteLine();
        }
    }

    private static async Task GetUserStorageData(string goodByMessage)
    {
        while (string.IsNullOrEmpty(storageFilePath))
        {
            var message02 = "2. Перетащите путь к отчету по приходу и остаткам клиента (обязательно)\n(или нажмите ESC для выхода из программы):";

            await Report(message02);

            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.Escape)
            {
                await Report(goodByMessage, true);
            }

            storageFilePath = Console.ReadLine();

            if (string.IsNullOrEmpty(storageFilePath)) continue;
        }

        storageFilePath = FormСorrectPath(storageFilePath);

        Console.WriteLine();
    }

    private static async Task GetUserConsumingData(string goodByMessage)
    {
        while (string.IsNullOrEmpty(consumptionPath))
        {
            var message03 = "3. Перетащите путь к отчету по расходу товаров клиента со склада (обязательно)\n" +
                "(или нажмите ESC для выхода из программы):";

            await Report(message03);

            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.Escape)
            {
                await Report(goodByMessage, true);
            }

            consumptionPath = Console.ReadLine();

            if (string.IsNullOrEmpty(consumptionPath)) continue;
        }

        consumptionPath = FormСorrectPath(consumptionPath);

        Console.WriteLine();
    }

    private static async Task GetUserPayShortagesData()
    {
        var message = "Есть отчет погашении недостач?\n(1 - Да, Любая другая клавиша - Нет):";

        await Report(message);

        ConsoleKeyInfo keyInfo = Console.ReadKey();

        if (keyInfo != default)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.D1:
                    break;
                default:
                    await Report("Отчет о погашении недостач не загружен и не будет учтен.");
                    return;
            }
        }

        while(string.IsNullOrEmpty(payShortagesPath))
        {
            var messageStep04 = "\n4.Перетащите путь к отчету о погашении недостач\n(ESC - выход из программы):";

            await Report(messageStep04);

            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.Escape)
            {
                await Report(goodByMessage, true);
            }

            payShortagesPath = Console.ReadLine();
        }

        payShortagesPath = FormСorrectPath(payShortagesPath);

        Console.WriteLine();
    }

    private static async Task GetRegrandingData()
    {
        var message = "Есть отчет о пересортах товаров?\n(1 - Да, Любая другая клавиша - Нет):";

        await Report(message);

        ConsoleKeyInfo keyInfo = Console.ReadKey();

        if (keyInfo != default)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.D1:
                    break;
                default:
                    await Report("Отчет о пересортах товаров не загружен и не будет учтен.");
                    return;
            }
        }

        while (string.IsNullOrEmpty(regradingPath))
        {
            message = "\n5. Перетащите путь к отчету о пересортах тоавров\n(ESC - выход из программы):";

            await Report(message);

            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.Escape)
            {
                await Report(goodByMessage, true);
            }

            regradingPath = Console.ReadLine();
        }

        regradingPath = FormСorrectPath(regradingPath);

        Console.WriteLine();
    }

    private static async Task ReadRedemptionReportFile(string? filePath)
    {
        const int ARTICLE_COL_INDEX = 2;
        const int DATE_FLAG_COL_INDEX = 8;
        const int STATUS_COL_INDEX = 10;
        const int LOG_FLAG_COL_INDEX = 11;
        const int PRICE_COL_INDEX = 15;

        if (filePath is null || CheckFileExistence(filePath) == null)
        {
            await Report(goodByMessage, true);
        }
        else
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await Report($"Анализирую отчет клиента {login} по выкупам...");

            using ExcelPackage package = new(new FileInfo(filePath));

            if (CheckSheets(in package) == null) Environment.Exit(0);

            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            (int colCount, int rowCount) = FindNumberColsAndRowsIn(in worksheet);

            for (int row = 1; row <= rowCount; row++)
            {
                var logisticFlagValue = worksheet.Cells[row, LOG_FLAG_COL_INDEX].Value.ToString();

                if (logisticFlagValue != null)
                {
                    var logisticFlag = logisticFlagValue.Replace(" ", "");

                    if (logisticFlag == LOGISTIC_FLAG &&
                        worksheet.Cells[row, ARTICLE_COL_INDEX].Value is not null &&
                        int.TryParse(worksheet.Cells[row, ARTICLE_COL_INDEX].Value.ToString(), out int articleIdKey))
                    {
                        var redemptionDateString = worksheet.Cells[row, DATE_FLAG_COL_INDEX].Value.ToString();
                        var itemStatusValue = worksheet.Cells[row, STATUS_COL_INDEX].Value.ToString();
                        var priceSting = worksheet.Cells[row, PRICE_COL_INDEX].Value.ToString();

                        if (redemptionDateString != null && itemStatusValue != null && priceSting != null)
                        {
                            var item = new ArticleRowItem(redemptionDateString, itemStatusValue.Trim(), priceSting.Trim());

                            if (articleDict.ContainsKey(articleIdKey))
                            {
                                articleDict[articleIdKey].Items.Add(item);
                            }
                            else
                            {
                                articleDict[articleIdKey] = new Article(item);
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
    }

    private static async Task ReadStorageReportFile(string? filePath)
    {
        if (filePath is null || CheckFileExistence(filePath) == null)
        {
            await Report(goodByMessage, true);
        }
        else
        {
            var existingFile = new FileInfo(filePath);
            using ExcelPackage package = new(existingFile);

            if (CheckSheets(in package) == null)
            {
                await Report(goodByMessage, true);
            }
            else
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                await Report($"Анализирую отчет клиента {login} по приходу и остаткам...");

                const int COL_STORE_COUNT_INDEX = 17;

                (int articleIndex, int nameIndex, int initBalanceIndex, int comingIndex, int finBalanceIndex) columnIndexes = (1, 6, 14, 15, 17);

                int rowFirstRoomAddress = default;
                int rowSecondRoomAddress = default;
                int rowFinalRoomAddress = default;

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                (_, int rowStoreCount) = FindNumberColsAndRowsIn(in worksheet);

                // Find addresses of special rows
                for (int row = 1; row <= rowStoreCount; row++)
                {
                    for (int col = 1; col <= COL_STORE_COUNT_INDEX; col++)
                    {

                        var addressString = worksheet.Cells[row, col].Value;

                        if (addressString is not null)
                        {
                            switch (addressString!.ToString().Trim())
                            {
                                case NAME_FIRST_ROOM:
                                    rowFirstRoomAddress = row;
                                    break;
                                case NAME_SECOND_ROOM:
                                    rowSecondRoomAddress = row;
                                    break;
                                case NAME_FINAL_ROW:
                                    rowFinalRoomAddress = row;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    if (rowFirstRoomAddress != default && rowSecondRoomAddress != default && rowFinalRoomAddress != default)
                    {
                        break;
                    }
                }

                // make row ranges 
                var firstRoomRowRange = Enumerable.Range(rowFirstRoomAddress + 1, rowSecondRoomAddress - 1).ToList();
                var secondRoomRowRange = Enumerable.Range(rowSecondRoomAddress + 1, rowFinalRoomAddress).ToList();

                // Get coming && finalBalance++
                for (int row = firstRoomRowRange.First(); row <= firstRoomRowRange.Count; row++)
                {
                    var article = Convert.ToInt32(worksheet.Cells[row, columnIndexes.articleIndex].Value);

                    if (articleDict.ContainsKey(article))
                    {
                        var name = worksheet.Cells[row, columnIndexes.nameIndex].Value.ToString().Split(",")[0].Trim();
                        if (name is not null)
                        {
                            articleDict[article]!.SetName(name);
                        }

                        var com = Convert.ToInt32(worksheet.Cells[row, columnIndexes.comingIndex].Value);
                        articleDict[article].StoreBalance.ChangeBalance(ItemBalanceUnit.Coming, com);

                        var finalBalance = Convert.ToInt32(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                        articleDict[article].StoreBalance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance);
                    }
                }

                // Get finalBalance++
                for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
                {
                    var article = Convert.ToInt32(worksheet.Cells[row, columnIndexes.articleIndex].Value);

                    if (articleDict.ContainsKey(article))
                    {
                        var finalBalance = Convert.ToInt32(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                        articleDict[article].StoreBalance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance);
                    }
                }

                package.Dispose();
                stopwatch.Stop();

                var coming = articleDict.Sum(art => art.Value.StoreBalance.Coming);
                var finBalance = articleDict.Sum(art => art.Value.StoreBalance.FinBalance);

                var comBalanceReport = $"Общий приход товаров: {coming}, общий остаток: {finBalance}\n";
                comBalanceReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

                await Report(comBalanceReport);
            }
        }
    }

    private static async Task ReadConsumptionReport(string? filePath)
    {
        const int ARTICLE_INDEX = 4;
        const int COUNT_INDEX = 28;

        if (filePath is null && CheckFileExistence(filePath) == null)
        {
            await Report(goodByMessage, true);
        }
        else
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using ExcelPackage package = new(new FileInfo(filePath));

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
                            articleDict[article].ChangeBalance(ItemBalanceUnit.Consumption, count);
                        }
                    }
                }
            }

            package.Dispose();
            stopwatch.Stop();

            var con = articleDict.Sum(art => art.Value.StoreBalance.Consumption);

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
                        articleDict[article].SetPayShortages(count, price);
                    }
                }
            }

            package.Dispose();
            stopwatch.Stop();

            var paySum =
                from art in articleDict
                from pay in art.Value.PayShortages
                select pay.Sum;

            var aggregatePaySum = paySum.Aggregate((acc, sum) => acc + sum);

            var payReport = string.Format("Сумма компенсации за недостачти товаров: {0:C}.\n", aggregatePaySum);
            payReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

            await Report(payReport);
        }
    }

    private static async Task ReadRegrandingReport(string filePath)
    {
        const int ARTICLE_INDEX = 1;
        const int REGRANDING_INDEX = 2;

        if (filePath is null || CheckFileExistence(filePath) == null)
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

            int regrandingCount = default;

            for (int row = 1; row <= rowCount; row++)
            {
                var articleAddress = worksheet.Cells[row, ARTICLE_INDEX];
                var regrandingAddress = worksheet.Cells[row, REGRANDING_INDEX];


                if (articleAddress.Value != null &&
                    int.TryParse(articleAddress.Value.ToString(), out int article) &&
                    articleDict.ContainsKey(article))
                {
                    articleDict[article].SetRegranding(true);
                    regrandingCount++;
                }


                if (regrandingAddress.Value != null &&
                    int.TryParse(regrandingAddress.Value.ToString(), out int regranding) &&
                    articleDict.ContainsKey(regranding))
                {
                    articleDict[regranding].SetRegranding(true);
                    regrandingCount++;
                }

            }

            package.Dispose();
            stopwatch.Stop();

            var regReport = $"Количество артикулов в пересорте: {regrandingCount}.\n";
            regReport += $"Анализ отчета занял {stopwatch.ElapsedMilliseconds / 1000} секунд.";

            await Report(regReport);
        }
    }

    private static async Task WriteXls(string? ouptutFilePath, string workSheetName = $"Report")
    {
        const int HEADER_TABLE_ROW_HEIGHT = 24;
        const int DEFAULT_TABLE_ROW_HEIGHT = 16;
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

        ResultCell[] resultCells = new ResultCell[]
        {
                new ResultCell("Артикул", "0"),
                new ResultCell("Название", "0"),
                new ResultCell("Всего", "0"),
                new ResultCell("Отмен", "0"),
                new ResultCell("В пути", "0"),
                new ResultCell("Доставлен на ПВЗ", "0"),
                new ResultCell("Забран с ПВЗ", "0"),
                new ResultCell("Дата последнего выкупа", "dd.MM.yyyy"),
                new ResultCell("Склад: Ожидание", "0"),
                new ResultCell("Склад: Приход", "0"),
                new ResultCell("Склад: Расход", "0"),
                new ResultCell("Склад: Остаток", "0"),
                new ResultCell("Средняя цена", "0.00"),
                new ResultCell("Оплачено", "0.00"),
                new ResultCell("Примечание", "0"),
        };

        // Fill header data 
        for (int i = 0; i < resultCells.Length; i++)
        {
            var colIndex = i + 1;

            // Add data
            newWorkSheet.Cells[1, colIndex].Value = resultCells[i].Name;

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

        // Populating with data

        int recordIndex = FIRST_ROW_INDEX + 1;

        foreach (var art in articleDict)
        {
            art.Value.Analize();

            // Fill Redemptions data
            newWorkSheet.Cells[recordIndex, 1].Value = art.Key;
            newWorkSheet.Cells[recordIndex, 2].Value = art.Value.Name;
            newWorkSheet.Cells[recordIndex, 3].Value = art.Value.Items.Count;
            newWorkSheet.Cells[recordIndex, 4].Value = art.Value.RedBalance.CanceledCount;
            newWorkSheet.Cells[recordIndex, 5].Value = art.Value.RedBalance.OnWayCount;
            newWorkSheet.Cells[recordIndex, 6].Value = art.Value.RedBalance.DeliveredCount;
            newWorkSheet.Cells[recordIndex, 7].Value = art.Value.RedBalance.WithDrawnCount;
            newWorkSheet.Cells[recordIndex, 8].Value = art.Value.MaxRedemptionDate;

            newWorkSheet.Cells[recordIndex, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
            newWorkSheet.Cells[recordIndex, 8].Style.Fill.BackgroundColor.SetColor(art.Value.StatusColor);

            // Balance data
            newWorkSheet.Cells[recordIndex, 9].Value = art.Value.StoreBalance.WaitBalance;
            newWorkSheet.Cells[recordIndex, 10].Value = art.Value.StoreBalance.Coming;
            newWorkSheet.Cells[recordIndex, 11].Value = art.Value.StoreBalance.Consumption;
            newWorkSheet.Cells[recordIndex, 12].Value = art.Value.StoreBalance.FinBalance;

            // Price
            newWorkSheet.Cells[recordIndex, 13].Value = art.Value.AveragePrice;

            // Payment
            newWorkSheet.Cells[recordIndex, 14].Value = art.Value.PayShortages.Sum(el => el.Count);

            // Comment
            newWorkSheet.Cells[recordIndex, 15].Value = art.Value.Comment;

            recordIndex++;
        }

        // Stilized table - fit && data format
        for (int i = 1; i < resultCells.Length + 1; i++)
        {
            newWorkSheet.Column(i).AutoFit();
            newWorkSheet.Column(i).Style.Numberformat.Format = i != 8 ? "0" : "dd.MM.yyyy";
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

    private static (int col, int row) FindNumberColsAndRowsIn(in ExcelWorksheet sheet)
    {
        return (sheet.Dimension.End.Column, sheet.Dimension.End.Row);
    }
}
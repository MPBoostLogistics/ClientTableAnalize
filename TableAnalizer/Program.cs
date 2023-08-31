// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5
// https://htmlcolorcodes.com/

#nullable enable

using OfficeOpenXml;
using OfficeOpenXml.Style;
using TableAnalizer.Objects;

internal class Program
{
    #region Variables and constants

    private const string LOGISTIC_FLAG = "Да";
    private const string NAME_FIRST_ROOM = "1 - Помещение приемки, сортировки и упаковки";
    private const string NAME_SECOND_ROOM = "2 - Помещение длительного хранения и отгрузки";
    private const string NAME_FINAL_ROW = "Итого товаров в единицах хранения";

    private const string pathRedemptionReport = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/RedemptionReport.xlsx";
    private const string storageReportPath = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/StorageReport.xlsx";
    private const string consumptionPath = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/Consumption.xlsx";
    private const string outputPath = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/Report.xlsx";

    private static readonly Dictionary<int, Article> articleDict = new();

    #endregion

    private static void Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        ReadRedemptionReportFile(pathRedemptionReport);

        ReadStorageReportFile(storageReportPath);

        ReadConsumptionReport(consumptionPath);

        WriteXls(outputPath);

        static void ReadRedemptionReportFile(string filePath)
        {
            const int ARTICLE_COL_INDEX = 2;
            const int DATE_FLAG_COL_INDEX = 8;
            const int STATUS_COL_INDEX = 10;
            const int LOG_FLAG_COL_INDEX = 11;
            const int PRICE_COL_INDEX = 15;

            if (CheckFileExistence(filePath) == null)
            {
                Environment.Exit(0);
            }
            else
            {
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
                                Console.WriteLine("Невозможно создать Item по выкупу.");
                                Console.ReadLine();
                                Environment.Exit(0);
                            }
                        }
                    }
                }

                package.Dispose();
            }
        }

        static void ReadStorageReportFile(string filePath)
        {
            if (CheckFileExistence(filePath) == null)
            {
                Environment.Exit(0);
            }
            else
            {
                var existingFile = new FileInfo(filePath);
                using ExcelPackage package = new(existingFile);

                if (CheckSheets(in package) == null)
                {
                    Environment.Exit(0);
                }
                else
                {
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
                            if(name is not null)
                            {
                                articleDict[article]!.SetName(name);
                            }

                            var coming = Convert.ToInt32(worksheet.Cells[row, columnIndexes.comingIndex].Value);
                            articleDict[article].StoreBalance.ChangeBalance(ItemBalanceUnit.Coming, coming);

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
                }

                package.Dispose();
            }
        }

        static void ReadConsumptionReport(string filePath)
        {
            const int ARTICLE_INDEX = 4;
            const int COUNT_INDEX = 28;

            if (CheckFileExistence(filePath) == null)
            {
                Environment.Exit(0);
            }
            else
            {
                using ExcelPackage package = new(new FileInfo(filePath));

                if (CheckSheets(in package) == null) Environment.Exit(0);

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
                            if(articleDict.ContainsKey(article))
                            {
                                articleDict[article].ChangeBalance(ItemBalanceUnit.Consumption, count);
                            }
                        }
                    }
                }

                package.Dispose();
            }
        }
    }

    private static void WriteXls(string ouptutFilePath, string workSheetName = "Report")
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
                new ResultCell("Средняя цена", "0.00")
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
    }

    private static bool? CheckFileExistence(string path)
    {
        if (File.Exists(path))
        {
            return true;
        }
        else
        {
            Console.WriteLine($"Файл по пути\n{path}\nне найден");
            Console.ReadLine();
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
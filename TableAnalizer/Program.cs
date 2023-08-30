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
            const int ARTICLE_COL_INDEX = 0;
            const int DATE_FLAG_COL_INDEX = 8;
            const int STATUS_COL_INDEX = 10;
            const int LOG_FLAG_COL_INDEX = 11;


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
                            int.TryParse(worksheet.Cells[row, ARTICLE_COL_INDEX].Value.ToString(), out int article))
                        {
                            var redemptionDateString = worksheet.Cells[row, DATE_FLAG_COL_INDEX].Value.ToString();
                            var itemStatusValue = worksheet.Cells[row, STATUS_COL_INDEX].Value.ToString();

                            if (redemptionDateString != null && itemStatusValue != null)
                            {
                                var item = new Item(redemptionDateString, itemStatusValue.Trim());

                                if (articleDict.ContainsKey(article))
                                {
                                    articleDict[article].Items.Add(item);
                                }
                                else
                                {
                                    articleDict[article] = new Article(item);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Невозможно содать Item по выкупу.");
                                Console.ReadLine();
                                Environment.Exit(0);
                            }
                        }
                    }

                }
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

        static void ReadStorageReportFile(string filePath)
        {
            if(CheckFileExistence(filePath) == null)
            {
                Environment.Exit(0);
            }
            else
            {
                var existingFile = new FileInfo(filePath);
                using ExcelPackage package = new(existingFile);

                if(CheckSheets(in package) == null)
                {
                    Environment.Exit(0);
                }
                else
                {
                    const int COL_STORE_COUNT_INDEX = 17;

                    (int articleIndex, int initBalanceIndex, int comingIndex,int finBalanceIndex) columnIndexes = (1, 14, 15, 17);

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
                            var address = worksheet.Cells[row, col];

                            if (address.Value != null)
                            {
                                switch (address.Value.ToString().Trim())
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
                            var coming = Convert.ToInt32(worksheet.Cells[row, columnIndexes.comingIndex].Value);
                            articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.Coming, coming);

                            var finalBalance = Convert.ToInt32(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                            articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance);
                        }
                    }

                    // Get finalBalance++
                    for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
                    {
                        var article = Convert.ToInt32(worksheet.Cells[row, columnIndexes.articleIndex].Value);

                        if (articleDict.ContainsKey(article))
                        {
                            var finalBalance = Convert.ToInt32(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                            articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance);
                        }
                    }
                }

                package.Dispose();
            }
        }

        static void WriteXls(string ouptutFilePath, string workSheetName = "Report")
        {
            const int HEADER_TABLE_ROW_HEIGHT = 20;
            const int DEFAULT_TABLE_ROW_HEIGHT = 14;

            using var excel = new ExcelPackage(ouptutFilePath);
            var workSheet = excel.Workbook.Worksheets.Add(workSheetName);

            // Stilized workSheet
            workSheet.TabColor = System.Drawing.Color.YellowGreen;
            workSheet.DefaultRowHeight = DEFAULT_TABLE_ROW_HEIGHT;


            ResultCell[] resultCells = new ResultCell[]
            {
                new ResultCell("Артикул", "0"),
                new ResultCell("Name", "0"),
                new ResultCell("Всего", "0"),
                new ResultCell("Отмен", "0"),
                new ResultCell("В пути", "0"),
                new ResultCell("Доставлен на ПВЗ", "0"),
                new ResultCell("Забран с ПВЗ", "0"),
                new ResultCell("Дата последнего выкупа", "0"),
                new ResultCell("Склад: Ожидание", "0"),
                new ResultCell("Склад: Приход", "0"),
                new ResultCell("Склад: Расход", "0"),
                new ResultCell("Склад: Остаток", "0"),
            };

            // Fill header data 
            for (int i = 0; i < resultCells.Length; i++)
            {
                workSheet.Cells[1, i + 1].Value = resultCells[i].Name;
                workSheet.Column(i + 1).AutoFit();
                workSheet.Column(i + 1).Style.Numberformat.Format = resultCells[i].Format;
            }

            // Stilized workSheet header
            workSheet.Row(1).Height = HEADER_TABLE_ROW_HEIGHT;
            workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Row(1).Style.Font.Bold = true;


            // Populating with data

            int recordIndex = 2;

            foreach (var art in articleDict)
            {
                var canceledCount = art.Value.Items.Where(i => i.ItemStatus == RedemptionStatus.Canceled).Count();
                var onWayCount = art.Value.Items.Where(i => i.ItemStatus == RedemptionStatus.OnWay).Count();
                var deliveredCount = art.Value.Items.Where(i => i.ItemStatus == RedemptionStatus.Delivered).Count();
                var withDrawnCount = art.Value.Items.Where(i => i.ItemStatus == RedemptionStatus.WithDrawn).Count();
                var maxDate = art.Value.Items.MaxBy(i => i.RedemptionDate).RedemptionDate;
                art.Value.SetMaxRedemptionDate(maxDate);

                var waitBalance = deliveredCount + withDrawnCount;
                art.Value.Balance.ChangeBalance(ItemBalanceUnit.WaitBalance, deliveredCount + withDrawnCount);

                art.Value.Analize();

                // Redemptions data
                workSheet.Cells[recordIndex, 1].Value = art.Key;
                workSheet.Cells[recordIndex, 2].Value = art.Value.Items.Count;
                workSheet.Cells[recordIndex, 3].Value = canceledCount;
                workSheet.Cells[recordIndex, 4].Value = onWayCount;
                workSheet.Cells[recordIndex, 5].Value = deliveredCount;
                workSheet.Cells[recordIndex, 6].Value = withDrawnCount;
                workSheet.Cells[recordIndex, 7].Value = maxDate;

                workSheet.Cells[recordIndex, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                workSheet.Cells[recordIndex, 7].Style.Fill.BackgroundColor.SetColor(art.Value.SetStatusColor);

                // Balance data
                workSheet.Cells[recordIndex, 8].Value = art.Value.Balance.WaitBalance;
                workSheet.Cells[recordIndex, 9].Value = art.Value.Balance.Coming;
                workSheet.Cells[recordIndex, 10].Value = art.Value.Balance.Consumption;
                workSheet.Cells[recordIndex, 11].Value = art.Value.Balance.FinBalance;

                recordIndex++;
            }

            // Stilized table - fit && data format
            for (int i = 1; i < 12; i++)
            {
                workSheet.Column(i).AutoFit();
                workSheet.Column(i).Style.Numberformat.Format = i != 7 ? "0" : "dd.MM.yyyy";
                workSheet.Column(i).Style.WrapText = true;
            }

            if (File.Exists(outputPath)) File.Delete(outputPath);

            // Create excel file on physical disk 
            var objFileStrm = File.Create(outputPath);
            objFileStrm.Close();

            // Write content to excel file 
            File.WriteAllBytes(outputPath, excel.GetAsByteArray());

            //Close Excel package
            excel.Dispose();
        }
    }

    internal static bool? CheckFileExistence(string path)
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

    internal static bool? CheckSheets(in ExcelPackage package)
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

    internal static (int col, int row) FindNumberColsAndRowsIn(in ExcelWorksheet sheet)
    {
        return (sheet.Dimension.End.Column, sheet.Dimension.End.Row);
    }
}
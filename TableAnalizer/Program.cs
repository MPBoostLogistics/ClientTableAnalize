// https://suresh-kamrushi.medium.com/c-reading-excel-file-using-epplus-package-95cfb0ed98c5

using System.Globalization;
using System.Numerics;
using OfficeOpenXml;
using OfficeOpenXml.Style;

internal enum SetStatus
{
    FullSet,    // Полный расчет: Ожидание = Приходу = Расходу + Остаток 0
    InWork,     // В работе: Ожидание != Приходу != Расходу + Остаток не 0 и дата последнего выкупа + сегодня < 14
    OnSearch    // На поиске: Ожидание != Приходу != Расходу + Остаток не 0 и дата последнего выкупа + сегодня > 14
}

internal enum RedemptionStatus
{
    Delivered,  // Доставлен на ПВЗ
    WithDrawn,  // Забран с ПВЗ
    Canceled,   // Отменен 
    OnWay,      // В пути
    Unknown     // Неизвестный
}

internal enum ItemBalanceUnit
{
    WaitBalance, InitBalance, Coming, Consumption, FinBalance
}

internal class ItemBalance
{
    public int WaitBalance { get; private set; } = 0;    // -
    public int InitBalance { get; private set; } = 0;    // 14
    public int Coming { get; private set; } = 0;         // 15
    public int Consumption { get; private set; } = 0;    // 16
    public int FinBalance { get; private set; } = 0;     // 17

    public void ChangeBalance(ItemBalanceUnit unit, int value)
    {
        switch (unit)
        {
            case ItemBalanceUnit.WaitBalance:
                WaitBalance = value;
                break;
            case ItemBalanceUnit.InitBalance:
                InitBalance += value;
                break;
            case ItemBalanceUnit.Coming:
                Coming += value;
                break;
            case ItemBalanceUnit.Consumption:
                Consumption += value;
                break;
            case ItemBalanceUnit.FinBalance:
                FinBalance += value;
                break;
            default:
                break;
        }
    }

    private void GetWaitBalance(int delivered, int withDrawn)
    {
        WaitBalance = delivered + withDrawn;
    }
}

internal class Item
{
    private const string DATE_TIME_FORMAT = "dd.MM.yyyy HH:mm";

    public DateTime RedemptionDate { get;  }            // 8
    public RedemptionStatus ItemStatus { get; }         // 10

    public Item(string redemptionDate, string itemStatus)
    {
        this.RedemptionDate = DateTime.ParseExact(redemptionDate, DATE_TIME_FORMAT, CultureInfo.GetCultureInfo("ru-RU"));
        this.ItemStatus = GetStatus(itemStatus);
    }

    private static RedemptionStatus GetStatus(string statusText)
    {
        return statusText switch
        {
            "Доставлен на ПВЗ" => RedemptionStatus.Delivered,
            "Забран с ПВЗ" => RedemptionStatus.WithDrawn,
            "Отменен" => RedemptionStatus.Canceled,
            "В пути" => RedemptionStatus.OnWay,
            _ => RedemptionStatus.Unknown
        };
    }


    public override string ToString()
    {
        return $"{RedemptionDate}, {ItemStatus}";
    }
}

internal class Article
{
    private const int DEADLINE_DAYS = 14;

    public List<Item> Items { get; private set; }
    public ItemBalance Balance { get; private set; }
    public DateTime MaxRedemptionDate { get; private set; }
    public bool IsDeadlineHavePassed { get; private set; }
    public SetStatus Status { get; private set; }

    public System.Drawing.Color SetStatusColor { get; set; }

    public Article(Item item)
    {
        Items = new List<Item> { item };
        Balance = new ItemBalance();
    }

    public void SetMaxRedemptionDate(DateTime date)
    {
        MaxRedemptionDate = date;
        this.IsDeadlineHavePassed = (this.MaxRedemptionDate - DateTime.Now).Duration().Days > DEADLINE_DAYS;
    }

    public void ChangeBalance(ItemBalanceUnit unit, int value)
    {
        this.Balance.ChangeBalance(unit, value);
    }

    public void Analize()
    {
        if (Balance.WaitBalance == Balance.Coming && Balance.Coming  == Balance.Consumption && Balance.FinBalance == 0)
        {
            Status = SetStatus.FullSet;
        } else if (Balance.WaitBalance != Balance.Coming && Balance.Coming != Balance.Consumption && Balance.FinBalance != 0 && !IsDeadlineHavePassed) {
            Status = SetStatus.InWork;
        } else if (Balance.WaitBalance != Balance.Coming && Balance.Coming != Balance.Consumption && Balance.FinBalance != 0 && IsDeadlineHavePassed)
        {
            Status = SetStatus.OnSearch;
        }

        switch (Status)
        {
            case SetStatus.FullSet:
                this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#d4efdf");
                break;
            case SetStatus.InWork:
                this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#fdebd0");
                break;
            case SetStatus.OnSearch:
                this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#fadbd8");
                break;
            default:
                break;
        }
    }
}

internal class Program
{
    #region Variables and constants

    private const string LOGISTIC_FLAG = "Да";
    private const string NAME_FIRST_ROOM = "1 - Помещение приемки, сортировки и упаковки";
    private const string NAME_SECOND_ROOM = "2 - Помещение длительного хранения и отгрузки";
    private const string NAME_FINAL_ROW = "Итого товаров в единицах хранения";

    private const string pathRedemptionReport = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/Report.xlsx";
    private const string storageReportPath = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/StorageReport.xlsx";
    private const string outputPath = "/Users/dmitrijsamarcev/Documents/Clients/Kirill/ClientReport.xlsx";

    private static readonly Dictionary<int, Article> articleDict = new();

    private static readonly Dictionary<SetStatus, System.Drawing.Color> SetStatusColors = new()
    {
        { SetStatus.FullSet,System.Drawing.ColorTranslator.FromHtml("#d4efdf") },
        { SetStatus.InWork,System.Drawing.ColorTranslator.FromHtml("#fdebd0") },
        { SetStatus.OnSearch,System.Drawing.ColorTranslator.FromHtml("#fadbd8") }
    };

    #endregion

    private static void Main(string[] args)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        ReadRedemptionReportFile(pathRedemptionReport);

        ReadStorageReportFile(storageReportPath);

        WriteXls(outputPath);

        static void ReadRedemptionReportFile(string filePath)
        {
            var existingFile = new FileInfo(filePath);
            using ExcelPackage package = new(existingFile);

            //get the first worksheet in workbook
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            int colCount = worksheet.Dimension.End.Column;              //get Column Count
            int rowCount = worksheet.Dimension.End.Row;                 //get row count
            for (int row = 1; row <= rowCount; row++)
            {
                var logisticFlagValue = worksheet.Cells[row, 11].Value.ToString();

                if(logisticFlagValue != null)
                {
                    var logisticFlag = logisticFlagValue.Replace(" ", "");

                    if (logisticFlag == LOGISTIC_FLAG)
                    {
                        var article = Convert.ToInt32(worksheet.Cells[row, 2].Value);
                        var redemptionDateString = worksheet.Cells[row, 8].Value.ToString();
                        var itemStatusValue = worksheet.Cells[row, 10].Value.ToString();

                        if (redemptionDateString != null && itemStatusValue != null)
                        {
                            var item = new Item(redemptionDateString, itemStatusValue.Trim());

                            if (articleDict.ContainsKey(article))
                            {
                                articleDict[article].Items.Add(item);
                            } else
                            {
                                articleDict[article] = new Article(item);
                            }
                        }
                        else
                        {
                            Console.WriteLine("fail");
                        }
                    }
                }

                //for (int col = 1; col <= colCount; col++)
                //{
                //    Console.WriteLine(" Row:" + row + " column:" + col + " Value:" + worksheet.Cells[row, col].Value.ToString().Trim());
                //}
            }
        }

        static void ReadStorageReportFile(string filePath)
        {
            var existingFile = new FileInfo(filePath);
            using ExcelPackage package = new(existingFile);

            //get the first worksheet in workbook
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            int colCount = 17; // worksheet.Dimension.End.Column;              //get col count
            int rowCount = worksheet.Dimension.End.Row;                        //get row count

            // get address of header cells
            int rowFirstRoomAddress = default;
            int rowSecondRoomAddress = default;
            int rowFinalRoomAddress = default;

            for (int row = 1; row <= rowCount; row++)
            {
                for (int col = 1; col <= colCount; col++)
                {
                    var value = worksheet.Cells[row, col].Value;

                    if (value != null)
                    {
                        if (value.ToString().Trim() == NAME_FIRST_ROOM)
                            rowFirstRoomAddress = row;

                        if (value.ToString().Trim() == NAME_SECOND_ROOM)
                            rowSecondRoomAddress = row;

                        if (value.ToString().Trim() == NAME_FINAL_ROW)
                            rowFinalRoomAddress = row;
                    }
                }
            }

            // make row ranges 
            var firstRoomRowRange = Enumerable.Range(rowFirstRoomAddress + 1, rowSecondRoomAddress - 1).ToList();
            var secondRoomRowRange = Enumerable.Range(rowSecondRoomAddress + 1, rowFinalRoomAddress).ToList();

            (int articleIndex, int initBalanceIndex, int comingIndex, int consumptionIndex, int finBalanceIndex) columnIndexes = (1,14,15,16,17);

            // Get coming && finalBalance++
            for (int row = firstRoomRowRange.First(); row <= firstRoomRowRange.Count; row++)
            {
                var article = Convert.ToInt32(worksheet.Cells[row, columnIndexes.articleIndex].Value);

                if(articleDict.ContainsKey(article))
                {
                    var coming = Convert.ToInt32(worksheet.Cells[row, columnIndexes.comingIndex].Value);
                    articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.Coming, coming);

                    var finalBalance = Convert.ToInt32(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                    articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance);
                }
            }

            // Get consumption && finalBalance++
            for (int row = secondRoomRowRange.First(); row < rowFinalRoomAddress - 2; row++)
            { 
                var article = Convert.ToInt32(worksheet.Cells[row, columnIndexes.articleIndex].Value);

                if (articleDict.ContainsKey(article))
                {
                    var consumption = Convert.ToInt32(worksheet.Cells[row, columnIndexes.consumptionIndex].Value);
                    articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.Consumption, consumption);

                    var finalBalance = Convert.ToInt32(worksheet.Cells[row, columnIndexes.finBalanceIndex].Value);
                    articleDict[article].Balance.ChangeBalance(ItemBalanceUnit.FinBalance, finalBalance);
                }
            }

            package.Dispose();
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

            // Stilized workSheet header
            workSheet.Row(1).Height = HEADER_TABLE_ROW_HEIGHT;
            workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Row(1).Style.Font.Bold = true;

            workSheet.Cells[1, 1].Value = "Артикул";
            workSheet.Cells[1, 2].Value = "Всего";
            workSheet.Cells[1, 3].Value = "Отменены";
            workSheet.Cells[1, 4].Value = "В пути";
            workSheet.Cells[1, 5].Value = "Доставлен на ПВЗ";
            workSheet.Cells[1, 6].Value = "Забран с ПВЗ";
            workSheet.Cells[1, 7].Value = "Дата последнего выкупа";
            workSheet.Cells[1, 8].Value = "Склад: Ожидание";
            workSheet.Cells[1, 9].Value = "Склад: Приход";
            workSheet.Cells[1, 10].Value = "Склад: Расход";
            workSheet.Cells[1, 11].Value = "Склад: Остаток";

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
}
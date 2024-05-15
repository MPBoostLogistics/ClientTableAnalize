#nullable enable

using System.Drawing;
using System.Formats.Tar;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Interfaces;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.ExcelUtilities;
using OfficeOpenXml.Style;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ExcelManager : BaseManager
    {
        #region Constants && variables
        public const string FIRST_ROOM_NAME = "1 - Помещение приемки, сортировки и упаковки";
        public const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения и отгрузки";
        public const string LAST_MARKER_KEY = "Итого товаров в единицах хранения";
        public const int LAST_MARKER_KEY_OFFSET = 2;
        public const string OUTPUT_REPORT_WORKSHEET01_NAME = "Отчет";
        public const string RANSOMS_WORKSHEET_NAME = "Выкупы";
        public const string REMAINING_WORKSHEET_NAME = "TDSheet";
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;
        private const string LOGISTIC_FLAG = "Да";
        #endregion

        #region Properties
        private static Dictionary<SourceSKUReportFileKey, ExcelPackage> ExcelPackages { get; } = new();
        private static Type TypeString { get; } = typeof(string);
        private static Type TypeInt { get; } = typeof(int);
        private static Type TypeDouble { get; } = typeof(double);
        private static Type TypeDateOnly { get; } = typeof(DateOnly);
        private static Type TypeSKURansomsStatus { get; } = typeof(SKURansomsStatus);
        private static Type TypeMarketplace { get; } = typeof(Marketplace);
        private static Type TypeSellmentStatus { get; } = typeof(SellmentStatus);

        public static Dictionary<ParsingTypeCode, Type> TypeMappingTable { get; } = new()
        {
            { ParsingTypeCode.StringTypeCode, TypeString },
            { ParsingTypeCode.IntTypeCode, TypeInt },
            { ParsingTypeCode.DoubleTypeCode, TypeDouble },
            { ParsingTypeCode.DataOnlyTypeCode, TypeDateOnly },
            { ParsingTypeCode.SKURansomsStatus, TypeSKURansomsStatus },
            { ParsingTypeCode.Marketplace, TypeMarketplace },
            { ParsingTypeCode.SellmentStatus, TypeSellmentStatus }
        };

        /// <summary>
        /// Collection of descriptions of source table cells (client ransoms).
        /// </summary>
        public static List<SourceCell> RansomsSourceCells { get; } = new()
        {
            new(2, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(7, "SKURansomDate", ParsingTypeCode.DataOnlyTypeCode,true),
            new(9, "SKURansomsStatus", ParsingTypeCode.SKURansomsStatus, true),
            new(10, "Логистика", ParsingTypeCode.StringTypeCode, true, LOGISTIC_FLAG),
            new(14, "SKUCost",  ParsingTypeCode.DoubleTypeCode, true)
        };

        public static List<SourceCell> RemainingSourceCells01 { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "SKUBrandAndName", ParsingTypeCode.StringTypeCode, true),
            new(15, "SKUReceipt", ParsingTypeCode.IntTypeCode, false),
            new(17, "SKURemaining", ParsingTypeCode.IntTypeCode, false)
        };

        public static List<SourceCell> RemainingSourceCells02 { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "SKUBrandAndName", ParsingTypeCode.StringTypeCode, true),
            new(17, "SKURemaining", ParsingTypeCode.IntTypeCode, false)
        };

        public static List<SourceCell> ShippedSourceCells { get; } = new()
        {
            new(4, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(28, "SKUShippedCount", ParsingTypeCode.IntTypeCode, true)
        };

        public static TargetCell[] MainReportSKUInfoCells { get; } = new TargetCell[]
        {
            new (colNumber: 1, colName: "МП", colWidth: 8, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUMp",
                parsingTypeCode: ParsingTypeCode.Marketplace, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 2, colName: "Артикул", colWidth: 12, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUNumber",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 3, colName: "Бренд товара", colWidth: 10, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUBrand",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 4, colName: "Название товара", colWidth: 50, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUName",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 5, colName: "Средняя цена, руб.", colWidth: 12, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, "SKUAveragePrice",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 6, colName: "Дата последнего выкупа", colWidth: 13, format: "dd.MM.yyyy",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUMaxRansomsDate",
                parsingTypeCode: ParsingTypeCode.DataOnlyTypeCode, excelFillStyle:  ExcelFillStyle.Solid),
            new (colNumber: 7, colName: "Статус взаиморасчета", colWidth: 12, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUSellmentStatus",
                parsingTypeCode: ParsingTypeCode.SellmentStatus, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 8, "Всего заказов", colWidth: 12, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKURansomsTotal",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 9, "Отменено",colWidth: 7, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 90, propertyName: "SKURansomsCanceled",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 10, "В пути", colWidth: 7, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 90, propertyName: "SKURansomsOnWay",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 11, "Доставлено на ПВЗ", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsDelivered",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 12, "Забрано с ПВЗ", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsWithDrawn",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None),
            new (colNumber: 13, "Склад: Ожидание", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsWaiting",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, isBoldFont: true)                                             

            // Info on arrival of goods at warehouse
            // new (12, "Склад: Ожидание", 11.5,"0", true, 0),
            // new (13, "Склад: Приход", 11 ,"0",true, 0),
            // new (14, "Склад: Недовоз от ПВЗ",11, "0", true, 0),          

            // // Info on shipment of goods from warehouse
            // new (15, "Склад: Расход всего",12, "0", true, 0),
            // new (16, "Склад: Расход товаром",7, "0", true, 90),
            // new (17, "Склад: Расход пересортом",7, "0", true, 90),
            // new (18, "Склад: Расход компенсацией",7, "0", true, 90),
            // new (19, "Склад: Учтено 'пустышками'",7, "0", true, 90),
            // new (20, "Склад: Остаток", 9, "0", true, 0),

            // new (21, "Комментарий", 60, "0", true, 0)
        };
        #endregion

        #region Constructors && destructors
        public ExcelManager() { }

        ~ExcelManager()
        {
            foreach (var excelPackage in ExcelPackages)
            {
                excelPackage.Value?.Dispose();
            }

            ExcelPackages.Clear();
        }

        #endregion

        #region Functionality

        // <summary>
        // Creates an Excel workbook using specified FileInfo.
        // </summary>
        // <param name="key">Specified key.</param>
        // <param name="fileInfo">Specified FileInfo.</param>
        // <returns>Existing ExcelPackage (optional).</returns>
        // <remarks><i>New version, tested</i></remarks>
        public static ExcelPackage? CreateExcelPackage(SourceSKUReportFileKey key, FileInfo fileInfo)
        {
            if (!GetExcelPackageByKey(key, out ExcelPackage? package) && package is null)
            {
                ExcelPackages.Add(key, new ExcelPackage(fileInfo));
            }

            GetExcelPackageByKey(key, out ExcelPackage? existingPackage);
            return existingPackage;
        }

        /// <summary>
        /// Returns ExcelPackage from internal dictionary by key.
        /// </summary>
        /// <param name="key">ExcelPackage search key.</param>
        /// <param name="package">ExcelPackage or null.</param>
        /// <returns>Boolean flag of operation.</returns>
        // <remarks><i>New version, tested</i></remarks>
        public static bool GetExcelPackageByKey(SourceSKUReportFileKey key, out ExcelPackage? package)
        {
            ExcelPackages.TryGetValue(key, out package);
            return package is not null;
        }

        public void FillingСellsWithData<T>(ExcelPackage package, ref ExcelWorksheet worksheet, in List<T> items, TargetCell[] targetCells, string outputPath, int recordIndex = 1)
            where T : IColorable
        {
            int rowIndex = recordIndex + 1;

            foreach (var item in items)
            {
                foreach (var targetCell in targetCells)
                {
                    object? value = null;

                    switch (targetCell.ParsingTypeCode)
                    {
                        case ParsingTypeCode.IntTypeCode:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out int intValue))
                            {
                                value = intValue;
                            }
                            break;
                        case ParsingTypeCode.DoubleTypeCode:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out double doubleValue))
                            {
                                value = doubleValue;
                            }
                            break;
                        case ParsingTypeCode.DataOnlyTypeCode:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out DateOnly dateOnlyValue))
                            {
                                value = dateOnlyValue.ToDateTime(new TimeOnly());
                            }
                            break;
                        case ParsingTypeCode.StringTypeCode:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out string? stringValue))
                            {
                                value = stringValue ?? null;
                            }
                            break;
                        case ParsingTypeCode.SKURansomsStatus:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out SKURansomsStatus ransomsStatusValue))
                            {
                                value = ransomsStatusValue;
                            }
                            break;
                        case ParsingTypeCode.Marketplace:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out Marketplace marketplaceValue))
                            {
                                value = marketplaceValue;
                            }
                            break;
                        case ParsingTypeCode.SellmentStatus:
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out SellmentStatus? sellmentStatus))
                            {
                                value = sellmentStatus;
                            }
                            break;
                        default:
                            break;
                    }

                    if (value is not null)
                    {
                        // Set value for cell 
                        var cell = worksheet.Cells[rowIndex, targetCell.ColNumber];
                        cell.Value = value;

                        // Cell value format
                        cell.Style.Numberformat.Format = targetCell.Format;

                        // Filling cells with color   
                        cell.Style.Fill.PatternType = targetCell.ExcelFillStyle;
                        if (cell.Style.Fill.PatternType == ExcelFillStyle.Solid &&
                            item.FillColor != Color.Empty)
                        {
                            cell.Style.Fill.BackgroundColor.SetColor(item.FillColor);
                        }

                        // Bold font for cell
                        cell.Style.Font.Bold = targetCell.IsBoldFont;
                    }
                }
                rowIndex++;
            }

            SaveReceivedData(outputPath, ref package);
        }

        /// <summary>
        /// Saves received data to an Excel workbook file.
        /// </summary>
        /// <param name="path">Directory to save file.</param>
        public static void SaveReceivedData(string path, ref ExcelPackage package)
        {
            FileStream? fileStream = File.Create(path);
            fileStream.Close();

            File.WriteAllBytes(path, package.GetAsByteArray());
        }

        // public double AnalizeReport<T>(SourceSKUReportFileKey reportFileKey, string workSheetName, out List<T>? items, bool isRequred = true)
        // {
        //     var stopwatch = new Stopwatch();

        //     double StopMethod()
        //     {
        //         stopwatch.Stop();
        //         return stopwatch.ElapsedMilliseconds / 1000;
        //     }

        //     stopwatch.Start();

        //     // Get report file 
        //     SourceReportFileForSKU? sourceReportFile = GetReportFile(reportFileKey, isRequred);
        //     if (sourceReportFile == null && !isRequred)
        //     {
        //         items = null;
        //         return StopMethod();
        //     }

        //     items = new List<T>();

        //     using ExcelPackage package = new(sourceReportFile!.FileInfo);

        //     var worksheet = package.Workbook.Worksheets.FirstOrDefault(sh => sh.Name == workSheetName) ??
        //         throw new Exception($"В книге Excel нет листа с именем '{workSheetName}'.");

        //     // пройти по массиву диапазонов

        //     package.Dispose();


        //     items = null; // Dummy
        //     return StopMethod();
        // }


        // public static (double time, List<SKURowInReport> articleRows) AnalyzeRedemptionReport(string path, string sheetName,
        //     (int articleIndex, int dateIndex, int statusIndex, int logisticFlagIndex, int priceIndex) indices)
        // {
        //     List<SKURowInReport> articleRows = new();
        //     var stopwatch = new Stopwatch();
        //     stopwatch.Start();

        //     if (IsDataPathExists(path, out FileInfo fileInfo))
        //     {
        //         using ExcelPackage package = new(fileInfo);

        //         var worksheet = package.Workbook.Worksheets.FirstOrDefault(sh => sh.Name == sheetName) ??
        //             throw new Exception($"In found workbook there is no sheet with name '{sheetName}'.");

        //         var dimention = worksheet.Dimension;
        //         var endRowIndex = dimention.End.Row;


        //         // Идем по диапазонам 
        //         for (int row = 1; row <= dimention.End.Row; row++)
        //         {
        //             var logisticFlagValue = worksheet.Cells[row, indices.logisticFlagIndex].Value.ToString();

        //             if (!string.IsNullOrEmpty(logisticFlagValue))
        //             {
        //                 var logisticFlag = logisticFlagValue.Replace(" ", "");

        //                 if (logisticFlag == LOGISTIC_FLAG)
        //                 {
        //                     var articleNumber = ParseValueToInt(worksheet.Cells[row, indices.articleIndex].Value);

        //                     if (!articleNumber.HasValue) continue;

        //                     var redemptionDateString = worksheet.Cells[row, indices.dateIndex].Value.ToString();
        //                     var goodStatusString = worksheet.Cells[row, indices.statusIndex].Value.ToString();
        //                     var priceSting = worksheet.Cells[row, indices.priceIndex].Value.ToString();

        //                     if (!string.IsNullOrEmpty(redemptionDateString) &&
        //                         !string.IsNullOrEmpty(goodStatusString) &&
        //                         !string.IsNullOrEmpty(priceSting))
        //                     {
        //                         var articleRow = new SKURowInReport(articleNumber.Value, redemptionDateString.Trim(), goodStatusString.Trim(), priceSting.Trim());
        //                         articleRows.Add(articleRow);
        //                     }
        //                 }
        //             }
        //         }
        //     }

        //     stopwatch.Stop();
        //     return (stopwatch.ElapsedMilliseconds / 1000, articleRows);
        // }

        // public static string CreateMainReportSheet(string workSheetName, in Dictionary<int, SKU> articles)
        // {
        //     //if(IsDataPathExists(OutputReportFilePath, out FileInfo fileInfo)) 
        //     //{
        //     using ExcelPackage excelPackage = new(new FileInfo(OutputReportFilePath));

        //     // Create ExcelWorksheet  
        //     CreateWorkSheet(workSheetName, in excelPackage, out ExcelWorksheet mainReportWorksheet, System.Drawing.Color.GreenYellow);

        //     // Filling table with data
        //     int recordIndex = 1;

        //     foreach (var (key, value) in articles)
        //     {
        //         // Fill Redemptions data
        //         mainReportWorksheet.Cells[recordIndex, 1].Value = value.Marketplace;
        //         mainReportWorksheet.Cells[recordIndex, 2].Value = key;
        //         mainReportWorksheet.Cells[recordIndex, 3].Value = value.Name;
        //         mainReportWorksheet.Cells[recordIndex, 4].Value = value.AveragePrice;

        //         // RedemptionDate
        //         mainReportWorksheet.Cells[recordIndex, 5].Value = value.MaxRedemptionDate;
        //         mainReportWorksheet.Cells[recordIndex, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
        //         mainReportWorksheet.Cells[recordIndex, 5].Style.Fill.BackgroundColor.SetColor(value.SellmentStatus.StatusColor);

        //         mainReportWorksheet.Cells[recordIndex, 6].Value = value.SellmentStatus.StatusLabel;

        //         // Redemtion balance data 
        //         // var arb = value.GetArticleBalancesReport();
        //         // if (arb != null)
        //         // {
        //         //     mainReportWorksheet.Cells[recordIndex, 7].Value = arb.RedemtionSum;
        //         //     mainReportWorksheet.Cells[recordIndex, 8].Value = arb.RedemtionCanceled;
        //         //     mainReportWorksheet.Cells[recordIndex, 9].Value = arb.RedemtionOnWay;
        //         //     mainReportWorksheet.Cells[recordIndex, 10].Value = arb.RedemtionDelivered;
        //         //     mainReportWorksheet.Cells[recordIndex, 11].Value = arb.RedemtionWithDrawn;
        //         // }

        //         // // Storage balance data
        //         // var articleStoreBalance = value.ArticleStorageBalance;
        //         // var asb = articleStoreBalance.AnalizeBalance();

        //         // if (asb.HasValue)
        //         // {
        //         //     //Console.WriteLine($"asb: {asb.Value.expected} {asb.Value.received} {asb.Value.undelivery} {asb.Value.shipped} {asb.Value.remaining}");

        //         //     mainReportWorksheet.Cells[recordIndex, 12].Value = asb.Value.expected;
        //         //     mainReportWorksheet.Cells[recordIndex, 12].Style.Font.Bold = true;

        //         //     mainReportWorksheet.Cells[recordIndex, 13].Value = asb.Value.received;

        //         //     var undelivery = asb.Value.undelivery;
        //         //     mainReportWorksheet.Cells[recordIndex, 14].Value = undelivery;
        //         //     if(undelivery > 0) 
        //         //         mainReportWorksheet.Cells[recordIndex, 14].Style.Font.Color.SetColor(DangerColor); 

        //         //     mainReportWorksheet.Cells[recordIndex, 16].Value = asb.Value.shipped;
        //         //     mainReportWorksheet.Cells[recordIndex, 20].Value = asb.Value.remaining;
        //         // }

        //         //     // Consumption
        //         //     mainReportWorksheet.Cells[recordIndex, 15].Value = articleDict[key].StoreBalance.TotalConsumption;

        //         //     mainReportWorksheet.Cells[recordIndex, 17].Value = articleDict[key].StoreBalance.Regradings.Sum(re => re.Value);
        //         //     mainReportWorksheet.Cells[recordIndex, 18].Value = articleDict[key].StoreBalance.PayShortagesCount;
        //         //     mainReportWorksheet.Cells[recordIndex, 19].Value = articleDict[key].StoreBalance.DummiesCount;

        //         //     // Comment
        //         //     mainReportWorksheet.Cells[recordIndex, 21].Value = articleDict[key].Comment;

        //         recordIndex++;
        //     }

        //     // Setup header 
        //     SetupTableHeader(MainReportCells, ref mainReportWorksheet);
        //     AddFilterInWorkSheet(ref mainReportWorksheet);

        //     //Freeze row && columns
        //     mainReportWorksheet.View.FreezePanes(2, 4);

        //     // HACK: Переделать !!!
        //     // Group column
        //     for (var i = 8; i <= 11; i++)
        //     {
        //         mainReportWorksheet.Column(i).OutlineLevel = 1;
        //         mainReportWorksheet.Column(i).Collapsed = true;
        //     }

        //     for (var i = 16; i <= 19; i++)
        //     {
        //         mainReportWorksheet.Column(i).OutlineLevel = 1;
        //         mainReportWorksheet.Column(i).Collapsed = true;
        //     }

        //     // Save work (temp)
        //     if (File.Exists(OutputReportFilePath)) File.Delete(OutputReportFilePath);

        //     FileStream? objFileStrm = File.Create(OutputReportFilePath);
        //     objFileStrm.Close();

        //     File.WriteAllBytes(OutputReportFilePath, excelPackage.GetAsByteArray());

        //     return OutputReportFilePath;
        // }

        /// <summary>
        /// Creates an Excel worksheet in specified Excel workbook.
        /// </summary>
        /// <param name="workSheetName">Excel worksheet name.</param>
        /// <param name="excelPackage">Excel workbook name.</param>
        /// <param name="worksheet">Excel worksheet that is created.</param>
        /// <param name="tabColor">Worksheet label (tab) color.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public static void CreateExcelWorkSheet(string workSheetName, in ExcelPackage excelPackage, out ExcelWorksheet worksheet, System.Drawing.Color tabColor)
        {
            // Remove existing worksheet
            var existingWorksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(x => x.Name == workSheetName);
            if (existingWorksheet is not null) excelPackage.Workbook.Worksheets.Delete(existingWorksheet);

            // Create net worksheet
            worksheet = excelPackage.Workbook.Worksheets.Add(workSheetName);

            // Stilized workSheet
            worksheet.TabColor = tabColor;
            worksheet.DefaultRowHeight = DEFAULT_TABLE_ROW_HEIGHT;
            worksheet.DefaultColWidth = DEFAULT_TABLE_COL_WIDTH;
        }

        private static void SetupTableHeader(in TargetCell[] resultCells, ref ExcelWorksheet worksheet, double headerRowHeight = HEADER_TABLE_ROW_HEIGHT)
        {
            var firstRow = worksheet.Row(RECORD_INDEX);
            if (firstRow is not null)
            {
                firstRow.Height = headerRowHeight;
                firstRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                firstRow.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                firstRow.Style.Font.Bold = true;
                firstRow.Style.Font.Size = 14;
            }

            for (int i = 0; i < resultCells.Length; i++)
            {
                var currentResultCell = resultCells[i];
                var columnNumber = currentResultCell.ColNumber;

                ExcelColumn columnInWorkSheet = worksheet.Column(columnNumber) ??
                    throw new Exception($"Column number {columnNumber} not found on worksheet");

                ExcelRange firstRowCell = worksheet.Cells[RECORD_INDEX, columnNumber] ?? throw new Exception($"Cell at address {RECORD_INDEX} {columnNumber} not found on worksheet");

                // Add name of column
                firstRowCell.Value = currentResultCell.ColName;

                // Rotation text in header 
                firstRowCell.Style.TextRotation = currentResultCell.TextRotationInHeader;
                columnInWorkSheet.Width = currentResultCell.ColumnWidth;
                columnInWorkSheet.Style.Numberformat.Format = currentResultCell.Format;
                columnInWorkSheet.Style.WrapText = currentResultCell.WrapTextInHeader;
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

        /// <summary>
        /// Returns a Excel worksheet in an Excel workbook by its name.
        /// </summary>
        /// <param name="name">Name of Excel worksheet to search for.</param>
        /// <param name="key">Key for searching ExcelPackage in internal dictionary.</param>
        /// <param name="worksheet">Found Excel worksheet or null.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public static void GetExcelWorksheetByName(string name, SourceSKUReportFileKey key, out ExcelWorksheet? worksheet) =>
             worksheet = (GetExcelPackageByKey(key, out ExcelPackage? package) && package is not null) ?
                package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == name) :
                null;

        /// <summary>
        /// Returns index of row from an Excel workbook by string value (key).
        /// </summary>
        /// <param name="key">String key to search for a row.</param>
        /// <param name="worksheet">Excel worksheet for searching.</param>
        /// <returns>Integer index of row or null.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public static int? GetRowIndexBy(string key, in ExcelWorksheet worksheet) =>
            worksheet.Cells.FirstOrDefault(c => c.Value != null && c.Value.ToString()?.Trim() == key)?.Start.Row;

        /// <summary>
        /// Gets data from row ranges in specified Excel worksheet.
        /// </summary>
        /// <param name="workSheet">Specified Excel worksheet</param>
        /// <param name="reportRowsRanges">List of ranges.</param>
        /// <exception cref="ArgumentException">Exceptions when there is a null range and a null range of rows in it.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public static void GetRawDataFromExcelRows(in ExcelWorksheet workSheet, ref List<RangeSourceReportRows> reportRowsRanges)
        {
            // Iterating Ranges
            for (int i = 0; i < reportRowsRanges.Count; i++)
            {
                RangeSourceReportRows? reportRowsRange = reportRowsRanges[i];

                if (reportRowsRange is null || reportRowsRange.RowsIndexRange is null)
                    throw new ArgumentException("Range for table or row index range cannot be empty");

                // Iterating Rows 
                var rowsIndexRange = reportRowsRange.RowsIndexRange.ToList();
                var currentSourceCells = reportRowsRange.SourceCells;

                for (int j = 0; j < rowsIndexRange.Count; j++)
                {
                    var rowIndex = rowsIndexRange[j];

                    if (GetValuesFromRow(in workSheet, rowIndex, in currentSourceCells, out Dictionary<string, ItemForParsing>? values) &&
                        values is not null)
                    {
                        reportRowsRange.SetValues(values);
                        reportRowsRanges![i] = reportRowsRange;
                    }
                }
            }
        }

        /// <summary>
        /// Gets data from a range row in specified Excel worksheet.
        /// </summary>
        /// <param name="workSheet">Specified Excel worksheet.</param>
        /// <param name="rowIndex">Row index.</param>
        /// <param name="sourceCells">A list with a description of source cells in a row.</param>
        /// <param name="values">Dictionary of retrieved values by row.</param>
        /// <returns>Boolean flag for success of completed operation.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public static bool GetValuesFromRow(in ExcelWorksheet workSheet, int rowIndex, in List<SourceCell> sourceCells, out Dictionary<string, ItemForParsing>? values)
        {
            values = null;

            if (!CheckingPresenceAddressesInRow(in workSheet, rowIndex, in sourceCells) ||
                !СheckingValuesInRequiredCells(in workSheet, rowIndex, in sourceCells) ||
                !FilterMatchingCheck(in workSheet, rowIndex, in sourceCells, out int filterCellsCount) ||
                !FindSorceCellsByType(in sourceCells, SourceCellsType.NonFiltration, out List<SourceCell>? dataСellsNonFilters) ||
                dataСellsNonFilters is null ||
                dataСellsNonFilters!.Count == 0)
            {
                return false;
            }

            // Getting values
            values = new();
            foreach (var dataCell in dataСellsNonFilters)
            {
                var value = workSheet.Cells[rowIndex, dataCell.ColIndex].Value;

                if (value is null)
                {
                    if (dataCell.IsValueRequired)
                        throw new ArgumentException($"A table cell does not contain a <b>required value</b> for name: '{dataCell.Name}'");
                    else
                        value = string.Empty;
                }

                var valueToString = value.ToString();
                if (valueToString is not null)
                    values.Add(dataCell.Name, new(valueToString, dataCell.ParsingTypeCode, !dataCell.IsValueRequired));
            }

            return Equals(values?.Count, sourceCells.Count - filterCellsCount);

        }

        private static bool CheckingPresenceAddressesInRow(in ExcelWorksheet workSheet, int rowIndex, in List<SourceCell> sourceCells)
        {
            foreach (var sourceCell in sourceCells)
            {
                ExcelRange? address = workSheet.Cells[rowIndex, sourceCell.ColIndex];
                if (address is null) return false;

                var addressValue = address.Value;
                if(addressValue is not null && !string.IsNullOrEmpty(addressValue.ToString())) 
                {
                    return ParsingRowEntity.Parse(addressValue.ToString()!, sourceCell.ParsingTypeCode, !sourceCell.IsValueRequired, out object? value) && 
                        value is not null; 
                }
                else 
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FilterMatchingCheck(in ExcelWorksheet workSheet, int rowIndex, in List<SourceCell> sourceCells, out int filterSourceCellsCount)
        {
            filterSourceCellsCount = 0;

            if (!FindSorceCellsByType(in sourceCells, SourceCellsType.Filtration, out List<SourceCell>? filterSourceCells)) 
            {
                return true;
            } 
            else 
            {
                filterSourceCellsCount = filterSourceCells!.Count;
                foreach (var filterSourceCell in filterSourceCells)
                {
                    if (!IsFilterMatch(filterSourceCell.FilterString!, workSheet.Cells[rowIndex, filterSourceCell.ColIndex].Value))
                        return false;
                }
                return true;
            }
        }

        private static bool СheckingValuesInRequiredCells(in ExcelWorksheet workSheet, int rowIndex, in List<SourceCell> sourceCells)
        {
            if(!FindSorceCellsByType(in sourceCells, SourceCellsType.Required, out List<SourceCell>? requiredCells) && 
                requiredCells is null || 
                requiredCells?.Count == 0)
            {
                return true;
            }
            else 
            {
                var requiredCellValues = new List<object>();
                foreach (var sourceCell in requiredCells!)
                {
                    var cell = workSheet.Cells[rowIndex, sourceCell.ColIndex];
                    var value = cell.Value;
                    if (cell is null || value is null)
                    {
                        return false;
                    } 
                    else 
                    {
                        requiredCellValues.Add(value);
                    }
                }

                return requiredCellValues.All(v => v is not null);
            }
        }

        private static bool FindSorceCellsByType(in List<SourceCell> sourceCells, SourceCellsType sourceCellsType, out List<SourceCell>? cells)
        {
            cells = sourceCellsType switch
            {
                SourceCellsType.Required => sourceCells.Where(sc => sc.IsValueRequired).ToList(),
                SourceCellsType.Filtration => sourceCells.Where(sc => !string.IsNullOrEmpty(sc.FilterString)).ToList(),
                SourceCellsType.NonFiltration => sourceCells.Where(sc => string.IsNullOrEmpty(sc.FilterString)).ToList(),
                _ => null,
            };

            return cells is not null && cells.Any();
        }

        /// <summary>
        /// Gets row ranges in an Excel worksheet.
        /// </summary>
        /// <param name="worksheet">Excel worksheet.</param>
        /// <param name="rangeSource">Range prototypes.</param>
        /// <param name="startRangeRowIndex">Start index.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public static void GetRangeSourceReportRows(in ExcelWorksheet worksheet, ref List<RangeSourceReportRows> rangeSource, int startRangeRowIndex = 1)
        {
            Exception missingStringKeyExc = new("Указание строкового ключа для диапазона обязательно");
            Exception missingIntIndexExc = new("Индекс строки по указанному ключу не был найден.");
            Exception terminatingStringExc = new("В последнем диапазоне обязательно использование завершающего маркера.");

            for (int i = 0; i < rangeSource.Count; i++)
            {
                // Если один элемент и у него нет ключа
                if (rangeSource.Count == 1 && rangeSource[i].Key == null)
                {
                    var dim = worksheet.Dimension;
                    rangeSource[i].SetRange(startRangeRowIndex + 1, dim.End.Row - 1);
                }
                // Если не элемент и у низ есть ключи
                else
                {
                    var currentRowStringKey = rangeSource[i].Key ?? throw missingStringKeyExc;
                    int startRowIndex = GetRowIndexBy(currentRowStringKey, in worksheet) ?? throw missingIntIndexExc;
                    int start = startRowIndex + 1;

                    int lastRowIndex;

                    // If this is last element
                    if (i == rangeSource.Count - 1)
                    {
                        var (key, offset) = rangeSource[i].TerminatingStringValue ?? throw terminatingStringExc;
                        lastRowIndex = GetRowIndexBy(key, in worksheet) ?? throw missingIntIndexExc;
                        rangeSource[i].SetRange(start, lastRowIndex - start - offset);
                    }
                    // If this is not last element
                    else
                    {
                        var lastRowStringKey = rangeSource[i + 1].Key
                            ?? throw missingStringKeyExc;

                        lastRowIndex = GetRowIndexBy(lastRowStringKey, in worksheet) ??
                            throw missingIntIndexExc;

                        rangeSource[i].SetRange(start, lastRowIndex - start);
                    }
                }
            }
        }

        private static bool IsFilterMatch(string filter, object value) =>
            value is not null &&
            !string.IsNullOrEmpty(value.ToString()) &&
            string.Equals(filter.Trim().ToLower(), value.ToString()!.Trim().ToLower());

        #endregion
    }
}
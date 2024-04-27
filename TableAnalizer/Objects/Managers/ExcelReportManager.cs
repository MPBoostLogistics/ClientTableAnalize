#nullable enable

using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Parsing;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ExcelReportManager : BaseManager
    {
        #region Constants && variables
        public const string RANSOMS_WORKSHEET_NAME = "Выкупы";
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;
        private const string LOGISTIC_FLAG = "Да";
        #endregion

        #region Properties
        private static System.Drawing.Color DangerColor { get; } = System.Drawing.Color.DarkRed;

        public static List<SourceCell> RansomsSourceCells { get; } = new()
        {
            new(2, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(9, "SKURansomsStatus", ParsingTypeCode.SKURansomsStatus, true),
            new(10, "Логистика", ParsingTypeCode.StringTypeCode, true, "Да"),
            new(14, "SKUCost",  ParsingTypeCode.DoubleTypeCode, true) // 1137 руб.
        };

        private static TargetCell[] MainReportCells { get; } = new TargetCell[]
        {
            // Article info
            new (1, "МП", 8, "0", false, 0),
            new (2, "Артикул", 12, "0", false, 0),
            new (3, "Название", 60, "0", true, 0),
            new (4, "Средняя цена, руб.", 12, "0.00", true, 0),
            new (5, "Дата последнего выкупа", 13, "dd.MM.yyyy", true, 0),
            new (6, "Статус", 12, "0", false, 0),                          

            // Buyout info
            new (7, "Всего, шт.", 12, "0", false, 0),
            new (8, "Отменено, шт.", 7, "0", false, 90),
            new (9, "В пути, шт.", 7, "0", false, 90),
            new (10, "Доставлено на ПВЗ. шт.", 7, "0", true, 90),
            new (11, "Забрано с ПВЗ, шт.", 7, "0", true, 90),                   

            // Info on arrival of goods at warehouse
            new (12, "Склад: Ожидание", 11.5,"0", true, 0),
            new (13, "Склад: Приход", 11 ,"0",true, 0),
            new (14, "Склад: Недовоз от ПВЗ",11, "0", true, 0),          

            // Info on shipment of goods from warehouse
            new (15, "Склад: Расход всего",12, "0", true, 0),
            new (16, "Склад: Расход товаром",7, "0", true, 90),
            new (17, "Склад: Расход пересортом",7, "0", true, 90),
            new (18, "Склад: Расход компенсацией",7, "0", true, 90),
            new (19, "Склад: Учтено 'пустышками'",7, "0", true, 90),
            new (20, "Склад: Остаток", 9, "0", true, 0),

            new (21, "Комментарий", 60, "0", true, 0)
        };
        #endregion

        #region Constructors && destructors
        public ExcelReportManager() { }
        #endregion

        #region Functionality

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

        private static void CreateWorkSheet(string workSheetName, in ExcelPackage excelPackage, out ExcelWorksheet worksheet, System.Drawing.Color tabColor)
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
                var columnNumber = currentResultCell.ColumnNumber;

                ExcelColumn columnInWorkSheet = worksheet.Column(columnNumber) ??
                    throw new Exception($"Column number {columnNumber} not found on worksheet");

                ExcelRange firstRowCell = worksheet.Cells[RECORD_INDEX, columnNumber] ?? throw new Exception($"Cell at address {RECORD_INDEX} {columnNumber} not found on worksheet");

                // Add name of column
                firstRowCell.Value = currentResultCell.Name;

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
        /// <param name="name">Name of Excel Excel worksheet to search for.</param>
        /// <param name="excelPackage">Excel package for specifying Excel workbook.</param>
        /// <param name="worksheet">Found Excel worksheet or null.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public static void GetExcelWorksheetBy(string name, in ExcelPackage excelPackage, out ExcelWorksheet? worksheet) =>
            worksheet = excelPackage.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == name);

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

                    if (GetValuesFromRow(in workSheet, rowIndex, in currentSourceCells, out Dictionary<string, (string value, ParsingTypeCode parsingTypeCode)>? values) &&
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
        public static bool GetValuesFromRow(in ExcelWorksheet workSheet, int rowIndex, in List<SourceCell> sourceCells, out Dictionary<string, (string value, ParsingTypeCode parsingTypeCode)>? values)
        {
            values = new();
            bool isMatchFilters = true;

            var filterCols = sourceCells
                .Where(cell => !string.IsNullOrEmpty(cell.FilterString))
                .Select(cell => (cell.ColIndex, cell.FilterString!.Trim().ToLower()))
                .ToList();

            // Есть фильтрационные колонки 
            if (filterCols.Count > 0)
            {
                foreach (var (colIndex, stringKey) in filterCols)
                {
                    var value = workSheet.Cells[rowIndex, colIndex].Value;

                    if (value is null || string.IsNullOrEmpty(value.ToString()))
                        throw new ArgumentException($"В ячейке {rowIndex}:{colIndex} нет данных для фильтрации");

                    if (!IsFilterMatch(stringKey, value))
                    {
                        isMatchFilters = false;
                        break;
                    }
                }
            }

            if (isMatchFilters)
            {
                var colsForGetValues = sourceCells
                    .Where(cell => string.IsNullOrEmpty(cell.FilterString))
                    .ToList();

                foreach (var sourceCell in colsForGetValues)
                {
                    var value = workSheet.Cells[rowIndex, sourceCell.ColIndex].Value;

                    if (value is null)
                    {
                        if (sourceCell.IsValueRequired)
                            throw new ArgumentException($"В ячейке не представлено обязательно значание для имени {sourceCell.Name}");
                        else
                            value = string.Empty;
                    }

                    var valueToString = value.ToString();
                    if (valueToString is not null)
                        values.Add(sourceCell.Name, (valueToString, sourceCell.ParsingTypeCode));
                }

            }

            return Equals(values?.Count, sourceCells.Count - filterCols.Count);
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
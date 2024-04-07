#nullable enable

using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ExcelReportManager : BaseManager
    {
        #region Constants && variables
        private const string reportExt = ".xlsx";
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;
        private const string LOGISTIC_FLAG = "Да";
        public Dictionary<SourceReportFileKey, SourceReportFileForSKU> ReportsPaths { get; private set; } = new();
        private readonly (int skuIndex, int dateIndex, int statusIndex, int logisticFlagIndex, int priceIndex) RedemptionTableIndexes = (2, 7, 9, 10, 14);
        private readonly (int skuIndex, int nameIndex, int initBalanceIndex, int goodsReceivedIndex, int finBalanceIndex) StorageTableIndexes = (1, 6, 14, 15, 17);

        #endregion

        #region Properties
        private static System.Drawing.Color DangerColor { get; } = System.Drawing.Color.DarkRed;
        public static DirectoryInfo? ClientDirectoryInfo { get; private set; }
        public string ClientLogin { get; private set; } = string.Empty;
        public static string OutputReportFilePath { get; private set; } = string.Empty;
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

        /// <summary>
        /// Receives supplied files from directory.
        /// </summary>
        /// <param name="dirPathString">Directory path string.</param>
        /// <returns>Number of files transferred for analysis.</returns>
        /// <exception cref="Exception">Exception for empty directory without files.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<int> GetReportFiles(string dirPathString)
        {
            ManagerReachedEventArgs? args = default;

            var files = GetFilesFromDirectory(dirPathString, reportExt);

            if (files is null || !files.Any())
            {
                args = new ManagerReachedEventArgs("Директория с файлами для обработки не сущесвтует или пустая, проверьте ее.",
                    1000,
                    false);
            }
            else
            {
                foreach (var fileInfo in files!)
                {
                    try
                    {
                        var sorceReportFile = new SourceReportFileForSKU(fileInfo);
                        var key = sorceReportFile.ReportKey;

                        if (sorceReportFile != null && key.HasValue)
                        {
                            // Save filePath by key 
                            ReportsPaths[key.Value] = sorceReportFile;

                            // Save login
                            if (sorceReportFile?.Login != null)
                                ClientLogin = sorceReportFile.Login;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        args = new($"{ex.Message}", 500, true);
                        throw;
                    }
                }

                var readinessMessage = $"Логин клиента: {ClientLogin}\n";
                readinessMessage = string.Concat(readinessMessage, $"{ReportsPaths.Count} файлов (-ла) готово к обработке.\n");

                try
                {
                    var clientOutputPath = await Task.Run(GetClientOutputPath);

                    if (!string.IsNullOrEmpty(clientOutputPath))
                    {
                        readinessMessage = string.Concat(readinessMessage, $"Директория для работы с клиентом: {clientOutputPath}");
                    }
                }
                catch (System.Exception ex)
                {
                    args = new($"{ex.Message}", 500, true);
                    throw;
                }

                args = new(readinessMessage, 500, false);
            }

            if (args != null) OnManagerReached(args);

            return files is null || !files.Any() ? 0 : ReportsPaths.Count;
        }

        /// <summary>
        /// Checks whether required files have been added to create a client report.
        /// </summary>
        /// <param name="lostKeys">A collection of required file keys that were not provided.</param>
        /// <returns>Logical flag for performing an operation.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public bool IsRequiredFilesProvided(out List<SourceReportFileKey> lostKeys)
        {
            lostKeys = new(Array.Empty<SourceReportFileKey>());

            var isContainsKeyRedemtionWB = ReportsPaths.ContainsKey(SourceReportFileKey.RedemtionWB);
            var isContainsKeyRedemtionOZ = ReportsPaths.ContainsKey(SourceReportFileKey.RedemtionOZ);

            var isContainsKeyStorageWB = ReportsPaths.ContainsKey(SourceReportFileKey.RemainingWB);
            var isContainsKeyStorageOZ = ReportsPaths.ContainsKey(SourceReportFileKey.RemainingOZ);

            var isContainsKeyShipped = ReportsPaths.ContainsKey(SourceReportFileKey.Shipped);

            var isWBSuccess = isContainsKeyRedemtionWB && isContainsKeyStorageWB && isContainsKeyShipped;
            if (!isWBSuccess)
            {
                if (!isContainsKeyRedemtionWB) lostKeys.Add(SourceReportFileKey.RedemtionWB);
                if (!isContainsKeyStorageWB) lostKeys.Add(SourceReportFileKey.RemainingWB);
            }

            var isOZSuccess = isContainsKeyRedemtionOZ && isContainsKeyStorageOZ && isContainsKeyShipped;
            if (!isOZSuccess)
            {
                if (!isContainsKeyRedemtionOZ) lostKeys.Add(SourceReportFileKey.RedemtionOZ);
                if (!isContainsKeyStorageOZ) lostKeys.Add(SourceReportFileKey.RemainingOZ);
            }

            if (!isContainsKeyShipped) lostKeys.Add(SourceReportFileKey.Shipped);

            var success = isWBSuccess == true || isOZSuccess == true;
            return success;
        }

        // Запустить цепочку обработок
        // - выкупы wb и oz (обязательно)
        // - приходы и хранение wb и oz (обязательно)
        // - расходы общие (обязательно)
        // - списаниния недостач (обязательно)
        // - пересорты (опционально)
        // - недостачи (опционально)
        // - пустышки (опционально)

        public double AnalizeReport(SourceReportFileKey reportFileKey, string workSheetName,  bool isRequred = true)
        {
            var stopwatch = new Stopwatch();

            double StopMethod()
            {
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds / 1000;
            }

            stopwatch.Start();

            // Get report file 
            SourceReportFileForSKU? sourceReportFile = GetReportFile(reportFileKey, isRequred);
            if (sourceReportFile == null && !isRequred) return StopMethod();

            using ExcelPackage package = new(sourceReportFile!.FileInfo);
            
            var worksheet = package.Workbook.Worksheets.FirstOrDefault(sh => sh.Name == workSheetName) ??
                throw new Exception($"В книге Excel нет листа с именем '{workSheetName}'.");

            // пройти по массиву диапазонов


            package.Dispose();
            return StopMethod();
        }

        /// <summary>
        /// Get a report file for analysis by key.
        /// </summary>
        /// <param name="reportFileKey">Key to search for report file.</param>
        /// <param name="isRequred">Flag of required presence of file.</param>
        /// <returns></returns>
        /// <exception cref="Exception">An exception in which a required file for analysis was not found for given key.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public SourceReportFileForSKU? GetReportFile(SourceReportFileKey reportFileKey, bool isRequred)
        {
            var reportFileForSKU = ReportsPaths
                .SingleOrDefault(k => Equals(k, reportFileKey))
                .Value;

            if (reportFileForSKU == null)
            {
                if (isRequred)
                {
                    throw new Exception($"Файл для анализа по ключу {reportFileKey} не найден.");
                }
                else
                {
                    return null;
                }
            }

            return reportFileForSKU;
        }

        public static (double time, List<SKURowInReport> articleRows) AnalyzeRedemptionReport(string path, string sheetName,
            (int articleIndex, int dateIndex, int statusIndex, int logisticFlagIndex, int priceIndex) indices)
        {
            List<SKURowInReport> articleRows = new();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (IsDataPathExists(path, out FileInfo fileInfo))
            {
                using ExcelPackage package = new(fileInfo);

                var worksheet = package.Workbook.Worksheets.FirstOrDefault(sh => sh.Name == sheetName) ??
                    throw new Exception($"In found workbook there is no sheet with name '{sheetName}'.");

                var dimention = worksheet.Dimension;
                var endRowIndex = dimention.End.Row;


                // Идем по диапазонам 
                for (int row = 1; row <= dimention.End.Row; row++)
                {
                    var logisticFlagValue = worksheet.Cells[row, indices.logisticFlagIndex].Value.ToString();

                    if (!string.IsNullOrEmpty(logisticFlagValue))
                    {
                        var logisticFlag = logisticFlagValue.Replace(" ", "");

                        if (logisticFlag == LOGISTIC_FLAG)
                        {
                            var articleNumber = ParseValueToInt(worksheet.Cells[row, indices.articleIndex].Value);

                            if (!articleNumber.HasValue) continue;

                            var redemptionDateString = worksheet.Cells[row, indices.dateIndex].Value.ToString();
                            var goodStatusString = worksheet.Cells[row, indices.statusIndex].Value.ToString();
                            var priceSting = worksheet.Cells[row, indices.priceIndex].Value.ToString();

                            if (!string.IsNullOrEmpty(redemptionDateString) &&
                                !string.IsNullOrEmpty(goodStatusString) &&
                                !string.IsNullOrEmpty(priceSting))
                            {
                                var articleRow = new SKURowInReport(articleNumber.Value, redemptionDateString.Trim(), goodStatusString.Trim(), priceSting.Trim());
                                articleRows.Add(articleRow);
                            }
                        }
                    }
                }
            }

            stopwatch.Stop();
            return (stopwatch.ElapsedMilliseconds / 1000, articleRows);
        }

        public static string CreateMainReportSheet(string workSheetName, in Dictionary<int, SKU> articles)
        {
            //if(IsDataPathExists(OutputReportFilePath, out FileInfo fileInfo)) 
            //{
            using ExcelPackage excelPackage = new(new FileInfo(OutputReportFilePath));

            // Create ExcelWorksheet  
            CreateWorkSheet(workSheetName, in excelPackage, out ExcelWorksheet mainReportWorksheet, System.Drawing.Color.GreenYellow);

            // Filling table with data
            int recordIndex = 1;

            foreach (var (key, value) in articles)
            {
                // Fill Redemptions data
                mainReportWorksheet.Cells[recordIndex, 1].Value = value.Marketplace;
                mainReportWorksheet.Cells[recordIndex, 2].Value = key;
                mainReportWorksheet.Cells[recordIndex, 3].Value = value.Name;
                mainReportWorksheet.Cells[recordIndex, 4].Value = value.AveragePrice;

                // RedemptionDate
                mainReportWorksheet.Cells[recordIndex, 5].Value = value.MaxRedemptionDate;
                mainReportWorksheet.Cells[recordIndex, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                mainReportWorksheet.Cells[recordIndex, 5].Style.Fill.BackgroundColor.SetColor(value.SellmentStatus.StatusColor);

                mainReportWorksheet.Cells[recordIndex, 6].Value = value.SellmentStatus.StatusLabel;

                // Redemtion balance data 
                // var arb = value.GetArticleBalancesReport();
                // if (arb != null)
                // {
                //     mainReportWorksheet.Cells[recordIndex, 7].Value = arb.RedemtionSum;
                //     mainReportWorksheet.Cells[recordIndex, 8].Value = arb.RedemtionCanceled;
                //     mainReportWorksheet.Cells[recordIndex, 9].Value = arb.RedemtionOnWay;
                //     mainReportWorksheet.Cells[recordIndex, 10].Value = arb.RedemtionDelivered;
                //     mainReportWorksheet.Cells[recordIndex, 11].Value = arb.RedemtionWithDrawn;
                // }

                // // Storage balance data
                // var articleStoreBalance = value.ArticleStorageBalance;
                // var asb = articleStoreBalance.AnalizeBalance();

                // if (asb.HasValue)
                // {
                //     //Console.WriteLine($"asb: {asb.Value.expected} {asb.Value.received} {asb.Value.undelivery} {asb.Value.shipped} {asb.Value.remaining}");

                //     mainReportWorksheet.Cells[recordIndex, 12].Value = asb.Value.expected;
                //     mainReportWorksheet.Cells[recordIndex, 12].Style.Font.Bold = true;

                //     mainReportWorksheet.Cells[recordIndex, 13].Value = asb.Value.received;

                //     var undelivery = asb.Value.undelivery;
                //     mainReportWorksheet.Cells[recordIndex, 14].Value = undelivery;
                //     if(undelivery > 0) 
                //         mainReportWorksheet.Cells[recordIndex, 14].Style.Font.Color.SetColor(DangerColor); 

                //     mainReportWorksheet.Cells[recordIndex, 16].Value = asb.Value.shipped;
                //     mainReportWorksheet.Cells[recordIndex, 20].Value = asb.Value.remaining;
                // }

                //     // Consumption
                //     mainReportWorksheet.Cells[recordIndex, 15].Value = articleDict[key].StoreBalance.TotalConsumption;

                //     mainReportWorksheet.Cells[recordIndex, 17].Value = articleDict[key].StoreBalance.Regradings.Sum(re => re.Value);
                //     mainReportWorksheet.Cells[recordIndex, 18].Value = articleDict[key].StoreBalance.PayShortagesCount;
                //     mainReportWorksheet.Cells[recordIndex, 19].Value = articleDict[key].StoreBalance.DummiesCount;

                //     // Comment
                //     mainReportWorksheet.Cells[recordIndex, 21].Value = articleDict[key].Comment;

                recordIndex++;
            }

            // Setup header 
            SetupTableHeader(MainReportCells, ref mainReportWorksheet);
            AddFilterInWorkSheet(ref mainReportWorksheet);

            //Freeze row && columns
            mainReportWorksheet.View.FreezePanes(2, 4);

            // HACK: Переделать !!!
            // Group column
            for (var i = 8; i <= 11; i++)
            {
                mainReportWorksheet.Column(i).OutlineLevel = 1;
                mainReportWorksheet.Column(i).Collapsed = true;
            }

            for (var i = 16; i <= 19; i++)
            {
                mainReportWorksheet.Column(i).OutlineLevel = 1;
                mainReportWorksheet.Column(i).Collapsed = true;
            }

            //}

            // Save work (temp)
            if (File.Exists(OutputReportFilePath)) File.Delete(OutputReportFilePath);

            FileStream? objFileStrm = File.Create(OutputReportFilePath);
            objFileStrm.Close();

            File.WriteAllBytes(OutputReportFilePath, excelPackage.GetAsByteArray());

            return OutputReportFilePath;
        }

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

        public async Task<string?> GetClientOutputPath()
        {
            if (ClientLogin is null)
                throw new Exception("Логин клинта не определен, поэтому невозможно создать директорию.");

            ClientDirectoryInfo = FileManager.GetDirectoryInfo(PathKey.MPBoostClients, ClientLogin);

            if (ClientLogin != null && ClientDirectoryInfo != null)
            {
                var dateTime = DateTime.Now.ToString("dd-MM-yyyy");
                OutputReportFilePath = $"{ClientDirectoryInfo.FullName}/ClientReport_{ClientLogin}_{dateTime}.xlsx";

                return ClientDirectoryInfo.FullName;
            }
            else
            {
                await Task.Delay(100);
                throw new Exception("Логин клинта не определен, невозможно создать директорию для работы с ним.");
            }

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

        public static void GetRangeSourceReportRows(in ExcelWorksheet worksheet, ref List<RangeSourceReportRows> rangeSource, int startRangeRowIndex = 1) 
        {

            for (int i = 0; i < rangeSource.Count; i++)
            {
                // Если один элемент и у него нет ключа
                if(rangeSource.Count == 1 && rangeSource[i].Key == null) 
                {
                    var dim = worksheet.Dimension;
                    rangeSource[i].SetRange(startRangeRowIndex + 1, dim.End.Row - 1);
                } 
                else 
                {
                    var currentRowStringKey = rangeSource[i].Key ?? throw new Exception("Указание строкового ключа для диапазона обязательно");
                    var startRowIndex = GetRowIndexBy(rangeSource[i].Key!, in worksheet);

                    // Индекс строки с ключом
                    // Индекс строки со следующим ключом

                }
            }

            // ..

        }


        #endregion
    }
}

using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.ArticleItems;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ExcelReportManager : BaseManager
    {
        #region Constants && variables
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;
        private const string LOGISTIC_FLAG = "Да";
        #endregion

        #region Properties
        private static System.Drawing.Color DangerColor { get; } = System.Drawing.Color.DarkRed;
        public static DirectoryInfo? ClientDirectoryInfo {get; private set; }
        public static string ClientLogin {get; private set; } = string.Empty;
        public static string OutputReportFilePath {get; private set; } = string.Empty;
        private static ResultCell[] MainReportCells { get; } = new ResultCell[]
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
        public ExcelReportManager(){}
        #endregion

        #region Functionality

        public static void DeterminingСlientLogin(string path)
        {
            var loginException = new Exception($"Логин клиента не может быть определен");

            var substrings01 = path.Split("/");

            if (substrings01.Length <= 1) throw loginException;

            var loginChars = substrings01.Last().Split(" ")[3].Trim().ToList();

            if (loginChars is null || loginChars.Count == 0) throw loginException;

            // Clear non-digits in login
            for (int i = 0; i < loginChars.Count; i++)
                if (!int.TryParse(loginChars[i].ToString(), out _)) loginChars.RemoveAt(i);
            
            ClientLogin = new string(loginChars.ToArray());
        }

        public static (double time, List<ArticleRowInReport> articleRows) AnalyzeRedemptionReport(string path, string sheetName, 
            (int articleIndex, int dateIndex, int statusIndex, int logisticFlagIndex, int priceIndex) indices) 
        {
            List<ArticleRowInReport> articleRows = new();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if(IsDataPathExists(path, out FileInfo fileInfo)) 
            {
                using ExcelPackage package = new(fileInfo);

                var worksheet = package.Workbook.Worksheets.FirstOrDefault(sh => sh.Name == sheetName) ?? 
                    throw new Exception($"In found workbook there is no sheet with name '{sheetName}'.");
                
                var dimention = worksheet.Dimension;
                var endRowIndex = dimention.End.Row;

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
                                var articleRow  = new ArticleRowInReport(articleNumber.Value, redemptionDateString.Trim(), goodStatusString.Trim(), priceSting.Trim());
                                articleRows.Add(articleRow);
                            }
                        }
                    }
                }
            }

            stopwatch.Stop();
            return (stopwatch.ElapsedMilliseconds / 1000, articleRows);
        }

        public static string CreateMainReportSheet(string workSheetName, in Dictionary<int, Article> articles) 
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

                    var arb = value.ArticleRedempionBalance.AnalizeBalance();
                    if(arb.HasValue)
                    {
                        // Redemtion balance Ddta 

                        Console.WriteLine($"arb: {arb.Value.sum} {arb.Value.canceled} {arb.Value.onWay} {arb.Value.delivered} {arb.Value.withDrawn}");

                        mainReportWorksheet.Cells[recordIndex, 7].Value = arb.Value.sum;
                        mainReportWorksheet.Cells[recordIndex, 8].Value = arb.Value.canceled;
                        mainReportWorksheet.Cells[recordIndex, 9].Value = arb.Value.onWay;
                        mainReportWorksheet.Cells[recordIndex, 10].Value = arb.Value.delivered;;
                        mainReportWorksheet.Cells[recordIndex, 11].Value = arb.Value.withDrawn;
                    } 

                    // Storage balance data
                    var articleStoreBalance = value.ArticleStorageBalance;
                    var asb = articleStoreBalance.AnalizeBalance();
                
                    if (asb.HasValue)
                    {
                        Console.WriteLine($"asb: {asb.Value.expected} {asb.Value.received} {asb.Value.undelivery} {asb.Value.shipped} {asb.Value.remaining}");

                        mainReportWorksheet.Cells[recordIndex, 12].Value = asb.Value.expected;
                        mainReportWorksheet.Cells[recordIndex, 12].Style.Font.Bold = true;

                        mainReportWorksheet.Cells[recordIndex, 13].Value = asb.Value.received;

                        var undelivery = asb.Value.undelivery;
                        mainReportWorksheet.Cells[recordIndex, 14].Value = undelivery;
                        if(undelivery > 0) 
                            mainReportWorksheet.Cells[recordIndex, 14].Style.Font.Color.SetColor(DangerColor); 

                        mainReportWorksheet.Cells[recordIndex, 16].Value = asb.Value.shipped;
                        mainReportWorksheet.Cells[recordIndex, 20].Value = asb.Value.remaining;
                    }

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

        // public static void SaveReportFiles() 
        // {
        //     //if(IsDataPathExists(filePath, out FileInfo fileInfo)) 
        //     //{
        //         var stopwatch = new Stopwatch();
        //         using ExcelPackage excelPackage = new(new FileInfo(OutputReportFilePath));

              

        //         stopwatch.Stop();
        //     //}
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

        private static void SetupTableHeader(in ResultCell[] resultCells, ref ExcelWorksheet worksheet, double headerRowHeight = HEADER_TABLE_ROW_HEIGHT)
        {
            var firstRow = worksheet.Row(RECORD_INDEX);
            if(firstRow is not null) 
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

                ExcelRange firstRowCell =  worksheet.Cells[RECORD_INDEX, columnNumber] ?? throw new Exception($"Cell at address {RECORD_INDEX} {columnNumber} not found on worksheet");

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
        
        public static async Task<string?> GetClientOutputPath()
        {
            if (ClientLogin is null)
                throw new Exception("Логин клинта не определен, поэтому невозможно создать директорию.");

            ClientDirectoryInfo = FileManager.GetDirectoryInfo(PathKey.MPBoostClients, ClientLogin);

            if (ClientLogin != null &&  ClientDirectoryInfo != null) 
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
        
        #endregion
    }
}

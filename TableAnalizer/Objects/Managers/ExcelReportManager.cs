using ClientTableAnalizer.Enums;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ExcelReportManager
    {
        #region 
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;

        private static ExcelPackage excelPackage;
        #endregion

        #region Constructors
        public ExcelReportManager(){}

        ~ ExcelReportManager()
        {
            excelPackage.Dispose();
        }
        #endregion


        #region Properties 

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

        public static string OuptutFilePath { get; private set;}

        #endregion

        #region Functionality

        public static void CreateExcelPackage (string ouptutFilePath) 
        {
             excelPackage = new ExcelPackage(ouptutFilePath);
             OuptutFilePath = ouptutFilePath;
        } 
  
        public static void CreateMainReportSheet(string workSheetName, in Dictionary<int, Article> articles, int deadline) 
        {
            // Create ExcelWorksheet  
            CreateWorkSheet(workSheetName, out ExcelWorksheet mainReportWorksheet, System.Drawing.Color.GreenYellow);

            // Filling table with data
            int recordIndex = RECORD_INDEX;
            foreach (var (key, value) in articles)
            {
                value.AnalyzeArticle(key, deadline);

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

                var articleRedempionBalance = value.ArticleRedempionBalance;
                var articleStoreBalance = value.ArticleStorageBalance;

                if(articleRedempionBalance.GetRowGroupsByStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus, ArticleRowInReport>>? groups) && groups is not null) 
                {
                    int canceled = 0, onWay = 0, delivered = 0, withDrawn = 0, sum = 0, expected = 0;

                    foreach (var group in groups)
                    {
                        if(group.Key == ArticleRedemptionStatus.Canceled) canceled = group.Count();
                        if(group.Key == ArticleRedemptionStatus.OnWay) onWay = group.Count();
                        if(group.Key == ArticleRedemptionStatus.Delivered) delivered = group.Count();
                        if(group.Key == ArticleRedemptionStatus.WithDrawn) withDrawn = group.Count();
                    }

                    sum = canceled + onWay  + delivered  + withDrawn;
                    expected = delivered + withDrawn;

                    // Redemtion balance Ddta 
                    mainReportWorksheet.Cells[recordIndex, 7].Value = sum;
                    mainReportWorksheet.Cells[recordIndex, 8].Value = canceled;
                    mainReportWorksheet.Cells[recordIndex, 9].Value = onWay;
                    mainReportWorksheet.Cells[recordIndex, 10].Value = delivered;
                    mainReportWorksheet.Cells[recordIndex, 11].Value = withDrawn;

                    // Storage balance data
                    // - expected goods
                    mainReportWorksheet.Cells[recordIndex, 12].Value = expected;
                    mainReportWorksheet.Cells[recordIndex, 12].Style.Font.Bold = true;

                }

        //     mainReportWorksheet.Cells[recordIndex, 13].Value = articleDict[key].StoreBalance.ReceivedQuantity;

        //     // - Undelivery
        //     mainReportWorksheet.Cells[recordIndex, 14].Value = articleDict[key].StoreBalance.Undelivery;
        //     if(articleDict[key].StoreBalance.Undelivery > 0 && articleDict[key].StatusValue.status != ArticleStatus.InWork)
        //     {
        //         mainReportWorksheet.Cells[recordIndex, 14].Style.Font.Color.SetColor(System.Drawing.Color.DarkRed); 
        //     }

        //     // Consumption
        //     mainReportWorksheet.Cells[recordIndex, 15].Value = articleDict[key].StoreBalance.TotalConsumption;
        //     mainReportWorksheet.Cells[recordIndex, 16].Value = articleDict[key].StoreBalance.ShippedQuantity;
        //     mainReportWorksheet.Cells[recordIndex, 17].Value = articleDict[key].StoreBalance.Regradings.Sum(re => re.Value);
        //     mainReportWorksheet.Cells[recordIndex, 18].Value = articleDict[key].StoreBalance.PayShortagesCount;
        //     mainReportWorksheet.Cells[recordIndex, 19].Value = articleDict[key].StoreBalance.DummiesCount;
        //     mainReportWorksheet.Cells[recordIndex, 20].Value = articleDict[key].StoreBalance.FinBalance;

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
        }

        public static void SaveReportFile() 
        {
            // Save work
            if (File.Exists(OuptutFilePath)) File.Delete(OuptutFilePath);

            // Create excel file on physical disk 
            var objFileStrm = File.Create(OuptutFilePath);
            objFileStrm.Close();

            // Write content to excel file 
            File.WriteAllBytes(OuptutFilePath, excelPackage.GetAsByteArray());

            // Close Excel package
            excelPackage.Dispose();
        }

        private static void CreateWorkSheet(string workSheetName, out ExcelWorksheet worksheet, System.Drawing.Color tabColor)
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


        #endregion
        
    }
}

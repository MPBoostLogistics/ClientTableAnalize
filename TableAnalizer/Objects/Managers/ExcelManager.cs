#nullable enable

using System.Drawing;
using TableAnalizer.Enums;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.SKUItems;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace TableAnalizer.Objects.Managers
{
    public class ExcelManager : BaseManager
    {
        #region Constants && variables
        public const string FIRST_ROOM_NAME = "1 - Помещение приемки и сортировки";
        public const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения";
        public const string THIRD_ROOM_NAME = "3 - Помещение упаковки и отгрузки";
        public const string LAST_MARKER_KEY = "Итого товаров в единицах хранения";
        public const int LAST_MARKER_KEY_OFFSET = 2;
        public const string OUTPUT_REPORT_WORKSHEET01_NAME = "Отчет по логистике";
        public const string OUTPUT_REPORT_WORKSHEET02_NAME = "Отчет по задержкам";
        public const string OUTPUT_REPORT_WORKSHEET03_NAME = "Отчет по перерасходам";
        public const string COMPENSATION_REPORT_WORKSHEET_NAME = "Недостачи";
        public const string RESORTS_REPORT_WORKSHEET_NAME = "Пересорты";
        public const string DUMMIES_REPORT_WORKSHEET_NAME = "Пустышки";
        public const string RANSOMS_WORKSHEET_NAME = "Выкупы";
        public const string REMAINING_OR_SHIPPED_WORKSHEET_NAME = "TDSheet";
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;
        private const int HEADER_FONT_SIZE = 14;
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
        public static List<SourceColumn> RansomsSourceCells { get; } = new()
        {
            new(2, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(7, "SKURansomDate", ParsingTypeCode.DataOnlyTypeCode,true),
            new(9, "SKURansomsStatus", ParsingTypeCode.SKURansomsStatus, true),
            new(10, "Логистика", ParsingTypeCode.StringTypeCode, true, LOGISTIC_FLAG),
            new(14, "SKUCost",  ParsingTypeCode.DoubleTypeCode, true)
        };

        public static List<SourceColumn> RemainingFirstRoomSourceCells { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "SKUBrandAndName", ParsingTypeCode.StringTypeCode, true),
            new(15, "SKUReceipt", ParsingTypeCode.IntTypeCode, false),
            new(17, "SKURemaining", ParsingTypeCode.IntTypeCode, false)
        };

        public static List<SourceColumn> RemainingSecondRoomSourceCells { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "SKUBrandAndName", ParsingTypeCode.StringTypeCode, true),
            new(17, "SKURemaining", ParsingTypeCode.IntTypeCode, false)
        };

        public static List<SourceColumn> RemainingThirdRoomSourceCells { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "SKUBrandAndName", ParsingTypeCode.StringTypeCode, true),
            new(17, "SKURemaining", ParsingTypeCode.IntTypeCode, false)
        };

        public static List<SourceColumn> ShippedSourceCells { get; } = new()
        {
            new(4, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(28, "SKUShippedCount", ParsingTypeCode.IntTypeCode, true)
        };

        public static List<SourceColumn> CompensationSourceCells { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(2, "CompensationCount", ParsingTypeCode.IntTypeCode, true),
            new(3, "CompensationSum", ParsingTypeCode.DoubleTypeCode, true),
            new(4, "CompensationComment", ParsingTypeCode.StringTypeCode, false)
        };

        public static List<SourceColumn> ResortSourceCells { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(2, "ResortSKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(3, "ResortSKUCount", ParsingTypeCode.IntTypeCode, true),
            new(4, "ResortComment", ParsingTypeCode.StringTypeCode, false)
        };

        public static List<SourceColumn> DummiesSourceCells { get; } = new()
        {
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(2, "DummiesSKUCount", ParsingTypeCode.IntTypeCode, true),
            new(3, "DummiesComment", ParsingTypeCode.StringTypeCode, false)
        };
        
        public static List<SourceColumn> WriteOffShortagesSourceCells { get; } = new() 
        {
            new(5, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(39, "SKUWriteOffCount", ParsingTypeCode.IntTypeCode, true)
        };     

        public TargetCell[] cellsReportingMain = new TargetCell[]
        {
            new (colNumber: 1, colName: "МП", colWidth: 8, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUmp",
                parsingTypeCode: ParsingTypeCode.Marketplace, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 2, colName: "Артикул", colWidth: 12, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUNumber",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 3, colName: "Бренд товара", colWidth: 15, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUBrand",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 4, colName: "Наименование товара", colWidth: 50, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUName",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null),
            new (colNumber: 5, colName: "Средняя цена, руб.", colWidth: 12, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, "SKUPrice",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x > 0),
            new (colNumber: 6, colName: "Дата последнего заказа", colWidth: 13, format: "dd.MM.yyyy",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDate",
                parsingTypeCode: ParsingTypeCode.DataOnlyTypeCode, excelFillStyle:  ExcelFillStyle.Solid,
                valueСheckingPredicate: null),
            new (colNumber: 7, colName: "Статус взаиморасчетов", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SellmentStatusString",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 8, colName: "Заказов: всего", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKURansomsTotal",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x > 0),
            new (colNumber: 9, colName: "Заказов: отменено",colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsCanceled",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 10, colName: "Заказов: в пути", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsOnWay",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 11, colName: "Заказов: доставлено на ПВЗ", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsDelivered",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 12, colName: "Заказов: забрано с ПВЗ", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsWithDrawn",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 13, colName: "Склад: Ожидание", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURansomsWaiting",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: true),                                             
            new (colNumber: 14, colName: "Склад: Приход товаров", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKUReceipt",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 15, colName: "Склад: Расход всего", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKUTotalСonsumption",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: true),
            new (colNumber: 16, colName: "Склад: Расход товаром", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKUShipped",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 17, colName: "Склад: Расход компенсацией", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKUPaid",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 18, colName: "Склад: Расход пересортом", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKUResort",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 19, colName: "Склад: Расход пустышками", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKUDummies",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 20, colName: "Склад: Остаток товаров", colWidth: 7, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 90, propertyName: "SKURemaining",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 21, colName: "Задержка довоза, всего", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayTotal",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle: ExcelFillStyle.None, 
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: true),
            new (colNumber: 22, colName: "Задержка довоза от точек, товаров", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayPoints",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 23, colName: "Задержка довоза от точек, сумма", colWidth: 12, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayPointsSum",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 24, colName: "Задержка довоза от склада, товаров", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayStorage",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 25, colName: "Задержка довоза от склада, сумма", colWidth: 12, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayStorageSum",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 26, colName: "Учтено пересортами, товаров", colWidth: 15, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUAccountingByResortsCount",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 27, colName: "Учтено пересортами, комментарий", colWidth: 25, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUAccountingByResortsComment",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null, isBoldFont: false),
            new (colNumber: 28, colName: "Перерасход, товаров", colWidth: 14, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUOverrunsCount",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: true),
            new (colNumber: 29, colName: "Перерасход, сумма", colWidth: 14, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUOverrunsSum",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 30, colName: "Комментарий к артикулу", colWidth: 50, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUComment",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null, isBoldFont: false),
        };
        
        public TargetCell[] cellsReportingDelays = new TargetCell[] 
        {
            new (colNumber: 1, colName: "МП", colWidth: 8, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUmp",
                parsingTypeCode: ParsingTypeCode.Marketplace, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 2, colName: "Артикул", colWidth: 12, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUNumber",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 3, colName: "Бренд товара", colWidth: 15, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUBrand",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 4, colName: "Наименование товара", colWidth: 50, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUName",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null),
            new (colNumber: 5, colName: "Задержка довоза от точек, товаров", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayPoints",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 6, colName: "Задержка довоза от точек, сумма", colWidth: 12, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayPointsSum",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 7, colName: "Задержка довоза от склада, товаров", colWidth: 12, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayStorage",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false),
            new (colNumber: 8, colName: "Задержка довоза от склада, сумма", colWidth: 12, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUDeliveryDelayStorageSum",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false)
        };

        public TargetCell[] cellsReportingOverruns = new TargetCell[] 
        {
             new (colNumber: 1, colName: "МП", colWidth: 8, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUmp",
                parsingTypeCode: ParsingTypeCode.Marketplace, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 2, colName: "Артикул", colWidth: 12, format: "0",
                wrapTextInHeader: false, textRotationInHeader: 0, propertyName: "SKUNumber",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 3, colName: "Бренд товара", colWidth: 15, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUBrand",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: null),
            new (colNumber: 4, colName: "Наименование товара", colWidth: 50, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUName",
                parsingTypeCode: ParsingTypeCode.StringTypeCode, excelFillStyle:  ExcelFillStyle.None, 
                valueСheckingPredicate: null),
             new (colNumber: 5, colName: "Перерасход, товаров", colWidth: 14, format: "0",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUOverrunsCount",
                parsingTypeCode: ParsingTypeCode.IntTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: true),
            new (colNumber: 6, colName: "Перерасход, сумма", colWidth: 14, format: "0.00",
                wrapTextInHeader: true, textRotationInHeader: 0, propertyName: "SKUOverrunsSum",
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, excelFillStyle:  ExcelFillStyle.None,
                valueСheckingPredicate: (int x) => x == 0, isBoldFont: false)
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
        /// <remarks><i>New version, tested</i></remarks>
        public static bool GetExcelPackageByKey(SourceSKUReportFileKey key, out ExcelPackage? package)
        {
            ExcelPackages.TryGetValue(key, out package);
            return package is not null;
        }

        /// <summary>
        /// Returns name of sheet in Excel workbook.
        /// </summary>
        /// <param name="reportFileKey">Report file key.</param>
        /// <param name="workSheetNames">Collection of Excel workbook sheet names.</param>
        /// <returns>Boolean operation execution flag.</returns>
        /// <remarks><i>New version</i></remarks>
        public static bool GetExcelWorksheetStringNamesByKey(SourceSKUReportFileKey reportFileKey, out List<string>? workSheetNames)
        {
            workSheetNames = reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => new(1) { RANSOMS_WORKSHEET_NAME },
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ or SourceSKUReportFileKey.Shipped or SourceSKUReportFileKey.WriteOffShortages => new(1) { REMAINING_OR_SHIPPED_WORKSHEET_NAME },
                SourceSKUReportFileKey.Additional => new(3) { COMPENSATION_REPORT_WORKSHEET_NAME, RESORTS_REPORT_WORKSHEET_NAME, DUMMIES_REPORT_WORKSHEET_NAME },
                _ => null,
            };
            return workSheetNames is not null && workSheetNames.Any();
        }

        public void MakeReports(List<SKUForTargetReports> skuForTargetReports) 
        {
            var outputPackage = new ExcelPackage(new FileInfo(FileManager.OutputClientReportFilePath)) ?? 
                throw new Exception("ExcelPackage for output reports cannot be created");
            
            var sheetName = string.Empty;
            TargetCell[] targetCells = Array.Empty<TargetCell>();
            List<SKUForTargetReports> actualData = new();

            // Make main report
            sheetName = OUTPUT_REPORT_WORKSHEET01_NAME;
            targetCells = cellsReportingMain;
            actualData = skuForTargetReports;
            CreateExcelWorkSheet(sheetName, in outputPackage, out ExcelWorksheet? mainOutputReportSheet, Colors[SKUSellmentStatus.FullSet]);
            FillingСellsWithData<SKUForTargetReports>(outputPackage, ref mainOutputReportSheet, in actualData, targetCells, FileManager.OutputClientReportFilePath); 
            SetupTableHeader(in targetCells, ref mainOutputReportSheet);    
            AddFilterInWorkSheet(ref mainOutputReportSheet);

            // Make delay report
            actualData = skuForTargetReports
                .Where(el => el.SKUDeliveryDelayTotal > 0 && (el.SellmentStatus.Status is SKUSellmentStatus.OnSearchPoint || el.SellmentStatus.Status is SKUSellmentStatus.OnSearchWarehouse || el.SellmentStatus.Status is SKUSellmentStatus.OnSearchWarehouseAndPoints))
                .ToList();
            sheetName = OUTPUT_REPORT_WORKSHEET02_NAME;
            targetCells = cellsReportingDelays;
            CreateExcelWorkSheet(sheetName, in outputPackage, out ExcelWorksheet? delayOutputReportSheet, Colors[SKUSellmentStatus.OnSearchWarehouse]);
            FillingСellsWithData<SKUForTargetReports>(outputPackage, ref delayOutputReportSheet, in actualData, targetCells, FileManager.OutputClientReportFilePath); 
            SetupTableHeader(in targetCells, ref delayOutputReportSheet); 
            AddFilterInWorkSheet(ref delayOutputReportSheet); 

            // Make overruns report
            actualData = skuForTargetReports
                .Where(el => el.SKUOverrunsSum > 0.00)
                .ToList();
            sheetName = OUTPUT_REPORT_WORKSHEET03_NAME;
            targetCells = cellsReportingOverruns;
            CreateExcelWorkSheet(sheetName, in outputPackage, out ExcelWorksheet? overrunsOutputReportSheet, Colors[SKUSellmentStatus.Unknown]);
            FillingСellsWithData<SKUForTargetReports>(outputPackage, ref overrunsOutputReportSheet, in actualData, targetCells, FileManager.OutputClientReportFilePath); 
            SetupTableHeader(in targetCells, ref overrunsOutputReportSheet); 
            AddFilterInWorkSheet(ref overrunsOutputReportSheet); 

            // Save work
            SaveReceivedData(FileManager.OutputClientReportFilePath, ref outputPackage);
            outputPackage.Dispose();
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
                            if (TryGetPropertyValue(item, targetCell.PropertyName, out Marketplace? marketplaceValue))
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

                        // Checking predicate condition ()
                        if(targetCell.ValueСheckingPredicate is not null) 
                        {
                            int? cheking = value is double ? Convert.ToInt32(value) : value is int v ? v : null;

                            if(cheking.HasValue && !targetCell.ValueСheckingPredicate(cheking.Value)) 
                            {
                                cell.Style.Font.Color.SetColor(targetCell.HighlightTextСolor);
                            }
                        }

                        //

                        // Bold font for cell
                        cell.Style.Font.Bold = targetCell.IsBoldFont;
                    }
                }
                rowIndex++;
            }
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
                firstRow.Style.Font.Size = HEADER_FONT_SIZE;
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


            //Column Group
            excelWorksheet.OutLineSummaryBelow = false;

            var range01 = Enumerable.Range(9, 4).ToList();
            var range02 = Enumerable.Range(16, 4).ToList();
            var range03 = Enumerable.Range(22, 4).ToList();

            excelWorksheet.Columns[range01.First(), range01.Last()].Group();
            excelWorksheet.Columns[range02.First(), range02.Last()].Group();
            excelWorksheet.Columns[range03.First(), range03.Last()].Group();

            excelWorksheet.View.FreezePanes(2, 4);
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
        public static bool GetRowIndexBy(string key, in ExcelWorksheet worksheet, out int? rowInex) 
        {
            rowInex =  worksheet.Cells.FirstOrDefault(c => c.Value != null && c.Value.ToString()?.Trim().ToLower() == key.Trim().ToLower())?.Start.Row;
            return rowInex.HasValue;
        }
            
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

                if (reportRowsRange is null || reportRowsRange.RangeRowIndexesСollection is null) continue;

                // Iterating Rows 
                var rowsIndexRange = reportRowsRange.RangeRowIndexesСollection.ToList();
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
        public static bool GetValuesFromRow(in ExcelWorksheet workSheet, int rowIndex, in List<SourceColumn> sourceCells, out Dictionary<string, ItemForParsing>? values)
        {
            values = null;

            if (!CheckingPresenceAddressesInRow(in workSheet, rowIndex, in sourceCells) ||
                !СheckingValuesInRequiredCells(in workSheet, rowIndex, in sourceCells) ||
                !FilterMatchingCheck(in workSheet, rowIndex, in sourceCells, out int filterCellsCount) ||
                !FindSorceCellsByType(in sourceCells, SourceCellsType.NonFiltration, out List<SourceColumn>? dataСellsNonFilters) ||
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

        private static bool CheckingPresenceAddressesInRow(in ExcelWorksheet workSheet, int rowIndex, in List<SourceColumn> sourceCells)
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

        private static bool FilterMatchingCheck(in ExcelWorksheet workSheet, int rowIndex, in List<SourceColumn> sourceCells, out int filterSourceCellsCount)
        {
            filterSourceCellsCount = 0;

            if (!FindSorceCellsByType(in sourceCells, SourceCellsType.Filtration, out List<SourceColumn>? filterSourceCells)) 
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

        private static bool СheckingValuesInRequiredCells(in ExcelWorksheet workSheet, int rowIndex, in List<SourceColumn> sourceCells)
        {
            if(!FindSorceCellsByType(in sourceCells, SourceCellsType.Required, out List<SourceColumn>? requiredCells) && 
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

        private static bool FindSorceCellsByType(in List<SourceColumn> sourceCells, SourceCellsType sourceCellsType, out List<SourceColumn>? cells)
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
        /// Creates a collection of RangeSourceReportRows objects using SourceSKUReportFileKey key.
        /// </summary>
        /// <param name="reportFileKey">Source report file key.</param>
        /// <param name="rangeSourceReportRows">Сollection of RangeSourceReportRows objects.</param>
        /// <returns>Boolean flag for performing the operation.</returns>
        public static bool MakeRangeSourceReportRowsByKey(SourceSKUReportFileKey reportFileKey, string workSheetName, out List<RangeSourceReportRows> rangeSourceReportRows)
        {
            rangeSourceReportRows = reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => new(1) { new(RansomsSourceCells) },
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ => new(3) { new(RemainingFirstRoomSourceCells, FIRST_ROOM_NAME),
                                                     new(RemainingSecondRoomSourceCells, SECOND_ROOM_NAME), new(RemainingThirdRoomSourceCells, THIRD_ROOM_NAME, (LAST_MARKER_KEY,LAST_MARKER_KEY_OFFSET))},
                SourceSKUReportFileKey.WriteOffShortages => new(1) {new (WriteOffShortagesSourceCells)},
                SourceSKUReportFileKey.Additional => workSheetName switch
                {
                    COMPENSATION_REPORT_WORKSHEET_NAME => new(1) { new(CompensationSourceCells) },
                    RESORTS_REPORT_WORKSHEET_NAME => new(1) { new(ResortSourceCells) },
                    DUMMIES_REPORT_WORKSHEET_NAME => new(1) { new(DummiesSourceCells) },
                    _ => new(),
                },
                SourceSKUReportFileKey.Shipped => new(1) { new(ShippedSourceCells) },
                _ => new(),
            };

            return rangeSourceReportRows.Any();
        }

        /// <summary>
        /// Gets row ranges in an Excel worksheet.
        /// </summary>
        /// <param name="worksheet">Excel worksheet.</param>
        /// <param name="rangeSourceReportRows">Range prototypes.</param>
        /// <param name="startRangeRowIndex">Start index.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public static void GetRangeSourceReportRows(in ExcelWorksheet worksheet, ref List<RangeSourceReportRows> rangeSourceReportRows, int startRangeRowIndex = 1)
        {
            const int DEFAULT_STEP = 1;

            IEnumerable<RangeSourceReportRows>? rangesWithKeys = rangeSourceReportRows.Where(rs => !string.IsNullOrEmpty(rs.Key));
            RangeSourceReportRows? rangeWithTermination = rangeSourceReportRows.SingleOrDefault(rs => rs.TerminatingStringValue.HasValue);

            bool isOneRangeWithoutKeysAndTerminations = rangeSourceReportRows.Count == 1 && 
                (rangesWithKeys is null || !rangesWithKeys.Any()) && 
                rangeWithTermination is null;
            
            // Range is one, no key and no termination value (e.g. Ransoms, Shipped, Compenaton, Resorts etc)
            if(isOneRangeWithoutKeysAndTerminations)
            { 
                var start = startRangeRowIndex + DEFAULT_STEP;
                var count = worksheet.Dimension.End.Row - DEFAULT_STEP;
                rangeSourceReportRows.First().SetRange(start,count);
            } 
            // There is more than one range (e.g. Remainig)
            else if(rangeSourceReportRows.Count > 1)
            {
                CreateRowRange(in worksheet, ref rangeSourceReportRows, DEFAULT_STEP, rangeWithTermination?.TerminatingStringValue);
            }
        }

        private static void CreateRowRange(in ExcelWorksheet worksheet, ref List<RangeSourceReportRows> rangeSourceReportRows, int DEFAULT_STEP, (string rowStringKey, int offset)? termination)
        {
            List<RangeSourceReportRows> outputRanges = new();

            // Get existing keys from templates 
            List<string>? keysInTemplateList = rangeSourceReportRows
                .Where(el => !string.IsNullOrEmpty(el.Key))
                .Select(el => el.Key!)
                .ToList();

            // Get existing elements in table by key
            List<(string key, int index)> existingElements = new();
            for (int i = 0; i < keysInTemplateList.Count; i++)
            {
                var key = keysInTemplateList[i];
                if(!string.IsNullOrEmpty(key) && 
                    GetRowIndexBy(key, in worksheet, out int? rowIndex) && 
                    rowIndex.HasValue)
                {
                    existingElements.Add((key, rowIndex.Value));
                }
            }

            // Make LinkedList from existing elements in table  
            LinkedList<(string key, int index)> existingElementsLinkedList = new(existingElements);

            int? count = null, start = null;

            // Iterate linked list
            var currentNode = existingElementsLinkedList.First;

            while(currentNode != null)
            {
                (string key, int index) = currentNode.Value;

                start = index + DEFAULT_STEP;

                if(currentNode.Next != null) 
                {
                    (string nextKey, int nextIndex) = currentNode.Next!.Value;
                    count = nextIndex - start.Value;
                } 
                else
                {
                    if(termination is not null) 
                    {
                        var terminationKey = termination.Value.rowStringKey;
                        var terminationOffset = termination.Value.offset;

                        if(!string.IsNullOrEmpty(terminationKey) && 
                           GetRowIndexBy(terminationKey, in worksheet, out int? terminationIndex) && 
                           terminationIndex.HasValue)
                        {
                            count = terminationIndex - start.Value - terminationOffset;
                        } 
                        else 
                        {
                            throw new ArgumentException("Cannot get index of termination row.");
                        }
                    } 
                    else 
                    {
                        throw new ArgumentException("Multiple ranges must contain a termination row.");
                    } 
                }

                // Set range
                var targetTemplate = rangeSourceReportRows
                    .SingleOrDefault(el => string.Equals(el.Key, key));

                if(targetTemplate is not null && 
                   start.HasValue && 
                   count.HasValue) 
                {
                    targetTemplate.SetRange(start.Value, count.Value);
                    outputRanges.Add(targetTemplate);
                }

                // Next step
                currentNode = currentNode!.Next;
            }

            // Return
            rangeSourceReportRows = outputRanges;
        }

        private static bool IsFilterMatch(string filter, object value) =>
            value is not null &&
            !string.IsNullOrEmpty(value.ToString()) &&
            string.Equals(filter.Trim().ToLower(), value.ToString()!.Trim().ToLower());

    #endregion
    }
}
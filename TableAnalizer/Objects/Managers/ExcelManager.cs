//https://www.color-hex.com/color-palette/1054961

using System.Drawing;
using iText.Layout.Element;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

namespace TableAnalizer.Objects.Managers
{
    public class ExcelManager : BaseManager
    {
        #region Fields

        public const string OUTPUT_REPORT_MAINREPORT_NAME = "Отчет по логистике";
        public const string OUTPUT_REPORT_DELAYREPORT_NAME = "Отчет по задержкам";
        public const string OUTPUT_REPORT_OVERRUNSREPORT_NAME = "Отчет по перерасходам";
        public const string OUTPUT_REPORT_ANALYTICS_RERORT_NAME = "Аналитика";
        public const string PAID_SHORTAGES_REPORT_WORKSHEET_NAME = "Претензии";
        public const string RANSOMS_WORKSHEET_NAME = "Выкупы";
        public const string REMAINING_OR_SHIPPED_WORKSHEET_NAME = "TDSheet";
        private const int DEFAULT_TABLE_ROW_HEIGHT = 18;
        private const int DEFAULT_TABLE_COL_WIDTH = 18;
        private const int RECORD_INDEX = 1;
        private const int HEADER_TABLE_ROW_HEIGHT = 120;
        private const int HEADER_FONT_SIZE = 14;
        private const string STRING_OR_INT_CELL_FORMAT = "0";
        private const string DOUBLE_CELL_FORMAT = "0.00";
        private const string DATE_CELL_FORNAT = "dd.MM.yyyy";

        // Default target cell settings
        private static readonly Color defaultTextColor = Color.FromArgb(100, 33, 34, 56);        //rgb(33, 34, 56)           
        private static readonly int defaultTextSize = 12;

        // Accent target cell settings
        private static readonly Color accentTextColor = Color.FromArgb(100, 59, 89, 104);        //rgb(59, 89, 104)8  
        private static readonly int accentTextSize = 15;

        // Summary target cell settings
        private static readonly Color summaryTextColor = Color.FromArgb(100, 79, 79, 94);        //rgb(79, 79, 94) 
        private static readonly int summaryTextSize = 15;

        private static readonly FinalReportCellSettings defaultTargetCellSettings =
            new(textСolor: defaultTextColor, textSize: defaultTextSize, cellFillColor: null, false);
        private static readonly FinalReportCellSettings accentTargetCellSettings =
            new(textСolor: accentTextColor, textSize: accentTextSize, cellFillColor: Color.FromArgb(100, 249, 249, 251), true); //rgb(249, 249, 251)

        private static readonly FinalReportCellSettings summaryTargetCellSettings =
            new(textСolor: summaryTextColor, textSize: summaryTextSize, cellFillColor: null);

        private static string SumFormila = string.Empty;

        // Colors
        private static readonly Color dangerColor = ColorTranslator.FromHtml("#d8675b"); // #d8675b

        private static readonly string SKUNumberColName = "Артикул";

        /// <summary>
        /// Цвет вкладок листов итового отчета.
        /// </summary>
        private static readonly Color finalReportTabColor = SKUManager.StatusColors[SKUSellmentStatus.FullSet];

        /// <summary>
        /// Имя листа клинтского отчета в итоговом файле.
        /// </summary>
        private const string CLIENT_SHEET_NAME = "Клиенский";

        /// <summary>
        /// Имя листа рабочего отчета в итоговом файле.
        /// </summary>
        private const string WORK_SHEET_NAME = "Рабочий";

        /// <summary>
        /// Имена листоы отчетов в итоговом файле.
        /// </summary>
        private static readonly string[] finalWorksheetNames = [CLIENT_SHEET_NAME, WORK_SHEET_NAME];


        #endregion

        #region Properties
        private static Dictionary<SourceReportItemKey, ExcelPackage> ExcelPackages { get; } = [];

        public static Dictionary<ParsingTypeCode, Type> TypeMappingTable { get; } = new()
        {
            { ParsingTypeCode.StringTypeCode, typeof(string) },
            { ParsingTypeCode.IntTypeCode, typeof(int) },
            { ParsingTypeCode.LongTypeCode, typeof(long) },
            { ParsingTypeCode.DoubleTypeCode, typeof(double) },
            { ParsingTypeCode.DataOnlyTypeCode, typeof(DateOnly) },
            { ParsingTypeCode.SKURansomsStatus, typeof(SKURansomsStatus) },
            { ParsingTypeCode.Marketplace, typeof(Marketplace) },
            { ParsingTypeCode.SellmentStatus, typeof(SellmentStatus) }
        };


        public FinalReportCell[] finalReportsCells = [
            // Основная информация
            new (columnName: "Логины", columnWidth: 15, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKULogins),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "МП", columnWidth: 4, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: false, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUmp),
                parsingTypeCode: ParsingTypeCode.Marketplace, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: SKUNumberColName, columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: false, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Бренд товара", columnWidth: 15, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUBrand),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Наименование товара", columnWidth: 50, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKUName),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Средняя цена, руб.", columnWidth: 12, columnFormat: DOUBLE_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0,  сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUPrice),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode,  valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Дата последнего заказа", columnWidth: 13, columnFormat: DATE_CELL_FORNAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKUDate),
                parsingTypeCode: ParsingTypeCode.DataOnlyTypeCode, valueСheckingPredicate: null,typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Статус взаиморасчетов", columnWidth: 18, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SellmentStatusString),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

            // Заказы (выкупы)
            new (columnName: "Заказов: всего", columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKURansomsTotal),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Заказов: отменено",columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKURansomsCanceled),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 4),
            new (columnName: "Заказов: в пути", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90,сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKURansomsOnWay),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Заказов: доставлено на ПВЗ", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKURansomsDelivered),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Заказов: забрано с ПВЗ", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForTargetReport.SKURansomsWithDrawn),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

            // Склад (ожидание и приходы)
            new (columnName: "Склад: Ожидание", columnWidth: 12.0f, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                propertyName:  nameof(SKUForTargetReport.SKURansomsWaiting),
                isCellUsedInClientReport: true, parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "Приход от точек, всего", columnWidth: 11, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUReceiptTotal),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "приход товарами", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUReceiptGoods),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 3),
            new (columnName: "списано с точек (через склад)", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUWriteOffFromPoints),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Комментарий к приходам", columnWidth: 25, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForTargetReport.SKUReceipComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        ];




        // public FinalReportCell[] cellsReportingMain =
        // [
        //     new (colNumber: 19, colName: "Общий взаиморасчет с клиентом", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUSummarySettlement),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),

        //     new (colNumber: 20, colName: "в том числе компенсацией", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUCompensatedTotalToClient),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 21, colName: "от склада", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUPaidShortageWarehouse),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 3),
        //     new (colNumber: 22, colName: "от организации", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUPaidShortageOrganization),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 23, colName: "от организации и склада", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUPaidShortageOrganizationAndWarehouse),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 24, colName: "в том числе товарами", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUSettlementByGoods) ,
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 25, colName: "отгружено товарами", colWidth: 10.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUShipmentGoods) ,
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 4),
        //     new (colNumber: 26, colName: "Комментарий к расходом товарами", colWidth: 25, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUShipmentComment),
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 27, colName: "списано с точек", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUWriteOffFromPoints),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 28, colName: "передано на фулфилмент", colWidth: 7.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUTransfered) ,
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 28, colName: "отгружено пересортом от других арт.", colWidth: 8.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUResort),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 30, colName: "Комментарий к пересортами", colWidth: 25, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUResortsComment),
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 31, colName: "Склад: Остаток товаров", colWidth: 11.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKURemaining),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate:  x => x < 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),

        //     new (colNumber: 32, colName: "Задержка довоза, всего", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUDeliveryDelayTotal),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 33, colName: "Задержка оплаты от организации, товаров", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty,  propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayOrganization),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 6),
        //     new (colNumber: 34, colName: "Задержка оплаты от организация, сумма руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayOrganizationSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 35, colName: "Задержка довоза от точек, товаров", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty,  propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayPoints),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 36, colName: "Задержка довоза от точек, сумма руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayPointsSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 37, colName: "Задержка довоза от склада, товаров", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayWarehouse),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 38, colName: "Задержка довоза от склада, сумма руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayWarehouseSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 39, colName: "Перерасход, кол-во", colWidth: 14, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUOverrunsCount),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 40, colName: "Перерасход, сумма руб.", colWidth: 14, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUOverrunsSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        // ];
        // public FinalReportCell[] cellsReportingDelays =
        // [
        //     new (colNumber: 1, colName: "МП", colWidth: 8, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: false, textRotationInHeader: 0,textCommentInHeader: string.Empty,  propertyName: "SKUmp",
        //         parsingTypeCode: ParsingTypeCode.Marketplace, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 2, colName: SKUNumberColName, colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: false, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUNumber",
        //         parsingTypeCode: ParsingTypeCode.LongTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 3, colName: "Бренд товара", colWidth: 15, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUBrand",
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 4, colName: "Наименование товара", colWidth: 50, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUName",
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 5, colName: "Задержка довоза, всего", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUDeliveryDelayTotal),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: null, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 6, colName: "Задержка оплаты от организация, товаров", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty,  propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayOrganization),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 7, colName: "Задержка оплаты от организация, сумма руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayOrganizationSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => (int)x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 8, colName: "Задержка довоза от точек, товаров", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty,  propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayPoints),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 9, colName: "Задержка довоза от точек, сумма руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayPointsSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => (int)x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 10, colName: "Задержка довоза от склада, товаров", colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayWarehouse),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 11, colName: "Задержка довоза от склада, сумма руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDeliveryDelayWarehouseSum),
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => (int)x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings)
        // ];
        // public FinalReportCell[] cellsReportingOverruns =
        // [
        //     new (colNumber: 1, colName: "МП", colWidth: 8, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: false, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUmp",
        //         parsingTypeCode: ParsingTypeCode.Marketplace, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 2, colName: SKUNumberColName, colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: false, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUNumber",
        //         parsingTypeCode: ParsingTypeCode.LongTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 3, colName: "Бренд товара", colWidth: 15, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUBrand",
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 4, colName: "Наименование товара", colWidth: 50, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUName",
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 5, colName: "Перерасход, товаров", colWidth: 14, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUOverrunsCount",
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 6, colName: "Перерасход, сумма", colWidth: 14, format: DOUBLE_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: "SKUOverrunsSum",
        //         parsingTypeCode: ParsingTypeCode.DoubleTypeCode,valueСheckingPredicate: x => x > 0, typeFormula: null, targetCellSettings: defaultTargetCellSettings)
        // ];
        // public FinalReportCell[] cellsReportingAnalitics =
        // [
        //     new (colNumber: 1, colName: "Логины", colWidth: 15, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKULogins),
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 2, colName: "МП", colWidth: 4, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: false, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUmp),
        //     parsingTypeCode: ParsingTypeCode.Marketplace, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 3, colName: SKUNumberColName, colWidth: 12, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: false, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUNumber),
        //     parsingTypeCode: ParsingTypeCode.LongTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 4, colName: "Номенклатура", colWidth: 50, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUName),
        //     parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 5, colName: "Средняя цена товара, руб.", colWidth: 12, format: DOUBLE_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUPrice),
        //     parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 6, colName: "Склад: Ожидание", colWidth: 12.0f, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKURansomsWaiting),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),

        //     new (colNumber: 7, colName: "Приход от точек, всего", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUReceiptTotal),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 8, colName: "приход товарами", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUReceiptGoods),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 3),
        //     new (colNumber: 9, colName: "списано с точек", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUWriteOffFromPoints),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 10, colName: "Комментарий к приходам", colWidth: 25, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUReceipComment),
        //     parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 11, colName: "Общий взаиморасчет с клиентом", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUSummarySettlement),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),

        //     new (colNumber: 12, colName: "в том числе компенсацией", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUCompensatedTotalToClient),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 13, colName: "от склада", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUPaidShortageWarehouse),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 3),
        //     new (colNumber: 14, colName: "от организации", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUPaidShortageOrganization),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 15, colName: "от организации и склада", colWidth: 7, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUPaidShortageOrganizationAndWarehouse),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 16, colName: "в том числе товарами", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUSettlementByGoods) ,
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
        //     new (colNumber: 17, colName: "отгружено товарами", colWidth: 10.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUShipmentGoods) ,
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 4),
        //     new (colNumber: 18, colName: "передано на фулфилмент", colWidth: 7.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUTransfered) ,
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 19, colName: "отгружено пересортом от других арт.", colWidth: 8.5f, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 90, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUResort),
        //         parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 20, colName: "Комментарий к пересортами", colWidth: 25, format: STRING_OR_INT_CELL_FORMAT,
        //         wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName:  nameof(SKUForTargetReport.SKUResortsComment),
        //         parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 21, colName: "Склад: Остаток по учету", colWidth: 11, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKURemaining),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x < 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),

        //     new (colNumber: 22, colName: $"{SKUNumberColName} параллельных клиентов", colWidth: 10, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.IsParallelClientsSKU),
        //     parsingTypeCode: ParsingTypeCode.BoolTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 23, colName: $"{SKUNumberColName} нормализован", colWidth: 10, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.IsSkuNormalize),
        //     parsingTypeCode: ParsingTypeCode.BoolTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 24, colName: "Долг по оплате компенсации перед клиентом", colWidth: 14, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: string.Empty, propertyName: nameof(SKUForTargetReport.SKUDebtToClientForCompensationSum),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

        //     new (colNumber: 25, colName: "Нужны входящие пересорты прихода", colWidth: 13, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: "Приход меньше ожидания", propertyName: nameof(SKUForTargetReport.UnderReceipt),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 26, colName: "Возможны исходящие пересорты прихода", colWidth: 13, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: "Приход больше ожидания", propertyName: nameof(SKUForTargetReport.OverReceipt),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 27, colName: "Нужны входящие пересорты расхода", colWidth: 13, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: "Взаиморасчет меньше ожидания", propertyName: nameof(SKUForTargetReport.UnderMutualSettlement),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        //     new (colNumber: 28, colName: "Возможны исходящие пересорты расхода", colWidth: 13, format: STRING_OR_INT_CELL_FORMAT,
        //     wrapTextInHeader: true, textRotationInHeader: 0, textCommentInHeader: "Взаиморасчет больше ожидания", propertyName: nameof(SKUForTargetReport.OverMutualSettlement),
        //     parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        // ];

        private readonly int sellmentStatusColomnNum;

        #endregion

        #region Constructors && destructors
        public ExcelManager(IConfiguration config) : base(config)
        {
            ExcelPackage.License.SetNonCommercialPersonal("vegiwoo");

            // Найти номер столбца со статусом взаиморасчетов
            sellmentStatusColomnNum = GetColumnNumber(nameof(SKUForTargetReport.SellmentStatusString));
        }
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

        /// <summary>
        /// Определяет номер колонки таблицы Excel с итоговым отчетом из коллекции FinalReportCell по названию свойства.
        /// </summary>
        /// <param name="propertyName">Название свойства для определения номера колонки в таблице.</param>
        /// <returns>Номер колонки.</returns>
        private int GetColumnNumber(string propertyName)
        {
            FinalReportCell cell = finalReportsCells.SingleOrDefault(el => el.PropertyName == propertyName);
            return Array.IndexOf(finalReportsCells, cell) + 1;
        }

        protected override void AddConfigVariables()
        {
            // Set formulaes variables
            IConfigurationSection? formulaesSessions = config!.GetSection("Formulas");
            SumFormila = formulaesSessions.GetValue<string>("SumFormula");
        }

        // <summary>
        // Creates an Excel workbook using specified FileInfo.
        // </summary>
        // <param name="key">Specified key.</param>
        // <param name="fileInfo">Specified FileInfo.</param>
        // <returns>Existing ExcelPackage (optional).</returns>
        public ExcelPackage? CreateExcelPackage(SourceReportItemKey key, FileInfo fileInfo)
        {
            if (!GetExcelPackageByKey(key, out ExcelPackage? package) && package is null)
            {
                ExcelPackage? excelPackage;

                try
                {
                    excelPackage = new(fileInfo, false);
                }
                catch (System.Exception ex)
                {
                    ManagerReachedEventArgs args = new($"Error creating ExcelPackage: {ex.Message}\nKey: {key}\nFile: {fileInfo.FullName}", true, true);
                    OnManagerReached(args);
                    throw;
                }

                if (excelPackage is not null)
                    ExcelPackages[key] = excelPackage;
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
        public bool GetExcelPackageByKey(SourceReportItemKey key, out ExcelPackage? package)
        {
            try
            {
                return ExcelPackages.TryGetValue(key, out package) && package is not null;
            }
            catch (Exception ex)
            {
                package = null;
                OnManagerReached(new($"{ex.Message}", true, true));
                return false;
            }
        }

        private Task<bool> CreateFinalReportTask(in ExcelPackage outputPackage, FinalReportCell[] finalDataCells,
            List<List<int>>? rangeForGrouping, List<SKUForTargetReport> actualData, bool isTracing)
        {
            foreach (var finalWorksheetName in finalWorksheetNames)
            {
                CreateExcelWorkSheet(in outputPackage, finalWorksheetName, out ExcelWorksheet? outputReportSheet);

                FinalReportCell[] fDataCells = finalWorksheetName switch
                {
                    CLIENT_SHEET_NAME => [.. finalDataCells.Where(fdt => fdt.IsCellUsedInClientReport == true)],
                    _ => finalDataCells,
                };

                try
                {
                    FillingСellsWithData(ref outputReportSheet, actualData, fDataCells, isTracing);
                }
                catch (Exception ex)
                {
                    OnManagerReached(new($"{ex.Message}", true, true));
                    return Task.Run(() => false);
                }

                SetupTableHeader(in fDataCells, ref outputReportSheet);
                AddFilterAndAccentInWorkSheet(ref outputReportSheet, in rangeForGrouping);
                CalculateFormulas(in fDataCells, ref outputReportSheet);
            }

            return Task.Run(() => true);
        }

        private void CalculateFormulas(in FinalReportCell[] targetCells, ref ExcelWorksheet outputReportSheet)
        {
            int firstRow = outputReportSheet.Dimension.Start.Row + 1;
            int endRow = outputReportSheet.Dimension.End.Row;

            IEnumerable<(int ColNumber, TypesFormulas? TypeFormula)> cellColumns =
                from cell in targetCells
                where cell.TypeFormula is not null
                select (GetColumnNumber(cell.PropertyName), cell.TypeFormula);

            foreach (var (ColNumber, TypeFormula) in cellColumns)
            {
                var cellAddress = GetCellsAddress(ColNumber, firstRow, ColNumber, endRow);
                var formula = string.Empty;

                switch (TypeFormula)
                {
                    case TypesFormulas.Sum:
                        formula = $"{SumFormila}({cellAddress})";
                        break;
                    case null:
                        break;
                }

                if (!string.IsNullOrEmpty(formula))
                {
                    var range = outputReportSheet.Cells[endRow + 1, ColNumber];
                    range.Formula = formula;

                    range.Style.Font.Size = summaryTargetCellSettings.FontSize;
                    range.Style.Font.Bold = true;
                    range.Style.Font.Color.SetColor(summaryTargetCellSettings.TextСolor);

                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Color.SetColor(summaryTargetCellSettings.TextСolor);
                }
            }

            outputReportSheet.Calculate();
        }

        /// <summary>
        /// Returns name of sheet in Excel workbook.
        /// </summary>
        /// <param name="reportFileKey">Report file key.</param>
        /// <param name="workSheetNames">Collection of Excel workbook sheet names.</param>
        /// <returns>Boolean operation execution flag.</returns>
        public static bool GetExcelWorksheetStringNamesByKey(SourceReportItemKey reportFileKey, out List<string>? workSheetNames)
        {
            workSheetNames = reportFileKey switch
            {
                SourceReportItemKey.Ransoms => [RANSOMS_WORKSHEET_NAME],
                SourceReportItemKey.Balance or SourceReportItemKey.OutgoingProducts
                    or SourceReportItemKey.Transfered
                    or SourceReportItemKey.WriteOffPoints
                    or SourceReportItemKey.WriteOffPointsViaWarehouseShortage
                    or SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment => [REMAINING_OR_SHIPPED_WORKSHEET_NAME],
                SourceReportItemKey.Additional => [ExcelSheetParsingManager.ADDREPORT_ALIASES_WORKSHEET_NAME, ExcelSheetParsingManager.ADDREPORT_WRITEOFF_ADJUSTMENTS_WORKSHEET_NAME],
                SourceReportItemKey.ShortagesPaid => [PAID_SHORTAGES_REPORT_WORKSHEET_NAME],
                _ => null,
            };
            return workSheetNames is not null && workSheetNames.Count != 0;
        }

        /// <summary>
        /// Создает и сохраняет итоговые отчеты.
        /// </summary>
        /// <param name="skuForTargetReports">Коллекция SKUForTargetReport для формирования итоговых отчетов.</param>
        /// <param name="isTracing">Флаг трассировки.</param>
        /// <returns>Флаг выполнения операции.</returns>
        public async Task<bool> CreateAndSaveFinalReports(List<SKUForTargetReport> skuForTargetReports, bool isTracing)
        {
            // Проверка указнания пути для сохранения файлов.
            if (string.IsNullOrEmpty(ReportFileManager.FinalReportFileName))
                OnManagerReached(new("Путь для сохранения файлов итогового отчета не может быть пустым", true, true));

            // Создание книги Excel (ExcelPackage) для сохранения файлав.
            ExcelPackage? finalReportsBook = CreateFinalExcelPackage(ReportFileManager.FinalReportFileName);
            if (finalReportsBook == null)
                return false;

            List<SKUForTargetReport> actualData = [.. skuForTargetReports];

            // Получение диапазонов для группировки столбцов.
            List<List<int>>? rangeForGrouping = GetRangeForGrouping(in finalReportsCells)?.ToList();

            // Создание отчета.
            var finalReportTask = CreateFinalReportTask(in finalReportsBook, finalReportsCells, rangeForGrouping, actualData, isTracing);
            await Task.Run(() => finalReportTask);

            // Сохранение.
            return SaveWorkProcedure(isTracing, ref finalReportsBook);
        }


        /// <summary>
        /// Создание объекта книги Excel для итогового отчета.
        /// </summary>
        /// <param name="path">Путь для сохранения файлов итогового отчета.</param>
        /// <returns>Объект ExcelPackage или null.</returns>
        private ExcelPackage? CreateFinalExcelPackage(string path)
        {
            FileInfo? fileInfo = new(path);
            if (fileInfo == null)
                OnManagerReached(new($"По пути {path}\n не может быть создан объект FileInfo!", true, true));

            var outputPackage = new ExcelPackage(new FileInfo(ReportFileManager.FinalReportFileName));
            if (outputPackage is null)
                OnManagerReached(new($"По пути {fileInfo?.FullName}\n не может быть создан объект ExcelPackage!", true, true));

            return outputPackage;
        }

        private bool SaveWorkProcedure(bool isTracing, ref ExcelPackage outputPackage)
        {
            // Save work
            if (isTracing)
            {
                ManagerReachedEventArgs args = new($"Saving the work done...", false, false);
                OnManagerReached(args);
            }
            var isSaveWork = false;
            if (SaveReceivedData(ReportFileManager.FinalReportFileName, ref outputPackage))
            {
                isSaveWork = true;
                if (isTracing)
                {
                    ManagerReachedEventArgs args = new($"Saving the new Excel workbook to the specified path is complete.", false, false);
                    OnManagerReached(args);
                }
            }

            outputPackage.Dispose();
            return isSaveWork;
        }

        private static List<SKUForTargetReport> GetActualDataForTargetReportBy(TargetReportType targetReportType, List<SKUForTargetReport> sourceData)
        {
            switch (targetReportType)
            {
                case TargetReportType.Delay:
                    return GetDelay(sourceData);

                case TargetReportType.Overruns:
                    return GetOverruns(sourceData);

                // case TargetReportType.Analytics:

                //     var delays =  GetDelay(sourceData).ToList();
                //     var overruns = GetOverruns(sourceData).ToList();
                //     var lessReceiptThanWaiting = GetLessReceiptThanWaiting(sourceData).ToList();
                //     var greatReceiptThanWaiting = GetGreatReceiptThanWaiting(sourceData).ToList();

                //    return [.. delays
                //         .UnionBy(overruns, p => p.SKUNumber)
                //         .UnionBy(lessReceiptThanWaiting, p => p.SKUNumber)
                //         .UnionBy(greatReceiptThanWaiting, p => p.SKUNumber)];

                default:
                    return [.. sourceData];
            }
        }

        /// <summary>
        /// Создает диапазоны для группировки столбцов в таблице итовового отчета.
        /// </summary>
        /// <param name="cells">Коллекция объектов FinalReportCell.</param>
        /// <returns>Перечисление диапазонов (списков целочисленных значений).</returns>
        private IEnumerable<List<int>>? GetRangeForGrouping(in FinalReportCell[] cells)
        {
            return from cell in cells
                   where cell.GroupCellsFromCurrentCount != 0
                   let range = Enumerable.Range(GetColumnNumber(cell.PropertyName), cell.GroupCellsFromCurrentCount)
                   select range.ToList();
        }

        /// <summary>
        /// Заполняет ячейки таблицы рабочего листа Excel данными из коллекции SKUForTargetReport.
        /// </summary>
        /// <param name="sheet">Рабочий лист Excel.</param>
        /// <param name="skusForTargetReport">Коллекция данных.</param>
        /// <param name="finalReportCells">Целевые ячейки итового отчета.</param>
        /// <param name="isTracing">Флаг трассировки.</param>
        /// <param name="firstRowIndex">Индекс первой строки заполнения.</param>
        /// <exception cref="Exception"></exception>
        public void FillingСellsWithData(ref ExcelWorksheet sheet, List<SKUForTargetReport> skusForTargetReport, FinalReportCell[] finalReportCells, bool isTracing, int firstRowIndex = 1)
        {
            if (isTracing)
                OnManagerReached(new($"Заполняю данными лист '{sheet.Name}'...", false, false));

            int rowNumber = firstRowIndex + 1;
            foreach (var skuForTargetReport in skusForTargetReport)
            {
                foreach (var finalReportCell in finalReportCells)
                {
                    GetValueForFinalReport(skuForTargetReport, finalReportCell, out object? value);

                    if (value == null)
                    {
                        OnManagerReached(new($"При заполнении данные на листе значение для колонки '{finalReportCell.ColumnName}' оказалось пустым!", true, true));
                        continue;
                    }

                    var columnNumber = GetColumnNumber(finalReportCell.PropertyName);
                    ExcelRange? tableCell = sheet.Cells[rowNumber, columnNumber];

                    if (tableCell == null)
                        OnManagerReached(new($"В ячейке таблицы (строка {rowNumber} && column number {columnNumber} равно null.", true, true));


                    ValueAndSettingsToTableCell(ref tableCell!, value, finalReportCell, skuForTargetReport.FillColor, skuForTargetReport.SKUAliasesComment);
                }
                rowNumber++;
            }

            if (isTracing)
            {
                ManagerReachedEventArgs args = new($"Excel sheet named '{sheet.Name}'is filled with data.", false, false);
                OnManagerReached(args);
            }
        }

        /// <summary>
        /// Получает значение для заполнение ячейки итогового отчета. 
        /// </summary>
        /// <param name="skuForTargetReport">Объект SKUForTargetReport для получение свойства.</param>
        /// <param name="finalReportCell">Прототип ячейки заполнения.</param>
        /// <param name="value">Полученное значение (опционально).</param>
        private void GetValueForFinalReport(in SKUForTargetReport skuForTargetReport, FinalReportCell finalReportCell, out object? value)
        {
            value = null;

            switch (finalReportCell.ParsingTypeCode)
            {
                case ParsingTypeCode.IntTypeCode:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out int intValue))
                        value = intValue;
                    break;

                case ParsingTypeCode.LongTypeCode:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out long longValue))
                        value = longValue;
                    break;
                case ParsingTypeCode.DoubleTypeCode:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out double doubleValue))
                        value = doubleValue;
                    break;
                case ParsingTypeCode.DataOnlyTypeCode:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out DateOnly dateOnlyValue))
                        value = dateOnlyValue.ToDateTime(new TimeOnly());
                    break;
                case ParsingTypeCode.StringTypeCode:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out string? stringValue))
                        value = stringValue ?? null;
                    break;
                case ParsingTypeCode.SKURansomsStatus:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out SKURansomsStatus ransomsStatusValue))
                        value = ransomsStatusValue;
                    break;
                case ParsingTypeCode.Marketplace:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out Marketplace? marketplaceValue))
                        value = marketplaceValue;
                    break;
                case ParsingTypeCode.SellmentStatus:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out SellmentStatus? sellmentStatus))
                        value = sellmentStatus;
                    break;
                case ParsingTypeCode.BoolTypeCode:
                    if (TryGetPropertyValue(skuForTargetReport, finalReportCell.PropertyName, out bool? boolean))
                        value = boolean;
                    break;
                default:
                    OnManagerReached(new("ParsingTypeCode для заполнения ячейки не определен!", true, true));
                    break;
            }
        }

        /// <summary>
        /// Установка значения и настроек для ячейки.
        /// </summary>
        /// <param name="tableCell">Целевая ячейка таблицы Excel.</param>
        /// <param name="value">Значение для ячейки.</param>
        /// <param name="finalReportCell">Объект FinalReportCell для настройки ячейки.</param>
        /// <param name="statusColor">Цвет статуса.</param>
        /// <param name="aliasesComment">Комментарий?</param>
        private void ValueAndSettingsToTableCell(ref ExcelRange tableCell, object? value, in FinalReportCell finalReportCell, Color? statusColor, string? aliasesComment = null)
        {
            // Установка значения
            tableCell.Value = value;

            // Установка цвета для колонки статуса
            if (tableCell.Start.Column == sellmentStatusColomnNum && statusColor != null)
                tableCell.Style.Fill.SetBackground((Color)statusColor);

            // Установка формата значения для ячейки.
            tableCell.Style.Numberformat.Format = finalReportCell.ColumnFormat;

            // Установка выравнивания текста в ячейке.
            tableCell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;

            // Назначение стиля и заливка ячейки цветом.  
            Color? color = finalReportCell.CellSettings.CellFillColor;
            if (finalReportCell.CellSettings.ExcelFillStyle == ExcelFillStyle.Solid)
            {
                if (finalReportCell.CellSettings.CellFillColor is null)
                    OnManagerReached(new($"Для заливки ячеек не назначен параметр {nameof(finalReportCell.CellSettings.CellFillColor)}.", true, true));

                tableCell.Style.Fill.PatternType = finalReportCell.CellSettings.ExcelFillStyle;

                if (color != null)
                {
                    tableCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    tableCell.Style.Fill.BackgroundColor.SetColor((Color)color);
                }
            }

            // Применение настроек шрифта. 
            if (int.TryParse(tableCell.Value?.ToString(), out int v) && finalReportCell.ValueСheckingPredicate is not null && finalReportCell.ValueСheckingPredicate(v))
            {
                tableCell.Style.Font.Size = finalReportCell.CellSettings.FontSize;
                tableCell.Style.Font.Bold = true;
                tableCell.Style.Font.Color.SetColor(dangerColor);
            }
            else
            {
                tableCell.Style.Font.Size = finalReportCell.CellSettings.FontSize;
                tableCell.Style.Font.Bold = finalReportCell.CellSettings.IsTextBold;
                tableCell.Style.Font.Color.SetColor(finalReportCell.CellSettings.TextСolor);
            }

            // Добавление комментария к ячейке.
            bool isAliasComment = finalReportCell.ColumnName.Trim().Equals(SKUNumberColName.Trim(), StringComparison.CurrentCultureIgnoreCase) && aliasesComment != null && !string.IsNullOrEmpty(aliasesComment);
            if (isAliasComment)
            {
                ExcelComment comm = tableCell.AddComment("");
                comm.Font.Color = Color.Black;
                comm.Font.Bold = true;

                var message = comm.RichText.Add(aliasesComment);
                message.Color = Color.Black;
                message.Bold = false;
                message.Size = tableCell.Style.Font.Size - 1;

                comm.VerticalAlignment = OfficeOpenXml.Drawing.Vml.eTextAlignVerticalVml.Top;
                comm.HorizontalAlignment = OfficeOpenXml.Drawing.Vml.eTextAlignHorizontalVml.Left;

                comm.AutoFit = true;
            }
        }

        /// <summary>
        /// Saves received data to an Excel workbook file.
        /// </summary>
        /// <param name="path">Directory to save file.</param>
        /// <remarks>oct 2025 ver.</remarks>
        public static bool SaveReceivedData(string path, ref ExcelPackage package)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.Write(package.GetAsByteArray());
            }

            return File.Exists(path);
        }
        // TODO: Вынеси в FileService!

        /// <summary>
        /// Создание нового листа Excel.
        /// </summary>
        /// <param name="package">Книга Excel.</param>
        /// <param name="workSheetName">Имя нового листа.</param>
        /// <param name="worksheet">Созданный лист Excel.</param>
        public static void CreateExcelWorkSheet(in ExcelPackage package, string workSheetName, out ExcelWorksheet worksheet)
        {
            // Удаление существующего листа.
            var existingWorksheet = package.Workbook.Worksheets.SingleOrDefault(x => x.Name == workSheetName);
            if (existingWorksheet is not null) package.Workbook.Worksheets.Delete(existingWorksheet);

            // Создание нового листа.
            worksheet = package.Workbook.Worksheets.Add(workSheetName);

            // Стилизация нового листа.
            worksheet.TabColor = finalReportTabColor;
            worksheet.DefaultRowHeight = DEFAULT_TABLE_ROW_HEIGHT;
            worksheet.DefaultColWidth = DEFAULT_TABLE_COL_WIDTH;
        }

        private void SetupTableHeader(in FinalReportCell[] resultCells, ref ExcelWorksheet worksheet, double headerRowHeight = HEADER_TABLE_ROW_HEIGHT)
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
                var columnNumber = GetColumnNumber(currentResultCell.PropertyName);

                ExcelColumn columnInWorkSheet = worksheet.Column(columnNumber) ??
                    throw new Exception($"Column number {columnNumber} not found on worksheet");

                ExcelRange firstRowCell = worksheet.Cells[RECORD_INDEX, columnNumber] ?? throw new Exception($"Cell at address {RECORD_INDEX} {columnNumber} not found on worksheet");

                // Add name of column
                firstRowCell.Value = currentResultCell.ColumnName;

                // Rotation text in header 
                firstRowCell.Style.TextRotation = currentResultCell.DegreeRotationTextInHeader;
                columnInWorkSheet.Width = currentResultCell.ColumnWidth;
                columnInWorkSheet.Style.Numberformat.Format = currentResultCell.ColumnFormat;
                columnInWorkSheet.Style.WrapText = currentResultCell.IsWrapTextInHeader;

                // Add comment in header cell 
                if (!string.IsNullOrEmpty(currentResultCell.CommentTextInHeader))
                {
                    var comment = firstRowCell.AddComment("Анализ отчета:\r\n");
                    comment.Font.Color = Color.Black;
                    comment.Font.Bold = true;

                    var message = comment.RichText.Add(currentResultCell.CommentTextInHeader);
                    message.Color = Color.Black;
                    message.Bold = false;
                    message.Size = firstRowCell.Style.Font.Size - 1;

                    comment.VerticalAlignment = OfficeOpenXml.Drawing.Vml.eTextAlignVerticalVml.Top;
                    comment.HorizontalAlignment = OfficeOpenXml.Drawing.Vml.eTextAlignHorizontalVml.Left;

                    comment.AutoFit = true;
                }
            }
        }

        private static string GetCellsAddress(int firstColumn, int firstRow, int endColumn, int endRow) =>
            $"{ExcelCellAddress.GetColumnLetter(firstColumn)}{firstRow}:{ExcelCellAddress.GetColumnLetter(endColumn)}{endRow}";

        private static void AddFilterAndAccentInWorkSheet(ref ExcelWorksheet excelWorksheet, in List<List<int>>? rangesForGrouping)
        {
            int firstRow = excelWorksheet.Dimension.Start.Row;
            int endRow = excelWorksheet.Dimension.End.Row;

            int firstCol = excelWorksheet.Dimension.Start.Column;
            int endCol = excelWorksheet.Dimension.End.Column;

            var cellAddress = GetCellsAddress(firstCol, firstRow, endCol, endRow);
            excelWorksheet.Cells[cellAddress].AutoFilter = true;

            //Column Group
            excelWorksheet.OutLineSummaryBelow = false;
            excelWorksheet.View.FreezePanes(2, 4);

            // 
            if (rangesForGrouping is not null && rangesForGrouping.Count > 0)
            {
                foreach (var rangeForGrouping in rangesForGrouping)
                {
                    ExcelRangeColumn? excelRangeColumn = excelWorksheet.Columns[rangeForGrouping.First(), rangeForGrouping.Last()];
                    excelRangeColumn.Group();
                    excelRangeColumn.OutlineLevel = 1;
                }
            }
        }

        /// <summary>
        /// Returns a Excel worksheet in an Excel workbook by its name.
        /// </summary>
        /// <param name="name">Name of Excel worksheet to search for.</param>
        /// <param name="key">Key for searching ExcelPackage in internal dictionary.</param>
        /// <param name="worksheet">Found Excel worksheet or null.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public static async Task<ExcelWorksheet?> GetExcelWorksheetByName(string name, ExcelPackage excelPackage)
        {
            await Task.Delay(1);
            return excelPackage.Workbook.Worksheets.SingleOrDefault(ws => ws.Name == name);
        }

        private static List<SKUForTargetReport> GetDelay(List<SKUForTargetReport> list)
        {
            var items = from el in list
                        where el.SKUDeliveryDelayTotal > 0 &&
                            (el.SellmentStatus.Status is SKUSellmentStatus.DelayDeliveryFromPoints ||
                            el.SellmentStatus.Status is SKUSellmentStatus.DelayFromWarehouse ||
                            el.SellmentStatus.Status is SKUSellmentStatus.DelayFromPointsAndWarehouse ||
                            el.SellmentStatus.Status is SKUSellmentStatus.DelayFromOrganization ||
                            el.SellmentStatus.Status is SKUSellmentStatus.DelayFromOrganizationAndWarehouse)
                        select el;

            return [.. items];
        }

        private static List<SKUForTargetReport> GetOverruns(List<SKUForTargetReport> list)
        {
            var items = from el in list
                        where el.SKUOverrunsSum > 0.00
                        select el;

            return [.. items];
        }
        #endregion
    }
}
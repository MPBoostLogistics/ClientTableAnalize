//https://www.color-hex.com/color-palette/1054961

using System.Drawing;
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

        public FinalReportCell[] finalReportsCells = [

            // Основная информация
            new (columnName: "Логины", columnWidth: 15, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKULogins),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "МП", columnWidth: 4, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: false, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUmp),
                parsingTypeCode: ParsingTypeCode.Marketplace, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: SKUNumberColName, columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: false, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Бренд товара", columnWidth: 15, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUBrand),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Наименование товара", columnWidth: 50, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUName),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Средняя цена, руб.", columnWidth: 12, columnFormat: DOUBLE_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0,  сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUPrice),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode,  valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Дата последнего заказа", columnWidth: 13, columnFormat: DATE_CELL_FORNAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUDate),
                parsingTypeCode: ParsingTypeCode.DataOnlyTypeCode, valueСheckingPredicate: null,typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Статус взаиморасчетов", columnWidth: 18, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SellmentStatusString),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

            // Заказы (выкупы)
            new (columnName: "Заказов: всего", columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKURansomsTotal),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Заказов: отменено",columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKURansomsCanceled),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 4),
            new (columnName: "Заказов: в пути", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90,сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKURansomsOnWay),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Заказов: доставлено на ПВЗ", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKURansomsDelivered),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Заказов: забрано с ПВЗ", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKURansomsWithDrawn),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

            // Склад (ожидание и приходы)
            new (columnName: "К взаиморасчету с Клиентом, товаров", columnWidth: 12.0f, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: "Всего выкупов минус Отменено минус В пути",
                propertyName:  nameof(SKUForFinalReport.SKURansomsWaiting),
                isCellUsedInClientReport: true, parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "Приход от точек, всего", columnWidth: 11, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader:  true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUReceiptTotal),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "приход товарами", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUReceiptGoods),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 3),
            new (columnName: "списано с точек и курьеров", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUWriteOffFromPointsAndCouriers),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Комментарий к приходам", columnWidth: 25, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUReceipComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

            // Взаиморасчет с клиентом
            new (columnName: "Общий взаиморасчет с Клиентом", columnWidth: 11, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUSummarySettlement),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "в том числе компенсацией", columnWidth: 11, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUCompensatedTotalToClient),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "от склада", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName:  nameof(SKUForFinalReport.SKUPaidShortageWarehouse),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 3),
            new (columnName: "от организации", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName:  nameof(SKUForFinalReport.SKUPaidShortageOrganization),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "от организации и склада", columnWidth: 7, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName:  nameof(SKUForFinalReport.SKUPaidShortageOrganizationAndWarehouse),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "в том числе товарами", columnWidth: 11, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUSettlementByGoods) ,
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "отгружено товарами", columnWidth: 10.5f, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUShipmentGoods) ,
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 4),
            new (columnName: "Комментарий к расходу товарами", columnWidth: 25, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUShipmentComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "передано  через фулфилмент", columnWidth: 7.5f, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName: nameof(SKUForFinalReport.SKUTransfered) ,
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "отгружено пересортом от/на др. арт.", columnWidth: 8.5f, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 90, сommentTextInHeader: "Автоматические пересорты,\nсделанные программой\nсчитаются если по учету\n одних товаров отдано больше,\nа других меньше",
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUResort),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: null, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Комментарий к пересортам от/на др. арт", columnWidth: 25, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUResortsComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

            // Остатки товаров
            new (columnName: "Склад: Остаток товаров", columnWidth: 11.5f, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKURemaining),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate:  x => x < 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),

            // Задержка взаиморасчетов
            new (columnName: "Общая задержка по взаиморасчетам, товаров", columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: true, propertyName:  nameof(SKUForFinalReport.SKUDeliveryDelayTotal),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "Задержка оплаты от организации, товаров", columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDeliveryDelayOrganization),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings, groupCellsFromCurrent: 6),
            new (columnName: "Задержка оплаты от организации, сумма", columnWidth: 12, columnFormat: DOUBLE_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDeliveryDelayOrganizationSum),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Задержка довоза товаров от точек, товаров", columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDeliveryDelayPoints),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Задержка довоза товаров от точек, сумма", columnWidth: 12, columnFormat: DOUBLE_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDeliveryDelayPointsSum),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Задержка довоза товаров от склада, товаров", columnWidth: 12, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDeliveryDelayWarehouse),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Задержка довоза товаров от склада, сумма", columnWidth: 12, columnFormat: DOUBLE_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDeliveryDelayWarehouseSum),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

            // Аналитика 
            new (columnName: "Перерасход, товаров", columnWidth: 14, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName:  nameof(SKUForFinalReport.SKUOverrunsCount),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: accentTargetCellSettings),
            new (columnName: "Перерасход, сумма", columnWidth: 14, columnFormat: DOUBLE_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName:  nameof(SKUForFinalReport.SKUOverrunsSum),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),


            new (columnName: $"{SKUNumberColName} нормализован", columnWidth: 10, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.IsSkuNormalize),
                parsingTypeCode: ParsingTypeCode.BoolTypeCode, valueСheckingPredicate: null, typeFormula: null, targetCellSettings: defaultTargetCellSettings),

            new (columnName: "Долг по оплате компенсации перед клиентом", columnWidth: 14, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: string.Empty,
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.SKUDebtToClientForCompensationSum),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),

            // Аналитика пр пересортам приходв
            new (columnName: "Нужны входящие пересорты прихода", columnWidth: 13, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: "Приход меньше ожидания",
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.UnderReceipt),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Возможны исходящие пересорты прихода", columnWidth: 13, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: "Приход больше ожидания",
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.OverReceipt),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Нужны входящие пересорты расхода", columnWidth: 13, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: "Взаиморасчет меньше ожидания",
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.UnderMutualSettlement),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
            new (columnName: "Возможны исходящие пересорты расхода", columnWidth: 13, columnFormat: STRING_OR_INT_CELL_FORMAT,
                isWrapTextInHeader: true, degreeRotationTextInHeader: 0, сommentTextInHeader: "Взаиморасчет больше ожидания",
                isCellUsedInClientReport: false, propertyName: nameof(SKUForFinalReport.OverMutualSettlement),
                parsingTypeCode: ParsingTypeCode.IntTypeCode, valueСheckingPredicate: x => x > 0, typeFormula: TypesFormulas.Sum, targetCellSettings: defaultTargetCellSettings),
        ];

        private readonly int sellmentStatusColomnNum;

        #endregion

        #region Constructors && destructors
        public ExcelManager(IConfiguration config) : base(config)
        {
            ExcelPackage.License.SetNonCommercialPersonal("vegiwoo");

            // Найти номер столбца со статусом взаиморасчетов
            sellmentStatusColomnNum = GetColumnNumberForFinalReport(nameof(SKUForFinalReport.SellmentStatusString), true);
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
        private int GetColumnNumberForFinalReport(string propertyName, bool isClentsReport)
        {
            FinalReportCell[] data = isClentsReport ?
                [.. finalReportsCells.Where(frs => frs.IsCellUsedInClientReport == true)] :
                finalReportsCells;

            var index = Array.FindIndex(data, c => c.PropertyName == propertyName);

            return index != -1 ? index + 1 : 0;
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

        private Task<bool> CreateFinalReportTask(in ExcelPackage outputPackage, FinalReportCell[] finalDataCells, List<SKUForFinalReport> actualData, bool isTracing)
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
                List<List<int>>? rangeForGrouping = GetRangeForGrouping(in finalReportsCells, finalWorksheetName)?.ToList();
                AddFilterAndAccentInWorkSheet(ref outputReportSheet, in rangeForGrouping);
                CalculateFormulas(in fDataCells, ref outputReportSheet);
            }

            return Task.Run(() => true);
        }

        private void CalculateFormulas(in FinalReportCell[] targetCells, ref ExcelWorksheet outputReportSheet)
        {
            int firstRow = outputReportSheet.Dimension.Start.Row + 1;
            int endRow = outputReportSheet.Dimension.End.Row;

            var sheetName = outputReportSheet.Name;

            IEnumerable<(int ColNumber, TypesFormulas? TypeFormula)> cellColumns =
                from cell in targetCells
                where cell.TypeFormula is not null
                select (GetColumnNumberForFinalReport(cell.PropertyName, sheetName == CLIENT_SHEET_NAME), cell.TypeFormula);

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
        public async Task<bool> CreateAndSaveFinalReports(List<SKUForFinalReport> skuForTargetReports, bool isTracing)
        {
            // Проверка указнания пути для сохранения файлов.
            if (string.IsNullOrEmpty(ReportFileManager.FinalReportFileName))
                OnManagerReached(new("Путь для сохранения файлов итогового отчета не может быть пустым", true, true));

            // Создание книги Excel (ExcelPackage) для сохранения файлав.
            ExcelPackage? finalReportsBook = CreateFinalExcelPackage(ReportFileManager.FinalReportFileName);
            if (finalReportsBook == null)
                return false;

            List<SKUForFinalReport> actualData = [.. skuForTargetReports];

            // Создание отчета.
            var finalReportTask = CreateFinalReportTask(in finalReportsBook, finalReportsCells, actualData, isTracing);
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

                OnManagerReached(new($"Saving the work done...", false, false));

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

        private static List<SKUForFinalReport> GetActualDataForTargetReportBy(TargetReportType targetReportType, List<SKUForFinalReport> sourceData)
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
        /// <param name="finalReportCells">Коллекция объектов FinalReportCell.</param>
        /// <returns>Перечисление диапазонов (списков целочисленных значений).</returns>
        private IEnumerable<List<int>>? GetRangeForGrouping(in FinalReportCell[] finalReportCells, string sheetName)
        {
            var data = sheetName == CLIENT_SHEET_NAME ?
                finalReportCells.Where(c => c.IsCellUsedInClientReport == true).ToArray() :
                finalReportCells;

            return from finalReportCell in data
                   where finalReportCell.GroupCellsFromCurrentCount != 0
                   let range = Enumerable.Range(GetColumnNumberForFinalReport(finalReportCell.PropertyName, sheetName == CLIENT_SHEET_NAME), finalReportCell.GroupCellsFromCurrentCount)
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
        public void FillingСellsWithData(ref ExcelWorksheet sheet, List<SKUForFinalReport> skusForTargetReport, FinalReportCell[] finalReportCells, bool isTracing, int firstRowIndex = 1)
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

                    var columnNumber = GetColumnNumberForFinalReport(finalReportCell.PropertyName, sheet.Name == CLIENT_SHEET_NAME);
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
        private void GetValueForFinalReport(in SKUForFinalReport skuForTargetReport, FinalReportCell finalReportCell, out object? value)
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
                var columnNumber = GetColumnNumberForFinalReport(currentResultCell.PropertyName, worksheet.Name == CLIENT_SHEET_NAME);

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

        private static List<SKUForFinalReport> GetDelay(List<SKUForFinalReport> list)
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

        private static List<SKUForFinalReport> GetOverruns(List<SKUForFinalReport> list)
        {
            var items = from el in list
                        where el.SKUOverrunsSum > 0.00
                        select el;

            return [.. items];
        }
        #endregion
    }
}
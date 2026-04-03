//https://www.color-hex.com/color-palette/27606

using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.Repositories;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalance;
using TableAnalizer.Objects.SKUItems.SKUBalances;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

namespace TableAnalizer.Objects.Managers
{
    public class SKUManager(IConfiguration config, int deadLineDays, ExcelManager excelManager, ExcelSheetParsingManager excelSheetsManager) : BaseManager(config)
    {
        #region Variables and constants 
        private const bool IS_REMOVE_BRAND_FROM_NAME = false;
        private readonly ExcelManager excelManager = excelManager;
        private readonly ExcelSheetParsingManager excelSheetsManager = excelSheetsManager;
        public static Dictionary<SKUSellmentStatus, string> StatusStrings = [];

        /// <summary>
        /// Сollection of colors to indicate status of mutual settlements with clients.
        /// </summary>
        /// <remarks>https://www.color-hex.com/color-palette/2922</remarks>
        public static Dictionary<SKUSellmentStatus, Color> StatusColors { get; } = new()
        {
            {SKUSellmentStatus.FullSet, ColorTranslator.FromHtml("#e9f9d6")},                             // #e6f6eb
            {SKUSellmentStatus.InWork, ColorTranslator.FromHtml("#fffac4")},                              // #fff6f0
            {SKUSellmentStatus.DelayFromPointsAndWarehouse, ColorTranslator.FromHtml("#ffdede")},         // #ffd9d9
            {SKUSellmentStatus.DelayFromWarehouse, ColorTranslator.FromHtml("#ffdede")},                  // #ffe0e0
            {SKUSellmentStatus.DelayFromOrganization, ColorTranslator.FromHtml("#ffdede")},               // #f3d9d9
            {SKUSellmentStatus.DelayFromOrganizationAndWarehouse, ColorTranslator.FromHtml("#ffdede")},   // #eddede
            {SKUSellmentStatus.DelayDeliveryFromPoints, ColorTranslator.FromHtml("#ffdede")},             // #ffefef
            {SKUSellmentStatus.Unknown, ColorTranslator.FromHtml("#daf3ff")}                              // #eef5f9
        };
        #endregion

        #region Properties 
        public int DeadlineDays { get; } = deadLineDays;
        public IRepository<long, SKU> SKUs { get; } = new SKURepo();
        public IRepository<long, List<ParsingRowEntity>> SkuRansomsBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuRemainingBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuShippedBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuTransferedBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuWriteOffFromPointsBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuWriteOffFromPointsViaWarehouseShortageUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuWriteOffFromPointsViaWarehouseAdjustmentUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuPaidShortagesBalanceUnits { get; } = new ParsingRowEntityRepo();
        /// <summary>
        /// Коллекция единиц экстра-прихода (пересорты, перемещения между помещениями)
        /// </summary>
        public IRepository<long, List<ParsingRowEntity>> SkuExtraReceiptBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<long, List<ParsingRowEntity>> SkuAliasesBalanceUnits { get; } = new ParsingRowEntityRepo();
        /// <summary>
        /// Коллекция единиц для корректировке расхода товаров
        /// </summary>
        public IRepository<long, List<ParsingRowEntity>> SkuShipmentAdjustmentUnits { get; } = new ParsingRowEntityRepo();


        // /// <summary>
        // /// Словарь прересортов в обратном направлении:
        // ///  - ключ: skuResortToNumber (артикул на который пересорт), 
        // ///  значение: словарь - ключ: skuResortFromNumber (артикул с которого пересорт), 
        // ///  значение:  словарь - ключ: resortTarget (цель пересорта)
        // ///                       значение: resortSum (количество)
        // /// </summary>
        // private readonly Dictionary<int, Dictionary<int, Dictionary<SKUResortTarget, int>>?> SKUToResorts =[]; 

        private static List<SKUParallelUnit> SKUParallelUnits { get; set; } = [];

        /// <summary>
        /// Returns number of SKUs in collection.
        /// </summary>
        /// <returns>Number of SKUs in collection.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        private delegate bool FindSkusWithPredicate(SKU sku);
        private FindSkusWithPredicate? predicateHandler;

        #endregion

        #region Functionality

        protected override void AddConfigVariables()
        {
            base.AddConfigVariables();

            IConfigurationSection? stringsSection = config!.GetSection("Strings");

            // Set status strings variables
            StatusStrings.Add(SKUSellmentStatus.FullSet, stringsSection.GetValue<string>("FullSetStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.InWork, stringsSection.GetValue<string>("InWorkStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayFromPointsAndWarehouse, stringsSection.GetValue<string>("DelayDeliveryFromPointsAndWarehouseStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayFromWarehouse, stringsSection.GetValue<string>("DelayDeliveryFromWarehouseStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayDeliveryFromPoints, stringsSection.GetValue<string>("DelayDeliveryFromPointsStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.Unknown, stringsSection.GetValue<string>("UnknownStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayFromOrganization, stringsSection.GetValue<string>("DelayFromOrganizationStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayFromOrganizationAndWarehouse, stringsSection.GetValue<string>("DelayFromOrganizationAndWarehouseStatusString") ?? string.Empty);
        }

        public async Task<double> ReceivingAndProcessingData(SourceReportItemKey reportFileKey, ExcelPackage excelPackage)
        {
            double time = 0.00;

            if (ExcelManager.GetExcelWorksheetStringNamesByKey(reportFileKey, out List<string>? workSheetNames) &&
                workSheetNames is not null &&
                workSheetNames.Count > 0)
            {
                List<Task<double>> processingTasksList = [];
                for (int i = 0; i < workSheetNames.Count; i++)
                {
                    var processingData = ProcessingData(reportFileKey, workSheetNames[i], excelPackage);
                    processingTasksList.Add(processingData);
                }

                if (processingTasksList.Count != 0)
                {
                    Task<double>[] processingTasksArray = [.. processingTasksList];
                    for (int i = 0; i < processingTasksArray.Length; i++)
                    {
                        Task<double>? currentTask = processingTasksArray[i];
                        if (currentTask is not null)
                            await Task.Run(async () => await currentTask);

                        time += currentTask is not null ? currentTask.Result : 0.00;
                    }

                    Task.WaitAll(processingTasksArray);
                }
            }

            return time;
        }

        private async Task<double> ProcessingData(SourceReportItemKey reportFileKey, string workSheetName, ExcelPackage excelPackage)
        {
            // Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Get data from excel sheet (in ExcelSheetParsingManager)
            Task<(double time, IEnumerable<Dictionary<string, ItemForParsing>>? data)> getDataTask =
                excelSheetsManager.GetDataFromExcelSheet(reportFileKey, excelPackage, workSheetName);
            await Task.Run(() => getDataTask);
            getDataTask.Wait();
            var (time, data) = getDataTask.Result;
            if (data is null)
                return StopFuctionExecuting(ref stopwatch);

            // Processing:
            // 1. Save balances for sku's
            await SaveSKUBalanceUnits(reportFileKey, workSheetName, [.. data]);

            // 2. Create or update sku's from balance units 
            var makeSKUsTask = UpdateSKUsFromBalanceUnits(reportFileKey, workSheetName);
            await Task.Run(async () => await makeSKUsTask);
            makeSKUsTask.Wait();

            // 3. Report
            static bool FindSkusWithRansoms(SKU sku) => sku.SKURansomsBalance != null;
            static bool FindSkusWithRemaining(SKU sku) => sku.SKURemainingBalance != null;
            static bool FindSkusWithOutgoingProducts(SKU sku) => sku.SKURemainingBalance?.SKUShipmentGoods != null;
            static bool FindSkusWithTransfered(SKU sku) => sku.SKURemainingBalance?.SKUTransfered != null;
            static bool FindSkusWithWriteOffPoints(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOffPoints > 0;
            static bool FindSkusWithWriteOffPointsViaWarehouseShortage(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseShortage > 0;
            static bool FindSkusWithWriteOffPointsViaWarehouseAdjustment(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseAdjustment > 0;
            //static bool FindSkusWithResort(SKU sku) => sku.SKUResortBalance != null;
            // ч    static bool FindSkusWithAliases(SKU sku) => SkuAliasesBalanceUnits.Count() > 0 ;
            //static bool FindSkusWithParallel(SKU sku) => SKUParallelUnits.Any();

            var message = string.Empty;

            switch (reportFileKey)
            {
                case SourceReportItemKey.Ransoms:
                    predicateHandler = FindSkusWithRansoms;
                    message = "Добавлено артикулов из выкупов: ";
                    break;
                case SourceReportItemKey.Balance:
                    predicateHandler = FindSkusWithRemaining;
                    message = "Добавлено артикулов из прихода и остатков на складе: ";
                    break;
                case SourceReportItemKey.Additional:
                    // switch (workSheetName)
                    // {
                    //     // case ExcelSheetParsingManager.ADDREPORT_EXTRA_RECEIPT_WORKSHEET_NAME:
                    //     //     predicateHandler = FindSkusWithResort;
                    //     //     message = "Артикулов из экстра-приходов: ";
                    //     //     break;
                    //         // case ReportFileManager.ADDREPORT_ALIASES_WORKSHEET_NAME:
                    //         //     predicateHandler = SkuAliasesBalanceUnits.Count() > 0;
                    //         //     message = "Артикулов с алиасами: ";
                    //         //     break;
                    // }
                    break;
                case SourceReportItemKey.OutgoingProducts:
                    predicateHandler = FindSkusWithOutgoingProducts;
                    message = "Артикулов в расходе со склада: ";
                    break;
                case SourceReportItemKey.Transfered:
                    predicateHandler = FindSkusWithTransfered;
                    message = "Артикулов в передаче со склада (на фулфилмент): ";
                    break;
                case SourceReportItemKey.WriteOffPoints:
                    predicateHandler = FindSkusWithWriteOffPoints;
                    message = "Артикулов в списании недостач от точек: ";
                    break;
                case SourceReportItemKey.WriteOffPointsViaWarehouseShortage:
                    predicateHandler = FindSkusWithWriteOffPointsViaWarehouseShortage;
                    message = "Артикулов в списании недостач от точек через склад: ";
                    break;
                case SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment:
                    predicateHandler = FindSkusWithWriteOffPointsViaWarehouseAdjustment;
                    message = "Артикулов в списании корректировок от точек через склад: ";
                    break;
                default:
                    break;
            }

            // Report
            if (predicateHandler is not null)
            {
                int skusWithPredicateCount = SKUs.GetValuesCount(predicateHandler.Invoke);
                message = string.Concat(message, skusWithPredicateCount);
                OnManagerReached(new(message, false, false));
            }


            return StopFuctionExecuting(ref stopwatch);
        }

        /// <summary>
        /// Creates and saves SKURansomsBalanceUnit collection from a list of raw data dictionaries.
        /// </summary>
        /// <param name="reportFileKey">Key of source file with report.</param>
        /// <param name="rowsWithRawData">List of raw data dictionaries.</param>
        /// <returns>Number of keys in final dictionary.</returns>
        /// <exception cref="ArgumentException">Exception when passing an empty dictionary.</exception>
        public async Task<int> SaveSKUBalanceUnits(SourceReportItemKey reportFileKey, string workSheetName, List<Dictionary<string, ItemForParsing>> rowsWithRawData)
        {
            bool isRansoms = reportFileKey == SourceReportItemKey.Ransoms;
            bool isBalance = reportFileKey == SourceReportItemKey.Balance;
            bool isOutgoingProducts = reportFileKey == SourceReportItemKey.OutgoingProducts;
            bool isTransfered = reportFileKey == SourceReportItemKey.Transfered;
            bool isWriteOffPoints = reportFileKey == SourceReportItemKey.WriteOffPoints;
            bool isWriteOffPointsViaWarehouseShortage = reportFileKey == SourceReportItemKey.WriteOffPointsViaWarehouseShortage;
            bool isWriteOffPointsViaWarehouseAdjustment = reportFileKey == SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment;
            // bool isResort = reportFileKey == SourceReportItemKey.Additional && workSheetName == ReportFileManager.ADDREPORT_RESORTS_WORKSHEET_NAME;
            // bool isParallel = reportFileKey == SourceReportItemKey.Additional && workSheetName == ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME;
            bool isAliases = reportFileKey == SourceReportItemKey.Additional && workSheetName == ExcelSheetParsingManager.ADDREPORT_ALIASES_WORKSHEET_NAME;
            bool isShipmentAdjustment = reportFileKey == SourceReportItemKey.Additional && workSheetName == ExcelSheetParsingManager.ADDREPORT_WRITEOFF_ADJUSTMENTS_WORKSHEET_NAME;
            bool isPaidShortages = reportFileKey == SourceReportItemKey.ShortagesPaid;

            await Task.Delay(10); // dummy

            if (isRansoms)
            {
                var skuRansomsBalanceDataUnits = from rowWithRawData in rowsWithRawData
                                                 let unit = new SKURansomsBalanceDataUnit(rowWithRawData)
                                                 group unit by unit.SKUNumber
                                                 into unitGroup
                                                 select unitGroup;

                foreach (var group in skuRansomsBalanceDataUnits)
                {
                    List<ParsingRowEntity>? list = (((List<SKURansomsBalanceDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                        throw new InvalidCastException("Невозможно привести List<SKURansomsBalanceDataUnit> к List<ParsingRowEntity>");

                    long skuNumber = group.Key;
                    SkuRansomsBalanceUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                }

            }
            else if (isBalance)
            {
                var skuRemainingBalanceDataUnits = from rowWithRawData in rowsWithRawData
                                                   let unit = new SKURemainingBalanceDataUnit(rowWithRawData)
                                                   group unit by unit.SKUNumber
                                                   into unitGroup
                                                   select unitGroup;

                foreach (var group in skuRemainingBalanceDataUnits)
                {
                    long skuNumber = group.Key;
                    if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                    {
                        var list = (((List<SKURemainingBalanceDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                            throw new InvalidCastException("Невозможно привести List<SKURemainingBalanceDataUnit> к List<ParsingRowEntity>");

                        SkuRemainingBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                    }
                }
            }
            else if (isOutgoingProducts)
            {
                var skuShippedDataUnits = from rowWithRawData in rowsWithRawData
                                          let unit = new SKUShippedDataUnit(rowWithRawData)
                                          group unit by unit.SKUNumber
                                            into unitGroup
                                          select unitGroup;

                foreach (var group in skuShippedDataUnits)
                {
                    long skuNumber = group.Key;
                    if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                    {
                        var list = (((List<SKUShippedDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                            throw new InvalidCastException("Невозможно привести List<SKUShippedDataUnit> к List<ParsingRowEntity>");

                        SkuShippedBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                    }
                }
            }
            else if (isTransfered)
            {
                var skuTransferedDataUnits = from rowWithRawData in rowsWithRawData
                                             let unit = new SKUTransferDataUnit(rowWithRawData)
                                             group unit by unit.SKUNumber
                                             into unitGroup
                                             select unitGroup;

                foreach (var group in skuTransferedDataUnits)
                {
                    long skuNumber = group.Key;
                    if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                    {
                        var list = (((List<SKUTransferDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                            throw new InvalidCastException("Невозможно привести List<SKUTransferDataUnit> к List<ParsingRowEntity>");

                        SkuTransferedBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                    }
                }
            }
            else if (isWriteOffPoints)
            {
                var skuWriteOffDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUWriteOffDataUnit(rowWithRawData)
                                           group unit by unit.SKUNumber
                                           into unitGroup
                                           select unitGroup;

                foreach (var group in skuWriteOffDataUnits)
                {
                    long skuNumber = group.Key;
                    if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                    {
                        var list = (((List<SKUWriteOffDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast {nameof(List<SKUWriteOffDataUnit>)} to {nameof(List<ParsingRowEntity>)}");

                        SkuWriteOffFromPointsBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                    }
                }
            }
            else if (isWriteOffPointsViaWarehouseShortage)
            {
                var skuWriteOffDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUWriteOffDataUnit(rowWithRawData)
                                           group unit by unit.SKUNumber
                                           into unitGroup
                                           select unitGroup;

                foreach (var group in skuWriteOffDataUnits)
                {
                    var skuNumber = group.Key;
                    if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                    {
                        var list = (((List<SKUWriteOffDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast {nameof(List<SKUWriteOffDataUnit>)} to {nameof(List<ParsingRowEntity>)}");

                        SkuWriteOffFromPointsViaWarehouseShortageUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                    }

                }
            }
            else if (isShipmentAdjustment)
            {
                var skuAdjustmentDataUnits = from rowWithRawData in rowsWithRawData
                                             let unit = new SKUAdjustmentShipmentUnit(rowWithRawData)
                                             group unit by unit.SKUSourceNumber
                                             into unitGroup
                                             select unitGroup;

                foreach (var skuAdjustmentGroup in skuAdjustmentDataUnits)
                {
                    long skuNumber = skuAdjustmentGroup.Key;

                    List<ParsingRowEntity>? list = (((List<SKUAdjustmentShipmentUnit>?)[.. skuAdjustmentGroup])?.Cast<ParsingRowEntity>().ToList()) ??
                        throw new InvalidCastException($"Cannot cast List of {nameof(SKUAdjustmentShipmentUnit)} to List of {nameof(ParsingRowEntity)}.");

                    SkuShipmentAdjustmentUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                }
            }
            // else if (isParallel)
            // {
            //     var skuParallelDataUnits = from rowWithRawData in rowsWithRawData
            //                                let unit = new SKUParallelUnit(rowWithRawData)
            //                                group unit by unit.SKUNumber
            //                                into unitGroup
            //                                select unitGroup;

            //     foreach (var skuParallelGroup in skuParallelDataUnits)
            //     {
            //         long skuNumber = skuParallelGroup.Key;
            //         if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
            //         {
            //             List<ParsingRowEntity>? list = (((List<SKUParallelUnit>?)[.. skuParallelGroup])?.Cast<ParsingRowEntity>().ToList()) ??
            //             throw new InvalidCastException($"Cannot cast List of {nameof(SKUParallelUnit)} to List of {nameof(ParsingRowEntity)}.");

            //             SkuParallelUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
            //         }
            //     }
            // }
            else if (isAliases)
            {
                var skuAliasDataUnits = from rowWithRawData in rowsWithRawData
                                        let unit = new SKUAliasDataUnit(rowWithRawData)
                                        group unit by unit.SKUSourceNumber
                                        into unitGroup
                                        select unitGroup;

                foreach (var skuAliasGroup in skuAliasDataUnits)
                {
                    long skuNumber = skuAliasGroup.Key;

                    //if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                    //{
                    List<ParsingRowEntity>? list = (((List<SKUAliasDataUnit>?)[.. skuAliasGroup])?.Cast<ParsingRowEntity>().ToList()) ??
                        throw new InvalidCastException($"Cannot cast List of {nameof(SKUAliasDataUnit)} to List of {nameof(ParsingRowEntity)}.");

                    SkuAliasesBalanceUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                    //}
                }

                // // Report for aliases
                // OnManagerReached(new($"Артикулов c пересортами 1С: {SkuResortBalanceUnits.Count()}", false, false));

                // // Report for aliases
                // OnManagerReached(new($"Артикулов c алиасами: {SkuAliasesBalanceUnits.Count()}", false, false));
            }
            else if (isPaidShortages)
            {
                var skuPaidShortageDataUnits = from rowWithRawData in rowsWithRawData
                                               let unit = new SKUPaidShortageDataUnit(rowWithRawData)
                                               where !string.IsNullOrEmpty(unit.ClientLogin) && ClientLogins.Contains(unit.ClientLogin)
                                               group unit by unit.SKUNumber
                                               into unitGroup
                                               select unitGroup;

                foreach (var group in skuPaidShortageDataUnits)
                {
                    var list = (((List<SKUPaidShortageDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ??
                        throw new InvalidCastException($"Cannot cast {nameof(List<SKUPaidShortageDataUnit>)} to {nameof(List<ParsingRowEntity>)}.");

                    if (group != null && group.Key.HasValue)
                    {
                        var skuNumber = group.Key.Value;
                        if (IsSkuRansomsBalanceUnitsHasKey(skuNumber))
                        {
                            SkuPaidShortagesBalanceUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                        }
                    }
                }
            }

            return reportFileKey switch
            {
                SourceReportItemKey.Ransoms => SkuRansomsBalanceUnits.Count(),
                SourceReportItemKey.Balance => SkuRemainingBalanceUnits.Count(),
                SourceReportItemKey.OutgoingProducts => SkuShippedBalanceUnits.Count(),
                SourceReportItemKey.Transfered => SkuTransferedBalanceUnits.Count(),
                SourceReportItemKey.WriteOffPoints => SkuWriteOffFromPointsBalanceUnits.Count(),
                SourceReportItemKey.WriteOffPointsViaWarehouseShortage => SkuWriteOffFromPointsViaWarehouseShortageUnits.Count(),
                SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment => SkuWriteOffFromPointsViaWarehouseAdjustmentUnits.Count(),
                // SourceReportItemKey.Additional => workSheetName switch
                // {
                //     ReportFileManager.ADDREPORT_RESORTS_WORKSHEET_NAME => SkuResortBalanceUnits.Count(),
                //     ReportFileManager.ADDREPORT_ALIASES_WORKSHEET_NAME => SkuAliasesBalanceUnits.Count(),
                //     //ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME => SkuParallelUnits.Count(),
                //     _ => 0
                // },
                SourceReportItemKey.ShortagesPaid => SkuPaidShortagesBalanceUnits.Count(),
                _ => 0
            };
        }

        /// <summary>
        /// Creates a collection of SKUs objects from an array of "raw data".
        /// </summary>
        /// <exception cref="ArgumentException">Manager does not contain an array of "raw data".</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task UpdateSKUsFromBalanceUnits(SourceReportItemKey skuReportFileKey, string workSheetName)
        {
            bool isRansoms = skuReportFileKey == SourceReportItemKey.Ransoms;
            bool isRemaining = skuReportFileKey == SourceReportItemKey.Balance;
            bool isShipped = skuReportFileKey == SourceReportItemKey.OutgoingProducts;
            bool isTransfered = skuReportFileKey == SourceReportItemKey.Transfered;
            bool isWriteOffPoints = skuReportFileKey == SourceReportItemKey.WriteOffPoints;
            bool isWriteOffPointsViaWarehouseShortage = skuReportFileKey == SourceReportItemKey.WriteOffPointsViaWarehouseShortage;
            bool isWriteOffPointsViaWarehouseAdjustment = skuReportFileKey == SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment;
            // bool isResorts = skuReportFileKey == SourceReportItemKey.Additional && workSheetName == ReportFileManager.ADDREPORT_RESORTS_WORKSHEET_NAME;
            // bool isParellel = skuReportFileKey == SourceReportItemKey.Additional && workSheetName == ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME;
            // bool isAliases = skuReportFileKey == SourceReportItemKey.Additional && workSheetName == ReportFileManager.ADDREPORT_ALIASES_WORKSHEET_NAME;
            bool isAdjustment = skuReportFileKey == SourceReportItemKey.Additional && workSheetName == ExcelSheetParsingManager.ADDREPORT_WRITEOFF_ADJUSTMENTS_WORKSHEET_NAME;
            bool isPaidShortages = skuReportFileKey == SourceReportItemKey.ShortagesPaid;

            // Dummy
            await Task.Delay(1);

            // Definition of a marketplace and collection
            Marketplace marketplace;
            IRepository<long, List<ParsingRowEntity>>? parsingEntityCollection = skuReportFileKey switch
            {
                SourceReportItemKey.Ransoms =>
                   parsingEntityCollection = SkuRansomsBalanceUnits,
                SourceReportItemKey.Balance =>
                    parsingEntityCollection = SkuRemainingBalanceUnits,
                SourceReportItemKey.OutgoingProducts =>
                    parsingEntityCollection = SkuShippedBalanceUnits,
                SourceReportItemKey.Transfered =>
                    parsingEntityCollection = SkuTransferedBalanceUnits,
                SourceReportItemKey.WriteOffPoints =>
                    parsingEntityCollection = SkuWriteOffFromPointsBalanceUnits,
                SourceReportItemKey.WriteOffPointsViaWarehouseShortage =>
                    parsingEntityCollection = SkuWriteOffFromPointsViaWarehouseShortageUnits,
                SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment =>
                    parsingEntityCollection = SkuWriteOffFromPointsViaWarehouseAdjustmentUnits,
                SourceReportItemKey.Additional => workSheetName switch
                {
                    ExcelSheetParsingManager.ADDREPORT_ALIASES_WORKSHEET_NAME =>
                        parsingEntityCollection = SkuAliasesBalanceUnits,
                    // ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME =>
                    //     parsingEntityCollection = SkuParallelUnits,
                    _ => parsingEntityCollection = null
                },
                SourceReportItemKey.ShortagesPaid => SkuPaidShortagesBalanceUnits,
                _ => parsingEntityCollection = null,
            };

            bool isCollectionValid = parsingEntityCollection is not null && parsingEntityCollection.Count() > 0;

            if (isCollectionValid)
            {
                List<(long key, List<ParsingRowEntity> value)>? listCollection = [.. parsingEntityCollection!.GetValuesList()];
                if (listCollection is null || listCollection.Count == 0)
                    throw new Exception("It is not possible to process an empty collection of type List<(int key, List<ParsingRowEntity> value)>.");

                foreach (var (skuNumber, listEntities) in listCollection)
                {
                    if (isRansoms)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKURansomsBalanceDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKURansomsBalanceDataUnit>.");

                        var logins = from unit in units
                                     select unit.SKUClientLogin;


                        var loginsString = string.Join(", ", logins.ToList());

                        marketplace = units.First().SKUMarketPlace;

                        var skuBrandAndName = units.First().SKUBrandAndName;

                        SKURansomsBalance ransomsBalance = new([.. units]);

                        var skuLastPurchaseDate = units
                            .Select(u => u.SKULastPurchaseDate)
                            .Max();

                        var skuAveragePrice = units
                            .Average(u => u.SKUAveragePrice);

                        SKU tempSKU = new(marketplace, loginsString, skuNumber, skuBrandAndName, ransomsBalance, skuAveragePrice, skuLastPurchaseDate, DeadlineDays, IS_REMOVE_BRAND_FROM_NAME);
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, tempSKU);
                    }
                    else if (isRemaining)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKURemainingBalanceDataUnit>().ToList()) ??
                            throw new InvalidCastException("Unable to cast collection to IList<SKURemainingBalanceDataUnit>.");


                        SKURemainingBalance remainingBalance = new(skuNumber, [.. units]);
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, remainingBalance));
                    }
                    else if (isShipped)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUShippedDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUShippedDataUnit>.");

                        SKURemainingBalance remainingBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, remainingBalance));
                    }
                    else if (isTransfered)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUTransferDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUTransferDataUnit>.");

                        SKURemainingBalance remainingBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, remainingBalance));
                    }
                    else if (isWriteOffPoints)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUWriteOffDataUnit>)}.");

                        if (SKUs.GetValue(skuNumber, out SKU? sKU))
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, [.. units], SourceReportItemKey.WriteOffPointsAndCouriers);
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, writeOffBalance));
                        }
                    }
                    else if (isWriteOffPointsViaWarehouseShortage)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                           throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUWriteOffDataUnit>)}.");

                        if (SKUs.GetValue(skuNumber, out SKU? sKU))
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, [.. units], SourceReportItemKey.WriteOffPointsViaWarehouseShortage);
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, writeOffBalance));
                        }
                    }
                    else if (isWriteOffPointsViaWarehouseAdjustment)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                           throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUWriteOffDataUnit>)}.");

                        if (SKUs.GetValue(skuNumber, out SKU? sKU))
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, [.. units], SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment);
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, writeOffBalance));
                        }
                    }
                    // else if (isResorts)
                    // {
                    //     // TODO: Work with resorts
                    // }
                    // else if (isAliases)
                    // {
                    //     // TODO: Work with aliases
                    // }
                    // else if (isParellel)
                    // {
                    //     var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUParallelUnit>().ToList()) ??
                    //         throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUParallelUnit>)}.");

                    //     foreach (var unit in units)
                    //     {
                    //         SKUs.RetrieveAndUpdateOrAddValue(unit.SKUNumber, new(unit.SKUNumber, true));
                    //     }
                    // }
                    else if (isPaidShortages)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUPaidShortageDataUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUPaidShortageDataUnit>)}.");

                        SKUPaidShortageBalance skuPaidShortageBalance = new(skuNumber, [.. units]);

                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, skuPaidShortageBalance));
                    }
                    else if (isAdjustment)
                    {

                    }
                }
            }
        }

        public async Task<List<SKUForTargetReport>> MakeSkusForTargetReport()
        {
            List<SKUForTargetReport> skuForTargetReports = [];

            foreach (var (_, targetSku) in SKUs.GetValuesList())
            {
                if (targetSku is null) continue;

                // Поиск экстра-приходов от перемещений и пересортов
                int extraReceiptSum = 0;
                SkuExtraReceiptBalanceUnits.GetValue(targetSku.SKUNumber, out List<ParsingRowEntity>? units);
                if (units != null && units.Count > 0)
                {
                    // Add resort element to SKU
                    var unit = units.First();
                    var extraReceiptUnit = (SKUExtraReceiptDataUnit)unit;
                    extraReceiptSum = extraReceiptUnit.SKUExtraReceiptCount;
                }

                // Поиск корректировок расхода от клиента
                int adjustmentWriteOffSum = 0;
                SkuShipmentAdjustmentUnits.GetValue(targetSku.SKUNumber, out List<ParsingRowEntity>? a_units);
                if (a_units != null && a_units.Count > 0)
                {
                    // Add resort element to SKU
                    var a_unit = a_units.First();
                    var adjustmentWriteOffDataUnit = (SKUAdjustmentShipmentUnit)a_unit;
                    adjustmentWriteOffSum = adjustmentWriteOffDataUnit.SKUAdjustmentSum;
                }

                // Add SKUForTargetReport with extra receipt
                skuForTargetReports.Add(new(targetSku, extraReceiptSum, adjustmentWriteOffSum));
            }

            await Task.Delay(10); // dummy 

            return [.. skuForTargetReports
                .OrderByDescending(p => p.SKUDate)];
        }

        public async Task<List<SKUForTargetReport>>? MakeAndCombineSkusForTargetReport()
        {
            // Make skus for target report
            Task<List<SKUForTargetReport>>? skusForTargetReportTask = MakeSkusForTargetReport();
            await Task.Run(() => skusForTargetReportTask);
            skusForTargetReportTask.Wait();
            List<SKUForTargetReport> result = skusForTargetReportTask.Result;

            // Combine skus for target report
            if (SkuAliasesBalanceUnits.Count() > 0)
            {
                CombineSkusForTargetReport(ref result);
            }

            return result;
        }

        private void CombineSkusForTargetReport(ref List<SKUForTargetReport> skus)
        {
            var aliases = SkuAliasesBalanceUnits.GetValuesList();

            for (int i = 0; i < aliases.Count; i++)
            {
                for (int j = 0; j < aliases[i].value.Count; j++)
                {
                    SKUAliasDataUnit unit = (SKUAliasDataUnit)aliases[i].value[j];
                    CombineSkus(unit, ref skus);
                }
            }
        }

        private static void CombineSkus(SKUAliasDataUnit aliasDataUnit, ref List<SKUForTargetReport> skus)
        {
            List<SKUForTargetReport> combines = [];
            List<long> sources = [];

            // Find tagret sku
            SKUForTargetReport? target = skus.SingleOrDefault(s => s.SKUNumber == aliasDataUnit.SKUSourceNumber);
            if (target is not null)
            {
                combines.Add(target);
            }

            // Find source skus
            foreach (var sourceSKUNumber in aliasDataUnit.SKUAliasNumbers)
            {
                SKUForTargetReport? source = skus.SingleOrDefault(s => s.SKUNumber == sourceSKUNumber);
                if (source is not null)
                {
                    combines.Add(source);
                    sources.Add(sourceSKUNumber);
                }
            }

            // price
            combines.First().SKUPrice = combines.Average(el => el.SKUPrice);

            // date 
            combines.First().SKUDate = combines.Max(el => el.SKUDate);

            // combines ransoms & waiting
            combines.First().SKURansomsCanceled = combines.Sum(el => el.SKURansomsCanceled);
            combines.First().SKURansomsOnWay = combines.Sum(el => el.SKURansomsOnWay);
            combines.First().SKURansomsDelivered = combines.Sum(el => el.SKURansomsDelivered);
            combines.First().SKURansomsWithDrawn = combines.Sum(el => el.SKURansomsWithDrawn);
            combines.First().SKURansomsWaiting = combines.Sum(el => el.SKURansomsWaiting);

            // combines receipt, receipt comment  && write off from points
            combines.First().SKUReceiptGoods = combines.Sum(el => el.SKUReceiptGoods);

            if (combines.Any(el => !string.IsNullOrEmpty(el.SKUReceipComment)))
            {
                List<string> comment = [];
                for (int i = 0; i < combines.Count; i++)
                {
                    if (!string.IsNullOrEmpty(combines[i].SKUReceipComment))
                        comment.Add(combines[i].SKUReceipComment);
                }

                if (comment.Count != 0)
                {

                    // "" - start with empty string to handle empty list case.
                    combines.First().SKUReceipComment = comment.Aggregate("", (current, next) => current + "\n" + next);
                }
            }
            combines.First().SKUWriteOffFromPoints = combines.Sum(el => el.SKUWriteOffFromPoints);

            // сombine paid shortage
            combines.First().SKUPaidShortageWarehouse = combines.Sum(el => el.SKUPaidShortageWarehouse);
            combines.First().SKUPaidShortageOrganization = combines.Sum(el => el.SKUPaidShortageOrganization);
            combines.First().SKUPaidShortageOrganizationAndWarehouse = combines.Sum(el => el.SKUPaidShortageOrganizationAndWarehouse);

            // сombine outgoing & transfered & resort & remaining
            combines.First().SKUShipmentGoods = combines.Sum(el => el.SKUShipmentGoods);
            combines.First().SKUTransfered = combines.Sum(el => el.SKUTransfered);
            combines.First().SKUResort = combines.Sum(el => el.SKUResort);
            combines.First().SKURemaining = combines.Sum(el => el.SKURemaining);

            // combine write offs
            combines.First().SKUWriteOffFromPoints = combines.Sum(el => el.SKUWriteOffFromPoints);
            combines.First().SKUWriteOffFromPointsViaWarehouseShortage = combines.Sum(el => el.SKUWriteOffFromPointsViaWarehouseShortage);
            combines.First().SKUWrittenOffFromPointsViaWarehouseAdjustment = combines.Sum(el => el.SKUWrittenOffFromPointsViaWarehouseAdjustment);

            // add combine comment
            if (sources.Count > 0)
            {
                combines.First().SKUAliasesComment = "Тот же товар под другими арт:";
                foreach (var source in sources)
                {
                    combines.First().SKUAliasesComment = source.ToString();
                }
            }

            // delete aliases
            foreach (var sourceSKUNumber in aliasDataUnit.SKUAliasNumbers)
            {
                int index = skus.FindIndex(s => s.SKUNumber == sourceSKUNumber);
                if (index >= 0)
                {
                    skus.RemoveAt(index);
                }
            }
        }

        private bool IsSkuRansomsBalanceUnitsHasKey(long skuNumber) =>
            SkuRansomsBalanceUnits.GetValue(skuNumber, out List<ParsingRowEntity>? item) && item != null;


        #endregion
    }
}
using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.Repositories;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalances;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUCompensationBalance;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance;

namespace TableAnalizer.Objects.Managers
{
    public class SKUManager(IConfiguration config, int deadLineDays, ExcelManager excelManager) : BaseManager(config)
    {
        #region Variables and constants 
        private const bool IS_REMOVE_BRAND_FROM_NAME = false;
        private readonly ExcelManager? excelManager = excelManager;
        public static Dictionary<SKUSellmentStatus, string> StatusStrings = [];

        /// <summary>
        /// Сollection of colors to indicate status of mutual settlements with clients.
        /// </summary>
        /// <remarks>https://www.color-hex.com/color-palette/2922</remarks>
        public static Dictionary<SKUSellmentStatus, Color> StatusColors { get; } = new()
        {
            {SKUSellmentStatus.FullSet, ColorTranslator.FromHtml("#e6f6eb")},                             // #e6f6eb
            {SKUSellmentStatus.InWork, ColorTranslator.FromHtml("#fff6f0")},                              // #fff6f0
            {SKUSellmentStatus.DelayDeliveryFromPointsAndWarehouse, ColorTranslator.FromHtml("#ffd9d9")}, // #ffd9d9
            {SKUSellmentStatus.DelayDeliveryFromWarehouse, ColorTranslator.FromHtml("#ffe0e0")},          // #ffe0e0
            {SKUSellmentStatus.DelayDeliveryFromPoints, ColorTranslator.FromHtml("#ffefef")},             // #ffefef
            {SKUSellmentStatus.Unknown, ColorTranslator.FromHtml("#eef5f9")}                              // #eef5f9
        };
        #endregion

        #region Properties 
        public int DeadlineDays { get; } = deadLineDays;
        public IRepository<int, SKU> SKUs { get; } = new SKURepo();
        public IRepository<int, List<ParsingRowEntity>> SkuRansomsBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuRemainingBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuShippedBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuTransferedBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuWriteOffFromPointsBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuWriteOffFromPointsViaWarehouseShortageUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuWriteOffFromPointsViaWarehouseAdjustmentUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuCompensationBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuResortBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuParallelUnits { get; } = new ParsingRowEntityRepo();

        // /// <summary>
        // /// Словарь прересортов в обратном направлении:
        // ///  - ключ: skuResortToNumber (артикул на который пересорт), 
        // ///  значение: словарь - ключ: skuResortFromNumber (артикул с которого пересорт), 
        // ///  значение:  словарь - ключ: resortTarget (цель пересорта)
        // ///                       значение: resortSum (количество)
        // /// </summary>
        // private readonly Dictionary<int, Dictionary<int, Dictionary<SKUResortTarget, int>>?> SKUToResorts =[]; 

        private static List<SKUParallelUnit> SKUParallelUnits {get; set;} = [];

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
            StatusStrings.Add(SKUSellmentStatus.DelayDeliveryFromPointsAndWarehouse, stringsSection.GetValue<string>("DelayDeliveryFromPointsAndWarehouseStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayDeliveryFromWarehouse, stringsSection.GetValue<string>("DelayDeliveryFromWarehouseStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.DelayDeliveryFromPoints, stringsSection.GetValue<string>("DelayDeliveryFromPointsStatusString") ?? string.Empty);
            StatusStrings.Add(SKUSellmentStatus.Unknown, stringsSection.GetValue<string>("UnknownStatusString") ?? string.Empty);
        }

        public async Task<double> ReceivingAndProcessingData(SourceSKUReportFileKey reportFileKey, bool isInternalReport, ExcelPackage excelPackage)
        {            
            double time = 0.00;

            if (ExcelManager.GetExcelWorksheetStringNamesByKey(reportFileKey, out List<string>? workSheetNames) &&
                workSheetNames is not null &&
                workSheetNames.Count > 0)
            {
                List<Task<double>> processingTasksList = [];
                for (int i = 0; i < workSheetNames.Count; i++)
                {
                    var processingData = ProcessingData(reportFileKey, workSheetNames[i], isInternalReport, excelPackage);
                    processingTasksList.Add(processingData);
                }

                if(processingTasksList.Count != 0) 
                {
                    Task<double> [] processingTasksArray = [.. processingTasksList];
                    for (int i = 0; i < processingTasksArray.Length; i++)
                    {
                        Task<double>? currentTask = processingTasksArray[i];
                        if(currentTask is not null) 
                            await Task.Run(async () => await currentTask);
                        
                        time += currentTask is not null ? currentTask.Result : 0.00;
                    }

                    Task.WaitAll(processingTasksArray);
                }
            }

            // Finish
            return time;
        }

        private async Task<double> ProcessingData(SourceSKUReportFileKey reportFileKey, string workSheetName, bool isInternalReport, ExcelPackage excelPackage)
        {
            // Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Get source excel worksheet by name
            var currentWorksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(ws => ws.Name == workSheetName);
            if (currentWorksheet is null)
            {
                OnManagerReached(new($"There is no worksheet named '{workSheetName}' in workbook file at key {reportFileKey}.",true, true));
                return StopFuctionExecuting(ref stopwatch);
            }

            // Get ranges from source excel worksheet
            if (ExcelManager.MakeRangeSourceReportRowsByKey(reportFileKey, workSheetName, out List<RangeSourceReportRows> rangeSourceReportRows))
            {
                // Get row ranges in source excel worksheet 
                var getRangeSourceReportRowsTask = ExcelManager.GetRangeSourceReportRows(in currentWorksheet, ref rangeSourceReportRows);
                await Task.Run(() => getRangeSourceReportRowsTask);
                getRangeSourceReportRowsTask.Wait();

                // Get raw data from cells in source excel worksheet 
                var getRawDataFromExcelRowsTask = ExcelManager.GetRawDataFromExcelRows(in currentWorksheet, ref rangeSourceReportRows);
                await Task.Run(() => getRawDataFromExcelRowsTask);
                getRawDataFromExcelRowsTask.Wait();

                // Union collections
                IEnumerable<Dictionary<string, ItemForParsing>> rawRowsItems = 
                    from sourceReportRowsRange in rangeSourceReportRows
                    from item in sourceReportRowsRange.DataOnReadRowsCollection
                    select item;

                // Processing:
                // 1. Save balances for sku's
                await SaveSKUBalanceUnits(reportFileKey, workSheetName, [.. rawRowsItems]);

                // 2. Create or update sku's from balance units 
                var makeSKUsTask = UpdateSKUsFromBalanceUnits(reportFileKey, workSheetName, isInternalReport);
                await Task.Run(async () => await makeSKUsTask);
                makeSKUsTask.Wait();
                
                // 3. Report
                static bool FindSkusWithRansoms(SKU sku) => sku.SKURansomsBalance != null;
                static bool FindSkusWithRemaining(SKU sku) => sku.SKURemainingBalance != null;
                static bool FindSkusWithOutgoingProducts(SKU sku) => sku.SKURemainingBalance?.SKUOutgoingProducts != null;
                static bool FindSkusWithTransfered(SKU sku) => sku.SKURemainingBalance?.SKUTransfered != null;
                static bool FindSkusWithWriteOffPoints(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOffPoints > 0;
                static bool FindSkusWithWriteOffPointsViaWarehouseShortage(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseShortage > 0;
                static bool FindSkusWithWriteOffPointsViaWarehouseAdjustment(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseAdjustment > 0;
                static bool FindSkusWithCompensation(SKU sku) => sku.SKUCompensationBalance != null;
                static bool FindSkusWithResort(SKU sku) => sku.SKUResortBalance != null;
                static bool FindSkusWithParallel(SKU sku) => SKUParallelUnits.Any();

                var message = string.Empty;

                switch (reportFileKey)
                {
                    case SourceSKUReportFileKey.Ransoms:
                        predicateHandler = FindSkusWithRansoms;
                        message = "Добавлено артикулов из выкупов: ";
                        break;
                    case SourceSKUReportFileKey.Balance:
                        predicateHandler = FindSkusWithRemaining;
                        message = "Добавлено артикулов из прихода и остатков на складе: ";
                        break;
                    case SourceSKUReportFileKey.Additional:
                        switch (workSheetName)
                        {
                            case ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME:
                                predicateHandler = FindSkusWithCompensation;
                                message = "Артикулов из компенсации недостач: ";
                                break;
                            case ExcelManager.RESORTS_REPORT_WORKSHEET_NAME:
                                predicateHandler = FindSkusWithResort;
                                message = "Артикулов из пересортов: ";
                                break;
                            case ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME:
                                predicateHandler = FindSkusWithParallel;
                                message = "Артикулов параллельных клиентов: ";
                                break;
                        };
                        break;
                    case SourceSKUReportFileKey.OutgoingProducts:
                        predicateHandler = FindSkusWithOutgoingProducts;
                        message = "Артикулов в расходе со склада: ";
                        break;
                    case SourceSKUReportFileKey.Transfered:
                        predicateHandler = FindSkusWithTransfered;
                        message = "Артикулов в передаче со склада (на фулфилмент): ";
                        break;
                    case SourceSKUReportFileKey.WriteOffPoints:
                        predicateHandler = FindSkusWithWriteOffPoints;
                        message = "Артикулов в списании недостач от точек: ";
                        break;
                    case SourceSKUReportFileKey.WriteOffPointsViaWarehouseShortage:
                        predicateHandler = FindSkusWithWriteOffPointsViaWarehouseShortage;
                        message = "Артикулов в списании недостач от точек через склад: ";
                        break;
                    case SourceSKUReportFileKey.WriteOffPointsViaWarehouseAdjustment:
                        predicateHandler = FindSkusWithWriteOffPointsViaWarehouseAdjustment;
                        message = "Артикулов в списании корректировок от точек через склад: ";
                        break;
                    default:
                        break;
                }

                // Report
                if(predicateHandler is not null) 
                {
                    int skusWithPredicateCount = SKUs.GetValuesCount(predicateHandler.Invoke);
                    message = string.Concat(message, skusWithPredicateCount);
                    OnManagerReached(new(message, false, false));
                }
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
        public async Task<int> SaveSKUBalanceUnits(SourceSKUReportFileKey reportFileKey, string workSheetName, List<Dictionary<string, ItemForParsing>> rowsWithRawData)
        {
            bool isRansoms = reportFileKey == SourceSKUReportFileKey.Ransoms;
            bool isBalance = reportFileKey == SourceSKUReportFileKey.Balance;
            bool isOutgoingProducts = reportFileKey == SourceSKUReportFileKey.OutgoingProducts;
            bool isTransfered = reportFileKey == SourceSKUReportFileKey.Transfered;
            bool isWriteOffPoints = reportFileKey == SourceSKUReportFileKey.WriteOffPoints;
            bool isWriteOffPointsViaWarehouseShortage = reportFileKey == SourceSKUReportFileKey.WriteOffPointsViaWarehouseShortage;
            bool isWriteOffPointsViaWarehouseAdjustment = reportFileKey == SourceSKUReportFileKey.WriteOffPointsViaWarehouseAdjustment;
            bool isCompensation = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResort = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;
            bool isParallel = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME;

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

                    var skuNumber = group.Key;
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
                    var list = (((List<SKURemainingBalanceDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKURemainingBalanceDataUnit> к List<ParsingRowEntity>");
                    
                    SkuRemainingBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            } 
            else if(isOutgoingProducts) 
            {
                var skuShippedDataUnits = from rowWithRawData in rowsWithRawData
                                            let unit = new SKUShippedDataUnit(rowWithRawData)
                                            group unit by unit.SKUNumber 
                                            into unitGroup  
                                            select unitGroup;
                
                foreach (var group in skuShippedDataUnits)
                {
                    var list = (((List<SKUShippedDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKUShippedDataUnit> к List<ParsingRowEntity>");
                    
                    SkuShippedBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
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
                    var list = (((List<SKUTransferDataUnit>?)[.. group])?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKUTransferDataUnit> к List<ParsingRowEntity>");
                    
                    SkuTransferedBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            }
            else if(isWriteOffPoints) 
            {
                var skuWriteOffDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUWriteOffDataUnit(rowWithRawData)
                                           group unit by unit.SKUNumber 
                                           into unitGroup  
                                           select unitGroup;
                
                foreach (var group in skuWriteOffDataUnits)
                {
                    var list = (((List<SKUWriteOffDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException($"Cannot cast {nameof(List<SKUWriteOffDataUnit>)} to {nameof(List<ParsingRowEntity>)}");
                    

                    SkuWriteOffFromPointsBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            }    
            else if(isWriteOffPointsViaWarehouseShortage) 
            {
                var skuWriteOffDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUWriteOffDataUnit(rowWithRawData)
                                           group unit by unit.SKUNumber 
                                           into unitGroup  
                                           select unitGroup;

                foreach (var group in skuWriteOffDataUnits)
                {
                    var list = (((List<SKUWriteOffDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException($"Cannot cast {nameof(List<SKUWriteOffDataUnit>)} to {nameof(List<ParsingRowEntity>)}");
                    

                    SkuWriteOffFromPointsViaWarehouseShortageUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            }
            else if(isCompensation) 
            {
                var skuCompensationDataUnits = from rowWithRawData in rowsWithRawData
                                    let unit = new SKUCompensationDataUnit(rowWithRawData)
                                    group unit by unit.SKUNumber 
                                    into unitGroup  
                                    select unitGroup;

                foreach (var group in skuCompensationDataUnits)
                {
                    var list = (((List<SKUCompensationDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKUCompensationDataUnit> к List<ParsingRowEntity>");
                    
                    SkuCompensationBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            }
            else if(isResort) 
            {
                var skuResortDataUnits = from rowWithRawData in rowsWithRawData
                                         let unit = new SKUResortDataUnit(rowWithRawData)
                                         group unit by unit.SKUFromResortNumber 
                                         into unitGroup  
                                         select unitGroup;

                foreach (var skuResortGroup in skuResortDataUnits)
                {
                    var skuNumber = skuResortGroup.Key;
                    List<ParsingRowEntity>? list = (((List<SKUResortDataUnit>?)[.. skuResortGroup])?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException($"Cannot cast List of {nameof(SKUResortDataUnit)} to List of {nameof(ParsingRowEntity)}.");

                    SkuResortBalanceUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                }
            } 
            else if(isParallel) 
            {
                var skuParallelDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUParallelUnit(rowWithRawData)
                                           group unit by unit.SKUNumber 
                                           into unitGroup  
                                           select unitGroup;

                foreach (var skuParallelGroup in skuParallelDataUnits) 
                {
                    var skuNumber = skuParallelGroup.Key;
                    List<ParsingRowEntity>? list = (((List<SKUParallelUnit>?)[.. skuParallelGroup])?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException($"Cannot cast List of {nameof(SKUParallelUnit)} to List of {nameof(ParsingRowEntity)}.");

                    SkuParallelUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                }
            } 

            return reportFileKey switch
            {
                SourceSKUReportFileKey.Ransoms => SkuRansomsBalanceUnits.Count(),
                SourceSKUReportFileKey.Balance => SkuRemainingBalanceUnits.Count(),
                SourceSKUReportFileKey.OutgoingProducts  => SkuShippedBalanceUnits.Count(),
                SourceSKUReportFileKey.Transfered => SkuTransferedBalanceUnits.Count(),
                SourceSKUReportFileKey.WriteOffPoints => SkuWriteOffFromPointsBalanceUnits.Count(),
                SourceSKUReportFileKey.WriteOffPointsViaWarehouseShortage => SkuWriteOffFromPointsViaWarehouseShortageUnits.Count(),
                SourceSKUReportFileKey.WriteOffPointsViaWarehouseAdjustment => SkuWriteOffFromPointsViaWarehouseAdjustmentUnits.Count(),
                SourceSKUReportFileKey.Additional => workSheetName switch 
                {
                    ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME => SkuCompensationBalanceUnits.Count(),
                    ExcelManager.RESORTS_REPORT_WORKSHEET_NAME => SkuResortBalanceUnits.Count(),
                    ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME => SkuParallelUnits.Count(),
                    _ => 0
                },
                _ => 0
            };
        }

        /// <summary>
        /// Creates a collection of SKUs objects from an array of "raw data".
        /// </summary>
        /// <exception cref="ArgumentException">Manager does not contain an array of "raw data".</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task UpdateSKUsFromBalanceUnits(SourceSKUReportFileKey skuReportFileKey, string workSheetName, bool isRawReport)
        {
            bool isRansoms = skuReportFileKey == SourceSKUReportFileKey.Ransoms;
            bool isRemaining = skuReportFileKey == SourceSKUReportFileKey.Balance;
            bool isShipped = skuReportFileKey == SourceSKUReportFileKey.OutgoingProducts;
            bool isTransfered = skuReportFileKey == SourceSKUReportFileKey.Transfered;
            bool isWriteOffPoints = skuReportFileKey == SourceSKUReportFileKey.WriteOffPoints;
            bool isWriteOffPointsViaWarehouseShortage = skuReportFileKey == SourceSKUReportFileKey.WriteOffPointsViaWarehouseShortage;
            bool isWriteOffPointsViaWarehouseAdjustment = skuReportFileKey == SourceSKUReportFileKey.WriteOffPointsViaWarehouseAdjustment;
            bool isCompensation = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResorts = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;
            bool isParellel = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME;

            // Dummy
            await Task.Delay(1);

            // Definition of a marketplace and collection
            Marketplace marketplace;       
            IRepository<int, List<ParsingRowEntity>>? parsingEntityCollection = skuReportFileKey switch
            {
                SourceSKUReportFileKey.Ransoms =>
                   parsingEntityCollection = SkuRansomsBalanceUnits,
                SourceSKUReportFileKey.Balance =>
                    parsingEntityCollection = SkuRemainingBalanceUnits,
                SourceSKUReportFileKey.OutgoingProducts =>
                    parsingEntityCollection = SkuShippedBalanceUnits,  
                SourceSKUReportFileKey.Transfered =>
                    parsingEntityCollection = SkuTransferedBalanceUnits,  
                SourceSKUReportFileKey.WriteOffPoints =>
                    parsingEntityCollection = SkuWriteOffFromPointsBalanceUnits,
                SourceSKUReportFileKey.WriteOffPointsViaWarehouseShortage =>
                    parsingEntityCollection = SkuWriteOffFromPointsViaWarehouseShortageUnits, 
                SourceSKUReportFileKey.WriteOffPointsViaWarehouseAdjustment =>
                    parsingEntityCollection = SkuWriteOffFromPointsViaWarehouseAdjustmentUnits, 
                SourceSKUReportFileKey.Additional => workSheetName switch 
                {
                    ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuCompensationBalanceUnits,
                    ExcelManager.RESORTS_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuResortBalanceUnits,
                    ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuParallelUnits,
                    _ => parsingEntityCollection = null
                },
                _ => parsingEntityCollection = null,
            };

            bool isCollectionValid = parsingEntityCollection is not null && parsingEntityCollection.Count() > 0;

            if(isCollectionValid) 
            {
                List<(int key, List<ParsingRowEntity> value)>? listCollection = [.. parsingEntityCollection!.GetValuesList()];
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
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new( skuNumber, remainingBalance));
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
                    else if(isWriteOffPoints) 
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUWriteOffDataUnit>)}.");

                        if(SKUs.GetValue(skuNumber, out SKU? sKU)) 
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, [.. units], WriteOffSource.Points);
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, writeOffBalance));
                        }
                    }
                    else if(isWriteOffPointsViaWarehouseShortage) 
                    {
                         var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUWriteOffDataUnit>)}.");

                        if(SKUs.GetValue(skuNumber, out SKU? sKU)) 
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, [.. units], WriteOffSource.PointsViaWarehouseShortage);
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, writeOffBalance));
                        }
                    }
                    else if(isWriteOffPointsViaWarehouseAdjustment) 
                    {
                         var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUWriteOffDataUnit>)}.");

                        if(SKUs.GetValue(skuNumber, out SKU? sKU)) 
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, [.. units], WriteOffSource.PointsViaWarehouseAdjustment);
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, writeOffBalance));
                        }
                    }
                    else if(isCompensation) 
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUCompensationDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUCompensationDataUnit>.");

                        SKUCompensationBalance skuCompensationBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, skuCompensationBalance));
                    }
                    else if(isResorts)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUResortDataUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUResortDataUnit>)}.");

                        SKUResortBalance skuResortBalance = new(skuNumber, [.. units]);
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, skuResortBalance));
                    }
                    else if(isParellel) 
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUParallelUnit>().ToList()) ??
                            throw new InvalidCastException($"Cannot cast collection to {nameof(IList<SKUParallelUnit>)}.");

                        foreach (var unit in units)
                        {
                            SKUs.RetrieveAndUpdateOrAddValue(unit.SKUNumber, new(unit.SKUNumber, true));
                        }
                    }
                }
            }
        }

        public async Task<List<SKUForTargetReports>> MakeSkusForTargetReport() 
        {
            List<SKUForTargetReports> skuForTargetReports = [];

            foreach (var (_, targetSku) in SKUs.GetValuesList())
            {
                if(targetSku is null) continue;
                skuForTargetReports.Add(new(targetSku));
            }

            await Task.Delay(10); // dummy 

            return [.. skuForTargetReports
                .OrderByDescending(p => p.SKUDate)];
        }
        
        #endregion
    }
}
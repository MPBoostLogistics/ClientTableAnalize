using System.Diagnostics;
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
    public class SKUManager() : BaseManager
    {
        #region Variables and constants 
        private const bool IS_REMOVE_BRAND_FROM_NAME = false;
        #endregion

        #region Properties 
        public int DeadlineDays { get; private set; }
        public IRepository<int, SKU> SKUs { get; } = new SKURepo();
        public IRepository<int, List<ParsingRowEntity>> SkuRansomsBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuRemainingBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuShippedBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuWriteOffBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuCompensationBalanceUnits { get; } = new ParsingRowEntityRepo();
        public IRepository<int, List<ParsingRowEntity>> SkuResortBalanceUnits { get; } = new ParsingRowEntityRepo();

        /// <summary>
        /// Словарь прересортов в обратном направлении:
        ///  - ключ: skuResortToNumber (артикул на который пересорт), 
        ///  значение: словарь - ключ: skuResortFromNumber (артикул с которого пересорт), 
        ///  значение:  словарь - ключ: resortTarget (цель пересорта)
        ///                       значение: resortSum (количество)
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>?> SKUToResorts =[]; 

        private static List<SKUParallelUnit> SKUParallelUnits {get; set;} = [];

        /// <summary>
        /// Returns number of SKUs in collection.
        /// </summary>
        /// <returns>Number of SKUs in collection.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public int SKUsCount() => SKUs.Count();
        private delegate bool FindSkusWithPredicate(SKU sku);
        private FindSkusWithPredicate? predicateHandler;

        #endregion

        #region Functionality

        /// <summary>
        /// Sets period before deadline when paying a client (in days).
        /// </summary>
        /// <param name="days">Period until deadline, days</param>
        public void SetDeadlineDays(int days) => DeadlineDays = days;

        public async Task<double> ReceivingAndProcessingData(SourceSKUReportFileKey reportFileKey, bool isRawReport, int delay, ExcelPackage? excelPackage = null, bool isMulty = false)
        {            
            double time = 0.00;

            if (ExcelManager.GetExcelWorksheetStringNamesByKey(reportFileKey, out List<string>? workSheetNames) &&
                workSheetNames is not null &&
                workSheetNames.Count > 0)
            {
                List<Task<double>> processingTasksList = [];
                for (int i = 0; i < workSheetNames.Count; i++)
                {
                    var processingData = ProcessingData(reportFileKey, workSheetNames[i], isRawReport, delay, excelPackage, isMulty);
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

        private async Task<double> ProcessingData(SourceSKUReportFileKey reportFileKey, string workSheetName, bool isRawReport, int taskDelay, ExcelPackage? excelPackage = null, bool isMulty = false)
        {
            // Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Get source excel worksheet by name
            ExcelWorksheet? currentWorksheet = ExcelManager.GetExcelWorksheetByName(workSheetName, reportFileKey, taskDelay, excelPackage).Result ;
            if (currentWorksheet is null)
            {
                await Task.Delay(taskDelay);
                OnManagerReached(new($"There is no worksheet named '{workSheetName}' in workbook file at key {reportFileKey}.",true, true));
                return StopFuctionExecuting(ref stopwatch);
            }

            // Get ranges from source excel worksheet
            if (ExcelManager.MakeRangeSourceReportRowsByKey(reportFileKey, workSheetName, out List<RangeSourceReportRows> rangeSourceReportRows))
            {
                // Get row ranges in source excel worksheet 
                ExcelManager.GetRangeSourceReportRows(in currentWorksheet, ref rangeSourceReportRows);

                // Get raw data from cells in source excel worksheet 
                ExcelManager.GetRawDataFromExcelRows(in currentWorksheet, ref rangeSourceReportRows);

                // Union collections
                IEnumerable<Dictionary<string, ItemForParsing>> rawRowsItems = 
                    from sourceReportRowsRange in rangeSourceReportRows
                    from item in sourceReportRowsRange.DataOnReadRowsCollection
                    select item;

                // Processing:
                // 1. Save balances for sku's
                SaveSKUBalanceUnits(reportFileKey, workSheetName, [.. rawRowsItems], isMulty, isRawReport);

                // 2. Create or update sku's from balance units 
                var makeSKUsTask = UpdateSKUsFromBalanceUnits(reportFileKey, workSheetName, isRawReport);
                await Task.Run(async () => await makeSKUsTask);
                makeSKUsTask.Wait();
                
                // 3. Report
                static bool FindSkusWithRansoms(SKU sku) => sku.SKURansomsBalance != null;
                static bool FindSkusWithRemaining(SKU sku) => sku.SKURemainingBalance != null;
                static bool FindSkusWithShipped(SKU sku) => sku.SKURemainingBalance?.SKUShipped != null;
                static bool FindSkusWithWriteOff(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOff > 0;
                static bool FindSkusWithCompensation(SKU sku) => sku.SKUCompensationBalance != null;
                static bool FindSkusWithResort(SKU sku) => sku.SKUResortBalance != null;
                static bool FindSkusWithParallel(SKU sku) => SKUParallelUnits.Any();

                var message = string.Empty;

                switch (reportFileKey)
                {
                    case SourceSKUReportFileKey.RansomsWB:
                    case SourceSKUReportFileKey.RansomsOZ:
                        predicateHandler = FindSkusWithRansoms;
                        message = "Добавлено артикулов из выкупов: ";
                        break;
                    case SourceSKUReportFileKey.RemainingWB:
                    case SourceSKUReportFileKey.RemainingOZ:
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
                                message = "Артикулов из параллельных клиентов: ";
                                break;
                        };
                        break;
                    case SourceSKUReportFileKey.Shipped or SourceSKUReportFileKey.Transfer:
                        predicateHandler = FindSkusWithShipped;
                        message = "Артикулов в расходе со склада: ";
                        break;

                    case SourceSKUReportFileKey.WriteOffShortages:
                        predicateHandler = FindSkusWithWriteOff;
                        message = "Артикулов в списании недостач: ";
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
        public int SaveSKUBalanceUnits(SourceSKUReportFileKey reportFileKey, string workSheetName, List<Dictionary<string, ItemForParsing>> rowsWithRawData, bool isMulty, bool isRawReport)
        {
            bool isRansoms = reportFileKey == SourceSKUReportFileKey.RansomsWB || reportFileKey == SourceSKUReportFileKey.RansomsOZ;
            bool isRemaining = reportFileKey == SourceSKUReportFileKey.RemainingWB || reportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isShipped = reportFileKey == SourceSKUReportFileKey.Shipped;
            bool isTransfer = reportFileKey == SourceSKUReportFileKey.Transfer;
            bool isWriteOff = reportFileKey == SourceSKUReportFileKey.WriteOffShortages;
            bool isCompensation = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResort = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;
            bool isParellel = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME;
            bool isClient = reportFileKey == SourceSKUReportFileKey.Client && workSheetName == ExcelManager.OUTPUT_REPORT_MAINREPORT_NAME;

            if (isRansoms)
            {
                var skuRansomsBalanceDataUnits = from rowWithRawData in rowsWithRawData
                                                 let unit = new SKURansomsBalanceDataUnit(rowWithRawData)
                                                 group unit by unit.SKUNumber 
                                                 into unitGroup  
                                                 select unitGroup;

                foreach (var group in skuRansomsBalanceDataUnits)
                {
                    var list = (((List<SKURansomsBalanceDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKURansomsBalanceDataUnit> к List<ParsingRowEntity>");

                    SkuRansomsBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }

            }
            else if (isRemaining)
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
            else if(isShipped || isTransfer) 
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
            else if(isWriteOff) 
            {
                var skuWriteOffDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUWriteOffDataUnit(rowWithRawData)
                                           group unit by unit.SKUNumber 
                                           into unitGroup  
                                           select unitGroup;
                
                foreach (var group in skuWriteOffDataUnits)
                {
                    var list = (((List<SKUWriteOffDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKUWriteOffDataUnit> к List<ParsingRowEntity>");
                    
                    SkuWriteOffBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
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
            else if(isResort && !isRawReport) 
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
                        throw new InvalidCastException("Cannot cast List<SKUResortDataUnit> to List<ParsingRowEntity>.");

                    SkuResortBalanceUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                }
            } 
            else if(isParellel) 
            {
                foreach (var rowWithRawData in rowsWithRawData)
                {
                    var unit = new SKUParallelUnit(rowWithRawData);
                    if(!SKUParallelUnits.Contains(unit)) 
                    {
                        SKUParallelUnits.Add(unit);
                    }
                }
            }
            else if(isClient) 
            {
                var skuUnits = from rowWithRawData in rowsWithRawData
                                let unit = new SKUClientReportDataUnit(rowWithRawData)
                                group unit by unit.SKUNumber 
                                into unitGroup  
                                select unitGroup;

                foreach (var skuUnit in skuUnits)
                {
                    foreach (var item in skuUnit)
                    {
                        SKU sku = new(item, DeadlineDays);
                        SKUs.RetrieveAndUpdateOrAddValue(sku.SKUNumber, sku, isMulty);
                    }
                }
            }

            return reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => SkuRansomsBalanceUnits.Count(),
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ => SkuRemainingBalanceUnits.Count(),
                SourceSKUReportFileKey.Shipped or SourceSKUReportFileKey.Transfer => SkuShippedBalanceUnits.Count(),
                SourceSKUReportFileKey.WriteOffShortages => SkuWriteOffBalanceUnits.Count(),
                SourceSKUReportFileKey.Additional => workSheetName switch 
                {
                    ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME => SkuCompensationBalanceUnits.Count(),
                    ExcelManager.RESORTS_REPORT_WORKSHEET_NAME => SkuResortBalanceUnits.Count(),
                    ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME => SKUParallelUnits.Count,
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
            bool isWB = skuReportFileKey == SourceSKUReportFileKey.RansomsWB || skuReportFileKey == SourceSKUReportFileKey.RemainingWB;
            bool isOZ = skuReportFileKey == SourceSKUReportFileKey.RansomsOZ || skuReportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isRansoms = skuReportFileKey == SourceSKUReportFileKey.RansomsWB || skuReportFileKey == SourceSKUReportFileKey.RansomsOZ;
            bool isRemaining = skuReportFileKey == SourceSKUReportFileKey.RemainingWB || skuReportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isShipped = skuReportFileKey == SourceSKUReportFileKey.Shipped;
            bool isTransfer = skuReportFileKey == SourceSKUReportFileKey.Transfer;
            bool isWriteOff = skuReportFileKey == SourceSKUReportFileKey.WriteOffShortages;
            bool isCompensation = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResorts = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;
            bool isParellel = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.PARALLEL_SKUS_WORKSHEET_NAME;

            // Dummy
            await Task.Delay(1);

            // Definition of a marketplace and collection
            Marketplace marketplace = isWB ? Marketplace.WB : Marketplace.OZ;          
            IRepository<int, List<ParsingRowEntity>>? parsingEntityCollection = skuReportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ =>
                   parsingEntityCollection = SkuRansomsBalanceUnits,
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ =>
                    parsingEntityCollection = SkuRemainingBalanceUnits,
                SourceSKUReportFileKey.Shipped or SourceSKUReportFileKey.Transfer =>
                    parsingEntityCollection = SkuShippedBalanceUnits,  
                SourceSKUReportFileKey.WriteOffShortages =>
                    parsingEntityCollection = SkuWriteOffBalanceUnits, 
                SourceSKUReportFileKey.Additional => workSheetName switch 
                {
                    ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuCompensationBalanceUnits,
                    ExcelManager.RESORTS_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuResortBalanceUnits,
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

                        SKURansomsBalance ransomsBalance = new(units.ToList());

                        var skuMaxRansomDate = units
                            .Select(u => u.SKURansomDate)
                            .Max();

                        var averagePrice = units
                            .Average(u => u.SKUCost);

                        SKU tempSKU = new(marketplace, skuNumber, ransomsBalance, averagePrice, skuMaxRansomDate, DeadlineDays);
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, tempSKU);
                    }
                    else if (isRemaining)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKURemainingBalanceDataUnit>().ToList()) ??
                            throw new InvalidCastException("Unable to cast collection to IList<SKURemainingBalanceDataUnit>.");

                        var skuBrandName = units.First().SKUBrandAndName;

                        SKURemainingBalance remainingBalance = new(skuNumber, skuBrandName, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance, IS_REMOVE_BRAND_FROM_NAME));
                    } 
                    else if (isShipped || isTransfer)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUShippedDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUShippedDataUnit>.");
                        
                        SKURemainingBalance remainingBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance, IS_REMOVE_BRAND_FROM_NAME));
                    } 
                    else if(isWriteOff) 
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUWriteOffDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUWriteOffDataUnit>.");

                        if(SKUs.GetValue(skuNumber, out SKU? sKU)) 
                        {
                            SKUWriteOffBalance writeOffBalance = new(skuNumber, units.ToList());
                            SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, marketplace, writeOffBalance));
                        }
                    }
                    else if(isCompensation) 
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUCompensationDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUCompensationDataUnit>.");

                        SKUCompensationBalance skuCompensationBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, skuCompensationBalance));
                    }
                    else if(isResorts && !isRawReport)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUResortDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUResortDataUnit>.");

                        SKUResortBalance skuResortBalance = new(skuNumber, [.. units]);
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, skuResortBalance));
                        
                        UpdateSKUsToResortCollection(skuResortBalance);
                    }
                }
            }

            if(isParellel && SKUParallelUnits.Count > 0) 
            {
                foreach (var skuParallelUnit in SKUParallelUnits)
                {
                    SKU tempSKU = new(skuParallelUnit.SKUNumber, true);
                    SKUs.RetrieveAndUpdateOrAddValue(skuParallelUnit.SKUNumber, tempSKU);
                }
            }
        }

        private void UpdateSKUsToResortCollection(SKUResortBalance skuResortBalance)
        { 
            int skuResortFromNumber = skuResortBalance.SKUFromResortNumber;

            ResortBalanceUnit? writeOffResortIn1CReceiptItem = skuResortBalance.GetSkusResortToSum(SKUResortBalanceTarget.ResortIn1CReceipt);
            ResortBalanceUnit? writeOff1CResortItems = skuResortBalance.GetSkusResortToSum(SKUResortBalanceTarget.ResortIn1C);
            ResortBalanceUnit? conceptualResortItems = skuResortBalance.GetSkusResortToSum(SKUResortBalanceTarget.Conceptual);

            List<ResortBalanceUnit>? skusResortToUnits = [];
            skusResortToUnits.AddRange([writeOffResortIn1CReceiptItem, writeOff1CResortItems, conceptualResortItems]);

            if (skusResortToUnits is not null && skusResortToUnits.Count != 0)
            {
                // Проходим по юнитам
                foreach (var skusResortToUnit in skusResortToUnits)
                {
                    // Цель пересорта
                    var resortToTarget = skusResortToUnit.BalanceTarget;

                    Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortsFromDict;
                    Dictionary<SKUResortBalanceTarget, int>? resortItemsByTarget;
                    int resortSumByTarget = 0;

                    // Проходим по элементам словаря внутри юнита (арт пересорта, сумма)
                    foreach ((int resortToNumber, int resortToSum) in skusResortToUnit.ResortTo)
                    {
                        // Если по артикулу resortToNumber есть сведения...
                        if (SKUToResorts.TryGetValue(resortToNumber, out Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortsFromExistingDict) &&
                        resortsFromExistingDict is not null &&
                        resortsFromExistingDict.Count > 0)
                        {
                            resortsFromDict = resortsFromExistingDict;

                            if(resortsFromDict.TryGetValue(skuResortFromNumber, out Dictionary<SKUResortBalanceTarget, int>? resortDictTargets)) 
                            {
                                resortItemsByTarget = resortDictTargets;

                                if(resortItemsByTarget.TryGetValue(resortToTarget, out int sum)) 
                                {
                                    resortSumByTarget = sum;
                                    resortSumByTarget += resortToSum;
                                } 
                                else 
                                {
                                    resortSumByTarget = resortToSum;
                                }

                                resortItemsByTarget[resortToTarget] = resortSumByTarget;
                            } 
                            else 
                            {
                                // Сумма пересортов
                                resortSumByTarget = resortToSum;

                                // Новый словарь: Цель пересортов - Сумма пересортов
                                resortItemsByTarget = new() 
                                {
                                    {resortToTarget, resortSumByTarget}
                                };
                            }  

                            resortsFromDict[skuResortFromNumber] = resortItemsByTarget;
                        } 
                        // Если по артикулу resortToNumber нет сведений, создается новая запись
                        else 
                        {
                            resortItemsByTarget = new() 
                            {
                                {resortToTarget, resortToSum}
                            };

                            resortsFromDict = new() 
                            {
                                { skuResortFromNumber, resortItemsByTarget }
                            };
                        } 

                        SKUToResorts[resortToNumber] = resortsFromDict;
                    }                    
                }
            }
        }

        public async Task<List<SKUForTargetReports>> MakeSkusForTargetReport(bool isRawReport, bool isMulty, int traceDelay) 
        {
            List<SKUForTargetReports> skuForTargetReports = [];
            foreach (var (_, targetSku) in SKUs.GetValuesList())
            {
                SKUForTargetReports newValue;
                if(SKUToResorts.TryGetValue(targetSku.SKUNumber, out Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues) && 
                    resortFromSkusValues is not null &&
                    resortFromSkusValues.Count > 0) 
                {
                    newValue = new(targetSku, isRawReport, isMulty,  traceDelay, resortFromSkusValues);
                } 
                else 
                {
                    newValue = new(targetSku, isRawReport, isMulty, traceDelay);
                }
                
                skuForTargetReports.Add(newValue);
            }

            await Task.Delay(10); // dummy 
            
            return skuForTargetReports;
        }
        
        #endregion
    }
}
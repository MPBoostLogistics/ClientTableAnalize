using System.Collections.Concurrent;
using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Interfaces;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.Repositories;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;
using OfficeOpenXml;

namespace ClientTableAnalizer.Objects.Managers
{
    public class SKUManager : BaseManager
    {
        #region Properties 
        public int DeadlineDays { get; private set; }
        public IRepository<int, SKU> SKUs { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuRansomsBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuRemainingBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuShippedBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuWriteOffBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuCompensationBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuResortBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuDummiesBalanceUnits { get; }
        // арткул на который пересорт (артикул с которого пересорт, количество)
        public ConcurrentDictionary <int, ConcurrentDictionary<int, int>> ResortSKUs {get; set;} = new();

        /// <summary>
        /// Returns number of SKUs in collection.
        /// </summary>
        /// <returns>Number of SKUs in collection.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public int SKUsCount() => SKUs.Count();
        private delegate bool FindSkusWithPredicate(SKU sku);
        private FindSkusWithPredicate? predicateHandler;

        #endregion

        #region Constructors and destructors
        public SKUManager(ExcelManager excelReportManager)
        {
            SKUs = new SKURepo();
            SkuRansomsBalanceUnits = new ParsingRowEntityRepo();
            SkuRemainingBalanceUnits = new ParsingRowEntityRepo();
            SkuShippedBalanceUnits = new ParsingRowEntityRepo();
            SkuWriteOffBalanceUnits = new ParsingRowEntityRepo();
            SkuCompensationBalanceUnits = new ParsingRowEntityRepo();
            SkuResortBalanceUnits = new ParsingRowEntityRepo();
            SkuDummiesBalanceUnits = new ParsingRowEntityRepo();
        }
        #endregion

        #region Functionality

        /// <summary>
        /// Sets period before deadline when paying a client (in days).
        /// </summary>
        /// <param name="days">Period until deadline, days</param>
        /// <remarks><i>New version, tested</i></remarks>
        public void SetDeadlineDays(int days) => DeadlineDays = days;

        /// <summary>
        /// Processes data from seller ransoms reports.
        /// </summary>
        /// <param name="reportFileKey">Key of source file with report.</param>
        /// <returns>Function execution time in seconds.</returns>
        /// <remarks><i>New version</i></remarks>
        public async Task<double> ReceivingAndProcessingRansomsData(SourceSKUReportFileKey reportFileKey)
        {
            double time = 0.00;

            if (ExcelManager.GetExcelWorksheetStringNamesByKey(reportFileKey, out List<string>? workSheetNames) &&
                workSheetNames is not null &&
                workSheetNames.Count > 0)
            {
                for (int i = 0; i < workSheetNames.Count; i++)
                {
                    var currentName = workSheetNames[i];

                    Task<double> processsingTask = ProcessingData(reportFileKey, currentName);
                    await Task.Run(async () => await processsingTask);
                    processsingTask.Wait();

                    time += processsingTask.Result;
                }
            }

            // Finish
            return time;
        }

        private async Task<double> ProcessingData(SourceSKUReportFileKey reportFileKey, string workSheetName)
        {
            // Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Get source excel worksheet by name
            ExcelManager.GetExcelWorksheetByName(workSheetName, reportFileKey, out ExcelWorksheet? currentWorksheet);
            if (currentWorksheet is null)
            {
                await Task.Delay(100);
                OnManagerReached(new($"There is no worksheet named '{workSheetName}' in workbook file at key {reportFileKey}.", 2000, true));
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
                List<Dictionary<string, ItemForParsing>> rawRowsItems = new() { };
                foreach (var sourceReportRowsRange in rangeSourceReportRows)
                {
                    sourceReportRowsRange.CollectionDataOnReadRows.ForEach(rawRowsItems.Add);
                }

                // Processing:
                // 1. Save balances for sku's
                SaveSKUBalanceUnits(reportFileKey, workSheetName, rawRowsItems);

                // 2. Create or update sku's from balance units 
                var makeSKUsTask = UpdateSKUsFromBalanceUnits(reportFileKey, workSheetName);
                await Task.Run(async () => await makeSKUsTask);
                makeSKUsTask.Wait();

                // 3. Report
                static bool FindSkusWithRansoms(SKU sku) => sku.SKURansomsBalance != null;
                static bool FindSkusWithRemaining(SKU sku) => sku.SKURemainingBalance != null;
                static bool FindSkusWithShipped(SKU sku) => sku.SKURemainingBalance?.SKUShipped != null;
                static bool FindSkusWithWriteOff(SKU sku) => sku.SKUWriteOffBalance is not null && sku.SKUWriteOffBalance.SKUWriteOff > 0;
                static bool FindSkusWithCompensation(SKU sku) => sku.SKUCompensationBalance != null;
                static bool FindSkusWithResort(SKU sku) => sku.SKUResortBalance != null;

                    var message = string.Empty;

                    switch (reportFileKey)
                    {
                        case SourceSKUReportFileKey.RansomsWB:
                        case SourceSKUReportFileKey.RansomsOZ:
                            predicateHandler = FindSkusWithRansoms;
                            message = "Артикулов в выкупах: ";
                            break;
                        case SourceSKUReportFileKey.RemainingWB:
                        case SourceSKUReportFileKey.RemainingOZ:
                            predicateHandler = FindSkusWithRemaining;
                            message = "Артикулов в приходе и остатоках склада: ";
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
                                case ExcelManager.DUMMIES_REPORT_WORKSHEET_NAME:
                                    // ...
                                    break;
                            };
                            break;
                        case SourceSKUReportFileKey.Shipped:
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
                    OnManagerReached(new(message, 1000, false));
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
        /// <remarks><i>New version, tested</i></remarks>
        public int SaveSKUBalanceUnits(SourceSKUReportFileKey reportFileKey, string workSheetName, List<Dictionary<string, ItemForParsing>> rowsWithRawData)
        {

            bool isRansoms = reportFileKey == SourceSKUReportFileKey.RansomsWB || reportFileKey == SourceSKUReportFileKey.RansomsOZ;
            bool isRemaining = reportFileKey == SourceSKUReportFileKey.RemainingWB || reportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isShipped = reportFileKey == SourceSKUReportFileKey.Shipped;
            bool isWriteOff = reportFileKey == SourceSKUReportFileKey.WriteOffShortages;
            bool isCompensation = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResort = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;

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
            else if(isShipped) 
            {
                var skuRShippedDataUnits = from rowWithRawData in rowsWithRawData
                                           let unit = new SKUShippedDataUnit(rowWithRawData)
                                           group unit by unit.SKUNumber 
                                           into unitGroup  
                                           select unitGroup;
                
                foreach (var group in skuRShippedDataUnits)
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
            else if(isResort) 
            {
                var skuResortDataUnits = from rowWithRawData in rowsWithRawData
                                         let unit = new SKUResortDataUnit(rowWithRawData)
                                         group unit by unit.SKUNumber 
                                         into unitGroup  
                                         select unitGroup;

                foreach (var skuResortGroup in skuResortDataUnits)
                {
                    var skuNumber = skuResortGroup.Key;

                    var list = (((List<SKUResortDataUnit>?)skuResortGroup.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKUResortDataUnit> к List<ParsingRowEntity>");

                    SkuResortBalanceUnits.RetrieveAndUpdateOrAddValue(skuNumber, list);
                }
            }

            return reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => SkuRansomsBalanceUnits.Count(),
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ => SkuRemainingBalanceUnits.Count(),
                SourceSKUReportFileKey.Shipped => SkuShippedBalanceUnits.Count(),
                SourceSKUReportFileKey.WriteOffShortages => SkuWriteOffBalanceUnits.Count(),
                SourceSKUReportFileKey.Additional => workSheetName switch 
                {
                    ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME => SkuCompensationBalanceUnits.Count(),
                    ExcelManager.RESORTS_REPORT_WORKSHEET_NAME => SkuResortBalanceUnits.Count(),
                    ExcelManager.DUMMIES_REPORT_WORKSHEET_NAME => SkuDummiesBalanceUnits.Count(),
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
        public async Task UpdateSKUsFromBalanceUnits(SourceSKUReportFileKey skuReportFileKey, string workSheetName)
        {
            bool isWB = skuReportFileKey == SourceSKUReportFileKey.RansomsWB || skuReportFileKey == SourceSKUReportFileKey.RemainingWB;
            bool isOZ = skuReportFileKey == SourceSKUReportFileKey.RansomsOZ || skuReportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isRansoms = skuReportFileKey == SourceSKUReportFileKey.RansomsWB || skuReportFileKey == SourceSKUReportFileKey.RansomsOZ;
            bool isRemaining = skuReportFileKey == SourceSKUReportFileKey.RemainingWB || skuReportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isShipped = skuReportFileKey == SourceSKUReportFileKey.Shipped;
            bool isWriteOff = skuReportFileKey == SourceSKUReportFileKey.WriteOffShortages;
            bool isCompensation = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResorts = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;
            bool isDummies = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.DUMMIES_REPORT_WORKSHEET_NAME;

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
                SourceSKUReportFileKey.Shipped =>
                    parsingEntityCollection = SkuShippedBalanceUnits,  
                SourceSKUReportFileKey.WriteOffShortages =>
                    parsingEntityCollection = SkuWriteOffBalanceUnits, 
                SourceSKUReportFileKey.Additional => workSheetName switch 
                {
                    ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuCompensationBalanceUnits,
                    ExcelManager.RESORTS_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuResortBalanceUnits,
                    ExcelManager.DUMMIES_REPORT_WORKSHEET_NAME => 
                        parsingEntityCollection = SkuDummiesBalanceUnits,
                    _ => parsingEntityCollection = null
                },
                _ => parsingEntityCollection = null,
            };

            bool isCollectionValid = parsingEntityCollection is not null && parsingEntityCollection.Count() > 0;

            if(isCollectionValid) 
            {
                List<(int key, List<ParsingRowEntity> value)>? listCollection = parsingEntityCollection!.GetValuesList().ToList();
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
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance));
                    } 
                    else if (isShipped)
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUShippedDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUShippedDataUnit>.");
                        
                        SKURemainingBalance remainingBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance));
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
                    else if(isResorts) 
                    {
                        var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUResortDataUnit>().ToList()) ??
                            throw new InvalidCastException("Cannot cast collection to IList<SKUResortDataUnit>.");
                        
                        SKUResortBalance skuResortBalance = new(skuNumber, units.ToList());
                        SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(skuNumber, skuResortBalance));

                        foreach (KeyValuePair<int, (int resortSKUCount, string? resortSKUComment)> item in skuResortBalance.ResortSKUs)
                        {
                            var skuFromResort = skuNumber;
                            var skuForResort = item.Key;
                            var skuForResortCount = item.Value.resortSKUCount;

                            if(ResortSKUs.TryGetValue(skuForResort, out ConcurrentDictionary<int, int>? value)) 
                            {
                                if(value.TryGetValue(skuFromResort, out int comparisonValue)) 
                                {
                                    var newValue = comparisonValue += skuForResortCount;
                                    value.TryUpdate(skuFromResort, newValue, comparisonValue);
                                }
                                else 
                                {
                                    value.TryAdd(skuFromResort, skuForResortCount);
                                }
                            } 
                            else 
                            {
                                var newDict = new ConcurrentDictionary<int, int>();
                                newDict.TryAdd(skuFromResort, skuForResortCount);
                                ResortSKUs.TryAdd(skuForResort, newDict);
                            }
                        }
                        
                    }
                    else if(isDummies)
                    {
                        // ... 
                    }
                }
            }
        }
        
        public List<SKUForTargetReports> MakeSkusForTargetReport() 
        {
            List<SKUForTargetReports> sKUForTargetReports = new();
            foreach (var (key, sku) in SKUs.GetValuesList())
            {
                SKUForTargetReports newValue = ResortSKUs.TryGetValue(key, out var value) && value is not null ? 
                    new(sku, value) : 
                    new(sku);
                    
                sKUForTargetReports.Add(newValue);
            }
            return sKUForTargetReports
                .OrderByDescending(el => el.SKUDate)
                .ToList();
        }
        
        #endregion
    }
}
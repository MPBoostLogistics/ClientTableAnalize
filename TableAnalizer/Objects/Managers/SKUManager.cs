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
        public IRepository<int, List<ParsingRowEntity>> SkuCompensationBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuResortBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuDummiesBalanceUnits { get; }

        /// <summary>
        /// Returns number of SKUs in collection.
        /// </summary>
        /// <returns>Number of SKUs in collection.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public int SKUsCount() => SKUs.Count();

        private delegate bool FindSkusWithPredicate(SKU sku);  
        private FindSkusWithPredicate predicateHandler;

        #endregion

        #region Constructors and destructors
        public SKUManager(ExcelManager excelReportManager)
        {
            SKUs = new SKURepo();
            SkuRansomsBalanceUnits = new ParsingRowEntityRepo();
            SkuRemainingBalanceUnits = new ParsingRowEntityRepo();
            SkuShippedBalanceUnits = new ParsingRowEntityRepo();
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
            // Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Get names of source excel worksheet by report file key
            if(ExcelManager.GetExcelWorksheetsByKey(reportFileKey, out List<string> workSheetNames)) 
            {
                foreach (var workSheetName in workSheetNames!)
                {
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
                            sourceReportRowsRange.RowsWithRawData.ForEach(rawRowsItems.Add);
                        }
                        // Processings:
                        // 1. Save balances for sku's
                        SaveSKUBalanceUnits(reportFileKey, workSheetName, rawRowsItems);

                        // 2. Create or update sku's from balance units 
                        var makeSKUsTask = UpdateSKUsFromBalanceUnits(reportFileKey, workSheetName);
                        await Task.Run(async () => await makeSKUsTask);
                        makeSKUsTask.Wait();

                        // Report
                        static bool FindSkusWithRansoms(SKU sku) => sku.SKURansomsBalance != null;
                        static bool FindSkusWithRemaining(SKU sku) => sku.SKURemainingBalance != null;
                        static bool FindSkusWithShipped(SKU sku) => sku.SKURemainingBalance?.SKUShipped != null;
                        static bool FindSkusWithCompensation(SKU sku) => sku.SKUCompensationBalance != null;

                        var message = string.Empty;

                        switch (reportFileKey)
                        {
                            case SourceSKUReportFileKey.RansomsWB:
                                predicateHandler = FindSkusWithRansoms;
                                message = "Артикулов из выкупов WB: ";
                                break;
                            case SourceSKUReportFileKey.RansomsOZ:
                                predicateHandler = FindSkusWithRansoms;
                                message = "Артикулов из выкупов OZ: ";
                                break;
                            case SourceSKUReportFileKey.RemainingWB:
                                predicateHandler = FindSkusWithRemaining;
                                message = "Артикулов из прихода и остатоков WB: ";
                                break;
                            case SourceSKUReportFileKey.RemainingOZ:
                                predicateHandler = FindSkusWithRemaining;
                                message = "Артикулов из прихода и остатоков OZ: ";
                                break;
                            case SourceSKUReportFileKey.Additional:
                                switch(workSheetName) 
                                {
                                    case ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME:
                                        predicateHandler = FindSkusWithCompensation;
                                        message = "Артикулов из компенсации: ";
                                        break;
                                    case ExcelManager.RESORTS_REPORT_WORKSHEET_NAME:
                                        // ...
                                        break;
                                    case ExcelManager.DUMMIES_REPORT_WORKSHEET_NAME:
                                        // ...
                                        break;
                                };
                                break;
                            case SourceSKUReportFileKey.Shipped:
                                 predicateHandler = FindSkusWithShipped;
                                 message = "Артикулов из расхода со склада: ";
                                break;
                            default:
                                break;
                        }

                        // Report
                        int skusWithPredicateCount = SKUs.GetValueCount(predicateHandler.Invoke);
                        message = string.Concat(message, skusWithPredicateCount);
                        OnManagerReached(new(message, 1000, false));
                        break;
                    }
                }
            }

            // Finish
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
            bool isCompensation = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResort = reportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;


            List<SourceSKUReportFileKey> validKeys = new()
            {
                SourceSKUReportFileKey.RansomsWB,
                SourceSKUReportFileKey.RansomsOZ,
                SourceSKUReportFileKey.RemainingWB,
                SourceSKUReportFileKey.RemainingOZ,
                SourceSKUReportFileKey.Shipped,
                SourceSKUReportFileKey.Additional
            };

            if (!validKeys.Contains(reportFileKey))
            {
                OnManagerReached(new ManagerReachedEventArgs($"An incorrect key ({reportFileKey}) was passed to save balance units", 2000, true));
                return 0;
            }

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

                foreach (var group in skuResortDataUnits)
                {
                    var list = (((List<SKUResortDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKUResortDataUnit> к List<ParsingRowEntity>");
                    
                    SkuResortBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            }

            return reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => SkuRansomsBalanceUnits.Count(),
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ => SkuRemainingBalanceUnits.Count(),
                SourceSKUReportFileKey.Shipped => SkuShippedBalanceUnits.Count(),
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
            bool isCompensation = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.COMPENSATION_REPORT_WORKSHEET_NAME;
            bool isResorts = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.RESORTS_REPORT_WORKSHEET_NAME;
            bool isDummies = skuReportFileKey == SourceSKUReportFileKey.Additional && workSheetName == ExcelManager.DUMMIES_REPORT_WORKSHEET_NAME;

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

            if (parsingEntityCollection == null || parsingEntityCollection.Count() == 0)
            {
                await Task.Delay(100);
                OnManagerReached(new("It is impossible to form a dict of articles from an empty dictionary of 'raw' data.", 2000, true));
                return;
            }

            List<(int key, List<ParsingRowEntity> value)>? listCollection = parsingEntityCollection.GetValuesList().ToList();
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

                    SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, ransomsBalance, averagePrice, skuMaxRansomDate));
                }
                else if (isRemaining)
                {
                    var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKURemainingBalanceDataUnit>().ToList()) ??
                        throw new InvalidCastException("Unable to cast collection to IList<SKURemainingBalanceDataUnit>.");

                    SKURemainingBalance remainingBalance = new(units.ToList());
                    SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance));
                } 
                else if(isShipped) 
                {
                    var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUShippedDataUnit>().ToList()) ??
                        throw new InvalidCastException("Cannot cast collection to IList<SKUShippedDataUnit>.");
                    
                    SKURemainingBalance remainingBalance = new(units.ToList());
                    SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance));
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

                }
                else if(isDummies)
                {
                    var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUDummiesDataUnit>().ToList()) ??
                        throw new InvalidCastException("Cannot cast collection to IList<SKUResortDataUnit>.");

                }
            }
        }

        // /// <summary>
        // /// Returns saved article by its number or null.
        // /// </summary>
        // /// <param name="articleNumber">Article number to look up in  dict.</param>
        // /// <param name="article">Link to a copy of article.</param>
        // /// <returns>Logical flag for performing an operation.</returns>
        // /// <remarks><i>New version, tested</i></remarks>
        // public bool GetSKUByNumber(int articleNumber, out SKU? article)
        // {
        //     article = _skus.TryGetValue(articleNumber, out SKU? value) ? value : null;
        //     return article != null;
        // }

        // /// <summary>
        // /// Saves ArticleRowInReport in corresponding article. 
        // /// </summary>
        // /// <param name="skuRowsInReport">Link to ArticleRowInReport.</param>
        // /// <param name="marketplace">Marketplace products.</param>
        // /// <returns>Time it takes to complete operation in seconds.</returns>
        // /// <remarks><i>New version, tested</i></remarks>
        // public double SaveSKURowsInReport(List<SKURowInReport> skuRowsInReport, Marketplace marketplace)
        // {
        //     var stopwatch = new Stopwatch();
        //     stopwatch.Start();

        //     foreach (var skuRowInReport in skuRowsInReport)
        //     {
        //         var skuNumber = skuRowInReport.SKUNumber;

        //         if (GetSKUByNumber(skuNumber, out SKU? sku) && sku != null)
        //         {
        //             sku.SetArticleRowInReport(skuRowInReport);
        //         }
        //         else
        //         {
        //             _skus.Add(skuNumber, new SKU(marketplace, skuNumber, skuRowInReport));
        //         }
        //     }

        //     stopwatch.Stop();

        //     return stopwatch.ElapsedMilliseconds / 1000;
        // }

        // /// <summary>
        // /// Performs analysis of elements in article collection.
        // /// </summary>
        // /// <exception cref="Exception">An exception occurred when trying to parse an empty collection.</exception>
        // /// <remarks><i>New version, tested</i></remarks>
        // public void AnalyzeSKUs()
        // {
        //     if (Equals(_skus.Count, 0))
        //         throw new Exception("An attempt to parse a dict with a number of elements - 0");

        //     foreach (var (key, value) in _skus)
        //         value.AnalyzeSKU(DeadlineDays);
        // }

        #endregion
    }
}